using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buffered_Sim_Piece_Mix_Piece.Models
{
    internal class Interval
    {
        public long Timestamp { get; set; }

        public double UpperBoundGradient { get; set; }

        public double LowerBoundGradient { get; set; }
    }
}
