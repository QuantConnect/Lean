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
 *
*/

using System;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class DelistingTests
    {
        [Test]
        public void AlwaysOpenExchange()
        {
            var time = new DateTime(2020, 1, 14);
            var delisting = new Delisting(Symbols.BTCUSD, time, 10, DelistingType.Warning);

            var liquidationTime = delisting.GetLiquidationTime(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc));

            Assert.AreEqual(new DateTime(2020, 1, 14, 23, 45, 0), liquidationTime);
        }

        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Option)]
        public void EquityAndOption(SecurityType securityType)
        {
            var symbol = Symbols.AAPL;
            if (securityType == SecurityType.Option)
            {
                symbol = Symbols.SPY_C_192_Feb19_2016;
            }
            var time = new DateTime(2020, 1, 14);
            var delisting = new Delisting(symbol, time, 10, DelistingType.Warning);

            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(delisting.Symbol.ID.Market, delisting.Symbol, delisting.Symbol.SecurityType);
            var liquidationTime = delisting.GetLiquidationTime(exchangeHours);

            Assert.AreEqual(new DateTime(2020, 1, 14, 15, 45, 0), liquidationTime);
        }

        [TestCase(SecurityType.FutureOption)]
        [TestCase(SecurityType.Future)]
        public void FuturesAndOption(SecurityType securityType)
        {
            var time = new DateTime(2020, 1, 14);
            var symbol = Symbols.Future_CLF19_Jan2019;
            if (securityType == SecurityType.FutureOption)
            {
                symbol = Symbol.CreateOption(symbol, symbol.ID.Market, OptionStyle.American, OptionRight.Call, 1, time);
            }

            var delisting = new Delisting(symbol, time, 10, DelistingType.Warning);

            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(delisting.Symbol.ID.Market, delisting.Symbol, delisting.Symbol.SecurityType);
            var liquidationTime = delisting.GetLiquidationTime(exchangeHours);

            Assert.AreEqual(new DateTime(2020, 1, 14, 16, 45, 0), liquidationTime);
        }

        [Test]
        public void ThrowsIfNotDelistingWarning()
        {
            var delisting = new Delisting(Symbols.AAPL, DateTime.UtcNow, 19, DelistingType.Delisted);
            Assert.Throws<ArgumentException>(() => delisting.GetLiquidationTime(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc)));
        }
    }
}