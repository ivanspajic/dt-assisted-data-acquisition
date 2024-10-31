using Buffered_Sim_Piece_Mix_Piece.Models;

namespace Buffered_Sim_Piece_Mix_Piece.Utilities
{
    /// <summary>
    /// Offers a few different algorithms for performing lossy compression with Piece-wise Linear Approximation.
    /// </summary>
    internal static class PiecewiseLinearApproximation
    {
        /// <summary>
        /// Performs lossy compression using the Sim-Piece algorithm.
        /// </summary>
        /// <param name="timeSeries">The original time series.</param>
        /// <param name="epsilonPercentage">The allowed data point value variance in terms of the percentage of the difference between the maximum and 
        /// minimum values of the whole time series.</param>
        /// <returns>A list of </returns>
        public static List<LinearSegment> CompressWithSimPiece(List<Point> timeSeries, double epsilonPercentage)
        {
            if (timeSeries == null || timeSeries.Count < 2 || epsilonPercentage <= 0)
                throw new ArgumentException("The time series must contain at least 2 data points, and epsilon must be a percentage greater than 0.");

            var epsilon = epsilonPercentage / 100;

            // Phase 1 of the algorithm.
            var intervalGroups = GetIntervalGroupsFromTimeSeries(timeSeries, epsilon);

            // Phase 2 of the algorithm.
            var linearSegments = GetLinearSegmentsFromIntervalGroups(intervalGroups);

            return linearSegments;
        }

        /// <summary>
        /// Performs lossy compression using the Mix-Piece algorithm.
        /// </summary>
        /// <param name="timeSeries">The original time series.</param>
        /// <param name="epsilonPercentage">The allowed data point value variance in terms of the percentage of the difference between the maximum and 
        /// minimum values of the whole time series.</param>
        /// <returns></returns>
        public static List<LinearSegment> CompressWithMixPiece(List<Point> timeSeries, double epsilonPercentage)
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSeries">The original time series.</param>
        /// <param name="epsilonPercentage">The allowed data point value variance in terms of the percentage of the difference between the maximum and 
        /// minimum values of the whole time series.</param>
        /// <returns></returns>
        //public static List<LinearSegment> CompressWithBufferedSimPiece(List<Point> timeSeries, double epsilonPercentage)
        //{
        //    // the first phase needs to play around with different segment distributions
        //    // all other phases should be like sim-piece
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="linearSegments"></param>
        /// <returns></returns>
        //public static List<Point> Decompress(List<LinearSegment> linearSegments)
        //{
        //    // should return an approximation of the original time series
        //}

        private static Dictionary<double, List<Interval>> GetIntervalGroupsFromTimeSeries(List<Point> timeSeries, double epsilon)
        {
            var intervalGroups = new Dictionary<double, List<Interval>>();

            var currentPoint = timeSeries[0];

            var currentQuantizedValue = GetQuantizedValue(currentPoint.Value, epsilon);
            var currentUpperBoundGradient = double.PositiveInfinity;
            var currentLowerBoundGradient = double.NegativeInfinity;

            for (var i = 0; i < timeSeries.Count - 1; i++)
            {
                var nextPoint = timeSeries[i + 1];

                // Use the point-slope form to check whether the next point's value is outside of the current upper and
                // lower bounds.
                if (nextPoint.Value > currentUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentQuantizedValue + epsilon ||
                    nextPoint.Value < currentLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentQuantizedValue - epsilon)
                {
                    // In case of the value being outside of the bounds, finalize the creation of the current interval and
                    // add it to the group for this origin value.
                    if (!intervalGroups.ContainsKey(currentQuantizedValue))
                        intervalGroups[currentQuantizedValue] = [];

                    intervalGroups[currentQuantizedValue].Add(new Interval
                    {
                        LowerBoundGradient = currentLowerBoundGradient,
                        UpperBoundGradient = currentUpperBoundGradient,
                        Timestamp = currentPoint.Timestamp
                    });

                    // Reset before continuing.
                    currentPoint = nextPoint;

                    currentQuantizedValue = GetQuantizedValue(currentPoint.Value, epsilon);
                    currentUpperBoundGradient = double.PositiveInfinity;
                    currentLowerBoundGradient = double.NegativeInfinity;
                }
                else
                {
                    // Use the point-slope form to check if the next point is below the upper bound but more than epsilon
                    // away.
                    if (nextPoint.Value < currentUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentQuantizedValue - epsilon)
                    {
                        // In case of being more than epsilon away, adjust the current upper bound to be within epsilon
                        // away from the next point.
                        currentUpperBoundGradient = (nextPoint.Value - currentPoint.Value + epsilon) /
                            (nextPoint.Timestamp - currentPoint.Timestamp);
                    }

                    // Use the point-slope form to check if the next point is above the lower bound but mor than epsilon
                    // away.
                    if (nextPoint.Value > currentLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + currentQuantizedValue - epsilon)
                    {
                        // In case of being more than epsilon away, adjust the current lower bound to be within epsilon
                        // away from the next point.
                        currentLowerBoundGradient = (nextPoint.Value - currentPoint.Value - epsilon) / (nextPoint.Timestamp - currentPoint.Timestamp);
                    }
                }
            }

            // Add the interval still under creation at the end of the time series.
            if (!intervalGroups.ContainsKey(currentQuantizedValue))
                intervalGroups[currentQuantizedValue] = [];

            intervalGroups[currentQuantizedValue].Add(new Interval
            {
                LowerBoundGradient = currentLowerBoundGradient,
                UpperBoundGradient = currentUpperBoundGradient,
                Timestamp = currentPoint.Timestamp
            });

            return intervalGroups;
        }

