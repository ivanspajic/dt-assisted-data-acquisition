using System.Collections;

namespace Sim_Mix_Custom_Piece_Tests.Utilities.TestDataConfigurations
{
    internal class TestData : IEnumerable<object[]>
    {
        public const double ZetaPercentage = 1;
        public static readonly string BaseDataFilepath = Path.Combine("C:", "dev", "low-bandwidth-dt", "data");
        public static readonly string DataSetPath = Path.Combine("data-sets", "austevoll-data");
        public const string TestResultsPath = "test-results";
        public static readonly TimeSpan SamplingInterval = TimeSpan.FromMinutes(30);

        // Filenames of data sets.
        public static readonly string[] DataSets =
        {
            "Turbidity#16340 - Analog Sensors #0.csv",
            //"Pressure - Pressure Sensor #1955.csv", // Longer data set, could make tests considerably slower.
            "Salinity - Conductivity Sensor #41.csv",
            "AirSaturation - Oxygen Optode #754.csv",
            "Chlorophyll#2103755 - Analog Sensors #0.csv",
            "Density - Conductivity Sensor #41.csv",
            "Temperature - Temperature Sensor #1063.csv"
        };

        // A list of bucket sizes.
        public static readonly int[] BucketSizes =
        {
            7, 10, 13, 15
        };

        // A list of epsilon percentages.
        public static readonly double[] EpsilonPercentages =
        {
            0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5
        };

        // Used for test parameter enumeration.
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var dataSet in DataSets)
            {
                foreach (var bucketSize in BucketSizes)
                {
                    foreach (var epsilonPercentage in EpsilonPercentages)
                    {
                        var filepath = Path.Combine(BaseDataFilepath, DataSetPath, dataSet);

                        yield return new object[] { filepath, bucketSize, epsilonPercentage };
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // TODO: create enumerator classes for different combinations of data sets above for more modular testing.
    }
}
