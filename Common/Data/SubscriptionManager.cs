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

using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using QuantConnect.Python;

namespace QuantConnect.Data
{
    /// <summary>
    ///     Enumerable Subscription Management Class
    /// </summary>
    public class SubscriptionManager
    {
        private readonly PriorityQueue<ConsolidatorWrapper, ConsolidatorScanPriority> _consolidatorsSortedByScanTime;
        private readonly Dictionary<IDataConsolidator, ConsolidatorWrapper> _consolidators;
        private readonly ITimeKeeper _timeKeeper;
        private IAlgorithmSubscriptionManager _subscriptionManager;

        /// <summary>
        ///     Instance that implements <see cref="ISubscriptionDataConfigService" />
        /// </summary>
        public ISubscriptionDataConfigService SubscriptionDataConfigService => _subscriptionManager;

        /// <summary>
        ///     Returns an IEnumerable of Subscriptions
        /// </summary>
        /// <remarks>Will not return internal subscriptions</remarks>
        public IEnumerable<SubscriptionDataConfig> Subscriptions => _subscriptionManager.SubscriptionManagerSubscriptions.Where(config => !config.IsInternalFeed);

        /// <summary>
        ///     The different <see cref="TickType" /> each <see cref="SecurityType" /> supports
        /// </summary>
        public Dictionary<SecurityType, List<TickType>> AvailableDataTypes => _subscriptionManager.AvailableDataTypes;

        /// <summary>
        ///     Get the count of assets:
        /// </summary>
        public int Count => _subscriptionManager.SubscriptionManagerCount();

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public SubscriptionManager(ITimeKeeper timeKeeper)
        {
            _consolidators = new();
            _timeKeeper = timeKeeper;
            _consolidatorsSortedByScanTime = new(1000);
        }

        /// <summary>
        ///     Add Market Data Required (Overloaded method for backwards compatibility).
        /// </summary>
        /// <param name="symbol">Symbol of the asset we're like</param>
        /// <param name="resolution">Resolution of Asset Required</param>
        /// <param name="timeZone">The time zone the subscription's data is time stamped in</param>
        /// <param name="exchangeTimeZone">
        ///     Specifies the time zone of the exchange for the security this subscription is for. This
        ///     is this output time zone, that is, the time zone that will be used on BaseData instances
        /// </param>
        /// <param name="isCustomData">True if this is custom user supplied data, false for normal QC data</param>
        /// <param name="fillForward">when there is no data pass the last tradebar forward</param>
        /// <param name="extendedMarketHours">Request premarket data as well when true </param>
        /// <returns>
        ///     The newly created <see cref="SubscriptionDataConfig" /> or existing instance if it already existed
        /// </returns>
        public SubscriptionDataConfig Add(
            Symbol symbol,
            Resolution resolution,
            DateTimeZone timeZone,
            DateTimeZone exchangeTimeZone,
            bool isCustomData = false,
            bool fillForward = true,
            bool extendedMarketHours = false
            )
        {
            //Set the type: market data only comes in two forms -- ticks(trade by trade) or tradebar(time summaries)
            var dataType = typeof(TradeBar);
            if (resolution == Resolution.Tick)
            {
                dataType = typeof(Tick);
            }

            var tickType = LeanData.GetCommonTickTypeForCommonDataTypes(dataType, symbol.SecurityType);
            return Add(dataType, tickType, symbol, resolution, timeZone, exchangeTimeZone, isCustomData, fillForward,
                extendedMarketHours);
        }

        /// <summary>
        ///     Add Market Data Required - generic data typing support as long as Type implements BaseData.
        /// </summary>
        /// <param name="dataType">Set the type of the data we're subscribing to.</param>
        /// <param name="tickType">Tick type for the subscription.</param>
        /// <param name="symbol">Symbol of the asset we're like</param>
        /// <param name="resolution">Resolution of Asset Required</param>
        /// <param name="dataTimeZone">The time zone the subscription's data is time stamped in</param>
        /// <param name="exchangeTimeZone">
        ///     Specifies the time zone of the exchange for the security this subscription is for. This
        ///     is this output time zone, that is, the time zone that will be used on BaseData instances
        /// </param>
        /// <param name="isCustomData">True if this is custom user supplied data, false for normal QC data</param>
        /// <param name="fillForward">when there is no data pass the last tradebar forward</param>
        /// <param name="extendedMarketHours">Request premarket data as well when true </param>
        /// <param name="isInternalFeed">
        ///     Set to true to prevent data from this subscription from being sent into the algorithm's
        ///     OnData events
        /// </param>
        /// <param name="isFilteredSubscription">
        ///     True if this subscription should have filters applied to it (market hours/user
        ///     filters from security), false otherwise
        /// </param>
        /// <param name="dataNormalizationMode">Define how data is normalized</param>
        /// <returns>
        ///     The newly created <see cref="SubscriptionDataConfig" /> or existing instance if it already existed
        /// </returns>
        public SubscriptionDataConfig Add(
            Type dataType,
            TickType tickType,
            Symbol symbol,
            Resolution resolution,
            DateTimeZone dataTimeZone,
            DateTimeZone exchangeTimeZone,
            bool isCustomData,
            bool fillForward = true,
            bool extendedMarketHours = false,
            bool isInternalFeed = false,
            bool isFilteredSubscription = true,
            DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted
            )
        {
            return SubscriptionDataConfigService.Add(symbol, resolution, fillForward,
                extendedMarketHours, isFilteredSubscription, isInternalFeed, isCustomData,
                new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(dataType, tickType) },
                dataNormalizationMode).First();
        }

