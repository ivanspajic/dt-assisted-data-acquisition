using System.Collections;

namespace Sim_Mix_Custom_Piece_Tests.Utilities
{
    internal class TestData : IEnumerable<object[]>
    {
        public const string BaseFilepath = @"C:\dev\low-bandwidth-dt\Mix-Piece_Sim-Piece-1.0.0-RC1\src\test\resources\Austevoll Data";

        public readonly string[] DataSets =
        {
            "Turbidity#16340 - Analog Sensors #0.csv",
            "Pressure - Pressure Sensor #1955.csv",
            "Salinity - Conductivity Sensor #41.csv",
            "AirSaturation - Oxygen Optode #754.csv",
            "Chlorophyll#2103755 - Analog Sensors #0.csv",
            "Density - Conductivity Sensor #41.csv"
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
                        var filepath = Path.Combine(BaseFilepath, dataSet);

                        yield return new object[] { filepath, bucketSize, epsilonPercentage };
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
