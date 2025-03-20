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
using MathNet.Numerics.Statistics;
using static QuantConnect.Tests.Indicators.TestHelper;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class BetaIndicatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override string TestFileName => "bi_datatest.csv";

        protected override string TestColumnName => "Beta";

        private DateTime _reference = new DateTime(2020, 1, 1);

        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            Symbol symbolA = "AMZN 2T";
            Symbol symbolB = "SPX 2T";
            if (SymbolList.Count > 1)
            {
                symbolA = SymbolList[0];
                symbolB = SymbolList[1];
            }
#pragma warning disable CS0618
            var indicator = new Beta("testBetaIndicator", symbolA, symbolB, 5);
#pragma warning restore CS0618
            return indicator;
        }

        protected override List<Symbol> GetSymbols()
        {
            return [Symbols.SPY, Symbols.AAPL];
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = new Beta("testBetaIndicator", Symbols.IBM, Symbols.SPY, 5);

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
            var indicator = new Beta("testBetaIndicator", Symbols.IBM, Symbols.SPY, 5);
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!period.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            for (var i = 0; i < period.Value; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2 * period.Value, indicator.Samples);
        }

        [Test]
        public override void WorksWithLowValues()
        {
#pragma warning disable CS0618
            Symbol = "SPX 2T";
#pragma warning restore CS0618
            base.WorksWithLowValues();
        }

        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var firstRenkoConsolidator = new RenkoConsolidator(10m);
            var secondRenkoConsolidator = new RenkoConsolidator(10m);
            firstRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            secondRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            foreach (var parts in GetCsvFileStream(TestFileName))
            {
                var tradebar = parts.GetTradeBar();
                if (tradebar.Symbol.Value == "AMZN")
                {
                    firstRenkoConsolidator.Update(tradebar);
                }
                else
                {
                    secondRenkoConsolidator.Update(tradebar);
                }
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreNotEqual(0, indicator.Samples);
            firstRenkoConsolidator.Dispose();
            secondRenkoConsolidator.Dispose();
        }

        [Test]
        public override void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var firstVolumeRenkoConsolidator = new VolumeRenkoConsolidator(1000000);
            var secondVolumeRenkoConsolidator = new VolumeRenkoConsolidator(1000000000);
            firstVolumeRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            secondVolumeRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            foreach (var parts in GetCsvFileStream(TestFileName))
            {
                var tradebar = parts.GetTradeBar();
                if (tradebar.Symbol.Value == "AMZN")
                {
                    firstVolumeRenkoConsolidator.Update(tradebar);
                }
                else
                {
                    secondVolumeRenkoConsolidator.Update(tradebar);
                }
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreNotEqual(0, indicator.Samples);
            firstVolumeRenkoConsolidator.Dispose();
            secondVolumeRenkoConsolidator.Dispose();
        }


        [Test]
        public void AcceptsQuoteBarsAsInput()
        {
            var indicator = new Beta("testBetaIndicator", Symbols.IBM, Symbols.SPY, 5);

            for (var i = 10; i > 0; i--)
            {
                indicator.Update(new QuoteBar { Symbol = Symbols.IBM, Ask = new Bar(1, 2, 1, 500), Bid = new Bar(1, 2, 1, 500), Time = _reference.AddDays(1 + i) });
                indicator.Update(new QuoteBar { Symbol = Symbols.SPY, Ask = new Bar(1, 2, 1, 500), Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public void EqualBetaValue()
        {
            var indicator = new Beta("testBetaIndicator", Symbols.AAPL, Symbols.SPX, 5);

            for (int i = 0; i < 3; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = startTime, EndTime = endTime });
            }

            Assert.AreEqual(1, (double)indicator.Current.Value, 0.0001);
        }

        [Test]
        public void NotEqualBetaValue()
        {
            var indicator = new Beta("testBetaIndicator", Symbols.AAPL, Symbols.SPX, 5);

            for (int i = 0; i < 3; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = i + 2, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreNotEqual(1, (double)indicator.Current.Value);
        }

        [Test]
        public void ValidateBetaCalculation()
        {
            var beta = new Beta(Symbols.AAPL, Symbols.SPX, 3);

            var values = new List<TradeBar>()
            {
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 10, Time = _reference.AddDays(1), EndTime = _reference.AddDays(2) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 35, Time = _reference.AddDays(1), EndTime = _reference.AddDays(2) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 2, Time = _reference.AddDays(2),EndTime = _reference.AddDays(3) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 2, Time = _reference.AddDays(2), EndTime = _reference.AddDays(3) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 15, Time = _reference.AddDays(3), EndTime = _reference.AddDays(4) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 80, Time = _reference.AddDays(3), EndTime = _reference.AddDays(4) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 4, Time = _reference.AddDays(4), EndTime = _reference.AddDays(5) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 4, Time = _reference.AddDays(4), EndTime = _reference.AddDays(5) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 37, Time = _reference.AddDays(5), EndTime = _reference.AddDays(6) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 90, Time = _reference.AddDays(5), EndTime = _reference.AddDays(6) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 105, Time = _reference.AddDays(6), EndTime = _reference.AddDays(7) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 302, Time = _reference.AddDays(6), EndTime = _reference.AddDays(7) },
            };

            // Calculating beta manually using the formula: Beta = Covariance(AAPL, SPX) / Variance(SPX)
            var closeAAPL = new List<double>() { 10, 15, 90, 105 };
            var closeSPX = new List<double>() { 35, 80, 37, 302 };
            var priceChangesAAPL = new List<double>();
            var priceChangesSPX = new List<double>();
            for (int i = 1; i < 4; i++)
            {
                priceChangesAAPL.Add((closeAAPL[i] - closeAAPL[i - 1]) / closeAAPL[i - 1]);
                priceChangesSPX.Add((closeSPX[i] - closeSPX[i - 1]) / closeSPX[i - 1]);
            }
            var variance = priceChangesSPX.Variance();
            var covariance = priceChangesAAPL.Covariance(priceChangesSPX);
            var expectedBeta = (decimal)(covariance / variance);

            // Calculating beta using the indicator
            for (int i = 0; i < values.Count; i++)
            {
                beta.Update(values[i]);
            }

            Assert.AreEqual(expectedBeta, beta.Current.Value);
        }

        [Test]
        public void BetaWithDifferentTimeZones()
        {
            var indicator = new Beta(Symbols.SPY, Symbols.BTCUSD, 5);

            for (int i = 0; i < 10; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.BTCUSD, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = startTime, EndTime = endTime });
            }
            Assert.AreEqual(1, (double)indicator.Current.Value);
        }

        [Test]
        public override void TracksPreviousState()
        {
            var period = 5;
            var indicator = new Beta(Symbols.SPY, Symbols.AAPL, period);
            var previousValue = indicator.Current.Value;

            // Update the indicator and verify the previous values
            for (var i = 1; i < 2 * period; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 1000 + i * 10, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 1000 + (i * 15), Time = startTime, EndTime = endTime });
                // Verify the previous value matches the indicator's previous value
                Assert.AreEqual(previousValue, indicator.Previous.Value);

                // Update previousValue to the current value for the next iteration
                previousValue = indicator.Current.Value;
            }
        }
    }
}
