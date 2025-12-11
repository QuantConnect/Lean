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
using System.Collections.Generic;
using static QuantConnect.Tests.Indicators.TestHelper;

namespace QuantConnect.Tests.Indicators
{
    public abstract class NewHighsNewLowsTestsBase<T> : CommonIndicatorTests<T>
        where T : class, IBaseDataBar
    {
        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            IndicatorBase<T> indicator = CreateIndicator();
            if (indicator is IndicatorBase<TradeBar>)
            {
                RenkoConsolidator aaplRenkoConsolidator = new(10000m);
                aaplRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                RenkoConsolidator googRenkoConsolidator = new(100000m);
                googRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                RenkoConsolidator ibmRenkoConsolidator = new(10000m);
                ibmRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                foreach (IReadOnlyDictionary<string, string> parts in GetCsvFileStream(TestFileName))
                {
                    TradeBar tradebar = parts.GetTradeBar();
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
            IndicatorBase<T> indicator = CreateIndicator();
            if (indicator is IndicatorBase<TradeBar>)
            {
                VolumeRenkoConsolidator aaplRenkoConsolidator = new(10000000m);
                aaplRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                VolumeRenkoConsolidator googRenkoConsolidator = new(500000m);
                googRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                VolumeRenkoConsolidator ibmRenkoConsolidator = new(500000m);
                ibmRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                foreach (IReadOnlyDictionary<string, string> parts in GetCsvFileStream(TestFileName))
                {
                    TradeBar tradebar = parts.GetTradeBar();
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
