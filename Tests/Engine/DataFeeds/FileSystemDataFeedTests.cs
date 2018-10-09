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
using NUnit.Framework;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;

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
            var dataManager = new DataManager(feed,
                new UniverseSelection(feed,
                    algorithm,
                    new SecurityService(algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, algorithm)),
                algorithm.Settings,
                algorithm.TimeKeeper,
                marketHoursDatabase);
            algorithm.SubscriptionManager.SetDataManager(dataManager);

            feed.Initialize(algorithm, job, resultHandler, mapFileProvider, factorFileProvider, dataProvider, dataManager);
            algorithm.Initialize();
            algorithm.PostInitialize();

            var count = 0;
            var stopwatch = Stopwatch.StartNew();
            var lastMonth = algorithm.StartDate.Month;
            foreach (var timeSlice in feed)
            {
                if (timeSlice.Time.Month != lastMonth)
                {
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    var thousands = count / 1000d;
                    Console.WriteLine($"{DateTime.Now} - Time: {timeSlice.Time}: KPS: {thousands/elapsed}");
                    lastMonth = timeSlice.Time.Month;
                }
                count++;
            }
            Console.WriteLine("Count: " + count);
            stopwatch.Stop();
            feed.Exit();
            Console.WriteLine($"Elapsed time: {stopwatch.Elapsed}   KPS: {count/1000d/stopwatch.Elapsed.TotalSeconds}");
        }

        [Test]
        public void TestDataFeedEnumeratorStackSpeed()
        {
            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.Initialize();
            algorithm.PostInitialize();

            var dataProvider = new DefaultDataProvider();
            var resultHandler = new BacktestingResultHandler();
            var mapFileProvider = new LocalDiskMapFileProvider();
            var factorFileProvider = new LocalDiskFactorFileProvider(mapFileProvider);
            var factory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(resultHandler, mapFileProvider, factorFileProvider, dataProvider, false, true);

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
                    Console.WriteLine("ERROR: Current is null");
                    continue;
                }

                if (current.Time.Month != lastMonth)
                {
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    var thousands = count / 1000d;
                    Console.WriteLine($"{DateTime.Now} - Time: {current.Time}: KPS: {thousands / elapsed}");
                    lastMonth = current.Time.Month;
                }
                count++;
            }
            Console.WriteLine("Count: " + count);

            stopwatch.Stop();
            Console.WriteLine($"Elapsed time: {stopwatch.Elapsed}   KPS: {count / 1000d / stopwatch.Elapsed.TotalSeconds}");
        }
    }
}