        /// <summary>
        /// Add a consolidator for the symbol
        /// </summary>
        /// <param name="symbol">Symbol of the asset to consolidate</param>
        /// <param name="consolidator">The consolidator</param>
        /// <param name="tickType">Desired tick type for the subscription</param>
        public void AddConsolidator(Symbol symbol, IDataConsolidator consolidator, TickType? tickType = null)
        {
            // Find the right subscription and add the consolidator to it
            var subscriptions = Subscriptions.Where(x => x.Symbol == symbol).ToList();

            if (subscriptions.Count == 0)
            {
                // If we made it here it is because we never found the symbol in the subscription list
                throw new ArgumentException("Please subscribe to this symbol before adding a consolidator for it. Symbol: " +
                    symbol.Value);
            }

            foreach (var subscription in subscriptions)
            {
                // we need to be able to pipe data directly from the data feed into the consolidator
                if (IsSubscriptionValidForConsolidator(subscription, consolidator, tickType))
                {
                    subscription.Consolidators.Add(consolidator);

                    var wrapper = _consolidators[consolidator] =
                        new ConsolidatorWrapper(consolidator, subscription.Increment, _timeKeeper, _timeKeeper.GetLocalTimeKeeper(subscription.ExchangeTimeZone));

                    _consolidatorsSortedByScanTime.Enqueue(wrapper, wrapper.Priority);
                    return;
                }
            }

            string tickTypeException = null;
            if (tickType != null && !subscriptions.Where(x => x.TickType == tickType).Any())
            {
                tickTypeException = $"No subscription with the requested Tick Type {tickType} was found. Available Tick Types: {string.Join(", ", subscriptions.Select(x => x.TickType))}";
            }

            throw new ArgumentException(tickTypeException ?? ("Type mismatch found between consolidator and symbol. " +
                $"Symbol: {symbol.Value} does not support input type: {consolidator.InputType.Name}. " +
                $"Supported types: {string.Join(",", subscriptions.Select(x => x.Type.Name))}."));
        }

        /// <summary>
        /// Add a custom python consolidator for the symbol
        /// </summary>
        /// <param name="symbol">Symbol of the asset to consolidate</param>
        /// <param name="pyConsolidator">The custom python consolidator</param>
        public void AddConsolidator(Symbol symbol, PyObject pyConsolidator)
        {
            if (!pyConsolidator.TryConvert(out IDataConsolidator consolidator))
            {
                consolidator = new DataConsolidatorPythonWrapper(pyConsolidator);
            }

            AddConsolidator(symbol, consolidator);
        }

        /// <summary>
        ///     Removes the specified consolidator for the symbol
        /// </summary>
        /// <param name="symbol">The symbol the consolidator is receiving data from</param>
        /// <param name="consolidator">The consolidator instance to be removed</param>
        public void RemoveConsolidator(Symbol symbol, IDataConsolidator consolidator)
        {
            // let's try to get associated symbol, not required but nice to have
            symbol ??= consolidator.Consolidated?.Symbol;
            symbol ??= consolidator.WorkingData?.Symbol;

            // remove consolidator from each subscription
            foreach (var subscription in _subscriptionManager.GetSubscriptionDataConfigs(symbol))
            {
                subscription.Consolidators.Remove(consolidator);

                if (_consolidators.Remove(consolidator, out var consolidatorsToScan))
                {
                    consolidatorsToScan.Dispose();
                }
            }

            // dispose of the consolidator to remove any remaining event handlers
            consolidator.DisposeSafely();
        }

        /// <summary>
        /// Removes the specified python consolidator for the symbol
        /// </summary>
        /// <param name="symbol">The symbol the consolidator is receiving data from</param>
        /// <param name="pyConsolidator">The python consolidator instance to be removed</param>
        public void RemoveConsolidator(Symbol symbol, PyObject pyConsolidator)
        {
            if (!pyConsolidator.TryConvert(out IDataConsolidator consolidator))
            {
                consolidator = new DataConsolidatorPythonWrapper(pyConsolidator);
            }

            RemoveConsolidator(symbol, consolidator);
        }

        /// <summary>
        /// Will trigger past consolidator scans
        /// </summary>
        /// <param name="newUtcTime">The new utc time</param>
        /// <param name="algorithm">The algorithm instance</param>
        public void ScanPastConsolidators(DateTime newUtcTime, IAlgorithm algorithm)
        {
            while (_consolidatorsSortedByScanTime.TryPeek(out _, out var priority) && priority.UtcScanTime < newUtcTime)
            {
                var consolidatorToScan = _consolidatorsSortedByScanTime.Dequeue();
                if (consolidatorToScan.Disposed)
                {
                    // consolidator has been removed
                    continue;
                }

                if (priority.UtcScanTime != algorithm.UtcTime)
                {
                    // only update the algorithm time once, it's not cheap because of TZ conversions
                    algorithm.SetDateTime(priority.UtcScanTime);
                }

                if (consolidatorToScan.UtcScanTime <= priority.UtcScanTime)
                {
                    // only scan if we still need to
                    consolidatorToScan.Scan();
                }

                _consolidatorsSortedByScanTime.Enqueue(consolidatorToScan, consolidatorToScan.Priority);
            }
        }

