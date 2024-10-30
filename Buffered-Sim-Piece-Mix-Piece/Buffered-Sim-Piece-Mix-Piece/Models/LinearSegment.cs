using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buffered_Sim_Piece_Mix_Piece.Models
{
    internal class LinearSegment(List<long> timestamps)
    {
        public double QuantizedOriginValue { get; set; }

        public double Gradient { get; set; }

        public List<long> Timestamps { get; private set; } = timestamps;
    }
}
