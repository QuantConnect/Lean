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
        public static class TradeWeightedIndexes
        {
            ///<summary>
            /// Trade Weighted U.S. Dollar Index: Major Currencies, Goods (in Index Mar 1973=100)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DTWEXM
            /// A weighted average of the foreign exchange value of the U.S. dollar against a subset of the broad index currencies that circulate widely outside the country of issue.
            /// Major currencies index includes the Euro Area, Canada, Japan, United Kingdom, Switzerland, Australia, and Sweden.For more information about trade-weighted indexes visit the Board of Governors(http://www.federalreserve.gov/pubs/bulletin/2005/winter05_index.pdf).
            /// </remarks>
            public static string MajorCurrenciesGoods = "DTWEXM";

            ///<summary>
            /// Trade Weighted U.S. Dollar Index: Other Important Trading Partners, Goods (in Index Jan 1997=100)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DTWEXO
            /// A weighted average of the foreign exchange value of the U.S. dollar against a subset of the broad index currencies that do not circulate widely outside the country of issue.
            /// Countries whose currencies are included in the other important trading partners index are Mexico, China, Taiwan, Korea, Singapore, Hong Kong, Malaysia, Brazil, Thailand, Philippines, Indonesia, India, Israel, Saudi Arabia, Russia, Argentina, Venezuela, Chile and Colombia.
            /// For more information about trade-weighted indexes see http://www.federalreserve.gov/pubs/bulletin/2005/winter05_index.pdf.
            /// </remarks>
            public static string OtherImportantTradingPartnersGoods = "DTWEXO";

            ///<summary>
            /// Trade Weighted U.S. Dollar Index: Broad, Goods (in Index Jan 1997=100)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DTWEXB
            /// A weighted average of the foreign exchange value of the U.S. dollar against the currencies of a broad group of major U.S. trading partners.
            /// Broad currency index includes the Euro Area, Canada, Japan, Mexico, China, United Kingdom, Taiwan, Korea, Singapore, Hong Kong, Malaysia, Brazil, Switzerland, Thailand, Philippines, Australia, Indonesia, India, Israel, Saudi Arabia, Russia, Sweden, Argentina, Venezuela, Chile and Colombia.
            /// For more information about trade-weighted indexes see http://www.federalreserve.gov/pubs/bulletin/2005/winter05_index.pdf.
            /// </remarks>
            public static string BroadGoods = "DTWEXB";

            ///<summary>
            /// Trade Weighted U.S. Dollar Index: Advanced Foreign Economies, Goods and Services (in Index Jan 2006=100)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DTWEXAFEGS
			/// </remarks>
            public static string AdvancedForeignEconomiesGoodsAndServices = "DTWEXAFEGS";

            ///<summary>
            /// Trade Weighted U.S. Dollar Index: Broad, Goods and Services (in Index Jan 2006=100)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DTWEXBGS
            /// </remarks>
            public static string BroadGoodsAndServices = "DTWEXBGS";

            ///<summary>
            /// Trade Weighted U.S. Dollar Index: Emerging Markets Economies, Goods and Services (in Index Jan 2006=100)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DTWEXEMEGS
            /// </remarks>

            public static string EmergingMarketsEconomiesGoodsAndServices = "DTWEXEMEGS";
        }
    }
}