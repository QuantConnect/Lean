
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
    /// relative standard deviation as the volatility of the security
    /// </summary>
    public class RelativeStandardDeviationVolatilityModel : BaseVolatilityModel
    {
        private bool _needsUpdate;
        private decimal _volatility;
        private DateTime _lastUpdate;
        private readonly TimeSpan _periodSpan;
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
                        var mean = Math.Abs(_window.Mean().SafeDecimalCast());
                        if (mean != 0m)
                        {
                            // volatility here is supposed to be a percentage
                            var std = _window.StandardDeviation().SafeDecimalCast();
                            _volatility = std / mean;
                        }
                    }
                }
                return _volatility;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeStandardDeviationVolatilityModel"/> class
        /// </summary>
        /// <param name="periodSpan">The time span representing one 'period' length</param>
        /// <param name="periods">The nuber of 'period' lengths to wait until updating the value</param>
        public RelativeStandardDeviationVolatilityModel(
            TimeSpan periodSpan,
            int periods)
        {
            if (periods < 2) throw new ArgumentOutOfRangeException("periods", "'periods' must be greater than or equal to 2.");
            _periodSpan = periodSpan;
            _window = new RollingWindow<double>(periods);
            _lastUpdate = DateTime.MinValue + TimeSpan.FromMilliseconds(periodSpan.TotalMilliseconds * periods);
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
            if (timeSinceLastUpdate >= _periodSpan && security.Price > 0)
            {
                lock (_sync)
                {
                    _needsUpdate = true;
                    // we purposefully use security.Price for consistency in our reporting
                    // some streams of data will have trade/quote data, so if we just use
                    // data.Value we could be mixing and matching data streams
                    _window.Add((double)security.Price);
                }
                _lastUpdate = data.EndTime;
            }
        }

        /// <summary>
        /// Returns history requirements for the volatility model expressed in the form of history request
        /// </summary>
        /// <param name="security">The security of the request</param>
        /// <param name="utcTime">The date/time of the request</param>
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

            var barCount = _window.Size + 1;
            // hour resolution does no have extended market hours data
            var extendedMarketHours = _periodSpan != Time.OneHour && configurations.IsExtendedMarketHours();
            var configuration = configurations.First();

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
                                   configurations.GetHighestResolution(),
                                   security.Exchange.Hours,
                                   configuration.DataTimeZone,
                                   configurations.GetHighestResolution(),
                                   extendedMarketHours,
                                   configurations.IsCustomData(),
                                   configurations.DataNormalizationMode(),
                                   LeanData.GetCommonTickTypeForCommonDataTypes(typeof(TradeBar), security.Type))
            };
        }
    }
}