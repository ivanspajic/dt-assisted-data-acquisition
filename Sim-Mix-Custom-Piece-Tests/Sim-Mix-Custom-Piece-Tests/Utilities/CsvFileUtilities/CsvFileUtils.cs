using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using SimMixCustomPiece.Models;
using System.Reflection.Metadata.Ecma335;

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

            using var streamReader = new StreamReader(filepath);
            using var csvReader = new CsvReader(streamReader, csvHelperConfig);

            return csvReader.GetRecords<Point>()
                .Skip((int)startIndex)
                .Chunk(bucketSize)
                .Where(bucket => bucket.Length == bucketSize)
                .Select(chunk => chunk.ToList())
                .ToList();
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
        /// Reads a portion of a time series from a CSV file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="startIndex"></param>
        /// <param name="bucketSize"></param>
        /// <returns></returns>
        public static List<Point> ReadCsvTimeSeriesBucket(string filepath, long startIndex, int bucketSize)
        {
            var csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            using var streamReader = new StreamReader(filepath);
            using var csvReader = new CsvReader(streamReader, csvHelperConfig);

            return csvReader.GetRecords<Point>()
                .Skip((int)startIndex)
                .Take(bucketSize)
                .ToList();
        }

        /// <summary>
        /// Reads a time series in a specific number of buckets evenly distributed across it.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="bucketSize"></param>
        /// <param name="bucketNumber"></param>
        /// <returns></returns>
        public static List<List<Point>> ReadCsvTimeSeriesInSpecificBuckets(string filepath, int bucketSize, int bucketNumber)
        {
            var csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            using var streamReader = new StreamReader(filepath);
            using var csvReader = new CsvReader(streamReader, csvHelperConfig);

            var records = csvReader.GetRecords<Point>()
                .ToList();
            var chunkNumber = (int)Math.Floor((double)records.Count / bucketNumber);

            return records.Chunk(chunkNumber)
                .Select(chunk => chunk.Take(bucketSize).ToList())
                .Where(bucket => bucket.Count == bucketSize)
                .Take(bucketNumber)
                .ToList();
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

            csvWriter.WriteRecords(testResults);
        }
    }
}