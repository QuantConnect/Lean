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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class OptimizationBacktestJsonConverterTests
    {
        private const string _validSerialization = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"startDate\":\"2023-01-01T00:00:00Z\",\"endDate\":\"2024-01-01T00:00:00Z\",\"outOfSampleMaxEndDate\":\"2024-01-01T00:00:00Z\",\"outOfSampleDays\":10,\"statistics\":{\"0\":0.374,\"1\":0.217,\"2\":0.047,\"3\":-4.51,\"4\":2.86,\"5\":-0.664,\"6\":52.602,\"7\":17.800,\"8\":6300000.00,\"9\":0.196,\"10\":1.571,\"11\":27.0,\"12\":123.888,\"13\":77.188,\"14\":0.63,\"15\":1.707,\"16\":1390.49,\"17\":180.0,\"18\":0.233,\"19\":-0.558,\"20\":73.0,\"21\":0.1,\"22\":100000.0,\"23\":200000.0,\"24\":3.0}," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0,1.0,1.0,1.0],[2,2.0,2.0,2.0,2.0],[3,3.0,3.0,3.0,3.0]]}";
        private const string _oldValidSerialization = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"statistics\":{\"0\":0.374,\"1\":0.217,\"2\":0.047,\"3\":-4.51,\"4\":2.86,\"5\":-0.664,\"6\":52.602,\"7\":17.800,\"8\":6300000.00,\"9\":0.196,\"10\":1.571,\"11\":27.0,\"12\":123.888,\"13\":77.188,\"14\":0.63,\"15\":1.707,\"16\":1390.49,\"17\":180.0,\"18\":0.233,\"19\":-0.558,\"20\":73.0,\"21\":0.1,\"22\":100000.0,\"23\":200000.0,\"24\":3.0}," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0,1.0,1.0,1.0],[2,2.0,2.0,2.0,2.0],[3,3.0,3.0,3.0,3.0]]}";
        private const string _oldValid2Serialization = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"statistics\":{\"0\":0.374,\"1\":0.217,\"2\":0.047,\"3\":-4.51,\"4\":2.86,\"5\":-0.664,\"6\":52.602,\"7\":17.800,\"8\":6300000.00,\"9\":0.196,\"10\":1.571,\"11\":27.0,\"12\":123.888,\"13\":77.188,\"14\":0.63,\"15\":1.707,\"16\":1390.49,\"17\":180.0,\"18\":0.233,\"19\":-0.558,\"20\":73.0,\"21\":0.1,\"22\":100000.0,\"23\":200000.0,\"24\":3.0}," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0],[2,2.0],[3,3.0]]}";
        private const string _oldValid3Serialization = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"statistics\":{\"0\":0.374,\"1\":0.217,\"2\":0.047,\"3\":-4.51,\"4\":2.86,\"5\":-0.664,\"6\":52.602,\"7\":17.800,\"8\":6300000.00,\"9\":0.196,\"10\":1.571,\"11\":27.0,\"12\":123.888,\"13\":77.188,\"14\":0.63,\"15\":1.707,\"16\":1390.49,\"17\":180.0,\"18\":0.233,\"19\":-0.558,\"20\":73.0}," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0],[2,2.0],[3,3.0]]}";

        private const string _validSerializationWithCustomStats = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"startDate\":\"2023-01-01T00:00:00Z\",\"endDate\":\"2024-01-01T00:00:00Z\",\"outOfSampleMaxEndDate\":\"2024-01-01T00:00:00Z\",\"outOfSampleDays\":10,\"statistics\":{\"0\":0.374,\"1\":0.217,\"2\":0.047,\"3\":-4.51,\"4\":2.86,\"5\":-0.664,\"6\":52.602,\"7\":17.800,\"8\":6300000.00,\"9\":0.196,\"10\":1.571,\"11\":27.0,\"12\":123.888,\"13\":77.188,\"14\":0.63,\"15\":1.707,\"16\":1390.49,\"17\":180.0,\"18\":0.233,\"19\":-0.558,\"20\":73.0,\"21\":0.1,\"22\":100000.0,\"23\":200000.0,\"24\":3.0,\"customstat2\":5.4321,\"customstat1\":1.2345}," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0,1.0,1.0,1.0],[2,2.0,2.0,2.0,2.0],[3,3.0,3.0,3.0,3.0]]}";

        private const string _validOldStatsDeserialization = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"startDate\":\"2023-01-01T00:00:00Z\",\"endDate\":\"2024-01-01T00:00:00Z\",\"outOfSampleMaxEndDate\":\"2024-01-01T00:00:00Z\",\"outOfSampleDays\":10,\"statistics\":[0.374,0.217,0.047,-4.51,2.86,-0.664,52.602,17.800,6300000.00,0.196,1.571,27.0,123.888,77.188,0.63,1.707,1390.49,180.0,0.233,-0.558,73.0]," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0,1.0,1.0,1.0],[2,2.0,2.0,2.0,2.0],[3,3.0,3.0,3.0,3.0]]}";
        private const string _validOldStatsDeserialization2 = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"statistics\":[0.374,0.217,0.047,-4.51,2.86,-0.664,52.602,17.800,6300000.00,0.196,1.571,27.0,123.888,77.188,0.63,1.707,1390.49,180.0,0.233,-0.558,73.0]," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0,1.0,1.0,1.0],[2,2.0,2.0,2.0,2.0],[3,3.0,3.0,3.0,3.0]]}";
        private const string _validOldStatsDeserialization3 = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"statistics\":[0.374,0.217,0.047,-4.51,2.86,-0.664,52.602,17.800,6300000.00,0.196,1.571,27.0,123.888,77.188,0.63,1.707,1390.49,180.0,0.233,-0.558,73.0]," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0],[2,2.0],[3,3.0]]}";
        private const string _validOldStatsDeserializationWithLessStats = "{\"name\":\"ImABacktestName\",\"id\":\"backtestId\",\"progress\":0.0,\"exitCode\":0," +
            "\"statistics\":[0.374,0.217,0.047,-4.51,2.86,-0.664,52.602,17.800,6300000.00,0.196,1.571,27.0,123.888,77.188,0.63]," +
            "\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"},\"equity\":[[1,1.0],[2,2.0],[3,3.0]]}";

        [Test]
        public void SerializationNulls()
        {
            var optimizationBacktest = new OptimizationBacktest(null, null, null);

            var serialized = JsonConvert.SerializeObject(optimizationBacktest);
            Assert.AreEqual("{}", serialized);
        }

        [TestCase(1)]
        [TestCase(0)]
        public void Serialization(int version)
        {
            var optimizationBacktest = new OptimizationBacktest(new ParameterSet(18,
                new Dictionary<string, string>
                {
                    { "pinocho", "19" },
                    { "pepe", "-1" }
                }), "backtestId", "ImABacktestName");

            optimizationBacktest.Statistics = new Dictionary<string, string>
            {
                { "Total Orders", "180" },
                { "Average Win", "2.86%" },
                { "Average Loss", "-4.51%" },
                { "Compounding Annual Return", "52.602%" },
                { "Drawdown", "17.800%" },
                { "Expectancy", "0.196" },
                { "Start Equity", "100000" },
                { "End Equity", "200000" },
                { "Net Profit", "123.888%" },
                { "Sharpe Ratio", "1.707" },
                { "Sortino Ratio", "0.1" },
                { "Probabilistic Sharpe Ratio", "77.188%" },
                { "Loss Rate", "27%" },
                { "Win Rate", "73%" },
                { "Profit-Loss Ratio", "0.63" },
                { "Alpha", "0.374" },
                { "Beta", "-0.664" },
                { "Annual Standard Deviation", "0.217" },
                { "Annual Variance", "0.047" },
                { "Information Ratio", "1.571" },
                { "Tracking Error", "0.233" },
                { "Treynor Ratio", "-0.558" },
                { "Total Fees", "$1390.49" },
                { "Estimated Strategy Capacity", "ZRX6300000.00" },
                { "Drawdown Recovery", "3" }
            };

            optimizationBacktest.Equity = new CandlestickSeries
            {
                Values = new List<ISeriesPoint> { new Candlestick(1, 1, 1, 1, 1), new Candlestick(2, 2, 2, 2, 2), new Candlestick(3, 3, 3, 3, 3) }
            };
            if (version > 0)
            {
                optimizationBacktest.StartDate = new DateTime(2023, 01, 01);
                optimizationBacktest.EndDate = new DateTime(2024, 01, 01);
                optimizationBacktest.OutOfSampleMaxEndDate = new DateTime(2024, 01, 01);
                optimizationBacktest.OutOfSampleDays = 10;
            }

            var serialized = JsonConvert.SerializeObject(optimizationBacktest);

            var expected = _validSerialization;
            if (version == 0)
            {
                expected = _oldValidSerialization;
            }
            Assert.AreEqual(expected, serialized);
        }

        [Test]
        public void SerializationWithCustomStatistics()
        {
            var optimizationBacktest = new OptimizationBacktest(new ParameterSet(18,
                new Dictionary<string, string>
                {
                    { "pinocho", "19" },
                    { "pepe", "-1" }
                }), "backtestId", "ImABacktestName");

            optimizationBacktest.Statistics = new Dictionary<string, string>
            {
                { "customstat2", "5.4321" },
                { "customstat1", "1.2345" },
                { "Total Orders", "180" },
                { "Average Win", "2.86%" },
                { "Average Loss", "-4.51%" },
                { "Compounding Annual Return", "52.602%" },
                { "Drawdown", "17.800%" },
                { "Expectancy", "0.196" },
                { "Start Equity", "100000" },
                { "End Equity", "200000" },
                { "Net Profit", "123.888%" },
                { "Sharpe Ratio", "1.707" },
                { "Sortino Ratio", "0.1" },
                { "Probabilistic Sharpe Ratio", "77.188%" },
                { "Loss Rate", "27%" },
                { "Win Rate", "73%" },
                { "Profit-Loss Ratio", "0.63" },
                { "Alpha", "0.374" },
                { "Beta", "-0.664" },
                { "Annual Standard Deviation", "0.217" },
                { "Annual Variance", "0.047" },
                { "Information Ratio", "1.571" },
                { "Tracking Error", "0.233" },
                { "Treynor Ratio", "-0.558" },
                { "Total Fees", "$1390.49" },
                { "Estimated Strategy Capacity", "ZRX6300000.00" },
                { "Drawdown Recovery", "3" }
            };

            optimizationBacktest.Equity = new CandlestickSeries
            {
                Values = new List<ISeriesPoint> { new Candlestick(1, 1, 1, 1, 1), new Candlestick(2, 2, 2, 2, 2), new Candlestick(3, 3, 3, 3, 3) }
            };
            optimizationBacktest.StartDate = new DateTime(2023, 01, 01);
            optimizationBacktest.EndDate = new DateTime(2024, 01, 01);
            optimizationBacktest.OutOfSampleMaxEndDate = new DateTime(2024, 01, 01);
            optimizationBacktest.OutOfSampleDays = 10;

            var serialized = JsonConvert.SerializeObject(optimizationBacktest);

            Assert.AreEqual(_validSerializationWithCustomStats, serialized);
        }

        [TestCase(_validSerialization, false, 25)]
        [TestCase(_oldValidSerialization, false, 25)]
        [TestCase(_oldValid2Serialization, false, 25)]
        // This case has only 21 stats because Sortino Ratio, Start Equity, End Equity and Drawdown Recovery were not supported
        [TestCase(_oldValid3Serialization, false, 21)]
        [TestCase(_validOldStatsDeserialization, false, 21)]
        [TestCase(_validOldStatsDeserialization2, false, 21)]
        [TestCase(_validOldStatsDeserialization3, false, 21)]
        [TestCase(_validOldStatsDeserializationWithLessStats, false, 15)]
        [TestCase(_validSerializationWithCustomStats, true, 25)]
        public void Deserialization(string serialization, bool hasCustomStats, int expectedLeanStats)
        {
            var deserialized = JsonConvert.DeserializeObject<OptimizationBacktest>(serialization);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("ImABacktestName", deserialized.Name);
            Assert.AreEqual("backtestId", deserialized.BacktestId);
            Assert.AreEqual(0.0m, deserialized.Progress);
            Assert.AreEqual(0, deserialized.ExitCode);
            Assert.AreEqual(-1, deserialized.ParameterSet.Id);
            Assert.IsTrue(deserialized.ParameterSet.Value.Count == 2);
            Assert.IsTrue(deserialized.ParameterSet.Value["pinocho"] == "19");
            Assert.IsTrue(deserialized.ParameterSet.Value["pepe"] == "-1");
            Assert.IsTrue(deserialized.Equity.Values.Count == 3);

            for (var i = 0; i < 3; i++)
            {
                var expected = i + 1;
                Assert.IsTrue(((Candlestick)deserialized.Equity.Values[i]).LongTime == expected);
                Assert.IsTrue(((Candlestick)deserialized.Equity.Values[i]).Open == expected);
                Assert.IsTrue(((Candlestick)deserialized.Equity.Values[i]).High == expected);
                Assert.IsTrue(((Candlestick)deserialized.Equity.Values[i]).Low == expected);
                Assert.IsTrue(((Candlestick)deserialized.Equity.Values[i]).Close == expected);
            }
            Assert.AreEqual("77.188", deserialized.Statistics[PerformanceMetrics.ProbabilisticSharpeRatio]);

            if (!hasCustomStats)
            {
                Assert.AreEqual(expectedLeanStats, deserialized.Statistics.Count);
            }
            else
            {
                // There are 2 custom stats
                Assert.AreEqual(expectedLeanStats + 2, deserialized.Statistics.Count);

                Assert.AreEqual("1.2345", deserialized.Statistics["customstat1"]);
                Assert.AreEqual("5.4321", deserialized.Statistics["customstat2"]);
            }
        }
    }
}
