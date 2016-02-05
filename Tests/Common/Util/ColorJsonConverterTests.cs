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

using System.Drawing;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ColorJsonConverterTests
    {
        /// <summary>
        /// Convert .NET Color to Json
        /// </summary>
        /// <param name="expected">String object that we expect to return after conversion</param>
        private static void ConvertColorToJson(string expected)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = {new ColorJsonConverter()}
            };

            var color = JsonConvert.DeserializeObject<Color>(expected);
            var actual = JsonConvert.SerializeObject(color);

            Assert.IsInstanceOf<Color>(color);
            Assert.IsInstanceOf<string>(actual);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Convert Json to .NET Color
        /// </summary>
        /// <param name="expected">.NET Color object that we expect to return after conversion</param>
        private static void ConvertJsonToColor(Color expected)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = {new ColorJsonConverter()}
            };

            var json = JsonConvert.SerializeObject(expected);
            var actual = JsonConvert.DeserializeObject<Color>(json);

            Assert.IsInstanceOf<Color>(actual);
            Assert.AreEqual(expected.IsEmpty, actual.IsEmpty);
            if (!expected.IsEmpty)
            {
                Assert.AreEqual(expected.ToArgb(), actual.ToArgb());
            }
        }

        [Test]
        public void ConvertColorToJsonTest()
        {
            ConvertColorToJson("\"\"");
            ConvertColorToJson("\"#0000FF\"");
        }

        [Test]
        public void ConvertJsonToColorTest()
        {
            ConvertJsonToColor(Color.Empty);
            ConvertJsonToColor(Color.Blue);
        }
    }
}