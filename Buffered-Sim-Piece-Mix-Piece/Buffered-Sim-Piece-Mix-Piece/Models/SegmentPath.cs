using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buffered_Sim_Piece_Mix_Piece.Models
{
    internal class SegmentPath
    {
        public Segment Segment { get; set; }

        public HashSet<SegmentPath> PossiblePaths { get; set; }
    }
}
