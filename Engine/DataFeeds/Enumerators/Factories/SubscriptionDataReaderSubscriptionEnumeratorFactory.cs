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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> that used the <see cref="SubscriptionDataReader"/>
    /// </summary>
    /// <remarks>Only used on backtesting by the <see cref="FileSystemDataFeed"/></remarks>
    public class SubscriptionDataReaderSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory, IDisposable
    {
        private readonly IResultHandler _resultHandler;
        private readonly IFactorFileProvider _factorFileProvider;
        private readonly IDataCacheProvider _dataCacheProvider;
        private readonly ConcurrentDictionary<Symbol, string> _numericalPrecisionLimitedWarnings;
        private readonly int _numericalPrecisionLimitedWarningsMaxCount = 10;
        private readonly ConcurrentDictionary<Symbol, string> _startDateLimitedWarnings;
        private readonly int _startDateLimitedWarningsMaxCount = 10;
        private readonly IMapFileProvider _mapFileProvider;
        private readonly bool _enablePriceScaling;
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionDataReaderSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="resultHandler">The result handler for the algorithm</param>
        /// <param name="mapFileProvider">The map file provider</param>
        /// <param name="factorFileProvider">The factor file provider</param>
        /// <param name="cacheProvider">Provider used to get data when it is not present on disk</param>
        /// <param name="algorithm">The algorithm instance to use</param>
        /// <param name="enablePriceScaling">Applies price factor</param>
        public SubscriptionDataReaderSubscriptionEnumeratorFactory(IResultHandler resultHandler,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            IDataCacheProvider cacheProvider,
            IAlgorithm algorithm,
            bool enablePriceScaling = true
            )
        {
            _algorithm = algorithm;
            _resultHandler = resultHandler;
            _mapFileProvider = mapFileProvider;
            _factorFileProvider = factorFileProvider;
            _dataCacheProvider = cacheProvider;
            _numericalPrecisionLimitedWarnings = new ConcurrentDictionary<Symbol, string>();
            _startDateLimitedWarnings = new ConcurrentDictionary<Symbol, string>();
            _enablePriceScaling = enablePriceScaling;
        }

        /// <summary>
        /// Creates a <see cref="SubscriptionDataReader"/> to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request, IDataProvider dataProvider)
        {
            var dataReader = new SubscriptionDataReader(request.Configuration,
                request,
                _mapFileProvider,
                _factorFileProvider,
                _dataCacheProvider,
                dataProvider,
                _algorithm.ObjectStore);

            dataReader.InvalidConfigurationDetected += (sender, args) => { _resultHandler.ErrorMessage(args.Message); };
            dataReader.StartDateLimited += (sender, args) =>
            {
                // Queue this warning into our dictionary to report on dispose
                if (_startDateLimitedWarnings.Count <= _startDateLimitedWarningsMaxCount)
                {
                    _startDateLimitedWarnings.TryAdd(args.Symbol, args.Message);
                }
            };
            dataReader.DownloadFailed += (sender, args) => { _resultHandler.ErrorMessage(args.Message, args.StackTrace); };
            dataReader.ReaderErrorDetected += (sender, args) => { _resultHandler.RuntimeError(args.Message, args.StackTrace); };
            dataReader.NumericalPrecisionLimited += (sender, args) =>
            {
                // Set a hard limit to keep this warning list from getting unnecessarily large
                if (_numericalPrecisionLimitedWarnings.Count <= _numericalPrecisionLimitedWarningsMaxCount)
                {
                    _numericalPrecisionLimitedWarnings.TryAdd(args.Symbol, args.Message);
                }
            };

            IEnumerator<BaseData> enumerator = dataReader;
            if (LeanData.UseDailyStrictEndTimes(_algorithm.Settings, request, request.Configuration.Symbol, request.Configuration.Increment))
            {
                // before corporate events which might yield data and we synchronize both feeds
                enumerator = new StrictDailyEndTimesEnumerator(enumerator, request.ExchangeHours);
            }

            enumerator = CorporateEventEnumeratorFactory.CreateEnumerators(
                enumerator,
                request.Configuration,
                _factorFileProvider,
                dataReader,
                _mapFileProvider,
                request.StartTimeLocal,
                request.EndTimeLocal,
                _enablePriceScaling);

            return enumerator;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            // Log our numerical precision limited warnings if any
            if (!_numericalPrecisionLimitedWarnings.IsNullOrEmpty())
            {
                var message = "Due to numerical precision issues in the factor file, data for the following" +
                    $" symbols was adjust to a later starting date: {string.Join(", ", _numericalPrecisionLimitedWarnings.Values.Take(_numericalPrecisionLimitedWarningsMaxCount))}";

                // If we reached our max warnings count suggest that more may have been left out
                if (_numericalPrecisionLimitedWarnings.Count >= _numericalPrecisionLimitedWarningsMaxCount)
                {
                    message += "...";
                }

                _resultHandler.DebugMessage(message);
            }

            // Log our start date adjustments because of map files
            if (!_startDateLimitedWarnings.IsNullOrEmpty())
            {
                var message = "The starting dates for the following symbols have been adjusted to match their" +
                    $" map files first date: {string.Join(", ", _startDateLimitedWarnings.Values.Take(_startDateLimitedWarningsMaxCount))}";

                // If we reached our max warnings count suggest that more may have been left out
                if (_startDateLimitedWarnings.Count >= _startDateLimitedWarningsMaxCount)
                {
                    message += "...";
                }

                _resultHandler.DebugMessage(message);
            }
        }
    }
}
