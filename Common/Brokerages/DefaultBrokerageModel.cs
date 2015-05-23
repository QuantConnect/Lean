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
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides a default implementation of <see cref="IBrokerageModel"/> that allows all orders and uses
    /// the default transaction models
    /// </summary>
    public class DefaultBrokerageModel : IBrokerageModel
    {
        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, order size limits, and time of day.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message"></param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public virtual bool CanSubmitOrder(DateTime time, Order order, out BrokerageMessageEvent message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the 
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public virtual bool CanExecuteOrder(DateTime time, Order order)
        {
            return true;
        }

        /// <summary>
        /// Gets a transaction model the represents this brokerage's fee structure and possibly it's fill behavior
        /// </summary>
        /// <param name="symbol">The symbol whose transaction model we seek</param>
        /// <param name="securityType">The security type whose transaction model we seek</param>
        /// <returns>The transaction model for this brokerage</returns>
        public virtual ISecurityTransactionModel GetTransactionModel(string symbol, SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Forex: 
                    return new ForexTransactionModel();
                
                case SecurityType.Equity: 
                    return new EquityTransactionModel();

                case SecurityType.Base:
                case SecurityType.Option:
                case SecurityType.Commodity:
                case SecurityType.Future:
                    return new SecurityTransactionModel();

                default:
                    throw new ArgumentOutOfRangeException("securityType", securityType, null);
            }
        }
    }
}