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
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Provides Bitfinex specific properties
    /// </summary>
    public class BitfinexBrokerageModel : DefaultBrokerageModel
    {

        string _wallet;
        const string exchange = "exchange";

        /// <summary>
        /// Initializes a new instance of the <see cref="BitfinexBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to 
        /// <see cref="QuantConnect.AccountType.Margin"/></param>
        public BitfinexBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
            _wallet = Config.Get("bitfinex-wallet");

            if (_wallet == "exchange" && accountType == AccountType.Margin)
            {
                throw new ArgumentException("Exchange wallet does not allow margin trades");
            }

        }

        /// <summary>
        /// Bitfinex global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            return this.AccountType == AccountType.Margin ? 3.3m : 0;
        }

        /// <summary>
        /// Provides Bitfinex fee model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new BitfinexFeeModel();
        }

        /// <summary>
        /// Provides Bitfinex slippage model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override ISlippageModel GetSlippageModel(Security security)
        {
            return new BitfinexSlippageModel();
        }

        //todo: support other currencies
        //todo: Checks for decimals are superfluous until quantity is changed from int to decimal
        /// <summary>
        /// Validates pending orders based on currency pair, order amount, security type
        /// </summary>
        /// <param name="security"></param>
        /// <param name="order"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;
            const string BTCUSD = "BTCUSD";
            var securityType = order.SecurityType;
            if (securityType != SecurityType.Forex || security.Symbol.Value != BTCUSD || NumberOfDecimals(order.Quantity) > 2)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "This model only supports BTCUSD orders on a scale of 0.01 or more.");

                return false;
            }


            return true;
        }

        private int NumberOfDecimals(decimal quantity)
        {
            return BitConverter.GetBytes(decimal.GetBits(quantity)[3])[2];
        }

    }
}