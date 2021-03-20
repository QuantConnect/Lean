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
using QuantConnect.Securities;

namespace QuantConnect.Data
{
    /// <summary>
    /// Abstract sharing logic for data requests
    /// </summary>
    public abstract class BaseDataRequest
    {
        private readonly Lazy<DateTime> _localStartTime;
        private readonly Lazy<DateTime> _localEndTime;

        /// <summary>
        /// Gets the beginning of the requested time interval in UTC
        /// </summary>
        public DateTime StartTimeUtc { get; protected set; }

        /// <summary>
        /// Gets the end of the requested time interval in UTC
        /// </summary>
        public DateTime EndTimeUtc { get; protected set;  }

        /// <summary>
        /// Gets the <see cref="StartTimeUtc"/> in the security's exchange time zone
        /// </summary>
        public DateTime StartTimeLocal => _localStartTime.Value;

        /// <summary>
        /// Gets the <see cref="EndTimeUtc"/> in the security's exchange time zone
        /// </summary>
        public DateTime EndTimeLocal => _localEndTime.Value;

        /// <summary>
        /// Gets the exchange hours used for processing fill forward requests
        /// </summary>
        public SecurityExchangeHours ExchangeHours { get; }

        /// <summary>
        /// Initializes the base data request
        /// </summary>
        /// <param name="startTimeUtc">The start time for this request,</param>
        /// <param name="endTimeUtc">The start time for this request</param>
        /// <param name="exchangeHours">The exchange hours for this request</param>
        /// <param name="tickType">The tick type of this request</param>
        protected  BaseDataRequest(DateTime startTimeUtc,
            DateTime endTimeUtc,
            SecurityExchangeHours exchangeHours,
            TickType tickType)
        {
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
            ExchangeHours = exchangeHours;

            // open interest data comes in once a day before market open,
            // make the subscription start from midnight and use always open exchange
            if (tickType == TickType.OpenInterest)
            {
                ExchangeHours = SecurityExchangeHours.AlwaysOpen(ExchangeHours.TimeZone);
            }

            _localStartTime = new Lazy<DateTime>(() => StartTimeUtc.ConvertFromUtc(ExchangeHours.TimeZone));
            _localEndTime = new Lazy<DateTime>(() => EndTimeUtc.ConvertFromUtc(ExchangeHours.TimeZone));
        }
    }
}
