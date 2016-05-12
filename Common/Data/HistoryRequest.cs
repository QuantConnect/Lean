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
 *
*/

using System;
using NodaTime;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents a request for historical data
    /// </summary>
    public class HistoryRequest
    {
        /// <summary>
        /// Gets the start time of the request.
        /// </summary>
        public DateTime StartTimeUtc { get; set; }
        /// <summary>
        /// Gets the end time of the request. 
        /// </summary>
        public DateTime EndTimeUtc { get; set; }
        /// <summary>
        /// Gets the symbol to request data for
        /// </summary>
        public Symbol Symbol { get; set; }
        /// <summary>
        /// Gets the exchange hours used for processing fill forward requests
        /// </summary>
        public SecurityExchangeHours ExchangeHours { get; set; }
        /// <summary>
        /// Gets the requested data resolution
        /// </summary>
        public Resolution Resolution { get; set; }
        /// <summary>
        /// Gets the requested fill forward resolution, set to null for no fill forward behavior
        /// </summary>
        public Resolution? FillForwardResolution { get; set; }
        /// <summary>
        /// Gets whether or not to include extended market hours data, set to false for only normal market hours
        /// </summary>
        public bool IncludeExtendedMarketHours { get; set; }
        /// <summary>
        /// Gets the data type used to process the subscription request, this type must derive from BaseData
        /// </summary>
        public Type DataType { get; set; }
        /// <summary>
        /// Gets the security type of the subscription
        /// </summary>
        public SecurityType SecurityType { get; set; }
        /// <summary>
        /// Gets the time zone of the time stamps on the raw input data
        /// </summary>
        public DateTimeZone TimeZone { get; set; }
        /// <summary>
        /// Gets the market for this subscription
        /// </summary>
        public string Market { get; set; }
        /// <summary>
        /// Gets true if this is a custom data request, false for normal QC data
        /// </summary>
        public bool IsCustomData { get; set; }

        /// <summary>
        /// Initializes a new default instance of the <see cref="HistoryRequest"/> class
        /// </summary>
        public HistoryRequest()
        {
            StartTimeUtc = EndTimeUtc = DateTime.UtcNow;
            Symbol = Symbol.Empty;
            ExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            Resolution = Resolution.Minute;
            FillForwardResolution = Resolution.Minute;
            IncludeExtendedMarketHours = false;
            DataType = typeof (TradeBar);
            SecurityType = SecurityType.Equity;
            TimeZone = TimeZones.NewYork;
            Market = QuantConnect.Market.USA;
            IsCustomData = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRequest"/> class from the specified parameters
        /// </summary>
        /// <param name="startTimeUtc">The start time for this request,</param>
        /// <param name="endTimeUtc">The start time for this request</param>
        /// <param name="dataType">The data type of the output data</param>
        /// <param name="symbol">The symbol to request data for</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="resolution">The requested data resolution</param>
        /// <param name="market">The market this data belongs to</param>
        /// <param name="exchangeHours">The exchange hours used in fill forward processing</param>
        /// <param name="fillForwardResolution">The requested fill forward resolution for this request</param>
        /// <param name="includeExtendedMarketHours">True to include data from pre/post market hours</param>
        /// <param name="isCustomData">True for custom user data, false for normal QC data</param>
        public HistoryRequest(DateTime startTimeUtc, 
            DateTime endTimeUtc,
            Type dataType,
            Symbol symbol,
            SecurityType securityType,
            Resolution resolution,
            string market,
            SecurityExchangeHours exchangeHours,
            Resolution? fillForwardResolution,
            bool includeExtendedMarketHours,
            bool isCustomData
            )
        {
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
            Symbol = symbol;
            ExchangeHours = exchangeHours;
            Resolution = resolution;
            FillForwardResolution = fillForwardResolution;
            IncludeExtendedMarketHours = includeExtendedMarketHours;
            DataType = dataType;
            SecurityType = securityType;
            Market = market;
            IsCustomData = isCustomData;
            TimeZone = exchangeHours.TimeZone;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRequest"/> class from the specified config and exchange hours
        /// </summary>
        /// <param name="config">The subscription data config used to initalize this request</param>
        /// <param name="hours">The exchange hours used for fill forward processing</param>
        /// <param name="startTimeUtc">The start time for this request,</param>
        /// <param name="endTimeUtc">The start time for this request</param>
        public HistoryRequest(SubscriptionDataConfig config, SecurityExchangeHours hours, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
            Symbol = config.Symbol;
            ExchangeHours = hours;
            Resolution = config.Resolution;
            FillForwardResolution = config.FillDataForward ? config.Resolution : (Resolution?) null;
            IncludeExtendedMarketHours = config.ExtendedMarketHours;
            DataType = config.Type;
            SecurityType = config.SecurityType;
            Market = config.Market;
            IsCustomData = config.IsCustomData;
            TimeZone = config.DataTimeZone;
        }
    }
}
