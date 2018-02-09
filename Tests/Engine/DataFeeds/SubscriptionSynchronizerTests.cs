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
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Category("TravisExclude")]
    public class SubscriptionSynchronizerTests
    {
        [Test]
        public void TestsSubscriptionSynchronizerSpeed_SingleSubscription()
        {
            TestSubscriptionSynchronizerSpeed(PerformanceBenchmarkAlgorithms.SingleSecurity_Second);
        }

        [Test]
        public void TestsSubscriptionSynchronizerSpeed_500Subscription()
        {
            TestSubscriptionSynchronizerSpeed(PerformanceBenchmarkAlgorithms.FiveHundredSecurity_Second);
        }

        private void TestSubscriptionSynchronizerSpeed(QCAlgorithm algorithm)
        {
            algorithm.Initialize();

            // set exchanges to be always open
            foreach (var kvp in algorithm.Securities)
            {
                var security = kvp.Value;
                security.Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(security.Exchange.TimeZone));
            }

            var endTimeUtc = algorithm.EndDate.ConvertToUtc(TimeZones.NewYork);
            var startTimeUtc = algorithm.StartDate.ConvertToUtc(TimeZones.NewYork);

            var feed = new AlgorithmManagerTests.MockDataFeed();
            var universeSelection = new UniverseSelection(feed, algorithm);
            var synchronizer = new SubscriptionSynchronizer(universeSelection, algorithm.TimeZone, algorithm.Portfolio.CashBook, startTimeUtc);

            var subscriptions = new List<Subscription>();
            foreach (var kvp in algorithm.Securities)
            {
                subscriptions.Add(CreateSubscription(algorithm, kvp.Value, startTimeUtc, endTimeUtc));
            }

            var count = 0;
            double kps = 0;
            var currentTime = default(DateTime);
            var stopwatch = new Stopwatch();
            var timer = new Timer(_ =>
            {
                kps = count / 1000d / stopwatch.Elapsed.TotalSeconds;
                Console.WriteLine($"Current Time: {currentTime:u}  Elapsed time: {(int)stopwatch.Elapsed.TotalSeconds,4}s  KPS: {kps,7:.00}  COUNT: {count,10}");
            });
            timer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            stopwatch.Start();
            do
            {
                var timeSlice = synchronizer.Sync(subscriptions);
                currentTime = timeSlice.Time;
                count += timeSlice.DataPointCount;
            }
            while (currentTime < endTimeUtc);

            stopwatch.Stop();
            timer.Dispose();

            kps = count / 1000d / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Current Time: {currentTime:u}  Elapsed time: {(int)stopwatch.Elapsed.TotalSeconds,4}s  KPS: {kps,7:.00}  COUNT: {count,10}");
        }

        private Subscription CreateSubscription(QCAlgorithm algorithm, Security security, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var universe = algorithm.UniverseManager.Values.OfType<UserDefinedUniverse>()
                .Single(u => u.SelectSymbols(default(DateTime), null).Contains(security.Symbol));

            var config = security.Subscriptions.First();
            var enumerator = DataTradeBarEnumerator(algorithm.StartDate, algorithm.EndDate, Time.OneSecond);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.NewYork, startTimeUtc, endTimeUtc);
            return new Subscription(universe, security, config, enumerator, offsetProvider, endTimeUtc, endTimeUtc, false);
        }

        private IEnumerator<BaseData> DataTradeBarEnumerator(DateTime startTimeLocal, DateTime endTimeLocal, TimeSpan increment)
        {
            var currentDataStartTime = startTimeLocal - increment;
            var currentDataEndTime = startTimeLocal;
            while (currentDataEndTime <= endTimeLocal)
            {
                var data = new TradeBar
                {
                    Time = currentDataStartTime,
                    EndTime = currentDataEndTime
                };

                yield return data;

                currentDataStartTime = currentDataEndTime;
                currentDataEndTime = currentDataEndTime + increment;
            }
        }
    }
}