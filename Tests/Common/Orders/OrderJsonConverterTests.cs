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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class OrderJsonConverterTests
    {

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesMarketOrder(Symbols.SymbolsKey key)
        {
            var expected = new MarketOrder(Symbols.Lookup(key), 100, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            TestOrderType(expected);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesComboMarketOrder(Symbols.SymbolsKey key)
        {
            var groupOrderManager = new GroupOrderManager(1, 2, 10);
            groupOrderManager.OrderIds.Add(12345);
            groupOrderManager.OrderIds.Add(12346);

            var expected = new ComboMarketOrder(Symbols.Lookup(key), 100, new DateTime(2015, 11, 23, 17, 15, 37), groupOrderManager, "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123457,
                BrokerId = new List<string> { "727", "54970" }
            };

            TestOrderType(expected);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesComboLimitOrder(Symbols.SymbolsKey key)
        {
            var groupOrderManager = new GroupOrderManager(1, 2, 10);
            groupOrderManager.OrderIds.Add(12345);
            groupOrderManager.OrderIds.Add(12346);
            groupOrderManager.LimitPrice = 201.1m;

            var expected = new ComboLimitOrder(Symbols.Lookup(key), 100, 210.1m, new DateTime(2015, 11, 23, 17, 15, 37), groupOrderManager, "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123457,
                BrokerId = new List<string> { "727", "54970" }
            };

            TestOrderType(expected);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesComboLegLimitOrder(Symbols.SymbolsKey key)
        {
            var groupOrderManager = new GroupOrderManager(1, 2, 10);
            groupOrderManager.OrderIds.Add(12345);
            groupOrderManager.OrderIds.Add(12346);

            var expected = new ComboLegLimitOrder(Symbols.Lookup(key), 100, 210.1m, new DateTime(2015, 11, 23, 17, 15, 37), groupOrderManager, "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123457,
                BrokerId = new List<string> { "727", "54970" }
            };

            TestOrderType(expected);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesMarketOnOpenOrder(Symbols.SymbolsKey key)
        {
            var expected = new MarketOnOpenOrder(Symbols.Lookup(key), 100, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            TestOrderType(expected);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesMarketOnCloseOrder(Symbols.SymbolsKey key)
        {
            var expected = new MarketOnCloseOrder(Symbols.Lookup(key), 100, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            TestOrderType(expected);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesLimitOrder(Symbols.SymbolsKey key)
        {
            var expected = new LimitOrder(Symbols.Lookup(key), 100, 210.10m, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            var actual = TestOrderType(expected);

            Assert.AreEqual(expected.LimitPrice, actual.LimitPrice);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesStopMarketOrder(Symbols.SymbolsKey key)
        {
            var expected = new StopMarketOrder(Symbols.Lookup(key), 100, 210.10m, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            var actual = TestOrderType(expected);

            Assert.AreEqual(expected.StopPrice, actual.StopPrice);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesTrailingStopOrder(Symbols.SymbolsKey key)
        {
            var expected = new TrailingStopOrder(Symbols.Lookup(key), 100, 210.10m, 0.1m, true, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> { "727", "54970" }
            };

            var actual = TestOrderType(expected);

            Assert.AreEqual(expected.StopPrice, actual.StopPrice);
            Assert.AreEqual(expected.TrailingAmount, actual.TrailingAmount);
            Assert.AreEqual(expected.TrailingAsPercentage, actual.TrailingAsPercentage);
        }

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesStopLimitOrder(Symbols.SymbolsKey key)
        {
            var expected = new StopLimitOrder(Symbols.Lookup(key), 100, 210.10m, 200.23m, new DateTime(2015, 11, 23, 17, 15, 37), "now")
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

        [TestCase(Symbols.SymbolsKey.SPY)]
        [TestCase(Symbols.SymbolsKey.EURUSD)]
        [TestCase(Symbols.SymbolsKey.BTCUSD)]
        public void DeserializesLimitIfTouchedOrder(Symbols.SymbolsKey key)
        {
            var expected = new LimitIfTouchedOrder(Symbols.Lookup(key), 100, 210.10m, 200.23m, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                Price = 209.03m,
                ContingentId = 123456,
                BrokerId = new List<string> {"727", "54970"}
            };

            var actual = TestOrderType(expected);

            Assert.AreEqual(expected.TriggerPrice, actual.TriggerPrice);
            Assert.AreEqual(expected.LimitPrice, actual.LimitPrice);
        }

        [Test]
        public void DeserializesOptionExpireOrder()
        {
            var expected = new OptionExerciseOrder(Symbols.SPY_P_192_Feb19_2016, 100, new DateTime(2015, 11, 23, 17, 15, 37), "now")
            {
                Id = 12345,
                ContingentId = 123456,
                BrokerId = new List<string> { "727", "54970" }
            };

            // Note: Order price equals strike price found in Symbol object
            // Price = Symbol.ID.StrikePrice

            TestOrderType(expected);
        }

        [Test]
        public void DeserializesNullLastFillTimeAndLastUpdateTime()
        {
            const string json = @"{
    'Type': 4,
    'Id': 1,
    'ContingentId': 0,
    'BrokerId': [
        '1'
    ],
    'Symbol': {
        'Value': 'SPY',
        'ID': 'SPY R735QTJ8XC9X',
        'Permtick': 'SPY'
    },
    'Price': 321.66,
    'PriceCurrency': 'USD',
    'Time': '2019-12-24T14:31:00Z',
    'CreatedTime': '2019-12-24T14:31:00Z',
    'LastUpdateTime': '2019-12-25T14:31:00Z',
    'LastFillTime': null,
    'Quantity': 1.0,
    'Status': 3,
    'TimeInForce': {},
    'Tag': '',
    'Properties': {
        'TimeInForce': {}
    },
    'SecurityType': 1,
    'Direction': 0,
    'AbsoluteQuantity': 1.0,
    'Value': 321.66,
    'OrderSubmissionData': {
        'BidPrice': 321.4700,
        'AskPrice': 321.4700,
        'LastPrice': 321.4700
    },
    'IsMarketable': false
}";

            const string json2 = @"{
    'Type': 4,
    'Id': 1,
    'ContingentId': 0,
    'BrokerId': [
        '1'
    ],
    'Symbol': {
        'Value': 'SPY',
        'ID': 'SPY R735QTJ8XC9X',
        'Permtick': 'SPY'
    },
    'Price': 321.66,
    'PriceCurrency': 'USD',
    'Time': '2019-12-24T14:31:00Z',
    'CreatedTime': '2019-12-24T14:31:00Z',
    'LastUpdateTime': null,
    'LastFillTime': '2019-12-26T14:31:00Z',
    'Quantity': 1.0,
    'Status': 3,
    'TimeInForce': {},
    'Tag': '',
    'Properties': {
        'TimeInForce': {}
    },
    'SecurityType': 1,
    'Direction': 0,
    'AbsoluteQuantity': 1.0,
    'Value': 321.66,
    'OrderSubmissionData': {
        'BidPrice': 321.4700,
        'AskPrice': 321.4700,
        'LastPrice': 321.4700
    },
    'IsMarketable': false
}";

            var time = DateTime.SpecifyKind(new DateTime(2019, 12, 24, 14, 31, 0), DateTimeKind.Utc);
            var fillTime = DateTime.SpecifyKind(new DateTime(2019, 12, 26, 14, 31, 0), DateTimeKind.Utc);
            var updateTime = DateTime.SpecifyKind(new DateTime(2019, 12, 25, 14, 31, 0), DateTimeKind.Utc);

            var expected1 = new MarketOnOpenOrder(Symbol.Create("SPY", SecurityType.Equity, Market.USA), 1m, time)
            {
                Id = 1,
                ContingentId = 0,
                BrokerId = new List<string> { "1" },
                Price = 321.66m,
                PriceCurrency = "USD",
                LastFillTime = null,
                LastUpdateTime = updateTime,
                Status = OrderStatus.Filled,
                OrderSubmissionData = new OrderSubmissionData(321.47m, 321.47m, 321.47m),
            };

            var expected2 = new MarketOnOpenOrder(Symbol.Create("SPY", SecurityType.Equity, Market.USA), 1m, time)
            {
                Id = 1,
                ContingentId = 0,
                BrokerId = new List<string> { "1" },
                Price = 321.66m,
                PriceCurrency = "USD",
                LastFillTime = updateTime,
                LastUpdateTime = null,
                Status = OrderStatus.Filled,
                OrderSubmissionData = new OrderSubmissionData(321.47m, 321.47m, 321.47m),
            };


            var actual1 = (MarketOnOpenOrder)DeserializeOrder<MarketOnOpenOrder>(json);
            var actual2 = (MarketOnOpenOrder)DeserializeOrder<MarketOnOpenOrder>(json2);

            TestOrderType(expected1);
            TestOrderType(expected2);
            TestOrderType(actual1);
            TestOrderType(actual2);
        }

        [Test]
        public void DeserializesStringStatusAndNullTime()
        {
            const string stringStatusJson = @"{
    'Type': 4,
    'Id': 1,
    'ContingentId': 0,
    'BrokerId': [
        '1'
    ],
    'Symbol': {
        'Value': 'SPY',
        'ID': 'SPY R735QTJ8XC9X',
        'Permtick': 'SPY'
    },
    'Price': 321.66,
    'PriceCurrency': 'USD',
    'Time': '2019-12-24T14:31:00Z',
    'CreatedTime': '2019-12-24T14:31:00Z',
    'LastUpdateTime': '2019-12-25T14:31:00Z',
    'LastFillTime': '2019-12-26T14:31:00Z',
    'Quantity': 1.0,
    'Status': 'filled',
    'TimeInForce': {},
    'Tag': '',
    'Properties': {
        'TimeInForce': {}
    },
    'SecurityType': 1,
    'Direction': 0,
    'AbsoluteQuantity': 1.0,
    'Value': 321.66,
    'OrderSubmissionData': {
        'BidPrice': 321.4700,
        'AskPrice': 321.4700,
        'LastPrice': 321.4700
    },
    'IsMarketable': false
}";

            const string nullTimeJson = @"{
    'Type': 4,
    'Id': 1,
    'ContingentId': 0,
    'BrokerId': [
        '1'
    ],
    'Symbol': {
        'Value': 'SPY',
        'ID': 'SPY R735QTJ8XC9X',
        'Permtick': 'SPY'
    },
    'Price': 321.66,
    'PriceCurrency': 'USD',
    'Time': null,
    'CreatedTime': '2019-12-24T14:31:00Z',
    'LastUpdateTime': '2019-12-25T14:31:00Z',
    'LastFillTime': '2019-12-26T14:31:00Z',
    'Quantity': 1.0,
    'Status': 3,
    'TimeInForce': {},
    'Tag': '',
    'Properties': {
        'TimeInForce': {}
    },
    'SecurityType': 1,
    'Direction': 0,
    'AbsoluteQuantity': 1.0,
    'Value': 321.66,
    'OrderSubmissionData': {
        'BidPrice': 321.4700,
        'AskPrice': 321.4700,
        'LastPrice': 321.4700
    },
    'IsMarketable': false
}";

            var time = DateTime.SpecifyKind(new DateTime(2019, 12, 24, 14, 31, 0), DateTimeKind.Utc);
            var fillTime = DateTime.SpecifyKind(new DateTime(2019, 12, 26, 14, 31, 0), DateTimeKind.Utc);
            var updateTime = DateTime.SpecifyKind(new DateTime(2019, 12, 25, 14, 31, 0), DateTimeKind.Utc);

            var expected1 = new MarketOnOpenOrder(Symbol.Create("SPY", SecurityType.Equity, Market.USA), 1m, time)
            {
                Id = 1,
                ContingentId = 0,
                BrokerId = new List<string> { "1" },
                Price = 321.66m,
                PriceCurrency = "USD",
                LastFillTime = fillTime,
                LastUpdateTime = updateTime,
                Status = OrderStatus.Filled,
                OrderSubmissionData = new OrderSubmissionData(321.47m, 321.47m, 321.47m),
            };

            var expected2 = new MarketOnOpenOrder(Symbol.Create("SPY", SecurityType.Equity, Market.USA), 1m, time)
            {
                Id = 1,
                ContingentId = 0,
                BrokerId = new List<string> { "1" },
                Price = 321.66m,
                PriceCurrency = "USD",
                LastFillTime = fillTime,
                LastUpdateTime = updateTime,
                Status = OrderStatus.Filled,
                OrderSubmissionData = new OrderSubmissionData(321.47m, 321.47m, 321.47m),
            };


            var actual1 = (MarketOnOpenOrder)DeserializeOrder<MarketOnOpenOrder>(stringStatusJson);
            var actual2 = (MarketOnOpenOrder)DeserializeOrder<MarketOnOpenOrder>(nullTimeJson);

            TestOrderType(expected1);
            TestOrderType(expected2);
            TestOrderType(actual1);
            TestOrderType(actual2);
        }

        [TestCase("Day")]
        [TestCase("GoodTilCanceled")]
        [TestCase("GoodTilDate")]
        public void RoundTripUsingJsonConverter(string  timeInForceStr)
        {
            TimeInForce timeInForce = null;
            switch (timeInForceStr)
            {
                case "Day":
                    timeInForce = TimeInForce.Day;
                    break;
                case "GoodTilCanceled":
                    timeInForce = TimeInForce.GoodTilCanceled;
                    break;
                case "GoodTilDate":
                    timeInForce = TimeInForce.GoodTilDate(DateTime.UtcNow);
                    break;
            }
            var expected = new MarketOnOpenOrder(Symbol.Create("SPY", SecurityType.Equity, Market.USA), 1m, DateTime.UtcNow)
            {
                Id = 1,
                ContingentId = 0,
                BrokerId = new List<string> { "1" },
                Price = 321.66m,
                PriceCurrency = "USD",
                LastFillTime = DateTime.UtcNow,
                LastUpdateTime = DateTime.UtcNow,
                CanceledTime = DateTime.UtcNow,
                Status = OrderStatus.Filled,
                OrderSubmissionData = new OrderSubmissionData(321.47m, 321.48m, 321.49m),
                Properties = { TimeInForce = timeInForce },
                PriceAdjustmentMode = DataNormalizationMode.Adjusted
            };

            var converter = new OrderJsonConverter();
            var serialized = JsonConvert.SerializeObject(expected, converter);
            var actual = JsonConvert.DeserializeObject<Order>(serialized, converter);

            CollectionAssert.AreEqual(expected.BrokerId, actual.BrokerId);
            Assert.AreEqual(expected.ContingentId, actual.ContingentId);
            Assert.AreEqual(expected.Direction, actual.Direction);
            Assert.AreEqual(expected.TimeInForce.GetType(), actual.TimeInForce.GetType());
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Price, actual.Price);
            Assert.AreEqual(expected.PriceCurrency, actual.PriceCurrency);
            Assert.AreEqual(expected.SecurityType, actual.SecurityType);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.Symbol, actual.Symbol);
            Assert.AreEqual(expected.Tag, actual.Tag);
            Assert.AreEqual(expected.Time, actual.Time);
            Assert.AreEqual(expected.CreatedTime, actual.CreatedTime);
            Assert.AreEqual(expected.LastFillTime, actual.LastFillTime);
            Assert.AreEqual(expected.LastUpdateTime, actual.LastUpdateTime);
            Assert.AreEqual(expected.CanceledTime, actual.CanceledTime);
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Quantity, actual.Quantity);
            Assert.AreEqual(expected.TimeInForce.GetType(), actual.TimeInForce.GetType());
            Assert.AreEqual(expected.Symbol.ID.Market, actual.Symbol.ID.Market);
            Assert.AreEqual(expected.OrderSubmissionData.AskPrice, actual.OrderSubmissionData.AskPrice);
            Assert.AreEqual(expected.OrderSubmissionData.BidPrice, actual.OrderSubmissionData.BidPrice);
            Assert.AreEqual(expected.OrderSubmissionData.LastPrice, actual.OrderSubmissionData.LastPrice);
            Assert.AreEqual(expected.PriceAdjustmentMode, actual.PriceAdjustmentMode);
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
'TimeInForce':0,
'Tag':'',
'SecurityType':1,
'Direction':0,
'AbsoluteQuantity':999}";


            var order = DeserializeOrder<MarketOrder>(json);
            var actual = order.Symbol;

            Assert.AreEqual(Symbols.SPY, actual);
            Assert.AreEqual(Market.USA, actual.ID.Market);
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
'TimeInForce':1,
'Tag':'',
'SecurityType':1,
'Direction':0,
'AbsoluteQuantity':999}";

            var order = JsonConvert.DeserializeObject<Order>(json);
            Assert.IsInstanceOf<MarketOrder>(order);
            Assert.AreEqual(Market.USA, order.Symbol.ID.Market);
            Assert.IsTrue(order.TimeInForce is DayTimeInForce);
        }

        [Test]
        public void DeserializesOldDurationProperty()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };

            // The Duration property has been renamed to TimeInForce,
            // we still want to deserialize old JSON files containing Duration.
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
'Duration':1,
'Tag':'',
'SecurityType':1,
'Direction':0,
'AbsoluteQuantity':999}";

            var order = JsonConvert.DeserializeObject<Order>(json);
            Assert.IsInstanceOf<MarketOrder>(order);
            Assert.AreEqual(Market.USA, order.Symbol.ID.Market);
            Assert.IsTrue(order.TimeInForce is DayTimeInForce);
        }

        [Test]
        public void DeserializesOldDurationValueProperty()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };

            // The DurationValue property has been moved to GoodTilDateTimeInforce.Expiry,
            // we still want to deserialize old JSON files containing Duration.
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
'Duration':2,
'DurationValue':'2010-04-04T14:31:00Z',
'Tag':'',
'SecurityType':1,
'Direction':0,
'AbsoluteQuantity':999}";

            var order = JsonConvert.DeserializeObject<Order>(json);
            Assert.IsInstanceOf<MarketOrder>(order);
            Assert.AreEqual(Market.USA, order.Symbol.ID.Market);
            Assert.IsTrue(order.TimeInForce is GoodTilDateTimeInForce);

            var timeInForce = (GoodTilDateTimeInForce)order.TimeInForce;
            Assert.AreEqual(new DateTime(2010, 4, 4, 14, 31, 0), timeInForce.Expiry);
        }

        [Test]
        public void DeserializesDecimalizedQuantity()
        {
            var expected = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.Today);
            TestOrderType(expected);
        }

        [Test]
        public void DeserializesOrderGoodTilCanceledTimeInForce()
        {
            var orderProperties = new OrderProperties { TimeInForce = TimeInForce.GoodTilCanceled };
            var expected = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.Today, "", orderProperties);
            TestOrderType(expected);
        }

        [Test]
        public void DeserializesOrderDayTimeInForce()
        {
            var orderProperties = new OrderProperties { TimeInForce = TimeInForce.Day };
            var expected = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.Today, "", orderProperties);
            TestOrderType(expected);
        }

        [Test]
        public void DeserializesOrderGoodTilDateTimeInForce()
        {
            var expiry = new DateTime(2018, 5, 26);
            var orderProperties = new OrderProperties { TimeInForce = TimeInForce.GoodTilDate(expiry) };
            var expected = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.Today, "", orderProperties);
            TestOrderType(expected);

            var json = JsonConvert.SerializeObject(expected);
            var actual = DeserializeOrder<MarketOrder>(json);

            var gtd = (GoodTilDateTimeInForce)actual.Properties.TimeInForce;
            Assert.AreEqual(expiry, gtd.Expiry);
        }

        [Test]
        public void JsonIgnores()
        {
            var json = JsonConvert.SerializeObject(new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.Today));

            Assert.IsFalse(json.Contains("Tag"));
            Assert.IsFalse(json.Contains("AbsoluteQuantity"));

            json = JsonConvert.SerializeObject(new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.Today, "This is a Tag"));

            Assert.IsTrue(json.Contains("Tag"));
            Assert.IsTrue(json.Contains("This is a Tag"));
        }

        [Test]
        public void TimeInForceInProperties()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
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
'Properties': {
    'TimeInForce': 1
},
'Tag':'',
'SecurityType':1,
'Direction':0,
'AbsoluteQuantity':999}";

            var order = JsonConvert.DeserializeObject<Order>(json);
            Assert.IsInstanceOf<MarketOrder>(order);
            Assert.AreEqual(Market.USA, order.Symbol.ID.Market);
            Assert.IsTrue(order.TimeInForce is DayTimeInForce);
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
            Assert.AreEqual(expected.TimeInForce.GetType(), actual.TimeInForce.GetType());
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Price, actual.Price);
            Assert.AreEqual(expected.SecurityType, actual.SecurityType);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.Symbol, actual.Symbol);
            Assert.AreEqual(expected.Tag, actual.Tag);
            Assert.AreEqual(expected.Time, actual.Time);
            Assert.AreEqual(expected.LastFillTime, actual.LastFillTime);
            Assert.AreEqual(expected.LastUpdateTime, actual.LastUpdateTime);
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Quantity, actual.Quantity);
            Assert.AreEqual(expected.Symbol.ID.Market, actual.Symbol.ID.Market);

            TestGroupOrderManager(expected.GroupOrderManager, actual.GroupOrderManager);

            return (T) actual;
        }

        private static void TestGroupOrderManager(GroupOrderManager expected, GroupOrderManager actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Quantity, actual.Quantity);
            Assert.AreEqual(expected.Count, actual.Count);
            Assert.AreEqual(expected.LimitPrice, actual.LimitPrice);
            Assert.AreEqual(expected.Direction, actual.Direction);
            CollectionAssert.AreEqual(expected.OrderIds, actual.OrderIds);
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
