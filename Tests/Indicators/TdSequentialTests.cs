using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class TdSequentialTests
    {
        [Test]
        public void GivenSellSetupThenSetupCountAtNine()
        {
            var indicator = new TdSequential("TDS");
            var prices = new decimal[]
            {
                100, 101, 102, 103, 104,
                105, 106, 107, 108, 109,
                110, 111, 112
            };

            var time = new DateTime(2023, 1, 1, 9, 30, 0);

            foreach (var price in prices)
            {
                var bar = new TradeBar(time, "MYY", price, price, price, price, 1000);
                indicator.Update(bar);
                time = time.AddMinutes(1);
            }

            Assert.IsTrue(indicator.Current.Value == (decimal)6.09);
            Assert.IsTrue(indicator.IsReady);
        }
    }
}
