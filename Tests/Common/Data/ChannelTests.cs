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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class ChannelTests
    {
        private static TestCaseData[] Equality => new[]
        {
            new TestCaseData(new Channel("trade", Symbols.SPY), Symbols.SPY, "trade"),
            new TestCaseData(new Channel("quote", Symbols.AAPL), Symbols.AAPL, "quote"),
            new TestCaseData(new Channel("quote-trade", Symbols.IBM), Symbols.IBM, "quote-trade")
        };

        [TestCaseSource(nameof(Equality))]
        public void Equal(Channel expected, Symbol symbol, string channelName)
        {
            var actual = new Channel(channelName, symbol);
            Assert.AreNotSame(expected, actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        private static TestCaseData[] Inequality => new[]
        {
            new TestCaseData(new Channel("trade", Symbols.SPY), Symbols.SPY, "quote"),
            new TestCaseData(new Channel("trade", Symbols.AAPL), Symbols.SPY, "trade"),
            new TestCaseData(new Channel("quote-trade", Symbols.IBM), Symbols.IBM, "quote"),
            new TestCaseData(new Channel("quote-trade", Symbols.MSFT), Symbols.MSFT, "trade")
        };

        [TestCaseSource(nameof(Inequality))]
        public void NotEqual(Channel expected, Symbol symbol, string channelName)
        {
            var actual = new Channel(channelName, symbol);
            Assert.AreNotSame(expected, actual);
            Assert.AreNotEqual(expected, actual);
            Assert.AreNotEqual(expected.GetHashCode(), actual.GetHashCode());
        }
    }
}
