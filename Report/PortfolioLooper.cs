using Deedle;
using QuantConnect.Orders;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Configuration;
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
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using QuantConnect.Brokerages;
using QuantConnect.Lean.Engine.Setup;

namespace QuantConnect.Report
{
    public class PortfolioLooper
    {
        private const Resolution _resolution = Resolution.Hour;

        private SecurityService _securityService;
        private DataManager _dataManager;
        private IEnumerable<Slice> _conversionSlices;

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
            var factorFileProvider = Composer.Instance.GetExportedValueByTypeName<IFactorFileProvider>("LocalDiskFactorFileProvider");
            var mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>("LocalDiskMapFileProvider");
            var dataCacheProvider = new ZipDataCacheProvider(new DefaultDataProvider(), false);
            var historyProvider = Composer.Instance.GetExportedValueByTypeName<IHistoryProvider>("SubscriptionDataReaderHistoryProvider");

            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, dataCacheProvider, mapFileProvider, factorFileProvider, (_) => { }));
            Algorithm = new PortfolioLooperAlgorithm((decimal)startingCash, orders);
            Algorithm.SetHistoryProvider(historyProvider);

            var job = new BacktestNodePacket(1, 2, "3", null, 9m, $"");
            var feed = new MockDataFeed();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            _dataManager = new DataManager(feed,
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

            _securityService = new SecurityService(Algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                Algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(Algorithm.Portfolio));

            var transactions = new BacktestingTransactionHandler();
            var results = new BacktestingResultHandler();

            Algorithm.Securities.SetSecurityService(_securityService);
            Algorithm.SubscriptionManager.SetDataManager(_dataManager);
            Algorithm.FromOrders(orders);
            Algorithm.Initialize();
            Algorithm.PostInitialize();

            results.Initialize(job, new Messaging.Messaging(), new Api.Api(), transactions);
            results.SetAlgorithm(Algorithm, Algorithm.Portfolio.TotalPortfolioValue);
            transactions.Initialize(Algorithm, new BacktestingBrokerage(Algorithm), results);
            feed.Initialize(Algorithm, job, results, null, null, null, _dataManager, null);

            var coreSecurities = Algorithm.Securities.Values.ToList();
            BaseSetupHandler.SetupCurrencyConversions(Algorithm, _dataManager.UniverseSelection);
            var conversionSecurities = Algorithm.Securities.Values.Where(s => !coreSecurities.Contains(s)).ToList();

            var conversionRateSecurityHistoryRequests = new List<Data.HistoryRequest>();

            foreach (var security in conversionSecurities)
            {
                var historyRequestFactory = new HistoryRequestFactory(Algorithm);
                var configs = Algorithm
                    .SubscriptionManager
                    .SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(security.Symbol, includeInternalConfigs: true);

                var startTime = historyRequestFactory.GetStartTimeAlgoTz(
                    security.Symbol,
                    1,
                    _resolution,
                    security.Exchange.Hours);
                var endTime = Algorithm.EndDate;

                // we need to order and select a specific configuration type
                // so the conversion rate is deterministic
                var configToUse = configs.OrderBy(x => x.TickType).First();

                conversionRateSecurityHistoryRequests.Add(historyRequestFactory.CreateHistoryRequest(
                    configToUse,
                    startTime,
                    endTime,
                    security.Exchange.Hours,
                    _resolution));
            }

            _conversionSlices = Algorithm.HistoryProvider.GetHistory(conversionRateSecurityHistoryRequests, Algorithm.TimeZone).ToList();
        }

        /// <summary>
        /// Process the orders
        /// </summary>
        /// <param name="orders">orders</param>
        /// <returns>PointInTimePortfolio</returns>
        public IEnumerable<PointInTimePortfolio> ProcessOrders(IEnumerable<Order> orders)
        {
            foreach (var order in orders.Where(x => x.Status != OrderStatus.Invalid))
            {
                var orderSecurity = Algorithm.Securities[order.Symbol];
                var tick = new Tick { Quantity = order.Quantity, AskPrice = order.Price, BidPrice = order.Price, Value = order.Price };

                orderSecurity.SetMarketPrice(tick);
                var baseCurrency = orderSecurity as IBaseCurrencySymbol;

                if (baseCurrency != null)
                {
                    var updateSlice = _conversionSlices.Where(x => x.Time <= order.Time).Last();

                    foreach (var quoteBar in updateSlice.QuoteBars.Values)
                    {
                        foreach (var cash in Algorithm.Portfolio.CashBook.Values.Where(x => x.ConversionRateSecurity != null && x.ConversionRateSecurity.Symbol == quoteBar.Symbol))
                        {
                            cash.Update(quoteBar);
                        }
                    }

                    Algorithm.Portfolio.InvalidateTotalPortfolioValue();
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
                    AddSecurity(symbol.SecurityType, symbol.Value, Resolution.Daily, symbol.ID.Market, false, Security.NullLeverage, false);
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
