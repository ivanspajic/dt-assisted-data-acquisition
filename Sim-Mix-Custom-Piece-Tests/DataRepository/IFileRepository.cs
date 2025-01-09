using Models.PLA;

namespace DataRepository
{
    public interface IFileRepository
    {
        public List<Point> ReadTimeSeries(string filepath);

        public List<Point> ReadTimeSeries(string filepath, int startIndex);

        public List<Point> ReadTimeSeries(string filepath, int startIndex, int size);

        public List<List<Point>> ReadTimeSeriesInBuckets(string filepath, int bucketSize);

        public List<List<Point>> ReadTimeSeriesInBuckets(string filepath, int bucketSize, int bucketNumber);

        public void Write<T>(string filepath, List<T> items);
    }
}
