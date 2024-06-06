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

        [TestCase("SPXW  230111C02400000", SecurityType.IndexOption, OptionStyle.European, "SPXW", "SPX")]
        [TestCase("SPY   230111C02400000", SecurityType.Option, OptionStyle.American, "SPY", "SPY")]
        public void ParseOptionTickerOSI(string optionStr, SecurityType securityType, OptionStyle optionStyle,
            string expectedTargetOptionTicker, string expectedUnderlyingTicker)
        {
            var result = SymbolRepresentation.ParseOptionTickerOSI(optionStr, securityType, optionStyle, Market.USA);

            Assert.AreEqual(expectedTargetOptionTicker, result.ID.Symbol);
            Assert.AreEqual(expectedUnderlyingTicker, result.Underlying.ID.Symbol);
            Assert.AreEqual(securityType, result.ID.SecurityType);
            Assert.AreEqual(optionStyle, result.ID.OptionStyle);
        }

        [Test]
        public void OptionSymbolAliasAddsPaddingSpaceForSixOrMoreCharacterSymbols()
        {
            const string expected = @"ABCDEF 060318C00047500";
            var symbol = SymbolRepresentation.GenerateOptionTickerOSI("ABCDEF", OptionRight.Call, 47.50m, new DateTime(2006, 03, 18));
            Assert.AreEqual(expected, symbol);
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
        [TestCase("NATURAL_GAS",QuantConnect.Securities.Futures.Energy.NaturalGas)]
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
