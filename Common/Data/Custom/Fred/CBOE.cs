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
        public static class CBOE
        {
            ///<summary>
            /// CBOE Equity VIX on Google (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXGOGCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VIXOnGoogle = "VXGOGCLS";

            ///<summary>
            /// CBOE DJIA Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXDCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VXD = "VXDCLS";

            ///<summary>
            /// CBOE Equity VIX on Goldman Sachs (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXGSCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VIXOnGoldmanSachs = "VXGSCLS";

            ///<summary>
            /// CBOE Equity VIX on IBM (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXIBMCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VIXOnIBM = "VXIBMCLS";

            ///<summary>
            /// CBOE Equity VIX on Amazon (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXAZNCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VIXOnAmazon = "VXAZNCLS";

            ///<summary>
            /// CBOE S&amp;P 100 Volatility Index: VXO (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXOCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VXO = "VXOCLS";

            ///<summary>
            /// CBOE NASDAQ 100 Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXNCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VXN = "VXNCLS";

            ///<summary>
            /// CBOE 10-Year Treasury Note Volatility Futures (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXTYN
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string TenYearTreasuryNoteVolatilityFutures = "VXTYN";

            ///<summary>
            /// CBOE Russell 2000 Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RVXCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string RVX = "RVXCLS";

            ///<summary>
            /// CBOE S&amp;P 500 3-Month Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXVCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string SP500ThreeMonthVolatilityIndex = "VXVCLS";

            ///<summary>
            /// CBOE Equity VIX on Apple (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXAPLCLS
            /// Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VIXOnApple = "VXAPLCLS";

            ///<summary>
            /// CBOE Gold Miners ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXGDXCLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string GoldMinersETFVolatilityIndex = "VXGDXCLS";

            ///<summary>
            /// CBOE China ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXFXICLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string ChinaETFVolatilityIndex = "VXFXICLS";

            ///<summary>
            /// CBOE Brazil ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXEWZCLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string BrazilETFVolatilityIndex = "VXEWZCLS";

            ///<summary>
            /// CBOE Emerging Markets ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXEEMCLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string EmergingMarketsETFVolatilityIndex = "VXEEMCLS";

            ///<summary>
            /// CBOE EuroCurrency ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EVZCLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string EuroCurrencyETFVolatilityIndex = "EVZCLS";

            ///<summary>
            /// CBOE Gold ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GVZCLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string GoldETFVolatilityIndex = "GVZCLS";

            ///<summary>
            /// CBOE Crude Oil ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OVXCLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string CrudeOilETFVolatilityIndex = "OVXCLS";

            ///<summary>
            /// CBOE Silver ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXSLVCLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string SilverETFVolatilityIndex = "VXSLVCLS";

            ///<summary>
            /// CBOE Energy Sector ETF Volatility Index (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VXXLECLS
            /// Exchange Traded Funds (ETFs) are shares of trusts that hold portfolios of stocks designed to closely track the price performance and yield of specific indices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string EnergySectorETFVolatilityIndex = "VXXLECLS";

            ///<summary>
            /// CBOE Volatility Index: VIX (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/VIXCLS
            /// VIX measures market expectation of near term volatility conveyed by stock index option prices. Copyright, 2016, Chicago Board Options Exchange, Inc. Reprinted with permission.
            /// </remarks>
            public static string VIX = "VIXCLS";
        }
    }
}