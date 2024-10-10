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
using NodaTime;

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
        private readonly DateChangeTimeKeeper _timeKeeper;

        // used when emitting aux data from within while loop
        private readonly IDataCacheProvider _dataCacheProvider;
        private DateTime _delistingDate;

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

            //Save access to securities
            var tradeableDates = dataRequest.TradableDaysInDataTimeZone.GetEnumerator();
            _timeKeeper = new DateChangeTimeKeeper(tradeableDates, _config.DataTimeZone, _config.ExchangeTimeZone);
            _timeKeeper.NewExchangeDate += HandleNewTradableDate;
            _dataProvider = dataProvider;
            _objectStore = objectStore;
        }

        private void HandleNewTradableDate(object sender, DateTime date)
        {
            OnNewTradableDate(new NewTradableDateEventArgs(date, _previous, _config.Symbol, _lastRawPrice));
            UpdateDataEnumerator(false, false);
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
            UpdateDataEnumerator(true, true);

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

                    var currentSource = _source;
                    var foundNewExchangeDate = false;
                    while (_timeKeeper.ExchangeTime < instance.EndTime && currentSource == _source)
                    {
                        _timeKeeper.AdvanceUntil(instance.EndTime.ConvertToUtc(_config.ExchangeTimeZone), _config.Increment, out foundNewExchangeDate);
                    }

                    var shouldGoThrough = false;
                    // New exchange date found and the enumerator was updated: skip this point and read again
                    if (foundNewExchangeDate && currentSource != _source)
                    {
                        // A mapping happened, so we might start reading a file with data before the mapping date
                        // (e.g. if exchange tz is behind data tz), so set a frontier
                        _mappingFrontier = _timeKeeper.ExchangeTime;

                        if (_config.Resolution != Resolution.Daily ||
                            // If daily and exchange tz is behind data tz, we skip the current bar
                            // since its end time will be the day after the mapping date
                            _timeKeeper.DataTime > _timeKeeper.ExchangeTime ||
                            // Current instance is after mapping date
                            instance.EndTime > _timeKeeper.ExchangeTime)
                        {
                            continue;
                        }

                        shouldGoThrough = true;
                    }

                    // Skip data until we reach the mapping date. Don't rely on the _previous instance,
                    // it could be a data point from a distant past
                    if (!shouldGoThrough && _mappingFrontier != default)
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
                UpdateDataEnumerator(true, true);
            }
            while (_subscriptionFactoryEnumerator != null);

            _endOfStream = true;
            return false;
        }

        private bool _updating;

        /// <summary>
        /// Resolves the next enumerator to be used in <see cref="MoveNext"/> and updates
        /// <see cref="_subscriptionFactoryEnumerator"/>
        /// </summary>
        /// <returns>True, if the enumerator has been updated (even if updated to null)</returns>
        private bool UpdateDataEnumerator(bool endOfEnumerator, bool updateDate)
        {
            // Guard for infinite recursion: during an enumerator update, we might ask for a new date,
            // which might end up with a new exchange date being detected and another update being requested.
            // Just skip that update and let's do it ourselves after the date is resolved
            if (_updating)
            {
                return false;
            }

            _updating = true;
            try
            {
                do
                {
                    var date = _timeKeeper.DataTime.Date;

                    // always advance the date enumerator, this function is intended to be
                    // called on date changes, never return null for live mode, we'll always
                    // just keep trying to refresh the subscription
                    if (updateDate && !TryGetNextDate(out date))
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
                        Dispose();

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
                _updating = false;
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
            _timeKeeper.DisposeSafely();
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

        private class DateChangeTimeKeeper : TimeKeeper, IDisposable
        {
            private IEnumerator<DateTime> _tradableDatesInDataTimeZone;
            private DateTimeZone _dataTimeZone;
            private DateTimeZone _exchangeTimeZone;

            public DateTime DataTime => GetTimeIn(_dataTimeZone);

            public DateTime ExchangeTime => GetTimeIn(_exchangeTimeZone);

            public event EventHandler<DateTime> NewExchangeDate;

            public DateChangeTimeKeeper(IEnumerator<DateTime> tradableDatesInDataTimeZone, DateTimeZone dataTimeZone,
                DateTimeZone exchangeTimeZone)
                : base(Time.BeginningOfTime, new[] { dataTimeZone, exchangeTimeZone } )
            {
                _tradableDatesInDataTimeZone = tradableDatesInDataTimeZone;
                _dataTimeZone = dataTimeZone;
                _exchangeTimeZone = exchangeTimeZone;
            }

            public void Dispose()
            {
                _tradableDatesInDataTimeZone.DisposeSafely();
            }

            private DateTime _previousNewExchangeDate;

            public void AdvanceUntil(DateTime endTimeUtc, TimeSpan step, out bool foundNewExchangeDate)
            {
                var currentTimeUtc = UtcTime;
                if (endTimeUtc <= currentTimeUtc)
                {
                    foundNewExchangeDate = false;
                    return;
                }

                while (currentTimeUtc < endTimeUtc)
                {
                    var newTimeUtc = currentTimeUtc + step;
                    if (newTimeUtc > endTimeUtc)
                    {
                        newTimeUtc = endTimeUtc;
                    }

                    var previousExchangeDate = ExchangeTime.Date;
                    var newExchangeTime = newTimeUtc.ConvertFromUtc(_exchangeTimeZone);
                    var newExchangeDate = newExchangeTime.Date;
                    if (newExchangeDate != previousExchangeDate)
                    {
                        SetUtcDateTime(newExchangeTime.ConvertToUtc(_exchangeTimeZone));
                        NewExchangeDate?.Invoke(this, newExchangeDate);
                        _previousNewExchangeDate = newExchangeDate;
                        foundNewExchangeDate = true;
                        return;
                    }

                    currentTimeUtc = newTimeUtc;
                }

                SetUtcDateTime(endTimeUtc);
                foundNewExchangeDate = false;
            }

            private bool _needsMoveNext = true;

            public bool TryAdvanceUntilNextDataDate()
            {
                if (!_needsMoveNext || _tradableDatesInDataTimeZone.MoveNext())
                {
                    _needsMoveNext = true;
                    var nextDataDate = _tradableDatesInDataTimeZone.Current;
                    var nextExchangeTime = nextDataDate.ConvertTo(_dataTimeZone, _exchangeTimeZone);

                    if (nextExchangeTime.Date > _previousNewExchangeDate)
                    {
                        _needsMoveNext = false;
                        nextDataDate = nextExchangeTime.Date.ConvertTo(_exchangeTimeZone, _dataTimeZone);
                        NewExchangeDate?.Invoke(this, nextExchangeTime.Date);
                        _previousNewExchangeDate = nextExchangeTime.Date;
                    }

                    SetUtcDateTime(nextDataDate.ConvertToUtc(_dataTimeZone));
                    return true;
                }

                return false;
            }
        }
    }
}
