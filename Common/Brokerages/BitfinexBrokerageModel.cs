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

    public class BitfinexBrokerageModel : DefaultBrokerageModel
    {

        string _wallet;
        const string exchange = "exchange";

        public BitfinexBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
            _wallet = Config.Get("bitfinex-wallet");

            if (_wallet == "exchange" && accountType == AccountType.Margin)
            {
                throw new ArgumentException("Exchange wallet does not allow margin trades");
            }

        }

        public override decimal GetLeverage(Security security)
        {
            return this.AccountType == AccountType.Margin ? 3.3m : 0;
        }

        public override IFeeModel GetFeeModel(Security security)
        {
            return new BitfinexFeeModel();
        }

        public override ISlippageModel GetSlippageModel(Security security)
        {
            return new BitfinexSlippageModel();
        }

        //todo: support other currencies
        //Checks for decimal are superfluous until quantity is changed from int to decimal
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;
            const string BTCUSD = "BTCUSD";
            var securityType = order.SecurityType;
            if (securityType != SecurityType.Forex || security.Symbol.Value != BTCUSD || NumberOfDecimals(order.Quantity) > 2)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    "This model only supports BTCUSD orders > 0.01.");

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