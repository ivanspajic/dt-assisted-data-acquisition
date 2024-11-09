namespace Buffered_Sim_Piece_Mix_Piece.Models.LinearSegments
{
    internal class BaseLinearSegment
    {
        public double UpperBoundGradient { get; set; }

        public double LowerBoundGradient { get; set; }

        public double Gradient
        {
            get
            {
                return (UpperBoundGradient + LowerBoundGradient) / 2;
            }
        }
    }
}
