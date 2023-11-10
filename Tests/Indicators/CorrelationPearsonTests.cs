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
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using static QuantConnect.Tests.Indicators.TestHelper;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class CorrelationPearsonTests : CommonIndicatorTests<IBaseDataBar>
    { 
        protected override string TestFileName => "spy_qqq_corr.csv";
        
        private DateTime _reference = new DateTime(2020, 1, 1);

        protected CorrelationType _correlationType = CorrelationType.Pearson;
        protected override string TestColumnName => (_correlationType==CorrelationType.Pearson)?"Correlation_Pearson":"Correlation_Spearman";
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            var indicator = new QuantConnect.Indicators.Correlation("testCorrelationIndicator", Symbols.SPY, "QQQ RIWIV7K5Z9LX", 252, _correlationType);
            return indicator;
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = new QuantConnect.Indicators.Correlation("testCorrelationIndicator",  Symbols.IBM, Symbols.SPY, 5, _correlationType);

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
            var indicator = new QuantConnect.Indicators.Correlation("testCorrelationIndicator", Symbols.IBM, Symbols.SPY, 5, _correlationType);
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
         
            Assert.AreEqual(2*period.Value, indicator.Samples);
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
            int counter = 0;
            foreach (var parts in GetCsvFileStream(TestFileName))
            {

                var tradebar = parts.GetTradeBar();
                if (tradebar.Symbol.Value == "SPY")
                {
                    firstRenkoConsolidator.Update(tradebar);
                }
                else
                {
                    secondRenkoConsolidator.Update(tradebar);
                    counter++;
                }
                if (counter >= 100)
                    break;
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
            var firstVolumeRenkoConsolidator = new VolumeRenkoConsolidator(100000);
            var secondVolumeRenkoConsolidator = new VolumeRenkoConsolidator(1000000);
            firstVolumeRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            secondVolumeRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };
            int counter = 0;
            foreach (var parts in GetCsvFileStream(TestFileName))
            {
                var tradebar = parts.GetTradeBar();
                if (tradebar.Symbol.Value == "SPY")
                {
                    firstVolumeRenkoConsolidator.Update(tradebar);
                }
                else
                {
                    secondVolumeRenkoConsolidator.Update(tradebar);
                    counter++;
                }
                if (counter >= 500)
                    break;
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreNotEqual(0, indicator.Samples);
            firstVolumeRenkoConsolidator.Dispose();
            secondVolumeRenkoConsolidator.Dispose();
        }
        [Test]
        public void AcceptsQuoteBarsAsInput()
        {
            var indicator = new QuantConnect.Indicators.Correlation("testCorrelationIndicator", Symbols.IBM, Symbols.SPY, 5, _correlationType);

            for (var i = 10; i > 0; i--)
            {
                indicator.Update(new QuoteBar { Symbol = Symbols.IBM, Ask = new Bar(1, 2, 1, 500), Bid = new Bar(1, 2, 1, 500), Time = _reference.AddDays(1 + i) });
                indicator.Update(new QuoteBar { Symbol = Symbols.SPY, Ask = new Bar(1, 2, 1, 500), Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public void EqualCorrelationValue()
        {
            var indicator = new QuantConnect.Indicators.Correlation("testCorrelationIndicator", Symbols.AAPL, Symbols.SPX, 3, _correlationType);

            for (int i = 0 ; i < 3 ; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = i + 1 ,Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(1, (double)indicator.Current.Value);
        }

        [Test]
        public void NotEqualCorrelationValue()
        {
            var indicator = new QuantConnect.Indicators.Correlation("testCorrelationIndicator", Symbols.AAPL, Symbols.SPX, 3, _correlationType);

            for (int i = 0; i < 3; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = i + 2, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreNotEqual(0, (double)indicator.Current.Value);
        }
    }
}
