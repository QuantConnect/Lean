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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class SymbolTests
    {
        [Theory]
        [TestCaseSource(nameof(GetSymbolCreateTestCaseData))]
        public void SymbolCreate(
            string ticker,
            SecurityType securityType,
            string market,
            Symbol expected
        )
        {
            Assert.AreEqual(Symbol.Create(ticker, securityType, market), expected);
        }

        private static TestCaseData[] GetSymbolCreateTestCaseData()
        {
            return new[]
            {
                new TestCaseData(
                    "SPY",
                    SecurityType.Equity,
                    Market.USA,
                    new Symbol(SecurityIdentifier.GenerateEquity("SPY", Market.USA), "SPY")
                ),
                new TestCaseData(
                    "EURUSD",
                    SecurityType.Forex,
                    Market.FXCM,
                    new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD")
                ),
                new TestCaseData(
                    "SPY",
                    SecurityType.Option,
                    Market.USA,
                    new Symbol(
                        SecurityIdentifier.GenerateOption(
                            SecurityIdentifier.DefaultDate,
                            Symbols.SPY.ID,
                            Market.USA,
                            0,
                            default(OptionRight),
                            default(OptionStyle)
                        ),
                        "?SPY"
                    )
                )
            };
        }

        [Test]
        public void SymbolCreateBaseWithUnderlyingEquity()
        {
            var type = typeof(BaseData);
            var equitySymbol = Symbol.Create("TWX", SecurityType.Equity, Market.USA);
            var symbol = Symbol.CreateBase(type, equitySymbol, Market.USA);
            var symbolIDSymbol = symbol
                .ID.Symbol.Split(new[] { ".BaseData" }, StringSplitOptions.None)
                .First();

            Assert.IsTrue(symbol.SecurityType == SecurityType.Base);
            Assert.IsTrue(symbol.HasUnderlying);

            Assert.AreEqual(symbol.Underlying, equitySymbol);

            Assert.AreEqual(symbol.ID.Date, new DateTime(1998, 1, 2));
            Assert.AreEqual("AOL", symbolIDSymbol);

            Assert.AreEqual(symbol.Underlying.ID.Symbol, symbolIDSymbol);
            Assert.AreEqual(symbol.Underlying.ID.Date, symbol.ID.Date);

            Assert.AreEqual(symbol.Underlying.Value, equitySymbol.Value);
            Assert.AreEqual(symbol.Underlying.Value, symbol.Value);
        }

        [Test]
        public void SymbolCreateBaseWithUnderlyingOption()
        {
            var type = typeof(BaseData);
            var optionSymbol = Symbol.CreateOption(
                "TWX",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                100,
                new DateTime(2050, 12, 31)
            );
            var symbol = Symbol.CreateBase(type, optionSymbol, Market.USA);
            var symbolIDSymbol = symbol
                .ID.Symbol.Split(new[] { ".BaseData" }, StringSplitOptions.None)
                .First();

            Assert.IsTrue(symbol.SecurityType == SecurityType.Base);
            Assert.IsTrue(symbol.HasUnderlying);

            Assert.AreEqual(symbol.Underlying, optionSymbol);

            Assert.IsTrue(symbol.Underlying.HasUnderlying);
            Assert.AreEqual(symbol.Underlying.Underlying.SecurityType, SecurityType.Equity);

            Assert.AreEqual(new DateTime(2050, 12, 31), symbol.ID.Date);
            Assert.AreEqual("AOL", symbolIDSymbol);

            Assert.AreEqual(symbol.Underlying.ID.Symbol, symbolIDSymbol);
            Assert.AreEqual(symbol.Underlying.ID.Date, symbol.ID.Date);
            Assert.AreEqual(symbol.Underlying.Value, symbol.Value);

            Assert.AreEqual(symbol.Underlying.Underlying.ID.Symbol, symbolIDSymbol);
            Assert.AreNotEqual(symbol.Underlying.Underlying.ID.Date, symbol.ID.Date);
            Assert.IsTrue(symbol.Value.StartsWith(symbol.Underlying.Underlying.Value));

            Assert.AreEqual(symbol.Underlying.Underlying.ID.Symbol, symbol.Underlying.ID.Symbol);
            Assert.AreNotEqual(symbol.Underlying.Underlying.ID.Date, symbol.Underlying.ID.Date);
            Assert.IsTrue(symbol.Underlying.Value.StartsWith(symbol.Underlying.Underlying.Value));
        }

        [Test]
        public void SymbolCreateWithOptionSecurityTypeCreatesCanonicalOptionSymbol()
        {
            var symbol = Symbol.Create("SPY", SecurityType.Option, Market.USA);
            var sid = symbol.ID;
            Assert.AreEqual(SecurityIdentifier.DefaultDate, sid.Date);
            Assert.AreEqual(0m, sid.StrikePrice);
            Assert.AreEqual(default(OptionRight), sid.OptionRight);
            Assert.AreEqual(default(OptionStyle), sid.OptionStyle);
        }

        [Test]
        public void CanonicalOptionSymbolAliasHasQuestionMark()
        {
            var symbol = Symbol.Create("SPY", SecurityType.Option, Market.USA);
            Assert.AreEqual("?SPY", symbol.Value);
        }

        [Test]
        public void UsesSidForDictionaryKey()
        {
            var sid = SecurityIdentifier.GenerateEquity("SPY", Market.USA);
            var dictionary = new Dictionary<Symbol, int> { { new Symbol(sid, "value"), 1 } };

            var key = new Symbol(sid, "other value");
            Assert.IsTrue(dictionary.ContainsKey(key));
        }

        [Test]
        public void CreatesOptionWithUnderlying()
        {
            var option = Symbol.CreateOption(
                "XLRE",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                21m,
                new DateTime(2016, 08, 19)
            );

            Assert.AreEqual(option.ID.Date, new DateTime(2016, 08, 19));
            Assert.AreEqual(option.ID.StrikePrice, 21m);
            Assert.AreEqual(option.ID.OptionRight, OptionRight.Call);
            Assert.AreEqual(option.ID.OptionStyle, OptionStyle.American);
            Assert.AreEqual(option.Underlying.ID.Symbol, "XLRE");
        }

        [Test]
        public void CompareToItselfReturnsZero()
        {
            var sym = new Symbol(SecurityIdentifier.GenerateForex("sym", Market.FXCM), "sym");
            Assert.AreEqual(0, sym.CompareTo(sym));
        }

        [Test]
        public void ComparesTheSameAsStringCompare()
        {
            var a = new Symbol(SecurityIdentifier.GenerateForex("a", Market.FXCM), "a");
            var z = new Symbol(SecurityIdentifier.GenerateForex("z", Market.FXCM), "z");

            Assert.AreEqual(string.Compare("a", "z", StringComparison.Ordinal), a.CompareTo(z));
            Assert.AreEqual(string.Compare("z", "a", StringComparison.Ordinal), z.CompareTo(a));
        }

        [Test]
        public void ComparesTheSameAsStringCompareAndIgnoresCase()
        {
            var a = new Symbol(SecurityIdentifier.GenerateForex("a", Market.FXCM), "a");
            var z = new Symbol(SecurityIdentifier.GenerateForex("z", Market.FXCM), "z");

            Assert.AreEqual(
                string.Compare("a", "Z", StringComparison.OrdinalIgnoreCase),
                a.CompareTo(z)
            );
            Assert.AreEqual(
                string.Compare("z", "A", StringComparison.OrdinalIgnoreCase),
                z.CompareTo(a)
            );
        }

        [Test]
        public void ComparesAgainstStringWithoutException()
        {
            var a = new Symbol(SecurityIdentifier.GenerateForex("a", Market.FXCM), "a");
            Assert.AreEqual(0, a.CompareTo("a"));
        }

        [Test]
        public void ComparesAgainstStringIgnoringCase()
        {
            var a = new Symbol(SecurityIdentifier.GenerateForex("a", Market.FXCM), "a");
            Assert.AreEqual(0, a.CompareTo("A"));
        }

        [Test]
        public void EqualsAgainstNullOrEmpty()
        {
            var validSymbol = Symbols.SPY;
            var emptySymbol = Symbol.Empty;
            var emptySymbolInstance = new Symbol(SecurityIdentifier.Empty, string.Empty);
            Symbol nullSymbol = null;

            Assert.IsTrue(emptySymbol.Equals(nullSymbol));
            Assert.IsTrue(Symbol.Empty.Equals(nullSymbol));
            Assert.IsTrue(emptySymbolInstance.Equals(nullSymbol));

            Assert.IsTrue(emptySymbol.Equals(emptySymbol));
            Assert.IsTrue(Symbol.Empty.Equals(emptySymbol));
            Assert.IsTrue(emptySymbolInstance.Equals(emptySymbol));

            Assert.IsFalse(validSymbol.Equals(nullSymbol));
            Assert.IsFalse(validSymbol.Equals(emptySymbol));
            Assert.IsFalse(validSymbol.Equals(emptySymbolInstance));
            Assert.IsFalse(Symbol.Empty.Equals(validSymbol));
        }

        [Test]
        public void ComparesAgainstNullOrEmpty()
        {
            var validSymbol = Symbols.SPY;
            var emptySymbol = Symbol.Empty;
            Symbol nullSymbol = null;

            Assert.IsTrue(nullSymbol == emptySymbol);
            Assert.IsFalse(nullSymbol != emptySymbol);

            Assert.IsTrue(emptySymbol == nullSymbol);
            Assert.IsFalse(emptySymbol != nullSymbol);

            Assert.IsTrue(validSymbol != null);
            Assert.IsTrue(emptySymbol == null);
            Assert.IsTrue(nullSymbol == null);

            Assert.IsFalse(validSymbol == null);
            Assert.IsFalse(emptySymbol != null);
            Assert.IsFalse(nullSymbol != null);

            Assert.IsTrue(validSymbol != Symbol.Empty);
            Assert.IsTrue(emptySymbol == Symbol.Empty);
            Assert.IsTrue(nullSymbol == Symbol.Empty);

            Assert.IsFalse(validSymbol == Symbol.Empty);
            Assert.IsFalse(emptySymbol != Symbol.Empty);
            Assert.IsFalse(nullSymbol != Symbol.Empty);

            Assert.IsTrue(null != validSymbol);
            Assert.IsTrue(null == emptySymbol);
            Assert.IsTrue(null == nullSymbol);

            Assert.IsFalse(null == validSymbol);
            Assert.IsFalse(null != emptySymbol);
            Assert.IsFalse(null != nullSymbol);

            Assert.IsTrue(Symbol.Empty != validSymbol);
            Assert.IsTrue(Symbol.Empty == emptySymbol);
            Assert.IsTrue(Symbol.Empty == nullSymbol);

            Assert.IsFalse(Symbol.Empty == validSymbol);
            Assert.IsFalse(Symbol.Empty != emptySymbol);
            Assert.IsFalse(Symbol.Empty != nullSymbol);
        }

        [Test]
        public void ImplicitOperatorsAreInverseFunctions()
        {
#pragma warning disable 0618 // This test requires implicit operators
            var eurusd = new Symbol(
                SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM),
                "EURUSD"
            );
            string stringEurusd = eurusd;
            Symbol symbolEurusd = stringEurusd;
            Assert.AreEqual(eurusd, symbolEurusd);
#pragma warning restore 0618
        }

        [Test]
        public void ImplicitOperatorsReturnSIDOnFailure()
        {
#pragma warning disable 0618 // This test requires implicit operators
            // this doesn't exist in the symbol cache
            var eurusd = new Symbol(
                SecurityIdentifier.GenerateForex("NOT-A-SECURITY", Market.FXCM),
                "EURUSD"
            );
            string stringEurusd = eurusd;
            Assert.AreEqual(eurusd.ID.ToString(), stringEurusd);

            Assert.Throws<ArgumentException>(() =>
            {
                Symbol symbol = "this will not resolve to a proper Symbol instance";
            });

            Symbol notASymbol = "NotASymbol";
            Assert.AreNotEqual(Symbol.Empty, notASymbol);
            Assert.IsTrue(notASymbol.ToString().Contains("NotASymbol"));
#pragma warning restore 0618
        }

        [Test]
        public void ImplicitFromStringChecksSymbolCache()
        {
#pragma warning disable 0618 // This test requires implicit operators
            SymbolCache.Set("EURUSD", Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM));
            string ticker = "EURUSD";
            Symbol actual = ticker;
            var expected = SymbolCache.GetSymbol(ticker);
            Assert.AreEqual(expected, actual);
            SymbolCache.Clear();
