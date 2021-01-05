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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Brokerages.Paper;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Messaging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Brokerages.Paper
{
    [TestFixture]
    public class PaperBrokerageTests
    {
        [Test]
        public void AppliesDividendDistributionDirectlyToPortfolioCashBook()
        {
            // init algorithm
            var algorithm = new AlgorithmStub(new MockDataFeed());
            algorithm.AddSecurities(equities: new List<string> {"SPY"});
            algorithm.PostInitialize();

            // init holdings
            var SPY = algorithm.Securities[Symbols.SPY];
            SPY.SetMarketPrice(new Tick {Value = 100m});
            SPY.Holdings.SetHoldings(100m, 1000);

            // resolve expected outcome
            var USD = algorithm.Portfolio.CashBook[Currencies.USD];
            var preDistributionCash = USD.Amount;
            var distributionPerShare = 10m;
            var expectedTotalDistribution = distributionPerShare * SPY.Holdings.Quantity;

            // create slice w/ dividend
            var slice = new Slice(algorithm.Time, new List<BaseData>());
            slice.Dividends.Add(new Dividend(Symbols.SPY, algorithm.Time, distributionPerShare, 100m));
            algorithm.SetCurrentSlice(slice);

            // invoke brokerage
            var brokerage = new PaperBrokerage(algorithm, null);
            brokerage.Scan();

            // verify results
            var postDistributionCash = USD.Amount;
            Assert.AreEqual(preDistributionCash + expectedTotalDistribution, postDistributionCash);
        }

        [Test]
        public void AppliesDividendsOnce()
        {
            // init algorithm
            var algorithm = new AlgorithmStub(new MockDataFeed());
            algorithm.SetLiveMode(true);
            var dividend = new Dividend(Symbols.SPY, DateTime.UtcNow, 10m, 100m);

            var feed = new MockDataFeed();

            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(feed,
                new UniverseSelection(
                    algorithm,
                    new SecurityService(algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, algorithm, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(algorithm.Portfolio)),
                    dataPermissionManager,
                    new DefaultDataProvider()),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                true,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);
            var synchronizer = new NullSynchronizer(algorithm, dividend);

            algorithm.SubscriptionManager.SetDataManager(dataManager);
            algorithm.AddSecurities(equities: new List<string> {"SPY"});
            algorithm.Securities[Symbols.SPY].Holdings.SetHoldings(100m, 1);
            algorithm.PostInitialize();

            var initializedCash = algorithm.Portfolio.CashBook[Currencies.USD].Amount;

            // init algorithm manager
            var manager = new AlgorithmManager(true);
            var job = new LiveNodePacket
            {
                UserId = 1,
                ProjectId = 2,
                DeployId = $"{nameof(PaperBrokerageTests)}.{nameof(AppliesDividendsOnce)}"
            };
            var results = new LiveTradingResultHandler();
            var transactions = new BacktestingTransactionHandler();
            var brokerage = new PaperBrokerage(algorithm, job);

            // initialize results and transactions
            results.Initialize(job, new EventMessagingHandler(), new Api.Api(), transactions);
            results.SetAlgorithm(algorithm, algorithm.Portfolio.TotalPortfolioValue);
            transactions.Initialize(algorithm, brokerage, results);

            var realTime = new BacktestingRealTimeHandler();
            // run algorithm manager
            manager.Run(job,
                algorithm,
                synchronizer,
                transactions,
                results,
                realTime,
                new AlgorithmManagerTests.NullLeanManager(),
                new AlgorithmManagerTests.NullAlphaHandler(),
                new CancellationToken()
            );

            var postDividendCash = algorithm.Portfolio.CashBook[Currencies.USD].Amount;

            realTime.Exit();
            results.Exit();
            Assert.AreEqual(initializedCash + dividend.Distribution, postDividendCash);
        }

        class NullSynchronizer : ISynchronizer
        {
            private readonly IAlgorithm _algorithm;
            private readonly Dividend _dividend;
            private readonly Symbol _symbol;
            private readonly TimeSliceFactory _timeSliceFactory;

            public NullSynchronizer(IAlgorithm algorithm, Dividend dividend)
            {
                _algorithm = algorithm;
                _dividend = dividend;
                _symbol = dividend.Symbol;
                _timeSliceFactory = new TimeSliceFactory(TimeZones.NewYork);
            }

            public IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
            {
                var dataFeedPacket = new DataFeedPacket(_algorithm.Securities[_symbol],
                    _algorithm.SubscriptionManager.Subscriptions.First(s => s.Symbol == _symbol),
                    new List<BaseData> { _dividend }, Ref.CreateReadOnly(() => false));

                yield return _timeSliceFactory.Create(DateTime.UtcNow,
                    new List<DataFeedPacket> { dataFeedPacket },
                    SecurityChanges.None,
                    new Dictionary<Universe, BaseDataCollection>()
                );
            }
        }
    }
}
