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

using NodaTime;

using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Securities.Volatility
{
    /// <summary>
    /// Provides extension methods to volatility models
    /// </summary>
    public static class VolatilityModelExtensions
    {
        /// <summary>
        /// Warms up the security's volatility model.
        /// This can happen either on initialization or after a split or dividend is processed.
        /// </summary>
        /// <param name="volatilityModel">The volatility model to be warmed up</param>
        /// <param name="historyProvider">The history provider to use to get historical data</param>
        /// <param name="subscriptionManager">The subscription manager to use</param>
        /// <param name="security">The security which volatility model is being warmed up</param>
        /// <param name="utcTime">The current UTC time</param>
        /// <param name="timeZone">The algorithm time zone</param>
        /// <param name="liveMode">Whether the algorithm is in live mode</param>
        /// <param name="dataNormalizationMode">The security subscribed data normalization mode</param>
        public static void WarmUp(
            this IVolatilityModel volatilityModel,
            IHistoryProvider historyProvider,
            SubscriptionManager subscriptionManager,
            Security security,
            DateTime utcTime,
            DateTimeZone timeZone,
            bool liveMode,
            DataNormalizationMode? dataNormalizationMode = null)
        {
            volatilityModel.WarmUp(
                historyProvider,
                subscriptionManager,
                security,
                timeZone,
                liveMode,
                dataNormalizationMode,
                () => volatilityModel.GetHistoryRequirements(security, utcTime));
        }

        /// <summary>
        /// Warms up the security's volatility model.
        /// This can happen either on initialization or after a split or dividend is processed.
        /// </summary>
        /// <param name="volatilityModel">The volatility model to be warmed up</param>
        /// <param name="historyProvider">The history provider to use to get historical data</param>
        /// <param name="subscriptionManager">The subscription manager to use</param>
        /// <param name="security">The security which volatility model is being warmed up</param>
        /// <param name="utcTime">The current UTC time</param>
        /// <param name="timeZone">The algorithm time zone</param>
        /// <param name="resolution">The data resolution required for the indicator</param>
        /// <param name="barCount">The bar count required to fully warm the indicator up</param>
        /// <param name="liveMode">Whether the algorithm is in live mode</param>
        /// <param name="dataNormalizationMode">The security subscribed data normalization mode</param>
        public static void WarmUp(
            this IndicatorVolatilityModel volatilityModel,
            IHistoryProvider historyProvider,
            SubscriptionManager subscriptionManager,
            Security security,
            DateTime utcTime,
            DateTimeZone timeZone,
            Resolution? resolution,
            int barCount,
            bool liveMode,
            DataNormalizationMode? dataNormalizationMode = null)
        {
            volatilityModel.WarmUp(
                historyProvider,
                subscriptionManager,
                security,
                timeZone,
                liveMode,
                dataNormalizationMode,
                () => volatilityModel.GetHistoryRequirements(security, utcTime, resolution, barCount));
        }

        /// <summary>
        /// Warms up the security's volatility model.
        /// This can happen either on initialization or after a split or dividend is processed.
        /// </summary>
        /// <param name="volatilityModel">The volatility model to be warmed up</param>
        /// <param name="algorithm">The algorithm running</param>
        /// <param name="security">The security which volatility model is being warmed up</param>
        /// <param name="resolution">The data resolution required for the indicator</param>
        /// <param name="barCount">The bar count required to fully warm the indicator up</param>
        /// <param name="dataNormalizationMode">The security subscribed data normalization mode</param>
        public static void WarmUp(
            this IndicatorVolatilityModel volatilityModel,
            IAlgorithm algorithm,
            Security security,
            Resolution? resolution,
            int barCount,
            DataNormalizationMode? dataNormalizationMode = null)
        {
            volatilityModel.WarmUp(
                algorithm.HistoryProvider,
                algorithm.SubscriptionManager,
                security,
                algorithm.UtcTime,
                algorithm.TimeZone,
                resolution,
                barCount,
                algorithm.LiveMode,
                dataNormalizationMode);
        }

        private static void WarmUp(
            this IVolatilityModel volatilityModel,
            IHistoryProvider historyProvider,
            SubscriptionManager subscriptionManager,
            Security security,
            DateTimeZone timeZone,
            bool liveMode,
            DataNormalizationMode? dataNormalizationMode,
            Func<IEnumerable<HistoryRequest>> getHistoryRequirementsFunc)
        {
            if (historyProvider == null || security == null || volatilityModel == VolatilityModel.Null)
            {
                return;
            }

            // start: this is a work around to maintain retro compatibility
            // did not want to add IVolatilityModel.SetSubscriptionDataConfigProvider
            // to prevent breaking existing user models.
            var baseTypeModel = volatilityModel as BaseVolatilityModel;
            baseTypeModel?.SetSubscriptionDataConfigProvider(subscriptionManager.SubscriptionDataConfigService);
            // end

            // Warm up
            var historyRequests = getHistoryRequirementsFunc().ToList();
            if (liveMode || (dataNormalizationMode.HasValue && dataNormalizationMode == DataNormalizationMode.Raw))
            {
                // If we're in live mode or raw mode, we need to warm up the volatility model with scaled raw data
                // to avoid jumps in volatility values due to price discontinuities on splits and dividends
                foreach (var request in historyRequests)
                {
                    request.DataNormalizationMode = DataNormalizationMode.ScaledRaw;
                }
            }

            var history = historyProvider.GetHistory(historyRequests, timeZone);
            foreach (var slice in history)
            {
                foreach (var request in historyRequests)
                {
                    if (slice.TryGet(request.DataType, security.Symbol, out var data))
                    {
                        volatilityModel.Update(security, data);
                    }
                }
            }
        }
    }
}
