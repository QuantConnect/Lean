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
    /// Base class for any slippage model. Returns no slippage by default
    /// </summary>
    /// <remarks>Please use <see cref="SlippageModel"/> as the base class for
    /// any implementations of <see cref="ISlippageModel"/>. Python algorithms
    /// must derive from this class (or implement a plain class with a
    /// get_slippage_approximation method) instead of the <see cref="ISlippageModel"/>
    /// interface, which cannot be used as a Python base class</remarks>
    public class SlippageModel : ISlippageModel
    {
        /// <summary>
        /// Slippage Model. Return a decimal cash slippage approximation on the order.
        /// </summary>
        /// <param name="asset">The security being traded</param>
        /// <param name="order">The order being filled</param>
        /// <returns>The slippage of the order in units of the account currency</returns>
        public virtual decimal GetSlippageApproximation(Security asset, Order order)
        {
            return 0;
        }
    }
}