#pragma warning restore 0618
        }

        [Test]
        public void ImplicitFromStringParsesSid()
        {
#pragma warning disable 0618 // This test requires implicit operators
            SymbolCache.Set("EURUSD", Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM));
            var expected = SymbolCache.GetSymbol("EURUSD");
            string sid = expected.ID.ToString();
            Symbol actual = sid;
            Assert.AreEqual(expected, actual);
            SymbolCache.Clear();
#pragma warning restore 0618
        }

        [Test]
        public void ImplicitFromWithinStringLiftsSecondArgument()
        {
#pragma warning disable 0618 // This test requires implicit operators
            SymbolCache.Clear();
            SymbolCache.Set("EURUSD", Symbols.EURUSD);
            var expected = SymbolCache.GetSymbol("EURUSD");
            string stringValue = expected;
            string notFound = "EURGBP 8G";
            var expectedNotFoundSymbol = Symbols.EURGBP;
            string sid = expected.ID.ToString();
            Symbol actual = sid;
            if (!(expected == stringValue))
            {
                Assert.Fail("Failed expected == string");
            }
            else if (!(stringValue == expected))
            {
                Assert.Fail("Failed string == expected");
            }
            else if (expected != stringValue)
            {
                Assert.Fail("Failed expected != string");
            }
            else if (stringValue != expected)
            {
                Assert.Fail("Failed string != expected");
            }

            Symbol notFoundSymbol = notFound;
            Assert.AreEqual(expectedNotFoundSymbol, notFoundSymbol);
            SymbolCache.Clear();
#pragma warning restore 0618
        }

        [Test]
        public void TestIfWeDetectCorrectlyWeekliesAndStandardOptionsBeforeFeb2015()
        {
            var symbol = Symbol.CreateOption(
                "SPY",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                200,
                new DateTime(2012, 09, 22)
            );
            var weeklySymbol = Symbol.CreateOption(
                "SPY",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                200,
                new DateTime(2012, 09, 07)
            );

            Assert.True(OptionSymbol.IsStandard(symbol));
            Assert.False(OptionSymbol.IsStandard(weeklySymbol));

            Assert.AreEqual(
                new DateTime(
                    2012,
                    09,
                    21
                ) /*Friday*/
                ,
                OptionSymbol.GetLastDayOfTrading(symbol)
            );
            Assert.AreEqual(
                new DateTime(
                    2012,
                    09,
                    07
                ) /*Friday*/
                ,
                OptionSymbol.GetLastDayOfTrading(weeklySymbol)
            );
        }

        [Test]
        public void TestIfWeDetectCorrectlyWeekliesAndStandardOptionsAfterFeb2015()
        {
            var symbol = Symbol.CreateOption(
                "SPY",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                200,
                new DateTime(2016, 02, 19)
            );
            var weeklySymbol = Symbol.CreateOption(
                "SPY",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                200,
                new DateTime(2016, 02, 05)
            );

            Assert.True(OptionSymbol.IsStandard(symbol));
            Assert.False(OptionSymbol.IsStandard(weeklySymbol));

            Assert.AreEqual(
                new DateTime(
                    2016,
                    02,
                    19
                ) /*Friday*/
                ,
                OptionSymbol.GetLastDayOfTrading(symbol)
            );
            Assert.AreEqual(
                new DateTime(
                    2016,
                    02,
                    05
                ) /*Friday*/
                ,
                OptionSymbol.GetLastDayOfTrading(weeklySymbol)
            );
        }

        [Test]
        public void TestIfWeDetectCorrectlyWeeklies()
        {
            var weeklySymbol = Symbol.CreateOption(
                "SPY",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                200,
                new DateTime(2020, 04, 10)
            );
            var monthlysymbol = Symbol.CreateOption(
                "SPY",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                200,
                new DateTime(2020, 04, 17)
            );

            Assert.True(OptionSymbol.IsWeekly(weeklySymbol));
            Assert.False(OptionSymbol.IsWeekly(monthlysymbol));

            Assert.AreEqual(
                new DateTime(
                    2020,
                    04,
                    17
                ) /*Friday*/
                ,
                OptionSymbol.GetLastDayOfTrading(monthlysymbol)
            );
            //Good Friday on 10th so should be 9th
            Assert.AreEqual(
                new DateTime(
                    2020,
                    04,
                    09
                ) /*Thursday*/
                ,
                OptionSymbol.GetLastDayOfTrading(weeklySymbol)
            );
        }

        [Test]
        public void HasUnderlyingSymbolReturnsTrueWhenSpecifyingCorrectUnderlying()
        {
            Assert.IsTrue(Symbols.SPY_C_192_Feb19_2016.HasUnderlyingSymbol(Symbols.SPY));
        }

        [Test]
        public void HasUnderlyingSymbolReturnsFalsWhenSpecifyingIncorrectUnderlying()
        {
            Assert.IsFalse(Symbols.SPY_C_192_Feb19_2016.HasUnderlyingSymbol(Symbols.AAPL));
        }

        [Test]
        public void TestIfFridayLastTradingDayIsHolidaysThenMoveToPreviousThursday()
        {
            var saturdayAfterGoodFriday = new DateTime(2014, 04, 19);
            var thursdayBeforeGoodFriday = saturdayAfterGoodFriday.AddDays(-2);
            var symbol = Symbol.CreateOption(
                "SPY",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                200,
                saturdayAfterGoodFriday
            );
            Assert.AreEqual(thursdayBeforeGoodFriday, OptionSymbol.GetLastDayOfTrading(symbol));
        }

        [TestCase("ES", "ES")]
        [TestCase("GC", "OG")]
        [TestCase("ZT", "OZT")]
        public void FutureOptionsWithDifferentUnderlyingGlobexTickersAreMapped(
            string futureTicker,
            string expectedFutureOptionTicker
        )
        {
            var future = Symbol.CreateFuture(futureTicker, Market.CME, DateTime.UtcNow.Date);
            var canonicalFutureOption = Symbol.CreateOption(
                future,
                Market.CME,
                default(OptionStyle),
                default(OptionRight),
                default(decimal),
                SecurityIdentifier.DefaultDate
            );

            var nonCanonicalFutureOption = Symbol.CreateOption(
                future,
                Market.CME,
                default(OptionStyle),
                default(OptionRight),
                default(decimal),
                new DateTime(2020, 12, 18)
            );

            Assert.AreEqual(canonicalFutureOption.Underlying.ID.Symbol, futureTicker);
            Assert.AreEqual(canonicalFutureOption.ID.Symbol, expectedFutureOptionTicker);
            Assert.IsTrue(canonicalFutureOption.Value.StartsWith("?" + expectedFutureOptionTicker));

            Assert.AreEqual(nonCanonicalFutureOption.Underlying.ID.Symbol, futureTicker);
            Assert.AreEqual(nonCanonicalFutureOption.ID.Symbol, expectedFutureOptionTicker);
            Assert.IsTrue(nonCanonicalFutureOption.Value.StartsWith(expectedFutureOptionTicker));
        }

        [Test]
        public void SymbolWithSidContainingUnderlyingCreatedWithoutNullUnderlying()
        {
            var future = Symbol.CreateFuture("ES", Market.CME, new DateTime(2020, 6, 19));
            var optionSid = SecurityIdentifier.GenerateOption(
                future.ID.Date,
                future.ID,
                future.ID.Market,
                3500m,
                OptionRight.Call,
                OptionStyle.American
            );

            var option = new Symbol(optionSid, "ES");
            Assert.IsNotNull(option.Underlying);
            Assert.AreEqual(future, option.Underlying);
        }

        [TestCase("CL XKJAZ588SI4H", "CL", "CL21F21")] // Future
        [TestCase("CL JL", "CL", "/CL")] // Canonical Future
        [TestCase("ES 1S4 | ES XLDTU1KH5XC1", "CL", "?ES21F21")] // Future Option Canonical
        [TestCase("ES XKGCMV4QK9VO | ES XLDTU1KH5XC1", "ES", "ES21F21  201218C00000000")] // Future Option
        [TestCase("SPY 2U | SPY R735QTJ8XC9X", "SPY", "?SPY")] // Option Canonical
        [TestCase("GOOCV 305RBQ2BZBZT2 | GOOCV VP83T1ZUHROL", "GOOCV", "GOOCV 151224P00750000")] // Option
        [TestCase("SPY R735QTJ8XC9X", "SPY", "SPY")] // Equity
        [TestCase("EURGBP 8G", "EURGBP", "EURGBP")] // Forex
        [TestCase("BTCUSD XJ", "BTCUSD", "BTCUSD")] // Crypto
        public void SymbolAlias(string identifier, string ticker, string expectedValue)
        {
            var symbol = new Symbol(SecurityIdentifier.Parse(identifier), ticker);
            Assert.AreEqual(expectedValue, symbol.Value);
        }

        [TestCase("CL XKJAZ588SI4H", "CL", "CL21F21")] // Future
        [TestCase("CL JL", "CL", "/CL")] // Canonical Future
        [TestCase("ES 1S4 | ES XLDTU1KH5XC1", "ES", "?ES21F21")] // Future Option Canonical
        [TestCase("ES XKGCMV4QK9VO | ES XLDTU1KH5XC1", "ES", "ES21F21  201218C00000000")] // Future Option
        [TestCase("SPY 2U | SPY R735QTJ8XC9X", "SPY", "?SPY")] // Option Canonical
        [TestCase("GOOCV 305RBQ2BZBZT2 | GOOCV VP83T1ZUHROL", "GOOCV", "GOOCV 151224P00750000")] // Option
        [TestCase("SPY R735QTJ8XC9X", "SPY", null)] // Equity
        [TestCase("EURGBP 8G", "EURGBP", null)] // Forex
        [TestCase("BTCUSD XJ", "BTCUSD", null)] // Crypto
        public void SymbolCanonical(string identifier, string ticker, string expectedValue)
        {
            var symbol = new Symbol(SecurityIdentifier.Parse(identifier), ticker);
            if (expectedValue != null)
            {
                var result = symbol.Canonical;

                Assert.IsNotNull(result);
                Assert.AreSame(result, symbol.Canonical);
                Assert.IsTrue(result.IsCanonical());
                Assert.IsTrue(result.Value.Contains(ticker));
                Assert.AreEqual(symbol.SecurityType, result.SecurityType);
                Assert.AreEqual(symbol.ID.Market, result.ID.Market);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    var canonical = symbol.Canonical;
                });
            }
        }

        [TestCase("SPY", "SPY", "SPY 2T")]
        [TestCase("NWSA", "FOXA", "NWSA 2T")]
        [TestCase("NWSA", "FOXA", "NWSA 2S|NWSA 2T")]
        [TestCase("QQQQ", "QQQ", "QQQQ 2S|QQQQ 31|QQQQ 2T")]
        public void SymbolAndUnderlyingSymbolsMapped(string ticker, string mappedTicker, string sid)
        {
            var symbol = new Symbol(SecurityIdentifier.Parse(sid), ticker);
            var symbolChain = symbol;

            do
            {
                Assert.AreEqual(symbolChain.Value, ticker);
                symbolChain = symbolChain.Underlying;
            } while (symbolChain != null);

            symbol = symbol.UpdateMappedSymbol(mappedTicker);
            symbolChain = symbol;

            do
            {
                Console.WriteLine(symbolChain.ToString() + "; Value: " + symbolChain.Value);
                if (symbolChain.SecurityType == SecurityType.Base || symbolChain.RequiresMapping())
                {
                    Assert.AreEqual(mappedTicker, symbolChain.Value);
                }
                else
                {
                    Assert.AreNotEqual(mappedTicker, symbolChain.Value);
                }
                symbolChain = symbolChain.Underlying;
            } while (symbolChain != null);
        }

        [Test]
        public void SymbolReferenceNotMappedOnUpdate()
        {
            var symbol = Symbol.Create("NWSA", SecurityType.Equity, Market.USA);
            var customSymbol = Symbol.CreateBase(typeof(BaseData), symbol, Market.USA);

            var mappedSymbol = customSymbol.UpdateMappedSymbol("FOXA");

            Assert.AreNotEqual(customSymbol.Value, mappedSymbol.Value);
            Assert.AreNotEqual(customSymbol.Underlying.Value, mappedSymbol.Underlying.Value);
            Assert.AreNotEqual(symbol.Value, mappedSymbol.Underlying.Value);
        }
    }
}
