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
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class SymbolJsonConverterTests
    {
        private JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        [Test]
        public void SurvivesRoundtripSerialization()
        {
            var sid = SecurityIdentifier.GenerateEquity("SPY", Market.USA);
            var expected = new Symbol(sid, "value");
            var json = JsonConvert.SerializeObject(expected, Settings);
            var actual = JsonConvert.DeserializeObject<Symbol>(json, Settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SurvivesRoundtripSerializationOption()
        {
            var expected = Symbol.CreateOption("XLRE", Market.USA, OptionStyle.American, OptionRight.Call, 21m, new DateTime(2016, 08, 19));

            var json = JsonConvert.SerializeObject(expected, Settings);
            var actual = JsonConvert.DeserializeObject<Symbol>(json, Settings);
            Assert.AreEqual(expected, actual);

            Assert.AreEqual(expected.ID, actual.ID);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.ID.Date, actual.ID.Date);
            Assert.AreEqual(expected.ID.StrikePrice, actual.ID.StrikePrice);
            Assert.AreEqual(expected.ID.OptionRight, actual.ID.OptionRight);
            Assert.AreEqual(expected.ID.OptionStyle, actual.ID.OptionStyle);

            Assert.AreEqual(expected.Underlying.ID, actual.Underlying.ID);
            Assert.AreEqual(expected.Underlying.Value, actual.Underlying.Value);
        }

        [Test]
        public void SurvivesRoundtripSerializationCanonicalOption()
        {
            var expected = Symbol.Create("SPY", SecurityType.Option, Market.USA);

            var json = JsonConvert.SerializeObject(expected, Settings);
            var actual = JsonConvert.DeserializeObject<Symbol>(json, Settings);
            Assert.AreEqual(expected, actual);

            Assert.AreEqual(SecurityIdentifier.DefaultDate, actual.ID.Date);
            Assert.AreEqual(0m, actual.ID.StrikePrice);
            Assert.AreEqual(default(OptionRight), actual.ID.OptionRight);
            Assert.AreEqual(default(OptionStyle), actual.ID.OptionStyle);
            Assert.AreNotEqual(default(Symbol), actual.Underlying);
        }

        [Test]
        public void SurvivesRoundtripSerializationWithTypeNameHandling()
        {
            var sid = SecurityIdentifier.GenerateEquity("SPY", Market.USA);
            var expected = new Symbol(sid, "value");
            var json = JsonConvert.SerializeObject(expected, Settings);
            var actual = JsonConvert.DeserializeObject<Symbol>(json);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void HandlesListTicks()
        {
            const string json = @"{'$type':'System.Collections.Generic.List`1[[QuantConnect.Data.BaseData, QuantConnect.Common]], mscorlib',
'$values':[{'$type':'QuantConnect.Data.Market.Tick, QuantConnect.Common',
'TickType':0,'Quantity':1,'Exchange':'',
'SaleCondition':'',
'Suspicious':false,'BidPrice':0.72722,'AskPrice':0.7278,'BidSize':0,'AskSize':0,'LastPrice':0.72722,'DataType':2,'IsFillForward':false,'Time':'2015-09-18T16:52:37.379',
'EndTime':'2015-09-18T16:52:37.379',
'Symbol':{'$type':'QuantConnect.Symbol, QuantConnect.Common',
'Value':'EURGBP',
'ID':'EURGBP 5O'},'Value':0.72722,'Price':0.72722}]}";

            var expected = new Symbol(SecurityIdentifier.GenerateForex("EURGBP", Market.FXCM), "EURGBP");
            var settings = Settings;
            var actual = JsonConvert.DeserializeObject<List<BaseData>>(json, settings);
            Assert.AreEqual(expected, actual[0].Symbol);
        }

        [Test]
        public void HandlesListTicksWithDifferentSymbols()
        {
            // the first serialized Tick object has a Symbol of EURGBP and the second has EURUSD, but the output
            const string json =
                "{'$type':'System.Collections.Generic.List`1[[QuantConnect.Data.BaseData, QuantConnect.Common]], mscorlib','$values':[" +

                    "{'$type':'QuantConnect.Data.Market.Tick, QuantConnect.Common'," +
                    "'TickType':0,'Quantity':1,'Exchange':'','SaleCondition':'','Suspicious':false," +
                    "'BidPrice':1.11895,'AskPrice':1.11898,'LastPrice':1.11895,'DataType':2,'IsFillForward':false," +
                    "'Time':'2015-09-22T01:26:44.676','EndTime':'2015-09-22T01:26:44.676'," +
                    "'Symbol':{'$type':'QuantConnect.Symbol, QuantConnect.Common','Value':'EURUSD', 'ID': 'EURUSD 5O'}," +
                    "'Value':1.11895,'Price':1.11895}," +

                    "{'$type':'QuantConnect.Data.Market.Tick, QuantConnect.Common'," +
                    "'TickType':0,'Quantity':1,'Exchange':'','SaleCondition':'','Suspicious':false," +
                    "'BidPrice':0.72157,'AskPrice':0.72162,'LastPrice':0.72157,'DataType':2,'IsFillForward':false," +
                    "'Time':'2015-09-22T01:26:44.675','EndTime':'2015-09-22T01:26:44.675'," +
                    "'Symbol':{'$type':'QuantConnect.Symbol, QuantConnect.Common','Value':'EURGBP', 'ID': 'EURGBP 5O'}," +
                    "'Value':0.72157,'Price':0.72157}," +

                    "]}";

            var actual = JsonConvert.DeserializeObject<List<BaseData>>(json, Settings);
            Assert.IsFalse(actual.All(x => x.Symbol == new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD")));
        }

        [TestCaseSource(nameof(TestSymbols))]
        public void CamelCaseSymbolIsDeserializedCorrectly(string json, string value, string id, SecurityType securityType, Symbol underlying)
        {
            var deserializedSymbol = JsonConvert.DeserializeObject<Symbol>(json, Settings);
            Assert.AreEqual(value, deserializedSymbol.Value);
            Assert.AreEqual(id, deserializedSymbol.ID.ToString());
            Assert.AreEqual(securityType, deserializedSymbol.SecurityType);
            Assert.AreEqual(underlying, deserializedSymbol.Underlying);
        }

        [Test]
        public void TickRoundTrip()
        {
            var tick = new Tick
            {
                Symbol = Symbols.EURGBP,
                AskPrice = 1,
                Time = DateTime.Now,
                Exchange = "",
                Value = 2,
                EndTime = DateTime.Now,
                Quantity = 1,
                BidPrice = 2,
                SaleCondition = ""
            };
            var json = JsonConvert.SerializeObject(tick, Settings);
            var actual = JsonConvert.DeserializeObject<Tick>(json, Settings);
            Assert.AreEqual(tick.Symbol, actual.Symbol);

            json = JsonConvert.SerializeObject(tick, Settings);
            actual = JsonConvert.DeserializeObject<Tick>(json);
            Assert.AreEqual(tick.Symbol, actual.Symbol);
        }

        [Test]
        public void BackwardsCompatibleJson()
        {
            var symbol = new Symbol(SecurityIdentifier.GenerateForex("a", Market.FXCM), "a");
            var json = JsonConvert.SerializeObject(symbol, new JsonSerializerSettings { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.All });
            var oldSymbol = JsonConvert.DeserializeObject<OldSymbol>(json);
            Assert.AreEqual("A", oldSymbol.Value);
            Assert.AreEqual("A", oldSymbol.Permtick);
        }

        [TestCase("{\"value\":\"Fb    210618c00322500\",\"type\":\"2\"}", SecurityType.Option, "FB", "FB", OptionRight.Call, OptionStyle.American, 2021)]
        [TestCase("{\"value\":\"aapl  210618C00129000\",\"type\":\"2\"}", SecurityType.Option, "AAPL", "AAPL", OptionRight.Call, OptionStyle.American, 2021)]

        [TestCase("{\"value\":\"OGV1 C2040\",\"type\":\"8\"}", SecurityType.FutureOption, "GC", "OG", OptionRight.Call, OptionStyle.American, 2021)]
        [TestCase("{\"value\":\"ESZ30 C3505\",\"type\":\"8\"}", SecurityType.FutureOption, "ES", "ES", OptionRight.Call, OptionStyle.American, 2030)]
        [TestCase("{\"value\":\"SPXW  210618C04165000\",\"type\":\"10\"}", SecurityType.IndexOption, "SPX", "SPXW", OptionRight.Call, OptionStyle.American, 2021)]
        public void OptionUserFriendlyDeserialization(string jsonValue, SecurityType type, string underlying, string option, OptionRight optionRight, OptionStyle optionStyle, int expirationYear)
        {
            var symbol = JsonConvert.DeserializeObject<Symbol>(jsonValue);

            Assert.IsNotNull(symbol);
            Assert.AreEqual(type, symbol.SecurityType);
            Assert.AreEqual(option, symbol.ID.Symbol);
            Assert.AreEqual(underlying, symbol.ID.Underlying.Symbol);
            Assert.AreEqual(optionRight, symbol.ID.OptionRight);
            Assert.AreEqual(optionStyle, symbol.ID.OptionStyle);
            Assert.AreEqual(expirationYear, symbol.ID.Date.Year);
        }

        [TestCase("{\"value\":\"GCV1\",\"type\":\"5\"}", SecurityType.Future, "GC", 10, Market.COMEX)]
        [TestCase("{\"value\":\"ESZ1\",\"type\":\"5\"}", SecurityType.Future, "ES", 12, Market.CME)]
        public void FutureUserFriendlyDeserialization(string jsonValue, SecurityType type, string symbolId, int month, string market)
        {
            var symbol = JsonConvert.DeserializeObject<Symbol>(jsonValue);

            Assert.IsNotNull(symbol);
            Assert.AreEqual(type, symbol.SecurityType);
            Assert.AreEqual(symbolId, symbol.ID.Symbol);
            Assert.AreEqual(month, symbol.ID.Date.Month);
            Assert.AreEqual(market, symbol.ID.Market);
        }

        [TestCase("{\"value\":\"fb\",\"type\":\"1\"}", SecurityType.Equity, "FB", Market.USA)]
        [TestCase("{\"value\":\"AAPL\",\"type\":\"1\"}", SecurityType.Equity, "AAPL", Market.USA)]

        [TestCase("{\"value\":\"BTCUSD\",\"type\":\"7\",\"market\":\"coinbase\"}", SecurityType.Crypto, "BTCUSD", Market.Coinbase)]
        [TestCase("{\"value\":\"BTCUSD\",\"type\":\"7\",\"market\":\"binance\"}", SecurityType.Crypto, "BTCUSD", Market.Binance)]

        [TestCase("{\"value\":\"xauusd\",\"type\":\"6\",\"market\":\"oanda\"}", SecurityType.Cfd, "XAUUSD", Market.Oanda)]

        [TestCase("{\"value\":\"eurusd\",\"type\":\"4\",\"market\":\"oanda\"}", SecurityType.Forex, "EURUSD", Market.Oanda)]
        public void UserFriendlyDeserialization(string jsonValue, SecurityType type, string symbolTicker, string market)
        {
            var symbol = JsonConvert.DeserializeObject<Symbol>(jsonValue);

            Assert.IsNotNull(symbol);
            Assert.AreEqual(type, symbol.SecurityType);
            Assert.AreEqual(symbolTicker, symbol.ID.Symbol);
            Assert.AreEqual(market, symbol.ID.Market);
        }

        public static object[] TestSymbols =
        {
            new object[] { @"{
				""value"": ""AAPL  140613P00660000"",
				""id"": ""AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X"",
				""permtick"": ""AAPL  140613P00660000"",
				""underlying"": {
					""value"": ""AAPL"",
					""id"": ""AAPL R735QTJ8XC9X"",
					""permtick"": ""AAPL""
				}
			}", "AAPL  140613P00660000", "AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X", SecurityType.Option, Symbols.AAPL },
            new object[] { @"{
                ""value"": ""GOOG  160115C00750000"",
                ""id"": ""GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"",
                ""permtick"": ""GOOG  160115C00750000"",
                ""underlying"": {
                    ""value"": ""GOOG"",
                    ""id"": ""GOOCV VP83T1ZUHROL"",
                    ""permtick"": ""GOOG""
                }
            }", "GOOG  160115C00750000", "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL", SecurityType.Option, Symbols.GOOG },
            new object[] { @"{
                ""value"": ""SPY"",
                ""id"": ""SPY R735QTJ8XC9X"",
                ""permtick"": ""SPY""
            }", "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity, null },
            new object[] { @"{
				""Value"": ""AAPL  140613P00660000"",
				""ID"": ""AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X"",
				""Permtick"": ""AAPL  140613P00660000"",
				""Underlying"": {
					""value"": ""AAPL"",
					""id"": ""AAPL R735QTJ8XC9X"",
					""permtick"": ""AAPL""
				}
			}", "AAPL  140613P00660000", "AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X", SecurityType.Option, Symbols.AAPL },
            new object[] { @"{
                ""Value"": ""GOOG  160115C00750000"",
                ""ID"": ""GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"",
                ""Permtick"": ""GOOG  160115C00750000"",
                ""Underlying"": {
                    ""value"": ""GOOG"",
                    ""id"": ""GOOCV VP83T1ZUHROL"",
                    ""permtick"": ""GOOG""
                }
            }", "GOOG  160115C00750000", "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL", SecurityType.Option, Symbols.GOOG },
            new object[] { @"{
                ""Value"": ""SPY"",
                ""ID"": ""SPY R735QTJ8XC9X"",
                ""Permtick"": ""SPY""
            }", "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity, null }
        };

        class OldSymbol
        {
            public string Value { get; set; }
            public string Permtick { get; set; }
        }
    }
}
