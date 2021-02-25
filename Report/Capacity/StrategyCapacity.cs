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
using System.IO;
using System.Linq;
using NodaTime;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.ToolBox;
using QuantConnect.Util;

namespace QuantConnect.Report
{
    /// <summary>
    /// Class to facilitate the calculation of the strategy capacity
    /// </summary>
    public class StrategyCapacity
    {
        private const Resolution _resolution = Resolution.Minute;
        private const decimal _forexMinuteVolume = 25000000m;
        private const decimal _percentageOfMinuteDollarVolume = 0.20m;
        /// <summary>
        /// If trades were more than 180 minutes apart, there was no impact.
        /// (390 / 2) roughly equals 180 minutes
        /// </summary>
        private const decimal _fastTradingVolumeScalingFactor = 2m;

        private LiveResult _live;
        private BacktestResult _backtest;
        private int _previousMonth;
        private Dictionary<Symbol, SymbolData> _symbolData;
        private Dictionary<Symbol, MapFile> _mapFileCache;
        private SubscriptionManager _subscriptionManager;
        private SecurityManager _securityManager;
        private SecurityService _securityService;
        private MapFileResolver _mapFileResolver;
        private SymbolPropertiesDatabase _spdb;
        private MarketHoursDatabase _mhdb;
        private CashBook _cashBook;

        /// <summary>
        /// Capacity of the strategy at different points in time
        /// </summary>
        public List<ChartPoint> Capacity { get; private set; }

        private void Initialize()
        {
            Log.Trace("StrategyCapacity.Initialize(): Initializing...");

            Capacity = new List<ChartPoint>();

            _symbolData = new Dictionary<Symbol, SymbolData>();
            _securityManager = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork, TimeZones.Utc));
            _cashBook = new CashBook();
            _subscriptionManager = new SubscriptionManager();
            _subscriptionManager.SetDataManager(new StubDataManager());
            _mhdb = MarketHoursDatabase.FromDataFolder();
            _spdb = SymbolPropertiesDatabase.FromDataFolder();

            _mapFileCache = new Dictionary<Symbol, MapFile>();
            _mapFileResolver = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"))
                .Get("usa");

