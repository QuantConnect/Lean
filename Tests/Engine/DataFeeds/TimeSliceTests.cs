using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class TimeSliceTests
    {
        [Test]
        public void HandlesTicks_ExpectInOrderWithNoDuplicates()
        {
            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(Tick), 
                Symbols.EURUSD, 
                Resolution.Tick, 
                TimeZones.Utc, 
                TimeZones.Utc, 
                true, 
                true, 
                false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), 
                subscriptionDataConfig, 
                new Cash(CashBook.AccountCurrency, 0, 1m), 
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            DateTime refTime = DateTime.UtcNow;

            Tick[] rawTicks = Enumerable
                .Range(0, 10)
                .Select(i => new Tick(refTime.AddSeconds(i), Symbols.EURUSD, 1.3465m, 1.34652m))
                .ToArray();

            IEnumerable<TimeSlice> timeSlices = rawTicks.Select(t => TimeSlice.Create(
                t.Time,
                DateTimeZone.Utc,
                new CashBook(),
                new List<DataFeedPacket> {new DataFeedPacket(security, new List<BaseData>() {t})},
                new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>())));

            Tick[] timeSliceTicks = timeSlices.SelectMany(ts => ts.Slice.Ticks.Values.SelectMany(x => x)).ToArray();

            Assert.AreEqual(rawTicks.Length, timeSliceTicks.Length);
            for (int i = 0; i < rawTicks.Length; i++)
            {
                Assert.IsTrue(Compare(rawTicks[i], timeSliceTicks[i]));
            }
        }

        private bool Compare(Tick expected, Tick actual)
        {
            return expected.Time == actual.Time
                   && expected.BidPrice == actual.BidPrice
                   && expected.AskPrice == actual.AskPrice
                   && expected.Quantity == actual.Quantity;
        }
    }
}