        private static Dictionary<double, List<Interval>> GetFloorAndCeilIntervalGroupsFromTimeSeries(List<Point> timeSeries, double epsilon)
        {

        }

        private static List<LinearSegment> GetLinearSegmentsFromIntervalGroups(Dictionary<double, List<Interval>> intervalGroups)
        {
            var linearSegmentList = new List<LinearSegment>();

            foreach (var intervalGroupPair in intervalGroups)
            {
                var currentLinearSegment = new LinearSegment([])
                {
                    QuantizedOriginValue = intervalGroupPair.Key,
                    UpperBoundGradient = double.PositiveInfinity,
                    LowerBoundGradient = double.NegativeInfinity
                };

                intervalGroupPair.Value.Sort(CompareIntervalsByLowerBound);

                foreach (var currentInterval in intervalGroupPair.Value)
                {
                    // Check if there is any overlap between the current interval and the current bounds of the
                    // group.
                    if (currentInterval.LowerBoundGradient <= currentLinearSegment.UpperBoundGradient && 
                        currentInterval.UpperBoundGradient >= currentLinearSegment.LowerBoundGradient)
                    {
                        // In case of an overlap, tighten the upper and lower bounds further.
                        currentLinearSegment.UpperBoundGradient = Math.Min(currentLinearSegment.UpperBoundGradient, currentInterval.UpperBoundGradient);
                        currentLinearSegment.LowerBoundGradient = Math.Max(currentLinearSegment.LowerBoundGradient, currentInterval.LowerBoundGradient);
                        currentLinearSegment.Timestamps.Add(currentInterval.Timestamp);
                    }
                    else
                    {
                        // In case of no overlaps, finalize the creation of the current segment.
                        linearSegmentList.Add(currentLinearSegment);

                        currentLinearSegment = new LinearSegment([])
                        {
                            QuantizedOriginValue = intervalGroupPair.Key,
                            UpperBoundGradient = double.PositiveInfinity,
                            LowerBoundGradient = double.NegativeInfinity
                        };
                    }
                }

                // Add the linear segment still under creation.
                linearSegmentList.Add(currentLinearSegment);
            }

            return linearSegmentList;
        }

        private static int CompareIntervalsByLowerBound(Interval x, Interval y)
        {
            if (x.LowerBoundGradient < y.LowerBoundGradient)
                return -1;
            else if (x.LowerBoundGradient > y.LowerBoundGradient)
                return 1;
            else
                return 0;
        }

        private static double GetQuantizedValue(double pointValue, double epsilon)
        {
            return Math.Floor(pointValue / epsilon) * epsilon;
        }
    }
}
