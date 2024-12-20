﻿using CsvHelper.Configuration.Attributes;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.TestModels
{
    /// <summary>
    /// Used for writing compression ratio test results to CSV files.
    /// </summary>
    public class CompressionRatioTestResults
    {
        [Index(0)]
        public string DataSet { get; set; }

        [Index(1)]
        public int BucketSize { get; set; }

        [Index(2)]
        public double EpsilonPercentage { get; set; }

        [Index(3)]
        public double CompressionRatio { get; set; }
    }
}
