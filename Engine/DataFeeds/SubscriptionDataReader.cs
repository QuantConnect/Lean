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
using System.ComponentModel;
using System.IO;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Subscription data reader is a wrapper on the stream reader class to download, unpack and iterate over a data file.
    /// </summary>
    /// <remarks>The class accepts any subscription configuration and automatically makes it availble to enumerate</remarks>
    public class SubscriptionDataReader : IEnumerator<BaseData>
    {
        // Source string to create memory stream:
        private SubscriptionDataSource _source;

        private bool _endOfStream;

        // Date of this source file.
        private DateTime _date = new DateTime();

        /// Internal stream reader for processing data line by line:
        private IStreamReader _reader;

        /// Configuration of the data-reader:
        private readonly SubscriptionDataConfig _config;

        /// Subscription Securities Access
        private readonly Security _security;

        /// true if we can find a scale factor file for the security of the form: ..\Lean\Data\equity\market\factor_files\{SYMBOL}.csv
        private readonly bool _hasScaleFactors;

        // Subscription is for a QC type:
        private readonly bool _isDynamicallyLoadedData;

        // Symbol Mapping:
        private string _mappedSymbol = "";

        // Location of the datafeed - the type of this data.
        private readonly DataFeedEndpoint _feedEndpoint;

        // Create a single instance to invoke all Type Methods:
        private readonly BaseData _dataFactory;

        //Start finish times of the backtest:
        private readonly DateTime _periodStart;
        private readonly DateTime _periodFinish;

        private readonly FactorFile _factorFile;
        private readonly MapFile _mapFile;

        // we set the price factor ratio when we encounter a dividend in the factor file
        // and on the next trading day we use this data to produce the dividend instance
        private decimal? _priceFactorRatio;

        // we set the split factor when we encounter a split in the factor file
        // and on the next trading day we use this data to produce the split instance
        private decimal? _splitFactor;

        // true if we're in live mode, false otherwise
        private readonly bool _isLiveMode;

        private BaseData _previous;
        private readonly Queue<BaseData> _auxiliaryData;
        private readonly IResultHandler _resultHandler;
        private readonly IEnumerator<DateTime> _tradeableDates;

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
        /// <param name="security">Security asset</param>
        /// <param name="feed">Feed type enum</param>
        /// <param name="periodStart">Start date for the data request/backtest</param>
        /// <param name="periodFinish">Finish date for the data request/backtest</param>
        /// <param name="resultHandler"></param>
        /// <param name="tradeableDates">Defines the dates for which we'll request data, in order</param>
        public SubscriptionDataReader(SubscriptionDataConfig config, Security security, DataFeedEndpoint feed, DateTime periodStart, DateTime periodFinish, IResultHandler resultHandler, IEnumerable<DateTime> tradeableDates)
        {
            //Save configuration of data-subscription:
            _config = config;

            _auxiliaryData = new Queue<BaseData>();

            //Save Start and End Dates:
            _periodStart = periodStart;
            _periodFinish = periodFinish;

            //Save access to securities
            _security = security;
            _isDynamicallyLoadedData = security.IsDynamicallyLoadedData;
            _isLiveMode = _feedEndpoint == DataFeedEndpoint.LiveTrading;

            // do we have factor tables?
            _hasScaleFactors = FactorFile.HasScalingFactors(config.Symbol, config.Market);

            //Save the type of data we'll be getting from the source.
            _feedEndpoint = feed;

            //Create the dynamic type-activators:
            var objectActivator = ObjectActivator.GetActivator(config.Type);

            _resultHandler = resultHandler;
            _tradeableDates = tradeableDates.GetEnumerator();
            if (objectActivator == null)
            {
                _resultHandler.ErrorMessage("Custom data type '" + config.Type.Name + "' missing parameterless constructor E.g. public " + config.Type.Name + "() { }");
                _endOfStream = true;
                return;
            }

            //Create an instance of the "Type":
            var userObj = objectActivator.Invoke(new object[] { });
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

            //Load the entire factor and symbol mapping tables into memory, we'll start with some defaults
            _factorFile = new FactorFile(config.Symbol, new List<FactorFileRow>());
            _mapFile = new MapFile(config.Symbol, new List<MapFileRow>());
            try 
            {
                if (_hasScaleFactors)
                {
                    _factorFile = FactorFile.Read(config.Symbol, config.Market);
                    _mapFile = MapFile.Read(config.Symbol, config.Market);
                }
            } 
            catch (Exception err) 
            {
                Log.Error("SubscriptionDataReader(): Fetching Price/Map Factors: " + err.Message);
           }
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
            if (_endOfStream)
            {
                return false;
            }

            _previous = Current;

            // get our reader, advancing to the next day if necessary
            var reader = ResolveReader();
            
            // if we were unable to resolve a reader it's because we're out of tradeable dates
            if (reader == null)
            {
                if (_isLiveMode)
                {
                    // HACK attack -- we don't want to block in live mode
                    Current = null;
                    return true;
                }

                _endOfStream = true;
                return false;
            }

            // loop until we find data that passes all of our filters
            do
            {
                // if we've run out of data on our current reader then let's find the next one, this will advance our tradeable dates as well
                if (reader.EndOfStream)
                {
                    reader = ResolveReader();
                    // resolve reader will return null when we're finished with tradeable dates
                    if (reader == null)
                    {
                        _endOfStream = true;
                        return false;
                    }
                }

                if (_auxiliaryData.Count > 0)
                {
                    // check for any auxilliary data before reading a line
                    Current = _auxiliaryData.Dequeue();
                    return true;
                }

                // read in a line and then parse it using the data factory
                var line = reader.ReadLine();
                BaseData instance = null;
                try
                {
                    instance = _dataFactory.Reader(_config, line, _tradeableDates.Current, _isLiveMode);
                }
                catch (Exception err)
                {
                    // TODO: this should be an event, such as OnReaderError
                    _resultHandler.RuntimeError("Error invoking " + _config.Symbol + " data reader. Line: " + line + " Error: " + err.Message, err.StackTrace);
                }

                if (instance == null)
                {
                    continue;
                }

                if (instance.EndTime < _periodStart)
                {
                    // keep readnig until we get a value on or after the start
                    _previous = instance;
                    continue;
                }

                if (instance.Time > _periodFinish)
                {
                    // stop reading when we get a value after the end
                    _endOfStream = true;
                    return false;
                }

                // we've made it past all of our filters, we're withing the requested start/end of the subscription,
                // we've satisfied user and market hour filters, so this data is good to go as current
                Current = instance;
                return true;
            }
            // keep looping, we control returning internally
            while (true);
        }

        /// <summary>
        /// Resolves a stream reader to read data from a subscription source
        /// </summary>
        /// <returns>A stream reader used to read lines from a subscription source, null if the source was unable to be resolved into a reader</returns>
        private IStreamReader ResolveReader()
        {
            // if we still have data to emit, keep using this one
            if (_reader != null && !_reader.EndOfStream)
            {
                return _reader;
            }

            // if we're out of data and non-null, clean up old resources
            if (_reader != null)
            {
                _reader.Dispose();
            }

            do
            {
                DateTime date;
                if (!TryGetNextDate(out date))
                {
                    // if don't have any more days to process then we're done
                    return null;
                }

                var newSource = _dataFactory.GetSource(_config, date, _isLiveMode);

                if (_source != newSource && newSource.Source != "" || (_isLiveMode && _source.TransportMedium == SubscriptionTransportMedium.RemoteFile))
                {
                    _source = newSource;

                    // create our stream reader from our data source
                    _reader = CreateStreamReader(_source);
                }
                else if (!(_isLiveMode && _source.TransportMedium == SubscriptionTransportMedium.RemoteFile))
                {
                    // if we didn't get a new source then we're done with this subscription
                    return null;
                }

                // if the created reader doesn't have any data, then advance to the next day to try again
                if (!(_reader != null && !_reader.EndOfStream))
                {
                    OnCreateStreamReaderError(date, _source);
                }
            }
            while (!(_reader != null && !_reader.EndOfStream));
            
            // we've finally found a reader with data!
            return _reader;
        }

        private IStreamReader CreateStreamReader(SubscriptionDataSource subscriptionDataSource)
        {
            IStreamReader reader;
            switch (subscriptionDataSource.TransportMedium)
            {
                case SubscriptionTransportMedium.LocalFile:
                    reader = HandleLocalFileSource(subscriptionDataSource.Source);
                    break;

                case SubscriptionTransportMedium.RemoteFile:
                    reader = HandleRemoteSourceFile(subscriptionDataSource.Source);
                    break;

                case SubscriptionTransportMedium.Rest:
                    reader = new RestSubscriptionStreamReader(subscriptionDataSource.Source);
                    break;

                default:
                    throw new InvalidEnumArgumentException("Unexpected SubscriptionTransportMedium specified: " + subscriptionDataSource.TransportMedium);
            }
            return reader;
        }

        /// <summary>
        /// Gets called when we fail to create a stream reader with data
        /// </summary>
        /// <param name="date">The date that caused this</param>
        /// <param name="subscriptionDataSource">The subscription source that caused this</param>
        private void OnCreateStreamReaderError(DateTime date, SubscriptionDataSource subscriptionDataSource)
        {
            // TODO: OnCreateStreamReaderError event
            Log.Error(string.Format("Failed to get StreamReader for data source({0}), symbol({1}). Skipping date({2}). Reader is null.", subscriptionDataSource.Source, _mappedSymbol, date.ToShortDateString()));
            if (_isDynamicallyLoadedData)
            {
                _resultHandler.ErrorMessage(string.Format("We could not fetch the requested data. This may not be valid data, or a failed download of custom data. Skipping source ({0}).", subscriptionDataSource));
            }
        }

        /// <summary>
        /// Iterates the tradeable dates enumerator
        /// </summary>
        /// <param name="date">The next tradeable date</param>
        /// <returns>True if we got a new date from the enumerator, false if it's exhausted, or in live mode if we're already at today</returns>
        private bool TryGetNextDate(out DateTime date)
        {
            if (_isLiveMode && _tradeableDates.Current > DateTime.Today)
            {
                // special behavior for live mode, don't advance past today
                date = _tradeableDates.Current;
                return false;
            }

            while (_tradeableDates.MoveNext())
            {
                date = _tradeableDates.Current;
                if (!_mapFile.HasData(date))
                {
                    continue;
                }

                // check for dividends and split for this security
                CheckForDividend(date);
                CheckForSplit(date);

                // if we have factor files check to see if we need to update the scale factors
                if (_hasScaleFactors)
                {
                    // check to see if the symbol was remapped
                    _mappedSymbol = _mapFile.GetMappedSymbol(date);
                    _config.MappedSymbol = _mappedSymbol;

                    // update our price scaling factors in light of the normalization mode
                    UpdateScaleFactors(date);
                }

                // if the exchange is open then we should look for data for this data
                if (_security.Exchange.DateIsOpen(date))
                {
                    return true;
                }
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
            if (_splitFactor != null)
            {
                var close = GetRawClose();
                var split = new Split(_config.Symbol, date, close, _splitFactor.Value);
                _auxiliaryData.Enqueue(split);
                _splitFactor = null;
            }

            decimal splitFactor;
            if (_factorFile.HasSplitEventOnNextTradingDay(date, out splitFactor))
            {
                _splitFactor = splitFactor;
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
                var dividend = new Dividend(_config.Symbol, date, close, _priceFactorRatio.Value);
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
        /// Un-normalizes the Previous.Value
        /// </summary>
        private decimal GetRawClose()
        {
            if (_previous == null) return 0m;

            var close = _previous.Value;

            switch (_config.DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    break;
                
                case DataNormalizationMode.SplitAdjusted:
                case DataNormalizationMode.Adjusted:
                    // we need to 'unscale' the price
                    close = close/_config.PriceScaleFactor;
                    break;
                
                case DataNormalizationMode.TotalReturn:
                    // we need to remove the dividends since we've been accumulating them in the price
                    close = (close - _config.SumOfDividends)/_config.PriceScaleFactor;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return close;
        }

        /// <summary>
        /// Dispose of the Stream Reader and close out the source stream and file connections.
        /// </summary>
        public void Dispose() 
        { 
            if (_reader != null) 
            {
                _reader.Close();
                _reader.Dispose();
            }
        }

        /// <summary>
        /// Opens up an IStreamReader for a local file source
        /// </summary>
        private IStreamReader HandleLocalFileSource(string source)
        {
            if (!File.Exists(source))
            {
                // the local uri doesn't exist, write an error and return null so we we don't try to get data for today
                Log.Trace("SubscriptionDataReader.GetReader(): Could not find QC Data, skipped: " + source);
                _resultHandler.SamplePerformance(_date.Date, 0);
                return null;
            }

            // handles zip or text files
            return new LocalFileSubscriptionStreamReader(source);
        }

        /// <summary>
        /// Opens up an IStreamReader for a remote file source
        /// </summary>
        private IStreamReader HandleRemoteSourceFile(string source)
        {
            // clean old files out of the cache
            if (!Directory.Exists(Constants.Cache)) Directory.CreateDirectory(Constants.Cache);
            foreach (var file in Directory.EnumerateFiles(Constants.Cache))
            {
                if (File.GetCreationTime(file) < DateTime.Now.AddHours(-24)) File.Delete(file);
            }

            try
            {
                // this will fire up a web client in order to download the 'source' file to the cache
                return new RemoteFileSubscriptionStreamReader(source, Constants.Cache);
            }
            catch (Exception err)
            {
                _resultHandler.ErrorMessage("Error downloading custom data source file, skipped: " + source + " Err: " + err.Message, err.StackTrace);
                _resultHandler.SamplePerformance(_date.Date, 0);
                return null;
            }
        }
    }
}