        /// <summary>
        ///     Hard code the set of default available data feeds
        /// </summary>
        public static Dictionary<SecurityType, List<TickType>> DefaultDataTypes()
        {
            return new Dictionary<SecurityType, List<TickType>>
            {
                {SecurityType.Base, new List<TickType> {TickType.Trade}},
                {SecurityType.Index, new List<TickType> {TickType.Trade}},
                {SecurityType.Forex, new List<TickType> {TickType.Quote}},
                {SecurityType.Equity, new List<TickType> {TickType.Trade, TickType.Quote}},
                {SecurityType.Option, new List<TickType> {TickType.Quote, TickType.Trade, TickType.OpenInterest}},
                {SecurityType.FutureOption, new List<TickType> {TickType.Quote, TickType.Trade, TickType.OpenInterest}},
                {SecurityType.IndexOption, new List<TickType> {TickType.Quote, TickType.Trade, TickType.OpenInterest}},
                {SecurityType.Cfd, new List<TickType> {TickType.Quote}},
                {SecurityType.Future, new List<TickType> {TickType.Quote, TickType.Trade, TickType.OpenInterest}},
                {SecurityType.Commodity, new List<TickType> {TickType.Trade}},
                {SecurityType.Crypto, new List<TickType> {TickType.Trade, TickType.Quote}},
                {SecurityType.CryptoFuture, new List<TickType> {TickType.Trade, TickType.Quote}}
            };
        }

        /// <summary>
        ///     Get the available data types for a security
        /// </summary>
        public IReadOnlyList<TickType> GetDataTypesForSecurity(SecurityType securityType)
        {
            return AvailableDataTypes[securityType];
        }

        /// <summary>
        ///     Get the data feed types for a given <see cref="SecurityType" /> <see cref="Resolution" />
        /// </summary>
        /// <param name="symbolSecurityType">The <see cref="SecurityType" /> used to determine the types</param>
        /// <param name="resolution">The resolution of the data requested</param>
        /// <param name="isCanonical">Indicates whether the security is Canonical (future and options)</param>
        /// <returns>Types that should be added to the <see cref="SubscriptionDataConfig" /></returns>
        public List<Tuple<Type, TickType>> LookupSubscriptionConfigDataTypes(
            SecurityType symbolSecurityType,
            Resolution resolution,
            bool isCanonical
            )
        {
            return _subscriptionManager.LookupSubscriptionConfigDataTypes(symbolSecurityType, resolution, isCanonical);
        }

        /// <summary>
        ///     Sets the Subscription Manager
        /// </summary>
        public void SetDataManager(IAlgorithmSubscriptionManager subscriptionManager)
        {
            _subscriptionManager = subscriptionManager;
        }

        /// <summary>
        /// Checks if the subscription is valid for the consolidator
        /// </summary>
        /// <param name="subscription">The subscription configuration</param>
        /// <param name="consolidator">The consolidator</param>
        /// <param name="desiredTickType">The desired tick type for the subscription. If not given is null.</param>
        /// <returns>true if the subscription is valid for the consolidator</returns>
        public static bool IsSubscriptionValidForConsolidator(SubscriptionDataConfig subscription, IDataConsolidator consolidator, TickType? desiredTickType = null)
        {
            if (subscription.Type == typeof(Tick) &&
                LeanData.IsCommonLeanDataType(consolidator.OutputType))
            {
                if (desiredTickType == null)
                {
                    var tickType = LeanData.GetCommonTickTypeForCommonDataTypes(
                    consolidator.OutputType,
                    subscription.Symbol.SecurityType);

                    return subscription.TickType == tickType;
                }
                else if (subscription.TickType != desiredTickType)
                {
                    return false;
                }
            }

            return consolidator.InputType.IsAssignableFrom(subscription.Type);
        }

        /// <summary>
        /// Returns true if the provided data is the default data type associated with it's <see cref="SecurityType"/>.
        /// This is useful to determine if a data point should be used/cached in an environment where consumers will not provider a data type and we want to preserve
        /// determinism and backwards compatibility when there are multiple data types available per <see cref="SecurityType"/> or new ones added.
        /// </summary>
        /// <remarks>Temporary until we have a dictionary for the default data type per security type see GH issue 4196.
        /// Internal so it's only accessible from this assembly.</remarks>
        internal static bool IsDefaultDataType(BaseData data)
        {
            switch (data.Symbol.SecurityType)
            {
                case SecurityType.Equity:
                    if (data.DataType == MarketDataType.QuoteBar || data.DataType == MarketDataType.Tick && (data as Tick).TickType == TickType.Quote)
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }
    }
}
