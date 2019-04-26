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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data.Auxiliary;

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

        [Test]
        public void GenerateEquityProperlyResolvesFirstDate()
        {
            var spy = SecurityIdentifier.GenerateEquity("SPY", Market.USA);
            Assert.AreEqual(new DateTime(1998, 01, 02), spy.Date);
        }

        [Test]
        public void GeneratesIdentifiersDeterministically()
        {
            var sid1 = SPY;
            var sid2 = SPY;
            Assert.AreEqual(sid1, sid2);
            Console.WriteLine(sid1);
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

            Console.WriteLine(SPY_Put_19550);
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

            Console.WriteLine(sid1);
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

            Console.WriteLine(eurusd);
        }
        [Test]
        public void FuturesSecurityIdReturnsProperties()
        {
            // verify various values
            Assert.AreEqual(new DateTime(2020, 12, 15), ED_Dec_2020.Date);
            Assert.AreEqual(Market.USA, ED_Dec_2020.Market);
            Assert.AreEqual(SecurityType.Future, ED_Dec_2020.SecurityType);
            Assert.AreEqual("ED", ED_Dec_2020.Symbol);

            Console.WriteLine(ED_Dec_2020);
        }

        [Test]
        public void Generates12Character()
        {
            var sid1 = SecurityIdentifier.GenerateBase(null, "123456789012", Market.USA);
            Assert.AreEqual("123456789012", sid1.Symbol);
            Console.WriteLine(sid1);
        }

        [Test]
        public void ParsedToStringEqualsValue()
        {
            var value = SPY_Put_19550.ToString();
            Console.WriteLine(value);
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
        public void ReturnsCorrectOptionRight()
        {
            Assert.AreEqual(OptionRight.Put, SPY_Put_19550.OptionRight);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), MatchType = MessageMatch.Contains,
            ExpectedMessage = "OptionRight is only defined for SecurityType.Option")]
        public void OptionRightThrowsOnNonOptionSecurityType()
        {
            var OptionRight = SPY.OptionRight;
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
        [ExpectedException(typeof(InvalidOperationException), MatchType = MessageMatch.Contains,
            ExpectedMessage = "OptionStyle is only defined for SecurityType.Option")]
        public void OptionStyleThrowsOnNonOptionSecurityType()
        {
            var optionStyle = SPY.OptionStyle;
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

        [Test]
        public void TryParseFailsInvalidProperties()
        {
            const string value = "SPY WhatEver";
            SecurityIdentifier sid;
            Assert.IsFalse(SecurityIdentifier.TryParse(value, out sid));
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
            Console.WriteLine("Elapsed: " + stopwatch.Elapsed);

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
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "must not contain the characters")]
        public void ThrowsOnInvalidSymbolCharacters(string input)
        {
            new SecurityIdentifier(input, 0);
        }

        [Test]
        public void GenerateEquityWithTickerUsingMapFile()
        {
            var expectedFirstDate = new DateTime(1998, 1, 2);
            var sid = SecurityIdentifier.GenerateEquity("TWX", Market.USA, mapSymbol: true, mapFileProvider: new LocalDiskMapFileProvider());

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

        class Container
        {
            public SecurityIdentifier sid;
        }
    }
}
