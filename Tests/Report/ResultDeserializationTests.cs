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
using QuantConnect.Packets;
using QuantConnect.Report;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class ResultDeserializationTests
    {
        public const string InvalidBacktestResultJson = "{\"RollingWindow\":{},\"TotalPerformance\":null,\"Charts\":{\"Equity\":{\"Name\":\"Equity\",\"ChartType\":0,\"Series\":{\"Performance\":{\"Name\":\"Performance\",\"Unit\":\"$\",\"Index\":0,\"Values\":[{\"x\":1583704925,\"y\":5.0},{\"x\":1583791325,\"y\":null},{\"x\":1583877725,\"y\":7.0},{\"x\":1583964125,\"y\":8.0},{\"x\":1584050525,\"y\":9.0}],\"SeriesType\":0,\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":{},\"ProfitLoss\":{},\"Statistics\":{},\"RuntimeStatistics\":{}}";
        public const string InvalidLiveResultJson = "{\"Holdings\":{},\"Cash\":{\"USD\":{\"SecuritySymbol\":{\"Value\":\"\",\"ID\":\" 0\",\"Permtick\":\"\"},\"Symbol\":\"USD\",\"Amount\":0.0,\"ConversionRate\":1.0,\"CurrencySymbol\":\"$\",\"ValueInAccountCurrency\":0.0}},\"ServerStatistics\":{\"CPU Usage\":\"0.0%\",\"Used RAM (MB)\":\"68\",\"Total RAM (MB)\":\"\",\"Used Disk Space (MB)\":\"1\",\"Total Disk Space (MB)\":\"5\",\"Hostname\":\"LEAN\",\"LEAN Version\":\"v2.4.0.0\"},\"Charts\":{\"Equity\":{\"Name\":\"Equity\",\"ChartType\":0,\"Series\":{\"Performance\":{\"Name\":\"Performance\",\"Unit\":\"$\",\"Index\":0,\"Values\":[{\"x\":1583705127,\"y\":5.0},{\"x\":1583791527,\"y\":null},{\"x\":1583877927,\"y\":7.0},{\"x\":1583964327,\"y\":8.0},{\"x\":1584050727,\"y\":9.0}],\"SeriesType\":0,\"Color\":\"\",\"ScatterMarkerSymbol\":\"none\"}}}},\"Orders\":{},\"ProfitLoss\":{},\"Statistics\":{},\"RuntimeStatistics\":{}}";

        [Test]
        public void BacktestResult_NullChartPoint_IsSkipped()
        {
            var converter = new NullResultValueTypeJsonConverter<BacktestResult>();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var deWithoutConverter = JsonConvert.DeserializeObject<BacktestResult>(InvalidBacktestResultJson, settings);
            var deWithConverter = JsonConvert.DeserializeObject<BacktestResult>(InvalidBacktestResultJson, converter);

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

            var deWithoutConverter = JsonConvert.DeserializeObject<LiveResult>(InvalidLiveResultJson, settings);
            var deWithConverter = JsonConvert.DeserializeObject<LiveResult>(InvalidLiveResultJson, converter);

            var noConverterPoints = GetChartPoints(deWithoutConverter).ToList();
            var withConverterPoints = GetChartPoints(deWithConverter).ToList();

            Assert.IsTrue(withConverterPoints.All(kvp => kvp.Value > 0));
            Assert.AreEqual(4, withConverterPoints.Count);

            var convertedSerialized = JsonConvert.SerializeObject(deWithConverter);
            var roundtripDeserialization = JsonConvert.DeserializeObject<LiveResult>(convertedSerialized);

            Assert.IsTrue(withConverterPoints.SequenceEqual(GetChartPoints(roundtripDeserialization).ToList()));
        }

        public IEnumerable<KeyValuePair<long, decimal>> GetChartPoints(Result result)
        {
            return result.Charts["Equity"].Series["Performance"].Values.Select(point => new KeyValuePair<long, decimal>(point.x, point.y));
        }
    }
}
