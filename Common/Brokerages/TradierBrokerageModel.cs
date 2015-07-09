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

using System;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides tradier specific properties
    /// </summary>
    public class TradierBrokerageModel : IBrokerageModel
    {
        private static readonly EquityExchange EquityExchange = 
            new EquityExchange(SecurityExchangeHoursProvider.FromDataFolder().GetExchangeHours("usa", null, SecurityType.Equity));

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            var securityType = order.SecurityType;
            if (securityType != SecurityType.Equity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "This model only supports equities."
                    );
                
                return false;
            }

            if (!CanExecuteOrder(security, order))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ExtendedMarket",
                    "Tradier does not support extended market hours trading.  Your order will be processed at market open."
                    );
            }

            // tradier order limits
            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the 
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public bool CanExecuteOrder(Security security, Order order)
        {
            EquityExchange.SetLocalDateTimeFrontier(security.Exchange.LocalTime);

            // tradier doesn't support after hours trading
            var timeOfDay = security.LocalTime.TimeOfDay;
            if (timeOfDay < EquityExchange.MarketOpen || timeOfDay > EquityExchange.MarketClose)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets a new transaction model the represents this brokerage's fee structure and fill behavior
        /// </summary>
        /// <param name="security">The security to get a transaction model for</param>
        /// <returns>The transaction model for this brokerage</returns>
        public ISecurityTransactionModel GetTransactionModel(Security security)
        {
            if (security.Type == SecurityType.Equity)
            {
                // tradier does 1 dollar trades for QC!!
                return new ConstantFeeTransactionModel(1m);
            }

            // since tradier only processes equities (and options but it's not supported), we'll just make
            // everything return a zero fee model
            return new ConstantFeeTransactionModel(0m);
        }

    }
}
