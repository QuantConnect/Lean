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
using NUnit.Framework;
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
            // the dual symbol indicators take the first two symbols, the cross-sectional information coefficient all of them
            Case("Beta", 2, symbols => new Beta(symbols[0], symbols[1], 3)),
            Case("Correlation", 2, symbols => new Correlation(symbols[0], symbols[1], 3)),
            Case("Covariance", 2, symbols => new Covariance(symbols[0], symbols[1], 3)),
            Case("LeastSquaresMovingAverageWithReference", 2, symbols => new LeastSquaresMovingAverageWithReference(symbols[0], symbols[1], 3)),
            Case("CrossSectionalInformationCoefficient", null, symbols => new CrossSectionalInformationCoefficient(symbols, _ => new RateOfChange(1))),
        };

        [TestCaseSource(nameof(Indicators))]
        public void ValuesOfAssetsOfDifferentTimeZonesAreInTheFirstSymbolTimeZone(int? assets, Func<Symbol[], IndicatorBase> createIndicator)
        {
            var symbols = MixedTimeZoneSymbols.Take(assets ?? MixedTimeZoneSymbols.Length).ToArray();
            Run(createIndicator, symbols);
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

        [Test]
        public void CrossSectionalInformationCoefficientPeriodWhoseLastAssetWasRemovedKeepsTheFirstSymbolTimeZone()
        {
            // ZN, in Chicago time, gives each period its last input
            var zn = MixedTimeZoneSymbols[1];
            var symbols = new[]
            {
                MixedTimeZoneSymbols[0],
                MixedTimeZoneSymbols[2],
                Symbol.CreateFuture(Futures.Indices.Russell2000EMini, Market.CME, FutureExpiry),
                MixedTimeZoneSymbols[3],
                zn
            };
            var indicator = new CrossSectionalInformationCoefficient(symbols, _ => new RateOfChange(1));
            var times = new List<DateTime>();
            indicator.Updated += (_, point) => times.Add(point.EndTime);
            const int lastHour = 5;
            for (var hour = 0; hour <= lastHour; hour++)
            {
                for (var i = 0; i < symbols.Length; i++)
                {
                    // ZB has no input for the last period, which is then correlated when the next one starts
                    if (hour < lastHour || symbols[i] != MixedTimeZoneSymbols[3])
                    {
                        indicator.Update(CreateBar(symbols[i], GetEndTimeUtc(hour), hour, i));
                    }
                }
            }
            indicator.Remove(zn);
            var valueCount = times.Count;

            indicator.Update(CreateBar(symbols[0], GetEndTimeUtc(lastHour + 1), lastHour + 1, 0));

            Assert.AreEqual(valueCount + 1, times.Count);
            Assert.AreEqual(GetEndTimeUtc(lastHour).ConvertFromUtc(TimeZones.NewYork), times[^1]);
        }

        private static TestCaseData Case(string name, int? assets, Func<Symbol[], IndicatorBase> createIndicator)
        {
            return new TestCaseData(assets, createIndicator).SetName(name);
        }

        private static void Run(Func<Symbol[], IndicatorBase> createIndicator, Symbol[] symbols)
        {
            var slices = GetSlices(symbols);

            var indicator = createIndicator(symbols);
            var times = new List<DateTime>();
            var expectedTimes = new List<DateTime>();
            Slice currentSlice = null;
            indicator.Updated += (_, point) =>
            {
                if (indicator.IsReady)
                {
                    times.Add(point.EndTime);
                    // each value has its period's time in the first symbol's exchange time zone, New York
                    expectedTimes.Add(currentSlice.UtcTime.ConvertFromUtc(TimeZones.NewYork));
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
            var algorithm = new AlgorithmStub();
            var history = historyIndicator switch
            {
                IndicatorBase<IBaseDataBar> barIndicator => algorithm.IndicatorHistory(barIndicator, slices),
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
    }
}
