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
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class ProjectParameterTests
    {
        [TestCase("[{\"name\":\"ema-fast\",\"value\":\"10\"},{\"name\":\"ema-slow\",\"value\":\"20\"}]")]
        [TestCase("[{\"key\":\"ema-fast\",\"value\":\"10\"},{\"key\":\"ema-slow\",\"value\":20,\"min\":10,\"max\":30,\"step\":5}]")]
        public void Deserialization(string parameters)
        {
            var json = $"{{\"projects\":[{{\"projectId\":1,\"name\":\"project\",\"parameters\":{parameters}}}],\"success\":true}}";

            var project = JsonConvert.DeserializeObject<ProjectResponse>(json).Projects[0];

            Assert.AreEqual(2, project.Parameters.Count);
            Assert.AreEqual("ema-fast", project.Parameters[0].Name);
            Assert.AreEqual("10", project.Parameters[0].Value);
            Assert.AreEqual("ema-slow", project.Parameters[1].Name);
            Assert.AreEqual("20", project.Parameters[1].Value);
        }

        [Test]
        public void SerializationKeepsNameFormat()
        {
            var serialized = JsonConvert.SerializeObject(new Parameter { Name = "ema-fast", Value = "10" });

            Assert.AreEqual("{\"name\":\"ema-fast\",\"value\":\"10\"}", serialized);
        }
    }
}
