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
using System.Linq;
using System.Threading;
using com.fxcm.external.api.transport;
using com.fxcm.external.api.transport.listeners;
using com.fxcm.fix;
using com.fxcm.fix.pretrade;
using com.fxcm.messaging;
using java.util;
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using TimeZone = java.util.TimeZone;

namespace QuantConnect.ToolBox.FxcmDownloader
{
    /// <summary>
    /// FXCM Data Downloader class
    /// </summary>
    public class FxcmDataDownloader : IDataDownloader, IGenericMessageListener, IStatusMessageListener
    {
        private readonly FxcmSymbolMapper _symbolMapper = new FxcmSymbolMapper();
        private readonly string _server;
        private readonly string _terminal;
        private readonly string _userName;
        private readonly string _password;

        private IGateway _gateway;
        private readonly object _locker = new object();
        private string _currentRequest;
        private const int ResponseTimeout = 2500;
        private readonly Dictionary<string, AutoResetEvent> _mapRequestsToAutoResetEvents = new Dictionary<string, AutoResetEvent>();
        private readonly Dictionary<string, TradingSecurity> _fxcmInstruments = new Dictionary<string, TradingSecurity>();
        private readonly List<TradeBar> _currentBars = new List<TradeBar>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FxcmDataDownloader"/> class
        /// </summary>
        public FxcmDataDownloader(string server, string terminal, string userName, string password)
        {
            _server = server;
            _terminal = terminal;
            _userName = userName;
            _password = password;
        }

        /// <summary>
        /// Converts a Java Date value to a UTC DateTime value
        /// </summary>
        /// <param name="javaDate">The Java date</param>
        /// <returns>A UTC DateTime value</returns>
        private static DateTime FromJavaDateUtc(Date javaDate)
        {
            var cal = Calendar.getInstance();
            cal.setTimeZone(TimeZone.getTimeZone("UTC"));
            cal.setTime(javaDate);

            // note that the Month component of java.util.Date  
            // from 0-11 (i.e. Jan == 0)
            return new DateTime(cal.get(Calendar.YEAR),
                                cal.get(Calendar.MONTH) + 1,
                                cal.get(Calendar.DAY_OF_MONTH),
                                cal.get(Calendar.HOUR_OF_DAY),
                                cal.get(Calendar.MINUTE),
                                cal.get(Calendar.SECOND),
                                cal.get(Calendar.MILLISECOND));
        }

        /// <summary>
        /// Converts a Java Date value to a UTC DateTime value
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime value</param>
        /// <returns>A UTC Java Date value</returns>
        private static Date ToJavaDateUtc(DateTime utcDateTime)
        {
            var cal = Calendar.getInstance();
            cal.setTimeZone(TimeZone.getTimeZone("UTC"));

            cal.set(Calendar.YEAR, utcDateTime.Year);
            cal.set(Calendar.MONTH, utcDateTime.Month - 1);
            cal.set(Calendar.DAY_OF_MONTH, utcDateTime.Day);
            cal.set(Calendar.HOUR_OF_DAY, utcDateTime.Hour);
            cal.set(Calendar.MINUTE, utcDateTime.Minute);
            cal.set(Calendar.SECOND, utcDateTime.Second);
            cal.set(Calendar.MILLISECOND, utcDateTime.Millisecond);

            return cal.getTime();
        }

        /// <summary>
        /// Checks if downloader can get the data for the Lean symbol
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>Returns true if the symbol is available</returns>
        public bool HasSymbol(string symbol)
        {
            return _symbolMapper.IsKnownLeanSymbol(Symbol.Create(symbol, GetSecurityType(symbol), Market.FXCM));
        }

        /// <summary>
        /// Gets the security type for the specified Lean symbol
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetSecurityType(string symbol)
        {
            return _symbolMapper.GetLeanSecurityType(symbol);
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (!_symbolMapper.IsKnownLeanSymbol(symbol))
                throw new ArgumentException("Invalid symbol requested: " + symbol.Value);

            if (resolution == Resolution.Tick)
                throw new NotSupportedException("Resolution not available: " + resolution);

            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd)
                throw new NotSupportedException("SecurityType not available: " + symbol.ID.SecurityType);

            if (endUtc < startUtc)
                throw new ArgumentException("The end date must be greater or equal than the start date.");

            Console.WriteLine("Logging in...");

            // create the gateway
            _gateway = GatewayFactory.createGateway();

            // register the message listeners with the gateway
            _gateway.registerGenericMessageListener(this);
            _gateway.registerStatusMessageListener(this);

            // create local login properties
            var loginProperties = new FXCMLoginProperties(_userName, _password, _terminal, _server);

            // log in
            _gateway.login(loginProperties);

            // initialize session
            RequestTradingSessionStatus();

            Console.WriteLine("Downloading data from {0} to {1}...", startUtc.ToShortDateString(), endUtc.ToShortDateString());

            // download bars
            var totalBars = new List<TradeBar>();

            // calculate the maximum time span for one request (using 10-second bars)
            const int maxBarsPerRequest = 300;
            var timeSpanPerRequest = TimeSpan.FromSeconds(maxBarsPerRequest * 10);

            var start = startUtc;
            var end = startUtc + timeSpanPerRequest;

