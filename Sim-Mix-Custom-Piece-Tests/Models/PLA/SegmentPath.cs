namespace Models.PLA
{
    /// <summary>
    /// Represents a node in a tree of possible <see cref="PLA.Segment"/> paths constructed from a time series.
    /// </summary>
    public class SegmentPath
    {
        public Segment Segment { get; set; }

        public HashSet<SegmentPath> PossiblePaths { get; set; }
    }
}
