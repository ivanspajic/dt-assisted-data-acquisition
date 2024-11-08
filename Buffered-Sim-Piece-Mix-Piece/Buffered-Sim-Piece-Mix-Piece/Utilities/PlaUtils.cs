using Buffered_Sim_Piece_Mix_Piece.Models;
using Buffered_Sim_Piece_Mix_Piece.Models.LinearSegments;

namespace Buffered_Sim_Piece_Mix_Piece.Utilities
{
    /// <summary>
    /// Offers a few different algorithms for performing lossy compression with Piece-wise Linear Approximation.
    /// </summary>
    internal static class PlaUtils
    {
        private const int ByteSize = sizeof(byte);
        private const int TimestampSize = ByteSize;
        private const int QuantizedValueSize = 8 * ByteSize;
        private const int GradientValueSize = 8 * ByteSize;

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
            double timeSeriesSize = timeSeries.Count * (TimestampSize + QuantizedValueSize);

            // A grouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for every timestamp in the
            // group.
            double compressedTimeSeriesSize = compressedTimeSeries.Count * (QuantizedValueSize + GradientValueSize);

            foreach (var linearSegment in compressedTimeSeries)
                compressedTimeSeriesSize += linearSegment.Timestamps.Count;

            return timeSeriesSize / compressedTimeSeriesSize;
        }

        public static double GetCompressionRatioForMixPiece(List<Point> timeSeries,
            Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedTimeSeries)
        {
            // A point can be represented with 1 byte for the timestamp + 8 bytes for the value.
            double timeSeriesSize = timeSeries.Count * (TimestampSize + QuantizedValueSize);

            // A grouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for every timestamp in the
            // group.
            double compressedGroupedLinearSegmentsSize = compressedTimeSeries.Item1.Count * (QuantizedValueSize + GradientValueSize);

            // A half-grouped linear segment can be represented with 8 bytes for the gradient + 8 bytes for every quantized value in the group + 1 byte for every
            // timestamp in the group.
            double compressedHalfGroupedLinearSegmentsSize = compressedTimeSeries.Item2.Count * GradientValueSize;

            foreach (var halfGroupedLinearSegment in compressedTimeSeries.Item2)
                compressedHalfGroupedLinearSegmentsSize += halfGroupedLinearSegment.QuantizedValueTimestampPairs.Count * (QuantizedValueSize + TimestampSize);

            // An ungrouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for the timestamp.
            double compressedUngroupedLinearSegmentsSize = compressedTimeSeries.Item3.Count * (QuantizedValueSize + GradientValueSize + TimestampSize);

            return timeSeriesSize / (compressedGroupedLinearSegmentsSize + compressedHalfGroupedLinearSegmentsSize + compressedUngroupedLinearSegmentsSize);
        }

        public static double GetCompressionRatioForCustomPiece(List<Point> timeSeries, 
            Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedTimeSeries)
        {
            // A point can be represented with 1 byte for the timestamp + 8 bytes for the value.
            double timeSeriesSize = timeSeries.Count * (TimestampSize + QuantizedValueSize);

            // A grouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for every timestamp in the
            // group.
            double compressedGroupedLinearSegmentsSize = compressedTimeSeries.Item1.Count * (QuantizedValueSize + GradientValueSize);

            // A half-grouped linear segment can be represented with 8 bytes for the gradient + 8 bytes for every quantized value in the group + 1 byte for every
            // timestamp in the group.
            double compressedHalfGroupedLinearSegmentsSize = compressedTimeSeries.Item2.Count * GradientValueSize;

            foreach (var halfGroupedLinearSegment in compressedTimeSeries.Item2)
                compressedHalfGroupedLinearSegmentsSize += halfGroupedLinearSegment.QuantizedValueTimestampPairs.Count * (QuantizedValueSize + TimestampSize);

            // An ungrouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for the timestamp.
            double compressedUngroupedLinearSegmentsSize = compressedTimeSeries.Item3.Count * (QuantizedValueSize + GradientValueSize + TimestampSize);

            return timeSeriesSize / (compressedGroupedLinearSegmentsSize + compressedHalfGroupedLinearSegmentsSize + compressedUngroupedLinearSegmentsSize);
        }
    }
}
