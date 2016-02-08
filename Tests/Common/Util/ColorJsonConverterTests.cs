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
        [Test]
        public void ConvertsKnownColorToJson()
        {
            var container = new ColorContainer { Color = Color.Blue };
            var json = JsonConvert.SerializeObject(container);
            Assert.AreEqual("{\"Color\":\"#0000FF\"}", json);
        }
        [Test]
        public void ConvertsEmptyColorToJson()
        {
            var container = new ColorContainer { Color = Color.Empty };
            var json = JsonConvert.SerializeObject(container);
            Assert.AreEqual("{\"Color\":\"\"}", json);
        }

        [Test]
        public void ConvertJsonToColorTest()
        {
            const string jsonValue = "{ 'Color': '#FFFFFF' }";
            var converted = JsonConvert.DeserializeObject<ColorContainer>(jsonValue).Color;
            Assert.AreEqual(Color.White.ToArgb(), converted.ToArgb());
        }

        struct ColorContainer
        {
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Color;
        }
    }
}