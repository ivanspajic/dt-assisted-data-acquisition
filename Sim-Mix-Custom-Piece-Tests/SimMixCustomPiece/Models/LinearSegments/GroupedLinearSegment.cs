namespace SimMixCustomPiece.Models.LinearSegments
{
    /// <summary>
    /// Represents the final output of a compressed time series where the compressed segments share a gradient and a quantized
    /// value.
    /// </summary>
    /// <param name="timestamps"></param>
    public class GroupedLinearSegment(List<long> timestamps) : BaseLinearSegment
    {
        public double QuantizedValue { get; set; }

        public List<long> Timestamps { get; private set; } = timestamps;
    }
}
