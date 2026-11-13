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
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class CrossSectionalInformationCoefficientTests
    {
        private static readonly Dictionary<Symbol, decimal[]> PricesBySymbol = new()
        {
            { Symbols.AAPL, new[] { 10m, 9.62m, 9.35m, 9.47m, 9.89m, 9.97m, 9.87m } },
            { Symbols.IBM, new[] { 20m, 20.95m, 20m, 20.72m, 20.28m, 19.56m, 18.81m } },
            { Symbols.GOOG, new[] { 30m, 29.43m, 30.36m, 29.39m, 29.63m, 30.04m, 29.66m } },
            { Symbols.MSFT, new[] { 40m, 40.19m, 38.43m, 36.74m, 35.66m, 36.3m, 36.04m } },
            { Symbols.SPY, new[] { 50m, 49.07m, 49.49m, 49.26m, 48.27m, 49.69m, 50.68m } },
        };

        // scipy.stats.spearmanr across the assets of the previous period's 1-period rate of change and this period's return
        private static readonly double?[] ExpectedValues = { null, null, -0.7, -0.4, 0.3, -0.2, 0.9 };

        private static readonly DateTime StartTime = new(2024, 1, 1);

        [Test]
        public void ComputesSpearmanCorrelationAcrossAssets()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));

            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                UpdateAll(indicator, i);

                Assert.AreEqual(ExpectedValues[i].HasValue, indicator.IsReady, $"Index {i}");
                Assert.AreEqual(ExpectedValues[i] ?? 0, (double)indicator.Current.Value, 1e-9, $"Index {i}");
            }
        }

        [Test]
        public void CorrelatesAPeriodAsSoonAsEveryAssetHasData()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));
            for (var i = 0; i < 3; i++)
            {
                UpdateAll(indicator, i);
            }
            var value = indicator.Current.Value;

            foreach (var symbol in PricesBySymbol.Keys.Skip(1))
            {
                indicator.Update(CreateBar(symbol, 3, 100m));
            }
            Assert.AreEqual(value, indicator.Current.Value);

            indicator.Update(CreateBar(PricesBySymbol.Keys.First(), 3, 100m));
            Assert.AreNotEqual(value, indicator.Current.Value);
        }

        [Test]
        public void CorrelatesAPeriodAnAssetHasNoDataForWhenTheNextOneStarts()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));
            var published = new List<decimal>();
            indicator.Updated += (_, point) => published.Add(point.Value);

            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                foreach (var (symbol, prices) in PricesBySymbol)
                {
                    if (i != 3 || symbol != Symbols.AAPL)
                    {
                        indicator.Update(CreateBar(symbol, i, prices[i]));
                    }
                }
                if (i == 3)
                {
                    // the period without AAPL is correlated when the next one starts
                    Assert.AreEqual(1, published.Count);
                }
            }

            // scipy.stats.spearmanr: index 3 correlates the other four assets, and at index 4 AAPL pairs its signal at
            // index 2 with its return since index 2, its last bar
            AssertPublished(new[] { -0.7, -0.4, 0.1, -0.2, 0.9 }, published);
        }

        [Test]
        public void KeepsCorrelatingTheOtherAssetsWhenOneStopsGettingData()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));
            var published = new List<decimal>();
            indicator.Updated += (_, point) => published.Add(point.Value);

            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                foreach (var (symbol, prices) in PricesBySymbol)
                {
                    // AAPL has no data after index 3, such as after a delisting
                    if (i <= 3 || symbol != Symbols.AAPL)
                    {
                        indicator.Update(CreateBar(symbol, i, prices[i]));
                    }
                }
            }
            // scipy.stats.spearmanr across the other four assets from index 4. Index 6 is correlated when the next period starts
            AssertPublished(new[] { -0.7, -0.4, 0.2, 0.0 }, published);

            indicator.Update(CreateBar(Symbols.IBM, ExpectedValues.Length, 100m));
            AssertPublished(new[] { -0.7, -0.4, 0.2, 0.0, 1.0 }, published);
        }

        [Test]
        public void CorrelatesAPeriodAtOnceAfterAnAssetIsRemoved()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));
            var published = new List<decimal>();
            indicator.Updated += (_, point) => published.Add(point.Value);

            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                if (i == 4)
                {
                    indicator.Remove(Symbols.AAPL);
                    Assert.IsFalse(indicator.Signals.ContainsKey(Symbols.AAPL));
                }
                // the removed asset's inputs are ignored
                UpdateAll(indicator, i);
            }

            AssertPublished(new[] { -0.7, -0.4, 0.2, 0.0, 1.0 }, published);
        }

        [Test]
        public void AddsAssets()
        {
            var indicator = new CrossSectionalInformationCoefficient(new[] { Symbols.IBM, Symbols.GOOG }, _ => new RateOfChange(1));
            var ibmSignal = indicator.Signals[Symbols.IBM];

            foreach (var symbol in PricesBySymbol.Keys)
            {
                indicator.Add(symbol);
            }

            Assert.AreEqual(PricesBySymbol.Count, indicator.Signals.Count);
            Assert.AreSame(ibmSignal, indicator.Signals[Symbols.IBM]);
            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                UpdateAll(indicator, i);
                Assert.AreEqual(ExpectedValues[i] ?? 0, (double)indicator.Current.Value, 1e-9, $"Index {i}");
            }
        }

        [Test]
        public void SkipsTheReturnOfAnAssetWhosePreviousPriceIsZero()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));
            var published = new List<decimal>();
            indicator.Updated += (_, point) => published.Add(point.Value);

            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                foreach (var (symbol, prices) in PricesBySymbol)
                {
                    indicator.Update(CreateBar(symbol, i, i == 3 && symbol == Symbols.AAPL ? 0m : prices[i]));
                }
            }

            // scipy.stats.spearmanr with AAPL's price 0 at index 3: index 4 correlates the other four assets
            AssertPublished(new[] { -0.7, -0.2, 0.2, -0.1, 0.9 }, published);
        }

        [Test]
        public void ThrowsWhenASignalIsAlsoUpdatedElsewhere()
        {
            var signals = PricesBySymbol.Keys.ToDictionary(symbol => symbol, _ => new RateOfChange(1));
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, symbol => signals[symbol]);

            Assert.Throws<InvalidOperationException>(() =>
            {
                for (var i = 0; i < ExpectedValues.Length; i++)
                {
                    var bar = CreateBar(Symbols.AAPL, i, PricesBySymbol[Symbols.AAPL][i]);
                    signals[Symbols.AAPL].Update(bar.EndTime, bar.Close);
                    UpdateAll(indicator, i);
                }
            });
        }

        [Test]
        public void WarmUpPeriodIsTheSamplesNeededToBeReady()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(2));

            for (var i = 0; i < indicator.WarmUpPeriod; i++)
            {
                Assert.IsFalse(indicator.IsReady);
                UpdateAll(indicator, i);
            }

            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void UpdatesAndResetsItsSignals()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));
            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                UpdateAll(indicator, i);
            }
            Assert.IsTrue(indicator.Signals.Values.All(signal => signal.Samples == ExpectedValues.Length));

            indicator.Reset();

            Assert.IsFalse(indicator.IsReady);
            Assert.AreEqual(0, indicator.Current.Value);
            Assert.IsTrue(indicator.Signals.Values.All(signal => signal.Samples == 0));

            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                UpdateAll(indicator, i);
                Assert.AreEqual(ExpectedValues[i] ?? 0, (double)indicator.Current.Value, 1e-9, $"Index {i}");
            }
        }

        [Test]
        public void PublishesOneValuePerCrossSection()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));
            var average = new SimpleMovingAverage(3).Of(indicator);
            var published = new List<decimal>();
            indicator.Updated += (_, point) => published.Add(point.Value);

            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                UpdateAll(indicator, i);
            }

            AssertPublished(ExpectedValues.Where(value => value.HasValue).Select(value => value.Value), published);
            // Of waits for the coefficient to be ready, so the average gets exactly the ready coefficients
            Assert.AreEqual(ExpectedValues.Count(value => value.HasValue), average.Samples);
            Assert.AreEqual(ExpectedValues.TakeLast(3).Average(value => value.Value), (double)average.Current.Value, 1e-9);
        }

        [Test]
        public void CorrelatesALargeCrossSection()
        {
            // each asset grows at its own constant rate, so its previous return ranks the same as its next one
            var symbols = Enumerable.Range(0, 1000).Select(i => Symbol.Create($"A{i}", SecurityType.Equity, Market.USA)).ToList();
            var indicator = new CrossSectionalInformationCoefficient(symbols, _ => new RateOfChange(1));
            var published = 0;
            indicator.Updated += (_, _) => published++;

            for (var period = 0; period < 4; period++)
            {
                for (var i = 0; i < symbols.Count; i++)
                {
                    var price = 100m * (decimal)Math.Pow(1 + 0.0001 * (i + 1), period);
                    indicator.Update(CreateBar(symbols[i], period, price));
                }
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1m, Math.Round(indicator.Current.Value, 9));
            Assert.AreEqual(2, published);
        }

        [Test]
        public void RejectsACompositeSignal()
        {
            Assert.Throws<ArgumentException>(() =>
                new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1).Times(-1m)));
        }

        [Test]
        public void WarmsUpOneInputForASignalWithoutWarmUpPeriod()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new ReadyOnFirstInputIndicator());

            Assert.AreEqual(2, indicator.WarmUpPeriod);
            for (var i = 0; i < indicator.WarmUpPeriod; i++)
            {
                Assert.IsFalse(indicator.IsReady);
                UpdateAll(indicator, i);
            }
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void WaitsForThreeAssets()
        {
            var indicator = new CrossSectionalInformationCoefficient(new[] { Symbols.AAPL, Symbols.IBM }, _ => new RateOfChange(1));
            var published = 0;
            indicator.Updated += (_, _) => published++;

            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                indicator.Update(CreateBar(Symbols.AAPL, i, PricesBySymbol[Symbols.AAPL][i]));
                indicator.Update(CreateBar(Symbols.IBM, i, PricesBySymbol[Symbols.IBM][i]));
            }

            Assert.IsFalse(indicator.IsReady);
            Assert.AreEqual(0, published);
        }

        [Test]
        public void IgnoresInputsOfAssetsItDoesNotTrack()
        {
            var indicator = new CrossSectionalInformationCoefficient(PricesBySymbol.Keys, _ => new RateOfChange(1));
            for (var i = 0; i < ExpectedValues.Length; i++)
            {
                indicator.Update(CreateBar(Symbols.BTCUSD, i, 100m + i));
                UpdateAll(indicator, i);
                Assert.AreEqual(ExpectedValues[i] ?? 0, (double)indicator.Current.Value, 1e-9, $"Index {i}");
            }
        }

        [Test]
        public void CreatesSignalsFromAPythonFunction()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(), @"
