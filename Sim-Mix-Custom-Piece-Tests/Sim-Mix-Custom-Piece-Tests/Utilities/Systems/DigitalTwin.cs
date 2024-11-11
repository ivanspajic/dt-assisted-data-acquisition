using SimMixCustomPiece.Algorithms;
using SimMixCustomPiece.Models;
using SimMixCustomPiece.Models.LinearSegments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.Systems
{
    internal static class DigitalTwin
    {
        /// <summary>
        /// Returns the size of the required time series portion required for a given desired size of the predicted time series portion.
        /// </summary>
        /// <param name="bucketSize"></param>
        /// <returns></returns>
        public static int GetRequiredTimeSeriesPortionForBucketSize(int bucketSize)
        {
            return 2 * bucketSize;
        }

        /// <summary>
        /// Returns a predicted compressed time series based on a previous time series portion.
        /// </summary>
        /// <param name="previousTimeSeriesPortion"></param>
        /// <param name="epsilonPercentage"></param>
        /// <returns></returns>
        public static List<Point> GetPredictedTimeSeriesPortionFromPreviousPortion(List<Point> previousTimeSeriesPortion)
        {
            var predictedTimeSeriesPortion = new List<Point>();

            for (var i = 0; i < previousTimeSeriesPortion.Count / 2; i++)
            {
                var predictedPoint = new Point
                {
                    Timestamp = i + previousTimeSeriesPortion.Count,
                    Value = (previousTimeSeriesPortion[i].Value + previousTimeSeriesPortion[i + previousTimeSeriesPortion.Count / 2].Value) / 2
                };

                predictedTimeSeriesPortion.Add(predictedPoint);
            }

            return predictedTimeSeriesPortion;
        }
    }
}
