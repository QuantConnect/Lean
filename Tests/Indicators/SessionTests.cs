/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class SessionTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AddMethodPreservesPreviousValuesInSessionWindow(int initialSize)
        {
            var symbol = Symbols.SPY;
            var session = GetSession(TickType.Trade, initialSize: initialSize);
            session.Size = 2;

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
        private Session GetSession(TickType tickType, int initialSize)
        {
            var symbol = Symbols.SPY;
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var exchangeHours = marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            return new Session(tickType, exchangeHours, symbol, initialSize);
        }
    }
}
