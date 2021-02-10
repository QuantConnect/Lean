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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
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

        private int _previousMonth;
        private Dictionary<Symbol, SymbolData> _symbolData;

        private LiveResult _live;
        private BacktestResult _backtest;
        private Dictionary<Symbol, DateTimeZone> _timeZones;
        private SecurityManager _securityManager;
        private CashBook _cashBook;
        private SubscriptionManager _subscriptionManager;
        private MarketHoursDatabase _mhdb;
        private SymbolPropertiesDatabase _spdb;
        private SecurityService _securityService;

        /// <summary>
        /// Capacity of the strategy at different points in time
        /// </summary>
        public List<ChartPoint> Capacity { get; private set; }

        private void Initialize()
        {
            Capacity = new List<ChartPoint>();

            _symbolData = new Dictionary<Symbol, SymbolData>();
            _timeZones = new Dictionary<Symbol, DateTimeZone>();
            _securityManager = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork, TimeZones.Utc));
            _cashBook = new CashBook();
            _subscriptionManager = new SubscriptionManager();
            _subscriptionManager.SetDataManager(new StubDataManager());
            _mhdb = MarketHoursDatabase.FromDataFolder();
            _spdb = SymbolPropertiesDatabase.FromDataFolder();
            _securityService = new SecurityService(
                _cashBook,
                _mhdb,
                _spdb,
                new QCAlgorithm(),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCacheProvider(new ReportSecurityProvider()));
        }

        public decimal? Estimate(Result result)
        {
            Initialize();

            var orders = result?.Orders?.Values
                .OrderBy(o => o.LastFillTime ?? o.Time)
                .ToList();

            if (orders == null || orders.Count == 0)
            {
                return null;
            }

            var start = orders[0].LastFillTime ?? orders[0].Time;
            // Add a buffer of 1 day so that orders placed in the last trading day are snapshotted if the month changes.
            var end = (orders[orders.Count - 1].LastFillTime ?? orders[orders.Count - 1].Time).AddDays(1);

            SetupDataSubscriptions(orders);

            var configs = _cashBook.EnsureCurrencyDataFeeds(
                    _securityManager,
                    _subscriptionManager,
                    new DefaultBrokerageModel().DefaultMarkets,
                    new SecurityChanges(_securityManager.Values, Array.Empty<Security>()),
                    _securityService)
                .Concat(_subscriptionManager.Subscriptions);

            var capacity = AlgorithmCapacity(configs, orders, start, end).RoundToSignificantDigits(3);

            return capacity;
        }

        /// <summary>
        /// Triggered on a new slice update
        /// </summary>
        /// <param name="data"></param>
        public void OnData(Slice data)
        {
            if (data.Time.Month != _previousMonth && _previousMonth != 0)
            {
                TakeCapacitySnapshot(data.Time);
            }

            foreach (var symbol in data.Keys)
            {
                SymbolData symbolData;
                if (!_symbolData.TryGetValue(symbol, out symbolData))
                {
                    symbolData = new SymbolData(symbol, _timeZones[symbol]);
                    _symbolData[symbol] = symbolData;
                }

                symbolData.OnData(data);
            }

            _previousMonth = data.Time.Month;
        }

        /// <summary>
        /// Triggered on a new order event
        /// </summary>
        /// <param name="orderEvent">Order event</param>
        public void OnOrderEvent(OrderEvent orderEvent)
        {
            var symbol = orderEvent.Symbol;

            SymbolData symbolData;
            if (!_symbolData.TryGetValue(symbol, out symbolData))
            {
                symbolData = new SymbolData(symbol, _timeZones[symbol]);
                _symbolData[symbol] = symbolData;
            }

            symbolData.OnOrderEvent(orderEvent);
        }

        public void TakeCapacitySnapshot(DateTime time)
        {
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

            Capacity.Add(new ChartPoint(time, (minimumMarketVolume.Value.AverageCapacity) / symbolByPercentageOfAbsoluteDollarVolume[minimumMarketVolume.Key]));
            ResetData();
        }

        protected void ResetData()
        {
            foreach (var symbolData in _symbolData.Values)
            {
                symbolData.Reset();
            }
        }

        private void SetupDataSubscriptions(IEnumerable<Order> orders)
        {
            var symbols = LinqExtensions.ToHashSet(orders.Select(x => x.Symbol));

            foreach (var symbol in symbols)
            {
                var dataTimeZone = _mhdb.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);
                var exchangeTimeZone = _mhdb.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;

                _timeZones[symbol] = exchangeTimeZone;

                var config = _subscriptionManager.Add(symbol, _resolution, dataTimeZone, exchangeTimeZone);
                _securityManager.Add(_securityService.CreateSecurity(config.Symbol, config));

                if (config.Symbol.SecurityType == SecurityType.Crypto || config.Symbol.SecurityType == SecurityType.Forex)
                {
                    var quoteConfig = new SubscriptionDataConfig(config, tickType: TickType.Quote);
                    _subscriptionManager.Add(typeof(QuoteBar), TickType.Quote, symbol, _resolution, dataTimeZone, exchangeTimeZone, false);
                    _securityManager.Add(_securityService.CreateSecurity(quoteConfig.Symbol, quoteConfig));
                }
            }
        }

        private List<IEnumerator<BaseData>> ReadData(
            IEnumerable<SubscriptionDataConfig> configs,
            DateTime start,
            DateTime end)
        {
            var readers = new List<IEnumerator<BaseData>>();

            foreach (var config in configs)
            {
                foreach (var date in Time.EachDay(start, end))
                {
                    if (File.Exists(LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, date, _resolution, config.TickType)))
                    {
                        readers.Add(new LeanDataReader(config, config.Symbol, _resolution, date, Globals.DataFolder).Parse().GetEnumerator());
                    }
                }
            }

            return readers;
        }

        private List<List<BaseData>> SynchronizeData(IEnumerable<SubscriptionDataConfig> configs, DateTime start, DateTime end)
        {
            var readers = ReadData(configs, start, end);
            var dataEnumerators = readers.ToArray();
            var synchronizer = new SynchronizingEnumerator(dataEnumerators);

            var dataBinnedByTime = new List<List<BaseData>>();
            var currentData = new List<BaseData>();
            var currentTime = DateTime.MinValue;

            while (synchronizer.MoveNext())
            {
                if (synchronizer.Current == null || synchronizer.Current.EndTime > end)
                {
                    break;
                }

                if (synchronizer.Current.EndTime < start)
                {
                    continue;
                }

                if (currentTime == DateTime.MinValue)
                {
                    currentTime = synchronizer.Current.EndTime;
                }

                if (currentTime != synchronizer.Current.EndTime)
                {
                    dataBinnedByTime.Add(currentData);
                    currentData = new List<BaseData>();
                    currentData.Add(synchronizer.Current);
                    currentTime = synchronizer.Current.EndTime;

                    continue;
                }

                currentData.Add(synchronizer.Current);
            }

            if (currentData.Count != 0)
            {
                dataBinnedByTime.Add(currentData);
            }

            return dataBinnedByTime;
        }

        private void UpdateCurrencyConversionData(
            IEnumerable<SubscriptionDataConfig> configs,
            IEnumerable<BaseData> dataBin)
        {
            foreach (var config in configs)//conversionConfigs.Concat(_subscriptionManager.Subscriptions))
            {
                var symbol = config.Symbol;
                var cashMoney = _cashBook.Values.FirstOrDefault(x => x.ConversionRateSecurity?.Symbol == symbol);
                var currencyUpdateData = dataBin.FirstOrDefault(x => x.Symbol == symbol);

                if (cashMoney != null && currencyUpdateData != null)
                {
                    cashMoney.Update(currencyUpdateData);
                }
            }
        }

        private void DataToAccountCurrency(List<BaseData> dataBin)
        {
            var forexTrades = new List<BaseData>();

            foreach (var dataPoint in dataBin)
            {
                var symbolProperties = _spdb.GetSymbolProperties(dataPoint.Symbol.ID.Market, dataPoint.Symbol, dataPoint.Symbol.SecurityType, "USD");
                var bar = dataPoint as TradeBar;

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

                    continue;
                }
                var quoteBar = dataPoint as QuoteBar;
                if (quoteBar != null)
                {
                    // Order matters here, since we need to have the raw quote values to convert them into
                    // the account currency.
                    if (quoteBar.Symbol.SecurityType == SecurityType.Forex)
                    {
                        forexTrades.Add(new TradeBar(
                            quoteBar.Time,
                            quoteBar.Symbol,
                            _cashBook.ConvertToAccountCurrency(quoteBar.Open, symbolProperties.QuoteCurrency),
                            _cashBook.ConvertToAccountCurrency(quoteBar.High, symbolProperties.QuoteCurrency),
                            _cashBook.ConvertToAccountCurrency(quoteBar.Low, symbolProperties.QuoteCurrency),
                            _cashBook.ConvertToAccountCurrency(quoteBar.Close, symbolProperties.QuoteCurrency),
                            25000000m / _cashBook.ConvertToAccountCurrency(quoteBar.Close, symbolProperties.QuoteCurrency),
                            TimeSpan.FromMinutes(1)
                        ));
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

            dataBin.AddRange(forexTrades);
        }

        private List<OrderEvent> ToOrderEvents(List<Order> orders, DateTime dataTime, ref int cursor)
        {
            var orderEvents = new List<OrderEvent>();

            while (cursor < orders.Count)
            {
                var order = orders[cursor];
                var exchangeHours = _mhdb.GetEntry(order.Symbol.ID.Market, order.Symbol, order.Symbol.SecurityType)
                    .ExchangeHours;

                var orderEvent = new OrderEvent(order, order.LastFillTime ?? order.Time, OrderFee.Zero);
                var symbolProperties = _spdb.GetSymbolProperties(order.Symbol.ID.Market, order.Symbol, order.Symbol.SecurityType, "USD");

                // Price is in USD/ETH
                orderEvent.FillPrice = _cashBook.ConvertToAccountCurrency(order.Price, symbolProperties.QuoteCurrency);
                // Qty is in ETH, (ETH/1) * (USD/ETH) == USD
                // However, the OnData handler inside SymbolData in the StrategyCapacity
                // class will multiply this for us, so let's keep this in the asset quantity for now.
                orderEvent.FillQuantity = order.Quantity;
                orderEvent.UtcTime = order.Type == OrderType.MarketOnOpen
                    ? exchangeHours.GetNextMarketOpen(orderEvent.UtcTime.ConvertFromUtc(_timeZones[order.Symbol]), false).AddMinutes(1).ConvertToUtc(_timeZones[order.Symbol])
                    : orderEvent.UtcTime;

                if (orderEvent.UtcTime.ConvertFromUtc(_timeZones[order.Symbol]) > dataTime)
                {
                    break;
                }

                orderEvents.Add(orderEvent);
                cursor++;
            }

            return orderEvents;
        }

        private decimal AlgorithmCapacity(
            IEnumerable<SubscriptionDataConfig> configs,
            List<Order> orders,
            DateTime start,
            DateTime end)
        {
            var dataBinnedByTime = SynchronizeData(configs, start, end);
            var symbols = LinqExtensions.ToHashSet(orders.Select(x => x.Symbol));
            var cursor = 0;

            foreach (var dataBin in dataBinnedByTime)
            {
                UpdateCurrencyConversionData(configs, dataBin);
                DataToAccountCurrency(dataBin);

                var dataTime = dataBin[0].EndTime;
                var orderEvents = ToOrderEvents(orders, dataTime, ref cursor);

                var slice = new Slice(dataTime, dataBin.Where(x => symbols.Contains(x.Symbol)));
                OnData(slice);

                foreach (var orderEvent in orderEvents)
                {
                    OnOrderEvent(orderEvent);
                }
            }

            return Capacity.LastOrDefault()?.y ?? 0;
        }

        private class SymbolData
        {
            private Symbol _symbol;
            private TradeBar _previousBar;
            private QuoteBar _previousQuoteBar;
            private OrderEvent _previousTrade;
            public decimal AverageCapacity => (_marketCapacityDollarVolume / TradeCount) * 0.20m;

            private DateTime _timeout;
            private double _fastTradingVolumeDiscountFactor;
            private decimal _averageVolume;
            private readonly DateTimeZone _timeZone;

            public bool TradedBetweenSnapshots { get; private set; }

            public int TradeCount { get; private set; }
            public decimal AbsoluteTradingDollarVolume { get; private set; }
            private decimal _marketCapacityDollarVolume;

            public SymbolData(Symbol symbol, DateTimeZone timeZone)
            {
                _symbol = symbol;
                _timeZone = timeZone;
                _fastTradingVolumeDiscountFactor = 1;
            }

            public void OnOrderEvent(OrderEvent orderEvent)
            {
                TradedBetweenSnapshots = true;
                AbsoluteTradingDollarVolume += orderEvent.FillPrice * orderEvent.AbsoluteFillQuantity;
                TradeCount++;

                // Use 6000000 as the maximum bound for trading volume in a single minute.
                // Any bars that exceed 6 million total volume will be capped to a timeout of one minute.
                var k = _averageVolume != 0
                    ? 6000000 / _averageVolume
                    : 10;

                var timeoutMinutes = k > 60 ? 60 : (int)Math.Max(5, (double)k);

                // To reduce the capacity of high frequency strategies, we scale down the
                // volume captured on each bar proportional to the trades per day.
                _fastTradingVolumeDiscountFactor = 2 * (((orderEvent.UtcTime - (_previousTrade?.UtcTime ?? orderEvent.UtcTime.AddDays(-1))).TotalMinutes) / 390);
                _fastTradingVolumeDiscountFactor = _fastTradingVolumeDiscountFactor > 1 ? 1 : Math.Max(0.01, _fastTradingVolumeDiscountFactor);

                // When trades occur within 10 minutes the total volume we will capture is implicitly limited
                // because of the reduced time that we're capturing the volume
                _timeout = orderEvent.UtcTime.ConvertFromUtc(_timeZone).AddMinutes(timeoutMinutes);
                _previousTrade = orderEvent;
            }

            public void OnData(Slice data)
            {
                var bar = data.Bars.FirstOrDefault(x => x.Key == _symbol).Value;
                var quote = data.QuoteBars.FirstOrDefault(x => x.Key == _symbol).Value;

                if (bar != null)
                {
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

                    _previousBar = bar;
                }

                if (quote != null)
                {
                    var bidDepth = quote.LastBidSize;
                    var askDepth = quote.LastAskSize;

                    var bidSideMarketCapacity = bidDepth * quote.Bid?.Close ?? _previousQuoteBar?.Bid?.Close ?? _previousBar?.Close;
                    var askSideMarketCapacity = askDepth * quote.Ask?.Close ?? _previousQuoteBar?.Ask?.Close ?? _previousBar?.Close;

                    if (bidSideMarketCapacity != null && quote.EndTime <= _timeout)
                    {
                        _marketCapacityDollarVolume += bidSideMarketCapacity.Value;
                    }
                    if (askSideMarketCapacity != null && quote.EndTime <= _timeout)
                    {
                        _marketCapacityDollarVolume += askSideMarketCapacity.Value;
                    }

                    _previousQuoteBar = quote;
                }
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
