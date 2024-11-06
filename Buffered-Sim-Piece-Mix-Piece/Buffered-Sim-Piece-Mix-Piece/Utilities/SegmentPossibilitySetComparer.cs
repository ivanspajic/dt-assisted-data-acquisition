using Buffered_Sim_Piece_Mix_Piece.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buffered_Sim_Piece_Mix_Piece.Utilities
{
    internal class SegmentPossibilitySetComparer : EqualityComparer<Tuple<long, long, Segment>>
    {
        public override bool Equals(Tuple<long, long, Segment>? x, Tuple<long, long, Segment>? y)
        {
            return x.Item1 == y.Item1 && x.Item2 == y.Item2;
        }

        public override int GetHashCode([DisallowNull] Tuple<long, long, Segment> obj)
        {
            return (obj.Item1.ToString() + obj.Item2.ToString()).GetHashCode();
        }
    }
}
