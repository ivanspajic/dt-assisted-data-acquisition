using Buffered_Sim_Piece_Mix_Piece.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buffered_Sim_Piece_Mix_Piece
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
        /// <param name="epsilonPercentage">The allowed data point value variance in terms of the percentage of
        /// the difference between the maximum and minimum values of the whole time series.</param>
        /// <returns>A list of </returns>
        public static List<LinearSegment> CompressWithSimPiece(List<Point> timeSeries, double epsilonPercentage)
        {
            // check sizes, defend
            // greedy pla needs to go through the time series
            // once intervals are created, they need to be sorted and compared to match the overlaps
            // once interval groups are matched, linear segments must be created and returned
        }

        /// <summary>
        /// Performs lossy compression using the Mix-Piece algorithm.
        /// </summary>
        /// <param name="timeSeries">The original time series.</param>
        /// <param name="epsilonPercentage">The allowed data point value variance in terms of the percentage of
        /// the difference between the maximum and minimum values of the whole time series.</param>
        /// <returns></returns>
        public static List<LinearSegment> CompressWithMixPiece(List<Point> timeSeries, double epsilonPercentage)
        {
            // the first phase should be sim-piece
            // additionally, non-matched segments should be grouped according to other characteristics
            // once everything is matched, linear segments must be created and returned
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSeries">The original time series.</param>
        /// <param name="epsilonPercentage">The allowed data point value variance in terms of the percentage of
        /// the difference between the maximum and minimum values of the whole time series.</param>
        /// <returns></returns>
        public static List<LinearSegment> CompressWithBufferedSimPiece(List<Point> timeSeries, double epsilonPercentage)
        {
            // the first phase needs to play around with different segment distributions
            // all other phases should be like sim-piece
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="linearSegments"></param>
        /// <returns></returns>
        public static List<Point> Decompress(List<LinearSegment> linearSegments)
        {
            // should return an approximation of the original time series
        }

        private static Dictionary<double, List<Interval>> GetIntervalGroupsFromTimeSeries(List<Point> timeSeries,
            double epsilon)
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
                if (nextPoint.Value > currentUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) + 
                    currentQuantizedValue +
                    epsilon ||
                    nextPoint.Value < currentLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) +
                    currentQuantizedValue -
                    epsilon)
                {
                    // In case of the value being outside of the bounds, finalize the creation of the current interval and
                    // add it to the group for this origin value.
                    if (intervalGroups[currentQuantizedValue] is null)
                        intervalGroups[currentQuantizedValue] = new List<Interval>();

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
                    if (nextPoint.Value < currentUpperBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) +
                        currentQuantizedValue -
                        epsilon)
                    {
                        // In case of being more than epsilon away, adjust the current upper bound to be within epsilon
                        // away from the next point.
                        currentUpperBoundGradient = (nextPoint.Value - currentPoint.Value + epsilon) / 
                            (nextPoint.Timestamp - currentPoint.Timestamp);
                    }

                    // Use the point-slope form to check if the next point is above the lower bound but mor than epsilon
                    // away.
                    if (nextPoint.Value > currentLowerBoundGradient * (nextPoint.Timestamp - currentPoint.Timestamp) +
                        currentQuantizedValue -
                        epsilon)
                    {
                        // In case of being more than epsilon away, adjust the current lower bound to be within epsilon
                        // away from the next point.
                        currentLowerBoundGradient = (nextPoint.Value - currentPoint.Value - epsilon) /
                            (nextPoint.Timestamp - currentPoint.Timestamp);
                    }
                }
            }

            // Add the interval still under creation at the end of the time series.
            intervalGroups[currentQuantizedValue].Add(new Interval
            {
                LowerBoundGradient = currentLowerBoundGradient,
                UpperBoundGradient = currentUpperBoundGradient,
                Timestamp = currentPoint.Timestamp
            });

            return intervalGroups;
        }

        private static List<LinearSegment> GetLinearSegments(Dictionary<double, List<Interval>> intervalGroups)
        {

        }

        private static double GetQuantizedValue(double pointValue, double epsilon)
        {
            return Math.Floor(pointValue / epsilon) * epsilon;
        }
    }
}
