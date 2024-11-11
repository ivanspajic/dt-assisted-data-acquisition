﻿using Sim_Mix_Custom_Piece_Tests.Utilities.CsvFileUtilities;
using SimMixCustomPiece.Algorithms;
using SimMixCustomPiece.Models;
using SimMixCustomPiece.Models.LinearSegments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.Systems
{
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

                var matchingDailyValues = GetSevenPointValuesFromLastWeekWithMatchingTimes(dataSet, simpleTimestamp, timestamp);
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
            var lastPoint = CsvFileUtils.ReadTimeSeriesFromCsvWithStartingTimestamp(dataSet,
                simpleTimestamp - 1,
                1)[0];

            return lastPoint.DateTime;
        }

        /// <summary>
        /// Returns 7 point values from the last week such that 1 with the same timestamp (time of day) is returned from each day.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private static List<double> GetSevenPointValuesFromLastWeekWithMatchingTimes(string dataSet, int simpleTimestamp, DateTime timestamp)
        {
            var matchingDailyValues = new List<double>();

            var requiredPreviousTimeSeriesPortion = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, simpleTimestamp);

            var i = RequiredNumberOfPreviousPointsForPrediction - 1;
            while (i >= 0 && matchingDailyValues.Count < 7)
            {
                if (requiredPreviousTimeSeriesPortion[i].DateTime.TimeOfDay == timestamp.TimeOfDay)
                    matchingDailyValues.Add(requiredPreviousTimeSeriesPortion[i].Value);

                i--;
            }

            return matchingDailyValues;
        }
    }
}
