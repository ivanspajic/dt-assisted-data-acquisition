using Sim_Mix_Custom_Piece_Tests.Utilities;
using SimMixCustomPiece.Algorithms;
using SimMixCustomPiece.Algorithms.Utilities;
using System.Diagnostics;

namespace Sim_Mix_Custom_Piece_Tests
{
    public class PlaTests
    {
        [Theory]
        [ClassData(typeof(TestData))]
        public void Reconstructed_Sim_Piece_time_series_within_epsilon_of_original(string dataSet, int bucketSize, double epsilonPercentage)
        {
            var timeSeries = TimeSeriesReader.ReadTimeSeriesDataFromFromFile(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);
            
            var compressedTimeSeries = SimPiece.Compress(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = SimPiece.Decompress(compressedTimeSeries, timeSeries[^1].Timestamp);

            Debug.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {PlaUtils.GetCompressionRatioForSimPiece(timeSeries, compressedTimeSeries)}");

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
            var timeSeries = TimeSeriesReader.ReadTimeSeriesDataFromFromFile(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var compressedTimeSeries = MixPiece.Compress(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = MixPiece.Decompress(compressedTimeSeries, timeSeries[^1].Timestamp);

            Debug.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries)}");

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
            var timeSeries = TimeSeriesReader.ReadTimeSeriesDataFromFromFile(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var compressedTimeSeries = CustomPiece.CompressWithLongestSegments(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, timeSeries[^1].Timestamp);

            Debug.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries)}");

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
            var timeSeries = TimeSeriesReader.ReadTimeSeriesDataFromFromFile(dataSet, bucketSize);

            var epsilon = PlaUtils.GetEpsilonForTimeSeries(timeSeries, epsilonPercentage);

            var compressedTimeSeries = CustomPiece.CompressWithMostCompressibleSegments(timeSeries, epsilonPercentage);
            var reconstructedTimeSeries = CustomPiece.Decompress(compressedTimeSeries, timeSeries[^1].Timestamp);

            Debug.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries)}");

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
    }
}