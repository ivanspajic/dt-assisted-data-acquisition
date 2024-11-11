using SimMixCustomPiece.Models.LinearSegments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.Systems
{
    internal static class SeaBorneDevice
    {
        public static double GetPercentageDeviationFromActual(Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> actualCompressedTimeSeries,
            Tuple<List<GroupedLinearSegment>, List<HalfGroupedLinearSegment>, List<UngroupedLinearSegment>> predictedCompressedTimeSeries)
        {
            return 0;
        }
    }
}
