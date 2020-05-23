using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class SubscriptionDataTests
    {
        [Test]
        public void CreatedSubscriptionRoundsTimeDownForDataWithPeriod()
        {
            var tb = new TradeBar
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Period = TimeSpan.FromHours(1),
                Symbol = Symbols.SPY
            };

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.Utc);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.Utc, new DateTime(2020, 5, 21), new DateTime(2020, 5, 22));

            var subscription = SubscriptionData.Create(config, exchangeHours, offsetProvider, tb);

            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 0, 0), subscription.Data.Time);
            Assert.AreEqual(new DateTime(2020, 5, 21, 9, 0, 0), subscription.Data.EndTime);
        }

        [Test]
        public void CreatedSubscriptionDoesNotRoundDownForPeriodLessData()
        {
            var data = new MyCustomData
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Symbol = Symbols.SPY
            };

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.Utc);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.Utc, new DateTime(2020, 5, 21), new DateTime(2020, 5, 22));

            var subscription = SubscriptionData.Create(config, exchangeHours, offsetProvider, data);

            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 9, 0), subscription.Data.Time);
            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 9, 0), subscription.Data.EndTime);
        }

        internal class MyCustomData : BaseData
        {
        }
    }
}
