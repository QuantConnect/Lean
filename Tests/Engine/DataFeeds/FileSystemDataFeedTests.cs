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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
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
                    new SecurityService(algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, algorithm, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(algorithm.Portfolio)),
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
            algorithm.SetOptionChainProvider(new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider));

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
                if (!timeSlice.IsTimePulse && timeSlice.UniverseData?.Count > 0)
                {
                    var baseDataCollection = timeSlice.UniverseData.Single().Value;
                    if (baseDataCollection.Symbol.SecurityType == SecurityType.Option)
                    {
                        var nyTime = timeSlice.Time.ConvertFromUtc(algorithm.TimeZone);
                        Assert.AreEqual(new TimeSpan(9, 30, 0).Add(TimeSpan.FromMinutes((count % 390) + 1)), nyTime.TimeOfDay, $"Failed on: {nyTime}");
                        Assert.IsNotNull(baseDataCollection.Underlying);
                        // make sure the underlying time stamp is getting updated
                        Assert.AreEqual(nyTime.TimeOfDay, baseDataCollection.Underlying.EndTime.TimeOfDay);
                        Assert.AreEqual(nyTime.TimeOfDay, baseDataCollection.EndTime.ConvertFromUtc(algorithm.TimeZone).TimeOfDay);
                        Assert.IsTrue(!baseDataCollection.FilteredContracts.IsNullOrEmpty());
                        count++;
                    }
                }
            }
            feed.Exit();
            algorithm.DataManager.RemoveAllSubscriptions();

            // 9:30 to 15:59 -> 6.5 hours * 60 => 390 minutes * 2 days = 780
            Assert.AreEqual(780, count);
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
            algorithm.SetFutureChainProvider(new BacktestingFutureChainProvider(TestGlobals.DataCacheProvider));

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
                if (!timeSlice.IsTimePulse && timeSlice.UniverseData?.Count > 0)
                {
                    var nyTime = timeSlice.Time.ConvertFromUtc(algorithm.TimeZone);

                    var currentExpectedTime = new TimeSpan(0, 0, 0).Add(TimeSpan.FromMinutes(count % (24 * 60)));
                    while (!future.Exchange.Hours.IsOpen(nyTime.Date.Add(currentExpectedTime).AddMinutes(-1), true))
                    {
                        // skip closed market times
                        currentExpectedTime = new TimeSpan(0, 0, 0).Add(TimeSpan.FromMinutes(++count % (24 * 60)));
                    }
                    var universeData = timeSlice.UniverseData.OrderBy(kvp => kvp.Key.Configuration.Symbol).ToList();

                    var chainData = universeData[0].Value;

                    Log.Trace($"{nyTime}. Count: {count}. Universe Data Count {universeData.Count}");
                    Assert.AreEqual(currentExpectedTime, nyTime.TimeOfDay, $"Failed on: {nyTime}. Count: {count}");
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

                        var continuousData = universeData[1].Value;
                        Assert.AreEqual(currentExpectedTime, nyTime.TimeOfDay, $"Failed on: {nyTime}");
                        Assert.IsTrue(!chainData.FilteredContracts.IsNullOrEmpty());
                    }

                    count++;
                }
            }
            feed.Exit();
            algorithm.DataManager.RemoveAllSubscriptions();

            // 2 days worth of minute data
            Assert.AreEqual(24 * 2 * 60 + 1, count);
        }
    }
}
