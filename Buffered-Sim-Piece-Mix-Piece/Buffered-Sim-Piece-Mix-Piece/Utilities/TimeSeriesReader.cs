using Buffered_Sim_Piece_Mix_Piece.Models;
using System.Globalization;

namespace Buffered_Sim_Piece_Mix_Piece.Utilities
{
    internal static class TimeSeriesReader
    {
        public static List<Point> ReadTimeSeriesDataFromFromFile(string filepath, int bufferWindowSize)
        {
            var timeSeries = new List<Point>();

            using (var streamReader = new StreamReader(filepath))
            {
                string line = streamReader.ReadLine()!;
                int i = 0;

                while (!string.IsNullOrEmpty(line) && i < bufferWindowSize)
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
