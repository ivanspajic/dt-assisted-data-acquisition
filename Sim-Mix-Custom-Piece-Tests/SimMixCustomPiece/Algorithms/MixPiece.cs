using SimMixCustomPiece.Algorithms.Utilities;
using SimMixCustomPiece.Models;
using SimMixCustomPiece.Models.LinearSegments;

namespace SimMixCustomPiece.Algorithms
{
    /// <summary>
    /// Implements the Mix-Piece Piece-wise Linear Approximation algorithm.
    /// </summary>
    public static class MixPiece
    {
        /// <summary>
        /// Performs lossy compression using the Mix-Piece algorithm.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilonPercentage"></param>
        /// <returns></returns>
        public static Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> Compress(List<Point> timeSeries, double epsilonPercentage)
        {
            if (timeSeries == null || timeSeries.Count < 2 || epsilonPercentage <= 0)
                throw new ArgumentException("The time series must contain at least 2 data points, and epsilon must be a percentage greater than 0.");

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var segmentGroups = GetSegmentGroupsFromTimeSeries(timeSeries, epsilon);
            var linearSegmentGroups = GetLinearSegmentGroupsFromSegmentGroups(segmentGroups);

            return linearSegmentGroups;
        }

        /// <summary>
        /// Decompresses the compressed time series and returns reconstructed data points, each fitting within +/- epsilon of the original value.
        /// </summary>
        /// <param name="compressedTimeSeries"></param>
        /// <param name="lastPointTimestamp"></param>
        /// <returns></returns>
        public static List<Point> Decompress(Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedTimeSeries, long lastPointTimestamp)
        {
            var segments = GetSegmentsFromAllLinearSegments(compressedTimeSeries);
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

            var currentFloorQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentPoint.Value, epsilon);
            var currentCeilingQuantizedValue = PlaUtils.GetCeilingQuantizedValue(currentPoint.Value, epsilon);
            var currentFloorUpperBoundGradient = double.PositiveInfinity;
            var currentFloorLowerBoundGradient = double.NegativeInfinity;
            var currentCeilingUpperBoundGradient = double.PositiveInfinity;
            var currentCeilingLowerBoundGradient = double.NegativeInfinity;

            // Used for checking if the next point falls within the bounds for both types of quantized values.
            var floorIncludedAnotherPoint = true;
            var ceilingIncludedAnotherPoint = true;

            // Used for keeping track of how long each quantized value's segment is.
            var floorSegmentLength = 0;
            var ceilingSegmentLength = 0;

            for (var i = 0; i < timeSeries.Count - 1; i++)
            {
                var nextPoint = timeSeries[i + 1];

                // Use the point-slope form to check whether the next point's value is outside of the current upper and lower bounds for both types of
                // quantized values.
                if (nextPoint.Value > currentFloorUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentFloorQuantizedValue + epsilon ||
                    nextPoint.Value < currentFloorLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentFloorQuantizedValue - epsilon)
                {
                    // In this case, the floor quantized value's segment doesn't include the next point.
                    floorIncludedAnotherPoint = false;
                }

                if (nextPoint.Value > currentCeilingUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentCeilingQuantizedValue + epsilon ||
                    nextPoint.Value < currentCeilingLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentCeilingQuantizedValue - epsilon)
                {
                    // In this case, the ceiling quantized value's segment doesn't include the next point.
                    ceilingIncludedAnotherPoint = false;
                }

                if (floorIncludedAnotherPoint)
                    floorSegmentLength++;

                if (ceilingIncludedAnotherPoint)
                    ceilingSegmentLength++;

                // In case of the next point's value being outside of the bounds of both quantized values, finalize the creation of the current segment and
                // add it to the respective group.
                if (!floorIncludedAnotherPoint && !ceilingIncludedAnotherPoint)
                {
                    // If the floor quantized value's segment is longer than the ceiling quantized value's one, add it to the floor quantized value's group.
                    // Otherwise, add it to the ceiling quantized value's group.
                    if (floorSegmentLength > ceilingSegmentLength)
                    {
                        if (!segmentGroups.ContainsKey(currentFloorQuantizedValue))
                            segmentGroups[currentFloorQuantizedValue] = [];

                        segmentGroups[currentFloorQuantizedValue].Add(new Segment
                        {
                            LowerBoundGradient = currentFloorLowerBoundGradient,
                            UpperBoundGradient = currentFloorUpperBoundGradient,
                            StartTimestamp = currentPoint.Timestamp,
                            EndTimestamp = timeSeries[i].Timestamp,
                            QuantizedValue = currentFloorQuantizedValue,
                            Type = "Floor"
                        });
                    }
                    else
                    {
                        if (!segmentGroups.ContainsKey(currentCeilingQuantizedValue))
                            segmentGroups[currentCeilingQuantizedValue] = [];

                        segmentGroups[currentCeilingQuantizedValue].Add(new Segment
                        {
                            LowerBoundGradient = currentCeilingLowerBoundGradient,
                            UpperBoundGradient = currentCeilingUpperBoundGradient,
                            StartTimestamp = currentPoint.Timestamp,
                            EndTimestamp = timeSeries[i].Timestamp,
                            QuantizedValue = currentCeilingQuantizedValue,
                            Type = "Ceiling"
                        });
                    }

                    // Reset before continuing.
                    currentPoint = timeSeries[i];

                    currentFloorQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentPoint.Value, epsilon);
                    currentCeilingQuantizedValue = PlaUtils.GetCeilingQuantizedValue(currentPoint.Value, epsilon);
                    currentFloorUpperBoundGradient = double.PositiveInfinity;
                    currentFloorLowerBoundGradient = double.NegativeInfinity;
                    currentCeilingUpperBoundGradient = double.PositiveInfinity;
                    currentCeilingLowerBoundGradient = double.NegativeInfinity;

                    floorIncludedAnotherPoint = true;
                    ceilingIncludedAnotherPoint = true;

                    floorSegmentLength = 0;
                    ceilingSegmentLength = 0;
                }

