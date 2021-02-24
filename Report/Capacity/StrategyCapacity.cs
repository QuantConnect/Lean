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

        private Result _result;
        private SortedList<DateTime, double> _equity;
        private Dictionary<Symbol, SymbolData> _symbolData;
        private Dictionary<Symbol, MapFile> _mapFileCache;
        private SubscriptionManager _subscriptionManager;
        private SecurityManager _securityManager;
        private SecurityService _securityService;
        private MapFileResolver _mapFileResolver;
        private IFactorFileProvider _factorFileProvider;
        private SymbolPropertiesDatabase _spdb;
        private MarketHoursDatabase _mhdb;
        private CashBook _cashBook;
        private List<string> csv = new List<string>();

        /// <summary>
        /// Capacity of the strategy at different points in time
        /// </summary>
        public List<ChartPoint> Capacity { get; private set; }

        private void Initialize(Result result)
        {
            Log.Trace("StrategyCapacity.Initialize(): Initializing...");

            Capacity = new List<ChartPoint>();

            _result = result;
            _equity = ResultsUtil.EquityPoints(result);

            _symbolData = new Dictionary<Symbol, SymbolData>();
            _securityManager = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork, TimeZones.Utc));
            _cashBook = new CashBook();
            _subscriptionManager = new SubscriptionManager();
            _subscriptionManager.SetDataManager(new StubDataManager());
            _mhdb = MarketHoursDatabase.FromDataFolder();
            _spdb = SymbolPropertiesDatabase.FromDataFolder();

            var mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
            _mapFileCache = new Dictionary<Symbol, MapFile>();
            _mapFileResolver = mapFileProvider.Get("usa");
            _factorFileProvider = Composer.Instance.GetExportedValueByTypeName<IFactorFileProvider>(Config.Get("factor-file-provider", "LocalDiskFactorFileProvider"));

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
            Initialize(result);

            var orders = result?.Orders?.Values
                .Where(o => (o.Status == OrderStatus.Filled || o.Status == OrderStatus.PartiallyFilled))// && DateTime.UtcNow - o.Time <= TimeSpan.FromDays(365))
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

            SetupDataSubscriptions(orders, start);

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

            File.WriteAllLines("capacity.csv", csv);
            return capacity;
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
        /// Creates the data subscriptions required for loading data
        /// </summary>
        /// <param name="orders">Orders to load data for</param>
        /// <remarks>We use L1 crypto data because there is much greater depth of crypto books vs. the trading volumes</remarks>
        private void SetupDataSubscriptions(List<Order> orders, DateTime start)
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

                SplitEventProvider splitEventProvider = null;

                if (symbol.SecurityType == SecurityType.Equity)
                {
                    var factorFile = _factorFileProvider.Get(symbol);
                    MapFile mapFile;
                    if (!_mapFileCache.TryGetValue(symbol, out mapFile))
                    {
                        mapFile = _mapFileResolver.ResolveMapFile(symbol, type);
                        _mapFileCache[symbol] = mapFile;
                    }

                    splitEventProvider = new SplitEventProvider();
                    splitEventProvider.Initialize(config, factorFile, mapFile, start);
                }

                _symbolData[symbol] = new SymbolData(
                    symbol,
                    exchangeTimeZone,
                    orderEvents,
                    _cashBook,
                    _spdb,
                    splitEventProvider,
                    _fastTradingVolumeScalingFactor,
                    _percentageOfMinuteDollarVolume);
            }
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
            var previousTime = start;
            foreach (var date in Time.EachDay(start, end))
            {
                var readers = ReadData(orders, orderSymbols, configs, date);
                var dataEnumerators = readers.ToArray();
                var feed = new SynchronizingEnumerator(dataEnumerators);

                while (feed.MoveNext() && feed.Current != null)
                {
                    var data = feed.Current;
                    if (data.EndTime > previousTime)
                    {
                        TakeCapacitySnapshot(previousTime);
                        previousTime = data.EndTime;
                    }

                    UpdateCurrencyConversionData(data);
                    DataToAccountCurrency(data);
                    OnData(data);
                }

                var nextTradingDay = date.AddDays(1);
                RemoveDelistedSymbols(nextTradingDay);
            }

            return Capacity.LastOrDefault()?.y ?? -1;
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
        /// Triggered when we have new data. This calls SymbolData
        /// so that its internal state is updated for the eventual snapshot.
        /// </summary>
        /// <param name="data"></param>
        public void OnData(BaseData data)
        {
            SymbolData symbolData;
            if (!_symbolData.TryGetValue(data.Symbol, out symbolData))
            {
                return;
            }

            symbolData.OnData(data);
        }

        public void TakeCapacitySnapshot(DateTime time)
        {
            //Log.Trace($"StrategyCapacity.TakeCapacitySnapshot(): Taking capacity snapshot for date: {time:yyyy-MM-dd HH:mm:ss}");

            var equityPoints = _equity.LastOrDefault(kvp => kvp.Key <= time);
            var totalEquity = (decimal)equityPoints.Value;
            if (equityPoints.Key == default(DateTime))
            {
                totalEquity = (decimal)_equity.Values.First();
            }

            var smallestCapacityAsset = _symbolData.Values
                .Where(s => s.TotalHoldingsInDollars != 0)
                .OrderBy(s => s.MarketCapacityDollarVolume)
                .FirstOrDefault();

            if (smallestCapacityAsset == null)
            {
                Log.Trace($"StrategyCapacity.TakeCapacitySnapshot(): Smallest capacity is null, we have no holdings at this time");
                return;
            }

            var capacity = smallestCapacityAsset.MarketCapacityDollarVolume / (Math.Abs(smallestCapacityAsset.TotalHoldingsInDollars) / totalEquity);

            csv.AddRange(_symbolData.Where(s => s.Value.TotalHoldingsInDollars != 0).Select(kvp => string.Join(",", time.ToStringInvariant("yyyy-MM-dd HH:mm:ss"), kvp.Key.ToString(), kvp.Value.MarketCapacityDollarVolume.RoundToSignificantDigits(6).ToStringInvariant(), kvp.Value.AbsoluteTradingDollarVolume.RoundToSignificantDigits(6).ToStringInvariant(), totalEquity.RoundToSignificantDigits(6).ToStringInvariant(), capacity.RoundToSignificantDigits(6).ToStringInvariant(), string.Join("|", _symbolData.Where(pvk => pvk.Value.TotalHoldingsInDollars != 0).Select(pvk => pvk.Key.ToString() + " " + ((pvk.Value.TotalHoldingsInDollars / totalEquity) * 100).RoundToSignificantDigits(4).ToStringInvariant() + "%")))));
            csv.Add("");

            Capacity.Add(new ChartPoint(time, capacity));

            ResetData();
        }

        private void RemoveDelistedSymbols(DateTime nextTradingDay)
        {
            var contractsToRemove = new List<Symbol>();

            foreach (var symbol in _symbolData.Keys)
            {
                if (symbol.SecurityType != SecurityType.Option &&
                    symbol.SecurityType != SecurityType.Future && symbol.SecurityType != SecurityType.FutureOption)
                {
                    continue;
                }

                if (symbol.ID.Date < nextTradingDay)
                {
                    contractsToRemove.Add(symbol);
                }
            }

            foreach (var contractToRemove in contractsToRemove)
            {
                Log.Trace($"StrategyCapacity.RemoveExpiredContracts(): Removing contract {contractToRemove}");
                _symbolData.Remove(contractToRemove);
            }
        }

        /// <summary>
        /// Utility method to reset all SymbolData instances
        /// </summary>
        private void ResetData()
        {
            foreach (var symbolData in _symbolData.Values)
            {
                symbolData.Reset();
            }
        }

        /// <summary>
        /// Class for calculating the capacity and volume of individual assets
        /// </summary>
        private class SymbolData
        {
            private readonly IEnumerator<OrderEvent> _orderEvents;
            private readonly string _quoteCurrency;
            private readonly SplitEventProvider _splitEventProvider;
            private readonly Symbol _symbol;

            private bool _orderEventsFinished;
            private decimal _percentageOfMinuteDollarVolume;
            private TradeBar _previousBar;
            private OrderEvent _previousTrade;
            private readonly CashBook _cashBook;
            private readonly SymbolProperties _symbolProperties;
            private decimal _splitFactor = 1m;

            private DateTime _timeout;
            private double _fastTradingVolumeDiscountFactor;
            private double _fastTradingVolumeScalingFactor;
            private decimal _averageVolume;
            private decimal _totalQuantityHeld;

            /// <summary>
            /// If an order event is encountered between the previous snapshot
            /// and the current snapshot, this will be set to true.
            /// </summary>
            private bool _tradedBetweenSnapshots;

            /// <summary>
            /// Time zone of the Symbol
            /// </summary>
            public DateTimeZone TimeZone { get; }

            /// <summary>
            /// Dollar volume traded by the user between snapshots
            /// </summary>
            public decimal AbsoluteTradingDollarVolume { get; private set; }

            /// <summary>
            /// Market capacity by dollar volume
            /// </summary>
            public decimal MarketCapacityDollarVolume { get; private set; }

            /// <summary>
            /// Total amount of stock we're holding in dollars for this Symbol
            /// </summary>
            public decimal TotalHoldingsInDollars => _totalQuantityHeld * (_previousBar?.Close ?? 0) * _symbolProperties.ContractMultiplier;

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
                SplitEventProvider splitEventProvider,
                decimal fastTradingVolumeScalingFactor,
                decimal percentageOfMinuteDollarVolume)
            {
                TimeZone = timeZone;

                _symbol = symbol;
                _orderEvents = orderEvents.GetEnumerator();
                _fastTradingVolumeScalingFactor = (double)fastTradingVolumeScalingFactor;
                _percentageOfMinuteDollarVolume = percentageOfMinuteDollarVolume;
                _symbolProperties = spdb.GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, "USD");
                _quoteCurrency = _symbolProperties.QuoteCurrency;
                _cashBook = cashBook;
                _splitEventProvider = splitEventProvider;
            }

            /// <summary>
            /// Processes an order event, calculating the dollar volume and setting the order timeout
            /// </summary>
            public void OnOrderEvent(OrderEvent orderEvent)
            {
                // We don't apply splits to order events since they're already adjusted for the split price
                orderEvent.FillPrice = _cashBook.ConvertToAccountCurrency(orderEvent.FillPrice, _quoteCurrency);

                _tradedBetweenSnapshots = true;
                _totalQuantityHeld += orderEvent.FillQuantity;

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
                // Bars don't need to be adjusted for splits since they
                // already incorporate the new price in the data itself.
                // We do however need to convert any internal quantities
                // to the new split factor so that our calculations are
                // accurate once the split occurs.
                AdjustForSplits(data);

                var resetMarketCapacity = !_tradedBetweenSnapshots;
                var endTimeUtc = data.EndTime.ConvertToUtc(TimeZone);

                if (_orderEvents.Current == null)
                {
                    _orderEvents.MoveNext();
                }
                while (!_orderEventsFinished && _orderEvents.Current != null && _orderEvents.Current.UtcTime <= endTimeUtc)
                {
                    OnOrderEvent(_orderEvents.Current);
                    _orderEventsFinished = !_orderEvents.MoveNext();
                }

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

                var absoluteMarketDollarVolume = bar.Close * bar.Volume * _symbolProperties.ContractMultiplier;
                if (_previousBar == null)
                {
                    _previousBar = bar;
                    _averageVolume = bar.Close * bar.Volume;

                    return;
                }

                // If we have an illiquid stock, we will get bars that might not be continuous.
                // Skip getting the notional average volume since some futures contracts
                // might be very illiquid but have an incredible amount of notional per contract.
                _averageVolume = (bar.Close * (bar.Volume + _previousBar.Volume)) / (decimal)(bar.EndTime - _previousBar.Time).TotalMinutes;

                if (bar.EndTime <= _timeout)
                {
                    if (resetMarketCapacity)
                    {
                        // We only reset whenever we have a new trade come in so
                        // that we maintain consistency of capacity.
                        MarketCapacityDollarVolume = 0;
                    }

                    MarketCapacityDollarVolume += absoluteMarketDollarVolume * (decimal)_fastTradingVolumeDiscountFactor * _percentageOfMinuteDollarVolume;
                }

                _previousBar = bar;
            }

            /// <summary>
            /// Adjusts internal quantities used to calculate capacity to the new value
            /// determined by the split.
            /// </summary>
            /// <param name="split">Split to apply. If null, nothing will happen</param>
            private void AdjustForSplits(BaseData data)
            {
                Split split = null;
                if (data.Symbol.SecurityType == SecurityType.Equity && _splitEventProvider != null &&
                    _previousBar != null && data.EndTime.Date != _previousBar.EndTime.Date)
                {
                    split = _splitEventProvider
                        .GetEvents(new NewTradableDateEventArgs(data.EndTime.Date, data, _symbol, data.Value))
                        .Cast<Split>()
                        .FirstOrDefault(s => s.Type == SplitType.SplitOccurred);
                }

                if (split != null)
                {
                    Log.Trace($"Split encountered at {split.Time} for {split.Symbol}. Adjusting Split factor from: {_splitFactor} to: {split.SplitFactor}");
                    _splitFactor = split.SplitFactor;

                    // We get a cash rebate in the case of a reverse split event, so we floor to the nearest multiple of quantity held
                    _totalQuantityHeld = Math.Floor(_totalQuantityHeld / _splitFactor);
                }
            }

            /// <summary>
            /// Resets any variables that are used only in between snapshots
            /// </summary>
            public void Reset()
            {
                _tradedBetweenSnapshots = false;
            }
        }
    }
}
