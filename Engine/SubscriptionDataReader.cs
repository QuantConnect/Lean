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

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Fasterflect;
using QuantConnect.Securities;
using QuantConnect.Logging;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine
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
        private string _source = "";

        ///Default true to fillforward for this subscription, take the previous result and continue returning it till the next time barrier.
        private bool _isFillForward = true;

        ///Date of this source file.
        private DateTime _date = new DateTime();

        ///End of stream from the reader
        private bool _endOfStream = false;

        /// Internal stream reader for processing data line by line:
        private SubscriptionStreamReader _reader = null;

        /// All streams done async via web protocols:
        private WebClient _web = new WebClient();

        /// Configuration of the data-reader:
        private SubscriptionDataConfig _config;

        /// Subscription Securities Access
        private Security _security;

        /// Save security type of speed
        private bool _isQCEquity = false;

        // Subscription is for a QC type:
        private bool _isQCData = false;

        //Price Factor Mapping:
        private SortedDictionary<DateTime, decimal> _priceFactors;
        private decimal _priceFactor = 0;

        //Symbol Mapping:
        private SortedDictionary<DateTime, string> _symbolMap;
        private string _mappedSymbol = "";

        /// Location of the datafeed - the type of this data.
        private DataFeedEndpoint _feedEndpoint = DataFeedEndpoint.Backtesting;

        /// Object Activator - Fast create new instance of "Type":
        private Func<object[], object> _objectActivator;

        ///Create a single instance to invoke all Type Methods:
        private BaseData _dataFactory;

        /// Access to Reader Method:
        private MethodInfo _readerMethod;

        ///FastReflect Method Invoker
        private MethodInvoker _readerMethodInvoker;

        /// Access to Get Source Method:
        private MethodInfo _getSourceMethod;

        /// Remember edge conditions as market enters/leaves open-closed.
        private BaseData _lastBarOfStream = null;
        private BaseData _lastBarOutsideMarketHours = null;

        //Start finish times of the backtest:
        private DateTime _periodStart;
        private DateTime _periodFinish;

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
            get { throw new NotImplementedException(); }
        }


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
        }

        /// <summary>
        /// Simple flag to show if this is a QC data type
        /// </summary>
        public bool IsQCTick
        {
            get;
            private set;
        }

        /// <summary>
        /// Simple flag to show if this is a QC data type
        /// </summary>
        public bool IsQCTradeBar
        {
            get;
            private set;
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

            //Save access to fill foward flag:
            _isFillForward = config.FillDataForward;

            //Save Start and End Dates:
            _periodStart = periodStart;
            _periodFinish = periodFinish;

            //Save access to securities
            _security = security;
            _isQCData = security.IsQuantConnectData;
            _isQCEquity = (security.Type == SecurityType.Equity) && _isQCData;
            
            //Set QC Type Flags:
            IsQCTick = (config.Type.Name == "Tick" && _isQCData);
            IsQCTradeBar = (config.Type.Name == "TradeBar" && _isQCData);

            //Save the type of data we'll be getting from the source.
            _feedEndpoint = feed;

            //Create the dynamic type-activators:
            _objectActivator = Loader.GetActivator(config.Type);

            if (_objectActivator == null) 
            {
                Engine.ResultHandler.ErrorMessage("Custom data type '" + config.Type.Name + "' missing parameterless constructor E.g. public " + config.Type.Name + "() { }");
                _endOfStream = true;
                return;
            }

            //Create an instance of the "Type":
            var userObj = _objectActivator.Invoke(new object[] { });
            _dataFactory = userObj as BaseData;

            //Save Access to the "Reader" Method:
            _readerMethod = _dataFactory.GetType().GetMethod("Reader", new[] { typeof(SubscriptionDataConfig), typeof(string), typeof(DateTime), typeof(DataFeedEndpoint) });

            //Create a Delagate Accessor.
            _readerMethodInvoker = _readerMethod.DelegateForCallMethod();

            //Save access to the "GetSource" Method:
            _getSourceMethod = _dataFactory.GetType().GetMethod("GetSource", new[] { typeof(SubscriptionDataConfig), typeof(DateTime), typeof(DataFeedEndpoint) });

            //Load the entire factor and symbol mapping tables into memory
            try 
            {
                if (_isQCEquity)
                {
                    _priceFactors = SubscriptionAdjustment.GetFactorTable(config.Symbol);
                    _symbolMap = SubscriptionAdjustment.GetMapTable(config.Symbol);
                }
            } 
            catch (Exception err) 
            {
                Log.Error("SubscriptionDataReader(): Fetching Price/Map Factors: " + err.Message);
                _priceFactors = new SortedDictionary<DateTime, decimal>();
                _symbolMap = new SortedDictionary<DateTime, string>();
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

            BaseData instance = null;
            var instanceMarketOpen = false;
            //Log.Debug("SubscriptionDataReader.MoveNext(): Starting MoveNext...");

            //Calls this when no file, first "moveNext()" in refresh source.
            if (_endOfStream || _reader == null || _reader.EndOfStream)
            {
                if (_reader == null) 
                {
                    //Handle the 1% of time:: getReader failed (e.g. missing day on S3) so skip day:
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
            while (instance == null  && !_reader.EndOfStream) 
            {
                //Get the next string line from file, create instance of BaseData:
                var line = _reader.ReadLine();
                try 
                {
                    //Using Fasterflex method invokers.
                    instance = _readerMethodInvoker(_dataFactory, _config, line, _date, _feedEndpoint) as BaseData;
                } 
                catch (Exception err) 
                {
                    //Log.Debug("SubscriptionDataReader.MoveNext(): Error invoking instance: " + err.Message);
                    Engine.ResultHandler.RuntimeError("Error invoking " + _config.Symbol + " data reader. Line: " + line + " Error: " + err.Message, err.StackTrace);
                    _endOfStream = true;
                }

                if (instance != null)
                {
                    instanceMarketOpen = _security.Exchange.DateTimeIsOpen(instance.Time);

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
                        Log.Trace("SubscriptionDataReader.MoveNext(): Instance null, continuing...");
                        continue;
                    }
                        

                    //Check if we're in date range of the data request
                    if (!_isQCData) 
                    {
                        if (instance.Time < _periodStart) 
                        {
                            _lastBarOutsideMarketHours = instance;
                            instance = null;
                            continue;
                        }
                        if (instance.Time > _periodFinish) 
                        {
                            instance = null;
                            continue;
                        }
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
                    Current = Previous.Clone();
                    //When market closes fastforward current bar to the last bar fill forwarded to close time.
                    Current.Time = _security.Exchange.TimeOfDayClosed(Previous.Time);
                    // Save the previous bar as last bar before next stream (for fill forwrd).
                    _lastBarOfStream = Previous;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// For backwards adjusted data the price is adjusted by a scale factor which is a combination of splits and dividends. 
        /// This backwards adjusted price is used by default and fed as the current price.
        /// </summary>
        /// <param name="date">Current date of the backtest.</param>
        private void UpdateScaleFactors(DateTime date) {
            try
            {
                _mappedSymbol = SubscriptionAdjustment.GetMappedSymbol(_symbolMap, date);
                _priceFactor = SubscriptionAdjustment.GetTimePriceFactor(_priceFactors, date);
            } 
            catch (Exception err) 
            {
                Log.Error("SubscriptionDataReader.UpdateScaleFactors(): " + err.Message);
            }
            _config.SetPriceScaleFactor(_priceFactor);
            _config.SetMappedSymbol(_mappedSymbol);
        }

        /// <summary>
        /// Check if this time is open for this subscription.
        /// </summary>
        /// <param name="time">Date and time we're checking to see if the market is open</param>
        /// <returns>Boolean true on market open</returns>
        public bool MarketOpen(DateTime time) 
        {
            return _security.Exchange.DateTimeIsOpen(time);
        }


        /// <summary>
        /// Check if we're still in the extended market hours
        /// </summary>
        /// <param name="time">Time to scan</param>
        /// <returns>True on extended market hours</returns>
        public bool ExtendedMarketOpen(DateTime time) 
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
            var newSource = "";

            //If this is an equity, apply the backward price scaling (backtesting only).
            if (_isQCEquity && (_feedEndpoint == DataFeedEndpoint.Backtesting || _feedEndpoint == DataFeedEndpoint.FileSystem)) {
                UpdateScaleFactors(date);
            }

            //Make sure this particular security is trading today:
            if (!_security.Exchange.DateIsOpen(date))
            {
                _endOfStream = true;
                return false;
            }

            //Choose the new source file, hide the QC source file locations
            if (_isQCData)
            {
                newSource = GetQuantConnectSource(date);
            }
            else
            {
                //If its not a QC bar, load the source from the user function.
                newSource = GetSource(date);
            }

            //When stream over stop looping on this data.
            if (newSource == "") 
            {
                _endOfStream = true;
                return false;
            }

            //Log.Debug("SubscriptionDataReader.MoveNext(): Source Refresh: " + newSource);
            if (_source != newSource && newSource != "")
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
                    _reader = GetReader(_source, (_isQCData && !Engine.IsLocal));
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

                    if (!_isQCData)
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
        /// Using this source URL, download it to our cache and open a local reader.
        /// </summary>
        /// <param name="source">Source URL for the data:</param>
        /// <param name="qcFile">Boolean if this is a QC managed data file, if so can look in QC store</param>
        /// <returns>StreamReader for the data source</returns>
        private SubscriptionStreamReader GetReader(string source, bool qcFile = false)
        { 
            //Prepare local folders:
            const string cache = "./cache/data";
            SubscriptionStreamReader reader = null;
            if (!Directory.Exists(cache)) Directory.CreateDirectory(cache);
            foreach (var file in Directory.EnumerateFiles(cache)) File.Delete(file);

            //1. Download this source file as fast as possible:
            //1.1 Create filename from source:
            var filename = source.ToMD5() + source.GetExtension();
            var location = cache + @"/" + filename;

            //1.2 Based on Endpoint, Download File (Backtest) or directly open SR of source:
            switch (_feedEndpoint)
            { 
                case DataFeedEndpoint.FileSystem:
                case DataFeedEndpoint.Backtesting:
                    //1.2 Download from source to location 
                    if (qcFile)
                    {
                        if (!File.Exists(source))
                        {
                            Log.Trace("SubscriptionDataReader.GetReader(): Could not find QC Data, skipped: " + source);
                            Engine.ResultHandler.SamplePerformance(_date.Date, 0);
                            return reader;
                        }
                        //Data source is also location of raw file; root /data directory.
                        location = source;
                    }
                    else
                    {
                        //If the file already exists a
                        try
                        {
                            using (var client = new WebClient())
                            {
                                client.DownloadFile(source, location);
                            }
                        }
                        catch (Exception err)
                        {
                            Engine.ResultHandler.ErrorMessage("Error downloading custom data source file, skipped: " + source + " Err: " + err.Message, err.StackTrace);
                            if (OS.IsWindows) Engine.ResultHandler.SamplePerformance(_date.Date, 0);
                            return reader;
                        }
                    }

                    //2. File downloaded. Open Stream:
                    if (File.Exists(location))
                    {
                        if (source.GetExtension() == ".zip")
                        {
                            //Extracting zip returns stream reader:
                            var sr = Compression.Unzip(location);
                            if (sr == null) return null;
                            reader = new SubscriptionStreamReader(sr, _feedEndpoint);
                        }
                        else
                        {
                            //Custom file stream: open from disk
                            reader = new SubscriptionStreamReader(location, _feedEndpoint);
                        }
                    }
                    break;

                //Directly open for REST Requests:
                case DataFeedEndpoint.Tradier:
                case DataFeedEndpoint.LiveTrading:
                    reader = new SubscriptionStreamReader(source, _feedEndpoint);
                    break;
            }

            return reader;
        }


        /// <summary>
        /// Stream the file over the net directly from its source. 
        /// </summary>
        /// <param name="source">Source URL for the file</param>
        /// <remarks>Left here for potential future reference instead of downloading files we stream then from external source.</remarks>
        /// <returns>StreamReader Interface for the data source.</returns>
        private StreamReader WebReader(string source) 
        {    
            //Initialize Required Variables for Web Reader:
            StreamReader reader;

            //Reopen the source with the new URL.
            _web = new WebClient();
            using (var stream = _web.OpenRead(source)) 
            {
                //If its a zip, unzip it:
                if (source.GetExtension() == ".zip")
                {
                    reader = Compression.UnzipStream(stream);
                }
                else
                {
                    reader = new StreamReader(stream);
                }
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
        public string GetSource(DateTime date)
        {
            var newSource = "";
            //Invoke our instance of this method.
            if (_dataFactory != null) 
            {
                try
                {
                    newSource = _getSourceMethod.Invoke(_dataFactory, new object[] { _config, date, _feedEndpoint }) as String;
                }
                catch (Exception err) 
                {
                    Log.Error("SubscriptionDataReader.GetSource(): " + err.Message);
                    Engine.ResultHandler.ErrorMessage("Error getting string source location for custom data source: " + err.Message, err.StackTrace);
                }
            }
            //Return the freshly calculated source URL.
            return newSource;
        }


        /// <summary>
        /// Get the source location of this QuantConnect data request.
        /// </summary>
        /// <param name="date">Date to retrieve</param>
        /// <returns>string source</returns>
        public string GetQuantConnectSource(DateTime date)
        {
            var source = "";
            var dataType = TickType.Trade;

            switch (_feedEndpoint)
            { 
                //BAcktesting S3 Endpoint:
                case DataFeedEndpoint.Backtesting:
                case DataFeedEndpoint.FileSystem:

                    var dateFormat = "yyyyMMdd";
                    if (_config.Security == SecurityType.Forex)
                    {
                        dataType = TickType.Quote;
                        dateFormat = "yyMMdd";
                    }

                    source =  @"../../../Data/" + _config.Security.ToString().ToLower();
                    source += @"/" + _config.Resolution.ToString().ToLower() + @"/" + _config.Symbol.ToLower() + @"/";
                    source += date.ToString(dateFormat) + "_" + dataType.ToString().ToLower() + ".zip";
                    break;

                //Live Trading Endpoint: Fake, not actually used but need for consistency with backtesting system. Set to "" so will not use subscription reader.
                case DataFeedEndpoint.Tradier:
                case DataFeedEndpoint.LiveTrading:
                    source = "";
                    break;
            }
            return source;
        }

    } // End Base Data Class

} // End QC Namespace
