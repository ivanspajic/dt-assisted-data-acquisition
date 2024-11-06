using Buffered_Sim_Piece_Mix_Piece.Models;
using Buffered_Sim_Piece_Mix_Piece.Models.LinearSegments;
using Buffered_Sim_Piece_Mix_Piece.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buffered_Sim_Piece_Mix_Piece.Algorithms
{
    /// <summary>
    /// Implements the Buffered-Piece algorithm.
    /// </summary>
    internal static class BufferedPiece
    {
        /// <summary>
        /// Performs lossy compression using the Buffered-Piece algorithm.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilonPercentage"></param>
        /// <returns></returns>
        public static Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> Compress(List<Point> timeSeries, 
            double epsilonPercentage)
        {
            if (timeSeries == null || timeSeries.Count < 2 || epsilonPercentage <= 0)
                throw new ArgumentException("The time series must contain at least 2 data points, and epsilon must be a percentage greater than 0.");

            var epsilon = epsilonPercentage / 100;

            var segmentGroups = GetFewestSegmentGroupsFromTimeSeries(timeSeries, epsilon);
            var linearSegmentGroups = GetLinearSegmentGroupsFromSegmentGroups(segmentGroups);

            return linearSegmentGroups;
        }

        /// <summary>
        /// Phase 1 of the algorithm.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilonPercentage"></param>
        /// <returns></returns>
        private static Dictionary<double, List<Segment>> GetFewestSegmentGroupsFromTimeSeries(List<Point> timeSeries, double epsilon)
        {
            var possibleSegmentPaths = GetPossibleSegmentPathsForTimeSeries(timeSeries, epsilon);

            return new();
        }

        private static HashSet<SegmentPath> GetPossibleSegmentPathsForTimeSeries(List<Point> timeSeries, double epsilon)
        {
            var segmentPathEqualityComparer = new SegmentPathEqualityComparer();
            var possibleSegmentPaths = new HashSet<SegmentPath>(segmentPathEqualityComparer);

            var currentStartPoint = timeSeries[0];

            var currentQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentStartPoint.Value, epsilon);
            var currentUpperBoundGradient = double.PositiveInfinity;
            var currentLowerBoundGradient = double.NegativeInfinity;

            for (var i = 0; i < timeSeries.Count - 1; i++)
            {
                var nextPoint = timeSeries[i + 1];
                var segmentCreationFinalized = false;

                // Use the point-slope form to check whether the next point's value is outside of the current upper and lower bounds.
                if (nextPoint.Value > currentUpperBoundGradient * (nextPoint.Timestamp - currentStartPoint.Timestamp) + currentQuantizedValue + epsilon ||
                    nextPoint.Value < currentLowerBoundGradient * (nextPoint.Timestamp - currentStartPoint.Timestamp) + currentQuantizedValue - epsilon)
                    // If the next point is out of bounds, mark the segment creation as finalized.
                    segmentCreationFinalized = true;
                else
                {
                    // Use the point-slope form to check if the next point is below the upper bound but more than epsilon away.
                    if (nextPoint.Value < currentUpperBoundGradient * (nextPoint.Timestamp - currentStartPoint.Timestamp) + currentQuantizedValue - epsilon)
                        // In case of being more than epsilon away, adjust the current upper bound to be within epsilon away from the next point.
                        currentUpperBoundGradient = (nextPoint.Value - currentStartPoint.Value + epsilon) / (nextPoint.Timestamp - currentStartPoint.Timestamp);

                    // Use the point-slope form to check if the next point is above the lower bound but mor than epsilon away.
                    if (nextPoint.Value > currentLowerBoundGradient * (nextPoint.Timestamp - currentStartPoint.Timestamp) + currentQuantizedValue - epsilon)
                        // In case of being more than epsilon away, adjust the current lower bound to be within epsilon away from the next point.
                        currentLowerBoundGradient = (nextPoint.Value - currentStartPoint.Value - epsilon) / (nextPoint.Timestamp - currentStartPoint.Timestamp);
                }

                var continuedTimeSeriesIndex = i;

                if (!segmentCreationFinalized)
                    continuedTimeSeriesIndex = i + 1;

                var currentSegment = new Segment
                {
                    LowerBoundGradient = currentLowerBoundGradient,
                    UpperBoundGradient = currentUpperBoundGradient,
                    StartTimestamp = currentStartPoint.Timestamp,
                    EndTimestamp = timeSeries[continuedTimeSeriesIndex].Timestamp
                };

                var possibleSegmentPath = new SegmentPath
                {
                    Segment = currentSegment
                };

                if (i < timeSeries.Count - 1)
                {
                    var remainingTimeSeries = timeSeries.Take(new Range(continuedTimeSeriesIndex, timeSeries.Count)).ToList();
                    var remainingTimeSeriesPossibleSegmentPaths = GetPossibleSegmentPathsForTimeSeries(remainingTimeSeries, epsilon);

                    possibleSegmentPath.PossiblePaths = remainingTimeSeriesPossibleSegmentPaths;
                }

                possibleSegmentPaths.Add(possibleSegmentPath);

                // Check if segment creation is finalized, in which case all possible combinations for this time series have been found.
                if (segmentCreationFinalized)
                    return possibleSegmentPaths;
            }

            // The end of the time series is reached, so return all previously found combinations.
            return possibleSegmentPaths;
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
                    QuantizedOriginValue = segmentGroupPair.Key,
                    UpperBoundGradient = double.PositiveInfinity,
                    LowerBoundGradient = double.NegativeInfinity
                };

                segmentGroupPair.Value.Sort(PlaUtils.CompareSegmentsByLowerBound);

                foreach (var currentSegment in segmentGroupPair.Value)
                {
                    // Check if there is any overlap between the current segment and the current bounds of the group.
                    if (currentSegment.LowerBoundGradient <= currentGroupedLinearSegment.UpperBoundGradient &&
                        currentSegment.UpperBoundGradient >= currentGroupedLinearSegment.LowerBoundGradient)
                    {
                        // In case of an overlap, tighten the upper and lower bounds further.
                        currentGroupedLinearSegment.UpperBoundGradient = Math.Min(currentGroupedLinearSegment.UpperBoundGradient, currentSegment.UpperBoundGradient);
                        currentGroupedLinearSegment.LowerBoundGradient = Math.Max(currentGroupedLinearSegment.LowerBoundGradient, currentSegment.LowerBoundGradient);
                        currentGroupedLinearSegment.Timestamps.Add(currentSegment.StartTimestamp);
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

                            stillUngroupedSegmentGroups[segmentGroupPair.Key].Add(currentSegment);
                        }

                        // Reset the new group creation with the current segment's information.
                        currentGroupedLinearSegment = new GroupedLinearSegment([])
                        {
                            QuantizedOriginValue = segmentGroupPair.Key,
                            UpperBoundGradient = currentSegment.UpperBoundGradient,
                            LowerBoundGradient = currentSegment.LowerBoundGradient
                        };
                        currentGroupedLinearSegment.Timestamps.Add(currentSegment.StartTimestamp);
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
                        StartTimestamp = currentGroupedLinearSegment.Timestamps[0]
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
                                ValueTimestampPair = new Tuple<double, long>(currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs[0].Item1,
                                    currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs[0].Item2)
                            });
                        }

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
                        ValueTimestampPair = new Tuple<double, long>(currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs[0].Item1,
                            currentHalfGroupedLinearSegment.QuantizedValueTimestampPairs[0].Item2)
                    });
                }
            }

            return new Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>>(groupedLinearSegmentList,
                halfGroupedLinearSegmentList,
                ungroupedLinearSegmentList);
        }

        private class SegmentPathEqualityComparer : EqualityComparer<SegmentPath>
        {
            public override bool Equals(SegmentPath? x, SegmentPath? y)
            {
                return x.Segment.StartTimestamp == y.Segment.StartTimestamp && x.Segment.EndTimestamp == y.Segment.EndTimestamp;
            }

            public override int GetHashCode([DisallowNull] SegmentPath obj)
            {
                return (obj.Segment.StartTimestamp.ToString() + obj.Segment.EndTimestamp.ToString()).GetHashCode();
            }
        }
    }
}
