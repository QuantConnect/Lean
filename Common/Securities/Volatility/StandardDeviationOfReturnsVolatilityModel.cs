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

using MathNet.Numerics.Statistics;

using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities.Volatility;

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
        private Resolution? _resolution;
        private TimeSpan _periodSpan;
        private readonly object _sync = new object();
        private RollingWindow<double> _window;

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
        /// <param name="periods">The max number of samples in the rolling window to be considered for calculating the standard deviation of returns</param>
        /// <param name="resolution">
        /// Resolution of the price data inserted into the rolling window series to calculate standard deviation.
        /// Will be used as the default value for update frequency if a value is not provided for <paramref name="updateFrequency"/>.
        /// This only has a material effect in live mode. For backtesting, this value does not cause any behavioral changes.
        /// </param>
        /// <param name="updateFrequency">Frequency at which we insert new values into the rolling window for the standard deviation calculation</param>
        /// <remarks>
        /// The volatility model will be updated with the most granular/highest resolution data that was added to your algorithm.
        /// That means that if I added <see cref="Resolution.Tick"/> data for my Futures strategy, that this model will be
        /// updated using <see cref="Resolution.Tick"/> data as the algorithm progresses in time.
        ///
        /// Keep this in mind when setting the period and update frequency. The Resolution parameter is only used for live mode, or for
        /// the default value of the <paramref name="updateFrequency"/> if no value is provided.
        /// </remarks>
        public StandardDeviationOfReturnsVolatilityModel(
            int periods,
            Resolution? resolution = null,
            TimeSpan? updateFrequency = null
            )
        {
            if (periods < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(periods), "'periods' must be greater than or equal to 2.");
            }

            _window = new RollingWindow<double>(periods);
            _resolution = resolution;
            _periodSpan = updateFrequency ?? resolution?.ToTimeSpan() ?? TimeSpan.FromDays(1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardDeviationOfReturnsVolatilityModel"/> class
        /// </summary>
        /// <param name="resolution">
        /// Resolution of the price data inserted into the rolling window series to calculate standard deviation.
        /// Will be used as the default value for update frequency if a value is not provided for <paramref name="updateFrequency"/>.
        /// This only has a material effect in live mode. For backtesting, this value does not cause any behavioral changes.
        /// </param>
        /// <param name="updateFrequency">Frequency at which we insert new values into the rolling window for the standard deviation calculation</param>
        /// <remarks>
        /// The volatility model will be updated with the most granular/highest resolution data that was added to your algorithm.
        /// That means that if I added <see cref="Resolution.Tick"/> data for my Futures strategy, that this model will be
        /// updated using <see cref="Resolution.Tick"/> data as the algorithm progresses in time.
        ///
        /// Keep this in mind when setting the period and update frequency. The Resolution parameter is only used for live mode, or for
        /// the default value of the <paramref name="updateFrequency"/> if no value is provided.
        /// </remarks>
        public StandardDeviationOfReturnsVolatilityModel(
            Resolution resolution,
            TimeSpan? updateFrequency = null
            ) : this(PeriodsInResolution(resolution), resolution, updateFrequency)
        {
        }

        /// <summary>
        /// Updates this model using the new price information in
        /// the specified security instance
        /// </summary>
        /// <param name="security">The security to calculate volatility for</param>
        /// <param name="data">Data to update the volatility model with</param>
        public override void Update(Security security, BaseData data)
        {
            var timeSinceLastUpdate = data.EndTime - _lastUpdate;
            if (timeSinceLastUpdate >= _periodSpan && data.Price > 0)
            {
                lock (_sync)
                {
                    // Update the last price applying the last price factor
                    if (LastFactor.HasValue)
                    {
                        _lastPrice *= LastFactor.Value;
                        LastFactor = null;
                    }

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
            return GetHistoryRequirements(
                security,
                utcTime,
                _resolution,
                _window.Size + 1);
        }

        private static int PeriodsInResolution(Resolution resolution)
        {
            int periods;
            switch (resolution)
            {
                case Resolution.Tick:
                case Resolution.Second:
                    periods = 600;
                    break;
                case Resolution.Minute:
                    periods = 60 * 24;
                    break;
                case Resolution.Hour:
                    periods = 24 * 30;
                    break;
                default:
                    periods = 30;
                    break;
            }

            return periods;
        }
    }
}
