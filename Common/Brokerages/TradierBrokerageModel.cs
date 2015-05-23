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
using System.Runtime.Remoting.Messaging;
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
        public ISecurityTransactionModel GetTransactionModel(string symbol, SecurityType securityType)
        {
            if (securityType == SecurityType.Equity)
            {
                // tradier does 1 dollar trades for QC!!
                return new ConstantFeeTransactionModel(1m);
            }

            // since tradier only processes equities (and options but it's not supported), we'll just make
            // everything return a zero fee model
            return new ConstantFeeTransactionModel(0m);
        }

        public bool CanSubmitOrder(DateTime time, Order order, out BrokerageMessageEvent message)
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

            if (!CanExecuteOrder(order.Time, order))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ExtendedMarket",
                    "Tradier does not support extended market hours trading.  Your order will be processed at market open."
                    );
            }

            // tradier order limits
            return true;
        }

        public bool CanExecuteOrder(DateTime time, Order order)
        {
            // tradier doesn't support after hours trading
            var timeOfDay = time.TimeOfDay;
            if (timeOfDay < EquityExchange.EquityMarketOpen || timeOfDay > EquityExchange.EquityMarketClose)
            {
                return false;
            }
            return true;
        }
    }
}
