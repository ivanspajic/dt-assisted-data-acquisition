namespace Buffered_Sim_Piece_Mix_Piece.Models.LinearSegments
{
    internal class GroupedLinearSegment(List<long> timestamps) : BaseLinearSegment
    {
        public double QuantizedValue { get; set; }

        public List<long> Timestamps { get; private set; } = timestamps;
    }
}
