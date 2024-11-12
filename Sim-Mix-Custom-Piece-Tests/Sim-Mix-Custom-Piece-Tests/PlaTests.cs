using Sim_Mix_Custom_Piece_Tests.Utilities.CsvFileUtilities;
using Sim_Mix_Custom_Piece_Tests.Utilities.Systems;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestDataConfigurations;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestModels;
using SimMixCustomPiece.Algorithms;
using SimMixCustomPiece.Algorithms.Utilities;
using SimMixCustomPiece.Models.LinearSegments;
using System.Diagnostics;

namespace Sim_Mix_Custom_Piece_Tests
{
    public class PlaTests
    {
        #region Tests
        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Sim_Piece_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);
            
            var compressedTimeSeries = SimPiece.Compress(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = SimPiece.Decompress(compressedTimeSeries, timeSeries[^1].SimpleTimestamp);

            Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

            for (var i = 0; i < timeSeries.Count; i++)
            {
                Assert.Equal(timeSeries[i].SimpleTimestamp, reconstructedTimeSeries[i].SimpleTimestamp);
                Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon);
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Mix_Piece_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var compressedTimeSeries = MixPiece.Compress(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = MixPiece.Decompress(compressedTimeSeries, timeSeries[^1].SimpleTimestamp);

            Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

            for (var i = 0; i < timeSeries.Count; i++)
            {
                Assert.Equal(timeSeries[i].SimpleTimestamp, reconstructedTimeSeries[i].SimpleTimestamp);
                Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon);
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Custom_Piece_longest_segments_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, bucketSize);
            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);
            // Creates the longest segments.
            var compressForHighestAccuracy = true;

