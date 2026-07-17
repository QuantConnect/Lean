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
using System.Linq;
using System.Threading;
using System.Diagnostics;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Data.Custom.IconicTypes;
using System.Collections.Generic;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class LiveTradingResultHandlerTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void CustomData(bool invested)
        {
            var algorithm = new AlgorithmStub();
            var equity = algorithm.AddEquity("SPY");
            var customData = algorithm.AddData<UnlinkedData>("SPY");
            equity.Holdings.SetHoldings(1, 10);
            var result = LiveTradingResultHandler.GetHoldings(algorithm.Securities.Values, algorithm.SubscriptionManager.SubscriptionDataConfigService, invested);

            if (invested)
            {
                Assert.AreEqual(1, result.Count);
            }
            else
            {
                Assert.AreEqual(2, result.Count);
                Assert.IsTrue(result.TryGetValue(customData.Symbol.ID.ToString(), out var holding));
                Assert.AreEqual(0, holding.Quantity);
            }

            Assert.IsTrue(result.TryGetValue(equity.Symbol.ID.ToString(), out var holding2));
            Assert.AreEqual(10, holding2.Quantity);
        }

        [Test]
        public void UninitializedAlgorithm()
        {
            using var messagging = new QuantConnect.Messaging.Messaging();
            var result = new LiveTradingResultHandler();
            result.Initialize(new(new LiveNodePacket(), messagging, null, new BacktestingTransactionHandler(), null));

            var algorithm = new AlgorithmStub();
            algorithm.AddEquity("SPY");
            result.SetAlgorithm(algorithm, 10);

            Assert.DoesNotThrow(() => result.Exit());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetHoldingsPositions(bool invested)
        {
            var algorithm = new AlgorithmStub();
            var future = algorithm.AddFuture(Futures.Indices.SP500EMini);
            var equity = algorithm.AddEquity("SPY");
            equity.Holdings.SetHoldings(1, 10);
            var result = LiveTradingResultHandler.GetHoldings(algorithm.Securities.Values, algorithm.SubscriptionManager.SubscriptionDataConfigService, invested);

            if (invested)
            {
                Assert.AreEqual(1, result.Count);
            }
            else
            {
                Assert.AreEqual(2, result.Count);
                Assert.IsTrue(result.TryGetValue(future.Symbol.ID.ToString(), out var holding));
                Assert.AreEqual(0, holding.Quantity);
            }

            Assert.IsTrue(result.TryGetValue(equity.Symbol.ID.ToString(), out var holding2));
            Assert.AreEqual(10, holding2.Quantity);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetHoldingsNoPosition(bool invested)
        {
            var algorithm = new AlgorithmStub();
            var future = algorithm.AddFuture(Futures.Indices.SP500EMini);
            var equity = algorithm.AddEquity("SPY");
            var result = LiveTradingResultHandler.GetHoldings(algorithm.Securities.Values, algorithm.SubscriptionManager.SubscriptionDataConfigService, invested);

            if (invested)
            {
                Assert.AreEqual(0, result.Count);
            }
            else
            {
                Assert.AreEqual(2, result.Count);
                Assert.IsTrue(result.TryGetValue(future.Symbol.ID.ToString(), out var holding));
                Assert.AreEqual(0, holding.Quantity);
                Assert.IsTrue(result.TryGetValue(equity.Symbol.ID.ToString(), out var holding2));
                Assert.AreEqual(0, holding2.Quantity);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetHoldingsSkipCanonicalOption(bool invested)
        {
            var algorithm = new AlgorithmStub();
            var equity = algorithm.AddEquity("SPY");
            algorithm.AddOption("SPY");
            var result = LiveTradingResultHandler.GetHoldings(algorithm.Securities.Values, algorithm.SubscriptionManager.SubscriptionDataConfigService, invested);

            if (invested)
            {
                Assert.AreEqual(0, result.Count);
            }
            else
            {
                Assert.AreEqual(1, result.Count);
                Assert.IsTrue(result.TryGetValue(equity.Symbol.ID.ToString(), out var holding));
                Assert.AreEqual(0, holding.Quantity);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DailySampleValueBasedOnMarketHour(bool extendedMarketHoursEnabled)
        {
            using var api = new Api.Api();
            using var messagging = new QuantConnect.Messaging.Messaging();
            var referenceDate = new DateTime(2020, 11, 25);
            var resultHandler = new LiveTradingResultHandler();
            resultHandler.Initialize(new(new LiveNodePacket(), messagging, api, new BacktestingTransactionHandler(), null));

            try
            {
                var algo = new AlgorithmStub(createDataManager: false);
                algo.SetFinishedWarmingUp();
                var dataManager = new DataManagerStub(new TestDataFeed(), algo);
                algo.SubscriptionManager.SetDataManager(dataManager);
                var aapl = algo.AddEquity("AAPL", extendedMarketHours: extendedMarketHoursEnabled);
                algo.PostInitialize();
                resultHandler.SetAlgorithm(algo, 100000);
                resultHandler.OnSecuritiesChanged(SecurityChangesTests.AddedNonInternal(aapl));

                // Add values during market hours, should always update
                algo.Portfolio.CashBook["USD"].AddAmount(1000);
                algo.Portfolio.InvalidateTotalPortfolioValue();

                resultHandler.Sample(referenceDate.AddHours(15));
                Assert.IsTrue(resultHandler.Charts.ContainsKey("Strategy Equity"));
                Assert.AreEqual(1, resultHandler.Charts["Strategy Equity"].Series["Equity"].Values.Count);

                var currentEquityValue = (Candlestick)resultHandler.Charts["Strategy Equity"].Series["Equity"].Values.Last();
                Assert.AreEqual(101000, currentEquityValue.Close);

                // Add value to portfolio, see if portfolio updates with new sample
                // will be changed to 'extendedMarketHoursEnabled' = true
                algo.Portfolio.CashBook["USD"].AddAmount(10000);
                algo.Portfolio.InvalidateTotalPortfolioValue();

                resultHandler.Sample(referenceDate.AddHours(22));
                Assert.AreEqual(2, resultHandler.Charts["Strategy Equity"].Series["Equity"].Values.Count);

                currentEquityValue = (Candlestick)resultHandler.Charts["Strategy Equity"].Series["Equity"].Values.Last();
                Assert.AreEqual(extendedMarketHoursEnabled ? 111000 : 101000, currentEquityValue.Close);
            }
            finally
            {
                resultHandler.Exit();
            }
        }

        [Test]
        public void MessagesArePrefixedWithAlgorithmTime()
        {
            using var messaging = new QuantConnect.Messaging.Messaging();
            var result = new LiveTradingResultHandler();
            result.Initialize(new(new LiveNodePacket(), messaging, null, new BacktestingTransactionHandler(), null));

            var algorithm = new AlgorithmStub();
            algorithm.AddEquity("SPY");
            algorithm.SetDateTime(new DateTime(2026, 1, 15, 9, 30, 0));
            result.SetAlgorithm(algorithm, 10);

            var algorithmTimePrefix = algorithm.Time.ToStringInvariant(DateFormat.UI);

            result.Messages.Clear();
            result.DebugMessage("debug message");
            result.LogMessage("log message");
            result.ErrorMessage("error message");
            result.RuntimeError("runtime message");

            var messages = new List<string>()
            {
                result.Messages.OfType<DebugPacket>().Single().Message,
                result.Messages.OfType<LogPacket>().Single().Message,
                result.Messages.OfType<HandledErrorPacket>().Single().Message,
                result.Messages.OfType<RuntimeErrorPacket>().Single().Message
            };

            Assert.That(messages, Has.All.StartsWith(algorithmTimePrefix));
        }

        [Test]
        public void TrimChartsKeepsDailySampleOfStatisticsSeries()
        {
            var handler = new TestableLiveTradingResultHandler();
            var utcNow = new DateTime(2020, 11, 25, 12, 0, 0, DateTimeKind.Utc);

            var benchmarkChart = new Chart(BaseResultsHandler.BenchmarkKey);
            benchmarkChart.Series.Add(BaseResultsHandler.BenchmarkKey, new Series(BaseResultsHandler.BenchmarkKey));
            handler.Charts[BaseResultsHandler.BenchmarkKey] = benchmarkChart;

            var customChart = new Chart("MyCustomChart");
            customChart.Series.Add("MyMetric", new Series("MyMetric"));
            handler.Charts["MyCustomChart"] = customChart;

            var returnSeries = handler.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.ReturnKey];
            var equitySeries = handler.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.EquityKey];
            var benchmarkSeries = benchmarkChart.Series[BaseResultsHandler.BenchmarkKey];
            var customSeries = customChart.Series["MyMetric"];

            // Return and Benchmark: one point per day, going beyond 2 years
            for (var i = 800; i >= 1; i--)
            {
                var t = utcNow.AddDays(-i);
                returnSeries.Values.Add(new ChartPoint(t, i));
                benchmarkSeries.Values.Add(new ChartPoint(t, i));
            }

            // Equity: several points per day for older days, with varying OHLC so the high and low come from intraday candles
            foreach (var day in new[] { 5, 4, 3 })
            {
                var date = utcNow.AddDays(-day).Date;
                equitySeries.Values.Add(new Candlestick(date.AddHours(10), 100, 105, 98, 101));
                equitySeries.Values.Add(new Candlestick(date.AddHours(14), 101, 120, 99, 102));
                equitySeries.Values.Add(new Candlestick(date.AddHours(16), 102, 106, 85, 103));
            }
            // Two recent points within the 2 day window
            equitySeries.Values.Add(new Candlestick(utcNow.AddHours(-5), 200, 210, 195, 205));
            equitySeries.Values.Add(new Candlestick(utcNow.AddHours(-1), 205, 215, 200, 211));

            // Custom chart: not a statistics series, so no daily sample
            for (var i = 5; i >= 1; i--)
            {
                customSeries.Values.Add(new ChartPoint(utcNow.AddDays(-i), i));
            }

            handler.PublicTrimCharts(utcNow);

            // Return and Benchmark keep one point per day, up to 2 years
            var dailyStatsCutoff = utcNow.AddDays(-730);
            Assert.IsTrue(returnSeries.Values.All(v => v.Time > dailyStatsCutoff));
            Assert.IsTrue(benchmarkSeries.Values.All(v => v.Time > dailyStatsCutoff));
            Assert.AreEqual(729, returnSeries.Values.Count);
            Assert.AreEqual(729, benchmarkSeries.Values.Count);

            // Equity keeps all recent points and one aggregated candlestick per day for older ones
            Assert.AreEqual(5, equitySeries.Values.Count);
            foreach (var day in new[] { 5, 4, 3 })
            {
                var date = utcNow.AddDays(-day).Date;
                var samplesForDay = equitySeries.Values.Where(v => v.Time.Date == date).Cast<Candlestick>().ToList();
                Assert.AreEqual(1, samplesForDay.Count);
                // The whole day OHLC is aggregated, not just the last candle
                var candle = samplesForDay[0];
                Assert.AreEqual(100, candle.Open);
                Assert.AreEqual(120, candle.High);
                Assert.AreEqual(85, candle.Low);
                Assert.AreEqual(103, candle.Close);
            }

            // Recent points are kept at full resolution
            var recent = equitySeries.Values.Where(v => v.Time > utcNow.AddDays(-2)).Cast<Candlestick>().ToList();
            Assert.AreEqual(2, recent.Count);
            Assert.AreEqual(205, recent[0].Close);
            Assert.AreEqual(211, recent[1].Close);

            // Custom chart keeps only the last 2 days
            var defaultCutoff = utcNow.AddDays(-2);
            Assert.IsTrue(customSeries.Values.All(v => v.Time > defaultCutoff));
            Assert.AreEqual(1, customSeries.Values.Count);

            // Trimming runs repeatedly in production, so a second pass must leave the already trimmed series unchanged
            var equitySnapshot = equitySeries.Values.Cast<Candlestick>()
                .Select(v => (v.Time, v.Open, v.High, v.Low, v.Close)).ToList();
            handler.PublicTrimCharts(utcNow);
            Assert.AreEqual(729, returnSeries.Values.Count);
            Assert.AreEqual(729, benchmarkSeries.Values.Count);
            Assert.AreEqual(1, customSeries.Values.Count);
            CollectionAssert.AreEqual(equitySnapshot, equitySeries.Values.Cast<Candlestick>()
                .Select(v => (v.Time, v.Open, v.High, v.Low, v.Close)).ToList());
        }

        [Test]
        public void HoldingsChangeForcesResultsStoreOnceSettled()
        {
            var settleDelay = TimeSpan.FromSeconds(2);
            Config.Set("holdings-changed-store-delay", settleDelay.TotalSeconds.ToStringInvariant());
            using var api = new Api.Api();
            using var messaging = new QuantConnect.Messaging.Messaging();
            var resultHandler = new TestableForceStoreOnSettledHoldingsResultHandler();

            try
            {
                var algorithm = new AlgorithmStub(createDataManager: false);
                var dataManager = new DataManagerStub(new TestDataFeed(), algorithm);
                algorithm.SubscriptionManager.SetDataManager(dataManager);
                // live algorithms run on wall-clock time, which the order event times the monitor tracks are in sync with
                algorithm.SetDateTime(DateTime.UtcNow);
                // normally initialized by the setup handlers, required for statistics generation
                algorithm.Settings.TradingDaysPerYear = 365;
                // crypto markets are always open, so the market orders fill right away regardless of when the test runs
                var btc = algorithm.AddCrypto("BTCUSD", Resolution.Minute, Market.Coinbase);
                btc.SetFeeModel(new ConstantFeeModel(0));
                // the price time must be fresh relative to the order submission time (algorithm utc time) for the fills to happen
                btc.SetMarketPrice(new TradeBar(algorithm.UtcTime.ConvertFromUtc(btc.Exchange.TimeZone), btc.Symbol, 100, 100, 100, 100, 100));
                algorithm.PostInitialize();

                var transactionHandler = new BacktestingTransactionHandler();
                using var brokerage = new BacktestingBrokerage(algorithm);
                transactionHandler.Initialize(algorithm, brokerage, resultHandler);
                algorithm.Transactions.SetOrderProcessor(transactionHandler);

                resultHandler.Initialize(new(new LiveNodePacket(), messaging, api, transactionHandler, null));
                resultHandler.SetAlgorithm(algorithm, 100000);
                algorithm.SetLocked();

                // places an order for the given quantity, changing the holdings when it fills
                OrderTicket Trade(decimal quantity, OrderType orderType = OrderType.Market, decimal limitPrice = 0)
                {
                    // keep the algorithm clock in sync with wall-clock time like in live trading,
                    // so the order events are stamped with current times
                    algorithm.SetDateTime(DateTime.UtcNow);
                    var ticket = algorithm.Transactions.ProcessRequest(new SubmitOrderRequest(orderType, btc.Symbol.SecurityType,
                        btc.Symbol, quantity, 0, limitPrice, algorithm.UtcTime, string.Empty));
                    brokerage.Scan();
                    return ticket;
                }

                var expectedStores = 1;
                // the first update pass always stores because the scheduled store is due right away
                Assert.IsTrue(resultHandler.WaitForStore(expectedStores, TimeSpan.FromSeconds(30)), "Initial scheduled store did not happen");

                // without holdings changes, no store is forced even after the settle delay elapses
                Assert.IsFalse(resultHandler.WaitForStore(expectedStores + 1, settleDelay + TimeSpan.FromSeconds(1)), "A store happened without holdings changes");

                // a position is opened. The stopwatch is started before trading so the elapsed time
                // is measured from no later than the order fill time
                var stopwatch = Stopwatch.StartNew();
                Trade(10);

                // no store is forced before the holdings settle
                Assert.IsFalse(resultHandler.WaitForStore(expectedStores + 1, TimeSpan.FromSeconds(1)), "A store was forced before the holdings settled");
                // but once they settle, a store is forced without waiting for the scheduled one
                Assert.IsTrue(resultHandler.WaitForStore(++expectedStores, settleDelay + TimeSpan.FromSeconds(5)), "No store was forced after the opened position settled");
                Assert.GreaterOrEqual(stopwatch.Elapsed, settleDelay);

                // the position is increased
                Trade(10);
                Assert.IsTrue(resultHandler.WaitForStore(++expectedStores, settleDelay + TimeSpan.FromSeconds(5)), "No store was forced after the increased position settled");

                // the position is reduced
                Trade(-5);
                Assert.IsTrue(resultHandler.WaitForStore(++expectedStores, settleDelay + TimeSpan.FromSeconds(5)), "No store was forced after the reduced position settled");

                // order events without fills don't force stores: a limit order far from the market price
                // is submitted and then canceled without ever changing the holdings
                var ticket = Trade(1, OrderType.Limit, limitPrice: 1);
                ticket.Cancel();
                Assert.IsFalse(resultHandler.WaitForStore(expectedStores + 1, settleDelay + TimeSpan.FromSeconds(1)), "A store was forced for order events without fills");

                // the position is liquidated
                Trade(-15);
                Assert.IsTrue(resultHandler.WaitForStore(++expectedStores, settleDelay + TimeSpan.FromSeconds(5)), "No store was forced after the liquidation settled");

                // a single settled change forces a single store
                Assert.IsFalse(resultHandler.WaitForStore(expectedStores + 1, settleDelay + TimeSpan.FromSeconds(1)), "More than one store was forced for a single holdings change");
            }
            finally
            {
                resultHandler.Exit();
                Config.Reset();
            }
        }

        private class TestableLiveTradingResultHandler : LiveTradingResultHandler
        {
            public void PublicTrimCharts(DateTime utcNow) => TrimCharts(utcNow);
        }

        private class TestableForceStoreOnSettledHoldingsResultHandler : LiveTradingResultHandler
        {
            private int _storeCount;

            // speed up the update loop so the test can use short settle delays
            protected override TimeSpan MainUpdateInterval => TimeSpan.FromMilliseconds(100);

            public TestableForceStoreOnSettledHoldingsResultHandler()
            {
                // keep the scheduled store out of the way so only forced stores happen after the initial one
                ChartUpdateInterval = TimeSpan.FromMinutes(10);
            }

            public bool WaitForStore(int count, TimeSpan timeout)
            {
                var start = DateTime.UtcNow;
                while (DateTime.UtcNow - start < timeout)
                {
                    if (Interlocked.CompareExchange(ref _storeCount, 0, 0) >= count)
                    {
                        return true;
                    }
                    Thread.Sleep(50);
                }
                return Interlocked.CompareExchange(ref _storeCount, 0, 0) >= count;
            }

            protected override void StoreResult(Packet packet)
            {
                Interlocked.Increment(ref _storeCount);
            }

            public override string SaveLogs(string id, List<LogEntry> logs)
            {
                return string.Empty;
            }
        }

        private class TestDataFeed : IDataFeed
        {
            public bool IsActive { get; }

            public void Initialize(
                IAlgorithm algorithm,
                AlgorithmNodePacket job,
                IResultHandler resultHandler,
                IMapFileProvider mapFileProvider,
                IFactorFileProvider factorFileProvider,
                IDataProvider dataProvider,
                IDataFeedSubscriptionManager subscriptionManager,
                IDataFeedTimeProvider dataFeedTimeProvider,
                IDataChannelProvider dataChannelProvider
                )
            {
            }
            public Subscription CreateSubscription(SubscriptionRequest request)
            {
                return null;
            }
            public void RemoveSubscription(Subscription subscription)
            {
            }
            public void Exit()
            {
            }
        }
    }
}
