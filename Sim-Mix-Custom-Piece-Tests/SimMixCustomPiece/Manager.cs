using DataRepository;
using Models.PLA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class Manager
    {
        private readonly IFileRepository _fileRepository;

        public Manager(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public List<Point> LoadTimeSeries(string locator)
        {
            return _fileRepository.ReadTimeSeries(locator);
        }

        public List<List<Point>> GetSampleBucketsFromTimeSeries(string locator, int bucketSize, int numberOfSamples)
        {
            return _fileRepository.ReadTimeSeriesInBuckets(locator, bucketSize, numberOfSamples);
        }
    }
}
