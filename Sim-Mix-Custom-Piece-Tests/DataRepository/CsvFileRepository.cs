using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using Models.PLA;

namespace DataRepository
{
    /// <summary>
    /// Helps with reading from and writing to CSV files.
    /// </summary>
    public class CsvFileRepository : IFileRepository
    {
        public List<Point> ReadTimeSeries(string filepath)
        {
            var csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            using var streamReader = new StreamReader(filepath);
            using var csvReader = new CsvReader(streamReader, csvHelperConfig);

            return csvReader.GetRecords<Point>()
                .ToList();
        }

        public List<Point> ReadTimeSeries(string filepath, int startIndex)
        {
            var csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            using var streamReader = new StreamReader(filepath);
            using var csvReader = new CsvReader(streamReader, csvHelperConfig);

            return csvReader.GetRecords<Point>()
                .Skip(startIndex)
                .ToList();
        }

        public List<Point> ReadTimeSeries(string filepath, int startIndex, int size)
        {
            var csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ","
            };

            using var streamReader = new StreamReader(filepath);
            using var csvReader = new CsvReader(streamReader, csvHelperConfig);

            return csvReader.GetRecords<Point>()
                .Skip(startIndex)
                .Take(size)
                .ToList();
        }

        public List<List<Point>> ReadTimeSeriesInBuckets(string filepath, int bucketSize)
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
                .Chunk(bucketSize)
                .Where(bucket => bucket.Length == bucketSize)
                .Select(chunk => chunk.ToList())
                .ToList();
        }

        public List<List<Point>> ReadTimeSeriesInBuckets(string filepath, int bucketSize, int bucketNumber)
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
            var chunkSize = (int)Math.Floor((double)records.Count / bucketNumber);

            return records.Chunk(chunkSize)
                .Select(chunk => chunk.Take(bucketSize).ToList())
                .Where(bucket => bucket.Count == bucketSize)
                .Take(bucketNumber)
                .ToList();
        }

        public void Write<T>(string filepath, List<T> items)
        {
            using var streamWriter = new StreamWriter(filepath);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            csvWriter.WriteHeader<T>();
            csvWriter.NextRecord();

            csvWriter.WriteRecords(items);
        }
    }
}