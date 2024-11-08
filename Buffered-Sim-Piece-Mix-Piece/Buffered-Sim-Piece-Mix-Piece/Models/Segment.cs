namespace Buffered_Sim_Piece_Mix_Piece.Models
{
    internal class Segment
    {
        public long StartTimestamp { get; set; }

        public long EndTimestamp { get; set; }

        public double UpperBoundGradient { get; set; }

        public double LowerBoundGradient { get; set; }

        public double QuantizedValue { get; set; }

        public string Type { get; set; }
    }
}
