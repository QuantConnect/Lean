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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Helper class used to create new <see cref="HistoryRequest"/>
    /// </summary>
    public class HistoryRequestFactory
    {
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="algorithm">The algorithm instance to use</param>
        public HistoryRequestFactory(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Creates a new history request
        /// </summary>
        /// <param name="subscription">The config </param>
        /// <param name="startAlgoTz">History request start time in algorithm time zone</param>
        /// <param name="endAlgoTz">History request end time in algorithm time zone</param>
        /// <param name="exchangeHours">Security exchange hours</param>
        /// <param name="resolution">The resolution to use. If null will use <see cref="SubscriptionDataConfig.Resolution"/></param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <returns>The new <see cref="HistoryRequest"/></returns>
        public HistoryRequest CreateHistoryRequest(SubscriptionDataConfig subscription,
            DateTime startAlgoTz,
            DateTime endAlgoTz,
            SecurityExchangeHours exchangeHours,
            Resolution? resolution,
            bool? fillForward = null,
            bool? extendedMarketHours = null,
            DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null)
        {
            resolution ??= subscription.Resolution;

            var dataType = subscription.Type;

            // if we change resolution the data type can change, for example subscription being Tick type and resolution daily
            // data type here won't be Tick anymore, but TradeBar/QuoteBar
            if (resolution.Value != subscription.Resolution && LeanData.IsCommonLeanDataType(dataType))
            {
                dataType = LeanData.GetDataType(resolution.Value, subscription.TickType);
            }

            var fillForwardResolution = subscription.FillDataForward ? resolution : null;
            if (fillForward != null)
            {
                fillForwardResolution = fillForward.Value ? resolution : null;
            }

            var request = new HistoryRequest(subscription,
                exchangeHours,
                startAlgoTz.ConvertToUtc(_algorithm.TimeZone),
                endAlgoTz.ConvertToUtc(_algorithm.TimeZone))
            {
                DataType = dataType,
                Resolution = resolution.Value,
                FillForwardResolution = fillForwardResolution,
                TickType = subscription.TickType
            };

            if (extendedMarketHours != null)
            {
                request.IncludeExtendedMarketHours = extendedMarketHours.Value;
            }

            if (dataMappingMode != null)
            {
                request.DataMappingMode = dataMappingMode.Value;
            }

            if (dataNormalizationMode != null)
            {
                request.DataNormalizationMode = dataNormalizationMode.Value;
            }

            if (contractDepthOffset != null)
            {
                request.ContractDepthOffset = (uint)Math.Abs(contractDepthOffset.Value);
            }

            return request;
        }

        /// <summary>
        /// Gets the start time required for the specified bar count in terms of the algorithm's time zone
        /// </summary>
        /// <param name="symbol">The symbol to select proper <see cref="SubscriptionDataConfig"/> config</param>
        /// <param name="periods">The number of bars requested</param>
        /// <param name="resolution">The length of each bar</param>
        /// <param name="exchange">The exchange hours used for market open hours</param>
        /// <param name="dataTimeZone">The time zone in which data are stored</param>
        /// <param name="dataType">The data type to request</param>
        /// <param name="extendedMarketHours">
        /// True to include extended market hours data, false otherwise.
        /// If not passed, the config will be used to determined whether to include extended market hours.
        /// </param>
        /// <returns>The start time that would provide the specified number of bars ending at the algorithm's current time</returns>
        public DateTime GetStartTimeAlgoTz(
            Symbol symbol,
            int periods,
            Resolution resolution,
            SecurityExchangeHours exchange,
            DateTimeZone dataTimeZone,
            Type dataType,
            bool? extendedMarketHours = null)
        {
            return GetStartTimeAlgoTz(_algorithm.UtcTime, symbol, periods, resolution, exchange, dataTimeZone, dataType, extendedMarketHours);
        }

        /// <summary>
        /// Gets the start time required for the specified bar count in terms of the algorithm's time zone
        /// </summary>
        /// <param name="referenceUtcTime">The end time in utc</param>
        /// <param name="symbol">The symbol to select proper <see cref="SubscriptionDataConfig"/> config</param>
        /// <param name="periods">The number of bars requested</param>
        /// <param name="resolution">The length of each bar</param>
        /// <param name="exchange">The exchange hours used for market open hours</param>
        /// <param name="dataTimeZone">The time zone in which data are stored</param>
        /// <param name="dataType">The data type to request</param>
        /// <param name="extendedMarketHours">
        /// True to include extended market hours data, false otherwise.
        /// If not passed, the config will be used to determined whether to include extended market hours.
        /// </param>
        /// <returns>The start time that would provide the specified number of bars ending at the algorithm's current time</returns>
        public DateTime GetStartTimeAlgoTz(
            DateTime referenceUtcTime,
            Symbol symbol,
            int periods,
            Resolution resolution,
            SecurityExchangeHours exchange,
            DateTimeZone dataTimeZone,
            Type dataType,
            bool? extendedMarketHours = null)
        {
            var isExtendedMarketHours = false;
            // hour resolution does no have extended market hours data. Same for chain universes
            if (resolution != Resolution.Hour && LeanData.SupportsExtendedMarketHours(dataType))
            {
                if (extendedMarketHours.HasValue)
                {
                    isExtendedMarketHours = extendedMarketHours.Value;
                }
                else
                {
                    var configs = _algorithm.SubscriptionManager
                        .SubscriptionDataConfigService
                        .GetSubscriptionDataConfigs(symbol);
                    isExtendedMarketHours = configs.IsExtendedMarketHours();
                }
            }

            var timeSpan = resolution.ToTimeSpan();
            // make this a minimum of one second
            timeSpan = timeSpan < Time.OneSecond ? Time.OneSecond : timeSpan;

            var localStartTime = Time.GetStartTimeForTradeBars(
                exchange,
                referenceUtcTime.ConvertFromUtc(exchange.TimeZone),
                timeSpan,
                periods,
                isExtendedMarketHours,
                dataTimeZone,
                LeanData.UseDailyStrictEndTimes(_algorithm.Settings, dataType, symbol, timeSpan, exchange));
            return localStartTime.ConvertTo(exchange.TimeZone, _algorithm.TimeZone);
        }
    }
}
