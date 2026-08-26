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

using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using static QuantConnect.Tests.Indicators.TestHelper;

namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// The expected values of the LSMAWithReference column of bi_datatest.csv were computed with
    /// numpy.polyfit, fitting the AMZN closes on the SPX closes of each five point window and
    /// evaluating the resulting line at the latest SPX close.
    /// </summary>
    [TestFixture]
    public class LeastSquaresMovingAverageWithReferenceTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override string TestFileName => "bi_datatest.csv";

        protected override string TestColumnName => "LSMAWithReference";

        private DateTime _reference = new DateTime(2020, 1, 1);

        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            Symbol targetSymbol = "AMZN 2T";
            Symbol referenceSymbol = "SPX 2T";
            if (SymbolList.Count > 1)
            {
                targetSymbol = SymbolList[0];
                referenceSymbol = SymbolList[1];
            }
            return new LeastSquaresMovingAverageWithReference("testLSMAWithReferenceIndicator", targetSymbol, referenceSymbol, 5);
        }

        protected override List<Symbol> GetSymbols()
        {
            return [Symbols.SPY, Symbols.AAPL];
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.IBM, Symbols.SPY, 5);

            for (var i = 10; i > 0; i--)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.IBM, Symbols.SPY, 5);
            var period = ((IIndicatorWarmUpPeriodProvider)indicator).WarmUpPeriod;

            for (var i = 0; i < period; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = 1, High = 2, Volume = 100, Close = 500 + i, Time = startTime, EndTime = endTime });
                Assert.IsFalse(indicator.IsReady, $"ready after the target bar of index {i}");
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 400 + 2 * i, Time = startTime, EndTime = endTime });
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(2 * period, indicator.Samples);
        }

        [Test]
        public override void WorksWithLowValues()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.IBM, Symbols.SPY, 5);

            var random = new Random();
            for (var i = 0; i < 20; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                var targetValue = (decimal)(random.NextDouble() * 0.000000000000000000000000000001);
                var referenceValue = (decimal)(random.NextDouble() * 0.000000000000000000000000000001);
                Assert.DoesNotThrow(() =>
                {
                    indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = targetValue, High = targetValue, Open = targetValue, Close = targetValue, Time = startTime, EndTime = endTime });
                    indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = referenceValue, High = referenceValue, Open = referenceValue, Close = referenceValue, Time = startTime, EndTime = endTime });
                });
            }
        }

        [Test]
        public override void TracksPreviousState()
        {
            var period = 5;
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.SPY, Symbols.AAPL, period);
            var previousValue = indicator.Current.Value;

            for (var i = 1; i < 2 * period; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 1000 + i * 10, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 1000 + (i * 15), Time = startTime, EndTime = endTime });

                Assert.AreEqual(previousValue, indicator.Previous.Value);

                previousValue = indicator.Current.Value;
            }
        }

        [Test]
        public override void IndicatorShouldHaveSymbolAfterUpdates()
        {
            var period = 5;
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.SPY, Symbols.AAPL, period);

            for (var i = 0; i < 2 * period; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                // The value takes the symbol of the update it was computed on
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 1000 + i * 10, Time = startTime, EndTime = endTime });
                Assert.AreEqual(Symbols.SPY, indicator.Current.Symbol);

                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 1000 + (i * 15), Time = startTime, EndTime = endTime });
                Assert.AreEqual(Symbols.AAPL, indicator.Current.Symbol);
            }
        }

        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var targetRenkoConsolidator = new RenkoConsolidator(10m);
            var referenceRenkoConsolidator = new RenkoConsolidator(10m);
            targetRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            referenceRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            foreach (var parts in GetCsvFileStream(TestFileName))
            {
                var tradebar = parts.GetTradeBar();
                if (tradebar.Symbol.Value == "AMZN")
                {
                    targetRenkoConsolidator.Update(tradebar);
                }
                else
                {
                    referenceRenkoConsolidator.Update(tradebar);
                }
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreNotEqual(0, indicator.Samples);
            targetRenkoConsolidator.Dispose();
            referenceRenkoConsolidator.Dispose();
        }

        [Test]
        public override void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var targetVolumeRenkoConsolidator = new VolumeRenkoConsolidator(1000000);
            var referenceVolumeRenkoConsolidator = new VolumeRenkoConsolidator(1000000000);
            targetVolumeRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            referenceVolumeRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            foreach (var parts in GetCsvFileStream(TestFileName))
            {
                var tradebar = parts.GetTradeBar();
                if (tradebar.Symbol.Value == "AMZN")
                {
                    targetVolumeRenkoConsolidator.Update(tradebar);
                }
                else
                {
                    referenceVolumeRenkoConsolidator.Update(tradebar);
                }
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreNotEqual(0, indicator.Samples);
            targetVolumeRenkoConsolidator.Dispose();
            referenceVolumeRenkoConsolidator.Dispose();
        }

        [Test]
        public void AcceptsQuoteBarsAsInput()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.IBM, Symbols.SPY, 5);

            // The target is worth twice the reference plus one at every time step
            for (var i = 0; i < 10; i++)
            {
                var time = _reference.AddDays(1 + i);
                var referenceValue = 100 + i;
                var targetValue = 2 * referenceValue + 1;
                indicator.Update(new QuoteBar { Symbol = Symbols.IBM, Ask = new Bar(1, 2, 1, targetValue), Bid = new Bar(1, 2, 1, targetValue), Time = time });
                indicator.Update(new QuoteBar { Symbol = Symbols.SPY, Ask = new Bar(1, 2, 1, referenceValue), Bid = new Bar(1, 2, 1, referenceValue), Time = time });
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(2d, (double)indicator.Slope.Current.Value, 1e-9);
            Assert.AreEqual(2 * 109 + 1, (double)indicator.Current.Value, 1e-9);
        }

        [Test]
        public void ValidateCalculation()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.AAPL, Symbols.SPX, 3);

            var bars = new List<TradeBar>()
            {
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 10, Time = _reference.AddDays(1), EndTime = _reference.AddDays(2) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 35, Time = _reference.AddDays(1), EndTime = _reference.AddDays(2) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 2, Time = _reference.AddDays(2), EndTime = _reference.AddDays(3) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 15, Time = _reference.AddDays(3), EndTime = _reference.AddDays(4) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 80, Time = _reference.AddDays(3), EndTime = _reference.AddDays(4) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 4, Time = _reference.AddDays(4), EndTime = _reference.AddDays(5) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 37, Time = _reference.AddDays(5), EndTime = _reference.AddDays(6) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 90, Time = _reference.AddDays(5), EndTime = _reference.AddDays(6) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 105, Time = _reference.AddDays(6), EndTime = _reference.AddDays(7) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 302, Time = _reference.AddDays(6), EndTime = _reference.AddDays(7) },
            };

            foreach (var bar in bars)
            {
                indicator.Update(bar);
            }

            // Only the time steps both symbols have a price for are paired up, and only the last
            // three of them are held by the indicator windows
            var closeAAPL = new List<double>() { 15, 90, 105 };
            var closeSPX = new List<double>() { 80, 37, 302 };

            // Fitting closeAAPL on closeSPX with the ordinary least squares closed form
            var count = closeSPX.Count;
            var sumX = closeSPX.Sum();
            var sumY = closeAAPL.Sum();
            var sumXy = closeSPX.Zip(closeAAPL, (x, y) => x * y).Sum();
            var sumXx = closeSPX.Sum(x => x * x);
            var expectedSlope = (count * sumXy - sumX * sumY) / (count * sumXx - sumX * sumX);
            var expectedIntercept = (sumY - expectedSlope * sumX) / count;
            var expectedValue = expectedIntercept + expectedSlope * closeSPX[^1];

            Assert.AreEqual(expectedSlope, (double)indicator.Slope.Current.Value, 1e-9);
            Assert.AreEqual(expectedIntercept, (double)indicator.Intercept.Current.Value, 1e-9);
            Assert.AreEqual(expectedValue, (double)indicator.Current.Value, 1e-9);
        }

        [Test]
        public void ProjectsTheReferenceWithALinearRelationship()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.AAPL, Symbols.SPX, 5);

            // The target is worth twice the reference plus one at every time step
            for (var i = 0; i < 10; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                var referenceValue = 100 + i;
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = referenceValue, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 2 * referenceValue + 1, Time = startTime, EndTime = endTime });
            }

            Assert.AreEqual(2d, (double)indicator.Slope.Current.Value, 1e-9);
            Assert.AreEqual(1d, (double)indicator.Intercept.Current.Value, 1e-9);
            Assert.AreEqual(2 * 109 + 1, (double)indicator.Current.Value, 1e-9);
        }

        [Test]
        public void ReturnsTheTargetPriceWhenTheReferenceDoesNotChange()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.AAPL, Symbols.SPX, 5);

            for (var i = 0; i < 10; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 200 + i, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 100, Time = startTime, EndTime = endTime });
            }

            // The regression line can not be fitted, so the indicator falls back to the target price
            Assert.AreEqual(209m, indicator.Current.Value);
            Assert.AreEqual(0m, indicator.Slope.Current.Value);
            Assert.AreEqual(0m, indicator.Intercept.Current.Value);
        }

        [Test]
        public void WorksWithDifferentTimeZones()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.SPY, Symbols.BTCUSD, 5);

            for (var i = 0; i < 10; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 2 * (100 + i) + 1, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.BTCUSD, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = startTime, EndTime = endTime });
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(2d, (double)indicator.Slope.Current.Value, 1e-9);
            Assert.AreEqual(2 * 109 + 1, (double)indicator.Current.Value, 1e-9);
        }

        [Test]
        public void PairsPricesByTimeRegardlessOfArrivalOrder()
        {
            var targetFirst = new LeastSquaresMovingAverageWithReference(Symbols.AAPL, Symbols.SPX, 5);
            var referenceFirst = new LeastSquaresMovingAverageWithReference(Symbols.AAPL, Symbols.SPX, 5);

            for (var i = 0; i < 10; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                var targetBar = new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = startTime, EndTime = endTime };
                var referenceBar = new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = startTime, EndTime = endTime };

                targetFirst.Update(targetBar);
                targetFirst.Update(referenceBar);

                referenceFirst.Update(referenceBar);
                referenceFirst.Update(targetBar);
            }

            Assert.IsTrue(targetFirst.IsReady);
            Assert.AreEqual(targetFirst.Current.Value, referenceFirst.Current.Value);
        }

        [Test]
        public void DoesNotPairPricesFromDifferentTimes()
        {
            var indicator = new LeastSquaresMovingAverageWithReference(Symbols.AAPL, Symbols.SPX, 5);

            for (var i = 0; i < 5; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = startTime, EndTime = endTime });
            }

            var lastValue = indicator.Current.Value;

            // The target bar of the next time step leaves the reference behind, so no price is paired up
            var lastStartTime = _reference.AddDays(6);
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 500, Time = lastStartTime, EndTime = lastStartTime.AddDays(1) });

            Assert.AreEqual(lastValue, indicator.Current.Value);
        }

        [Test]
        public void ThrowsOnPeriodBelowTwo()
        {
            Assert.Throws<ArgumentException>(() =>
                new LeastSquaresMovingAverageWithReference(Symbols.AAPL, Symbols.SPX, 1));
        }
    }
}
