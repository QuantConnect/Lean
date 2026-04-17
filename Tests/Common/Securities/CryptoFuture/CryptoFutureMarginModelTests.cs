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
using QuantConnect.Orders.Fees;
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
                // ((quantity * contract multiplier * price) / leverage) * conversion rate (BTC -> USD)
                marginRequirement = ((parameters.Quantity * 100m * cryptoFuture.Price) / 25m) * 1 / cryptoFuture.Price;
            }
            else
            {
                // ((quantity * contract multiplier * price) / leverage) * conversion rate (USDT ~= USD)
                marginRequirement = ((parameters.Quantity * 1m * cryptoFuture.Price) / 25m) * 1;
            }

            Assert.AreEqual(Math.Abs(marginRequirement), result.Value);
        }

        [TestCase("BTCUSDT", 10, 16000, 12000)]
        [TestCase("BTCUSD", 15000, 31300, 30000)]
        public void InitialMarginRequiredForOrderUsesLimitOrderPrice(string ticker, decimal quantity, decimal securityPrice, decimal limitPrice)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            SetPrice(cryptoFuture, securityPrice);

            var limitOrder = new LimitOrder(cryptoFuture.Symbol, quantity, limitPrice, DateTime.UtcNow);
            var marginModel = cryptoFuture.BuyingPowerModel;

            var marginForLimitOrder = marginModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(algo.Portfolio.CashBook, cryptoFuture, limitOrder)).Value;

            var expectedLimitOrderPrice = GetOrderMarginPrice(quantity, limitPrice, securityPrice);
            var expected = GetExpectedOrderInitialMargin(algo, cryptoFuture, limitOrder, expectedLimitOrderPrice);

            Assert.AreEqual(expected, marginForLimitOrder);

            var marketOrder = new MarketOrder(cryptoFuture.Symbol, quantity, DateTime.UtcNow);
            var marginForMarketOrder = marginModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(algo.Portfolio.CashBook, cryptoFuture, marketOrder)).Value;

            Assert.AreNotEqual(marginForMarketOrder, marginForLimitOrder);
        }

        [TestCase("BTCUSDT", -10, 16000, 19000)]
        [TestCase("BTCUSD", -15000, 31300, 34000)]
        public void InitialMarginRequiredForOrderUsesLimitOrderPriceForShortOrders(string ticker, decimal quantity, decimal securityPrice, decimal limitPrice)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            SetPrice(cryptoFuture, securityPrice);

            var limitOrder = new LimitOrder(cryptoFuture.Symbol, quantity, limitPrice, DateTime.UtcNow);
            var marginModel = cryptoFuture.BuyingPowerModel;

            var marginForLimitOrder = marginModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(algo.Portfolio.CashBook, cryptoFuture, limitOrder)).Value;

            var expectedLimitOrderPrice = GetOrderMarginPrice(quantity, limitPrice, securityPrice);
            var expected = GetExpectedOrderInitialMargin(algo, cryptoFuture, limitOrder, expectedLimitOrderPrice);

            Assert.AreEqual(expected, marginForLimitOrder);

            var marketOrder = new MarketOrder(cryptoFuture.Symbol, quantity, DateTime.UtcNow);
            var marginForMarketOrder = marginModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(algo.Portfolio.CashBook, cryptoFuture, marketOrder)).Value;

            Assert.AreNotEqual(marginForMarketOrder, marginForLimitOrder);
        }

        [TestCase("BTCUSDT", -10, 16000, 17000, 19000)]
        [TestCase("BTCUSD", -15000, 31300, 32000, 34000)]
        public void InitialMarginRequiredForOrderUsesStopLimitOrderPrice(string ticker, decimal quantity, decimal securityPrice, decimal stopPrice, decimal limitPrice)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            SetPrice(cryptoFuture, securityPrice);

            var stopLimitOrder = new StopLimitOrder(cryptoFuture.Symbol, quantity, stopPrice, limitPrice, DateTime.UtcNow);
            var marginModel = cryptoFuture.BuyingPowerModel;

            var marginForStopLimitOrder = marginModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(algo.Portfolio.CashBook, cryptoFuture, stopLimitOrder)).Value;

            var expectedStopLimitOrderPrice = GetOrderMarginPrice(quantity, limitPrice, securityPrice);
            var expected = GetExpectedOrderInitialMargin(algo, cryptoFuture, stopLimitOrder, expectedStopLimitOrderPrice);

            Assert.AreEqual(expected, marginForStopLimitOrder);

            var marketOrder = new MarketOrder(cryptoFuture.Symbol, quantity, DateTime.UtcNow);
            var marginForMarketOrder = marginModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(algo.Portfolio.CashBook, cryptoFuture, marketOrder)).Value;

            Assert.AreNotEqual(marginForMarketOrder, marginForStopLimitOrder);
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

            Assert.AreEqual(100m, buyingPower.Value);
        }

        [Test]
        public void BnfcrZeroBalanceIncludesSupplementaryCollateral()
        {
            var algo = GetBinanceFuturesAlgorithm();
            var algoCash = algo.Portfolio.Cash;
            var cryptoFuture = algo.AddCryptoFuture("BTCUSDT");
            SetPrice(cryptoFuture, 16000);

            // EU user: BNFCR present with zero balance, USDC is the real collateral
            algo.SetCash("BNFCR", 0, 1);
            algo.SetCash("USDC", 100, 1);

            var buyingPower = cryptoFuture.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, cryptoFuture, OrderDirection.Buy));

            // BNFCR presence triggers supplementary collateral - USDC should be included
            Assert.AreEqual(100m + algoCash, buyingPower.Value);
        }

        [Test]
        public void BtcCollateralConvertedToQuoteCurrency()
        {
            var algo = GetBinanceFuturesAlgorithm();
            var algoCash = algo.Portfolio.Cash;
            var cryptoFuture = algo.AddCryptoFuture("BTCUSDC");
            SetPrice(cryptoFuture, 16000);

            // EU user: BNFCR present, 0.5 BTC as collateral @ $16,000
            algo.SetCash("BNFCR", 0, 1);
            algo.SetCash("BTC", 0.5m, 16000);

            var buyingPower = cryptoFuture.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, cryptoFuture, OrderDirection.Buy));

            // 0 (USDC) + 0.5 * 16000 (BTC -> USDC via USD) = 8000
            Assert.AreEqual(8000m + algoCash, buyingPower.Value);
        }

        [Test]
        public void SharedCollateralDeductsMaintenanceMarginAcrossQuoteCurrencies()
        {
            var algo = GetBinanceFuturesAlgorithm();
            var algoCash = algo.Portfolio.Cash;

            // Two USD-margined futures with DIFFERENT quote currencies
            var btcUsdt = algo.AddCryptoFuture("BTCUSDT");
            var ethUsdc = algo.AddCryptoFuture("ETHUSDC");
            SetPrice(btcUsdt, 16000);
            SetPrice(ethUsdc, 1600);

            // EU user: BNFCR present - all USD-margined futures share collateral pool
            algo.SetCash("BNFCR", 10000, 1);

            // Simulate an existing ETHUSDC position (10 ETH @ $1,600)
            ethUsdc.Holdings.SetHoldings(1600, 10);

            // ETHUSDC maintenance margin = (10 * 1 * 1600) / 25 * 1 = 640
            var ethMaintenanceMargin = ethUsdc.BuyingPowerModel.GetMaintenanceMargin(
                MaintenanceMarginParameters.ForCurrentHoldings(ethUsdc));

            Assert.AreEqual(640m, ethMaintenanceMargin.Value);

            // Buying power for BTCUSDT should deduct ETHUSDC's maintenance margin
            var buyingPower = btcUsdt.BuyingPowerModel.GetBuyingPower(
                new BuyingPowerParameters(algo.Portfolio, btcUsdt, OrderDirection.Buy));

            // Expected: (10000 BNFCR + algoCash) - 640 maintenance margin
            var expectedBuyingPower = 10000m + algoCash - ethMaintenanceMargin.Value;
            Assert.AreEqual(expectedBuyingPower, buyingPower.Value,
                "ETHUSDC maintenance margin should be deducted from BTCUSDT buying power when sharing EU collateral pool");
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

        private static decimal GetExpectedOrderInitialMargin(QCAlgorithm algo, Security security, Order order, decimal orderPrice)
        {
            var marginModel = security.BuyingPowerModel;
            var positionValue = security.Holdings.GetQuantityValue(order.Quantity, orderPrice);
            var expected = Math.Abs(positionValue.Amount) / marginModel.GetLeverage(security)
                           * positionValue.Cash.ConversionRate;

            var fees = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, order)).Value;
            var feesInAccountCurrency = algo.Portfolio.CashBook.ConvertToAccountCurrency(fees).Amount;

            return expected + feesInAccountCurrency;
        }

        private static decimal GetOrderMarginPrice(decimal quantity, decimal orderLimitPrice, decimal securityPrice)
        {
            if (quantity == 0m)
            {
                return securityPrice;
            }

            return quantity > 0m
                ? Math.Min(orderLimitPrice, securityPrice)
                : Math.Max(orderLimitPrice, securityPrice);
        }
    }
}
