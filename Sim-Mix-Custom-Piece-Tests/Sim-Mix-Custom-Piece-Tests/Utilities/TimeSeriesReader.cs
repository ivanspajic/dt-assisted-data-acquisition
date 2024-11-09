using SimMixCustomPiece.Models;
using System.Globalization;

namespace Sim_Mix_Custom_Piece_Tests.Utilities
{
    /// <summary>
    /// Used for reading time series stored in files.
    /// </summary>
    internal static class TimeSeriesReader
    {
        /// <summary>
        /// Reads a time series from a comma-delimited CSV file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="bucketSize"></param>
        /// <returns></returns>
        public static List<Point> ReadTimeSeriesDataFromFromFile(string filepath, int bucketSize)
        {
            var timeSeries = new List<Point>();

            using (var streamReader = new StreamReader(filepath))
            {
                string line = streamReader.ReadLine()!;
                int i = 0;

                while (!string.IsNullOrEmpty(line) && i < bucketSize)
                {
                    var keyValue = line.Split(',');

                    var point = new Point
                    {
                        Timestamp = long.Parse(keyValue[0]),
                        Value = double.Parse(keyValue[1], CultureInfo.InvariantCulture)
                    };

                    timeSeries.Add(point);

                    line = streamReader.ReadLine()!;
                    i++;
                }
            }

            return timeSeries;
        }
    }
}
