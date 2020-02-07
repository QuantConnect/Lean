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

namespace QuantConnect.Data.Custom
{
    /// <summary>
    /// Represents a custom data type that works as a heartbeat of data in live mode
    /// </summary>
    public class NullData : BaseData
    {
        /// <inheritdoc/>
        public override DateTime EndTime { get; set; }

        /// <inheritdoc/>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource("http://localhost/", SubscriptionTransportMedium.Rest);
        }

        /// <summary>
        /// Returns a new instance of the <see cref="NullData"/>. Its Value property is always 1
        /// and the Time property is the current date/time of the exchange.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Line of the source document.</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">True if we're in live mode</param>
        /// <returns>Instance of <see cref="NullData"/></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var nullData = new NullData
            {
                Symbol = config.Symbol,
                Time = DateTime.UtcNow.ConvertFromUtc(config.ExchangeTimeZone),
                Value = 1
            };

            nullData.EndTime = nullData.Time + config.Resolution.ToTimeSpan();
            return nullData;
        }
    }
}