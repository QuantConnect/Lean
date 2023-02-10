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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Kraken Brokerage model
    /// </summary>
    public class KrakenBrokerageModel : DefaultBrokerageModel
    {
        private readonly List<string> _fiatsAvailableMargin = new() {"USD", "EUR"};
        private readonly List<string> _onlyFiatsAvailableMargin = new() {"BTC", "USDT", "USDC"};
        private readonly List<string> _ethAvailableMargin = new() {"REP", "XTZ", "ADA", "EOS", "TRX", "LINK" };

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets();

        /// <summary>
        /// Leverage map of different coins
        /// </summary>
        public IReadOnlyDictionary<string, decimal> CoinLeverage { get; } = new Dictionary<string, decimal>
        {
            {"BTC", 5}, // only with fiats
            {"ETH", 5},
            {"USDT", 2}, // only with fiats
            {"XMR", 2},
            {"REP", 2}, // eth available
            {"XRP", 3},
            {"BCH", 2},
            {"XTZ", 2}, // eth available
            {"LTC", 3},
            {"ADA", 3}, // eth available
            {"EOS", 3}, // eth available
            {"DASH", 3},
            {"TRX", 3}, // eth available
            {"LINK", 3}, // eth available
            {"USDC", 3}, // only with fiats
        };

        /// <summary>
        /// Constructor for Kraken brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public KrakenBrokerageModel(AccountType accountType = AccountType.Cash) : base(accountType)
        {

        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (!IsValidOrderSize(security, order.Quantity, out message))
            {
                return false;
            }

            message = null;
            if (security.Type != SecurityType.Crypto)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));

                return false;
            }

            if (order.Type == OrderType.MarketOnClose || order.Type == OrderType.MarketOnOpen || order.Type == OrderType.OptionExercise)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order));

                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Kraken does not support update of orders
        /// </summary>
        /// <param name="security">Security</param>
        /// <param name="order">Order that should be updated</param>
        /// <param name="request">Update request</param>
        /// <param name="message">Outgoing message</param>
        /// <returns>Always false as Kraken does not support update of orders</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, 0, Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
            return false;
        }

        /// <summary>
        /// Provides Kraken fee model
        /// </summary>
        /// <param name="security">Security</param>
        /// <returns>Kraken fee model</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new KrakenFeeModel();
        }

        /// <summary>
        /// Kraken global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            if (AccountType == AccountType.Cash)
            {
                return 1m;
            }

            // first check whether this security support margin only with fiats.
            foreach (var coin in _onlyFiatsAvailableMargin.Where(coin => security.Symbol.ID.Symbol.StartsWith(coin)).Where(coin => _fiatsAvailableMargin.Any(rightFiat => security.Symbol.Value.EndsWith(rightFiat))))
            {
                return CoinLeverage[coin];
            }

            List<string> extendedCoinArray = new() {"BTC", "ETH"};
            extendedCoinArray.AddRange(_fiatsAvailableMargin);
            // Then check whether this security support margin with ETH.
            foreach (var coin in _ethAvailableMargin.Where(coin => security.Symbol.ID.Symbol.StartsWith(coin)).Where(coin => extendedCoinArray.Any(rightFiat => security.Symbol.Value.EndsWith(rightFiat))))
            {
                return CoinLeverage[coin];
            }

            extendedCoinArray.Remove("ETH");
            // At the end check all others.
            foreach (var coin in CoinLeverage.Keys.Where(coin => security.Symbol.ID.Symbol.StartsWith(coin)).Where(coin => extendedCoinArray.Any(rightFiat => security.Symbol.Value.EndsWith(rightFiat))))
            {
                return CoinLeverage[coin];
            }

            return 1m;
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Kraken);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <summary>
        /// Get default markets and specify Kraken as crypto market
        /// </summary>
        /// <returns>default markets</returns>
        private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets()
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Crypto] = Market.Kraken;
            return map.ToReadOnlyDictionary();
        }
    }
}
