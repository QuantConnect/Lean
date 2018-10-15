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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;
using QuantConnect.Securities.Option;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Subscription data reader is a wrapper on the stream reader class to download, unpack and iterate over a data file.
    /// </summary>
    /// <remarks>The class accepts any subscription configuration and automatically makes it availble to enumerate</remarks>
    public class SubscriptionDataReader : IEnumerator<BaseData>
    {
        private bool _initialized;

        // Source string to create memory stream:
        private SubscriptionDataSource _source;

        private bool _endOfStream;

        private IEnumerator<BaseData> _subscriptionFactoryEnumerator;

        /// Configuration of the data-reader:
        private readonly SubscriptionDataConfig _config;

        /// true if we can find a scale factor file for the security of the form: ..\Lean\Data\equity\market\factor_files\{SYMBOL}.csv
        private bool _hasScaleFactors;

        // Location of the datafeed - the type of this data.

        // Create a single instance to invoke all Type Methods:
        private BaseData _dataFactory;

        //Start finish times of the backtest:
        private DateTime _periodStart;
        private readonly DateTime _periodFinish;

        private readonly MapFileResolver _mapFileResolver;
        private readonly IFactorFileProvider _factorFileProvider;
        private FactorFile _factorFile;
        private MapFile _mapFile;

        // we set the price factor ratio when we encounter a dividend in the factor file
        // and on the next trading day we use this data to produce the dividend instance
        private decimal? _priceFactorRatio;

        // we set the split factor when we encounter a split in the factor file
        // and on the next trading day we use this data to produce the split instance
        private decimal? _splitFactor;

        // we'll use these flags to denote we've already fired off the DelistingType.Warning
        // and a DelistedType.Delisted Delisting object, the _delistingType object is save here
        // since we need to wait for the next trading day before emitting
        private bool _delisted;
        private bool _delistedWarning;

        // true if we're in live mode, false otherwise
        private readonly bool _isLiveMode;
        private readonly bool _includeAuxilliaryData;

        private BaseData _previous;
        private readonly Queue<BaseData> _auxiliaryData;
        private readonly IEnumerator<DateTime> _tradeableDates;

        // used when emitting aux data from within while loop
        private bool _emittedAuxilliaryData;
        private BaseData _lastInstanceBeforeAuxilliaryData;
        private readonly IDataCacheProvider _dataCacheProvider;
        private DateTime _delistingDate;

        /// <summary>
        /// Event fired when an invalid configuration has been detected
        /// </summary>
        public event EventHandler<InvalidConfigurationDetectedEventArgs> InvalidConfigurationDetected;

        /// <summary>
        /// Event fired when the numerical precision in the factor file has been limited
        /// </summary>
        public event EventHandler<NumericalPrecisionLimitedEventArgs> NumericalPrecisionLimited;

        /// <summary>
        /// Event fired when there was an error downloading a remote file
        /// </summary>
        public event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        /// <summary>
        /// Event fired when there was an error reading the data
        /// </summary>
        public event EventHandler<ReaderErrorDetectedEventArgs> ReaderErrorDetected;

        /// <summary>
        /// Last read BaseData object from this type and source
        /// </summary>
        public BaseData Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Explicit Interface Implementation for Current
        /// </summary>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Subscription data reader takes a subscription request, loads the type, accepts the data source and enumerate on the results.
        /// </summary>
        /// <param name="config">Subscription configuration object</param>
        /// <param name="periodStart">Start date for the data request/backtest</param>
        /// <param name="periodFinish">Finish date for the data request/backtest</param>
        /// <param name="mapFileResolver">Used for resolving the correct map files</param>
        /// <param name="factorFileProvider">Used for getting factor files</param>
        /// <param name="dataCacheProvider">Used for caching files</param>
        /// <param name="tradeableDates">Defines the dates for which we'll request data, in order, in the security's exchange time zone</param>
        /// <param name="isLiveMode">True if we're in live mode, false otherwise</param>
        /// <param name="includeAuxilliaryData">True if we want to emit aux data, false to only emit price data</param>
        public SubscriptionDataReader(SubscriptionDataConfig config,
            DateTime periodStart,
            DateTime periodFinish,
            MapFileResolver mapFileResolver,
            IFactorFileProvider factorFileProvider,
            IEnumerable<DateTime> tradeableDates,
            bool isLiveMode,
            IDataCacheProvider dataCacheProvider,
            bool includeAuxilliaryData = true)
        {
            //Save configuration of data-subscription:
            _config = config;

            _auxiliaryData = new Queue<BaseData>();

            //Save Start and End Dates:
            _periodStart = periodStart;
            _periodFinish = periodFinish;
            _mapFileResolver = mapFileResolver;
            _factorFileProvider = factorFileProvider;
            _dataCacheProvider = dataCacheProvider;

            //Save access to securities
            _isLiveMode = isLiveMode;
            _includeAuxilliaryData = includeAuxilliaryData;

            _tradeableDates = tradeableDates.GetEnumerator();
        }

        /// <summary>
        /// Initializes the <see cref="SubscriptionDataReader"/> instance
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            //Save the type of data we'll be getting from the source.

            //Create the dynamic type-activators:
            var objectActivator = ObjectActivator.GetActivator(_config.Type);

            if (objectActivator == null)
            {
                OnInvalidConfigurationDetected(
                    new InvalidConfigurationDetectedEventArgs(
                        $"Custom data type \'{_config.Type.Name}\' missing parameterless constructor " +
                        $"E.g. public {_config.Type.Name}() {{ }}"));

                _endOfStream = true;
                return;
            }

            //Create an instance of the "Type":
            var userObj = objectActivator.Invoke(new object[] { _config.Type });

            _dataFactory = userObj as BaseData;

            //If its quandl set the access token in data factory:
            var quandl = _dataFactory as Quandl;
            if (quandl != null)
            {
                if (!Quandl.IsAuthCodeSet)
                {
                    Quandl.SetAuthCode(Config.Get("quandl-auth-token"));
                }
            }

            // If Tiingo data, set the access token in data factory
            var tiingo = _dataFactory as TiingoDailyData;
            if (tiingo != null)
            {
                if (!Tiingo.IsAuthCodeSet)
                {
                    Tiingo.SetAuthCode(Config.Get("tiingo-auth-token"));
                }
            }

            _factorFile = new FactorFile(_config.Symbol.Value, new List<FactorFileRow>());
            _mapFile = new MapFile(_config.Symbol.Value, new List<MapFileRow>());

            // load up the map and factor files for equities
            if (!_config.IsCustomData && _config.SecurityType == SecurityType.Equity)
            {
                try
                {
                    var mapFile = _mapFileResolver.ResolveMapFile(_config.Symbol.ID.Symbol, _config.Symbol.ID.Date);

                    // only take the resolved map file if it has data, otherwise we'll use the empty one we defined above
                    if (mapFile.Any()) _mapFile = mapFile;

                    var factorFile = _factorFileProvider.Get(_config.Symbol);
                    _hasScaleFactors = factorFile != null;
                    if (_hasScaleFactors)
                    {
                        _factorFile = factorFile;

                        // if factor file has minimum date, update start period if before minimum date
                        if (!_isLiveMode && _factorFile != null && _factorFile.FactorFileMinimumDate.HasValue)
                        {
                            if (_periodStart < _factorFile.FactorFileMinimumDate.Value)
                            {
                                _periodStart = _factorFile.FactorFileMinimumDate.Value;

                                OnNumericalPrecisionLimited(
                                    new NumericalPrecisionLimitedEventArgs(
                                        $"Data for symbol {_config.Symbol.Value} has been limited due to numerical precision issues in the factor file. " +
                                        $"The starting date has been set to {_factorFile.FactorFileMinimumDate.Value.ToShortDateString()}."));
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err, "Fetching Price/Map Factors: " + _config.Symbol.ID + ": ");
                }
            }

            // load up the map and factor files for underlying of equity option
            if (!_config.IsCustomData && _config.SecurityType == SecurityType.Option)
            {
                try
                {
                    var mapFile = _mapFileResolver.ResolveMapFile(_config.Symbol.Underlying.ID.Symbol, _config.Symbol.Underlying.ID.Date);

                    // only take the resolved map file if it has data, otherwise we'll use the empty one we defined above
                    if (mapFile.Any()) _mapFile = mapFile;
                }
                catch (Exception err)
                {
                    Log.Error(err, "Map Factors: " + _config.Symbol.ID + ": ");
                }
            }

            // Estimate delisting date.
            switch (_config.Symbol.ID.SecurityType)
            {
                case SecurityType.Future:
                    _delistingDate = _config.Symbol.ID.Date;
                    break;
                case SecurityType.Option:
                    _delistingDate = OptionSymbol.GetLastDayOfTrading(_config.Symbol);
                    break;
                default:
                    _delistingDate = _mapFile.DelistingDate;
                    break;
            }

            _subscriptionFactoryEnumerator = ResolveDataEnumerator(true);

            _initialized = true;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_endOfStream)
            {
                return false;
            }

            if (Current != null && Current.DataType != MarketDataType.Auxiliary)
            {
                // only save previous price data
                _previous = Current;
            }

            if (_subscriptionFactoryEnumerator == null)
            {
                // in live mode the trade able dates will eventually advance to the next
                if (_isLiveMode)
                {
                    // HACK attack -- we don't want to block in live mode
                    Current = null;
                    return true;
                }

                _endOfStream = true;
                return false;
            }

            do
            {
                // check for aux data first
                if (HasAuxDataBefore(_lastInstanceBeforeAuxilliaryData))
                {
                    // check for any auxilliary data before reading a line, but make sure
                    // it should be going ahead of '_lastInstanceBeforeAuxilliaryData'
                    Current = _auxiliaryData.Dequeue();
                    return true;
                }

                if (_emittedAuxilliaryData)
                {
                    _emittedAuxilliaryData = false;
                    Current = _lastInstanceBeforeAuxilliaryData;
                    _lastInstanceBeforeAuxilliaryData = null;
                    return true;
                }

                if (_delisted)
                {
                    break;
                }

                // keep enumerating until we find something that is within our time frame
                while (_subscriptionFactoryEnumerator.MoveNext())
                {
                    var instance = _subscriptionFactoryEnumerator.Current;
                    if (instance == null)
                    {
                        // keep reading until we get valid data
                        continue;
                    }

                    // prevent emitting past data, this can happen when switching symbols on daily data
                    if (_previous != null && _config.Resolution != Resolution.Tick)
                    {
                        if (_config.IsCustomData)
                        {
                            // Skip the point if time went backwards for custom data?
                            // TODO: Should this be the case for all datapoints?
                            if (instance.EndTime < _previous.EndTime) continue;
                        }
                        else
                        {
                            // all other resolutions don't allow duplicate end times
                            if (instance.EndTime <= _previous.EndTime) continue;
                        }
                    }

                    if (instance.EndTime < _periodStart)
                    {
                        // keep reading until we get a value on or after the start
                        _previous = instance;
                        continue;
                    }

                    if (instance.Time > _periodFinish)
                    {
                        // stop reading when we get a value after the end
                        _endOfStream = true;
                        return false;
                    }

                    // if we move past our current 'date' then we need to do daily things, such
                    // as updating factors and symbol mapping as well as detecting aux data
                    if (instance.EndTime.Date > _tradeableDates.Current)
                    {
                        var currentPriceScaleFactor = _config.PriceScaleFactor;

                        // this is fairly hacky and could be solved by removing the aux data from this class
                        // the case is with coarse data files which have many daily sized data points for the
                        // same date,
                        if (!_config.IsInternalFeed)
                        {
                            // this will advance the date enumerator and determine if a new
                            // instance of the subscription enumerator is required
                            _subscriptionFactoryEnumerator = ResolveDataEnumerator(false);
                        }

                        // we produce auxiliary data on date changes, but make sure our current instance
                        // isn't before it in time
                        if (HasAuxDataBefore(instance))
                        {
                            // since we're emitting this here we need to save off the instance for next time
                            Current = _auxiliaryData.Dequeue();
                            _emittedAuxilliaryData = true;

                            // with hourly resolution the first bar for the new date is received
                            // before the price scale factor is updated by ResolveDataEnumerator,
                            // so we have to 'rescale' prices before emitting the bar
                            if (_config.Resolution == Resolution.Hour &&
                               (_config.SecurityType == SecurityType.Equity || _config.SecurityType == SecurityType.Option))
                            {
                                var tradeBar = instance as TradeBar;
                                if (tradeBar != null)
                                {
                                    var bar = tradeBar;
                                    bar.Open = _config.GetNormalizedPrice(GetRawValue(bar.Open, _config.SumOfDividends, currentPriceScaleFactor));
                                    bar.High = _config.GetNormalizedPrice(GetRawValue(bar.High, _config.SumOfDividends, currentPriceScaleFactor));
                                    bar.Low = _config.GetNormalizedPrice(GetRawValue(bar.Low, _config.SumOfDividends, currentPriceScaleFactor));
                                    bar.Close = _config.GetNormalizedPrice(GetRawValue(bar.Close, _config.SumOfDividends, currentPriceScaleFactor));
                                }
                            }

                            _lastInstanceBeforeAuxilliaryData = instance;
                            return true;
                        }
                    }

                    // we've made it past all of our filters, we're withing the requested start/end of the subscription,
                    // we've satisfied user and market hour filters, so this data is good to go as current
                    Current = instance;

                    return true;
                }

                // we've ended the enumerator, time to refresh
                _subscriptionFactoryEnumerator = ResolveDataEnumerator(true);
            }
            while (_subscriptionFactoryEnumerator != null);

            _endOfStream = true;
            return false;
        }

        private bool HasAuxDataBefore(BaseData instance)
        {
            // this function is always used to check for aux data, as such, we'll implement the
            // feature of whether to include or not here so if other aux data is added we won't
            // need to remember this feature. this is mostly here until aux data gets moved into
            // its own subscription class
            if (!_includeAuxilliaryData) _auxiliaryData.Clear();
            if (_auxiliaryData.Count == 0) return false;
            if (instance == null) return true;
            return _auxiliaryData.Peek().EndTime < instance.EndTime;
        }

        /// <summary>
        /// Resolves the next enumerator to be used in <see cref="MoveNext"/>
        /// </summary>
        private IEnumerator<BaseData> ResolveDataEnumerator(bool endOfEnumerator)
        {
            do
            {
                // always advance the date enumerator, this function is intended to be
                // called on date changes, never return null for live mode, we'll always
                // just keep trying to refresh the subscription
                DateTime date;
                if (!TryGetNextDate(out date) && !_isLiveMode)
                {
                    // if we run out of dates then we're finished with this subscription
                    return null;
                }

                // fetch the new source, using the data time zone for the date
                var dateInDataTimeZone = date.ConvertTo(_config.ExchangeTimeZone, _config.DataTimeZone);
                var newSource = _dataFactory.GetSource(_config, dateInDataTimeZone, _isLiveMode);

                // check if we should create a new subscription factory
                var sourceChanged = _source != newSource && newSource.Source != "";
                var liveRemoteFile = _isLiveMode && (_source == null || _source.TransportMedium == SubscriptionTransportMedium.RemoteFile);
                if (sourceChanged || liveRemoteFile)
                {
                    // dispose of the current enumerator before creating a new one
                    Dispose();

                    // save off for comparison next time
                    _source = newSource;
                    var subscriptionFactory = CreateSubscriptionFactory(newSource);
                    return subscriptionFactory.Read(newSource).GetEnumerator();
                }

                // if there's still more in the enumerator and we received the same source from the GetSource call
                // above, then just keep using the same enumerator as we were before
                if (!endOfEnumerator) // && !sourceChanged is always true here
                {
                    return _subscriptionFactoryEnumerator;
                }

                // keep churning until we find a new source or run out of tradeable dates
                // in live mode tradeable dates won't advance beyond today's date, but
                // TryGetNextDate will return false if it's already at today
            }
            while (true);
        }

        private ISubscriptionDataSourceReader CreateSubscriptionFactory(SubscriptionDataSource source)
        {
            var factory = SubscriptionDataSourceReader.ForSource(source, _dataCacheProvider, _config, _tradeableDates.Current, _isLiveMode);
            AttachEventHandlers(factory, source);
            return factory;
        }

        private void AttachEventHandlers(ISubscriptionDataSourceReader dataSourceReader, SubscriptionDataSource source)
        {
            // handle missing files
            dataSourceReader.InvalidSource += (sender, args) =>
            {
                switch (args.Source.TransportMedium)
                {
                    case SubscriptionTransportMedium.LocalFile:
                        // the local uri doesn't exist, write an error and return null so we we don't try to get data for today
                        // Log.Trace(string.Format("SubscriptionDataReader.GetReader(): Could not find QC Data, skipped: {0}", source));
                        break;

                    case SubscriptionTransportMedium.RemoteFile:
                        OnDownloadFailed(
                            new DownloadFailedEventArgs(
                                $"Error downloading custom data source file, skipped: {source} " +
                                $"Error: {args.Exception.Message}", args.Exception.StackTrace));
                        break;

                    case SubscriptionTransportMedium.Rest:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };

            if (dataSourceReader is TextSubscriptionDataSourceReader)
            {
                // handle empty files/instantiation errors
                var textSubscriptionFactory = (TextSubscriptionDataSourceReader)dataSourceReader;
                textSubscriptionFactory.CreateStreamReaderError += (sender, args) =>
                {
                    //Log.Error(string.Format("Failed to get StreamReader for data source({0}), symbol({1}). Skipping date({2}). Reader is null.", args.Source.Source, _mappedSymbol, args.Date.ToShortDateString()));
                    if (_config.IsCustomData)
                    {
                        OnDownloadFailed(
                            new DownloadFailedEventArgs(
                                "We could not fetch the requested data. " +
                                "This may not be valid data, or a failed download of custom data. " +
                                $"Skipping source ({args.Source.Source})."));
                    }
                };

                // handle parser errors
                textSubscriptionFactory.ReaderError += (sender, args) =>
                {
                    OnReaderErrorDetected(
                        new ReaderErrorDetectedEventArgs(
                            $"Error invoking {_config.Symbol} data reader. " +
                            $"Line: {args.Line} Error: {args.Exception.Message}",
                            args.Exception.StackTrace));
                };
            }
        }

        /// <summary>
        /// Iterates the tradeable dates enumerator
        /// </summary>
        /// <param name="date">The next tradeable date</param>
        /// <returns>True if we got a new date from the enumerator, false if it's exhausted, or in live mode if we're already at today</returns>
        private bool TryGetNextDate(out DateTime date)
        {
            if (_isLiveMode && _tradeableDates.Current >= DateTime.Today)
            {
                // special behavior for live mode, don't advance past today
                date = _tradeableDates.Current;
                return false;
            }

            while (_tradeableDates.MoveNext())
            {
                date = _tradeableDates.Current;

                CheckForDelisting(date);
                if (_delisted)
                {
                    return true;
                }

                if (!_mapFile.HasData(date))
                {
                    continue;
                }

                // don't do other checks if we haven't goten data for this date yet
                if (_previous != null && _previous.EndTime > _tradeableDates.Current)
                {
                    continue;
                }

                // check for dividends and split for this security
                CheckForDividend(date);
                CheckForSplit(date);

                // if we have factor files check to see if we need to update the scale factors
                if (_hasScaleFactors)
                {
                    // update our price scaling factors in light of the normalization mode
                    UpdateScaleFactors(date);
                }

                // check to see if the symbol was remapped
                var newSymbol = _mapFile.GetMappedSymbol(date, _config.MappedSymbol);
                if (newSymbol != _config.MappedSymbol)
                {
                    var changed = new SymbolChangedEvent(_config.Symbol, date, _config.MappedSymbol, newSymbol);
                    _auxiliaryData.Enqueue(changed);
                    _config.MappedSymbol = newSymbol;
                }

                // we've passed initial checks,now go get data for this date!
                return true;
            }

            // no more tradeable dates, we've exhausted the enumerator
            date = DateTime.MaxValue.Date;
            return false;
        }

        /// <summary>
        /// For backwards adjusted data the price is adjusted by a scale factor which is a combination of splits and dividends.
        /// This backwards adjusted price is used by default and fed as the current price.
        /// </summary>
        /// <param name="date">Current date of the backtest.</param>
        private void UpdateScaleFactors(DateTime date)
        {
            switch (_config.DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    return;

                case DataNormalizationMode.TotalReturn:
                case DataNormalizationMode.SplitAdjusted:
                    _config.PriceScaleFactor = _factorFile.GetSplitFactor(date);
                    break;

                case DataNormalizationMode.Adjusted:
                    _config.PriceScaleFactor = _factorFile.GetPriceScaleFactor(date);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Reset the IEnumeration
        /// </summary>
        /// <remarks>Not used</remarks>
        public void Reset()
        {
            throw new NotImplementedException("Reset method not implemented. Assumes loop will only be used once.");
        }

        /// <summary>
        /// Check for dividends and emit them into the aux data queue
        /// </summary>
        private void CheckForSplit(DateTime date)
        {
            var factor = _splitFactor;
            if (factor != null)
            {
                var close = GetRawClose();
                var split = new Split(_config.Symbol, date, close, factor.Value, SplitType.SplitOccurred);
                _auxiliaryData.Enqueue(split);
                _splitFactor = null;
            }

            decimal splitFactor;
            if (_factorFile.HasSplitEventOnNextTradingDay(date, out splitFactor))
            {
                _splitFactor = splitFactor;
                var split = new Split(_config.Symbol, date, GetRawClose(), splitFactor, SplitType.Warning);
                _auxiliaryData.Enqueue(split);
            }
        }

        /// <summary>
        /// Check for dividends and emit them into the aux data queue
        /// </summary>
        private void CheckForDividend(DateTime date)
        {
            if (_priceFactorRatio != null)
            {
                var close = GetRawClose();
                var dividend = Dividend.Create(_config.Symbol, date, close, _priceFactorRatio.Value);
                // let the config know about it for normalization
                _config.SumOfDividends += dividend.Distribution;
                _auxiliaryData.Enqueue(dividend);
                _priceFactorRatio = null;
            }

            // check the factor file to see if we have a dividend event tomorrow
            decimal priceFactorRatio;
            if (_factorFile.HasDividendEventOnNextTradingDay(date, out priceFactorRatio))
            {
                _priceFactorRatio = priceFactorRatio;
            }
        }

        /// <summary>
        /// Check for delistings and emit them into the aux data queue
        /// </summary>
        private void CheckForDelisting(DateTime date)
        {
            if (!_delistedWarning && date >= _delistingDate)
            {
                _delistedWarning = true;
                var price = _previous != null ? _previous.Price : 0;
                _auxiliaryData.Enqueue(new Delisting(_config.Symbol, date, price, DelistingType.Warning));
            }
            else if (!_delisted && date > _delistingDate)
            {
                _delisted = true;
                var price = _previous != null ? _previous.Price : 0;
                // delisted at EOD
                _auxiliaryData.Enqueue(new Delisting(_config.Symbol, _delistingDate.AddDays(1), price, DelistingType.Delisted));
            }
        }

        /// <summary>
        /// Un-normalizes the Previous.Value
        /// </summary>
        private decimal GetRawClose()
        {
            return _previous == null ? 0m : GetRawValue(_previous.Value, _config.SumOfDividends, _config.PriceScaleFactor);
        }

        /// <summary>
        /// Un-normalizes a price
        /// </summary>
        private decimal GetRawValue(decimal price, decimal sumOfDividends, decimal priceScaleFactor)
        {
            switch (_config.DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    break;

                case DataNormalizationMode.SplitAdjusted:
                case DataNormalizationMode.Adjusted:
                    // we need to 'unscale' the price
                    price = price / priceScaleFactor;
                    break;

                case DataNormalizationMode.TotalReturn:
                    // we need to remove the dividends since we've been accumulating them in the price
                    price = (price - sumOfDividends) / priceScaleFactor;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return price;
        }

        /// <summary>
        /// Dispose of the Stream Reader and close out the source stream and file connections.
        /// </summary>
        public void Dispose()
        {
            _subscriptionFactoryEnumerator?.Dispose();
        }

        /// <summary>
        /// Event invocator for the <see cref="InvalidConfigurationDetected"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="InvalidConfigurationDetected"/> event</param>
        protected virtual void OnInvalidConfigurationDetected(InvalidConfigurationDetectedEventArgs e)
        {
            InvalidConfigurationDetected?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="NumericalPrecisionLimited"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="NumericalPrecisionLimited"/> event</param>
        protected virtual void OnNumericalPrecisionLimited(NumericalPrecisionLimitedEventArgs e)
        {
            NumericalPrecisionLimited?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="DownloadFailed"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="DownloadFailed"/> event</param>
        protected virtual void OnDownloadFailed(DownloadFailedEventArgs e)
        {
            DownloadFailed?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="ReaderErrorDetected"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="ReaderErrorDetected"/> event</param>
        protected virtual void OnReaderErrorDetected(ReaderErrorDetectedEventArgs e)
        {
            ReaderErrorDetected?.Invoke(this, e);
        }
    }
}
