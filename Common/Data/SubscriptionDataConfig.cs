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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NodaTime;
using QuantConnect.Data.Consolidators;
using QuantConnect.Securities;

namespace QuantConnect.Data
{
    /// <summary>
    /// Subscription data required including the type of data.
    /// </summary>
    public class SubscriptionDataConfig
    {
        private Symbol _symbol;
        private string _mappedSymbol;
        private readonly string _sid;

        /// <summary>
        /// Type of data
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Security type of this data subscription
        /// </summary>
        public readonly SecurityType SecurityType;

        /// <summary>
        /// Symbol of the asset we're requesting: this is really a perm tick!!
        /// </summary>
        public Symbol Symbol
        {
            get { return _symbol; }
        }

        /// <summary>
        /// Resolution of the asset we're requesting, second minute or tick
        /// </summary>
        public readonly Resolution Resolution;

        /// <summary>
        /// Timespan increment between triggers of this data:
        /// </summary>
        public readonly TimeSpan Increment;

        /// <summary>
        /// True if wish to send old data when time gaps in data feed.
        /// </summary>
        public readonly bool FillDataForward;

        /// <summary>
        /// Boolean Send Data from between 4am - 8am (Equities Setting Only)
        /// </summary>
        public readonly bool ExtendedMarketHours;

        /// <summary>
        /// True if this subscription was added for the sole purpose of providing currency conversion rates via <see cref="CashBook.EnsureCurrencyDataFeeds"/>
        /// </summary>
        public readonly bool IsInternalFeed;

        /// <summary>
        /// True if this subscription is for custom user data, false for QC data
        /// </summary>
        public readonly bool IsCustomData;

        /// <summary>
        /// The sum of dividends accrued in this subscription, used for scaling total return prices
        /// </summary>
        public decimal SumOfDividends;

        /// <summary>
        /// Gets the normalization mode used for this subscription
        /// </summary>
        public DataNormalizationMode DataNormalizationMode = DataNormalizationMode.Adjusted;

        /// <summary>
        /// Price Scaling Factor:
        /// </summary>
        public decimal PriceScaleFactor;

        /// <summary>
        /// Symbol Mapping: When symbols change over time (e.g. CHASE-> JPM) need to update the symbol requested.
        /// </summary>
        public string MappedSymbol
        {
            get { return _mappedSymbol; }
            set
            {
                _mappedSymbol = value;
                _symbol = new Symbol(_sid, value);
            }
        }

        /// <summary>
        /// Gets the market / scope of the symbol
        /// </summary>
        public readonly string Market;

        /// <summary>
        /// Gets the time zone for this subscription
        /// </summary>
        public readonly DateTimeZone TimeZone;

        /// <summary>
        /// Consolidators that are registred with this subscription
        /// </summary>
        public readonly HashSet<IDataConsolidator> Consolidators;

