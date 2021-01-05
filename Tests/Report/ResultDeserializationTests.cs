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
using QuantConnect.Packets;
using QuantConnect.Report;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class ResultDeserializationTests
    {
        public const string OrderStringReplace = "{{orderStringReplace}}";
        public const string OrderTypeStringReplace = "{{marketOrderType}}";
        public const string EmptyJson = "{}";

        public const string InvalidBacktestResultJson = "{\"RollingWindow\":{},\"TotalPerformance\":null,\"Charts\":{\"Equity\":{\"Name\":\"Equity\",\"ChartType\":0,\"Series\":{\"Performance\":{\"Name\":\"Performance\",\"Unit\":\"$\",\"Index\":0,\"Values\":[{\"x\":1583704925,\"y\":5.0},{\"x\":1583791325,\"y\":null},{\"x\":1583877725,\"y\":7.0},{\"x\":1583964125,\"y\":8.0},{\"x\":1584050525,\"y\":9.0}],\"SeriesType\":0,\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":" + OrderStringReplace + ",\"ProfitLoss\":{},\"Statistics\":{},\"RuntimeStatistics\":{}}";
        public const string InvalidLiveResultJson = "{\"Holdings\":{},\"Cash\":{\"USD\":{\"SecuritySymbol\":{\"Value\":\"\",\"ID\":\" 0\",\"Permtick\":\"\"},\"Symbol\":\"USD\",\"Amount\":0.0,\"ConversionRate\":1.0,\"CurrencySymbol\":\"$\",\"ValueInAccountCurrency\":0.0}},\"ServerStatistics\":{\"CPU Usage\":\"0.0%\",\"Used RAM (MB)\":\"68\",\"Total RAM (MB)\":\"\",\"Used Disk Space (MB)\":\"1\",\"Total Disk Space (MB)\":\"5\",\"Hostname\":\"LEAN\",\"LEAN Version\":\"v2.4.0.0\"},\"Charts\":{\"Equity\":{\"Name\":\"Equity\",\"ChartType\":0,\"Series\":{\"Performance\":{\"Name\":\"Performance\",\"Unit\":\"$\",\"Index\":0,\"Values\":[{\"x\":1583705127,\"y\":5.0},{\"x\":1583791527,\"y\":null},{\"x\":1583877927,\"y\":7.0},{\"x\":1583964327,\"y\":8.0},{\"x\":1584050727,\"y\":9.0}],\"SeriesType\":0,\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":" + OrderStringReplace + ",\"ProfitLoss\":{},\"Statistics\":{},\"RuntimeStatistics\":{}}";
        public const string OrderJson = @"{'1': {
    'Type':" + OrderTypeStringReplace + @",
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
    'AbsoluteQuantity':999
}}";

        [Test]
        public void BacktestResult_NullChartPoint_IsSkipped()
        {
            var converter = new NullResultValueTypeJsonConverter<BacktestResult>();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var deWithoutConverter = JsonConvert.DeserializeObject<BacktestResult>(InvalidBacktestResultJson.Replace(OrderStringReplace, EmptyJson), settings);
            var deWithConverter = JsonConvert.DeserializeObject<BacktestResult>(InvalidBacktestResultJson.Replace(OrderStringReplace, EmptyJson), converter);

            var noConverterPoints = GetChartPoints(deWithoutConverter).ToList();
            var withConverterPoints = GetChartPoints(deWithConverter).ToList();

            Assert.IsTrue(withConverterPoints.All(kvp => kvp.Value > 0));
            Assert.AreEqual(4, withConverterPoints.Count);

            var convertedSerialized = JsonConvert.SerializeObject(deWithConverter);
            var roundtripDeserialization = JsonConvert.DeserializeObject<BacktestResult>(convertedSerialized);

            Assert.IsTrue(withConverterPoints.SequenceEqual(GetChartPoints(roundtripDeserialization).ToList()));
        }

        [Test]
        public void LiveResult_NullChartPoint_IsSkipped()
        {
            var converter = new NullResultValueTypeJsonConverter<LiveResult>();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var deWithoutConverter = JsonConvert.DeserializeObject<LiveResult>(InvalidLiveResultJson.Replace(OrderStringReplace, EmptyJson), settings);
            var deWithConverter = JsonConvert.DeserializeObject<LiveResult>(InvalidLiveResultJson.Replace(OrderStringReplace, EmptyJson), converter);

            var noConverterPoints = GetChartPoints(deWithoutConverter).ToList();
            var withConverterPoints = GetChartPoints(deWithConverter).ToList();

            Assert.IsTrue(withConverterPoints.All(kvp => kvp.Value > 0));
            Assert.AreEqual(4, withConverterPoints.Count);

            var convertedSerialized = JsonConvert.SerializeObject(deWithConverter);
            var roundtripDeserialization = JsonConvert.DeserializeObject<LiveResult>(convertedSerialized);

            Assert.IsTrue(withConverterPoints.SequenceEqual(GetChartPoints(roundtripDeserialization).ToList()));
        }

        [Test]
        public void OrderTypeEnumStringAndValueDeserialization()
        {

            var settings = new JsonSerializerSettings
            {
                Converters = new[] { new NullResultValueTypeJsonConverter<LiveResult>() }
            };

            foreach (var orderType in (OrderType[])Enum.GetValues(typeof(OrderType)))
            {
                //var orderObjectType = OrderTypeNormalizingJsonConverter.TypeFromOrderTypeEnum(orderType);
                var intValueJson = OrderJson.Replace(OrderTypeStringReplace, ((int)orderType).ToStringInvariant());
                var upperCaseJson = OrderJson.Replace(OrderTypeStringReplace, $"'{orderType.ToStringInvariant().ToUpperInvariant()}'");
                var camelCaseJson = OrderJson.Replace(OrderTypeStringReplace, $"'{orderType.ToStringInvariant().ToCamelCase()}'");

                var intValueLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, intValueJson);
                var upperCaseLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, upperCaseJson);
                var camelCaseLiveResult = InvalidLiveResultJson.Replace(OrderStringReplace, camelCaseJson);

                var intInstance = JsonConvert.DeserializeObject<LiveResult>(intValueLiveResult, settings).Orders.Values.Single();
                var upperCaseInstance = JsonConvert.DeserializeObject<LiveResult>(upperCaseLiveResult, settings).Orders.Values.Single();
                var camelCaseInstance = JsonConvert.DeserializeObject<LiveResult>(camelCaseLiveResult, settings).Orders.Values.Single();

                CollectionAssert.AreEqual(intInstance.BrokerId, upperCaseInstance.BrokerId);
                Assert.AreEqual(intInstance.ContingentId, upperCaseInstance.ContingentId);
                Assert.AreEqual(intInstance.Direction, upperCaseInstance.Direction);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), upperCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Id, upperCaseInstance.Id);
                Assert.AreEqual(intInstance.Price, upperCaseInstance.Price);
                Assert.AreEqual(intInstance.PriceCurrency, upperCaseInstance.PriceCurrency);
                Assert.AreEqual(intInstance.SecurityType, upperCaseInstance.SecurityType);
                Assert.AreEqual(intInstance.Status, upperCaseInstance.Status);
                Assert.AreEqual(intInstance.Symbol, upperCaseInstance.Symbol);
                Assert.AreEqual(intInstance.Tag, upperCaseInstance.Tag);
                Assert.AreEqual(intInstance.Time, upperCaseInstance.Time);
                Assert.AreEqual(intInstance.CreatedTime, upperCaseInstance.CreatedTime);
                Assert.AreEqual(intInstance.LastFillTime, upperCaseInstance.LastFillTime);
                Assert.AreEqual(intInstance.LastUpdateTime, upperCaseInstance.LastUpdateTime);
                Assert.AreEqual(intInstance.CanceledTime, upperCaseInstance.CanceledTime);
                Assert.AreEqual(intInstance.Type, upperCaseInstance.Type);
                Assert.AreEqual(intInstance.Value, upperCaseInstance.Value);
                Assert.AreEqual(intInstance.Quantity, upperCaseInstance.Quantity);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), upperCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Symbol.ID.Market, upperCaseInstance.Symbol.ID.Market);
                Assert.AreEqual(intInstance.OrderSubmissionData?.AskPrice, upperCaseInstance.OrderSubmissionData?.AskPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.BidPrice, upperCaseInstance.OrderSubmissionData?.BidPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.LastPrice, upperCaseInstance.OrderSubmissionData?.LastPrice);

                CollectionAssert.AreEqual(intInstance.BrokerId, camelCaseInstance.BrokerId);
                Assert.AreEqual(intInstance.ContingentId, camelCaseInstance.ContingentId);
                Assert.AreEqual(intInstance.Direction, camelCaseInstance.Direction);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), camelCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Id, camelCaseInstance.Id);
                Assert.AreEqual(intInstance.Price, camelCaseInstance.Price);
                Assert.AreEqual(intInstance.PriceCurrency, camelCaseInstance.PriceCurrency);
                Assert.AreEqual(intInstance.SecurityType, camelCaseInstance.SecurityType);
                Assert.AreEqual(intInstance.Status, camelCaseInstance.Status);
                Assert.AreEqual(intInstance.Symbol, camelCaseInstance.Symbol);
                Assert.AreEqual(intInstance.Tag, camelCaseInstance.Tag);
                Assert.AreEqual(intInstance.Time, camelCaseInstance.Time);
                Assert.AreEqual(intInstance.CreatedTime, camelCaseInstance.CreatedTime);
                Assert.AreEqual(intInstance.LastFillTime, camelCaseInstance.LastFillTime);
                Assert.AreEqual(intInstance.LastUpdateTime, camelCaseInstance.LastUpdateTime);
                Assert.AreEqual(intInstance.CanceledTime, camelCaseInstance.CanceledTime);
                Assert.AreEqual(intInstance.Type, camelCaseInstance.Type);
                Assert.AreEqual(intInstance.Value, camelCaseInstance.Value);
                Assert.AreEqual(intInstance.Quantity, camelCaseInstance.Quantity);
                Assert.AreEqual(intInstance.TimeInForce.GetType(), camelCaseInstance.TimeInForce.GetType());
                Assert.AreEqual(intInstance.Symbol.ID.Market, camelCaseInstance.Symbol.ID.Market);
                Assert.AreEqual(intInstance.OrderSubmissionData?.AskPrice, camelCaseInstance.OrderSubmissionData?.AskPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.BidPrice, camelCaseInstance.OrderSubmissionData?.BidPrice);
                Assert.AreEqual(intInstance.OrderSubmissionData?.LastPrice, camelCaseInstance.OrderSubmissionData?.LastPrice);
            }
        }

        public IEnumerable<KeyValuePair<long, decimal>> GetChartPoints(Result result)
        {
            return result.Charts["Equity"].Series["Performance"].Values.Select(point => new KeyValuePair<long, decimal>(point.x, point.y));
        }
    }
}
