using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Linq;
using QuantConnect.Data.Consolidators;
using QuantConnect.Util;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class TdSequentialTests : CommonIndicatorTests<TradeBar>
    {
        protected override string TestFileName => "td_sequential_test_data.csv";
        protected override string TestColumnName => "TDS";
        
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new TdSequential("ABC");
        }
        
        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var ci = CreateIndicator();

            Assert.IsFalse(ci.IsReady);
            Enumerable.Range(1, 5).DoForEach(t => ci.Update(new TradeBar()));
            Assert.IsTrue(ci.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var ci = CreateIndicator();
            Enumerable.Range(1, 5).DoForEach(t => ci.Update(new TradeBar()));
            Assert.IsTrue(ci.IsReady);
            ci.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(ci);
        }

        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var renkoConsolidator = new RenkoConsolidator(RenkoBarSize);
            var renkoBarCount = 0;
            renkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                renkoBarCount++;
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            foreach (var parts in TestHelper.GetCsvFileStream(TestFileName))
            {
                var tradebar = parts.GetTradeBar();
                renkoConsolidator.Update(tradebar);
            }
          
            Assert.IsTrue(renkoBarCount >= 1, "At least one Renko bars were emitted.");
            renkoConsolidator.Dispose();
        }

        [Test]
        public override void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var volumeRenkoConsolidator = new VolumeRenkoConsolidator(VolumeRenkoBarSize);
            var renkoBarCount = 0;
            
            volumeRenkoConsolidator.DataConsolidated += (sender, volumeRenkoBar) =>
            {
                renkoBarCount++;
                Assert.DoesNotThrow(() => indicator.Update(volumeRenkoBar));
            };

            foreach (var parts in TestHelper.GetCsvFileStream(TestFileName))
            {
                var tradebar = parts.GetTradeBar();
                volumeRenkoConsolidator.Update(tradebar);
            }
            
            Assert.IsTrue(renkoBarCount >= 1, "Atleast one renko bar were emitted.");
            volumeRenkoConsolidator.Dispose();
        }

        [TestCase(TdSequentialPhase.BuySetup, 1.09)]
        [TestCase(TdSequentialPhase.SellSetup, 2.09)]
        [TestCase(TdSequentialPhase.BuyCountdown, 3.13)]
        [TestCase(TdSequentialPhase.SellCountdown, 4.13)]
        [TestCase(TdSequentialPhase.BuySetupPerfect, 5.09)]
        [TestCase(TdSequentialPhase.SellSetupPerfect, 6.09)]
        public void GivenTradeBarsThenValidateExpectedResult(TdSequentialPhase phase, decimal expectedResult)
        {
            var indicator = CreateIndicator();
            var (prices, time) = SetupData(phase);

            CallIndicatorWithData(prices, time, indicator);

            Assert.AreEqual(true, indicator.IsReady);
            Assert.AreEqual(expectedResult, indicator.Current.Value);
        }

        private struct OCHL
        {
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
        }
        private static void CallIndicatorWithData(OCHL[] prices, DateTime time, IndicatorBase<TradeBar> indicator)
        {
            foreach (var price in prices)
            {
                var bar = new TradeBar(time, "ABC", price.Open, price.High, price.Low, price.Close, 1000);
                indicator.Update(bar);
                time = time.AddMinutes(1);
            }
        }
        
        private static (OCHL[], DateTime) SetupData(TdSequentialPhase phase)
        {
            OCHL[] prices = [];
            var time = new DateTime(2023, 1, 1, 9, 30, 0);
            prices = phase switch
            {
                TdSequentialPhase.BuySetup =>
                [
                    // Bar 1 to 9 - Close < Close 4 bars ago
                    // bar8.Low <= bar6.Low && bar8.Low <= bar7.Low || bar9.Low <= bar6.Low && bar9.Low <= bar7.Low;
                    new OCHL { Open = 110, High = 111, Low = 109, Close = 115 },
                    new OCHL { Open = 110, High = 111, Low = 109, Close = 114 },
                    new OCHL { Open = 110, High = 111, Low = 109, Close = 113 },
                    new OCHL { Open = 110, High = 111, Low = 109, Close = 111 },
                    new OCHL { Open = 110, High = 111, Low = 109, Close = 110 },
                    new OCHL { Open = 108, High = 109, Low = 107, Close = 106 },
                    new OCHL { Open = 106, High = 107, Low = 105, Close = 104 },
                    new OCHL { Open = 104, High = 105, Low = 103, Close = 102 },
                    new OCHL { Open = 102, High = 103, Low = 101, Close = 100 },
                    new OCHL { Open = 100, High = 101, Low = 91.5m, Close = 98 },
                    new OCHL { Open = 98, High = 99, Low = 95.5m, Close = 96 },
                    new OCHL { Open = 96, High = 97, Low = 96, Close = 94 },
                    new OCHL { Open = 94, High = 105, Low = 104, Close = 92 }
                ],
                TdSequentialPhase.SellSetup =>
                [
                    new OCHL { Open = 90, High = 91, Low = 89, Close = 85 },
                    new OCHL { Open = 90, High = 91, Low = 89, Close = 87 },
                    new OCHL { Open = 90, High = 91, Low = 89, Close = 88 },
                    new OCHL { Open = 90, High = 91, Low = 89, Close = 89 },
                    new OCHL { Open = 90, High = 91, Low = 89, Close = 90 }, // 1
                    new OCHL { Open = 91, High = 92, Low = 90, Close = 91 }, // 2
                    new OCHL { Open = 92, High = 93, Low = 91, Close = 92 }, // 3
                    new OCHL { Open = 93, High = 94, Low = 92, Close = 93 }, // 4
                    new OCHL { Open = 94, High = 95, Low = 93, Close = 94 }, // 5 (Close > Bar1.Close)
                    new OCHL { Open = 95, High = 96, Low = 94, Close = 95 }, // 6 (Close > Bar2.Close)
                    new OCHL { Open = 96, High = 97, Low = 95, Close = 96 }, // 7 (Close > Bar3.Close)
                    new OCHL { Open = 97, High = 94, Low = 95.5m, Close = 97 },
                    new OCHL { Open = 98, High = 91.8m, Low = 90.8m, Close = 98 }
                ],
                TdSequentialPhase.SellSetupPerfect => Enumerable.Range(100, 13)
                    .Select(x => new OCHL { Open = x, High = x, Low = x, Close = x }).ToArray(),
                TdSequentialPhase.BuySetupPerfect => Enumerable.Range(1, 13).Select(y =>
                {
                    var x = (decimal)(100 - y);
                    return new OCHL { Open = x, High = x, Low = x, Close = x };
                }).ToArray(),
                TdSequentialPhase.BuyCountdown => Enumerable.Range(1, 26).Select(y =>
                {
                    var x = (decimal)(100 - y);
                    return new OCHL { Open = x, High = x, Low = x, Close = x };
                }).ToArray(),
                TdSequentialPhase.SellCountdown => Enumerable.Range(100, 26)
                    .Select(x => new OCHL { Open = x, High = x, Low = x, Close = x }).ToArray(),
                _ => prices
            };

            return (prices, time);
        }
    }
}
