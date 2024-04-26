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
using NUnit.Framework;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class ReadOrdersResponseJsonConverterTests
    {
        private JsonSerializerSettings _jsonSettings = new() { Converters = { new ReadOrdersResponseJsonConverter()} };

        [TestCaseSource(nameof(DeserializeOrdersTests))]
        public void DeserializesCamelAndCapitalCaseOrders(string json, OrderType expectedType, string id, SecurityType securityType)
        {
            var apiOrderResponse = JsonConvert.DeserializeObject<ApiOrderResponse>(json, _jsonSettings);
            var order = apiOrderResponse.Order;
            var actualType = order.Type;
            Assert.AreEqual(expectedType, actualType);

            switch (actualType)
            {
                case OrderType.Market:
                    Assert.IsTrue(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.Limit:
                    Assert.AreEqual(139.240078869942m, (order as LimitOrder).LimitPrice);
                    Assert.IsTrue(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.StopMarket:
                    Assert.AreEqual(138.232948134345, (order as StopMarketOrder).StopPrice);
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.StopLimit:
                    Assert.AreEqual(139.240078869942m, (order as StopLimitOrder).LimitPrice);
                    Assert.AreEqual(138.232948134345, (order as StopLimitOrder).StopPrice);
                    Assert.AreEqual(false, (order as StopLimitOrder).StopTriggered);
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.MarketOnOpen:
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.MarketOnClose:
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.OptionExercise:
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.LimitIfTouched:
                    Assert.AreEqual(139.240078869942m, (order as LimitIfTouchedOrder).LimitPrice);
                    Assert.AreEqual(138.26, (order as LimitIfTouchedOrder).TriggerPrice);
                    Assert.AreEqual(false, (order as LimitIfTouchedOrder).TriggerTouched);
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.ComboMarket:
                    Assert.IsTrue(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.ComboLimit:
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.ComboLegLimit:
                    Assert.AreEqual(139.240078869942m, (order as ComboLegLimitOrder).LimitPrice);
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
                case OrderType.TrailingStop:
                    Assert.AreEqual(138.232948134345m, (order as TrailingStopOrder).StopPrice);
                    Assert.IsFalse(order.IsMarketable, "Failed in Order.IsMarketable");
                    break;
            }

            Assert.AreEqual(1, order.Id, "Failed in Order.Id");
            Assert.AreEqual(0, order.ContingentId, "Failed in Order.ContingentId");
            Assert.AreEqual(new List<string>() { "1" }, order.BrokerId, "Failed in Order.BrokerId");
            Assert.AreEqual(id, order.Symbol.ID.ToString(), "Failed in Order.ID.Symbol");
            Assert.AreEqual(Market.USA, order.Symbol.ID.Market, "Failed in Order.Symbol.ID.Market");
            Assert.AreEqual(138.513986945m, order.Price, "Failed in Order.Price");
            Assert.AreEqual("USD", order.PriceCurrency, "Failed in Order.PriceCurrency");
            Assert.AreEqual(new DateTime(2013, 10, 7, 13, 31, 00), order.Time.RoundDown(TimeSpan.FromSeconds(1)), "Failed in Order.Time");
            Assert.AreEqual(new DateTime(2013, 10, 7, 13, 31, 00), order.CreatedTime.RoundDown(TimeSpan.FromSeconds(1)));
            Assert.AreEqual(new DateTime(2013, 10, 7, 13, 31, 00), order.LastFillTime?.RoundDown(TimeSpan.FromSeconds(1)));
            Assert.AreEqual(10, order.Quantity, "Failed in Order.Quantity");
            Assert.AreEqual(OrderStatus.Submitted, order.Status, "Failed in Order.Status");
            Assert.AreEqual(TimeInForce.GoodTilCanceled.ToString(), order.Properties.TimeInForce.ToString(), "Failed in Order.Properties.TimeInForce");
            Assert.AreEqual(securityType, order.SecurityType, "Failed in Order.SecurityType");
            Assert.AreEqual(OrderDirection.Buy, order.Direction, "Failed in Order.Direction");
            Assert.AreEqual(1385.139869450m, order.Value, "Failed in Order.Value");
            Assert.AreEqual(138.505714984m, order.OrderSubmissionData.BidPrice, "Failed in Order.OrderSubmissionData.BidPrice");
            Assert.AreEqual(138.513986945m, order.OrderSubmissionData.AskPrice, "Failed in Order.OrderSubmissionData.AskPrice");
            Assert.AreEqual(138.505714984m, order.OrderSubmissionData.LastPrice, "Failed in Order.OrderSubmissionData.LastPrice");
            Assert.AreEqual(DataNormalizationMode.Adjusted, order.PriceAdjustmentMode, "Failed in Order.PriceAdjustmentMode");
        }

        [TestCaseSource(nameof(SerializeOrdersTests))]
        public void SerializesCamelAndCapitalCaseOrders(string json)
        {
            var order = JsonConvert.DeserializeObject<ApiOrderResponse>(json, _jsonSettings);

            var serializedOrder = JsonConvert.SerializeObject(order, _jsonSettings);
            var jsonFormat = json.Replace("\r\n            ", "").Replace(": ", ":").Replace("    ", "").Replace("\r\n", "").Replace("\t", "").Replace("\n", "");
            if (order.Order.Type == OrderType.ComboMarket || order.Order.Type == OrderType.ComboLimit || order.Order.Type == OrderType.ComboLegLimit)
            {
                jsonFormat = Regex.Replace(jsonFormat, @"\""value\"":\""(.*?)\"",", "");
                jsonFormat = Regex.Replace(jsonFormat, @"\""permtick\"":\""(.*?)\"",", "");
                serializedOrder = Regex.Replace(serializedOrder, @"\""value\"":\""(.*?)\"",", "");
                serializedOrder = Regex.Replace(serializedOrder, @"\""permtick\"":\""(.*?)\"",", "");
            }
            Assert.AreEqual(jsonFormat, serializedOrder);
            Assert.AreEqual(jsonFormat.GetHashCode(), serializedOrder.GetHashCode());
        }

        private const string _camelCaseMarketOrder = @"{
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
            ""tag"": """",
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
        }";

        private const string _camelCaseLimitOrder = @"{
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
            ""tag"": """",
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
		}";

        private const string _camelCaseStopMarket = @"{
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
            ""tag"": """",
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
            ""isMarketable"": false,
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
		}";

        private const string _camelCaseStopLimitOrder = @"{
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
            ""tag"": """",
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
            ""isMarketable"": false,
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
		}";

        private const string _camelCaseMarketOnOpen = @"{
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
            ""tag"": """",
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
            ""isMarketable"": false,
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
		}";

        private const string _camelCaseMarketOnClose = @"{
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
            ""tag"": """",
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
            ""isMarketable"": false,
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
		}";

        private const string _camelCaseOptionExercise = @"{
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
            ""tag"": """",
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 2,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": false,
            ""priceAdjustmentMode"": 1,
            ""events"": [
                {
					""id"": ""015fbee84f01918498775c6c3a08dd39-3-1"",
					""algorithmId"": ""015fbee84f01918498775c6c3a08dd39"",
					""orderId"": 3,
					""orderEventId"": 1,
					""symbol"": ""AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X"",
					""symbolValue"": ""AAPL  140613P00660000"",
					""symbolPermtick"": ""AAPL"",
					""time"": 1402061460.0,
					""status"": ""submitted"",
					""fillPrice"": 0.0,
					""fillPriceCurrency"": ""USD"",
					""fillQuantity"": 0.0,
					""direction"": ""sell"",
					""message"": null,
					""isAssignment"": false,
					""quantity"": -20.0
				}
            ]
        }";

        private const string _camelCaseLimitIfTouched = @"{
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
            ""tag"": """",
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
            ""isMarketable"": false,
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
		}";

        private const string _camelCaseComboMarket = @"{
            ""type"": 8,
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
                ""underlying"": {
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
            ""status"": 1,
            ""tag"": """",
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 2,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": true,
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
            ""priceAdjustmentMode"": 1,
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
        }";

        private const string _camelCaseComboLimit = @"{
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
                ""underlying"": {
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
            ""status"": 1,
            ""tag"": """",
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 2,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": false,
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
            ""priceAdjustmentMode"": 1,
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
        }";

        private const string _camelCaseComboLegLimit = @"{
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
                ""underlying"": {
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
            ""status"": 1,
            ""tag"": """",
            ""properties"": {
                ""timeInForce"": {}
            },
            ""securityType"": 2,
            ""direction"": 0,
            ""value"": 1385.139869450,
            ""orderSubmissionData"": {
                ""bidPrice"": 138.505714984,
                ""askPrice"": 138.513986945,
                ""lastPrice"": 138.505714984
            },
            ""isMarketable"": false,
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
            ""priceAdjustmentMode"": 1,
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
        }";

        private const string _camelCaseTrailingStop = @"{
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
            ""tag"": """",
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
            ""isMarketable"": false,
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
		}";

        private const string _capitalCaseMarketOrder = @"{
            ""Type"": 0,
            ""Id"": 1,
            ""ContingentId"": 0,
            ""BrokerId"": [
                ""1""
            ],
            ""Symbol"": {
                ""Value"": ""SPY"",
                ""ID"": ""SPY R735QTJ8XC9X"",
                ""Permtick"": ""SPY""
            },
            ""Price"": 138.513986945,
            ""PriceCurrency"": ""USD"",
            ""Time"": ""2013-10-07T13:31:00Z"",
            ""CreatedTime"": ""2013-10-07T13:31:00Z"",
            ""LastFillTime"": ""2013-10-07T13:31:00Z"",
            ""Quantity"": 10.0,
            ""Status"": 1,
            ""Tag"": """",
            ""Properties"": {
                ""TimeInForce"": {}
            },
            ""SecurityType"": 1,
            ""Direction"": 0,
            ""Value"": 1385.139869450,
            ""OrderSubmissionData"": {
                ""BidPrice"": 138.505714984,
                ""AskPrice"": 138.513986945,
                ""LastPrice"": 138.505714984
            },
            ""IsMarketable"": true,
            ""PriceAdjustmentMode"": 1,
            ""events"": []
        }";

        private const string _capitalCaseLimitOrder = @"{
	""LimitPrice"": 139.240078869942,
	""Type"": 1,
	""Id"": 1,
	""ContingentId"": 0,
	""BrokerId"": [
		""1""
	],
	""Symbol"": {
		""Value"": ""SPY"",
		""ID"": ""SPY R735QTJ8XC9X"",
		""Permtick"": ""SPY""
	},
	""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Quantity"": 10.0,
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 1,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": true,
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseStopMarket = @"{
	""StopPrice"": 138.232948134345,
	""Type"": 2,
	""Id"": 1,
	""ContingentId"": 0,
	""BrokerId"": [
		""1""
	],
	""Symbol"": {
		""Value"": ""SPY"",
		""ID"": ""SPY R735QTJ8XC9X"",
		""Permtick"": ""SPY""
	},
	""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Quantity"": 10.0,
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 1,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseStopLimitOrder = @"{
	""StopPrice"": 138.232948134345,
	""StopTriggered"": false,
	""LimitPrice"": 139.240078869942,
	""Type"": 3,
	""Id"": 1,
	""ContingentId"": 0,
	""BrokerId"": [
		""1""
	],
	""Symbol"": {
		""Value"": ""SPY"",
		""ID"": ""SPY R735QTJ8XC9X"",
		""Permtick"": ""SPY""
	},
	""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Quantity"": 10.0,
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 1,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseMarketOnOpen = @"{
	""Type"": 4,
	""Id"": 1,
	""ContingentId"": 0,
	""BrokerId"": [
		""1""
	],
	""Symbol"": {
		""Value"": ""SPY"",
		""ID"": ""SPY R735QTJ8XC9X"",
		""Permtick"": ""SPY""
	},
	""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Quantity"": 10.0,
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 1,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseMarketOnClose = @"{
	""Type"": 5,
	""Id"": 1,
	""ContingentId"": 0,
	""BrokerId"": [
		""1""
	],
	""Symbol"": {
		""Value"": ""SPY"",
		""ID"": ""SPY R735QTJ8XC9X"",
		""Permtick"": ""SPY""
	},
	""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Quantity"": 10.0,
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 1,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseOptionExercise = @"{
    ""Type"": 6,
    ""Id"": 1,
    ""ContingentId"": 0,
    ""BrokerId"": [
        ""1""
    ],
    ""Symbol"": {
        ""Value"": ""AAPL  140613P00660000"",
        ""ID"": ""AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X"",
        ""Permtick"": ""AAPL  140613P00660000"",
        ""Underlying"": {
            ""Value"": ""AAPL"",
            ""ID"": ""AAPL R735QTJ8XC9X"",
            ""Permtick"": ""AAPL""
        }
    },
    ""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Quantity"": 10.0,
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 2,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseLimitIfTouched = @"{
	""Type"": 7,
	""triggerPrice"": 138.26,
	""LimitPrice"": 139.240078869942,
	""TriggerTouched"": false,
	""Id"": 1,
	""ContingentId"": 0,
	""BrokerId"": [
		""1""
	],
	""Symbol"": {
		""Value"": ""SPY"",
		""ID"": ""SPY R735QTJ8XC9X"",
		""Permtick"": ""SPY""
	},
	""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Quantity"": 10.0,
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 1,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseComboMarket = @"{
    ""Type"": 8,
    ""Quantity"": 10.0,
    ""Id"": 1,
    ""ContingentId"": 0,
    ""BrokerId"": [
        ""1""
    ],
    ""Symbol"": {
        ""Value"": ""GOOG  160115C00745000"",
        ""ID"": ""GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"",
        ""Permtick"": ""GOOG  160115C00745000"",
        ""Underlying"": {
            ""Value"": ""GOOG"",
            ""ID"": ""GOOCV VP83T1ZUHROL"",
            ""Permtick"": ""GOOG""
        }
    },
    ""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 2,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": true,
    ""GroupOrderManager"": {
        ""Id"": 1,
        ""Quantity"": 10.0,
        ""Count"": 3,
        ""LimitPrice"": 0.0,
        ""OrderIds"": [
            1,
            2,
            3
        ],
        ""Direction"": 0
    },
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseComboLimit = @"{
    ""Type"": 9,
    ""Quantity"": 10.0,
    ""Id"": 1,
    ""ContingentId"": 0,
    ""BrokerId"": [
        ""1""
    ],
    ""Symbol"": {
        ""Value"": ""GOOG  160115C00745000"",
        ""ID"": ""GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"",
        ""Permtick"": ""GOOG  160115C00745000"",
        ""Underlying"": {
            ""Value"": ""GOOG"",
            ""ID"": ""GOOCV VP83T1ZUHROL"",
            ""Permtick"": ""GOOG""
        }
    },
    ""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 2,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""GroupOrderManager"": {
        ""Id"": 1,
        ""Quantity"": 10.0,
        ""Count"": 3,
        ""LimitPrice"": 1.9,
        ""OrderIds"": [
            1,
            2,
            3
        ],
        ""Direction"": 0
    },
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseComboLegLimit = @"{
    ""Type"": 10,
    ""LimitPrice"": 139.240078869942,
    ""Quantity"": 10.0,
    ""Id"": 1,
    ""ContingentId"": 0,
    ""BrokerId"": [
        ""1""
    ],
    ""Symbol"": {
        ""Value"": ""GOOG  160115C00750000"",
        ""ID"": ""GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"",
        ""Permtick"": ""GOOG  160115C00750000"",
        ""Underlying"": {
            ""Value"": ""GOOG"",
            ""ID"": ""GOOCV VP83T1ZUHROL"",
            ""Permtick"": ""GOOG""
        }
    },
    ""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 2,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""GroupOrderManager"": {
        ""Id"": 1,
        ""Quantity"": 10.0,
        ""Count"": 3,
        ""LimitPrice"": 0.0,
        ""OrderIds"": [
            1,
            2,
            3
        ],
        ""Direction"": 0
    },
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        private const string _capitalCaseTrailingStop = @"{
	""TrailingAmount"": 0.0019,
	""TrailingAsPercentage"": true,
	""Type"": 11,
	""StopPrice"": 138.232948134345,
	""Id"": 1,
	""ContingentId"": 0,
	""BrokerId"": [
		""1""
	],
	""Symbol"": {
		""Value"": ""SPY"",
		""ID"": ""SPY R735QTJ8XC9X"",
		""Permtick"": ""SPY""
	},
	""Price"": 138.513986945,
    ""PriceCurrency"": ""USD"",
    ""Time"": ""2013-10-07T13:31:00Z"",
    ""CreatedTime"": ""2013-10-07T13:31:00Z"",
    ""LastFillTime"": ""2013-10-07T13:31:00Z"",
    ""Quantity"": 10.0,
    ""Status"": 1,
    ""Tag"": """",
    ""Properties"": {
        ""TimeInForce"": {}
    },
    ""SecurityType"": 1,
    ""Direction"": 0,
    ""Value"": 1385.139869450,
    ""OrderSubmissionData"": {
        ""BidPrice"": 138.505714984,
        ""AskPrice"": 138.513986945,
        ""LastPrice"": 138.505714984
    },
    ""IsMarketable"": false,
    ""PriceAdjustmentMode"": 1,
    ""Events"": []
}";

        public static object[] DeserializeOrdersTests =
        {
            new object[] { _camelCaseMarketOrder, OrderType.Market, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _camelCaseLimitOrder, OrderType.Limit, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _camelCaseStopMarket, OrderType.StopMarket, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _camelCaseStopLimitOrder, OrderType.StopLimit, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _camelCaseMarketOnOpen, OrderType.MarketOnOpen, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _camelCaseMarketOnClose, OrderType.MarketOnClose, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _camelCaseOptionExercise, OrderType.OptionExercise, "AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X", SecurityType.Option },
            new object[] { _camelCaseLimitIfTouched, OrderType.LimitIfTouched, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _camelCaseComboMarket, OrderType.ComboMarket, "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { _camelCaseComboLimit, OrderType.ComboLimit, "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { _camelCaseComboLegLimit, OrderType.ComboLegLimit, "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { _camelCaseTrailingStop, OrderType.TrailingStop, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _capitalCaseMarketOrder, OrderType.Market, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _capitalCaseLimitOrder, OrderType.Limit, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _capitalCaseStopMarket, OrderType.StopMarket, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _capitalCaseStopLimitOrder, OrderType.StopLimit, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _capitalCaseMarketOnOpen, OrderType.MarketOnOpen, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _capitalCaseMarketOnClose, OrderType.MarketOnClose, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _capitalCaseOptionExercise, OrderType.OptionExercise, "AAPL 2ZQGWTST4Z8NA|AAPL R735QTJ8XC9X", SecurityType.Option },
            new object[] { _capitalCaseLimitIfTouched, OrderType.LimitIfTouched, "SPY R735QTJ8XC9X", SecurityType.Equity },
            new object[] { _capitalCaseComboMarket, OrderType.ComboMarket, "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { _capitalCaseComboLimit, OrderType.ComboLimit, "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { _capitalCaseComboLegLimit, OrderType.ComboLegLimit, "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL", SecurityType.Option },
            new object[] { _capitalCaseTrailingStop, OrderType.TrailingStop, "SPY R735QTJ8XC9X", SecurityType.Equity }
        };

        public static object[] SerializeOrdersTests =
        {
            new object[] { _camelCaseMarketOrder },
            new object[] { _camelCaseLimitOrder },
            new object[] { _camelCaseStopMarket },
            new object[] { _camelCaseStopLimitOrder },
            new object[] { _camelCaseMarketOnOpen },
            new object[] { _camelCaseMarketOnClose },
            new object[] { _camelCaseOptionExercise },
            new object[] { _camelCaseLimitIfTouched },
            new object[] { _camelCaseComboMarket },
            new object[] { _camelCaseComboLimit },
            new object[] { _camelCaseComboLegLimit },
            new object[] { _camelCaseTrailingStop },
        };
    }
}
