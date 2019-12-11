using Deedle;
using QuantConnect.Orders;
using QuantConnect.Algorithm;
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
using QuantConnect.Packets;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QuantConnect.Report
{
    public class PortfolioLooper
    {
        /// <summary>
        /// QCAlgorithm derived class that sets up internal data feeds for
        /// use with crypto and forex data
        /// </summary>
        public PortfolioLooperAlgorithm Algorithm { get; protected set; }

        /// <summary>
        /// Creates an instance of the PortfolioLooper class
        /// </summary>
        /// <param name="equityCurve">Equity curve</param>
        /// <param name="orders">Order events</param>
        public PortfolioLooper(List<KeyValuePair<DateTime, double>> equityCurve, List<Order> orders)
        {
            var startingCash = equityCurve.First().Value;

            Algorithm = new PortfolioLooperAlgorithm((decimal)startingCash, orders);

            var job = new BacktestNodePacket(1, 2, "3", null, 9m, $"");
            var feed = new MockDataFeed();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var dataManager = new DataManager(feed,
                new UniverseSelection(
                    Algorithm,
                    new SecurityService(Algorithm.Portfolio.CashBook,
                        marketHoursDatabase,
                        symbolPropertiesDataBase,
                        Algorithm,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCacheProvider(Algorithm.Portfolio))),
                Algorithm,
                Algorithm.TimeKeeper,
                marketHoursDatabase,
                false,
                RegisteredSecurityDataTypesProvider.Null);

            var securityService = new SecurityService(Algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                Algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(Algorithm.Portfolio));

            var transactions = new BacktestingTransactionHandler();
            var results = new BacktestingResultHandler();

            Algorithm.Securities.SetSecurityService(securityService);
            Algorithm.SubscriptionManager.SetDataManager(dataManager);
            Algorithm.FromOrders(orders);
            Algorithm.Initialize();
            Algorithm.PostInitialize();

            results.Initialize(job, new Messaging.Messaging(), new Api.Api(), transactions);
            results.SetAlgorithm(Algorithm, Algorithm.Portfolio.TotalPortfolioValue);
            transactions.Initialize(Algorithm, new BacktestingBrokerage(Algorithm), results);
            feed.Initialize(Algorithm, job, results, null, null, null, dataManager, null);
        }

        /// <summary>
        /// Process the orders
        /// </summary>
        /// <param name="orders">orders</param>
        /// <returns>PointInTimePortfolio</returns>
        public IEnumerable<PointInTimePortfolio> ProcessOrders(IEnumerable<Order> orders)
        {
            foreach (var order in orders)
            {
                var orderSecurity = Algorithm.Securities[order.Symbol];
                var tick = new Tick { Quantity = order.Quantity, AskPrice = order.Price, BidPrice = order.Price, Value = order.Price };

                orderSecurity.SetMarketPrice(tick);
                var baseCurrency = orderSecurity as IBaseCurrencySymbol;

                if (baseCurrency != null)
                {
                    // poke each cash object to update from the recent security data
                    Algorithm.Portfolio.CashBook[baseCurrency.BaseCurrencySymbol].Update(tick);
                }

                // Make sure to manually set the FillPrice and FillQuantity since constructor doesn't do it by default
                var orderEvent = new OrderEvent(order, order.Time, Orders.Fees.OrderFee.Zero) { FillPrice = order.Price, FillQuantity = order.Quantity };

                Algorithm.Portfolio.ProcessFill(orderEvent);

                yield return new PointInTimePortfolio(order, Algorithm.Portfolio);
            }
        }

        public class MockDataFeed : IDataFeed
        {
            public bool IsActive { get; }

            public void Initialize(
                IAlgorithm algorithm,
                AlgorithmNodePacket job,
                IResultHandler resultHandler,
                IMapFileProvider mapFileProvider,
                IFactorFileProvider factorFileProvider,
                IDataProvider dataProvider,
                IDataFeedSubscriptionManager subscriptionManager,
                IDataFeedTimeProvider dataFeedTimeProvider
                )
            {
            }

            public Subscription CreateSubscription(SubscriptionRequest request)
            {
                return null;
            }

            public void RemoveSubscription(Subscription subscription)
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


        public class PortfolioLooperAlgorithm : QCAlgorithm
        {
            private decimal _startingCash;
            private List<Order> _orders;

            public PortfolioLooperAlgorithm(decimal startingCash, IEnumerable<Order> orders) : base()
            {
                _startingCash = startingCash;
                _orders = orders.ToList();
            }

            public void FromOrders(IEnumerable<Order> orders)
            {
                foreach (var symbol in orders.Select(x => x.Symbol).Distinct())
                {
                    AddSecurity(symbol.SecurityType, symbol.Value, Resolution.Daily);
                }
            }

            public override void Initialize()
            {
                SetCash(_startingCash);
                SetStartDate(_orders.First().Time);
                SetEndDate(_orders.Last().Time);

                SetBenchmark(b => 0);
            }
        }

        public class NullAlphaHandler : IAlphaHandler
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

        public class NullSynchronizer : ISynchronizer
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
