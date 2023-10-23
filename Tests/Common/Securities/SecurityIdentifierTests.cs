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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Util;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityIdentifierTests
    {
        private static SecurityIdentifier SPY
        {
            get { return SecurityIdentifier.GenerateEquity(new DateTime(1998, 01, 02), "SPY", Market.USA); }
        }

        // this is really not european style, but I'd prefer to test a value of 1 vs a value of 0
        private readonly SecurityIdentifier SPY_Put_19550 = SecurityIdentifier.GenerateOption(new DateTime(2015, 09, 18), SPY, Market.USA, 195.50m, OptionRight.Put, OptionStyle.European);
        // this is euro-dollar futures contract (for tests)
        private readonly SecurityIdentifier ED_Dec_2020 = SecurityIdentifier.GenerateFuture(new DateTime(2020, 12, 15), "ED", Market.USA);

        [TestCase("SPY", "SPY", "20230403")]
        [TestCase("GOOG", "GOOG", "20140403")]
        [TestCase("GOOG", "GOOCV", "20140402")]
        public void Ticker(string symbol, string expectedTicker, string date)
        {
            var equity = Symbol.Create(symbol, SecurityType.Equity, Market.USA);
            var ticker = SecurityIdentifier.Ticker(equity, Time.ParseDate(date));

            Assert.AreEqual(expectedTicker, ticker);
        }

        [Test]
        public void GenerateEquityProperlyResolvesFirstDate()
        {
            var spy = SecurityIdentifier.GenerateEquity("SPY", Market.USA);
            Assert.AreEqual(new DateTime(1998, 01, 02), spy.Date);
        }

        [Test]
        public void GenerateFailsOnInvalidDate()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SecurityIdentifier.GenerateEquity(Time.BeginningOfTime.AddDays(-1), "SPY", Market.USA));
        }

        [Test]
        public void GeneratesIdentifiersDeterministically()
        {
            var sid1 = SPY;
            var sid2 = SPY;
            Assert.AreEqual(sid1, sid2);
            Log.Trace(sid1.ToString());
        }

        [Test]
        public void GeneratesOptionSecurityIdentifier()
        {
            var spyPut = SPY_Put_19550;

            // verify various values
            Assert.AreEqual(OptionRight.Put, spyPut.OptionRight); // put
            Assert.AreEqual(new DateTime(2015, 09, 18), spyPut.Date); // oa date 2015.09.18
            Assert.AreEqual(OptionStyle.European, spyPut.OptionStyle); // option style
            Assert.AreEqual(195.5m, spyPut.StrikePrice); // strike/scale
            Assert.AreEqual(Market.USA, spyPut.Market); // market
            Assert.AreEqual(SecurityType.Option, spyPut.SecurityType); // security type
            Assert.AreEqual("SPY", spyPut.Symbol); // SPY in base36
            Assert.IsTrue(spyPut.HasUnderlying);
            Assert.AreEqual(SPY, spyPut.Underlying);

            Log.Trace(SPY_Put_19550.ToString());
        }

        [Test]
        public void GeneratesEquitySecurityIdentifier()
        {
            var sid1 = SecurityIdentifier.GenerateEquity(new DateTime(1998, 01, 02), "SPY", Market.USA);

            // verify various values
            Assert.AreEqual(new DateTime(1998, 01, 02), sid1.Date);
            Assert.AreEqual(Market.USA, sid1.Market);
            Assert.AreEqual(SecurityType.Equity, sid1.SecurityType);
            Assert.AreEqual("SPY", sid1.Symbol);

            Log.Trace(sid1.ToString());
        }

        [Test]
        public void GeneratesForexSecurityIdentifier()
        {
            var eurusd = SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM);

            // verify various values
            Assert.Throws<InvalidOperationException>(() => { var x = eurusd.Date; });
            Assert.AreEqual(Market.FXCM, eurusd.Market);
            Assert.AreEqual(SecurityType.Forex, eurusd.SecurityType);
            Assert.AreEqual("EURUSD", eurusd.Symbol);

            Log.Trace(eurusd.ToString());
        }

        [Test]
        public void FuturesSecurityIdReturnsProperties()
        {
            // verify various values
            Assert.AreEqual(new DateTime(2020, 12, 15), ED_Dec_2020.Date);
            Assert.AreEqual(Market.USA, ED_Dec_2020.Market);
            Assert.AreEqual(SecurityType.Future, ED_Dec_2020.SecurityType);
            Assert.AreEqual("ED", ED_Dec_2020.Symbol);

            Log.Trace(ED_Dec_2020.ToString());
        }

        [Test]
        public void Generates12Character()
        {
            var sid1 = SecurityIdentifier.GenerateBase(null, "123456789012", Market.USA);
            Assert.AreEqual("123456789012", sid1.Symbol);
            Log.Trace(sid1.ToString());
        }

        [Test]
        public void ParsedToStringEqualsValue()
        {
            var value = SPY_Put_19550.ToString();
            Log.Trace(value);
            var sid2 = SecurityIdentifier.Parse(value);
            Assert.AreEqual(SPY_Put_19550, sid2);
        }

        [Test]
        public void ToStringPipeDelimitsUnderlying()
        {
            var actual = SPY_Put_19550.ToString();
            var parts = actual.Split('|');
            var option = SecurityIdentifier.Parse(parts[0]);
            // verify various values
            Assert.AreEqual(OptionRight.Put, option.OptionRight); // put
            Assert.AreEqual(new DateTime(2015, 09, 18), option.Date); // oa date 2015.09.18
            Assert.AreEqual(OptionStyle.European, option.OptionStyle); // option style
            Assert.AreEqual(195.5m, option.StrikePrice); // strike/scale
            Assert.AreEqual(Market.USA, option.Market); // market
            Assert.AreEqual(SecurityType.Option, option.SecurityType); // security type
            Assert.AreEqual("SPY", option.Symbol); // SPY in base36
            Assert.IsFalse(option.HasUnderlying);
            Assert.Throws<InvalidOperationException>(() => { var x = option.Underlying; });
            var equity = SecurityIdentifier.Parse(parts[1]);
            Assert.AreEqual(SPY, equity);
        }

        [Test]
        public void ReturnsCorrectMarket()
        {
            Assert.AreEqual(Market.USA, SPY.Market);
        }

        [Test]
        public void ReturnsCorrectMarketWhenNotFound()
        {
            var sid = new SecurityIdentifier("some-symbol", 0357960000000009901);
            Assert.AreEqual("99", sid.Market);
        }

        [Test]
        public void InvalidSecurityType()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var sid = new SecurityIdentifier("some-symbol", 0357960000000009915);
            }, $"The provided properties do not match with a valid {nameof(SecurityType)}");
        }

        [TestCaseSource(nameof(ValidSecurityTypes))]
        public void ValidSecurityType(ulong properties)
        {
            Assert.DoesNotThrow(() =>
            {
                var sid = new SecurityIdentifier("some-symbol", properties);
            });
        }

        [Test]
        public void ReturnsCorrectOptionRight()
        {
            Assert.AreEqual(OptionRight.Put, SPY_Put_19550.OptionRight);
        }

        [Test]
        public void OptionRightThrowsOnNonOptionSecurityType()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var OptionRight = SPY.OptionRight;
            }, "OptionRight is only defined for SecurityType.Option");
        }

        [Test]
        public void ReturnsCorrectSecurityType()
        {
            Assert.AreEqual(SecurityType.Equity, SPY.SecurityType);
            Assert.AreEqual(SecurityType.Option, SPY_Put_19550.SecurityType);
        }

        [Test]
        public void ReturnsCorrectSymbol()
        {
            Assert.AreEqual("SPY", SPY.Symbol);
        }

        [Test]
        public void ReturnsCorrectStrikePrice()
        {
            Assert.AreEqual(195.50m, SPY_Put_19550.StrikePrice);
        }

        [Test]
        public void ReturnsCorrectDate()
        {
            Assert.AreEqual(new DateTime(1998, 01, 02), SPY.Date);
            Assert.AreEqual(new DateTime(2015, 09, 18), SPY_Put_19550.Date);
        }

        [Test]
        public void ReturnsCorrectOptionStyle()
        {
            Assert.AreEqual(OptionStyle.European, SPY_Put_19550.OptionStyle);
        }

        [Test]
        public void OptionStyleThrowsOnNonOptionSecurityType()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var optionStyle = SPY.OptionStyle;
            }, "OptionStyle is only defined for SecurityType.Option");
        }

        [Test]
        public void EmptyUsesEmptySymbol()
        {
            Assert.AreEqual(string.Empty, SecurityIdentifier.Empty.Symbol);
        }

        [Test]
        public void PreviousEmptyFormatStillSupported()
        {
            Assert.AreEqual(SecurityIdentifier.Empty, SecurityIdentifier.Parse(" "));
        }

        [Test]
        public void RoundTripEmptyParse()
        {
            Assert.AreEqual(SecurityIdentifier.Empty, SecurityIdentifier.Parse(SecurityIdentifier.Empty.ToString()));
        }

        [Test]
        public void RoundTripNoneParse()
        {
            Assert.AreEqual(SecurityIdentifier.None, SecurityIdentifier.Parse(SecurityIdentifier.None.ToString()));
        }

        [Test]
        public void UsedAsDictionaryKey()
        {
            var hash = new HashSet<SecurityIdentifier>();
            Assert.IsTrue(hash.Add(SPY));
            Assert.IsFalse(hash.Add(SPY));
        }

        [Test]
        public void SerializesToSimpleString()
        {
            var sid = SPY;
            var str = sid.ToString();
            var serialized = JsonConvert.SerializeObject(sid);
            Assert.AreEqual("\"" + str + "\"", serialized);
        }

        [Test]
        public void DeserializesFromSimpleString()
        {
            var sid = SPY;
            var str = "\"" + sid + "\"";
            var deserialized = JsonConvert.DeserializeObject<SecurityIdentifier>(str);
            Assert.AreEqual(sid, deserialized);
        }

        [Test]
        public void DeserializesFromSimpleStringWithinContainerClass()
        {
            var sid = new Container{sid =SPY};
            var str =
@"
{
    'sid': '" + SPY + @"'
}";
            var deserialized = JsonConvert.DeserializeObject<Container>(str);
            Assert.AreEqual(sid.sid, deserialized.sid);
        }

        [Test]
        public void ParsesFromStringCorrectly()
        {
            const string value = "SPY R735QTJ8XC9X";
            SecurityIdentifier sid;
            Assert.IsTrue(SecurityIdentifier.TryParse(value, out sid));
            Assert.AreEqual(sid.ToString(), value);
        }

        [TestCase("SPY WhatEver")]
        [TestCase("Sharpe ratio")]
        public void TryParseFailsInvalidProperties(string value)
        {
            Assert.IsFalse(SecurityIdentifier.TryParse(value, out var _));
            // On the second call, we test the cache to increase speed and remove redundant logging
            Assert.IsFalse(SecurityIdentifier.TryParse(value, out var _));
        }

        [Test, Category("TravisExclude")]
        public void ParsesFromStringFastEnough()
        {
            const string value = "SPY R735QTJ8XC9X";

            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < 1000000; i++)
            {
                SecurityIdentifier sid;
                SecurityIdentifier.TryParse(value, out sid);
            }
            stopwatch.Stop();
            Log.Trace("Elapsed: " + stopwatch.Elapsed);

            Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SupportsSpecialCharactersInSymbol()
        {
            const string symbol = "~!@#$%^&*()_+¼»`ÆÜCⁿª▓G";
            var sid = new SecurityIdentifier(symbol, 0);
            Assert.AreEqual(sid.Symbol, symbol);
        }

        [Theory, TestCase("|"), TestCase(" ")]
        public void ThrowsOnInvalidSymbolCharacters(string input)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new SecurityIdentifier(input, 0);
            }, "must not contain the characters");
        }

        [Test]
        public void GenerateEquityWithTickerUsingMapFile()
        {
            var expectedFirstDate = new DateTime(1998, 1, 2);
            var sid = SecurityIdentifier.GenerateEquity("TWX", Market.USA, mapSymbol: true, mapFileProvider: TestGlobals.MapFileProvider);

            Assert.AreEqual(sid.Date, expectedFirstDate);
            Assert.AreEqual(sid.Symbol, "AOL");
        }

        [Test]
        public void GenerateBaseDataWithTickerUsingMapFile()
        {
            var expectedFirstDate = new DateTime(1998, 1, 2);
            var sid = SecurityIdentifier.GenerateBase(null, "TWX", Market.USA, mapSymbol: true);

            Assert.AreEqual(sid.Date, expectedFirstDate);
            Assert.AreEqual(sid.Symbol, "AOL");
        }

        [Test]
        public void GenerateBase_SymbolAppendsDptTypeName_WhenBaseDataTypeIsNotNull()
        {
            var symbol = "BTC";
            var expected = "BTC.Bitcoin";
            var baseDataType = typeof(LiveTradingFeaturesAlgorithm.Bitcoin);
            var sid = SecurityIdentifier.GenerateBase(baseDataType, symbol, Market.USA);
            Assert.AreEqual(expected, sid.Symbol);
        }

        [Test]
        public void GenerateBase_UsesProvidedSymbol_WhenBaseDataTypeIsNull()
        {
            var symbol = "BTC";
            var expected = "BTC";
            var baseDataType = (Type) null;
            var sid = SecurityIdentifier.GenerateBase(baseDataType, symbol, Market.USA);
            Assert.AreEqual(expected, sid.Symbol);
        }

        [Test]
        public void NegativeStrikePriceRoundTrip()
        {
            var future = Symbol.CreateFuture(
                "CL",
                Market.NYMEX,
                new DateTime(2020, 5, 20));

            var option = Symbol.CreateOption(
                future,
                Market.NYMEX,
                OptionStyle.American,
                OptionRight.Call,
                -50,
                new DateTime(2020, 4, 16));

            Assert.AreEqual(-50, option.ID.StrikePrice);

            // Forces the reconstruction of the strike price to ensure that it's been properly parsed.
            var newSid = SecurityIdentifier.Parse(option.ID.ToString());
            Assert.AreEqual(-50, newSid.StrikePrice);
        }

        [TestCase(OptionStyle.American, OptionRight.Call, "AAPL XEOLB4YAQ8BQ|AAPL R735QTJ8XC9X")]
        [TestCase(OptionStyle.American, OptionRight.Put, "AAPL 31DSLGKXI01PI|AAPL R735QTJ8XC9X")]
        [TestCase(OptionStyle.European, OptionRight.Call, "AAPL XEOOUQW0JB1I|AAPL R735QTJ8XC9X")]
        [TestCase(OptionStyle.European, OptionRight.Put, "AAPL 31DSP06V7T4FA|AAPL R735QTJ8XC9X")]
        public void SymbolHashForOptionsBackwardsCompatibilityWholeNumber(OptionStyle style, OptionRight right, string expected)
        {
            var equity = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var option = Symbol.CreateOption(
                equity,
                Market.USA,
                style,
                right,
                100m,
                new DateTime(2020, 5, 21));

            Assert.AreEqual(expected, option.ID.ToString());
            Assert.AreEqual(100m, option.ID.StrikePrice);
        }

        [TestCase(OptionStyle.American, OptionRight.Call, "AAPL XEOLB4YAHNOM|AAPL R735QTJ8XC9X")]
        [TestCase(OptionStyle.American, OptionRight.Put, "AAPL 31DSLGKXHRH2E|AAPL R735QTJ8XC9X")]
        [TestCase(OptionStyle.European, OptionRight.Call, "AAPL XEOOUQW0AQEE|AAPL R735QTJ8XC9X")]
        [TestCase(OptionStyle.European, OptionRight.Put, "AAPL 31DSP06V7KJS6|AAPL R735QTJ8XC9X")]
        public void SymbolHashForOptionsBackwardsCompatibilityFractionalNumber(OptionStyle style, OptionRight right, string expected)
        {
            var equity = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var option = Symbol.CreateOption(
                equity,
                Market.USA,
                style,
                right,
                0.01m, // strike decimal precision is limited to 4 decimal places only
                new DateTime(2020, 5, 21));

            Assert.AreEqual(expected, option.ID.ToString());
            Assert.AreEqual(0.01m, option.ID.StrikePrice);
        }

        [Test]
        public void SymbolHashForOptionsBackwardsCompatibilityLargeFractionalNumberDoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var equity = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
                var option = Symbol.CreateOption(
                    equity,
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Call,
                    3600.75m, // strike decimal precision is limited to 4 decimal places only
                    new DateTime(2020, 5, 21));
            });
        }

        [TestCase("-475711")]
        [TestCase("475711")]
        [TestCase("47.5711")]
        [TestCase("-47.5711")]
        public void NumberStrikePriceApproachesBoundsWithoutOverflowingSid(string strikeStr)
        {
            var strike = decimal.Parse(strikeStr, CultureInfo.InvariantCulture);
            var equity = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var option = Symbol.CreateOption(
                equity,
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                strike,
                new DateTime(2020, 5, 21));

            // The SID specification states that the total width for the properties value
            // is at most 20 digits long. If we overflowed the SID, the strike price can and will
            // eat from other slots, corrupting the data. If we have no overflow, the SID will
            // be constructed properly without corrupting any data, even as we approach the bounds.
            // We will assert that all properties contained within the _properties field are valid and not corrupted
            var sid = SecurityIdentifier.Parse(option.ID.ToString());

            Assert.AreEqual(new DateTime(2020, 5, 21), sid.Date);
            Assert.AreEqual(strike, sid.StrikePrice);
            Assert.AreEqual(OptionRight.Call, sid.OptionRight);
            Assert.AreEqual(OptionStyle.American, sid.OptionStyle);
            Assert.AreEqual(Market.USA, sid.Market);
            Assert.AreEqual(SecurityType.Option, sid.SecurityType);

            Assert.AreEqual(option.ID.Date,sid.Date);
            Assert.AreEqual(option.ID.StrikePrice, sid.StrikePrice);
            Assert.AreEqual(option.ID.OptionRight, sid.OptionRight);
            Assert.AreEqual(option.ID.OptionStyle, sid.OptionStyle);
            Assert.AreEqual(option.ID.Market, sid.Market);
            Assert.AreEqual(option.ID.SecurityType, sid.SecurityType);
        }

        [TestCase(475712.0)]
        [TestCase(47.5712)]
        [TestCase(999999.0)]
        [TestCase(-475712.0)]
        [TestCase(-47.5712)]
        [TestCase(-999999)]
        public void HighPrecisionNumberThrows(double strike)
        {
            var equity = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            Assert.Throws<ArgumentException>(() =>
            {
                Symbol.CreateOption(
                    equity,
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Call,
                    (decimal)strike, // strike decimal precision is limited to 4 decimal places only
                    new DateTime(2020, 5, 21));
            });
        }

        [Test, Ignore("Requires complete option data to validate chain")]
        public void ValidateAAPLOptionChainSecurityIdentifiers()
        {
            var chainProvider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var chains = new HashSet<Symbol>();
            var expectedChains = File.ReadAllLines("TestData/aapl_chain.csv")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToDictionary(x => x, _ => false);

            Assert.AreNotEqual(0, expectedChains.Count);

            var start = new DateTime(2020, 1, 1);
            var end = new DateTime(2020, 7, 1);

            foreach (var date in Time.EachDay(start, end))
            {
                if (MarketHoursDatabase.FromDataFolder()
                        .GetEntry(Market.USA, (string)null, SecurityType.Equity)
                        .ExchangeHours
                        .Holidays.Contains(date) || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                foreach (var symbol in chainProvider.GetOptionContractList(aapl, date))
                {
                    chains.Add(symbol);
                }
            }

            var fails = new HashSet<Symbol>();
            foreach (var chain in chains)
            {
                if (expectedChains.ContainsKey(chain.ID.ToString()))
                {
                    expectedChains[chain.ID.ToString()] = true;
                    continue;
                }

                fails.Add(chain);
            }

            Assert.AreEqual(0, fails.Count, $"The following option Symbols were not found in the expected chain:    \n{string.Join("\n", fails.Select(x => x.ID.ToString()))}");
            Assert.IsTrue(expectedChains.All(kvp => kvp.Value), $"The following option Symbols were not loaded:    \n{string.Join("\n", expectedChains.Where(kvp => !kvp.Value).Select(x => x.Key))}");
        }

        [Test]
        public void SortsAccordingToStringRepresentation()
        {
            var sids = Symbols.All.ToList(s => s.ID);
            var expected = sids
                .Select(sid => new {symbol = sid, str = sid.ToString()})
                .OrderBy(item => item.str)
                .ToList(item => item.symbol);

            sids.Sort();

            CollectionAssert.AreEqual(expected, sids);
        }

        class Container
        {
            public SecurityIdentifier sid;
        }
        private static List<TestCaseData> ValidSecurityTypes =>
            (from object value in Enum.GetValues(typeof(SecurityType)) select new TestCaseData((ulong)(0357960000000009900 + (int)value))).ToList();

    }
}
