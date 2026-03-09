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


using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models RBI order fees
    /// </summary>
    public class RBIFeeModel : FeeModel
    {
        private readonly decimal _feesPerShare;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="feesPerShare">The fees per share to apply</param>
        /// <remarks>Default value is $0.005 per share</remarks>
        public RBIFeeModel(decimal? feesPerShare = null)
        {
            _feesPerShare = feesPerShare ?? 0.005m;
        }

        /// <summary>
        /// Get the fee for this order in quote currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in quote currency</returns>
        public OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            return new OrderFee(new CashAmount(_feesPerShare * parameters.Order.AbsoluteQuantity, Currencies.USD));
        }
    }
}
