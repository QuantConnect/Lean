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
    public abstract class NewHighsNewLowsTestsBase<T> : CommonIndicatorTests<T>
        where T : class, IBaseDataBar
    {
        protected override IndicatorBase<T> CreateIndicator()
        {
            var nhnlRatio = CreateNewHighsNewLowsIndicator();
            if (SymbolList.Count > 2)
            {
                SymbolList.Take(3).ToList().ForEach(nhnlRatio.Add);
            }
            else
            {
                nhnlRatio.Add(Symbols.AAPL);
                nhnlRatio.Add(Symbols.IBM);
                nhnlRatio.Add(Symbols.GOOG);
                RenkoBarSize = 5000000;
            }

            // Even if the indicator is ready, there may be zero values
            ValueCanBeZero = true;

            return nhnlRatio;
        }

        protected override List<Symbol> GetSymbols()
        {
            return [Symbols.SPY, Symbols.AAPL, Symbols.IBM];
        }

        protected override Action<IndicatorBase<T>, double> Assertion => (indicator, expected) =>
        {
            // we need to use the Ratio sub-indicator
            base.Assertion(GetSubIndicator(indicator), expected);
        };

        protected abstract NewHighsNewLows<T> CreateNewHighsNewLowsIndicator();

        protected abstract IndicatorBase<T> GetSubIndicator(IndicatorBase<T> mainIndicator);

        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<IBaseDataBar>)
            {
                var aaplRenkoConsolidator = new RenkoConsolidator(10000m);
                aaplRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                var googRenkoConsolidator = new RenkoConsolidator(100000m);
                googRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                var ibmRenkoConsolidator = new RenkoConsolidator(10000m);
                ibmRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                foreach (var parts in GetCsvFileStream(TestFileName))
                {
                    var tradebar = parts.GetTradeBar();
                    if (tradebar.Symbol.Value == "AAPL")
                    {
                        aaplRenkoConsolidator.Update(tradebar);
                    }
                    else if (tradebar.Symbol.Value == "GOOG")
                    {
                        googRenkoConsolidator.Update(tradebar);
                    }
                    else
                    {
                        ibmRenkoConsolidator.Update(tradebar);
                    }
                }

                Assert.IsTrue(indicator.IsReady);
                Assert.AreNotEqual(0, indicator.Samples);
                IndicatorValueIsNotZeroAfterReceiveRenkoBars(indicator);
                aaplRenkoConsolidator.Dispose();
                googRenkoConsolidator.Dispose();
                ibmRenkoConsolidator.Dispose();
            }
        }

        [Test]
        public override void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<IBaseDataBar>)
            {
                var aaplRenkoConsolidator = new VolumeRenkoConsolidator(10000000m);
                aaplRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                var googRenkoConsolidator = new VolumeRenkoConsolidator(500000m);
                googRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                var ibmRenkoConsolidator = new VolumeRenkoConsolidator(500000m);
                ibmRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                foreach (var parts in GetCsvFileStream(TestFileName))
                {
                    var tradebar = parts.GetTradeBar();
                    if (tradebar.Symbol.Value == "AAPL")
                    {
                        aaplRenkoConsolidator.Update(tradebar);
                    }
                    else if (tradebar.Symbol.Value == "GOOG")
                    {
                        googRenkoConsolidator.Update(tradebar);
                    }
                    else
                    {
                        ibmRenkoConsolidator.Update(tradebar);
                    }
                }

                Assert.IsTrue(indicator.IsReady);
                Assert.AreNotEqual(0, indicator.Samples);
                IndicatorValueIsNotZeroAfterReceiveVolumeRenkoBars(indicator);
                aaplRenkoConsolidator.Dispose();
                googRenkoConsolidator.Dispose();
                ibmRenkoConsolidator.Dispose();
            }
        }
    }
}
