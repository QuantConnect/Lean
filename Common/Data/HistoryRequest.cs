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
using QuantConnect.Securities;

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents a request for historical data
    /// </summary>
    public class HistoryRequest : BaseDataRequest
    {
        private Resolution? _fillForwardResolution;

        /// <summary>
        /// Gets the symbol to request data for
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Gets the requested data resolution
        /// </summary>
        public Resolution Resolution { get; set; }

        /// <summary>
        /// Gets the requested fill forward resolution, set to null for no fill forward behavior.
        /// Will always return null when Resolution is set to Tick.
        /// </summary>
        public Resolution? FillForwardResolution
        {
            get
            {
                return Resolution == Resolution.Tick ? null : _fillForwardResolution;
            }
            set
            {
                _fillForwardResolution = value;
            }
        }

        /// <summary>
        /// Gets whether or not to include extended market hours data, set to false for only normal market hours
        /// </summary>
        public bool IncludeExtendedMarketHours { get; set; }

        /// <summary>
        /// Gets the data type used to process the subscription request, this type must derive from BaseData
        /// </summary>
        public Type DataType { get; set; }

        /// <summary>
        /// Gets the time zone of the time stamps on the raw input data
        /// </summary>
        public DateTimeZone DataTimeZone { get; set; }

        /// <summary>
        /// TickType of the history request
        /// </summary>
        public TickType TickType { get; set; }

        /// <summary>
        /// Gets true if this is a custom data request, false for normal QC data
        /// </summary>
        public bool IsCustomData { get; set; }

        /// <summary>
        /// Gets the normalization mode used for this subscription
        /// </summary>
        public DataNormalizationMode DataNormalizationMode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRequest"/> class from the specified parameters
        /// </summary>
        /// <param name="startTimeUtc">The start time for this request,</param>
        /// <param name="endTimeUtc">The start time for this request</param>
        /// <param name="dataType">The data type of the output data</param>
        /// <param name="symbol">The symbol to request data for</param>
        /// <param name="resolution">The requested data resolution</param>
        /// <param name="exchangeHours">The exchange hours used in fill forward processing</param>
        /// <param name="dataTimeZone">The time zone of the data</param>
        /// <param name="fillForwardResolution">The requested fill forward resolution for this request</param>
        /// <param name="includeExtendedMarketHours">True to include data from pre/post market hours</param>
        /// <param name="isCustomData">True for custom user data, false for normal QC data</param>
        /// <param name="dataNormalizationMode">Specifies normalization mode used for this subscription</param>
        /// <param name="tickType">The tick type used to created the <see cref="SubscriptionDataConfig"/> for the retrieval of history data</param>
        public HistoryRequest(DateTime startTimeUtc,
            DateTime endTimeUtc,
            Type dataType,
            Symbol symbol,
            Resolution resolution,
            SecurityExchangeHours exchangeHours,
            DateTimeZone dataTimeZone,
            Resolution? fillForwardResolution,
            bool includeExtendedMarketHours,
            bool isCustomData,
            DataNormalizationMode dataNormalizationMode,
            TickType tickType)
            : base(startTimeUtc, endTimeUtc, exchangeHours, tickType)
        {
            Symbol = symbol;
            DataTimeZone = dataTimeZone;
            Resolution = resolution;
            FillForwardResolution = fillForwardResolution;
            IncludeExtendedMarketHours = includeExtendedMarketHours;
            DataType = dataType;
            IsCustomData = isCustomData;
            DataNormalizationMode = dataNormalizationMode;
            TickType = tickType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRequest"/> class from the specified config and exchange hours
        /// </summary>
        /// <param name="config">The subscription data config used to initalize this request</param>
        /// <param name="hours">The exchange hours used for fill forward processing</param>
        /// <param name="startTimeUtc">The start time for this request,</param>
        /// <param name="endTimeUtc">The start time for this request</param>
        public HistoryRequest(SubscriptionDataConfig config, SecurityExchangeHours hours, DateTime startTimeUtc, DateTime endTimeUtc)
            : this(startTimeUtc, endTimeUtc, config.Type, config.Symbol, config.Resolution,
                hours, config.DataTimeZone, config.FillDataForward ? config.Resolution : (Resolution?)null,
                config.ExtendedMarketHours, config.IsCustomData, config.DataNormalizationMode, config.TickType)
        {
        }
    }
}
