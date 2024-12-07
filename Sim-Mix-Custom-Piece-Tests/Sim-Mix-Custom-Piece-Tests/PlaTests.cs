using Sim_Mix_Custom_Piece_Tests.Utilities.CsvFileUtilities;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestDataConfigurations;
using Sim_Mix_Custom_Piece_Tests.Utilities.TestModels;
using SimMixCustomPiece.Algorithms;
using SimMixCustomPiece.Algorithms.Utilities;
using SimMixCustomPiece.Models;
using SimMixCustomPiece.Models.LinearSegments;
using System.Collections.Concurrent;
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
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, "Sim-Piece Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();
            var testData = new TestData();

            // Not using the enumerator of TestData directly ensures sequential execution.
            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                Debug.WriteLine("");
                Debug.WriteLine("Data Set: " + dataSet);
                Debug.WriteLine("Bucket Size: " + bucketSize);
                Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
                Debug.WriteLine("");

                var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

                // Use the average compression ratio for this set of test data.
                var averageCompressionRatioList = new ConcurrentBag<double>();

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 4
                };

                Parallel.ForEach(timeSeriesInBuckets, parallelOptions, timeSeries =>
                {
                    Debug.WriteLine("Timestamp: " + timeSeries[0].SimpleTimestamp);

                    var compressedTimeSeries = CustomPiece.Compress(timeSeries, epsilonPercentage);
                    var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                    averageCompressionRatioList.Add(compressionRatio);
                });

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
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, "Mix-Piece Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();
            var testData = new TestData();

            foreach (var testDataPoint in testData)
            {
                var dataSet = testDataPoint[0].ToString();
                var bucketSize = (int)testDataPoint[1];
                var epsilonPercentage = (double)testDataPoint[2];

                Debug.WriteLine("");
                Debug.WriteLine("Data Set: " + dataSet);
                Debug.WriteLine("Bucket Size: " + bucketSize);
                Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
                Debug.WriteLine("");

                var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSet, bucketSize);

                // Use the average compression ratio for this set of test data.
                var averageCompressionRatioList = new ConcurrentBag<double>();

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 4
                };

                Parallel.ForEach(timeSeriesInBuckets, parallelOptions, timeSeries =>
                {
                    Debug.WriteLine("Timestamp: " + timeSeries[0].SimpleTimestamp);

                    var compressedTimeSeries = CustomPiece.Compress(timeSeries, epsilonPercentage);
                    var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                    averageCompressionRatioList.Add(compressionRatio);
                });

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
            // Pick a single data set at a time due to the speed of this test.
            var dataSet = TestData.DataSets[0];

            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.DataSetPath, dataSet);

            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, $"Custom-Piece {dataSet} Compression Test Results.csv");
            var testResults = new List<CompressionRatioTestResults>();

            foreach (var bucketSize in TestData.BucketSizes)
            {
                var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSetFilepath, bucketSize);

                foreach (var epsilonPercentage in TestData.EpsilonPercentages)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine("Data Set: " + dataSet);
                    Debug.WriteLine("Bucket Size: " + bucketSize);
                    Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
                    Debug.WriteLine("");

                    // Use the average compression ratio for this set of test data.
                    var averageCompressionRatioList = new ConcurrentBag<double>();

                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 4
                    };

                    Parallel.ForEach(timeSeriesInBuckets, parallelOptions, timeSeries =>
                    {
                        Debug.WriteLine("Timestamp: " + timeSeries[0].SimpleTimestamp);

                        var compressedTimeSeries = CustomPiece.Compress(timeSeries, epsilonPercentage);
                        var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                        averageCompressionRatioList.Add(compressionRatio);
                    });

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
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, $"Sim-Piece {epsilonPercentage} Percent Deviation Test Results.csv");
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
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, $"Mix-Piece {epsilonPercentage} Percent Deviation Test Results.csv");
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
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, $"Custom-Piece {epsilonPercentage} Percent Deviation Test Results.csv");
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
        // Used for hand-picking tests for more customizable CSV results.
        public void Table_1_timed_compression_results_Mix_Piece_Custom_Piece()
        {
            // Table 1 results.
            // Turbidity data set.
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 10, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 10, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 10, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 10, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 10, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 10, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 13, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 13, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 13, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 13, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 13, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 13, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 15, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 15, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 15, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 15, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 15, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 15, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 16, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 16, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 16, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 16, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 16, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[0], 16, 7, CustomPiece.Compress);

            // Salinity data set.
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 10, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 10, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 10, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 10, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 10, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 10, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 13, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 13, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 13, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 13, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 13, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 13, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 15, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 15, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 15, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 15, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 15, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 15, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 16, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 16, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 16, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 16, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 16, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[1], 16, 7, CustomPiece.Compress);

            // Temperature data set.
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 10, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 10, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 10, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 10, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 10, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 10, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 13, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 13, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 13, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 13, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 13, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 13, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 15, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 15, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 15, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 15, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 15, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 15, 7, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 16, 3, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 16, 3, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 16, 5, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 16, 5, CustomPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 16, 7, MixPiece.Compress);
            Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(TestData.DataSets[5], 16, 7, CustomPiece.Compress);
        }

        private static void Mix_Piece_based_algorithm_timed_compression_ratios_in_csv(string dataSet,
            int bucketSize,
            double epsilonPercentage,
            Func<List<Point>, double, Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>>> compressor)
        {            
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.DataSetPath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, $"Table 1 Data Set {dataSet} " +
                $"Bucket Size {bucketSize} Epsilon {epsilonPercentage} Compressor {compressor.Method.DeclaringType!.Name}.csv");
            var testResults = new List<TimedCompressionRatioTestResults>();

            var stopwatch = new Stopwatch();

            var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSetFilepath, bucketSize);

            Debug.WriteLine("");
            Debug.WriteLine("Data Set: " + dataSet);
            Debug.WriteLine("Bucket Size: " + bucketSize);
            Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
            Debug.WriteLine("");

            // Use the average compression ratio for this set of test data.
            var averageCompressionRatioList = new ConcurrentBag<double>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4
            };

            stopwatch.Start();

            Parallel.ForEach(timeSeriesInBuckets, parallelOptions, timeSeries =>
            {
                Debug.WriteLine("Timestamp: " + timeSeries[0].SimpleTimestamp);

                var compressedTimeSeries = compressor(timeSeries, epsilonPercentage);
                var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                averageCompressionRatioList.Add(compressionRatio);
            });

            stopwatch.Stop();

            var csvTestResults = new TimedCompressionRatioTestResults
            {
                BucketSize = bucketSize,
                CompressionRatio = averageCompressionRatioList.Sum() / averageCompressionRatioList.Count,
                DataSet = dataSet,
                EpsilonPercentage = epsilonPercentage,
                Compressor = compressor.Method.DeclaringType.Name,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };

            testResults.Add(csvTestResults);

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        /// <summary>
        /// Can be used for running through data sets not in entirety but in specific numbers of buckets.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="bucketSize"></param>
        /// <param name="bucketNumber"></param>
        /// <param name="epsilonPercentage"></param>
        /// <param name="compressor"></param>
        private static void Mix_Piece_based_algorithm_timed_compression_ratios_specified_buckets_in_csv(string dataSet,
            int bucketSize,
            int bucketNumber,
            double epsilonPercentage,
            Func<List<Point>, double, Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>>> compressor)
        {
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.DataSetPath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, $"Table 1 Data Set {dataSet} " +
                $"Bucket Size {bucketSize} Bucket Number {bucketNumber} Epsilon {epsilonPercentage} Compressor " +
                $"{compressor.Method.DeclaringType!.Name}.csv");
            var testResults = new List<TimedCompressionRatioTestResults>();

            var stopwatch = new Stopwatch();

            var timeSeriesInSpecificBuckets = CsvFileUtils.ReadCsvTimeSeriesInSpecificBuckets(dataSetFilepath, bucketSize, bucketNumber);

            Debug.WriteLine("");
            Debug.WriteLine("Data Set: " + dataSet);
            Debug.WriteLine("Bucket Size: " + bucketSize);
            Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
            Debug.WriteLine("");

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4
            };

            var averageCompressionRatioList = new List<double>();

            stopwatch.Start();

            Parallel.ForEach(timeSeriesInSpecificBuckets, parallelOptions, bucket =>
            {
                Debug.WriteLine("New Bucket (Timestamp): " + bucket[0].SimpleTimestamp);

                var compressedTimeSeries = compressor(bucket, epsilonPercentage);
                var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(bucket, compressedTimeSeries);

                averageCompressionRatioList.Add(compressionRatio);
            });

            stopwatch.Stop();

            var csvTestResults = new TimedCompressionRatioTestResults
            {
                BucketSize = bucketSize,
                CompressionRatio = averageCompressionRatioList.Sum() / averageCompressionRatioList.Count,
                DataSet = dataSet,
                EpsilonPercentage = epsilonPercentage,
                Compressor = compressor.Method.DeclaringType.Name,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };

            testResults.Add(csvTestResults);

            CsvFileUtils.WriteTestResultsToCsv(testResultsFilepath, testResults);
        }

        [Fact]
        public void Mix_Piece_trade_off_chart()
        {
            // Pick a specific data set.
            var dataSet = TestData.DataSets[0];
            var dataSetFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.DataSetPath, dataSet);
            var testResultsFilepath = Path.Combine(TestData.BaseDataFilepath, TestData.TestResultsPath, $"Trade Offs {dataSet}");
            var testResults = new List<CompressionRatioTestResults>();

            var epsilonPercentages = new List<double> { 5, 5.5, 6, 6.5, 7, 7.5, 8, 8.5, 9, 9.5, 10 };
            var bucketSizes = new List<int> { 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27 };

            foreach (var bucketSize in bucketSizes)
            {
                foreach (var epsilonPercentage in epsilonPercentages)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine("Bucket Size: " + bucketSize);
                    Debug.WriteLine("Epsilon Percentage: " + epsilonPercentage);
                    Debug.WriteLine("");

                    var timeSeriesInBuckets = CsvFileUtils.ReadWholeCsvTimeSeriesInBuckets(dataSetFilepath, bucketSize);

                    // Use the average compression ratio for this set of test data.
                    var averageCompressionRatioList = new ConcurrentBag<double>();

                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 4
                    };

                    Parallel.ForEach(timeSeriesInBuckets, parallelOptions, timeSeries =>
                    {
                        var compressedTimeSeries = MixPiece.Compress(timeSeries, epsilonPercentage);
                        var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                        averageCompressionRatioList.Add(compressionRatio);
                    });

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