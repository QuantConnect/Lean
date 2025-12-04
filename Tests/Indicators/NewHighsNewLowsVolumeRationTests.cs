using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    internal class NewHighsNewLowsVolumeRationTests : NewHighsNewLowsDifferenceTests
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            // For test purposes we use period of two
            var nhnlVolumeRatio = new NewHighsNewLowsVolumeRatio("test_name", 2);
            if (SymbolList.Count > 2)
            {
                SymbolList.Take(3).ToList().ForEach(nhnlVolumeRatio.Add);
            }
            else
            {
                nhnlVolumeRatio.Add(Symbols.AAPL);
                nhnlVolumeRatio.Add(Symbols.IBM);
                nhnlVolumeRatio.Add(Symbols.GOOG);
                RenkoBarSize = 5000000;
            }

            // Even if the indicator is ready, there may be zero values
            ValueCanBeZero = true;

            return nhnlVolumeRatio;
        }

        [Test]
        public override void ShouldIgnoreRemovedStocks()
        {
            var indicator = (NewHighsNewLowsVolumeRatio)CreateIndicator();
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.3m, Volume = 100, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.2m, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.AreEqual(0.5m, indicator.Current.Value);

            indicator.Reset();
            indicator.Remove(Symbols.GOOG);

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.9m, Volume = 100, Time = reference.AddMinutes(3) });
            // new low (ignored)
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 0.2m, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.AreEqual(200m, indicator.Current.Value);
        }

        [Test]
        public override void IgnorePeriodIfAnyStockMissed()
        {
            var indicator = (NewHighsNewLowsVolumeRatio)CreateIndicator();
            indicator.Add(Symbols.MSFT);
            var reference = System.DateTime.Today;

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });

            // value is not ready yet
            Assert.AreEqual(0m, indicator.Current.Value);

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.5m, Volume = 100, Time = reference.AddMinutes(3) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, Close = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.AreEqual(0m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 4, Low = 1, Volume = 100, Time = reference.AddMinutes(4) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.3m, Volume = 100, Time = reference.AddMinutes(4) });
            // no change
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(4) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 4, Low = 1, Volume = 100, Time = reference.AddMinutes(4) });

            Assert.AreEqual(2m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 5, Low = 1, Volume = 100, Time = reference.AddMinutes(5) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 2, Low = 0.2m, Volume = 100, Time = reference.AddMinutes(5) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 4, Low = 1, Volume = 100, Time = reference.AddMinutes(5) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 5, Low = 1, Volume = 100, Time = reference.AddMinutes(5) });

            Assert.AreEqual(3m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 6, Low = 1, Volume = 100, Time = reference.AddMinutes(6) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(6) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 6, Low = 1, Volume = 100, Time = reference.AddMinutes(6) });

            Assert.AreEqual(3m, indicator.Current.Value);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 7, Low = 1, Volume = 100, Time = reference.AddMinutes(7) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 1, Volume = 100, Time = reference.AddMinutes(7) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.MSFT, High = 7, Low = 1, Volume = 100, Time = reference.AddMinutes(7) });

            Assert.AreEqual(3m, indicator.Current.Value);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            // setup period (unordered)
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.5m, Low = 0.2m, Volume = 100, Time = reference.AddMinutes(2) });

            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });
            // new low
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.75m, Low = 0.1m, Volume = 100, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 2, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(2m, indicator.Current.Value);
            Assert.AreEqual(9, indicator.Samples);
        }

        [Test]
        public override void WarmsUpOrdered()
        {
            var indicator = CreateIndicator();
            var reference = System.DateTime.Today;

            // setup period (ordered)
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 1, Low = 1, Volume = 100, Time = reference.AddMinutes(1) });

            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 2, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 0.5m, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(2) });

            // indicator is not ready yet
            Assert.IsFalse(indicator.IsReady);

            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, High = 3, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.IBM, High = 4, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });
            // new high
            indicator.Update(new TradeBar() { Symbol = Symbols.GOOG, High = 5, Low = 1, Volume = 100, Time = reference.AddMinutes(3) });

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(300m, indicator.Current.Value);
        }

        protected override string TestFileName => "nhnl_data.csv";

        protected override string TestColumnName => "NH/NL Volume Ratio";

        /// <summary>
        /// The final value of this indicator is zero because it uses the Volume of the bars it receives.
        /// Since RenkoBar's don't always have Volume, the final current value is zero. Therefore we
        /// skip this test
        /// </summary>
        /// <param name="indicator"></param>
        protected override void IndicatorValueIsNotZeroAfterReceiveRenkoBars(IndicatorBase indicator)
        {
        }
    }
}
