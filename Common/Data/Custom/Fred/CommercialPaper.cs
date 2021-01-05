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
        /// Commercial paper (CP) consists of short-term, promissory notes issued primarily by corporations. Maturities range up to 270 days but average about 30 days. Many companies use CP to raise cash needed for current transactions, and many find it to be a lower-cost alternative to bank loans.
        /// The Federal Reserve Board disseminates information on CP primarily through its World Wide Web site. In addition, the Board publishes one-, two-, and three-month rates on AA nonfinancial and AA financial CP weekly in its H.15 Statistical Release.
        /// The Federal Reserve Board's CP release is derived from data supplied by The Depository Trust & Clearing Corporation (DTCC), a national clearinghouse for the settlement of securities trades and a custodian for securities. DTCC performs these functions for almost all activity in the domestic CP market. The Federal Reserve Board only considers maturities of 270 days or less. CP is exempt from SEC registration if its maturity does not exceed 270 days.
        /// Data on CP issuance rates and volumes typically are updated daily and typically posted with a one-day lag. Data on CP outstanding usually are available as of the close of business each Wednesday and as of the last business day of the month; these data are also posted with a one-day lag. The daily CP release will usually be available at 9:45 a.m. EST. However, the Federal Reserve Board makes no guarantee regarding the timing of the daily CP release. This policy is subject to change at any time without notice.
        /// </summary>
        public static class CommercialPaper
        {
            ///<summary>
            /// 3-Month AA Nonfinancial Commercial Paper Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DCPN3M
            /// Discount Basis
            /// </remarks>
            public static string ThreeMonthAANonfinancialCommercialPaperRate = "DCPN3M";

            ///<summary>
            /// 1-Month AA Nonfinancial Commercial Paper Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DCPN30
            /// Discount Basis
            /// </remarks>
            public static string OneMonthAANonfinancialCommercialPaperRate = "DCPN30";

            ///<summary>
            /// 2-Month AA Nonfinancial Commercial Paper Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DCPN2M
            /// Discount Basis
            /// </remarks>
            public static string TwoMonthAANonfinancialCommercialPaperRate = "DCPN2M";

            ///<summary>
            /// 3-Month AA Financial Commercial Paper Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DCPF3M
            /// Discount Basis
            /// </remarks>
            public static string ThreeMonthAAFinancialCommercialPaperRate = "DCPF3M";

            ///<summary>
            /// 2-Month AA Financial Commercial Paper Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DCPF2M
            /// Discount Basis
            /// </remarks>
            public static string TwoMonthAAFinancialCommercialPaperRate = "DCPF2M";

            ///<summary>
            /// 1-Month AA Financial Commercial Paper Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DCPF1M
            /// Discount Basis
            /// </remarks>
            public static string OneMonthAAFinancialCommercialPaperRate = "DCPF1M";

            ///<summary>
            /// Number of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN14A2P2VOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween1and4DaysUsedForA2P2Nonfinancial = "NONFIN14A2P2VOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN59A2P2VOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween5and9DaysUsedForA2P2Nonfinancial = "NONFIN59A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN59A2P2AMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween5and9DaysUsedForA2P2Nonfinancial = "NONFIN59A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN4180AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween41and80DaysUsedForAANonfinancial = "NONFIN4180AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ABGT80AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityGreaterThan80DaysUsedForAAAssetBacked = "ABGT80AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN4180AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween41and80DaysUsedForAANonfinancial = "NONFIN4180AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN4180A2P2VOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween41and80DaysUsedForA2P2Nonfinancial = "NONFIN4180A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN4180A2P2AMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween41and80DaysUsedForA2P2Nonfinancial = "NONFIN4180A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN2140AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween21and40DaysUsedForAANonfinancial = "NONFIN2140AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN2140AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween21and40DaysUsedForAANonfinancial = "NONFIN2140AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN2140A2P2VOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween21and40DaysUsedForA2P2Nonfinancial = "NONFIN2140A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN2140A2P2AMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween21and40DaysUsedForA2P2Nonfinancial = "NONFIN2140A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN14AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween1and4DaysUsedForAANonfinancial = "NONFIN14AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN1020A2P2VOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween10And20DaysUsedForA2P2Nonfinancial = "NONFIN1020A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN1020AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween10And20DaysUsedForAANonfinancial = "NONFIN1020AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB2140AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween21and40DaysUsedForAAAssetBacked = "AB2140AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN1020AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween10And20DaysUsedForAANonfinancial = "NONFIN1020AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN14A2P2AMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween1and4DaysUsedForA2P2Nonfinancial = "NONFIN14A2P2AMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN14AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween1and4DaysUsedForAANonfinancial = "NONFIN14AAAMT";

            ///<summary>
            /// Total Value of Commercial Paper Issues with a Maturity Between 1 and 4 Days (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT14MKTAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueofCommercialPaperIssueswithaMaturityBetween1and4Days = "MKT14MKTAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN1020A2P2AMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween10And20DaysUsedForA2P2Nonfinancial = "NONFIN1020A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FINGT80AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityGreaterThan80DaysUsedForAAFinancial = "FINGT80AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN1020AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween10And20DaysUsedForAAFinancial = "FIN1020AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN14AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween1and4DaysUsedForAAFinancial = "FIN14AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN14AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween1and4DaysUsedForAAFinancial = "FIN14AAVOL";

            ///<summary>
            /// Total Value of Commercial Paper Issues with a Maturity Between 10 and 20 Days (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT1020MKTAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueofCommercialPaperIssueswithaMaturityBetween10And20Days = "MKT1020MKTAMT";

            ///<summary>
            /// Number of Commercial Paper Issues with a Maturity Between 10 and 20 Days (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT1020MKTVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberofCommercialPaperIssueswithaMaturityBetween10And20Days = "MKT1020MKTVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN2140AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween21and40DaysUsedForAAFinancial = "FIN2140AAAMT";

            ///<summary>
            /// Number of Commercial Paper Issues with a Maturity Between 1 and 4 Days (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT14MKTVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberofCommercialPaperIssueswithaMaturityBetween1and4Days = "MKT14MKTVOL";

            ///<summary>
            /// Total Value of Issuers of Commercial Paper with a Maturity Between 21 and 40 Days (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT2140MKTAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueofIssuersofCommercialPaperwithaMaturityBetween21and40Days = "MKT2140MKTAMT";

            ///<summary>
            /// Number of Commercial Paper Issues with a Maturity Between 21 and 40 Days (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT2140MKTVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberofCommercialPaperIssueswithaMaturityBetween21and40Days = "MKT2140MKTVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN2140AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween21and40DaysUsedForAAFinancial = "FIN2140AAVOL";

            ///<summary>
            /// Total Value of Issuers of Commercial Paper with a Maturity Between 41 and 80 Days (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT4180MKTAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueofIssuersofCommercialPaperwithaMaturityBetween41and80Days = "MKT4180MKTAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN59AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween5and9DaysUsedForAANonfinancial = "NONFIN59AAAMT";

            ///<summary>
            /// Number of Commercial Paper Issues with a Maturity Between 41 and 80 Days (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT4180MKTVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberofCommercialPaperIssueswithaMaturityBetween41and80Days = "MKT4180MKTVOL";

            ///<summary>
            /// Number of Commercial Paper Issues with a Maturity Between 5 and 9 Days (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT59MKTVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberofCommercialPaperIssueswithaMaturityBetween5and9Days = "MKT59MKTVOL";

            ///<summary>
            /// Total Value of Issuers of Commercial Paper with a Maturity Greater Than 80 Days (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKTGT80MKTAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueofIssuersofCommercialPaperwithaMaturityGreaterThan80Days = "MKTGT80MKTAMT";

            ///<summary>
            /// Number of Commercial Paper Issues with a Maturity Greater Than 80 Days (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKTGT80MKTVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberofCommercialPaperIssueswithaMaturityGreaterThan80Days = "MKTGT80MKTVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN4180AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween41and80DaysUsedForAAFinancial = "FIN4180AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN4180AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween41and80DaysUsedForAAFinancial = "FIN4180AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB4180AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween41and80DaysUsedForAAAssetBacked = "AB4180AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN59AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween5and9DaysUsedForAAFinancial = "FIN59AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN59AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween5and9DaysUsedForAAFinancial = "FIN59AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FINGT80AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityGreaterThan80DaysUsedForAAFinancial = "FINGT80AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN1020AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween10And20DaysUsedForAAFinancial = "FIN1020AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB2140AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween21and40DaysUsedForAAAssetBacked = "AB2140AAVOL";

            ///<summary>
            /// Total Value of Issuers of Commercial Paper with a Maturity Between 5 and 9 Days (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MKT59MKTAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueofIssuersofCommercialPaperwithaMaturityBetween5and9Days = "MKT59MKTAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ABGT80AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityGreaterThan80DaysUsedForAAAssetBacked = "ABGT80AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN59AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween5and9DaysUsedForAANonfinancial = "NONFIN59AAVOL";

            ///<summary>
            /// 15-Day AA Asset-backed Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPAAAD15NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string FifteenDayAAAssetbackedCommercialPaperInterestRate = "RIFSPPAAAD15NB";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB59AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween5and9DaysUsedForAAAssetBacked = "AB59AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB4180AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween41and80DaysUsedForAAAssetBacked = "AB4180AAVOL";

            ///<summary>
            /// 15-Day A2/P2 Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNA2P2D15NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string FifteenDayA2P2NonfinancialCommercialPaperInterestRate = "RIFSPPNA2P2D15NB";

            ///<summary>
            /// 7-Day A2/P2 Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNA2P2D07NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string SevenDayA2P2NonfinancialCommercialPaperInterestRate = "RIFSPPNA2P2D07NB";

            ///<summary>
            /// Overnight A2/P2 Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNA2P2D01NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string OvernightA2P2NonfinancialCommercialPaperInterestRate = "RIFSPPNA2P2D01NB";

            ///<summary>
            /// 90-Day AA Financial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPFAAD90NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NinetyDayAAFinancialCommercialPaperInterestRate = "RIFSPPFAAD90NB";

            ///<summary>
            /// Overnight AA Asset-backed Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPAAAD01NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string OvernightAAAssetbackedCommercialPaperInterestRate = "RIFSPPAAAD01NB";

            ///<summary>
            /// 30-Day A2/P2 Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNA2P2D30NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string Three0DayA2P2NonfinancialCommercialPaperInterestRate = "RIFSPPNA2P2D30NB";

            ///<summary>
            /// 60-Day AA Financial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPFAAD60NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string SixtyDayAAFinancialCommercialPaperInterestRate = "RIFSPPFAAD60NB";

            ///<summary>
            /// 30-Day AA Financial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPFAAD30NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string Three0DayAAFinancialCommercialPaperInterestRate = "RIFSPPFAAD30NB";

            ///<summary>
            /// Total Value of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFINGT80A2P2AMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityGreaterThan80DaysUsedForA2P2Nonfinancial = "NONFINGT80A2P2AMT";

            ///<summary>
            /// 30-Day AA Asset-backed Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPAAAD30NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string Three0DayAAAssetbackedCommercialPaperInterestRate = "RIFSPPAAAD30NB";

            ///<summary>
            /// 60-Day AA Asset-backed Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPAAAD60NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string SixtyDayAAAssetbackedCommercialPaperInterestRate = "RIFSPPAAAD60NB";

            ///<summary>
            /// 90-Day AA Asset-backed Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPAAAD90NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NinetyDayAAAssetbackedCommercialPaperInterestRate = "RIFSPPAAAD90NB";

            ///<summary>
            /// 15-Day AA Financial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPFAAD15NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string FifteenDayAAFinancialCommercialPaperInterestRate = "RIFSPPFAAD15NB";

            ///<summary>
            /// 7-Day AA Financial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPFAAD07NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string SevenDayAAFinancialCommercialPaperInterestRate = "RIFSPPFAAD07NB";

            ///<summary>
            /// 7-Day AA Asset-backed Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPAAAD07NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string SevenDayAAAssetbackedCommercialPaperInterestRate = "RIFSPPAAAD07NB";

            ///<summary>
            /// Overnight AA Financial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPFAAD01NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string OvernightAAFinancialCommercialPaperInterestRate = "RIFSPPFAAD01NB";

            ///<summary>
            /// 60-Day A2/P2 Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNA2P2D60NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string SixtyDayA2P2NonfinancialCommercialPaperInterestRate = "RIFSPPNA2P2D60NB";

            ///<summary>
            /// Number of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB59AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween5and9DaysUsedForAAAssetBacked = "AB59AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB14AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween1and4DaysUsedForAAAssetBacked = "AB14AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFINGT80A2P2VOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityGreaterThan80DaysUsedForA2P2Nonfinancial = "NONFINGT80A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB14AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween1and4DaysUsedForAAAssetBacked = "AB14AAAMT";

            ///<summary>
            /// 90-Day A2/P2 Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNA2P2D90NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NinetyDayA2P2NonfinancialCommercialPaperInterestRate = "RIFSPPNA2P2D90NB";

            ///<summary>
            /// Number of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB1020AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween10And20DaysUsedForAAAssetBacked = "AB1020AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFINGT80AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityGreaterThan80DaysUsedForAANonfinancial = "NONFINGT80AAAMT";

            ///<summary>
            /// Overnight AA Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNAAD01NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string OvernightAANonfinancialCommercialPaperInterestRate = "RIFSPPNAAD01NB";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB1020AAAMT
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween10And20DaysUsedForAAAssetBacked = "AB1020AAAMT";

            ///<summary>
            /// 7-Day AA Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNAAD07NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string SevenDayAANonfinancialCommercialPaperInterestRate = "RIFSPPNAAD07NB";

            ///<summary>
            /// 90-Day AA Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNAAD90NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NinetyDayAANonfinancialCommercialPaperInterestRate = "RIFSPPNAAD90NB";

            ///<summary>
            /// 15-Day AA Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNAAD15NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string FifteenDayAANonfinancialCommercialPaperInterestRate = "RIFSPPNAAD15NB";

            ///<summary>
            /// 30-Day AA Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNAAD30NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string Three0DayAANonfinancialCommercialPaperInterestRate = "RIFSPPNAAD30NB";

            ///<summary>
            /// 60-Day AA Nonfinancial Commercial Paper Interest Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RIFSPPNAAD60NB
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string SixtyDayAANonfinancialCommercialPaperInterestRate = "RIFSPPNAAD60NB";

            ///<summary>
            /// Number of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFINGT80AAVOL
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityGreaterThan80DaysUsedForAANonfinancial = "NONFINGT80AAVOL";

            ///<summary>
            /// 3-Month Commercial Paper Minus Federal Funds Rate (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CPFF
            /// Series is calculated as the spread between 3-Month AA Financial Commercial Paper (RIFSPPFAAD90NB) and Effective Federal Funds Rate (https://fred.stlouisfed.org/series/DFF).
            /// Starting with the update on June 21, 2019, the Treasury bond data used in calculating interest rate spreads is obtained directly from the U.S.Treasury Department(https://www.treasury.gov/resource-center/data-chart-center/interest-rates/Pages/TextView.aspx?data=yield).
            /// </remarks>
            public static string ThreeMonthCommercialPaperMinusFederalFundsRate = "CPFF";
        }
    }
}