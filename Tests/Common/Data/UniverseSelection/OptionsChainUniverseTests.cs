using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class OptionsChainUniverseTests
    {
        private OptionChainUniverse _optionsChainUniverse;
        private Option _canonicalSecurity;

        [TestFixtureSetUp]
        public void Setup()
        {
            var algo = new QCAlgorithm();
            var settings = new UniverseSettings(Resolution.Second, 1, true, false, TimeSpan.Zero);
            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            var subscriptionManager = new SubscriptionManager(timeKeeper);
            var securityInitializer = SecurityInitializer.Null;
            _canonicalSecurity = algo.AddOption("SPY");
            _optionsChainUniverse = new OptionChainUniverse(_canonicalSecurity, settings, subscriptionManager, securityInitializer);
        }

        [Test]
        public void ConcreteOptionSubscriptionRequests_AreCreatedWithCorrectTickTypeAndType_Correctly()
        {
            var subscriptions = _optionsChainUniverse.GetSubscriptionRequests(_canonicalSecurity, DateTime.MaxValue, DateTime.MaxValue).ToList();

            Assert.AreEqual(subscriptions.Count, 3);
            Assert.IsTrue(subscriptions.Any(x => x.Configuration.TickType == TickType.Quote && x.Configuration.Type == typeof(QuoteBar)));
            Assert.IsTrue(subscriptions.Any(x => x.Configuration.TickType == TickType.Trade && x.Configuration.Type == typeof(TradeBar)));
            Assert.IsTrue(subscriptions.Any(x => x.Configuration.TickType == TickType.OpenInterest && x.Configuration.Type == typeof(OpenInterest)));
        }
    }
}
