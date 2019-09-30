/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.ToolBox.TradingEconomicsDataDownloader;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    public class TradingEconomicsCalendarTests
    {
        [TestCase("2.5.0", 2.5, false)]
        [TestCase("Foobar", null, false)]
        [TestCase("1.0%", 0.01, true)]
        [TestCase("--1.0", -1.0, false)]
        [TestCase("1.0T", 1000000000000.0, false)]
        [TestCase("1.0B", 1000000000.0, false)]
        [TestCase("1.0M", 1000000.0, false)]
        [TestCase("1.0K", 1000.0, false)]
        [TestCase("1.0K%", 0.01, true)]
        [TestCase("1", 0.01, true)]
        public void DecimalIsParsedCorrectly(string value, double? expected, bool inPercentage)
        {
            // Cast inside since we can't pass in decimal values through TestCase attributes
            Assert.AreEqual((decimal?)expected, TradingEconomicsCalendarDownloader.ParseDecimal(value, inPercentage));
        }
    }
}
