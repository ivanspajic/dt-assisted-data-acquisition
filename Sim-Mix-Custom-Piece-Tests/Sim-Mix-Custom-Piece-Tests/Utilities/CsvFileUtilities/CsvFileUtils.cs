using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using SimMixCustomPiece.Models;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.CsvFileUtilities
{
    internal static class CsvFileUtils
    {
        public static List<List<Point>> ReadCsvTimeSeriesInBuckets(string filepath, int bucketSize)
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
                var i = 0;
                var j = 0;

                var currentBucket = new List<Point>();
                while (csvReader.Read())
                {
                    if (j == bucketSize)
                    {
                        timeSeriesInBuckets.Add(currentBucket);

                        currentBucket = [];
                        j = 0;
                    }

                    currentBucket.Add(csvReader.GetRecord<Point>());

                    i++;
                    j++;
                }
            }

            return timeSeriesInBuckets;
        }

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
