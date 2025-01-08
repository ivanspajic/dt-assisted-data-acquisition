using BusinessLogic.Algorithms.Utilities;
using Models;
using Models.LinearSegments;

namespace BusinessLogic.Algorithms
{
    /// <summary>
    /// Implements Custom-Piece based on Mix-Piece with phase 1 (segment construction) changed.
    /// </summary>
    public static class CustomPiece
    {
        /// <summary>
        /// Performs lossy compression using the Custom-Piece algorithm based on Mix-Piece.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilonPercentage"></param>
        /// <param name="compressForHighestAccuracy"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> Compress(List<Point> timeSeries, double epsilonPercentage)
        {
            if (timeSeries == null || timeSeries.Count < 2 || epsilonPercentage <= 0)
                throw new ArgumentException("The time series must contain at least 2 data points, and epsilon must be a percentage greater than 0.");

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);
            var segmentPathTree = GetSegmentPathTreeForTimeSeries(timeSeries, epsilon);
            var separateSegmentPaths = GetSeparateSegmentPathsFromTree(segmentPathTree.ToList());

            return GetLinearSegmentGroupsFromMostCompressibleSegmentPath(timeSeries, separateSegmentPaths);
        }

        /// <summary>
        /// Decompresses the compressed time series and returns reconstructed data points, each fitting within +/- epsilon of the original value.
        /// </summary>
        /// <param name="compressedTimeSeries"></param>
        /// <param name="lastPointTimestamp"></param>
        /// <returns></returns>
        public static List<Point> Decompress(Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedTimeSeries, 
            long lastPointTimestamp)
        {
            var segments = GetSegmentsFromAllLinearSegments(compressedTimeSeries);
            var reconstructedTimeSeries = PlaUtils.GetReconstructedTimeSeriesFromSegments(segments, lastPointTimestamp);

            return reconstructedTimeSeries;
        }

        /// <summary>
        /// Creates a tree of all possible segment combinations from the input time series.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        private static HashSet<SegmentPath> GetSegmentPathTreeForTimeSeries(List<Point> timeSeries, double epsilon)
        {
            var segmentPathEqualityComparer = new SegmentPathEqualityComparer();
            var segmentPathTree = new HashSet<SegmentPath>(segmentPathEqualityComparer);

            var currentStartPoint = timeSeries[0];

            // In cases of all points having the same value, epsilon will be 0, so assign default values.
            var currentFloorQuantizedValue = currentStartPoint.Value;
            var currentCeilingQuantizedValue = currentStartPoint.Value;

            if (epsilon > 0)
            {
                currentFloorQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentStartPoint.Value, epsilon);
                currentCeilingQuantizedValue = PlaUtils.GetCeilingQuantizedValue(currentStartPoint.Value, epsilon);
            }

            var currentFloorUpperBoundGradient = double.PositiveInfinity;
            var currentFloorLowerBoundGradient = double.NegativeInfinity;
            var currentCeilingUpperBoundGradient = double.PositiveInfinity;
            var currentCeilingLowerBoundGradient = double.NegativeInfinity;

            // Used for checking if segments can be extended to include more data points in subsequent iterations.
            var floorSegmentCreationFinalized = false;
            var ceilingSegmentCreationFinalized = false;

            // Used for keeping track of segments already added to the possible segment paths.
            var floorSegmentAdded = false;
            var ceilingSegmentAdded = false;

