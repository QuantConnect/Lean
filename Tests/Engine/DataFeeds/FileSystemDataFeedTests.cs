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
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Category("TravisExclude")]
    public class FileSystemDataFeedTests
    {
        [Test]
        public void TestsFileSystemDataFeedSpeed()
        {
            var job = new BacktestNodePacket();
            var resultHandler = new BacktestingResultHandler();
            var mapFileProvider = new LocalDiskMapFileProvider();
            var factorFileProvider = new LocalDiskFactorFileProvider(mapFileProvider);
            var dataProvider = new DefaultDataProvider();

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
                    new DefaultDataProvider()),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                false,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);
            algorithm.SubscriptionManager.SetDataManager(dataManager);
            var synchronizer = new Synchronizer();
            synchronizer.Initialize(algorithm, dataManager);

            feed.Initialize(algorithm, job, resultHandler, mapFileProvider, factorFileProvider, dataProvider, dataManager, synchronizer, dataPermissionManager.DataChannelProvider);
            algorithm.Initialize();
            algorithm.PostInitialize();

            var cancellationTokenSource = new CancellationTokenSource();
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

            var dataProvider = new DefaultDataProvider();
            var resultHandler = new BacktestingResultHandler();
            var mapFileProvider = new LocalDiskMapFileProvider();
            var factorFileProvider = new LocalDiskFactorFileProvider(mapFileProvider);
            var factory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(resultHandler, mapFileProvider, factorFileProvider, dataProvider, true, enablePriceScaling: false);

            var universe = algorithm.UniverseManager.Single().Value;
            var security = algorithm.Securities.Single().Value;
            var securityConfig = security.Subscriptions.First();
            var subscriptionRequest = new SubscriptionRequest(false, universe, security, securityConfig, algorithm.StartDate, algorithm.EndDate);
            var enumerator = factory.CreateEnumerator(subscriptionRequest, dataProvider);

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

            var dataProvider = new DefaultDataProvider();
            var resultHandler = new TestResultHandler();
            var mapFileProvider = new LocalDiskMapFileProvider();
            var factorFileProvider = new LocalDiskFactorFileProvider(mapFileProvider);
            var factory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(resultHandler, mapFileProvider, factorFileProvider, dataProvider, true, enablePriceScaling: false);

            var universe = algorithm.UniverseManager.Single().Value;
            var security = algorithm.AddEquity("AAA", Resolution.Daily);
            var securityConfig = security.Subscriptions.First();
            // start date is before the first date in the map file
            var subscriptionRequest = new SubscriptionRequest(false, universe, security, securityConfig, new DateTime(2001, 12, 1),
                new DateTime(2016, 11, 1));
            var enumerator = factory.CreateEnumerator(subscriptionRequest, dataProvider);
            // should initialize the data source reader
            enumerator.MoveNext();

            enumerator.Dispose();
            factory.DisposeSafely();
            resultHandler.Exit();

            var message = ((DebugPacket) resultHandler.Messages.Single()).Message;
            Assert.IsTrue(message.Equals(
                "The starting date for symbol AAA, 2001-11-30, has been adjusted to match map file first date 2020-09-09."));
        }
    }
}