                // Use the point-slope form to check if the next point is below the upper bound but more than epsilon away or above the lower bound but more
                // than an epsilon away, for both quantized value types. If true, adjust the bounds to fit the point within an epsilon.
                if (nextPoint.Value < currentFloorUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentFloorQuantizedValue - epsilon)
                    currentFloorUpperBoundGradient = (nextPoint.Value - currentFloorQuantizedValue + epsilon) /
                        (nextPoint.Timestamp - currentPoint.Timestamp);

                if (nextPoint.Value > currentFloorLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentFloorQuantizedValue + epsilon)
                    currentFloorLowerBoundGradient = (nextPoint.Value - currentFloorQuantizedValue - epsilon) /
                        (nextPoint.Timestamp - currentPoint.Timestamp);

                if (nextPoint.Value < currentCeilingUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentCeilingQuantizedValue - epsilon)
                    currentCeilingUpperBoundGradient = (nextPoint.Value - currentCeilingQuantizedValue + epsilon) /
                        (nextPoint.Timestamp - currentPoint.Timestamp);

                if (nextPoint.Value > currentCeilingLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentCeilingQuantizedValue + epsilon)
                    currentCeilingLowerBoundGradient = (nextPoint.Value - currentCeilingQuantizedValue - epsilon) /
                        (nextPoint.Timestamp - currentPoint.Timestamp);
            }

            // Add the segment still under creation at the end of the time series.
            if (floorSegmentLength > ceilingSegmentLength)
            {
                if (!segmentGroups.ContainsKey(currentFloorQuantizedValue))
                    segmentGroups[currentFloorQuantizedValue] = [];

                segmentGroups[currentFloorQuantizedValue].Add(new Segment
                {
                    LowerBoundGradient = currentFloorLowerBoundGradient,
                    UpperBoundGradient = currentFloorUpperBoundGradient,
                    StartTimestamp = currentPoint.Timestamp,
                    EndTimestamp = timeSeries[^1].Timestamp,
                    QuantizedValue = currentFloorQuantizedValue,
                    Type = "Floor"
                });
            }
            else
            {
                if (!segmentGroups.ContainsKey(currentCeilingQuantizedValue))
                    segmentGroups[currentCeilingQuantizedValue] = [];

                segmentGroups[currentCeilingQuantizedValue].Add(new Segment
                {
                    LowerBoundGradient = currentCeilingLowerBoundGradient,
                    UpperBoundGradient = currentCeilingUpperBoundGradient,
                    StartTimestamp = currentPoint.Timestamp,
                    EndTimestamp = timeSeries[^1].Timestamp,
                    QuantizedValue = currentCeilingQuantizedValue,
                    Type = "Ceiling"
                });
            }

