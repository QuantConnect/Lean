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
using NodaTime;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Python;
using Python.Runtime;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Gets or sets the history provider for the algorithm
        /// </summary>
        public IHistoryProvider HistoryProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets whether or not this algorithm is still warming up
        /// </summary>
        [DocumentationAttribute(HistoricalData)]
        public bool IsWarmingUp
        {
            get;
            private set;
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        [DocumentationAttribute(HistoricalData)]
        public void SetWarmup(TimeSpan timeSpan)
        {
            SetWarmUp(timeSpan, null);
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        [DocumentationAttribute(HistoricalData)]
        public void SetWarmUp(TimeSpan timeSpan)
        {
            SetWarmup(timeSpan);
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        /// <param name="resolution">The resolution to request</param>
        [DocumentationAttribute(HistoricalData)]
        public void SetWarmup(TimeSpan timeSpan, Resolution? resolution)
        {
            SetWarmup(null, timeSpan, resolution);
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        /// <param name="resolution">The resolution to request</param>
        [DocumentationAttribute(HistoricalData)]
        public void SetWarmUp(TimeSpan timeSpan, Resolution? resolution)
        {
            SetWarmup(timeSpan, resolution);
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm. The highest (smallest) resolution in the securities collection will be used.
        /// For example, if an algorithm has minute and daily data and 200 bars are requested, that would
        /// use 200 minute bars.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        [DocumentationAttribute(HistoricalData)]
        public void SetWarmup(int barCount)
        {
            SetWarmUp(barCount, null);
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm. The highest (smallest) resolution in the securities collection will be used.
        /// For example, if an algorithm has minute and daily data and 200 bars are requested, that would
        /// use 200 minute bars.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        [DocumentationAttribute(HistoricalData)]
        public void SetWarmUp(int barCount)
        {
            SetWarmup(barCount);
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        /// <param name="resolution">The resolution to request</param>
        [DocumentationAttribute(HistoricalData)]
        public void SetWarmup(int barCount, Resolution? resolution)
        {
            SetWarmup(barCount, null, resolution);
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        /// <param name="resolution">The resolution to request</param>
        [DocumentationAttribute(HistoricalData)]
        public void SetWarmUp(int barCount, Resolution? resolution)
        {
            SetWarmup(barCount, resolution);
        }

        /// <summary>
        /// Sets <see cref="IAlgorithm.IsWarmingUp"/> to false to indicate this algorithm has finished its warm up
        /// </summary>
        [DocumentationAttribute(HistoricalData)]
        public void SetFinishedWarmingUp()
        {
            IsWarmingUp = false;
        }

        /// <summary>
        /// Message for exception that is thrown when the implicit conversion between symbol and string fails
        /// </summary>
        private readonly string _symbolEmptyErrorMessage = "Cannot create history for the given ticker. " +
                                                           "Either explicitly use a symbol object to make the history request " +
                                                           "or ensure the symbol has been added using the AddSecurity() method before making the history request.";

        /// <summary>
        /// Gets the history requests required for provide warm up data for the algorithm
        /// </summary>
        /// <returns></returns>
        [DocumentationAttribute(HistoricalData)]
        private bool TryGetWarmupHistoryStartTime(out DateTime result)
        {
            result = Time;

            if (_warmupBarCount.HasValue)
            {
                var symbols = Securities.Keys;
                if (symbols.Count != 0)
                {
                    var startTimeUtc = CreateBarCountHistoryRequests(symbols, _warmupBarCount.Value, Settings.WarmupResolution)
                        .DefaultIfEmpty()
                        .Min(request => request == null ? default : request.StartTimeUtc);
                    if(startTimeUtc != default)
                    {
                        result = startTimeUtc.ConvertFromUtc(TimeZone);
                        return true;
                    }
                }

                var defaultResolutionToUse = UniverseSettings.Resolution;
                if (Settings.WarmupResolution.HasValue)
                {
                    defaultResolutionToUse = Settings.WarmupResolution.Value;
                }

                // if the algorithm has no added security, let's take a look at the universes to determine
                // what the start date should be used. Defaulting to always open
                result = Time - _warmupBarCount.Value * defaultResolutionToUse.ToTimeSpan();

                foreach (var universe in _pendingUniverseAdditions.Concat(UniverseManager.Values))
                {
                    var config = universe.Configuration;
                    var resolution = universe.Configuration.Resolution;
                    if (Settings.WarmupResolution.HasValue)
                    {
                        resolution = Settings.WarmupResolution.Value;
                    }
                    var exchange = MarketHoursDatabase.GetExchangeHours(config);
                    var start = _historyRequestFactory.GetStartTimeAlgoTz(config.Symbol, _warmupBarCount.Value, resolution, exchange, config.DataTimeZone);
                    // we choose the min start
                    result = result < start ? result : start;
                }
                return true;
            }
            if (_warmupTimeSpan.HasValue)
            {
                result = Time - _warmupTimeSpan.Value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the history for all configured securities over the requested span.
        /// This will use the resolution and other subscription settings for each security.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="span">The span over which to request data. This is a calendar span, so take into consideration weekends and such</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing data over the most recent span for all configured securities</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<Slice> History(TimeSpan span, Resolution? resolution = null, bool? fillForward = null, bool? extendedMarketHours = null,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
        {
            return History(Securities.Keys, Time - span, Time, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset).Memoize();
        }

        /// <summary>
        /// Get the history for all configured securities over the requested span.
        /// This will use the resolution and other subscription settings for each security.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing data over the most recent span for all configured securities</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<Slice> History(int periods, Resolution? resolution = null, bool? fillForward = null, bool? extendedMarketHours = null,
            DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
        {
            return History(Securities.Keys, periods, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset).Memoize();
        }

        /// <summary>
        /// Gets the historical data for all symbols of the requested type over the requested span.
        /// The symbol's configured values for resolution and fill forward behavior will be used
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<DataDictionary<T>> History<T>(TimeSpan span, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode ? dataNormalizationMode = null,
            int? contractDepthOffset = null)
            where T : IBaseData
        {
            return History<T>(Securities.Keys, span, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, TimeSpan span, Resolution? resolution = null,
            bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
            where T : IBaseData
        {
            return History<T>(symbols, Time - span, Time, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols. The exact number of bars will be returned for
        /// each symbol. This may result in some data start earlier/later than others due to when various
        /// exchanges are open. The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null,
            bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
            where T : IBaseData
        {
            CheckPeriodBasedHistoryRequestResolution(symbols, resolution);
            var requests = CreateBarCountHistoryRequests(symbols, typeof(T), periods, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset);
            return GetDataTypedHistory<T>(requests);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, DateTime start, DateTime end, Resolution? resolution = null,
            bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
            where T : IBaseData
        {
            var requests = CreateDateRangeHistoryRequests(symbols, typeof(T), start, end, resolution, fillForward, extendedMarketHours,
                dataMappingMode, dataNormalizationMode, contractDepthOffset);
            return GetDataTypedHistory<T>(requests);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbol</typeparam>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<T> History<T>(Symbol symbol, TimeSpan span, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
            where T : IBaseData
        {
            return History<T>(symbol, Time - span, Time, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<TradeBar> History(Symbol symbol, int periods, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
        {
            if (symbol == null) throw new ArgumentException(_symbolEmptyErrorMessage);

            resolution = GetResolution(symbol, resolution);
            CheckPeriodBasedHistoryRequestResolution(new[] { symbol }, resolution);
            var marketHours = GetMarketHours(symbol);
            var start = _historyRequestFactory.GetStartTimeAlgoTz(symbol, periods, resolution.Value, marketHours.ExchangeHours,
                marketHours.DataTimeZone, extendedMarketHours);

            return History(symbol, start, Time, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbol</typeparam>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<T> History<T>(Symbol symbol, int periods, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
            where T : IBaseData
        {
            resolution = GetResolution(symbol, resolution);
            CheckPeriodBasedHistoryRequestResolution(new[] { symbol }, resolution);
            var requests = CreateBarCountHistoryRequests(new [] { symbol }, typeof(T), periods, resolution, fillForward, extendedMarketHours,
                dataMappingMode, dataNormalizationMode, contractDepthOffset);
            return GetDataTypedHistory<T>(requests, symbol);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol between the specified dates. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<T> History<T>(Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
            where T : IBaseData
        {
            var requests = CreateDateRangeHistoryRequests(new[] { symbol }, typeof(T), start, end, resolution, fillForward, extendedMarketHours,
                dataMappingMode, dataNormalizationMode, contractDepthOffset);
            return GetDataTypedHistory<T>(requests, symbol);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<TradeBar> History(Symbol symbol, TimeSpan span, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
        {
            return History(symbol, Time - span, Time, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<TradeBar> History(Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
        {
            var securityType = symbol.ID.SecurityType;
            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd)
            {
                Error("Calling History<TradeBar> method on a Forex or CFD security will return an empty result. Please use the generic version with QuoteBar type parameter.");
            }

            var resolutionToUse = resolution ?? GetResolution(symbol, resolution);
            if (resolutionToUse == Resolution.Tick)
            {
                throw new InvalidOperationException("Calling History<TradeBar> method with Resolution.Tick will return an empty result." +
                                                    " Please use the generic version with Tick type parameter or provide a list of Symbols to use the Slice history request API.");
            }

            return History(new[] { symbol }, start, end, resolutionToUse, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset).Get(symbol).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbol's configured values for resolution and fill forward behavior will be used
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, TimeSpan span, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
        {
            return History(symbols, Time - span, Time, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols. The exact number of bars will be returned for
        /// each symbol. This may result in some data start earlier/later than others due to when various
        /// exchanges are open. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
        {
            CheckPeriodBasedHistoryRequestResolution(symbols, resolution);
            return History(CreateBarCountHistoryRequests(symbols, periods, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset)).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, DateTime start, DateTime end, Resolution? resolution = null,
            bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
        {
            return History(CreateDateRangeHistoryRequests(symbols, start, end, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset)).Memoize();
        }

        /// <summary>
        /// Executes the specified history request
        /// </summary>
        /// <param name="request">the history request to execute</param>
        /// <returns>An enumerable of slice satisfying the specified history request</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<Slice> History(HistoryRequest request)
        {
            return History(new[] { request }).Memoize();
        }

        /// <summary>
        /// Executes the specified history requests
        /// </summary>
        /// <param name="requests">the history requests to execute</param>
        /// <returns>An enumerable of slice satisfying the specified history request</returns>
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<Slice> History(IEnumerable<HistoryRequest> requests)
        {
            return History(requests, TimeZone).Memoize();
        }

        /// <summary>
        /// Yields data to warmup a security for all it's subscribed data types
        /// </summary>
        /// <param name="security"><see cref="Security"/> object for which to retrieve historical data</param>
        /// <returns>Securities historical data</returns>
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<BaseData> GetLastKnownPrices(Security security)
        {
            return GetLastKnownPrices(security.Symbol);
        }

        /// <summary>
        /// Yields data to warmup a security for all it's subscribed data types
        /// </summary>
        /// <param name="symbol">The symbol we want to get seed data for</param>
        /// <returns>Securities historical data</returns>
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(HistoricalData)]
        public IEnumerable<BaseData> GetLastKnownPrices(Symbol symbol)
        {
            if (!HistoryRequestValid(symbol) || HistoryProvider == null)
            {
                return Enumerable.Empty<BaseData>();
            }

            var result = new Dictionary<TickType, BaseData>();
            Resolution? resolution = null;
            Func<int, bool> requestData = period =>
            {
                var historyRequests = CreateBarCountHistoryRequests(new[] { symbol }, period)
                    .Select(request =>
                    {
                        // For speed and memory usage, use Resolution.Minute as the minimum resolution
                        request.Resolution = (Resolution)Math.Max((int)Resolution.Minute, (int)request.Resolution);
                        // force no fill forward behavior
                        request.FillForwardResolution = null;

                        resolution = request.Resolution;
                        return request;
                    })
                    // request only those tick types we didn't get the data we wanted
                    .Where(request => !result.ContainsKey(request.TickType))
                    .ToList();
                foreach (var slice in History(historyRequests))
                {
                    for (var i = 0; i < historyRequests.Count; i++)
                    {
                        var historyRequest = historyRequests[i];
                        var data = slice.Get(historyRequest.DataType);
                        if (data.ContainsKey(symbol))
                        {
                            // keep the last data point per tick type
                            result[historyRequest.TickType] = (BaseData)data[symbol];
                        }
                    }
                }
                // true when all history requests tick types have a data point
                return historyRequests.All(request => result.ContainsKey(request.TickType));
            };

            if (!requestData(5))
            {
                if (resolution.HasValue)
                {
                    // If the first attempt to get the last know price returns null, it maybe the case of an illiquid security.
                    // We increase the look-back period for this case accordingly to the resolution to cover 3 trading days
                    var periods =
                        resolution.Value == Resolution.Daily ? 3 :
                        resolution.Value == Resolution.Hour ? 24 : 1440;
                    requestData(periods);
                }
                else
                {
                    // this shouldn't happen but just in case
                    QuantConnect.Logging.Log.Error(
                        $"QCAlgorithm.GetLastKnownPrices(): no history request was created for symbol {symbol} at {Time}");
                }
            }
            // return the data ordered by time ascending
            return result.Values.OrderBy(data => data.Time);
        }

        /// <summary>
        /// Get the last known price using the history provider.
        /// Useful for seeding securities with the correct price
        /// </summary>
        /// <param name="security"><see cref="Security"/> object for which to retrieve historical data</param>
        /// <returns>A single <see cref="BaseData"/> object with the last known price</returns>
        [Obsolete("This method is obsolete please use 'GetLastKnownPrices' which will return the last data point" +
            " for each type associated with the requested security")]
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(HistoricalData)]
        public BaseData GetLastKnownPrice(Security security)
        {
            return GetLastKnownPrices(security.Symbol)
                // since we are returning a single data point let's respect order
                .OrderByDescending(data => GetTickTypeOrder(data.Symbol.SecurityType, LeanData.GetCommonTickTypeForCommonDataTypes(data.GetType(), data.Symbol.SecurityType)))
                .LastOrDefault();
        }

        /// <summary>
        /// Centralized logic to get data typed history given a list of requests for the specified symbol.
        /// This method is used to keep backwards compatibility for those History methods that expect an ArgumentException to be thrown
        /// when the security and the requested data type do not match
        /// </summary>
        /// <remarks>
        /// This method will check for Python custom data types in order to call the right Slice.Get dynamic method
        /// </remarks>
        private IEnumerable<T> GetDataTypedHistory<T>(IEnumerable<HistoryRequest> requests, Symbol symbol)
            where T : IBaseData
        {
            var type = typeof(T);

            var historyRequests = requests.Where(x => x != null).ToList();
            if (historyRequests.Count == 0)
            {
                throw new ArgumentException($"No history data could be fetched. " +
                    $"This could be due to the specified security not being of the requested type. Symbol: {symbol} Requested Type: {type.Name}");
            }

            var slices = History(historyRequests, TimeZone);

            IEnumerable<T> result = null;

            // If T is a custom data coming from Python (a class derived from PythonData), T will get here as PythonData
            // and not the actual custom type. We take care of this especial case by using a dynamic version of GetDataTypedHistory that
            // receives the Python type, and we get it from the history requests.
            if (type == typeof(PythonData))
            {
                result = GetPythonCustomDataTypeHistory(slices, historyRequests, symbol).OfType<T>();
            }
            // TODO: This is a patch to fix the issue with the Slice.GetImpl method returning only the last tick
            //       for each symbol instead of the whole list of ticks.
            //       The actual issue is Slice.GetImpl, so patch this can be removed right after it is properly addressed.
            //       A proposed solution making the Tick class a BaseDataCollection and make the Ticks class a dictionary Symbol->Tick instead of
            //       Symbol->List<Tick> so we can use the Slice.Get methods to collect all ticks in every slice instead of only the last one.
            else if (type == typeof(Tick))
            {
                result = (IEnumerable<T>)slices.Select(x => x.Ticks).Where(x => x.ContainsKey(symbol)).SelectMany(x => x[symbol]);
            }
            else
            {
                result = slices.Get<T>(symbol);
            }

            return result.Memoize();
        }

        /// <summary>
        /// Centralized logic to get data typed history for a given list of requests.
        /// </summary>
        /// <remarks>
        /// This method will check for Python custom data types in order to call the right Slice.Get dynamic method
        /// </remarks>
        private IEnumerable<DataDictionary<T>> GetDataTypedHistory<T>(IEnumerable<HistoryRequest> requests)
            where T : IBaseData
        {
            var historyRequests = requests.Where(x => x != null).ToList();
            var slices = History(historyRequests, TimeZone);

            IEnumerable<DataDictionary<T>> result = null;

            if (typeof(T) == typeof(PythonData))
            {
                result = GetPythonCustomDataTypeHistory(slices, historyRequests).OfType<DataDictionary<T>>();
            }
            else
            {
                result = slices.Get<T>();
            }

            return result.Memoize();
        }

        [DocumentationAttribute(HistoricalData)]
        private IEnumerable<Slice> History(IEnumerable<HistoryRequest> requests, DateTimeZone timeZone)
        {
            var sentMessage = false;
            var hasPythonDataRequest = false;
            // filter out any universe securities that may have made it this far
            var filteredRequests = requests.Where(hr => HistoryRequestValid(hr.Symbol)).ToList();
            for (var i = 0; i < filteredRequests.Count; i++)
            {
                var request  = filteredRequests[i];
                // prevent future requests
                if (request.EndTimeUtc > UtcTime)
                {
                    var endTimeUtc = UtcTime;
                    var startTimeUtc = request.StartTimeUtc;
                    if (request.StartTimeUtc > request.EndTimeUtc)
                    {
                        startTimeUtc = request.EndTimeUtc;
                    }

                    filteredRequests[i] = new HistoryRequest(startTimeUtc, endTimeUtc,
                        request.DataType, request.Symbol, request.Resolution, request.ExchangeHours,
                        request.DataTimeZone, request.FillForwardResolution, request.IncludeExtendedMarketHours,
                        request.IsCustomData, request.DataNormalizationMode, request.TickType, request.DataMappingMode,
                        request.ContractDepthOffset);

                    if (!sentMessage)
                    {
                        sentMessage = true;
                        Debug("Request for future history modified to end now.");
                    }
                }

                if (!hasPythonDataRequest)
                {
                    hasPythonDataRequest = request.IsCustomData && typeof(PythonData).IsAssignableFrom(request.DataType);
                }
            }

            // filter out future data to prevent look ahead bias
            var history = HistoryProvider.GetHistory(filteredRequests, timeZone);

            if (hasPythonDataRequest && PythonEngine.IsInitialized)
            {
                // add protection against potential python deadlocks
                return WrapPythonDataHistory(history);
            }

            return history;
        }

        /// <summary>
        /// Helper method to create history requests from a date range
        /// </summary>
        private IEnumerable<HistoryRequest> CreateDateRangeHistoryRequests(IEnumerable<Symbol> symbols, DateTime startAlgoTz, DateTime endAlgoTz,
            Resolution? resolution = null, bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
        {
            return CreateDateRangeHistoryRequests(symbols, typeof(BaseData), startAlgoTz, endAlgoTz, resolution, fillForward, extendedMarketHours,
                dataMappingMode, dataNormalizationMode, contractDepthOffset);
        }

        /// <summary>
        /// Helper method to create history requests from a date range with custom data type
        /// </summary>
        private IEnumerable<HistoryRequest> CreateDateRangeHistoryRequests(IEnumerable<Symbol> symbols, Type requestedType, DateTime startAlgoTz, DateTime endAlgoTz,
            Resolution? resolution = null, bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
        {
            return symbols.Where(HistoryRequestValid).SelectMany(x =>
            {
                var requests = new List<HistoryRequest>();

                foreach (var config in GetMatchingSubscriptions(x, requestedType, resolution))
                {
                    var request = _historyRequestFactory.CreateHistoryRequest(config, startAlgoTz, endAlgoTz, GetExchangeHours(x), resolution,
                        fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode, contractDepthOffset);
                    requests.Add(request);
                }

                return requests;
            });
        }

        /// <summary>
        /// Helper methods to create a history request for the specified symbols and bar count
        /// </summary>
        private IEnumerable<HistoryRequest> CreateBarCountHistoryRequests(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null,
            bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
        {
            return CreateBarCountHistoryRequests(symbols, typeof(BaseData), periods, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset);
        }

        /// <summary>
        /// Helper methods to create a history request for the specified symbols and bar count with custom data type
        /// </summary>
        private IEnumerable<HistoryRequest> CreateBarCountHistoryRequests(IEnumerable<Symbol> symbols, Type requestedType, int periods,
            Resolution? resolution = null, bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null)
        {
            return symbols.Where(HistoryRequestValid).SelectMany(symbol =>
            {
                var res = GetResolution(symbol, resolution);
                var exchange = GetExchangeHours(symbol);
                var configs = GetMatchingSubscriptions(symbol, requestedType, resolution).ToList();
                if (configs.Count == 0)
                {
                    return Enumerable.Empty<HistoryRequest>();
                }

                var start = _historyRequestFactory.GetStartTimeAlgoTz(symbol, periods, res, exchange, configs.First().DataTimeZone, extendedMarketHours);
                var end = Time;

                return configs.Select(config => _historyRequestFactory.CreateHistoryRequest(config, start, end, exchange, res, fillForward,
                    extendedMarketHours, dataMappingMode, dataNormalizationMode, contractDepthOffset));
            });
        }

        private int GetTickTypeOrder(SecurityType securityType, TickType tickType)
        {
            return SubscriptionManager.AvailableDataTypes[securityType].IndexOf(tickType);
        }

        private IEnumerable<SubscriptionDataConfig> GetMatchingSubscriptions(Symbol symbol, Type type, Resolution? resolution = null)
        {
            var matchingSubscriptions = SubscriptionManager.SubscriptionDataConfigService
                 // we add internal subscription so that history requests are covered, this allows us to warm them up too
                .GetSubscriptionDataConfigs(symbol, includeInternalConfigs:true)
                // find all subscriptions matching the requested type with a higher resolution than requested
                .OrderByDescending(s => s.Resolution)
                // lets make sure to respect the order of the data types
                .ThenByDescending(config => GetTickTypeOrder(config.SecurityType, config.TickType))
                .Where(s => SubscriptionDataConfigTypeFilter(type, s.Type));

            var internalConfig = new List<SubscriptionDataConfig>();
            var userConfig = new List<SubscriptionDataConfig>();
            foreach (var config in matchingSubscriptions)
            {
                if (config.IsInternalFeed)
                {
                    internalConfig.Add(config);
                }
                else
                {
                    userConfig.Add(config);
                }
            }

            // if we have any user defined subscription configuration we use it, else we use internal ones if any
            List<SubscriptionDataConfig> configs = null;
            if(userConfig.Count != 0)
            {
                configs = userConfig;
            }
            else if (internalConfig.Count != 0)
            {
                configs = internalConfig;
            }

            // we use the subscription manager registered configurations here, we can not rely on the Securities collection
            // since this might be called when creating a security and warming it up
            if (configs != null && configs.Count != 0)
            {
                if (resolution.HasValue
                    && (resolution == Resolution.Daily || resolution == Resolution.Hour)
                    && symbol.SecurityType == SecurityType.Equity)
                {
                    // for Daily and Hour resolution, for equities, we have to
                    // filter out any existing subscriptions that could be of Quote type
                    // This could happen if they were Resolution.Minute/Second/Tick
                    return configs.Where(s => s.TickType != TickType.Quote);
                }

                return configs;
            }
            else
            {
                var entry = MarketHoursDatabase.GetEntry(symbol, new []{ type });
                resolution = GetResolution(symbol, resolution);

                return SubscriptionManager
                    .LookupSubscriptionConfigDataTypes(symbol.SecurityType, resolution.Value, symbol.IsCanonical())
                    .Where(tuple => SubscriptionDataConfigTypeFilter(type, tuple.Item1))
                    .Select(x => new SubscriptionDataConfig(
                        x.Item1,
                        symbol,
                        resolution.Value,
                        entry.DataTimeZone,
                        entry.ExchangeHours.TimeZone,
                        UniverseSettings.FillForward,
                        UniverseSettings.ExtendedMarketHours,
                        true,
                        false,
                        x.Item2,
                        true,
                        UniverseSettings.GetUniverseNormalizationModeOrDefault(symbol.SecurityType)));
            }
        }

        /// <summary>
        /// Helper method to determine if the provided config type passes the filter of the target type
        /// </summary>
        /// <remarks>If the target type is <see cref="BaseData"/>, <see cref="OpenInterest"/> config types will return false.
        /// This is useful to filter OpenInterest by default from history requests unless it's explicitly requested</remarks>
        private bool SubscriptionDataConfigTypeFilter(Type targetType, Type configType)
        {
            var targetIsGenericType = targetType == typeof(BaseData);

            return targetType.IsAssignableFrom(configType) && (!targetIsGenericType || configType != typeof(OpenInterest));
        }

        private SecurityExchangeHours GetExchangeHours(Symbol symbol)
        {
            return GetMarketHours(symbol).ExchangeHours;
        }

        private MarketHoursDatabase.Entry GetMarketHours(Symbol symbol)
        {
            var hoursEntry = MarketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType);

            // user can override the exchange hours in algorithm, i.e. HistoryAlgorithm
            Security security;
            if (Securities.TryGetValue(symbol, out security))
            {
                return new MarketHoursDatabase.Entry(hoursEntry.DataTimeZone, security.Exchange.Hours);
            }

            return hoursEntry;
        }

        private Resolution GetResolution(Symbol symbol, Resolution? resolution)
        {
            Security security;
            if (Securities.TryGetValue(symbol, out security))
            {
                if (resolution != null)
                {
                    return resolution.Value;
                }

                Resolution? result = null;
                var hasNonInternal = false;
                foreach (var config in SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(symbol, includeInternalConfigs: true)
                    // we process non internal configs first
                    .OrderBy(config => config.IsInternalFeed ? 1 : 0))
                {
                    if (!config.IsInternalFeed || !hasNonInternal)
                    {
                        // once we find a non internal config we ignore internals
                        hasNonInternal |= !config.IsInternalFeed;
                        if (!result.HasValue || config.Resolution < result)
                        {
                            result = config.Resolution;
                        }
                    }
                }

                return result ?? UniverseSettings.Resolution;
            }
            else
            {
                return resolution ?? UniverseSettings.Resolution;
            }
        }

        /// <summary>
        /// Validate a symbol for a history request.
        /// Universe and canonical symbols are only valid for future security types
        /// </summary>
        private bool HistoryRequestValid(Symbol symbol)
        {
            return symbol.SecurityType == SecurityType.Future || !UniverseManager.ContainsKey(symbol) && !symbol.IsCanonical();
        }

        /// <summary>
        /// Will set warmup settings validating the algorithm has not finished initialization yet
        /// </summary>
        private void SetWarmup(int? barCount, TimeSpan? timeSpan, Resolution? resolution)
        {
            if (_locked)
            {
                throw new InvalidOperationException("QCAlgorithm.SetWarmup(): This method cannot be used after algorithm initialized");
            }

            _warmupTimeSpan = timeSpan;
            _warmupBarCount = barCount;
            Settings.WarmupResolution = resolution;
        }

        /// <summary>
        /// Throws if a period bases history request is made for tick resolution, which is not allowed.
        /// </summary>
        private void CheckPeriodBasedHistoryRequestResolution(IEnumerable<Symbol> symbols, Resolution? resolution)
        {
            if (symbols.Any(symbol => GetResolution(symbol, resolution) == Resolution.Tick))
            {
                throw new InvalidOperationException("History functions that accept a 'periods' parameter can not be used with Resolution.Tick");
            }
        }

        /// <summary>
        /// Centralized logic to get data typed history given a list of requests for the specified symbol.
        /// This method is used to keep backwards compatibility for those History methods that expect an ArgumentException to be thrown
        /// when the security and the requested data type do not match
        /// </summary>
        /// <remarks>
        /// This method is only used for Python algorithms, specially for those requesting custom data type history.
        /// The reason for using this method is that custom data type Python history calls to
        /// <see cref="History{T}(QuantConnect.Symbol, int, Resolution?)"/> will always use <see cref="PythonData"/> (the custom data base class)
        /// as the T argument, because the custom data class is a Python type, which will cause the history data in the slices to not be matched
        /// to the actual requested type, resulting in an empty list of slices.
        /// </remarks>
        private static IEnumerable<dynamic> GetPythonCustomDataTypeHistory(IEnumerable<Slice> slices, List<HistoryRequest> requests,
            Symbol symbol = null)
        {
            if (requests.Count == 0 || requests.Any(x => x.DataType != requests[0].DataType))
            {
                throw new ArgumentException("QCAlgorithm.GetPythonCustomDataTypeHistory(): All history requests must be for the same data type");
            }

            var pythonType = requests[0].DataType;

            if (symbol == null)
            {
                return slices.Get(pythonType);
            }

            return slices.Get(pythonType, symbol);
        }

        /// <summary>
        /// Wraps the resulting history enumerable in case of a Python custom data history request.
        /// We need to get and release the Python GIL when parallel history requests are enabled to avoid deadlocks
        /// in the custom data readers.
        /// </summary>
        private static IEnumerable<Slice> WrapPythonDataHistory(IEnumerable<Slice> history)
        {
            using var enumerator = history.GetEnumerator();

            var hasData = true;
            while (hasData)
            {
                // TODO: we don't really need the GIL. We should find a way to check whether we have the lock and only call this wrapper method if we do.
                using (Py.GIL())
                {
                    var state = PythonEngine.BeginAllowThreads();
                    hasData = enumerator.MoveNext();
                    PythonEngine.EndAllowThreads(state);
                }

                if (hasData)
                {
                    yield return enumerator.Current;
                }
            }
        }
    }
}
