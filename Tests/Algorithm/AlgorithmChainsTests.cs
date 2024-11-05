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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
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

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var historyProvider = Composer.Instance.GetExportedValueByTypeName<IHistoryProvider>("SubscriptionDataReaderHistoryProvider", true);
            var parameters = new HistoryProviderInitializeParameters(null, null, TestGlobals.DataProvider, TestGlobals.DataCacheProvider,
                TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, (_) => { }, true, new DataPermissionManager(), null,
                new AlgorithmSettings());
            try
            {
                historyProvider.Initialize(parameters);
            }
            catch (InvalidOperationException)
            {
               // Already initialized
            }

            _algorithm = new QCAlgorithm();
            _algorithm.SetHistoryProvider(historyProvider);
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));

            _optionChainProvider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            _algorithm.SetOptionChainProvider(_optionChainProvider);
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

                symbols = AssertUnflattenedSingleChainDataFrame(dataFrame, symbol);
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
            var chainsTotalCount = expectedOptionChains.Values.Sum(x => x.Count);

            if (flatten)
            {
                var dfLength = dataFrame.GetAttr("shape")[0].GetAndDispose<int>();
                Assert.AreEqual(chainsTotalCount, dfLength);

                Assert.Multiple(() =>
                {
                    foreach (var (symbol, expectedChain) in expectedOptionChains)
                    {
                        var chainSymbols = AssertFlattenedSingleChainDataFrame(dataFrame, symbol);

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
                    foreach (var (symbol, expectedChain) in expectedOptionChains)
                    {
                        var chainSymbols = AssertUnflattenedSingleChainDataFrame(dataFrame, symbol);

                        Assert.IsNotNull(chainSymbols);
                        CollectionAssert.AreEquivalent(expectedChain, chainSymbols);
                    }
                });
            }
        }

        private static List<Symbol> AssertFlattenedSingleChainDataFrame(PyObject dataFrame, Symbol symbol, bool hasCanonicalIndex = true)
        {
            PyObject subDataFrame = null;
            try
            {
                subDataFrame = hasCanonicalIndex ? GetCanonicalSubDataFrame(dataFrame, symbol) : dataFrame;

                using var dfColumns = subDataFrame.GetAttr("columns");
                using var dfColumnsList = dfColumns.InvokeMethod("tolist");
                using var dfColumnsIterator = dfColumnsList.GetIterator();
                var columns = new List<string>();
                foreach (PyObject item in dfColumnsIterator)
                {
                    columns.Add(item.ToString());
                    item.DisposeSafely();
                }

                var expectedColumns = symbol.SecurityType != SecurityType.Future && symbol.SecurityType != SecurityType.FutureOption
                    ? new[]
                    {
                        "expiry", "strike", "right", "style", "volume", "delta", "gamma", "vega", "theta", "rho", "underlyingsymbol"
                    }
                    : new[]
                    {
                        "expiry", "strike", "scaledstrike", "right", "style", "volume", "underlyingsymbol"
                    };

                CollectionAssert.IsSubsetOf(expectedColumns, columns);
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

        private static List<Symbol> AssertUnflattenedSingleChainDataFrame(PyObject dataFrame, Symbol symbol)
        {
            using var subDataFrame = GetCanonicalSubDataFrame(dataFrame, symbol);

            using var dfOptionChainList = subDataFrame["contracts"];
            var contracts = dfOptionChainList.GetAndDispose<IEnumerable<OptionContract>>().ToList();

            return contracts.Select(x => x.Symbol).ToList();
        }

        private static PyObject GetCanonicalSubDataFrame(PyObject dataFrame, Symbol symbol)
        {
            var canonicalSymbol = symbol.SecurityType.IsOption() ? symbol : Symbol.CreateCanonicalOption(symbol);
            using var pySymbol = canonicalSymbol.ToPython();
            return dataFrame.GetAttr("loc")[pySymbol];
        }

        [TestCase(2015, 12, 23, 23)]
        [TestCase(2015, 12, 24, 0)]
        [TestCase(2015, 12, 24, 1)]
        [TestCase(2015, 12, 24, 2)]
        [TestCase(2015, 12, 24, 6)]
        [TestCase(2015, 12, 24, 12)]
        [TestCase(2015, 12, 24, 16)]
        public void IndexOptionChainApisAreConsistent(int year, int month, int day, int hour)
        {
            var dateTime = new DateTime(year, month, day, hour, 0, 0);
            _algorithm.SetDateTime(dateTime.ConvertToUtc(_algorithm.TimeZone));

            var symbol = Symbols.SPX;
            var exchange = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

            var chainFromAlgorithmApi = _algorithm.OptionChain(symbol).Select(x => x.Symbol).ToList();
            var chainFromChainProviderApi = _optionChainProvider.GetOptionContractList(symbol, dateTime.ConvertTo(_algorithm.TimeZone, exchange.TimeZone)).ToList();

            CollectionAssert.IsNotEmpty(chainFromAlgorithmApi);
            CollectionAssert.AreEquivalent(chainFromAlgorithmApi, chainFromChainProviderApi);
        }
    }
}
