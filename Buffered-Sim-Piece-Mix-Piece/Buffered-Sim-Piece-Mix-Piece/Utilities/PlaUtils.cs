using Buffered_Sim_Piece_Mix_Piece.Models;
using Buffered_Sim_Piece_Mix_Piece.Models.LinearSegments;

namespace Buffered_Sim_Piece_Mix_Piece.Utilities
{
    /// <summary>
    /// Offers a few different algorithms for performing lossy compression with Piece-wise Linear Approximation.
    /// </summary>
    internal static class PlaUtils
    {
        public static int CompareSegmentsByLowerBound(Segment x, Segment y)
        {
            if (x.LowerBoundGradient < y.LowerBoundGradient)
                return -1;
            else if (x.LowerBoundGradient > y.LowerBoundGradient)
                return 1;
            else
                return 0;
        }

        public static double GetFloorQuantizedValue(double pointValue, double epsilon)
        {
            return Math.Floor(pointValue / epsilon) * epsilon;
        }

        public static double GetCeilingQuantizedValue(double pointValue, double epsilon)
        {
            return Math.Ceiling(pointValue / epsilon) * epsilon;
        }

        public static double GetCompressionRatioForSimPiece(List<Point> timeSeries, List<GroupedLinearSegment> compressedTimeSeries)
        {
            // A point can be represented with 1 byte for the timestamp + 8 bytes for the value.
            double timeSeriesSize = timeSeries.Count * (1 + 8);

            // A grouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for every timestamp in the group.
            double compressedTimeSeriesSize = compressedTimeSeries.Count * (8 + 8);

            foreach (var linearSegment in compressedTimeSeries)
                compressedTimeSeriesSize += linearSegment.Timestamps.Count;

            return timeSeriesSize / compressedTimeSeriesSize;
        }

        public static double GetCompressionRatioForMixPiece(List<Point> timeSeries,
            Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedTimeSeries)
        {
            // A point can be represented with 1 byte for the timestamp + 8 bytes for the value.
            double timeSeriesSize = timeSeries.Count * (1 + 8);

            // A grouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for every timestamp in the group.
            double compressedGroupedLinearSegmentsSize = compressedTimeSeries.Item1.Count * (8 + 8);

            // A half-grouped linear segment can be represented with 8 bytes for the gradient + 8 bytes for every quantized value in the group + 1 byte for every
            // timestamp in the group.
            double compressedHalfGroupedLinearSegmentsSize = compressedTimeSeries.Item2.Count * 8;

            foreach (var halfGroupedLinearSegment in compressedTimeSeries.Item2)
                compressedHalfGroupedLinearSegmentsSize += halfGroupedLinearSegment.QuantizedValueTimestampPairs.Count * (8 + 1);

            // An ungrouped linear segment can be represented with 8 bytes for the gradient + 8 bytes for the quantized value + 1 byte for the timestamp.
            double compressedUngroupedLinearSegmentsSize = compressedTimeSeries.Item3.Count * (8 + 8 + 1);

            return timeSeriesSize / (compressedGroupedLinearSegmentsSize + compressedHalfGroupedLinearSegmentsSize + compressedUngroupedLinearSegmentsSize);
        }

        public static double GetCompressionRatioForBufferedPiece(List<Point> timeSeries, 
            Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedTimeSeries)
        {
            return 0;
        }
    }
}
