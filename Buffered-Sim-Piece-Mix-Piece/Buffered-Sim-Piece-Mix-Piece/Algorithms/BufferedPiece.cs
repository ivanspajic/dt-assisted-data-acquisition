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
        public static Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> Compress(List<Point> timeSeries, double epsilonPercentage)
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
            var timeSeriesSegmentCombinations = GetSegmentPossibilitiesForTimeSeries(timeSeries, epsilon).ToList();

            // TODO: remember to update all of the algorithms to use the new segment model instead of Dictionaries with quantization values as keys.
            // Recursively go through the collection from the start and call the inner method again with the respective ending timestamps.
            // Upon a new segment, add the end timestamp to a list that is added to recursively.
            // After all the segments were looped through and all the lists of all possible connections have been constructed, find the shortest one!
            // Use the timestamps from the shortest list to filter out the segments to pass onto the next stage.

            return new();            
        }

        private static HashSet<Segment> GetSegmentPossibilitiesForTimeSeries(List<Point> timeSeries, double epsilon)
        {
            var segmentPossibilitySetComparer = new FirstSecondItemSegmentComparer();
            var segmentPossibilities = new HashSet<Segment>(segmentPossibilitySetComparer);

            var currentStartPoint = timeSeries[0];

            var currentQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentStartPoint.Value, epsilon);
            var currentUpperBoundGradient = double.PositiveInfinity;
            var currentLowerBoundGradient = double.NegativeInfinity;

            var segmentStartTimestamp = timeSeries[0].Timestamp;
            var segmentEndTimestamp = timeSeries[1].Timestamp;

            for (var i = 0; i < timeSeries.Count - 1; i++)
            {
                var nextPoint = timeSeries[i + 1];
                var segmentCreationFinalized = false;

                // Use the point-slope form to check whether the next point's value is outside of the current upper and lower bounds.
                if (nextPoint.Value > currentUpperBoundGradient * (nextPoint.Timestamp - currentStartPoint.Timestamp) + currentQuantizedValue + epsilon ||
                    nextPoint.Value < currentLowerBoundGradient * (nextPoint.Timestamp - currentStartPoint.Timestamp) + currentQuantizedValue - epsilon)
                {
                    // If the next point is out of bounds, mark the segment creation as finalized.
                    segmentCreationFinalized = true;

                    // The next point cannot be included, so the end timestamp is the current point's timestamp.
                    segmentEndTimestamp = timeSeries[i].Timestamp;
                }
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

                    // The next point is within bounds, so the end timestamp for the segment is the next point's timestamp.
                    segmentEndTimestamp = nextPoint.Timestamp;
                }

                var currentSegment = new Segment
                {
                    LowerBoundGradient = currentLowerBoundGradient,
                    UpperBoundGradient = currentUpperBoundGradient,
                    Timestamp = currentStartPoint.Timestamp,
                    StartTimestamp = segmentStartTimestamp,
                    EndTimestamp = segmentEndTimestamp
                };

                segmentPossibilities.Add(currentSegment);

                if (i < timeSeries.Count - 2)
                {
                    var remainingTimeSeries = timeSeries.Take(new Range(i + 1, timeSeries.Count)).ToList();
                    var remainingTimeSeriesSegmentPossibilities = GetSegmentPossibilitiesForTimeSeries(remainingTimeSeries, epsilon);

                    segmentPossibilities = segmentPossibilities.Union(remainingTimeSeriesSegmentPossibilities).ToHashSet(segmentPossibilitySetComparer);
                }

                // Check if segment creation is finalized, in which case all possible combinations for this time series have been found.
                if (segmentCreationFinalized)
                    return segmentPossibilities;
            }

            // The end of the time series is reached, so return all previously found combinations.
            return segmentPossibilities;
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
                        currentGroupedLinearSegment.Timestamps.Add(currentSegment.Timestamp);
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
                        currentGroupedLinearSegment.Timestamps.Add(currentSegment.Timestamp);
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
                        Timestamp = currentGroupedLinearSegment.Timestamps[0]
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
                            currentSegment.Timestamp));
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
                            currentSegment.Timestamp));
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

        private class FirstSecondItemSegmentComparer : EqualityComparer<Segment>
        {
            public override bool Equals(Segment? x, Segment? y)
            {
                return x.StartTimestamp == y.StartTimestamp && x.EndTimestamp == y.EndTimestamp;
            }

            public override int GetHashCode([DisallowNull] Segment obj)
            {
                return (obj.StartTimestamp.ToString() + obj.EndTimestamp.ToString()).GetHashCode();
            }
        }
    }
}