            // request loop
            while (start < endUtc.AddDays(1))
            {
                _currentBars.Clear();

                var mdr = new MarketDataRequest();
                mdr.setSubscriptionRequestType(SubscriptionRequestTypeFactory.SNAPSHOT);
                mdr.setResponseFormat(IFixMsgTypeDefs.__Fields.MSGTYPE_FXCMRESPONSE);
                mdr.setFXCMTimingInterval(FXCMTimingIntervalFactory.SEC10);
                mdr.setMDEntryTypeSet(MarketDataRequest.MDENTRYTYPESET_ALL);

                mdr.setFXCMStartDate(new UTCDate(ToJavaDateUtc(start)));
                mdr.setFXCMStartTime(new UTCTimeOnly(ToJavaDateUtc(start)));
                mdr.setFXCMEndDate(new UTCDate(ToJavaDateUtc(end)));
                mdr.setFXCMEndTime(new UTCTimeOnly(ToJavaDateUtc(end)));
                mdr.addRelatedSymbol(_fxcmInstruments[_symbolMapper.GetBrokerageSymbol(symbol)]);

                AutoResetEvent autoResetEvent;
                lock (_locker)
                {
                    _currentRequest = _gateway.sendMessage(mdr);
                    autoResetEvent = new AutoResetEvent(false);
                    _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
                }
                if (!autoResetEvent.WaitOne(1000))
                {
                    // no response, continue loop
                    start = end.AddSeconds(10);
                    // if saturday, fast-forward to sunday
                    if (start.DayOfWeek == DayOfWeek.Saturday) start = start.AddDays(1);
                    end = start + timeSpanPerRequest;
                    continue;
                }

                var lastBarTime = _currentBars[_currentBars.Count - 1].Time;
                if (lastBarTime < start)
                {
                    // no more data available, exit loop
                    break;
                }

                // add bars received
                totalBars.AddRange(_currentBars.Where(x => x.Time.Date <= endUtc.Date));

                // calculate time span for next request
                start = lastBarTime.AddSeconds(10);
                end = start + timeSpanPerRequest;

                if (start >= DateTime.UtcNow)
                {
                    // data in the future not available, exit loop
                    break;
                }
            }

            Console.WriteLine("Logging out...");

            // log out
            _gateway.logout();

            // remove the message listeners
            _gateway.removeGenericMessageListener(this);
            _gateway.removeStatusMessageListener(this);

            switch (resolution)
            {
                case Resolution.Second:
                    foreach (var bar in totalBars) 
                        yield return bar;
                    break;

                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    foreach (var bar in AggregateBars(symbol, totalBars, resolution.ToTimeSpan())) 
                        yield return bar;
                    break;
            }
        }

        private void RequestTradingSessionStatus()
        {
            // Note: requestTradingSessionStatus() MUST be called just after login

            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.requestTradingSessionStatus();
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.LoadInstruments(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));
        }

        #region IGenericMessageListener implementation

        /// <summary>
        /// Receives generic messages from the FXCM API
        /// </summary>
        /// <param name="message">Generic message received</param>
        public void messageArrived(ITransportable message)
        {
            // Dispatch message to specific handler
            lock (_locker)
            {
                if (message is TradingSessionStatus)
                    OnTradingSessionStatus((TradingSessionStatus)message);

                else if (message is MarketDataSnapshot)
                    OnMarketDataSnapshot((MarketDataSnapshot)message);
            }
        }

        /// <summary>
        /// TradingSessionStatus message handler
        /// </summary>
        private void OnTradingSessionStatus(TradingSessionStatus message)
        {
            if (message.getRequestID() == _currentRequest)
            {
                // load instrument list into a dictionary
                var securities = message.getSecurities();
                while (securities.hasMoreElements())
                {
                    var security = (TradingSecurity)securities.nextElement();
                    _fxcmInstruments[security.getSymbol()] = security;
                }

                _mapRequestsToAutoResetEvents[_currentRequest].Set();
                _mapRequestsToAutoResetEvents.Remove(_currentRequest);
            }
        }

        /// <summary>
        /// MarketDataSnapshot message handler
        /// </summary>
        private void OnMarketDataSnapshot(MarketDataSnapshot message)
        {
            if (message.getRequestID() == _currentRequest)
            {
                // create new TradeBar from FXCM response message
                var symbol = _symbolMapper.GetLeanSymbol(message.getInstrument().getSymbol());
                var time = FromJavaDateUtc(message.getDate().toDate());
                var open = Convert.ToDecimal((message.getBidOpen() + message.getAskOpen()) / 2);
                var high = Convert.ToDecimal((message.getBidHigh() + message.getAskHigh()) / 2);
                var low = Convert.ToDecimal((message.getBidLow() + message.getAskLow()) / 2);
                var close = Convert.ToDecimal((message.getBidClose() + message.getAskClose()) / 2);
                var bar = new TradeBar(time, symbol, open, high, low, close, 0);

                // add bar to list
                _currentBars.Add(bar);

                if (message.getFXCMContinuousFlag() == IFixValueDefs.__Fields.FXCMCONTINUOUS_END)
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        #endregion


        #region IStatusMessageListener implementation

        /// <summary>
        /// Receives status messages from the FXCM API
        /// </summary>
        /// <param name="message">Status message received</param>
        public void messageArrived(ISessionStatus message)
        {
        }

        #endregion

        /// <summary>
        /// Aggregates a list of 10-second bars at the requested resolution
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="bars"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        internal static IEnumerable<TradeBar> AggregateBars(Symbol symbol, IEnumerable<TradeBar> bars, TimeSpan resolution)
        {
            return
                (from b in bars
                    group b by b.Time.RoundDown(resolution)
                    into g
                    select new TradeBar
                    {
                        Symbol = symbol,
                        Time = g.Key,
                        Open = g.First().Open,
                        High = g.Max(b => b.High),
                        Low = g.Min(b => b.Low),
                        Close = g.Last().Close
                    });
        }
    }
}
