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
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Orders.Serialization;

namespace QuantConnect.Tests.Common.Orders.Serialization
{
    [TestFixture]
    public class SerializedOrderJsonConverterTests
    {
        [Test]
        public void RoundTrip()
        {
            var expected = new MarketOrder(Symbols.SPY, 100, DateTime.UtcNow, "BuyTheDip")
            {
                Properties = { TimeInForce = TimeInForce.GoodTilDate(DateTime.UtcNow) },
                CanceledTime = DateTime.UtcNow,
                LastUpdateTime = DateTime.UtcNow,
                LastFillTime = DateTime.UtcNow,
                Status = OrderStatus.Canceled,
                Id = 99,
                OrderSubmissionData = new OrderSubmissionData(11, 12, 13),
                BrokerId = new List<string> { "LL", "ASD" },
                ContingentId = 77,
                PriceCurrency = "EUR",
                Price = 88
            };

            var serializedOrder = JsonConvert.SerializeObject(new List<Order> { expected }, new SerializedOrderJsonConverter("JoseAlgorithmId"));

            var actual = JsonConvert.DeserializeObject<List<Order>>(serializedOrder, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new SerializedOrderJsonConverter() }
            }).Single();

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
            // there is a small loss of precision because we use double
            Assert.AreEqual(expected.Time.Ticks, actual.Time.Ticks, 500);
            Assert.AreEqual(expected.CreatedTime.Ticks, actual.CreatedTime.Ticks, 500);
            Assert.AreEqual(expected.LastFillTime.Value.Ticks, actual.LastFillTime.Value.Ticks, 500);
            Assert.AreEqual(expected.LastUpdateTime.Value.Ticks, actual.LastUpdateTime.Value.Ticks, 500);
            Assert.AreEqual(expected.CanceledTime.Value.Ticks, actual.CanceledTime.Value.Ticks, 500);
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Quantity, actual.Quantity);
            Assert.AreEqual(expected.TimeInForce.GetType(), actual.TimeInForce.GetType());
            Assert.AreEqual(expected.Symbol.ID.Market, actual.Symbol.ID.Market);
            Assert.AreEqual(expected.OrderSubmissionData.AskPrice, actual.OrderSubmissionData.AskPrice);
            Assert.AreEqual(expected.OrderSubmissionData.BidPrice, actual.OrderSubmissionData.BidPrice);
            Assert.AreEqual(expected.OrderSubmissionData.LastPrice, actual.OrderSubmissionData.LastPrice);
        }
    }
}
