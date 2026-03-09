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

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Base class for any order fee model
    /// </summary>
    /// <remarks>Please use <see cref="FeeModel"/> as the base class for
    /// any implementations of <see cref="IFeeModel"/></remarks>
    public class FeeModel : IFeeModel
    {
        /// <summary>
        /// Gets the order fee associated with the specified order.
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in a <see cref="CashAmount"/> instance</returns>
        public virtual OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            return new OrderFee(new CashAmount(
                0,
                "USD"));
        }
    }
}