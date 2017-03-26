using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataProviders
{
    [TestFixture]
    public class DefaultDataProviderTests
    {
        private DefaultDataProvider _defaultDataProvider;

        [TestFixtureSetUp]
        public void Setup()
        {
            _defaultDataProvider = new DefaultDataProvider();
        }

        [Test]
        public void DefaultDataProvider_CanReadDataThatExists()
        {
            var stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/aapl/20140606_trade.zip");
            
            Assert.IsNotNull(stream);
        }

        [Test]
        public void DefaultDataProvider_CannotReadDataThatDoesNotExist()
        {
            var stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/aapl/19980606_trade.zip");

            Assert.IsNull(stream);
        }
    }
}
