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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Tests.Engine.DataFeeds;

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

        [TestCase(10, 0.40, Description = "leverage=10 => initialMargin ≈ 4 / 0.267 / 10 * 0.267 = 0.40 USD")]
        [TestCase(25, 0.16, Description = "leverage=25 => initialMargin ≈ 4 / 0.267 / 25 * 0.267 = 0.16 USD")]
        public void GetBuyingPowerUsesUsdBalance_WithDifferentLeverage(decimal leverage, double expectedInitialMarginUsd)
        {
            // Reproduces the live trading scenario: Bybit UTA reports TotalAvailableBalance as USD
            // (no ADA in account), so the margin model must use USD as collateral.
            var algo = new AlgorithmStub();
            algo.SetBrokerageModel(BrokerageName.BybitInverseFutures, AccountType.Margin);
            algo.SetFinishedWarmingUp();

            var adaUsd = algo.AddCryptoFuture("ADAUSD");
            adaUsd.SetLeverage(leverage);

            const decimal adaPrice = 0.267m;
            const decimal usdBalance = 100m;

            adaUsd.QuoteCurrency.SetAmount(usdBalance); // USD = 100 (from GetCashBalance)
            adaUsd.BaseCurrency.SetAmount(0m);           // ADA = 0 (no base asset in account)
            adaUsd.BaseCurrency.ConversionRate = adaPrice;
            adaUsd.QuoteCurrency.ConversionRate = 1m;
            adaUsd.SetMarketPrice(new TradeBar(new DateTime(2026, 1, 1), adaUsd.Symbol, adaPrice, adaPrice, adaPrice, adaPrice, volume: 1m));

            // Buying power = USD balance regardless of leverage
            var buyingPower = adaUsd.BuyingPowerModel.GetBuyingPower(new BuyingPowerParameters(algo.Portfolio, adaUsd, OrderDirection.Buy));
            Assert.AreEqual((double)usdBalance, (double)buyingPower.Value, delta: 0.01);

            // Initial margin scales inversely with leverage
            var initialMargin = adaUsd.BuyingPowerModel.GetInitialMarginRequirement(new InitialMarginParameters(adaUsd, 4));
            Assert.AreEqual(expectedInitialMarginUsd, (double)initialMargin.Value, delta: 0.05);
        }
    }
}
