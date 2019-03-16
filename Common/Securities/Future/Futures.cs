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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Futures static class contains shortcut definitions of major futures contracts available for trading
    /// </summary>
    public static class Futures
    {
        /// <summary>
        /// Grains and Oilseeds group 
        /// </summary>
        public static class Grains
        {
            /// <summary>
            /// Wheat Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Wheat = "ZW";

            /// <summary>
            /// Corn Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Corn = "ZC";

            /// <summary>
            /// Soybeans Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Soybeans = "ZS";

            /// <summary>
            /// Soybean Meal Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string SoybeanMeal = "ZM";

            /// <summary>
            /// Soybean Oil Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string SoybeanOil = "ZL";

            /// <summary>
            /// Oats Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Oats = "ZO";
        }

        /// <summary>
        /// Currencies group
        /// </summary>
        public static class Currencies
        {
            /// <summary>
            /// U.S. Dollar Index Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string USD = "DX";

            /// <summary>
            /// British Pound Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string GBP = "6B";

            /// <summary>
            /// Canadian Dollar Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string CAD = "6C";

            /// <summary>
            /// Japanese Yen Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string JPY = "6J";

            /// <summary>
            /// Swiss Franc Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string CHF = "6S";

            /// <summary>
            /// Euro FX Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string EUR = "6E";

            /// <summary>
            /// Australian Dollar Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string AUD = "6A";

            /// <summary>
            /// New Zealand Dollar Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string NZD = "6N";
        }

        /// <summary>
        /// Energies group
        /// </summary>
        public static class Energies
        {
            /// <summary>
            /// Crude Oil WTI Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string CrudeOilWTI = "CL";

            /// <summary>
            /// Heating Oil Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string HeatingOil = "HO";

            /// <summary>
            /// Gasoline RBOB Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Gasoline = "RB";

            /// <summary>
            /// Natural Gas Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string NaturalGas = "NG";
        }

        /// <summary>
        /// Financials group
        /// </summary>
        public static class Financials
        {
            /// <summary>
            /// 30Y U.S. Treasury Bond Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Y30TreasuryBond = "ZB";

            /// <summary>
            /// 10Y U.S. Treasury Note Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Y10TreasuryNote = "ZN";

            /// <summary>
            /// 5Y U.S. Treasury Note Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Y5TreasuryNote = "ZF";

            /// <summary>
            /// 2Y U.S. Treasury Note Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Y2TreasuryNote = "ZT";

            /// <summary>
            /// EuroDollar Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string EuroDollar = "GE";
        }

        /// <summary>
        /// Indices group
        /// </summary>
        public static class Indices
        {
            /// <summary>
            /// E-mini S&amp;P 500 Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string SP500EMini = "ES";

            /// <summary>
            /// E-mini NASDAQ 100 Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string NASDAQ100EMini = "NQ";

            /// <summary>
            /// E-mini Dow Indu 30 Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Dow30EMini = "YM";

            /// <summary>
            /// CBOE Volatility Index Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string VIX = "VX";

            /// <summary>
            /// E-mini Russell 2000 Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Russell2000EMini = "RTY";

            /// <summary>
            /// Nikkei-225 Dollar Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Nikkei225Dollar = "NKD";
        }

        /// <summary>
        /// Meats group
        /// </summary>
        public static class Meats
        {
            /// <summary>
            /// Live Cattle Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string LiveCattle = "LE";

            /// <summary>
            /// Feeder Cattle Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string FeederCattle = "GF";

            /// <summary>
            /// Lean Hogs Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string LeanHogs = "HE";
        }

        /// <summary>
        /// Metals group
        /// </summary>
        public static class Metals
        {
            /// <summary>
            /// Gold Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Gold = "GC";

            /// <summary>
            /// Silver Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Silver = "SI";

            /// <summary>
            /// Platinum Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Platinum = "PL";

            /// <summary>
            /// Palladium Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Palladium = "PA";
        }

        /// <summary>
        /// Softs group
        /// </summary>
        public static class Softs
        {
            /// <summary>
            /// Cotton #2 Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Cotton2 = "CT";

            /// <summary>
            /// Orange Juice Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string OrangeJuice = "OJ";

            /// <summary>
            /// Coffee C Arabica Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Coffee = "KC";

            /// <summary>
            /// Sugar #11 Futures ICE
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Sugar11 = "SB";

            /// <summary>
            /// Sugar #11 Futures CME
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Sugar11CME = "YO";

            /// <summary>
            /// Cocoa Futures
            /// </summary>
            /// <returns>The symbol</returns>
            public const string Cocoa = "CC";
        }
    }
}