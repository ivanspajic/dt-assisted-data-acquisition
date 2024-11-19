using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using SimMixCustomPiece.Models;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.CsvFileUtilities
{
    /// <summary>
    /// Helps with reading from and writing to CSV files. Some methods are separated for performance reasons.
    /// </summary>
    internal static class CsvFileUtils
    {
        /// <summary>
        /// Reads a time series from a CSV file from a specified index and in specified bucket sizes.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="startIndex"></param>
        /// <param name="bucketSize"></param>
        /// <returns></returns>
        public static List<List<Point>> ReadWholeCsvTimeSeriesInBuckets(string filepath, long startIndex, int bucketSize)
        {
            var timeSeriesInBuckets = new List<List<Point>>();

            var csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            using (var streamReader = new StreamReader(filepath))
            using (var csvReader = new CsvReader(streamReader, csvHelperConfig))
            {
                var chunks = csvReader.GetRecords<Point>().Skip((int)startIndex).Chunk(bucketSize);
                timeSeriesInBuckets = chunks.Select(chunk => chunk.ToList()).ToList();
                // last bucket potentially incomplete
            }
            return timeSeriesInBuckets;
        }

        /// <summary>
        /// Reads the whole time series, beginning to end, from a CSV file in specified bucket sizes.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="bucketSize"></param>
        /// <returns></returns>
        public static List<List<Point>> ReadWholeCsvTimeSeriesInBuckets(string filepath, int bucketSize)
        {
            return ReadWholeCsvTimeSeriesInBuckets(filepath, 0, bucketSize);
        }

        /// <summary>
        /// Reads a whole time series from a CSV file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static List<Point> ReadCsvTimeSeries(string filepath, long startIndex)
        {
            var timeSeries = new List<Point>();

            var csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            using (var streamReader = new StreamReader(filepath))
            using (var csvReader = new CsvReader(streamReader, csvHelperConfig))
            {
                var i = 0;

                while (i < startIndex)
                {
                    csvReader.Read();

                    i++;
                }

                while (csvReader.Read())
                {
                    timeSeries.Add(csvReader.GetRecord<Point>());

                    i++;
                }
            }

            return timeSeries;
        }

        /// <summary>
        /// Reads a portion of a time series from a CSV file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="startIndex"></param>
        /// <param name="bucketSize"></param>
        /// <returns></returns>
        public static List<Point> ReadCsvTimeSeriesBucket(string filepath, long startIndex, int bucketSize)
        {
            var timeSeries = new List<Point>();

            var csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            using (var streamReader = new StreamReader(filepath))
            using (var csvReader = new CsvReader(streamReader, csvHelperConfig))
            {
                var i = 0;

                while (i < startIndex)
                {
                    csvReader.Read();

                    i++;
                }

                var j = 0;
                while (csvReader.Read() && j < bucketSize)
                {
                    timeSeries.Add(csvReader.GetRecord<Point>());

                    i++;
                    j++;
                }
            }

            if (timeSeries.Count < bucketSize)
                return [];

            return timeSeries;
        }

        /// <summary>
        /// Writes results to a CSV file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filepath"></param>
        /// <param name="testResults"></param>
        public static void WriteTestResultsToCsv<T>(string filepath, List<T> testResults)
        {
            using var streamWriter = new StreamWriter(filepath);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            csvWriter.WriteHeader<T>();
            csvWriter.NextRecord();

            foreach (var testResult in testResults)
            {
                csvWriter.WriteRecord(testResult);
                csvWriter.NextRecord();
            }
        }
    }
}
