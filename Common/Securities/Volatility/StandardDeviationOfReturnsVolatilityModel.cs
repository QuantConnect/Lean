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
using MathNet.Numerics.Statistics;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities.Volatility;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="IVolatilityModel"/> that computes the
    /// annualized sample standard deviation of daily returns as the volatility of the security
    /// </summary>
    public class StandardDeviationOfReturnsVolatilityModel : BaseVolatilityModel
    {
        private bool _needsUpdate;
        private decimal _volatility;
        private DateTime _lastUpdate = DateTime.MinValue;
        private decimal _lastPrice;
        private readonly TimeSpan _periodSpan = TimeSpan.FromDays(1);
        private readonly object _sync = new object();
        private readonly RollingWindow<double> _window;

        /// <summary>
        /// Gets the volatility of the security as a percentage
        /// </summary>
        public override decimal Volatility
        {
            get
            {
                lock (_sync)
                {
                    if (_window.Count < 2)
                    {
                        return 0m;
                    }

                    if (_needsUpdate)
                    {
                        _needsUpdate = false;
                        var std = _window.StandardDeviation().SafeDecimalCast();
                        _volatility = std * (decimal)Math.Sqrt(252.0);
                    }
                }
                return _volatility;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardDeviationOfReturnsVolatilityModel"/> class
        /// </summary>
        /// <param name="periods">The number of periods (days) to wait until updating the value</param>
        public StandardDeviationOfReturnsVolatilityModel(int periods)
        {
            if (periods < 2) throw new ArgumentOutOfRangeException("periods", "'periods' must be greater than or equal to 2.");
            _window = new RollingWindow<double>(periods);
        }

        /// <summary>
        /// Updates this model using the new price information in
        /// the specified security instance
        /// </summary>
        /// <param name="security">The security to calculate volatility for</param>
        /// <param name="data"></param>
        public override void Update(Security security, BaseData data)
        {
            var timeSinceLastUpdate = data.EndTime - _lastUpdate;
            if (timeSinceLastUpdate >= _periodSpan && data.Price > 0)
            {
                lock (_sync)
                {
                    if (_lastPrice > 0.0m)
                    {
                        _needsUpdate = true;
                        _window.Add((double)(data.Price / _lastPrice) - 1.0);
                    }
                }
                _lastUpdate = data.EndTime;
                _lastPrice = data.Price;
            }
        }

        /// <summary>
        /// Returns history requirements for the volatility model expressed in the form of history request
        /// </summary>
        /// <param name="security">The security of the request</param>
        /// <param name="utcTime">The date of the request</param>
        /// <returns>History request object list, or empty if no requirements</returns>
        public override IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        {
            if (SubscriptionDataConfigProvider == null)
            {
                throw new InvalidOperationException(
                    "RelativeStandardDeviationVolatilityModel.GetHistoryRequirements(): " +
                    "SubscriptionDataConfigProvider was not set."
                );
            }

            var configurations = SubscriptionDataConfigProvider
                .GetSubscriptionDataConfigs(security.Symbol)
                .ToList();
            var configuration = configurations.First();

            var barCount = _window.Size + 1;
            var extendedMarketHours = configurations.IsExtendedMarketHours();
            var localStartTime = Time.GetStartTimeForTradeBars(
                security.Exchange.Hours,
                utcTime.ConvertFromUtc(security.Exchange.TimeZone),
                _periodSpan,
                barCount,
                extendedMarketHours,
                configuration.DataTimeZone);
            var utcStartTime = localStartTime.ConvertToUtc(security.Exchange.TimeZone);

            return new[]
            {
                new HistoryRequest(utcStartTime,
                                   utcTime,
                                   typeof(TradeBar),
                                   configuration.Symbol,
                                   Resolution.Daily,
                                   security.Exchange.Hours,
                                   configuration.DataTimeZone,
                                   Resolution.Daily,
                                   extendedMarketHours,
                                   configurations.IsCustomData(),
                                   configurations.DataNormalizationMode(),
                                   LeanData.GetCommonTickTypeForCommonDataTypes(typeof(TradeBar), security.Type))
            };
        }
    }
}
