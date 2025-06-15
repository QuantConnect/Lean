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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class FileSystemDataFeedTests
    {
        [Test]
        public void TestsFileSystemDataFeedSpeed()
        {
            var job = new BacktestNodePacket();
            var resultHandler = new BacktestingResultHandler();

            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            var feed = new FileSystemDataFeed();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(feed,
                new UniverseSelection(
                    algorithm,
                    new SecurityService(algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, algorithm, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(algorithm.Portfolio), algorithm: algorithm),
                    dataPermissionManager,
                    TestGlobals.DataProvider),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                false,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);
            algorithm.SubscriptionManager.SetDataManager(dataManager);
            using var synchronizer = new Synchronizer();
            synchronizer.Initialize(algorithm, dataManager);

            feed.Initialize(algorithm, job, resultHandler, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, TestGlobals.DataProvider, dataManager, synchronizer, dataPermissionManager.DataChannelProvider);
            algorithm.Initialize();
            algorithm.PostInitialize();

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var count = 0;
            var stopwatch = Stopwatch.StartNew();
            var lastMonth = algorithm.StartDate.Month;
            foreach (var timeSlice in synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (timeSlice.Time.Month != lastMonth)
                {
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    var thousands = count / 1000d;
                    Log.Trace($"{DateTime.Now} - Time: {timeSlice.Time}: KPS: {thousands / elapsed}");
                    lastMonth = timeSlice.Time.Month;
                }
                count++;
            }
            Log.Trace("Count: " + count);
            stopwatch.Stop();
            feed.Exit();
            dataManager.RemoveAllSubscriptions();
            Log.Trace($"Elapsed time: {stopwatch.Elapsed}   KPS: {count / 1000d / stopwatch.Elapsed.TotalSeconds}");
        }

        [Test]
        public void TestDataFeedEnumeratorStackSpeed()
        {
            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            algorithm.Initialize();
            algorithm.PostInitialize();

            var resultHandler = new BacktestingResultHandler();
            using var factory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(resultHandler, TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider, TestGlobals.DataCacheProvider, algorithm, enablePriceScaling: false);

            var universe = algorithm.UniverseManager.Single().Value;
            var security = algorithm.Securities.Single().Value;
            var securityConfig = security.Subscriptions.First();
            var subscriptionRequest = new SubscriptionRequest(false, universe, security, securityConfig, algorithm.StartDate, algorithm.EndDate);
            var enumerator = factory.CreateEnumerator(subscriptionRequest, TestGlobals.DataProvider);

            var count = 0;
            var stopwatch = Stopwatch.StartNew();
            var lastMonth = algorithm.StartDate.Month;
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (current == null)
                {
                    Log.Trace("ERROR: Current is null");
                    continue;
                }

                if (current.Time.Month != lastMonth)
                {
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    var thousands = count / 1000d;
                    Log.Trace($"{DateTime.Now} - Time: {current.Time}: KPS: {thousands / elapsed}");
                    lastMonth = current.Time.Month;
                }
                count++;
            }
            Log.Trace("Count: " + count);

            stopwatch.Stop();
            enumerator.Dispose();
            factory.DisposeSafely();
            Log.Trace($"Elapsed time: {stopwatch.Elapsed}   KPS: {count / 1000d / stopwatch.Elapsed.TotalSeconds}");
        }

        [Test]
        public void ChecksMapFileFirstDate()
        {
            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            algorithm.Initialize();
            algorithm.PostInitialize();

            var resultHandler = new TestResultHandler();
            using var factory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(resultHandler, TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider, TestGlobals.DataCacheProvider, algorithm, enablePriceScaling: false);

            var universe = algorithm.UniverseManager.Single().Value;
            var security = algorithm.AddEquity("AAA", Resolution.Daily);
            var securityConfig = security.Subscriptions.First();
            // start date is before the first date in the map file
            var subscriptionRequest = new SubscriptionRequest(false, universe, security, securityConfig, new DateTime(2001, 12, 1),
                new DateTime(2016, 11, 1));
            var enumerator = factory.CreateEnumerator(subscriptionRequest, TestGlobals.DataProvider);
            // should initialize the data source reader
            enumerator.MoveNext();

            enumerator.Dispose();
            factory.DisposeSafely();
            resultHandler.Exit();

            var message = ((DebugPacket)resultHandler.Messages.Single()).Message;
            Assert.IsTrue(message.Equals(
                "The starting dates for the following symbols have been adjusted to match their map files first date: [AAA, 2020-09-09]"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OptionChainEnumerator(bool fillForward)
        {
            var job = new BacktestNodePacket();
            var resultHandler = new BacktestingResultHandler();
            var feed = new FileSystemDataFeed();
            var algorithm = new AlgorithmStub(feed);
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            algorithm.SetStartDate(new DateTime(2014, 06, 06));
            algorithm.SetEndDate(new DateTime(2014, 06, 09));

            var optionChainProvider = new BacktestingOptionChainProvider();
            optionChainProvider.Initialize(new(TestGlobals.MapFileProvider, TestGlobals.HistoryProvider));
            algorithm.SetOptionChainProvider(optionChainProvider);

            var dataPermissionManager = new DataPermissionManager();
            using var synchronizer = new Synchronizer();
            synchronizer.Initialize(algorithm, algorithm.DataManager);

            feed.Initialize(algorithm, job, resultHandler, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, TestGlobals.DataProvider, algorithm.DataManager, synchronizer, dataPermissionManager.DataChannelProvider);
            var option = algorithm.AddOption("AAPL", fillForward: fillForward);
            option.SetFilter(filter => filter.FrontMonth());
            algorithm.PostInitialize();

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var count = 0;
            var lastMonth = algorithm.StartDate.Month;
            foreach (var timeSlice in synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (!timeSlice.IsTimePulse && timeSlice.UniverseData?.Count > 0 && timeSlice.Time.Date <= algorithm.EndDate)
                {
                    var baseDataCollection = timeSlice.UniverseData.Where(x => x.Key is OptionChainUniverse).SingleOrDefault().Value;
                    if (baseDataCollection != null)
                    {
                        var nyTime = timeSlice.Time.ConvertFromUtc(algorithm.TimeZone);
                        Assert.AreEqual(new TimeSpan(0, 0, 0), nyTime.TimeOfDay, $"Failed on: {nyTime}");

                        Assert.AreEqual(nyTime.TimeOfDay, baseDataCollection.EndTime.ConvertFromUtc(algorithm.TimeZone).TimeOfDay);
                        Assert.IsNotNull(baseDataCollection.FilteredContracts);
                        CollectionAssert.IsNotEmpty(baseDataCollection.FilteredContracts);
                        count++;
                    }
                }
            }
            feed.Exit();
            algorithm.DataManager.RemoveAllSubscriptions();

            // 2 tradable dates between 2014-06-06 and 2014-06-09 (the 6th and 9th)
            Assert.AreEqual(2, count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FutureChainEnumerator(bool fillForward)
        {
            var job = new BacktestNodePacket();
            var resultHandler = new BacktestingResultHandler();
            var feed = new FileSystemDataFeed();
            var algorithm = new AlgorithmStub(feed);
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            algorithm.SetStartDate(new DateTime(2013, 10, 07));
            algorithm.SetEndDate(new DateTime(2013, 10, 08));

            var optionChainProvider = new BacktestingOptionChainProvider();
            optionChainProvider.Initialize(new(TestGlobals.MapFileProvider, TestGlobals.HistoryProvider));
            algorithm.SetOptionChainProvider(optionChainProvider);

            var dataPermissionManager = new DataPermissionManager();
            using var synchronizer = new Synchronizer();
            synchronizer.Initialize(algorithm, algorithm.DataManager);

            feed.Initialize(algorithm, job, resultHandler, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, TestGlobals.DataProvider,
                algorithm.DataManager, synchronizer, dataPermissionManager.DataChannelProvider);
            var future = algorithm.AddFuture("ES", fillForward: fillForward, extendedMarketHours: true);
            future.SetFilter(0, 300);
            algorithm.PostInitialize();

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var count = 0L;
            var lastMonth = algorithm.StartDate.Month;
            foreach (var timeSlice in synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (!timeSlice.IsTimePulse && timeSlice.UniverseData?.Count > 0 && timeSlice.Time.Date <= algorithm.EndDate)
                {
                    var nyTime = timeSlice.Time.ConvertFromUtc(algorithm.TimeZone);
                    var universeData = timeSlice.UniverseData;
                    var chainData = universeData.Where(x => x.Key is FuturesChainUniverse).Single().Value;

                    Log.Trace($"{nyTime}. Count: {count}. Universe Data Count {universeData.Count}");
                    Assert.AreEqual(TimeSpan.Zero, nyTime.TimeOfDay, $"Failed on: {nyTime}. Count: {count}");
                    Assert.IsTrue(timeSlice.UniverseData.All(kvp => kvp.Value.EndTime.ConvertFromUtc(algorithm.TimeZone).TimeOfDay == nyTime.TimeOfDay));
                    if (chainData.FilteredContracts.IsNullOrEmpty())
                    {
                        Assert.AreEqual(new DateTime(2013, 10, 09), nyTime, $"Unexpected chain FilteredContracts was empty on {nyTime}");
                    }

                    if (universeData.Count == 1)
                    {
                        // the chain
                        Assert.IsTrue(universeData.Any(kvp => kvp.Key.Configuration.Symbol == future.Symbol));
                    }
                    else
                    {
                        // we have 2 universe data, the chain and the continuous future
                        Assert.AreEqual(2, universeData.Count);
                        Assert.IsTrue(universeData.All(kvp => kvp.Key.Configuration.Symbol.SecurityType == SecurityType.Future));
                        Assert.IsTrue(universeData.Any(kvp => kvp.Key.Configuration.Symbol == future.Symbol));
                        Assert.IsTrue(universeData.Any(kvp => kvp.Key.Configuration.Symbol.ID.Symbol.Contains("CONTINUOUS", StringComparison.InvariantCultureIgnoreCase)));

                        var continuousData = universeData.Where(x => x.Key is ContinuousContractUniverse).Single().Value;
                        Assert.AreEqual(TimeSpan.Zero, nyTime.TimeOfDay, $"Failed on: {nyTime}");
                        Assert.IsTrue(!chainData.FilteredContracts.IsNullOrEmpty());
                    }

                    count++;
                }
            }
            feed.Exit();
            algorithm.DataManager.RemoveAllSubscriptions();

            // 2 tradable days
            Assert.AreEqual(2, count);
        }

        [Test]
        public void ContinuousFutureUniverseSelectionIsPerformedOnExtendedMarketHoursDates([Values] bool extendedMarketHours)
        {
            var job = new BacktestNodePacket();
            var resultHandler = new BacktestingResultHandler();
            var feed = new FileSystemDataFeed();
            var algorithm = new AlgorithmStub(feed);
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            algorithm.SetStartDate(new DateTime(2019, 08, 01));
            algorithm.SetEndDate(new DateTime(2019, 08, 08));

            var dataPermissionManager = new DataPermissionManager();
            using var synchronizer = new Synchronizer();
            synchronizer.Initialize(algorithm, algorithm.DataManager);

            feed.Initialize(algorithm, job, resultHandler, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, TestGlobals.DataProvider,
                algorithm.DataManager, synchronizer, dataPermissionManager.DataChannelProvider);
            var future = algorithm.AddFuture("GC", Resolution.Daily, extendedMarketHours: extendedMarketHours);
            algorithm.PostInitialize();

            var addedSecurities = new HashSet<Symbol>();
            var mappingCounts = 0;

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            foreach (var timeSlice in synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (timeSlice.IsTimePulse) continue;

                var addedSymbols = timeSlice.SecurityChanges.AddedSecurities.Select(x => x.Symbol).ToHashSet();

                if (timeSlice.Slice.SymbolChangedEvents.TryGetValue(future.Symbol, out var symbolChangedEvent))
                {
                    mappingCounts++;
                    var oldSymbol = algorithm.Symbol(symbolChangedEvent.OldSymbol);
                    var newSymbol = algorithm.Symbol(symbolChangedEvent.NewSymbol);

                    Assert.IsTrue(addedSecurities.Contains(oldSymbol));

                    Assert.IsTrue(addedSymbols.Contains(newSymbol));
                }

                addedSecurities.UnionWith(addedSymbols);
            }

            feed.Exit();
            algorithm.DataManager.RemoveAllSubscriptions();

            var expectedMappingCounts = extendedMarketHours ? 2 : 1;
            Assert.AreEqual(expectedMappingCounts, mappingCounts);
        }

        [Test]
        public void DataIsFillForwardedFromWarmupToNormalFeed()
        {
            var job = new BacktestNodePacket();
            var resultHandler = new BacktestingResultHandler();
            var feed = new FileSystemDataFeed();
            var algorithm = new AlgorithmStub(feed);
            algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            algorithm.SetStartDate(new DateTime(2013, 10, 15));
            algorithm.SetEndDate(new DateTime(2013, 10, 16));

            var dataPermissionManager = new DataPermissionManager();
            using var synchronizer = new Synchronizer();
            synchronizer.Initialize(algorithm, algorithm.DataManager);

            feed.Initialize(algorithm, job, resultHandler, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, TestGlobals.DataProvider, algorithm.DataManager, synchronizer, dataPermissionManager.DataChannelProvider);
            var equity = algorithm.AddEquity("SPY", fillForward: true, dataNormalizationMode: DataNormalizationMode.Raw);
            algorithm.SetWarmup(1000);
            algorithm.PostInitialize();

            QuoteBar lastWarmupQuoteBar = null;
            TradeBar lastWarmupTradeBar = null;
            QuoteBar lastQuoteBar = null;
            TradeBar lastTradeBar = null;

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            foreach (var timeSlice in synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (!timeSlice.IsTimePulse && timeSlice.Time.Date <= algorithm.EndDate)
                {
                    Assert.IsTrue(timeSlice.Slice.QuoteBars.TryGetValue(equity.Symbol, out var quoteBar));
                    Assert.IsTrue(timeSlice.Slice.Bars.TryGetValue(equity.Symbol, out var tradeBar));

                    if (timeSlice.Slice.Time <= algorithm.StartDate)
                    {
                        lastWarmupQuoteBar = quoteBar;
                        lastWarmupTradeBar = tradeBar;
                    }
                    else
                    {
                        lastQuoteBar = quoteBar;
                        lastTradeBar = tradeBar;

                        // We don't have local data for the start-end range, so we expect all data to be fill-forwarded
                        Assert.IsTrue(lastQuoteBar.IsFillForward);
                        Assert.IsTrue(lastTradeBar.IsFillForward);
                    }

                }
            }
            feed.Exit();
            algorithm.DataManager.RemoveAllSubscriptions();

            // Assert we actually got warmup data
            Assert.IsNotNull(lastWarmupQuoteBar);
            Assert.IsNotNull(lastWarmupTradeBar);

            // Assert we got normal data
            Assert.IsNotNull(lastQuoteBar);
            Assert.IsNotNull(lastTradeBar);
        }
    }
}
