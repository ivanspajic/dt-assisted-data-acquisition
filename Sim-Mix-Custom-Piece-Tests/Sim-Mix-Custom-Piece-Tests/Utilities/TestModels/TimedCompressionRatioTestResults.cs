using CsvHelper.Configuration.Attributes;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.TestModels
{
    public class TimedCompressionRatioTestResults : CompressionRatioTestResults
    {
        [Index(4)]
        public string Compressor { get; set; }

        [Index(5)]
        public long ElapsedMilliseconds { get; set; }
    }
}
