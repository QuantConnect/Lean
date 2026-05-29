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
using NUnit.Framework;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Data.Custom.IconicTypes;

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
        public void TrimChartsUsesLongerWindowForPerformanceCharts()
        {
            var handler = new TestableLiveTradingResultHandler();
            var utcNow = new DateTime(2020, 11, 25, 12, 0, 0, DateTimeKind.Utc);

            var benchmarkChart = new Chart(BaseResultsHandler.BenchmarkKey);
            benchmarkChart.Series.Add(BaseResultsHandler.BenchmarkKey, new Series(BaseResultsHandler.BenchmarkKey));
            handler.Charts[BaseResultsHandler.BenchmarkKey] = benchmarkChart;

            // Add a custom user chart to verify it still uses the 2 day window 
            var customChart = new Chart("MyCustomChart");
            customChart.Series.Add("MyMetric", new Series("MyMetric"));
            handler.Charts["MyCustomChart"] = customChart;

            var returnSeries = handler.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.ReturnKey];
            var equitySeries = handler.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.EquityKey];
            var benchmarkSeries = benchmarkChart.Series[BaseResultsHandler.BenchmarkKey];
            var customSeries = customChart.Series["MyMetric"];

            // performance charts: 15 daily samples covering well beyond both trim windows
            for (var i = 15; i >= 1; i--)
            {
                var t = utcNow.AddDays(-i);
                returnSeries.Values.Add(new ChartPoint(t, i));
                benchmarkSeries.Values.Add(new ChartPoint(t, i));
                equitySeries.Values.Add(new Candlestick(t, 100, 110, 90, 105));
            }

            // custom chart: 5 samples to verify it uses the 2-day window
            for (var i = 5; i >= 1; i--)
            {
                customSeries.Values.Add(new ChartPoint(utcNow.AddDays(-i), i));
            }

            handler.PublicTrimCharts(utcNow);

            // performance charts keep 10 day window
            var performanceChartsCutoff = utcNow.AddDays(-10);
            Assert.IsTrue(returnSeries.Values.All(v => v.Time > performanceChartsCutoff));
            Assert.IsTrue(benchmarkSeries.Values.All(v => v.Time > performanceChartsCutoff));
            Assert.IsTrue(equitySeries.Values.All(v => v.Time > performanceChartsCutoff));
            Assert.AreEqual(9, returnSeries.Values.Count);
            Assert.AreEqual(9, benchmarkSeries.Values.Count);

            // other charts keep 2 day window
            var otherChartsCutoff = utcNow.AddDays(-2);
            Assert.IsTrue(customSeries.Values.All(v => v.Time > otherChartsCutoff));
            Assert.AreEqual(1, customSeries.Values.Count);
        }

        private class TestableLiveTradingResultHandler : LiveTradingResultHandler
        {
            public void PublicTrimCharts(DateTime utcNow) => TrimCharts(utcNow);
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
