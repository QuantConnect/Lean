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

        [TestCaseSource(nameof(OptionChainTestCases))]
        public void GetsFullDataOptionChainAsDataFrame(Symbol symbol, DateTime date)
        {
            _algorithm.SetPandasConverter();
            _algorithm.SetDateTime(date.ConvertToUtc(_algorithm.TimeZone));

            using var _ = Py.GIL();

            var module = PyModule.FromString(nameof(GetsFullDataOptionChainAsDataFrame), @"
def get_option_chain_data_from_dataframe(algorithm, canonical):
    option_chain_df = algorithm.option_chain(canonical).data_frame

    # Will make it more complex than it needs to be,
    # just so that we can test indexing by symbol using df.loc[]
    for symbol in option_chain_df.index:
        symbol_data = option_chain_df.loc[symbol]

        if symbol_data.shape[0] != 21:
            raise ValueError(f'Expected 21 row for {symbol}, got {symbol_data.shape[0]}')

        yield {
            'symbol': symbol,
            'expiry': symbol_data['expiry'],
            'strike': symbol_data['strike'],
            'right': symbol_data['right'],
            'style': symbol_data['style'],
            'lastprice': symbol_data['lastprice'],
            'askprice': symbol_data['askprice'],
            'bidprice': symbol_data['bidprice'],
            'openinterest': symbol_data['openinterest'],
            'impliedvolatility': symbol_data['impliedvolatility'],
            'delta': symbol_data['delta'],
            'gamma': symbol_data['gamma'],
            'vega': symbol_data['vega'],
            'theta': symbol_data['theta'],
            'rho': symbol_data['rho'],
            'underlyingsymbol': symbol_data['underlyingsymbol'],
            'underlyinglastprice': symbol_data['underlyinglastprice'],
        }
");

            using var pyAlgorithm = _algorithm.ToPython();
            using var pySymbol = symbol.ToPython();

            using var pyOptionChainData = module.GetAttr("get_option_chain_data_from_dataframe").Invoke(pyAlgorithm, pySymbol);
            var optionChain = new List<Symbol>();

            Assert.DoesNotThrow(() =>
            {
                foreach (PyObject item in pyOptionChainData.GetIterator())
                {
                    var contractSymbol = item["symbol"].GetAndDispose<Symbol>();
                    optionChain.Add(contractSymbol);
                    item.DisposeSafely();
                }
            });

            var optionContractsSymbols = _optionChainProvider.GetOptionContractList(symbol, date.Date).ToList();

            CollectionAssert.AreEquivalent(optionContractsSymbols, optionChain);
        }

        [Test]
        public void IndexOptionChainApisAreConsistent()
        {
            var date = new DateTime(2015, 12, 24);
            _algorithm.SetDateTime(date.ConvertToUtc(_algorithm.TimeZone));

            var symbol = Symbols.SPX;
            var exchange = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

            var chainFromAlgorithmApi = _algorithm.OptionChain(symbol).Select(x => x.Symbol).ToList();
            var chainFromChainProviderApi = _optionChainProvider.GetOptionContractList(symbol, date.ConvertTo(_algorithm.TimeZone, exchange.TimeZone)).ToList();

            CollectionAssert.AreEquivalent(chainFromAlgorithmApi, chainFromChainProviderApi);
        }
    }
}
