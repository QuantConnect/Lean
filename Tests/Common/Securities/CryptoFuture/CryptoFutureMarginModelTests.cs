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
using System.Collections.Generic;
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

            // BNFCR presence triggers supplementary collateral — USDC should be included
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

            // 0 (USDC) + 0.5 * 16000 (BTC → USDC via USD) = 8000
            Assert.AreEqual(8000m + algoCash, buyingPower.Value);
        }

        [Test]
        public void SharedCollateralDeductsMaintenanceMarginAcrossQuoteCurrencies()
        {
            var algo = GetBinanceFuturesAlgorithm();
            var algoCash = algo.Portfolio.Cash;

            // Two USDⓈ-M futures with DIFFERENT quote currencies
            var btcUsdt = algo.AddCryptoFuture("BTCUSDT");
            var ethUsdc = algo.AddCryptoFuture("ETHUSDC");
            SetPrice(btcUsdt, 16000);
            SetPrice(ethUsdc, 1600);

            // EU user: BNFCR present — all USDⓈ-M futures share collateral pool
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

        [TestCase("BTCUSD", Market.Binance, 100, 40000, 44000, 1000)]
        [TestCase("BTCUSD", Market.Binance, 100, 40000, 36000, -1000)]
        [TestCase("BTCUSD", Market.Binance, -100, 40000, 44000, -1000)]
        [TestCase("BTCUSD", Market.Binance, -100, 40000, 36000, 1000)]
        [TestCase("ADAUSDT", Market.Binance, 100, 0.4, 0.44, 4)]
        [TestCase("ADAUSDT", Market.Binance, 100, 0.4, 0.36, -4)]
        [TestCase("ADAUSDT", Market.Binance, -100, 0.4, 0.44, -4)]
        [TestCase("ADAUSDT", Market.Binance, -100, 0.4, 0.36, 4)]
        [TestCase("BTCUSD", Market.DYDX, 1, 40000, 44000, 4000)]
        [TestCase("BTCUSD", Market.DYDX, 1, 40000, 36000, -4000)]
        [TestCase("BTCUSD", Market.DYDX, -1, 40000, 44000, -4000)]
        [TestCase("BTCUSD", Market.DYDX, -1, 40000, 36000, 4000)]
        public void UnrealizedProfitFollowsPriceMove(string ticker, string market, decimal quantity,
            decimal entryPrice, decimal exitPrice, decimal expectedProfit)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker, market: market);
            cryptoFuture.FeeModel = new ConstantFeeModel(2);
            cryptoFuture.Holdings.SetHoldings(entryPrice, quantity);
            SetPrice(cryptoFuture, exitPrice);

            // COIN-M: q * 100 * (1 / entry - 1 / exit) BTC, converted to USD at exit.
            // Linear contracts: q * (exit - entry) in the quote currency (USD or USDT at 1).
            Assert.Multiple(() =>
            {
                Assert.That(cryptoFuture.Holdings.TotalCloseProfit(includeFees: false), Is.EqualTo(expectedProfit).Within(0.00000001m));
                Assert.That(cryptoFuture.Holdings.UnrealizedProfit, Is.EqualTo(expectedProfit - 2).Within(0.00000001m));
                Assert.That(algo.Portfolio.TotalPortfolioValue,
                    Is.EqualTo(algo.Portfolio.CashBook.TotalValueInAccountCurrency + expectedProfit - 2).Within(0.00000001m));
                Assert.That(cryptoFuture.Holdings.TotalCloseProfit(includeFees: false,
                    entryPrice: entryPrice, exitPrice: exitPrice, quantity: quantity / 2), Is.EqualTo(expectedProfit / 2).Within(0.00000001m));
            });
        }

        [TestCase("BTCUSD", 100, 40000, 44000, 100, 0, 1000)]
        [TestCase("BTCUSD", 100, 40000, 36000, 100, 0, -1000)]
        [TestCase("BTCUSD", -100, 40000, 44000, 100, 0, -1000)]
        [TestCase("BTCUSD", -100, 40000, 36000, 100, 0, 1000)]
        [TestCase("BTCUSD", 100, 40000, 44000, 40, 0.0001, 400)]
        [TestCase("BTCUSD", 100, 40000, 36000, 40, 0.0001, -400)]
        [TestCase("BTCUSD", -100, 40000, 44000, 40, 0.0001, -400)]
        [TestCase("BTCUSD", -100, 40000, 36000, 40, 0.0001, 400)]
        [TestCase("ADAUSDT", 100, 0.4, 0.44, 100, 0, 4)]
        [TestCase("ADAUSDT", -100, 0.4, 0.36, 100, 0, 4)]
        [TestCase("ADAUSDT", 100, 0.4, 0.36, 40, 0.01, -1.6)]
        [TestCase("ADAUSDT", -100, 0.4, 0.44, 40, 0.01, -1.6)]
        public void CloseProfitSettlesInMarginCurrency(string ticker, decimal quantity, decimal entryPrice,
            decimal exitPrice, decimal absoluteQuantityClosed, decimal fee, decimal expectedProfit)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            var isInverse = ticker == "BTCUSD";
            var marginCash = isInverse ? cryptoFuture.BaseCurrency : cryptoFuture.QuoteCurrency;
            var otherCash = isInverse ? cryptoFuture.QuoteCurrency : cryptoFuture.BaseCurrency;
            marginCash.SetAmount(1);
            cryptoFuture.FeeModel = new ConstantFeeModel(fee, marginCash.Symbol);
            var orderFee = new OrderFee(new CashAmount(fee, marginCash.Symbol));

            SetPrice(cryptoFuture, entryPrice);
            algo.Portfolio.ProcessFills(new List<OrderEvent>
            {
                new OrderEvent(1, cryptoFuture.Symbol, algo.UtcTime, OrderStatus.Filled,
                    quantity > 0 ? OrderDirection.Buy : OrderDirection.Sell, entryPrice, quantity, orderFee)
            });

            SetPrice(cryptoFuture, exitPrice);
            var cashBeforeClose = marginCash.Amount;
            var otherCashBeforeClose = otherCash.Amount;
            var cashValueBeforeClose = algo.Portfolio.CashBook.TotalValueInAccountCurrency;
            var quantityClosed = Math.Sign(quantity) * absoluteQuantityClosed;
            algo.Portfolio.ProcessFills(new List<OrderEvent>
            {
                new OrderEvent(2, cryptoFuture.Symbol, algo.UtcTime, OrderStatus.Filled,
                    quantity > 0 ? OrderDirection.Sell : OrderDirection.Buy, exitPrice, -quantityClosed, orderFee)
            });

            // The literal USD profits use the contract formulas above, scaled to the closed quantity.
            // Inverse profit settles in BTC at the exit conversion rate; linear profit settles in USDT.
            var expectedMarginProfit = isInverse ? expectedProfit / exitPrice : expectedProfit;
            var expectedFees = isInverse ? fee * (entryPrice + exitPrice) : 2 * fee;
            var expectedRemainingProfit = expectedProfit * (quantity - quantityClosed) / quantityClosed;
            var closeFeeInAccountCurrency = fee * marginCash.ConversionRate;
            var remainingLiquidationFee = quantity == quantityClosed ? 0 : closeFeeInAccountCurrency;
            Assert.Multiple(() =>
            {
                Assert.AreEqual(1 - fee, cashBeforeClose, "Opening only debits the fee from collateral");
                Assert.That(marginCash.Amount - cashBeforeClose, Is.EqualTo(expectedMarginProfit - fee).Within(0.000000000001m));
                Assert.AreEqual(otherCashBeforeClose, otherCash.Amount);
                Assert.That(cryptoFuture.Holdings.Profit, Is.EqualTo(expectedProfit).Within(0.00000001m));
                Assert.That(cryptoFuture.Holdings.LastTradeProfit, Is.EqualTo(expectedProfit).Within(0.00000001m));
                Assert.That(cryptoFuture.Holdings.TotalFees, Is.EqualTo(expectedFees).Within(0.00000001m));
                Assert.That(cryptoFuture.Holdings.NetProfit, Is.EqualTo(expectedProfit - expectedFees).Within(0.00000001m));
                Assert.AreEqual(quantity - quantityClosed, cryptoFuture.Holdings.Quantity);
                Assert.That(cryptoFuture.Holdings.UnrealizedProfit,
                    Is.EqualTo(expectedRemainingProfit - remainingLiquidationFee).Within(0.00000001m));
                Assert.That(algo.Portfolio.TotalPortfolioValue, Is.EqualTo(cashValueBeforeClose + expectedProfit + expectedRemainingProfit
                    - closeFeeInAccountCurrency - remainingLiquidationFee).Within(0.00000001m));
            });
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
