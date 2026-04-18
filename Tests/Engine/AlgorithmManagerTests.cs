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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Server;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using Log = QuantConnect.Logging.Log;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class AlgorithmManagerTests
    {
        [TestCase(AlgorithmStatus.Deleted)]
        [TestCase(AlgorithmStatus.Stopped)]
        [TestCase(AlgorithmStatus.Liquidated)]
        [TestCase(AlgorithmStatus.RuntimeError)]
        public void MonitorsAlgorithmState(AlgorithmStatus algorithmStatus)
        {
            AlgorithmManagerAlgorithmStatusTest.Loops = 0;
            AlgorithmManagerAlgorithmStatusTest.AlgorithmStatus = algorithmStatus;
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("QuantConnect.Tests.Engine.AlgorithmManagerTests+AlgorithmManagerAlgorithmStatusTest",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "0"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "0%"},
                    {"Drawdown", "0%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "0%"},
                    {"Sharpe Ratio", "0"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0"},
                    {"Beta", "0"},
                    {"Annual Standard Deviation", "0"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "0"},
                    {"Tracking Error", "0"},
                    {"Treynor Ratio", "0"},
                    {"Total Fees", "$0.00"}
                },
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                algorithmLocation: "QuantConnect.Tests.dll");

            Assert.AreEqual(1, AlgorithmManagerAlgorithmStatusTest.Loops);
        }

        [Test, Explicit("TravisExclude")]
        public void TestAlgorithmManagerSpeed()
        {
            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            var algorithmManager = new AlgorithmManager(false);
            var job = new BacktestNodePacket(1, 2, "3", null, 9m, $"{nameof(AlgorithmManagerTests)}.{nameof(TestAlgorithmManagerSpeed)}");
            var feed = new MockDataFeed();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(feed,
                new UniverseSelection(
                    algorithm,
                    new SecurityService(algorithm.Portfolio.CashBook,
                        marketHoursDatabase,
                        symbolPropertiesDataBase,
                        algorithm,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCacheProvider(algorithm.Portfolio),
                        algorithm: algorithm), dataPermissionManager,
                    TestGlobals.DataProvider),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                false,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);
            algorithm.SubscriptionManager.SetDataManager(dataManager);
            var transactions = new BacktestingTransactionHandler();
            var results = new BacktestingResultHandler();
            var realtime = new BacktestingRealTimeHandler();
            using var leanManager = new NullLeanManager();
            var nullSynchronizer = new NullSynchronizer(algorithm);

            algorithm.Initialize();
            algorithm.PostInitialize();

            using var messaging = new QuantConnect.Messaging.Messaging();
            using var api = new Api.Api();
            results.Initialize(new (job, messaging, api, transactions, null));
            results.SetAlgorithm(algorithm, algorithm.Portfolio.TotalPortfolioValue);
            using var backtestingBrokerage = new BacktestingBrokerage(algorithm);
            transactions.Initialize(algorithm, backtestingBrokerage, results);
            feed.Initialize(algorithm, job, results, null, null, null, dataManager, null, null);

            Log.Trace("Starting algorithm manager loop to process " + nullSynchronizer.Count + " time slices");
            var sw = Stopwatch.StartNew();
            using var tokenSource = new CancellationTokenSource();
            algorithmManager.Run(job, algorithm, nullSynchronizer, transactions, results, realtime, leanManager, tokenSource);
            sw.Stop();

            realtime.Exit();
            results.Exit();
            var thousands = nullSynchronizer.Count / 1000d;
            var seconds = sw.Elapsed.TotalSeconds;
            Log.Trace("COUNT: " + nullSynchronizer.Count + "  KPS: " + thousands/seconds);
        }

        public class NullLeanManager : ILeanManager
        {
            public void Dispose()
            {
            }

            public void Initialize(LeanEngineSystemHandlers systemHandlers,
                LeanEngineAlgorithmHandlers algorithmHandlers,
                AlgorithmNodePacket job,
                AlgorithmManager algorithmManager)
            {
            }

            public void SetAlgorithm(IAlgorithm algorithm)
            {
            }

            public void Update()
            {
            }

            public void OnAlgorithmStart()
            {
            }

            public void OnAlgorithmEnd()
            {
            }

            public void OnSecuritiesChanged(SecurityChanges changes)
            {
            }
        }

        class NullResultHandler : IResultHandler
        {
            public ConcurrentQueue<Packet> Messages { get; set; }
            public bool IsActive { get; }

            public void OnSecuritiesChanged(SecurityChanges changes)
            {
            }

            public void DebugMessage(string message)
            {
            }

            public void SystemDebugMessage(string message)
            {
            }

            public void SecurityType(List<SecurityType> types)
            {
            }

            public void LogMessage(string message)
            {
            }

            public void ErrorMessage(string error, string stacktrace = "")
            {
            }

            public void RuntimeError(string message, string stacktrace = "")
            {
            }

            public void BrokerageMessage(BrokerageMessageEvent brokerageMessageEvent)
            {
            }

            public void Sample(DateTime time)
            {
            }

            public void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue)
            {
            }

            public void SendStatusUpdate(AlgorithmStatus status, string message = "")
            {
            }

            public void RuntimeStatistic(string key, string value)
            {
            }

            public void OrderEvent(OrderEvent newEvent)
            {
            }

            public void Exit()
            {
            }

            public void ProcessSynchronousEvents(bool forceProcess = false)
            {
            }

            public void SaveResults(string name, Result result)
            {
            }

            public void SetDataManager(IDataFeedSubscriptionManager dataManager)
            {
            }

            public StatisticsResults StatisticsResults()
            {
                return new StatisticsResults();
            }

            public void SetSummaryStatistic(string name, string value)
            {
            }

            public void AlgorithmTagsUpdated(HashSet<string> tags)
            {
            }

            public void AlgorithmNameUpdated(string name)
            {
            }

            public void Initialize(ResultHandlerInitializeParameters parameters)
            {
            }
        }

        class NullRealTimeHandler : IRealTimeHandler
        {
            public void Add(ScheduledEvent scheduledEvent)
            {
            }

            public void Remove(ScheduledEvent scheduledEvent)
            {
            }

            public bool IsActive { get; }
            public void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api, IIsolatorLimitResultProvider isolatorLimitProvider)
            {
            }

            public void Run()
            {
            }

            public void SetTime(DateTime time)
            {
            }

            public void ScanPastEvents(DateTime time)
            {
            }

            public void Exit()
            {
            }

            public void OnSecuritiesChanged(SecurityChanges changes)
            {
            }
        }

        class NullSynchronizer : ISynchronizer
        {
            private DateTime _frontierUtc;
            private readonly DateTime _endTimeUtc;
            private readonly List<BaseData> _data = new List<BaseData>();
            private readonly List<UpdateData<SubscriptionDataConfig>> _consolidatorUpdateData = new List<UpdateData<SubscriptionDataConfig>>();
            private readonly List<TimeSlice> _timeSlices = new List<TimeSlice>();
            private readonly TimeSpan _frontierStepSize = TimeSpan.FromSeconds(1);
            private readonly List<UpdateData<ISecurityPrice>> _securitiesUpdateData = new List<UpdateData<ISecurityPrice>>();
            public int Count => _timeSlices.Count;

            public NullSynchronizer(IAlgorithm algorithm)
            {
                _frontierUtc = algorithm.StartDate.ConvertToUtc(algorithm.TimeZone);
                _endTimeUtc = algorithm.EndDate.ConvertToUtc(algorithm.TimeZone);
                foreach (var kvp in algorithm.Securities)
                {
                    var security = kvp.Value;
                    var tick = new Tick
                    {
                        Symbol = security.Symbol,
                        EndTime = _frontierUtc.ConvertFromUtc(security.Exchange.TimeZone)
                    };
                    _data.Add(tick);
                    _securitiesUpdateData.Add(new UpdateData<ISecurityPrice>(security, typeof(Tick), new BaseData[] { tick }, false));
                    _consolidatorUpdateData.Add(new UpdateData<SubscriptionDataConfig>(security.Subscriptions.First(), typeof(Tick), new BaseData[] { tick }, false));
                }

                _timeSlices.AddRange(GenerateTimeSlices().Take(int.MaxValue / 1000));
            }

            public IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
            {
                return _timeSlices;
            }

            private IEnumerable<TimeSlice> GenerateTimeSlices()
            {
                var bars = new TradeBars();
                var quotes = new QuoteBars();
                var ticks = new Ticks();
                var options = new OptionChains();
                var futures = new FuturesChains();
                var splits = new Splits();
                var dividends = new Dividends();
                var delistings = new Delistings();
                var symbolChanges = new SymbolChangedEvents();
                var marginInterestRates = new MarginInterestRates();
                var dataFeedPackets = new List<DataFeedPacket>();
                var customData = new List<UpdateData<ISecurityPrice>>();
                var changes = SecurityChanges.None;
                do
                {
                    var slice = new Slice(default(DateTime), _data, bars, quotes, ticks, options, futures, splits, dividends, delistings, symbolChanges, marginInterestRates, default(DateTime));
                    var timeSlice = new TimeSlice(_frontierUtc, _data.Count, slice, dataFeedPackets, _securitiesUpdateData, _consolidatorUpdateData, customData, changes, new Dictionary<Universe, BaseDataCollection>());
                    yield return timeSlice;
                    _frontierUtc += _frontierStepSize;
                }
                while (_frontierUtc <= _endTimeUtc);
            }
        }

        public class AlgorithmManagerAlgorithmStatusTest : BasicTemplateDailyAlgorithm
        {
            public static int Loops { get; set; }
            public static AlgorithmStatus AlgorithmStatus { get; set; }

            public AlgorithmManagerAlgorithmStatusTest() : base()
            {
            }
            public override void OnData(Slice data)
            {
                ++Loops;
                SetStatus(AlgorithmStatus);
            }
        }
    }
}
