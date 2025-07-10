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

using Moq;
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
        private readonly InteractiveBrokersFixModel _interactiveBrokersFixModel = new();

        [TestCase(OrderType.ComboLimit)]
        [TestCase(OrderType.ComboMarket)]
        [TestCase(OrderType.ComboLegLimit)]
        public void FopComboOrders(OrderType orderType)
        {
            var underlying = Symbol.CreateFuture("ES", Market.CME, new DateTime(2025, 12, 19));
            var symbol = Symbol.CreateOption(underlying, Market.CME, OptionStyle.American, OptionRight.Call, 6000m, new DateTime(2025, 12, 19));
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var order = Order.CreateOrder(new SubmitOrderRequest(orderType, SecurityType.FutureOption, symbol, 1, 1, 1, new DateTime(2025, 7, 10), "",
                groupOrderManager: new(2, 2)));
            var canSubmit = _interactiveBrokersFixModel.CanSubmitOrder(security, order, out var message);

            Assert.IsFalse(canSubmit, message.Message);
        }
    }
}
