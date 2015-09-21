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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Securities 
{
    /// <summary>
    /// A base vehicle properties class for providing a common interface to all assets in QuantConnect.
    /// </summary>
    /// <remarks>
    /// Security object is intended to hold properties of the specific security asset. These properties can include trade start-stop dates, 
    /// price, market hours, resolution of the security, the holdings information for this security and the specific fill model.
    /// </remarks>
    public class Security 
    {
        private LocalTimeKeeper _localTimeKeeper;

        private readonly SubscriptionDataConfig _config;

        /// <summary>
        /// <see cref="Symbol"/> for the asset.
        /// </summary>
        public Symbol Symbol 
        {
            get 
            {
                return _config.Symbol;
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
                return _config.SecurityType;
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
                return _config.Resolution;
            }
        }

        /// <summary>
        /// Indicates the data will use previous bars when there was no trading in this time period. This was a configurable datastream setting set in initialization.
        /// </summary>
        public bool IsFillDataForward 
        {
            get 
            {
                return _config.FillDataForward;
            }
        }

        /// <summary>
        /// Indicates the security will continue feeding data after the primary market hours have closed. This was a configurable setting set in initialization.
        /// </summary>
        public bool IsExtendedMarketHours
        {
            get 
            {
                return _config.ExtendedMarketHours;
            }
        }

        /// <summary>
        /// Gets the subscription configuration for this security
        /// </summary>
        public SubscriptionDataConfig SubscriptionDataConfig
        {
            get { return _config; }
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
        [Obsolete("Security.Model has been made obsolete, use Security.TransactionModel instead.")]
        public virtual ISecurityTransactionModel Model
        {
            get { return TransactionModel; }
            set { TransactionModel = value; }
        }

        /// <summary>
        /// Transaction model class implements the fill models for the security. If the user does not define a model the default
        /// model is used for this asset class.
        /// </summary>
        /// <remarks>This is ignored in live trading and the real fill prices are used instead</remarks>
        /// <seealso cref="EquityTransactionModel"/>
        /// <seealso cref="ForexTransactionModel"/>
        public ISecurityTransactionModel TransactionModel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the portfolio model used by this security
        /// </summary>
        public ISecurityPortfolioModel PortfolioModel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the margin model used for this security
        /// </summary>
        public ISecurityMarginModel MarginModel
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
        public ISecurityDataFilter DataFilter
        {
            get; 
            set;
        }

        /// <summary>
        /// Construct a new security vehicle based on the user options.
        /// </summary>
        public Security(SecurityExchangeHours exchangeHours, SubscriptionDataConfig config, decimal leverage) 
        {
            _config = config;

            Cache = new SecurityCache();
            Exchange = new SecurityExchange(exchangeHours);
            DataFilter = new SecurityDataFilter();
            PortfolioModel = new SecurityPortfolioModel();
            TransactionModel = new SecurityTransactionModel();
            MarginModel = new SecurityMarginModel(leverage);
            Holdings = new SecurityHolding(this);
        }

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
        public virtual DateTime LocalTime
        {
            get
            {
                if (_localTimeKeeper == null)
                {
                    throw new Exception("Security.SetLocalTimeKeeper(LocalTimeKeeper) must be called in order to use the LocalTime property.");
                }
                return _localTimeKeeper.LocalTime;
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
                return Holdings.Leverage;
            }
        }

        /// <summary>
        /// If this uses tradebar data, return the most recent high.
        /// </summary>
        public virtual decimal High {
            get 
            { 
                var data = GetLastData();

                var bar = data as TradeBar;
                if (bar != null) 
                {
                    return bar.High;
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

                var bar = data as TradeBar;
                if (bar != null)
                {
                    return bar.Low;
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

                var bar = data as TradeBar;
                if (bar != null)
                {
                    return bar.Open;
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

                var bar = data as TradeBar;
                if (bar != null)
                {
                    return bar.Volume;
                }
                return 0;
            }
        }

        /// <summary>
        /// Get the last price update set to the security.
        /// </summary>
        /// <returns>BaseData object for this security</returns>
        public BaseData GetLastData() 
        {
            return Cache.GetData();
        }

        /// <summary>
        /// Sets the <see cref="LocalTimeKeeper"/> to be used for this <see cref="Security"/>.
        /// This is the source of this instance's time.
        /// </summary>
        /// <param name="localTimeKeeper">The source of this <see cref="Security"/>'s time.</param>
        public void SetLocalTimeKeeper(LocalTimeKeeper localTimeKeeper)
        {
            _localTimeKeeper = localTimeKeeper;
            _localTimeKeeper.TimeUpdated += (sender, args) =>
            {
                //Update the Exchange/Timer:
                Exchange.SetLocalDateTimeFrontier(args.Time);
            };
        }

        /// <summary>
        /// Update any security properties based on the lastest market data and time
        /// </summary>
        /// <param name="data">New data packet from LEAN</param>
        public void SetMarketPrice(BaseData data) 
        {
            //Add new point to cache:
            if (data == null) return;
            Cache.AddData(data);
            Holdings.UpdateMarketPrice(data.Value);
        }

        /// <summary>
        /// Set the leverage parameter for this security
        /// </summary>
        /// <param name="leverage">Leverage for this asset</param>
        public void SetLeverage(decimal leverage)
        {
            MarginModel.SetLeverage(this, leverage);
        }

        /// <summary>
        /// Sets the data normalization mode to be used by this security
        /// </summary>
        public void SetDataNormalizationMode(DataNormalizationMode mode)
        {
            _config.DataNormalizationMode = mode;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Symbol.Permtick;
        }
    }
}