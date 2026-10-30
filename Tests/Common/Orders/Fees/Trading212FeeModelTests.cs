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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class Trading212FeeModelTests
    {
        private static readonly DateTime Time = new(2024, 1, 3, 15, 0, 0);

        // 10 shares at $100 => $1,000 traded.
        // FX fee 0.15% = $1.50; SEC Section 31 0.206 bps = $0.0206; FINRA TAF $0.000195/share = $0.00195.
        [Test]
        public void GetOrderFeeUsEquityBuyInAnotherCurrencyChargesFxFee()
        {
            var security = CreateUsEquity(100m);
            var order = new MarketOrder(security.Symbol, 10m, Time);

            var fee = new Trading212FeeModel().GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(1.5m));
            Assert.That(fee.Value.Currency, Is.EqualTo(Currencies.USD));
        }

        [Test]
        public void GetOrderFeeUsEquitySellInAnotherCurrencyChargesFxAndRegulatoryFees()
        {
            var security = CreateUsEquity(100m);
            var order = new MarketOrder(security.Symbol, -10m, Time);

            var fee = new Trading212FeeModel().GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(1.52255m));
            Assert.That(fee.Value.Currency, Is.EqualTo(Currencies.USD));
        }

        [Test]
        public void GetOrderFeeUsEquityBuyInAccountCurrencyChargesNothing()
        {
            var security = CreateUsEquity(100m);
            var order = new MarketOrder(security.Symbol, 10m, Time);

            var fee = new Trading212FeeModel(Currencies.USD).GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0m));
        }

        [Test]
        public void GetOrderFeeUsEquitySellInAccountCurrencyChargesOnlyRegulatoryFees()
        {
            var security = CreateUsEquity(100m);
            var order = new MarketOrder(security.Symbol, -10m, Time);

            var fee = new Trading212FeeModel(Currencies.USD).GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0.02255m));
        }

        // FINRA caps the trading activity fee at $9.79 per trade, so a large sale is not charged the
        // uncapped per-share rate.
        [Test]
        public void GetOrderFeeCapsTheFinraTradingActivityFeePerTrade()
        {
            // 100,000 shares at $1 => the uncapped TAF would be $19.50.
            var security = CreateUsEquity(1m);
            var order = new MarketOrder(security.Symbol, -100000m, Time);

            var fee = new Trading212FeeModel(Currencies.USD).GetOrderFee(new OrderFeeParameters(security, order));

            // SEC Section 31 0.206 bps of $100,000 = $2.06, plus the capped $9.79 TAF.
            Assert.That(fee.Value.Amount, Is.EqualTo(11.85m));
        }

        // 50,205 shares is the largest quantity whose TAF is below the cap; one more share reaches it.
        [TestCase(50205, 9.789975)]
        [TestCase(50206, 9.79)]
        public void GetOrderFeeAppliesTheFinraTradingActivityFeeCapAtTheBoundary(decimal quantity, decimal expectedTaf)
        {
            var security = CreateUsEquity(1m);
            var order = new MarketOrder(security.Symbol, -quantity, Time);

            var fee = new Trading212FeeModel(Currencies.USD).GetOrderFee(new OrderFeeParameters(security, order));

            var expectedSection31 = 0.0000206m * quantity;
            Assert.That(fee.Value.Amount, Is.EqualTo(expectedSection31 + expectedTaf));
        }

        // The fee is charged once, at the completed fill, against the quantity the order has then, so
        // an amendment that moves the quantity across the cap boundary is reflected in the fee. This
        // pins the cap to the amended quantity rather than the submitted one.
        [TestCase(50000, 50300)] // 9.75 below the cap, amended to 50,300 => capped at 9.79
        [TestCase(50300, 50000)] // capped at 9.79, amended to 50,000 => back to the per-share rate
        public void GetOrderFeeAppliesTheFinraTradingActivityFeeCapToTheAmendedQuantity(decimal submittedQuantity, decimal amendedQuantity)
        {
            var security = CreateUsEquity(1m);
            var order = new MarketOrder(security.Symbol, -submittedQuantity, Time);

            order.ApplyUpdateOrderRequest(new UpdateOrderRequest(Time, order.Id, new UpdateOrderFields
            {
                Quantity = -amendedQuantity
            }));

            var fee = new Trading212FeeModel(Currencies.USD).GetOrderFee(new OrderFeeParameters(security, order));

            var expectedTaf = Math.Min(0.000195m * amendedQuantity, 9.79m);
            var expectedSection31 = 0.0000206m * amendedQuantity;
            Assert.That(fee.Value.Amount, Is.EqualTo(expectedSection31 + expectedTaf));

            // The submitted quantity would give a different fee, so the assertions above do pin the
            // cap to the amended quantity.
            var submittedTaf = Math.Min(0.000195m * submittedQuantity, 9.79m);
            Assert.That(expectedTaf, Is.Not.EqualTo(submittedTaf));
        }

        [Test]
        public void GetOrderFeeUnsupportedSecurityTypeThrows()
        {
            var security = CreateSecurity(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase), Currencies.USD, 100m);
            var order = new MarketOrder(security.Symbol, 1m, Time);

            Assert.Throws<ArgumentException>(() =>
                new Trading212FeeModel().GetOrderFee(new OrderFeeParameters(security, order)));
        }

        // The fees are charged on the trade value in the instrument's quote currency. With a GBP
        // account the order is worth GBP 800 but USD 1,000, so the fee is the USD one.
        [Test]
        public void GetOrderFeeUsesTheQuoteCurrencyValueWhenTheAccountCurrencyDiffers()
        {
            var security = CreateUsEquity(100m, conversionRate: 0.8m);
            var order = new MarketOrder(security.Symbol, 10m, Time);

            var fee = new Trading212FeeModel().GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(1.5m));
            Assert.That(fee.Value.Currency, Is.EqualTo(Currencies.USD));
        }

        [Test]
        public void GetOrderFeeSettlesInTheAccountCurrency()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetAccountCurrency(Currencies.GBP, 1000m);
            var security = algorithm.AddSecurity(Symbols.AAPL);
            security.SetMarketPrice(new Tick(Time, security.Symbol, 100m, 100m));
            var portfolio = algorithm.Portfolio;
            // One USD is worth 0.8 GBP.
            portfolio.SetCash(Currencies.USD, 0m, 0.8m);

            var order = new MarketOrder(security.Symbol, 10m, Time);
            var fee = new Trading212FeeModel().GetOrderFee(new OrderFeeParameters(security, order));
            fee.ApplyToPortfolio(portfolio, null);

            // The 1.50 USD fee is 1.20 GBP, and no USD balance is left behind.
            Assert.That(portfolio.CashBook[Currencies.GBP].Amount, Is.EqualTo(998.8m));
            Assert.That(portfolio.CashBook[Currencies.USD].Amount, Is.EqualTo(0m));
        }

        // Only US-listed equities carry the SEC Section 31 and FINRA fees.
        [Test]
        public void GetOrderFeeNonUsEquitySellChargesOnlyTheFxFee()
        {
            var security = CreateSecurity(Symbol.Create("TCS", SecurityType.Equity, Market.India), Currencies.INR, 100m);
            var order = new MarketOrder(security.Symbol, -10m, Time);

            var fee = new Trading212FeeModel().GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(1.5m));
            Assert.That(fee.Value.Currency, Is.EqualTo(Currencies.INR));
        }

        [Test]
        public void BrokerageModelReturnsTheTrading212FeeModel()
        {
            var security = CreateUsEquity(100m);

            var feeModel = new Trading212BrokerageModel().GetFeeModel(security);

            Assert.That(feeModel, Is.InstanceOf<Trading212FeeModel>());
        }

        private static Security CreateUsEquity(decimal price, decimal conversionRate = 1m)
        {
            return CreateSecurity(Symbol.Create("AAPL", SecurityType.Equity, Market.USA), Currencies.USD, price, conversionRate);
        }

        private static Security CreateSecurity(Symbol symbol, string quoteCurrency, decimal price, decimal conversionRate = 1m)
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute,
                TimeZones.Utc, TimeZones.Utc, true, true, false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                config,
                new Cash(quoteCurrency, 0, conversionRate),
                SymbolProperties.GetDefault(quoteCurrency),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            security.SetMarketPrice(new Tick(Time, symbol, price, price));
            return security;
        }
    }
}
