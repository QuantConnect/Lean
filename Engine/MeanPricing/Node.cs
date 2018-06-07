using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.MeanPricing
{
    /// <summary>
    /// Asset like USD or ETH
    /// </summary>
    public class Asset
    {
        public Cash Cash;

        /// <summary>
        /// Asset code like ETH
        /// </summary>
        public string Code { get { return Cash.Symbol; } }

        public int Level { get; set; }

        /// <summary>
        /// All connections connected to this asset
        /// </summary>
        public List<Edge> Edges { get; set; }
        
    }
}