            return segmentGroups;
        }

        /// <summary>
        /// Phase 2 of the algorithm.
        /// </summary>
        /// <param name="segmentGroups"></param>
        /// <returns></returns>
        private static Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> GetLinearSegmentGroupsFromSegmentGroups(Dictionary<double, List<Segment>> segmentGroups)
        {
            var groupedLinearSegmentList = new List<GroupedLinearSegment>();
            var stillUngroupedSegmentGroups = new Dictionary<double, List<Segment>>();

            foreach (var segmentGroupPair in segmentGroups)
            {
                var currentGroupedLinearSegment = new GroupedLinearSegment([])
                {
                    QuantizedValue = segmentGroupPair.Key,
                    UpperBoundGradient = double.PositiveInfinity,
                    LowerBoundGradient = double.NegativeInfinity
                };

                segmentGroupPair.Value.Sort(PlaUtils.CompareSegmentsByLowerBound);

                for (var i = 0; i < segmentGroupPair.Value.Count; i++)
                {
                    // Check if there is any overlap between the current segment and the current bounds of the group.
                    if (segmentGroupPair.Value[i].LowerBoundGradient <= currentGroupedLinearSegment.UpperBoundGradient &&
                        segmentGroupPair.Value[i].UpperBoundGradient >= currentGroupedLinearSegment.LowerBoundGradient)
                    {
                        // In case of an overlap, tighten the upper and lower bounds further.
                        currentGroupedLinearSegment.UpperBoundGradient = Math.Min(currentGroupedLinearSegment.UpperBoundGradient,
                            segmentGroupPair.Value[i].UpperBoundGradient);
                        currentGroupedLinearSegment.LowerBoundGradient = Math.Max(currentGroupedLinearSegment.LowerBoundGradient,
                            segmentGroupPair.Value[i].LowerBoundGradient);
                        currentGroupedLinearSegment.Timestamps.Add(segmentGroupPair.Value[i].StartTimestamp);
                    }
                    else
                    {
                        // Check if the segment group has already been added to.
                        if (currentGroupedLinearSegment.Timestamps.Count > 1)
                        {
                            // In case of the group already containing more than one timestamp, there must be overlaps between the previous segments. Hence,
                            // add the current group under creation to the list of groups.
                            groupedLinearSegmentList.Add(currentGroupedLinearSegment);
                        }
                        // Otherwise, add the current segment to the list of still ungrouped segments.
                        else
                        {
                            if (!stillUngroupedSegmentGroups.ContainsKey(segmentGroupPair.Key))
                                stillUngroupedSegmentGroups[segmentGroupPair.Key] = [];

                            stillUngroupedSegmentGroups[segmentGroupPair.Key].Add(new Segment
                            {
                                UpperBoundGradient = currentGroupedLinearSegment.UpperBoundGradient,
                                LowerBoundGradient = currentGroupedLinearSegment.LowerBoundGradient,
                                StartTimestamp = currentGroupedLinearSegment.Timestamps[0],
                                QuantizedValue = segmentGroupPair.Key,
                                EndTimestamp = segmentGroupPair.Value[i - 1].EndTimestamp,
                            });
                        }

                        // Reset the new group creation with the current segment's relevant information.
                        currentGroupedLinearSegment = new GroupedLinearSegment([])
                        {
                            QuantizedValue = segmentGroupPair.Key,
                            UpperBoundGradient = segmentGroupPair.Value[i].UpperBoundGradient,
                            LowerBoundGradient = segmentGroupPair.Value[i].LowerBoundGradient
                        };
                        currentGroupedLinearSegment.Timestamps.Add(segmentGroupPair.Value[i].StartTimestamp);
                    }
                }

                // Check if the group still under creation can be added to the list of groups.
                if (currentGroupedLinearSegment.Timestamps.Count > 1)
                    groupedLinearSegmentList.Add(currentGroupedLinearSegment);
                else
                {
                    if (!stillUngroupedSegmentGroups.ContainsKey(segmentGroupPair.Key))
                        stillUngroupedSegmentGroups[segmentGroupPair.Key] = [];

                    stillUngroupedSegmentGroups[segmentGroupPair.Key].Add(new Segment
                    {
                        UpperBoundGradient = currentGroupedLinearSegment.UpperBoundGradient,
                        LowerBoundGradient = currentGroupedLinearSegment.LowerBoundGradient,
                        StartTimestamp = currentGroupedLinearSegment.Timestamps[0],
                        QuantizedValue = segmentGroupPair.Value[^1].QuantizedValue,
                        EndTimestamp = segmentGroupPair.Value[^1].EndTimestamp,
                    });
                }
            }

            // Similarly, group the remaining segments as best as possible according to their gradients.
            var halfGroupedLinearSegmentList = new List<HalfGroupedLinearSegment>();
            var ungroupedLinearSegmentList = new List<UngroupedLinearSegment>();

            foreach (var ungroupedSegmentGroupPair in stillUngroupedSegmentGroups)
            {
                var currentHalfGroupedLinearSegment = new HalfGroupedLinearSegment([])
                {
                    UpperBoundGradient = double.PositiveInfinity,
                    LowerBoundGradient = double.NegativeInfinity
                };

                ungroupedSegmentGroupPair.Value.Sort(PlaUtils.CompareSegmentsByLowerBound);

                foreach (var currentSegment in ungroupedSegmentGroupPair.Value)
                {
                    if (currentSegment.LowerBoundGradient <= currentHalfGroupedLinearSegment.UpperBoundGradient &&
                        currentSegment.UpperBoundGradient >= currentHalfGroupedLinearSegment.LowerBoundGradient)
                    {
                        currentHalfGroupedLinearSegment.UpperBoundGradient = Math.Min(currentHalfGroupedLinearSegment.UpperBoundGradient,
                            currentSegment.UpperBoundGradient);
                        currentHalfGroupedLinearSegment.LowerBoundGradient = Math.Max(currentHalfGroupedLinearSegment.LowerBoundGradient,
                            currentSegment.LowerBoundGradient);
                        currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs.Add(new Tuple<double, long>(ungroupedSegmentGroupPair.Key,
                            currentSegment.StartTimestamp));
                    }
                    else
                    {
                        if (currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs.Count > 1)
                            halfGroupedLinearSegmentList.Add(currentHalfGroupedLinearSegment);
                        else
                        {
                            ungroupedLinearSegmentList.Add(new UngroupedLinearSegment
                            {
                                UpperBoundGradient = currentHalfGroupedLinearSegment.UpperBoundGradient,
                                LowerBoundGradient = currentHalfGroupedLinearSegment.LowerBoundGradient,
                                ValueTimestampPair = new Tuple<double, long>(currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs[0].Item1,
                                    currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs[0].Item2)
                            });
                        }

                        // Reset the new group creation with the current segment's relevant information.
                        currentHalfGroupedLinearSegment = new HalfGroupedLinearSegment([])
                        {
                            UpperBoundGradient = currentSegment.UpperBoundGradient,
                            LowerBoundGradient = currentSegment.LowerBoundGradient
                        };
                        currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs.Add(new Tuple<double, long>(ungroupedSegmentGroupPair.Key,
                            currentSegment.StartTimestamp));
                    }
                }

                // Check for any group still under creation.
                if (currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs.Count > 1)
                    halfGroupedLinearSegmentList.Add(currentHalfGroupedLinearSegment);
                else
                {
                    ungroupedLinearSegmentList.Add(new UngroupedLinearSegment
                    {
                        UpperBoundGradient = currentHalfGroupedLinearSegment.UpperBoundGradient,
                        LowerBoundGradient = currentHalfGroupedLinearSegment.LowerBoundGradient,
                        ValueTimestampPair = new Tuple<double, long>(currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs[0].Item1,
                            currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs[0].Item2)
                    });
                }
            }

            return new Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>>(groupedLinearSegmentList,
                halfGroupedLinearSegmentList,
                ungroupedLinearSegmentList);
        }

        /// <summary>
        /// Gets segments for reconstructing the original time series from the grouped, half-grouped, and ungrouped linear segments.
        /// </summary>
        /// <param name="linearSegments"></param>
        /// <returns></returns>
        private static List<Segment> GetSegmentsFromAllLinearSegments(Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> linearSegments)
        {
            var segments = new List<Segment>();

            // Get segments from grouped linear segments.
            foreach (var groupedLinearSegment in linearSegments.Item1)
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

            // Get segments from half-grouped linear segments.
            foreach (var halfGroupedLinearSegment in linearSegments.Item2)
            {
                foreach (var valueTimestampPair in halfGroupedLinearSegment.QuantizedValueTimestampPairs)
                {
                    segments.Add(new Segment
                    {
                        QuantizedValue = valueTimestampPair.Item1,
                        UpperBoundGradient = halfGroupedLinearSegment.Gradient,
                        StartTimestamp = valueTimestampPair.Item2
                    });
                }
            }

            // Get segments from ungrouped linear segments.
            foreach (var ungroupedLinearSegment in linearSegments.Item3)
            {
                segments.Add(new Segment
                {
                    QuantizedValue = ungroupedLinearSegment.ValueTimestampPair.Item1,
                    UpperBoundGradient = ungroupedLinearSegment.Gradient,
                    StartTimestamp = ungroupedLinearSegment.ValueTimestampPair.Item2
                });
            }

            return segments;
        }
    }
}
