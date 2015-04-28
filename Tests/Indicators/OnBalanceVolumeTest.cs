using System;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class OnBalanceVolumeTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var onBalanceVolumeIndicator = new OnBalanceVolume("OBV");

            onBalanceVolumeIndicator.OnBalanceVoulumeValues.Update(
                new DateTime(2013, 4, 30), Decimal.Parse("1.156486E+08", System.Globalization.NumberStyles.Float));

            TestHelper.TestIndicator(onBalanceVolumeIndicator, "spy_with_obv.txt", "OBV",
                (ind, expected) => Assert.AreEqual(
                    expected.ToString("0.##E-00"),
                    (onBalanceVolumeIndicator.OnBalanceVoulumeValues.Current.Value).ToString("0.##E-00")
                    )

                );
          
        }

        [Test]
        public void ResetsProperly()
        {
            var onBalanceVolumeIndicator = new OnBalanceVolume("OBV");
            foreach (var data in TestHelper.GetTradeBarStream("spy_with_obv.txt", false))
            {
                onBalanceVolumeIndicator.Update(data);
            }

            Assert.IsTrue(onBalanceVolumeIndicator.IsReady);

            onBalanceVolumeIndicator.Reset();
        }
    }
}
