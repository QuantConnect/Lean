using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Conversion.Graph
{
    public class BrokeragePricePair
    {
        public string Market;
        public decimal Volume;
        public decimal Price;
    }

    /// <summary>
    /// The connection represents a pair, but also holds other information
    /// Should be dynamically updated by AlgorithmManager (get Ticker information from exchanges)
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// Base asset, left side of pair
        /// </summary>
        public Node Base { get; private set; }

        /// <summary>
        /// Quote asset, right side of pair
        /// </summary>
        public Node Quote { get; private set; }

        /// <summary>
        /// Pair name
        /// </summary>
        public string Symbol { get { return Base.Code + Quote.Code; } }

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
        public decimal BaseVolume { get; private set; }

        public decimal QuoteVolume { get ; private set; }

        /// <summary>
        /// Used for calculating path
        /// </summary>
        public int Value { get; set; }
        
        public void Update(string Brokerage, string Pair, Data.BaseData LastData, decimal BaseVolume24, decimal QuoteVolume24 = -1)
        {
            // Approximate quote volume if none provided
            if(QuoteVolume24 == -1)           
                QuoteVolume24 = BaseVolume24 * LastData.Price;
            
        }

        // public decimal Brokerage (or some other price-volume source)
        public bool Contains(Node A)
        {
            return Base == A || Quote == A;
        }

        // should rate be inverted, 1/rate
        public bool NormalOrientation(Node leftSideAsset)
        {
            return Base == leftSideAsset;
        }

        // should rate be inverted, 1/rate
        public bool InverseOrientation(Node leftSideAsset)
        {
            return Quote == leftSideAsset;
        }
    }

}
