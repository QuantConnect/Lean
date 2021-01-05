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
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Alpha;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Server;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using Log = QuantConnect.Logging.Log;

namespace QuantConnect.Tests.Engine
{
    [TestFixture, Category("TravisExclude")]
    public class AlgorithmManagerTests
    {
        [Test]
        public void TestAlgorithmManagerSpeed()
        {
            var algorithmManager = new AlgorithmManager(false);
            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
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
                        new SecurityCacheProvider(algorithm.Portfolio)),
                    dataPermissionManager,
                    new DefaultDataProvider()),
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
            var leanManager = new NullLeanManager();
            var alphas = new NullAlphaHandler();
            var token = new CancellationToken();
            var nullSynchronizer = new NullSynchronizer(algorithm);

            algorithm.Initialize();
            algorithm.PostInitialize();

            results.Initialize(job, new QuantConnect.Messaging.Messaging(), new Api.Api(), transactions);
            results.SetAlgorithm(algorithm, algorithm.Portfolio.TotalPortfolioValue);
            transactions.Initialize(algorithm, new BacktestingBrokerage(algorithm), results);
            feed.Initialize(algorithm, job, results, null, null, null, dataManager, null, null);

            Log.Trace("Starting algorithm manager loop to process " + nullSynchronizer.Count + " time slices");
            var sw = Stopwatch.StartNew();
            algorithmManager.Run(job, algorithm, nullSynchronizer, transactions, results, realtime, leanManager, alphas, token);
            sw.Stop();

            realtime.Exit();
            results.Exit();
            var thousands = nullSynchronizer.Count / 1000d;
            var seconds = sw.Elapsed.TotalSeconds;
            Log.Trace("COUNT: " + nullSynchronizer.Count + "  KPS: " + thousands/seconds);
        }

        public class NullAlphaHandler : IAlphaHandler
        {
            public bool IsActive { get; }
            public AlphaRuntimeStatistics RuntimeStatistics { get; }
            public void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm, IMessagingHandler messagingHandler, IApi api, ITransactionHandler transactionHandler)
            {
            }

            public void OnAfterAlgorithmInitialized(IAlgorithm algorithm)
            {
            }

            public void ProcessSynchronousEvents()
            {
            }

            public void Run()
            {
            }

            public void Exit()
            {
            }
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
        }

        class NullResultHandler : IResultHandler
        {
            public ConcurrentQueue<Packet> Messages { get; set; }
            public bool IsActive { get; }

            public void OnSecuritiesChanged(SecurityChanges changes)
            {
            }

            public void Initialize(AlgorithmNodePacket job,
                IMessagingHandler messagingHandler,
                IApi api,
                ITransactionHandler transactionHandler)
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

            public void Sample(DateTime time, bool force = false)
            {
            }

            public void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue)
            {
            }

            public void SetAlphaRuntimeStatistics(AlphaRuntimeStatistics statistics)
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
                var dataFeedPackets = new List<DataFeedPacket>();
                var customData = new List<UpdateData<ISecurityPrice>>();
                var changes = SecurityChanges.None;
                do
                {
                    var slice = new Slice(default(DateTime), _data, bars, quotes, ticks, options, futures, splits, dividends, delistings, symbolChanges);
                    var timeSlice = new TimeSlice(_frontierUtc, _data.Count, slice, dataFeedPackets, _securitiesUpdateData, _consolidatorUpdateData, customData, changes, new Dictionary<Universe, BaseDataCollection>());
                    yield return timeSlice;
                    _frontierUtc += _frontierStepSize;
                }
                while (_frontierUtc <= _endTimeUtc);
            }
        }
    }
}
