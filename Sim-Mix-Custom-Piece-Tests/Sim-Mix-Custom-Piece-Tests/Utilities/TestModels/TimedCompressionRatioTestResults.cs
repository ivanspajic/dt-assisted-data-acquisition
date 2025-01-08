using CsvHelper.Configuration.Attributes;

namespace Tests.Utilities.TestModels
{
    /// <summary>
    /// Used for writing experiment results relevant to table 1 to CSV files.
    /// </summary>
    public class TimedCompressionRatioTestResults : CompressionRatioTestResults
    {
        [Index(4)]
        public string Compressor { get; set; }

        [Index(5)]
        public long ElapsedMilliseconds { get; set; }
    }
}
