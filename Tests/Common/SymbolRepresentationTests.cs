using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;

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
    }
}
