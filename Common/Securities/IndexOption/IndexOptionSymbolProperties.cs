using QuantConnect.Data;
using QuantConnect.Securities.Option;

namespace QuantConnect.Securities.IndexOption
{
    /// <summary>
    /// Index Option Symbol Properties
    /// </summary>
    public class IndexOptionSymbolProperties : OptionSymbolProperties
    {
        private BaseData _lastData;

        /// <summary>
        /// Minimum price variation, subject to variability due to contract price
        /// </summary>
        public override decimal MinimumPriceVariation => _lastData != null && _lastData.Price >= 3m ? 0.10m : 0.05m;

        /// <summary>
        /// Creates an instance of index symbol properties
        /// </summary>
        /// <param name="description">Description of the Symbol</param>
        /// <param name="quoteCurrency">Currency the price is quoted in</param>
        /// <param name="contractMultiplier">Contract multiplier of the index option</param>
        /// <param name="pipSize">Minimum price variation</param>
        /// <param name="lotSize">Minimum order lot size</param>
        public IndexOptionSymbolProperties(
            string description,
            string quoteCurrency,
            decimal contractMultiplier,
            decimal pipSize,
            decimal lotSize
            )
            : base(description, quoteCurrency, contractMultiplier, pipSize, lotSize)
        {
        }

        /// <summary>
        /// Creates instance of index symbol properties
        /// </summary>
        /// <param name="properties"></param>
        public IndexOptionSymbolProperties(SymbolProperties properties)
            : base(properties)
        {
        }

        /// <summary>
        /// Updates the last data received, required for calculating some
        /// index options contracts that have a variable step size for their premium's quotes
        /// </summary>
        /// <param name="marketData">Data to update with</param>
        internal void UpdateMarketPrice(BaseData marketData)
        {
            _lastData = marketData;
        }
    }
}