            for (var i = 0; i < timeSeries.Count - 1; i++)
            {
                var nextPoint = timeSeries[i + 1];

                if (!floorSegmentCreationFinalized)
                {
                    // Use the point-slope form to check whether the next point's value is outside of the current floor-based upper and lower bounds.
                    if (nextPoint.Value > currentFloorUpperBoundGradient * (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp) + currentFloorQuantizedValue + epsilon ||
                        nextPoint.Value < currentFloorLowerBoundGradient * (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp) + currentFloorQuantizedValue - epsilon)
                        // If the next point is out of bounds, mark the floor-based segment creation as finalized.
                        floorSegmentCreationFinalized = true;
                    else
                    {
                        // Use the point-slope form to check if the next point is below the upper bound but more than epsilon away.
                        if (nextPoint.Value < currentFloorUpperBoundGradient * (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp) + currentFloorQuantizedValue - epsilon)
                            // In case of being more than epsilon away, adjust the current upper bound to be within epsilon away from the next point.
                            currentFloorUpperBoundGradient = (nextPoint.Value - currentFloorQuantizedValue + epsilon) / (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp);

                        // Use the point-slope form to check if the next point is above the lower bound but mor than epsilon away.
                        if (nextPoint.Value > currentFloorLowerBoundGradient * (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp) + currentFloorQuantizedValue + epsilon)
                            // In case of being more than epsilon away, adjust the current lower bound to be within epsilon away from the next point.
                            currentFloorLowerBoundGradient = (nextPoint.Value - currentFloorQuantizedValue - epsilon) / (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp);

                        floorSegmentAdded = false;
                    }
                }

                if (!ceilingSegmentCreationFinalized)
                {
                    // Use the point-slope form to check whether the next point's value is outside of the current ceiling-based upper and lower bounds.
                    if (nextPoint.Value > currentCeilingUpperBoundGradient * (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp) + currentCeilingQuantizedValue + epsilon ||
                        nextPoint.Value < currentCeilingLowerBoundGradient * (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp) + currentCeilingQuantizedValue - epsilon)
                        // If the next point is out of bounds, mark the ceiling-based segment creation as finalized.
                        ceilingSegmentCreationFinalized = true;
                    else
                    {
                        // Use the point-slope form to check if the next point is below the upper bound but more than epsilon away.
                        if (nextPoint.Value < currentCeilingUpperBoundGradient * (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp) + currentCeilingQuantizedValue - epsilon)
                            // In case of being more than epsilon away, adjust the current upper bound to be within epsilon away from the next point.
                            currentCeilingUpperBoundGradient = (nextPoint.Value - currentCeilingQuantizedValue + epsilon) / (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp);

                        // Use the point-slope form to check if the next point is above the lower bound but mor than epsilon away.
                        if (nextPoint.Value > currentCeilingLowerBoundGradient * (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp) + currentCeilingQuantizedValue + epsilon)
                            // In case of being more than epsilon away, adjust the current lower bound to be within epsilon away from the next point.
                            currentCeilingLowerBoundGradient = (nextPoint.Value - currentCeilingQuantizedValue - epsilon) / (nextPoint.SimpleTimestamp - currentStartPoint.SimpleTimestamp);

                        ceilingSegmentAdded = false;
                    }
                }

                if (!floorSegmentAdded)
                {
                    // Find the right point for the start of the remainder of the time series. In case of the segment having been finalized, the next point is out
                    // of bounds, and thus the next segment must start creation from the current index. Otherwise, the next point is within bounds, and the next
                    // segment can begin creation at i + 1.
                    var continuedTimeSeriesIndex = i;
                    if (!floorSegmentCreationFinalized)
                        continuedTimeSeriesIndex = i + 1;

                    var currentFloorSegment = new Segment
                    {
                        LowerBoundGradient = currentFloorLowerBoundGradient,
                        UpperBoundGradient = currentFloorUpperBoundGradient,
                        StartTimestamp = currentStartPoint.SimpleTimestamp,
                        EndTimestamp = timeSeries[continuedTimeSeriesIndex].SimpleTimestamp,
                        QuantizedValue = currentFloorQuantizedValue,
                        Type = "Floor"
                    };

                    var possibleFloorSegmentPath = new SegmentPath
                    {
                        Segment = currentFloorSegment
                    };

                    // Check if the time series has been processed fully.
                    if (i < timeSeries.Count - 1)
                    {
                        // In case of remaining time series points, call this method with the remainder and add the output to the possible paths from the current
                        // segment.
                        var remainingTimeSeries = timeSeries.Take(new Range(continuedTimeSeriesIndex, timeSeries.Count)).ToList();
                        var remainingTimeSeriesPossibleSegmentPaths = GetSegmentPathTreeForTimeSeries(remainingTimeSeries, epsilon);

                        possibleFloorSegmentPath.PossiblePaths = remainingTimeSeriesPossibleSegmentPaths;
                    }

                    segmentPathTree.Add(possibleFloorSegmentPath);

                    floorSegmentAdded = true;
                }

                if (!ceilingSegmentAdded)
                {
                    // Find the right point for the start of the remainder of the time series. In case of the segment having been finalized, the next point is out
                    // of bounds, and thus the next segment must start creation from the current index. Otherwise, the next point is within bounds, and the next
                    // segment can begin creation at i + 1.
                    var continuedTimeSeriesIndex = i;
                    if (!ceilingSegmentCreationFinalized)
                        continuedTimeSeriesIndex = i + 1;

                    var currentCeilingSegment = new Segment
                    {
                        LowerBoundGradient = currentCeilingLowerBoundGradient,
                        UpperBoundGradient = currentCeilingUpperBoundGradient,
                        StartTimestamp = currentStartPoint.SimpleTimestamp,
                        EndTimestamp = timeSeries[continuedTimeSeriesIndex].SimpleTimestamp,
                        QuantizedValue = currentCeilingQuantizedValue,
                        Type = "Ceiling"
                    };

                    var possibleCeilingSegmentPath = new SegmentPath
                    {
                        Segment = currentCeilingSegment
                    };

                    // Check if the time series has been processed fully.
                    if (i < timeSeries.Count - 1)
                    {
                        // In case of remaining time series points, call this method with the remainder and add the output to the possible paths from the current
                        // segment.
                        var remainingTimeSeries = timeSeries.Take(new Range(continuedTimeSeriesIndex, timeSeries.Count)).ToList();
                        var remainingTimeSeriesPossibleSegmentPaths = GetSegmentPathTreeForTimeSeries(remainingTimeSeries, epsilon);

                        possibleCeilingSegmentPath.PossiblePaths = remainingTimeSeriesPossibleSegmentPaths;
                    }

                    segmentPathTree.Add(possibleCeilingSegmentPath);

                    ceilingSegmentAdded = true;
                }

                // Check if both floor-based and ceiling-based segment creation is finalized, in which case all possible combinations for this time series have
                // been found.
                if (floorSegmentCreationFinalized && ceilingSegmentCreationFinalized)
                    return segmentPathTree;
            }

