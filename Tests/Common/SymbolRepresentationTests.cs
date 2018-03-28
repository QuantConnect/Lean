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
            // ticker contains two digits year of expiration
            var result = SymbolRepresentation.ParseFutureTicker("EDX20");
            Assert.AreEqual(result.Underlying, "ED");
            Assert.AreEqual(result.ExpirationYearShort, 20);
            Assert.AreEqual(result.ExpirationMonth, 11); // November

            // ticker contains one digit year of expiration
            result = SymbolRepresentation.ParseFutureTicker("ABCZ1");
            Assert.AreEqual(result.Underlying, "ABC");
            Assert.AreEqual(result.ExpirationYearShort, 1);
            Assert.AreEqual(result.ExpirationMonth, 12); // December
        }

        [Test]
        public void GenerateFuturesTickers()
        {
            const string ticker = @"ED";
            var result = SymbolRepresentation.GenerateFutureTicker(ticker, new DateTime(2016, 12, 12));

            // ticker contains two digits year of expiration
            Assert.AreEqual(result, "EDZ16");

            // ticker contains one digit year of expiration
            result = SymbolRepresentation.GenerateFutureTicker(ticker, new DateTime(2016, 12, 12), false);
            Assert.AreEqual(result, "EDZ6");
        }

        [Test]
        public void GenerateFuturesTickersBackAndForth()
        {
            const string expected = @"EDZ16";
            var result = SymbolRepresentation.ParseFutureTicker(expected);
            var ticker = SymbolRepresentation.GenerateFutureTicker(result.Underlying, new DateTime(2000 + result.ExpirationYearShort, result.ExpirationMonth, 1));

            Assert.AreEqual(ticker, expected);
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

            Assert.AreEqual("CLZ17", result);
        }
    }
}
