using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Custom.FRED
{
    public static partial class FredDataSeries
    {
        /// <summary>
        /// Wilshire Indexes help clients, investment professionals and researchers accurately measure and better understand the market. The Wilshire Index family leverages more than 40 years of Wilshire performance measurement expertise and employs unbiased construction rules. 
        /// </summary>
        public static class Wilshire
        {
            ///<summary>
            /// Wilshire US Small-Cap Value Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPVALPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSSmallCapValuePrice => "WILLSMLCAPVALPR";

            ///<summary>
            /// Wilshire 2500 Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500PR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire2500Price => "WILL2500PR";

            ///<summary>
            /// Wilshire 4500 Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL4500PR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire4500Price => "WILL4500PR";

            ///<summary>
            /// Wilshire 2500 Value Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500PRVAL
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire2500ValuePrice => "WILL2500PRVAL";

            ///<summary>
            /// Wilshire 2500 Growth Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500PRGR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire2500GrowthPrice => "WILL2500PRGR";

            ///<summary>
            /// Wilshire US Small-Cap Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSSmallCapPrice => "WILLSMLCAPPR";

            ///<summary>
            /// Wilshire 5000 Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL5000PR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire5000Price => "WILL5000PR";

            ///<summary>
            /// Wilshire US Small-Cap Growth Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPGRPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSSmallCapGrowthPrice => "WILLSMLCAPGRPR";

            ///<summary>
            /// Wilshire US Mid-Cap Value Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPVALPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSMidCapValuePrice => "WILLMIDCAPVALPR";

            ///<summary>
            /// Wilshire US Real Estate Securities Price Index (Wilshire US RESI) (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLRESIPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSRealEstateSecuritiesPrice => "WILLRESIPR";

            ///<summary>
            /// Wilshire US Large-Cap Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSLargeCapPrice => "WILLLRGCAPPR";

            ///<summary>
            /// Wilshire US Mid-Cap Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSMidCapPrice => "WILLMIDCAPPR";

            ///<summary>
            /// Wilshire US Mid-Cap Growth Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPGRPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSMidCapGrowthPrice => "WILLMIDCAPGRPR";

            ///<summary>
            /// Wilshire US Micro-Cap Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMICROCAPPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSMicroCapPrice => "WILLMICROCAPPR";

            ///<summary>
            /// Wilshire US Real Estate Investment Trust Price Index (Wilshire US REIT) (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLREITPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSRealEstateInvestmentTrustPrice => "WILLREITPR";

            ///<summary>
            /// Wilshire US Large-Cap Value Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPVALPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSLargeCapValuePrice => "WILLLRGCAPVALPR";

            ///<summary>
            /// Wilshire US Large-Cap Growth Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPGRPR
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSLargeCapGrowthPrice => "WILLLRGCAPGRPR";

            ///<summary>
            /// Wilshire 5000 Full Cap Price Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL5000PRFC
            /// </summary> 
            /// <remarks>
            /// The price indexes are price returns, which do not reinvest dividends. The designation Full Cap for an index signifies a float adjusted market capitalization that includes shares of stock not considered available to "ordinary" investors. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire5000FullCapPrice => "WILL5000PRFC";

            ///<summary>
            /// Wilshire US Mid-Cap Value Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPVAL
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSMidCapValue => "WILLMIDCAPVAL";

            ///<summary>
            /// Wilshire US Mid-Cap Growth Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAPGR
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSMidCapGrowth => "WILLMIDCAPGR";

            ///<summary>
            /// Wilshire US Mid-Cap Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMIDCAP
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSMidCap => "WILLMIDCAP";

            ///<summary>
            /// Wilshire US Real Estate Securities Total Market Index (Wilshire US RESI) (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLRESIND
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSRealEstateSecurities => "WILLRESIND";

            ///<summary>
            /// Wilshire 4500 Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL4500IND
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire4500 => "WILL4500IND";

            ///<summary>
            /// Wilshire 5000 Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL5000IND
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire5000 => "WILL5000IND";

            ///<summary>
            /// Wilshire US Large-Cap Growth Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPGR
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSLargeCapGrowth => "WILLLRGCAPGR";

            ///<summary>
            /// Wilshire US Micro-Cap Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLMICROCAP
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSMicroCap => "WILLMICROCAP";

            ///<summary>
            /// Wilshire 2500 Value Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500INDVAL
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire2500Value => "WILL2500INDVAL";

            ///<summary>
            /// Wilshire US Small-Cap Growth Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPGR
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSSmallCapGrowth => "WILLSMLCAPGR";

            ///<summary>
            /// Wilshire US Small-Cap Value Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAPVAL
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSSmallCapValue => "WILLSMLCAPVAL";

            ///<summary>
            /// Wilshire US Large-Cap Value Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAPVAL
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSLargeCapValue => "WILLLRGCAPVAL";

            ///<summary>
            /// Wilshire US Real Estate Investment Trust Total Market Index (Wilshire US REIT) (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLREITIND
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSRealEstateInvestmentTrust => "WILLREITIND";

            ///<summary>
            /// Wilshire 2500 Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500IND
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire2500 => "WILL2500IND";

            ///<summary>
            /// Wilshire US Small-Cap Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLSMLCAP
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSSmallCap => "WILLSMLCAP";

            ///<summary>
            /// Wilshire US Large-Cap Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILLLRGCAP
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string WilshireUSLargeCap => "WILLLRGCAP";

            ///<summary>
            /// Wilshire 2500 Growth Total Market Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL2500INDGR
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. More information about the various indexes from Wilshire Associates can be found at http://www.wilshire.com/Indexes/.
            /// </remarks>
            public static string Wilshire2500Growth => "WILL2500INDGR";

            ///<summary>
            /// Wilshire 5000 Total Market Full Cap Index (in Index)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/WILL5000INDFC
            /// </summary> 
            /// <remarks>
            /// The total market indexes are total market returns, which do include reinvested dividends. The designation Full Cap for an index signifies a float adjusted market capitalization that includes shares of stock not considered available to "ordinary" investors. Copyright, 2016, Wilshire Associates Incorporated. Reprinted with permission. For more information about the various indexes, visit Wilshire Associates (http://www.wilshire.com/Indexes/).
            /// </remarks>
            public static string Wilshire5000TotalMarketFullCap => "WILL5000INDFC";
        }
    }
}
