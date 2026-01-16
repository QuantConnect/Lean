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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Statistics;
using System;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class TradeTests
    {
        [Test]
        public void JsonSerializationRoundTrip()
        {
            var trade = MakeTrade();

            var json = JsonConvert.SerializeObject(trade);
            var deserializedTrade = JsonConvert.DeserializeObject<Trade>(json);
            CollectionAssert.AreEqual(trade.Symbols, deserializedTrade.Symbols);
            Assert.AreEqual(trade.EntryTime, deserializedTrade.EntryTime);
            Assert.AreEqual(trade.EntryPrice, deserializedTrade.EntryPrice);
            Assert.AreEqual(trade.Direction, deserializedTrade.Direction);
            Assert.AreEqual(trade.Quantity, deserializedTrade.Quantity);
            Assert.AreEqual(trade.ExitTime, deserializedTrade.ExitTime);
            Assert.AreEqual(trade.ExitPrice, deserializedTrade.ExitPrice);
            Assert.AreEqual(trade.ProfitLoss, deserializedTrade.ProfitLoss);
            Assert.AreEqual(trade.TotalFees, deserializedTrade.TotalFees);
            Assert.AreEqual(trade.MAE, deserializedTrade.MAE);
            Assert.AreEqual(trade.MFE, deserializedTrade.MFE);

            // For backwards compatibility, also verify Symbol property is set correctly
            Assert.IsNotNull(trade.Symbol);
            Assert.AreEqual(trade.Symbols[0], trade.Symbol);
            Assert.AreEqual(trade.Symbol, deserializedTrade.Symbol);
        }

        [Test]
        public void DeprecatedSymbolIsNotSerialized()
        {
            var trade = MakeTrade();
            var jsonStr = JsonConvert.SerializeObject(trade);
            var json = JObject.Parse(jsonStr);
            Assert.IsFalse(json.ContainsKey("Symbol"));
        }

        [Test]
        public void CanDeserializeOldFormatWithSymbol()
        {
            var jsonTrade = @"
{
  ""Symbol"": {
    ""value"": ""EURUSD"",
    ""id"": ""EURUSD 8G"",
    ""permtick"": ""EURUSD""
  },
  ""EntryTime"": ""2023-01-02T12:31:45"",
  ""EntryPrice"": 1.07,
  ""Direction"": 0,
  ""Quantity"": 1000.0,
  ""ExitTime"": ""2023-01-02T12:51:45"",
  ""ExitPrice"": 1.09,
  ""ProfitLoss"": 20.0,
  ""TotalFees"": 2.5,
  ""MAE"": -5.0,
  ""MFE"": 30.0,
  ""Duration"": ""00:20:00"",
  ""EndTradeDrawdown"": -10.0,
  ""IsWin"": false,
  ""OrderIds"": []
}";
            var deserializedTrade = JsonConvert.DeserializeObject<Trade>(jsonTrade);
            Assert.IsNotNull(deserializedTrade);
            CollectionAssert.AreEqual(new[] { Symbols.EURUSD }, deserializedTrade.Symbols);
            Assert.AreEqual(new DateTime(2023, 1, 2, 12, 31, 45), deserializedTrade.EntryTime);
            Assert.AreEqual(1.07m, deserializedTrade.EntryPrice);
            Assert.AreEqual(TradeDirection.Long, deserializedTrade.Direction);
            Assert.AreEqual(1000m, deserializedTrade.Quantity);
            Assert.AreEqual(new DateTime(2023, 1, 2, 12, 51, 45), deserializedTrade.ExitTime);
            Assert.AreEqual(1.09m, deserializedTrade.ExitPrice);
            Assert.AreEqual(20m, deserializedTrade.ProfitLoss);
            Assert.AreEqual(2.5m, deserializedTrade.TotalFees);
            Assert.AreEqual(-5m, deserializedTrade.MAE);
            Assert.AreEqual(30m, deserializedTrade.MFE);
            // For backwards compatibility, also verify Symbol property is set correctly
            Assert.IsNotNull(deserializedTrade.Symbol);
            Assert.AreEqual(deserializedTrade.Symbols[0], deserializedTrade.Symbol);
        }

        private static Trade MakeTrade()
        {
            var entryTime = new DateTime(2023, 1, 2, 12, 31, 45);
            var exitTime = entryTime.AddMinutes(20);
            var trade = new Trade
            {
                Symbols = [Symbols.EURUSD],
                EntryTime = entryTime,
                EntryPrice = 1.07m,
                Direction = TradeDirection.Long,
                Quantity = 1000,
                ExitTime = exitTime,
                ExitPrice = 1.09m,
                ProfitLoss = 20,
                TotalFees = 2.5m,
                MAE = -5,
                MFE = 30
            };
            return trade;
        }
    }
}