            // The end of the time series is reached, so return all previously found combinations.
            return segmentPathTree;
        }

        /// <summary>
        /// Returns the recorded separate paths from the root node to every leaf node in the segment path tree.
        /// </summary>
        /// <param name="segmentPaths"></param>
        /// <returns></returns>
        private static List<List<Segment>> GetSeparateSegmentPathsFromTree(List<SegmentPath> segmentPaths)
        {
            var collectionOfPaths = new List<List<Segment>>();

            foreach (var segmentPath in segmentPaths)
            {
                if (segmentPath.PossiblePaths.Count > 0)
                {
                    var collectionOfInnerPaths = GetSeparateSegmentPathsFromTree(segmentPath.PossiblePaths.ToList());

                    foreach (var innerPath in collectionOfInnerPaths)
                    {
                        var localPath = new List<Segment>()
                        {
                            segmentPath.Segment
                        };

                        localPath.AddRange(innerPath);
                        collectionOfPaths.Add(localPath);
                    }
                }
                else
                {
                    var localPath = new List<Segment>()
                    {
                        segmentPath.Segment
                    };

                    collectionOfPaths.Add(localPath);
                }
            }

            return collectionOfPaths;
        }

        /// <summary>
        /// Groups the segments by their quantized value for phase 2 processing.
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        private static Dictionary<double, List<Segment>> GetGroupedSegmentsByQuantizedValue(List<Segment> segments)
        {
            var segmentGroups = new Dictionary<double, List<Segment>>();

            foreach (var segment in segments)
            {
                if (!segmentGroups.ContainsKey(segment.QuantizedValue))
                    segmentGroups[segment.QuantizedValue] = [];

                segmentGroups[segment.QuantizedValue].Add(segment);
            }

            return segmentGroups;
        }

        /// <summary>
        /// Performs phase 2 on all possible segment paths and returns the most compressible.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="separateSegmentPaths"></param>
        /// <returns></returns>
        private static Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> GetLinearSegmentGroupsFromMostCompressibleSegmentPath(List<Point> timeSeries, List<List<Segment>> separateSegmentPaths)
        {
            Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> smallestLinearSegmentGroups = null!;

            foreach (var segmentPath in separateSegmentPaths)
            {
                var segmentGroups = GetGroupedSegmentsByQuantizedValue(segmentPath);
                var currentLinearSegmentGroups = GetLinearSegmentGroupsFromSegmentGroups(segmentGroups);

                if (smallestLinearSegmentGroups == null)
                    smallestLinearSegmentGroups = currentLinearSegmentGroups;
                else
                {
                    var currentIterationCompressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, currentLinearSegmentGroups);
                    var currentBiggestCompressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, smallestLinearSegmentGroups);

                    if (currentIterationCompressionRatio > currentBiggestCompressionRatio)
                        smallestLinearSegmentGroups = currentLinearSegmentGroups;
                }
            }

            return smallestLinearSegmentGroups;
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

        /// <summary>
        /// Used for equality comparisons during segment path tree construction.
        /// </summary>
        private class SegmentPathEqualityComparer : EqualityComparer<SegmentPath>
        {
            public override bool Equals(SegmentPath? x, SegmentPath? y)
            {
                return x.Segment.StartTimestamp == y.Segment.StartTimestamp &&
                    x.Segment.EndTimestamp == y.Segment.EndTimestamp &&
                    x.Segment.QuantizedValue == y.Segment.QuantizedValue;
            }

            public override int GetHashCode(SegmentPath obj)
            {
                return (obj.Segment.StartTimestamp.ToString() + obj.Segment.EndTimestamp.ToString() + obj.Segment.QuantizedValue).GetHashCode();
            }
        }
    }
}
