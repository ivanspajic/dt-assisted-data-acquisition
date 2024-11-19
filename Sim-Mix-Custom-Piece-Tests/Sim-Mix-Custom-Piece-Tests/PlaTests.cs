using Sim_Mix_Custom_Piece_Tests.Utilities.CsvFileUtilities;
using Sim_Mix_Custom_Piece_Tests.Utilities.Systems;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestDataConfigurations;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestModels;
using SimMixCustomPiece.Algorithms;
using SimMixCustomPiece.Algorithms.Utilities;
using SimMixCustomPiece.Models;
using System.Diagnostics;

namespace Sim_Mix_Custom_Piece_Tests
{
    public class PlaTests
    {
        // Used for combating floating-point rounding errors during test assertions.
        private const double FloatingPointProblemFixer = 1.0000000001;

        #region Tests
        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Sim_Piece_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

            foreach (var timeSeries in timeSeriesInBuckets)
            {
                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

                var compressedTimeSeries = SimPiece.Compress(timeSeries, epsilonPercentage);
                var reconstructedTimeSeries = SimPiece.Decompress(compressedTimeSeries, timeSeries[^1].SimpleTimestamp);

                Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

                for (var i = 0; i < timeSeries.Count; i++)
                {
                    Assert.Equal(timeSeries[i].SimpleTimestamp, reconstructedTimeSeries[i].SimpleTimestamp);
                    Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon * FloatingPointProblemFixer);
                }
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Mix_Piece_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

            foreach (var timeSeries in timeSeriesInBuckets)
            {
                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

                var compressedTimeSeries = MixPiece.Compress(timeSeries, epsilonPercentage);
                var reconstructedTimeSeries = MixPiece.Decompress(compressedTimeSeries, timeSeries[^1].SimpleTimestamp);

                Assert.Equal(timeSeries.Count, reconstructedTimeSeries.Count);

                for (var i = 0; i < timeSeries.Count; i++)
                {
                    Assert.Equal(timeSeries[i].SimpleTimestamp, reconstructedTimeSeries[i].SimpleTimestamp);
                    Assert.True(Math.Abs(timeSeries[i].Value - reconstructedTimeSeries[i].Value) <= epsilon * FloatingPointProblemFixer);
                }
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Custom_Piece_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

            // Speeds this test up by skipping some buckets. Otherwise, this test takes a long time.
            var accelerator = 500;

            for (var i = 0; i < timeSeriesInBuckets.Count; i += accelerator)
            {
                var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeriesInBuckets[i], epsilonPercentage);

                var compressedTimeSeries = CustomPiece.Compress(timeSeriesInBuckets[i], epsilonPercentage);
                var reconstructedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, timeSeriesInBuckets[i][^1].SimpleTimestamp);

                Assert.Equal(timeSeriesInBuckets[i].Count, reconstructedTimeSeries.Count);

                for (var j = 0; j < timeSeriesInBuckets[i].Count; j++)
                {
                    Assert.Equal(timeSeriesInBuckets[i][j].SimpleTimestamp, reconstructedTimeSeries[j].SimpleTimestamp);
                    Assert.True(Math.Abs(timeSeriesInBuckets[i][j].Value - reconstructedTimeSeries[j].Value) <= epsilon * FloatingPointProblemFixer);
                }
            }
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Custom_Piece_yields_smallest_possible_Mix_Piece_result(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

            // Speeds this test up by skipping some buckets. Otherwise, this test takes a long time.
            var accelerator = 500;

            for (var i = 0; i < timeSeriesInBuckets.Count; i += accelerator)
            {
                var mixPieceCompressed = MixPiece.Compress(timeSeriesInBuckets[i], epsilonPercentage);
                var customPieceCompressed = CustomPiece.Compress(timeSeriesInBuckets[i], epsilonPercentage);

                var mixPieceCompressedRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeriesInBuckets[i], mixPieceCompressed);
                var customPieceCompressedRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeriesInBuckets[i], customPieceCompressed);

