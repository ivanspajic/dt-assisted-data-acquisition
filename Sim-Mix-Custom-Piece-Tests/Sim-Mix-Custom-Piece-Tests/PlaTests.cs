using CsvHelper;
using CsvHelper.Configuration;
using Sim_Mix_Custom_Piece_Tests.Utilities.Systems;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestDataConfigurations;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestModels;
using SimMixCustomPiece.Algorithms;
using SimMixCustomPiece.Algorithms.Utilities;
using SimMixCustomPiece.Models;
using SimMixCustomPiece.Models.LinearSegments;
using System.Globalization;

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
            var testResults = new List<CompressionRatioTestResults>();
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

                var csvTestResults = new CompressionRatioTestResults
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
            var testResults = new List<CompressionRatioTestResults>();
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

                var csvTestResults = new CompressionRatioTestResults
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
            var testResults = new List<CompressionRatioTestResults>();
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

                var csvTestResults = new CompressionRatioTestResults
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
            var testResults = new List<CompressionRatioTestResults>();
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

                var csvTestResults = new CompressionRatioTestResults
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
            // pick a data set and skip over the first 10 points to have something to start from
            // make a prediction based on the first 10 for what the next 10 would be
            // make a compression based on the real next 10 points
            // compare the prediction with the real ones and determine if the real ones should be sent

            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Digital Twin Prediction Test Results.csv");
            var testResults = new List<EnergySavingsTestResults>();
            var testData = new TestData();

            var zetaPercentage = 5;

            // Determines whether compression should happen for highest accuracy or highest compressibility.
            var isMostAccurate = true;

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];
                
                // Match the portion of the time series to the current bucket size.
                var timeSeriesEndIndex = 100 * bucketSize;

                var requiredTimeSeriesPortion = DigitalTwin.GetRequiredTimeSeriesPortionForBucketSize(bucketSize);

                // Start at 2 * bucketSize index to have enough earlier points available for subsequent predictions.
                for (var i = requiredTimeSeriesPortion; i < timeSeriesEndIndex; i += bucketSize)
                {
                    var actualTimeSeriesPortion = ReadTimeSeriesDataFromCsvFile(dataSet, i, bucketSize);

                    var oldTimeSeriesPortion = ReadTimeSeriesDataFromCsvFile(dataSet, i - requiredTimeSeriesPortion, requiredTimeSeriesPortion);
                    var predictedTimeSeriesPortion = DigitalTwin.GetPredictedTimeSeriesPortionFromPreviousPortion(oldTimeSeriesPortion);

                    Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedPredictedTimeSeriesPortion;
                    Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedActualTimeSeriesPortion;
                    if (isMostAccurate)
                    {
                        compressedPredictedTimeSeriesPortion = CustomPiece.CompressWithLongestSegments(predictedTimeSeriesPortion, epsilonPercentage);
                        compressedActualTimeSeriesPortion = CustomPiece.CompressWithLongestSegments(actualTimeSeriesPortion, epsilonPercentage);
                    }
                    else
                    {
                        compressedPredictedTimeSeriesPortion = CustomPiece.CompressWithMostCompressibleSegments(predictedTimeSeriesPortion, epsilonPercentage);
                        compressedActualTimeSeriesPortion = CustomPiece.CompressWithMostCompressibleSegments(actualTimeSeriesPortion, epsilonPercentage);
                    }


                }
            }

            WriteCompressionTestResultsToCsvFile(testResultsFilepath, testResults);
        }
        #endregion

        #region Helpers
        private List<Point> ReadTimeSeriesDataFromCsvFile(string filepath, int startIndexInclusive, int bucketSize)
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

        private List<Point> ReadTimeSeriesDataFromCsvFile(string filepath, int bucketSize)
        {
            return ReadTimeSeriesDataFromCsvFile(filepath, 0, bucketSize);
        }

        private void WriteCompressionTestResultsToCsvFile<T>(string filepath, List<T> testResults)
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
        #endregion
    }
}