using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;

namespace QuantConnect.Conversion.Graph
{
    /// <summary>
    /// Asset like USD or ETH
    /// </summary>
    public class Node
    {
        public Cash Cash;

        /// <summary>
        /// Asset code like ETH
        /// </summary>
        public string Code { get { return Cash.Symbol; } }

        public int Value { get; set; }

        /// <summary>
        /// All edges connected to this asset
        /// </summary>
        public List<Edge> Edges { get; set; }

   
    }
}