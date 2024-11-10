using System.Collections;

namespace Sim_Mix_Custom_Piece_Tests.Utilities
{
    internal class TestData : IEnumerable<object[]>
    {
        public const string BaseDataFilepath = @"C:\dev\low-bandwidth-dt\data";

        public readonly string[] DataSets =
        {
            @"data-sets\austevoll-data\Turbidity#16340 - Analog Sensors #0.csv",
            @"data-sets\austevoll-data\Pressure - Pressure Sensor #1955.csv",
            @"data-sets\austevoll-data\Salinity - Conductivity Sensor #41.csv",
            @"data-sets\austevoll-data\AirSaturation - Oxygen Optode #754.csv",
            @"data-sets\austevoll-data\Chlorophyll#2103755 - Analog Sensors #0.csv",
            @"data-sets\austevoll-data\Density - Conductivity Sensor #41.csv"
        };

        public readonly int[] BucketSizes =
        {
            5, 7, 10, 13
        };

        public readonly double[] EpsilonPercentages =
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
    }
}
