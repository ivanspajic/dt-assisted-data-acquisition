﻿using SimMixCustomPiece.Algorithms.Utilities;
using SimMixCustomPiece.Models;
using SimMixCustomPiece.Models.LinearSegments;

namespace SimMixCustomPiece.Algorithms
{
    /// <summary>
    /// Implements the Sim-Piece Piece-wise Linear Approximation algorithm.
    /// </summary>
    public static class SimPiece
    {
        /// <summary>
        /// Performs lossy compression using the Sim-Piece algorithm.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilonPercentage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static List<GroupedLinearSegment> Compress(List<Point> timeSeries, double epsilonPercentage)
        {
            if (timeSeries == null || timeSeries.Count < 2 || epsilonPercentage <= 0)
                throw new ArgumentException("The time series must contain at least 2 data points, and epsilon must be a percentage greater than 0.");

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var segmentGroups = GetSegmentGroupsFromTimeSeries(timeSeries, epsilon);
            var linearSegments = GetLinearSegmentsFromSegmentGroups(segmentGroups);

            return linearSegments;
        }

        /// <summary>
        /// Decompresses the compressed time series and returns reconstructed data points, each fitting within +/- epsilon of the original value.
        /// </summary>
        /// <param name="compressedTimeSeries"></param>
        /// <param name="epsilonPercentage"></param>
        /// <returns></returns>
        public static List<Point> Decompress(List<GroupedLinearSegment> compressedTimeSeries, long lastPointTimestamp)
        {
            var segments = GetSegmentsFromGroupedLinearSegments(compressedTimeSeries);
            var reconstructedTimeSeries = PlaUtils.GetReconstructedTimeSeriesFromSegments(segments, lastPointTimestamp);

            return reconstructedTimeSeries;
        }

        /// <summary>
        /// Phase 1 of the algorithm.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        private static Dictionary<double, List<Segment>> GetSegmentGroupsFromTimeSeries(List<Point> timeSeries, double epsilon)
        {
            var segmentGroups = new Dictionary<double, List<Segment>>();

            var currentPoint = timeSeries[0];

            // In cases of all points having the same value, epsilon will be 0, so assign a default value.
            var currentQuantizedValue = currentPoint.Value;

            if (epsilon > 0)
            {
                currentQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentPoint.Value, epsilon);
            }
            
            var currentUpperBoundGradient = double.PositiveInfinity;
            var currentLowerBoundGradient = double.NegativeInfinity;

            for (var i = 0; i < timeSeries.Count - 1; i++)
            {
                var nextPoint = timeSeries[i + 1];

                // Use the point-slope form to check whether the next point's value is outside of the current upper and lower bounds.
                if (nextPoint.Value > currentUpperBoundGradient * (nextPoint.SimpleTimestamp - currentPoint.SimpleTimestamp) + currentQuantizedValue + epsilon ||
                    nextPoint.Value < currentLowerBoundGradient * (nextPoint.SimpleTimestamp - currentPoint.SimpleTimestamp) + currentQuantizedValue - epsilon)
                {
                    // In case of the value being outside of the bounds, finalize the creation of the current segment and add it to the group for
                    // this origin value.
                    if (!segmentGroups.ContainsKey(currentQuantizedValue))
                        segmentGroups[currentQuantizedValue] = [];

                    segmentGroups[currentQuantizedValue].Add(new Segment
                    {
                        LowerBoundGradient = currentLowerBoundGradient,
                        UpperBoundGradient = currentUpperBoundGradient,
                        StartTimestamp = currentPoint.SimpleTimestamp,
                        EndTimestamp = timeSeries[i].SimpleTimestamp
                    });

                    // Reset before continuing.
                    currentPoint = timeSeries[i];

                    currentQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentPoint.Value, epsilon);
                    currentUpperBoundGradient = double.PositiveInfinity;
                    currentLowerBoundGradient = double.NegativeInfinity;
                }

                // Use the point-slope form to check if the next point is below the upper bound but more than epsilon away.
                if (nextPoint.Value < currentUpperBoundGradient * (nextPoint.SimpleTimestamp - currentPoint.SimpleTimestamp) + currentQuantizedValue - epsilon)
                    // In case of being more than epsilon away, adjust the current upper bound to be within epsilon away from the next point.
                    currentUpperBoundGradient = (nextPoint.Value - currentQuantizedValue + epsilon) / (nextPoint.SimpleTimestamp - currentPoint.SimpleTimestamp);