            _securityService = new SecurityService(
                _cashBook,
                _mhdb,
                _spdb,
                new QCAlgorithm(),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCacheProvider(new ReportSecurityProvider()));
        }

        /// <summary>
        /// Estimates the strategy's capacity. Can be a live or backtest result.
        /// </summary>
        /// <param name="result">Backtest or live result</param>
        /// <returns>Estimated capacity in USD</returns>
        public decimal? Estimate(Result result)
        {
            Initialize();

            var orders = result?.Orders?.Values
                .Where(o => (o.Status == OrderStatus.Filled || o.Status == OrderStatus.PartiallyFilled))
                .OrderBy(o => o.LastFillTime ?? o.Time)
                .ToList();

            if (orders == null || orders.Count == 0)
            {
                Log.Trace("StrategyCapacity.Estimate(): No orders found. Skipping capacity estimation.");
                return null;
            }

            var start = orders[0].LastFillTime ?? orders[0].Time;
            // Add a buffer of 1 day so that orders placed in the last trading day are snapshotted if the month changes.
            var end = (orders[orders.Count - 1].LastFillTime ?? orders[orders.Count - 1].Time).AddDays(1);

            Log.Trace($"StrategyCapacity.Estimate(): Creating estimate for date range: {start:yyyy-MM-dd} until: {end:yyyy-MM-dd}");

            SetupDataSubscriptions(orders);

            var configs = _cashBook.EnsureCurrencyDataFeeds(
                    _securityManager,
                    _subscriptionManager,
                    new DefaultBrokerageModel().DefaultMarkets,
                    new SecurityChanges(_securityManager.Values, Array.Empty<Security>()),
                    _securityService)
                .Concat(_subscriptionManager.Subscriptions)
                .ToList();

            Log.Trace($"StrategyCapacity.Estimate(): Created {_subscriptionManager.Count} order data configs, and created {configs.Count - _subscriptionManager.Count} currency conversion configs");

            var capacity = AlgorithmCapacity(orders, configs, start, end).RoundToSignificantDigits(2);

            return capacity;
        }

        /// <summary>
        /// Triggered on a new slice update
        /// </summary>
        /// <param name="data"></param>
        public void OnData(BaseData data)
        {
            if (data.Time.Month != _previousMonth && _previousMonth != 0)
            {
                TakeCapacitySnapshot(data.Time);
            }

            SymbolData symbolData;
            if (!_symbolData.TryGetValue(data.Symbol, out symbolData))
            {
                return;
            }

            symbolData.OnData(data);
            _previousMonth = data.Time.Month;
        }

        public void TakeCapacitySnapshot(DateTime time)
        {
            Log.Trace($"StrategyCapacity.TakeCapacitySnapshot(): Taking capacity snapshot for date: {time:yyyy-MM-dd}");

            if (_symbolData.Values.All(x => !x.TradedBetweenSnapshots))
            {
                ResetData();
                return;
            }

            var totalAbsoluteSymbolDollarVolume = _symbolData.Values
                .Sum(x => x.AbsoluteTradingDollarVolume);

            var symbolByPercentageOfAbsoluteDollarVolume = _symbolData
                .Where(kvp => kvp.Value.TradedBetweenSnapshots)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AbsoluteTradingDollarVolume / totalAbsoluteSymbolDollarVolume);

            var minimumMarketVolume = _symbolData
                .Where(kvp => kvp.Value.TradedBetweenSnapshots)
                .OrderBy(kvp => kvp.Value.AverageCapacity)
                .FirstOrDefault();

            var capacity = minimumMarketVolume.Value.AverageCapacity / symbolByPercentageOfAbsoluteDollarVolume[minimumMarketVolume.Key];

            Log.Trace($"StrategyCapacity.TakeCapacitySnapshot(): Capacity for date {time:yyyy-MM-dd} is {capacity}");

            Capacity.Add(new ChartPoint(time, capacity));
            ResetData();
        }

        protected void ResetData()
        {
            Log.Trace("StrategyCapacity.ResetData(): Resetting SymbolData");

            foreach (var symbolData in _symbolData.Values)
            {
                symbolData.Reset();
            }
        }

        /// <summary>
        /// Creates the data subscriptions required for loading data
        /// </summary>
        /// <param name="orders">Orders to load data for</param>
        /// <remarks>We use L1 crypto data because there is much greater depth of crypto books vs. the trading volumes</remarks>
        private void SetupDataSubscriptions(List<Order> orders)
        {
            var symbols = LinqExtensions.ToHashSet(orders.Select(x => x.Symbol));

            foreach (var symbol in symbols)
            {
                var dataTimeZone = _mhdb.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);
                var exchangeTimeZone = _mhdb.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;

                var usesQuotes = symbol.SecurityType == SecurityType.Crypto || symbol.SecurityType == SecurityType.Forex;
                var type = usesQuotes ? typeof(QuoteBar) : typeof(TradeBar);
                var tickType = usesQuotes ? TickType.Quote : TickType.Trade;
                var config = _subscriptionManager.Add(type, tickType, symbol, _resolution, dataTimeZone, exchangeTimeZone, false);

                _securityManager.Add(_securityService.CreateSecurity(config.Symbol, config));

                var orderEvents = ToOrderEvents(orders.Where(o => o.Symbol == symbol), exchangeTimeZone)
                    .OrderBy(o => o.UtcTime);

                _symbolData[symbol] = new SymbolData(
                    symbol,
                    exchangeTimeZone,
                    orderEvents,
                    _cashBook,
                    _spdb,
                    _fastTradingVolumeScalingFactor,
                    _percentageOfMinuteDollarVolume);
            }
        }

        /// <summary>
        /// Uses the orders to load data
        /// </summary>
        /// <param name="configs">Configurations to use for reading data</param>
        /// <param name="start">Starting date to read data for</param>
        /// <param name="end">Ending date to read data for</param>
        /// <returns>List of enumerators of data</returns>
        private List<IEnumerator<BaseData>> ReadData(
            IEnumerable<Order> orders,
            HashSet<Symbol> orderSymbols,
            IEnumerable<SubscriptionDataConfig> configs,
            DateTime date)
        {
            var readers = new List<IEnumerator<BaseData>>();

            var symbolsOnDate = new HashSet<Symbol>(orders
                .Where(o => (o.LastFillTime ?? o.Time).Date == date)
                .Select(o => o.Symbol));

            // If the config is not in the order Symbols at all, then it's a currency conversion
            // feed, and should be loaded on every day.
            var configsOnDate = configs
                .Where(c => symbolsOnDate.Contains(c.Symbol) || !orderSymbols.Contains(c.Symbol))
                .ToList();

            foreach (var config in configsOnDate)
            {
                var mappedSymbol = config.Symbol;
                if (config.Symbol.SecurityType == SecurityType.Equity || config.Symbol.SecurityType == SecurityType.Option)
                {
                    MapFile mapFile;
                    if (!_mapFileCache.TryGetValue(config.Symbol, out mapFile))
                    {
                        mapFile = _mapFileResolver.ResolveMapFile(config.Symbol, null);
                        _mapFileCache[config.Symbol] = mapFile;
                    }

                    mappedSymbol = config.Symbol.UpdateMappedSymbol(mapFile.GetMappedSymbol(date, config.Symbol.Value));
                }

                if (File.Exists(LeanData.GenerateZipFilePath(Globals.DataFolder, mappedSymbol, date, _resolution, config.TickType)))
                {
                    readers.Add(new LeanDataReader(config, mappedSymbol, _resolution, date, Globals.DataFolder).Parse().GetEnumerator());
                }
            }

            return readers;
        }

        /// <summary>
        /// Updates the currency converter with the price data required for it to convert to the account currency (USD)
        /// </summary>
        /// <remarks>Used primarily for crypto and FX</remarks>
        private void UpdateCurrencyConversionData(BaseData data)
        {
            var symbol = data.Symbol;
            var cashMoney = _cashBook.Values.FirstOrDefault(x => x.ConversionRateSecurity?.Symbol == symbol);

            cashMoney?.Update(data);
        }

        /// <summary>
        /// Converts any crypto/FX data to USD so that we can calculate the USD capacity
        /// </summary>
        /// <remarks>
        /// Futures uses the notional value to approximate trading volume for the day.
        /// FX estimates the capacity at 25MM USD per minute.
        /// <param name="dataBin"></param>
        private void DataToAccountCurrency(BaseData data)
        {
            var symbolProperties = _spdb.GetSymbolProperties(data.Symbol.ID.Market, data.Symbol, data.Symbol.SecurityType, "USD");
            var bar = data as TradeBar;

            if (bar != null)
            {
                // Actual units are:
                // USD/BTC
                // BTC/ETH
                //
                // 0.02541 BTC == 1 ETH
                // 0.02541 BTC == 744 USD
                // 0.02541 BTC/ETH * 29280 USD/BTC = 744 USD/ETH
                // So converting from BTC to USD should be sufficient.
                bar.Open = _cashBook.ConvertToAccountCurrency(bar.Open, symbolProperties.QuoteCurrency);
                bar.High = _cashBook.ConvertToAccountCurrency(bar.High, symbolProperties.QuoteCurrency);
                bar.Low = _cashBook.ConvertToAccountCurrency(bar.Low, symbolProperties.QuoteCurrency);
                bar.Close = _cashBook.ConvertToAccountCurrency(bar.Close, symbolProperties.QuoteCurrency);
                // We don't convert bar volume here, since it will be converted for us as dollar volume
                // in the SymbolData class inside the StrategyCapacity class.
                bar.Volume *= symbolProperties.ContractMultiplier;

                return;
            }
            var quoteBar = data as QuoteBar;
            if (quoteBar != null)
            {
                if (quoteBar.LastBidSize == 0)
                {
                    quoteBar.LastBidSize = _forexMinuteVolume / _cashBook.ConvertToAccountCurrency(quoteBar.Close, symbolProperties.QuoteCurrency);
                }
                if (quoteBar.LastAskSize == 0)
                {
                    quoteBar.LastAskSize = _forexMinuteVolume / _cashBook.ConvertToAccountCurrency(quoteBar.Close, symbolProperties.QuoteCurrency);
                }

                if (quoteBar.Bid != null)
                {
                    quoteBar.Bid.Open = _cashBook.ConvertToAccountCurrency(quoteBar.Bid.Open, symbolProperties.QuoteCurrency);
                    quoteBar.Bid.High = _cashBook.ConvertToAccountCurrency(quoteBar.Bid.High, symbolProperties.QuoteCurrency);
                    quoteBar.Bid.Low = _cashBook.ConvertToAccountCurrency(quoteBar.Bid.Low, symbolProperties.QuoteCurrency);
                    quoteBar.Bid.Close = _cashBook.ConvertToAccountCurrency(quoteBar.Bid.Close, symbolProperties.QuoteCurrency);
                }
                if (quoteBar.Ask != null)
                {
                    quoteBar.Ask.Open = _cashBook.ConvertToAccountCurrency(quoteBar.Ask.Open, symbolProperties.QuoteCurrency);
                    quoteBar.Ask.High = _cashBook.ConvertToAccountCurrency(quoteBar.Ask.High, symbolProperties.QuoteCurrency);
                    quoteBar.Ask.Low = _cashBook.ConvertToAccountCurrency(quoteBar.Ask.Low, symbolProperties.QuoteCurrency);
                    quoteBar.Ask.Close = _cashBook.ConvertToAccountCurrency(quoteBar.Ask.Close, symbolProperties.QuoteCurrency);
                }
            }
        }

        /// <summary>
        /// Takes orders and converts them to OrderEvents. Orders will have their fill price converted to
        /// the account currency (USD) and have their fill time set to UTC. MOO orders are shifted until market open.
        /// </summary>
        /// <param name="cursor">Cursor is used to keep track of what order we're on</param>
        /// <returns>OrderEvents</returns>
        private List<OrderEvent> ToOrderEvents(IEnumerable<Order> orders, DateTimeZone timeZone)
        {
            var orderEvents = new List<OrderEvent>();

            foreach (var order in orders)
            {
                var exchangeHours = _mhdb.GetEntry(order.Symbol.ID.Market, order.Symbol, order.Symbol.SecurityType)
                    .ExchangeHours;

                var orderEvent = new OrderEvent(order, order.LastFillTime ?? order.Time, OrderFee.Zero);

                // Price is in USD/ETH
                orderEvent.FillPrice = order.Price;
                // Qty is in ETH, (ETH/1) * (USD/ETH) == USD
                // However, the OnData handler inside SymbolData in the StrategyCapacity
                // class will multiply this for us, so let's keep this in the asset quantity for now.
                orderEvent.FillQuantity = order.Quantity;
                orderEvent.UtcTime = order.Type == OrderType.MarketOnOpen
                    ? exchangeHours.GetNextMarketOpen(orderEvent.UtcTime.ConvertFromUtc(timeZone), false).AddMinutes(1).ConvertToUtc(timeZone)
                    : orderEvent.UtcTime;

                orderEvents.Add(orderEvent);
            }

            return orderEvents;
        }

        /// <summary>
        /// Step through time and order events and calculate capacity based on the volumes in the minutes surrounding the order.
        /// </summary>
        /// <returns>Capacity in USD</returns>
        private decimal AlgorithmCapacity(
            IEnumerable<Order> orders,
            IEnumerable<SubscriptionDataConfig> configs,
            DateTime start,
            DateTime end)
        {
            var orderSymbols = new HashSet<Symbol>(orders.Select(x => x.Symbol));
            foreach (var date in Time.EachDay(start, end))
            {
                var readers = ReadData(orders, orderSymbols, configs, date);
                var dataEnumerators = readers.ToArray();
                var feed = new SynchronizingEnumerator(dataEnumerators);

                while (feed.MoveNext() && feed.Current != null)
                {
                    var data = feed.Current;

                    UpdateCurrencyConversionData(data);
                    DataToAccountCurrency(data);
                    OnData(data);
                }
            }

            return Capacity.LastOrDefault()?.y ?? -1;
        }

        /// <summary>
        /// Class for calculating the capacity and volume of individual assets
        /// </summary>
        private class SymbolData
        {
            private readonly IEnumerator<OrderEvent> _orderEvents;
            private bool _orderEventsFinished;
            private string _quoteCurrency;
            private decimal _percentageOfMinuteDollarVolume = 0.20m;
            private Symbol _symbol;
            private TradeBar _previousBar;
            private QuoteBar _previousQuoteBar;
            private OrderEvent _previousTrade;
            private CashBook _cashBook;

            private DateTime _timeout;
            private double _fastTradingVolumeDiscountFactor;
            private double _fastTradingVolumeScalingFactor;
            private decimal _marketCapacityDollarVolume;
            private decimal _averageVolume;

            /// <summary>
            /// 20% of Total market capacity dollar volume of minutes surrounding after an order. It is penalized by frequency of trading.
            /// </summary>
            public decimal AverageCapacity => (_marketCapacityDollarVolume / TradeCount) * _percentageOfMinuteDollarVolume;

            /// <summary>
            /// If an order event is encountered between the previous snapshot
            /// and the current snapshot, this will be set to true.
            /// </summary>
            public bool TradedBetweenSnapshots { get; private set; }

            /// <summary>
            /// Time zone of the Symbol
            /// </summary>
            public DateTimeZone TimeZone { get; }

            /// <summary>
            /// Number of trades placed by the algorithm between snapshots
            /// </summary>
            public int TradeCount { get; private set; }

            /// <summary>
            /// Dollar volume traded by the user between snapshots
            /// </summary>
            public decimal AbsoluteTradingDollarVolume { get; private set; }

            /// <summary>
            /// Creates an instance of SymbolData, used internally to calculate capacity
            /// </summary>
            /// <param name="symbol">Symbol to calculate capacity for</param>
            /// <param name="timeZone">Time zone of the data</param>
            /// <param name="fastTradingVolumeScalingFactor">Penalty for fast trading</param>
            /// <param name="percentageOfMinuteDollarVolume">Percentage of minute dollar volume to assume as take-able without moving the market</param>
            public SymbolData(
                Symbol symbol,
                DateTimeZone timeZone,
                IEnumerable<OrderEvent> orderEvents,
                CashBook cashBook,
                SymbolPropertiesDatabase spdb,
                decimal fastTradingVolumeScalingFactor,
                decimal percentageOfMinuteDollarVolume)
            {
                TimeZone = timeZone;

                _symbol = symbol;
                _orderEvents = orderEvents.GetEnumerator();
                _fastTradingVolumeScalingFactor = (double)fastTradingVolumeScalingFactor;
                _percentageOfMinuteDollarVolume = percentageOfMinuteDollarVolume;
                _quoteCurrency = spdb.GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, "USD").QuoteCurrency;
                _cashBook = cashBook;
            }

            /// <summary>
            /// Processes an order event, calculating the dollar volume and setting the order timeout
            /// </summary>
            public void OnOrderEvent(OrderEvent orderEvent)
            {
                orderEvent.FillPrice = _cashBook.ConvertToAccountCurrency(orderEvent.FillPrice, _quoteCurrency);

                TradedBetweenSnapshots = true;
                AbsoluteTradingDollarVolume += orderEvent.FillPrice * orderEvent.AbsoluteFillQuantity;
                TradeCount++;

                // Use 6000000 as the maximum bound for trading volume in a single minute.
                // Any bars that exceed 6 million total volume will be capped to a timeout of five minutes.
                var k = _averageVolume != 0
                    ? 6000000 / _averageVolume
                    : 10;

                var timeoutMinutes = k > 120 ? 120 : (int)Math.Max(5, (double)k);

                // To reduce the capacity of high frequency strategies, we scale down the
                // volume captured on each bar proportional to the trades per day.
                _fastTradingVolumeDiscountFactor = _fastTradingVolumeScalingFactor * (((orderEvent.UtcTime - (_previousTrade?.UtcTime ?? orderEvent.UtcTime.AddDays(-1))).TotalMinutes) / 390);
                _fastTradingVolumeDiscountFactor = _fastTradingVolumeDiscountFactor > 1 ? 1 : Math.Max(0.20, _fastTradingVolumeDiscountFactor);

                // When trades occur within 10 minutes the total volume we will capture is implicitly limited
                // because of the reduced time that we're capturing the volume
                _timeout = orderEvent.UtcTime.ConvertFromUtc(TimeZone).AddMinutes(timeoutMinutes);
                _previousTrade = orderEvent;
            }

            /// <summary>
            /// Process data and calculate volume at this time step, as well as the dollar volume of the market.
            /// </summary>
            public void OnData(BaseData data)
            {
                var bar = data as TradeBar;
                var quote = data as QuoteBar;

                if (quote != null)
                {
                    // Fake a tradebar for quote data using market depth as a proxy for volume
                    bar = new TradeBar(
                        quote.Time,
                        quote.Symbol,
                        quote.Open,
                        quote.High,
                        quote.Low,
                        quote.Close,
                        (quote.LastBidSize + quote.LastAskSize) / 2);
                }

                var absoluteMarketDollarVolume = bar.Close * bar.Volume;
                if (_previousBar == null)
                {
                    _previousBar = bar;
                    _averageVolume = absoluteMarketDollarVolume;

                    return;
                }

                // If we have an illiquid stock, we will get bars that might not be continuous
                _averageVolume = (bar.Close * (bar.Volume + _previousBar.Volume)) / (decimal)(bar.EndTime - _previousBar.Time).TotalMinutes;

                if (bar.EndTime <= _timeout)
                {
                    _marketCapacityDollarVolume += absoluteMarketDollarVolume * (decimal)_fastTradingVolumeDiscountFactor;
                }

                var endTimeUtc = bar.EndTime.ConvertToUtc(TimeZone);

                if (_orderEvents.Current == null)
                {
                    _orderEvents.MoveNext();
                }

                while (!_orderEventsFinished && _orderEvents.Current != null && _orderEvents.Current.UtcTime <= endTimeUtc)
                {
                    OnOrderEvent(_orderEvents.Current);
                    _orderEventsFinished = !_orderEvents.MoveNext();
                }

                _previousBar = bar;
            }

            public void Reset()
            {
                TradedBetweenSnapshots = false;

                _marketCapacityDollarVolume = 0;
                AbsoluteTradingDollarVolume = 0;
                TradeCount = 0;
            }
        }
    }
}
