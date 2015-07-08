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
using NodaTime;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class TimeZonesTest
    {
        [Test]
        public void TimeZonesLoadFromTzdb()
        {
            // verifies each of the fields in the TimeZones class can be retrieved.
            foreach (var field in typeof(TimeZones).GetFields())
            {
                var value = field.GetValue(null);
                Assert.IsNotNull(value);
                Assert.IsInstanceOf(typeof (DateTimeZone), value);
                Console.WriteLine(((DateTimeZone)value).Id);
            }
        }
    }
}
