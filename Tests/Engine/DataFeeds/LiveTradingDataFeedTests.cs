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
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Ignore("These tests depend on a remote server")]
    public class LiveTradingDataFeedTests
    {
        [Test]
        public void EmitsData()
        {
            DataManager dataManager;
            var algorithm = new AlgorithmStub(out dataManager, forex: new List<string> {"EURUSD"});

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();
            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new BacktestingResultHandler();
            var dataProvider = new DefaultDataProvider();

            var lastTime = DateTime.MinValue;
            var timeProvider = new RealTimeProvider();
            var dataQueueHandler = new FuncDataQueueHandler(fdqh =>
            {
                var time = timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.EasternStandard);
                if (time == lastTime) return Enumerable.Empty<BaseData>();
                lastTime = time;
                 return Enumerable.Range(0, 9).Select(x => new Tick(time.AddMilliseconds(x*100), Symbols.EURUSD, 1.3m, 1.2m, 1.3m));
            });

            var feed = new TestableLiveTradingDataFeed(dataQueueHandler, timeProvider);
            var mapFileProvider = new LocalDiskMapFileProvider();
            feed.Initialize(algorithm, job, resultHandler, mapFileProvider, new LocalDiskFactorFileProvider(mapFileProvider), dataProvider, dataManager.DataFeedSubscriptions);

            var feedThreadStarted = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                feedThreadStarted.Set();
                feed.Run();
            });

            // wait for feed.Run to actually begin
            feedThreadStarted.WaitOne();

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), true, ts =>
            {
                if (ts.Slice.Count != 0)
                {
                    emittedData = true;
                    Console.WriteLine("HasData: " + ts.Slice.Bars[Symbols.EURUSD].EndTime);
                    Console.WriteLine();
                }
            });

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void HandlesMultipleSecurities()
        {
            DataManager dataManager;
            var algorithm = new AlgorithmStub(out dataManager,
                equities: new List<string> {"SPY", "IBM", "AAPL", "GOOG", "MSFT", "BAC", "GS"},
                forex: new List<string> {"EURUSD", "USDJPY", "GBPJPY", "AUDUSD", "NZDUSD"}
                );
            var feed = RunDataFeed(algorithm, dataManager);

            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                var delta = (DateTime.UtcNow - ts.Time).TotalMilliseconds;
                Console.WriteLine(((decimal)delta).SmartRounding() + "ms : " + string.Join(",", ts.Slice.Keys.Select(x => x.Value)));
            });
        }

        [Test]
        public void PerformanceBenchmark()
        {
            DataManager dataManager;
            var symbolCount = 600;
            var algorithm = new AlgorithmStub(out dataManager, Resolution.Tick,
                equities: Enumerable.Range(0, symbolCount).Select(x => "E"+x.ToString()).ToList()
                );

            var securitiesCount = algorithm.Securities.Count;
            var expected = algorithm.Securities.Keys.ToHashSet();
            Console.WriteLine("Securities.Count: " + securitiesCount);

            FuncDataQueueHandler queue;
            var count = new Count();
            var stopwatch = Stopwatch.StartNew();
            var feed = RunDataFeed(algorithm, out queue, dataManager, null, fdqh => ProduceBenchmarkTicks(fdqh, count));

            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                Console.WriteLine("Count: " + ts.Slice.Keys.Count + " " + DateTime.UtcNow.ToString("o"));
                if (ts.Slice.Keys.Count != securitiesCount)
                {
                    var included = ts.Slice.Keys.ToHashSet();
                    expected.ExceptWith(included);
                    Console.WriteLine("Missing: " + string.Join(",", expected.OrderBy(x => x.Value)));
                }
            });
            stopwatch.Stop();

            Console.WriteLine("Total ticks: " + count.Value);
            Console.WriteLine("Elapsed time: " + stopwatch.Elapsed);
            Console.WriteLine("Ticks/sec: " + (count.Value/stopwatch.Elapsed.TotalSeconds));
            Console.WriteLine("Ticks/sec/symbol: " + (count.Value/stopwatch.Elapsed.TotalSeconds)/symbolCount);
        }

        class Count
        {
            public int Value;
        }

        private static IEnumerable<BaseData> ProduceBenchmarkTicks(FuncDataQueueHandler fdqh, Count count)
        {
            for (int i = 0; i < 10000; i++)
            {
                foreach (var symbol in fdqh.Subscriptions)
                {
                    count.Value++;
                    yield return new Tick{Symbol = symbol};
                }
            }
        }

        [Test]
        public void DoesNotSubscribeToCustomData()
        {
            // Current implementation only sends equity/forex subscriptions to the queue handler,
            // new impl sends all, the restriction shouldn't live in the feed, but rather in the
            // queue handler impl

            DataManager dataManager;
            var algorithm = new AlgorithmStub(out dataManager, equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            algorithm.AddData<RemoteFileBaseData>("RemoteFile");
            var remoteFile = SymbolCache.GetSymbol("RemoteFile");
            FuncDataQueueHandler dataQueueHandler;
            RunDataFeed(algorithm, out dataQueueHandler, dataManager);

            Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
            Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
            Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(remoteFile));
            Assert.AreEqual(2, dataQueueHandler.Subscriptions.Count);
        }

        [Test]
        public void Unsubscribes()
        {
            DataManager dataManager;
            var algorithm = new AlgorithmStub(out dataManager, equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            algorithm.AddData<RemoteFileBaseData>("RemoteFile");
            var remoteFile = SymbolCache.GetSymbol("RemoteFile");
            FuncDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(algorithm, out dataQueueHandler, dataManager);

            feed.RemoveSubscription(feed.Subscriptions.Single(sub => sub.Configuration.Symbol == Symbols.SPY).Configuration);

            Assert.AreEqual(1, dataQueueHandler.Subscriptions.Count);
            Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
            Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(remoteFile));
            Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
        }

        [Test]
        public void HandlesAtLeast10kTicksPerSecondWithTwentySymbols()
        {
            DataManager dataManager;
            // this ran at ~25k ticks/per symbol for 20 symbols
            var algorithm = new AlgorithmStub(out dataManager, Resolution.Tick, Enumerable.Range(0, 20).Select(x => x.ToString()).ToList());
            var t = Enumerable.Range(0, 20).Select(x => new Tick {Symbol = SymbolCache.GetSymbol(x.ToString())}).ToList();
            var feed = RunDataFeed(algorithm, dataManager, handler => t);
            var flag = false;
            int ticks = 0;
            var averages = new List<decimal>();
            var timer = new Timer(state =>
            {
                var avg = ticks/20m;
                Interlocked.Exchange(ref ticks, 0);
                Console.WriteLine("Average ticks per symbol: " + avg.SmartRounding());
                if (flag) flag = false;
                averages.Add(avg);
            }, null, Time.OneSecond, Time.OneSecond);
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), false, ts =>
            {
                Interlocked.Add(ref ticks, ts.Slice.Ticks.Sum(x => x.Value.Count));
            }, true);


            var average = averages.Average();
            Console.WriteLine("\r\nAverage ticks per symbol per second: " + average);
            Assert.That(average, Is.GreaterThan(10000));
        }

        [Test]
        public void EmitsForexDataWithRoundedUtcTimes()
        {
            DataManager dataManager;
            var algorithm = new AlgorithmStub(out dataManager, forex: new List<string> { "EURUSD" });

            var feed = RunDataFeed(algorithm, dataManager);

            var emittedData = false;
            var lastTime = DateTime.UtcNow;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                if (!emittedData)
                {
                    emittedData = true;
                    lastTime = ts.Time;
                    return;
                }
                var delta = (DateTime.UtcNow - ts.Time).TotalMilliseconds;
                Console.WriteLine(((decimal)delta).SmartRounding() + "ms : " + string.Join(", ", ts.Slice.Keys.Select(x => x.Value + ": " + ts.Slice[x].Volume)));
                Assert.AreEqual(lastTime.Add(Time.OneSecond), ts.Time);
                Assert.AreEqual(1, ts.Slice.Bars.Count);
                lastTime = ts.Time;
            });

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void HandlesManyCustomDataSubscriptions()
        {
            DataManager dataManager;
            var resolution = Resolution.Second;
            var algorithm = new AlgorithmStub(out dataManager);
            for (int i = 0; i < 5; i++)
            {
                algorithm.AddData<RemoteFileBaseData>((100+ i).ToString(), resolution, fillDataForward: false);
            }

            var feed = RunDataFeed(algorithm, dataManager);

            int count = 0;
            //bool receivedData = false;
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine("start: " + DateTime.UtcNow.ToString("o"));
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                // because this is a remote file we may skip data points while the newest
                // version of the file is downloading [internet speed] and also we decide
                // not to emit old data

                stopwatch.Stop();
                if (ts.Slice.Count == 0) return;

                count++;
                //receivedData = true;
                var time = ts.Slice.Min(x => x.Value.EndTime).ConvertToUtc(TimeZones.NewYork);
                // make sure within 2 seconds
                var delta = DateTime.UtcNow.Subtract(time);
                //Assert.IsTrue(delta <= TimeSpan.FromSeconds(2), delta.ToString());
                Console.WriteLine("Count: " + ts.Slice.Count + "Data time: " + time.ConvertFromUtc(TimeZones.NewYork) + " Delta (ms): "
                    + ((decimal) delta.TotalMilliseconds).SmartRounding() + Environment.NewLine);
            });

            Console.WriteLine("end: " + DateTime.UtcNow.ToString("o"));
            Console.WriteLine("Spool up time: " + stopwatch.Elapsed);

            // even though we're doing 20 seconds, give a little
            // leeway for slow internet traffic
            //Assert.That(count, Is.GreaterThan(17));
            //Assert.IsTrue(receivedData);
        }

        [Test]
        public void HandlesRestApi()
        {
            DataManager dataManager;
            var resolution = Resolution.Second;
            var algorithm = new AlgorithmStub(out dataManager);
            algorithm.AddData<RestApiBaseData>("RestApi", resolution);
            var symbol = SymbolCache.GetSymbol("RestApi");
            FuncDataQueueHandler dqgh;
            var timeProvider = new ManualTimeProvider(new DateTime(2015, 10, 10, 16, 36, 0));
            var feed = RunDataFeed(algorithm, out dqgh, null);

            var count = 0;
            var receivedData = false;
            var timeZone = algorithm.Securities[symbol].Exchange.TimeZone;
            RestApiBaseData last = null;

            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            foreach (var ts in feed)
            {
                //timeProvider.AdvanceSeconds(0.5);

                if (!ts.Slice.ContainsKey(symbol)) return;

                count++;
                receivedData = true;
                var data = (RestApiBaseData)ts.Slice[symbol];
                var time = data.EndTime.ConvertToUtc(timeZone);
                Console.WriteLine(DateTime.UtcNow + ": Data time: " + time.ConvertFromUtc(TimeZones.NewYork) + Environment.NewLine);
                if (last != null)
                {
                    Assert.AreEqual(last.EndTime, data.EndTime.Subtract(resolution.ToTimeSpan()));
                }
                last = data;
            }

            // even though we're doing 10 seconds, give a little
            // leeway for slow internet traffic
            Assert.That(count, Is.GreaterThanOrEqualTo(8));
            Assert.IsTrue(receivedData);
            Assert.That(RestApiBaseData.ReaderCount, Is.LessThanOrEqualTo(30)); // we poll at 10x frequency

            Console.WriteLine("Count: " + count + " ReaderCount: " + RestApiBaseData.ReaderCount);
        }

        [Test]
        public void HandlesCoarseFundamentalData()
        {
            DataManager dataManager;
            var algorithm = new AlgorithmStub(out dataManager);
            Symbol symbol = CoarseFundamental.CreateUniverseSymbol(Market.USA);
            algorithm.AddUniverse(new FuncUniverse(
                new SubscriptionDataConfig(typeof(CoarseFundamental), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false),
                new UniverseSettings(Resolution.Second, 1, true, false, TimeSpan.Zero), SecurityInitializer.Null,
                coarse => coarse.Take(10).Select(x => x.Symbol)
                ));

            var lck = new object();
            BaseDataCollection list = null;
            const int coarseDataPointCount = 100000;
            var timer = new Timer(state =>
            {
                var currentTime = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork);
                Console.WriteLine(currentTime + ": timer.Elapsed");

                lock (state)
                {
                    list = new BaseDataCollection {Symbol = symbol};
                    list.Data.AddRange(Enumerable.Range(0, coarseDataPointCount).Select(x => new CoarseFundamental
                    {
                        Symbol = SymbolCache.GetSymbol(x.ToString()),
                        Time = currentTime - Time.OneDay, // hard-coded coarse period of one day
                    }));
                }
            }, lck, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(500));

            bool yieldedUniverseData = false;
            var feed = RunDataFeed(algorithm, dataManager, fdqh =>
            {
                lock (lck)
                {
                    if (list != null)
                        try
                        {
                            var tmp = list;
                            return new List<BaseData> { tmp };
                        }
                        finally
                        {
                            list = null;
                            yieldedUniverseData = true;
                        }
                }
                return Enumerable.Empty<BaseData>();
            });

            Assert.IsTrue(feed.Subscriptions.Any(x => x.IsUniverseSelectionSubscription));

            var universeSelectionHadAllData = false;


            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
            });

            Assert.IsTrue(yieldedUniverseData);
            Assert.IsTrue(universeSelectionHadAllData);
        }


        [Test]
        public void FastExitsDoNotThrowUnhandledExceptions()
        {
            DataManager dataManager;
            var algorithm = new AlgorithmStub(out dataManager, Resolution.Tick, Enumerable.Range(0, 20).Select(x => x.ToString()).ToList());
            var getNextTicksFunction = Enumerable.Range(0, 20).Select(x => new Tick { Symbol = SymbolCache.GetSymbol(x.ToString()) }).ToList();

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();

            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new BacktestingResultHandler();

            var dataQueueHandler = new FuncDataQueueHandler(handler => getNextTicksFunction);

            var feed = new TestableLiveTradingDataFeed(dataQueueHandler, null);
            var mapFileProvider = new LocalDiskMapFileProvider();
            var fileProvider = new DefaultDataProvider();
            feed.Initialize(algorithm, job, resultHandler, mapFileProvider, new LocalDiskFactorFileProvider(mapFileProvider), fileProvider, dataManager.DataFeedSubscriptions);

            var feedThreadStarted = new ManualResetEvent(false);

            var unhandledExceptionWasThrown = false;
            Task.Run(() =>
            {
                try
                {
                    feedThreadStarted.Set();
                    feed.Run();
                }
                catch(Exception ex)
                {
                    QuantConnect.Logging.Log.Error(ex.ToString());
                    unhandledExceptionWasThrown = true;
                }
            });

            feedThreadStarted.WaitOne();
            feed.Exit();

            Thread.Sleep(1000);

            Assert.IsFalse(unhandledExceptionWasThrown);
        }

        private IDataFeed RunDataFeed(IAlgorithm algorithm, DataManager dataManager, Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction = null)
        {
            FuncDataQueueHandler dataQueueHandler;
            return RunDataFeed(algorithm, out dataQueueHandler, dataManager, null, getNextTicksFunction);
        }

        private IDataFeed RunDataFeed(IAlgorithm algorithm, out FuncDataQueueHandler dataQueueHandler, DataManager dataManager, ITimeProvider timeProvider = null, Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction = null)
        {
            getNextTicksFunction = getNextTicksFunction ?? (fdqh => fdqh.Subscriptions.Select(symbol => new Tick(DateTime.Now, symbol, 1, 2){Quantity = 1}));

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();
            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new BacktestingResultHandler(); // new ResultHandlerStub();

            dataQueueHandler = new FuncDataQueueHandler(getNextTicksFunction);

            var feed = new TestableLiveTradingDataFeed(dataQueueHandler, timeProvider);
            var mapFileProvider = new LocalDiskMapFileProvider();
            var fileProvider = new DefaultDataProvider();
            feed.Initialize(algorithm, job, resultHandler, mapFileProvider, new LocalDiskFactorFileProvider(mapFileProvider), fileProvider, dataManager.DataFeedSubscriptions);

            var feedThreadStarted = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                feedThreadStarted.Set();
                feed.Run();
            });

            // wait for feed.Run to actually begin
            feedThreadStarted.WaitOne();

            return feed;
        }

        private static void ConsumeBridge(IDataFeed feed, Action<TimeSlice> handler)
        {
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), handler);
        }

        private static void ConsumeBridge(IDataFeed feed, TimeSpan timeout, Action<TimeSlice> handler)
        {
            ConsumeBridge(feed, timeout, false, handler);
        }

        private static void ConsumeBridge(IDataFeed feed, TimeSpan timeout, bool alwaysInvoke, Action<TimeSlice> handler, bool noOutput = false)
        {
            Task.Delay(timeout).ContinueWith(_ => feed.Exit());
            bool startedReceivingata = false;
            foreach (var timeSlice in feed)
            {
                if (!noOutput)
                {
                    Console.WriteLine("\r\n" + "Now (EDT): {0} TimeSlice.Time (EDT): {1}",
                        DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork).ToString("o"),
                        timeSlice.Time.ConvertFromUtc(TimeZones.NewYork).ToString("o")
                        );
                }

                if (!startedReceivingata && timeSlice.Slice.Count != 0)
                {
                    startedReceivingata = true;
                }
                if (startedReceivingata || alwaysInvoke)
                {
                    handler(timeSlice);
                }
            }
        }

    }

    public class TestableLiveTradingDataFeed : LiveTradingDataFeed
    {
        private readonly ITimeProvider _timeProvider;
        private readonly IDataQueueHandler _dataQueueHandler;

        public TestableLiveTradingDataFeed(IDataQueueHandler dataQueueHandler, ITimeProvider timeProvider = null)
        {
            _dataQueueHandler = dataQueueHandler;
            _timeProvider = timeProvider ?? new RealTimeProvider();
        }

        protected override IDataQueueHandler GetDataQueueHandler()
        {
            return _dataQueueHandler;
        }

        protected override ITimeProvider GetTimeProvider()
        {
            return _timeProvider;
        }
    }
}
