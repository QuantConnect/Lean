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

        [TestCaseSource(nameof(camelCaseOrders))]
        public void DeserializesCamelCaseMarketOrder(string json, OrderType type, string value, string id, SecurityType securityType)
        {
            Order order = default;
            switch (type)
            {
                case OrderType.Market:
                    order = DeserializeOrder<MarketOrder>(json);
                    break;
                case OrderType.Limit:
                    order = DeserializeOrder<LimitOrder>(json);
                    Assert.AreEqual(139.240078869942m, (order as LimitOrder).LimitPrice);
                    break;
                case OrderType.StopMarket:
                    order = DeserializeOrder<StopMarketOrder>(json);
                    Assert.AreEqual(138.232948134345, (order as StopMarketOrder).StopPrice);
                    break;
                case OrderType.StopLimit:
                    order = DeserializeOrder<StopLimitOrder>(json);
                    Assert.AreEqual(139.240078869942m, (order as StopLimitOrder).LimitPrice);
                    Assert.AreEqual(138.232948134345, (order as StopLimitOrder).StopPrice);
                    Assert.AreEqual(false, (order as StopLimitOrder).StopTriggered);
                    break;
                case OrderType.MarketOnOpen:
                    order = DeserializeOrder<MarketOnOpenOrder>(json);
                    break;
                case OrderType.MarketOnClose:
                    order = DeserializeOrder<MarketOnCloseOrder>(json);
                    break;
                case OrderType.OptionExercise:
                    order = DeserializeOrder<OptionExerciseOrder>(json);
                    break;
                case OrderType.LimitIfTouched:
                    order = DeserializeOrder<LimitIfTouchedOrder>(json);
                    Assert.AreEqual(139.240078869942m, (order as LimitIfTouchedOrder).LimitPrice);
                    Assert.AreEqual(0, (order as LimitIfTouchedOrder).TriggerPrice);
                    Assert.AreEqual(false, (order as LimitIfTouchedOrder).TriggerTouched);
                    break;
                case OrderType.ComboMarket:
                    order = DeserializeOrder<ComboMarketOrder>(json);
                    break;
                case OrderType.ComboLimit:
                    order = DeserializeOrder<ComboLimitOrder>(json);
                    break;
                case OrderType.ComboLegLimit:
                    order = DeserializeOrder<ComboLegLimitOrder>(json);
                    Assert.AreEqual(139.240078869942m, (order as ComboLegLimitOrder).LimitPrice);
                    break;
                case OrderType.TrailingStop:
                    order = DeserializeOrder<TrailingStopOrder>(json);
                    Assert.AreEqual(138.232948134345m, (order as TrailingStopOrder).StopPrice);
                    break;
                default:
                    throw new Exception($"Unknown order type, {type}");
                    break;
            }

            Assert.AreEqual(1, order.Id, "Failed in Order.Id");
            Assert.AreEqual(0, order.ContingentId, "Failed in Order.ContingentId");
            Assert.AreEqual(new List<string>() { "1" }, order.BrokerId, "Failed in Order.BrokerId");
            Assert.AreEqual(id, order.Symbol.ID.ToString(), "Failed in Order.ID.Symbol");
            Assert.AreEqual(value, order.Symbol.Value, "Failed in Order.Symbol.Value");
            Assert.AreEqual(Market.USA, order.Symbol.ID.Market, "Failed in Order.Symbol.ID.Market");
            Assert.AreEqual(138.513986945m, order.Price, "Failed in Order.Price");
            Assert.AreEqual("USD", order.PriceCurrency, "Failed in Order.PriceCurrency");
            Assert.AreEqual(new DateTime(2013, 10, 7, 13, 31, 00), order.Time.RoundDown(TimeSpan.FromSeconds(1)), "Failed in Order.Time");
            //Assert.AreEqual(new DateTime(2013, 10, 7, 13, 31, 00), order.Time.RoundDown(TimeSpan.FromSeconds(1)));
            Assert.AreEqual(10, order.Quantity, "Failed in Order.Quantity");
            Assert.AreEqual(OrderStatus.Submitted, order.Status, "Failed in Order.Status");
            Assert.AreEqual(TimeInForce.GoodTilCanceled.ToString(), order.Properties.TimeInForce.ToString(), "Failed in Order.Properties.TimeInForce");
            Assert.AreEqual(securityType, order.SecurityType, "Failed in Order.SecurityType");
            Assert.AreEqual(OrderDirection.Buy, order.Direction, "Failed in Order.Direction");
            Assert.AreEqual(1385.139869450m, order.Value, "Failed in Order.Value");
            Assert.AreEqual(138.505714984m, order.OrderSubmissionData.BidPrice, "Failed in Order.OrderSubmissionData.BidPrice");
            Assert.AreEqual(138.513986945m, order.OrderSubmissionData.AskPrice, "Failed in Order.OrderSubmissionData.AskPrice");
            Assert.AreEqual(138.505714984m, order.OrderSubmissionData.LastPrice, "Failed in Order.OrderSubmissionData.LastPrice");
            //Assert.IsTrue(order.IsMarketable, "Failed in Order.IsMarketable");
            Assert.AreEqual(DataNormalizationMode.Adjusted, order.PriceAdjustmentMode, "Failed in Order.PriceAdjustmentMode");
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

        public static object[] camelCaseOrders =
        {
            new object[] { @"{
            ""type"": 0,
            ""id"": 1,
            ""contingentId"": 0,
            ""brokerId"": [
                ""1""
            ],
            ""symbol"": {
                ""value"": ""SPY"",
                ""id"": ""SPY R735QTJ8XC9X"",
                ""permtick"": ""SPY""
            },
            ""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
            ""events"": [
                {
                    ""id"": ""3b2259c444e04c9124784bb491bf016f-1-1"",
                    ""algorithmId"": ""3b2259c444e04c9124784bb491bf016f"",
                    ""orderId"": 1,
                    ""orderEventId"": 1,
                    ""symbol"": ""SPY R735QTJ8XC9X"",
                    ""symbolValue"": ""SPY"",
                    ""symbolPermtick"": ""SPY"",
                    ""time"": 1381152660.0,
                    ""status"": ""submitted"",
                    ""fillPrice"": 0.0,
                    ""fillPriceCurrency"": ""USD"",
                    ""fillQuantity"": 0.0,
                    ""direction"": ""buy"",
                    ""message"": null,
                    ""isAssignment"": false,
                    ""quantity"": 10.0
                }
            ]
        }", OrderType.Market, "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { @"{
			""limitPrice"": 139.240078869942,
			""type"": 1,
			""id"": 1,
			""contingentId"": 0,
			""brokerId"": [
				""1""
			],
			""symbol"": {
				""value"": ""SPY"",
				""id"": ""SPY R735QTJ8XC9X"",
				""permtick"": ""SPY""
			},
			""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
			""events"": [
				{
					""id"": ""3b2259c444e04c9124784bb491bf016f-4-1"",
					""algorithmId"": ""3b2259c444e04c9124784bb491bf016f"",
					""orderId"": 4,
					""orderEventId"": 1,
					""symbol"": ""SPY R735QTJ8XC9X"",
					""symbolValue"": ""SPY"",
					""symbolPermtick"": ""SPY"",
					""time"": 1381161600.0,
					""status"": ""submitted"",
					""fillPrice"": 0.0,
					""fillPriceCurrency"": ""USD"",
					""fillQuantity"": 0.0,
					""direction"": ""sell"",
					""message"": null,
					""isAssignment"": false,
					""quantity"": -10.0,
					""limitPrice"": 139.290078869942
				}
			]
		}", OrderType.Limit, "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { @"{
			""stopPrice"": 138.232948134345,
			""type"": 2,
			""id"": 1,
			""contingentId"": 0,
			""brokerId"": [
				""1""
			],
			""symbol"": {
				""value"": ""SPY"",
				""id"": ""SPY R735QTJ8XC9X"",
				""permtick"": ""SPY""
			},
			""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
			""events"": [
				{
					""id"": ""3b2259c444e04c9124784bb491bf016f-8-1"",
					""algorithmId"": ""3b2259c444e04c9124784bb491bf016f"",
					""orderId"": 8,
					""orderEventId"": 1,
					""symbol"": ""SPY R735QTJ8XC9X"",
					""symbolValue"": ""SPY"",
					""symbolPermtick"": ""SPY"",
					""time"": 1381176000.0,
					""status"": ""submitted"",
					""fillPrice"": 0.0,
					""fillPriceCurrency"": ""USD"",
					""fillQuantity"": 0.0,
					""direction"": ""sell"",
					""message"": null,
					""isAssignment"": false,
					""quantity"": -10.0,
					""stopPrice"": 138.142948134345
				}
			]
		}", OrderType.StopMarket, "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { @"{
			""stopPrice"": 138.232948134345,
			""stopTriggered"": false,
			""limitPrice"": 139.240078869942,
			""type"": 3,
			""id"": 1,
			""contingentId"": 0,
			""brokerId"": [
				""1""
			],
			""symbol"": {
				""value"": ""SPY"",
				""id"": ""SPY R735QTJ8XC9X"",
				""permtick"": ""SPY""
			},
			""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""tag"": ""Update message"",
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
			""events"": [
				{
					""id"": ""3b2259c444e04c9124784bb491bf016f-10-1"",
					""algorithmId"": ""3b2259c444e04c9124784bb491bf016f"",
					""orderId"": 10,
					""orderEventId"": 1,
					""symbol"": ""SPY R735QTJ8XC9X"",
					""symbolValue"": ""SPY"",
					""symbolPermtick"": ""SPY"",
					""time"": 1381248060.0,
					""status"": ""submitted"",
					""fillPrice"": 0.0,
					""fillPriceCurrency"": ""USD"",
					""fillQuantity"": 0.0,
					""direction"": ""sell"",
					""message"": null,
					""isAssignment"": false,
					""quantity"": -10.0,
					""stopPrice"": 137.367302895297,
					""limitPrice"": 137.534807703
				}
			]
		}", OrderType.StopLimit, "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { @"{
			""type"": 4,
			""id"": 1,
			""contingentId"": 0,
			""brokerId"": [
				""1""
			],
			""symbol"": {
				""value"": ""SPY"",
				""id"": ""SPY R735QTJ8XC9X"",
				""permtick"": ""SPY""
			},
			""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
			""events"": [
				{
					""id"": ""3b2259c444e04c9124784bb491bf016f-11-1"",
					""algorithmId"": ""3b2259c444e04c9124784bb491bf016f"",
					""orderId"": 11,
					""orderEventId"": 1,
					""symbol"": ""SPY R735QTJ8XC9X"",
					""symbolValue"": ""SPY"",
					""symbolPermtick"": ""SPY"",
					""time"": 1381255200.0,
					""status"": ""submitted"",
					""fillPrice"": 0.0,
					""fillPriceCurrency"": ""USD"",
					""fillQuantity"": 0.0,
					""direction"": ""buy"",
					""message"": null,
					""isAssignment"": false,
					""quantity"": 50.0
				}
			]
		}", OrderType.MarketOnOpen, "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { @"{
			""type"": 5,
			""id"": 1,
			""contingentId"": 0,
			""brokerId"": [
				""1""
			],
			""symbol"": {
				""value"": ""SPY"",
				""id"": ""SPY R735QTJ8XC9X"",
				""permtick"": ""SPY""
			},
			""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
			""events"": [
				{
					""id"": ""3b2259c444e04c9124784bb491bf016f-12-1"",
					""algorithmId"": ""3b2259c444e04c9124784bb491bf016f"",
					""orderId"": 12,
					""orderEventId"": 1,
					""symbol"": ""SPY R735QTJ8XC9X"",
					""symbolValue"": ""SPY"",
					""symbolPermtick"": ""SPY"",
					""time"": 1381334400.0,
					""status"": ""submitted"",
					""fillPrice"": 0.0,
					""fillPriceCurrency"": ""USD"",
					""fillQuantity"": 0.0,
					""direction"": ""buy"",
					""message"": null,
					""isAssignment"": false,
					""quantity"": 104.0
				}
			]
		}", OrderType.MarketOnClose, "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { @"{
            ""type"": 6,
            ""id"": 1,
            ""contingentId"": 0,
            ""brokerId"": [
                ""1""
            ],
            ""symbol"": {
                ""value"": ""AAPL  140613P00660000"",
                ""id"": ""AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X"",
                ""permtick"": ""AAPL  140613P00660000"",
                ""underlying"": {
                    ""value"": ""AAPL"",
                    ""id"": ""AAPL R735QTJ8XC9X"",
                    ""permtick"": ""AAPL""
                }
            },
            ""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
            ""Events"": [
                {
                    ""id"": ""1a9ec93f4763e28d2ec1ae34dd3ce47d-3-2"",
                    ""algorithmId"": ""1a9ec93f4763e28d2ec1ae34dd3ce47d"",
                    ""orderId"": 3,
                    ""orderEventId"": 2,
                    ""symbol"": ""AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X"",
                    ""symbolValue"": ""AAPL  140613P00660000"",
                    ""symbolPermtick"": ""AAPL"",
                    ""time"": 1402061460.0,
                    ""status"": ""filled"",
                    ""fillPrice"": 0.0,
                    ""fillPriceCurrency"": """",
                    ""fillQuantity"": -20.0,
                    ""direction"": ""sell"",
                    ""message"": ""Automatic Exercise. Underlying: 649.3400"",
                    ""isAssignment"": false,
                    ""quantity"": -20.0,
                    ""isInTheMoney"": true
                }
            ]
        }", OrderType.OptionExercise, "AAPL  140613P00660000", "AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X", SecurityType.Option },
            new object[] { @"{
			""type"": 7,
			""triggerPrice"": 138.26,
			""limitPrice"": 139.240078869942,
			""triggerTouched"": false,
			""id"": 1,
			""contingentId"": 0,
			""brokerId"": [
				""1""
			],
			""symbol"": {
				""value"": ""SPY"",
				""id"": ""SPY R735QTJ8XC9X"",
				""permtick"": ""SPY""
			},
			""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
			""events"": [
				{
					""id"": ""4c4c10b22ca562d9419c869abd23bfaf-1-1"",
					""algorithmId"": ""4c4c10b22ca562d9419c869abd23bfaf"",
					""orderId"": 1,
					""orderEventId"": 1,
					""symbol"": ""SPY R735QTJ8XC9X"",
					""symbolValue"": ""SPY"",
					""symbolPermtick"": ""SPY"",
					""time"": 1381152660.0,
					""status"": ""submitted"",
					""fillPrice"": 0.0,
					""fillPriceCurrency"": ""USD"",
					""fillQuantity"": 0.0,
					""direction"": ""buy"",
					""message"": null,
					""isAssignment"": false,
					""quantity"": 10.0,
					""limitPrice"": 137.505714984
				}
			]
		}", OrderType.LimitIfTouched, "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { @"{
            ""type"": 8,
            ""quantity"": 10.0,
            ""id"": 1,
            ""contingentId"": 0,
            ""brokerId"": [
                ""1""
            ],
            ""symbol"": {
                ""value"": ""GOOG  160115C00750000"",
                ""id"": ""GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"",
                ""permtick"": ""GOOG  160115C00750000"",
                ""Underlying"": {
                    ""value"": ""GOOG"",
                    ""id"": ""GOOCV VP83T1ZUHROL"",
                    ""permtick"": ""GOOG""
                }
            },
            ""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
            ""groupOrderManager"": {
                ""id"": 1,
                ""quantity"": 10.0,
                ""count"": 3,
                ""limitPrice"": 0.0,
                ""orderIds"": [
                    1,
                    2,
                    3
                ],
                ""direction"": 0
            },
            ""events"": [
                {
                    ""id"": ""c628892c0f508fd780013e01383f1c4e-3-1"",
                    ""algorithmId"": ""c628892c0f508fd780013e01383f1c4e"",
                    ""orderId"": 3,
                    ""orderEventId"": 1,
                    ""symbol"": ""GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"",
                    ""symbolValue"": ""GOOG  160115C00750000"",
                    ""symbolPermtick"": ""GOOCV"",
                    ""time"": 1450967460.0,
                    ""status"": ""submitted"",
                    ""fillPrice"": 0.0,
                    ""fillPriceCurrency"": ""USD"",
                    ""fillQuantity"": 0.0,
                    ""direction"": ""buy"",
                    ""message"": null,
                    ""isAssignment"": false,
                    ""quantity"": 10.0
                }
            ]
        }", OrderType.ComboMarket, "GOOCV 160115C00750000", "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { @"{
            ""type"": 9,
            ""quantity"": 10.0,
            ""id"": 1,
            ""contingentId"": 0,
            ""brokerId"": [
                ""1""
            ],
            ""symbol"": {
                ""value"": ""GOOG  160115C00745000"",
                ""id"": ""GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"",
                ""permtick"": ""GOOG  160115C00745000"",
                ""Underlying"": {
                    ""value"": ""GOOG"",
                    ""id"": ""GOOCV VP83T1ZUHROL"",
                    ""permtick"": ""GOOG""
                }
            },
            ""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
            ""groupOrderManager"": {
                ""id"": 1,
                ""quantity"": 10.0,
                ""count"": 3,
                ""limitPrice"": 1.9,
                ""orderIds"": [
                    1,
                    2,
                    3
                ],
                ""direction"": 0
            },
            ""events"": [
                {
                    ""id"": ""02162a310244a08034bcbcd571f5aec9-1-1"",
                    ""algorithmId"": ""02162a310244a08034bcbcd571f5aec9"",
                    ""orderId"": 1,
                    ""orderEventId"": 1,
                    ""symbol"": ""GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"",
                    ""symbolValue"": ""GOOG  160115C00745000"",
                    ""symbolPermtick"": ""GOOCV"",
                    ""time"": 1450967460.0,
                    ""status"": ""submitted"",
                    ""fillPrice"": 0.0,
                    ""fillPriceCurrency"": ""USD"",
                    ""fillQuantity"": 0.0,
                    ""direction"": ""buy"",
                    ""message"": null,
                    ""isAssignment"": false,
                    ""quantity"": 100.0
                }
            ]
        }", OrderType.ComboLimit, "GOOCV 160115C00745000", "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { @"{
            ""type"": 10,
            ""limitPrice"": 139.240078869942,
            ""quantity"": 10.0,
            ""id"": 1,
            ""contingentId"": 0,
            ""brokerId"": [
                ""1""
            ],
            ""symbol"": {
                ""value"": ""GOOG  160115C00750000"",
                ""id"": ""GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"",
                ""permtick"": ""GOOG  160115C00750000"",
                ""Underlying"": {
                    ""value"": ""GOOG"",
                    ""id"": ""GOOCV VP83T1ZUHROL"",
                    ""permtick"": ""GOOG""
                }
            },
            ""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
            ""groupOrderManager"": {
                ""id"": 1,
                ""quantity"": 10.0,
                ""count"": 3,
                ""limitPrice"": 0.0,
                ""orderIds"": [
                    1,
                    2,
                    3
                ],
                ""direction"": 0
            },
            ""events"": [
                {
                    ""id"": ""a19c5b42ef28e3db679bb6fd59c14984-3-1"",
                    ""algorithmId"": ""a19c5b42ef28e3db679bb6fd59c14984"",
                    ""orderId"": 3,
                    ""orderEventId"": 1,
                    ""symbol"": ""GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"",
                    ""symbolValue"": ""GOOG  160115C00750000"",
                    ""symbolPermtick"": ""GOOCV"",
                    ""time"": 1450967460.0,
                    ""status"": ""submitted"",
                    ""fillPrice"": 0.0,
                    ""fillPriceCurrency"": ""USD"",
                    ""fillQuantity"": 0.0,
                    ""direction"": ""buy"",
                    ""message"": null,
                    ""isAssignment"": false,
                    ""quantity"": 10.0,
                    ""limitPrice"": 28.0
                }
            ]
        }", OrderType.ComboLegLimit, "GOOCV 160115C00750000", "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { @"{
			""trailingAmount"": 0.0019,
			""trailingAsPercentage"": true,
			""type"": 11,
			""stopPrice"": 138.232948134345,
			""id"": 1,
			""contingentId"": 0,
			""brokerId"": [
				""1""
			],
			""symbol"": {
				""value"": ""SPY"",
				""id"": ""SPY R735QTJ8XC9X"",
				""permtick"": ""SPY""
			},
			""price"": 138.513986945,
            ""priceCurrency"": ""USD"",
            ""time"": ""2013-10-07T13:31:00Z"",
            ""createdTime"": ""2013-10-07T13:31:00Z"",
            ""lastFillTime"": ""2013-10-07T13:31:00Z"",
            ""quantity"": 10.0,
            ""status"": 1,
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 1,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
            ""priceAdjustmentMode"": 1,
			""events"": [
				{
					""id"": ""3b2259c444e04c9124784bb491bf016f-6-1"",
					""algorithmId"": ""3b2259c444e04c9124784bb491bf016f"",
					""orderId"": 6,
					""orderEventId"": 1,
					""symbol"": ""SPY R735QTJ8XC9X"",
					""symbolValue"": ""SPY"",
					""symbolPermtick"": ""SPY"",
					""time"": 1381161600.0,
					""status"": ""submitted"",
					""fillPrice"": 0.0,
					""fillPriceCurrency"": ""USD"",
					""fillQuantity"": 0.0,
					""direction"": ""sell"",
					""message"": null,
					""isAssignment"": false,
					""quantity"": -10.0,
					""stopPrice"": 138.803050622145
				}
			]
		}", OrderType.TrailingStop, "SPY", "SPY R735QTJ8XC9X", SecurityType.Equity }
        };
    }
}
