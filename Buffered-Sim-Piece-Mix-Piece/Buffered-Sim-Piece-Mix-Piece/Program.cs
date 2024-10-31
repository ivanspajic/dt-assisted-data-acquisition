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
                "Turbidity#16340 - Analog Sensors #0.csv"
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

            Console.WriteLine("Sim-Piece");
            do
            {
                var compressedTimeSeries = PiecewiseLinearApproximation.CompressWithSimPiece(timeSeries, epsilonPercentage);

                var compressionRatio = GetCompressionRatio(timeSeries, compressedTimeSeries);

                Console.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {compressionRatio.ToString("#.000")}");

                epsilonPercentage += epsilonPercentageSteps;
            }
            while (epsilonPercentage <= epsilonMaximum);

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Mix-Piece");
            do
            {
                var compressedTimeSeries = PiecewiseLinearApproximation.CompressWithMixPiece(timeSeries, epsilonPercentage);

                var compressionRatio = GetCompressionRatio(timeSeries, compressedTimeSeries);

                Console.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {compressionRatio}");

                epsilonPercentage += epsilonPercentageSteps;
            }
            while (epsilonPercentage <= epsilonMaximum);

            Console.WriteLine();
            Console.WriteLine();

            //Console.WriteLine("Buffered Sim-Piece");
            //do
            //{
            //    var compressedTimeSeries = PiecewiseLinearApproximation.CompressWithSimPiece(timeSeries, epsilonPercentage);

            //    var compressionRatio = GetCompressionRatio(timeSeries, compressedTimeSeries);

            //    Console.WriteLine($"Epsilon: {epsilonPercentage}%, Compression Ratio: {compressionRatio}");

            //    epsilonPercentage += epsilonPercentageSteps;
            //}
            //while (epsilonPercentage <= epsilonMaximum);
        }

        private static double GetCompressionRatio(List<Point> timeSeries, List<LinearSegment> compressedTimeSeries)
        {
            // A point can be represented with 1 byte for the timestamp + 8 bytes for the value.
            double timeSeriesSize = timeSeries.Count * (1 + 8);

            // A linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the
            // gradient + 1 byte for every timestamp in every group.
            double compressedTimeSeriesSize = compressedTimeSeries.Count * (8 + 8);

            foreach (var linearSegment in compressedTimeSeries)
                compressedTimeSeriesSize += linearSegment.Timestamps.Count;

            return timeSeriesSize / compressedTimeSeriesSize;
        }
    }
}
