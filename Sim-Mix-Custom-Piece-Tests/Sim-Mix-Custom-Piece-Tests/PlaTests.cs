using CsvHelper;
using CsvHelper.Configuration;
using Sim_Mix_Custom_Piece_Tests.Utilities;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestModels;
using SimMixCustomPiece.Algorithms;
using SimMixCustomPiece.Algorithms.Utilities;
using SimMixCustomPiece.Models;
using System.Globalization;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Sim_Mix_Custom_Piece_Tests
{
    public class PlaTests
    {
        #region Tests
        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Sim_Piece_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = ReadTimeSeriesDataFromCsvFile(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);
            
            var compressedTimeSeries = SimPiece.Compress(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = SimPiece.Decompress(compressedTimeSeries, timeSeries[^1].Timestamp);

            Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

            for (var i = 0; i < timeSeries.Count; i++)
            {
                Assert.Equal(timeSeries[i].Timestamp, reconstructedTimeSeries[i].Timestamp);
                Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon);
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Mix_Piece_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = ReadTimeSeriesDataFromCsvFile(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var compressedTimeSeries = MixPiece.Compress(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = MixPiece.Decompress(compressedTimeSeries, timeSeries[^1].Timestamp);

            Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

            for (var i = 0; i < timeSeries.Count; i++)
            {
                Assert.Equal(timeSeries[i].Timestamp, reconstructedTimeSeries[i].Timestamp);
                Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon);
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Custom_Piece_longest_segments_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = ReadTimeSeriesDataFromCsvFile(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var compressedTimeSeries = CustomPiece.CompressWithLongestSegments(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, timeSeries[^1].Timestamp);

            Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

            for (var i = 0; i < timeSeries.Count; i++)
            {
                Assert.Equal(timeSeries[i].Timestamp, reconstructedTimeSeries[i].Timestamp);
                Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon);
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Custom_Piece_most_compressible_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = ReadTimeSeriesDataFromCsvFile(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var compressedTimeSeries = CustomPiece.CompressWithMostCompressibleSegments(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, timeSeries[^1].Timestamp);

            Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

            for (var i = 0; i < timeSeries.Count; i++)
            {
                Assert.Equal(timeSeries[i].Timestamp, reconstructedTimeSeries[i].Timestamp);
                Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon);
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Custom_Piece_longest_segments_yields_most_accurate_data(string dataSet, int bucketSize, double epsilonPercentage)
        {
            // TODO: show RMSE vs the original point values compared to the other algorithms.
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Custom_Piece_most_compressible_yields_smallest_Mix_Piece_result(string dataSet, int bucketSize, double epsilonPercentage)
        {
            // TODO: compare all Mix-Piece-based algorithms and show that Custom-Piece with the most compressible option always yields
            // the smallest size.
        }
        #endregion

        #region Experiments
        [Fact]
        public void All_algorithms_produce_test_results_in_csv_files()
        {
            Sim_Piece_produces_test_results_in_csv_file();
            Mix_Piece_produces_test_results_in_csv_file();
            Custom_Piece_longest_segments_produces_test_results_in_csv_file();
            Custom_Piece_most_compressible_segments_produces_test_results_in_csv_file();
        }

        [Fact]
        public void Sim_Piece_produces_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Sim-Piece Test Results.csv");
            var testResults = new List<CsvTestResults>();
            var testData = new TestData();

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeries = ReadTimeSeriesDataFromCsvFile(dataSet, bucketSize);

                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

                var compressedTimeSeries = SimPiece.Compress(timeSeries, epsilonPercentage);
                var compressionRatio = PlaUtils.GetCompressionRatioForSimPiece(timeSeries, compressedTimeSeries);

                var csvTestResults = new CsvTestResults
                {
                    BucketSize = bucketSize,
                    CompressionRatio = compressionRatio,
                    DataSet = dataSet,
                    EpsilonPercentage = epsilonPercentage
                };

                testResults.Add(csvTestResults);
            }

            WriteCompressionTestResultsToCsvFile(testResultsFilepath, testResults);
        }

        [Fact]
        public void Mix_Piece_produces_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Mix-Piece Test Results.csv");
            var testResults = new List<CsvTestResults>();
            var testData = new TestData();

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeries = ReadTimeSeriesDataFromCsvFile(dataSet, bucketSize);

                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

                var compressedTimeSeries = MixPiece.Compress(timeSeries, epsilonPercentage);
                var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                var csvTestResults = new CsvTestResults
                {
                    BucketSize = bucketSize,
                    CompressionRatio = compressionRatio,
                    DataSet = dataSet,
                    EpsilonPercentage = epsilonPercentage
                };

                testResults.Add(csvTestResults);
            }

            WriteCompressionTestResultsToCsvFile(testResultsFilepath, testResults);
        }

        [Fact]
        public void Custom_Piece_longest_segments_produces_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Custom-Piece (Longest Segments) Test Results.csv");
            var testResults = new List<CsvTestResults>();
            var testData = new TestData();

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeries = ReadTimeSeriesDataFromCsvFile(dataSet, bucketSize);

                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

                var compressedTimeSeries = CustomPiece.CompressWithLongestSegments(timeSeries, epsilonPercentage);
                var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                var csvTestResults = new CsvTestResults
                {
                    BucketSize = bucketSize,
                    CompressionRatio = compressionRatio,
                    DataSet = dataSet,
                    EpsilonPercentage = epsilonPercentage
                };

                testResults.Add(csvTestResults);
            }

            WriteCompressionTestResultsToCsvFile(testResultsFilepath, testResults);
        }

        [Fact]
        public void Custom_Piece_most_compressible_segments_produces_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Custom-Piece (Most Compressible Segments) Test Results.csv");
            var testResults = new List<CsvTestResults>();
            var testData = new TestData();

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeries = ReadTimeSeriesDataFromCsvFile(dataSet, bucketSize);

                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

                var compressedTimeSeries = CustomPiece.CompressWithMostCompressibleSegments(timeSeries, epsilonPercentage);
                var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                var csvTestResults = new CsvTestResults
                {
                    BucketSize = bucketSize,
                    CompressionRatio = compressionRatio,
                    DataSet = dataSet,
                    EpsilonPercentage = epsilonPercentage
                };

                testResults.Add(csvTestResults);
            }

            WriteCompressionTestResultsToCsvFile(testResultsFilepath, testResults);
        }

        [Fact]
        public void Digital_twin_prediction_data_savings_in_csv_file()
        {
            // TODO: make the DT prediction experiment that shows how much data is saved by attempting to predict what the
            // sea-borne sensors will send. Pick a data set and go over a portion to simulate predictions.
        }
        #endregion

        #region Helpers
        private List<Point> ReadTimeSeriesDataFromCsvFile(string filepath, int bucketSize)
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
                while (csvReader.Read() && i < bucketSize)
                {
                    timeSeries.Add(csvReader.GetRecord<Point>());

                    i++;
                }
            }

            return timeSeries;
        }

        private void WriteCompressionTestResultsToCsvFile(string filepath, List<CsvTestResults> testResults)
        {
            using var streamWriter = new StreamWriter(filepath);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            csvWriter.WriteHeader<CsvTestResults>();
            csvWriter.NextRecord();

            foreach (var testResult in testResults)
            {
                csvWriter.WriteRecord(testResult);
                csvWriter.NextRecord();
            }
        }
        #endregion
    }
}