                // Use the point-slope form to check if the next point is above the lower bound but mor than epsilon away.
                if (nextPoint.Value > currentLowerBoundGradient * (nextPoint.SimpleTimestamp - currentPoint.SimpleTimestamp) + currentQuantizedValue + epsilon)
                    // In case of being more than epsilon away, adjust the current lower bound to be within epsilon away from the next point.
                    currentLowerBoundGradient = (nextPoint.Value - currentQuantizedValue - epsilon) / (nextPoint.SimpleTimestamp - currentPoint.SimpleTimestamp);
            }

            // Add the segment still under creation at the end of the time series.
            if (!segmentGroups.ContainsKey(currentQuantizedValue))
                segmentGroups[currentQuantizedValue] = [];

            segmentGroups[currentQuantizedValue].Add(new Segment
            {
                LowerBoundGradient = currentLowerBoundGradient,
                UpperBoundGradient = currentUpperBoundGradient,
                StartTimestamp = currentPoint.SimpleTimestamp,
                EndTimestamp = timeSeries[^1].SimpleTimestamp
            });

            return segmentGroups;
        }

        /// <summary>
        /// Phase 2 of the algorithm.
        /// </summary>
        /// <param name="segmentGroups"></param>
        /// <returns></returns>
        private static List<GroupedLinearSegment> GetLinearSegmentsFromSegmentGroups(Dictionary<double, List<Segment>> segmentGroups)
        {
            var linearSegmentList = new List<GroupedLinearSegment>();

            foreach (var segmentGroupPair in segmentGroups)
            {
                var currentLinearSegment = new GroupedLinearSegment([])
                {
                    QuantizedValue = segmentGroupPair.Key,
                    UpperBoundGradient = double.PositiveInfinity,
                    LowerBoundGradient = double.NegativeInfinity
                };

                segmentGroupPair.Value.Sort(PlaUtils.CompareSegmentsByLowerBound);

                foreach (var currentSegment in segmentGroupPair.Value)
                {
                    // Check if there is any overlap between the current segment and the current bounds of the group.
                    if (currentSegment.LowerBoundGradient <= currentLinearSegment.UpperBoundGradient &&
                        currentSegment.UpperBoundGradient >= currentLinearSegment.LowerBoundGradient)
                    {
                        // In case of an overlap, tighten the upper and lower bounds further.
                        currentLinearSegment.UpperBoundGradient = Math.Min(currentLinearSegment.UpperBoundGradient, currentSegment.UpperBoundGradient);
                        currentLinearSegment.LowerBoundGradient = Math.Max(currentLinearSegment.LowerBoundGradient, currentSegment.LowerBoundGradient);
                        currentLinearSegment.Timestamps.Add(currentSegment.StartTimestamp);
                    }
                    else
                    {
                        // In case of no overlaps, finalize the creation of the current segment.
                        linearSegmentList.Add(currentLinearSegment);

                        currentLinearSegment = new GroupedLinearSegment([])
                        {
                            QuantizedValue = segmentGroupPair.Key,
                            UpperBoundGradient = currentSegment.UpperBoundGradient,
                            LowerBoundGradient = currentSegment.LowerBoundGradient
                        };
                        currentLinearSegment.Timestamps.Add(currentSegment.StartTimestamp);
                    }
                }

                // Add the linear segment still under creation.
                linearSegmentList.Add(currentLinearSegment);
            }

            return linearSegmentList;
        }

        /// <summary>
        /// Gets segments for reconstructing the original time series from the grouped linear segments.
        /// </summary>
        /// <param name="groupedLinearSegments"></param>
        /// <returns></returns>
        private static List<Segment> GetSegmentsFromGroupedLinearSegments(List<GroupedLinearSegment> groupedLinearSegments)
        {
            var segments = new List<Segment>();

            foreach (var groupedLinearSegment in groupedLinearSegments)
            {
                foreach (var timestamp in groupedLinearSegment.Timestamps)
                {
                    // One of the gradient properties can be used for the gradient from the grouped linear segment.
                    segments.Add(new Segment
                    {
                        QuantizedValue = groupedLinearSegment.QuantizedValue,
                        UpperBoundGradient = groupedLinearSegment.Gradient,
                        StartTimestamp = timestamp
                    });
                }
            }

            return segments;
        }
    }
}
