using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.TestModels
{
    internal class EnergySavingsTestResults
    {
        [Index(0)]
        public string DataSet { get; set; }

        [Index(1)]
        public int BucketSize { get; set; }

        [Index(2)]
        public double EpsilonPercentage { get; set; }

        [Index(3)]
        public double ZetaPercentage { get; set; }

        [Index(4)]
        public double PercentageEnergySaved { get; set; }
    }
}
