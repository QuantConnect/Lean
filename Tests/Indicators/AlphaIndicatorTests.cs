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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using static QuantConnect.Tests.Indicators.TestHelper;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AlphaIndicatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override string TestFileName => "alpha_indicator_datatest.csv";

        protected override string TestColumnName => "Alpha";

        private DateTime _reference = new DateTime(2020, 1, 1);

        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            var indicator = new Alpha("testAlphaIndicator", "AMZN 2T", "SPX 2T", 5);
            return indicator;
        }
        [Test]
        public override void TimeMovesForward()
        {
            var indicator = new Alpha("testAlphaIndicator", Symbols.IBM, Symbols.SPY, 5);

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
            var period = 5;
            var indicator = new Alpha("testAlphaIndicator", Symbols.IBM, Symbols.SPY, period);
            var warmUpPeriod = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!warmUpPeriod.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            // warmup period is 5 + 1
            for (var i = 1; i <= warmUpPeriod.Value; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });

                Assert.IsFalse(indicator.IsReady);

                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });

                if (i < warmUpPeriod.Value)
                {
                    Assert.IsFalse(indicator.IsReady);
                }
                else
                {
                    Assert.IsTrue(indicator.IsReady);
                }

            }

            Assert.AreEqual(2 * warmUpPeriod.Value, indicator.Samples);
        }

        [Test]
        public override void WorksWithLowValues()
        {
            var indicator = new Alpha("testAlphaIndicator", Symbols.IBM, Symbols.SPY, 10);
            var warmUpPeriod = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            var random = new Random();
            var time = DateTime.UtcNow;
            for (int i = 0; i < 2 * warmUpPeriod; i++)
            {
                var value = (decimal)(random.NextDouble() * 0.000000000000000000000000000001);
                Assert.DoesNotThrow(() => indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = value, High = value, Volume = 100, Close = value, Time = _reference.AddDays(1 + i) }));
                Assert.DoesNotThrow(() => indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = value, High = value, Volume = 100, Close = value, Time = _reference.AddDays(1 + i) }));
            }
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
            var indicator = new Alpha("testAlphaIndicator", Symbols.IBM, Symbols.SPY, 5);

            for (var i = 10; i > 0; i--)
            {
                indicator.Update(new QuoteBar { Symbol = Symbols.IBM, Ask = new Bar(1, 2, 1, 500), Bid = new Bar(1, 2, 1, 500), Time = _reference.AddDays(1 + i) });
                indicator.Update(new QuoteBar { Symbol = Symbols.SPY, Ask = new Bar(1, 2, 1, 500), Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public void EqualAlphaValue()
        {
            int period = 5;
            var indicator = new Alpha("testAlphaIndicator", Symbols.AAPL, Symbols.SPX, period);

            for (int i = 0; i <= period; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = _reference.AddDays(1 + i) });

                Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);

                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = _reference.AddDays(1 + i) });

                if (i < period)
                {
                    Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);
                }
                else
                {
                    Assert.AreEqual(0.0032053150, (double)indicator.Current.Value, 0.0000000001);
                }

            }
        }

        [Test]
        public void RiskFreeRate()
        {
            decimal riskFreeRate = 0.0002m;
            int period = 5;
            var indicator = new Alpha("testAlphaIndicator", Symbols.AAPL, Symbols.SPX, period, riskFreeRate);

            for (int i = 0; i <= period; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = _reference.AddDays(1 + i) });

                Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);

                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = _reference.AddDays(1 + i) });

                if (i < period)
                {
                    Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);
                }
                else
                {
                    Assert.AreEqual(0.0030959108, (double)indicator.Current.Value, 0.0000000001);
                }

            }
        }

        [Test]
        public void RiskFreeRate252()
        {
            int alphaPeriod = 1;
            int betaPeriod = 252;
            decimal riskFreeRate = 0.0025m;
            var indicator = new Alpha("testAlphaIndicator", Symbols.AAPL, Symbols.SPX, alphaPeriod, betaPeriod, riskFreeRate);

            for (int i = 0; i <= betaPeriod; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = _reference.AddDays(1 + i) });

                Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);

                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = _reference.AddDays(1 + i) });

                if (i < betaPeriod)
                {
                    Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);
                }
                else
                {
                    Assert.AreEqual(-0.0000620852, (double)indicator.Current.Value, 0.0000000001);
                }

            }
        }

        [Test]
        public void NoRiskFreeRate252()
        {
            int alphaPeriod = 1;
            int betaPeriod = 252;
            var indicator = new Alpha("testAlphaIndicator", Symbols.AAPL, Symbols.SPX, alphaPeriod, betaPeriod);

            for (int i = 0; i <= betaPeriod; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = _reference.AddDays(1 + i) });

                Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);

                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = _reference.AddDays(1 + i) });

                if (i < betaPeriod)
                {
                    Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);
                }
                else
                {
                    Assert.AreEqual(0.0008518139, (double)indicator.Current.Value, 0.0000000001);
                }

            }
        }

        [Test]
        public void ConstantZeroRiskFreeRateModel()
        {
            int alphaPeriod = 1;
            int betaPeriod = 252;
            IRiskFreeInterestRateModel riskFreeRateModel = new ConstantRiskFreeRateInterestRateModel(0.0m);
            var indicator = new Alpha("testAlphaIndicator", Symbols.AAPL, Symbols.SPX, alphaPeriod, betaPeriod, riskFreeRateModel);

            for (int i = 0; i <= betaPeriod; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = _reference.AddDays(1 + i) });

                Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);

                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = _reference.AddDays(1 + i) });

                if (i < betaPeriod)
                {
                    Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);
                }
                else
                {
                    Assert.AreEqual(0.0008518139, (double)indicator.Current.Value, 0.0000000001);
                }

            }
        }

        [Test]
        public void ConstantRiskFreeRateModel()
        {
            int alphaPeriod = 1;
            int betaPeriod = 252;
            IRiskFreeInterestRateModel riskFreeRateModel = new ConstantRiskFreeRateInterestRateModel(0.0025m);
            var indicator = new Alpha("testAlphaIndicator", Symbols.AAPL, Symbols.SPX, alphaPeriod, betaPeriod, riskFreeRateModel);

            for (int i = 0; i <= betaPeriod; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = _reference.AddDays(1 + i) });

                Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);

                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = _reference.AddDays(1 + i) });

                if (i < betaPeriod)
                {
                    Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);
                }
                else
                {
                    Assert.AreEqual(-0.0000620852, (double)indicator.Current.Value, 0.0000000001);
                }

            }
        }

        [Test]
        public void NullRiskFreeRate()
        {
            int alphaPeriod = 1;
            int betaPeriod = 252;
            var indicator = new Alpha("testAlphaIndicator", Symbols.AAPL, Symbols.SPX, alphaPeriod, betaPeriod, riskFreeRate: null);

            for (int i = 0; i <= betaPeriod; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 100 + i, Time = _reference.AddDays(1 + i) });

                Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);

                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 200 + i * 3, Time = _reference.AddDays(1 + i) });

                if (i < betaPeriod)
                {
                    Assert.AreEqual(0.0, (double)indicator.Current.Value, 0.0000000001);
                }
                else
                {
                    Assert.AreEqual(0.0008518139, (double)indicator.Current.Value, 0.0000000001);
                }

            }
        }
    }
}
