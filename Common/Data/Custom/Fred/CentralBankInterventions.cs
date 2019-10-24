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
        public static class CentralBankInterventions
        {
            ///<summary>
            /// Japan Intervention: Japanese Bank purchases of DM/Euro against JPY (in 100 Million Yen)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPINTDDMEJPY
            /// (+) numbers mean purchases of DM/EURO (Sell Yen), (-)numbers mean sales of DM/EURO (Buy Yen). Unpublished Data
            /// Copyright, 2016, Bank of Japan.
            /// </remarks>
            public static string JapaneseBankPurchasesOfDmEuroAgainstJpy = "JPINTDDMEJPY";

            ///<summary>
            /// Japan Intervention: Japanese Bank purchases of USD against DM (in 100 Million Yen)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPINTDEXR
            /// (+) numbers mean purchases of USD (Sell DM), (-)numbers mean sales of USD (Buy DM). Unpublished Data
            /// Copyright, 2016, Bank of Japan.
            /// </remarks>
            public static string JapaneseBankPurchasesOfUsdAgainstDm = "JPINTDEXR";

            ///<summary>
            /// Japan Intervention: Japanese Bank purchases of USD against Rupiah (in 100 Million Yen)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPINTDUSDRP
            /// (+) numbers mean purchases of USD (Sell Rupiah), (-)numbers mean sales of USD (Buy Rupiah). Unpublished Data
            /// Copyright, 2016, Bank of Japan.
            /// </remarks>
            public static string JapaneseBankPurchasesOfUsdAgainstRupiah = "JPINTDUSDRP";

            ///<summary>
            /// U.S. Intervention: in Market Transactions in the JPY/USD (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USINTDMRKTJPY
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string USInterventionInMarketTransactionsInTheJpyUsd = "USINTDMRKTJPY";

            ///<summary>
            /// U.S. Intervention: With-Customer Transactions in Other Currencies (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USINTDCSOTH
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string USInterventionWithCustomerTransactionsInOtherCurrencies = "USINTDCSOTH";

            ///<summary>
            /// U.S. Intervention: With-Customer Transactions in the JPY/USD (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USINTDCSJPY
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string USInterventionWithCustomerTransactionsInTheJpyUsd = "USINTDCSJPY";

            ///<summary>
            /// U.S. Intervention: With-Customer Transactions in the DEM/USD (Euro since 1999) (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USINTDCSDM
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string USInterventionWithCustomerTransactionsInTheDemUsdEuro = "USINTDCSDM";

            ///<summary>
            /// U.S. Intervention: in Market Transactions in Other Currencies (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USINTDMRKTOTH
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string USInterventionInMarketTransactionsInOtherCurrencies = "USINTDMRKTOTH";

            ///<summary>
            /// Turkish Intervention: Central Bank of Turkey Purchases of USD (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/TRINTDEXR
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// Since 2002, the foreign exchange interventions have started to be published through press releases at the same day when the intervention was made. The amount that was bought/sold at an intervention is published on the first working day of the month which comes after 3 months following the intervention date.
            /// </remarks>
            public static string CentralBankOfTurkeyPurchasesOfUsd = "TRINTDEXR";

            ///<summary>
            /// Japan Intervention: Japanese Bank purchases of USD against JPY (in 100 Million Yen)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPINTDUSDJPY
            /// (+) numbers mean purchases of the USD (sell Yen), (-) numbers mean sales of USD (buy Yen). Unpublished data.
            /// Copyright, 2016, Bank of Japan.
            /// </remarks>
            public static string JapaneseBankPurchasesOfUsdAgainstJpy = "JPINTDUSDJPY";

            ///<summary>
            /// U.S. Intervention: in Market Transactions in the DEM/USD (Euro since 1999) (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USINTDMRKTDM
            /// (+)numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string USInterventionInMarketTransactionsInTheDemUsdEuro = "USINTDMRKTDM";

            ///<summary>
            /// Swiss Intervention: Swiss National Bank Purchases of DEM against CHF (Millions of DEM) (in Millions of DEM)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHINTDCHFDM
            /// Copyright, 2016, Swiss National Bank.
            /// (+) numbers mean purchases of DEM, (-) numbers mean sales of DEM. Unpublished data.
            /// </remarks>
            public static string SwissNationalBankPurchasesOfDemAgainstChfMillionsOfDem = "CHINTDCHFDM";

            ///<summary>
            /// Swiss Intervention: Swiss National Bank Purchases of USD against DEM (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHINTDUSDDM
            /// Copyright, 2016, Swiss National Bank.
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string SwissNationalBankPurchasesOfUsdAgainstDem = "CHINTDUSDDM";

            ///<summary>
            /// Swiss Intervention: Swiss National Bank Purchases of USD against JPY (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHINTDUSDJPY
            /// Copyright, 2016, Swiss National Bank.
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string SwissNationalBankPurchasesOfUsdAgainstJpy = "CHINTDUSDJPY";

            ///<summary>
            /// Swiss Intervention: Swiss National Bank Purchases of USD against CHF (Millions of USD) (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHINTDCHFUSD
            /// Copyright, 2016, Swiss National Bank.
            /// (+) numbers mean purchases of USD, (-) numbers mean sales of USD. Unpublished data.
            /// </remarks>
            public static string SwissNationalBankPurchasesOfUsdAgainstChf = "CHINTDCHFUSD";

            ///<summary>
            /// Mexican Intervention: Banco de Mexico Purchase on the USD (in Millions of USD)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MEXINTDUSD
            /// </remarks>
            public static string BancoDeMexicoPurchaseOnTheUsd = "MEXINTDUSD";
        }
    }
}