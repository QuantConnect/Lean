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
        }

        private static TestCaseData[] OptionChainTestCases = new TestCaseData[]
        {
            // By underlying
            new(Symbols.AAPL, new DateTime(2014, 06, 06)),
            new(Symbols.SPX, new DateTime(2021, 01, 04)),
            // By canonical
            new(Symbol.CreateCanonicalOption(Symbols.AAPL), new DateTime(2014, 06, 06)),
            new(Symbol.CreateCanonicalOption(Symbols.SPX), new DateTime(2021, 01, 04))
        };

        [TestCaseSource(nameof(OptionChainTestCases))]
        public void GetsFullDataOptionChain(Symbol symbol, DateTime date)
        {
            _algorithm.SetDateTime(date.ConvertToUtc(_algorithm.TimeZone));
            var optionContractsData = _algorithm.OptionChain(symbol).ToList();

            var optionContractsSymbols = _optionChainProvider.GetOptionContractList(symbol, date).ToList();

            CollectionAssert.AreEquivalent(optionContractsSymbols, optionContractsData.Select(x => x.Symbol));
        }

        [Test]
        public void CannotGetFutureOptionsChain()
        {
            var result = _algorithm.OptionChain(Symbols.ES_Future_Chain).ToList();
            Assert.IsEmpty(result);
            Assert.IsTrue(_algorithm.LogMessages.Any(x => x.Contains($"Warning: QCAlgorithm.{nameof(QCAlgorithm.OptionChain)} method cannot be used to get future options chains yet. Until support is added, please fall back to the {nameof(QCAlgorithm.OptionChainProvider)}.", StringComparison.InvariantCulture)));
        }
    }
}
