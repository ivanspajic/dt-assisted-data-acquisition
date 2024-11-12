namespace SimMixCustomPiece.Models
{
    /// <summary>
    /// Represents a data point in a time series.
    /// </summary>
    public class Point
    {
        public long SimpleTimestamp { get; set; }

        public DateTime DateTime { get; set; }

        public double Value { get; set; }
    }
}
