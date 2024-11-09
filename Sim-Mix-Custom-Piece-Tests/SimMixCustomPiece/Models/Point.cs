namespace SimMixCustomPiece.Models
{
    /// <summary>
    /// Represents a data point in a time series.
    /// </summary>
    public class Point
    {
        public long Timestamp { get; set; }

        public double Value { get; set; }
    }
}
