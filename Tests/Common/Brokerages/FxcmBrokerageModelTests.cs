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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class FxcmBrokerageModelTests
    {
        private SymbolPropertiesDatabase _symbolPropertiesDatabase;

        [TestFixtureSetUp]
        public void Setup()
        {
            _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
        }

        [TestCaseSource(nameof(GetOrderTestData))]
        public void ValidatesOrders(OrderType orderType, Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, bool isValid)
        {
            var security = CreateSecurity(symbol);
            security.SetMarketPrice(new Tick { Value = symbol == Symbols.EURUSD ? 1m : 10000m });

            var request = new SubmitOrderRequest(orderType, symbol.SecurityType, symbol, quantity, stopPrice, limitPrice, DateTime.UtcNow, "");
            var order = Order.CreateOrder(request);

            var model = new FxcmBrokerageModel();

            BrokerageMessageEvent messageEvent;
            Assert.AreEqual(isValid, model.CanSubmitOrder(security, order, out messageEvent));
        }

        private Security CreateSecurity(Symbol symbol)
        {
            var quoteCurrency = symbol.Value.Substring(symbol.Value.Length - 3);
            var properties = _symbolPropertiesDatabase.GetSymbolProperties(
                symbol.ID.Market,
                symbol,
                symbol.SecurityType,
                quoteCurrency);

            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    symbol,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(symbol.SecurityType == SecurityType.Equity ? properties.QuoteCurrency : quoteCurrency, 0, 1m),
                properties,
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
        }

        public TestCaseData[] GetOrderTestData()
        {
            return new[]
            {
                // invalid security type
                new TestCaseData(OrderType.Market, Symbols.SPY, 1m, 0m, 0m, false),
                new TestCaseData(OrderType.Market, Symbols.BTCUSD, 1m, 0m, 0m, false),

                // invalid order type
                new TestCaseData(OrderType.MarketOnOpen, Symbols.EURUSD, 1m, 0m, 0m, false),
                new TestCaseData(OrderType.MarketOnClose, Symbols.EURUSD, 1m, 0m, 0m, false),
                new TestCaseData(OrderType.StopLimit, Symbols.EURUSD, 1m, 0m, 0m, false),

                // invalid lot size
                new TestCaseData(OrderType.Market, Symbols.EURUSD, 1m, 0m, 0m, false),
                new TestCaseData(OrderType.Market, Symbols.DE30EUR, 0.5m, 0m, 0m, false),

                // valid lot size
                new TestCaseData(OrderType.Market, Symbols.EURUSD, 1000m, 0m, 0m, true),
                new TestCaseData(OrderType.Market, Symbols.DE30EUR, 1m, 0m, 0m, true),

                // invalid limit buy price
                new TestCaseData(OrderType.Limit, Symbols.EURUSD, 1000m, 0m, 1.0001m, false),
                new TestCaseData(OrderType.Limit, Symbols.EURUSD, 1000m, 0m, 0.4999m, false),
                new TestCaseData(OrderType.Limit, Symbols.DE30EUR, 1m, 0m, 10000.1m, false),
                new TestCaseData(OrderType.Limit, Symbols.DE30EUR, 1m, 0m, 4999m, false),

                // valid limit buy price
                new TestCaseData(OrderType.Limit, Symbols.EURUSD, 1000m, 0m, 1m, true),
                new TestCaseData(OrderType.Limit, Symbols.EURUSD, 1000m, 0m, 0.5m, true),
                new TestCaseData(OrderType.Limit, Symbols.DE30EUR, 1m, 0m, 10000m, true),
                new TestCaseData(OrderType.Limit, Symbols.DE30EUR, 1m, 0m, 5000m, true),

                // invalid limit sell price
                new TestCaseData(OrderType.Limit, Symbols.EURUSD, -1000m, 0m, 0.9999m, false),
                new TestCaseData(OrderType.Limit, Symbols.EURUSD, -1000m, 0m, 1.5001m, false),
                new TestCaseData(OrderType.Limit, Symbols.DE30EUR, -1m, 0m, 9999.9m, false),
                new TestCaseData(OrderType.Limit, Symbols.DE30EUR, -1m, 0m, 15000.1m, false),

                // valid limit sell price
                new TestCaseData(OrderType.Limit, Symbols.EURUSD, -1000m, 0m, 1m, true),
                new TestCaseData(OrderType.Limit, Symbols.EURUSD, -1000m, 0m, 1.5m, true),
                new TestCaseData(OrderType.Limit, Symbols.DE30EUR, -1m, 0m, 10000m, true),
                new TestCaseData(OrderType.Limit, Symbols.DE30EUR, -1m, 0m, 15000m, true),

                // invalid stop buy price
                new TestCaseData(OrderType.StopMarket, Symbols.EURUSD, 1000m, 0.9999m, 0m, false),
                new TestCaseData(OrderType.StopMarket, Symbols.EURUSD, 1000m, 1.5001m, 0m, false),
                new TestCaseData(OrderType.StopMarket, Symbols.DE30EUR, 1m, 9999.9m, 0m, false),
                new TestCaseData(OrderType.StopMarket, Symbols.DE30EUR, 1m, 15000.1m, 0m, false),

                // valid stop buy price
                new TestCaseData(OrderType.StopMarket, Symbols.EURUSD, 1000m, 1m, 0m, true),
                new TestCaseData(OrderType.StopMarket, Symbols.EURUSD, 1000m, 1.5m, 0m, true),
                new TestCaseData(OrderType.StopMarket, Symbols.DE30EUR, 1m, 10000m, 0m, true),
                new TestCaseData(OrderType.StopMarket, Symbols.DE30EUR, 1m, 15000m, 0m, true),

                // invalid stop sell price
                new TestCaseData(OrderType.StopMarket, Symbols.EURUSD, -1000m, 1.0001m, 0m, false),
                new TestCaseData(OrderType.StopMarket, Symbols.EURUSD, -1000m, 0.4999m, 0m, false),
                new TestCaseData(OrderType.StopMarket, Symbols.DE30EUR, -1m, 10000.1m, 0m, false),
                new TestCaseData(OrderType.StopMarket, Symbols.DE30EUR, -1m, 4999m, 0m, false),

                // valid stop sell price
                new TestCaseData(OrderType.StopMarket, Symbols.EURUSD, -1000m, 1m, 0m, true),
                new TestCaseData(OrderType.StopMarket, Symbols.EURUSD, -1000m, 0.5m, 0m, true),
                new TestCaseData(OrderType.StopMarket, Symbols.DE30EUR, -1m, 10000m, 0m, true),
                new TestCaseData(OrderType.StopMarket, Symbols.DE30EUR, -1m, 5000m, 0m, true)
            };
        }
    }
}
