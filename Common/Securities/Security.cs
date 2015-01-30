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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;


namespace QuantConnect.Securities 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// A base vehicle properties class for providing a common interface to all assets in QuantConnect.
    /// </summary>
    /// <remarks>
    /// Security object is intended to hold properties of the specific security asset. These properties can include trade start-stop dates, 
    /// price, market hours, resolution of the security, the holdings information for this security and the specific fill model.
    /// </remarks>
    public class Security 
    {

        /******************************************************** 
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        private string _symbol = "";
        private SecurityType _type = SecurityType.Equity;
        private Resolution _resolution = Resolution.Second;
        private bool _isFillDataForward = false;
        private bool _isExtendedMarketHours = false;
        private bool _isDynamicallyLoadedData = false;
        private decimal _leverage = 1;

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// String symbol for the asset.
        /// </summary>
        public string Symbol 
        {
            get 
            {
                return _symbol;
            }
        }
        
        /// <summary>
        /// Type of the security.
        /// </summary>
        /// <remarks>
        /// QuantConnect currently only supports Equities and Forex
        /// </remarks>
        public SecurityType Type 
        {
            get 
            {
                return _type;
            }
        }

        /// <summary>
        /// Resolution of data requested for this security.
        /// </summary>
        /// <remarks>Tick, second or minute resolution for QuantConnect assets.</remarks>
        public Resolution Resolution 
        {
            get 
            {
                return _resolution;
            }
        }

        /// <summary>
        /// Indicates the data will use previous bars when there was no trading in this time period. This was a configurable datastream setting set in initialization.
        /// </summary>
        public bool IsFillDataForward 
        {
            get 
            {
                return _isFillDataForward;
            }
        }

        /// <summary>
        /// Indicates the security will continue feeding data after the primary market hours have closed. This was a configurable setting set in initialization.
        /// </summary>
        public bool IsExtendedMarketHours
        {
            get 
            {
                return _isExtendedMarketHours;
            }
        }

        /// <summary>
        /// There has been at least one datapoint since our algorithm started running for us to determine price.
        /// </summary>
        public bool HasData
        {
            get
            {
                return GetLastData() != null; 
            }
        }

        /// <summary>
        /// Data cache for the security to store previous price information.
        /// </summary>
        /// <seealso cref="EquityCache"/>
        /// <seealso cref="ForexCache"/>
        public virtual SecurityCache Cache { get; set; }

        /// <summary>
        /// Holdings class contains the portfolio, cash and processes order fills.
        /// </summary>
        /// <seealso cref="EquityHolding"/>
        /// <seealso cref="ForexHolding"/>
        public virtual SecurityHolding Holdings
        {
            get; 
            set;
        }

        /// <summary>
        /// Exchange class contains the market opening hours, along with pre-post market hours.
        /// </summary>
        /// <seealso cref="EquityExchange"/>
        /// <seealso cref="ForexExchange"/>
        public virtual SecurityExchange Exchange
        {
            get;
            set;
        }

        /// <summary>
        /// Transaction model class implements the fill models for the security. If the user does not define a model the default
        /// model is used for this asset class.
        /// </summary>
        /// <remarks>This is ignored in live trading and the real fill prices are used instead</remarks>
        /// <seealso cref="EquityTransactionModel"/>
        /// <seealso cref="ForexTransactionModel"/>
        public virtual ISecurityTransactionModel Model
        {
            get; 
            set;
        }

        /// <summary>
        /// Customizable data filter to filter outlier ticks before they are passed into user event handlers. 
        /// By default all ticks are passed into the user algorithms.
        /// </summary>
        /// <remarks>TradeBars (seconds and minute bars) are prefiltered to ensure the ticks which build the bars are realistically tradeable</remarks>
        /// <seealso cref="EquityDataFilter"/>
        /// <seealso cref="ForexDataFilter"/>
        public virtual ISecurityDataFilter DataFilter
        {
            get; 
            set;
        }

        /******************************************************** 
        * CONSTRUCTOR/DELEGATE DEFINITIONS
        *********************************************************/
        /// <summary>
        /// Construct a new security vehicle based on the user options.
        /// </summary>
        public Security(string symbol, SecurityType type, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours, bool isDynamicallyLoadedData = false) 
        {
            //Set Basics:
            _symbol = symbol;
            _type = type;
            _resolution = resolution;
            _isFillDataForward = fillDataForward;
            _leverage = leverage;
            _isExtendedMarketHours = extendedMarketHours;
            _isDynamicallyLoadedData = isDynamicallyLoadedData;

            //Setup Transaction Model for this Asset
            switch (type) 
            { 
                case SecurityType.Equity:
                    Model = new EquityTransactionModel();
                    DataFilter = new EquityDataFilter();
                    break;
                case SecurityType.Forex:
                    Model = new ForexTransactionModel();
                    DataFilter = new ForexDataFilter();
                    break;
                case SecurityType.Base:
                    Model = new SecurityTransactionModel();
                    DataFilter = new SecurityDataFilter();
                    break;
            }

            //Holdings for new Vehicle:
            Cache = new SecurityCache();
            Holdings = new SecurityHolding(symbol, type, Model);
            Exchange = new SecurityExchange();
        }



        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Read only property that checks if we currently own stock in the company.
        /// </summary>
        public virtual bool HoldStock 
        {
            get
            {
                //Get a boolean, true if we own this stock.
                return Holdings.AbsoluteQuantity > 0;
            }
        }

        /// <summary>
        /// Alias for HoldStock - Do we have any of this security
        /// </summary>
        public virtual bool Invested 
        {
            get
            {
                return HoldStock;
            }
        }

        /// <summary>
        /// Local time for this market 
        /// </summary>
        public virtual DateTime Time 
        {
            get 
            {
                return Exchange.Time;
            }
        }

        /// <summary>
        /// Get the current value of the security.
        /// </summary>
        public virtual decimal Price 
        {
            get 
            {
                //Get the current security value from the cache
                var data = GetLastData();
                if (data != null) 
                {
                    return data.Value;
                }
                return 0;
            }
        }

        /// <summary>
        /// Leverage for this Security.
        /// </summary>
        public virtual decimal Leverage
        {
            get 
            { 
                return _leverage; 
            }
        }

        /// <summary>
        /// Use QuantConnect data source flag, or is the security a user imported object
        /// </summary>
        public virtual bool IsDynamicallyLoadedData 
        {
            get
            {
                return _isDynamicallyLoadedData;
            }
        }

        /// <summary>
        /// If this uses tradebar data, return the most recent high.
        /// </summary>
        public virtual decimal High {
            get 
            { 
                var data = GetLastData();
                if (data.DataType == MarketDataType.TradeBar) 
                {
                    return ((TradeBar)data).High;
                }
                return data.Value;
            }
        }

        /// <summary>
        /// If this uses tradebar data, return the most recent low.
        /// </summary>
        public virtual decimal Low {
            get 
            {
                var data = GetLastData();
                if (data.DataType == MarketDataType.TradeBar) 
                {
                    return ((TradeBar)data).Low;
                }
                return data.Value;
            }
        }

        /// <summary>
        /// If this uses tradebar data, return the most recent close.
        /// </summary>
        public virtual decimal Close 
        {
            get 
            {
                var data = GetLastData();
                if (data == null) return 0;
                return data.Value;
            }
        }

        /// <summary>
        /// If this uses tradebar data, return the most recent open.
        /// </summary>
        public virtual decimal Open {
            get {
                var data = GetLastData();
                if (data.DataType == MarketDataType.TradeBar) 
                {
                    return ((TradeBar)data).Open;
                }
                return data.Value;
            }
        }


        /// <summary>
        /// Access to the volume of the equity today
        /// </summary>
        public virtual long Volume
        {
            get
            {
                var data = GetLastData();
                if (data.DataType == MarketDataType.TradeBar)
                {
                    return ((TradeBar)data).Volume;
                }
                return 0;
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Get the last price update set to the security.
        /// </summary>
        /// <returns>BaseData object for this security</returns>
        public BaseData GetLastData() 
        {
            return Cache.GetData();
        }

        /// <summary>
        /// Update any security properties based on the lastest market data and time
        /// </summary>
        /// <param name="data">New data packet from LEAN</param>
        /// <param name="frontier">Time frontier / where we are in time.</param>
        public void Update(DateTime frontier, BaseData data) 
        { 
            //Update the Exchange/Timer:
            Exchange.SetDateTimeFrontier(frontier);

            //Add new point to cache:
            if (data == null) return;
            Cache.AddData(data);
            Holdings.UpdatePrice(data.Value);
        }

        /// <summary>
        /// Set the leverage parameter for this security
        /// </summary>
        /// <param name="leverage">Leverage for this asset</param>
        public void SetLeverage(decimal leverage)
        {
            _leverage = leverage;
        }

    } // End Security

} // End QC Namespace
