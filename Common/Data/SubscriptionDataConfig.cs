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

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using QuantConnect.Data.Consolidators;
using QuantConnect.Securities;

namespace QuantConnect.Data
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Subscription data required including the type of data.
    /// </summary>
    public struct SubscriptionDataConfig
    {
        /******************************************************** 
        * STRUCT PUBLIC VARIABLES
        *********************************************************/
        /// Type of data
        public Type Type;
        /// Security type of this data subscription
        public SecurityType Security;
        /// Symbol of the asset we're requesting.
        public string Symbol;
        /// Resolution of the asset we're requesting, second minute or tick
        public Resolution Resolution;
        /// Timespan increment between triggers of this data:
        public TimeSpan Increment;
        /// True if wish to send old data when time gaps in data feed.
        public bool FillDataForward;
        /// Boolean Send Data from between 4am - 8am (Equities Setting Only)
        public bool ExtendedMarketHours;
        /// True if the data type has OHLC properties, even if dynamic data
        public readonly bool IsTradeBar;
        /// True if the data type has a Volume property, even if it is dynamic data
        public readonly bool HasVolume;
        /// True if this subscription was added for the sole purpose of providing currency conversion rates via <see cref="CashBook.EnsureCurrencyDataFeeds"/>
        public readonly bool IsCurrencyConversionFeed;

        /// Price Scaling Factor:
        public decimal PriceScaleFactor;
        ///Symbol Mapping: When symbols change over time (e.g. CHASE-> JPM) need to update the symbol requested.
        public string MappedSymbol;
        ///Consolidators that are registred with this subscription
        public List<IDataConsolidator> Consolidators; 

        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Constructor for Data Subscriptions
        /// </summary>
        /// <param name="objectType">Type of the data objects.</param>
        /// <param name="securityType">SecurityType Enum Set Equity/FOREX/Futures etc.</param>
        /// <param name="symbol">Symbol of the asset we're requesting</param>
        /// <param name="resolution">Resolution of the asset we're requesting</param>
        /// <param name="fillForward">Fill in gaps with historical data</param>
        /// <param name="extendedHours">Equities only - send in data from 4am - 8pm</param>
        public SubscriptionDataConfig(Type objectType, SecurityType securityType, string symbol, Resolution resolution, bool fillForward, bool extendedHours, bool isTradeBar, bool hasVolume, bool isCurrencyConversionFeed)
        {
            Type = objectType;
            Security = securityType;
            Resolution = resolution;
            Symbol = symbol;
            FillDataForward = fillForward;
            ExtendedMarketHours = extendedHours;
            IsTradeBar = isTradeBar;
            HasVolume = hasVolume;
            PriceScaleFactor = 1;
            MappedSymbol = symbol;
            IsCurrencyConversionFeed = isCurrencyConversionFeed;
            Consolidators = new List<IDataConsolidator>();

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
        /// Update the price scaling factor for this subscription:
        /// -> Used for backwards scaling _equity_ prices to adjust for splits and dividends. Unused
        /// </summary>
        public void SetPriceScaleFactor(decimal newFactor) 
        {
            PriceScaleFactor = newFactor;
        }

        /// <summary>
        /// Update the mapped symbol stored here: 
        /// </summary>
        /// <param name="newSymbol"></param>
        public void SetMappedSymbol(string newSymbol) 
        {
            MappedSymbol = newSymbol;
        }

    } // End Base Data Class

} // End QC Namespace
