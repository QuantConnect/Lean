using System;
using System.Collections.Generic;
using System.Collections;
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

        public List<Edge> Connections;

        public Graph()
        {
            Assets = new Dictionary<string, Node>();
            Connections = new List<Edge>();
        }

}
