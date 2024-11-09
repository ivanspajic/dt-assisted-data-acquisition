namespace SimMixCustomPiece.Models
{
    /// <summary>
    /// Represents a compressed section of a time series made of multiple points.
    /// </summary>
    public class Segment
    {
        public long StartTimestamp { get; set; }

        public long EndTimestamp { get; set; }

        public double UpperBoundGradient { get; set; }

        public double LowerBoundGradient { get; set; }

        public double QuantizedValue { get; set; }

        public string Type { get; set; }
    }
}
