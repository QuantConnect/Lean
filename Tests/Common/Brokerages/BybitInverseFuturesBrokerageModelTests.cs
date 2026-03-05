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

using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class BybitInverseFuturesBrokerageModelTests
    {
        private static readonly Symbol BTCUSD_Future = Symbol.Create("BTCUSD", SecurityType.CryptoFuture, Market.Bybit);
        private static readonly BybitInverseFuturesBrokerageModel Model = new();

        [Test]
        public void DefaultAccountTypeIsMargin()
        {
            Assert.AreEqual(AccountType.Margin, Model.AccountType);
        }

        [Test]
        public void GetFeeModelReturnsBybitFuturesFeeModel_ForCryptoFuture()
        {
            var security = TestsHelpers.GetSecurity(symbol: BTCUSD_Future.Value,
                securityType: SecurityType.CryptoFuture,
                market: Market.Bybit,
                quoteCurrency: "USD");

            Assert.IsInstanceOf<BybitFuturesFeeModel>(Model.GetFeeModel(security));
        }

        [Test]
        public void GetBrokerageNameReturnsBybitInverseFutures()
        {
            Assert.AreEqual(BrokerageName.BybitInverseFutures, BrokerageModel.GetBrokerageName(new BybitInverseFuturesBrokerageModel()));
        }

        [Test]
        public void GetBrokerageModelReturnsInverseFuturesModel()
        {
            var model = BrokerageModel.Create(null, BrokerageName.BybitInverseFutures, AccountType.Margin);
            Assert.IsInstanceOf<BybitInverseFuturesBrokerageModel>(model);
        }

        [TestCase(AccountType.Cash, 1)]
        [TestCase(AccountType.Margin, 10)]
        public void GetLeverageReturnsCorrectValue(AccountType accountType, decimal expectedLeverage)
        {
            var security = TestsHelpers.GetSecurity(symbol: BTCUSD_Future.Value,
                securityType: SecurityType.CryptoFuture,
                market: Market.Bybit,
                quoteCurrency: "USD");

            var model = new BybitInverseFuturesBrokerageModel(accountType);
            Assert.AreEqual(expectedLeverage, model.GetLeverage(security));
        }
    }
}
