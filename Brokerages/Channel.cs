using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Represents a subscription channel
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The ticker symbol of the channel
        /// </summary>
        public string Symbol { get; set; }
    }
}
