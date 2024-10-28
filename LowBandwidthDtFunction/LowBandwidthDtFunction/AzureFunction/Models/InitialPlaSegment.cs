using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowBandwidthDtFunction.AzureFunction.Models
{
    /// <summary>
    /// Represents a <see cref="PlaSegment"/> with additional data used for processing subsequent
    /// <see cref="PlaSegment"/>s in a group.
    /// </summary>
    internal class InitialPlaSegment : PlaSegment
    {
        // should contain:

        // a data point value
        // a timestamp value
    }
}
