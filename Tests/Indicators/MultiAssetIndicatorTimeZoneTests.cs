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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class MultiAssetIndicatorTimeZoneTests
    {
        private const int Periods = 10;
        private static readonly DateTime FutureExpiry = new(2024, 3, 15);
        // ES and NQ trade in New York time, ZN and ZB in Chicago time
        private static readonly Symbol[] MixedTimeZoneSymbols =
        {
            Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, FutureExpiry),
            Symbol.CreateFuture(Futures.Financials.Y10TreasuryNote, Market.CBOT, FutureExpiry),
            Symbol.CreateFuture(Futures.Indices.NASDAQ100EMini, Market.CME, FutureExpiry),
            Symbol.CreateFuture(Futures.Financials.Y30TreasuryBond, Market.CBOT, FutureExpiry)
        };
        private static readonly Symbol[] NewYorkSymbols = { Symbols.SPY, Symbols.AAPL, Symbols.IBM };
        private static readonly MarketHoursDatabase MarketHours = MarketHoursDatabase.FromDataFolder();

        private static readonly TestCaseData[] Indicators =
        {
            // the dual symbol indicators and alpha take the first two symbols, the others all of them
            Case("Beta", 2, symbols => new Beta(symbols[0], symbols[1], 3)),
            Case("Correlation", 2, symbols => new Correlation(symbols[0], symbols[1], 3)),
            Case("Covariance", 2, symbols => new Covariance(symbols[0], symbols[1], 3)),
            Case("LeastSquaresMovingAverageWithReference", 2, symbols => new LeastSquaresMovingAverageWithReference(symbols[0], symbols[1], 3)),
            Case("Alpha", 2, symbols => new Alpha(symbols[0], symbols[1], 2, 3, 0m)),
            Case("CrossSectionalInformationCoefficient", null, symbols => new CrossSectionalInformationCoefficient(symbols, _ => new RateOfChange(1))),
            Case("AdvanceDeclineRatio", null, symbols => AddAll(new AdvanceDeclineRatio("ADR"), symbols)),
            Case("AdvanceDeclineVolumeRatio", null, symbols => AddAll(new AdvanceDeclineVolumeRatio("ADVR"), symbols)),
            Case("AdvanceDeclineDifference", null, symbols => AddAll(new AdvanceDeclineDifference("ADDIFF"), symbols)),
            Case("NewHighsNewLows", null, symbols => AddAll(new NewHighsNewLows("NHNL", 2), symbols)),
            Case("NewHighsNewLowsVolume", null, symbols => AddAll(new NewHighsNewLowsVolume("NHNLV", 2), symbols)),
        };

        [TestCaseSource(nameof(Indicators))]
        public void ValuesOfAssetsOfDifferentTimeZonesAreInTheFirstSymbolTimeZone(int? assets, Func<Symbol[], IndicatorBase> createIndicator)
        {
            var symbols = MixedTimeZoneSymbols.Take(assets ?? MixedTimeZoneSymbols.Length).ToArray();
            Run(createIndicator, symbols, null, TimeZones.NewYork);
        }

        [TestCaseSource(nameof(Indicators))]
        public void ValuesAreInTheTimeZoneSet(int? assets, Func<Symbol[], IndicatorBase> createIndicator)
        {
            var symbols = MixedTimeZoneSymbols.Take(assets ?? MixedTimeZoneSymbols.Length).ToArray();
            Run(createIndicator, symbols, TimeZones.Utc, TimeZones.Utc);
        }

        [TestCaseSource(nameof(Indicators))]
        public void ValuesOfAssetsOfOneTimeZoneKeepTheirInputsTimes(int? assets, Func<Symbol[], IndicatorBase> createIndicator)
        {
            var symbols = NewYorkSymbols.Take(assets ?? NewYorkSymbols.Length).ToArray();
            var indicator = createIndicator(symbols);
            var publishedTimes = new List<DateTime>();
            var inputTimes = new List<DateTime>();
            IBaseData input = null;
            indicator.Updated += (_, point) =>
            {
                publishedTimes.Add(point.EndTime);
                inputTimes.Add(input.EndTime);
            };

            foreach (var bar in GetSlices(symbols).SelectMany(slice => slice.Bars.Values))
            {
                input = bar;
                indicator.Update(bar);
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.IsNotEmpty(publishedTimes);
            CollectionAssert.AreEqual(inputTimes, publishedTimes);
        }

        [TestCaseSource(nameof(Indicators))]
        public void ValuesKeepIncreasingTimesAcrossADaylightSavingFallBackInUtc(int? assets, Func<Symbol[], IndicatorBase> createIndicator)
        {
            // New York's clocks go back from 2:00 to 1:00 on 2023-11-05, at 6:00 UTC, when crypto trades but equities don't
            var symbols = new[] { Symbols.SPY, Symbols.BTCUSD, Symbols.ETHUSD, Symbols.LTCUSD }.Take(assets ?? 4).ToArray();
            var indicator = createIndicator(symbols);
            // the breadth indicators and the cross-sectional information coefficient wait for every asset they track
            indicator.GetType().GetMethod("Remove", new[] { typeof(Symbol) })?.Invoke(indicator, new object[] { Symbols.SPY });
            Assert.AreEqual(TimeZones.NewYork, GetTimeZone(indicator));
            SetTimeZone(indicator, TimeZones.Utc);
            var times = new List<DateTime>();
            indicator.Updated += (_, point) => times.Add(point.EndTime);

            var start = new DateTime(2023, 11, 5, 3, 0, 0);
            var endTimesUtc = Enumerable.Range(1, 6).Select(hour => start.AddHours(hour)).ToList();
            foreach (var endTimeUtc in endTimesUtc)
            {
                for (var i = 1; i < symbols.Length; i++)
                {
                    indicator.Update(CreateBar(symbols[i], endTimeUtc, endTimeUtc.Hour, i));
                }
            }

            // a value per period: a single asset completes the periods of the dual symbol indicators
            Assert.Greater(times.Count, 2);
            CollectionAssert.AllItemsAreUnique(times);
            CollectionAssert.IsOrdered(times);
            CollectionAssert.IsSubsetOf(times, endTimesUtc);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NewHighsNewLowsSubIndicatorsShareTheValueTime(bool volume)
        {
            IndicatorBase indicator;
            IndicatorBase[] subIndicators;
            if (volume)
            {
                var nhnlv = AddAll(new NewHighsNewLowsVolume("NHNLV", 2), MixedTimeZoneSymbols);
                indicator = nhnlv;
                subIndicators = new IndicatorBase[] { nhnlv.Difference, nhnlv.Ratio, nhnlv.VolumeRatio };
            }
            else
            {
                var nhnl = AddAll(new NewHighsNewLows("NHNL", 2), MixedTimeZoneSymbols);
                indicator = nhnl;
                subIndicators = new IndicatorBase[] { nhnl.Difference, nhnl.Ratio };
            }
            var checkedValues = 0;
            indicator.Updated += (_, point) =>
            {
                foreach (var subIndicator in subIndicators)
                {
                    Assert.AreEqual(point.EndTime, subIndicator.Current.EndTime, subIndicator.Name);
                }
                checkedValues++;
            };

            foreach (var bar in GetSlices(MixedTimeZoneSymbols).SelectMany(slice => slice.Bars.Values))
            {
                indicator.Update(bar);
            }

            Assert.Greater(checkedValues, 5);
        }

        [Test]
        public void TimeZoneCanBeSetFromPython()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(), @"
from AlgorithmImports import *

def create(target, reference):
    beta = Beta(target, reference, 3)
    beta.time_zone = TimeZones.UTC
    return beta
");
                var beta = module.GetAttr("create").Invoke(MixedTimeZoneSymbols[0].ToPython(), MixedTimeZoneSymbols[1].ToPython())
                    .GetAndDispose<Beta>();

                Assert.AreEqual(TimeZones.Utc, GetTimeZone(beta));
            }
        }

        private static TestCaseData Case(string name, int? assets, Func<Symbol[], IndicatorBase> createIndicator)
        {
            return new TestCaseData(assets, createIndicator).SetName(name);
        }

        private static void Run(Func<Symbol[], IndicatorBase> createIndicator, Symbol[] symbols, DateTimeZone timeZone,
            DateTimeZone expectedTimeZone)
        {
            var slices = GetSlices(symbols);

            var indicator = createIndicator(symbols);
            if (timeZone != null)
            {
                SetTimeZone(indicator, timeZone);
            }
            var times = new List<DateTime>();
            var expectedTimes = new List<DateTime>();
            Slice currentSlice = null;
            indicator.Updated += (_, point) =>
            {
                if (indicator.IsReady)
                {
                    times.Add(point.EndTime);
                    // each value is published with an input of its own period
                    expectedTimes.Add(currentSlice.UtcTime.ConvertFromUtc(expectedTimeZone));
                }
            };
            foreach (var slice in slices)
            {
                currentSlice = slice;
                foreach (var bar in slice.Bars.Values)
                {
                    indicator.Update(bar);
                }
            }

            var historyIndicator = createIndicator(symbols);
            if (timeZone != null)
            {
                SetTimeZone(historyIndicator, timeZone);
            }
            var algorithm = new AlgorithmStub();
            var history = historyIndicator switch
            {
                IndicatorBase<IBaseDataBar> barIndicator => algorithm.IndicatorHistory(barIndicator, slices),
                IndicatorBase<TradeBar> tradeBarIndicator => algorithm.IndicatorHistory(tradeBarIndicator, slices),
                IndicatorBase<IBaseData> dataIndicator => algorithm.IndicatorHistory(dataIndicator, slices),
                _ => throw new ArgumentException(historyIndicator.GetType().Name)
            };

            CollectionAssert.AreEqual(expectedTimes, times);
            // every period after the warm up has a value, and a row in the history
            var periodTimes = times.Distinct().ToList();
            Assert.Greater(periodTimes.Count, Periods / 2);
            CollectionAssert.AreEqual(periodTimes, history.Select(dataPoints => dataPoints.EndTime));
        }

        private static List<Slice> GetSlices(Symbol[] symbols)
        {
            return Enumerable.Range(0, Periods).Select(hour =>
            {
                var endTimeUtc = GetEndTimeUtc(hour);
                var bars = symbols.Select((symbol, i) => CreateBar(symbol, endTimeUtc, hour, i)).ToList();
                return new Slice(endTimeUtc, bars, endTimeUtc);
            }).ToList();
        }

        private static DateTime GetEndTimeUtc(int hour)
        {
            return new DateTime(2024, 1, 8, 15, 0, 0).AddHours(hour);
        }

        private static BaseData CreateBar(Symbol symbol, DateTime endTimeUtc, int hour, int index)
        {
            var timeZone = MarketHours.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
            var endTime = endTimeUtc.ConvertFromUtc(timeZone);
            // prices that move differently for each asset, so every indicator gets values
            var close = 100m + (hour * (index + 2) + index * 3) % 7 + hour * (index - 1) * 0.5m;
            return new TradeBar(endTime.AddHours(-1), symbol, close, close + 1 + index, close - 1 - hour % 3, close, 1000 + 100 * index,
                TimeSpan.FromHours(1));
        }

        private static T AddAll<T>(T indicator, Symbol[] symbols)
        {
            foreach (var symbol in symbols)
            {
                ((dynamic)indicator).Add(symbol);
            }
            return indicator;
        }

        private static DateTimeZone GetTimeZone(IndicatorBase indicator)
        {
            return (DateTimeZone)indicator.GetType().GetProperty("TimeZone").GetValue(indicator);
        }

        private static void SetTimeZone(IndicatorBase indicator, DateTimeZone timeZone)
        {
            indicator.GetType().GetProperty("TimeZone").SetValue(indicator, timeZone);
        }
    }
}
