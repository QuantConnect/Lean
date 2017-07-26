using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders;

namespace QuantConnect
{
    public class Result
    {
        /// <summary>
        /// Charts updates for the live algorithm since the last result packet
        /// </summary>
        public IDictionary<string, Chart> Charts = new Dictionary<string, Chart>();

        /// <summary>
        /// Order updates since the last result packet
        /// </summary>
        public IDictionary<int, Order> Orders = new Dictionary<int, Order>();

        /// <summary>
        /// Trade profit and loss information since the last algorithm result packet
        /// </summary>
        public IDictionary<DateTime, decimal> ProfitLoss = new Dictionary<DateTime, decimal>();

        /// <summary>
        /// Statistics information sent during the algorithm operations.
        /// </summary>
        /// <remarks>Intended for update mode -- send updates to the existing statistics in the result GUI. If statistic key does not exist in GUI, create it</remarks>
        public IDictionary<string, string> Statistics = new Dictionary<string, string>();

        /// <summary>
        /// Runtime banner/updating statistics in the title banner of the live algorithm GUI.
        /// </summary>
        public IDictionary<string, string> RuntimeStatistics = new Dictionary<string, string>();
    }
}
