﻿using Buffered_Sim_Piece_Mix_Piece.Models;
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

        /// <summary>
        /// Gets the epsilon relative to the maximum and minimum values in the time series.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="epsilonPercentage"></param>
        /// <returns></returns>
        public static double GetEpsilonForTimeSeries(List<Point> timeSeries, double epsilonPercentage)
        {
            var maximumValue = double.NegativeInfinity;
            var minimumValue = double.PositiveInfinity;

            foreach (var point in timeSeries)
            {
                maximumValue = Math.Max(maximumValue, point.Value);
                minimumValue = Math.Min(minimumValue, point.Value);
            }

            return (maximumValue - minimumValue) * (epsilonPercentage / 100);
        }

        /// <summary>
        /// Used for sorting segments by their lower bound gradients.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int CompareSegmentsByLowerBound(Segment x, Segment y)
        {
            if (x.LowerBoundGradient < y.LowerBoundGradient)
                return -1;
            else if (x.LowerBoundGradient > y.LowerBoundGradient)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// Used for sorting segments by their start timestamps.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int CompareSegmentsByStartTimestamp(Segment x, Segment y)
        {
            if (x.StartTimestamp < y.StartTimestamp)
                return -1;
            else if (x.StartTimestamp > y.StartTimestamp)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// Returns a floor-quantized value within the provided epsilon.
        /// </summary>
        /// <param name="pointValue"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static double GetFloorQuantizedValue(double pointValue, double epsilon)
        {
            return Math.Floor(pointValue / epsilon) * epsilon;
        }

        /// <summary>
        /// Returns the ceiling-quantized value within the provided epsilon.
        /// </summary>
        /// <param name="pointValue"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static double GetCeilingQuantizedValue(double pointValue, double epsilon)
        {
            return Math.Ceiling(pointValue / epsilon) * epsilon;
        }

        /// <summary>
        /// Returns the compression ratio for a time series compressed with Sim-Piece.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="compressedTimeSeries"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the compression ratio for a time series compressed with Mix-Piece (or Mix-Piece-based Custom-Piece).
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="compressedTimeSeries"></param>
        /// <returns></returns>
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
    }
}
