using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Conversion
{
    
    
    public class Graph
    {
        /// <summary>
        /// Key is Asset name, such as ETH
        /// </summary>
        public Dictionary<string, Asset> Assets;

        /// <summary>
        /// Key is Pair name
        /// </summary>
        public Dictionary<string, Connection> Connections;
    }
}
