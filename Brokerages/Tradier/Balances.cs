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

using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Inside "Account" User-account balance information.
    /// </summary>
    public class TradierBalance
    {
    //    /// Account Number
    //    [JsonProperty(PropertyName = "account_number")]
    //    public long AccountNumber;

        /// Balances of the Tradier Account:
        [JsonProperty(PropertyName = "balances")]
        public TradierBalanceDetails Balances;
    }

    /// <summary>
    /// Trader Balance Detail:
    /// </summary>
    public class TradierBalanceDetails 
    {
        ///Account Number
        [JsonProperty(PropertyName = "account_number")]
        public string AccountNumber;

        ///Account Type (margin, cash, pdt)
        [JsonProperty(PropertyName = "account_type")]
        public TradierAccountType Type;

        ///The amount of cash that could be withdrawn or invested in new investments, cash that is not required to support existing positions
        [JsonProperty(PropertyName = "cash_available")]
        public decimal CashAvailable;

        /// The ProfitLoss of the current trading day’s closed positions.
        [JsonProperty(PropertyName = "close_pl")]
        public decimal ClosingProfitLoss;

        /// The option requirement of current account positions.
        [JsonProperty(PropertyName = "current_requirement")]
        public decimal CurrentRequirement;

        /// Dividend Balance
        [JsonProperty(PropertyName = "dividend_balance")]
        public decimal DividendBalance;

        /// Equity Value
        [JsonProperty(PropertyName = "equity")]
        public decimal Equity;

        /// Long Liquid Value
        [JsonProperty(PropertyName = "long_liquid_value")]
        public decimal LongLiquidValue;

        /// Long Market Value
        [JsonProperty(PropertyName = "long_market_value")]
        public decimal LongMarketValue;

        /// Market Value
        [JsonProperty(PropertyName = "market_value")]
        public decimal MarketValue;

        /// Net Value
        [JsonProperty(PropertyName = "net_value")]
        public decimal NetValue;

        /// The Profit Loss of current account positions.
        [JsonProperty(PropertyName = "open_pl")]
        public decimal OpenProfitLoss;

        /// The value of long options held in the account.
        [JsonProperty(PropertyName = "option_long_value")]
        public decimal OptionLongValue;

        /// Option Requirement
        [JsonProperty(PropertyName = "option_requirement")]
        public decimal OptionRequirement;

        /// The value of short options held in the account.
        [JsonProperty(PropertyName = "option_short_value")]
        public decimal OptionShortValue;

        /// The amount of cash that is being held for open orders.
        [JsonProperty(PropertyName = "pending_cash")]
        public decimal PendingCash;

        /// The amount of open orders.
        [JsonProperty(PropertyName = "pending_orders_count")]
        public int PendingOrdersCount;

        ///Short Liquid Value
        [JsonProperty(PropertyName = "short_liquid_value")]
        public decimal ShortLiquidValue;

        ///Short Market Value
        [JsonProperty(PropertyName = "short_market_value")]
        public decimal ShortMarketValue;

        ///The value of long stocks held in the account.
        [JsonProperty(PropertyName = "stock_long_value")]
        public decimal StockLongValue;

        ///The amount of funds that are not currently available for trading.
        [JsonProperty(PropertyName = "uncleared_funds")]
        public decimal UnclearedFunds;

        ///Cash that is in the account from recent stock or option sales, but has not yet settled; 
        ///cash from stock sales occurring during the last 3 trading days or from option sales occurring during the previous trading day.
        [JsonProperty(PropertyName = "unsettled_funds")]
        public decimal UnsettledFunds;

        ///The total amount of cash in the account.
        [JsonProperty(PropertyName = "total_cash")]
        public decimal TotalCash;

        ///The total account value.
        [JsonProperty(PropertyName = "total_equity")]
        public decimal TotalEquity;

        /// Settings class for PDT specific accounts:
        [JsonProperty(PropertyName = "cash")]
        public TradierAccountTypeCash CashTypeSettings;

        /// Settings class for PDT specific accounts:
        [JsonProperty(PropertyName = "pdt")]
        public TradierAccountTypeDayTrader PatternTraderTypeSettings;

        /// Settings class for margin specific accounts
        [JsonProperty(PropertyName = "margin")]
        public TradierAccountTypeMargin MarginTypeSettings;
    }

    /// <summary>
    /// Common Account Settings.
    /// </summary>
    public class TradierAccountTypeSettings
    {
        ///The amount that the account is in deficit for trades that have occurred but not been paid for.
        [JsonProperty(PropertyName = "fed_call")]
        public decimal FedCall;

        ///The amount that the account is under the minimum equity required in the account to support the current holdings.
        [JsonProperty(PropertyName = "maintenance_call")]
        public decimal MaintenanceCall;

        ///The amount of funds available to purchase fully marginable securities.
        [JsonProperty(PropertyName = "stock_buying_power")]
        public decimal StockBuyingPower;

        ///The amount of funds available to purchase non-marginable securities.
        [JsonProperty(PropertyName = "option_buying_power")]
        public decimal OptionBuyingPower;

        ///The value of short stocks held in the account.
        [JsonProperty(PropertyName = "stock_short_value")]
        public decimal StockShortValue;

        ///Constructor
        public TradierAccountTypeSettings()
        { }
    }

    /// <summary>
    /// Account Type Day Trader Settings:
    /// </summary>
    public class TradierAccountTypeDayTrader : TradierAccountTypeSettings
    {
        ///The total amount of funds available for the purchase of fully marginable stock during that trading day, a portion of these funds cannot be held overnight.
        [JsonProperty(PropertyName = "day_trade_buying_power")]
        public decimal DayTradeBuyingPower;

        /// Constructor
        public TradierAccountTypeDayTrader()
        { }
    }

    /// <summary>
    /// Account Type Margin Settings:
    /// </summary>
    public class TradierAccountTypeMargin : TradierAccountTypeSettings
    {
        ///"Sweep"
        [JsonProperty(PropertyName = "sweep")]
        public int Sweep;

        /// Constructor
        public TradierAccountTypeMargin()
        { }
    }

    /// <summary>
    /// Account Type Margin Settings:
    /// </summary>
    public class TradierAccountTypeCash
    {
        ///"Sweep"
        [JsonProperty(PropertyName = "sweep")]
        public int Sweep;

        ///"Cash Available"
        [JsonProperty(PropertyName = "cash_available")]
        public decimal CashAvailable;

        ///"Unsettled."
        [JsonProperty(PropertyName = "unsettled_funds")]
        public decimal UnsettledFunds;

        /// Constructor
        public TradierAccountTypeCash()
        { }
    }
}
