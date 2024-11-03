using Buffered_Sim_Piece_Mix_Piece.Models;
using Buffered_Sim_Piece_Mix_Piece.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buffered_Sim_Piece_Mix_Piece.Algorithms
{
    internal static class MixPiece
    {
        /// <summary>
        /// Performs lossy compression using the Mix-Piece algorithm.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static List<QuantizedLinearSegment> Compress(List<Point> timeSeries, double epsilon)
        {

        }

        private static Dictionary<double, List<Segment>> GetFloorAndCeilSegmentGroupsFromTimeSeries(List<Point> timeSeries, double epsilon)
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
                            Timestamp = currentPoint.Timestamp
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
                            Timestamp = currentPoint.Timestamp
                        });
                    }

                    // Reset before continuing.
                    currentPoint = nextPoint;

                    currentFloorQuantizedValue = PlaUtils.GetFloorQuantizedValue(currentPoint.Value, epsilon);
                    currentCeilingQuantizedValue = PlaUtils.GetCeilingQuantizedValue(currentPoint.Value, epsilon);
                    currentFloorUpperBoundGradient = double.PositiveInfinity;
                    currentFloorLowerBoundGradient = double.NegativeInfinity;
                    currentCeilingUpperBoundGradient = double.PositiveInfinity;
                    currentCeilingLowerBoundGradient = double.NegativeInfinity;
                }
                else
                {
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
                    Timestamp = currentPoint.Timestamp
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
                    Timestamp = currentPoint.Timestamp
                });
            }

            return segmentGroups;
        }

        private static Tuple<List<QuantizedLinearSegment>, List<UnquantizedLinearSegment>> GetLinearSegmentGroupsFromSegmentGroups(Dictionary<double, List<Segment>> segmentGroups)
        {
            var quantizedLinearSegmentList = new List<QuantizedLinearSegment>();
            var unquantizedLinearSegmentList = new List<UnquantizedLinearSegment>();


        }
    }
}