from AlgorithmImports import *

def create(symbols):
    return CrossSectionalInformationCoefficient(symbols, lambda symbol: RateOfChange(1))
");
                var indicator = module.GetAttr("create").Invoke(PricesBySymbol.Keys.ToList().ToPython())
                    .GetAndDispose<CrossSectionalInformationCoefficient>();

                for (var i = 0; i < ExpectedValues.Length; i++)
                {
                    UpdateAll(indicator, i);
                    Assert.AreEqual(ExpectedValues[i] ?? 0, (double)indicator.Current.Value, 1e-9, $"Index {i}");
                }
            }
        }

        private static void AssertPublished(IEnumerable<double> expected, List<decimal> published)
        {
            CollectionAssert.AreEqual(expected.Select(value => Math.Round(value, 9)), published.Select(value => Math.Round((double)value, 9)));
        }

        private static void UpdateAll(CrossSectionalInformationCoefficient indicator, int index)
        {
            foreach (var (symbol, prices) in PricesBySymbol)
            {
                indicator.Update(CreateBar(symbol, index, prices[index]));
            }
        }

        private static TradeBar CreateBar(Symbol symbol, int index, decimal price)
        {
            return new TradeBar(StartTime.AddDays(index), symbol, price, price, price, price, 1000, TimeSpan.FromDays(1));
        }
    }
}
