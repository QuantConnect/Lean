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
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class OrderJsonConverterTests
    {
        [Test]
        public void DeserializesMarketOrder()
        {
            var expected = new MarketOrder(Symbols.SPY, 100, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            TestOrderType(expected);
        }

        [Test]
        public void DeserializesMarketOnOpenOrder()
        {
            var expected = new MarketOnOpenOrder(Symbols.SPY, 100, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            TestOrderType(expected);
        }

        [Test]
        public void DeserializesMarketOnCloseOrder()
        {
            var expected = new MarketOnCloseOrder(Symbols.SPY, 100, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            TestOrderType(expected);
        }

        [Test]
        public void DeserializesLimitOrder()
        {
            var expected = new LimitOrder(Symbols.SPY, 100, 210.10m, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            var actual = TestOrderType(expected);

            Assert.AreEqual(expected.LimitPrice, actual.LimitPrice);
        }

        [Test]
        public void DeserializesStopMarketOrder()
        {
            var expected = new StopMarketOrder(Symbols.SPY, 100, 210.10m, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            var actual = TestOrderType(expected);

            Assert.AreEqual(expected.StopPrice, actual.StopPrice);
        }

        [Test]
        public void DeserializesStopLimitOrder()
        {
            var expected = new StopLimitOrder(Symbols.SPY, 100, 210.10m, 200.23m, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            var actual = TestOrderType(expected);

            Assert.AreEqual(expected.StopPrice, actual.StopPrice);
            Assert.AreEqual(expected.LimitPrice, actual.LimitPrice);
        }

        [Test]
        public void DeserializesOldSymbol()
        {
            const string json = @"{'Type':0,
'Value':99986.827413672,
'Id':1,
'ContingentId':0,
'BrokerId':[1],
'Symbol':{'Value':'SPY',
'Permtick':'SPY'},
'Price':100.086914328,
'Time':'2010-03-04T14:31:00Z',
'Quantity':999,
'Status':3,
'Duration':0,
'Tag':'',
'SecurityType':1,
'Direction':0,
'AbsoluteQuantity':999}";


            var order = DeserializeOrder<MarketOrder>(json);
            var actual = order.Symbol;

            Assert.AreEqual(Symbols.SPY, actual);
        }

        [Test]
        public void WorksWithJsonConvert()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = {new OrderJsonConverter()}
            };

            const string json = @"{'Type':0,
'Value':99986.827413672,
'Id':1,
'ContingentId':0,
'BrokerId':[1],
'Symbol':{'Value':'SPY',
'Permtick':'SPY'},
'Price':100.086914328,
'Time':'2010-03-04T14:31:00Z',
'Quantity':999,
'Status':3,
'Duration':0,
'Tag':'',
'SecurityType':1,
'Direction':0,
'AbsoluteQuantity':999}";

            var order = JsonConvert.DeserializeObject<Order>(json);
            Assert.IsInstanceOf<MarketOrder>(order);

        }

        private static T TestOrderType<T>(T expected)
            where T : Order
        {
            var json = JsonConvert.SerializeObject(expected);

            var actual = DeserializeOrder<T>(json);

            Assert.IsInstanceOf<T>(actual);
            Assert.AreEqual(expected.AbsoluteQuantity, actual.AbsoluteQuantity);
            CollectionAssert.AreEqual(expected.BrokerId, actual.BrokerId);
            Assert.AreEqual(expected.ContingentId, actual.ContingentId);
            Assert.AreEqual(expected.Direction, actual.Direction);
            Assert.AreEqual(expected.Duration, actual.Duration);
            Assert.AreEqual(expected.DurationValue, actual.DurationValue);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Price, actual.Price);
            Assert.AreEqual(expected.SecurityType, actual.SecurityType);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.Symbol, actual.Symbol);
            Assert.AreEqual(expected.Tag, actual.Tag);
            Assert.AreEqual(expected.Time, actual.Time);
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Quantity, actual.Quantity);

            return (T) actual;
        }

        private static Order DeserializeOrder<T>(string json) where T : Order
        {
            var converter = new OrderJsonConverter();
            var reader = new JsonTextReader(new StringReader(json));
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(converter);
            var actual = jsonSerializer.Deserialize<Order>(reader);
            return actual;
        }
    }
}
