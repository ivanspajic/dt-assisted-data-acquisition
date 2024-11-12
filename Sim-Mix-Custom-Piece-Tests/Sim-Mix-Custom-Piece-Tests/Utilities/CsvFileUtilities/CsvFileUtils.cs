using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using SimMixCustomPiece.Models;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.CsvFileUtilities
{
    internal static class CsvFileUtils
    {
        public static List<Point> ReadTimeSeriesFromCsvWithStartingTimestamp(string filepath, int startIndexInclusive, int bucketSize)
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

                while (i < startIndexInclusive)
                {
                    csvReader.Read();

                    i++;
                }

                while (csvReader.Read() && i < startIndexInclusive + bucketSize)
                {
                    timeSeries.Add(csvReader.GetRecord<Point>());

                    i++;
                }
            }

            return timeSeries;
        }

        public static List<Point> ReadTimeSeriesFromCsv(string filepath, int bucketSize)
        {
            return ReadTimeSeriesFromCsvWithStartingTimestamp(filepath, 0, bucketSize);
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
