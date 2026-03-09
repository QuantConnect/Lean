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
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class InteractiveBrokersFixModelTests
    {
        [TestCase(OrderType.ComboLimit, SecurityType.FutureOption, SecurityType.Future, false)]
        [TestCase(OrderType.ComboMarket, SecurityType.FutureOption, SecurityType.Future, false)]
        [TestCase(OrderType.ComboLimit, SecurityType.Future, SecurityType.Future, true)]
        [TestCase(OrderType.ComboLimit, SecurityType.FutureOption, SecurityType.FutureOption, true)]
        [TestCase(OrderType.ComboMarket, SecurityType.Future, SecurityType.Future, true)]
        [TestCase(OrderType.ComboMarket, SecurityType.FutureOption, SecurityType.FutureOption, true)]
        [TestCase(OrderType.ComboLegLimit, SecurityType.FutureOption, SecurityType.Future, false)]
        [TestCase(OrderType.ComboLegLimit, SecurityType.Future, SecurityType.Future, false)]
        [TestCase(OrderType.ComboLegLimit, SecurityType.FutureOption, SecurityType.FutureOption, false)]
        public void ComboOrderValidatesSecurityTypes(OrderType orderType, SecurityType securityType1, SecurityType securityType2, bool expected)
        {
            var model = new InteractiveBrokersFixModel();
            var groupManager = new GroupOrderManager(1, 2, 2);

            var leg1 = CreateSecurity(securityType1, 0);
            var leg2 = CreateSecurity(securityType2, 1);

            var order1 = new SubmitOrderRequest(orderType, securityType1, leg1.Symbol, 1, 1, 1, new DateTime(2025, 7, 10), "", groupOrderManager: groupManager);
            order1.SetOrderId(1);
            var leg1Order = Order.CreateOrder(order1);

            var order2 = new SubmitOrderRequest(orderType, securityType2, leg2.Symbol, -1, 1, 1, new DateTime(2025, 7, 10), "", groupOrderManager: groupManager);
            order2.SetOrderId(2);
            var leg2Order = Order.CreateOrder(order2);

            var canSubmit = model.CanSubmitOrder(leg1, leg1Order, out _) && model.CanSubmitOrder(leg2, leg2Order, out _);
            Assert.AreEqual(expected, canSubmit);
        }

        private static Security CreateSecurity(SecurityType securityType, int type)
        {
            var futureSymbol = Symbol.CreateFuture("ES", Market.CME, new DateTime(2025, 12, 19));
            var symbol = securityType == SecurityType.FutureOption
                ? Symbol.CreateOption(futureSymbol, Market.CME, OptionStyle.American,
                    type == 0 ? OptionRight.Call : OptionRight.Put,
                    type == 0 ? 6000m : 5900m, new DateTime(2025, 12, 19))
                : futureSymbol;

            return new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }
    }
}
