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
using Moq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class LiveTradingDataFeedTests
    {
        private static bool LogsEnabled = false; // this is for travis log not to fill up and reach the max size.
        private ManualTimeProvider _manualTimeProvider;
        private AlgorithmStub _algorithm;
        private TestableLiveSynchronizer _synchronizer;
        private DateTime _startDate;
        private TestableLiveTradingDataFeed _feed;
        private DataManager _dataManager;
        private FuncDataQueueHandler _dataQueueHandler;
        private static readonly Dictionary<Type, BaseData> _instances = new Dictionary<Type, BaseData>
        {
            {typeof(BaseData), typeof(TradeBar).GetBaseDataInstance() },
            {typeof(BenzingaNews), typeof(BenzingaNews).GetBaseDataInstance() },
            {typeof(TiingoNews), typeof(TiingoNews).GetBaseDataInstance() },
        };

        [SetUp]
        public void SetUp()
        {
            _manualTimeProvider = new ManualTimeProvider();
            _algorithm = new AlgorithmStub(false);
            _startDate = new DateTime(2018, 08, 1, 11, 0, 0);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);
            CustomMockedFileBaseData.StartDate = _startDate;

            Interlocked.Exchange(ref TestCustomData.ReaderCallsCount, 0);
            TestCustomData.ReturnNull = false;
            TestCustomData.ThrowException = false;
        }

        [TearDown]
        public void TearDown()
        {
            _dataManager?.RemoveAllSubscriptions();
            _feed?.Exit();
            _synchronizer.DisposeSafely();
            _dataQueueHandler?.DisposeSafely();
        }

        [Test]
        public void EmitsData()
        {
            var endDate = _startDate.AddDays(10);
            var feed = RunDataFeed(forex: new List<string> { Symbols.EURUSD });

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    emittedData = true;
                    var data = ts.Slice[Symbols.EURUSD];
                    ConsoleWriteLine("HasData: " + data);
                    ConsoleWriteLine();

                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                }
            }, endDate: endDate);

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void HandlesMultipleSecurities()
        {
            var endDate = _startDate.AddDays(10);
            var equities = new List<string> { "SPY", "IBM", "AAPL", "GOOG", "MSFT", "BAC", "GS" };
            var forex = new List<string> { "EURUSD", "USDJPY", "GBPJPY", "AUDUSD", "NZDUSD" };

            var feed = RunDataFeed(equities: equities, forex: forex);

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(2), ts =>
            {
                var delta = (DateTime.UtcNow - ts.Time).TotalMilliseconds;
                var values = ts.Slice.Keys.Select(x => x.Value).ToList();
                ConsoleWriteLine(((decimal)delta).SmartRounding().ToStringInvariant() + "ms : " + string.Join(",", values));
                Assert.IsTrue(equities.All(x => values.Contains(x)));
                Assert.IsTrue(forex.All(x => values.Contains(x)));
                emittedData = true;

                // we got what we wanted shortcut unit test
                _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
            }, endDate: endDate);
            Assert.IsTrue(emittedData);
        }

        [Test]
        public void PerformanceBenchmark()
        {
            var symbolCount = 600;

            var count = new Count();
            var stopwatch = Stopwatch.StartNew();
            var feed = RunDataFeed(Resolution.Tick, equities: Enumerable.Range(0, symbolCount).Select(x => "E" + x.ToStringInvariant()).ToList(),
                getNextTicksFunction: fdqh => ProduceBenchmarkTicks(fdqh, count));

            var securitiesCount = _algorithm.Securities.Count;
            var expected = _algorithm.Securities.Keys.ToHashSet();
            Log.Trace("Securities.Count: " + securitiesCount);

            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                ConsoleWriteLine("Count: " + ts.Slice.Keys.Count + " " + DateTime.UtcNow.ToStringInvariant("o"));
                if (ts.Slice.Keys.Count != securitiesCount)
                {
                    var included = ts.Slice.Keys.ToHashSet();
                    expected.ExceptWith(included);
                    ConsoleWriteLine("Missing: " + string.Join(",", expected.OrderBy(x => x.Value)));
                }
            });
            stopwatch.Stop();

            Log.Trace("Total ticks: " + count.Value);
            Assert.GreaterOrEqual(count.Value, 700000);
            Log.Trace("Elapsed time: " + stopwatch.Elapsed);
            var ticksPerSec = count.Value / stopwatch.Elapsed.TotalSeconds;
            Log.Trace("Ticks/sec: " + ticksPerSec);
            Assert.GreaterOrEqual(ticksPerSec, 70000);
            var ticksPerSecPerSymbol = (count.Value / stopwatch.Elapsed.TotalSeconds) / symbolCount;
            Log.Trace("Ticks/sec/symbol: " + ticksPerSecPerSymbol);
            Assert.GreaterOrEqual(ticksPerSecPerSymbol, 100);
        }

        [Test]
        public void DoesNotSubscribeToCustomData()
        {
            var endDate = _startDate.AddDays(10);
            // Current implementation only sends equity/forex subscriptions to the queue handler,
            // new impl sends all, the restriction shouldn't live in the feed, but rather in the
            // queue handler impl
            var feed = RunDataFeed(equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            _algorithm.AddData<CustomMockedFileBaseData>("CustomMockedFileBaseData");
            var customMockedFileBaseData = SymbolCache.GetSymbol("CustomMockedFileBaseData");

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(2), ts =>
            {
                ConsoleWriteLine("Count: " + ts.Slice.Keys.Count + " " + DateTime.UtcNow.ToStringInvariant("o"));
                Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                Assert.IsFalse(_dataQueueHandler.Subscriptions.Contains(customMockedFileBaseData));
                emittedData = true;

                // we got what we wanted shortcut unit test
                _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
            }, endDate: endDate);

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void AddsSubscription_NewUserUniverse()
        {
            var endDate = _startDate.AddDays(10);
            var feed = RunDataFeed(equities: new List<string> { "SPY" });

            var forexFxcmUserUniverse = UserDefinedUniverse.CreateSymbol(SecurityType.Forex, Market.Oanda);
            var emittedData = false;
            var newDataCount = 0;
            var securityChanges = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                securityChanges += ts.SecurityChanges.Count;
                if (!emittedData)
                {
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    if (ts.Data.Count > 0)
                    {
                        Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                    }
                    // SPY benchmark and the UserDefinedUniverse
                    Assert.AreEqual(2, _dataQueueHandler.Subscriptions.Count);

                    _algorithm.AddSecurities(forex: new List<string> { "EURUSD" });
                    emittedData = true;

                    // The custom exchange has to pick up the universe selection data point and push it into the universe subscription to
                    // trigger adding EURUSD in the next loop
                    Thread.Sleep(150);

                    _algorithm.OnEndOfTimeStep();
                }
                else
                {
                    // SPY benchmark and the UserDefinedUniverse Equity/Forex, EURUSD
                    if (_dataQueueHandler.Subscriptions.Count == 4) // there could be some slices with no data
                    {
                        Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                        if (ts.Data.Count > 0)
                        {
                            Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                        }
                        Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD)
                                      || _dataQueueHandler.Subscriptions.Contains(forexFxcmUserUniverse));
                        // Might delay a couple of Slices to send over the data, so we will count them
                        // and assert a minimum amount
                        if (ts.Slice.Keys.Contains(Symbols.EURUSD))
                        {
                            newDataCount++;
                            if (newDataCount >= 5)
                            {
                                // we got what we wanted shortcut unit test
                                _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                            }
                        }
                    }
                    else
                    {
                        Assert.Fail($"Subscriptions.Count: {_dataQueueHandler.Subscriptions.Count}: {string.Join(",", _dataQueueHandler.Subscriptions)}");
                    }
                }
            }, endDate: endDate);

            Log.Trace("newDataCount: " + newDataCount);
            Assert.AreEqual(2, securityChanges);

            Assert.GreaterOrEqual(newDataCount, 5);
            Assert.IsTrue(emittedData);
        }

        [Test]
        public void AddsNewUniverse()
        {
            var endDate = _startDate.AddDays(10);
            _algorithm.UniverseSettings.Resolution = Resolution.Second; // Default is Minute and we need something faster
            _algorithm.UniverseSettings.ExtendedMarketHours = true; // Current _startDate is at extended market hours

            var feed = RunDataFeed(forex: new List<string> { "EURUSD" });
            var firstTime = false;
            var securityChanges = 0;
            var newDataCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                securityChanges += ts.SecurityChanges.Count;
                if (!firstTime)
                {
                    // benchmark SPY, EURUSD and the UserDefinedUniverse
                    Assert.AreEqual(3, _dataQueueHandler.Subscriptions.Count);
                    _algorithm.AddUniverse("TestUniverse", time => new List<string> { "AAPL", "SPY" });
                    firstTime = true;
                }
                else
                {
                    if (_dataQueueHandler.Subscriptions.Count == 2)
                    {
                        Assert.AreEqual(1, _dataQueueHandler.Subscriptions.Count(x => x.Value.Contains("TESTUNIVERSE")));
                    }
                    else if (_dataQueueHandler.Subscriptions.Count == 4)
                    {
                        // Coarse universe isn't added to the data queue handler
                        Assert.AreNotEqual(1, _dataQueueHandler.Subscriptions.Count(x => x.Value.Contains("TESTUNIVERSE")));
                        Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                        Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.AAPL));
                        Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                        // Might delay a couple of Slices to send over the data, so we will count them and assert a minimum amount
                        if (ts.Slice.Keys.Contains(Symbols.AAPL)
                            && ts.Slice.Keys.Contains(Symbols.SPY))
                        {
                            newDataCount++;
                            if (newDataCount >= 5)
                            {
                                // we got what we wanted shortcut unit test
                                _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                            }
                        }
                    }
                    else
                    {
                        Assert.Fail($"Subscriptions.Count: {_dataQueueHandler.Subscriptions.Count}");
                    }
                }
            }, endDate: endDate);

            Log.Trace("newDataCount: " + newDataCount);
            Assert.AreEqual(3, securityChanges);

            Assert.GreaterOrEqual(newDataCount, 5);
            Assert.IsTrue(firstTime);
        }

        [Test]
        public void AddsSubscription_SameUserUniverse()
        {
            var endDate = _startDate.AddDays(10);
            var feed = RunDataFeed(equities: new List<string> { "SPY" });

            var emittedData = false;
            var newDataCount = 0;
            var changes = new List<SecurityChanges>();
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.SecurityChanges != SecurityChanges.None)
                {
                    changes.Add(ts.SecurityChanges);
                }
                if (!emittedData)
                {
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    if (ts.Data.Count > 0)
                    {
                        Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                    }
                    // SPY benchmark and the UserDefinedUniverse
                    Assert.AreEqual(2, _dataQueueHandler.Subscriptions.Count);

                    _algorithm.AddSecurities(equities: new List<string> { "AAPL" });
                    emittedData = true;

                    // The custom exchange has to pick up the universe selection data point and push it into the universe subscription to
                    // trigger adding AAPL in the next loop
                    Thread.Sleep(150);
                }
                else
                {
                    // SPY benchmark and the UserDefinedUniverse Equity, AAPL
                    if (_dataQueueHandler.Subscriptions.Count == 3) // there could be some slices with no data
                    {
                        Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                        if (ts.Data.Count > 0)
                        {
                            Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                        }
                        Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.AAPL));
                        // Might delay a couple of Slices to send over the data, so we will count them
                        // and assert a minimum amount
                        if (ts.Slice.Keys.Contains(Symbols.AAPL))
                        {
                            newDataCount++;
                            if (newDataCount >= 5)
                            {
                                // we got what we wanted shortcut unit test
                                _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                            }
                        }
                    }
                    else
                    {
                        Assert.Fail($"Subscriptions.Count: {_dataQueueHandler.Subscriptions.Count}");
                    }
                }
            }, endDate: endDate);

            Assert.GreaterOrEqual(newDataCount, 5);
            Assert.IsTrue(emittedData);
            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(Symbols.SPY, changes[0].AddedSecurities.Single().Symbol);
            Assert.AreEqual(Symbols.AAPL, changes[1].AddedSecurities.Single().Symbol);
        }

        [Test]
        public void Unsubscribes()
        {
            var endDate = _startDate.AddDays(10);
            var feed = RunDataFeed(equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            _algorithm.AddData<CustomMockedFileBaseData>("CustomMockedFileBaseData");
            var customMockedFileBaseData = SymbolCache.GetSymbol("CustomMockedFileBaseData");

            var emittedData = false;
            var currentSubscriptionCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), false, ts =>
            {
                Assert.IsFalse(_dataQueueHandler.Subscriptions.Contains(customMockedFileBaseData));
                if (!emittedData)
                {
                    currentSubscriptionCount = _dataQueueHandler.SubscriptionDataConfigs.Count;
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                    var subscriptions = _dataManager.DataFeedSubscriptions
                        .Where(subscription => !subscription.Configuration.IsInternalFeed && subscription.Configuration.Symbol == Symbols.SPY);
                    foreach (var subscription in subscriptions)
                    {
                        _dataManager.RemoveSubscription(subscription.Configuration);
                    }
                    emittedData = true;
                }
                else
                {
                    // should of remove trade and quote bar subscription and split/dividend for trade bar
                    Assert.AreEqual(currentSubscriptionCount - 4, _dataQueueHandler.SubscriptionDataConfigs.Count);
                    // internal subscription should still be there
                    Assert.AreEqual(0, _dataQueueHandler.SubscriptionDataConfigs
                        .Where(config => !config.IsInternalFeed)
                        .Count(config => config.Symbol == Symbols.SPY));
                    Assert.AreEqual(1, _dataQueueHandler.SubscriptionDataConfigs.Count(config => config.Symbol == Symbols.SPY));
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));

                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                }
            }, endDate: endDate);

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void RemoveSecurity()
        {
            var endDate = _startDate.AddDays(10);
            _algorithm.SetFinishedWarmingUp();
            _algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var feed = RunDataFeed(equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            _algorithm.AddData<CustomMockedFileBaseData>("CustomMockedFileBaseData");
            var customMockedFileBaseData = SymbolCache.GetSymbol("CustomMockedFileBaseData");

            var emittedData = false;
            var currentSubscriptionCount = 0;
            var changes = new List<SecurityChanges>();
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.SecurityChanges != SecurityChanges.None)
                {
                    changes.Add(ts.SecurityChanges);
                }
                Assert.IsFalse(_dataQueueHandler.Subscriptions.Contains(customMockedFileBaseData));
                if (!emittedData)
                {
                    currentSubscriptionCount = _dataQueueHandler.SubscriptionDataConfigs.Count;
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                    _algorithm.RemoveSecurity(Symbols.SPY);
                    emittedData = true;
                }
                else
                {
                    // should of remove trade and quote bar subscription and split/dividend for trade bar
                    Assert.AreEqual(currentSubscriptionCount - 4, _dataQueueHandler.SubscriptionDataConfigs.Count);
                    // internal subscription should still be there
                    Assert.AreEqual(0, _dataQueueHandler.SubscriptionDataConfigs
                        .Where(config => !config.IsInternalFeed)
                        .Count(config => config.Symbol == Symbols.SPY));
                    Assert.AreEqual(1, _dataQueueHandler.SubscriptionDataConfigs.Count(config => config.Symbol == Symbols.SPY));
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                }
            }, endDate: endDate);

            Assert.IsTrue(emittedData);
            Assert.AreEqual(4, changes.Aggregate(0, (i, securityChanges) => i + securityChanges.Count));
            Assert.AreEqual(Symbols.SPY, changes[1].RemovedSecurities.Single().Symbol);
        }

        [Test]
        public void BenchmarkTicksPerSecondWithTwentySymbols()
        {
            // this ran at ~25k ticks/per symbol for 20 symbols

            var feed = RunDataFeed(Resolution.Tick, equities: Enumerable.Range(0, 20).Select(x => x.ToStringInvariant()).ToList());
            int ticks = 0;
            var averages = new List<decimal>();
            var timer = new Timer(state =>
            {
                var avg = ticks / 20m;
                Interlocked.Exchange(ref ticks, 0);
                Log.Trace("Average ticks per symbol: " + avg.SmartRounding());
                averages.Add(avg);
            }, null, Time.OneSecond, Time.OneSecond);

            ConsumeBridge(feed, TimeSpan.FromSeconds(5), false, ts =>
            {
                Interlocked.Add(ref ticks, ts.Slice.Ticks.Sum(x => x.Value.Count));
            });

            timer.Dispose();
            var average = averages.Average();
            Log.Trace("\r\nAverage ticks per symbol per second: " + average);
            Assert.That(average, Is.GreaterThan(40));
        }

        [Test]
        public void EmitsForexDataWithRoundedUtcTimes()
        {
            var feed = RunDataFeed(forex: new List<string> { "EURUSD" });

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
                Assert.AreEqual(lastTime.Add(Time.OneSecond), ts.Time);
                Assert.AreEqual(1, ts.Slice.QuoteBars.Count);
                lastTime = ts.Time;
            });

            Assert.IsTrue(emittedData);
        }

        [Test]
        public void HandlesManyCustomDataSubscriptions()
        {
            var feed = RunDataFeed();
            for (int i = 0; i < 100; i++)
            {
                _algorithm.AddData<CustomMockedFileBaseData>((100 + i).ToStringInvariant(), Resolution.Second, fillDataForward: false);
            }

            int count = 0;
            var emittedData = false;
            var stopwatch = Stopwatch.StartNew();

            var previousTime = DateTime.Now;
            Log.Trace("start: " + previousTime.ToStringInvariant("o"));
            ConsumeBridge(feed, TimeSpan.FromSeconds(3), false, ts =>
            {
                // because this is a remote file we may skip data points while the newest
                // version of the file is downloading [internet speed] and also we decide
                // not to emit old data
                stopwatch.Stop();
                if (ts.Slice.Count == 0) return;

                emittedData = true;
                count++;

                // make sure within 2 seconds
                var delta = DateTime.Now.Subtract(previousTime);
                previousTime = DateTime.Now;
                Assert.IsTrue(delta <= TimeSpan.FromSeconds(2), delta.ToString());
                ConsoleWriteLine($"TimeProvider now: {_manualTimeProvider.GetUtcNow().ToStringInvariant()} Count: {ts.Slice.Count}. " +
                    $"Delta (ms): {((decimal)delta.TotalMilliseconds).SmartRounding().ToStringInvariant()}{Environment.NewLine}"
                );
            });

            Log.Trace("Count: " + count);
            Log.Trace("Spool up time: " + stopwatch.Elapsed);

            Assert.That(count, Is.GreaterThan(5));
            Assert.IsTrue(emittedData);
        }

        [TestCase(FileFormat.Csv, true, false)]
        [TestCase(FileFormat.Collection, true, false)]
        [TestCase(FileFormat.Csv, false, false)]
        [TestCase(FileFormat.Collection, false, false)]
        [TestCase(FileFormat.Csv, false, true)]
        [TestCase(FileFormat.Collection, false, true)]
        public void RestCustomDataReturningNullDoesNotInfinitelyPoll(FileFormat fileFormat, bool returnsNull, bool throwsException)
        {
            TestCustomData.FileFormat = fileFormat;

            var feed = RunDataFeed();

            _algorithm.AddData<TestCustomData>("Pinocho", Resolution.Minute, fillDataForward: false);

            TestCustomData.ReturnNull = returnsNull;
            TestCustomData.ThrowException = throwsException;
            ConsumeBridge(feed, TimeSpan.FromSeconds(2), false, ts =>
            {
                Log.Trace("Emitted data");
            });

            Assert.AreEqual(1, TestCustomData.ReaderCallsCount);
        }

        [Test, Ignore("These tests depend on a remote server")]
        public void HandlesRestApi()
        {
            var resolution = Resolution.Second;
            var feed = RunDataFeed();
            _algorithm.AddData<RestApiBaseData>("RestApi", resolution);
            var symbol = SymbolCache.GetSymbol("RestApi");

            var count = 0;
            var receivedData = false;
            var timeZone = _algorithm.Securities[symbol].Exchange.TimeZone;
            RestApiBaseData last = null;

            var cancellationTokenSource = new CancellationTokenSource();
            foreach (var ts in _synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (!ts.Slice.ContainsKey(symbol)) return;

                count++;
                receivedData = true;
                var data = (RestApiBaseData)ts.Slice[symbol];
                var time = data.EndTime.ConvertToUtc(timeZone);
                ConsoleWriteLine(DateTime.UtcNow + ": Data time: " + time.ConvertFromUtc(TimeZones.NewYork) + Environment.NewLine);
                if (last != null)
                {
                    Assert.AreEqual(last.EndTime, data.EndTime.Subtract(resolution.ToTimeSpan()));
                }
                last = data;
            }

            feed.Exit();
            Assert.That(count, Is.GreaterThanOrEqualTo(8));
            Assert.IsTrue(receivedData);
            Assert.That(RestApiBaseData.ReaderCount, Is.LessThanOrEqualTo(30)); // we poll at 10x frequency

            Log.Trace("Count: " + count + " ReaderCount: " + RestApiBaseData.ReaderCount);
        }

        [Test]
        public void CoarseFundamentalDataIsHoldUntilTimeIsRight()
        {
            _startDate = new DateTime(2014, 3, 25);
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            Log.Trace($"StartTime {_manualTimeProvider.GetUtcNow()}");

            // we just want to emit one single coarse data packet
            var feed = RunDataFeed(getNextTicksFunction: fdqh => Enumerable.Empty<BaseData>());

            _algorithm.AddUniverse(coarse => coarse.Take(10).Select(x => x.Symbol));
            // will add the universe
            _algorithm.OnEndOfTimeStep();

            var receivedCoarseData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                if (ts.UniverseData.Count > 0 &&
                    ts.UniverseData.First().Value.Data.First() is CoarseFundamental)
                {
                    var now = _manualTimeProvider.GetUtcNow();
                    Log.Trace($"Received BaseDataCollection {now}");

                    // Assert data got hold until time was right
                    Assert.IsTrue(now.Hour < 23 && now.Hour > 5, $"Unexpected now value: {now}");
                    receivedCoarseData = true;

                    // we got what we wanted, end unit test
                    _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                }
            }, sendUniverseData: true,
                alwaysInvoke: true,
                secondsTimeStep: 3600,
                endDate: _startDate.AddDays(1));

            Log.Trace($"EndTime {_manualTimeProvider.GetUtcNow()}");

            Assert.IsTrue(receivedCoarseData, "Did not receive Coarse data.");
        }

        [Test]
        public void CustomUniverseFineFundamentalDataGetsPipedCorrectly()
        {
            _startDate = new DateTime(2014, 10, 07);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            // we use test ConstituentsUniverse, we have daily data for it
            var customUniverseSymbol = new Symbol(
                SecurityIdentifier.GenerateConstituentIdentifier(
                    "constituents-universe-qctest",
                    SecurityType.Equity,
                    Market.USA),
                "constituents-universe-qctest");
            var customUniverse = new ConstituentsUniverse(customUniverseSymbol,
                new UniverseSettings(Resolution.Daily, 1, false, true, TimeSpan.Zero));

            var feed = RunDataFeed();

            var fineWasCalled = false;
            _algorithm.AddUniverse(customUniverse,
                fine =>
                {
                    var symbol = fine.First().Symbol;
                    if (symbol == Symbols.AAPL)
                    {
                        fineWasCalled = true;
                    }
                    return new[] { symbol };
                });
            SecurityChanges securityChanges = null;
            var receivedFundamentalsData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), ts =>
            {
                if (ts.UniverseData.Count > 0 &&
                    ts.UniverseData.First().Value.Data.First() is Fundamentals)
                {
                    securityChanges = ts.SecurityChanges;
                    receivedFundamentalsData = true;
                    // short cut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                }
            }, secondsTimeStep: 60 * 60 * 6, // 6 hour time step
                alwaysInvoke: true,
                sendUniverseData: true,
                endDate:_startDate.AddDays(10));

            Assert.IsNotNull(securityChanges);
            Assert.IsTrue(securityChanges.AddedSecurities.Single().Symbol.Value == "AAPL");
            Assert.IsTrue(receivedFundamentalsData);
            Assert.IsTrue(fineWasCalled);
        }


        [Test]
        public void FineCoarseFundamentalDataGetsPipedCorrectly()
        {
            _startDate = new DateTime(2014, 3, 25);
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var feed = RunDataFeed(getNextTicksFunction: fdqh => Enumerable.Empty<BaseData>());

            var fineWasCalled = false;
            _algorithm.AddUniverse(coarse => coarse
                    .Where(x => x.Symbol.ID.Symbol.Contains("AAPL")).Select((fundamental, i) => fundamental.Symbol),
                fine =>
                {
                    var symbol = fine.First().Symbol;
                    if (symbol == Symbols.AAPL)
                    {
                        fineWasCalled = true;
                    }
                    return new[] { symbol };
                });

            var receivedFundamentalsData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                if (ts.UniverseData.Count > 0 &&
                    ts.UniverseData.First().Value.Data.First() is Fundamentals)
                {
                    receivedFundamentalsData = true;
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                }
            }, sendUniverseData: true, alwaysInvoke: true, secondsTimeStep: 3600, endDate: _startDate.AddDays(10));

            Assert.IsTrue(receivedFundamentalsData);
            Assert.IsTrue(fineWasCalled);
        }

        [Test]
        public void ConstituentsUniverse()
        {
            var qqq = Symbol.Create("QQQ", SecurityType.Equity, Market.USA);
            // Set a date for which we have the test data.
            // Note the date is a Tuesday
            _startDate = new DateTime(2013, 10, 07);
            var endDate = new DateTime(2013, 10, 10);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate.AddHours(20));
            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            _algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var yieldedSymbols = false;
            var yieldedNoneSymbol = false;
            var feed = RunDataFeed();

            _algorithm.AddUniverse(new ConstituentsUniverse(
                new Symbol(
                    SecurityIdentifier.GenerateConstituentIdentifier(
                        "constituents-universe-qctest",
                        SecurityType.Equity,
                        Market.USA),
                    "constituents-universe-qctest"),
                _algorithm.UniverseSettings));
            // will add the universe
            _algorithm.OnEndOfTimeStep();
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), ts =>
            {
                if (ts.UniverseData.Count > 0)
                {
                    var data = ts.UniverseData.Values.First();
                    if (data.EndTime >= new DateTime(2013, 10, 09))
                    {
                        Assert.AreEqual(1, data.Data.Count);
                        Assert.IsTrue(data.Data.Any(baseData => baseData.Symbol == Symbol.None));
                        yieldedNoneSymbol = true;
                    }
                    else if (data.EndTime >= new DateTime(2013, 10, 08))
                    {
                        Assert.AreEqual(2, data.Data.Count);
                        Assert.IsTrue(data.Data.Any(baseData => baseData.Symbol == Symbols.AAPL));
                        Assert.IsTrue(data.Data.Any(baseData => baseData.Symbol == qqq));
                        yieldedSymbols = true;
                    }

                    if (yieldedSymbols && yieldedNoneSymbol)
                    {
                        // we got what we wanted, end unit test
                        _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                    }
                }
            }, secondsTimeStep: 60 * 60 * 3, // 3 hour time step
                alwaysInvoke: true,
                endDate: endDate);

            Assert.IsTrue(yieldedSymbols, "Did not yielded Symbols");
            Assert.IsTrue(yieldedNoneSymbol, "Did not yield NoneSymbol");
        }

        [Test]
        public void FastExitsDoNotThrowUnhandledExceptions()
        {
            var algorithm = new AlgorithmStub();

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();

            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new BacktestingResultHandler();

            _feed = new TestableLiveTradingDataFeed();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();

            var securityService = new SecurityService(
                algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(algorithm.Portfolio));
            var fileProvider = new DefaultDataProvider();
            algorithm.Securities.SetSecurityService(securityService);
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(_feed,
                new UniverseSelection(algorithm, securityService, dataPermissionManager, fileProvider),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                true,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);
            algorithm.SubscriptionManager.SetDataManager(dataManager);
            _synchronizer = new TestableLiveSynchronizer();
            _synchronizer.Initialize(algorithm, dataManager);
            algorithm.AddSecurities(Resolution.Tick, Enumerable.Range(0, 20).Select(x => x.ToStringInvariant()).ToList());
            var getNextTicksFunction = Enumerable.Range(0, 20).Select(x => new Tick { Symbol = SymbolCache.GetSymbol(x.ToStringInvariant()) }).ToList();
            _feed.DataQueueHandler = new FuncDataQueueHandler(handler => getNextTicksFunction, new RealTimeProvider());
            var mapFileProvider = new LocalDiskMapFileProvider();
            _feed.Initialize(
                algorithm,
                job,
                resultHandler,
                mapFileProvider,
                new LocalDiskFactorFileProvider(mapFileProvider),
                fileProvider,
                dataManager,
                _synchronizer,
                new TestDataChannelProvider());

            var unhandledExceptionWasThrown = false;
            try
            {
                _feed.Exit();
            }
            catch (Exception ex)
            {
                QuantConnect.Logging.Log.Error(ex.ToString());
                unhandledExceptionWasThrown = true;
            }

            Thread.Sleep(500);
            Assert.IsFalse(unhandledExceptionWasThrown);
        }

        [Test]
        public void HandlesAllTickTypesAtTickResolution()
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);
            // setting func benchmark so we don't add SPY
            _algorithm.SetBenchmark(time => 1);
            var feed = RunDataFeed(
                Resolution.Tick,
                crypto: new List<string> { symbol.Value },
                getNextTicksFunction: dqh => Enumerable.Range(1, 2)
                    .Select(x => new Tick
                    {
                        Symbol = symbol,
                        TickType = x % 2 == 0 ? TickType.Trade : TickType.Quote
                    })
                    .ToList());

            var tradeCount = 0;
            var quoteCount = 0;
            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(1), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    emittedData = true;
                    tradeCount += ts.Slice.Ticks[symbol].Count(tick => tick.TickType == TickType.Trade);
                    quoteCount += ts.Slice.Ticks[symbol].Count(tick => tick.TickType == TickType.Quote);
                }
            });

            Assert.IsTrue(emittedData, "No data was emitted");
            Assert.AreNotEqual(0, quoteCount);
            Assert.AreNotEqual(0, tradeCount);
        }

        [Test]
        public void SuspiciousTicksAreNotFilteredAtTickResolution()
        {
            var endDate = _startDate.AddDays(10);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var feed = RunDataFeed(
                Resolution.Tick,
                equities: new List<string> { symbol.Value },
                getNextTicksFunction: dqh => Enumerable.Range(0, 1)
                    .Select(
                        x => new Tick
                        {
                            Symbol = symbol,
                            TickType = TickType.Trade,
                            Suspicious = x % 2 == 0
                        })
                    .ToList());

            var emittedData = false;
            var suspiciousTicksReceived = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(3), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    emittedData = true;

                    foreach (var kvp in ts.Slice.Ticks)
                    {
                        foreach (var tick in kvp.Value)
                        {
                            if (tick.Suspicious)
                            {
                                suspiciousTicksReceived = true;
                                // we got what we wanted shortcut unit test
                                _manualTimeProvider.SetCurrentTimeUtc(DateTime.UtcNow);
                            }
                        }
                    }
                }
            }, endDate: endDate);

            Assert.IsTrue(emittedData);
            Assert.IsTrue(suspiciousTicksReceived);
        }

        [TestCase(SecurityType.Equity, TickType.Trade)]
        [TestCase(SecurityType.Forex, TickType.Quote)]
        [TestCase(SecurityType.Crypto, TickType.Trade)]
        [TestCase(SecurityType.Crypto, TickType.Quote)]
        public void SuspiciousTicksAreFilteredAtNonTickResolution(SecurityType securityType, TickType tickType)
        {
            var lastTime = _manualTimeProvider.GetUtcNow();
            var feed = RunDataFeed(Resolution.Minute,
                equities: securityType == SecurityType.Equity ? new List<string> { Symbols.SPY } : new List<string>(),
                forex: securityType == SecurityType.Forex ? new List<string> { Symbols.EURUSD } : new List<string>(),
                crypto: securityType == SecurityType.Crypto ? new List<string> { Symbols.BTCUSD } : new List<string>(),
                getNextTicksFunction: (fdqh =>
                {
                    var time = _manualTimeProvider.GetUtcNow();
                    if (time == lastTime) return Enumerable.Empty<BaseData>();
                    lastTime = time;
                    var tickTime = lastTime.AddMinutes(-1).ConvertFromUtc(TimeZones.NewYork);
                    return fdqh.Subscriptions.Where(symbol => !_algorithm.UniverseManager.ContainsKey(symbol)) // its not a universe
                        .Select(symbol => new Tick(tickTime, symbol, 1, 2)
                        {
                            Quantity = 1,
                            TickType = tickType,
                            Suspicious = true
                        }).ToList();
                }));

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(2), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    emittedData = true;
                }
            });

            Assert.IsFalse(emittedData);
        }

        [Test]
        public void HandlesAuxiliaryDataAtTickResolution()
        {
            var symbol = Symbols.AAPL;

            var feed = RunDataFeed(
                Resolution.Tick,
                equities: new List<string> { symbol.Value },
                getNextTicksFunction: delegate
                {
                    return Enumerable.Range(1, 2)
                        .Select(
                            x => x % 2 == 0
                                ? (BaseData)new Tick { Symbol = symbol, TickType = TickType.Trade }
                                : new Dividend { Symbol = symbol, Value = x })
                        .ToList();
                });

            var emittedTicks = false;
            var emittedAuxData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(1), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    if (ts.Slice.Ticks.ContainsKey(symbol))
                    {
                        emittedTicks = true;
                    }
                    if (ts.Slice.Dividends.ContainsKey(symbol))
                    {
                        emittedAuxData = true;
                    }
                }
            });

            Assert.IsTrue(emittedTicks);
            Assert.IsTrue(emittedAuxData);
        }

        [Test]
        public void AggregatesTicksToTradeBar()
        {
            var symbol = Symbols.AAPL;

            var feed = RunDataFeed(Resolution.Second, equities: new List<string> { symbol.Value });

            var emittedTradeBars = false;

            ConsumeBridge(feed, TimeSpan.FromSeconds(1), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    if (ts.Slice.Bars.ContainsKey(symbol))
                    {
                        emittedTradeBars = true;
                    }
                }
            });

            Assert.IsTrue(emittedTradeBars);
        }

        [Test]
        public void DoesNotAggregateTicksToTradeBar()
        {
            var symbol = Symbols.AAPL;
            var feed = RunDataFeed(
                Resolution.Tick,
                equities: new List<string> { symbol.Value },
                getNextTicksFunction: delegate
                {
                    return Enumerable.Range(0, 2)
                        .Select(_ => (BaseData)new Tick { Symbol = symbol, TickType = TickType.Trade })
                        .ToList();
                });

            var emittedTradebars = false;

            ConsumeBridge(feed, TimeSpan.FromSeconds(1), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    if (ts.Slice.Bars.ContainsKey(symbol))
                    {
                        emittedTradebars = true;
                    }
                }
            });

            Assert.IsFalse(emittedTradebars);
        }


        private IDataFeed RunDataFeed(Resolution resolution = Resolution.Second, List<string> equities = null, List<string> forex = null, List<string> crypto = null,
            Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction = null)
        {
            _algorithm.SetStartDate(_startDate);

            var lastTime = _manualTimeProvider.GetUtcNow();
            getNextTicksFunction = getNextTicksFunction ?? (fdqh =>
            {
                var time = _manualTimeProvider.GetUtcNow();
                if (time == lastTime) return Enumerable.Empty<BaseData>();
                lastTime = time;
                var tickTimeUtc = lastTime.AddMinutes(-1);
                return fdqh.SubscriptionDataConfigs.Where(config => !_algorithm.UniverseManager.ContainsKey(config.Symbol)) // its not a universe
                    .SelectMany(config =>
                        {
                            var ticks = new List<Tick>
                            {
                                new Tick(tickTimeUtc.ConvertFromUtc(config.ExchangeTimeZone), config.Symbol, 1, 2)
                                {
                                    Quantity = 1,
                                    // Symbol could not be in the Securities collections for the custom Universe tests. AlgorithmManager is in charge of adding them, and we are not executing that code here.
                                    TickType = config.TickType
                                }
                            };
                            return ticks;
                        }
                    ).ToList();
            });

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();
            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new BacktestingResultHandler();

            _dataQueueHandler = new FuncDataQueueHandler(getNextTicksFunction, _manualTimeProvider);

            _feed = new TestableLiveTradingDataFeed(_dataQueueHandler);
            var mapFileProvider = new LocalDiskMapFileProvider();
            var fileProvider = new DefaultDataProvider();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var securityService = new SecurityService(_algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, _algorithm, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(_algorithm.Portfolio));
            _algorithm.Securities.SetSecurityService(securityService);
            var dataPermissionManager = new DataPermissionManager();
            _dataManager = new DataManager(_feed,
                new UniverseSelection(_algorithm, securityService, dataPermissionManager, fileProvider),
                _algorithm,
                _algorithm.TimeKeeper,
                marketHoursDatabase,
                true,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);
            _algorithm.SubscriptionManager.SetDataManager(_dataManager);
            _algorithm.AddSecurities(resolution, equities, forex, crypto);
            _synchronizer = new TestableLiveSynchronizer(_manualTimeProvider, 500);
            _synchronizer.Initialize(_algorithm, _dataManager);

            _feed.Initialize(_algorithm, job, resultHandler, mapFileProvider,
                new LocalDiskFactorFileProvider(mapFileProvider), fileProvider, _dataManager, _synchronizer, new TestDataChannelProvider());

            _algorithm.PostInitialize();
            Thread.Sleep(150); // small handicap for the data to be pumped so TimeSlices have data of all subscriptions
            return _feed;
        }

        private void ConsumeBridge(IDataFeed feed, TimeSpan timeout, Action<TimeSlice> handler, bool sendUniverseData = false,
            int secondsTimeStep = 1, bool alwaysInvoke = false, DateTime endDate = default(DateTime))
        {
            ConsumeBridge(feed, timeout, alwaysInvoke, handler, sendUniverseData: sendUniverseData, secondsTimeStep: secondsTimeStep, endDate: endDate);
        }

        private void ConsumeBridge(IDataFeed feed,
            TimeSpan timeout,
            bool alwaysInvoke,
            Action<TimeSlice> handler,
            bool noOutput = true,
            bool sendUniverseData = false,
            int secondsTimeStep = 1,
            DateTime endDate = default(DateTime))
        {
            var endTime = DateTime.UtcNow.Add(timeout);
            bool startedReceivingata = false;
            var cancellationTokenSource = new CancellationTokenSource();
            foreach (var timeSlice in _synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (!noOutput)
                {
                    ConsoleWriteLine("\r\n" + $"Now (EDT): {DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork):o}" +
                                     $". TimeSlice.Time (EDT): {timeSlice.Time.ConvertFromUtc(TimeZones.NewYork):o}");
                }

                if (timeSlice.IsTimePulse)
                {
                    continue;
                }
                if (!startedReceivingata
                    && (timeSlice.Slice.Count != 0
                        || sendUniverseData && timeSlice.UniverseData.Count > 0))
                {
                    startedReceivingata = true;
                }
                if (startedReceivingata || alwaysInvoke)
                {
                    handler(timeSlice);
                }
                _algorithm.OnEndOfTimeStep();
                _manualTimeProvider.AdvanceSeconds(secondsTimeStep);
                Thread.Sleep(10);
                if (endDate != default(DateTime) && _manualTimeProvider.GetUtcNow() > endDate
                    || endTime <= DateTime.UtcNow)
                {
                    feed.Exit();
                    cancellationTokenSource.Cancel();
                    // allow LTDF tasks to finish
                    Thread.Sleep(10);
                    return;
                }
            }
        }

        private class Count
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
                    yield return new Tick { Symbol = symbol };
                }
            }
        }

        private void ConsoleWriteLine(string line = "")
        {
            if (LogsEnabled)
            {
                Log.Trace(line);
            }
        }

        private static TestCaseData[] DataTypeTestCases => new[]
        {
            // Equity - Hourly resolution
            // We expect 7 hourly bars for 6.5 hours in open market hours
            // We expect only 1 dividend at midnight
            new TestCaseData(Symbols.SPY, Resolution.Hour, 1, 0, 7, 0, 1, 0, false, _instances[typeof(BaseData)]),

            // Equity - Minute resolution
            // We expect 30 minute bars for 0.5 hours in open market hours
            new TestCaseData(Symbols.SPY, Resolution.Minute, 1, 0, (int)(0.5 * 60), (int)(0.5 * 60), 0, 0, false, _instances[typeof(BaseData)]),

            // Equity - Tick resolution
            // In this test we only emit ticks once per hour
            // We expect only 6 ticks -- the 4 PM tick is not received because it's outside market hours -> times 2 (quote/trade bar)
            // We expect only 1 dividend at midnight
            new TestCaseData(Symbols.SPY, Resolution.Tick, 1, (7 - 1) * 2, 0, 0, 1, 0, false, _instances[typeof(BaseData)]),

            // Forex - FXCM
            new TestCaseData(Symbols.EURUSD, Resolution.Hour, 1, 0, 0, 24, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.EURUSD, Resolution.Minute, 1, 0, 0, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.EURUSD, Resolution.Tick, 1, 24, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Forex - Oanda
            new TestCaseData(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), Resolution.Hour, 1, 0, 0, 24, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), Resolution.Minute, 1, 0, 0, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), Resolution.Tick, 1, 24, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // CFD - FXCM
            new TestCaseData(Symbols.DE30EUR, Resolution.Hour, 1, 0, 0, 14, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.DE30EUR, Resolution.Minute, 1, 0, 0, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.DE30EUR, Resolution.Tick, 1, 14, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // CFD - Oanda
            new TestCaseData(Symbol.Create("DE30EUR", SecurityType.Cfd, Market.Oanda), Resolution.Hour, 1, 0, 0, 14, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbol.Create("DE30EUR", SecurityType.Cfd, Market.Oanda), Resolution.Minute, 1, 0, 0, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbol.Create("DE30EUR", SecurityType.Cfd, Market.Oanda), Resolution.Tick, 1, 14, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Crypto
            new TestCaseData(Symbols.BTCUSD, Resolution.Hour, 1, 0, 24, 24, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.BTCUSD, Resolution.Minute, 1, 0, 1 * 60, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            // x2 because counting trades and quotes
            new TestCaseData(Symbols.BTCUSD, Resolution.Tick, 1, 24 * 2, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Futures
            // ES has two session breaks totalling 1h 15m, so total trading hours = 22.75
            new TestCaseData(Symbols.Future_ESZ18_Dec2018, Resolution.Hour, 1, 0, 23, 23, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.Future_ESZ18_Dec2018, Resolution.Minute, 1, 0, 1 * 60, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            // x2 because counting trades and quotes
            new TestCaseData(Symbols.Future_ESZ18_Dec2018, Resolution.Tick, 1, 23 * 2, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Options
            new TestCaseData(Symbols.SPY_C_192_Feb19_2016, Resolution.Hour, 1, 0, 7, 7, 0, 0, false, _instances[typeof(BaseData)]),
            // We expect 30 minute bars for 0.5 hours in open market hours
            new TestCaseData(Symbols.SPY_C_192_Feb19_2016, Resolution.Minute, 1, 0, (int)(0.5 * 60), (int)(0.5 * 60), 0, 0, false, _instances[typeof(BaseData)]),
            // x2 because counting trades and quotes
            new TestCaseData(Symbols.SPY_C_192_Feb19_2016, Resolution.Tick, 1, (7 - 1) * 2, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Custom data not supported
            new TestCaseData(Symbol.CreateBase(typeof(BenzingaNews), Symbols.AAPL, Market.USA), Resolution.Hour, 1, 0, 0, 0, 0, 24 * 2, true, _instances[typeof(BenzingaNews)]),
            new TestCaseData(Symbol.CreateBase(typeof(BenzingaNews), Symbols.AAPL, Market.USA), Resolution.Minute, 1, 0, 0, 0, 0, 60 * 2, true, _instances[typeof(BenzingaNews)]),
            new TestCaseData(Symbol.CreateBase(typeof(BenzingaNews), Symbols.AAPL, Market.USA), Resolution.Tick, 1, 0, 0, 0, 0, 24, true, _instances[typeof(BenzingaNews)]),

            // Custom data streamed
            new TestCaseData(Symbol.CreateBase(typeof(TiingoNews), Symbols.AAPL, Market.USA), Resolution.Hour, 1, 0, 0, 0, 0, 24 * 2, false, _instances[typeof(TiingoNews)]),
            new TestCaseData(Symbol.CreateBase(typeof(TiingoNews), Symbols.AAPL, Market.USA), Resolution.Minute, 1, 0, 0, 0, 0, 60 * 2, false, _instances[typeof(TiingoNews)]),
            new TestCaseData(Symbol.CreateBase(typeof(TiingoNews), Symbols.AAPL, Market.USA), Resolution.Tick, 1, 0, 0, 0, 0, 24, false, _instances[typeof(TiingoNews)])
        };

        [TestCaseSource(nameof(DataTypeTestCases))]
        public void HandlesAllTypes<T>(
            Symbol symbol,
            Resolution resolution,
            int days,
            int expectedTicksReceived,
            int expectedTradeBarsReceived,
            int expectedQuoteBarsReceived,
            int expectedAuxPointsReceived,
            int expectedCustomPointsReceived,
            bool shouldThrowException,
            T customDataType) where T : BaseData, new()
        {
            // startDate and endDate are in algorithm time zone
            var startDate = new DateTime(2019, 6, 3);
            var endDate = startDate.AddDays(days);

            if (resolution == Resolution.Minute)
            {
                // for faster test execution time
                startDate = startDate.AddHours(9);
                endDate = startDate.AddHours(1);
            }

            var algorithmTimeZone = TimeZones.NewYork;
            DateTimeZone exchangeTimeZone = null;

            var timeProvider = new ManualTimeProvider(algorithmTimeZone);
            timeProvider.SetCurrentTime(startDate);

            var actualPricePointsEnqueued = 0;
            var actualAuxPointsEnqueued = 0;
            var lastTime = DateTime.MinValue;
            var emittedData = new ManualResetEvent(false);

            var dataQueueStarted = new ManualResetEvent(false);
            _dataQueueHandler = new FuncDataQueueHandler(fdqh =>
            {
                dataQueueStarted.Set();

                if (exchangeTimeZone == null)
                {
                    return Enumerable.Empty<BaseData>();
                }

                var utcTime = timeProvider.GetUtcNow();
                var exchangeTime = utcTime.ConvertFromUtc(exchangeTimeZone);
                if (exchangeTime == lastTime ||
                    exchangeTime > endDate.ConvertTo(algorithmTimeZone, exchangeTimeZone))
                {
                    emittedData.Set();
                    return Enumerable.Empty<BaseData>();
                }

                lastTime = exchangeTime;

                var algorithmTime = utcTime.ConvertFromUtc(algorithmTimeZone);

                var dataPoints = new List<BaseData>();

                if (symbol.SecurityType == SecurityType.Base)
                {
                    BaseData dataPoint = null;
                    dataPoints.Add(new T
                    {
                        Symbol = symbol,
                        EndTime = exchangeTime,
                        Value = actualPricePointsEnqueued++
                    });

                    ConsoleWriteLine(
                        $"{algorithmTime} - FuncDataQueueHandler emitted custom data point: {dataPoint}");
                }
                else
                {
                    if (symbol.SecurityType == SecurityType.Equity && exchangeTime.Day == startDate.Day + 1 &&
                        exchangeTime.Hour == 0 && exchangeTime.Minute == 0)
                    {
                        var dividend = new Dividend
                        {
                            Symbol = symbol,
                            Time = exchangeTime,
                            EndTime = exchangeTime,
                            Value = actualAuxPointsEnqueued++
                        };

                        dataPoints.Add(dividend);

                        ConsoleWriteLine($"{algorithmTime} - FuncDataQueueHandler emitted dividend: {dividend}");
                    }
                    else
                    {
                        var tickType = TickType.Quote;
                        var dataPoint = new Tick
                        {
                            Symbol = symbol,
                            Time = exchangeTime,
                            EndTime = exchangeTime,
                            TickType = tickType,
                            Value = actualPricePointsEnqueued
                        };

                        if (symbol.SecurityType != SecurityType.Equity
                            || resolution != Resolution.Daily
                            || resolution != Resolution.Hour)
                        {
                            actualPricePointsEnqueued++;
                            // equity has minute/second/tick quote data
                            dataPoints.Add(dataPoint);
                        }

                        ConsoleWriteLine(
                            $"{algorithmTime} - FuncDataQueueHandler emitted {tickType} tick: {dataPoint}");

                        dataPoint = new Tick
                        {
                            Symbol = symbol,
                            Time = exchangeTime,
                            EndTime = exchangeTime,
                            TickType = TickType.Trade,
                            Value = actualPricePointsEnqueued++
                        };

                        dataPoints.Add(dataPoint);

                        ConsoleWriteLine(
                            $"{algorithmTime} - FuncDataQueueHandler emitted Trade tick: {dataPoint}");
                    }
                }

                emittedData.Set();
                return dataPoints;
            }, timeProvider);

            _feed = new TestableLiveTradingDataFeed(_dataQueueHandler);

            var algorithm = new QCAlgorithm();
            algorithm.SetDateTime(timeProvider.GetUtcNow());

            var historyProvider = new Mock<IHistoryProvider>();
            historyProvider.Setup(
                    m => m.GetHistory(It.IsAny<IEnumerable<Data.HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns(Enumerable.Empty<Slice>());
            algorithm.SetHistoryProvider(historyProvider.Object);

            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var securityService = new SecurityService(
                algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(algorithm.Portfolio));
            algorithm.Securities.SetSecurityService(securityService);
            var dataPermissionManager = new DataPermissionManager();
            var fileProvider = new DefaultDataProvider();
            var dataManager = new DataManager(_feed,
                new UniverseSelection(algorithm, securityService, dataPermissionManager, fileProvider),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                true,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);
            algorithm.SubscriptionManager.SetDataManager(dataManager);
            algorithm.SetLiveMode(true);

            var mock = new Mock<ITransactionHandler>();
            mock.Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>())).Returns(new List<Order>());
            algorithm.Transactions.SetOrderProcessor(mock.Object);

            _synchronizer = new TestableLiveSynchronizer(timeProvider, 150);
            _synchronizer.Initialize(algorithm, dataManager);

            Security security;
            switch (symbol.SecurityType)
            {
                case SecurityType.Base:
                    algorithm.AddEquity(symbol.Underlying.Value, resolution, symbol.ID.Market,
                        fillDataForward: false);

                    if (customDataType.RequiresMapping())
                    {
                        security = algorithm.AddData<T>(symbol.Value, resolution,
                            fillDataForward: false);
                    }
                    else
                    {
                        throw new NotImplementedException($"Custom data not implemented: {symbol}");
                    }
                    break;

                case SecurityType.Future:
                    security = algorithm.AddFutureContract(symbol, resolution, fillDataForward: false);
                    break;

                case SecurityType.Option:
                    security = algorithm.AddOptionContract(symbol, resolution, fillDataForward: false);
                    break;

                default:
                    security = algorithm.AddSecurity(symbol.SecurityType, symbol.Value, resolution,
                        symbol.ID.Market, false, 1, false);
                    break;
            }

            var mapFileProvider = new LocalDiskMapFileProvider();
            _feed.Initialize(algorithm, new LiveNodePacket(), new BacktestingResultHandler(),
                mapFileProvider, new LocalDiskFactorFileProvider(mapFileProvider), fileProvider,
                dataManager, _synchronizer, new TestDataChannelProvider());

            if (!dataQueueStarted.WaitOne(TimeSpan.FromMilliseconds(5000)))
            {
                throw new TimeoutException("Timeout waiting for IDQH to start");
            }
            var cancellationTokenSource = new CancellationTokenSource();

            // for tick resolution, we advance one hour at a time for less unit test run time
            TimeSpan advanceTimeSpan;
            switch (resolution)
            {
                case Resolution.Tick:
                default:
                    advanceTimeSpan = TimeSpan.FromHours(1);
                    break;
                case Resolution.Second:
                    advanceTimeSpan = TimeSpan.FromSeconds(0.5);
                    break;
                case Resolution.Minute:
                    advanceTimeSpan = TimeSpan.FromSeconds(30);
                    break;
                case Resolution.Hour:
                    advanceTimeSpan = TimeSpan.FromMinutes(30);
                    break;
                case Resolution.Daily:
                    advanceTimeSpan = TimeSpan.FromHours(12);
                    break;
            }
            try
            {
                algorithm.PostInitialize();

                // The custom exchange has to pick up the universe selection data point and push it into the universe subscription to
                // trigger adding the securities. else there will be a race condition emitting the first data point and having a subscription
                // to receive it
                Thread.Sleep(200);

                var actualTicksReceived = 0;
                var actualTradeBarsReceived = 0;
                var actualQuoteBarsReceived = 0;
                var actualAuxPointsReceived = 0;
                var actualCustomPointsReceived = 0;
                var sliceCount = 0;
                foreach (var timeSlice in _synchronizer.StreamData(cancellationTokenSource.Token))
                {
                    if (timeSlice.IsTimePulse)
                    {
                        algorithm.OnEndOfTimeStep();
                        continue;
                    }

                    exchangeTimeZone = security.Exchange.TimeZone;

                    sliceCount++;

                    if (resolution == Resolution.Tick)
                    {
                        if (timeSlice.Slice.Ticks.ContainsKey(symbol))
                        {
                            foreach (var tick in timeSlice.Slice.Ticks[symbol].ToList())
                            {
                                actualTicksReceived++;

                                ConsoleWriteLine(
                                    $"{algorithm.Time} - Tick received, value: {tick.Value} {tick.TickType} (count: {actualTicksReceived})"
                                );
                            }
                        }

                        if (timeSlice.Slice.Dividends.ContainsKey(symbol))
                        {
                            actualAuxPointsReceived++;

                            ConsoleWriteLine(
                                $"{algorithm.Time} - Dividend received, value: {timeSlice.Slice.Dividends[symbol].Value} (count: {actualAuxPointsReceived})"
                            );
                        }

                        var customDataCount = timeSlice.Slice.Get<T>().Count;
                        if (customDataCount > 0)
                        {
                            actualCustomPointsReceived += customDataCount;

                            ConsoleWriteLine(
                                $"{algorithm.Time} - Custom received, value: {timeSlice.Slice.Get<T>().First().Value} (count: {actualCustomPointsReceived})"
                            );
                        }
                    }
                    else
                    {
                        if (timeSlice.Slice.Bars.ContainsKey(symbol))
                        {
                            actualTradeBarsReceived++;

                            ConsoleWriteLine(
                                $"{algorithm.Time} - TradeBar received, value: {timeSlice.Slice.Bars[symbol].Value} (count: {actualTradeBarsReceived})"
                            );
                        }

                        if (timeSlice.Slice.Dividends.ContainsKey(symbol))
                        {
                            actualAuxPointsReceived++;

                            ConsoleWriteLine(
                                $"{algorithm.Time} - Dividend received, value: {timeSlice.Slice.Dividends[symbol].Value} (count: {actualAuxPointsReceived})"
                            );
                        }

                        if (timeSlice.Slice.QuoteBars.ContainsKey(symbol))
                        {
                            actualQuoteBarsReceived++;

                            ConsoleWriteLine(
                                $"{algorithm.Time} - QuoteBar received, value: {timeSlice.Slice.QuoteBars[symbol].Value} (count: {actualQuoteBarsReceived})"
                            );
                        }

                        if (symbol.SecurityType == SecurityType.Base)
                        {
                            var customDataCount = timeSlice.Slice.Get<T>().Count;
                            if (customDataCount > 0)
                            {
                                actualCustomPointsReceived += customDataCount;

                                ConsoleWriteLine(
                                    $"{algorithm.Time} - Custom received, value: {timeSlice.Slice.Get<T>().First().Value} (count: {actualCustomPointsReceived})"
                                );
                            }
                        }
                    }

                    algorithm.OnEndOfTimeStep();

                    _synchronizer.NewDataEvent.Reset();
                    emittedData.Reset();
                    timeProvider.Advance(advanceTimeSpan);

                    // give enough time to the producer to emit
                    if (!emittedData.WaitOne(300))
                    {
                        Assert.Fail("Timeout waiting for data generation");
                    }

                    var currentTime = timeProvider.GetUtcNow();
                    algorithm.SetDateTime(currentTime);

                    ConsoleWriteLine($"Algorithm time set to {currentTime.ConvertFromUtc(algorithmTimeZone)}");

                    if (resolution != Resolution.Tick)
                    {
                        var amount = currentTime.Ticks % resolution.ToTimeSpan().Ticks;
                        if (amount == 0)
                        {
                            // let's avoid race conditions and give time for the funDataQueueHandler thread to distribute the data among the consolidators
                            if (!_synchronizer.NewDataEvent.Wait(500))
                            {
                                Assert.Fail("Timeout waiting for data generation");
                            }
                        }
                    }
                    else
                    {
                        _synchronizer.NewDataEvent.Wait(300);
                    }

                    if (currentTime.ConvertFromUtc(algorithmTimeZone) > endDate)
                    {
                        _feed.Exit();
                        cancellationTokenSource.Cancel();
                        break;
                    }
                }

                emittedData.DisposeSafely();
                dataQueueStarted.DisposeSafely();

                Log.Trace(
                    $"SliceCount:{sliceCount} - PriceData: Enqueued:{actualPricePointsEnqueued} TicksReceived:{actualTicksReceived}"
                );
                Log.Trace(
                    $"SliceCount:{sliceCount} - PriceData: Enqueued:{actualPricePointsEnqueued} TradeBarsReceived:{actualTradeBarsReceived}"
                );
                Log.Trace(
                    $"SliceCount:{sliceCount} - PriceData: Enqueued:{actualPricePointsEnqueued} QuoteBarsReceived:{actualQuoteBarsReceived}"
                );
                Log.Trace(
                    $"SliceCount:{sliceCount} - AuxData: Enqueued:{actualAuxPointsEnqueued} Received:{actualAuxPointsReceived}"
                );
                Log.Trace(
                    $"SliceCount:{sliceCount} - AuxData: Enqueued:{actualPricePointsEnqueued} Received:{actualCustomPointsReceived}"
                );

                Assert.IsTrue(actualPricePointsEnqueued > 0);

                if (resolution == Resolution.Tick)
                {
                    if (symbol.SecurityType == SecurityType.Base)
                    {
                        Assert.IsTrue(actualTicksReceived == 0);
                    }
                    else
                    {
                        Assert.IsTrue(actualTicksReceived > 0);
                    }
                }
                else
                {
                    switch (symbol.SecurityType)
                    {
                        case SecurityType.Equity:
                            Assert.IsTrue(actualTradeBarsReceived > 0);
                            if (resolution == Resolution.Daily || resolution == Resolution.Hour)
                            {
                                Assert.IsTrue(actualQuoteBarsReceived == 0);
                            }
                            else
                            {
                                Assert.IsTrue(actualQuoteBarsReceived > 0);

                            }

                            break;

                        case SecurityType.Forex:
                        case SecurityType.Cfd:
                            Assert.IsTrue(actualQuoteBarsReceived > 0);
                            break;

                        case SecurityType.Crypto:
                        case SecurityType.Option:
                        case SecurityType.Future:
                            Assert.IsTrue(actualTradeBarsReceived > 0);
                            Assert.IsTrue(actualQuoteBarsReceived > 0);
                            break;

                        case SecurityType.Base:
                            Assert.IsTrue(actualCustomPointsReceived > 0);
                            break;
                    }
                }

                Assert.AreEqual(expectedTicksReceived, actualTicksReceived);
                Assert.AreEqual(expectedTradeBarsReceived, actualTradeBarsReceived);
                Assert.AreEqual(expectedQuoteBarsReceived, actualQuoteBarsReceived);
                Assert.AreEqual(expectedAuxPointsReceived, actualAuxPointsReceived);
                Assert.AreEqual(expectedCustomPointsReceived, actualCustomPointsReceived);

                dataManager.RemoveAllSubscriptions();
                _dataQueueHandler.DisposeSafely();
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                if (!shouldThrowException)
                {
                    throw;
                }
            }
            finally
            {
                dataManager?.RemoveAllSubscriptions();
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
                _dataQueueHandler.DisposeSafely();
                _feed?.Exit();
            }
        }

        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Future)]
        public void HandlesFutureAndOptionChainUniverse(SecurityType securityType)
        {
            Log.DebuggingEnabled = LogsEnabled;

            // startDate and endDate are in algorithm time zone
            var startDate = new DateTime(2019, 11, 19, 4, 0, 0);
            var endDate = startDate.AddDays(2.3);

            var algorithmTimeZone = TimeZones.NewYork;
            DateTimeZone exchangeTimeZone = null;

            var timeProvider = new ManualTimeProvider(algorithmTimeZone);
            timeProvider.SetCurrentTime(startDate);

            var lastTime = DateTime.MinValue;
            var timeAdvanceStep = TimeSpan.FromMinutes(120);
            var timeAdvanced = new AutoResetEvent(true);
            var started = new ManualResetEvent(false);
            var lookupCount = 0;

            var optionSymbol1 = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 192m, new DateTime(2019, 12, 19));
            var optionSymbol2 = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 192m, new DateTime(2019, 12, 19));

            var futureSymbol1 = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2019, 12, 19));
            var futureSymbol2 = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 3, 19));

            Symbol canonicalOptionSymbol = null;
            Exception lookupSymbolsException = null;

            var futureSymbols = new HashSet<Symbol>();
            var optionSymbols = new HashSet<Symbol>();

            _dataQueueHandler = new FuncDataQueueHandlerUniverseProvider(
                fdqh =>
                {
                    started.Set();
                    if (!timeAdvanced.WaitOne(TimeSpan.FromMilliseconds(5000)))
                    {
                        Log.Error("Timeout waiting for time to advance");
                        return Enumerable.Empty<BaseData>();
                    }

                    if (exchangeTimeZone == null)
                    {
                        return Enumerable.Empty<BaseData>();
                    }

                    var utcTime = timeProvider.GetUtcNow();
                    var exchangeTime = utcTime.ConvertFromUtc(exchangeTimeZone);
                    if (exchangeTime == lastTime ||
                        exchangeTime > endDate.ConvertTo(algorithmTimeZone, exchangeTimeZone))
                    {
                        return Enumerable.Empty<BaseData>();
                    }

                    lastTime = exchangeTime;

                    var dataPoints = new List<BaseData>
                    {
                        new Tick
                        {
                            Symbol = Symbols.SPY,
                            Time = exchangeTime,
                            EndTime = exchangeTime,
                            TickType = TickType.Trade,
                            Value = 100,
                            Quantity = 1
                        }
                    };

                    if (securityType == SecurityType.Option)
                    {
                        dataPoints.Add(new Tick
                        {
                            Symbol = canonicalOptionSymbol,
                            Time = exchangeTime,
                            EndTime = exchangeTime,
                            TickType = TickType.Trade,
                            Value = 100,
                            Quantity = 1
                        });
                    }

                    dataPoints.AddRange(
                        futureSymbols.Select(
                            symbol => new Tick
                            {
                                Symbol = symbol,
                                Time = exchangeTime,
                                EndTime = exchangeTime,
                                TickType = TickType.Trade,
                                Value = 100,
                                Quantity = 1
                            }));

                    dataPoints.AddRange(
                        optionSymbols.Select(
                            symbol => new Tick
                            {
                                Symbol = symbol,
                                Time = exchangeTime,
                                EndTime = exchangeTime,
                                TickType = TickType.Trade,
                                Value = 100,
                                Quantity = 1
                            }));

                    Log.Debug($"DQH: Emitting data point(s) at {utcTime.ConvertFromUtc(algorithmTimeZone)} ({algorithmTimeZone})");

                    return dataPoints;
                },

                // LookupSymbols
                (symbol, includeExpired, securityCurrency) =>
                {
                    lookupCount++;

                    var utcTime = timeProvider.GetUtcNow();
                    var time = utcTime.ConvertFromUtc(algorithmTimeZone);

                    var isValidTime = time.Hour >= 1 && time.Hour < 23;

                    Log.Trace($"LookupSymbols() called at {time} ({algorithmTimeZone}) - valid: {isValidTime}");

                    if (!isValidTime)
                    {
                        lookupSymbolsException = new Exception($"Invalid LookupSymbols call time: {time} ({algorithmTimeZone})");
                    }

                    time = utcTime.ConvertFromUtc(exchangeTimeZone);

                    switch (symbol.SecurityType)
                    {
                        case SecurityType.Option:
                            return time.Day == 19
                                ? new List<Symbol> { optionSymbol1 }
                                : new List<Symbol> { optionSymbol1, optionSymbol2 };

                        case SecurityType.Future:
                            return time.Day == 19
                                ? new List<Symbol> { futureSymbol1 }
                                : new List<Symbol> { futureSymbol1, futureSymbol2 };
                    }

                    return Enumerable.Empty<Symbol>();
                },

                // CanAdvanceTime
                secType =>
                {
                    var time = timeProvider.GetUtcNow().ConvertFromUtc(algorithmTimeZone);
                    var result = time.Hour >= 1 && time.Hour < 23 && time.Day != 21;

                    Log.Debug($"CanAdvanceTime() called at {time} ({algorithmTimeZone}), returning {result}");

                    return result;
                },
                timeProvider);

            _feed = new TestableLiveTradingDataFeed(_dataQueueHandler);

            var algorithm = new QCAlgorithm();
            algorithm.SetDateTime(timeProvider.GetUtcNow());
            algorithm.SetBenchmark(t => 0);

            var historyProvider = new Mock<IHistoryProvider>();
            historyProvider.Setup(
                    m => m.GetHistory(It.IsAny<IEnumerable<Data.HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns(Enumerable.Empty<Slice>());
            algorithm.SetHistoryProvider(historyProvider.Object);

            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var securityService = new SecurityService(
                algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(algorithm.Portfolio));
            algorithm.Securities.SetSecurityService(securityService);
            var dataPermissionManager = new DataPermissionManager();
            var dataProvider = new DefaultDataProvider();
            var dataManager = new DataManager(_feed,
                new UniverseSelection(algorithm, securityService, dataPermissionManager, dataProvider),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                true,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);
            algorithm.SubscriptionManager.SetDataManager(dataManager);
            algorithm.SetLiveMode(true);

            var mock = new Mock<ITransactionHandler>();
            mock.Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>())).Returns(new List<Order>());
            algorithm.Transactions.SetOrderProcessor(mock.Object);

            _synchronizer = new TestableLiveSynchronizer(timeProvider, 500);
            _synchronizer.Initialize(algorithm, dataManager);

            if (securityType == SecurityType.Option)
            {
                algorithm.AddEquity("SPY", Resolution.Minute);
                var option = algorithm.AddOption("SPY", Resolution.Minute, Market.USA);
                option.SetFilter(x => x);
                exchangeTimeZone = option.Exchange.TimeZone;

                canonicalOptionSymbol = option.Symbol;
            }
            else if (securityType == SecurityType.Future)
            {
                var future = algorithm.AddFuture(Futures.Indices.SP500EMini, Resolution.Minute);
                // Must include weeklys because the contracts returned by the lookup, futureSymbol1 & futureSymbol2, are non-standard
                future.SetFilter(x => x.IncludeWeeklys());
                exchangeTimeZone = future.Exchange.TimeZone;
            }
            else
            {
                throw new NotSupportedException($"Unsupported security type: {securityType}");
            }

            var mapFileProvider = new LocalDiskMapFileProvider();
            _feed.Initialize(algorithm, new LiveNodePacket(), new BacktestingResultHandler(),
                mapFileProvider, new LocalDiskFactorFileProvider(mapFileProvider), dataProvider,
                dataManager, _synchronizer, new TestDataChannelProvider());

            var cancellationTokenSource = new CancellationTokenSource();

            algorithm.PostInitialize();

            DateTime? lastSecurityChangedTime = null;

            if (!started.WaitOne(TimeSpan.FromMilliseconds(5000)))
            {
                throw new TimeoutException("Timeout waiting for IDQH to start");
            }

            var interval = TimeSpan.FromMilliseconds(100);
            Timer timer = null;
            timer = new Timer(
                _ =>
                {
                    // stop the timer to prevent reentrancy
                    timer.Change(Timeout.Infinite, Timeout.Infinite);

                    timeProvider.Advance(timeAdvanceStep);
                    Log.Debug($"Time advanced to {timeProvider.GetUtcNow()} (UTC)");
                    timeAdvanced.Set();

                    // restart the timer
                    timer.Change(interval, interval);
                }, null, interval, interval);

            foreach (var timeSlice in _synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (timeSlice.IsTimePulse || !timeSlice.Slice.HasData && timeSlice.SecurityChanges == SecurityChanges.None)
                {
                    continue;
                }

                var futureContractCount = 0;
                var optionContractCount = 0;

                if (securityType == SecurityType.Future)
                {
                    foreach (var futureChain in timeSlice.Slice.FutureChains.Values)
                    {
                        var symbols = futureChain.Contracts.Values.Select(x => x.Symbol).ToList();
                        futureContractCount += symbols.Count;
                        Log.Debug($"{timeSlice.Time} - future contracts: {string.Join(",", symbols)}");
                    }
                    Log.Debug($"{timeSlice.Time} - future symbols: {string.Join(",", futureSymbols)}");
                }
                else if (securityType == SecurityType.Option)
                {
                    foreach (var optionChain in timeSlice.Slice.OptionChains.Values)
                    {
                        var symbols = optionChain.Contracts.Values.Select(x => x.Symbol).ToList();
                        optionContractCount += symbols.Count;
                        Log.Debug($"{timeSlice.Time} - option contracts: {string.Join(",", symbols)}");
                    }
                    Log.Debug($"{timeSlice.Time} - option symbols: {string.Join(",", optionSymbols)}");
                }

                if (lastSecurityChangedTime != null &&
                    timeSlice.Time > lastSecurityChangedTime.Value.Add(timeAdvanceStep))
                {
                    if (securityType == SecurityType.Future)
                    {
                        Assert.AreEqual(futureSymbols.Count, futureContractCount);

                        foreach (var symbol in futureSymbols)
                        {
                            Assert.IsTrue(timeSlice.Slice.ContainsKey(symbol));
                        }
                    }

                    if (securityType == SecurityType.Option && timeSlice.Slice.OptionChains.Values.Count > 0)
                    {
                        Assert.AreEqual(optionSymbols.Count, optionContractCount);

                        foreach (var symbol in optionSymbols)
                        {
                            Assert.IsTrue(timeSlice.Slice.ContainsKey(symbol));
                        }
                    }
                }

                foreach (var security in timeSlice.SecurityChanges.AddedSecurities)
                {
                    if (security.Symbol.SecurityType == SecurityType.Future)
                    {
                        lastSecurityChangedTime = timeSlice.Time;
                        Log.Debug($"{timeSlice.Time} - Adding future symbol: {security.Symbol}");
                        futureSymbols.Add(security.Symbol);
                    }
                    else if (security.Symbol.SecurityType == SecurityType.Option)
                    {
                        lastSecurityChangedTime = timeSlice.Time;
                        Log.Debug($"{timeSlice.Time} - Adding option symbol: {security.Symbol}");
                        optionSymbols.Add(security.Symbol);
                    }
                }

                foreach (var security in timeSlice.SecurityChanges.RemovedSecurities)
                {
                    if (security.Symbol.SecurityType == SecurityType.Future)
                    {
                        lastSecurityChangedTime = timeSlice.Time;
                        Log.Debug($"{timeSlice.Time} - Removing future symbol: {security.Symbol}");
                        futureSymbols.Remove(security.Symbol);
                    }
                    else if (security.Symbol.SecurityType == SecurityType.Option)
                    {
                        lastSecurityChangedTime = timeSlice.Time;
                        Log.Debug($"{timeSlice.Time} - Removing option symbol: {security.Symbol}");
                        optionSymbols.Remove(security.Symbol);
                    }
                }

                algorithm.OnEndOfTimeStep();

                foreach (var baseDataCollection in timeSlice.UniverseData.Values)
                {
                    var symbols = string.Join(",", baseDataCollection.Data.Select(x => x.Symbol));
                    Log.Debug($"{timeSlice.Time} - universe data: {symbols}");
                }

                var currentTime = timeProvider.GetUtcNow();
                algorithm.SetDateTime(currentTime);

                Log.Debug($"{timeSlice.Time} - Algorithm time set to {currentTime.ConvertFromUtc(algorithmTimeZone)} ({algorithmTimeZone})");

                if (currentTime.ConvertFromUtc(algorithmTimeZone) > endDate)
                {
                    _feed.Exit();
                    cancellationTokenSource.Cancel();
                    break;
                }
            }

            if (lookupSymbolsException != null)
            {
                throw lookupSymbolsException;
            }

            Assert.AreEqual(2, lookupCount, "LookupSymbols call count mismatch");

            if (securityType == SecurityType.Future)
            {
                Assert.AreEqual(2, futureSymbols.Count, "Future symbols count mismatch");
            }
            else if (securityType == SecurityType.Option)
            {
                Assert.AreEqual(2, optionSymbols.Count, "Option symbols count mismatch");
            }

            dataManager.RemoveAllSubscriptions();
            _dataQueueHandler.DisposeSafely();
            timeAdvanced.DisposeSafely();
            started.DisposeSafely();
            timer.DisposeSafely();
        }
    }

    internal class TestableLiveTradingDataFeed : LiveTradingDataFeed
    {
        public IDataQueueHandler DataQueueHandler;

        public TestableLiveTradingDataFeed(IDataQueueHandler dataQueueHandler = null)
        {
            DataQueueHandler = dataQueueHandler;
        }

        protected override IDataQueueHandler GetDataQueueHandler()
        {
            return DataQueueHandler;
        }

        public override void Exit()
        {
            base.Exit();
            DataQueueHandler.DisposeSafely();
        }
    }

    internal class TestDataChannelProvider : DataChannelProvider
    {
        public override bool ShouldStreamSubscription(LiveNodePacket job, SubscriptionDataConfig config)
        {
            if (config.Type == typeof(TiingoNews))
            {
                return true;
            }
            return base.ShouldStreamSubscription(job, config);
        }
    }


    internal class TestableLiveSynchronizer : LiveSynchronizer
    {
        private readonly ITimeProvider _timeProvider;
        private readonly int _newLiveDataTimeout;

        public ManualResetEventSlim NewDataEvent { get; set; }

        public TestableLiveSynchronizer(ITimeProvider timeProvider = null, int? newLiveDataTimeout = null)
        {
            NewDataEvent = new ManualResetEventSlim(true);
            _timeProvider = timeProvider ?? new RealTimeProvider();
            _newLiveDataTimeout = newLiveDataTimeout ?? 500;
        }

        protected override int GetPulseDueTime(DateTime now)
        {
            return _newLiveDataTimeout;
        }

        protected override ITimeProvider GetTimeProvider()
        {
            return _timeProvider;
        }

        protected override void OnSubscriptionNewDataAvailable(object sender, EventArgs args)
        {
            base.OnSubscriptionNewDataAvailable(sender, args);
            NewDataEvent.Set();
        }
    }

    internal class TestCustomData : BaseData
    {
        public static int ReaderCallsCount;

        public static bool ReturnNull { get; set; }

        public static bool ThrowException { get; set; }

        public static FileFormat FileFormat { get; set; }

        static TestCustomData()
        {
            ReaderCallsCount = 0;
            FileFormat = FileFormat.Csv;
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            Interlocked.Increment(ref ReaderCallsCount);
            if (ThrowException)
            {
                throw new Exception("Custom data Reader threw exception");
            }
            else if (ReturnNull)
            {
                return null;
            }
            else
            {
                var data = new TestCustomData
                {
                    // return not null but 'old data' -> there is no data yet available for today
                    Time = date.AddHours(-100),
                    Value = 1,
                    Symbol = config.Symbol
                };

                if (FileFormat == FileFormat.Collection)
                {
                    return new BaseDataCollection
                    {
                        Time = date.AddHours(-100),
                        // return not null but 'old data' -> there is no data yet available for today
                        EndTime = date.AddHours(-99),
                        Value = 1,
                        Symbol = config.Symbol,
                        Data = new List<BaseData> { data }
                    };
                }
                return data;
            }
        }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource("localhost:1232/fake",
                SubscriptionTransportMedium.Rest,
                FileFormat);
        }
    }
}
