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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture]
    public class OptionChainProviderTests
    {
        [Test]
        public void CachingProviderCachesSymbolsByDate()
        {
            var provider = new CachingOptionChainProvider(new DelayedOptionChainProvider(1000));

            var stopwatch = Stopwatch.StartNew();
            var symbols = provider.GetOptionContractList(Symbol.Empty, new DateTime(2017, 7, 28));
            stopwatch.Stop();

            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 1000);
            Assert.AreEqual(2, symbols.Count());

            stopwatch.Restart();
            symbols = provider.GetOptionContractList(Symbol.Empty, new DateTime(2017, 7, 28));
            stopwatch.Stop();

            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 10);
            Assert.AreEqual(2, symbols.Count());

            stopwatch.Restart();
            symbols = provider.GetOptionContractList(Symbol.Empty, new DateTime(2017, 7, 29));
            stopwatch.Stop();

            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 1000);
            Assert.AreEqual(2, symbols.Count());
        }
    }

    internal class DelayedOptionChainProvider : IOptionChainProvider
    {
        private readonly int _delayMilliseconds;

        public DelayedOptionChainProvider(int delayMilliseconds)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        public IEnumerable<Symbol> GetOptionContractList(Symbol symbol, DateTime date)
        {
            Thread.Sleep(_delayMilliseconds);

            return new[] { Symbols.SPY_C_192_Feb19_2016, Symbols.SPY_P_192_Feb19_2016 };
        }
    }
}