        /// <summary>
        /// Constructor for Data Subscriptions
        /// </summary>
        /// <param name="objectType">Type of the data objects.</param>
        /// <param name="securityType">SecurityType Enum Set Equity/FOREX/Futures etc.</param>
        /// <param name="symbol">Symbol of the asset we're requesting</param>
        /// <param name="resolution">Resolution of the asset we're requesting</param>
        /// <param name="market">The market this subscription comes from</param>
        /// <param name="timeZone">The time zone the raw data is time stamped in</param>
        /// <param name="fillForward">Fill in gaps with historical data</param>
        /// <param name="extendedHours">Equities only - send in data from 4am - 8pm</param>
        /// <param name="isInternalFeed">Set to true if this subscription is added for the sole purpose of providing currency conversion rates,
        /// setting this flag to true will prevent the data from being sent into the algorithm's OnData methods</param>
        /// <param name="isCustom">True if this is user supplied custom data, false for normal QC data</param>
        public SubscriptionDataConfig(Type objectType, 
            SecurityType securityType, 
            Symbol symbol, 
            Resolution resolution, 
            string market, 
            DateTimeZone timeZone,
            bool fillForward, 
            bool extendedHours,
            bool isInternalFeed,
            bool isCustom = false)
        {
            Type = objectType;
            SecurityType = securityType;
            Resolution = resolution;
            _sid = symbol.Permtick;
            FillDataForward = fillForward;
            ExtendedMarketHours = extendedHours;
            PriceScaleFactor = 1;
            MappedSymbol = symbol.Value;
            IsInternalFeed = isInternalFeed;
            IsCustomData = isCustom;
            Market = market;
            TimeZone = timeZone;
            Consolidators = new HashSet<IDataConsolidator>();

            // verify the market string contains letters a-Z
            if (string.IsNullOrWhiteSpace(market))
            {
                throw new ArgumentException("The market cannot be an empty string.");
            }
            if (!Regex.IsMatch(market, @"^[a-zA-Z]+$"))
            {
                throw new ArgumentException("The market must only contain letters A-Z.");
            }

            switch (resolution)
            {
                case Resolution.Tick:
                    //Ticks are individual sales and fillforward doesn't apply.
                    Increment = TimeSpan.FromSeconds(0);
                    FillDataForward = false;
                    break;
                case Resolution.Second:
                    Increment = TimeSpan.FromSeconds(1);
                    break;
                case Resolution.Minute:
                    Increment = TimeSpan.FromMinutes(1);
                    break;
                case Resolution.Hour:
                    Increment = TimeSpan.FromHours(1);
                    break;
                case Resolution.Daily:
                    Increment = TimeSpan.FromDays(1);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unexpected Resolution: " + resolution);
            }
        }

        /// <summary>
        /// Copy constructor with overrides
        /// </summary>
        /// <param name="config">The config to copy, then overrides are applied and all option</param>
        /// <param name="objectType">Type of the data objects.</param>
        /// <param name="securityType">SecurityType Enum Set Equity/FOREX/Futures etc.</param>
        /// <param name="symbol">Symbol of the asset we're requesting</param>
        /// <param name="resolution">Resolution of the asset we're requesting</param>
        /// <param name="market">The market this subscription comes from</param>
        /// <param name="timeZone">The time zone the raw data is time stamped in</param>
        /// <param name="fillForward">Fill in gaps with historical data</param>
        /// <param name="extendedHours">Equities only - send in data from 4am - 8pm</param>
        /// <param name="isInternalFeed">Set to true if this subscription is added for the sole purpose of providing currency conversion rates,
        /// setting this flag to true will prevent the data from being sent into the algorithm's OnData methods</param>
        /// <param name="isCustom">True if this is user supplied custom data, false for normal QC data</param>
        public SubscriptionDataConfig(SubscriptionDataConfig config,
            Type objectType = null,
            SecurityType? securityType = null,
            Symbol symbol = null,
            Resolution? resolution = null,
            string market = null,
            DateTimeZone timeZone = null,
            bool? fillForward = null,
            bool? extendedHours = null,
            bool? isInternalFeed = null,
            bool? isCustom = null)
            : this(
            objectType ?? config.Type,
            securityType ?? config.SecurityType,
            symbol ?? config.Symbol,
            resolution ?? config.Resolution,
            market ?? config.Market,
            timeZone ?? config.TimeZone,
            fillForward ?? config.FillDataForward,
            extendedHours ?? config.ExtendedMarketHours,
            isInternalFeed ?? config.IsInternalFeed,
            isCustom ?? config.IsCustomData
            )
        {
        }

        /// <summary>
        /// Normalizes the specified price based on the DataNormalizationMode
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal GetNormalizedPrice(decimal price)
        {
            switch (DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    return price;
                
                // the price scale factor will be set accordingly based on the mode in update scale factors
                case DataNormalizationMode.Adjusted:
                case DataNormalizationMode.SplitAdjusted:
                    return price*PriceScaleFactor;
                
                case DataNormalizationMode.TotalReturn:
                    return (price*PriceScaleFactor) + SumOfDividends;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}
