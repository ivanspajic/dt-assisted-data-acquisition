using Models.PLA;
using Models.PLA.LinearSegments;

namespace BusinessLogic.Algorithms.Utilities
{
    /// <summary>
    /// Contains utilities for PLA-based lossy compression as well as shared functionality of the Sim-, Mix-, and Custom-Piece
    /// algorithms.
    /// </summary>
    public static class PlaUtils
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

            return (maximumValue - minimumValue) * epsilonPercentage / 100;
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
        /// Reconstructs time series points from the provided segments.
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="lastPointTimestamp"></param>
        /// <returns></returns>
        public static List<Point> GetReconstructedTimeSeriesFromSegments(List<Segment> segments, long lastPointTimestamp)
        {
            var reconstructedTimeSeries = new List<Point>();

            // Sort the segments by their starting timestamp.
            segments.Sort(CompareSegmentsByStartTimestamp);

            // Add the first point to simplify later segment iteration.
            reconstructedTimeSeries.Add(new Point
            {
                SimpleTimestamp = segments[0].StartTimestamp,
                Value = segments[0].QuantizedValue
            });

            for (var i = 0; i < segments.Count; i++)
            {
                var startTimestamp = segments[i].StartTimestamp;
                long endTimestamp;

                // Check to assign appropriate end timestamp values.
                if (i < segments.Count - 1)
                    // In case the segment isn't last, it's end timestamp will be the next segment's start timestamp.
                    endTimestamp = segments[i + 1].StartTimestamp;
                else
                    // In case of the segment being last, its end timestamp must be provided.
                    endTimestamp = lastPointTimestamp;

                // The first point is always added as the previous segment's last point. This covers the whole timeseries.
                for (var currentTimestamp = startTimestamp + 1; currentTimestamp <= endTimestamp; currentTimestamp++)
                {
                    var reconstructedValue = segments[i].UpperBoundGradient * (currentTimestamp - startTimestamp) + segments[i].QuantizedValue;

                    reconstructedTimeSeries.Add(new Point
                    {
                        SimpleTimestamp = currentTimestamp,
                        Value = reconstructedValue
                    });
                }
            }

            return reconstructedTimeSeries;
        }

        /// <summary>
        /// Returns the compression ratio for a time series compressed with Sim-Piece.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="compressedTimeSeries"></param>
        /// <returns></returns>
        public static double GetCompressionRatioForSimPiece(List<Point> timeSeries, List<GroupedLinearSegment> compressedTimeSeries)
        {
            double timeSeriesSize = GetUncompressedTimeSeriesSizeInBytes(timeSeries);
            double compressedTimeSeriesSize = GetCompressedSimPieceTimeSeriesSizeInBytes(compressedTimeSeries);

            return timeSeriesSize / compressedTimeSeriesSize;
        }

        /// <summary>
        /// Returns the size of an uncompressed time series in bytes.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <returns></returns>
        public static int GetUncompressedTimeSeriesSizeInBytes(List<Point> timeSeries)
        {
            // A point can be represented with 1 byte for the timestamp + 8 bytes for the value.
            return timeSeries.Count * (TimestampSize + QuantizedValueSize);
        }

        /// <summary>
        /// Returns the size of a Sim-Piece-compressed time series in bytes.
        /// </summary>
        /// <param name="compressedTimeSeries"></param>
        /// <returns></returns>
        public static int GetCompressedSimPieceTimeSeriesSizeInBytes(List<GroupedLinearSegment> compressedTimeSeries)
        {
            // A grouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for every timestamp in the
            // group.
            var compressedTimeSeriesSize = compressedTimeSeries.Count * (QuantizedValueSize + GradientValueSize);

            foreach (var linearSegment in compressedTimeSeries)
                compressedTimeSeriesSize += linearSegment.Timestamps.Count;

            return compressedTimeSeriesSize;
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
            double timeSeriesSize = GetUncompressedTimeSeriesSizeInBytes(timeSeries);
            double compressedTimeSeriesSize = GetCompressedMixPieceTimeSeriesSizeInBytes(compressedTimeSeries);

            return timeSeriesSize / compressedTimeSeriesSize;
        }

        /// <summary>
        /// Returns the size of a Mix-Piece-compressed time series in bytes.
        /// </summary>
        /// <param name="compressedTimeSeries"></param>
        /// <returns></returns>
        public static int GetCompressedMixPieceTimeSeriesSizeInBytes(Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> compressedTimeSeries)
        {
            // A grouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for every timestamp in the
            // group.
            var compressedGroupedLinearSegmentsSize = compressedTimeSeries.Item1.Count * (QuantizedValueSize + GradientValueSize);

            // A half-grouped linear segment can be represented with 8 bytes for the gradient + 8 bytes for every quantized value in the group + 1 byte for every
            // timestamp in the group.
            var compressedHalfGroupedLinearSegmentsSize = compressedTimeSeries.Item2.Count * GradientValueSize;

            foreach (var halfGroupedLinearSegment in compressedTimeSeries.Item2)
                compressedHalfGroupedLinearSegmentsSize += halfGroupedLinearSegment.QuantizedValueTimestampPairs.Count * (QuantizedValueSize + TimestampSize);

            // An ungrouped linear segment can be represented with 8 bytes for the quantized value + 8 bytes for the gradient + 1 byte for the timestamp.
            var compressedUngroupedLinearSegmentsSize = compressedTimeSeries.Item3.Count * (QuantizedValueSize + GradientValueSize + TimestampSize);

            return compressedGroupedLinearSegmentsSize + compressedHalfGroupedLinearSegmentsSize + compressedUngroupedLinearSegmentsSize;
        }
    }
}
