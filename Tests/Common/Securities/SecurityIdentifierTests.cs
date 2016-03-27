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
using Newtonsoft.Json;
using NUnit.Framework;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityIdentifierTests
    {
        private SecurityIdentifier SPY
        {
            get { return SecurityIdentifier.GenerateEquity(new DateTime(1998, 01, 02), "SPY", Market.USA); }
        }


        // this is really not european style, but I'd prefer to test a value of 1 vs a value of 0
        private readonly SecurityIdentifier SPY_Put_19550 = SecurityIdentifier.GenerateOption(new DateTime(2015, 09, 18), "SPY", Market.USA, 195.50m, OptionRight.Put, OptionStyle.European);

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
        public void Generates12Character()
        {
            var sid1 = SecurityIdentifier.GenerateBase("123456789012", Market.USA);
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
        public void ReturnsCorrectMarket()
        {
            Assert.AreEqual(Market.USA, SPY.Market);
        }

        [Test]
        public void ReturnsCorrectMarketWhenNotFound()
        {
            var sid = new SecurityIdentifier("some symbol", 0357960000000009901);
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
        public void RoundTripEmptyParse()
        {
            Assert.AreEqual(SecurityIdentifier.Empty, SecurityIdentifier.Parse(SecurityIdentifier.Empty.ToString()));
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
        public void SupportsSpecialCharactersInSymbol()
        {
            const string symbol = "~!@#$%^&*()_+¼»`ÆÜCⁿª▓G";
            var sid = new SecurityIdentifier(symbol, 0);
            Assert.AreEqual(sid.Symbol, symbol);
        }

        class Container
        {
            public SecurityIdentifier sid;
        }
    }
}
