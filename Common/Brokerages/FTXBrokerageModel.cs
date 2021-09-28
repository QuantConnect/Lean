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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// FTX Brokerage model
    /// </summary>
    public class FTXBrokerageModel : DefaultBrokerageModel
    {
        private const decimal _defaultLeverage = 3m;

        /// <summary>
        /// Constructor for Kraken brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public FTXBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets();


        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// For margin trading, max leverage = 5
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            return AccountType == AccountType.Margin
                ? new SecurityMarginModel(_defaultLeverage)
                : new CashBuyingPowerModel();
        }

        private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets()
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Crypto] = Market.FTX;
            return map.ToReadOnlyDictionary();
        }

        /// <summary>
        /// Please note that the order's queue priority will be reset, and the order ID of the modified order will be different from that of the original order.
        /// Also note: this is implemented as cancelling and replacing your order.
        /// There's a chance that the order meant to be cancelled gets filled and its replacement still gets placed.
        /// </summary>
        /// <param name="security"></param>
        /// <param name="order"></param>
        /// <param name="request"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = 
                new BrokerageMessageEvent(
                    BrokerageMessageType.Warning, 
                    0, 
                    "You must cancel and re-create instead.");
            return false;
        }
    }
}
