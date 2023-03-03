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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Securities.Volatility
{
    /// <summary>
    /// Represents a base model that computes the volatility of a security
    /// </summary>
    public class BaseVolatilityModel : IVolatilityModel
    {
        private List<Dividend> _dividends = new();
        private List<Split> _splits = new();

        /// <summary>
        /// Provides access to registered <see cref="SubscriptionDataConfig"/>
        /// </summary>
        protected ISubscriptionDataConfigProvider SubscriptionDataConfigProvider { get; set; }

        /// <summary>
        /// Gets the volatility of the security as a percentage
        /// </summary>
        public virtual decimal Volatility { get; protected set; }

        /// <summary>
        /// Sets the <see cref="ISubscriptionDataConfigProvider"/> instance to use.
        /// </summary>
        /// <param name="subscriptionDataConfigProvider">Provides access to registered <see cref="SubscriptionDataConfig"/></param>
        public virtual void SetSubscriptionDataConfigProvider(
            ISubscriptionDataConfigProvider subscriptionDataConfigProvider)
        {
            SubscriptionDataConfigProvider = subscriptionDataConfigProvider;
        }

        /// <summary>
        /// Updates this model using the new price information in
        /// the specified security instance
        /// </summary>
        /// <param name="security">The security to calculate volatility for</param>
        /// <param name="data">The new data used to update the model</param>
        public virtual void Update(Security security, BaseData data)
        {
        }

        /// <summary>
        /// Returns history requirements for the volatility model expressed in the form of history request
        /// </summary>
        /// <param name="security">The security of the request</param>
        /// <param name="utcTime">The date/time of the request</param>
        /// <returns>History request object list, or empty if no requirements</returns>
        public virtual IEnumerable<HistoryRequest> GetHistoryRequirements(
            Security security,
            DateTime utcTime
            )
        {
            return Enumerable.Empty<HistoryRequest>();
        }

        /// <summary>
        /// Gets history requests required for warming up the greeks with the provided resolution
        /// </summary>
        /// <param name="security">Security to get history for</param>
        /// <param name="utcTime">UTC time of the request (end time)</param>
        /// <param name="resolution">Resolution of the security</param>
        /// <param name="barCount">Number of bars to lookback for the start date</param>
        /// <returns>Enumerable of history requests</returns>
        /// <exception cref="InvalidOperationException">The <see cref="SubscriptionDataConfigProvider"/> has not been set</exception>
        public IEnumerable<HistoryRequest> GetHistoryRequirements(
            Security security,
            DateTime utcTime,
            Resolution? resolution,
            int barCount)
        {
            if (SubscriptionDataConfigProvider == null)
            {
                throw new InvalidOperationException(
                    "BaseVolatilityModel.GetHistoryRequirements(): " +
                    "SubscriptionDataConfigProvider was not set."
                );
            }

            var configurations = SubscriptionDataConfigProvider
                .GetSubscriptionDataConfigs(security.Symbol)
                .OrderBy(c => c.TickType)
                .ToList();
            var configuration = configurations.First();

            var bar = configuration.Type.GetBaseDataInstance();
            bar.Symbol = security.Symbol;

            var historyResolution = resolution ?? bar.SupportedResolutions().Max();

            var periodSpan = historyResolution.ToTimeSpan();

            // hour resolution does no have extended market hours data
            var extendedMarketHours = periodSpan != Time.OneHour && configurations.IsExtendedMarketHours();
            var localStartTime = Time.GetStartTimeForTradeBars(
                security.Exchange.Hours,
                utcTime.ConvertFromUtc(security.Exchange.TimeZone),
                periodSpan,
                barCount,
                extendedMarketHours,
                configuration.DataTimeZone);
            var utcStartTime = localStartTime.ConvertToUtc(security.Exchange.TimeZone);

            return new[]
            {
                new HistoryRequest(utcStartTime,
                                   utcTime,
                                   configuration.Type,
                                   configuration.Symbol,
                                   historyResolution,
                                   security.Exchange.Hours,
                                   configuration.DataTimeZone,
                                   historyResolution,
                                   extendedMarketHours,
                                   configurations.IsCustomData(),
                                   DataNormalizationMode.Adjusted,
                                   LeanData.GetCommonTickTypeForCommonDataTypes(configuration.Type, security.Type))
            };
        }

        /// <summary>
        /// Applies a dividend to the model
        /// </summary>
        /// <param name="dividend">The dividend to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="dataNormalizationMode">The <see cref="DataNormalizationMode"/> for the security</param>
        public virtual void ApplyDividend(Dividend dividend, bool liveMode, DataNormalizationMode dataNormalizationMode)
        {
            // only apply splits in live or raw/split adjusted data mode
            if (!liveMode && !(dataNormalizationMode == DataNormalizationMode.Raw || dataNormalizationMode == DataNormalizationMode.SplitAdjusted))
            {
                return;
            }

            _dividends.Add(dividend);
        }

        /// <summary>
        /// Applies a split to the model
        /// </summary>
        /// <param name="split">The split to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="dataNormalizationMode">The <see cref="DataNormalizationMode"/> for the security</param>
        public virtual void ApplySplit(Split split, bool liveMode, DataNormalizationMode dataNormalizationMode)
        {
            // only apply splits in live or raw data mode
            if (!liveMode && dataNormalizationMode != DataNormalizationMode.Raw)
            {
                return;
            }

            _splits.Add(split);
        }

        /// <summary>
        /// Resets and warms up the model using historical data
        /// </summary>
        /// <param name="historyProvider">History provider to use to get historical data</param>
        /// <param name="security">The security of the request</param>
        /// <param name="utcTime">The date/time of the request</param>
        /// <param name="timeZone">The algorithm time zone</param>
        public void WarmUp(IHistoryProvider historyProvider, Security security, DateTime utcTime, DateTimeZone timeZone)
        {
            // Reset
            Reset();

            // Warm up
            var historyRequests = GetHistoryRequirements(security, utcTime).ToList();
            if (historyRequests == null)
            {
                return;
            }

            var history = historyProvider.GetHistory(historyRequests, timeZone);
            var data = history.Get(historyRequests[0].DataType, security.Symbol).Cast<BaseData>().ToList();
            if (data.Count == 0)
            {
                return;
            }

            var firstTime = data[0].Time;
            // We don't need dividends and splits before the first history slice
            _dividends.RemoveAll(x => x.Time < firstTime);
            _splits.RemoveAll(x => x.Time < firstTime);

            var factor = _dividends.Aggregate(1m, (current, dividend) => current * (1 - dividend.Distribution / dividend.ReferencePrice)) *
                _splits.Aggregate(1m, (current, split) => current * split.SplitFactor);

            foreach (BaseData dataPoint in data)
            {
                Update(security, factor == 1 ? dataPoint : dataPoint.Normalize(factor, DataNormalizationMode.Adjusted, 0));
            }
        }

        /// <summary>
        /// Resets the model to its initial state
        /// </summary>
        public virtual void Reset()
        {
            Volatility = 0;
        }
    }
}
