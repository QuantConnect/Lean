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
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using QuantConnect.Tests.Brokerages.Models;
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
            var slice = new Slice(algorithm.Time, new List<BaseData>(), algorithm.Time);
            slice.Dividends.Add(new Dividend(Symbols.SPY, algorithm.Time, distributionPerShare, 100m));
            algorithm.SetCurrentSlice(slice);

            // invoke brokerage
            using var brokerage = new PaperBrokerage(algorithm, null);
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
                    new SecurityService(algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, algorithm, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(algorithm.Portfolio), algorithm: algorithm),
                    dataPermissionManager,
                    TestGlobals.DataProvider),
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
            using var brokerage = new PaperBrokerage(algorithm, job);

            // initialize results and transactions
            using var eventMessagingHandler = new EventMessagingHandler();
            using var api = new Api.Api();
            results.Initialize(new (job, eventMessagingHandler, api, transactions, null));
            results.SetAlgorithm(algorithm, algorithm.Portfolio.TotalPortfolioValue);
            transactions.Initialize(algorithm, brokerage, results);

            var realTime = new BacktestingRealTimeHandler();
            using var nullLeanManager = new AlgorithmManagerTests.NullLeanManager();

            using var tokenSource = new CancellationTokenSource();
            // run algorithm manager
            manager.Run(job,
                algorithm,
                synchronizer,
                transactions,
                results,
                realTime,
                nullLeanManager,
                tokenSource
            );

            var postDividendCash = algorithm.Portfolio.CashBook[Currencies.USD].Amount;

            realTime.Exit();
            results.Exit();
            Assert.AreEqual(initializedCash + dividend.Distribution, postDividendCash);
        }

        [Test]
        public void PredictableCashSettlement()
        {
            var symbol = Symbols.SPY;
            var securityPrice = 550m;
            var initialCashBalance = 100_000m;
            var defaultSettlementTime = Securities.Equity.Equity.DefaultSettlementTime;
            // Time at which cash sync is typically performed, based on system log (TRACE:: Brokerage.PerformCashSync())
            var performCashSyncTimeSpan = new TimeSpan(11, 45, 0);

            var feed = new MockDataFeed();
            var algorithm = new AlgorithmStub(feed);
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm, new MockDataFeed()));

            // Initialize()
            algorithm.SetStartDate(2025, 03, 30);
            algorithm.SetEndDate(2025, 04, 02);
            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);
            var security = algorithm.AddSecurity(symbol.ID.SecurityType, symbol.ID.Symbol);
            algorithm.PostInitialize();

            // Update Security Price like AlgorithmManager
            security.Update([new Tick(algorithm.Time, symbol, string.Empty, string.Empty, 10m, securityPrice)], typeof(TradeBar));

            var portfolio = algorithm.Portfolio;
            using var brokerage = new PaperBrokerageWithManualCashBalance(algorithm, new LiveNodePacket(), initialCashBalance: initialCashBalance);

            // Sync initial cash state with the brokerage
            brokerage.PerformCashSync(algorithm, algorithm.Time, () => TimeSpan.Zero);

            // Market SPY 10
            var buyQuantity = 10;
            portfolio.ProcessFills([new OrderEvent(new MarketOrder(symbol, buyQuantity, algorithm.Time), algorithm.Time, OrderFee.Zero)
            { FillPrice = security.Price, FillQuantity = buyQuantity }]);

            var totalMarginUserAfterBuy = portfolio.TotalMarginUsed;
            var marginRemainingAfterBuy = portfolio.MarginRemaining;

            // Manually decrease the brokerage cash balance to simulate the cash outflow
            brokerage.DecreaseCashBalance(buyQuantity * security.Price);
            brokerage.PerformCashSync(algorithm, algorithm.Time, () => TimeSpan.Zero);

            // Advance to the next day to simulate settlement
            var timeUtc = algorithm.Time.AddDays(1).ConvertToUtc(algorithm.TimeZone);
            algorithm.SetDateTime(timeUtc);
            portfolio.Securities[symbol].SettlementModel.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            brokerage.PerformCashSync(algorithm, timeUtc, () => TimeSpan.Zero);

            // Validate: After syncing cash and waiting for settlement, portfolio state should be correct
            Assert.AreEqual(portfolio.TotalMarginUsed, totalMarginUserAfterBuy);
            Assert.AreEqual(portfolio.MarginRemaining, marginRemainingAfterBuy);
            Assert.AreEqual(portfolio.UnsettledCash, 0m);

            // Market SPY -10
            var sellQuantity = -10;
            portfolio.ProcessFills([new OrderEvent(new MarketOrder(symbol, sellQuantity, algorithm.Time), algorithm.Time, OrderFee.Zero)
            { FillPrice = security.Price, FillQuantity = sellQuantity }]);

            // Simulate brokerage immediately crediting the cash from the sell, before Lean's internal settlement
            brokerage.IncreaseCashBalance(Math.Abs(sellQuantity) * security.Price);

            // Move to just before the settlement time (T+1 - 1 minute)
            timeUtc = algorithm.Time.Add(defaultSettlementTime.Subtract(Time.OneMinute)).ConvertToUtc(algorithm.TimeZone);
            algorithm.SetDateTime(timeUtc);

            // At this point, brokerage has credited the cash, but Lean still considers it unsettled
            Assert.Greater(portfolio.UnsettledCash, 0m);

            // Advance 1 minute to reach full settlement time (T+1)
            timeUtc = algorithm.Time.Add(Time.OneMinute).ConvertToUtc(algorithm.TimeZone);
            algorithm.SetDateTime(timeUtc);

            if (algorithm.Time.ConvertToUtc(algorithm.TimeZone).TimeOfDay < performCashSyncTimeSpan)
            {
                // Lean clears the unsettled cash to available balance
                portfolio.Securities[symbol].SettlementModel.Scan(new ScanSettlementModelParameters(portfolio, security, timeUtc));
            }

            brokerage.PerformCashSync(algorithm, timeUtc, () => TimeSpan.Zero);

            Assert.AreEqual(0m, portfolio.UnsettledCash);

            // Brokerage UnsettledCash + Lean UnsettledCash
            Assert.AreEqual(portfolio.TotalPortfolioValue, initialCashBalance);
            var orderRequestMarginRemaining = portfolio.TotalMarginUsed * 2 + portfolio.MarginRemaining;
            Assert.AreEqual(portfolio.TotalPortfolioValue, orderRequestMarginRemaining);
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
