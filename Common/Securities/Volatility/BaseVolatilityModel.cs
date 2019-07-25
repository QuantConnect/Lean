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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Securities.Volatility
{
    /// <summary>
    /// Represents a base model that computes the volatility of a security
    /// </summary>
    public class BaseVolatilityModel : IVolatilityModel
    {
        /// <summary>
        /// Provides access to registered <see cref="SubscriptionDataConfig"/>
        /// </summary>
        protected ISubscriptionDataConfigProvider SubscriptionDataConfigProvider;

        /// <summary>
        /// Gets the volatility of the security as a percentage
        /// </summary>
        public virtual decimal Volatility { get; } = 0m;

        /// <summary>
        /// Latest price factor to be applied
        /// </summary>
        public decimal? LastFactor { get; private set; }

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
        /// Applies a dividend to the portfolio
        /// </summary>
        /// <param name="dividend">The dividend to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="mode">The <see cref="DataNormalizationMode"/> for this security</param>
        public virtual void ApplyDividend(Dividend dividend, bool liveMode, DataNormalizationMode mode)
        {
            // only apply splits in live or raw data mode
            if (!liveMode && !(mode == DataNormalizationMode.Raw || mode == DataNormalizationMode.SplitAdjusted))
            {
                return;
            }

            var factor = 1 - dividend.Distribution / dividend.ReferencePrice;
            SetLastFactor(factor);
        }

        /// <summary>
        /// Applies a split to the model
        /// </summary>
        /// <param name="split">The split to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="mode">The <see cref="DataNormalizationMode"/> for this security</param>
        public virtual void ApplySplit(Split split, bool liveMode, DataNormalizationMode mode)
        {
            // only apply splits in live or raw data mode
            if (!liveMode && mode != DataNormalizationMode.Raw)
            {
                return;
            }

            SetLastFactor(split.SplitFactor);
        }

        /// <summary>
        /// Sets the value of the latest price factor
        /// </summary>
        /// <param name="factor">Latest price factor to be applied</param>
        public void SetLastFactor(decimal? factor)
        {
            if (!LastFactor.HasValue || !factor.HasValue)
            {
                LastFactor = factor;
                return;
            }

            LastFactor *= factor;
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
    }
}
