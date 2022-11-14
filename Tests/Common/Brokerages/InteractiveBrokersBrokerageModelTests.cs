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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class InteractiveBrokersBrokerageModelTests
    {
        private readonly InteractiveBrokersBrokerageModel _interactiveBrokersBrokerageModel = new InteractiveBrokersBrokerageModel();

        [TestCaseSource(nameof(GetUnsupportedOptions))]
        public void CannotSubmitOrder_IndexOptionExercise(Security security)
        {
            var order = new Mock<OptionExerciseOrder>();
            order.Setup(x => x.Type).Returns(OrderType.OptionExercise);

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order.Object, out var message);

            Assert.IsFalse(canSubmit, message.Message);
            Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
            Assert.AreEqual("NotSupported", message.Code);
            StringAssert.Contains("exercises for index and cash-settled options", message.Message);
        }

        private static List<Security> GetUnsupportedOptions()
        {
            // Index option
            var spxSymbol = Symbol.Create("SPX", SecurityType.IndexOption, Market.USA);
            var spx = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new SubscriptionDataConfig(typeof(TradeBar), spxSymbol, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false),
                new Cash("USD", 1000, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            //Cash settled option
            var vixSymbol = Symbol.Create("VIX", SecurityType.Option, Market.USA);
            var vix = new Option(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new SubscriptionDataConfig(typeof(TradeBar), vixSymbol, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false),
                new Cash("USD", 1000, 1),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
            vix.ExerciseSettlement = SettlementType.Cash;

            return new() {spx, vix};
        }
    }
}
