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
using Newtonsoft.Json.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// ZerodhaBrokerage - IHistoryProvider implementation
    /// </summary>
    public partial class ZerodhaBrokerage
    {
        #region IHistoryProvider implementation
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
        /// Event fired when the start date has been limited
        /// </summary>
        public event EventHandler<StartDateLimitedEventArgs> StartDateLimited;

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public int DataPointCount { get; private set; }



        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public void Initialize(HistoryProviderInitializeParameters parameters)
        {
            Log.Trace("Init Zerodha Intraday History Provider");
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

        /// <summary>
        /// Event invocator for the Message event
        /// </summary>
        /// <param name="e">The error</param>
        public new void OnMessage(BrokerageMessageEvent e)
        {
            base.OnMessage(e);
        }

        /// <summary>
        /// Gets a list of current subscriptions
        /// </summary>
        /// <returns></returns>
        protected virtual IList<Symbol> GetSubscribed()
        {
            IList<Symbol> list = new List<Symbol>();
            lock (ChannelList)
            {
                foreach (var item in ChannelList)
                {
                    list.Add(Symbol.Create(item.Value.Symbol, item.Value.SecurityType, _market));
                }
            }
            return list;
        }

        public IEnumerable<Slice> GetHistory(IEnumerable<Data.HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {

            foreach (var request in requests)
            {
                if (request.Symbol.ID.SecurityType == SecurityType.Cfd || request.Symbol.ID.SecurityType == SecurityType.Crypto || request.Symbol.ID.SecurityType == SecurityType.Forex || request.Symbol.ID.SecurityType == SecurityType.Commodity)
                {
                    throw new ArgumentException("Zerodha does not support this security type: " + request.Symbol.ID.SecurityType);
                }

                if (request.StartTimeUtc >= request.EndTimeUtc)
                {
                    throw new ArgumentException("Invalid date range specified");
                }

                var start = request.StartTimeUtc.ConvertTo(DateTimeZone.Utc, TimeZones.Kolkata);
                var end = request.EndTimeUtc.ConvertTo(DateTimeZone.Utc, TimeZones.Kolkata);

                var history = Enumerable.Empty<Slice>();


                switch (request.Resolution)
                {
                    case Resolution.Tick:
                        history = GetHistoryTick(request.Symbol, start, end);
                        break;

                    case Resolution.Second:
                        history = GetHistorySecond(request.Symbol, start, end);
                        break;

                    case Resolution.Minute:
                        history = GetHistoryMinute(request.Symbol, start, end);
                        break;

                    case Resolution.Hour:
                        history = GetHistoryHour(request.Symbol, start, end);
                        break;

                    case Resolution.Daily:
                        history = GetHistoryDaily(request.Symbol, start, end);
                        break;
                }

                foreach (var slice in history)
                {
                    yield return slice;
                }
            }
        }

        private IEnumerable<Slice> GetHistoryDaily(Symbol symbol, DateTime start, DateTime end)
        {
            Log.Debug("ZerodhaBrokerage.GetHistoryDaily();");
            string scripSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var candles = _kite.GetHistoricalData(scripSymbol, start, end, "day");

            if (!candles.Any())
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "NoHistoricalData",
                    $"Exchange returned no data for {symbol} on history request " +
                    $"from {start:s} to {end:s}"));
            }

            if (candles == null)
                return Enumerable.Empty<Slice>();

            DataPointCount += candles.Count;

            return candles
                .Select(bar => new TradeBar(bar.TimeStamp, symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, Time.OneDay))
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }));
        }

        private IEnumerable<Slice> GetHistoryHour(Symbol symbol, DateTime start, DateTime end)
        {
            Log.Debug("ZerodhaBrokerage.GetHistoryHour();");
            string scripSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var candles = _kite.GetHistoricalData(scripSymbol, start, end, "60minute");

            if (!candles.Any())
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "NoHistoricalData",
                    $"Exchange returned no data for {symbol} on history request " +
                    $"from {start:s} to {end:s}"));
            }

            if (candles == null)
                return Enumerable.Empty<Slice>();

            DataPointCount += candles.Count;

            return candles
                .Select(bar => new TradeBar(bar.TimeStamp, symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, Time.OneHour))
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }));
        }

        private IEnumerable<Slice> GetHistoryMinute(Symbol symbol, DateTime start, DateTime end)
        {
            Log.Debug("ZerodhaBrokerage.GetHistoryMinute();");
            string scripSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var candles = _kite.GetHistoricalData(scripSymbol, start, end, "minute");

            if (!candles.Any())
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "NoHistoricalData",
                    $"Exchange returned no data for {symbol} on history request " +
                    $"from {start:s} to {end:s}"));
            }

            if (candles == null)
                return Enumerable.Empty<Slice>();

            DataPointCount += candles.Count;

            return candles
                .Select(bar => new TradeBar(bar.TimeStamp, symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, Time.OneMinute))
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }));
        }

        private IEnumerable<Slice> GetHistorySecond(Symbol symbol, DateTime start, DateTime end)
        {
            throw new ArgumentException("Zerodha only supports minute, day, 3minute, 5minute, 10minute, 15minute, 30minute & 60minute resolutions");
        }

        private IEnumerable<Slice> GetHistoryTick(Symbol symbol, DateTime start, DateTime end)
        {
            throw new ArgumentException("Zerodha only supports minute, day, 3minute, 5minute, 10minute, 15minute, 30minute & 60minute resolutions");
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
        }

        private void OnError(object sender, WebSocketError e)
        {
            Log.Error($"ZerodhaSubscriptionManager.OnError(): Message: {e.Message} Exception: {e.Exception}");
        }

        private void OnMessage(object sender, MessageData e)
        {
            LastHeartbeatUtcTime = DateTime.UtcNow;

            // Verify if we're allowed to handle the streaming packet yet; while we're placing an order we delay the
            // stream processing a touch.
            try
            {
                if (_streamLocked)
                {
                    _messageBuffer.Enqueue(e);
                    return;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            OnMessageImpl(e);
        }

        /// <summary>
        /// Implementation of the OnMessage event
        /// </summary>
        /// <param name="e"></param>
        private void OnMessageImpl(MessageData e)
        {
            try
            {
                var token = JToken.Parse(e.Data.ToString());
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1, $"Parsing new wss message. Data: {e.Data}"));
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Data} Exception: {exception}"));
                throw;
            }
        }
        #endregion
    }
}
