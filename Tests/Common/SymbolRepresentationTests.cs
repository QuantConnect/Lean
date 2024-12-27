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
using Python.Runtime;
using System.Globalization;

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

        [TestCase("SPXW  230111C02400000", SecurityType.IndexOption, OptionStyle.European, "SPXW", "SPX", "SPX", 2400.00, "2023-01-11")]
        [TestCase("SPY   230111C02400000", SecurityType.Option, OptionStyle.American, "SPY", "SPY", "SPY", 2400.00, "2023-01-11")]
        [TestCase("GOOG  160318C00320000", SecurityType.Option, OptionStyle.American, "GOOCV", "GOOCV", "GOOG", 320.00, "2016-03-18")]
        [TestCase("AAPL240614C00100000", SecurityType.Option, OptionStyle.American, "AAPL", "AAPL", "AAPL", 100.00, "2024-06-14")]
        [TestCase("MSFT240614C00150000", SecurityType.Option, OptionStyle.American, "MSFT", "MSFT", "MSFT", 150.00, "2024-06-14")]
        [TestCase("AMZN  220630C01000000", SecurityType.Option, OptionStyle.American, "AMZN", "AMZN", "AMZN", 1000.00, "2022-06-30")]
        [TestCase("NFLX  230122P00250000", SecurityType.Option, OptionStyle.American, "NFLX", "NFLX", "NFLX", 250.00, "2023-01-22")]
        [TestCase("TSLA  240815C00775000", SecurityType.Option, OptionStyle.American, "TSLA", "TSLA", "TSLA", 775.00, "2024-08-15")]
        [TestCase("V     231211P00220000", SecurityType.Option, OptionStyle.American, "V", "V", "V", 220.00, "2023-12-11")]
        [TestCase("JPM   240501C00130750", SecurityType.Option, OptionStyle.American, "JPM", "JPM", "JPM", 130.75, "2024-05-01")]
        [TestCase("IBM   250212P00145000", SecurityType.Option, OptionStyle.American, "IBM", "IBM", "IBM", 145.00, "2025-02-12")]
        [TestCase("DIS   230630C00075000", SecurityType.Option, OptionStyle.American, "DIS", "DIS", "DIS", 75.00, "2023-06-30")]
        [TestCase("ORCL  231030C00065000", SecurityType.Option, OptionStyle.American, "ORCL", "ORCL", "ORCL", 65.00, "2023-10-30")]
        [TestCase("CSCO  230501P00045000", SecurityType.Option, OptionStyle.American, "CSCO", "CSCO", "CSCO", 45.00, "2023-05-01")]
        [TestCase("DAX   250715C01000000", SecurityType.IndexOption, OptionStyle.European, "DAX", "DAX", "DAX", 1000.00, "2025-07-15")]
        [TestCase("FTSE  230122C00750000", SecurityType.IndexOption, OptionStyle.European, "FTSE", "FTSE", "FTSE", 750.00, "2023-01-22")]
        [TestCase("DC01H12  120401C00015500", SecurityType.FutureOption, OptionStyle.American, "DC01H12", "DC", "DC01H12", 15.5, "2012-04-01")]
        [TestCase("ES20H20  200320P03290000", SecurityType.FutureOption, OptionStyle.American, "ES20H20", "ES", "ES20H20", 3290.00, "2020-03-20")]
        public void ParseOptionTickerOSI(string optionStr, SecurityType securityType, OptionStyle optionStyle,
            string expectedTargetOptionTicker, string expectedUnderlyingTicker, string expectedUnderlyingMappedTicker,
            decimal expectedStrikePrice, string expectedDate)
        {
            var result = SymbolRepresentation.ParseOptionTickerOSI(optionStr, securityType, optionStyle, Market.USA);

            Assert.AreEqual(expectedTargetOptionTicker, result.ID.Symbol);
            Assert.AreEqual(optionStr, result.Value);
            Assert.AreEqual(expectedUnderlyingTicker, result.Underlying.ID.Symbol);
            Assert.AreEqual(expectedUnderlyingMappedTicker, result.Underlying.Value);
            Assert.AreEqual(securityType, result.ID.SecurityType);
            Assert.AreEqual(optionStyle, result.ID.OptionStyle);
            Assert.AreEqual(expectedStrikePrice, result.ID.StrikePrice);
            Assert.AreEqual(DateTime.ParseExact(expectedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture), result.ID.Date);
        }

        [Test]
        public void OptionSymbolAliasAddsPaddingSpaceForSixOrMoreCharacterSymbols()
        {
            const string expected = @"ABCDEF 060318C00047500";
            var symbol = SymbolRepresentation.GenerateOptionTickerOSI("ABCDEF", OptionRight.Call, 47.50m, new DateTime(2006, 03, 18));
            Assert.AreEqual(expected, symbol);
        }

        [TestCase("SPXW", OptionRight.Call, 2400.00, "230111", "SPXW230111C02400000")]
        [TestCase("SPY", OptionRight.Put, 250.00, "230615", "SPY230615P00250000")]
        [TestCase("AAPL", OptionRight.Call, 100.00, "240614", "AAPL240614C00100000")]
        [TestCase("MSFT", OptionRight.Put, 150.00, "240614", "MSFT240614P00150000")]
        [TestCase("GOOG", OptionRight.Call, 2000.00, "211231", "GOOG211231C02000000")]
        [TestCase("AMZN", OptionRight.Put, 3500.50, "250101", "AMZN250101P03500500")]
        [TestCase("NFLX", OptionRight.Call, 500.25, "221201", "NFLX221201C00500250")]
        [TestCase("TSLA", OptionRight.Put, 725.00, "241231", "TSLA241231P00725000")]
        [TestCase("V", OptionRight.Call, 220.00, "230420", "V230420C00220000")]
        [TestCase("JPM", OptionRight.Put, 130.75, "230710", "JPM230710P00130750")]
        [TestCase("IBM", OptionRight.Call, 145.00, "250212", "IBM250212C00145000")]
        [TestCase("BABA", OptionRight.Put, 88.88, "240508", "BABA240508P00088880")]
        public void GenerateOptionTickerOSICompact_ValidInputs(string underlying, OptionRight right, decimal strikePrice, string date, string expected)
        {
            DateTime expiration = DateTime.ParseExact(date, "yyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            string result = SymbolRepresentation.GenerateOptionTickerOSICompact(underlying, right, strikePrice, expiration);
            Assert.AreEqual(expected, result);
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
        public void GenerateOptionTickerWithIndexOptionReturnsCorrectTicker()
        {
            // Expected ticker for the option contract
            var expected = "SPXW2104A3800";

            var underlying = Symbols.SPX;

            // Create the option contract (IndexOption) with specific parameters
            var option = Symbol.CreateOption(
                underlying,
                "SPXW",
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                3800m,
                new DateTime(2021, 1, 04));

            var result = SymbolRepresentation.GenerateOptionTicker(option);

            // Assert that the result matches the expected ticker
            Assert.AreEqual(expected, result);
        }

        [TestCase(Futures.Energy.ArgusLLSvsWTIArgusTradeMonth, 2017, 1, 29, "AE529G7", false)] // Previous month
        [TestCase(Futures.Energy.ArgusPropaneSaudiAramco, 2017, 1, 29, "A9N29G7", false)] // Previous month
        [TestCase(Futures.Energy.BrentCrude, 2017, 1, 29, "B29H7", false)] // Second prior month
        [TestCase(Futures.Energy.BrentLastDayFinancial, 2017, 1, 29, "BZ29H7", false)] // Second prior month
        [TestCase(Futures.Energy.CrudeOilWTI, 2017, 11, 20, "CL20Z17", true)] // Prior month
        [TestCase(Futures.Energy.Gasoline, 2017, 11, 20, "RB20Z17", true)] // Prior month
        [TestCase(Futures.Energy.HeatingOil, 2017, 11, 20, "HO20Z17", true)] // Prior month
        [TestCase(Futures.Energy.MarsArgusVsWTITradeMonth, 2017, 11, 20, "AYV20Z17", true)] // Prior month
        [TestCase(Futures.Energy.NaturalGas, 2017, 11, 20, "NG20Z17", true)] // Prior month
        [TestCase(Futures.Energy.NaturalGasHenryHubLastDayFinancial, 2017, 11, 20, "HH20Z17", true)] // Prior month
        [TestCase(Futures.Energy.NaturalGasHenryHubPenultimateFinancial, 2017, 11, 20, "HP20Z17", true)] // Prior month
        [TestCase(Futures.Energy.WTIHoustonArgusVsWTITradeMonth, 2017, 11, 20, "HTT20Z17", true)] // Prior month
        [TestCase(Futures.Energy.WTIHoustonCrudeOil, 2017, 11, 20, "HCL20Z17", true)] // Prior month
        [TestCase(Futures.Softs.Sugar11, 2017, 11, 20, "SB20Z17", true)] // Prior month
        [TestCase(Futures.Softs.Sugar11CME, 2017, 11, 20, "YO20Z17", true)] // Prior month
        public void GenerateFutureTickerExpiringInPreviousMonth(string underlying, int year, int month, int day, string ticker, bool doubleDigitsYear)
        {
            // CL Dec17 expires in Nov17
            var result = SymbolRepresentation.GenerateFutureTicker(underlying, new DateTime(year, month, day), doubleDigitsYear);

            Assert.AreEqual(ticker, result);
        }

        [TestCase(Futures.Energy.ArgusLLSvsWTIArgusTradeMonth, 2016, 12, 29, "AE529F7", false, true)] // Previous month
        [TestCase(Futures.Energy.ArgusPropaneSaudiAramco, 2016, 12, 29, "A9N29F7", false, true)] // Previous month
        [TestCase(Futures.Energy.BrentCrude, 2016, 11, 29, "B29F7", false, true)] // Second prior month
        [TestCase(Futures.Energy.BrentCrude, 2016, 12, 29, "B29G7", false, true)] // Second prior month
        [TestCase(Futures.Energy.BrentLastDayFinancial, 2016, 11, 29, "BZ29F7", false, true)] // Second prior month
        [TestCase(Futures.Energy.BrentLastDayFinancial, 2016, 12, 29, "BZ29G7", false, true)] // Second prior month
        [TestCase(Futures.Energy.CrudeOilWTI, 2016, 12, 20, "CL20F17", true, true)] // Prior month
        [TestCase(Futures.Energy.Gasoline, 2016, 12, 20, "RB20F17", true, true)] // Prior month
        [TestCase(Futures.Energy.HeatingOil, 2016, 12, 20, "HO20F17", true, true)] // Prior month
        [TestCase(Futures.Energy.MarsArgusVsWTITradeMonth, 2016, 12, 20, "AYV20F17", true, true)] // Prior month
        [TestCase(Futures.Energy.NaturalGas, 2016, 12, 20, "NG20F17", true, true)] // Prior month
        [TestCase(Futures.Energy.NaturalGasHenryHubLastDayFinancial, 2016, 12, 20, "HH20F17", true, true)] // Prior month
        [TestCase(Futures.Energy.NaturalGasHenryHubPenultimateFinancial, 2016, 12, 20, "HP20F17", true, true)] // Prior month
        [TestCase(Futures.Energy.WTIHoustonArgusVsWTITradeMonth, 2016, 12, 20, "HTT20F17", true, true)] // Prior month
        [TestCase(Futures.Energy.WTIHoustonCrudeOil, 2016, 12, 20, "HCL20F17", true, true)] // Prior month
        [TestCase(Futures.Softs.Sugar11, 2016, 12, 20, "SB20F17", true, true)] // Prior month
        [TestCase(Futures.Softs.Sugar11CME, 2016, 12, 20, "YO20F17", true, true)] // Prior month
        [TestCase(Futures.Softs.Sugar11CME, 2016, 12, 20, "YOF17", true, false)] // Prior month
        [TestCase(Futures.Softs.Sugar11CME, 2016, 12, 20, "YOF7", false, false)] // Prior month
        [TestCase(Futures.Indices.SP500EMini, 2010, 3, 1, "ESH0", false, false)]
        public void GenerateFutureTickerExpiringInPreviousMonthOverYearBoundary(string underlying, int year, int month, int day, string ticker, bool doubleDigitsYear, bool includeExpirationDate)
        {
            // CL Dec17 expires in Nov17
            var result = SymbolRepresentation.GenerateFutureTicker(underlying, new DateTime(year, month, day), doubleDigitsYear, includeExpirationDate);

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

        [TestCase("CLU0", 2008, "2010-08-20")]
        [TestCase("CLU1", 2008, "2011-08-22")]
        [TestCase("CLU2", 2008, "2012-08-21")]
        [TestCase("CLU8", 2008, "2008-08-20")]
        [TestCase("CLU9", 2008, "2009-08-20")]
        public void GenerateFutureSymbolFromTickerKnownYearSingleDigit(string ticker, int futureYear, DateTime expectedExpiration)
        {
            var result = SymbolRepresentation.ParseFutureSymbol(ticker, futureYear);
            Assert.AreEqual(expectedExpiration, result.ID.Date.Date);
        }

        [TestCase("CLU20", 2020, "2020-08-20")]
        [TestCase("CLU21", 2020, "2021-08-20")]
        [TestCase("CLU22", 2020, "2022-08-22")]
        [TestCase("CLU28", 2020, "2028-08-22")]
        [TestCase("CLU29", 2020, "2029-08-21")]
        public void GenerateFutureSymbolFromTickerUnknownYearSingleDigit(string ticker, int futureYear, DateTime expectedExpiration)
        {
            var result = SymbolRepresentation.ParseFutureSymbol(ticker, futureYear);
            Assert.AreEqual(expectedExpiration, result.ID.Date.Date);
        }

        [TestCase("CLU20", "2020-08-20")]
        [TestCase("CLU21", "2021-08-20")]
        [TestCase("CLU22", "2022-08-22")]
        [TestCase("CLU28", "2028-08-22")]
        [TestCase("CLU29", "2029-08-21")]
        public void GenerateFutureSymbolFromTickerUnknownYearSingleDigit(string ticker, DateTime expectedExpiration)
        {
            var result = SymbolRepresentation.ParseFutureSymbol(ticker);
            Assert.AreEqual(expectedExpiration, result.ID.Date.Date);
        }

        [TestCase("CLU0", "2030-08-20")]
        [TestCase("CLU1", "2031-08-20")]
        [TestCase("CLU2", "2032-08-20")]
        [TestCase("CLU8", "2028-08-22")]
        [TestCase("CLU9", "2029-08-21")]
        public void GenerateFutureSymbolFromTickerUnknownYearDoubleDigit(string ticker, DateTime expectedExpiration)
        {
            var result = SymbolRepresentation.ParseFutureSymbol(ticker);
            Assert.AreEqual(expectedExpiration, result.ID.Date.Date);
        }

        [TestCase("NQZ23")]
        public void GenerateFutureSymbolFromTickerUnknownYear(string ticker)
        {
            var result = SymbolRepresentation.ParseFutureSymbol(ticker);
            // When the future year is not provided, we have an ambiguous case (1923 or 2023) and default 2000
            Assert.AreEqual(new DateTime(2023, 12, 15), result.ID.Date.Date);
        }

        [TestCase("NQZ99")]
        public void GenerateFutureSymbolFromTickerExpiringBefore2000(string ticker)
        {
            var result = SymbolRepresentation.ParseFutureSymbol(ticker, 1999);
            Assert.AreEqual(new DateTime(1999, 12, 17), result.ID.Date.Date);
        }

        [TestCase("PROPANE_NON_LDH_MONT_BELVIEU", QuantConnect.Securities.Futures.Energy.PropaneNonLDHMontBelvieu)]
        [TestCase("ARGUS_PROPANE_FAR_EAST_INDEX_BALMO", QuantConnect.Securities.Futures.Energy.ArgusPropaneFarEastIndexBALMO)]
        [TestCase("GASOLINE", QuantConnect.Securities.Futures.Energy.Gasoline)]
        [TestCase("NATURAL_GAS", QuantConnect.Securities.Futures.Energy.NaturalGas)]
        public void FutureEnergySymbolsWorkInPythonWithPEP8(string FutureEnergyName, string expectedFutureEnergyValue)
        {
            using (Py.GIL())
            {
                var pythonModule = PyModule.FromString("testModule", @$"
from AlgorithmImports import *

def return_futures_energy():
    return Futures.Energy.{FutureEnergyName};
");
                dynamic pythonFunction = pythonModule.GetAttr("return_futures_energy");
                var futureEnergyValue = pythonFunction();
                Assert.AreEqual(expectedFutureEnergyValue, (futureEnergyValue as PyObject).GetAndDispose<string>());
            }
        }

    }
}
