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

using System;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.CurrencyConversion;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class Trading212SettlementModelTests
    {
        private static readonly DateTime Noon = new(2014, 6, 24, 12, 0, 0);

        // A GBP account trading a USD equity: the fill, its fee and the settlement all have to meet
        // in SecurityPortfolioModel.ProcessFill and leave no USD balance behind.
        [Test]
        public void BuyAndSellSettleInTheAccountCurrencyThroughProcessFill()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetDateTime(Noon);
            algorithm.SetAccountCurrency(Currencies.GBP, 100000);
            var portfolio = algorithm.Portfolio;
            var security = algorithm.AddSecurity(Symbols.AAPL);
            security.SetMarketPrice(new Tick(Noon, security.Symbol, 100m, 100m));

            // One USD is worth 0.8 GBP, and the account is GBP.
            portfolio.SetCash(Currencies.USD, 0m, 0.8m);
            portfolio.CashBook[Currencies.USD].CurrencyConversion =
                new ConstantCurrencyConversion(Currencies.USD, Currencies.GBP, 0.8m);
            security.SettlementModel = new Trading212BrokerageModel().GetSettlementModel(security);

            var feeModel = new Trading212FeeModel();
            var timeUtc = Noon.ConvertToUtc(TimeZones.NewYork);

            // Buy 10 shares at USD 100 => GBP 800, plus the 0.15% FX fee (GBP 1.20).
            security.PortfolioModel.ProcessFill(portfolio, security, CreateFill(security, timeUtc, OrderDirection.Buy, 100m, 10m, feeModel));
            Assert.That(portfolio.CashBook[Currencies.GBP].Amount, Is.EqualTo(99198.8m));
            Assert.That(portfolio.CashBook[Currencies.USD].Amount, Is.EqualTo(0m));
            Assert.That(portfolio.UnsettledCash, Is.EqualTo(0m));

            // Sell 10 shares at USD 110 => GBP 880, less the FX fee and the US regulatory fees.
            security.SetMarketPrice(new Tick(Noon, security.Symbol, 110m, 110m));
            security.PortfolioModel.ProcessFill(portfolio, security, CreateFill(security, timeUtc, OrderDirection.Sell, 110m, -10m, feeModel));
            Assert.That(portfolio.CashBook[Currencies.GBP].Amount, Is.EqualTo(100077.460312m));
            Assert.That(portfolio.CashBook[Currencies.USD].Amount, Is.EqualTo(0m));
            Assert.That(portfolio.UnsettledCash, Is.EqualTo(0m));
        }

        private static OrderEvent CreateFill(Security security, DateTime timeUtc, OrderDirection direction,
            decimal price, decimal quantity, Trading212FeeModel feeModel)
        {
            var order = new MarketOrder(security.Symbol, quantity, timeUtc)
            {
                Status = OrderStatus.Filled,
                Price = price
            };
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(security, order));
            return new OrderEvent(order.Id, order.Symbol, timeUtc, OrderStatus.Filled, direction, price, quantity, fee);
        }
    }
}
