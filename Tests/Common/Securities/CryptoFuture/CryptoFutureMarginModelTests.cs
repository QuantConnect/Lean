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
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Brokerages;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities.CryptoFuture
{
    [TestFixture]
    public class CryptoFutureMarginModelTests
    {
        [TestCase("BTCUSD")]
        [TestCase("BTCUSDT")]
        public void DefaultMarginModelType(string ticker)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);

            Assert.AreEqual(typeof(CryptoFutureMarginModel), cryptoFuture.BuyingPowerModel.GetType());
        }

        [TestCase("BTCUSD", 10)]
        [TestCase("BTCUSDT", 10)]
        [TestCase("BTCUSD", -10)]
        [TestCase("BTCUSDT", -10)]
        public void InitialMarginRequirement(string ticker, decimal quantity)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            SetPrice(cryptoFuture, 16000);

            var parameters = new InitialMarginParameters(cryptoFuture, quantity);
            var result = cryptoFuture.BuyingPowerModel.GetInitialMarginRequirement(parameters);

            decimal marginRequirement;
            if (ticker == "BTCUSD")
            {
                // ((quantity * contract mutiplier * price) / leverage) * conversion rate (BTC -> USD)
                marginRequirement = ((parameters.Quantity * 100m * cryptoFuture.Price) / 25m) * 1 / cryptoFuture.Price;
            }
            else
            {
                // ((quantity * contract mutiplier * price) / leverage) * conversion rate (USDT ~= USD)
                marginRequirement = ((parameters.Quantity * 1m * cryptoFuture.Price) / 25m) * 1;
            }

            Assert.AreEqual(Math.Abs(marginRequirement), result.Value);
        }

        [Test]
        public void MarginRemainingWithBnfcrOnlyCollateral()
        {
            var algo = GetBinanceFuturesAlgorithm();
            var algoCash = algo.Portfolio.Cash;
            var cryptoFuture = algo.AddCryptoFuture("BTCUSDT");
            SetPrice(cryptoFuture, 16000);

            // EU Binance user: only BNFCR, no USDT
            algo.SetCash("BNFCR", 100, 1);

            var buyingPower = cryptoFuture.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, cryptoFuture, OrderDirection.Buy));

            Assert.Greater(buyingPower.Value, 0);
            Assert.AreEqual(100m + algoCash, buyingPower.Value);
        }

        [Test]
        public void MarginRemainingWithMixedCollateral()
        {
            var algo = GetBinanceFuturesAlgorithm();
            var algoCash = algo.Portfolio.Cash;
            var cryptoFuture = algo.AddCryptoFuture("BTCUSDT");
            SetPrice(cryptoFuture, 16000);

            // Mixed: 50 USDT + 50 BNFCR
            algo.SetCash("USDT", 50, 1);
            algo.SetCash("BNFCR", 50, 1);

            var buyingPower = cryptoFuture.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, cryptoFuture, OrderDirection.Buy));

            Assert.AreEqual(100m + algoCash, buyingPower.Value);
        }

        [Test]
        public void MarginRemainingWithUsdtOnlyCollateral()
        {
            var algo = GetBinanceFuturesAlgorithm();
            var algoCash = algo.Portfolio.Cash;
            var cryptoFuture = algo.AddCryptoFuture("BTCUSDT");
            SetPrice(cryptoFuture, 16000);

            // Standard user: only USDT (backward compatibility)
            algo.SetCash("USDT", 100, 1);

            var buyingPower = cryptoFuture.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, cryptoFuture, OrderDirection.Buy));

            Assert.AreEqual(100m + algoCash, buyingPower.Value);
        }

        [Test]
        public void CoinFutureDoesNotIncludeBnfcrAsCollateral()
        {
            var algo = GetBinanceFuturesAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture("BTCUSD");
            SetPrice(cryptoFuture, 16000);

            // Coin future: BTC is collateral, BNFCR should NOT be included
            algo.SetCash("BNFCR", 100, 1);

            var buyingPower = cryptoFuture.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, cryptoFuture, OrderDirection.Buy));

            // Only BTC collateral counts (0 BTC), BNFCR is irrelevant for coin futures
            Assert.AreEqual(0, buyingPower.Value);
        }

        [Test]
        public void DefaultMarginModelDoesNotIncludeSupplementaryCollateral()
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture("BTCUSDT");
            SetPrice(cryptoFuture, 16000);

            // Default model should NOT include BNFCR as collateral
            algo.SetCash("BNFCR", 100, 1);

            var buyingPower = cryptoFuture.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, cryptoFuture, OrderDirection.Buy));

            Assert.AreEqual(0, buyingPower.Value);
        }

        private static QCAlgorithm GetAlgorithm()
        {
            // Initialize algorithm
            var algo = new AlgorithmStub();
            algo.SetFinishedWarmingUp();
            return algo;
        }

        private static QCAlgorithm GetBinanceFuturesAlgorithm()
        {
            var algo = new AlgorithmStub();
            algo.SetBrokerageModel(BrokerageName.BinanceFutures, AccountType.Margin);
            algo.SetFinishedWarmingUp();
            return algo;
        }

        private static void SetPrice(Security security, decimal price)
        {
            var cryptoFuture = (QuantConnect.Securities.CryptoFuture.CryptoFuture)security;
            cryptoFuture.BaseCurrency.ConversionRate = price;
            cryptoFuture.QuoteCurrency.ConversionRate = 1;

            security.SetMarketPrice(new TradeBar
            {
                Time = new DateTime(2022, 12, 22),
                Symbol = security.Symbol,
                Open = price,
                High = price,
                Low = price,
                Close = price
            });
        }
    }
}
