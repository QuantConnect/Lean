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

namespace QuantConnect.Data
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Abstract base data class of QuantConnect. It is intended to be extended to define 
    /// generic user customizable data types while at the same time implementing the basics of data where possible
    /// </summary>
    public abstract class BaseData : IBaseData
    {
        /******************************************************** 
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        private MarketDataType _dataType = MarketDataType.Base;
        private DateTime _time = new DateTime();
        private string _symbol = "";
        private decimal _value = 0;

        /******************************************************** 
        * CLASS PUBLIC VARIABLES
        *********************************************************/
        /// <summary>
        /// Market Data Type of this data - does it come in individual price packets or is it grouped into OHLC.
        /// </summary>
        /// <remarks>Data is classed into two categories - streams of instantaneous prices and groups of OHLC data.</remarks>
        public MarketDataType DataType
        {
            get 
            {
                return _dataType;
            }
            set 
            {
                _dataType = value;
            }
        }
        
        /// <summary>
        /// Current time marker of this data packet.
        /// </summary>
        /// <remarks>All data is timeseries based.</remarks>
        public DateTime Time
        {
            get
            {
                return _time;
            }
            set
            {
                _time = value;
            }
        }
        
        /// <summary>
        /// String symbol representation for underlying Security
        /// </summary>
        public string Symbol
        {
            get
            {
                return _symbol;
            }
            set
            {
                _symbol = value;
            }
        }

        /// <summary>
        /// Value representation of this data packet. All data requires a representative value for this moment in time.
        /// For streams of data this is the price now, for OHLC packets this is the closing price.
        /// </summary>
        public decimal Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// As this is a backtesting platform we'll provide an alias of value as price.
        /// </summary>
        public decimal Price
        {
            get 
            {
                return Value;
            }
        }
        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Constructor for initialising the dase data class
        /// </summary>
        public BaseData() 
        { 
            //Empty constructor required for fast-reflection initialization
        }

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        
        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object 
        /// each time it is called. 
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Line of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="datafeed">Type of datafeed we're requesting - a live or backtest feed.</param>
        /// <returns>Instance of the T:BaseData object generated by this line of the CSV</returns>
        public abstract BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed);


        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream 
        /// </summary>
        /// <param name="datafeed">Type of datafeed we're reqesting - backtest or live</param>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <returns>String URL of source file.</returns>
        public abstract string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed);


        /// <summary>
        /// Update routine to build a bar/tick from a data update. 
        /// </summary>
        /// <param name="lastTrade">The last trade price</param>
        /// <param name="bidPrice">Current bid price</param>
        /// <param name="askPrice">Current asking price</param>
        /// <param name="volume">Volume of this trade</param>
        public virtual void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal volume)
        {
            Value = lastTrade;
        }

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        /// <returns></returns>
        public virtual BaseData Clone() 
        { 
            //Optional implementation
            return default(BaseData);
        }

    } // End Base Data Class

} // End QC Namespace
