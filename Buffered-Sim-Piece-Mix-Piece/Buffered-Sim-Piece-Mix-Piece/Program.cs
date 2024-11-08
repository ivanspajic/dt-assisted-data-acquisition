using Buffered_Sim_Piece_Mix_Piece.Algorithms;
using Buffered_Sim_Piece_Mix_Piece.Models;
using Buffered_Sim_Piece_Mix_Piece.Utilities;
using System.IO.Compression;
using System.Text.Json;

namespace Buffered_Sim_Piece_Mix_Piece
{
    internal class Program
    {
        private const string BaseFilepath = @"C:\dev\low-bandwidth-dt\Mix-Piece_Sim-Piece-1.0.0-RC1\src\test\resources\" +
            @"Austevoll Data\";

        static void Main(string[] args)
        {
            var bufferWindow = 10;

            var filenames = new string[]
            {
                "Turbidity#16340 - Analog Sensors #0.csv",
                "Pressure - Pressure Sensor #1955.csv",
                "Salinity - Conductivity Sensor #41.csv",
                "AirSaturation - Oxygen Optode #754.csv",
                "Chlorophyll#2103755 - Analog Sensors #0.csv",
                "Density - Conductivity Sensor #41.csv"
            };

            foreach (var filename in filenames)
                TestDataSet(filename, bufferWindow);
        }

        private static void TestDataSet(string name, int bufferWindowSize)
        {
            var filepath = Path.Combine(BaseFilepath, name);

            var timeSeries = TimeSeriesReader.ReadTimeSeriesDataFromFromFile(filepath, bufferWindowSize);

            var epsilonPercentage = 0.5;
            var epsilonPercentageSteps = 0.5;
            var epsilonMaximum = 5;

            Console.WriteLine($"Data set: {name}, Algorithm: Sim-Piece");
            do
            {
                var compressedTimeSeries = SimPiece.Compress(timeSeries, epsilonPercentage);

                var compressionRatio = PlaUtils.GetCompressionRatioForSimPiece(timeSeries, compressedTimeSeries);

                Console.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {compressionRatio:#.000}");

                epsilonPercentage += epsilonPercentageSteps;
            }
            while (epsilonPercentage <= epsilonMaximum);

            Console.WriteLine();
            Console.WriteLine();

            epsilonPercentage = 0.5;

            Console.WriteLine($"Data set: {name}, Algorithm: Mix-Piece");
            do
            {
                var compressedTimeSeries = MixPiece.Compress(timeSeries, epsilonPercentage);

                var compressionRatio = PlaUtils.GetCompressionRatioForMixPiece(timeSeries, compressedTimeSeries);

                Console.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {compressionRatio:#.000}");

                epsilonPercentage += epsilonPercentageSteps;
            }
            while (epsilonPercentage <= epsilonMaximum);

            Console.WriteLine();
            Console.WriteLine();

            epsilonPercentage = 0.5;

            Console.WriteLine($"Data set: {name}, Algorithm: Custom-Piece (Longest Segments)");
            do
            {
                var compressedTimeSeries = CustomPiece.CompressWithLongestSegments(timeSeries, epsilonPercentage);

                var compressionRatio = PlaUtils.GetCompressionRatioForCustomPiece(timeSeries, compressedTimeSeries);

                Console.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {compressionRatio:#.000}");

                epsilonPercentage += epsilonPercentageSteps;
            }
            while (epsilonPercentage <= epsilonMaximum);

            Console.WriteLine();
            Console.WriteLine();

            epsilonPercentage = 0.5;

            Console.WriteLine($"Data set: {name}, Algorithm: Custom-Piece (Most Compressible Segments)");
            do
            {
                var compressedTimeSeries = CustomPiece.CompressWithMostCompressibleSegments(timeSeries, epsilonPercentage);

                var compressionRatio = PlaUtils.GetCompressionRatioForCustomPiece(timeSeries, compressedTimeSeries);

                Console.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {compressionRatio:#.000}");

                epsilonPercentage += epsilonPercentageSteps;
            }
            while (epsilonPercentage <= epsilonMaximum);

            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
