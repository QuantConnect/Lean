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
using System.Collections;
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
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Statistics;
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
            var dataManager = new DataManager(feed, new UniverseSelection(feed, algorithm), algorithm.Settings);
            algorithm.SubscriptionManager.SetDataManager(dataManager);
            var transactions = new BacktestingTransactionHandler();
            var results = new BacktestingResultHandler();
            var realtime = new BacktestingRealTimeHandler();
            var leanManager = new NullLeanManager();
            var alphas = new NullAlphaHandler();
            var token = new CancellationToken();

            algorithm.Initialize();
            algorithm.PostInitialize();

            results.Initialize(job, new QuantConnect.Messaging.Messaging(), new Api.Api(), feed, new BacktestingSetupHandler(), transactions);
            results.SetAlgorithm(algorithm);
            transactions.Initialize(algorithm, new BacktestingBrokerage(algorithm), results);
            feed.Initialize(algorithm, job, results, null, null, null, dataManager);

            Log.Trace("Starting algorithm manager loop to process " + feed.Count + " time slices");
            var sw = Stopwatch.StartNew();
            algorithmManager.Run(job, algorithm, dataManager, transactions, results, realtime, leanManager, alphas, token);
            sw.Stop();

            var thousands = feed.Count / 1000d;
            var seconds = sw.Elapsed.TotalSeconds;
            Log.Trace("COUNT: " + feed.Count + "  KPS: " + thousands/seconds);
        }

        public class MockDataFeed : IDataFeed
        {
            private DateTime _frontierUtc;
            private DateTime _endTimeUtc;
            private readonly List<BaseData> _data = new List<BaseData>();
            public readonly List<UpdateData<Security>> securitiesUpdateData = new List<UpdateData<Security>>();
            private readonly List<UpdateData<SubscriptionDataConfig>> _consolidatorUpdateData = new List<UpdateData<SubscriptionDataConfig>>();
            private readonly List<TimeSlice> _timeSlices = new List<TimeSlice>();

            public int Count => _timeSlices.Count;
            public TimeSpan FrontierStepSize = TimeSpan.FromSeconds(1);

            public IEnumerator<TimeSlice> GetEnumerator()
            {
                return _timeSlices.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerable<TimeSlice> GetTimeSlices()
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
                var customData = new List<UpdateData<Security>>();
                var changes = SecurityChanges.None;
                do
                {
                    var slice = new Slice(default(DateTime), _data, bars, quotes, ticks, options, futures, splits, dividends, delistings, symbolChanges);
                    var timeSlice = new TimeSlice(_frontierUtc, _data.Count, slice, dataFeedPackets, securitiesUpdateData, _consolidatorUpdateData, customData, changes, new Dictionary<Universe, BaseDataCollection>());
                    yield return timeSlice;
                    _frontierUtc += FrontierStepSize;
                }
                while (_frontierUtc <= _endTimeUtc);
            }

            public bool IsActive { get; }
            public IEnumerable<Subscription> Subscriptions { get; }

            public void Initialize(IAlgorithm algorithm,
                AlgorithmNodePacket job,
                IResultHandler resultHandler,
                IMapFileProvider mapFileProvider,
                IFactorFileProvider factorFileProvider,
                IDataProvider dataProvider,
                IDataFeedSubscriptionManager subscriptionManager)
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
                    securitiesUpdateData.Add(new UpdateData<Security>(security, typeof(Tick), new BaseData[]{tick}));
                    _consolidatorUpdateData.Add(new UpdateData<SubscriptionDataConfig>(security.Subscriptions.First(), typeof(Tick), new BaseData[]{tick}));
                }

                _timeSlices.AddRange(GetTimeSlices().Take(int.MaxValue/1000));
            }

            public bool AddSubscription(SubscriptionRequest request)
            {
                _data.Add(new Tick
                {
                    Symbol = request.Security.Symbol,
                    EndTime = _frontierUtc.ConvertFromUtc(request.Configuration.ExchangeTimeZone)
                });
                return true;
            }

            public bool RemoveSubscription(SubscriptionDataConfig configuration)
            {
                _data.RemoveAll(d => d.Symbol == configuration.Symbol);
                return true;
            }

            public void Run()
            {
            }

            public void Exit()
            {
            }
        }

        class NullAlphaHandler : IAlphaHandler
        {
            public bool IsActive { get; }
            public AlphaRuntimeStatistics RuntimeStatistics { get; }
            public void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm, IMessagingHandler messagingHandler, IApi api)
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

        class NullLeanManager : ILeanManager
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
            public ConcurrentDictionary<string, Chart> Charts { get; set; }
            public TimeSpan ResamplePeriod { get; }
            public TimeSpan NotificationPeriod { get; }
            public bool IsActive { get; }

            public void Initialize(AlgorithmNodePacket job,
                IMessagingHandler messagingHandler,
                IApi api,
                IDataFeed dataFeed,
                ISetupHandler setupHandler,
                ITransactionHandler transactionHandler)
            {
            }

            public void Run()
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

            public void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$")
            {
            }

            public void SampleEquity(DateTime time, decimal value)
            {
            }

            public void SamplePerformance(DateTime time, decimal value)
            {
            }

            public void SampleBenchmark(DateTime time, decimal value)
            {
            }

            public void SampleAssetPrices(Symbol symbol, DateTime time, decimal value)
            {
            }

            public void SampleRange(List<Chart> samples)
            {
            }

            public void SetAlgorithm(IAlgorithm algorithm)
            {
            }

            public void SetAlphaRuntimeStatistics(AlphaRuntimeStatistics statistics)
            {
            }

            public void StoreResult(Packet packet, bool async = false)
            {
            }

            public void SendFinalResult(AlgorithmNodePacket job,
                Dictionary<int, Order> orders,
                Dictionary<DateTime, decimal> profitLoss,
                Dictionary<string, Holding> holdings,
                CashBook cashbook,
                StatisticsResults statisticsResults,
                Dictionary<string, string> banner)
            {
            }

            public void SendStatusUpdate(AlgorithmStatus status, string message = "")
            {
            }

            public void SetChartSubscription(string symbol)
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

            public void PurgeQueue()
            {
            }

            public void ProcessSynchronousEvents(bool forceProcess = false)
            {
            }

            public string SaveLogs(string id, IEnumerable<string> logs)
            {
                return id;
            }

            public void SaveResults(string name, Result result)
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
            public void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api)
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
        }

        class NullTransactionHandler : ITransactionHandler
        {
            public int OrdersCount { get; }
            public Order GetOrderById(int orderId)
            {
                throw new NotImplementedException();
            }

            public Order GetOrderByBrokerageId(string brokerageId)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<OrderTicket> GetOpenOrderTickets(Func<OrderTicket, bool> filter = null)
            {
                return OrderTickets.Values.Where(x => x.Status.IsOpen() && (filter == null || filter(x)));
            }

            public OrderTicket GetOrderTicket(int orderId)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Order> GetOrders(Func<Order, bool> filter = null)
            {
                throw new NotImplementedException();
            }

            public OrderTicket Process(OrderRequest request)
            {
                throw new NotImplementedException();
            }

            public List<Order> GetOpenOrders(Func<Order, bool> filter = null)
            {
                return Orders.Values.Where(x => x.Status.IsOpen() && (filter == null || filter(x))).ToList();
            }

            public bool IsActive { get; }
            public ConcurrentDictionary<int, Order> Orders { get; }
            public ConcurrentDictionary<int, OrderTicket> OrderTickets { get; }
            public void Initialize(IAlgorithm algorithm, IBrokerage brokerage, IResultHandler resultHandler)
            {
            }

            public void Run()
            {
            }

            public void Exit()
            {
            }

            public void ProcessSynchronousEvents()
            {
            }

            public void AddOpenOrder(Order order, OrderTicket orderTicket)
            {
                throw new NotImplementedException();
            }
        }
    }
}
