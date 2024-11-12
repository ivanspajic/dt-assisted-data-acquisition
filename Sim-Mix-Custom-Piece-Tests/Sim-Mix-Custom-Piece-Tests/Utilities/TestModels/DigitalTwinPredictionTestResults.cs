using CsvHelper.Configuration.Attributes;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.TestModels
{
    internal class DigitalTwinPredictionTestResults
    {
        [Index(0)]
        public string DataSet { get; set; }

        [Index(1)]
        public double Epsilon { get; set; }

        [Index(2)]
        public long StartTimestamp { get; set; }

        [Index(3)]
        public long EndTimestamp { get; set; }

        [Index(4)]
        public double AverageDeviationPercentage { get; set; }

        [Index(5)]
        public double ZetaPercentage { get; set; }

        [Index(6)]
        public int CompressedTimeSeriesSizeInBytes { get; set; }

        [Index(6)]
        public double BytesTransmitted { get; set; }
    }
}
