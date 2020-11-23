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
using QuantConnect.Securities;

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

        [TestCase(Futures.Energies.ArgusLLSvsWTIArgusTradeMonth, 2017, 1, 29, "AE529G7", false)] // Previous month
        [TestCase(Futures.Energies.ArgusPropaneSaudiAramco, 2017, 1, 29, "A9N29G7", false)] // Previous month
        [TestCase(Futures.Energies.BrentCrude, 2017, 1, 29, "B29H7", false)] // Second prior month
        [TestCase(Futures.Energies.BrentLastDayFinancial, 2017, 1, 29, "BZ29H7", false)] // Second prior month
        [TestCase(Futures.Energies.CrudeOilWTI, 2017, 11, 20, "CL20Z17", true)] // Prior month
        [TestCase(Futures.Energies.Gasoline, 2017, 11, 20, "RB20Z17", true)] // Prior month
        [TestCase(Futures.Energies.HeatingOil, 2017, 11, 20, "HO20Z17", true)] // Prior month
        [TestCase(Futures.Energies.MarsArgusVsWTITradeMonth, 2017, 11, 20, "AYV20Z17", true)] // Prior month
        [TestCase(Futures.Energies.NaturalGas, 2017, 11, 20, "NG20Z17", true)] // Prior month
        [TestCase(Futures.Energies.NaturalGasHenryHubLastDayFinancial, 2017, 11, 20, "HH20Z17", true)] // Prior month
        [TestCase(Futures.Energies.NaturalGasHenryHubPenultimateFinancial, 2017, 11, 20, "HP20Z17", true)] // Prior month
        [TestCase(Futures.Energies.WTIHoustonArgusVsWTITradeMonth, 2017, 11, 20, "HTT20Z17", true)] // Prior month
        [TestCase(Futures.Energies.WTIHoustonCrudeOil, 2017, 11, 20, "HCL20Z17", true)] // Prior month
        [TestCase(Futures.Softs.Sugar11, 2017, 11, 20, "SB20Z17", true)] // Prior month
        [TestCase(Futures.Softs.Sugar11CME, 2017, 11, 20, "YO20Z17", true)] // Prior month
        public void GenerateFutureTickerExpiringInPreviousMonth(string underlying, int year, int month, int day, string ticker, bool doubleDigitsYear)
        {
            // CL Dec17 expires in Nov17
            var result = SymbolRepresentation.GenerateFutureTicker(underlying, new DateTime(year, month, day), doubleDigitsYear);

            Assert.AreEqual(ticker, result);
        }

        [TestCase(Futures.Energies.ArgusLLSvsWTIArgusTradeMonth, 2016, 12, 29, "AE529F7", false)] // Previous month
        [TestCase(Futures.Energies.ArgusPropaneSaudiAramco, 2016, 12, 29, "A9N29F7", false)] // Previous month
        [TestCase(Futures.Energies.BrentCrude, 2016, 11, 29, "B29F7", false)] // Second prior month
        [TestCase(Futures.Energies.BrentCrude, 2016, 12, 29, "B29G7", false)] // Second prior month
        [TestCase(Futures.Energies.BrentLastDayFinancial, 2016, 11, 29, "BZ29F7", false)] // Second prior month
        [TestCase(Futures.Energies.BrentLastDayFinancial, 2016, 12, 29, "BZ29G7", false)] // Second prior month
        [TestCase(Futures.Energies.CrudeOilWTI, 2016, 12, 20, "CL20F17", true)] // Prior month
        [TestCase(Futures.Energies.Gasoline, 2016, 12, 20, "RB20F17", true)] // Prior month
        [TestCase(Futures.Energies.HeatingOil, 2016, 12, 20, "HO20F17", true)] // Prior month
        [TestCase(Futures.Energies.MarsArgusVsWTITradeMonth, 2016, 12, 20, "AYV20F17", true)] // Prior month
        [TestCase(Futures.Energies.NaturalGas, 2016, 12, 20, "NG20F17", true)] // Prior month
        [TestCase(Futures.Energies.NaturalGasHenryHubLastDayFinancial, 2016, 12, 20, "HH20F17", true)] // Prior month
        [TestCase(Futures.Energies.NaturalGasHenryHubPenultimateFinancial, 2016, 12, 20, "HP20F17", true)] // Prior month
        [TestCase(Futures.Energies.WTIHoustonArgusVsWTITradeMonth, 2016, 12, 20, "HTT20F17", true)] // Prior month
        [TestCase(Futures.Energies.WTIHoustonCrudeOil, 2016, 12, 20, "HCL20F17", true)] // Prior month
        [TestCase(Futures.Softs.Sugar11, 2016, 12, 20, "SB20F17", true)] // Prior month
        [TestCase(Futures.Softs.Sugar11CME, 2016, 12, 20, "YO20F17", true)] // Prior month
        public void GenerateFutureTickerExpiringInPreviousMonthOverYearBoundary(string underlying, int year, int month, int day, string ticker, bool doubleDigitsYear)
        {
            // CL Dec17 expires in Nov17
            var result = SymbolRepresentation.GenerateFutureTicker(underlying, new DateTime(year, month, day), doubleDigitsYear);

            Assert.AreEqual(ticker, result);
        }

        [TestCase("ABC", 2017, 12, 20, "ABC20Z17", true)] // Generic contract (i.e. expires current month
        public void GenerateFutureTickerExpiringInCurrentMonth(string underlying, int year, int month, int day, string ticker, bool doubleDigitsYear)
        {
            // CL Dec17 expires in Nov17
            var result = SymbolRepresentation.GenerateFutureTicker(underlying, new DateTime(year, month, day), doubleDigitsYear);

            Assert.AreEqual(ticker, result);
        }

        [TestCase("DC", 2023, 1, 4, "DC04Z22", true)] // Contract month is 2022-12, expires on 2023-01-04. Same situation with the rest of the test cases.
        [TestCase("DY", 2022, 10, 4, "DY04U22", true)]
        [TestCase("GDK", 2022, 11, 1, "GDK01V22", true)]
        public void GenerateFutureTickerExpiringInNextMonth(string ticker, int year, int month, int day, string expectedValue, bool doubleDigitsYear)
        {
            var result = SymbolRepresentation.GenerateFutureTicker(ticker, new DateTime(year, month, day), doubleDigitsYear);

            Assert.AreEqual(expectedValue, result);
        }
    }
}
