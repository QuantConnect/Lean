using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.ToolBox.IQFeed;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Engine.DataFeeds
{

    /// <summary>
    ///  Test fixture is explicit, because tests are dependent on network and are long
    /// </summary>
    [TestFixture, Ignore("Tests are dependent on network and are long")]
    public class IQFeedRealtimeDataFeedTests
    {

        [Test]
        public void IQFeedSanityCheckIfDataIsLoaded ()
        {
            var symbolUniverse = new IQFeedDataQueueUniverseProvider();

            var lookup = symbolUniverse as IDataQueueUniverseProvider;
            var mapper = symbolUniverse as ISymbolMapper;

            Assert.IsTrue(lookup.LookupSymbols("SPY", SecurityType.Option).Any());
            Assert.IsTrue(lookup.LookupSymbols("SPY", SecurityType.Equity).Count() == 1);
            Assert.IsTrue(!string.IsNullOrEmpty(mapper.GetBrokerageSymbol(Symbols.SPY)));
            Assert.IsTrue(mapper.GetLeanSymbol("SPY", SecurityType.Equity, "") != Symbol.Empty);
        }
    }
}
