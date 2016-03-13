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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the model responsible for picking which orders should be executed during a margin call
    /// </summary>
    public class BitfinexMarginCallModel : MarginCallModel
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BitfinexMarginCallModel"/> class
        /// </summary>
        /// <param name="portfolio">The portfolio object to receive margin calls</param>
        public BitfinexMarginCallModel(SecurityPortfolioManager portfolio)
            : base(portfolio)
        {
        }

        /// <summary>
        /// Ignores automated margin call orders. Margin call warnings should be manually resolved.
        /// </summary>
        /// <param name="generatedMarginCallOrders">These are the margin call orders that were generated
        /// by individual security margin models.</param>
        /// <returns>Empty collection</returns>
        public virtual List<OrderTicket> ExecuteMarginCall(IEnumerable<SubmitOrderRequest> generatedMarginCallOrders)
        {
            return new List<OrderTicket>();
        }
    }
}