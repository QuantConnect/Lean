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
using System.Collections.Generic;
using QuantConnect.Api;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class ParameterSetJsonConverterTests
    {
        private const string ValidSerialization = "{\"parameterSet\":{\"pinocho\":\"19\",\"pepe\":\"-1\"}}";

        [Test]
        public void SerializationNulls()
        {
            var parameterSet = new ParameterSet(0, null);

            var serialized = JsonConvert.SerializeObject(parameterSet);
            Assert.AreEqual("{}", serialized);
        }

        [Test]
        public void Serialization()
        {
            var parameterSet = new ParameterSet(18,
                new Dictionary<string, string>
                {
                    { "pinocho", "19" },
                    { "pepe", "-1" }
                });

            var serialized = JsonConvert.SerializeObject(parameterSet);

            Assert.AreEqual(ValidSerialization, serialized);
        }

        [TestCase("{}", 0)]
        [TestCase("[]", 0)]
        [TestCase(ValidSerialization, 2)]
        [TestCase("{\"pinocho\":\"19\",\"pepe\":\"-1\"}", 2)]
        [TestCase("[{\"key\":\"pinocho\",\"value\":\"19\"},{\"key\":\"pepe\",\"value\":\"-1\"}]", 2)]
        [TestCase("[{\"key\":\"pinocho\",\"value\":19,\"min\":1,\"max\":100,\"step\":1},{\"key\":\"pepe\",\"value\":-1,\"min\":null,\"max\":null,\"step\":null}]", 2)]
        [TestCase("[{\"name\":\"pinocho\",\"value\":\"19\"},{\"name\":\"pepe\",\"value\":\"-1\"}]", 2)]
        [TestCase("[{\"key\":\"pinocho\",\"value\":\"19\"},{\"value\":\"7\"},{\"key\":\"pepe\",\"value\":\"-1\"}]", 2)]
        [TestCase("{\"parameterSet\":[{\"key\":\"pinocho\",\"value\":\"19\"},{\"key\":\"pepe\",\"value\":\"-1\"}]}", 2)]
        public void Deserialization(string validSerialization, int count)
        {
            var deserialized = JsonConvert.DeserializeObject<ParameterSet>(validSerialization);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(-1, deserialized.Id);
            Assert.AreEqual(count, deserialized.Value.Count);
            if (count != 0)
            {
                Assert.IsTrue(deserialized.Value["pinocho"] == "19");
                Assert.IsTrue(deserialized.Value["pepe"] == "-1");
            }
        }

        [TestCase("{\"start-date\":\"2024-01-01T10:00:00Z\"}")]
        [TestCase("{\"parameterSet\":{\"start-date\":\"2024-01-01T10:00:00Z\"}}")]
        [TestCase("[{\"key\":\"start-date\",\"value\":\"2024-01-01T10:00:00Z\"}]")]
        public void DeserializationKeepsDateValues(string serialization)
        {
            var deserialized = JsonConvert.DeserializeObject<ParameterSet>(serialization);

            Assert.AreEqual("2024-01-01T10:00:00Z", deserialized.Value["start-date"]);
        }

        [Test]
        public void DeserializationNull()
        {
            Assert.IsNull(JsonConvert.DeserializeObject<ParameterSet>("null"));
        }

        [Test]
        public void BacktestDeserialization()
        {
            var json = "{\"backtest\":{\"name\":\"Backtest with parameters\",\"projectId\":0,\"completed\":true,\"backtestId\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"," +
                "\"parameterSet\":[{\"key\":\"pinocho\",\"value\":\"19\"},{\"key\":\"pepe\",\"value\":\"-1\"}],\"status\":\"Completed.\",\"progress\":1}," +
                "\"debugging\":false,\"success\":true,\"errors\":[]}";

            var deserialized = JsonConvert.DeserializeObject<BacktestResponseWrapper>(json);

            Assert.IsNotNull(deserialized.Backtest.ParameterSet);
            Assert.AreEqual(2, deserialized.Backtest.ParameterSet.Value.Count);
            Assert.AreEqual("19", deserialized.Backtest.ParameterSet.Value["pinocho"]);
            Assert.AreEqual("-1", deserialized.Backtest.ParameterSet.Value["pepe"]);
        }
    }
}
