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


using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Common.Packets
{
    [TestFixture]
    public class BreakpointTests
    {
        [Test]
        public void SurvivesSerializationRoundTrip()
        {
            var breakpoints = new List<Breakpoint>
            {
                new Breakpoint{ FileName = "MichelAngelo", LineNumber = 1475},
                new Breakpoint{ FileName = "LeonardoDaVinci", LineNumber = 1452}
            };

            var serialized = JsonConvert.SerializeObject(breakpoints);
            var deserialized = JsonConvert.DeserializeObject<List<Breakpoint>>(serialized);

            Assert.AreEqual(deserialized.Count, 2);
            Assert.AreEqual(deserialized[0].FileName, "MichelAngelo");
            Assert.AreEqual(deserialized[0].LineNumber, 1475);
            Assert.AreEqual(deserialized[1].FileName, "LeonardoDaVinci");
            Assert.AreEqual(deserialized[1].LineNumber, 1452);
        }
    }
}
