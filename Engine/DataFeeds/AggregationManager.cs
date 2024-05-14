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

using QuantConnect.Data;
using QuantConnect.Data.Common;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Aggregates ticks and bars based on given subscriptions.
    /// Current implementation is based on <see cref="IDataConsolidator"/> that consolidates ticks and put them into enumerator.
    /// </summary>
    public class AggregationManager : IDataAggregator
    {
        private readonly ConcurrentDictionary<SecurityIdentifier, List<KeyValuePair<SubscriptionDataConfig, ScannableEnumerator<BaseData>>>> _enumerators
            = new ConcurrentDictionary<SecurityIdentifier, List<KeyValuePair<SubscriptionDataConfig, ScannableEnumerator<BaseData>>>>();
        private bool _dailyStrictEndTimeEnabled;

        /// <summary>
        /// Continuous UTC time provider
        /// </summary>
        protected ITimeProvider TimeProvider { get; set; } = RealTimeProvider.Instance;

        /// <summary>
        /// Initialize this instance
        /// </summary>
        /// <param name="parameters">The parameters dto instance</param>
        public void Initialize(DataAggregatorInitializeParameters parameters)
        {
            _dailyStrictEndTimeEnabled = parameters.AlgorithmSettings.DailyStrictEndTimeEnabled;
            Log.Trace($"AggregationManager.Initialize(): daily strict end times: {_dailyStrictEndTimeEnabled}");
        }

        /// <summary>
        /// Add new subscription to current <see cref="IDataAggregator"/> instance
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Add(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            var consolidator = GetConsolidator(dataConfig);
            var isPeriodBased = (dataConfig.Type.Name == nameof(QuoteBar) ||
                    dataConfig.Type.Name == nameof(TradeBar) ||
                    dataConfig.Type.Name == nameof(OpenInterest)) &&
                dataConfig.Resolution != Resolution.Tick;
            var enumerator = new ScannableEnumerator<BaseData>(consolidator, dataConfig.ExchangeTimeZone, TimeProvider, newDataAvailableHandler, isPeriodBased);

            _enumerators.AddOrUpdate(
                dataConfig.Symbol.ID,
                new List<KeyValuePair<SubscriptionDataConfig, ScannableEnumerator<BaseData>>> { new KeyValuePair<SubscriptionDataConfig, ScannableEnumerator<BaseData>>(dataConfig, enumerator) },
                (k, v) => { return v.Concat(new[] { new KeyValuePair<SubscriptionDataConfig, ScannableEnumerator<BaseData>>(dataConfig, enumerator) }).ToList(); });

            return enumerator;
        }

        /// <summary>
        /// Removes the handler with the specified identifier
        /// </summary>
        /// <param name="dataConfig">Subscription data configuration to be removed</param>
        public bool Remove(SubscriptionDataConfig dataConfig)
        {
            List<KeyValuePair<SubscriptionDataConfig, ScannableEnumerator<BaseData>>> enumerators;
            if (_enumerators.TryGetValue(dataConfig.Symbol.ID, out enumerators))
            {
                if (enumerators.Count == 1)
                {
                    List<KeyValuePair<SubscriptionDataConfig, ScannableEnumerator<BaseData>>> output;
                    return _enumerators.TryRemove(dataConfig.Symbol.ID, out output);
                }
                else
                {
                    _enumerators[dataConfig.Symbol.ID] = enumerators.Where(pair => pair.Key != dataConfig).ToList();
                    return true;
                }
            }
            else
            {
                Log.Debug($"AggregationManager.Update(): IDataConsolidator for symbol ({dataConfig.Symbol.Value}) was not found.");
                return false;
            }
        }

        /// <summary>
        /// Add new data to aggregator
        /// </summary>
        /// <param name="input">The new data</param>
        public void Update(BaseData input)
        {
            try
            {
                List<KeyValuePair<SubscriptionDataConfig, ScannableEnumerator<BaseData>>> enumerators;
                if (_enumerators.TryGetValue(input.Symbol.ID, out enumerators))
                {
                    for (var i = 0; i < enumerators.Count; i++)
                    {
                        var kvp = enumerators[i];

                        // for non tick resolution subscriptions drop suspicious ticks
                        if (kvp.Key.Resolution != Resolution.Tick)
                        {
                            var tick = input as Tick;
                            if (tick != null && tick.Suspicious)
                            {
                                continue;
                            }
                        }

                        kvp.Value.Update(input);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        /// <summary>
        /// Dispose of the aggregation manager.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Gets the consolidator to aggregate data for the given config
        /// </summary>
        protected virtual IDataConsolidator GetConsolidator(SubscriptionDataConfig config)
        {
            var period = config.Resolution.ToTimeSpan();
            if (config.Resolution == Resolution.Daily && (config.Type == typeof(QuoteBar) || config.Type == typeof(TradeBar)))
            {
                // let's build daily bars that respect market hours data as requested by 'ExtendedMarketHours',
                // also this allows us to enable the daily strict end times if required
                return new MarketHourAwareConsolidator(_dailyStrictEndTimeEnabled, config.Resolution, typeof(Tick), config.TickType, config.ExtendedMarketHours);
            }
            if (config.Type == typeof(QuoteBar))
            {
                return new TickQuoteBarConsolidator(period);
            }
            if (config.Type == typeof(TradeBar))
            {
                return new TickConsolidator(period);
            }
            if (config.Type == typeof(OpenInterest))
            {
                return new OpenInterestConsolidator(period);
            }
            if (config.Type == typeof(Tick))
            {
                return FilteredIdentityDataConsolidator.ForTickType(config.TickType);
            }
            if (config.Type == typeof(Split))
            {
                return new IdentityDataConsolidator<Split>();
            }
            if (config.Type == typeof(Dividend))
            {
                return new IdentityDataConsolidator<Dividend>();
            }

            // streaming custom data subscriptions can pass right through
            return new FilteredIdentityDataConsolidator<BaseData>(data => data.GetType() == config.Type);
        }
    }
}
