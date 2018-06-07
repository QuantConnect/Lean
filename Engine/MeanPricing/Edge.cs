using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Lean.Engine.MeanPricing
{
    /// <summary>
    /// The connection represents a pair, but also holds other information
    /// Should be dynamically updated by AlgorithmManager (get Ticker information from exchanges)
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// Base asset, left side of pair
        /// </summary>
        public Asset Base { get; private set; }

        /// <summary>
        /// Quote asset, right side of pair
        /// </summary>
        public Asset Quote { get; private set; }

        /// <summary>
        /// Pair name
        /// </summary>
        public string PairName { get { return Base.Code + Quote.Code; } }

        /// <summary>
        /// Price, value
        /// </summary>
        public decimal Rate { get; private set; }

        /// <summary>
        /// 24h Volume expressed in Base asset
        /// </summary>
        public decimal Volume { get; private set; }

        /// <summary>
        /// 24h Volume expressed in Quote asset
        /// </summary>
        public decimal QuoteVolume { get; private set; }

        /// <summary>
        /// Used for calculating price flow
        /// </summary>
        public int Level { get; set; }

        //public Dictionary<Brokerages.Brokerage>

        public void Update(string Brokerage, string Pair, decimal LastPrice, decimal Volume24)
        {
            // Check if Brokerage is online, if not, remove it from list

            // Check if Brokerage exists, if not, add it to

        }

        // public decimal Brokerage (or some other ticker source
        public bool Contains(Asset A)
        {
            return Base == A || Quote == A;
        }

        // should rate be inverted, 1/rate
        public bool NormalOrientation(Asset leftSideAsset)
        {
            return Base == leftSideAsset;
        }

        // should rate be inverted, 1/rate
        public bool InverseOrientation(Asset leftSideAsset)
        {
            return Quote == leftSideAsset;
        }
    }

}