            var compressedTimeSeries = CustomPiece.Compress(timeSeries, epsilonPercentage, compressForHighestAccuracy);
            var reconstructedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, timeSeries[^1].SimpleTimestamp);

            Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

            for (var i = 0; i < timeSeries.Count; i++)
            {
                Assert.Equal(timeSeries[i].SimpleTimestamp, reconstructedTimeSeries[i].SimpleTimestamp);
                Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon);
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Custom_Piece_most_compressible_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, bucketSize);
            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);
            // Creates the most compressible segments.
            var compressForHighestAccuracy = false;

            var compressedTimeSeries = CustomPiece.Compress(timeSeries, epsilonPercentage, compressForHighestAccuracy);
            var reconstructedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, timeSeries[^1].SimpleTimestamp);

            Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

            for (var i = 0; i < timeSeries.Count; i++)
            {
                Assert.Equal(timeSeries[i].SimpleTimestamp, reconstructedTimeSeries[i].SimpleTimestamp);
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
        public void All_algorithms_compression_test_results_in_csv_files()
        {
            Sim_Piece_compression_test_results_in_csv_file();
            Mix_Piece_compression_test_results_in_csv_file();
            Custom_Piece_longest_segments_compression_test_results_in_csv_file();
            Custom_Piece_most_compressible_segments_compression_test_results_in_csv_file();
        }

        [Fact]
        public void Sim_Piece_compression_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Sim-Piece Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();
            var testData = new TestData();

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, bucketSize);

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

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Theory]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 3)]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 5)]
        public void Sim_Piece_actual_and_decompressed_time_series_comparison_in_csv(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", $"Sim-Piece {epsilonPercentage} Percent Deviation Test Results.csv");
            var testResults = new List<DeviationTestResults>();

            var originalTimeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSetFilepath, bucketSize);
            var compressedTimeSeries = SimPiece.Compress(originalTimeSeries, epsilonPercentage);
            var decompressedTimeSeries = SimPiece.Decompress(compressedTimeSeries, originalTimeSeries[^1].SimpleTimestamp);
            var epsilon = PlaUtils.GetEpsilonForTimeSeries(originalTimeSeries, epsilonPercentage);

            for (var i = 0; i < originalTimeSeries.Count; i++)
            {
                testResults.Add(new DeviationTestResults
                {
                    Timestamp = originalTimeSeries[i].SimpleTimestamp,
                    OriginalValue = originalTimeSeries[i].Value,
                    ReconstructedValue = decompressedTimeSeries[i].Value,
                    LowerBound = decompressedTimeSeries[i].Value - epsilon,
                    UpperBound = decompressedTimeSeries[i].Value + epsilon
                });
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Fact]
        public void Mix_Piece_compression_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Mix-Piece Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();
            var testData = new TestData();

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, bucketSize);

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

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Theory]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 1.5)]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 0.5)]
        public void Mix_Piece_actual_and_decompressed_time_series_comparison_in_csv(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", $"Mix-Piece {epsilonPercentage} Percent Deviation Test Results.csv");
            var testResults = new List<DeviationTestResults>();

            var originalTimeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSetFilepath, bucketSize);
            var compressedTimeSeries = MixPiece.Compress(originalTimeSeries, epsilonPercentage);
            var decompressedTimeSeries = MixPiece.Decompress(compressedTimeSeries, originalTimeSeries[^1].SimpleTimestamp);
            var epsilon = PlaUtils.GetEpsilonForTimeSeries(originalTimeSeries, epsilonPercentage);

            for (var i = 0; i < originalTimeSeries.Count; i++)
            {
                testResults.Add(new DeviationTestResults
                {
                    Timestamp = originalTimeSeries[i].SimpleTimestamp,
                    OriginalValue = originalTimeSeries[i].Value,
                    ReconstructedValue = decompressedTimeSeries[i].Value,
                    LowerBound = decompressedTimeSeries[i].Value - epsilon,
                    UpperBound = decompressedTimeSeries[i].Value + epsilon
                });
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Fact]
        public void Custom_Piece_longest_segments_compression_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Custom-Piece (Longest Segments) Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();
            var testData = new TestData();

            // Creates the longest segments.
            var compressForHighestAccuracy = true;

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, bucketSize);

                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

                var compressedTimeSeries = CustomPiece.Compress(timeSeries, epsilonPercentage, compressForHighestAccuracy);
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

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Theory]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 2)]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 3.5)]
        public void Custom_Piece_longest_segments_actual_and_decompressed_time_series_comparison_in_csv(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", $"Custom-Piece (Longest Segments) {epsilonPercentage} Percent Deviation Test Results.csv");
            var testResults = new List<DeviationTestResults>();

            // Creates the longest segments.
            var compressForHighestAccuracy = true;

            var originalTimeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSetFilepath, bucketSize);
            var compressedTimeSeries = CustomPiece.Compress(originalTimeSeries, epsilonPercentage, compressForHighestAccuracy);
            var decompressedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, originalTimeSeries[^1].SimpleTimestamp);
            var epsilon = PlaUtils.GetEpsilonForTimeSeries(originalTimeSeries, epsilonPercentage);

            for (var i = 0; i < originalTimeSeries.Count; i++)
            {
                testResults.Add(new DeviationTestResults
                {
                    Timestamp = originalTimeSeries[i].SimpleTimestamp,
                    OriginalValue = originalTimeSeries[i].Value,
                    ReconstructedValue = decompressedTimeSeries[i].Value,
                    LowerBound = decompressedTimeSeries[i].Value - epsilon,
                    UpperBound = decompressedTimeSeries[i].Value + epsilon
                });
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Fact]
        public void Custom_Piece_most_compressible_segments_compression_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Custom-Piece (Most Compressible Segments) Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();
            var testData = new TestData();

            // Creates the most compressible segments.
            var compressForHighestAccuracy = false;

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSet, bucketSize);

                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

                var compressedTimeSeries = CustomPiece.Compress(timeSeries, epsilonPercentage, compressForHighestAccuracy);
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

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Theory]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 2)]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 2.5)]
        public void Custom_Piece_most_compressible_segments_actual_and_decompressed_time_series_comparison_in_csv(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", $"Custom-Piece (Most Compressible Segments) {epsilonPercentage} Percent Deviation Test Results.csv");
            var testResults = new List<DeviationTestResults>();

            // Creates the most compressible segments.
            var compressForHighestAccuracy = false;

            var originalTimeSeries = CsvFileUtils.ReadTimeSeriesFromCsv(dataSetFilepath, bucketSize);
            var compressedTimeSeries = CustomPiece.Compress(originalTimeSeries, epsilonPercentage, compressForHighestAccuracy);
            var decompressedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, originalTimeSeries[^1].SimpleTimestamp);
            var epsilon = PlaUtils.GetEpsilonForTimeSeries(originalTimeSeries, epsilonPercentage);

            for (var i = 0; i < originalTimeSeries.Count; i++)
            {
                testResults.Add(new DeviationTestResults
                {
                    Timestamp = originalTimeSeries[i].SimpleTimestamp,
                    OriginalValue = originalTimeSeries[i].Value,
                    ReconstructedValue = decompressedTimeSeries[i].Value,
                    LowerBound = decompressedTimeSeries[i].Value - epsilon,
                    UpperBound = decompressedTimeSeries[i].Value + epsilon
                });
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Fact]
        public void Digital_twin_temperature_prediction_data_savings_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Digital Twin Prediction Test Results.csv");
            var testResults = new List<EnergySavingsTestResults>();
            
            var testData = new TestData();
            var dataSet = Path.Combine(TestData.BaseDataFilepath, "data-sets", "austevoll-data", "Temperature - Temperature Sensor #1063.csv");
            var bucketSize = 10;

            var percentageList = new List<double>();

            foreach (var epsilonPercentage in TestData.EpsilonPercentages)
            {
                // Match the portion of the time series to test over with the current bucket size.
                var timeSeriesEndIndex = 100 * bucketSize;

                // Start at a required index to have enough earlier points available for subsequent predictions.
                for (var i = DigitalTwin.RequiredNumberOfPreviousPointsForPrediction; i < timeSeriesEndIndex; i += bucketSize)
                {
                    var actualTimeSeriesPortion = CsvFileUtils.ReadTimeSeriesFromCsvWithStartingTimestamp(dataSet, i, bucketSize);
                    var predictedTimeSeriesPortion = DigitalTwin.GetPredictedTimeSeriesPortion(dataSet, i, TestData.SamplingInterval, bucketSize);

                    for (var j = 0; j < actualTimeSeriesPortion.Count; j++)
                    {
                        var difference = Math.Abs(actualTimeSeriesPortion[j].Value - predictedTimeSeriesPortion[j].Value);
                        var percentage = difference / actualTimeSeriesPortion[j].Value * 100;

                        percentageList.Add(percentage);
                    }

                    var compressedPredictedTimeSeriesPortion = CustomPiece.Compress(predictedTimeSeriesPortion, epsilonPercentage, TestData.CompressForHighestAccuracy);
                    var compressedActualTimeSeriesPortion = CustomPiece.Compress(actualTimeSeriesPortion, epsilonPercentage, TestData.CompressForHighestAccuracy);
                }
            }

            var average = percentageList.Sum() / percentageList.Count;
            Debug.WriteLine(average);

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }
        #endregion
    }
}