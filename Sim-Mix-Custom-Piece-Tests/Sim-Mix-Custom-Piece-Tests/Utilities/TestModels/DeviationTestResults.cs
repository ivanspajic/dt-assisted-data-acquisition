using CsvHelper.Configuration.Attributes;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.TestModels
{
    internal class DeviationTestResults
    {
        [Index(0)]
        public long Timestamp { get; set; }

        [Index(1)]
        public double OriginalValue { get; set; }

        [Index(2)]
        public double ReconstructedValue { get; set; }

        [Index(3)]
        public double UpperBound { get; set; }

        [Index(4)]
        public double LowerBound { get; set; }
    }
}
