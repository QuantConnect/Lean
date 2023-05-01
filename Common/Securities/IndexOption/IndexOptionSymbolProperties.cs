/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

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
        public static decimal MinimumPriceVariationForPrice(decimal? referencePrice) => referencePrice.HasValue && referencePrice >= 3m ? 0.10m : 0.05m;

        /// <summary>
        /// Minimum price variation, subject to variability due to contract price
        /// </summary>
        public override decimal MinimumPriceVariation => MinimumPriceVariationForPrice(_lastData?.Price);

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