                Assert.True(customPieceCompressedRatio >= mixPieceCompressedRatio);
            }
        }

        //#endregion

        //#region Experiments

        [Fact]
        public void All_algorithms_compression_test_results_in_csv_files()
        {
            Sim_Piece_compression_test_results_in_csv_file();
            Mix_Piece_compression_test_results_in_csv_file();
            Custom_Piece_compression_test_results_in_csv_file();
        }

        [Fact]
        public void Sim_Piece_compression_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Sim-Piece Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();
            var testData = new TestData();

            // Not using the enumerator of TestData directly ensures sequential execution.
            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

                // Use the average compression ratio for this set of test data.
                var averageCompressionRatioList = new List<double>();

                foreach (var timeSeries in timeSeriesInBuckets)
                {
                    var compressedTimeSeries = SimPiece.Compress(timeSeries, epsilonPercentage);
                    var compressionRatio = PlaUtils.GetCompressionRatioForSimPiece(timeSeries, compressedTimeSeries);

                    averageCompressionRatioList.Add(compressionRatio);
                }

                var csvTestResults = new CompressionRatioTestResults
                {
                    BucketSize = bucketSize,
                    CompressionRatio = averageCompressionRatioList.Sum() / averageCompressionRatioList.Count,
                    DataSet = dataSet,
                    EpsilonPercentage = epsilonPercentage
                };

                testResults.Add(csvTestResults);
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

                Debug.WriteLine("Data Set: " + dataSet);
                Debug.WriteLine("Bucket Size: " + bucketSize);
                Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
                Debug.WriteLine("");
                Debug.WriteLine("");

                var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

                // Use the average compression ratio for this set of test data.
                var averageCompressionRatioList = new List<double>();

                foreach (var timeSeries in timeSeriesInBuckets)
                {
                    var compressedTimeSeries = MixPiece.Compress(timeSeries, epsilonPercentage);
                    var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                    averageCompressionRatioList.Add(compressionRatio);
                }

                var csvTestResults = new CompressionRatioTestResults
                {
                    BucketSize = bucketSize,
                    CompressionRatio = averageCompressionRatioList.Sum() / averageCompressionRatioList.Count,
                    DataSet = dataSet,
                    EpsilonPercentage = epsilonPercentage
                };

                testResults.Add(csvTestResults);
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Fact]
        public void Custom_Piece_compression_test_results_in_csv_file()
        {
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Custom-Piece Temperature Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();
            var testData = new TestData();

            // Pick a single data set at a time due to the speed of this test.
            var dataSet = Path.Combine(TestData.BaseDataFilepath, TestData.DataSets[5]);

            foreach (var bucketSize in TestData.BucketSizes)
            {
                var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

                foreach (var epsilonPercentage in TestData.EpsilonPercentages)
                {
                    Debug.WriteLine("Data Set: " + dataSet);
                    Debug.WriteLine("Bucket Size: " + bucketSize);
                    Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
                    Debug.WriteLine("");
                    Debug.WriteLine("");

                    // Use the average compression ratio for this set of test data.
                    var averageCompressionRatioList = new List<double>();

                    foreach (var timeSeries in timeSeriesInBuckets)
                    {
                        var compressedTimeSeries = CustomPiece.Compress(timeSeries, epsilonPercentage);
                        var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                        averageCompressionRatioList.Add(compressionRatio);
                    }

                    var csvTestResults = new CompressionRatioTestResults
                    {
                        BucketSize = bucketSize,
                        CompressionRatio = averageCompressionRatioList.Sum() / averageCompressionRatioList.Count,
                        DataSet = dataSet,
                        EpsilonPercentage = epsilonPercentage
                    };

                    testResults.Add(csvTestResults);
                }
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Theory] // The strings in InlineData parameters must be constant, so system-agnostic paths cannot be applied. Remember to update these manually according to need.
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 3)]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 5)]
        public void Sim_Piece_actual_and_decompressed_time_series_comparison_in_csv(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", $"Sim-Piece {epsilonPercentage} Percent Deviation Test Results.csv");
            var testResults = new List<DeviationTestResults>();

            var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSetFilepath, bucketSize);

            foreach (var originalTimeSeries in timeSeriesInBuckets)
            {
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
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Theory] // The strings in InlineData parameters must be constant, so system-agnostic paths cannot be applied. Remember to update these manually according to need.
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 1.5)]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 0.5)]
        public void Mix_Piece_actual_and_decompressed_time_series_comparison_in_csv(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", $"Mix-Piece {epsilonPercentage} Percent Deviation Test Results.csv");
            var testResults = new List<DeviationTestResults>();

            var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSetFilepath, bucketSize);

            foreach (var originalTimeSeries in timeSeriesInBuckets)
            {
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
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Theory] // The strings in InlineData parameters must be constant, so system-agnostic paths cannot be applied. Remember to update these manually according to need.
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 2)]
        [InlineData(@"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv", 10, 2.5)]
        public void Custom_Piece_actual_and_decompressed_time_series_comparison_in_csv(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", $"Custom-Piece {epsilonPercentage} Percent Deviation Test Results.csv");
            var testResults = new List<DeviationTestResults>();

            var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSetFilepath, bucketSize);

            foreach (var originalTimeSeries in timeSeriesInBuckets)
            {
                var compressedTimeSeries = CustomPiece.Compress(originalTimeSeries, epsilonPercentage);
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
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Fact]
        public void Digital_twin_temperature_prediction_data_savings_in_csv_file()
        {
            var testResults = new List<DigitalTwinPredictionTestResults>();

            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, "test-results", "Digital Twin Prediction Test Results.csv");
            var dataSet = Path.Combine(TestData.BaseDataFilepath, "data-sets", "austevoll-data", "Temperature - Temperature Sensor #1063.csv");
            var bucketSize = 15;

            foreach (var epsilonPercentage in TestData.EpsilonPercentages)
            {
                // Match the portion of the time series to test over with the current bucket size.
                var timeSeriesEndIndex = 100 * bucketSize;

                // Start at a required index to have enough earlier points available for subsequent predictions.
                for (var i = DigitalTwin.RequiredNumberOfPreviousPointsForPrediction; i < timeSeriesEndIndex; i += bucketSize)
                {
                    Debug.WriteLine("Data Set: " + dataSet);
                    Debug.WriteLine("Bucket Size: " + bucketSize);
                    Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
                    Debug.WriteLine("Timestamp: " + i);
                    Debug.WriteLine("");
                    Debug.WriteLine("");

                    // Get the actual portion of the time series being predicted and make a prediction for the same portion.
                    var actualTimeSeriesPortion = CsvFileUtils.ReadCsvTimeSeriesBucket(dataSet, i, bucketSize);

                    var previousTimeSeriesPortion = CsvFileUtils.ReadCsvTimeSeriesBucket(dataSet, i - bucketSize, bucketSize);
                    var predictedTimeSeriesPortion = DigitalTwin.GetPredictedTimeSeriesPortion(dataSet, previousTimeSeriesPortion, TestData.SamplingInterval);

                    // Simulate the sending of the compressed predicted time series portion to the sea-borne device for comparison.
                    var compressedPredictedTimeSeriesPortion = CustomPiece.Compress(predictedTimeSeriesPortion, epsilonPercentage);
                    var decompressedPredictedTimeSeriesPortion = CustomPiece.Decompress(compressedPredictedTimeSeriesPortion, predictedTimeSeriesPortion[^1].SimpleTimestamp);

                    // Check the average deviation percentage.
                    var averageDeviationPercentage = GetPercentageDeviationFromActual(actualTimeSeriesPortion, decompressedPredictedTimeSeriesPortion);

                    // Compress the actual time series and get its size.
                    var compressedActualTimeSeriesPortion = CustomPiece.Compress(actualTimeSeriesPortion, epsilonPercentage);
                    var compressedActualTimeSeriesSizeInBytes = PlaUtils.GetCompressedMixPieceTimeSeriesSizeInBytes(compressedActualTimeSeriesPortion);

                    var testResult = new DigitalTwinPredictionTestResults
                    {
                        DataSet = dataSet,
                        Epsilon = epsilonPercentage,
                        ZetaPercentage = TestData.ZetaPercentage,
                        StartTimestamp = actualTimeSeriesPortion[0].SimpleTimestamp,
                        EndTimestamp = actualTimeSeriesPortion[^1].SimpleTimestamp,
                        AverageDeviationPercentage = averageDeviationPercentage,
                        CompressedTimeSeriesSizeInBytes = compressedActualTimeSeriesSizeInBytes,
                        BytesTransmitted = 0
                    };

                    // Check if the deviation is greater than zeta, in which case the prediction is wrong, and the compressed time series needs to be sent from the
                    // sea-borne device.
                    if (testResult.AverageDeviationPercentage > testResult.ZetaPercentage)
                        testResult.BytesTransmitted = testResult.CompressedTimeSeriesSizeInBytes;

                    testResults.Add(testResult);
                }
            }

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the average deviation percentage based on the deviation from each actual time series point value.
        /// </summary>
        /// <param name="actualTimeSeries"></param>
        /// <param name="predictedTimeSeries"></param>
        /// <returns></returns>
        public static double GetPercentageDeviationFromActual(List<Point> actualTimeSeries, List<Point> testedTimeSeries)
        {
            var percentageAverageList = new List<double>();

            for (var j = 0; j < actualTimeSeries.Count; j++)
            {
                var difference = Math.Abs(actualTimeSeries[j].Value - testedTimeSeries[j].Value);
                var percentage = difference / actualTimeSeries[j].Value * 100;

                percentageAverageList.Add(percentage);
            }

            return percentageAverageList.Sum() / percentageAverageList.Count;
        }

        #endregion
    }
}