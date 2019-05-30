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
*/

using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers
{
    /// <summary>
    /// Provides an implementation of <see cref="ISecurityProvider"/> that uses the <see cref="SecurityManager"/>
    /// to get the price for the specified symbols
    /// </summary>
    public class AlgorithmSecurityValuesProvider : ISecurityValuesProvider
    {
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmSecurityValuesProvider"/> class
        /// </summary>
        /// <param name="algorithm">The wrapped algorithm instance</param>
        public AlgorithmSecurityValuesProvider(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Gets the current values for the specified symbol (price/volatility)
        /// </summary>
        /// <param name="symbol">The symbol to get price/volatility for</param>
        /// <returns>The insight target values for the specified symbol</returns>
        public SecurityValues GetValues(Symbol symbol)
        {
            var security = _algorithm.Securities[symbol];
            var volume = security.Cache.GetData<TradeBar>()?.Volume ?? 0;
            return new SecurityValues(symbol, _algorithm.UtcTime, security.Exchange.Hours, security.Price, security.VolatilityModel.Volatility, volume, security.QuoteCurrency.ConversionRate);
        }

        /// <summary>
        /// Gets the current values for all the algorithm securities (price/volatility)
        /// </summary>
        /// <returns>The insight target values for all the algorithm securities</returns>
        public ReadOnlySecurityValuesCollection GetAllValues()
        {
            // lets be lazy creating the SecurityValues
            return new ReadOnlySecurityValuesCollection(
                symbol =>
                {
                    var security = _algorithm.Securities[symbol];
                    var volume = security.Cache.GetData<TradeBar>()?.Volume ?? 0;
                    return new SecurityValues(security.Symbol, _algorithm.UtcTime, security.Exchange.Hours, security.Price, security.VolatilityModel.Volatility, volume, security.QuoteCurrency.ConversionRate);
                });
        }
    }
}