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

namespace QuantConnect.Orders.Slippage
{
    /// <summary>
    /// Represents a slippage model which will use the spread if available (tick data)
    /// otherwise it will com
    /// </summary>
    public class ConstantOrSpreadSlippageModel : ISlippageModel
    {
        private readonly ISlippageModel _defaultSlippageModel = new DefaultSlippageModel();
        private readonly ISlippageModel _constantSlippageModel = new ConstantSlippageModel(0.0001m);

        /// <summary>
        /// Initializes a new default insance of the <see cref="ConstantOrSpreadSlippageModel"/>
        /// using a slippage percent of 0.01%
        /// </summary>
        public ConstantOrSpreadSlippageModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantOrSpreadSlippageModel"/> class
        /// </summary>
        /// <param name="slippagePercent">The percent slippage (0 to 1) used for non-tick data</param>
        public ConstantOrSpreadSlippageModel(decimal slippagePercent)
        {
            _constantSlippageModel = new ConstantSlippageModel(slippagePercent);
        }

        /// <summary>
        /// Slippage Model. Return a decimal cash slippage approximation on the order.
        /// </summary>
        public decimal GetSlippageApproximation(Security asset, Order order)
        {
            if (asset.Resolution == Resolution.Tick) return _defaultSlippageModel.GetSlippageApproximation(asset, order);
            return _constantSlippageModel.GetSlippageApproximation(asset, order);
        }
    }
}