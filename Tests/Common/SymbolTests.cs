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
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class SymbolTests
    {
        private JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        [Test]
        public void UsesSidForDictionaryKey()
        {
            var dictionary = new Dictionary<Symbol, int>
            {
                {new Symbol("sid", "value"), 1}
            };

            var key = new Symbol("sid", "other value");
            Assert.IsTrue(dictionary.ContainsKey(key));
        }
        
        [Test]
        public void SurvivesRoundtripSerialization()
        {
            var expected = new Symbol("sid", "value");
            var json = JsonConvert.SerializeObject(expected, Settings);
            var actual = JsonConvert.DeserializeObject<Symbol>(json, Settings);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public void SurvivesRoundtripSerializationWithTypeNameHandling()
        {
            var expected = new Symbol("sid", "value");
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
'Suspicious':false,'BidPrice':0.72722,'AskPrice':0.7278,'LastPrice':0.72722,'DataType':2,'IsFillForward':false,'Time':'2015-09-18T16:52:37.379',
'EndTime':'2015-09-18T16:52:37.379',
'Symbol':{'$type':'QuantConnect.Symbol, QuantConnect.Common',
'Value':'EURGBP',
'Permtick':'EURGBP'},'Value':0.72722,'Price':0.72722}]}";

            var expected = new Symbol("EURGBP");
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
                    "'Symbol':{'$type':'QuantConnect.Symbol, QuantConnect.Common','Value':'EURUSD','Permtick':'EURUSD'}," +
                    "'Value':1.11895,'Price':1.11895}," +

                    "{'$type':'QuantConnect.Data.Market.Tick, QuantConnect.Common'," +
                    "'TickType':0,'Quantity':1,'Exchange':'','SaleCondition':'','Suspicious':false," +
                    "'BidPrice':0.72157,'AskPrice':0.72162,'LastPrice':0.72157,'DataType':2,'IsFillForward':false," +
                    "'Time':'2015-09-22T01:26:44.675','EndTime':'2015-09-22T01:26:44.675'," +
                    "'Symbol':{'$type':'QuantConnect.Symbol, QuantConnect.Common','Value':'EURGBP','Permtick':'EURGBP'}," +
                    "'Value':0.72157,'Price':0.72157}," +

                    "]}";
            
            var actual = JsonConvert.DeserializeObject<List<BaseData>>(json, Settings);
            Assert.IsFalse(actual.All(x => x.Symbol == "EURUSD"));
        }

        [Test]
        public void SymbolTypeNameHandling()
        {
            const string json = @"{'$type':'QuantConnect.Symbol, QuantConnect.Common',
'Value':'EURGBP',
'Permtick':'EURGBP'}";
            var expected = new Symbol("EURGBP");
            var actual = JsonConvert.DeserializeObject<Symbol>(json, Settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TickRoundTrip()
        {
            var tick = new Tick
            {
                Symbol = "EURGBP",
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
        public void CompareToItselfReturnsZero()
        {
            var sym = new Symbol("sym");
            Assert.AreEqual(0, sym.CompareTo(sym));
        }

        [Test]
        public void ComparesTheSameAsStringCompare()
        {
            var a = new Symbol("a");
            var z = new Symbol("z");

            Assert.AreEqual(string.Compare("a", "z", StringComparison.Ordinal), a.CompareTo(z));
            Assert.AreEqual(string.Compare("z", "a", StringComparison.Ordinal), z.CompareTo(a));
        }

        [Test]
        public void ComparesTheSameAsStringCompareAndIgnoresCase()
        {
            var a = new Symbol("a");
            var z = new Symbol("z");

            Assert.AreEqual(string.Compare("a", "Z", StringComparison.OrdinalIgnoreCase), a.CompareTo(z));
            Assert.AreEqual(string.Compare("z", "A", StringComparison.OrdinalIgnoreCase), z.CompareTo(a));
        }

        [Test]
        public void ComparesAgainstStringWithoutException()
        {
            var a = new Symbol("a");
            Assert.AreEqual(0, a.CompareTo("a"));
        }

        [Test]
        public void ComparesAgainstStringIgnoringCase()
        {
            var a = new Symbol("a");
            Assert.AreEqual(0, a.CompareTo("A"));
        }
    }
}
