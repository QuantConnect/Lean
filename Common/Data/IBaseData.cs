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

namespace QuantConnect.Data
{
    /// <summary>
    /// Base Data Class: Type, Timestamp, Key -- Base Features.
    /// </summary>
    public interface IBaseData
    {
        /// <summary>
        /// Market Data Type of this data - does it come in individual price packets or is it grouped into OHLC.
        /// </summary>
        MarketDataType DataType
        {
            get;
            set;
        }
        
        /// <summary>
        /// Time keeper of data -- all data is timeseries based.
        /// </summary>
        DateTime Time
        {
            get;
            set;
        }

        DateTime EndTime
        {
            get;
            set;
        }
        
        
        /// <summary>
        /// Symbol for underlying Security
        /// </summary>
        Symbol Symbol
        {
            get;
            set;
        }


        /// <summary>
        /// All timeseries data is a time-value pair:
        /// </summary>
        decimal Value
        {
            get;
            set;
        }


        /// <summary>
        /// Alias of Value.
        /// </summary>
        decimal Price
        {
            get;
        }

        /// <summary>
        /// Reader Method :: using set of arguements we specify read out type. Enumerate
        /// until the end of the data stream or file. E.g. Read CSV file line by line and convert
        /// into data types.
        /// </summary>
        /// <returns>BaseData type set by Subscription Method.</returns>
        BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed);


        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream 
        /// </summary>
        /// <param name="datafeed">Type of datafeed we're reqesting - backtest or live</param>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <returns>String URL of source file.</returns>
        string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed);

        /// <summary>
        /// Indicates if there is support for mapping
        /// </summary>
        /// <returns>True indicates mapping should be used</returns>
        bool RequiresMapping();

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        /// <returns></returns>
        BaseData Clone();

    } // End Base Data Class

} // End QC Namespace
