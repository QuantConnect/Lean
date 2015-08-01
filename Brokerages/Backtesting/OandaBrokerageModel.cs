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
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Brokerages.Backtesting
{
    /// <summary>
    ///     Oanda Brokerage Model Implemenatation for Back Testing.
    /// </summary>
    public class OandaBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        ///     Returns true if the brokerage could accept this order. This takes into account
        ///     order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        ///     For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            if (order.DurationValue > DateTime.Now.AddMonths(3))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "Oanda does not support order expiration dates more than 3 months in the future."
                  );
                return false;
            }
            return true;
        }
        
        /// <summary>
        ///     Gets a new transaction model the represents this brokerage's fee structure and fill behavior
        /// </summary>
        /// <param name="security">The security to get a transaction model for</param>
        /// <returns>The transaction model for this brokerage</returns>
        public override ISecurityTransactionModel GetTransactionModel(Security security)
        {
            //TODO figure out how to incorporate the transaction fee model for Oanda
            // everything return a zero fee model
            return new ConstantFeeTransactionModel(0m);
        }
    }
}