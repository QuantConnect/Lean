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

namespace QuantConnect.Data.Custom.Fred
{
    public partial class Fred
    {
        /// <summary>
        /// Wilshire Indexes help clients, investment professionals and researchers accurately measure and better understand the market. The Wilshire Index family leverages more than 40 years of Wilshire performance measurement expertise and employs unbiased construction rules.
        /// </summary>
        public static class Wilshire
        {
            ///<summary>
            /// Wilshire US Small-Cap Value Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPVALPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USSmallCapValuePrice = "WILLSMLCAPVALPR";

            ///<summary>
            /// Wilshire 2500 Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500PR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Price2500 = "WILL2500PR";

            ///<summary>
            /// Wilshire 4500 Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL4500PR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Price4500 = "WILL4500PR";

            ///<summary>
            /// Wilshire 2500 Value Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500PRVAL
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string ValuePrice2500 = "WILL2500PRVAL";

            ///<summary>
            /// Wilshire 2500 Growth Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500PRGR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string GrowthPrice2500 = "WILL2500PRGR";

            ///<summary>
            /// Wilshire US Small-Cap Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USSmallCapPrice = "WILLSMLCAPPR";

            ///<summary>
            /// Wilshire 5000 Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL5000PR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Price5000 = "WILL5000PR";

            ///<summary>
            /// Wilshire US Small-Cap Growth Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPGRPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USSmallCapGrowthPrice = "WILLSMLCAPGRPR";

            ///<summary>
            /// Wilshire US Mid-Cap Value Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPVALPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USMidCapValuePrice = "WILLMIDCAPVALPR";

            ///<summary>
            /// Wilshire US Real Estate Securities Price Index (Wilshire US RESI) (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLRESIPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USRealEstateSecuritiesPrice = "WILLRESIPR";

            ///<summary>
            /// Wilshire US Large-Cap Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USLargeCapPrice = "WILLLRGCAPPR";

            ///<summary>
            /// Wilshire US Mid-Cap Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USMidCapPrice = "WILLMIDCAPPR";

            ///<summary>
            /// Wilshire US Mid-Cap Growth Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPGRPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USMidCapGrowthPrice = "WILLMIDCAPGRPR";

            ///<summary>
            /// Wilshire US Micro-Cap Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMICROCAPPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USMicroCapPrice = "WILLMICROCAPPR";

            ///<summary>
            /// Wilshire US Real Estate Investment Trust Price Index (Wilshire US REIT) (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLREITPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USRealEstateInvestmentTrustPrice = "WILLREITPR";

            ///<summary>
            /// Wilshire US Large-Cap Value Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPVALPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USLargeCapValuePrice = "WILLLRGCAPVALPR";

            ///<summary>
            /// Wilshire US Large-Cap Growth Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPGRPR
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USLargeCapGrowthPrice = "WILLLRGCAPGRPR";

            ///<summary>
            /// Wilshire 5000 Full Cap Price Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL5000PRFC
            /// The price indexes are price returns, which do not reinvest dividends. The designation Full Cap for an index signifies a float adjusted market capitalization that includes shares of stock not considered available to "ordinary" investors. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string FullCapPrice5000 = "WILL5000PRFC";

            ///<summary>
            /// Wilshire US Mid-Cap Value Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPVAL
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USMidCapValue = "WILLMIDCAPVAL";

            ///<summary>
            /// Wilshire US Mid-Cap Growth Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPGR
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USMidCapGrowth = "WILLMIDCAPGR";

            ///<summary>
            /// Wilshire US Mid-Cap Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAP
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USMidCap = "WILLMIDCAP";

            ///<summary>
            /// Wilshire US Real Estate Securities Total Market Index (Wilshire US RESI) (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLRESIND
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USRealEstateSecurities = "WILLRESIND";

            ///<summary>
            /// Wilshire 4500 Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL4500IND
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Index4500 = "WILL4500IND";

            ///<summary>
            /// Wilshire 5000 Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL5000IND
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Index5000 = "WILL5000IND";

            ///<summary>
            /// Wilshire US Large-Cap Growth Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPGR
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USLargeCapGrowth = "WILLLRGCAPGR";

            ///<summary>
            /// Wilshire US Micro-Cap Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMICROCAP
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USMicroCap = "WILLMICROCAP";

            ///<summary>
            /// Wilshire 2500 Value Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500INDVAL
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Value2500 = "WILL2500INDVAL";

            ///<summary>
            /// Wilshire US Small-Cap Growth Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPGR
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USSmallCapGrowth = "WILLSMLCAPGR";

            ///<summary>
            /// Wilshire US Small-Cap Value Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPVAL
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USSmallCapValue = "WILLSMLCAPVAL";

            ///<summary>
            /// Wilshire US Large-Cap Value Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPVAL
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USLargeCapValue = "WILLLRGCAPVAL";

            ///<summary>
            /// Wilshire US Real Estate Investment Trust Total Market Index (Wilshire US REIT) (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLREITIND
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USRealEstateInvestmentTrust = "WILLREITIND";

            ///<summary>
            /// Wilshire 2500 Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500IND
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Index2500 = "WILL2500IND";

            ///<summary>
            /// Wilshire US Small-Cap Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAP
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USSmallCap = "WILLSMLCAP";

            ///<summary>
            /// Wilshire US Large-Cap Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAP
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string USLargeCap = "WILLLRGCAP";

            ///<summary>
            /// Wilshire 2500 Growth Total Market Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500INDGR
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Growth2500 = "WILL2500INDGR";

            ///<summary>
            /// Wilshire 5000 Total Market Full Cap Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL5000INDFC
            /// The total market indexes are total market returns, which do include reinvested dividends. The designation Full Cap for an index signifies a float adjusted market capitalization that includes shares of stock not considered available to "ordinary" investors. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. For more information about the various indexes, visit Wilshire Associates (http://www.wilshire.com/Indexes/).
            /// </remarks>
            public static string TotalMarketFullCap5000 = "WILL5000INDFC";
        }
    }
}