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
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OptionChainProviderTests
    {
        [Test]
        public void UsesMultipleResolutionsFutureOption()
        {
            // we don't have minute data for this date
            var date = new DateTime(2020, 01, 7);
            var future = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 6, 19));
            var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            var optionChain = provider.GetOptionContractList(future, date).OrderBy(s => s.ID.StrikePrice).ToList();

            Assert.IsTrue(optionChain.All(x => x.SecurityType == SecurityType.FutureOption));
            Assert.IsTrue(optionChain.All(x => x.ID.Symbol == "ES"));
            Assert.IsTrue(optionChain.All(x => x.Underlying == future));
            Assert.IsTrue(optionChain.All(x => x.ID.Date.Date >= date));
            Assert.AreEqual(107, optionChain.Count);
            Assert.AreEqual(2900m, optionChain.First().ID.StrikePrice);
            Assert.AreEqual(3500, optionChain.Last().ID.StrikePrice);
        }

        [Test]
        public void UsesMultipleResolutionsEquityOption()
        {
            // we don't have minute data for this date
            var date = new DateTime(2014, 10, 7);

            var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            var optionChain = provider.GetOptionContractList(Symbols.AAPL, date).OrderBy(s => s.ID.StrikePrice).ToList();

            Assert.IsTrue(optionChain.All(x => x.SecurityType == SecurityType.Option));
            Assert.IsTrue(optionChain.All(x => x.ID.Symbol == "AAPL"));
            Assert.IsTrue(optionChain.All(x => x.Underlying == Symbols.AAPL));
            Assert.IsTrue(optionChain.All(x => x.ID.Date.Date >= date));
            Assert.AreEqual(1828, optionChain.Count);
            Assert.AreEqual(27.86m, optionChain.First().ID.StrikePrice);
            Assert.AreEqual(1050m, optionChain.Last().ID.StrikePrice);
        }

        [Test]
        public void BacktestingOptionChainProviderUsesPreviousTradableDateChain()
        {
            // the 7th is a saturday should fetch fridays data instead
            var date = new DateTime(2014, 6, 7);
            Assert.AreEqual(DayOfWeek.Saturday, date.DayOfWeek);

            var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            var twxOptionChain = provider.GetOptionContractList(Symbol.Create("TWX", SecurityType.Equity, Market.USA), date)
                .ToList();

            Assert.AreEqual(184, twxOptionChain.Count);
            Assert.AreEqual(23m, twxOptionChain.OrderBy(s => s.ID.StrikePrice).First().ID.StrikePrice);
            Assert.AreEqual(105m, twxOptionChain.OrderBy(s => s.ID.StrikePrice).Last().ID.StrikePrice);
        }

        [Test]
        public void BacktestingOptionChainProviderLoadsEquityOptionChain()
        {
            var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            var twxOptionChain = provider.GetOptionContractList(Symbol.Create("TWX", SecurityType.Equity, Market.USA), new DateTime(2014, 6, 5))
                .ToList();

            Assert.AreEqual(184, twxOptionChain.Count);
            Assert.AreEqual(23m, twxOptionChain.OrderBy(s => s.ID.StrikePrice).First().ID.StrikePrice);
            Assert.AreEqual(105m, twxOptionChain.OrderBy(s => s.ID.StrikePrice).Last().ID.StrikePrice);
        }

        [Test]
        public void BacktestingOptionChainProviderLoadsFutureOptionChain()
        {
            var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
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
        public void BacktestingOptionChainProviderIndexOption()
        {
            var spxOption = Symbol.CreateCanonicalOption(Symbols.SPX);
            foreach (var option in new [] { Symbols.SPX, spxOption })
            {
                var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);

                var optionChain = provider.GetOptionContractList(option, new DateTime(2021, 01, 04)).ToList();

                Assert.AreEqual(6, optionChain.Count);
                Assert.AreEqual(3200, optionChain.OrderBy(s => s.ID.StrikePrice).First().ID.StrikePrice);
                Assert.AreEqual(4250, optionChain.OrderBy(s => s.ID.StrikePrice).Last().ID.StrikePrice);

                foreach (var optionSymbol in optionChain)
                {
                    Assert.AreEqual("SPX", optionSymbol.ID.Symbol);
                    Assert.AreEqual("SPX", optionSymbol.Underlying.ID.Symbol);
                }
            }
        }

        [Test]
        public void BacktestingOptionChainProviderWeeklyIndexOption()
        {
            var spxWeeklyOption = Symbol.CreateCanonicalOption(Symbols.SPX, "SPXW", null, null);
            foreach (var option in new[] { spxWeeklyOption })
            {
                var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);

                var optionChain = provider.GetOptionContractList(option, new DateTime(2021, 01, 04)).ToList();

                Assert.AreEqual(12, optionChain.Count);
                Assert.AreEqual(3700, optionChain.OrderBy(s => s.ID.StrikePrice).First().ID.StrikePrice);
                Assert.AreEqual(3800, optionChain.OrderBy(s => s.ID.StrikePrice).Last().ID.StrikePrice);

                foreach (var optionSymbol in optionChain)
                {
                    Assert.AreEqual("SPXW", optionSymbol.ID.Symbol);
                    Assert.AreEqual("SPX", optionSymbol.Underlying.ID.Symbol);
                }
            }
        }

        [Test]
        public void BacktestingOptionChainProviderResolvesSymbolMapping()
        {
            var ticker = "GOOCV"; // Old ticker, should resolve and fetch GOOG
            var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);

            var underlyingSymbol = QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA);
            var alias = "?" + underlyingSymbol.Value;
            var optionSymbol = Symbol.CreateOption(
                underlyingSymbol,
                underlyingSymbol.ID.Market,
                Symbol.GetOptionTypeFromUnderlying(underlyingSymbol).DefaultOptionStyle(),
                default(OptionRight),
                0,
                SecurityIdentifier.DefaultDate,
                alias);

            var googOptionChain = provider.GetOptionContractList(optionSymbol.Underlying, new DateTime(2015, 12, 23))
                .ToList();

            Assert.AreEqual(118, googOptionChain.Count);
            Assert.AreEqual(600m, googOptionChain.OrderBy(s => s.ID.StrikePrice).First().ID.StrikePrice);
            Assert.AreEqual(800m, googOptionChain.OrderBy(s => s.ID.StrikePrice).Last().ID.StrikePrice);
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
            var provider = new LiveOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);

            var spxOption = Symbol.CreateCanonicalOption(Symbols.SPX);
            var spxwOption = Symbol.CreateCanonicalOption(Symbols.SPX, "SPXW", null, null);

            foreach (var symbol in new[] { Symbols.SPY, Symbols.AAPL, Symbols.MSFT, Symbols.SPX, spxOption, spxwOption })
            {
                var result = provider.GetOptionContractList(symbol, DateTime.Today).ToList();
                var countCall = result.Count(x => x.ID.OptionRight == OptionRight.Call);
                var countPut = result.Count(x => x.ID.OptionRight == OptionRight.Put);

                Assert.Greater(countCall, 0);
                Assert.Greater(countPut, 0);

                var expectedOptionTicker = symbol.ID.Symbol;
                var expectedUnderlyingTicker = symbol.ID.Symbol;
                if (symbol.ID.Symbol == "SPXW")
                {
                    expectedUnderlyingTicker = "SPX";
                }
                foreach (var optionSymbol in result)
                {
                    Assert.AreEqual(expectedOptionTicker, optionSymbol.ID.Symbol);
                    Assert.AreEqual(expectedUnderlyingTicker, optionSymbol.Underlying.ID.Symbol);
                }
            }
        }

        [Test]
        public void LiveOptionChainProviderReturnsNoDataForInvalidSymbol()
        {
            var symbol = Symbol.Create("ABCDEF123", SecurityType.Equity, Market.USA);

            var provider = new LiveOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
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
            var provider = new LiveOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
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

            var provider = new LiveOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            var result = provider.GetOptionContractList(underlyingFuture, december);

            Assert.AreEqual(0, result.Count());
        }

        [TestCase(OptionRight.Call, 1650, 2020, 3, 26)]
        [TestCase(OptionRight.Put, 1540, 2020, 3, 26)]
        [TestCase(OptionRight.Call, 1600, 2020, 2, 25)]
        [TestCase(OptionRight.Put, 1545, 2020, 2, 25)]
        public void BacktestingOptionChainProviderReturnsMultipleContractsForZipFileContainingMultipleContracts(
            OptionRight right,
            int strike,
            int year,
            int month,
            int day)
        {
            var underlying = Symbol.CreateFuture("GC", Market.COMEX, new DateTime(2020, 4, 28));
            var expiry = new DateTime(year, month, day);
            var expectedOption = Symbol.CreateOption(
                underlying,
                Market.COMEX,
                OptionStyle.American,
                right,
                strike,
                expiry);

            var provider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            var contracts = provider.GetOptionContractList(underlying, new DateTime(2020, 1, 5))
                .ToHashSet();

            Assert.IsTrue(
                contracts.Contains(expectedOption),
                $"Failed to find contract {expectedOption} in: [{string.Join(", ", contracts.Select(s => s.ToString()))}");
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
