using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            algorithm.Initialize();

            // set exchanges to be always open
            foreach (var kvp in algorithm.Securities)
            {
                var security = kvp.Value;
                security.Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(security.Exchange.TimeZone));
            }

            var startTimeUtc = algorithm.StartDate.ConvertToUtc(TimeZones.NewYork);
            var endTimeUtc = algorithm.EndDate.ConvertToUtc(TimeZones.NewYork);

            var feed = new AlgorithmManagerTests.MockDataFeed();
            var universeSelection = new UniverseSelection(feed, algorithm);
            var synchronizer = new SubscriptionSynchronizer(universeSelection, algorithm.TimeZone, algorithm.Portfolio.CashBook, startTimeUtc);

            var subscriptions = new List<Subscription>
            {
                CreateSubscription(algorithm, algorithm.Securities.Single().Value, startTimeUtc, endTimeUtc)
            };

            var count = 0;
            TimeSlice slice;
            var stopwatch = Stopwatch.StartNew();
            do
            {
                slice = synchronizer.Sync(subscriptions);
                count++;
                if (slice.Time == DateTime.MaxValue)
                {
                    break;
                }
            }
            while (slice.Time < endTimeUtc);
            stopwatch.Stop();

            var kps = count/1000d/ stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Current Time: {slice.Time}:: Elapsed time: {stopwatch.Elapsed}  COUNT: {count}    KPS: {kps:.00}");
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