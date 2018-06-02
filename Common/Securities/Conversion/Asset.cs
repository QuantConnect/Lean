using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Conversion
{
    /// <summary>
    /// Asset like USD or ETH
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// Asset code like ETH
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// All connections connected to this asset
        /// </summary>
        public List<Connection> Connections { get; set; }
    }
}