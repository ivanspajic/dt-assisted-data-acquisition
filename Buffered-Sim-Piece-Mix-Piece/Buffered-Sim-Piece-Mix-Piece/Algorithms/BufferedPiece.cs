using Buffered_Sim_Piece_Mix_Piece.Models;
using Buffered_Sim_Piece_Mix_Piece.Models.LinearSegments;
using Buffered_Sim_Piece_Mix_Piece.Utilities;
using System;
using System.Collections.Generic;
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
            var segmentGroupList = new List<Dictionary<double, List<Segment>>>();

            for (var i = 0; i < timeSeries.Count; i++)
            {
                var currentPoint = timeSeries[i];

                var currentQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentPoint.Value, epsilon);
                var currentUpperBoundGradient = double.PositiveInfinity;
                var currentLowerBoundGradient = double.NegativeInfinity;

                var currentSegment = new Segment();

                for (var j = i; j < timeSeries.Count - 1; j++)
                {
                    var nextPoint = timeSeries[j + 1];

                    var currentSegmentGroup = new Dictionary<double, List<Segment>>();
                    var partialTimeSeries = new List<Point>();

                    // Use the point-slope form to check whether the next point's value is outside of the current upper and lower bounds.
                    if (nextPoint.Value > currentUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentQuantizedValue + epsilon ||
                        nextPoint.Value < currentLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentQuantizedValue - epsilon)
                    {
                        currentSegment.UpperBoundGradient = currentUpperBoundGradient;
                        currentSegment.LowerBoundGradient = currentLowerBoundGradient;
                        currentSegment.Timestamp = currentPoint.Timestamp;

                        partialTimeSeries = timeSeries.Take(new Range(j + 1, timeSeries.Count)).ToList();
                        currentSegmentGroup = GetFewestSegmentGroupsFromTimeSeries(partialTimeSeries, epsilon);

                        if (!currentSegmentGroup.ContainsKey(currentQuantizedValue))
                            currentSegmentGroup[currentQuantizedValue] = [];

                        currentSegmentGroup[currentQuantizedValue].Add(currentSegment);
                        segmentGroupList.Add(currentSegmentGroup);

                        break;
                    }
                   
                    // Use the point-slope form to check if the next point is below the upper bound but more than epsilon away.
                    if (nextPoint.Value < currentUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentQuantizedValue - epsilon)
                        // In case of being more than epsilon away, adjust the current upper bound to be within epsilon away from the next point.
                        currentUpperBoundGradient = (nextPoint.Value - currentPoint.Value + epsilon) / (nextPoint.Timestamp - currentPoint.Timestamp);

                    // Use the point-slope form to check if the next point is above the lower bound but mor than epsilon away.
                    if (nextPoint.Value > currentLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentQuantizedValue - epsilon)
                        // In case of being more than epsilon away, adjust the current lower bound to be within epsilon away from the next point.
                        currentLowerBoundGradient = (nextPoint.Value - currentPoint.Value - epsilon) / (nextPoint.Timestamp - currentPoint.Timestamp);

                    currentSegment.UpperBoundGradient = currentUpperBoundGradient;
                    currentSegment.LowerBoundGradient = currentLowerBoundGradient;
                    currentSegment.Timestamp = currentPoint.Timestamp;

                    if (j < timeSeries.Count - 1)
                    {
                        partialTimeSeries = timeSeries.Take(new Range(j + 1, timeSeries.Count)).ToList();
                        currentSegmentGroup = GetFewestSegmentGroupsFromTimeSeries(partialTimeSeries, epsilon);
                    }

                    if (!currentSegmentGroup.ContainsKey(currentQuantizedValue))
                        currentSegmentGroup[currentQuantizedValue] = [];

                    currentSegmentGroup[currentQuantizedValue].Add(currentSegment);
                    segmentGroupList.Add(currentSegmentGroup);
                }
            }

            var fewestNumberOfSegments = int.MaxValue;
            Dictionary<double, List<Segment>> segmentGroupWithFewestSegments = [];
            for (var i = 1; i < segmentGroupList.Count; i++)
            {
                var currentNumberOfSegments = 0;
                foreach (var segmentGroupKeyValuePair in segmentGroupList[i])
                {
                    currentNumberOfSegments += segmentGroupKeyValuePair.Value.Count;
                }

                if (currentNumberOfSegments < fewestNumberOfSegments)
                    segmentGroupWithFewestSegments = segmentGroupList[i];
            }

            return segmentGroupWithFewestSegments;
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
    }
}
