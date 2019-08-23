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
        /// <returns>The new <see cref="HistoryRequest"/></returns>
        public HistoryRequest CreateHistoryRequest(SubscriptionDataConfig subscription,
            DateTime startAlgoTz,
            DateTime endAlgoTz,
            SecurityExchangeHours exchangeHours,
            Resolution? resolution)
        {
            resolution = resolution ?? subscription.Resolution;

            // find the correct data type for the history request
            var dataType = subscription.IsCustomData ? subscription.Type : LeanData.GetDataType(resolution.Value, subscription.TickType);

            var request = new HistoryRequest(subscription,
                exchangeHours,
                startAlgoTz.ConvertToUtc(_algorithm.TimeZone),
                endAlgoTz.ConvertToUtc(_algorithm.TimeZone))
            {
                DataType = dataType,
                Resolution = resolution.Value,
                FillForwardResolution = subscription.FillDataForward ? resolution : null,
                TickType = subscription.TickType
            };

            return request;
        }


        /// <summary>
        /// Gets the start time required for the specified bar count in terms of the algorithm's time zone
        /// </summary>
        /// <param name="symbol">The symbol to select proper <see cref="SubscriptionDataConfig"/> config</param>
        /// <param name="periods">The number of bars requested</param>
        /// <param name="resolution">The length of each bar</param>
        /// <param name="exchange">The exchange hours used for market open hours</param>
        /// <returns>The start time that would provide the specified number of bars ending at the algorithm's current time</returns>
        public DateTime GetStartTimeAlgoTz(
            Symbol symbol,
            int periods,
            Resolution resolution,
            SecurityExchangeHours exchange)
        {
            var isExtendedMarketHours = _algorithm.SubscriptionManager
                .SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(symbol)
                .IsExtendedMarketHours();

            var timeSpan = resolution.ToTimeSpan();
            // make this a minimum of one second
            timeSpan = timeSpan < Time.OneSecond ? Time.OneSecond : timeSpan;

            var localStartTime = Time.GetStartTimeForTradeBars(
                exchange,
                _algorithm.UtcTime.ConvertFromUtc(exchange.TimeZone),
                timeSpan,
                periods,
                isExtendedMarketHours);
            return localStartTime.ConvertTo(exchange.TimeZone, _algorithm.TimeZone);
        }
    }
}