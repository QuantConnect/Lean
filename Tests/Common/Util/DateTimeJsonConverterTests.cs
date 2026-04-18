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
 *
*/

using System;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class DateTimeJsonConverterTests
    {
        [Test]
        public void Write()
        {
            var instance = new MultiDateTimeFormatClassTest { Value = new DateTime(2025, 08, 08, 10, 30, 0) };
            var result = JsonConvert.SerializeObject(instance);
            Assert.AreEqual("{\"Value\":\"2025-08-08T10:30:00Z\"}", result);
        }

        [Test]
        public void WriteNullable()
        {
            var instance = new NullableDateTimeFormatClassTest { Value = null };
            var result = JsonConvert.SerializeObject(instance);
            Assert.AreEqual("{\"Value\":null}", result);
        }

        [TestCase("{ \"value\": \"2025-08-08 10:30:00\"}", false)]
        [TestCase("{ \"value\": \"2025-08-08T10:30:00Z\"}", false)]
        [TestCase("{ \"value\": \"2025-08-08T10:30:00.000Z\"}", false)]
        [TestCase("{ \"value\": \"20250808-10:30:00\"}", false)]

        [TestCase("{ \"value\": \"2025/08/08 10:30:00.000\"}", true)]
        [TestCase("{ \"value\": \"2025-08-08 10:30:00.000\"}", true)]
        public void MultipleFormats(string strObject, bool throws)
        {
            if (throws)
            {
                Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<SingleDateTimeFormatClassTest>(strObject));
                return;
            }
            var result = JsonConvert.DeserializeObject<MultiDateTimeFormatClassTest>(strObject);

            Assert.AreEqual(new DateTime(2025, 08, 08, 10, 30, 0), result.Value);
        }

        [TestCase("{ \"value\": \"2025-08-08 10:30:00\"}", false)]
        [TestCase("{ \"value\": \"2025-08-08T10:30:00\"}", false)]
        [TestCase("{ \"value\": \"2025-08-08T10:30:00Z\"}", false)]

        [TestCase("{ \"value\": \"2025/08/08 10:30:00\"}", true)]
        public void SingleFormat(string strObject, bool throws)
        {
            if (throws)
            {
                Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<SingleDateTimeFormatClassTest>(strObject));
                return;
            }
            var result = JsonConvert.DeserializeObject<SingleDateTimeFormatClassTest>(strObject);

            Assert.AreEqual(new DateTime(2025, 08, 08, 10, 30, 0), result.Value);
        }

        [TestCase("{ \"value\": \"2025-08-08T10:30:00\"}", false, false)]
        [TestCase("{ \"value\": \"2025-08-08T10:30:00Z\"}", false, false)]
        [TestCase("{ \"value\": null}", false, true)]
        [TestCase("{ }", false, true)]

        [TestCase("{ \"value\": \"2025/08/08 10:30:00\"}", true, false)]
        [TestCase("{ \"value\": \"2025-08-08 10:30:00\"}", true, false)]
        public void NullFormat(string strObject, bool throws, bool expectNull)
        {
            if (throws)
            {
                Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NullableDateTimeFormatClassTest>(strObject));
                return;
            }
            var result = JsonConvert.DeserializeObject<NullableDateTimeFormatClassTest>(strObject);
            if (expectNull)
            {
                Assert.AreEqual(null, result.Value);
                return;
            }

            Assert.AreEqual(new DateTime(2025, 08, 08, 10, 30, 0), result.Value);
        }

        [TestCase("\"2025-08-08T10:30:00Z\"", false)]
        [TestCase("\"2025-08-08 10:30:00\"", false)]
        [TestCase("", true)]
        [TestCase("\"\"", true)]
        public void ManualConversion(string strObject, bool expectNull)
        {
            var converter = new DateTimeJsonConverter(DateFormat.ISOShort, DateFormat.UI);
            var result = JsonConvert.DeserializeObject<DateTime?>(strObject, converter);
            if (expectNull)
            {
                Assert.AreEqual(null, result);
                return;
            }
            Assert.AreEqual(new DateTime(2025, 08, 08, 10, 30, 0), result.Value);
        }

        internal class MultiDateTimeFormatClassTest
        {
            [JsonConverter(typeof(DateTimeJsonConverter), DateFormat.ISOShort, DateFormat.UI, DateFormat.FIX)]
            public DateTime Value { get; set; }
        }
        internal class SingleDateTimeFormatClassTest
        {
            [JsonConverter(typeof(DateTimeJsonConverter), DateFormat.UI)]
            public DateTime Value { get; set; }
        }
        internal class NullableDateTimeFormatClassTest
        {
            [JsonConverter(typeof(DateTimeJsonConverter), DateFormat.ISOShort)]
            public DateTime? Value { get; set; }
        }
    }
}
