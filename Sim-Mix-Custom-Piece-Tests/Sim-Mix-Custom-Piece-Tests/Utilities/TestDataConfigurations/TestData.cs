using System.Collections;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.TestDataConfigurations
{
    internal class TestData : IEnumerable<object[]>
    {
        public const string BaseDataFilepath = @"C:\dev\low-bandwidth-dt\data";
        public const double ZetaPercentage = 2;
        public const bool CompressForHighestAccuracy = true;
        public static readonly string DataSetPath = Path.Combine("data-sets", "austevoll-data");
        public static readonly TimeSpan SamplingInterval = TimeSpan.FromMinutes(30);

        public static readonly string[] DataSets =
        {
            Path.Combine(DataSetPath, "Turbidity#16340 - Analog Sensors #0.csv"),
            Path.Combine(DataSetPath, "Pressure - Pressure Sensor #1955.csv"),
            Path.Combine(DataSetPath, "Salinity - Conductivity Sensor #41.csv"),
            Path.Combine(DataSetPath, "AirSaturation - Oxygen Optode #754.csv"),
            Path.Combine(DataSetPath, "Chlorophyll#2103755 - Analog Sensors #0.csv"),
            Path.Combine(DataSetPath, "Density - Conductivity Sensor #41.csv"),
            Path.Combine(DataSetPath, "Temperature - Temperature Sensor #1063.csv")
        };

        public static readonly int[] BucketSizes =
        {
            5, 7, 10, 13
        };

        public static readonly double[] EpsilonPercentages =
        {
            0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5
        };

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var dataSet in DataSets)
            {
                foreach (var bucketSize in BucketSizes)
                {
                    foreach (var epsilonPercentage in EpsilonPercentages)
                    {
                        var filepath = Path.Combine(BaseDataFilepath, dataSet);

                        yield return new object[] { filepath, bucketSize, epsilonPercentage };
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // TODO: create enumerator classes for different combinations of data sets above for more modular testing.
    }
}
