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
using QuantConnect.Data.Consolidators;

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
        public SubscriptionDataConfig(Type objectType, SecurityType securityType = SecurityType.Equity, string symbol = "", Resolution resolution = Resolution.Minute, bool fillForward = true, bool extendedHours = false)
        {
            Type = objectType;
            Security = securityType;
            Resolution = resolution;
            Symbol = symbol;
            FillDataForward = fillForward;
            ExtendedMarketHours = extendedHours;
            PriceScaleFactor = 1;
            MappedSymbol = symbol;
            Consolidators = new List<IDataConsolidator>();

            switch (resolution)
            {
                case Resolution.Tick:
                    Increment = TimeSpan.FromSeconds(0);
                    break;
                case Resolution.Second:
                    Increment = TimeSpan.FromSeconds(1);
                    break;
                default:
                case Resolution.Minute:
                    Increment = TimeSpan.FromMinutes(1);
                    break;
                case Resolution.Hour:
                    Increment = TimeSpan.FromHours(1);
                    break;
                case Resolution.Daily:
                    Increment = TimeSpan.FromDays(1);
                    break;
            }
        }

        /// <summary>
        /// User defined source of data configuration
        /// </summary>
        /// <param name="objectType">Type the user defines</param>
        /// <param name="symbol">Symbol of the asset we'll trade</param>
        /// <param name="source">String source of the data.</param>
        public SubscriptionDataConfig(Type objectType, string symbol, string source)
        {
            Type = objectType;
            Security = SecurityType.Base;
            Resolution = Resolution.Second;
            Increment = TimeSpan.FromSeconds(1);
            Symbol = symbol;
            Consolidators = new List<IDataConsolidator>();

            //NOT NEEDED FOR USER DATA:*********//
            FillDataForward = true;        //
            ExtendedMarketHours = false;   //
            PriceScaleFactor = 1;          //
            MappedSymbol = symbol;         //
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
