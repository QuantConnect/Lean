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
    public class NewHighsNewLowsDifferenceTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            // For test purposes we use period of two
            var nhnlDifference = new NewHighsNewLowsDifference("test_name", 2);
            if (SymbolList.Count > 2)
            {
                SymbolList.Take(3).ToList().ForEach(nhnlDifference.Add);
            }
            else
            {
                nhnlDifference.Add(Symbols.AAPL);
                nhnlDifference.Add(Symbols.IBM);
                nhnlDifference.Add(Symbols.GOOG);
                RenkoBarSize = 5000000;
            }

            return nhnlDifference;
        }

        protected override List<Symbol> GetSymbols()
        {
            return [Symbols.SPY, Symbols.AAPL, Symbols.IBM];
        }

        [Test]
        public virtual void ShouldIgnoreRemovedStocks()
        {
            var indicator = (NewHighsNewLowsDifference)CreateIndicator();
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 0.9m, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.9m, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.9m, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.Current.Value);

            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 0.5m, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.3m, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.2m, Time = reference.AddMinutes(3) });

            Assert.AreEqual(-3m, indicator.Current.Value);

            indicator.Reset();
            indicator.Remove(Symbols.GOOG);

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 0.9m, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.9m, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.9m, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.Current.Value);

            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 0.5m, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.3m, Time = reference.AddMinutes(3) });
            // new low (ignored)
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.2m, Time = reference.AddMinutes(3) });

            Assert.AreEqual(-2m, indicator.Current.Value);
        }

        [Test]
        public virtual void IgnorePeriodIfAnyStockMissed()
        {
            var indicator = (NewHighsNewLowsDifference)CreateIndicator();
            indicator.Add(Symbols.MSFT);
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 1, Low = 1, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 2, Low = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 2, Low = 1, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.Current.Value);

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Time = reference.AddMinutes(3) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.5m, Time = reference.AddMinutes(3) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Low = 1, Time = reference.AddMinutes(3) });

            Assert.AreEqual(0m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 4, Low = 1, Time = reference.AddMinutes(4) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.3m, Time = reference.AddMinutes(4) });
            // no change
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Time = reference.AddMinutes(4) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 4, Low = 1, Time = reference.AddMinutes(4) });

            Assert.AreEqual(1m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 5, Low = 1, Time = reference.AddMinutes(5) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.2m, Time = reference.AddMinutes(5) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 4, Low = 1, Time = reference.AddMinutes(5) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 5, Low = 1, Time = reference.AddMinutes(5) });

            Assert.AreEqual(2m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 6, Low = 1, Time = reference.AddMinutes(6) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 3, Low = 1, Time = reference.AddMinutes(6) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 6, Low = 1, Time = reference.AddMinutes(6) });

            Assert.AreEqual(2m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 7, Low = 1, Time = reference.AddMinutes(7) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 1, Time = reference.AddMinutes(7) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 7, Low = 1, Time = reference.AddMinutes(7) });

            Assert.AreEqual(2m, indicator.Current.Value);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            // setup period (unordered)
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.5m, Low = 0.2m, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Time = reference.AddMinutes(2) });

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.75m, Low = 0.1m, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 2, Time = reference.AddMinutes(3) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1m, indicator.Current.Value);
            Assert.AreEqual(9, indicator.Samples);
        }

        [Test]
        public virtual void WarmsUpOrdered()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            // setup period (ordered)
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.5m, Low = 1, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Time = reference.AddMinutes(2) });

            // indicator is not ready yet
            Assert.IsFalse(indicator.IsReady);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 4, Low = 1, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 1, Time = reference.AddMinutes(3) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(3m, indicator.Current.Value);
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
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 1, Time = reference.AddMinutes(1) });
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 1, Time = reference.AddMinutes(1) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 1, Time = reference.AddMinutes(1) });

                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Volume = 1, Time = reference.AddMinutes(2) });
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.5m, Volume = 1, Time = reference.AddMinutes(2) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Volume = 1, Time = reference.AddMinutes(2) });

                // indicator is not ready yet

                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Volume = 1, Time = reference.AddMinutes(3) });
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.3m, Volume = 1, Time = reference.AddMinutes(3) });
                indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 4, Low = 1, Volume = 1, Time = reference.AddMinutes(3) });

                // indicator is ready

                // The last update used Symbol.GOOG, so the indicator's current Symbol should be GOOG
                Assert.AreEqual(Symbols.GOOG, indicator.Current.Symbol);
            }
        }

        protected override string TestFileName => "nhnl_data.csv";

        protected override string TestColumnName => "NH/NL Difference";
    }
}
