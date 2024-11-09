﻿namespace Buffered_Sim_Piece_Mix_Piece.Models.LinearSegments
{
    /// <summary>
    /// Represents the final output of a compressed time series where the compressed segments do not share a gradient
    /// nor a quantized value.
    /// </summary>
    internal class UngroupedLinearSegment : BaseLinearSegment
    {
        public Tuple<double, long> ValueTimestampPair { get; set; }
    }
}
