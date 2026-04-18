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
using Microsoft.CodeAnalysis;
using Moq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Util;
using static QuantConnect.Tests.Engine.DataFeeds.Enumerators.LiveSubscriptionEnumeratorTests;

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
            {typeof(IndexedLinkedData), typeof(IndexedLinkedData).GetBaseDataInstance() },
            {typeof(IndexedLinkedData2), typeof(IndexedLinkedData2).GetBaseDataInstance() },
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

            FundamentalService.Initialize(TestGlobals.DataProvider, new TestFundamentalDataProviderTrue(), false);
        }

        [TearDown]
        public void TearDown()
        {
            _dataManager?.RemoveAllSubscriptions();
            _feed?.Exit();
            _synchronizer.DisposeSafely();
            _dataQueueHandler?.DisposeSafely();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmitsStreamedDailyData(bool strictEndTimes)
        {
            _startDate = new DateTime(2014, 3, 27, 14, 0, 0);
            _algorithm.SetStartDate(_startDate);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate.ConvertToUtc(TimeZones.NewYork));

            var symbol = Symbols.SPY;
            _algorithm.Settings.DailyPreciseEndTime = strictEndTimes;
            _algorithm.SetBenchmark(x => 1);

            var dqh = new TestDataQueueHandler
            {
                DataPerSymbol = new Dictionary<Symbol, List<BaseData>>
                {
                    {
                        symbol, new List<BaseData> { new TradeBar(_algorithm.StartDate, symbol, 1, 5, 1, 3, 100, Time.OneDay) }
                    }
                }
            };
            var feed = RunDataFeed(Resolution.Daily, dataQueueHandler: dqh, equities: new() { "SPY" });
            _algorithm.OnEndOfTimeStep();

            DateTime emittedDataTime = default;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    var data = ts.Slice[Symbols.SPY];
                    if (data == null)
                    {
                        return;
                    }
                    emittedDataTime = _algorithm.Time;
                    // short cut
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            }, endDate: _startDate.AddDays(1),
            secondsTimeStep: 60 * 10);

            Assert.AreEqual(strictEndTimes ? _startDate.Date.AddHours(16) : _startDate.Date.AddDays(1), emittedDataTime);
        }

        [TestCase(false, true)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(true, false)]
        public void EmitsLeanAggregatedDailyData(bool strictEndTimes, bool warmup)
        {
            _startDate = new DateTime(2014, 3, 27, 10, 0, 0);
            _algorithm.Settings.DailyPreciseEndTime = strictEndTimes;
            _algorithm.SetStartDate(_startDate);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate.ConvertToUtc(TimeZones.NewYork));
            var endDate = _startDate.AddDays(1);

            _algorithm.SetBenchmark(x => 1);
            if (warmup)
            {
                _algorithm.SetWarmUp(TimeSpan.FromDays(2));
            }
            var feed = RunDataFeed();
            _algorithm.AddEquity("SPY", Resolution.Daily);
            _algorithm.OnEndOfTimeStep();

            List<DateTime> emittedDataTime = new();
            List<BaseData> emittedData = new();
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    var data = ts.Slice[Symbols.SPY];
                    if (data == null)
                    {
                        return;
                    }
                    emittedDataTime.Add(_algorithm.Time);
                    emittedData.Add(data);

                    if (warmup && emittedData.Count == 3 || !warmup && emittedData.Count == 1)
                    {
                        // short cut
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            }, endDate: endDate,
            secondsTimeStep: 60 * 60);

            for (var i = 0; i < emittedDataTime.Count; i++)
            {
                Assert.AreEqual(emittedDataTime[i], emittedData[i].EndTime);
            }

            if (warmup)
            {
                Assert.AreEqual(3, emittedData.Count);
                Assert.AreEqual(strictEndTimes ? _startDate.Date.AddDays(-2).AddHours(16) : _startDate.Date.AddDays(-1), emittedData[0].EndTime);
                Assert.AreEqual(strictEndTimes ? _startDate.Date.AddDays(-1).AddHours(16) : _startDate.Date, emittedData[1].EndTime);
            }
            else
            {
                Assert.AreEqual(1, emittedData.Count);
            }
            Assert.AreEqual(strictEndTimes ? _startDate.Date.AddHours(16) : _startDate.Date.AddDays(1), emittedData.Last().EndTime);
        }

        [TestCase(SecurityType.Option, Resolution.Daily, 7, true)]
        [TestCase(SecurityType.Future, Resolution.Daily, 11, true)]
        [TestCase(SecurityType.IndexOption, Resolution.Daily, 14, true)]
        [TestCase(SecurityType.Option, Resolution.Daily, 14, true)]
        [TestCase(SecurityType.Future, Resolution.Daily, 120, true)]

        [TestCase(SecurityType.Option, Resolution.Daily, 7, false)]
        [TestCase(SecurityType.Future, Resolution.Daily, 11, false)]
        [TestCase(SecurityType.IndexOption, Resolution.Daily, 14, false)]
        [TestCase(SecurityType.Option, Resolution.Hour, 7, false)]
        [TestCase(SecurityType.Future, Resolution.Hour, 11, false)]
        [TestCase(SecurityType.IndexOption, Resolution.Hour, 14, false)]
        [TestCase(SecurityType.Option, Resolution.Minute, 7, false)]
        [TestCase(SecurityType.Future, Resolution.Minute, 11, false)]
        [TestCase(SecurityType.IndexOption, Resolution.Minute, 14, false)]
        [TestCase(SecurityType.Option, Resolution.Second, 7, false)]
        [TestCase(SecurityType.Future, Resolution.Second, 11, false)]
        [TestCase(SecurityType.IndexOption, Resolution.Second, 14, false)]
        [TestCase(SecurityType.Option, Resolution.Tick, 7, false)]
        [TestCase(SecurityType.Future, Resolution.Tick, 11, false)]
        [TestCase(SecurityType.IndexOption, Resolution.Tick, 14, false)]
        [TestCase(SecurityType.Option, Resolution.Daily, 14, false)]
        [TestCase(SecurityType.Future, Resolution.Daily, 120, false)]
        [TestCase(SecurityType.Option, Resolution.Hour, 14, false)]
        [TestCase(SecurityType.Future, Resolution.Hour, 120, false)]
        [TestCase(SecurityType.Option, Resolution.Minute, 14, false)]
        [TestCase(SecurityType.Future, Resolution.Minute, 120, false)]
        [TestCase(SecurityType.Option, Resolution.Second, 14, false)]
        [TestCase(SecurityType.Future, Resolution.Second, 120, false)]
        [TestCase(SecurityType.Option, Resolution.Tick, 14, false)]
        [TestCase(SecurityType.Future, Resolution.Tick, 120, false)]
        public void LiveChainSelection(SecurityType securityType, Resolution resolution, int expirationDatesFilter, bool strictEndTimes)
        {
            _startDate = securityType == SecurityType.IndexOption ? new DateTime(2021, 1, 4) : new DateTime(2014, 6, 9);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);
            var endDate = _startDate.AddDays(securityType == SecurityType.Future ? 5 : 1);

            _algorithm.SetBenchmark(x => 1);
            _algorithm.Settings.DailyPreciseEndTime = strictEndTimes;

            var feed = RunDataFeed();

            var selectionHappened = 0;
            if (securityType.IsOption())
            {
                var chainAsset = securityType == SecurityType.Option
                    ? _algorithm.AddOption("AAPL", resolution)
                    : _algorithm.AddIndexOption("SPX", resolution);
                chainAsset.SetFilter(x =>
                {
                    selectionHappened++;
                    var count = 0;
                    var symbols = x.Expiration(0, expirationDatesFilter).IncludeWeeklys().OnlyApplyFilterAtMarketOpen().Where(x => count++ < 2).ToList();
                    Assert.AreEqual(2, symbols.Count);
                    return x;
                });
            }
            else
            {
                var chainAsset = _algorithm.AddFuture("ES", resolution);
                chainAsset.SetFilter(x =>
                {
                    selectionHappened++;
                    var symbols = x.Expiration(0, expirationDatesFilter).IncludeWeeklys().ToList();
                    Assert.AreEqual(expirationDatesFilter < 30 ? 1 : 2, symbols.Count);
                    return x;
                });
            }
            _algorithm.OnEndOfTimeStep();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (selectionHappened == 2)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            var expectedSelections = securityType == SecurityType.Future ? 2 : 1;
            Assert.AreEqual(expectedSelections, selectionHappened);
        }

        [Test]
        public void ContinuousFuturesImmediateSelection()
        {
            _startDate = new DateTime(2013, 10, 7, 12, 0, 0);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(5);

            _algorithm.SetBenchmark(x => 1);

            var feed = RunDataFeed(runPostInitialize: false);

            var esSelectionTime = DateTime.MinValue;
            var esFuture = _algorithm.AddFuture("ES", Resolution.Minute, extendedMarketHours: true);
            esFuture.SetFilter(x =>
            {
                esSelectionTime = x.LocalTime.ConvertToUtc(esFuture.Exchange.TimeZone);

                Assert.IsNotEmpty(x);

                return x;
            });

            // DC future time zone is Chicago while ES is New York, we need to assert that both selection happen right away
            var dcSelectionTime = DateTime.MinValue;
            var dcFuture = _algorithm.AddFuture("DC", Resolution.Minute, extendedMarketHours: true);
            dcFuture.SetFilter(x =>
            {
                dcSelectionTime = x.LocalTime.ConvertToUtc(dcFuture.Exchange.TimeZone);

                Assert.IsNotEmpty(x);

                return x;
            });

            _algorithm.PostInitialize();

            Assert.IsNull(esFuture.Mapped);
            Assert.IsNull(dcFuture.Mapped);

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            var timeSliceCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                timeSliceCount++;
                if (esFuture.Mapped != null && dcFuture.Mapped != null)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            // Continuous futures should select the first contract immediately
            Assert.IsNotNull(esFuture.Mapped);
            Assert.IsNotNull(dcFuture.Mapped);

            Assert.AreEqual(startDateUtc, esSelectionTime);
            Assert.AreEqual(startDateUtc, dcSelectionTime);

            Assert.AreEqual(1, timeSliceCount);
        }

        [Test]
        public void ETFsImmediateSelection()
        {
            _startDate = new DateTime(2020, 12, 1, 1, 0, 0);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(5);

            _algorithm.SetBenchmark(x => 1);

            var feed = RunDataFeed(runPostInitialize: false);

            var spy = _algorithm.AddEquity("SPY").Symbol;

            var selectionTime = DateTime.MinValue;
            List<Symbol> constituents = null;

            var universe = _algorithm.AddUniverse(_algorithm.Universe.ETF(spy, constituentsData =>
            {
                selectionTime = _algorithm.UtcTime;
                constituents = constituentsData.Select(x => x.Symbol).ToList();
                return constituents;
            }));

            _algorithm.PostInitialize();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            var timeSliceCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                timeSliceCount++;
                if (selectionTime != DateTime.MinValue)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            Assert.AreEqual(startDateUtc, selectionTime);
            Assert.AreEqual(1, timeSliceCount);

            Assert.IsNotNull(constituents);
            Assert.IsNotEmpty(constituents);

            CollectionAssert.AreEquivalent(constituents, universe.Members.Keys);

            // The algorithm's security collection has all constituents and SPY (added manually)
            constituents.Add(spy);
            CollectionAssert.AreEquivalent(constituents, _algorithm.Securities.Keys);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FundamentalScheduleSelection(bool warmup)
        {
            _startDate = new DateTime(2014, 3, 27, 9, 0, 0);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(10);
            _algorithm.SetStartDate(_startDate);

            _algorithm.SetBenchmark(x => 1);
            if (warmup)
            {
                _algorithm.SetWarmUp(TimeSpan.FromDays(2));
            }
            _algorithm.UniverseSettings.Schedule.On(_algorithm.DateRules.On(new DateTime(2014, 3, 24), new DateTime(2014, 3, 25),
                new DateTime(2014, 3, 28), new DateTime(2014, 4, 3)));

            var feed = RunDataFeed(runPostInitialize: false);

            var selectionTime = DateTime.MinValue;

            var selectionAlgoTime = new List<DateTime>();
            var selectionDataTime = new List<DateTime>();
            IEnumerable<Symbol> Filter(IEnumerable<Fundamental> fundamentals)
            {
                selectionAlgoTime.Add(_algorithm.Time.Date);
                var dataPoint = fundamentals.Take(1);
                selectionDataTime.Add(dataPoint.First().EndTime);
                return dataPoint.Select(x => x.Symbol);
            }

            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            var universe = _algorithm.AddUniverse(Filter);

            _algorithm.PostInitialize();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            ConsumeBridge(feed, TimeSpan.FromSeconds(500), true, ts =>
            {
                if (selectionTime != DateTime.MinValue)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60 * 12);

            Assert.AreEqual(3, selectionAlgoTime.Count, string.Join(",", selectionAlgoTime));
            var index = 0;
            if (warmup)
            {
                // warmup start time
                Assert.AreEqual(new DateTime(2014, 3, 25), selectionAlgoTime[index++]);
            }
            else
            {
                // triggers right away, outside of schedule
                Assert.AreEqual(new DateTime(2014, 3, 27), selectionAlgoTime[index++]);
            }
            Assert.AreEqual(new DateTime(2014, 3, 28), selectionAlgoTime[index++]);
            Assert.AreEqual(new DateTime(2014, 4, 3), selectionAlgoTime[index++]);
        }

        [Test]
        public void CoarseFundamentalsImmediateSelection()
        {
            _startDate = new DateTime(2014, 03, 26, 9, 0, 0);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(5);

            _algorithm.SetBenchmark(x => 1);

            var feed = RunDataFeed(runPostInitialize: false);

            var selectionTime = DateTime.MinValue;
            List<Symbol> selectedSymbols = null;

            IEnumerable<Symbol> CoarseFilter(IEnumerable<CoarseFundamental> coarse)
            {
                selectionTime = _algorithm.UtcTime;
                selectedSymbols = coarse.Select(x => x.Symbol).ToList();
                return selectedSymbols;
            }

            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            var universe = _algorithm.AddUniverse(CoarseFilter);

            _algorithm.PostInitialize();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            var timeSliceCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                timeSliceCount++;
                if (selectionTime != DateTime.MinValue)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            Assert.AreEqual(startDateUtc, selectionTime);
            Assert.AreEqual(1, timeSliceCount);

            Assert.IsNotNull(selectedSymbols);
            Assert.IsNotEmpty(selectedSymbols);
            CollectionAssert.AreEquivalent(selectedSymbols, universe.Members.Keys);
            CollectionAssert.AreEquivalent(selectedSymbols, _algorithm.Securities.Keys);
        }

        [Test]
        public void FutureChainsImmediateSelection()
        {
            _startDate = new DateTime(2014, 6, 9, 12, 0, 0);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(5);

            _algorithm.SetBenchmark(x => 1);

            var feed = RunDataFeed(runPostInitialize: false);

            var firstSelectionTimeUtc = DateTime.MinValue;
            List<Symbol> selectedSymbols = null;

            var future = _algorithm.AddFuture("ES");
            future.SetFilter(universe =>
            {
                firstSelectionTimeUtc = universe.LocalTime.ConvertToUtc(future.Exchange.TimeZone);
                selectedSymbols = universe.Data.Select(x => x.Symbol).ToList();

                return universe;
            });

            _algorithm.PostInitialize();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            var timeSliceCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                timeSliceCount++;
                if (firstSelectionTimeUtc != DateTime.MinValue)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            Assert.AreEqual(startDateUtc, firstSelectionTimeUtc);
            Assert.AreEqual(1, timeSliceCount);
            Assert.IsNotNull(selectedSymbols);
            Assert.IsNotEmpty(selectedSymbols);
        }

        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.IndexOption)]
        public void OptionChainImmediateSelection(SecurityType securityType)
        {
            _startDate = securityType == SecurityType.Option
                ? new DateTime(2015, 12, 24, 12, 0, 0)
                : new DateTime(2021, 01, 04, 12, 0, 0);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(5);

            _algorithm.SetBenchmark(x => 1);

            var feed = RunDataFeed(runPostInitialize: false);

            var firstSelectionTimeUtc = DateTime.MinValue;
            List<Symbol> selectedSymbols = null;

            var option = securityType == SecurityType.Option
                ? _algorithm.AddOption("GOOG")
                : _algorithm.AddIndexOption("SPX");
            option.SetFilter(universe =>
            {
                firstSelectionTimeUtc = universe.LocalTime.ConvertToUtc(option.Exchange.TimeZone);
                selectedSymbols = (List<Symbol>)universe;

                return universe;
            });

            _algorithm.PostInitialize();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            var timeSliceCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), true, ts =>
            {
                timeSliceCount++;
                if (firstSelectionTimeUtc != default)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60);

            Assert.AreEqual(startDateUtc, firstSelectionTimeUtc);
            Assert.GreaterOrEqual(timeSliceCount, 1);
            Assert.IsNotNull(selectedSymbols);
            Assert.IsNotEmpty(selectedSymbols);
        }

        [Test]
        public void CustomUniverseImmediateSelection()
        {
            _startDate = new DateTime(2013, 10, 07);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(5);

            _algorithm.SetBenchmark(x => 1);

            var feed = RunDataFeed(runPostInitialize: false);

            var selectionTime = DateTime.MinValue;

            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            var universe = _algorithm.AddUniverse(SecurityType.Equity,
                "my-custom-universe",
                Resolution.Daily,
                Market.USA,
                _algorithm.UniverseSettings,
                time =>
                {
                    selectionTime = _algorithm.UtcTime;
                    return new[] { "SPY", "GOOG", "APPL" };
                });

            _algorithm.PostInitialize();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            var timeSliceCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                timeSliceCount++;
                if (selectionTime != DateTime.MinValue)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            var expectedSymbols = new List<Symbol>()
            {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA),
                Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                Symbol.Create("APPL", SecurityType.Equity, Market.USA)
            };

            Assert.AreEqual(startDateUtc, selectionTime);
            Assert.AreEqual(1, timeSliceCount);

            CollectionAssert.AreEquivalent(expectedSymbols, universe.Members.Keys);
            CollectionAssert.AreEquivalent(expectedSymbols, _algorithm.Securities.Keys);
        }

        [Test]
        public void CustomDataUniverseImmediateSelection()
        {
            _startDate = new DateTime(2014, 03, 26, 11, 0, 0);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(5);

            _algorithm.SetBenchmark(x => 1);

            var feed = RunDataFeed(runPostInitialize: false);

            var selectionTime = DateTime.MinValue;
            List<Symbol> selectedSymbols = null;

            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            var universe = _algorithm.AddUniverse<CoarseFundamental>("my-custom-coarse-universe", stockDataSource =>
            {
                selectionTime = _algorithm.UtcTime;
                selectedSymbols = stockDataSource.Select(x => x.Symbol).ToList();

                return selectedSymbols;
            });

            _algorithm.PostInitialize();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            var timeSliceCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                timeSliceCount++;
                if (selectionTime != DateTime.MinValue)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            Assert.AreEqual(startDateUtc, selectionTime);
            Assert.AreEqual(1, timeSliceCount);

            Assert.IsNotNull(selectedSymbols);
            Assert.IsNotEmpty(selectedSymbols);
            CollectionAssert.AreEquivalent(selectedSymbols, universe.Members.Keys);
            CollectionAssert.AreEquivalent(selectedSymbols, _algorithm.Securities.Keys);
        }

        [Test]
        public void ConstituentsImmediateSelection()
        {
            _startDate = new DateTime(2013, 10, 08);
            var startDateUtc = _startDate.ConvertToUtc(_algorithm.TimeZone);
            _manualTimeProvider.SetCurrentTimeUtc(startDateUtc);
            var endDate = _startDate.AddDays(5);

            _algorithm.SetBenchmark(x => 1);

            var feed = RunDataFeed(runPostInitialize: false);

            var selectionTime = DateTime.MinValue;
            List<Symbol> constituents = null;

            _algorithm.UniverseSettings.Resolution = Resolution.Daily;

            var customUniverseSymbol = new Symbol(
                SecurityIdentifier.GenerateConstituentIdentifier(
                    "constituents-universe-qctest",
                    SecurityType.Equity,
                    Market.USA),
                "constituents-universe-qctest");
            using var constituentsUniverse = new ConstituentsUniverse(customUniverseSymbol, _algorithm.UniverseSettings, x =>
            {
                selectionTime = _algorithm.UtcTime;
                constituents = x.Select(x => x.Symbol).ToList();

                return constituents;

            });

            var universe = _algorithm.AddUniverse(constituentsUniverse);

            _algorithm.PostInitialize();

            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);

            var timeSliceCount = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                timeSliceCount++;
                if (selectionTime != DateTime.MinValue)
                {
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            Assert.AreEqual(startDateUtc, selectionTime);
            Assert.AreEqual(1, timeSliceCount);

            Assert.IsNotNull(constituents);
            Assert.IsNotEmpty(constituents);

            CollectionAssert.AreEquivalent(constituents, universe.Members.Keys);
            CollectionAssert.AreEquivalent(constituents, _algorithm.Securities.Keys);
        }

        [TestCase(false, SecurityType.Option, Resolution.Hour, false)]
        [TestCase(true, SecurityType.Option, Resolution.Hour, false)]
        [TestCase(false, SecurityType.IndexOption, Resolution.Hour, false)]
        [TestCase(true, SecurityType.IndexOption, Resolution.Hour, false)]
        [TestCase(false, SecurityType.Option, Resolution.Daily, false)]
        [TestCase(true, SecurityType.Option, Resolution.Daily, false)]
        [TestCase(false, SecurityType.IndexOption, Resolution.Daily, false)]
        [TestCase(true, SecurityType.IndexOption, Resolution.Daily, false)]
        [TestCase(false, SecurityType.Option, Resolution.Daily, true)]
        [TestCase(true, SecurityType.Option, Resolution.Daily, true)]
        [TestCase(false, SecurityType.IndexOption, Resolution.Daily, true)]
        [TestCase(true, SecurityType.IndexOption, Resolution.Daily, true)]
        public void WarmupOptionSelection(bool useWarmupResolution, SecurityType securityType, Resolution resolution, bool strictEndTimes)
        {
            _startDate = securityType == SecurityType.Option ? new DateTime(2014, 6, 9) : new DateTime(2021, 1, 4);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var endDate = _startDate.AddDays(3);
            _algorithm.SetBenchmark(x => 1);

            _algorithm.Settings.DailyPreciseEndTime = strictEndTimes;
            if (useWarmupResolution)
            {
                _algorithm.SetWarmup(2, Resolution.Daily);
            }
            else
            {
                _algorithm.SetWarmup(TimeSpan.FromDays(2));
            }

            _algorithm.UniverseSettings.Resolution = resolution;
            var feed = RunDataFeed();
            // after algorithm initialization let's set the time provider time to reflect warmup window
            _manualTimeProvider.SetCurrentTimeUtc(_algorithm.UtcTime);

            var es = securityType == SecurityType.Option
                ? _algorithm.AddOption("AAPL", resolution)
                : _algorithm.AddIndexOption("SPX", resolution);
            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);
            var countWarmup = 0;
            var countLive = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.UniverseData?.Count > 0)
                {
                    Assert.IsNotNull(ts.UniverseData.Select(x => x.Value.Underlying));
                    Assert.IsNotEmpty(ts.UniverseData.Select(x => x.Value.FilteredContracts));
                    if (_algorithm.IsWarmingUp)
                    {
                        countWarmup++;
                    }
                    else
                    {
                        countLive++;
                        // we got what we wanted shortcut unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }

                if (_algorithm.IsWarmingUp && countWarmup == 0 || !_algorithm.IsWarmingUp && countLive == 0)
                {
                    Thread.Sleep(50);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60 * 4);

            Assert.AreNotEqual(0, countWarmup);
            Assert.AreNotEqual(0, countLive);
        }

        [Test]
        public void FutureLiveHoldingsFutureMapping()
        {
            _startDate = new DateTime(2013, 12, 15);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var endDate = _startDate.AddDays(1);
            _algorithm.SetBenchmark(x => 1);
            _algorithm.UniverseSettings.Resolution = Resolution.Hour;
            var feed = RunDataFeed();
            // after algorithm initialization let's set the time provider time to reflect warmup window
            _manualTimeProvider.SetCurrentTimeUtc(_algorithm.UtcTime);

            var es = _algorithm.AddFuture("ES");
            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);
            var assertedHoldings = false;
            var securityChanges = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(7), true, ts =>
            {
                if (ts.SecurityChanges != SecurityChanges.None)
                {
                    securityChanges++;
                }

                // let's wait till it's remapped
                if (securityChanges == 3)
                {
                    Assert.IsNotNull(_algorithm.Securities.Total.SingleOrDefault(sec => sec.IsTradable));
                    Assert.AreEqual(3, _algorithm.Securities.Total.Count);

                    var result = LiveTradingResultHandler.GetHoldings(_algorithm.Securities.Total, _algorithm.SubscriptionManager.SubscriptionDataConfigService);
                    // old future mapped contract is removed
                    Assert.AreEqual(2, result.Count);
                    Assert.IsTrue(result.TryGetValue(es.Symbol.ID.ToString(), out var holding));
                    Assert.IsTrue(result.TryGetValue(es.Mapped.ID.ToString(), out holding));

                    Assert.AreEqual(0, LiveTradingResultHandler.GetHoldings(_algorithm.Securities.Total, _algorithm.SubscriptionManager.SubscriptionDataConfigService, onlyInvested: true).Count);

                    _algorithm.RemoveSecurity(es.Symbol);
                    // allow time for the exchange to pick up the selection point
                    Thread.Sleep(150);
                }
                else if (securityChanges == 4)
                {
                    Assert.IsTrue(_algorithm.Securities.Total.All(sec => !sec.IsTradable));
                    Assert.AreEqual(3, _algorithm.Securities.Total.Count);

                    var result = LiveTradingResultHandler.GetHoldings(_algorithm.Securities.Total, _algorithm.SubscriptionManager.SubscriptionDataConfigService);
                    Assert.AreEqual(0, result.Count);

                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    assertedHoldings = true;
                }
            },
            endDate: _startDate.AddDays(10),
            secondsTimeStep: 60 * 60 * 8);

            Assert.IsTrue(assertedHoldings);
            Assert.AreEqual(4, securityChanges);
        }

        [Test]
        public void FutureLiveHoldings()
        {
            _startDate = new DateTime(2013, 10, 10);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var endDate = _startDate.AddDays(1);
            _algorithm.SetBenchmark(x => 1);
            _algorithm.UniverseSettings.Resolution = Resolution.Hour;
            var feed = RunDataFeed();
            // after algorithm initialization let's set the time provider time to reflect warmup window
            _manualTimeProvider.SetCurrentTimeUtc(_algorithm.UtcTime);

            var es = _algorithm.AddFuture("ES");
            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);
            var assertedHoldings = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.SecurityChanges != SecurityChanges.None)
                {
                    Assert.IsNotNull(_algorithm.Securities.Values.SingleOrDefault(sec => sec.IsTradable));
                    var result = LiveTradingResultHandler.GetHoldings(_algorithm.Securities.Values, _algorithm.SubscriptionManager.SubscriptionDataConfigService);

                    Assert.AreEqual(2, result.Count);
                    Assert.IsTrue(result.TryGetValue(es.Symbol.ID.ToString(), out var holding));
                    Assert.IsTrue(result.TryGetValue(es.Mapped.ID.ToString(), out holding));

                    Assert.AreEqual(0, LiveTradingResultHandler.GetHoldings(_algorithm.Securities.Values, _algorithm.SubscriptionManager.SubscriptionDataConfigService, onlyInvested: true).Count);

                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    assertedHoldings = true;
                }
            },
            endDate: _startDate.AddDays(1),
            secondsTimeStep: 60 * 60);

            Assert.IsTrue(assertedHoldings);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WarmupFutureSelection(bool useWarmupResolution)
        {
            _startDate = new DateTime(2013, 10, 10);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var endDate = _startDate.AddDays(1);
            _algorithm.SetBenchmark(x => 1);
            if (useWarmupResolution)
            {
                _algorithm.SetWarmup(2, Resolution.Daily);
            }
            else
            {
                _algorithm.SetWarmup(TimeSpan.FromDays(2));
            }
            _algorithm.UniverseSettings.Resolution = Resolution.Hour;
            var feed = RunDataFeed();
            // after algorithm initialization let's set the time provider time to reflect warmup window
            _manualTimeProvider.SetCurrentTimeUtc(_algorithm.UtcTime);

            var es = _algorithm.AddFuture("ES");
            // allow time for the exchange to pick up the selection point
            Thread.Sleep(50);
            var countWarmup = 0;
            var countLive = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.UniverseData?.Count > 0)
                {
                    Assert.IsNotEmpty(ts.UniverseData.Select(x => x.Value.FilteredContracts));
                    if (_algorithm.IsWarmingUp)
                    {
                        countWarmup++;
                    }
                    else
                    {
                        countLive++;
                        // we got what we wanted shortcut unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            },
            endDate: endDate,
            secondsTimeStep: 60 * 60);

            Assert.AreNotEqual(0, countWarmup);
            Assert.AreNotEqual(0, countLive);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WarmupExpiredContinuousFuture(bool useWarmupResolution)
        {
            _startDate = new DateTime(2014, 12, 1);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var endDate = _startDate.AddDays(30);
            var futureChainProvider = new BacktestingFutureChainProvider();
            futureChainProvider.Initialize(new(TestGlobals.MapFileProvider, TestGlobals.HistoryProvider));
            _algorithm.SetFutureChainProvider(futureChainProvider);
            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            if (useWarmupResolution)
            {
                _algorithm.SetWarmup(365, Resolution.Daily);
            }
            else
            {
                _algorithm.SetWarmup(TimeSpan.FromDays(365));
            }
            var feed = RunDataFeed(runPostInitialize: false);

            var continuousContract = _algorithm.AddFuture(Futures.Indices.SP500EMini, Resolution.Daily,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.LastTradingDay,
                contractDepthOffset: 0
            );
            // the expiration of this option contract is before the start date of the algorithm but we should still get some data during warmup
            continuousContract.SetFilter(0, 182);

            // Post initialize after securities are added (Initialize)
            _algorithm.PostInitialize();

            var emittedChainData = false;
            var emittedContinuousData = false;
            var assertedSubscriptions = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (_algorithm.IsWarmingUp)
                {
                    Assert.IsFalse(_dataQueueHandler.SubscriptionDataConfigs.Any(
                        // the data queue handler shouldn't of seen the expired subscription at any point
                        x => !x.Symbol.IsCanonical() && x.Symbol.SecurityType == SecurityType.Future && x.Symbol.ID.Date < _algorithm.StartDate));

                    if (ts.Slice.HasData)
                    {
                        emittedContinuousData |= ts.Slice.Keys.Any(s => s == continuousContract.Symbol
                            // let's assert that during warmup the continuous future got data of expired future
                            && continuousContract.Mapped.ID.Date < _algorithm.StartDate);
                        emittedChainData |= ts.Slice.Keys.Any(s => !s.IsCanonical() && s.SecurityType == SecurityType.Future
                            // let's assert that during warmup we got chain data of expired futures
                            && s.ID.Date < _algorithm.StartDate);
                    }
                }
                else
                {
                    Assert.IsTrue(_dataQueueHandler.SubscriptionDataConfigs.Any(
                        // the data queue handler should of seen the Non expired subscription at any point
                        x => !x.Symbol.IsCanonical() && x.Symbol.SecurityType == SecurityType.Future && x.Symbol.ID.Date >= _algorithm.StartDate));

                    assertedSubscriptions = true;
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            },
            endDate: endDate,
            secondsTimeStep: 60);

            Assert.IsTrue(assertedSubscriptions);
            Assert.IsTrue(emittedContinuousData);
            Assert.IsTrue(emittedChainData);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WarmupExpiredOption(bool useWarmupResolution)
        {
            _startDate = new DateTime(2014, 6, 14);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var endDate = _startDate.AddDays(1);
            _algorithm.SetBenchmark(x => 1);
            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            if (useWarmupResolution)
            {
                _algorithm.SetWarmup(10, Resolution.Daily);
            }
            else
            {
                _algorithm.SetWarmup(TimeSpan.FromDays(10));
            }
            var feed = RunDataFeed();

            var aapl = _algorithm.AddEquity("AAPL");
            // the expiration of this option contract is before the start date of the algorithm but we should still get some data during warmup
            var option = Symbol.CreateOption(aapl.Symbol, Market.USA, OptionStyle.American, OptionRight.Call, 925, new DateTime(2014, 06, 13));
            _algorithm.AddOptionContract(option, Resolution.Minute);

            var emittedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                // the data queue handler shouldn't of seen the expired option subscription at any point
                Assert.IsFalse(_dataQueueHandler.SubscriptionDataConfigs.Any(x => !x.Symbol.IsCanonical() && x.Symbol.SecurityType.IsOption()));

                if (ts.Slice.HasData)
                {
                    if (_algorithm.IsWarmingUp && ts.Slice.Keys.Any(s => s == option))
                    {
                        emittedData = true;
                        // we got what we wanted shortcut unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            },
            endDate: endDate,
            secondsTimeStep: 60);

            Assert.IsTrue(emittedData);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WarmupAddSecurity(bool useWarmupResolution)
        {
            _startDate = new DateTime(2014, 5, 8);
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var endDate = _startDate.AddDays(10);
            if (useWarmupResolution)
            {
                _algorithm.SetWarmup(1, Resolution.Daily);
            }
            else
            {
                _algorithm.SetWarmup(TimeSpan.FromDays(1));
            }
            var feed = RunDataFeed(forex: new List<string> { Symbols.EURUSD.ToString() }, resolution: Resolution.Minute);

            var emittedData = false;
            var emittedDataDuringWarmup = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.Slice.ContainsKey(Symbols.EURUSD))
                {
                    if (_algorithm.IsWarmingUp)
                    {
                        emittedDataDuringWarmup = true;
                    }
                    else
                    {
                        emittedData = true;
                        // we got what we wanted shortcut unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            }, endDate: endDate);

            Assert.IsTrue(emittedData);
            Assert.IsTrue(emittedDataDuringWarmup);
        }

        [Test]
        public void EmitsData()
        {
            var endDate = _startDate.AddDays(10);
            var feed = RunDataFeed(forex: new List<string> { Symbols.EURUSD.ToString() });

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
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
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

            // allow the feed to create a data point for all
            Thread.Sleep(25);

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
                _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
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
                _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
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
                    // SPY benchmark
                    Assert.AreEqual(1, _dataQueueHandler.Subscriptions.Count);

                    _algorithm.AddSecurities(forex: new List<string> { "EURUSD" });
                    _algorithm.OnEndOfTimeStep();
                    emittedData = true;
                }
                else
                {
                    // SPY benchmark and EURUSD
                    if (_dataQueueHandler.Subscriptions.Count != 2)
                    {
                        // The custom exchange has to pick up the universe selection data point and push it into the universe subscription to
                        // trigger adding EURUSD in the next loop
                        Thread.Sleep(50);
                        return;
                    }

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
                            _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                        }
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
                    // benchmark SPY, EURUSD
                    Assert.AreEqual(2, _dataQueueHandler.Subscriptions.Count);
                    _algorithm.AddUniverse("TestUniverse", time => new List<string> { "AAPL", "SPY" });
                    firstTime = true;
                }
                else
                {
                    if (_dataQueueHandler.Subscriptions.Count == 2)
                    {
                        Assert.AreEqual(1, _dataQueueHandler.Subscriptions.Count(x => x.Value.Contains("TESTUNIVERSE")));
                    }
                    else if (_dataQueueHandler.Subscriptions.Count == 3)
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
                                _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
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
                    return;
                }
                if (!emittedData)
                {
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.SPY));
                    if (ts.Data.Count > 0)
                    {
                        Assert.IsTrue(ts.Slice.Keys.Contains(Symbols.SPY));
                    }
                    // SPY benchmark
                    Assert.AreEqual(1, _dataQueueHandler.Subscriptions.Count);

                    _algorithm.AddSecurities(equities: new List<string> { "AAPL" });
                    _algorithm.OnEndOfTimeStep();
                    emittedData = true;
                }
                else
                {
                    if (_dataQueueHandler.Subscriptions.Count != 2)
                    {
                        // SPY benchmark and AAPL, retry it might not being picked up yet
                        Thread.Sleep(50);
                        return;
                    }

                    // there could be some slices with no data
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
                            _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                        }
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
                    // should of remove trade and quote bar subscription for both (4)
                    Assert.AreEqual(currentSubscriptionCount - 2, _dataQueueHandler.SubscriptionDataConfigs.Count);
                    // internal subscription should still be there
                    Assert.AreEqual(0, _dataQueueHandler.SubscriptionDataConfigs
                        .Where(config => !config.IsInternalFeed)
                        .Count(config => config.Symbol == Symbols.SPY));
                    // Should be 1 left because of internal subscription trade hour
                    Assert.AreEqual(1, _dataQueueHandler.SubscriptionDataConfigs.Count(config => config.Symbol == Symbols.SPY));
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));

                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
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
                    // should of remove trade and quote bar subscription for both (4)
                    Assert.AreEqual(currentSubscriptionCount - 2, _dataQueueHandler.SubscriptionDataConfigs.Count);
                    // internal subscription should still be there
                    Assert.AreEqual(0, _dataQueueHandler.SubscriptionDataConfigs
                        .Where(config => !config.IsInternalFeed)
                        .Count(config => config.Symbol == Symbols.SPY));
                    // Should be 1 left because of internal subscription trade hour
                    Assert.AreEqual(1, _dataQueueHandler.SubscriptionDataConfigs.Count(config => config.Symbol == Symbols.SPY));
                    Assert.IsTrue(_dataQueueHandler.Subscriptions.Contains(Symbols.EURUSD));
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
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

            ConsumeBridge(feed, TimeSpan.FromSeconds(3), false, ts =>
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
            ConsumeBridge(feed, TimeSpan.FromSeconds(3), ts =>
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
                _algorithm.AddData<CustomMockedFileBaseData>((100 + i).ToStringInvariant(), Resolution.Second, fillForward: false);
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
        [TestCase(FileFormat.UnfoldingCollection, true, false)]
        [TestCase(FileFormat.Csv, false, false)]
        [TestCase(FileFormat.UnfoldingCollection, false, false)]
        [TestCase(FileFormat.Csv, false, true)]
        [TestCase(FileFormat.UnfoldingCollection, false, true)]
        public void RestCustomDataReturningNullDoesNotInfinitelyPoll(FileFormat fileFormat, bool returnsNull, bool throwsException)
        {
            TestCustomData.FileFormat = fileFormat;

            var feed = RunDataFeed();

            _algorithm.AddData<TestCustomData>("Pinocho", Resolution.Minute, fillForward: false);

            TestCustomData.ReturnNull = returnsNull;
            TestCustomData.ThrowException = throwsException;
            ConsumeBridge(feed, TimeSpan.FromSeconds(2), alwaysInvoke: true, ts =>
            {
                // we request every 30min, so let's make sure time doesn't advance beyond 30 min, we want to test we are not requesting in a tight loop in the data stack
                Thread.Sleep(100);
                if (ts.DataPointCount > 0)
                {
                    Log.Debug("Emitted data");
                }
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

            using var cancellationTokenSource = new CancellationTokenSource();
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

        [TestCase(DataNormalizationMode.Raw, true)]
        [TestCase(DataNormalizationMode.BackwardsRatio, true)]
        [TestCase(DataNormalizationMode.BackwardsPanamaCanal, true)]
        [TestCase(DataNormalizationMode.ForwardPanamaCanal, true)]

        [TestCase(DataNormalizationMode.Raw, false)]
        [TestCase(DataNormalizationMode.BackwardsRatio, false)]
        [TestCase(DataNormalizationMode.BackwardsPanamaCanal, false)]
        [TestCase(DataNormalizationMode.ForwardPanamaCanal, false)]
        public void LivePriceScaling(DataNormalizationMode dataNormalizationMode, bool warmup)
        {
            _startDate = new DateTime(2013, 10, 10);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            _algorithm.SetBenchmark(x => 1);
            if (warmup)
            {
                _algorithm.SetWarmup(TimeSpan.FromDays(2));
            }
            else
            {
                _algorithm.SetFinishedWarmingUp();
            }
            var feed = RunDataFeed(runPostInitialize: false);

            var security = _algorithm.AddFuture("ES",
                dataNormalizationMode: dataNormalizationMode);
            var symbol = security.Symbol;

            _algorithm.PostInitialize();

            var receivedSecurityChanges = false;
            var receivedData = false;

            var assertPrice = new Action<decimal>((decimal price) =>
            {
                ConsoleWriteLine($"assertPrice: {price} for {symbol} @{security.LocalTime}");
                if (_algorithm.IsWarmingUp)
                {
                    if (dataNormalizationMode == DataNormalizationMode.ForwardPanamaCanal && Math.Abs(price - 1760m) > 10)
                    {
                        throw new RegressionTestException($"unexpected price {price} for {symbol} @{security.LocalTime}");
                    }
                    else if (dataNormalizationMode == DataNormalizationMode.Raw && Math.Abs(price -1660m) > 10)
                    {
                        throw new RegressionTestException($"unexpected price {price} for {symbol} @{security.LocalTime}");
                    }
                    else if (dataNormalizationMode == DataNormalizationMode.BackwardsPanamaCanal && Math.Abs(price - 1510m) > 10)
                    {
                        throw new RegressionTestException($"unexpected price {price} for {symbol} @{security.LocalTime}");
                    }
                    else if (dataNormalizationMode == DataNormalizationMode.BackwardsRatio && Math.Abs(price - 1560m) > 10m)
                    {
                        throw new RegressionTestException($"unexpected price {price} for {symbol} @{security.LocalTime}");
                    }
                }
                else
                {
                    if (dataNormalizationMode == DataNormalizationMode.ForwardPanamaCanal && price < 90)
                    {
                        throw new RegressionTestException($"unexpected price {price} for {symbol} @{security.LocalTime}");
                    }
                    else if (dataNormalizationMode == DataNormalizationMode.Raw && Math.Abs(price - 2m) > 1)
                    {
                        throw new RegressionTestException($"unexpected price {price} for {symbol} @{security.LocalTime}");
                    }
                    else if (dataNormalizationMode == DataNormalizationMode.BackwardsPanamaCanal && price < -160)
                    {
                        throw new RegressionTestException($"unexpected price {price} for {symbol} @{security.LocalTime}");
                    }
                    else if (dataNormalizationMode == DataNormalizationMode.BackwardsRatio && Math.Abs(price - 1.48m) > price * 0.1m)
                    {
                        throw new RegressionTestException($"unexpected price {price} for {symbol} @{security.LocalTime}");
                    }
                }
            });

            var lastPrice = 0m;
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), ts =>
            {
                foreach (var addedSecurity in ts.SecurityChanges.AddedSecurities)
                {
                    if (addedSecurity.Symbol == symbol)
                    {
                        receivedSecurityChanges = true;
                    }
                }

                if (warmup != _algorithm.IsWarmingUp)
                {
                    return;
                }

                if (ts.Slice.Bars.ContainsKey(symbol))
                {
                    receivedData = true;
                    assertPrice(ts.Slice.Bars[symbol].Price);
                }

                if (lastPrice != security.Price && security.HasData)
                {
                    lastPrice = security.Price;
                    // assert realtime prices too
                    assertPrice(lastPrice);
                }
            },
            alwaysInvoke: true,
            secondsTimeStep: 60 * 60 * 8,
            endDate: _startDate.AddDays(7));

            Assert.IsTrue(receivedSecurityChanges, "Did not add symbol!");
            Assert.IsTrue(receivedData, "Did not get any symbol data!");
        }

        [TestCase("AAPL", SecurityType.Equity)]
        [TestCase("BTCUSD", SecurityType.Crypto)]
        [TestCase("SPX500USD", SecurityType.Cfd)]
        [TestCase("ES", SecurityType.Future)]
        [TestCase("ES", SecurityType.FutureOption)]
        [TestCase("AAPL", SecurityType.Option)]
        public void UserDefinedUniverseSelection(string ticker, SecurityType securityType)
        {
            var feed = RunDataFeed();
            _algorithm.SetFinishedWarmingUp();

            Symbol symbol = null;
            if (securityType == SecurityType.Cfd)
            {
                symbol = _algorithm.AddCfd(ticker).Symbol;
            }
            else if (securityType == SecurityType.Equity)
            {
                symbol = _algorithm.AddEquity(ticker).Symbol;
            }
            else if (securityType == SecurityType.Crypto)
            {
                symbol = _algorithm.AddCrypto(ticker).Symbol;
            }
            else if (securityType == SecurityType.Option)
            {
                symbol = Symbol.CreateOption(Symbols.AAPL, Symbols.AAPL.ID.Market, OptionStyle.American,
                    OptionRight.Call, 1, _manualTimeProvider.GetUtcNow().AddDays(20));
                _algorithm.AddOptionContract(symbol);
            }
            else if (securityType == SecurityType.Future)
            {
                symbol = _algorithm.AddFuture(ticker).Symbol;
            }
            else if (securityType == SecurityType.FutureOption)
            {
                var expiration = _manualTimeProvider.GetUtcNow().AddDays(20);
                symbol = Symbol.CreateFuture("ES", Market.CME, expiration);
                symbol = Symbol.CreateOption(symbol, symbol.ID.Market, OptionStyle.American, OptionRight.Call, 1, expiration);
                _algorithm.AddFutureOptionContract(symbol);
            }
            _algorithm.OnEndOfTimeStep();

            var receivedSecurityChanges = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(3), ts =>
            {
                foreach (var addedSecurity in ts.SecurityChanges.AddedSecurities)
                {
                    if (addedSecurity.Symbol == symbol)
                    {
                        receivedSecurityChanges = true;
                        // we got what we wanted, end unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            },
            alwaysInvoke: true,
            // need to give time for future universe selection to trigger, midnight exchange tz
            secondsTimeStep: 60,
            endDate: _startDate.AddDays(1));

            Assert.IsTrue(receivedSecurityChanges, "Did not add symbol!");
        }

        [Test]
        public void DelistedEventEmitted_Equity()
        {
            _startDate = new DateTime(2007, 05, 17);
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var feed = RunDataFeed();
            var symbol = _algorithm.AddEquity("AAA.1").Symbol;
            _algorithm.OnEndOfTimeStep();
            _algorithm.SetFinishedWarmingUp();

            var receivedDelistedWarning = 0;
            var receivedDelisted = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                foreach (var delistingEvent in ts.Slice.Delistings)
                {
                    if (delistingEvent.Key != symbol)
                    {
                        throw new RegressionTestException($"Unexpected delisting for symbol {delistingEvent.Key}");
                    }

                    if (delistingEvent.Value.Type == DelistingType.Warning)
                    {
                        Interlocked.Increment(ref receivedDelistedWarning);
                    }
                    if (delistingEvent.Value.Type == DelistingType.Delisted)
                    {
                        Interlocked.Increment(ref receivedDelisted);
                        // we got what we wanted, end unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            },
            alwaysInvoke: false,
            secondsTimeStep: 3600 * 8,
            endDate: _startDate.AddDays(3));

            Assert.AreEqual(1, receivedDelistedWarning, $"Did not receive {DelistingType.Warning}");
            Assert.AreEqual(1, receivedDelisted, $"Did not receive {DelistingType.Delisted}");
        }

        [Test]
        public void DelistedEventEmitted()
        {
            _startDate = new DateTime(2016, 2, 18);
            var delistingDate = Symbols.SPY_C_192_Feb19_2016.GetDelistingDate();
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);
            var feed = RunDataFeed();

            var option = _algorithm.AddOptionContract(Symbols.SPY_C_192_Feb19_2016);
            _algorithm.OnEndOfTimeStep();
            _algorithm.SetFinishedWarmingUp();

            var receivedDelistedWarning = 0;
            var receivedDelisted = 0;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
                {
                    foreach (var delisting in ts.Slice.Delistings)
                    {
                        if (delisting.Key != Symbols.SPY_C_192_Feb19_2016)
                        {
                            throw new RegressionTestException($"Unexpected delisting for symbol {delisting.Key}");
                        }

                        if (delisting.Value.Type == DelistingType.Warning)
                        {
                            Interlocked.Increment(ref receivedDelistedWarning);
                        }
                        if (delisting.Value.Type == DelistingType.Delisted)
                        {
                            Interlocked.Increment(ref receivedDelisted);
                            // we got what we wanted, end unit test
                            _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                        }
                    }
                },
                alwaysInvoke: false,
                secondsTimeStep: 3600 * 8,
                endDate: delistingDate.AddDays(2));

            Assert.AreEqual(1, receivedDelistedWarning, $"Did not receive {DelistingType.Warning}");
            Assert.AreEqual(1, receivedDelisted, $"Did not receive {DelistingType.Delisted}");

            Assert.IsTrue(option.IsDelisted);
            Assert.IsFalse(option.IsTradable);
            Assert.IsFalse(_algorithm.Securities.Any(x => x.Key == option.Symbol));
        }

        [TestCase("20140325", typeof(CoarseFundamental))]
        [TestCase("20201202", typeof(ETFConstituentUniverse))]
        public void UniverseDataIsHoldUntilTimeIsRight(string dateTime, Type universeData)
        {
            _startDate = Time.ParseDate(dateTime);
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            Log.Debug($"StartTime {_manualTimeProvider.GetUtcNow()}");

            var feed = RunDataFeed(getNextTicksFunction: fdqh => Enumerable.Empty<BaseData>());
            if (universeData == typeof(CoarseFundamental))
            {
                _algorithm.AddUniverse(coarse => coarse.Take(10).Select(x => x.Symbol));
            }
            else
            {
                _algorithm.AddUniverse(_algorithm.Universe.ETF("SPY", Market.USA, _algorithm.UniverseSettings,
                    constituentData => constituentData.Take(10).Select(x => x.Symbol)));
            }
            // will add the universe
            _algorithm.OnEndOfTimeStep();

            var receivedUniverseData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                if (ts.UniverseData.Count > 0 &&
                    ts.UniverseData.First().Value.Data.First().GetType().IsAssignableTo(universeData))
                {
                    var now = _manualTimeProvider.GetUtcNow();
                    Log.Trace($"Received BaseDataCollection {now}");

                    // Assert data got hold until time was right
                    Assert.IsTrue(now.Hour < 23 && now.Hour > 5, $"Unexpected now value: {now}");
                    receivedUniverseData = true;

                    // we got what we wanted, end unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            }, sendUniverseData: true,
                alwaysInvoke: true,
                secondsTimeStep: 3600,
                endDate: _startDate.AddDays(1));

            Log.Debug($"EndTime {_manualTimeProvider.GetUtcNow()}");

            Assert.IsTrue(receivedUniverseData, "Did not receive universe data.");
        }

        [Test]
        public void CustomUniverseFineFundamentalDataGetsPipedCorrectly()
        {
            _startDate = new DateTime(2014, 10, 07, 15, 0, 0);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            // we use test ConstituentsUniverse, we have daily data for it
            var customUniverseSymbol = new Symbol(
                SecurityIdentifier.GenerateConstituentIdentifier(
                    "constituents-universe-qctest",
                    SecurityType.Equity,
                    Market.USA),
                "constituents-universe-qctest");
            using var customUniverse = new ConstituentsUniverse(customUniverseSymbol,
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
            _algorithm.OnEndOfTimeStep();
            // allow time for the base exchange to pick up the universe selection point
            Thread.Sleep(100);
            SecurityChanges securityChanges = null;
            var receivedFundamentalsData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), ts =>
            {
                if (ts.UniverseData.Count > 0 &&
                    ts.UniverseData.First().Value.Data.First() is Fundamental)
                {
                    securityChanges = ts.SecurityChanges;
                    receivedFundamentalsData = true;
                    // short cut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            }, secondsTimeStep: 60 * 60,
                alwaysInvoke: true,
                sendUniverseData: true,
                endDate: _startDate.AddDays(10));

            Assert.IsNotNull(securityChanges);
            Assert.IsTrue(securityChanges.AddedSecurities.Single().Symbol.Value == "AAPL");
            Assert.IsTrue(receivedFundamentalsData);
            Assert.IsTrue(fineWasCalled);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FineCoarseFundamentalDataGetsPipedCorrectlyWarmup(bool useWarmupResolution)
        {
            _startDate = new DateTime(2014, 3, 27);
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);
            if (useWarmupResolution)
            {
                _algorithm.SetWarmup(1, Resolution.Daily);
            }
            else
            {
                _algorithm.SetWarmup(TimeSpan.FromDays(1));
            }

            var fineWasCalled = false;
            var fineWasCalledDuringWarmup = false;
            _algorithm.UniverseSettings.Resolution = Resolution.Second;
            _algorithm.AddUniverse(coarse => coarse
                    .Where(x => x.Symbol.ID.Symbol.Contains("AAPL")).Select((fundamental, _) => fundamental.Symbol),
                fine =>
                {
                    var symbol = fine.FirstOrDefault()?.Symbol;
                    if (symbol == Symbols.AAPL)
                    {
                        if (_algorithm.IsWarmingUp)
                        {
                            fineWasCalledDuringWarmup = true;
                        }
                        else
                        {
                            fineWasCalled = true;
                        }
                        return new[] { symbol };
                    }
                    return Enumerable.Empty<Symbol>();
                });

            var feed = RunDataFeed(getNextTicksFunction: fdqh => Enumerable.Empty<BaseData>());

            var receivedFundamentalsData = false;
            var receivedFundamentalsDataDuringWarmup = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                if (ts.UniverseData.Count > 0 &&
                    ts.UniverseData.First().Value.Data.First() is Fundamental)
                {
                    if (_algorithm.IsWarmingUp)
                    {
                        receivedFundamentalsDataDuringWarmup = true;
                    }
                    else
                    {
                        receivedFundamentalsData = true;
                        // we got what we wanted shortcut unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            }, sendUniverseData: true, alwaysInvoke: true, secondsTimeStep: 3600, endDate: _startDate.AddDays(10));

            Assert.IsTrue(fineWasCalledDuringWarmup);
            Assert.IsTrue(fineWasCalled);
            Assert.IsTrue(receivedFundamentalsData);
            Assert.IsTrue(receivedFundamentalsDataDuringWarmup);
        }

        [TestCase("BTCUSD")]
        [TestCase("ADAUSDT")]
        public void MarginInterestDataGetsPipedCorrectly(string cryptoFuture)
        {
            _startDate = new DateTime(2022, 12, 12);
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var feed = RunDataFeed(getNextTicksFunction: fdqh => Enumerable.Empty<BaseData>());

            var asset = _algorithm.AddCryptoFuture(cryptoFuture);

            var receivedData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                var interestRates = ts.Slice.Get<MarginInterestRate>();
                foreach (var interestRate in interestRates)
                {
                    receivedData = true;
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);

                    Assert.AreEqual(asset.Symbol, interestRate.Key);
                }
            }, secondsTimeStep: 60 * 60 * 3, endDate: _startDate.AddDays(2));

            Assert.IsTrue(receivedData);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void FineCoarseFundamentalDataGetsPipedCorrectly(int numberOfUniverses)
        {
            _startDate = new DateTime(2014, 3, 25);
            CustomMockedFileBaseData.StartDate = _startDate;
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var feed = RunDataFeed(getNextTicksFunction: fdqh => Enumerable.Empty<BaseData>());

            var fineWasCalled = new List<bool> { false, false };
            for (var i = 0; i < numberOfUniverses; i++)
            {
                var index = i;
                _algorithm.AddUniverse(coarse => coarse
                        .Where(x => x.Symbol.ID.Symbol.Contains("AAPL")).Select((fundamental, i) => fundamental.Symbol),
                    fine =>
                    {
                        var symbol = fine.First().Symbol;
                        if (symbol == Symbols.AAPL)
                        {
                            fineWasCalled[index] = true;
                        }
                        return new[] { symbol };
                    });
            }

            var receivedFundamentalsData = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), ts =>
            {
                if (ts.UniverseData.Count > 0 &&
                    ts.UniverseData.First().Value.Data.First() is Fundamental)
                {
                    receivedFundamentalsData = true;
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            }, sendUniverseData: true, alwaysInvoke: true, secondsTimeStep: 1200, endDate: _startDate.AddDays(10));

            Assert.IsTrue(receivedFundamentalsData);
            for (var i = 0; i < numberOfUniverses; i++)
            {
                Assert.IsTrue(fineWasCalled[i]);
            }
        }

        [TestCase(SecurityType.Future, true)]
        [TestCase(SecurityType.Option, true)]
        [TestCase(SecurityType.IndexOption, true)]
        [TestCase(SecurityType.Future, false)]
        [TestCase(SecurityType.Option, false)]
        [TestCase(SecurityType.IndexOption, false)]
        public void AddChainUniverseCanNotAdvanceTime(SecurityType securityType, bool strictEndTimes)
        {
            _algorithm.Settings.DailyPreciseEndTime = strictEndTimes;
            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            _algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            // this reproduces GH issue #5245 where time can not advance and will keep it's default value
            var feed = RunDataFeed(lookupSymbolsFunction: null, canPerformSelection: () => false);

            if (securityType == SecurityType.Future)
            {
                _algorithm.AddFuture(Futures.Indices.SP500EMini);
            }
            else if (securityType == SecurityType.IndexOption)
            {
                _algorithm.AddIndexOption("SPX");
            }
            else
            {
                _algorithm.AddOption("AAPL");
            }
            // will add the universe
            _algorithm.OnEndOfTimeStep();
            ConsumeBridge(feed, TimeSpan.FromSeconds(2), ts =>
            {
                if (ts.UniverseData.Count > 0)
                {
                }
            }, secondsTimeStep: 60 * 60 * 3, // 3 hour time step
                alwaysInvoke: true);

            Assert.AreNotEqual(AlgorithmStatus.RuntimeError, _algorithm.Status);
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

            using var constituentsUniverse = new ConstituentsUniverse(
                new Symbol(
                    SecurityIdentifier.GenerateConstituentIdentifier(
                        "constituents-universe-qctest",
                        SecurityType.Equity,
                        Market.USA),
                    "constituents-universe-qctest"),
                _algorithm.UniverseSettings);
            _algorithm.AddUniverse(constituentsUniverse);
            // will add the universe
            _algorithm.OnEndOfTimeStep();
            // allow time for the base exchange to pick up the universe selection point
            Thread.Sleep(100);
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
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            }, secondsTimeStep: 60 * 60,
                alwaysInvoke: true,
                endDate: endDate);

            Assert.IsTrue(yieldedSymbols, "Did not yielded Symbols");
            Assert.IsTrue(yieldedNoneSymbol, "Did not yield NoneSymbol");
        }

        [Test]
        public void ThrowingDataQueueHandlerRuntimeError()
        {
            _algorithm.UniverseSettings.Resolution = Resolution.Daily;
            _algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            var feed = RunDataFeed(dataQueueHandler: new ThrowingDataQueueHandler());

            _algorithm.AddEquity("SPY");
            _algorithm.OnEndOfTimeStep();
            ConsumeBridge(feed, TimeSpan.FromSeconds(2), ts =>
            {
                if (_algorithm.Status == AlgorithmStatus.RuntimeError)
                {
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            }, secondsTimeStep: 60 * 60 * 3);

            Assert.AreEqual(AlgorithmStatus.RuntimeError, _algorithm.Status);
        }

        [Test]
        public void FastExitsDoNotThrowUnhandledExceptions()
        {
            var algorithm = new AlgorithmStub();

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();

            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new BacktestingResultHandler();

            _feed = new TestableLiveTradingDataFeed(algorithm.Settings);
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();

            var securityService = new SecurityService(
                algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(algorithm.Portfolio),
                algorithm: algorithm);
            algorithm.Securities.SetSecurityService(securityService);
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(_feed,
                new UniverseSelection(algorithm, securityService, dataPermissionManager, TestGlobals.DataProvider),
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
            _feed.DataQueueHandler = new FuncDataQueueHandler(handler => getNextTicksFunction, new RealTimeProvider(), _algorithm.Settings);

            _feed.Initialize(
                algorithm,
                job,
                resultHandler,
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                TestGlobals.DataProvider,
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
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase);
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
                                _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
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
                equities: securityType == SecurityType.Equity ? new List<string> { Symbols.SPY.ToString() } : new List<string>(),
                forex: securityType == SecurityType.Forex ? new List<string> { Symbols.EURUSD.ToString() } : new List<string>(),
                crypto: securityType == SecurityType.Crypto ? new List<string> { Symbols.BTCUSD.ToString() } : new List<string>(),
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

        [TestCase(false)]
        [TestCase(true)]
        public void SkipLiveDividend(bool warmup)
        {
            var symbol = Symbols.AAPL;
            // aapl has a dividend on the 6th
            if (warmup)
            {
                _startDate = new DateTime(2013, 11, 09);
                _manualTimeProvider.SetCurrentTimeUtc(_startDate);
                _algorithm.SetWarmup(5);
            }
            else
            {
                _startDate = new DateTime(2013, 11, 05);
                _manualTimeProvider.SetCurrentTimeUtc(_startDate);
            }

            var startPortfolioValue = _algorithm.Portfolio.TotalPortfolioValue;
            var feed = RunDataFeed(Resolution.Daily, equities: new List<string> { symbol.Value },
                    getNextTicksFunction: delegate
                    {
                        return Enumerable.Empty<BaseData>();
                    });

            var emittedDividend = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.Slice.Dividends.ContainsKey(symbol))
                {
                    Assert.AreEqual(warmup, _algorithm.IsWarmingUp);

                    emittedDividend = true;
                    // we got what we wanted shortcut unit test
                    _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                }
            }, secondsTimeStep: warmup ? 60 * 60 : 60 * 60 * 5,
            endDate: _startDate.AddDays(30));

            Assert.IsTrue(emittedDividend);
            // we do not handle dividends in live trading, we leave it for the cash sync
            Assert.AreEqual(startPortfolioValue, _algorithm.Portfolio.TotalPortfolioValue);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LiveSplitHandling(bool warmup)
        {
            // there's an split starting on the 7th
            var symbol = Symbols.AAPL;
            if (warmup)
            {
                _startDate = new DateTime(2014, 06, 10);
                _manualTimeProvider.SetCurrentTimeUtc(_startDate);
                _algorithm.SetWarmup(6);
            }
            else
            {
                _startDate = new DateTime(2014, 06, 5);
                _manualTimeProvider.SetCurrentTimeUtc(_startDate);
            }

            var feed = RunDataFeed(Resolution.Daily, equities: new List<string> { symbol.Value },
                    getNextTicksFunction: delegate
                    {
                        return Enumerable.Empty<BaseData>();
                    });

            var holdings = _algorithm.Securities[symbol].Holdings;
            holdings.SetHoldings(10, quantity: 100);
            var startPortfolioValue = _algorithm.Portfolio.TotalPortfolioValue;

            var emittedSplit = false;
            var emittedSplitWarning = false;
            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.Slice.Splits.TryGetValue(symbol, out var split))
                {
                    Assert.AreEqual(warmup, _algorithm.IsWarmingUp);

                    if (split.Type == SplitType.SplitOccurred)
                    {
                        emittedSplit = true;
                        // we got what we wanted shortcut unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                    else
                    {
                        emittedSplitWarning = true;
                    }
                }
            }, secondsTimeStep: warmup ? 60 * 60 : 60 * 60 * 12,
            endDate: _startDate.AddDays(30));

            Assert.IsTrue(emittedSplit);
            Assert.IsTrue(emittedSplitWarning);
            Assert.AreEqual((double)startPortfolioValue, (double)_algorithm.Portfolio.TotalPortfolioValue, delta: (double)(0.1m * _algorithm.Portfolio.TotalPortfolioValue));
            if (!warmup)
            {
                Assert.AreNotEqual(10, holdings.Quantity);
                Assert.AreNotEqual(100, holdings.AveragePrice);
            }
            else
            {
                // during warmup they shouldn't change
                Assert.AreEqual(100, holdings.Quantity);
                Assert.AreEqual(10, holdings.AveragePrice);
            }
        }

        [Test]
        public void HandlesAuxiliaryDataAtTickResolution()
        {
            // aapl has a dividend on the 6th
            _startDate = new DateTime(2013, 11, 05);
            _manualTimeProvider.SetCurrentTimeUtc(_startDate);

            var symbol = Symbols.AAPL;

            var feed = RunDataFeed(
                Resolution.Tick,
                equities: new List<string> { symbol.Value },
                getNextTicksFunction: delegate
                {
                    return new[] { (BaseData)new Tick { Symbol = symbol, TickType = TickType.Trade } };
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

                    if (emittedAuxData && emittedTicks)
                    {
                        // we got what we wanted shortcut unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            }, secondsTimeStep: 60 * 60 * 4,
            endDate: _startDate.AddDays(2));

            Assert.IsTrue(emittedTicks);
            Assert.IsTrue(emittedAuxData);
        }

        [Test]
        public void AggregatesTicksToTradeBar()
        {
            var symbol = Symbols.AAPL;

            var feed = RunDataFeed(Resolution.Second, equities: new List<string> { symbol.Value });

            var emittedTradeBars = false;

            ConsumeBridge(feed, TimeSpan.FromSeconds(3), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    if (ts.Slice.Bars.ContainsKey(symbol))
                    {
                        emittedTradeBars = true;
                        // we got what we wanted shortcut unit test
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                    }
                }
            }, endDate: _startDate.AddDays(1));

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

        [Test]
        public void FillForwardsWarmUpDataToLiveFeed(
            [Values(Resolution.Minute, Resolution.Daily)] Resolution warmupResolution,
            [Values] bool fromHistoryProviderWarmUp,
            [Values] bool withLiveDataPoint)
        {
            var symbol = Symbols.SPY;
            TradeBar lastHistoryWarmUpBar = null;
            if (fromHistoryProviderWarmUp)
            {
                _startDate = new DateTime(2025, 06, 12);

                var historyBarTime = warmupResolution == Resolution.Minute ? _startDate.AddHours(-12) : _startDate.AddDays(-2);
                lastHistoryWarmUpBar = new TradeBar(historyBarTime, symbol, 1, 1, 1, 1, 100, warmupResolution.ToTimeSpan());

                var historyProvider = new Mock<IHistoryProvider>();
                historyProvider
                    .Setup(m => m.GetHistory(It.IsAny<IEnumerable<Data.HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                    .Returns(new List<Slice>
                    {
                        new Slice(lastHistoryWarmUpBar.EndTime,
                            new List<BaseData> { lastHistoryWarmUpBar },
                            lastHistoryWarmUpBar.EndTime.ConvertToUtc(TimeZones.NewYork))
                    });
                _algorithm.SetHistoryProvider(historyProvider.Object);
            }
            else
            {
                _startDate = new DateTime(2013, 10, 12);
            }

            _algorithm.Settings.DailyPreciseEndTime = false;
            _algorithm.SetStartDate(_startDate);
            _manualTimeProvider.SetCurrentTimeUtc(_algorithm.Time.ConvertToUtc(TimeZones.NewYork));

            _algorithm.SetBenchmark(_ => 0);
            _algorithm.SetWarmUp(warmupResolution == Resolution.Minute ? 60 * 8 : 10, warmupResolution);

            var firstLiveBarTime = warmupResolution == Resolution.Minute
                ? _startDate.AddHours(8)
                : _startDate.AddHours(0.25);
            var firstLiveBar = new TradeBar(firstLiveBarTime, symbol, 1, 5, 1, 3, 100, Time.OneMinute);
            var liveData = withLiveDataPoint ? new List<BaseData> { firstLiveBar } : new List<BaseData>();
            var dqh = new TestDataQueueHandler { DataPerSymbol = new() { { symbol, liveData } } };
            var feed = RunDataFeed(Resolution.Minute, dataQueueHandler: dqh, equities: new() { "SPY" });
            _algorithm.OnEndOfTimeStep();

            TradeBar lastWarmupTradeBar = null;
            TradeBar lastTradeBar = null;
            var dataFillForwardedFromWarmupCount = 0;
            var dataFillForwardedFromLiveCount = 0;
            var gotLivePoint = false;

            var stopTime = withLiveDataPoint ? firstLiveBar.EndTime.AddHours(0.25) : _startDate.AddHours(0.5);
            if (warmupResolution == Resolution.Minute)
            {
                stopTime = withLiveDataPoint? firstLiveBar.EndTime.AddHours(1) : _startDate.AddHours(8);
            }

            ConsumeBridge(feed, TimeSpan.FromSeconds(5), true, ts =>
            {
                if (ts.Slice.HasData)
                {
                    Assert.IsTrue(ts.Slice.Bars.TryGetValue(symbol, out var tradeBar));

                    if (_algorithm.IsWarmingUp)
                    {
                        lastWarmupTradeBar = tradeBar;
                    }
                    else
                    {
                        lastTradeBar = tradeBar;

                        if (lastTradeBar.EndTime == firstLiveBar.EndTime && withLiveDataPoint)
                        {
                            Assert.IsFalse(lastTradeBar.IsFillForward);
                            gotLivePoint = true;
                        }
                        else
                        {
                            Assert.IsTrue(lastTradeBar.IsFillForward);

                            if (!withLiveDataPoint || lastTradeBar.EndTime < firstLiveBar.EndTime)
                            {
                                dataFillForwardedFromWarmupCount++;
                            }
                            else if (withLiveDataPoint && lastTradeBar.EndTime > firstLiveBar.EndTime)
                            {
                                dataFillForwardedFromLiveCount++;
                            }
                        }

                        if (tradeBar.EndTime >= stopTime)
                        {
                            // short cut
                            _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                        }
                    }
                }
            },
            endDate: _startDate.AddDays(60),
            secondsTimeStep: 60);

            // Assert we actually got warmup data
            Assert.IsNotNull(lastWarmupTradeBar);

            // Assert we got normal data
            Assert.IsNotNull(lastTradeBar);

            // Assert we got fill-forwarded data before the actual live data
            Assert.Greater(dataFillForwardedFromWarmupCount, 0);

            // Assert we got fill-forwarded data after the actual live data
            if (withLiveDataPoint)
            {
                Assert.IsTrue(gotLivePoint);
                Assert.Greater(dataFillForwardedFromLiveCount, 0);
            }
            else
            {
                Assert.AreEqual(0, dataFillForwardedFromLiveCount);
            }
        }

        private IDataFeed RunDataFeed(Resolution resolution = Resolution.Second, List<string> equities = null, List<string> forex = null, List<string> crypto = null,
            Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction = null,
            Func<Symbol, bool, string, IEnumerable<Symbol>> lookupSymbolsFunction = null,
            Func<bool> canPerformSelection = null, IDataQueueHandler dataQueueHandler = null,
            bool runPostInitialize = true)
        {
            _algorithm.SetStartDate(_startDate);
            _algorithm.SetDateTime(_manualTimeProvider.GetUtcNow());

            var lastTime = _manualTimeProvider.GetUtcNow();
            getNextTicksFunction ??= (fdqh =>
            {
                var time = _manualTimeProvider.GetUtcNow();
                if (time == lastTime) return Enumerable.Empty<BaseData>();
                lastTime = time;
                var tickTimeUtc = lastTime.AddMinutes(-1);
                return fdqh.SubscriptionDataConfigs.Where(config => !_algorithm.UniverseManager.ContainsKey(config.Symbol)) // its not a universe
                    .SelectMany(config =>
                        {
                            if (_algorithm.IsWarmingUp)
                            {
                                return Enumerable.Empty<Tick>();
                            }
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

            if (dataQueueHandler == null)
            {
                _dataQueueHandler = new FuncDataQueueHandlerUniverseProvider(getNextTicksFunction,
                    lookupSymbolsFunction ?? ((symbol, _, _) =>
                    {
                        var date = _manualTimeProvider.GetUtcNow()
                            .ConvertFromUtc(MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone)
                            .Date;

                        var symbols = new List<Symbol>();
                        for (var i = 0; i < 4; i++)
                        {
                            if (symbol.SecurityType.IsOption())
                            {
                                foreach (var optionRight in new[] { OptionRight.Call, OptionRight.Put })
                                {
                                    symbols.Add(Symbol.CreateOption(symbol.Underlying ?? symbol,
                                        symbol.ID.Market,
                                        symbol.SecurityType.DefaultOptionStyle(),
                                        optionRight,
                                        i,
                                        date.AddDays(i)));
                                }
                            }
                            else
                            {
                                symbols.Add(Symbol.CreateFuture(symbol.ID.Symbol, symbol.ID.Market, date.AddDays(i)));
                            }
                        }
                        return symbols;
                    }),
                    canPerformSelection ?? (() => true), _manualTimeProvider, _algorithm.Settings);
            }

            _feed = new TestableLiveTradingDataFeed(_algorithm.Settings, dataQueueHandler ?? _dataQueueHandler);
            _feed.TestDataQueueHandlerManager.TimeProvider = _manualTimeProvider;
            var fileProvider = TestGlobals.DataProvider;
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var securityService = new SecurityService(_algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, _algorithm, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(_algorithm.Portfolio), algorithm: _algorithm);
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
            _synchronizer = new TestableLiveSynchronizer(_manualTimeProvider, 10);
            _synchronizer.Initialize(_algorithm, _dataManager);

            _feed.Initialize(_algorithm, job, resultHandler, TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider, fileProvider, _dataManager, _synchronizer, new TestDataChannelProvider());
            if (runPostInitialize)
            {
                _algorithm.PostInitialize();
            }

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
            using var cancellationTokenSource = new CancellationTokenSource(timeout * 2);
            _algorithm.SetLocked();
            foreach (var timeSlice in _synchronizer.StreamData(cancellationTokenSource.Token))
            {
                _algorithm.ProcessSecurityChanges(timeSlice.SecurityChanges);
                _algorithm.SetDateTime(timeSlice.Time);
                if (!noOutput)
                {
                    ConsoleWriteLine("\r\n" + $"Now (EDT): {DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork):o}" +
                                     $". TimeSlice.Time (EDT): {timeSlice.Time.ConvertFromUtc(TimeZones.NewYork):o}. HasData {timeSlice.Slice?.HasData}");
                }

                if (timeSlice.IsTimePulse)
                {
                    continue;
                }

                AlgorithmManager.HandleDividends(timeSlice, _algorithm, liveMode: true);
                AlgorithmManager.HandleSplits(timeSlice, _algorithm, liveMode: true);

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
                if (!_algorithm.IsWarmingUp)
                {
                    Thread.Sleep(10);
                }
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
            new TestCaseData(Symbols.SPY, Resolution.Hour, 1, 0, 7, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Equity - Minute resolution
            // We expect 30 minute bars for 0.5 hours in open market hours
            new TestCaseData(Symbols.SPY, Resolution.Minute, 1, 0, (int)(0.5 * 60), (int)(0.5 * 60), 0, 0, false, _instances[typeof(BaseData)]),

            // Equity - Tick resolution
            // In this test we only emit ticks once per hour
            // We expect only 6 ticks -- the 4 PM tick is not received because it's outside market hours -> times 2 (quote/trade bar)
            new TestCaseData(Symbols.SPY, Resolution.Tick, 1, (7 - 1) * 2, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Forex - FXCM
            new TestCaseData(Symbols.EURUSD, Resolution.Hour, 1, 0, 0, 24, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.EURUSD, Resolution.Minute, 1, 0, 0, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            // emit at the start and end time
            new TestCaseData(Symbols.EURUSD, Resolution.Tick, 1, 24, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Forex - Oanda
            new TestCaseData(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), Resolution.Hour, 1, 0, 0, 24, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), Resolution.Minute, 1, 0, 0, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            // emit at the start and end time
            new TestCaseData(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), Resolution.Tick, 1, 24, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // CFD - FXCM
            new TestCaseData(Symbol.Create("DE30EUR", SecurityType.Cfd, Market.FXCM), Resolution.Hour, 1, 0, 0, 14, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbol.Create("DE30EUR", SecurityType.Cfd, Market.FXCM), Resolution.Minute, 1, 0, 0, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbol.Create("DE30EUR", SecurityType.Cfd, Market.FXCM), Resolution.Tick, 1, 14, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // CFD - Oanda
            new TestCaseData(Symbols.DE30EUR, Resolution.Hour, 1, 0, 0, 21, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.DE30EUR, Resolution.Minute, 1, 0, 0, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.DE30EUR, Resolution.Tick, 1, 21, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Crypto
            new TestCaseData(Symbols.BTCUSD, Resolution.Hour, 1, 0, 24, 24, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.BTCUSD, Resolution.Minute, 1, 0, 1 * 60, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            // x2 because counting trades and quotes. Emit at the start and end time
            new TestCaseData(Symbols.BTCUSD, Resolution.Tick, 1, 25 * 2, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Futures
            // ES has two session breaks totalling 1h 15m, so total trading hours = 22.75
            new TestCaseData(Symbols.Future_ESZ18_Dec2018, Resolution.Hour, 1, 0, 23, 23, 0, 0, false, _instances[typeof(BaseData)]),
            new TestCaseData(Symbols.Future_ESZ18_Dec2018, Resolution.Minute, 1, 0, 1 * 60, 1 * 60, 0, 0, false, _instances[typeof(BaseData)]),
            // x2 because counting trades and quotes. Emit at the start and end time
            new TestCaseData(Symbols.Future_ESZ18_Dec2018, Resolution.Tick, 1, 24 * 2, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Options
            new TestCaseData(Symbols.SPY_C_192_Feb19_2016, Resolution.Hour, 1, 0, 7, 7, 0, 0, false, _instances[typeof(BaseData)]),
            // We expect 30 minute bars for 0.5 hours in open market hours
            new TestCaseData(Symbols.SPY_C_192_Feb19_2016, Resolution.Minute, 1, 0, (int)(0.5 * 60), (int)(0.5 * 60), 0, 0, false, _instances[typeof(BaseData)]),
            // x2 because counting trades and quotes
            new TestCaseData(Symbols.SPY_C_192_Feb19_2016, Resolution.Tick, 1, (7 - 1) * 2, 0, 0, 0, 0, false, _instances[typeof(BaseData)]),

            // Custom data not supported
            new TestCaseData(Symbol.CreateBase(typeof(IndexedLinkedData2), Symbols.AAPL, Market.USA), Resolution.Hour, 1, 0, 0, 0, 0, 24 * 2, true, _instances[typeof(IndexedLinkedData2)]),
            new TestCaseData(Symbol.CreateBase(typeof(IndexedLinkedData2), Symbols.AAPL, Market.USA), Resolution.Minute, 1, 0, 0, 0, 0, 60 * 2, true, _instances[typeof(IndexedLinkedData2)]),
            new TestCaseData(Symbol.CreateBase(typeof(IndexedLinkedData2), Symbols.AAPL, Market.USA), Resolution.Tick, 1, 0, 0, 0, 0, 24, true, _instances[typeof(IndexedLinkedData2)]),

            //// Custom data streamed
            new TestCaseData(Symbol.CreateBase(typeof(IndexedLinkedData), Symbols.AAPL, Market.USA), Resolution.Hour, 1, 0, 0, 0, 0, 24, false, _instances[typeof(IndexedLinkedData)]),
            new TestCaseData(Symbol.CreateBase(typeof(IndexedLinkedData), Symbols.AAPL, Market.USA), Resolution.Minute, 1, 0, 0, 0, 0, 60, false, _instances[typeof(IndexedLinkedData)]),
            new TestCaseData(Symbol.CreateBase(typeof(IndexedLinkedData), Symbols.AAPL, Market.USA), Resolution.Tick, 1, 0, 0, 0, 0, 24, false, _instances[typeof(IndexedLinkedData)])
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
            // startDate and endDate are in algorithm time zone. Start date has to be before the expiration of symbol
            var startDate = new DateTime(2015, 6, 8);
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
            using var emittedData = new ManualResetEvent(false);

            var algorithm = new QCAlgorithm();
            using var dataQueueStarted = new ManualResetEvent(false);
            _dataQueueHandler = new FuncDataQueueHandler(fdqh =>
            {
                dataQueueStarted.Set();

                if (exchangeTimeZone == null)
                {
                    return Enumerable.Empty<BaseData>();
                }

                var utcTime = timeProvider.GetUtcNow();
                var exchangeTime = utcTime.ConvertFromUtc(exchangeTimeZone);
                var ended = exchangeTime > endDate.ConvertTo(algorithmTimeZone, exchangeTimeZone);
                if (exchangeTime == lastTime || ended)
                {
                    if (ended)
                    {
                        emittedData.Set();
                    }
                    return Enumerable.Empty<BaseData>();
                }

                lastTime = exchangeTime;

                var algorithmTime = utcTime.ConvertFromUtc(algorithmTimeZone);

                var dataPoints = new List<BaseData>();

                if (symbol.SecurityType == SecurityType.Base)
                {
                    var dataPoint = new T
                    {
                        Symbol = symbol,
                        EndTime = exchangeTime,
                        Value = actualPricePointsEnqueued++
                    };
                    dataPoints.Add(dataPoint);

                    ConsoleWriteLine(
                        $"{algorithmTime} - FuncDataQueueHandler emitted custom data point: {dataPoint}");
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

                emittedData.Set();
                return dataPoints;
            }, timeProvider, algorithm.Settings);

            _feed = new TestableLiveTradingDataFeed(algorithm.Settings, _dataQueueHandler);

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
                new SecurityCacheProvider(algorithm.Portfolio),
                algorithm: algorithm);
            algorithm.Securities.SetSecurityService(securityService);
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(_feed,
                new UniverseSelection(algorithm, securityService, dataPermissionManager, TestGlobals.DataProvider),
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

            _synchronizer = new TestableLiveSynchronizer(timeProvider, 10);
            _synchronizer.Initialize(algorithm, dataManager);

            Security security;
            switch (symbol.SecurityType)
            {
                case SecurityType.Base:
                    algorithm.AddEquity(symbol.Underlying.Value, resolution, symbol.ID.Market,
                        fillForward: false);

                    if (customDataType.RequiresMapping())
                    {
                        security = algorithm.AddData<T>(symbol.Value, resolution,
                            fillForward: false);
                    }
                    else
                    {
                        throw new NotImplementedException($"Custom data not implemented: {symbol}");
                    }
                    break;

                case SecurityType.Future:
                    security = algorithm.AddFutureContract(symbol, resolution, fillForward: false, extendedMarketHours: true);
                    break;

                case SecurityType.Option:
                    security = algorithm.AddOptionContract(symbol, resolution, fillForward: false);
                    break;

                default:
                    security = algorithm.AddSecurity(symbol.SecurityType, symbol.Value, resolution,
                        symbol.ID.Market, false, 1, false);
                    break;
            }

            _feed.Initialize(algorithm, new LiveNodePacket(), new BacktestingResultHandler(),
                TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, TestGlobals.DataProvider,
                dataManager, _synchronizer, new TestDataChannelProvider());

            if (!dataQueueStarted.WaitOne(TimeSpan.FromMilliseconds(5000)))
            {
                throw new TimeoutException("Timeout waiting for IDQH to start");
            }
            using var cancellationTokenSource = new CancellationTokenSource();

            // for tick resolution, we advance one hour at a time for less unit test run time
            TimeSpan advanceTimeSpan;
            switch (resolution)
            {
                case Resolution.Tick:
                default:
                    advanceTimeSpan = TimeSpan.FromHours(1);
                    break;
                case Resolution.Second:
                    advanceTimeSpan = TimeSpan.FromSeconds(1);
                    break;
                case Resolution.Minute:
                    advanceTimeSpan = TimeSpan.FromSeconds(60);
                    break;
                case Resolution.Hour:
                    advanceTimeSpan = TimeSpan.FromMinutes(60);
                    break;
                case Resolution.Daily:
                    advanceTimeSpan = TimeSpan.FromHours(24);
                    break;
            }
            try
            {
                algorithm.PostInitialize();
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
                        exchangeTimeZone = security.Exchange.TimeZone;
                        continue;
                    }
                    sliceCount++;

                    // give enough time to the producer to emit
                    if (sliceCount == 1 && !emittedData.WaitOne(300))
                    {
                        Assert.Fail("Timeout waiting for data generation");
                    }

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
                        Assert.Fail($"Timeout waiting for data generation at {algorithm.Time} algorithm tz");
                    }

                    var currentTime = timeProvider.GetUtcNow();
                    algorithm.SetDateTime(currentTime);

                    ConsoleWriteLine($"Algorithm time set to {currentTime.ConvertFromUtc(algorithmTimeZone)}");

                    if (shouldThrowException && algorithm.Status == AlgorithmStatus.RuntimeError)
                    {
                        // expected
                        return;
                    }

                    if (currentTime.ConvertFromUtc(algorithmTimeZone) > endDate)
                    {
                        _feed.Exit();
                        cancellationTokenSource.Cancel();
                        break;
                    }

                    if (resolution != Resolution.Tick)
                    {
                        var amount = currentTime.Ticks % resolution.ToTimeSpan().Ticks;
                        if (amount == 0)
                        {
                            // let's avoid race conditions and give time for the funDataQueueHandler thread to distribute the data among the consolidators
                            if (!_synchronizer.NewDataEvent.Wait(500))
                            {
                                if (!shouldThrowException || algorithm.Status != AlgorithmStatus.RuntimeError)
                                {
                                    Assert.Fail("Timeout waiting for data generation");
                                }
                            }
                        }
                    }
                    else
                    {
                        _synchronizer.NewDataEvent.Wait(300);
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

        [TestCase(SecurityType.Future, 4)]
        [TestCase(SecurityType.Option, 1232)]
        [TestCase(SecurityType.IndexOption, 6)]
        public void HandlesFutureAndOptionChainUniverse(SecurityType securityType, int expectedContractsCount)
        {
            Log.DebuggingEnabled = LogsEnabled;

            // startDate and endDate are in algorithm time zone. Midnight so selection happens right away
            var startDate = securityType switch
            {
                SecurityType.Option => new DateTime(2015, 12, 24),
                SecurityType.IndexOption => new DateTime(2021, 01, 04),
                SecurityType.Future => new DateTime(2013, 07, 11),
                _ => throw new ArgumentOutOfRangeException(nameof(securityType), securityType, null)
            };
            var endDate = startDate.AddDays(2.3);

            var algorithmTimeZone = TimeZones.NewYork;
            DateTimeZone exchangeTimeZone = null;

            var timeProvider = new ManualTimeProvider(algorithmTimeZone);
            timeProvider.SetCurrentTime(startDate);

            var lastTime = DateTime.MinValue;
            var timeAdvanceStep = TimeSpan.FromMinutes(180);
            using var timeAdvanced = new AutoResetEvent(true);
            using var started = new ManualResetEvent(false);
            var futureSelectionCount = 0;

            var selectedFutureSymbols = new HashSet<Symbol>();

            Symbol canonicalOptionSymbol = null;
            Exception lookupSymbolsException = null;

            var futureSymbols = new HashSet<Symbol>();
            var optionSymbols = new HashSet<Symbol>();

            var algorithm = new QCAlgorithm();
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

                    var dataPoints = new List<BaseData>();
                    if (securityType.IsOption())
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
                        dataPoints.Add(new Tick
                        {
                            Symbol = canonicalOptionSymbol.Underlying,
                            Time = exchangeTime,
                            EndTime = exchangeTime,
                            TickType = TickType.Trade,
                            Value = 100,
                            Quantity = 1
                        });

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
                    }
                    else if (securityType == SecurityType.Future)
                    {
                        if (selectedFutureSymbols.Count > 0)
                        {
                            var canonicalFutureSymbol = selectedFutureSymbols.First().Canonical;
                            var mappedSymbol = (algorithm.Securities[canonicalFutureSymbol] as Future).Mapped;

                            dataPoints.AddRange(
                                selectedFutureSymbols.Union(new[] { canonicalFutureSymbol, mappedSymbol }).Select(
                                    symbol => new Tick
                                    {
                                        Symbol = symbol,
                                        Time = exchangeTime,
                                        EndTime = exchangeTime,
                                        TickType = TickType.Trade,
                                        Value = 100,
                                        Quantity = 1
                                    }));
                        }
                    }

                    Log.Debug($"DQH: Emitting data point(s) at {utcTime.ConvertFromUtc(algorithmTimeZone)} ({algorithmTimeZone})");

                    return dataPoints;
                },

                // LookupSymbols
                (symbol, includeExpired, securityCurrency) => Enumerable.Empty<Symbol>(),

                // CanAdvanceTime
                () =>
                {
                    var time = timeProvider.GetUtcNow().ConvertFromUtc(algorithmTimeZone);
                    var result = time.Hour >= 1 && time.Hour < 23 && time.Day != 21;

                    Log.Debug($"CanPerformSelection() called at {time} ({algorithmTimeZone}), returning {result}");

                    return result;
                },
                timeProvider, algorithm.Settings);

            _feed = new TestableLiveTradingDataFeed(_algorithm.Settings, _dataQueueHandler);

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
                new SecurityCacheProvider(algorithm.Portfolio),
                algorithm: algorithm);
            algorithm.Securities.SetSecurityService(securityService);
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(_feed,
                new UniverseSelection(algorithm, securityService, dataPermissionManager, TestGlobals.DataProvider),
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

            _synchronizer = new TestableLiveSynchronizer(timeProvider, 10);
            _synchronizer.Initialize(algorithm, dataManager);

            if (securityType == SecurityType.Option)
            {
                algorithm.AddEquity("GOOG", Resolution.Minute);
                var option = algorithm.AddOption("GOOG", Resolution.Minute, Market.USA);
                option.SetFilter(x => x);
                exchangeTimeZone = option.Exchange.TimeZone;

                canonicalOptionSymbol = option.Symbol;
            }
            else if (securityType == SecurityType.IndexOption)
            {
                algorithm.AddIndex("SPX", Resolution.Minute);
                var option = algorithm.AddIndexOption("SPX", Resolution.Minute, Market.USA);
                option.SetFilter(x => x);
                exchangeTimeZone = option.Exchange.TimeZone;

                canonicalOptionSymbol = option.Symbol;
            }
            else if (securityType == SecurityType.Future)
            {
                var future = algorithm.AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, extendedMarketHours: true, fillForward: false);
                future.SetFilter(u =>
                {
                    futureSelectionCount++;
                    var result = u.IncludeWeeklys().Contracts(x => x.Take(2));
                    selectedFutureSymbols.UnionWith(result.Take(2).Select(x => x.Symbol));
                    return result;
                });
                exchangeTimeZone = future.Exchange.TimeZone;
            }
            else
            {
                throw new NotSupportedException($"Unsupported security type: {securityType}");
            }

            _feed.Initialize(algorithm, new LiveNodePacket(), new BacktestingResultHandler(),
                TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, TestGlobals.DataProvider,
                dataManager, _synchronizer, new TestDataChannelProvider());

            using var cancellationTokenSource = new CancellationTokenSource();

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
                    try
                    {
                        // stop the timer to prevent reentrancy
                        timer.Change(Timeout.Infinite, Timeout.Infinite);

                        timeProvider.Advance(timeAdvanceStep);
                        Log.Debug($"Time advanced to {timeProvider.GetUtcNow()} (UTC)");
                        timeAdvanced.Set();

                        // restart the timer
                        timer.Change(interval, interval);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }, null, interval, interval);

            // We should wait for the base exchange to pick up the universe and push a selection data point
            Thread.Sleep(100);

            bool IsPastEndTime(out DateTime currentTime)
            {
                currentTime = timeProvider.GetUtcNow();
                if (currentTime.ConvertFromUtc(algorithmTimeZone) > endDate)
                {
                    _feed.Exit();
                    cancellationTokenSource.Cancel();
                    return true;
                }

                return false;
            }

            foreach (var timeSlice in _synchronizer.StreamData(cancellationTokenSource.Token))
            {
                if (timeSlice.IsTimePulse || !timeSlice.Slice.HasData && timeSlice.SecurityChanges == SecurityChanges.None)
                {
                    if (IsPastEndTime(out _)) break;

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
                else if (securityType.IsOption())
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
                    timeSlice.Time > lastSecurityChangedTime.Value.Add(timeAdvanceStep)
                    && timeSlice.Slice.HasData)
                {
                    if (securityType == SecurityType.Future)
                    {
                        // -2 to remove canonical & internal since it's not part of the chain
                        Assert.AreEqual(futureSymbols.Count - 2, futureContractCount);

                        foreach (var symbol in futureSymbols)
                        {
                            // only assert there is data for non internal subscriptions
                            if (algorithm.SubscriptionManager.SubscriptionDataConfigService
                                .GetSubscriptionDataConfigs(symbol).Any())
                            {
                                Assert.IsTrue(timeSlice.Slice.ContainsKey(symbol), $"{symbol} was not found, has [{string.Join(",", timeSlice.Slice.Keys)}]");
                            }
                        }
                    }

                    if (securityType.IsOption() && timeSlice.Slice.OptionChains.Values.Count > 0)
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
                    else if (security.Symbol.SecurityType.IsOption())
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
                    else if (security.Symbol.SecurityType.IsOption())
                    {
                        lastSecurityChangedTime = timeSlice.Time;
                        Log.Debug($"{timeSlice.Time} - Removing option symbol: {security.Symbol}");
                        optionSymbols.Remove(security.Symbol);
                    }
                }

                algorithm.OnEndOfTimeStep();
                // We should wait for the base exchange to pick up the universe and push a selection data point
                Thread.Sleep(150);

                foreach (var baseDataCollection in timeSlice.UniverseData.Values)
                {
                    var symbols = string.Join(",", baseDataCollection.Data.Select(x => x.Symbol));
                    Log.Debug($"{timeSlice.Time} - universe data: {symbols}");
                }

                // Get current time and check if we should stop the algorithm
                IsPastEndTime(out var currentTime);
                algorithm.SetDateTime(currentTime);

                Log.Debug($"{timeSlice.Time} - Algorithm time set to {currentTime.ConvertFromUtc(algorithmTimeZone)} ({algorithmTimeZone})");
            }

            if (lookupSymbolsException != null)
            {
                throw lookupSymbolsException;
            }

            if (securityType == SecurityType.Future)
            {
                Assert.AreEqual(2, futureSelectionCount);
                // we add 2 symbols + 1 continuous future + 1 continuous future mapped symbol
                Assert.AreEqual(4, futureSymbols.Count, "Future symbols count mismatch");
            }
            else if (securityType.IsOption())
            {
                Assert.AreEqual(expectedContractsCount, optionSymbols.Count, "Option symbols count mismatch");
            }

            dataManager.RemoveAllSubscriptions();
            _dataQueueHandler.DisposeSafely();
            timeAdvanced.DisposeSafely();
            started.DisposeSafely();
            timer.Dispose();
        }

        // Reproduces https://github.com/QuantConnect/Lean/issues/8363
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Daily)]
        public void UsesFullPeriodDataForConsolidation(Resolution resolution)
        {
            _startDate = new DateTime(2014, 3, 27);
            _algorithm.SetStartDate(_startDate);
            _algorithm.Settings.DailyPreciseEndTime = false;

            // Add a few milliseconds to the start date to mimic a real world live scenario, where the time provider
            // will not always return an perfect rounded-down to second time
            _manualTimeProvider.SetCurrentTimeUtc(_startDate.AddMilliseconds(1).ConvertToUtc(TimeZones.NewYork));

            var symbol = Symbols.SPY;
            _algorithm.SetBenchmark(x => 0);

            var data = new[]
            {
                new [] { 108, 109, 90, 109, 72 },
                new [] { 105, 105, 94, 100, 175 },
                new [] { 93, 109, 90, 90, 170 },
                new [] { 95, 105, 90, 91, 19 },
                new [] { 91, 109, 91, 93, 132 },
                new [] { 98, 109, 94, 102, 175 },
                new [] { 107, 107, 91, 96, 97 },
                new [] { 105, 108, 91, 101, 124 },
                new [] { 105, 107, 91, 107, 81 },
                new [] { 91, 109, 91, 101, 168 },
                new [] { 93, 107, 90, 107, 199 },
                new [] { 101, 108, 90, 90, 169 },
                new [] { 101, 109, 90, 103, 14 },
                new [] { 92, 109, 90, 105, 55 },
                new [] { 96, 107, 92, 92, 176 },
                new [] { 94, 105, 90, 94, 28 },
                new [] { 105, 109, 91, 93, 172 },
                new [] { 107, 109, 93, 93, 137 },
                new [] { 95, 109, 91, 97, 168 },
                new [] { 103, 109, 91, 107, 178 },
                new [] { 96, 109, 96, 100, 168 },
                new [] { 90, 108, 90, 102, 63 },
                new [] { 100, 109, 96, 102, 134 },
                new [] { 95, 103, 90, 94, 39 },
                new [] { 105, 109, 91, 108, 117 },
                new [] { 106, 106, 91, 103, 20 },
                new [] { 95, 109, 93, 107, 7 },
                new [] { 104, 108, 90, 102, 150 },
                new [] { 94, 109, 90, 99, 178 },
                new [] { 99, 109, 90, 106, 150 },
            };

            var seconds = 0;
            var timeSpan = resolution.ToTimeSpan();
            using var dataQueueHandler = new TestDataQueueHandler
            {
                DataPerSymbol = new Dictionary<Symbol, List<BaseData>>
                {
                    {
                        symbol,
                        data
                            .Select(prices => new TradeBar(_startDate.Add(timeSpan * seconds++),
                                symbol,
                                prices[0],
                                prices[1],
                                prices[2],
                                prices[3],
                                prices[4],
                                timeSpan))
                            .Cast<BaseData>()
                            .ToList()
                    }
                }
            };

            var feed = RunDataFeed(
                resolution: resolution,
                equities: new() { "SPY" },
                dataQueueHandler: dataQueueHandler);

            var consolidatedData = new List<TradeBar>();
            var consolidatorUpdateData = new List<TradeBar>();

            const int consolidatorBarCountSpan = 6;
            var consolidatedCount = 0;
            var dataCountUsedForFirstConsolidatedBar = 0;

            _algorithm.Consolidate<TradeBar>(symbol, timeSpan * consolidatorBarCountSpan, (consolidatedBar) =>
            {
                _algorithm.Debug($"Consolidated: {_algorithm.Time} - {consolidatedBar}");

                // The first consolidated bar will be consolidated from 1 to consolidatorSpanSeconds second bars,
                // from the start time to the next multiple of consolidatorSpanSeconds
                var dataCountToTake = 0;
                if (consolidatedCount++ == 0)
                {
                    Assert.LessOrEqual(consolidatorUpdateData.Count, consolidatorBarCountSpan);
                    dataCountToTake = dataCountUsedForFirstConsolidatedBar = consolidatorUpdateData.Count;
                }
                else
                {
                    Assert.AreEqual(dataCountUsedForFirstConsolidatedBar + consolidatorBarCountSpan * (consolidatedCount - 1),
                        consolidatorUpdateData.Count);
                    dataCountToTake = consolidatorBarCountSpan;
                }

                var dataForCurrentConsolidatedBar = consolidatorUpdateData
                    .Skip(consolidatorBarCountSpan * (consolidatedCount - 1))
                    .Take(dataCountToTake)
                    .ToList();

                Assert.AreEqual(consolidatedBar.Time, dataForCurrentConsolidatedBar[0].Time);
                Assert.AreEqual(consolidatedBar.EndTime, dataForCurrentConsolidatedBar[^1].EndTime);

                var expectedOpen = dataForCurrentConsolidatedBar[0].Open;
                Assert.AreEqual(expectedOpen, consolidatedBar.Open);

                var expectedClose = dataForCurrentConsolidatedBar[^1].Close;
                Assert.AreEqual(expectedClose, consolidatedBar.Close);

                var expectedHigh = dataForCurrentConsolidatedBar.Max(x => x.High);
                Assert.AreEqual(expectedHigh, consolidatedBar.High);

                var expectedLow = dataForCurrentConsolidatedBar.Min(x => x.Low);
                Assert.AreEqual(expectedLow, consolidatedBar.Low);

                var expectedVolume = dataForCurrentConsolidatedBar.Sum(x => x.Volume);
                Assert.AreEqual(expectedVolume, consolidatedBar.Volume);
            });

            ConsumeBridge(feed,
                TimeSpan.FromSeconds(5),
                true,
                timeSlice =>
                {
                    if (consolidatorUpdateData.Count >= data.Length)
                    {
                        // Ran out of data, stop the feed
                        _manualTimeProvider.SetCurrentTimeUtc(Time.EndOfTime);
                        return;
                    }

                    // Mimic the algorithm manager consolidators scan:

                    // First, scan for consolidators that need to be updated
                    // NOTE: Rounding time down to mimic the algorithm manager consolidators scan
                    _algorithm.SubscriptionManager.ScanPastConsolidators(timeSlice.Time.RoundDown(Time.OneSecond), _algorithm);

                    // Then, update the consolidators with the new data
                    if (timeSlice.ConsolidatorUpdateData.Count > 0)
                    {
                        var timeKeeper = _algorithm.TimeKeeper;
                        foreach (var update in timeSlice.ConsolidatorUpdateData)
                        {
                            var localTime = timeKeeper.GetLocalTimeKeeper(update.Target.ExchangeTimeZone).LocalTime;
                            var consolidators = update.Target.Consolidators;
                            foreach (var consolidator in consolidators)
                            {
                                foreach (var dataPoint in update.Data)
                                {
                                    if (consolidator is TradeBarConsolidator tradeBarConsolidator)
                                    {
                                        consolidatorUpdateData.Add(dataPoint as TradeBar);
                                    }

                                    consolidator.Update(dataPoint);
                                }

                                // scan for time after we've pumped all the data through for this consolidator
                                consolidator.Scan(localTime);
                            }
                        }
                    }
                },
                endDate: _startDate.Date.AddDays(60),
                secondsTimeStep: (int)timeSpan.TotalSeconds);

            Assert.AreEqual(dataQueueHandler.DataPerSymbol.Values.Single().Count / consolidatorBarCountSpan, consolidatedCount);
        }

        private class TestFundamentalDataProviderTrue : IFundamentalDataProvider
        {
            public T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty name)
            {
                if (securityIdentifier == SecurityIdentifier.Empty)
                {
                    return default;
                }
                return Get(time, securityIdentifier, name);
            }

            private dynamic Get(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty enumName)
            {
                var name = Enum.GetName(enumName);
                switch (name)
                {
                    case "HasFundamentalData":
                        return true;
                }
                return null;
            }
            public void Initialize(IDataProvider dataProvider, bool liveMode)
            {
            }
        }

        private class ThrowingDataQueueHandler : IDataQueueHandler
        {
            public bool IsConnected => true;
            public void Dispose()
            { }
            public void SetJob(LiveNodePacket job)
            { }
            public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
            {
                throw new NotImplementedException();
            }
            public void Unsubscribe(SubscriptionDataConfig dataConfig)
            {
                throw new NotImplementedException();
            }
        }
    }

    internal class TestableLiveTradingDataFeed : LiveTradingDataFeed
    {
        public IDataQueueHandler DataQueueHandler;
        public TestDataQueueHandlerManager TestDataQueueHandlerManager;

        public TestableLiveTradingDataFeed(IAlgorithmSettings settings, IDataQueueHandler dataQueueHandler = null)
        {
            DataQueueHandler = dataQueueHandler;
            TestDataQueueHandlerManager = new(new[] { DataQueueHandler }, settings);
        }

        protected override BaseDataExchange GetBaseDataExchange()
        {
            var result = base.GetBaseDataExchange();
            result.SleepInterval = 10;
            return result;
        }

        protected override IDataQueueHandler GetDataQueueHandler()
        {
            return TestDataQueueHandlerManager;
        }

        public override void Exit()
        {
            base.Exit();
            DataQueueHandler.DisposeSafely();
        }
    }

    internal class TestDataQueueHandlerManager : DataQueueHandlerManager
    {
        public ITimeProvider TimeProvider { get; set; }

        public TestDataQueueHandlerManager(IEnumerable<IDataQueueHandler> dataQueueHandlers, IAlgorithmSettings settings)
            : base(settings)
        {
            DataHandlers = dataQueueHandlers.ToList();
        }
        protected override ITimeProvider InitializeFrontierTimeProvider()
        {
            return TimeProvider;
        }
    }

    internal class TestDataChannelProvider : DataChannelProvider
    {
        public override bool ShouldStreamSubscription(SubscriptionDataConfig config)
        {
            if (config.Type == typeof(IndexedLinkedData))
            {
                return true;
            }
            return base.ShouldStreamSubscription(config);
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
            _newLiveDataTimeout = newLiveDataTimeout ?? 10;
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
                throw new RegressionTestException("Custom data Reader threw exception");
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

                if (FileFormat == FileFormat.UnfoldingCollection)
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
