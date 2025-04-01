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
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmChainsTest
    {
        private QCAlgorithm _algorithm;
        private BacktestingOptionChainProvider _optionChainProvider;
        private BacktestingFutureChainProvider _futureChainProvider;

        [SetUp]
        public void SetUp()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SetHistoryProvider(TestGlobals.HistoryProvider);
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));

            var initParameters = new ChainProviderInitializeParameters(TestGlobals.MapFileProvider, TestGlobals.HistoryProvider);
            _optionChainProvider = new BacktestingOptionChainProvider();
            _optionChainProvider.Initialize(initParameters);
            _algorithm.SetOptionChainProvider(_optionChainProvider);

            _futureChainProvider = new BacktestingFutureChainProvider();
            _futureChainProvider.Initialize(initParameters);
            _algorithm.SetFutureChainProvider(_futureChainProvider);
        }

        private static TestCaseData[] OptionChainTestCases => new TestCaseData[]
        {
            // By underlying
            new(Symbols.AAPL, new DateTime(2014, 06, 06, 12, 0, 0)),
            new(Symbols.SPX, new DateTime(2021, 01, 04, 12, 0, 0)),
            // By canonical
            new(Symbol.CreateCanonicalOption(Symbols.AAPL), new DateTime(2014, 06, 06, 12, 0, 0)),
            new(Symbol.CreateCanonicalOption(Symbols.SPX), new DateTime(2021, 01, 04, 12, 0, 0)),
            new(Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 6, 19)), new DateTime(2020, 01, 05, 12, 0, 0)),
        };

        [TestCaseSource(nameof(OptionChainTestCases))]
        public void GetsFullDataOptionChain(Symbol symbol, DateTime date)
        {
            _algorithm.SetDateTime(date.ConvertToUtc(_algorithm.TimeZone));
            var optionContractsData = _algorithm.OptionChain(symbol).ToList();
            Assert.IsNotEmpty(optionContractsData);

            var optionContractsSymbols = _optionChainProvider.GetOptionContractList(symbol, date.Date).ToList();

            CollectionAssert.AreEquivalent(optionContractsSymbols, optionContractsData.Select(x => x.Symbol));
        }

        private static TestCaseData[] PythonOptionChainTestCases => OptionChainTestCases.SelectMany(x =>
        {
            return new object[] { true, false }.Select(y => new TestCaseData(x.OriginalArguments.Concat(new[] { y }).ToArray()));
        }).ToArray();

        [TestCaseSource(nameof(PythonOptionChainTestCases))]
        public void GetsFullDataOptionChainAsDataFrame(Symbol symbol, DateTime date, bool flatten)
        {
            _algorithm.SetPandasConverter();
            _algorithm.SetDateTime(date.ConvertToUtc(_algorithm.TimeZone));

            using var _ = Py.GIL();

            using var dataFrame = _algorithm.OptionChain(symbol, flatten).DataFrame;
            List<Symbol> symbols = null;

            var expectedOptionContractsSymbols = _optionChainProvider.GetOptionContractList(symbol, date.Date).ToList();

            if (flatten)
            {
                symbols = AssertFlattenedSingleChainDataFrame(dataFrame, symbol, hasCanonicalIndex: false);
            }
            else
            {
                var dfLength = dataFrame.GetAttr("shape")[0].GetAndDispose<int>();
                Assert.AreEqual(1, dfLength);

                symbols = AssertUnflattenedSingleChainDataFrame<OptionContract>(dataFrame, symbol);
            }

            Assert.IsNotNull(symbols);
            CollectionAssert.AreEquivalent(expectedOptionContractsSymbols, symbols);
        }

        [Test]
        public void GetsMultipleFullDataOptionChainAsDataFrame([Values] bool flatten)
        {
            var date = new DateTime(2015, 12, 24, 12, 0, 0);
            _algorithm.SetPandasConverter();
            _algorithm.SetDateTime(date.ConvertToUtc(_algorithm.TimeZone));

            using var _ = Py.GIL();

            var symbols = new[] { Symbols.GOOG, Symbols.SPX };
            using var dataFrame = _algorithm.OptionChains(symbols, flatten).DataFrame;

            var expectedOptionChains = symbols.ToDictionary(x => x, x => _optionChainProvider.GetOptionContractList(x, date).ToList());

            AssertMultiChainsDataFrame<OptionContract>(flatten, symbols, dataFrame, expectedOptionChains, isOptionChain: true);
        }

        private static List<Symbol> AssertFlattenedSingleChainDataFrame(PyObject dataFrame, Symbol symbol, bool hasCanonicalIndex = true,
            bool isOptionChain = true)
        {
            PyObject subDataFrame = null;
            try
            {
                subDataFrame = GetCanonicalSubDataFrame(dataFrame, symbol, isOptionChain, hasCanonicalIndex, out var canonicalSymbol);

                using var dfColumns = subDataFrame.GetAttr("columns");
                using var dfColumnsList = dfColumns.InvokeMethod("tolist");
                using var dfColumnsIterator = dfColumnsList.GetIterator();
                var columns = new List<string>();
                foreach (PyObject item in dfColumnsIterator)
                {
                    columns.Add(item.ToString());
                    item.DisposeSafely();
                }

                var expectedColumns = canonicalSymbol.SecurityType switch
                {
                    SecurityType.Future => new[] { "expiry", "volume", "askprice", "asksize", "bidprice", "bidsize", "lastprice", "openinterest" },
                    SecurityType.FutureOption => new[]
                    {
                        "expiry", "strike", "scaledstrike", "right", "style", "volume", "askprice", "asksize", "bidprice", "bidsize",
                        "lastprice", "underlyingsymbol", "underlyinglastprice"
                    },
                    _ => new[]
                    {
                        "expiry", "strike", "scaledstrike", "right", "style", "volume", "askprice", "asksize", "bidprice", "bidsize",
                        "lastprice", "openinterest", "impliedvolatility", "delta", "gamma", "vega", "theta", "rho",
                        "underlyingsymbol", "underlyinglastprice"
                    }
                };

                CollectionAssert.AreEquivalent(expectedColumns, columns);
                using var dfIndex = subDataFrame.GetAttr("index");

                return dfIndex.InvokeMethod("tolist").GetAndDispose<List<Symbol>>();
            }
            finally
            {
                if (hasCanonicalIndex)
                {
                    subDataFrame?.DisposeSafely();
                }
            }
        }

        private static List<Symbol> AssertUnflattenedSingleChainDataFrame<T>(PyObject dataFrame, Symbol symbol, bool isOptionChain = true)
            where T : BaseContract
        {
            using var subDataFrame = GetCanonicalSubDataFrame(dataFrame, symbol, isOptionChain, true, out _);

            using var dfOptionChainList = subDataFrame["contracts"];
            var contracts = dfOptionChainList.GetAndDispose<IEnumerable<T>>().ToList();

            return contracts.Select(x => x.Symbol).ToList();
        }

        private static PyObject GetCanonicalSubDataFrame(PyObject dataFrame, Symbol symbol, bool forOptionChain, bool hasCanonicalIndex,
            out Symbol canonicalSymbol)
        {
            canonicalSymbol = symbol;
            if (canonicalSymbol.SecurityType == SecurityType.Future && !forOptionChain)
            {
                canonicalSymbol = canonicalSymbol.Canonical;
            }
            else if (!canonicalSymbol.SecurityType.IsOption())
            {
                canonicalSymbol = Symbol.CreateCanonicalOption(symbol);
            }

            if (!hasCanonicalIndex)
            {
                return dataFrame;
            }

            using var pySymbol = canonicalSymbol.ToPython();
            return dataFrame.GetAttr("loc")[pySymbol];
        }

        private static IEnumerable<TestCaseData> GetOptionChainApisTestData()
        {
            var indexSymbol = Symbols.SPX;
            var equitySymbol = Symbols.GOOG;
            var futureSymbol = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 6, 19));

            foreach (var withSecurityAdded in new[] { true, false })
            {
                var extendedMarketHoursCases = withSecurityAdded ? [true, false] : new[] { false };
                foreach (var withExtendedMarketHours in extendedMarketHoursCases)
                {
                    yield return new TestCaseData(indexSymbol, new DateTime(2015, 12, 23, 23, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(indexSymbol, new DateTime(2015, 12, 24, 0, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(indexSymbol, new DateTime(2015, 12, 24, 1, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(indexSymbol, new DateTime(2015, 12, 24, 2, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(indexSymbol, new DateTime(2015, 12, 24, 6, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(indexSymbol, new DateTime(2015, 12, 24, 12, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(indexSymbol, new DateTime(2015, 12, 24, 16, 0, 0), withSecurityAdded, withExtendedMarketHours);

                    yield return new TestCaseData(equitySymbol, new DateTime(2015, 12, 24, 0, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(equitySymbol, new DateTime(2015, 12, 24, 1, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(equitySymbol, new DateTime(2015, 12, 24, 2, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(equitySymbol, new DateTime(2015, 12, 24, 6, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(equitySymbol, new DateTime(2015, 12, 24, 12, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(equitySymbol, new DateTime(2015, 12, 24, 16, 0, 0), withSecurityAdded, withExtendedMarketHours);

                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 04, 23, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 05, 0, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 05, 1, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 05, 2, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 05, 6, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 05, 12, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 05, 16, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 06, 0, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 06, 1, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 06, 2, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 06, 6, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 06, 12, 0, 0), withSecurityAdded, withExtendedMarketHours);
                    yield return new TestCaseData(futureSymbol, new DateTime(2020, 01, 06, 16, 0, 0), withSecurityAdded, withExtendedMarketHours);
                }
            }
        }

        [TestCaseSource(nameof(GetOptionChainApisTestData))]
        public void OptionChainApisAreConsistent(Symbol symbol, DateTime dateTime, bool withSecurityAdded, bool withExtendedMarketHours)
        {
            _algorithm.SetDateTime(dateTime.ConvertToUtc(_algorithm.TimeZone));

            if (withSecurityAdded)
            {
                if (symbol.SecurityType == SecurityType.Future)
                {
                    var future = _algorithm.AddFuture(symbol.ID.Symbol, extendedMarketHours: withExtendedMarketHours);
                    _algorithm.AddFutureOption(future.Symbol);
                    _algorithm.AddFutureContract(symbol, extendedMarketHours: withExtendedMarketHours);
                }
                else
                {
                    _algorithm.AddSecurity(symbol, extendedMarketHours: withExtendedMarketHours);
                }
            }

            var exchange  = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var chainFromAlgorithmApi = _algorithm.OptionChain(symbol).Select(x => x.Symbol).ToList();
            var chainFromChainProviderApi = _optionChainProvider.GetOptionContractList(symbol,
                dateTime.ConvertTo(_algorithm.TimeZone, exchange.TimeZone)).ToList();

            CollectionAssert.IsNotEmpty(chainFromAlgorithmApi);
            CollectionAssert.AreEquivalent(chainFromAlgorithmApi, chainFromChainProviderApi);
        }

        private static IEnumerable<TestCaseData> GetFutureChainApisTestData()
        {
            var futureSymbol = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 6, 19));
            var canonicalFutureSymbol = futureSymbol.Canonical;
            var futureOptionSymbol = Symbol.CreateOption(futureSymbol, futureSymbol.ID.Market, OptionStyle.American, OptionRight.Call,
                75m, new DateTime(2020, 5, 19));

            foreach (var symbol in new[] { futureSymbol, canonicalFutureSymbol, futureOptionSymbol })
            {
                foreach (var withFutureAdded in new[] { true, false })
                {
                    var extendedMarketHoursCases = withFutureAdded ? [true, false] : new[] { false };
                    foreach (var withExtendedMarketHours in extendedMarketHoursCases)
                    {
                        yield return new TestCaseData(symbol, new DateTime(2013, 10, 06, 23, 0, 0), withFutureAdded, withExtendedMarketHours);
                        yield return new TestCaseData(symbol, new DateTime(2013, 10, 07, 0, 0, 0), withFutureAdded, withExtendedMarketHours);
                        yield return new TestCaseData(symbol, new DateTime(2013, 10, 07, 1, 0, 0), withFutureAdded, withExtendedMarketHours);
                        yield return new TestCaseData(symbol, new DateTime(2013, 10, 07, 2, 0, 0), withFutureAdded, withExtendedMarketHours);
                        yield return new TestCaseData(symbol, new DateTime(2013, 10, 07, 6, 0, 0), withFutureAdded, withExtendedMarketHours);
                        yield return new TestCaseData(symbol, new DateTime(2013, 10, 07, 12, 0, 0), withFutureAdded, withExtendedMarketHours);
                        yield return new TestCaseData(symbol, new DateTime(2013, 10, 07, 16, 0, 0), withFutureAdded, withExtendedMarketHours);
                    }
                }
            }
        }

        [TestCaseSource(nameof(GetFutureChainApisTestData))]
        public void FuturesChainApisAreConsistent(Symbol symbol, DateTime dateTime, bool withFutureAdded, bool withExtendedMarketHours)
        {
            _algorithm.SetDateTime(dateTime.ConvertToUtc(_algorithm.TimeZone));

            if (withFutureAdded)
            {
                // It should work regardless of whether the future is added to the algorithm
                _algorithm.AddFuture(symbol.ID.Symbol, extendedMarketHours: withExtendedMarketHours);
            }

            var exchange = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var chainFromAlgorithmApi = _algorithm.FuturesChain(symbol).Select(x => x.Symbol).ToList();
            var chainFromChainProviderApi = _futureChainProvider.GetFutureContractList(symbol,
                dateTime.ConvertTo(_algorithm.TimeZone, exchange.TimeZone)).ToList();

            CollectionAssert.IsNotEmpty(chainFromAlgorithmApi);
            CollectionAssert.AreEquivalent(chainFromAlgorithmApi, chainFromChainProviderApi);
        }

        [Test]
        public void GetsFullDataFuturesChainAsDataFrame([Values] bool flatten, [Values] bool withFutureAdded)
        {
            _algorithm.SetPandasConverter();
            var date = new DateTime(2013, 10, 07);
            _algorithm.SetDateTime(date.ConvertToUtc(_algorithm.TimeZone));

            using var _ = Py.GIL();

            // It should work regardless of whether the future is added to the algorithm
            var symbol = withFutureAdded ? _algorithm.AddFuture(Futures.Indices.SP500EMini).Symbol : Symbols.ES_Future_Chain;
            using var dataFrame = _algorithm.FuturesChain(symbol, flatten).DataFrame;
            List<Symbol> symbols = null;

            var exchange = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var exchangeTime = date.ConvertTo(_algorithm.TimeZone, exchange.TimeZone);
            var expectedFutureContractSymbols = _futureChainProvider.GetFutureContractList(symbol, exchangeTime).ToList();

            if (flatten)
            {
                symbols = AssertFlattenedSingleChainDataFrame(dataFrame, symbol, hasCanonicalIndex: false, isOptionChain: false);
            }
            else
            {
                var dfLength = dataFrame.GetAttr("shape")[0].GetAndDispose<int>();
                Assert.AreEqual(1, dfLength);

                symbols = AssertUnflattenedSingleChainDataFrame<FuturesContract>(dataFrame, symbol, isOptionChain: false);
            }

            Assert.IsNotNull(symbols);
            CollectionAssert.AreEquivalent(expectedFutureContractSymbols, symbols);
        }

        [Test]
        public void GetsMultipleFullDataFuturesChainsAsDataFrame([Values] bool flatten, [Values] bool withFutureAdded)
        {
            var dateTime = new DateTime(2013, 10, 07, 12, 0, 0);
            _algorithm.SetPandasConverter();
            _algorithm.SetDateTime(dateTime.ConvertToUtc(_algorithm.TimeZone));

            using var _ = Py.GIL();

            var symbols = withFutureAdded
                ? new[] { Symbols.ES_Future_Chain, Symbols.CreateFuturesCanonicalSymbol(Futures.Dairy.ClassIIIMilk) }
                : new[] { _algorithm.AddFuture(Futures.Indices.SP500EMini).Symbol, _algorithm.AddFuture(Futures.Dairy.ClassIIIMilk).Symbol };
            using var dataFrame = _algorithm.FuturesChains(symbols, flatten).DataFrame;

            var expectedFuturesChains = symbols.ToDictionary(x => x, x =>
            {
                var exchange = MarketHoursDatabase.FromDataFolder().GetExchangeHours(x.ID.Market, x, x.SecurityType);
                return _futureChainProvider.GetFutureContractList(x, dateTime.ConvertTo(_algorithm.TimeZone, exchange.TimeZone)).ToList();
            });

            AssertMultiChainsDataFrame<FuturesContract>(flatten, symbols, dataFrame, expectedFuturesChains, isOptionChain: false);
        }

        private static void AssertMultiChainsDataFrame<T>(bool flatten, Symbol[] symbols, PyObject dataFrame,
            Dictionary<Symbol, List<Symbol>> expectedChains, bool isOptionChain)
            where T : BaseContract
        {
            var chainsTotalCount = expectedChains.Values.Sum(x => x.Count);

            if (flatten)
            {
                var dfLength = dataFrame.GetAttr("shape")[0].GetAndDispose<int>();
                Assert.AreEqual(chainsTotalCount, dfLength);

                Assert.Multiple(() =>
                {
                    foreach (var (symbol, expectedChain) in expectedChains)
                    {
                        var chainSymbols = AssertFlattenedSingleChainDataFrame(dataFrame, symbol, isOptionChain: isOptionChain);

                        Assert.IsNotNull(chainSymbols);
                        CollectionAssert.AreEquivalent(expectedChain, chainSymbols);
                    }
                });
            }
            else
            {
                var dfLength = dataFrame.GetAttr("shape")[0].GetAndDispose<int>();
                Assert.AreEqual(symbols.Length, dfLength);

                Assert.Multiple(() =>
                {
                    foreach (var (symbol, expectedChain) in expectedChains)
                    {
                        var chainSymbols = AssertUnflattenedSingleChainDataFrame<T>(dataFrame, symbol, isOptionChain);

                        Assert.IsNotNull(chainSymbols);
                        CollectionAssert.AreEquivalent(expectedChain, chainSymbols);
                    }
                });
            }
        }
    }
}
