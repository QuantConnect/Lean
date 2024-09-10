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
using QuantConnect.Algorithm;
using QuantConnect.Data;
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
            historyProvider.Initialize(parameters);

            _algorithm = new QCAlgorithm();
            _algorithm.SetHistoryProvider(historyProvider);
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));

            _optionChainProvider = new BacktestingOptionChainProvider(TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            _algorithm.SetOptionChainProvider(_optionChainProvider);
        }

        private static TestCaseData[] OptionChainTestCases = new TestCaseData[]
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
    }
}
