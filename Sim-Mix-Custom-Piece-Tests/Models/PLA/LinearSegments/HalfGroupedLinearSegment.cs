namespace Models.PLA.LinearSegments
{
    /// <summary>
    /// Represents the final output of a compressed time series where the compressed segments only share a gradient.
    /// </summary>
    /// <param name="quantizedValueTimestampPairs"></param>
    public class HalfGroupedLinearSegment(List<Tuple<double, long>> quantizedValueTimestampPairs) : BaseLinearSegment
    {
        public List<Tuple<double, long>> QuantizedValueTimestampPairs { get; private set; } = quantizedValueTimestampPairs;
    }
}
