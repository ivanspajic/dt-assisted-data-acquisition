﻿namespace Buffered_Sim_Piece_Mix_Piece.Models.LinearSegments
{
    internal class HalfGroupedLinearSegment(List<Tuple<double, long>> quantizedValueTimestampPairs) : BaseLinearSegment
    {
        public List<Tuple<double, long>> QuantizedValueTimestampPairs { get; private set; } = quantizedValueTimestampPairs;
    }
}
