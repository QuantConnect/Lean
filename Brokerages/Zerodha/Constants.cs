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


namespace QuantConnect.Brokerages.Zerodha
{
#pragma warning disable 1591
    /// <summary>
    /// Types of product supported by Kite
    /// </summary>
    public enum KiteProductType
    {
        MIS,
        CNC,
        NRML
    }

    /// <summary>
    /// Types of order supported by Kite
    /// </summary>
    public enum KiteOrderType
    {
        MARKET,
        LIMIT,
        SLM,
        SL
    }

    public class Constants
    {

        // Products
        public const string PRODUCT_MIS = "MIS";
        public const string PRODUCT_CNC = "CNC";
        public const string PRODUCT_NRML = "NRML";

        // Order types
        public const string ORDER_TYPE_MARKET = "MARKET";
        public const string ORDER_TYPE_LIMIT = "LIMIT";
        public const string ORDER_TYPE_SLM = "SL-M";
        public const string ORDER_TYPE_SL = "SL";

        // Order status
        public const string ORDER_STATUS_COMPLETE = "COMPLETE";
        public const string ORDER_STATUS_CANCELLED = "CANCELLED";
        public const string ORDER_STATUS_REJECTED = "REJECTED";

        // Varities
        public const string VARIETY_REGULAR = "regular";
        public const string VARIETY_BO = "bo";
        public const string VARIETY_CO = "co";
        public const string VARIETY_AMO = "amo";

        // Transaction type
        public const string TRANSACTION_TYPE_BUY = "BUY";
        public const string TRANSACTION_TYPE_SELL = "SELL";

        // Validity
        public const string VALIDITY_DAY = "DAY";
        public const string VALIDITY_IOC = "IOC";

        // Exchanges
        public const string EXCHANGE_NSE = "NSE";
        public const string EXCHANGE_BSE = "BSE";
        public const string EXCHANGE_NFO = "NFO";
        public const string EXCHANGE_CDS = "CDS";
        public const string EXCHANGE_BFO = "BFO";
        public const string EXCHANGE_MCX = "MCX";

        // Margins segments
        public const string MARGIN_EQUITY = "equity";
        public const string MARGIN_COMMODITY = "commodity";

        // Ticker modes
        public const string MODE_FULL = "full";
        public const string MODE_QUOTE = "quote";
        public const string MODE_LTP = "ltp";

        // Positions
        public const string POSITION_DAY = "day";
        public const string POSITION_OVERNIGHT = "overnight";

        // Historical intervals
        public const string INTERVAL_MINUTE = "minute";
        public const string INTERVAL_3MINUTE = "3minute";
        public const string INTERVAL_5MINUTE = "5minute";
        public const string INTERVAL_10MINUTE = "10minute";
        public const string INTERVAL_15MINUTE = "15minute";
        public const string INTERVAL_30MINUTE = "30minute";
        public const string INTERVAL_60MINUTE = "60minute";
        public const string INTERVAL_DAY = "day";

        // GTT status
        public const string GTT_ACTIVE = "active";
        public const string GTT_TRIGGERED = "triggered";
        public const string GTT_DISABLED = "disabled";
        public const string GTT_EXPIRED = "expired";
        public const string GTT_CANCELLED = "cancelled";
        public const string GTT_REJECTED = "rejected";
        public const string GTT_DELETED = "deleted";


        // GTT trigger type
        public const string GTT_TRIGGER_OCO = "two-leg";
        public const string GTT_TRIGGER_SINGLE = "single";
    }
#pragma warning restore 1591
}
