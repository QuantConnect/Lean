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
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class SymbolRepresentationTests
    {
        [Test]
        public void OptionSymbolAliasMatchesOSI()
        {
            const string expected = @"MSFT  060318C00047500";
            var result = SymbolRepresentation.GenerateOptionTickerOSI("MSFT", OptionRight.Call, 47.50m, new DateTime(2006, 03, 18));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void OptionSymbolAliasAddsPaddingSpaceForSixOrMoreCharacterSymbols()
        {
            const string expected = @"ABCDEF 060318C00047500";
            var symbol = SymbolRepresentation.GenerateOptionTickerOSI("ABCDEF", OptionRight.Call, 47.50m, new DateTime(2006, 03, 18));
            Assert.AreEqual(expected, symbol);
        }

        [Test]
        public void ParseOptionIQFeedTicker()
        {
            // ticker contains two digits year of expiration
            var result = SymbolRepresentation.ParseOptionTickerIQFeed("MSFT1615D30");

            Assert.AreEqual(result.Underlying, "MSFT");
            Assert.AreEqual(result.OptionRight, OptionRight.Call);
            Assert.AreEqual(result.OptionStrike, 30m);
            Assert.AreEqual(result.ExpirationDate, new DateTime(2016, 4, 15));
        }

        [Test]
        public void ParseFuturesTickers()
        {
            // ticker contains two digits year of expiration, no day expiration
            var result = SymbolRepresentation.ParseFutureTicker("EX20");
            Assert.AreEqual(result.Underlying, "E");
            Assert.AreEqual(result.ExpirationDay, 1);
            Assert.AreEqual(result.ExpirationYearShort, 20);
            Assert.AreEqual(result.ExpirationMonth, 11); // November

            // ticker contains one digit year of expiration, no day expiration
            result = SymbolRepresentation.ParseFutureTicker("ABCZ1");
            Assert.AreEqual(result.Underlying, "ABC");
            Assert.AreEqual(result.ExpirationDay, 1);
            Assert.AreEqual(result.ExpirationYearShort, 1);
            Assert.AreEqual(result.ExpirationMonth, 12); // December

            // ticker contains two digits year of expiration, with day expiration
            result = SymbolRepresentation.ParseFutureTicker("ED01X20");
            Assert.AreEqual(result.Underlying, "ED");
            Assert.AreEqual(result.ExpirationDay, 1);
            Assert.AreEqual(result.ExpirationYearShort, 20);
            Assert.AreEqual(result.ExpirationMonth, 11); // November

            // ticker contains one digit year of expiration, with day expiration
            result = SymbolRepresentation.ParseFutureTicker("ABC11Z1");
            Assert.AreEqual(result.Underlying, "ABC");
            Assert.AreEqual(result.ExpirationDay, 11);
            Assert.AreEqual(result.ExpirationYearShort, 1);
            Assert.AreEqual(result.ExpirationMonth, 12); // December
        }

        [Test]
        public void GenerateFuturesTickers()
        {
            const string ticker = @"ED";
            var result = SymbolRepresentation.GenerateFutureTicker(ticker, new DateTime(2016, 12, 12));

            // ticker contains two digits year of expiration
            Assert.AreEqual(result, "ED12Z16");

            // ticker contains one digit year of expiration
            result = SymbolRepresentation.GenerateFutureTicker(ticker, new DateTime(2016, 12, 12), false);
            Assert.AreEqual(result, "ED12Z6");
        }

        [Test]
        public void GenerateFuturesTickersBackAndForth()
        {
            const string expected = @"ED01Z16";
            var result = SymbolRepresentation.ParseFutureTicker(expected);
            var ticker = SymbolRepresentation.GenerateFutureTicker(result.Underlying, new DateTime(2000 + result.ExpirationYearShort, result.ExpirationMonth, result.ExpirationDay));

            Assert.AreEqual(expected, ticker);
        }

        [Test]
        public void ParseInvalidFuturesTickers()
        {
            var result = SymbolRepresentation.ParseFutureTicker("invalid");
            Assert.AreEqual(result, null);
        }

        [Test]
        public void GenerateFutureTickerExpiringInPreviousMonth()
        {
            // CL Dec17 expires in Nov17
            var result = SymbolRepresentation.GenerateFutureTicker("CL", new DateTime(2017, 11, 20));

            Assert.AreEqual("CL20Z17", result);
        }
    }
}
