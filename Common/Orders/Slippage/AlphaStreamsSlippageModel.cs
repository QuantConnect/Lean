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

using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Orders.Slippage
{
    /// <summary>
    /// Represents a slippage model that uses a constant percentage of slip
    /// </summary>
    public class AlphaStreamsSlippageModel : ISlippageModel
    {
        private const decimal _slippagePercent = 0.0001m;

        /// <summary>
        /// Unfortunate dictionary of ETFs and their spread on 10/10 so that we can better approximate
        /// slippage for the competition
        /// </summary>
        private readonly IDictionary<string, decimal> _spreads = new Dictionary<string, decimal>
        {
            {"PPLT",  0.13m}, {"DGAZ", 0.135m}, {"EDV", 0.085m}, {"SOXL", 0.1m}
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaStreamsSlippageModel"/> class
        /// </summary>
        public AlphaStreamsSlippageModel() { }

        /// <summary>
        /// Return a decimal cash slippage approximation on the order.
        /// </summary>
        public decimal GetSlippageApproximation(Security asset, Order order)
        {
            if (asset.Type != SecurityType.Equity)
            {
                return 0;
            }

            decimal slippageValue;

            if (!_spreads.TryGetValue(asset.Symbol.Value, out slippageValue))
            {
                return _slippagePercent * asset.GetLastData()?.Value ?? 0;
            }

            return slippageValue;
        }
    }
}