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
        public static List<Point> GetPredictedTimeSeriesPortion(string dataSet, int simpleTimestamp, TimeSpan samplingInterval, int timeSeriesPortionSize)
        {
            var predictedTimeSeriesPortion = new List<Point>();

            var lastPointTimestamp = GetLastPointTimestamp(dataSet, simpleTimestamp);

            for (var i = 0; i < timeSeriesPortionSize; i++)
            {
                var timestamp = lastPointTimestamp.Add((i + 1) * samplingInterval);

                var matchingDailyValues = GetPreviousPointValuesWithMatchingTimes(dataSet, simpleTimestamp, timestamp);
                var averageValue = matchingDailyValues.Sum() / matchingDailyValues.Count;

                predictedTimeSeriesPortion.Add(new Point
                {
                    SimpleTimestamp = simpleTimestamp + i,
                    DateTime = timestamp,
                    Value = averageValue
                });
            }

            return predictedTimeSeriesPortion;
        }

        /// <summary>
        /// Returns the DateTime timestamp of the point before the start of the predicted time series.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        private static DateTime GetLastPointTimestamp(string dataSet, int simpleTimestamp)
        {
            var lastPoint = CsvFileUtils.ReadTimeSeriesFromCsvWithStartingTimestamp(dataSet, simpleTimestamp - 1, 1)[0];

            return lastPoint.DateTime;
        }

        /// <summary>
        /// Returns previous point values from the last week such that one with the same timestamp (time of day) is returned from each day.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private static List<double> GetPreviousPointValuesWithMatchingTimes(string dataSet, int simpleTimestamp, DateTime timestamp)
        {
            var numberOfValuesToMatchFor = 2;
            var matchingDailyValues = new List<double>();

            var requiredPreviousTimeSeriesPortion = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, simpleTimestamp);

            var i = RequiredNumberOfPreviousPointsForPrediction - 1;
            while (i >= 0 && matchingDailyValues.Count < numberOfValuesToMatchFor)
            {
                if (requiredPreviousTimeSeriesPortion[i].DateTime.TimeOfDay == timestamp.TimeOfDay)
                    matchingDailyValues.Add(requiredPreviousTimeSeriesPortion[i].Value);

                i--;
            }

            return matchingDailyValues;
        }
    }
}
