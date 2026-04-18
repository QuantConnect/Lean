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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class McClellanOscillatorTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            var mcClellanOscillator = new McClellanOscillator(19, 39);
            if (SymbolList.Count > 2)
            {
                SymbolList.Take(3).ToList().ForEach(mcClellanOscillator.Add);
            }
            else
            {
                mcClellanOscillator.Add(Symbols.MSFT);
                mcClellanOscillator.Add(Symbols.GOOG);
                mcClellanOscillator.Add(Symbols.AAPL);
            }
            return mcClellanOscillator;
        }

        protected override List<Symbol> GetSymbols()
        {
            return [Symbols.SPY, Symbols.AAPL, Symbols.IBM];
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = (McClellanOscillator)CreateIndicator();
            var reference = DateTime.Today;

            for (int i = 1; i <= indicator.WarmUpPeriod; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = i, Volume = 1, Time = reference.AddMinutes(i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = i, Volume = 1, Time = reference.AddMinutes(i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = i, Volume = 1, Time = reference.AddMinutes(i) });
            }

            Assert.AreEqual(0m, indicator.Current.Value);
            Assert.AreEqual(indicator.WarmUpPeriod * 3, indicator.Samples);
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var indicator = (McClellanOscillator)CreateIndicator();
            var reference = DateTime.Today;

            for (int i = 1; i <= indicator.WarmUpPeriod; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = i, Volume = 1, Time = reference.AddMinutes(i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = i, Volume = 1, Time = reference.AddMinutes(i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = i, Volume = 1, Time = reference.AddMinutes(i) });
            }

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(indicator);
        }

        [Test]
        public override void ComparesAgainstExternalData()
        {
            var indicator = new TestMcClellanOscillator();
            McClellanIndicatorTestHelper.RunTestIndicator(indicator, TestFileName, TestColumnName);
        }

        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            var indicator = new TestMcClellanOscillator();
            McClellanIndicatorTestHelper.RunTestIndicator(indicator, TestFileName, TestColumnName);
            indicator.Reset();
            McClellanIndicatorTestHelper.RunTestIndicator(indicator, TestFileName, TestColumnName);
        }

        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            var indicator = new TestMcClellanOscillator();
            var renkoConsolidator = new RenkoConsolidator(0.5m);
            renkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            McClellanIndicatorTestHelper.UpdateRenkoConsolidator(renkoConsolidator, TestFileName);
            Assert.AreNotEqual(0, indicator.Samples);
            renkoConsolidator.Dispose();
        }

        [Test]
        public override void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = new TestMcClellanOscillator();
            var volumeRenkoConsolidator = new VolumeRenkoConsolidator(0.5m);
            volumeRenkoConsolidator.DataConsolidated += (sender, volumeRenkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(volumeRenkoBar));
            };

            McClellanIndicatorTestHelper.UpdateRenkoConsolidator(volumeRenkoConsolidator, TestFileName);
            Assert.AreNotEqual(0, indicator.Samples);
            volumeRenkoConsolidator.Dispose();
        }

        protected override string TestFileName => "mcclellan_data.csv";

        protected override string TestColumnName => "MO";
    }

    public class TestMcClellanOscillator : McClellanOscillator, ITestMcClellanOscillator
    {
        private Dictionary<Symbol, decimal> _symbols = new();
        private int _dateCount = 1;

        public TestMcClellanOscillator() : base()
        {
            // Maximum A/D difference from the test set is 2527
            for (int i = 1; i <= 2530; i++)
            {
                var symbol = Symbol.Create($"TestSymbol{i}", SecurityType.Equity, Market.USA);
                _symbols.Add(symbol, 0m);
                Add(symbol);
            }

            // Set to the first EMA values to account for past A/D Difference values that we don't have access
            Reset();
            EMAFast.Update(new DateTime(2022, 6, 30), -209.85m);
            EMASlow.Update(new DateTime(2022, 6, 30), -186.41m);
        }

        public void TestUpdate(IndicatorDataPoint input)
        {
            var isTotal2530 = McClellanIndicatorTestHelper.GetAdvanceDeclineNumber(input.Value, out var advance, out var decline);
            var symbols = _symbols.Keys.ToList();

            for (int i = 0; i < advance; i++)
            {
                Update(new TradeBar() { Symbol = symbols[i], Close = _dateCount, Volume = 1, Time = input.Time });
                _symbols[symbols[i]] = _dateCount;
            }
            for (int j = 1; j <= decline; j++)
            {
                Update(new TradeBar() { Symbol = symbols[^j], Close = -_dateCount, Volume = 1, Time = input.Time });
                _symbols[symbols[^j]] = -_dateCount;
            }
            if (!isTotal2530)
            {
                Update(new TradeBar() { Symbol = symbols[advance], Close = _symbols[symbols[advance]], Volume = 1, Time = input.Time });
            }

            _dateCount += 1;
        }

        public override void Reset()
        {
            base.Reset();
            _dateCount = 1;

            foreach (var symbol in _symbols.Keys)
            {
                _symbols[symbol] = 0m;
            }
        }
    }
}
