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
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Category("TravisExclude")]
    public class SubscriptionSynchronizerTests
    {
        [Test]
        [TestCase(1, Resolution.Second)]
        [TestCase(20, Resolution.Minute)]
        [TestCase(50, Resolution.Minute)]
        [TestCase(100, Resolution.Minute)]
        [TestCase(250, Resolution.Minute)]
        [TestCase(500, Resolution.Hour)]
        [TestCase(1000, Resolution.Hour)]
        public void SubscriptionSynchronizerPerformance(int securityCount, Resolution resolution)
        {
            // since data is pre-generated, it's important to use the larger resolutions with large security counts

            var algorithm = PerformanceBenchmarkAlgorithms.CreateBenchmarkAlgorithm(securityCount, resolution);
            TestSubscriptionSynchronizerSpeed(algorithm);
        }

        private void TestSubscriptionSynchronizerSpeed(QCAlgorithm algorithm)
        {
            var feed = new AlgorithmManagerTests.MockDataFeed();
            var dataManager = new DataManager(feed, new UniverseSelection(feed, algorithm), algorithm.Settings, algorithm.TimeKeeper);
            algorithm.SubscriptionManager.SetDataManager(dataManager);

            algorithm.Initialize();
            algorithm.PostInitialize();

            // set exchanges to be always open
            foreach (var kvp in algorithm.Securities)
            {
                var security = kvp.Value;
                security.Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(security.Exchange.TimeZone));
            }

            var endTimeUtc = algorithm.EndDate.ConvertToUtc(TimeZones.NewYork);
            var startTimeUtc = algorithm.StartDate.ConvertToUtc(TimeZones.NewYork);
            var subscriptionBasedTimeProvider = new SubscriptionFrontierTimeProvider(startTimeUtc, dataManager);
            var synchronizer = new SubscriptionSynchronizer(dataManager.UniverseSelection, algorithm.TimeZone,
                                                            algorithm.Portfolio.CashBook,
                                                            subscriptionBasedTimeProvider);

            var totalDataPoints = 0;
            var subscriptions = dataManager.DataFeedSubscriptions;
            foreach (var kvp in algorithm.Securities)
            {
                int dataPointCount;
                subscriptions.TryAdd(CreateSubscription(algorithm, kvp.Value, startTimeUtc, endTimeUtc, out dataPointCount));
                totalDataPoints += dataPointCount;
            }

            // force JIT
            synchronizer.Sync(subscriptions);

            // log what we're doing
            Console.WriteLine($"Running {subscriptions.Count()} subscriptions with a total of {totalDataPoints} data points. Start: {algorithm.StartDate:yyyy-MM-dd} End: {algorithm.EndDate:yyyy-MM-dd}");

            var count = 0;
            DateTime currentTime = DateTime.MaxValue;
            DateTime previousValue;
            var stopwatch = Stopwatch.StartNew();
            do
            {
                previousValue = currentTime;
                var timeSlice = synchronizer.Sync(subscriptions);
                currentTime = timeSlice.Time;
                count += timeSlice.DataPointCount;
            }
            while (currentTime != previousValue);

            stopwatch.Stop();

            var kps = count / 1000d / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Current Time: {currentTime:u}  Elapsed time: {(int)stopwatch.Elapsed.TotalSeconds,4}s  KPS: {kps,7:.00}  COUNT: {count,10}");
            Assert.GreaterOrEqual(count, 100); // this assert is for sanity purpose
        }

        private Subscription CreateSubscription(QCAlgorithm algorithm, Security security, DateTime startTimeUtc, DateTime endTimeUtc, out int dataPointCount)
        {
            var universe = algorithm.UniverseManager.Values.OfType<UserDefinedUniverse>()
                .Single(u => u.SelectSymbols(default(DateTime), null).Contains(security.Symbol));

            var config = security.Subscriptions.First();
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.NewYork, startTimeUtc, endTimeUtc);
            var data = LinqExtensions.Range(algorithm.StartDate, algorithm.EndDate, c => c + config.Increment).Select(time => new DataPoint
            {
                Time = time,
                EndTime = time + config.Increment
            })
            .Select(d => SubscriptionData.Create(config, security.Exchange.Hours, offsetProvider, d))
            .ToList();

            dataPointCount = data.Count;
            return new Subscription(universe, security, config, data.GetEnumerator(), offsetProvider, endTimeUtc, endTimeUtc, false);
        }

        private class DataPoint : BaseData
        {
            // bare bones base data to minimize memory footprint
        }
    }
}