using Sim_Mix_Custom_Piece_Tests.Utilities.CsvFileUtilities;
using SimMixCustomPiece.Models;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.Systems
{
    /// <summary>
    /// Simulates a digital twin.
    /// </summary>
    internal static class DigitalTwin
    {
        /// <summary>
        /// Simulates the need for at least 10 days of sampling at 30min intervals for subsequent predictions.
        /// </summary>
        public const int RequiredNumberOfPreviousPointsForPrediction = 480;

        /// <summary>
        /// Returns a predicted compressed time series based on a previous time series portion.
        /// </summary>
        /// <param name="timeSeriesPortionSize"></param>
        /// <returns></returns>
        public static List<Point> GetPredictedTimeSeriesPortion(string dataSet, List<Point> lastTimeSeriesPortion, TimeSpan samplingInterval)
        {
            var predictedTimeSeriesPortion = new List<Point>();
            var lastPointFromPreviousPortion = lastTimeSeriesPortion[^1];

            for (var i = 0; i < lastTimeSeriesPortion.Count; i++)
            {
                var timestamp = lastPointFromPreviousPortion.DateTime.Add((i + 1) * samplingInterval);

                var matchingDailyValues = GetPreviousPointValuesWithMatchingTimes(dataSet, lastPointFromPreviousPortion.SimpleTimestamp, timestamp);
                var averageValue = matchingDailyValues.Sum() / matchingDailyValues.Count;

                predictedTimeSeriesPortion.Add(new Point
                {
                    SimpleTimestamp = lastPointFromPreviousPortion.SimpleTimestamp + i,
                    DateTime = timestamp,
                    Value = averageValue
                });
            }

            return predictedTimeSeriesPortion;
        }

        /// <summary>
        /// Returns previous point values from the last week such that one with the same timestamp (time of day) is returned from each day.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private static List<double> GetPreviousPointValuesWithMatchingTimes(string dataSet, long lastTimestamp, DateTime timestamp)
        {
            var numberOfValuesToMatchFor = 2;
            var matchingDailyValues = new List<double>();

            var timeSeriesFromTimestamp = CsvFileUtils.ReadCsvTimeSeriesBucket(dataSet, 0, (int)lastTimestamp);

            var i = timeSeriesFromTimestamp.Count - 1;
            while (i >= 0 && matchingDailyValues.Count < numberOfValuesToMatchFor)
            {
                if (timeSeriesFromTimestamp[i].DateTime.TimeOfDay == timestamp.TimeOfDay)
                    matchingDailyValues.Add(timeSeriesFromTimestamp[i].Value);

                i--;
            }

            return matchingDailyValues;
        }
    }
}
