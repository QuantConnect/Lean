using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Conversion.Graph
{  
    public class Graph
    {
        /// <summary>
        /// Key is Cash Code, such as "ETH"
        /// </summary>
        public Dictionary<string, Node> Assets;

        /// <summary>
        /// Key is Pair name
        /// Problematic, because it can be ETHUSD or USDETH
        /// </summary>
        public List<Edge> Connections;

        public Graph()
        {
            Assets = new Dictionary<string, Node>();
            Connections = new List<Edge>();
        }
    }
}
