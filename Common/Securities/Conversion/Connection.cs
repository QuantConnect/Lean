using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Conversion
{
    /// <summary>
    /// The connection represents a pair, but also holds other information
    /// Should be dynamically updated by AlgorithmManager (get Ticker information from exchanges)
    /// </summary>
    public class Connection
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
        public decimal BaseVolume { get; private set; }

        /// <summary>
        /// 24h Volume expressed in Quote asset
        /// </summary>
        public decimal QuoteVolume { get; private set; }

        //public decimal Brokerage (or some other ticker source 

        public bool Contains(Asset A)
        {
            return Base == A || Quote == A;
        }

        // should rate be inverted, 1/rate
        public bool Invert(Asset leftSideAsset)
        {
            return Quote == leftSideAsset;
        }
    }

}
