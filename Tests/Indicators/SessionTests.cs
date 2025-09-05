using System;
using System.Reflection;
using Common.Data.Consolidators;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;


namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class SessionTests
    {
        [Test]
        public void AddMethodPreservesPreviousValuesInSessionWindow()
        {
            var session = new Session(TickType.Trade);

            var symbol = Symbols.SPY;
            var date = new DateTime(2025, 8, 25);

            var bar1 = new TradeBar(date.AddHours(12), symbol, 100, 101, 99, 100, 1000, TimeSpan.FromHours(1));
            session.Update(bar1);
            var bar2 = new TradeBar(date.AddHours(13), symbol, 101, 102, 100, 101, 1100, TimeSpan.FromHours(1));
            session.Update(bar2);

            // Verify current session values after multiple updates
            Assert.AreEqual(100, session[0].Open);
            Assert.AreEqual(102, session[0].High);
            Assert.AreEqual(99, session[0].Low);
            Assert.AreEqual(101, session[0].Close);
            Assert.AreEqual(2100, session[0].Volume);

            // Start of a new trading day
            date = date.AddDays(1);
            bar1 = new TradeBar(date.AddHours(12), symbol, 200, 201, 199, 200, 2000, TimeSpan.FromHours(1));
            session.Update(bar1);
            bar2 = new TradeBar(date.AddHours(13), symbol, 300, 301, 299, 300, 3100, TimeSpan.FromHours(1));
            session.Update(bar2);

            // Verify current session reflects new day data
            Assert.AreEqual(200, session[0].Open);
            Assert.AreEqual(301, session[0].High);
            Assert.AreEqual(199, session[0].Low);
            Assert.AreEqual(300, session[0].Close);
            Assert.AreEqual(5100, session[0].Volume);

            // Verify previous session values are preserved
            Assert.AreEqual(100, session[1].Open);
            Assert.AreEqual(102, session[1].High);
            Assert.AreEqual(99, session[1].Low);
            Assert.AreEqual(101, session[1].Close);
            Assert.AreEqual(2100, session[1].Volume);
        }

        [TestCase(TickType.Trade, typeof(TradeBar), false)]
        [TestCase(TickType.Trade, typeof(Tick), true)]
        [TestCase(TickType.Quote, typeof(QuoteBar), false)]
        [TestCase(TickType.Quote, typeof(Tick), true)]
        public void SessionUsesCorrectInputTypeBasedOnTickTypeAndFirstDataPoint(TickType tickType, Type expectedInputType, bool useTick)
        {
            var session = new Session(tickType);
            BaseData data = null;
            if (useTick)
            {
                data = new Tick { TickType = tickType };
            }
            else
            {
                if (tickType == TickType.Trade)
                {
                    data = new TradeBar();
                }
                else if (tickType == TickType.Quote)
                {
                    data = new QuoteBar();
                }
            }

            // First update initializes the consolidator
            session.Update(data);

            // Access the private consolidator via reflection
            var consolidatorField = typeof(Session).GetField("_consolidator", BindingFlags.NonPublic | BindingFlags.Instance);
            var consolidator = (SessionConsolidator)consolidatorField.GetValue(session);

            // Ensure consolidator will be fed with the correct data type
            Assert.AreEqual(expectedInputType, consolidator.InputType);
        }

        [Test]
        public void SessionDoesNotChangeConsolidatorInputTypeAfterInitialization()
        {
            var session = new Session(TickType.Trade);
            var tradeBar = new TradeBar();
            var expectedInputType = typeof(TradeBar);

            // First update sets consolidator input type to TradeBar
            session.Update(tradeBar);

            // Then update with Tick (should NOT change to Tick mode)
            var tick = new Tick { TickType = TickType.Trade };
            session.Update(tick);

            // Access the private consolidator via reflection
            var consolidatorField = typeof(Session).GetField("_consolidator", BindingFlags.NonPublic | BindingFlags.Instance);
            var consolidator = (SessionConsolidator)consolidatorField.GetValue(session);

            // Ensure consolidator will be fed with the correct data type
            Assert.AreEqual(expectedInputType, consolidator.InputType);
        }
    }
}