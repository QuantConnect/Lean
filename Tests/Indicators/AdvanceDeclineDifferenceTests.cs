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
using System.Linq;
using static QuantConnect.Tests.Indicators.TestHelper;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AdvanceDeclineDifferenceTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            var adDifference = new AdvanceDeclineDifference("test_name");
            if (SymbolList.Count > 2)
            {
                SymbolList.Take(3).ToList().ForEach(adDifference.AddStock);
            }
            else
            {
                adDifference.AddStock(Symbols.AAPL);
                adDifference.AddStock(Symbols.IBM);
                adDifference.AddStock(Symbols.GOOG);
                RenkoBarSize = 5000000;
            }
            return adDifference;
        }

        protected override List<Symbol> GetSymbols()
        {
            return [Symbols.SPY, Symbols.AAPL, Symbols.IBM];
        }

        [Test]
        public virtual void ShouldIgnoreRemovedStocks()
        {
            var adDifference = (AdvanceDeclineDifference)CreateIndicator();
            var reference = System.DateTime.Today;

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, adDifference.Current.Value);

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 100, Time = reference.AddMinutes(2) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 100, Time = reference.AddMinutes(2) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 100, Time = reference.AddMinutes(2) });

            Assert.AreEqual(1m, adDifference.Current.Value);
            adDifference.Reset();
            adDifference.RemoveStock(Symbols.GOOG);

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 100, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, adDifference.Current.Value);

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 100, Time = reference.AddMinutes(2) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 100, Time = reference.AddMinutes(2) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 100, Time = reference.AddMinutes(2) });

            Assert.AreEqual(0m, adDifference.Current.Value);
        }

        [Test]
        public virtual void IgnorePeriodIfAnyStockMissed()
        {
            var adDifference = (AdvanceDeclineDifference)CreateIndicator();
            adDifference.AddStock(Symbols.MSFT);
            var reference = System.DateTime.Today;

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Time = reference.AddMinutes(1) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Time = reference.AddMinutes(1) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Time = reference.AddMinutes(1) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Time = reference.AddMinutes(1) });

            // value is not ready yet
            Assert.AreEqual(0m, adDifference.Current.Value);

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Time = reference.AddMinutes(2) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Time = reference.AddMinutes(2) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Time = reference.AddMinutes(2) });

            Assert.AreEqual(0m, adDifference.Current.Value);

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 3, Time = reference.AddMinutes(3) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Time = reference.AddMinutes(3) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 2, Time = reference.AddMinutes(3) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Time = reference.AddMinutes(3) });

            Assert.AreEqual(1m, adDifference.Current.Value);

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Time = reference.AddMinutes(4) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 2, Time = reference.AddMinutes(4) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Time = reference.AddMinutes(4) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 2, Time = reference.AddMinutes(4) });

            Assert.AreEqual(2m, adDifference.Current.Value);

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Time = reference.AddMinutes(5) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 3, Time = reference.AddMinutes(5) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 1, Time = reference.AddMinutes(5) });

            Assert.AreEqual(2m, adDifference.Current.Value);

            adDifference.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Time = reference.AddMinutes(6) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 4, Time = reference.AddMinutes(6) });
            adDifference.Update(new TradeBar() { Symbol = Symbols.MSFT, Close = 5, Time = reference.AddMinutes(6) });

            Assert.AreEqual(2m, adDifference.Current.Value);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 1, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 1, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 1, Time = reference.AddMinutes(2) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1m, indicator.Current.Value);
            Assert.AreEqual(6, indicator.Samples);
        }

        [Test]
        public virtual void WarmsUpOrdered()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });

            // indicator is not ready yet
            Assert.IsFalse(indicator.IsReady);

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 1, Time = reference.AddMinutes(2) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1m, indicator.Current.Value);
        }

        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<TradeBar>)
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
            if (indicator is IndicatorBase<TradeBar>)
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

        [Test]
        public override void IndicatorShouldHaveSymbolAfterUpdates()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            for (int i = 0; i < 10; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 1, Volume = 1, Time = reference.AddMinutes(1) });

                // indicator is not ready yet
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = 2, Volume = 1, Time = reference.AddMinutes(2) });
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 0.5m, Volume = 1, Time = reference.AddMinutes(2) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Volume = 1, Time = reference.AddMinutes(2) });

                // indicator is ready
                // The last update used Symbol.GOOG, so the indicator's current Symbol should be GOOG
                Assert.AreEqual(Symbols.GOOG, indicator.Current.Symbol);
            }
        }

        protected override string TestFileName => "arms_data.txt";

        protected override string TestColumnName => "A/D Difference";
    }
}
