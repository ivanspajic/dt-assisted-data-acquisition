using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.TestModels
{
    public class Table1TestResults : CompressionRatioTestResults
    {
        [Index(4)]
        public string Compressor { get; set; }

        [Index(5)]
        public long ElapsedMilliseconds { get; set; }
    }
}
