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
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// DTO for storing data and the time at which it should be synchronized
    /// </summary>
    public class SubscriptionData
    {
        /// <summary>
        /// Gets the data
        /// </summary>
        public BaseData Data { get; }

        /// <summary>
        /// Gets the UTC emit time for this data
        /// </summary>
        public DateTime EmitTimeUtc { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionData"/> class
        /// </summary>
        /// <param name="data">The base data</param>
        /// <param name="emitTimeUtc">The emit time for the data</param>
        public SubscriptionData(BaseData data, DateTime emitTimeUtc)
        {
            Data = data;
            EmitTimeUtc = emitTimeUtc;
        }

        /// <summary>
        /// Clones the data, computes the utc emit time and performs exchange round down behavior, storing the result in a new <see cref="SubscriptionData"/> instance
        /// </summary>
        /// <param name="configuration">The subscription's configuration</param>
        /// <param name="exchangeHours">The exchange hours of the security</param>
        /// <param name="offsetProvider">The subscription's offset provider</param>
        /// <param name="data">The data being emitted</param>
        /// <returns>A new <see cref="SubscriptionData"/> containing the specified data</returns>
        public static SubscriptionData Create(SubscriptionDataConfig configuration, SecurityExchangeHours exchangeHours, TimeZoneOffsetProvider offsetProvider, BaseData data)
        {
            if (data == null)
            {
                return null;
            }

            data = data.Clone(data.IsFillForward);
            var emitTimeUtc = offsetProvider.ConvertToUtc(data.EndTime);
            data.Time = data.Time.ExchangeRoundDownInTimeZone(configuration.Increment, exchangeHours, configuration.DataTimeZone, configuration.ExtendedMarketHours);
            return new SubscriptionData(data, emitTimeUtc);
        }
    }
}