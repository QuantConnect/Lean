using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using NUnit.Framework;
using Moq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Brokerages.Bitfinex;


namespace QuantConnect.Securities.Tests
{
    [TestFixture()]
    public class BitfinexSecurityMarginModelTests
    {


        BitfinexSecurityMarginModel unit = new BitfinexSecurityMarginModel();

        [Test()]
        public void GenerateMarginCallOrderLongTest()
        {
            var security = BitfinexTestsHelpers.GetSecurity();
            security.Holdings.SetHoldings(400, 100);
            decimal price = 299m;
            security.Holdings.UpdateMarketPrice(price);
            security.SetMarketPrice(new Tick { Value = price });
            security.SetLocalTimeKeeper(new LocalTimeKeeper(DateTime.UtcNow, NodaTime.DateTimeZone.Utc));
            var actual = unit.GenerateMarginCallOrder(security, 1000, 1101);
            Assert.AreEqual(-1, actual.Quantity);
        }

        [Test()]
        public void GenerateMarginCallOrderShortTest()
        {
            var security = BitfinexTestsHelpers.GetSecurity();
            security.Holdings.SetHoldings(400, -100);
            decimal price = 631m;
            security.Holdings.UpdateMarketPrice(price);
            security.SetMarketPrice(new Tick { Value = price });
            security.SetLocalTimeKeeper(new LocalTimeKeeper(DateTime.UtcNow, NodaTime.DateTimeZone.Utc));
            var actual = unit.GenerateMarginCallOrder(security, 1000, 1101);
            Assert.AreEqual(1, actual.Quantity);
        }



    }
}
