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
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Data;
using System.Collections;
using System.Globalization;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Subscription data reader is a wrapper on the stream reader class to download, unpack and iterate over a data file.
    /// </summary>
    /// <remarks>The class accepts any subscription configuration and automatically makes it available to enumerate</remarks>
    public class SubscriptionDataReader : IEnumerator<BaseData>, ITradableDatesNotifier, IDataProviderEvents
    {
        private IDataProvider _dataProvider;
        private IObjectStore _objectStore;
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

        private readonly IMapFileProvider _mapFileProvider;
        private readonly IFactorFileProvider _factorFileProvider;
        private IFactorProvider _factorFile;
        private MapFile _mapFile;

        private bool _pastDelistedDate;

        private BaseData _previous;
        private decimal? _lastRawPrice;
        private DateChangeTimeKeeper _timeKeeper;
        private readonly IEnumerable<DateTime> _tradableDatesInDataTimeZone;
        private readonly SecurityExchangeHours _exchangeHours;

        // used when emitting aux data from within while loop
        private readonly IDataCacheProvider _dataCacheProvider;
        private DateTime _delistingDate;

        private bool _updatingDataEnumerator;
        private DateTime _mappingFrontier;

        /// <summary>
        /// Event fired when an invalid configuration has been detected
        /// </summary>
        public event EventHandler<InvalidConfigurationDetectedEventArgs> InvalidConfigurationDetected;

        /// <summary>
        /// Event fired when the numerical precision in the factor file has been limited
        /// </summary>
        public event EventHandler<NumericalPrecisionLimitedEventArgs> NumericalPrecisionLimited;

        /// <summary>
        /// Event fired when the start date has been limited
        /// </summary>
        public event EventHandler<StartDateLimitedEventArgs> StartDateLimited;

        /// <summary>
        /// Event fired when there was an error downloading a remote file
        /// </summary>
        public event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        /// <summary>
        /// Event fired when there was an error reading the data
        /// </summary>
        public event EventHandler<ReaderErrorDetectedEventArgs> ReaderErrorDetected;

        /// <summary>
        /// Event fired when there is a new tradable date
        /// </summary>
        public event EventHandler<NewTradableDateEventArgs> NewTradableDate;

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
        /// <param name="dataRequest">The data request</param>
        /// <param name="mapFileProvider">Used for resolving the correct map files</param>
        /// <param name="factorFileProvider">Used for getting factor files</param>
        /// <param name="dataCacheProvider">Used for caching files</param>
        /// <param name="dataProvider">The data provider to use</param>
        public SubscriptionDataReader(SubscriptionDataConfig config,
            BaseDataRequest dataRequest,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            IDataCacheProvider dataCacheProvider,
            IDataProvider dataProvider,
            IObjectStore objectStore)
        {
            //Save configuration of data-subscription:
            _config = config;

            //Save Start and End Dates:
            _periodStart = dataRequest.StartTimeLocal;
            _periodFinish = dataRequest.EndTimeLocal;
            _mapFileProvider = mapFileProvider;
            _factorFileProvider = factorFileProvider;
            _dataCacheProvider = dataCacheProvider;

            _dataProvider = dataProvider;
            _objectStore = objectStore;

            _tradableDatesInDataTimeZone = dataRequest.TradableDaysInDataTimeZone;
            _exchangeHours = dataRequest.ExchangeHours;
        }

        /// <summary>
        /// Initializes the <see cref="SubscriptionDataReader"/> instance
        /// </summary>
        /// <remarks>Should be called after all consumers of <see cref="NewTradableDate"/> event are set,
        /// since it will produce events.</remarks>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            //Save the type of data we'll be getting from the source.
            try
            {
                _dataFactory = _config.GetBaseDataInstance();
            }
            catch (ArgumentException exception)
            {
                OnInvalidConfigurationDetected(new InvalidConfigurationDetectedEventArgs(_config.Symbol, exception.Message));
                _endOfStream = true;
                return;
            }

            // If Tiingo data, set the access token in data factory
            var tiingo = _dataFactory as TiingoPrice;
            if (tiingo != null)
            {
                if (!Tiingo.IsAuthCodeSet)
                {
                    Tiingo.SetAuthCode(Config.Get("tiingo-auth-token"));
                }
            }

            // load up the map files for equities, options, and custom data if it supports it.
            // Only load up factor files for equities
            if (_dataFactory.RequiresMapping())
            {
                try
                {
                    var mapFile = _mapFileProvider.ResolveMapFile(_config);

                    // only take the resolved map file if it has data, otherwise we'll use the empty one we defined above
                    if (mapFile.Any()) _mapFile = mapFile;

                    if (_config.PricesShouldBeScaled())
                    {
                        var factorFile = _factorFileProvider.Get(_config.Symbol);
                        _hasScaleFactors = factorFile != null;
                        if (_hasScaleFactors)
                        {
                            _factorFile = factorFile;

                            // if factor file has minimum date, update start period if before minimum date
                            if (_factorFile != null && _factorFile.FactorFileMinimumDate.HasValue)
                            {
                                if (_periodStart < _factorFile.FactorFileMinimumDate.Value)
                                {
                                    _periodStart = _factorFile.FactorFileMinimumDate.Value;

                                    OnNumericalPrecisionLimited(
                                        new NumericalPrecisionLimitedEventArgs(_config.Symbol,
                                            $"[{_config.Symbol.Value}, {_factorFile.FactorFileMinimumDate.Value.ToShortDateString()}]"));
                                }
                            }
                        }

                        if (_periodStart < mapFile.FirstDate)
                        {
                            _periodStart = mapFile.FirstDate;

                            OnStartDateLimited(
                                new StartDateLimitedEventArgs(_config.Symbol,
                                    $"[{_config.Symbol.Value}," +
                                    $" {mapFile.FirstDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}]"));
                        }
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err, "Fetching Price/Map Factors: " + _config.Symbol.ID + ": ");
                }
            }

            _factorFile ??= _config.Symbol.GetEmptyFactorFile();
            _mapFile ??= new MapFile(_config.Symbol.Value, Enumerable.Empty<MapFileRow>());

            _delistingDate = _config.Symbol.GetDelistingDate(_mapFile);

            // adding a day so we stop at EOD
            _delistingDate = _delistingDate.AddDays(1);

            _timeKeeper = new DateChangeTimeKeeper(_tradableDatesInDataTimeZone, _config, _exchangeHours, _delistingDate);
            _timeKeeper.NewExchangeDate += HandleNewTradableDate;

            UpdateDataEnumerator(true);

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
                // Late initialization so it is performed in the data feed stack
                // and not in the algorithm thread
                Initialize();
            }

            if (_endOfStream)
            {
                return false;
            }

            if (Current != null)
            {
                // only save previous price data
                _previous = Current;
            }

            if (_subscriptionFactoryEnumerator == null)
            {
                _endOfStream = true;
                return false;
            }

            do
            {
                if (_pastDelistedDate)
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

                    // We rely on symbol change to detect a mapping or symbol change, instead of using SubscriptionDataConfig.NewSymbol
                    // because only one of the configs with the same symbol will trigger a symbol change event.
                    var previousMappedSymbol = _config.MappedSymbol;

                    // Advance the time keeper either until the current instance time (to synchronize) or until the source changes.
                    // Note: use time instead of end time to avoid skipping instances that all have the same timestamps in the same file (e.g. universe data)
                    var currentSource = _source;
                    var nextExchangeDate = _config.Resolution == Resolution.Daily && _timeKeeper.IsExchangeBehindData()
                        // If daily and exchange is behind data, data for date X will have a start time within date X-1,
                        // so we use the actual date from end time. e.g. a daily bar for Jan15 can have a start time of Jan14 8PM
                        // (exchange tz 4 hours behind data tz) and end time would be Jan15 8PM.
                        ? instance.EndTime.Date
                        : instance.Time;
                    while (_timeKeeper.ExchangeTime < nextExchangeDate && currentSource == _source)
                    {
                        _timeKeeper.AdvanceTowardsExchangeTime(nextExchangeDate);
                    }

                    // Source change, check if we should emit the current instance
                    if (currentSource != _source)
                    {
                        var mappingOccured = _config.MappedSymbol != previousMappedSymbol;
                        if (mappingOccured)
                        {
                            // Update the frontier to the current instance time
                            _mappingFrontier = _timeKeeper.ExchangeTime;
                        }

                        // Should the current instance be skipped?:
                        if (// After a mapping for every resolution except daily:
                            // For other resolutions, the instance that triggered the exchange date change should be skipped,
                            // it's end time will be either midnight or for a future date. The new source might have a data point with this times.
                            (mappingOccured && _config.Resolution != Resolution.Daily)

                            // Skip if the exchange time zone is behind of the data time zone:
                            // The new source might have data for these same times, we want data for the new symbol
                            || (_config.Resolution == Resolution.Daily && _timeKeeper.IsExchangeBehindData())

                            // skip if the instance if it's beyond what the previous source should have.
                            // e.g. A file mistakenly has data for the next day
                            // (see SubscriptionDataReaderTests.DoesNotEmitDataBeyondTradableDate unit test)
                            // or the instance that triggered the exchange date change is for a future date (no data found in between)
                            || instance.EndTime.ConvertTo(_config.ExchangeTimeZone, _config.DataTimeZone).Date >= _timeKeeper.DataTime.Date)
                        {
                            continue;
                        }
                    }
                    // Skip data until we reach the mapping date. Don't rely on the _previous instance,
                    // it could be a data point from a distant past
                    else if (_mappingFrontier != default)
                    {
                        if (instance.EndTime < _mappingFrontier)
                        {
                            continue;
                        }

                        // Reset so we don't keep skipping data
                        _mappingFrontier = default;
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

                    // We have to perform this check after refreshing the enumerator, if appropriate
                    // 'instance' could be a data point far in the future due to remapping (GH issue 5232) in which case it will be dropped
                    if (instance.Time > _periodFinish)
                    {
                        // stop reading when we get a value after the end
                        _endOfStream = true;
                        return false;
                    }

                    // we've made it past all of our filters, we're withing the requested start/end of the subscription,
                    // we've satisfied user and market hour filters, so this data is good to go as current
                    Current = instance;

                    // we keep the last raw price registered before we return so we are not affected by anyone (price scale) modifying our current
                    _lastRawPrice = Current.Price;
                    return true;
                }

                // we've ended the enumerator, time to refresh
                UpdateDataEnumerator(true);
            }
            while (_subscriptionFactoryEnumerator != null);

            _endOfStream = true;
            return false;
        }

        /// <summary>
        /// Emits a new tradable date event and tries to update the data enumerator if necessary
        /// </summary>
        private void HandleNewTradableDate(object sender, DateTime date)
        {
            if (_config.TickType == TickType.OpenInterest && _config.Symbol.IsCanonical() && date.Month == 12 && date.Day > 15)
            {

            }

            OnNewTradableDate(new NewTradableDateEventArgs(date, _previous, _config.Symbol, _lastRawPrice));
            UpdateDataEnumerator(false);
        }

        /// <summary>
        /// Resolves the next enumerator to be used in <see cref="MoveNext"/> and updates
        /// <see cref="_subscriptionFactoryEnumerator"/>
        /// </summary>
        /// <returns>True, if the enumerator has been updated (even if updated to null)</returns>
        private bool UpdateDataEnumerator(bool endOfEnumerator)
        {
            // Guard for infinite recursion: during an enumerator update, we might ask for a new date,
            // which might end up with a new exchange date being detected and another update being requested.
            // Just skip that update and let's do it ourselves after the date is resolved
            if (_updatingDataEnumerator)
            {
                return false;
            }

            _updatingDataEnumerator = true;
            try
            {
                do
                {
                    var date = _timeKeeper.DataTime.Date;

                    // Update current date only if the enumerator has ended, else we might just need to change files
                    // (e.g. same date, but symbol was mapped)
                    if (endOfEnumerator && !TryGetNextDate(out date))
                    {
                        _subscriptionFactoryEnumerator = null;
                        // if we run out of dates then we're finished with this subscription
                        return true;
                    }

                    // fetch the new source, using the data time zone for the date
                    var newSource = _dataFactory.GetSource(_config, date, false);
                    if (newSource == null)
                    {
                        // move to the next day
                        continue;
                    }

                    // check if we should create a new subscription factory
                    var sourceChanged = _source != newSource && !string.IsNullOrEmpty(newSource.Source);
                    if (sourceChanged)
                    {
                        // dispose of the current enumerator before creating a new one
                        _subscriptionFactoryEnumerator.DisposeSafely();

                        // save off for comparison next time
                        _source = newSource;
                        var subscriptionFactory = CreateSubscriptionFactory(newSource, _dataFactory, _dataProvider);
                        _subscriptionFactoryEnumerator = SortEnumerator<DateTime>.TryWrapSortEnumerator(newSource.Sort, subscriptionFactory.Read(newSource));
                        return true;
                    }

                    // if there's still more in the enumerator and we received the same source from the GetSource call
                    // above, then just keep using the same enumerator as we were before
                    if (!endOfEnumerator) // && !sourceChanged is always true here
                    {
                        return false;
                    }

                    // keep churning until we find a new source or run out of tradeable dates
                    // in live mode tradeable dates won't advance beyond today's date, but
                    // TryGetNextDate will return false if it's already at today
                }
                while (true);
            }
            finally
            {
                _updatingDataEnumerator = false;
            }
        }

        private ISubscriptionDataSourceReader CreateSubscriptionFactory(SubscriptionDataSource source, BaseData baseDataInstance, IDataProvider dataProvider)
        {
            var factory = SubscriptionDataSourceReader.ForSource(source, _dataCacheProvider, _config, _timeKeeper.DataTime.Date, false, baseDataInstance, dataProvider, _objectStore);
            AttachEventHandlers(factory, source);
            return factory;
        }

        private void AttachEventHandlers(ISubscriptionDataSourceReader dataSourceReader, SubscriptionDataSource source)
        {
            dataSourceReader.InvalidSource += (sender, args) =>
            {
                if (_config.IsCustomData && !_config.Type.GetBaseDataInstance().IsSparseData())
                {
                    OnDownloadFailed(
                        new DownloadFailedEventArgs(_config.Symbol,
                            "We could not fetch the requested data. " +
                            "This may not be valid data, or a failed download of custom data. " +
                            $"Skipping source ({args.Source.Source})."));
                    return;
                }

                switch (args.Source.TransportMedium)
                {
                    case SubscriptionTransportMedium.LocalFile:
                        // the local uri doesn't exist, write an error and return null so we we don't try to get data for today
                        // Log.Trace(string.Format("SubscriptionDataReader.GetReader(): Could not find QC Data, skipped: {0}", source));
                        break;

                    case SubscriptionTransportMedium.RemoteFile:
                        OnDownloadFailed(
                            new DownloadFailedEventArgs(_config.Symbol,
                                $"Error downloading custom data source file, skipped: {source} " +
                                $"Error: {args.Exception.Message}", args.Exception.StackTrace));
                        break;

                    case SubscriptionTransportMedium.Rest:
                        break;

                    case SubscriptionTransportMedium.ObjectStore:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };

            if (dataSourceReader is TextSubscriptionDataSourceReader)
            {
                // handle empty files/instantiation errors
                var textSubscriptionFactory = (TextSubscriptionDataSourceReader)dataSourceReader;
                // handle parser errors
                textSubscriptionFactory.ReaderError += (sender, args) =>
                {
                    OnReaderErrorDetected(
                        new ReaderErrorDetectedEventArgs(_config.Symbol,
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
            while (_timeKeeper.TryAdvanceUntilNextDataDate())
            {
                date = _timeKeeper.DataTime.Date;

                if (_pastDelistedDate || date > _delistingDate)
                {
                    // if we already passed our delisting date we stop
                    _pastDelistedDate = true;
                    break;
                }

                if (!_mapFile.HasData(date))
                {
                    continue;
                }

                // don't do other checks if we haven't gotten data for this date yet
                if (_previous != null && _previous.EndTime.ConvertTo(_config.ExchangeTimeZone, _config.DataTimeZone) > date)
                {
                    continue;
                }

                // we've passed initial checks,now go get data for this date!
                return true;
            }

            // no more tradeable dates, we've exhausted the enumerator
            date = DateTime.MaxValue.Date;
            return false;
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
        /// Dispose of the Stream Reader and close out the source stream and file connections.
        /// </summary>
        public void Dispose()
        {
            _subscriptionFactoryEnumerator.DisposeSafely();

            if (_initialized)
            {
                _timeKeeper.NewExchangeDate -= HandleNewTradableDate;
                _timeKeeper.DisposeSafely();
            }
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
        /// Event invocator for the <see cref="StartDateLimited"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="StartDateLimited"/> event</param>
        protected virtual void OnStartDateLimited(StartDateLimitedEventArgs e)
        {
            StartDateLimited?.Invoke(this, e);
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

        /// <summary>
        /// Event invocator for the <see cref="NewTradableDate"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="NewTradableDate"/> event</param>
        protected virtual void OnNewTradableDate(NewTradableDateEventArgs e)
        {
            NewTradableDate?.Invoke(this, e);
        }
    }
}
