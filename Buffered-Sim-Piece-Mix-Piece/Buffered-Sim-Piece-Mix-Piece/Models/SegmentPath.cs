namespace Buffered_Sim_Piece_Mix_Piece.Models
{
    internal class SegmentPath
    {
        public Segment Segment { get; set; }

        public HashSet<SegmentPath> PossiblePaths { get; set; }
    }
}
