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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Custom.FRED
{
    public static partial class FredDataSeries
    {
        public static class CommercialPaperRates
        {
            ///<summary>
            /// Number of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN14A2P2VOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween1and4DaysUsedForA2P2Nonfinancial => "NONFIN14A2P2VOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN59A2P2VOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween5and9DaysUsedForA2P2Nonfinancial => "NONFIN59A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN59A2P2AMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween5and9DaysUsedForA2P2Nonfinancial => "NONFIN59A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN4180AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween41and80DaysUsedForAANonfinancial => "NONFIN4180AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ABGT80AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityGreaterThan80DaysUsedForAAAssetBacked => "ABGT80AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN4180AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween41and80DaysUsedForAANonfinancial => "NONFIN4180AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN4180A2P2VOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween41and80DaysUsedForA2P2Nonfinancial => "NONFIN4180A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN4180A2P2AMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween41and80DaysUsedForA2P2Nonfinancial => "NONFIN4180A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN2140AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween21and40DaysUsedForAANonfinancial => "NONFIN2140AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN2140AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween21and40DaysUsedForAANonfinancial => "NONFIN2140AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN2140A2P2VOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween21and40DaysUsedForA2P2Nonfinancial => "NONFIN2140A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN2140A2P2AMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween21and40DaysUsedForA2P2Nonfinancial => "NONFIN2140A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN14AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween1and4DaysUsedForAANonfinancial => "NONFIN14AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN1020A2P2VOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween10And20DaysUsedForA2P2Nonfinancial => "NONFIN1020A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN1020AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween10And20DaysUsedForAANonfinancial => "NONFIN1020AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB2140AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween21and40DaysUsedForAAAssetBacked => "AB2140AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN1020AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween10And20DaysUsedForAANonfinancial => "NONFIN1020AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN14A2P2AMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween1and4DaysUsedForA2P2Nonfinancial => "NONFIN14A2P2AMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN14AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween1and4DaysUsedForAANonfinancial => "NONFIN14AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN1020A2P2AMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween10And20DaysUsedForA2P2Nonfinancial => "NONFIN1020A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FINGT80AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityGreaterThan80DaysUsedForAAFinancial => "FINGT80AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN1020AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween10And20DaysUsedForAAFinancial => "FIN1020AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN14AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween1and4DaysUsedForAAFinancial => "FIN14AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN14AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween1and4DaysUsedForAAFinancial => "FIN14AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN2140AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween21and40DaysUsedForAAFinancial => "FIN2140AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN2140AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween21and40DaysUsedForAAFinancial => "FIN2140AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN59AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween5and9DaysUsedForAANonfinancial => "NONFIN59AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN4180AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween41and80DaysUsedForAAFinancial => "FIN4180AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN4180AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween41and80DaysUsedForAAFinancial => "FIN4180AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB4180AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween41and80DaysUsedForAAAssetBacked => "AB4180AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN59AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween5and9DaysUsedForAAFinancial => "FIN59AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN59AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween5and9DaysUsedForAAFinancial => "FIN59AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FINGT80AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityGreaterThan80DaysUsedForAAFinancial => "FINGT80AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Financial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FIN1020AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween10And20DaysUsedForAAFinancial => "FIN1020AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 21 and 40 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB2140AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween21and40DaysUsedForAAAssetBacked => "AB2140AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ABGT80AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityGreaterThan80DaysUsedForAAAssetBacked => "ABGT80AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFIN59AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween5and9DaysUsedForAANonfinancial => "NONFIN59AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB59AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween5and9DaysUsedForAAAssetBacked => "AB59AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 41 and 80 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB4180AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween41and80DaysUsedForAAAssetBacked => "AB4180AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFINGT80A2P2AMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityGreaterThan80DaysUsedForA2P2Nonfinancial => "NONFINGT80A2P2AMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 5 and 9 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB59AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween5and9DaysUsedForAAAssetBacked => "AB59AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB14AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween1and4DaysUsedForAAAssetBacked => "AB14AAVOL";

            ///<summary>
            /// Number of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the A2/P2 Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFINGT80A2P2VOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityGreaterThan80DaysUsedForA2P2Nonfinancial => "NONFINGT80A2P2VOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 1 and 4 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB14AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween1and4DaysUsedForAAAssetBacked => "AB14AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB1020AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityBetween10And20DaysUsedForAAAssetBacked => "AB1020AAVOL";

            ///<summary>
            /// Total Value of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFINGT80AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityGreaterThan80DaysUsedForAANonfinancial => "NONFINGT80AAAMT";

            ///<summary>
            /// Total Value of Issues, with a Maturity Between 10 and 20 Days, Used in Calculating the AA Asset-Backed Commercial Paper Rates (in Millions of Dollars)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AB1020AAAMT
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string TotalValueOfIssuesWithMaturityBetween10And20DaysUsedForAAAssetBacked => "AB1020AAAMT";

            ///<summary>
            /// Number of Issues, with a Maturity Greater Than 80 Days, Used in Calculating the AA Nonfinancial Commercial Paper Rates (in Number)
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NONFINGT80AAVOL
            /// </summary> 
            /// <remarks>
            /// For more information, please see http://www.federalreserve.gov/releases/cp/about.htm.
            /// </remarks>
            public static string NumberOfIssuesWithMaturityGreaterThan80DaysUsedForAANonfinancial => "NONFINGT80AAVOL";
        }
    }
}
