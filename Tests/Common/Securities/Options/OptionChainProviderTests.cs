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
using QuantConnect.Securities.Future;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OptionChainProviderTests
    {
        [Test]
        public void BacktestingOptionChainProviderLoadsEquityOptionChain()
        {
            var provider = new BacktestingOptionChainProvider();
            var twxOptionChain = provider.GetOptionContractList(Symbol.Create("TWX", SecurityType.Equity, Market.USA), new DateTime(2014, 6, 5))
                .ToList();

            Assert.AreEqual(184, twxOptionChain.Count);
            Assert.AreEqual(23m, twxOptionChain.OrderBy(s => s.ID.StrikePrice).First().ID.StrikePrice);
            Assert.AreEqual(105m, twxOptionChain.OrderBy(s => s.ID.StrikePrice).Last().ID.StrikePrice);
        }

        [Test]
        public void BacktestingOptionChainProviderLoadsFutureOptionChain()
        {
            var provider = new BacktestingOptionChainProvider();
            var esOptionChain = provider.GetOptionContractList(
                Symbol.CreateFuture(
                    QuantConnect.Securities.Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 6, 19)),
                new DateTime(2020, 1, 5))
                .ToList();

            Assert.AreEqual(107, esOptionChain.Count);
            Assert.AreEqual(2900m, esOptionChain.OrderBy(s => s.ID.StrikePrice).First().ID.StrikePrice);
            Assert.AreEqual(3500m, esOptionChain.OrderBy(s => s.ID.StrikePrice).Last().ID.StrikePrice);
        }

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

        [Test]
        public void LiveOptionChainProviderReturnsData()
        {
            var provider = new LiveOptionChainProvider();

            foreach (var symbol in new[] { Symbols.SPY, Symbols.AAPL, Symbols.MSFT })
            {
                var result = provider.GetOptionContractList(symbol, DateTime.Today);

                Assert.IsTrue(result.Any());
            }
        }

        [Test]
        public void LiveOptionChainProviderReturnsNoDataForInvalidSymbol()
        {
            var symbol = Symbol.Create("ABCDEF123", SecurityType.Equity, Market.USA);

            var provider = new LiveOptionChainProvider();
            var result = provider.GetOptionContractList(symbol, DateTime.Today);

            Assert.IsFalse(result.Any());
        }

        [Test]
        public void LiveOptionChainProviderReturnsFutureOptionData()
        {
            var now = DateTime.Now;
            var december = new DateTime(now.Year, 12, 1);
            var canonicalFuture = Symbol.Create("ES", SecurityType.Future, Market.CME);
            var expiry = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFuture)(december);

            // When the current year's december contract expires, the test starts failing.
            // This will happen around the last 10 days of December, but will start working
            // once we've crossed into the new year.
            // Let's try the next listed contract, which is in March of the next year if this is the case.
            if (now >= expiry)
            {
                expiry = now.AddMonths(-now.Month).AddYears(1).AddMonths(3);
            }

            var underlyingFuture = Symbol.CreateFuture("ES", Market.CME, expiry);
            var provider = new LiveOptionChainProvider();
            var result = provider.GetOptionContractList(underlyingFuture, now).ToList();

            Assert.AreNotEqual(0, result.Count);

            foreach (var symbol in result)
            {
                Assert.IsTrue(symbol.HasUnderlying);
                Assert.AreEqual(Market.CME, symbol.ID.Market);
                Assert.AreEqual(OptionStyle.American, symbol.ID.OptionStyle);
                Assert.GreaterOrEqual(symbol.ID.StrikePrice, 100m);
                Assert.Less(symbol.ID.StrikePrice, 30000m);
            }
        }

        [Test]
        public void LiveOptionChainProviderReturnsNoDataForOldFuture()
        {
            var now = DateTime.Now;
            var december = now.AddMonths(-now.Month).AddYears(-1);
            var underlyingFuture = Symbol.CreateFuture("ES", Market.CME, december);

            var provider = new LiveOptionChainProvider();
            var result = provider.GetOptionContractList(underlyingFuture, december);

            Assert.AreEqual(0, result.Count());
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
