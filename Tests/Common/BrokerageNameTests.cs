using NUnit.Framework;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class BrokerageNameTests
    {
        [Test]
        public void BrokerageNameEnumsAreNumberedCorrectly()
        {
            Assert.AreEqual((int) BrokerageName.Default, 0);
            Assert.AreEqual((int) BrokerageName.QuantConnectBrokerage, 0);
            Assert.AreEqual((int) BrokerageName.InteractiveBrokersBrokerage, 1);
            Assert.AreEqual((int) BrokerageName.TradierBrokerage, 2);
            Assert.AreEqual((int) BrokerageName.OandaBrokerage, 3);
            Assert.AreEqual((int) BrokerageName.FxcmBrokerage, 4);
            Assert.AreEqual((int) BrokerageName.Bitfinex, 5);
            Assert.AreEqual((int) BrokerageName.GDAX, 12);
        }
    }
}
