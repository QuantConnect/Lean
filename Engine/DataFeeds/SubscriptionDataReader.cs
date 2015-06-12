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
using System.Net;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Subscription data reader is a wrapper on the stream reader class to download, unpack and iterate over a data file.
    /// </summary>
    /// <remarks>The class accepts any subscription configuration and automatically makes it availble to enumerate</remarks>
    public class SubscriptionDataReader : IEnumerator<BaseData>
    {
        /******************************************************** 
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        /// Source string to create memory stream:
        private SubscriptionDataSource _source;

        ///Default true to fillforward for this subscription, take the previous result and continue returning it till the next time barrier.
        private bool _isFillForward = true;

        ///Date of this source file.
        private DateTime _date = new DateTime();

        ///End of stream from the reader
        private bool _endOfStream = false;

        /// Internal stream reader for processing data line by line:
        private IStreamReader _reader = null;

        /// All streams done async via web protocols:
        private WebClient _web = new WebClient();

        /// Configuration of the data-reader:
        private SubscriptionDataConfig _config;

        /// Subscription Securities Access
        private Security _security;

        /// true if we can find a scale factor file for the security of the form: ..\Lean\Data\equity\factor_files\{SYMBOL}.csv
        private bool _hasScaleFactors = false;

        // Subscription is for a QC type:
        private bool _isDynamicallyLoadedData = false;

        //Symbol Mapping:
        private string _mappedSymbol = "";

        /// Location of the datafeed - the type of this data.
        private readonly DataFeedEndpoint _feedEndpoint;

        /// Object Activator - Fast create new instance of "Type":
        private readonly Func<object[], object> _objectActivator;

        ///Create a single instance to invoke all Type Methods:
        private readonly BaseData _dataFactory;

        /// Remember edge conditions as market enters/leaves open-closed.
        private BaseData _lastBarOfStream = null;
        private BaseData _lastBarOutsideMarketHours = null;

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

        /******************************************************** 
        * CLASS PUBLIC VARIABLES
        *********************************************************/
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
        /// Provides a means of exposing extra data related to this subscription.
        /// For now we expose dividend data for equities through here
        /// </summary>
        /// <remarks>
        /// It is currently assumed that whomever is pumping data into here is handling the
        /// time syncing issues. Dividends do this through the RefreshSource method
        /// </remarks>
        public Queue<BaseData> AuxiliaryData { get; private set; }

        /// <summary>
        /// Save an instance of the previous basedata we generated
        /// </summary>
        public BaseData Previous
        {
            get;
            private set;
        }

        /// <summary>
        /// Source has been completed, load up next stream or stop asking for data.
        /// </summary>
        public bool EndOfStream
        {
            get 
            {
                return _endOfStream || _reader == null;
            }
            set
            {
                _endOfStream = value;
            }
        }

        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Subscription data reader takes a subscription request, loads the type, accepts the data source and enumerate on the results.
        /// </summary>
        /// <param name="config">Subscription configuration object</param>
        /// <param name="security">Security asset</param>
        /// <param name="feed">Feed type enum</param>
        /// <param name="periodStart">Start date for the data request/backtest</param>
        /// <param name="periodFinish">Finish date for the data request/backtest</param>
        public SubscriptionDataReader(SubscriptionDataConfig config, Security security, DataFeedEndpoint feed, DateTime periodStart, DateTime periodFinish)
        {
            //Save configuration of data-subscription:
            _config = config;

            AuxiliaryData = new Queue<BaseData>();

            //Save access to fill foward flag:
            _isFillForward = config.FillDataForward;

            //Save Start and End Dates:
            _periodStart = periodStart;
            _periodFinish = periodFinish;

            //Save access to securities
            _security = security;
            _isDynamicallyLoadedData = security.IsDynamicallyLoadedData;
            _isLiveMode = _feedEndpoint == DataFeedEndpoint.LiveTrading;

            // do we have factor tables?
            _hasScaleFactors = FactorFile.HasScalingFactors(config.Symbol);

            //Save the type of data we'll be getting from the source.
            _feedEndpoint = feed;

            //Create the dynamic type-activators:
            _objectActivator = ObjectActivator.GetActivator(config.Type);

            if (_objectActivator == null) 
            {
                Engine.ResultHandler.ErrorMessage("Custom data type '" + config.Type.Name + "' missing parameterless constructor E.g. public " + config.Type.Name + "() { }");
                _endOfStream = true;
                return;
            }

            //Create an instance of the "Type":
            var userObj = _objectActivator.Invoke(new object[] { });
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
                    _factorFile = FactorFile.Read(config.Symbol);
                    _mapFile = MapFile.Read(config.Symbol);
                }
            } 
            catch (Exception err) 
            {
                Log.Error("SubscriptionDataReader(): Fetching Price/Map Factors: " + err.Message);
           }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Try and create a new instance of the object and return it using the MoveNext enumeration pattern ("Current" public variable).
        /// </summary>
        /// <remarks>This is a highly called method and should be kept lean as possible.</remarks>
        /// <returns>Boolean true on successful move next. Set Current public property.</returns>
        public bool MoveNext() {

            // yield the aux data first
            if (AuxiliaryData.Count != 0)
            {
                Previous = Current;
                Current = AuxiliaryData.Dequeue();
                return true;
            }

            BaseData instance = null;
            var instanceMarketOpen = false;
            //Log.Debug("SubscriptionDataReader.MoveNext(): Starting MoveNext...");

            try
            {
                //Calls this when no file, first "moveNext()" in refresh source.
                if (_endOfStream || _reader == null || _reader.EndOfStream)
                {
                    if (_reader == null)
                    {
                        //Handle the 1% of time:: getReader failed e.g. missing day so skip day:
                        Current = null;
                    }
                    else
                    {
                        //This is a MoveNext() after reading the last line of file:
                        _lastBarOfStream = Current;
                    }
                    _endOfStream = true;
                    return false;
                }

                //Log.Debug("SubscriptionDataReader.MoveNext(): Launching While-InstanceNotNull && not EOS: " + reader.EndOfStream);
                //Keep looking until output's an instance:
                while (instance == null && !_reader.EndOfStream)
                {
                    //Get the next string line from file, create instance of BaseData:
                    var line = _reader.ReadLine();
                    try
                    {
                        instance = _dataFactory.Reader(_config, line, _date, _isLiveMode);
                    }
                    catch (Exception err)
                    {
                        //Log.Debug("SubscriptionDataReader.MoveNext(): Error invoking instance: " + err.Message);
                        Engine.ResultHandler.RuntimeError("Error invoking " + _config.Symbol + " data reader. Line: " + line + " Error: " + err.Message, err.StackTrace);
                        _endOfStream = true;
                        continue;
                    }

                    if (instance != null)
                    {
                        // we care if the market was open at any time over the bar
                        instanceMarketOpen = Exchange.IsOpenDuringBar(instance.Time, instance.EndTime, false);
                        
                        //Apply custom user data filters:
                        try
                        {
                            if (!_security.DataFilter.Filter(_security, instance))
                            {
                                instance = null;
                                continue;
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Error("SubscriptionDataReader.MoveNext(): Error applying filter: " + err.Message);
                            Engine.ResultHandler.RuntimeError("Runtime error applying data filter. Assuming filter pass: " + err.Message, err.StackTrace);
                        }

                        if (instance == null)
                        {
                            // REVIEW -- Is this condition heuristically possible?
                            Log.Trace("SubscriptionDataReader.MoveNext(): Instance null, continuing...");
                            continue;
                        }


                        //Check if we're in date range of the data request
                        if (instance.Time < _periodStart)
                        {
                            _lastBarOutsideMarketHours = instance;
                            instance = null;
                            continue;
                        }
                        if (instance.Time > _periodFinish)
                        {
                            // we're done with data from this subscription, finalize the reader
                            Current = null;
                            _endOfStream = true;
                            return false;
                        }

                        //Save bar for extended market hours (fill forward).
                        if (!instanceMarketOpen)
                        {
                            _lastBarOutsideMarketHours = instance;
                        }

                        //However, if we only want market hours data, don't return yet: Discard and continue looping.
                        if (!_config.ExtendedMarketHours && !instanceMarketOpen)
                        {
                            instance = null;
                        }
                    }
                }

                //Handle edge conditions: First Bar Read: 
                // -> Use previous bar from yesterday if available
                if (Current == null)
                {
                    //Handle first loop where not set yet:
                    if (_lastBarOfStream == null)
                    {
                        //For first bar, fill forward from premarket data where possible
                        _lastBarOfStream = _lastBarOutsideMarketHours ?? instance;
                    }
                    //If current not set yet, set Previous to yesterday/last bar read. 
                    Previous = _lastBarOfStream;
                }
                else
                {
                    Previous = Current;
                }

                Current = instance;

                //End of Stream: rewind reader to last
                if (_reader.EndOfStream && instance == null)
                {
                    //Log.Debug("SubscriptionDataReader.MoveNext(): Reader EOS.");
                    _endOfStream = true;

                    if (_isFillForward && Previous != null)
                    {
                        //If instance == null, current is null, so clone previous to record the final sample:
                        Current = Previous.Clone(true);
                        //When market closes fastforward current bar to the last bar fill forwarded to close time.
                        Current.Time = _security.Exchange.TimeOfDayClosed(Previous.Time);
                        // Save the previous bar as last bar before next stream (for fill forwrd).
                        _lastBarOfStream = Previous;
                    }
                    return false;
                }
                return true;
            }
            catch (Exception err)
            {
                Log.Error("SubscriptionDataReader.MoveNext(): " + err.Message);
                return false;
            }
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
                case DataNormalizationMode.TotalReturn:
                    return;
                
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
        /// Check if this time is open for this subscription.
        /// </summary>
        /// <param name="time">Date and time we're checking to see if the market is open</param>
        /// <returns>Boolean true on market open</returns>
        public bool IsMarketOpen(DateTime time) 
        {
            return _security.Exchange.DateTimeIsOpen(time);
        }

        /// <summary>
        /// Gets the associated exchange for this data reader/security
        /// </summary>
        public SecurityExchange Exchange
        {
            get { return _security.Exchange; }
        }

        /// <summary>
        /// Check if we're still in the extended market hours
        /// </summary>
        /// <param name="time">Time to scan</param>
        /// <returns>True on extended market hours</returns>
        public bool IsExtendedMarketOpen(DateTime time) 
        {
            return _security.Exchange.DateTimeIsExtendedOpen(time);
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
        /// Fetch and set the location of the data from the user's BaseData factory:
        /// </summary>
        /// <param name="date">Date of the source file.</param>
        /// <returns>Boolean true on successfully retrieving the data</returns>
        public bool RefreshSource(DateTime date)
        {
            //Update the source from the getSource method:
            _date = date;

            // if the map file is an empty instance this will always return true
            if (!_mapFile.HasData(date))
            {
                // don't even bother checking the disk if the map files state we don't have ze dataz
                return false;
            }

            // check for dividends and split for this security
            CheckForDividend(date);
            CheckForSplit(date);


            //If we can find scale factor files on disk, use them. LiveTrading will aways use 1 by definition
            if (_hasScaleFactors)
            {
                // check to see if the symbol was remapped
                _mappedSymbol = _mapFile.GetMappedSymbol(date);
                _config.MappedSymbol = _mappedSymbol;

                // update our price scaling factors in light of the normalization mode
                UpdateScaleFactors(date);
            }

            //Make sure this particular security is trading today:
            if (!_security.Exchange.DateIsOpen(date))
            {
                _endOfStream = true;
                return false;
            }

            //Choose the new source file, hide the QC source file locations, if we're returned null new up a default instance
            var newSource = GetSource(date) ?? new SubscriptionDataSource("", SubscriptionTransportMedium.LocalFile);

            //When stream over stop looping on this data.
            if (newSource.Source == "") 
            {
                _endOfStream = true;
                return false;
            }

            // if the source has changed refresh it, in some cases the source string may
            // not actually change, such is the case with a live remote file, but we still want
            // to re-fetch it, so make a special case for live remote file types, we'll assume
            // local files aren't changing, instead they should point to a new file
            if (_source != newSource && newSource.Source != "" || (_isLiveMode && _source.TransportMedium == SubscriptionTransportMedium.RemoteFile))
            {
                //If a new file, reset the EOS flag:
                _endOfStream = false;
                //Set the new source.
                _source = newSource;
                //Close out the last source file.
                Dispose();

                //Load the source:
                try 
                {
                    //Log.Debug("SubscriptionDataReader.RefreshSource(): Created new reader for source: " + source);
                    _reader = GetReader(_source);
                } 
                catch (Exception err) 
                {
                    Log.Error("SubscriptionDataReader.RefreshSource(): Failed to get reader: " + err.Message);
                    //Engine.ResultHandler.DebugMessage("Failed to get a reader for the data source. There may be an error in your custom data source reader. Skipping date (" + date.ToShortDateString() + "). Err: " + err.Message);
                    return false;
                }

                if (_reader == null)
                {
                    Log.Error("Failed to get StreamReader for data source(" + _source + "), symbol(" + _mappedSymbol + "). Skipping date(" + date.ToShortDateString() + "). Reader is null.");
                    //Engine.ResultHandler.DebugMessage("We could not find the requested data. This may be an invalid data request, failed download of custom data, or a public holiday. Skipping date (" + date.ToShortDateString() + ").");
                    if (_isDynamicallyLoadedData)
                    {
                        Engine.ResultHandler.ErrorMessage("We could not fetch the requested data. This may not be valid data, or a failed download of custom data. Skipping source (" + _source + ").");
                    }
                    return false;
                }

                //Reset the public properties so we can explicitly set them with lastBar data.
                Current = null;
                Previous = null;

                //99% of time, populate the first "Current". 1% of of time no source file (getReader fails), so
                // method sets the Subscription properties as if no data.
                try
                {
                    MoveNext();
                }
                catch (Exception err) 
                {
                    throw new Exception("SubscriptionDataReader.RefreshSource(): Could not MoveNext to init stream: " + _source + " " + err.Message + " >> " + err.StackTrace);
                }
            }

            //Success:
            return true;
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
                AuxiliaryData.Enqueue(split);
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
                AuxiliaryData.Enqueue(dividend);
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
            if (Previous == null) return 0m;

            var close = Previous.Value;

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
                    close -= _config.SumOfDividends;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return close;
        }


        /// <summary>
        /// Using this source URL, download it to our cache and open a local reader.
        /// </summary>
        /// <param name="source">Source URL for the data:</param>
        /// <returns>StreamReader for the data source</returns>
        private IStreamReader GetReader(SubscriptionDataSource source)
        {
            IStreamReader reader;
            switch (source.TransportMedium)
            {
                case SubscriptionTransportMedium.LocalFile:
                    reader = HandleLocalFileSource(source.Source);
                    break;

                case SubscriptionTransportMedium.RemoteFile:
                    reader = HandleRemoteSourceFile(source.Source);
                    break;

                case SubscriptionTransportMedium.Rest:
                    reader = new RestSubscriptionStreamReader(source.Source);
                    break;

                default:
                    throw new InvalidEnumArgumentException("Unexpected SubscriptionTransportMedium specified: " + source.TransportMedium);
            }

            // if the reader is already at end of stream, just set to null so we don't try to get data for today
            // this provides a fail fast mechanism so we don't need to go into MoveNext via RefreshSource
            if (reader != null && reader.EndOfStream)
            {
                reader = null;
            }

            return reader;
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

            if (_web != null) 
            {
                _web.Dispose();
            }
        }

        /// <summary>
        /// Get the source URL string for this datetime from the users GetSource() method in BaseData.
        /// </summary>
        /// <param name="date">DateTime we're requesting.</param>
        /// <returns>URL string of the source file</returns>
        public SubscriptionDataSource GetSource(DateTime date)
        {
            //Invoke our instance of this method.
            if (_dataFactory != null) 
            {
                try
                {
                    return _dataFactory.GetSource(_config, date, _isLiveMode);
                }
                catch (Exception err) 
                {
                    Log.Error("SubscriptionDataReader.GetSource(): " + err.Message);
                    Engine.ResultHandler.ErrorMessage("Error getting string source location for custom data source: " + err.Message, err.StackTrace);
                }
            }

            // return a default instance with an empty string source, this is an indication of failure
            return new SubscriptionDataSource("", SubscriptionTransportMedium.LocalFile);
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
                Engine.ResultHandler.SamplePerformance(_date.Date, 0);
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
                Engine.ResultHandler.ErrorMessage("Error downloading custom data source file, skipped: " + source + " Err: " + err.Message, err.StackTrace);
                Engine.ResultHandler.SamplePerformance(_date.Date, 0);
                return null;
            }
        }
    }
}