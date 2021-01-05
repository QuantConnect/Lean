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
        public static class ICEBofAML
        {
            ///<summary>
            /// ICE BofAML AAA-A Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM1BRRAAA2ACRPITRIV
            /// The ICE BofAML AAA-A Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated AAA through A3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AAAAEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM1BRRAAA2ACRPITRIV";

            ///<summary>
            /// ICE BofAML AAA-A US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM1RAAA2ALCRPIUSTRIV
            /// The ICE BofAML AAA-A US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML US Emerging Markets Liquid Corporate Plus Index, which includes only securities rated AAA through A3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AAAAUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM1RAAA2ALCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML Asia Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRACRPIASIATRIV
            /// The ICE BofAML Asia Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Asia, excluding Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AsiaEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMRACRPIASIATRIV";

            ///<summary>
            /// ICE BofAML Asia US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMALLCRPIASIAUSTRIV
            /// The ICE BofAML Asia US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Asia, excluding Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AsiaUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMALLCRPIASIAUSTRIV";

            ///<summary>
            /// ICE BofAML B and Lower Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM4BRRBLCRPITRIV
            /// The ICE BofAML B and Lower Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated B1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BandLowerEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM4BRRBLCRPITRIV";

            ///<summary>
            /// ICE BofAML B and Lower US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM4RBLLCRPIUSTRIV
            /// The ICE BofAML B and Lower US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated B1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BandLowerUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM4RBLLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML BB Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM3BRRBBCRPITRIV
            /// The ICE BofAML BB Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM3BRRBBCRPITRIV";

            ///<summary>
            /// ICE BofAML BB US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM3RBBLCRPIUSTRIV
            /// The ICE BofAML BB US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM3RBBLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML BBB Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM2BRRBBBCRPITRIV
            /// The ICE BofAML BBB Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BBB1 through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBBEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM2BRRBBBCRPITRIV";

            ///<summary>
            /// ICE BofAML BBB US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM2RBBBLCRPIUSTRIV
            /// The ICE BofAML BBB US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BBB1 through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBBUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM2RBBBLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML Crossover Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM5BCOCRPITRIV
            /// The ICE BofAML Crossover Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BBB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string CrossoverEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEM5BCOCRPITRIV";

            ///<summary>
            /// ICE BofAML Crossover US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMXOCOLCRPIUSTRIV
            /// The ICE BofAML Crossover US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BBB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string CrossoverUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMXOCOLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML Emerging Markets Corporate Plus Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMCBPITRIV
            /// The ICE BofAML Emerging Markets Corporate Plus Index tracks the performance of US dollar (USD) and Euro denominated emerging markets non-sovereign debt publicly issued within the major domestic and Eurobond markets. To qualify for inclusion in the index, the issuer of debt must have risk exposure to countries other than members of the FX G10 (US, Japan, New Zealand, Australia, Canada, Sweden, UK, Switzerland, Norway, and Euro Currency Members), all Western European countries, and territories of the US and Western European countries. Each security must also be denominated in USD or Euro with a time to maturity greater than 1 year and have a fixed coupon. For inclusion in the index, investment grade rated bonds of qualifying issuers must have at least 250 million (Euro or USD) in outstanding face value, and below investment grade rated bonds must have at least 100 million (Euro or USD) in outstanding face value. The index includes corporate and quasi-government debt of qualifying countries, but excludes sovereign and supranational debt. Other types of securities acceptable for inclusion in this index are: original issue zero coupon bonds, ‚Äúglobal‚Äù securities (debt issued in the US domestic bond markets as well the Eurobond Market simultaneously), 144a securities, pay-in-kind securities (includes toggle notes), callable perpetual securities (qualify if they are at least one year from the first call date), fixed-to-floating rate securities (qualify if the securities are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security). Defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of US mortgage pass-throughs and US structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for US mortgage pass-through and US structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until
            /// the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EmergingMarketsCorporatePlusIndexTotalReturnIndexValue = "BAMLEMCBPITRIV";

            ///<summary>
            /// ICE BofAML Euro Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMEBCRPIETRIV
            /// The ICE BofAML Euro Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities denominated in Euros. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EuroEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMEBCRPIETRIV";

            ///<summary>
            /// ICE BofAML Europe, the Middle East, and Africa (EMEA) Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRECRPIEMEATRIV
            /// The ICE BofAML Europe, the Middle East, and Africa (EMEA) Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Europe, the Middle East and Africa, also including Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EMEAEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMRECRPIEMEATRIV";

            ///<summary>
            /// ICE BofAML Europe, the Middle East, and Africa (EMEA) US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMELLCRPIEMEAUSTRIV
            /// The ICE BofAML Europe, the Middle East, and Africa (EMEA) US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Europe, the Middle East and Africa, also including Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EMEAUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMELLCRPIEMEAUSTRIV";

            ///<summary>
            /// ICE BofAML Financial Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMFSFCRPITRIV
            /// The ICE BofAML Financial Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes all Financial securities except the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string FinancialEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMFSFCRPITRIV";

            ///<summary>
            /// ICE BofAML Financial US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMFLFLCRPIUSTRIV
            /// The ICE BofAML Financial US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all Financial securities except the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string FinancialUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMFLFLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML High Grade Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMIBHGCRPITRIV
            /// The ICE BofAML High Grade Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated AAA through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighGradeEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMIBHGCRPITRIV";

            ///<summary>
            /// ICE BofAML High Grade US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHGHGLCRPIUSTRIV
            /// The ICE BofAML High Grade US Emerging Markets Liquid Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated AAA through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighGradeUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMHGHGLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML High Yield Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHBHYCRPITRIV
            /// The ICE BofAML High Yield Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BB1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighYieldEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMHBHYCRPITRIV";

            ///<summary>
            /// ICE BofAML High Yield US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHYHYLCRPIUSTRIV
            /// The ICE BofAML High Yield US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BB1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighYieldUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMHYHYLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML Latin America Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRLCRPILATRIV
            /// The ICE BofAML Latin America Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Latin America. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string LatinAmericaEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMRLCRPILATRIV";

            ///<summary>
            /// ICE BofAML Latin America US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMLLLCRPILAUSTRIV
            /// The ICE BofAML Latin America US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Latin America. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string LatinAmericaUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMLLLCRPILAUSTRIV";

            ///<summary>
            /// ICE BofAML Non-Financial Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMNSNFCRPITRIV
            /// The ICE BofAML Non-Financial Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which excludes all Financial securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string NonFinancialEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMNSNFCRPITRIV";

            ///<summary>
            /// ICE BofAML Non-Financial US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMNFNFLCRPIUSTRIV
            /// The ICE BofAML Non-Financial US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which excludes all Financial securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string NonFinancialUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMNFNFLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML US Corporate Master Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A0CM
            /// The ICE BofAML Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. The Corporate Master OAS uses an index of bonds that are considered investment grade (those rated BBB or better). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// This data represents the ICE BofAML US Corporate Master Index value, which tracks the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. To qualify for inclusion in the index, securities must have an investment grade rating (based on an average of Moody's, S&P, and Fitch) and an investment grade rated country of risk (based on an average of Moody's, S&P, and Fitch foreign currency long term sovereign debt ratings). Each security must have greater than 1 year of remaining maturity, a fixed coupon schedule, and a minimum amount outstanding of $250 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and US
            /// domestic bond markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. DRD-eligible and defaulted securities are excluded from the Index
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S.
            /// mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming
            /// next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming
            /// same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until
            /// the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while
            /// it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to
            /// and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are
            /// included in the Index for the following month. Issues that no longer meet the criteria during the course of the month
            /// remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateMasterOptionAdjustedSpread = "BAMLC0A0CM";

            ///<summary>
            /// ICE BofAML US High Yield Master II Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A0HYM2
            /// The ICE BofAML Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. The ICE BofAML High Yield Master II OAS uses an index of bonds that are below investment grade (those rated BB or below).
            /// This data represents the ICE BofAML US High Yield Master II Index value, which tracks the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. To qualify for inclusion in the index, securities must have a below investment grade rating (based on an average of Moody's, S&P, and Fitch) and an investment grade rated country of risk (based on an average of Moody's, S&P, and Fitch foreign currency long term sovereign debt ratings). Each security must have greater than 1 year of remaining maturity, a fixed coupon schedule, and a minimum amount outstanding of $100 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and US domestic bond markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. DRD-eligible and defaulted securities are excluded from the Index
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldMasterIIOptionAdjustedSpread = "BAMLH0A0HYM2";

            ///<summary>
            /// ICE BofAML US Corporate 1-3 Year Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC1A0C13Y
            /// The ICE BofAML Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. The US Corporate 1-3 Year OAS is a subset of the ICE BofAML US Corporate Master OAS, BAMLC0A0CM. This subset includes all securities with a remaining term to maturity of less than 3 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate1To3YearOptionAdjustedSpread = "BAMLC1A0C13Y";

            ///<summary>
            /// ICE BofAML US Corporate 10-15 Year Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC7A0C1015Y
            /// The ICE BofAML Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. The US Corporate 10-15 Year OAS is a subset of the ICE BofAML US Corporate Master OAS, BAMLC0A0CM. This subset includes all securities with a remaining term to maturity of greater than or equal to 10 years and less than 15 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate10To15YearOptionAdjustedSpread = "BAMLC7A0C1015Y";

            ///<summary>
            /// ICE BofAML US Corporate 15+ Year Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC8A0C15PY
            /// The ICE BofAML Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. The US Corporate 15+ Year OAS is a subset of the ICE BofAML US Corporate Master OAS, BAMLC0A0CM. This subset includes all securities with a remaining term to maturity of greater than or equal to 15 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateMoreThan15YearOptionAdjustedSpread = "BAMLC8A0C15PY";

            ///<summary>
            /// ICE BofAML US Corporate 3-5 Year Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC2A0C35Y
            /// The ICE BofAML Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. The US Corporate 3-5 Year OAS is a subset of the ICE BofAML US Corporate Master OAS, BAMLC0A0CM. This subset includes all securities with a remaining term to maturity of greater than or equal to 3 years and less than 5 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate3To5YearOptionAdjustedSpread = "BAMLC2A0C35Y";

            ///<summary>
            /// ICE BofAML US Corporate 5-7 Year Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC3A0C57Y
            /// The ICE BofAML Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. The US Corporate 5-7 Year OAS is a subset of the ICE BofAML US Corporate Master OAS, BAMLC0A0CM. This subset includes all securities with a remaining term to maturity of greater than or equal to 5 years and less than 7 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate5To7YearOptionAdjustedSpread = "BAMLC3A0C57Y";

            ///<summary>
            /// ICE BofAML US Corporate 7-10 Year Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC4A0C710Y
            /// The ICE BofAML Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. The US Corporate 7-10 Year OAS is a subset of the ICE BofAML US Corporate Master OAS, BAMLC0A0CM. This subset includes all securities with a remaining term to maturity of greater than or equal to 7 years and less than 10 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate7To10YearOptionAdjustedSpread = "BAMLC4A0C710Y";

            ///<summary>
            /// ICE BofAML Public Sector Issuers US Emerging Markets Liquid Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMPUPUBSLCRPIUSTRIV
            /// The ICE BofAML Public Sector US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all quasi-government securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string PublicSectorIssuersUSEmergingMarketsLiquidCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMPUPUBSLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML US Emerging Markets Corporate Plus Sub-Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMUBCRPIUSTRIV
            /// The ICE BofAML US Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities denominated in US Dollars. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USEmergingMarketsCorporatePlusSubIndexTotalReturnIndexValue = "BAMLEMUBCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML US Emerging Markets Liquid Corporate Plus Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV
            /// The ICE BofAML US Emerging Markets Liquid Corporate Plus Index tracks the performance of US dollar (USD) denominated emerging markets non-sovereign debt publicly issued in the major domestic and Eurobond markets. To qualify for inclusion in the index, the issuer of debt must have risk exposure to countries other than members of the FX G10 (US, Japan, New Zealand, Australia, Canada, Sweden, UK, Switzerland, Norway, and Euro Currency Members), all Western European countries, and territories of the US and Western European countries. Each security must also be denominated in USD with a time to maturity greater than 1 year until final maturity and have a fixed coupon. For inclusion in the index, investment grade rated bonds of qualifying issuers must have at least $500 million in outstanding face value, and below investment grade rated bonds must have at least $300 million in outstanding face value. The index includes corporate and quasi-government debt of qualifying countries, but excludes sovereign and supranational debt. Other types of securities acceptable for inclusion in this index are: original issue zero coupon bonds, ‚Äúglobal‚Äù securities (debt issued in the US domestic bond markets as well the Eurobond Market simultaneously), 144a securities, pay-in-kind securities (includes toggle notes), callable perpetual securities (qualify if they are at least one year from the first call date), fixed-t-floating rate securities (qualify if the securities are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security). Defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding times the market price plus accrued interest, subject to a 10% country of risk cap and a 2% issuer cap. Countries and issuers that exceed the limits are reduced to 10% and 2%, respectively, and the face value of each of their bonds is adjusted on a pro-rata basis. Similarly, the face values of bonds of all other countries and issuers that fall below their respective caps are increased on a pro-rata basis. In the event there are fewer than 10 countries in the Index, or fewer than 50 issuers, each is equally weighted and the face values of their respective bonds are increased or decreased on a pro-rata basis. Accrued interest is calculated assuming next-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USEmergingMarketsLiquidCorporatePlusIndexTotalReturnIndexValue = "BAMLEMCLLCRPIUSTRIV";

            ///<summary>
            /// ICE BofAML Euro High Yield Index Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLHE00EHYITRIV
            /// This data represents the ICE BofAML Euro High Yield Index value, which tracks the performance of Euro denominated below investment grade corporate debt publicly issued in the euro domestic or eurobond markets. Qualifying securities must have a below investment grade rating (based on an average of Moody's, S&P, and Fitch). Qualifying securities must have at least one year remaining term to maturity, a fixed coupon schedule, and a minimum amount outstanding of Euro 100 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and euro domestic markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. Defaulted, warrant-bearing and euro legacy currency securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until
            /// the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EuroHighYieldIndexTotalReturnIndexValue = "BAMLHE00EHYITRIV";

            ///<summary>
            /// ICE BofAML US Corp 1-3yr Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC1A013YTRIV
            /// This data represents the ICE BofAML US Corporate 1-3 Year Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of less than 3 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorp1To3YearsTotalReturnIndexValue = "BAMLCC1A013YTRIV";

            ///<summary>
            /// ICE BofAML US Corp 10-15yr Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC7A01015YTRIV
            /// This data represents the ICE BofAML US Corporate 10-15 Year Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 10 years and less than 15 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorp10To15TotalReturnIndexValue = "BAMLCC7A01015YTRIV";

            ///<summary>
            /// ICE BofAML US Corp 15+yr Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC8A015PYTRIV
            /// This data represents the ICE BofAML US Corporate 15+ Year Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 15 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorpMoreThan15YearsTotalReturnIndexValue = "BAMLCC8A015PYTRIV";

            ///<summary>
            /// ICE BofAML US Corp 3-5yr Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC2A035YTRIV
            /// This data represents the ICE BofAML US Corporate 3-5 Year Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 3 years and less than 5 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorpeTo5YearsTotalReturnIndexValue = "BAMLCC2A035YTRIV";

            ///<summary>
            /// ICE BofAML US Corp 5-7yr Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC3A057YTRIV
            /// This data represents the ICE BofAML US Corporate 5-7 Year Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 5 years and less than 7 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorp5To7YearsTotalReturnIndexValue = "BAMLCC3A057YTRIV";

            ///<summary>
            /// ICE BofAML US Corporate 7-10yr Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC4A0710YTRIV
            /// This data represents the ICE BofAML US Corporate 7-10 Year Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 7 years and less than 10 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate7To10YearsTotalReturnIndexValue = "BAMLCC4A0710YTRIV";

            ///<summary>
            /// ICE BofAML US Corp A Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC0A3ATRIV
            /// This data represents the ICE BofAML US Corporate A Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating A. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorpATotalReturnIndexValue = "BAMLCC0A3ATRIV";

            ///<summary>
            /// ICE BofAML US Corp AA Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC0A2AATRIV
            /// This data represents the ICE BofAML US Corporate AA Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating AA. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorpAATotalReturnIndexValue = "BAMLCC0A2AATRIV";

            ///<summary>
            /// ICE BofAML US Corp AAA Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC0A1AAATRIV
            /// This data represents the ICE BofAML US Corporate AAA Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating AAA. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorpAAATotalReturnIndexValue = "BAMLCC0A1AAATRIV";

            ///<summary>
            /// ICE BofAML US High Yield B Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLHYH0A2BTRIV
            /// This data represents the ICE BofAML US Corporate B Index value, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating B. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldBTotalReturnIndexValue = "BAMLHYH0A2BTRIV";

            ///<summary>
            /// ICE BofAML US High Yield BB Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLHYH0A1BBTRIV
            /// This data represents the ICE BofAML US Corporate BB Index value, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating BB. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldBBTotalReturnIndexValue = "BAMLHYH0A1BBTRIV";

            ///<summary>
            /// ICE BofAML US Corp BBB Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC0A4BBBTRIV
            /// This data represents the ICE BofAML US Corporate BBB Index value, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating BBB. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorpBBBTotalReturnIndexValue = "BAMLCC0A4BBBTRIV";

            ///<summary>
            /// ICE BofAML US High Yield CCC or Below Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLHYH0A3CMTRIV
            /// This data represents the ICE BofAML US Corporate C Index value, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating CCC or below. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldCCCorBelowTotalReturnIndexValue = "BAMLHYH0A3CMTRIV";

            ///<summary>
            /// ICE BofAML US Corp Master Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLCC0A0CMTRIV
            /// This data represents the ICE BofAML US Corporate Master Index value, which tracks the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. To qualify for inclusion in the index, securities must have an investment grade rating (based on an average of Moody's, S&P, and Fitch) and an investment grade rated country of risk (based on an average of Moody's, S&P, and Fitch foreign currency long term sovereign debt ratings). Each security must have greater than 1 year of remaining maturity, a fixed coupon schedule, and a minimum amount outstanding of $250 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and US
            /// domestic bond markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. DRD-eligible and defaulted securities are excluded from the Index
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorpMasterTotalReturnIndexValue = "BAMLCC0A0CMTRIV";

            ///<summary>
            /// ICE BofAML US High Yield Master II Total Return Index Value (in Index)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLHYH0A0HYM2TRIV
            /// This data represents the ICE BofAML US High Yield Master II Index value, which tracks the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. To qualify for inclusion in the index, securities must have a below investment grade rating (based on an average of Moody's, S&P, and Fitch) and an investment grade rated country of risk (based on an average of Moody's, S&P, and Fitch foreign currency long term sovereign debt ratings). Each security must have greater than 1 year of remaining maturity, a fixed coupon schedule, and a minimum amount outstanding of $100 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and US domestic bond markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. DRD-eligible and defaulted securities are excluded from the Index
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldMasterIITotalReturnIndexValue = "BAMLHYH0A0HYM2TRIV";

            ///<summary>
            /// ICE BofAML AAA-A Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM1BRRAAA2ACRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML AAA-A Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated AAA through A3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AAAAEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM1BRRAAA2ACRPIOAS";

            ///<summary>
            /// ICE BofAML AAA-A US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM1RAAA2ALCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML AAA-A US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML US Emerging Markets Liquid Corporate Plus Index, which includes only securities rated AAA through A3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AAAAUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM1RAAA2ALCRPIUSOAS";

            ///<summary>
            /// ICE BofAML Asia Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRACRPIASIAOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Asia Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Asia, excluding Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AsiaEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMRACRPIASIAOAS";

            ///<summary>
            /// ICE BofAML Asia US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMALLCRPIASIAUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Asia US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Asia, excluding Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AsiaUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMALLCRPIASIAUSOAS";

            ///<summary>
            /// ICE BofAML B and Lower Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM4BRRBLCRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML B and Lower Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated B1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BandLowerEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM4BRRBLCRPIOAS";

            ///<summary>
            /// ICE BofAML B and Lower US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM4RBLLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML B and Lower US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated B1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BandLowerUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM4RBLLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML BB Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM3BRRBBCRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML BB Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM3BRRBBCRPIOAS";

            ///<summary>
            /// ICE BofAML BB US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM3RBBLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML BB US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM3RBBLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML BBB Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM2BRRBBBCRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML BBB Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BBB1 through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBBEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM2BRRBBBCRPIOAS";

            ///<summary>
            /// ICE BofAML BBB US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM2RBBBLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML BBB US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BBB1 through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBBUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM2RBBBLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML Crossover Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM5BCOCRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Crossover Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BBB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string CrossoverEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEM5BCOCRPIOAS";

            ///<summary>
            /// ICE BofAML Crossover US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMXOCOLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Crossover US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BBB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string CrossoverUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMXOCOLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML Emerging Markets Corporate Plus Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMCBPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413), which tracks the performance of US dollar (USD) and Euro denominated emerging markets non-sovereign debt publicly issued within the major domestic and Eurobond markets. To qualify for inclusion in the index, the issuer of debt must have risk exposure to countries other than members of the FX G10 (US, Japan, New Zealand, Australia, Canada, Sweden, UK, Switzerland, Norway, and Euro Currency Members), all Western European countries, and territories of the US and Western European countries. Each security must also be denominated in USD or Euro with a time to maturity greater than 1 year and have a fixed coupon. For inclusion in the index, investment grade rated bonds of qualifying issuers must have at least 250 million (Euro or USD) in outstanding face value, and below investment grade rated bonds must have at least 100 million (Euro or USD) in outstanding face value. The index includes corporate and quasi-government debt of qualifying countries, but excludes sovereign and supranational debt. Other types of securities acceptable for inclusion in this index are: original issue zero coupon bonds, ‚Äúglobal‚Äù securities (debt issued in the US domestic bond markets as well the Eurobond Market simultaneously), 144a securities, pay-in-kind securities (includes toggle notes), callable perpetual securities (qualify if they are at least one year from the first call date), fixed-to-floating rate securities (qualify if the securities are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security). Defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of US mortgage pass-throughs and US structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for US mortgage pass-through and US structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until
            /// the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EmergingMarketsCorporatePlusIndexOptionAdjustedSpread = "BAMLEMCBPIOAS";

            ///<summary>
            /// ICE BofAML Euro Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMEBCRPIEOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Euro Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities denominated in Euros. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EuroEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMEBCRPIEOAS";

            ///<summary>
            /// ICE BofAML Europe, the Middle East, and Africa (EMEA) Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRECRPIEMEAOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Europe, the Middle East, and Africa (EMEA) Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Europe, the Middle East and Africa, also including Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EMEAEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMRECRPIEMEAOAS";

            ///<summary>
            /// ICE BofAML Europe, the Middle East, and Africa (EMEA) US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMELLCRPIEMEAUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Europe, the Middle East, and Africa (EMEA) US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Europe, the Middle East and Africa, also including Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EMEAUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMELLCRPIEMEAUSOAS";

            ///<summary>
            /// ICE BofAML Financial Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMFSFCRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Financial Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes all Financial securities except the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string FinancialEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMFSFCRPIOAS";

            ///<summary>
            /// ICE BofAML Financial US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMFLFLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Financial US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all Financial securities except the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string FinancialUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMFLFLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML High Grade Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMIBHGCRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML High Grade Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated AAA through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighGradeEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMIBHGCRPIOAS";

            ///<summary>
            /// ICE BofAML High Grade US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHGHGLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML High Grade US Emerging Markets Liquid Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated AAA through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighGradeUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMHGHGLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML High Yield Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHBHYCRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML High Yield Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BB1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighYieldEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMHBHYCRPIOAS";

            ///<summary>
            /// ICE BofAML High Yield US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHYHYLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML High Yield US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BB1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighYieldUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMHYHYLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML Latin America Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRLCRPILAOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Latin America Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Latin America. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string LatinAmericaEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMRLCRPILAOAS";

            ///<summary>
            /// ICE BofAML Latin America US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMLLLCRPILAUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Latin America US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Latin America. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string LatinAmericaUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMLLLCRPILAUSOAS";

            ///<summary>
            /// ICE BofAML Non-Financial Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMNSNFCRPIOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Non-Financial Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which excludes all Financial securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string NonFinancialEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMNSNFCRPIOAS";

            ///<summary>
            /// ICE BofAML Non-Financial US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMNFNFLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Non-Financial US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which excludes all Financial securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string NonFinancialUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMNFNFLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML Public Sector Issuers US Emerging Markets Liquid Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMPUPUBSLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML Public Sector US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all quasi-government securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string PublicSectorIssuersUSEmergingMarketsLiquidCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMPUPUBSLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML US Emerging Markets Corporate Plus Sub-Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMUBCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML US Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities denominated in US Dollars. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413).
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USEmergingMarketsCorporatePlusSubIndexOptionAdjustedSpread = "BAMLEMUBCRPIUSOAS";

            ///<summary>
            /// ICE BofAML US Emerging Markets Liquid Corporate Plus Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSOAS
            /// This data represents the Option-Adjusted Spread (OAS) for the ICE BofAML US Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413) tracks the performance of US dollar (USD) denominated emerging markets non-sovereign debt publicly issued in the major domestic and Eurobond markets. To qualify for inclusion in the index, the issuer of debt must have risk exposure to countries other than members of the FX G10 (US, Japan, New Zealand, Australia, Canada, Sweden, UK, Switzerland, Norway, and Euro Currency Members), all Western European countries, and territories of the US and Western European countries. Each security must also be denominated in USD with a time to maturity greater than 1 year until final maturity and have a fixed coupon. For inclusion in the index, investment grade rated bonds of qualifying issuers must have at least $500 million in outstanding face value, and below investment grade rated bonds must have at least $300 million in outstanding face value. The index includes corporate and quasi-government debt of qualifying countries, but excludes sovereign and supranational debt. Other types of securities acceptable for inclusion in this index are: original issue zero coupon bonds, ‚Äúglobal‚Äù securities (debt issued in the US domestic bond markets as well the Eurobond Market simultaneously), 144a securities, pay-in-kind securities (includes toggle notes), callable perpetual securities (qualify if they are at least one year from the first call date), fixed-t-floating rate securities (qualify if the securities are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security). Defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding times the market price plus accrued interest, subject to a 10% country of risk cap and a 2% issuer cap. Countries and issuers that exceed the limits are reduced to 10% and 2%, respectively, and the face value of each of their bonds is adjusted on a pro-rata basis. Similarly, the face values of bonds of all other countries and issuers that fall below their respective caps are increased on a pro-rata basis. In the event there are fewer than 10 countries in the Index, or fewer than 50 issuers, each is equally weighted and the face values of their respective bonds are increased or decreased on a pro-rata basis. Accrued interest is calculated assuming next-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USEmergingMarketsLiquidCorporatePlusIndexOptionAdjustedSpread = "BAMLEMCLLCRPIUSOAS";

            ///<summary>
            /// ICE BofAML Euro High Yield Index Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLHE00EHYIOAS
            /// This data represents the Option-Adjusted Spread (OAS) of the ICE BofAML Euro High Yield Index tracks the performance of Euro denominated below investment grade corporate debt publicly issued in the euro domestic or eurobond markets. Qualifying securities must have a below investment grade rating (based on an average of Moody's, S&P, and Fitch). Qualifying securities must have at least one year remaining term to maturity, a fixed coupon schedule, and a minimum amount outstanding of Euro 100 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and euro domestic markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. Defaulted, warrant-bearing and euro legacy currency securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until
            /// the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond's OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EuroHighYieldIndexOptionAdjustedSpread = "BAMLHE00EHYIOAS";

            ///<summary>
            /// ICE BofAML US Corporate A Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A3CA
            /// This data represents the Option-Adjusted Spread (OAS) of the ICE BofAML US Corporate A Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating A.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateAOptionAdjustedSpread = "BAMLC0A3CA";

            ///<summary>
            /// ICE BofAML US Corporate AA Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A2CAA
            /// This data represents the Option-Adjusted Spread (OAS) of the ICE BofAML US Corporate AA Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating AA.
            /// The ICE BofAML OAS are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateAAOptionAdjustedSpread = "BAMLC0A2CAA";

            ///<summary>
            /// ICE BofAML US Corporate AAA Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A1CAAA
            /// This data represents the Option-Adjusted Spread (OAS) of the ICE BofAML US Corporate AAA Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating AAA.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateAAAOptionAdjustedSpread = "BAMLC0A1CAAA";

            ///<summary>
            /// ICE BofAML US High Yield B Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A2HYB
            /// This data represents the Option-Adjusted Spread (OAS) of the ICE BofAML US Corporate B Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating B.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldBOptionAdjustedSpread = "BAMLH0A2HYB";

            ///<summary>
            /// ICE BofAML US High Yield BB Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A1HYBB
            /// This data represents the Option-Adjusted Spread (OAS) of the ICE BofAML US Corporate BB Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating BB.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldBBOptionAdjustedSpread = "BAMLH0A1HYBB";

            ///<summary>
            /// ICE BofAML US Corporate BBB Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A4CBBB
            /// This data represents the Option-Adjusted Spread (OAS) of the ICE BofAML US Corporate BBB Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating BBB.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateBBBOptionAdjustedSpread = "BAMLC0A4CBBB";

            ///<summary>
            /// ICE BofAML US High Yield CCC or Below Option-Adjusted Spread (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A3HYC
            /// This data represents the Option-Adjusted Spread (OAS) of the ICE BofAML US Corporate C Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating CCC or below.
            /// The ICE BofAML OASs are the calculated spreads between a computed OAS index of all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent bond‚Äôs OAS, weighted by market capitalization. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldCCCorBelowOptionAdjustedSpread = "BAMLH0A3HYC";

            ///<summary>
            /// ICE BofAML AAA-A Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM1BRRAAA2ACRPIEY
            /// This data represents the effective yield of the ICE BofAML AAA-A Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated AAA through A3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AAAAEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEM1BRRAAA2ACRPIEY";

            ///<summary>
            /// ICE BofAML AAA-A US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM1RAAA2ALCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML AAA-A US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML US Emerging Markets Liquid Corporate Plus Index, which includes only securities rated AAA through A3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AAAAUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEM1RAAA2ALCRPIUSEY";

            ///<summary>
            /// ICE BofAML Asia Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRACRPIASIAEY
            /// This data represents the effective yield of the ICE BofAML Asia Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Asia, excluding Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AsiaEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMRACRPIASIAEY";

            ///<summary>
            /// ICE BofAML Asia US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMALLCRPIASIAUSEY
            /// This data represents the effective yield of the ICE BofAML Asia US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Asia, excluding Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AsiaUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMALLCRPIASIAUSEY";

            ///<summary>
            /// ICE BofAML B and Lower Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM4BRRBLCRPIEY
            /// This data represents the effective yield of the ICE BofAML B and Lower Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated B1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BandLowerEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEM4BRRBLCRPIEY";

            ///<summary>
            /// ICE BofAML B and Lower US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM4RBLLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML B and Lower US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated B1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BandLowerUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEM4RBLLCRPIUSEY";

            ///<summary>
            /// ICE BofAML BB Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM3BRRBBCRPIEY
            /// This data represents the effective yield of the ICE BofAML BB Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEM3BRRBBCRPIEY";

            ///<summary>
            /// ICE BofAML BB US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM3RBBLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML BB US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEM3RBBLCRPIUSEY";

            ///<summary>
            /// ICE BofAML BBB Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM2BRRBBBCRPIEY
            /// This data represents the effective yield of the ICE BofAML BBB Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BBB1 through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBBEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEM2BRRBBBCRPIEY";

            ///<summary>
            /// ICE BofAML BBB US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM2RBBBLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML BBB US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BBB1 through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBBUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEM2RBBBLCRPIUSEY";

            ///<summary>
            /// ICE BofAML Crossover Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM5BCOCRPIEY
            /// This data represents the effective yield of the ICE BofAML Crossover Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BBB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string CrossoverEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEM5BCOCRPIEY";

            ///<summary>
            /// ICE BofAML Crossover US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMXOCOLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML Crossover US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BBB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string CrossoverUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMXOCOLCRPIUSEY";

            ///<summary>
            /// ICE BofAML Emerging Markets Corporate Plus Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMCBPIEY
            /// This data represents the effective yield of the ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413), which tracks the performance of US dollar (USD) and Euro denominated emerging markets non-sovereign debt publicly issued within the major domestic and Eurobond markets. To qualify for inclusion in the index, the issuer of debt must have risk exposure to countries other than members of the FX G10 (US, Japan, New Zealand, Australia, Canada, Sweden, UK, Switzerland, Norway, and Euro Currency Members), all Western European countries, and territories of the US and Western European countries. Each security must also be denominated in USD or Euro with a time to maturity greater than 1 year and have a fixed coupon. For inclusion in the index, investment grade rated bonds of qualifying issuers must have at least 250 million (Euro or USD) in outstanding face value, and below investment grade rated bonds must have at least 100 million (Euro or USD) in outstanding face value. The index includes corporate and quasi-government debt of qualifying countries, but excludes sovereign and supranational debt. Other types of securities acceptable for inclusion in this index are: original issue zero coupon bonds, ‚Äúglobal‚Äù securities (debt issued in the US domestic bond markets as well the Eurobond Market simultaneously), 144a securities, pay-in-kind securities (includes toggle notes), callable perpetual securities (qualify if they are at least one year from the first call date), fixed-to-floating rate securities (qualify if the securities are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security). Defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of US mortgage pass-throughs and US structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for US mortgage pass-through and US structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EmergingMarketsCorporatePlusIndexEffectiveYield = "BAMLEMCBPIEY";

            ///<summary>
            /// ICE BofAML Euro Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMEBCRPIEEY
            /// This data represents the effective yield of the ICE BofAML Euro Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities denominated in Euros. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EuroEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMEBCRPIEEY";

            ///<summary>
            /// ICE BofAML Euro High Yield Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLHE00EHYIEY
            /// This data represents the effective yield of the ICE BofAML Euro High Yield Index tracks the performance of Euro denominated below investment grade corporate debt publicly issued in the euro domestic or eurobond markets. Qualifying securities must have a below investment grade rating (based on an average of Moody's, S&P, and Fitch). Qualifying securities must have at least one year remaining term to maturity, a fixed coupon schedule, and a minimum amount outstanding of Euro 100 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and euro domestic markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. Defaulted, warrant-bearing and euro legacy currency securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until
            /// the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EuroHighYieldIndexEffectiveYield = "BAMLHE00EHYIEY";

            ///<summary>
            /// ICE BofAML Europe, the Middle East, and Africa (EMEA) Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRECRPIEMEAEY
            /// This data represents the effective yield of the ICE BofAML Europe, the Middle East, and Africa (EMEA) Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Europe, the Middle East and Africa, also including Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EMEAEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMRECRPIEMEAEY";

            ///<summary>
            /// ICE BofAML Europe, the Middle East, and Africa (EMEA) US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMELLCRPIEMEAUSEY
            /// This data represents the effective yield of the ICE BofAML Europe, the Middle East, and Africa (EMEA) US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Europe, the Middle East and Africa, also including Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EMEAUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMELLCRPIEMEAUSEY";

            ///<summary>
            /// ICE BofAML Financial Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMFSFCRPIEY
            /// This data represents the effective yield of the ICE BofAML Financial Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes all Financial securities except the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string FinancialEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMFSFCRPIEY";

            ///<summary>
            /// ICE BofAML Financial US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMFLFLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML Financial US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all Financial securities except the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string FinancialUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMFLFLCRPIUSEY";

            ///<summary>
            /// ICE BofAML High Grade Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMIBHGCRPIEY
            /// This data represents the effective yield of the ICE BofAML High Grade Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated AAA through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighGradeEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMIBHGCRPIEY";

            ///<summary>
            /// ICE BofAML High Grade US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHGHGLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML High Grade US Emerging Markets Liquid Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated AAA through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighGradeUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMHGHGLCRPIUSEY";

            ///<summary>
            /// ICE BofAML High Yield Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHBHYCRPIEY
            /// This data represents the effective yield of the ICE BofAML High Yield Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BB1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighYieldEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMHBHYCRPIEY";

            ///<summary>
            /// ICE BofAML High Yield US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHYHYLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML High Yield US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BB1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighYieldUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMHYHYLCRPIUSEY";

            ///<summary>
            /// ICE BofAML Latin America Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRLCRPILAEY
            /// This data represents the effective yield of the ICE BofAML Latin America Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Latin America. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string LatinAmericaEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMRLCRPILAEY";

            ///<summary>
            /// ICE BofAML Latin America US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMLLLCRPILAUSEY
            /// This data represents the effective yield of the ICE BofAML Latin America US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Latin America. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string LatinAmericaUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMLLLCRPILAUSEY";

            ///<summary>
            /// ICE BofAML Non-Financial Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMNSNFCRPIEY
            /// This data represents the effective yield of the ICE BofAML Non-Financial Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which excludes all Financial securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string NonFinancialEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMNSNFCRPIEY";

            ///<summary>
            /// ICE BofAML Non-Financial US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMNFNFLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML Non-Financial US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which excludes all Financial securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string NonFinancialUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMNFNFLCRPIUSEY";

            ///<summary>
            /// ICE BofAML Public Sector Issuers US Emerging Markets Liquid Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMPUPUBSLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML Public Sector US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all quasi-government securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string PublicSectorIssuersUSEmergingMarketsLiquidCorporatePlusSubIndexEffectiveYield = "BAMLEMPUPUBSLCRPIUSEY";

            ///<summary>
            /// ICE BofAML US Corporate 1-3 Year Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC1A0C13YEY
            /// This data represents the effective yield of the ICE BofAML US Corporate 1-3 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of less than 3 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate1ThreeYearEffectiveYield = "BAMLC1A0C13YEY";

            ///<summary>
            /// ICE BofAML US Corporate 10-15 Year Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC7A0C1015YEY
            /// This data represents the effective yield of the ICE BofAML US Corporate 10-15 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 10 years and less than 15 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate10To15YearEffectiveYield = "BAMLC7A0C1015YEY";

            ///<summary>
            /// ICE BofAML US Corporate 15+ Year Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC8A0C15PYEY
            /// This data represents the effective yield of the ICE BofAML US Corporate 15+ Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 15 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateMoreThan15YearEffectiveYield = "BAMLC8A0C15PYEY";

            ///<summary>
            /// ICE BofAML US Corporate 3-5 Year Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC2A0C35YEY
            /// This data represents the effective yield of the ICE BofAML US Corporate 3-5 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 3 years and less than 5 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate3To5YearEffectiveYield = "BAMLC2A0C35YEY";

            ///<summary>
            /// ICE BofAML US Corporate 5-7 Year Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC3A0C57YEY
            /// This data represents the effective yield of the ICE BofAML US Corporate 5-7 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 5 years and less than 7 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate5To7YearEffectiveYield = "BAMLC3A0C57YEY";

            ///<summary>
            /// ICE BofAML US Corporate 7-10 Year Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC4A0C710YEY
            /// This data represents the effective yield of the ICE BofAML US Corporate 7-10 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 7 years and less than 10 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate7To10YearEffectiveYield = "BAMLC4A0C710YEY";

            ///<summary>
            /// ICE BofAML US Corporate A Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A3CAEY
            /// This data represents the effective yield of the ICE BofAML US Corporate A Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating A. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateAEffectiveYield = "BAMLC0A3CAEY";

            ///<summary>
            /// ICE BofAML US Corporate AA Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A2CAAEY
            /// This data represents the effective yield of the ICE BofAML US Corporate AA Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating AA. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateAAEffectiveYield = "BAMLC0A2CAAEY";

            ///<summary>
            /// ICE BofAML US Corporate AAA Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A1CAAAEY
            /// This data represents the effective yield of the ICE BofAML US Corporate AAA Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating AAA. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateAAAEffectiveYield = "BAMLC0A1CAAAEY";

            ///<summary>
            /// ICE BofAML US High Yield B Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A2HYBEY
            /// This data represents the effective yield of the ICE BofAML US Corporate B Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating B. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldBEffectiveYield = "BAMLH0A2HYBEY";

            ///<summary>
            /// ICE BofAML US High Yield BB Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A1HYBBEY
            /// This data represents the effective yield of the ICE BofAML US Corporate BB Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating BB. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldBBEffectiveYield = "BAMLH0A1HYBBEY";

            ///<summary>
            /// ICE BofAML US Corporate BBB Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A4CBBBEY
            /// This data represents the effective yield of the ICE BofAML US Corporate BBB Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating BBB. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateBBBEffectiveYield = "BAMLC0A4CBBBEY";

            ///<summary>
            /// ICE BofAML US High Yield CCC or Below Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A3HYCEY
            /// This data represents the effective yield of the ICE BofAML US Corporate C Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating CCC or below. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldCCCorBelowEffectiveYield = "BAMLH0A3HYCEY";

            ///<summary>
            /// ICE BofAML US Corporate Master Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A0CMEY
            /// This data represents the effective yield of the ICE BofAML US Corporate Master Index, which tracks the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. To qualify for inclusion in the index, securities must have an investment grade rating (based on an average of Moody's, S&P, and Fitch) and an investment grade rated country of risk (based on an average of Moody's, S&P, and Fitch foreign currency long term sovereign debt ratings). Each security must have greater than 1 year of remaining maturity, a fixed coupon schedule, and a minimum amount outstanding of $250 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and US domestic bond markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. DRD-eligible and defaulted securities are excluded from the Index
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateMasterEffectiveYield = "BAMLC0A0CMEY";

            ///<summary>
            /// ICE BofAML US Emerging Markets Corporate Plus Sub-Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMUBCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML US Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities denominated in US Dollars. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USEmergingMarketsCorporatePlusSubIndexEffectiveYield = "BAMLEMUBCRPIUSEY";

            ///<summary>
            /// ICE BofAML US Emerging Markets Liquid Corporate Plus Index Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSEY
            /// This data represents the effective yield of the ICE BofAML US Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413) tracks the performance of US dollar (USD) denominated emerging markets non-sovereign debt publicly issued in the major domestic and Eurobond markets. To qualify for inclusion in the index, the issuer of debt must have risk exposure to countries other than members of the FX G10 (US, Japan, New Zealand, Australia, Canada, Sweden, UK, Switzerland, Norway, and Euro Currency Members), all Western European countries, and territories of the US and Western European countries. Each security must also be denominated in USD with a time to maturity greater than 1 year until final maturity and have a fixed coupon. For inclusion in the index, investment grade rated bonds of qualifying issuers must have at least $500 million in outstanding face value, and below investment grade rated bonds must have at least $300 million in outstanding face value. The index includes corporate and quasi-government debt of qualifying countries, but excludes sovereign and supranational debt. Other types of securities acceptable for inclusion in this index are: original issue zero coupon bonds, ‚Äúglobal‚Äù securities (debt issued in the US domestic bond markets as well the Eurobond Market simultaneously), 144a securities, pay-in-kind securities (includes toggle notes), callable perpetual securities (qualify if they are at least one year from the first call date), fixed-t-floating rate securities (qualify if the securities are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security). Defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding times the market price plus accrued interest, subject to a 10% country of risk cap and a 2% issuer cap. Countries and issuers that exceed the limits are reduced to 10% and 2%, respectively, and the face value of each of their bonds is adjusted on a pro-rata basis. Similarly, the face values of bonds of all other countries and issuers that fall below their respective caps are increased on a pro-rata basis. In the event there are fewer than 10 countries in the Index, or fewer than 50 issuers, each is equally weighted and the face values of their respective bonds are increased or decreased on a pro-rata basis. Accrued interest is calculated assuming next-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USEmergingMarketsLiquidCorporatePlusIndexEffectiveYield = "BAMLEMCLLCRPIUSEY";

            ///<summary>
            /// ICE BofAML US High Yield Master II Effective Yield (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A0HYM2EY
            /// This data represents the effective yield of the ICE BofAML US High Yield Master II Index, which tracks the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. To qualify for inclusion in the index, securities must have a below investment grade rating (based on an average of Moody's, S&P, and Fitch) and an investment grade rated country of risk (based on an average of Moody's, S&P, and Fitch foreign currency long term sovereign debt ratings). Each security must have greater than 1 year of remaining maturity, a fixed coupon schedule, and a minimum amount outstanding of $100 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and US domestic bond markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. DRD-eligible and defaulted securities are excluded from the Index
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldMasterIIEffectiveYield = "BAMLH0A0HYM2EY";

            ///<summary>
            /// ICE BofAML AAA-A Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM1BRRAAA2ACRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML AAA-A Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated AAA through A3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AAAAEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM1BRRAAA2ACRPISYTW";

            ///<summary>
            /// ICE BofAML AAA-A US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM1RAAA2ALCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML AAA-A US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML US Emerging Markets Liquid Corporate Plus Index, which includes only securities rated AAA through A3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AAAAUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM1RAAA2ALCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML Asia Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRACRPIASIASYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Asia Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Asia, excluding Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AsiaEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMRACRPIASIASYTW";

            ///<summary>
            /// ICE BofAML Asia US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMALLCRPIASIAUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Asia US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Asia, excluding Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string AsiaUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMALLCRPIASIAUSSYTW";

            ///<summary>
            /// ICE BofAML B and Lower Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM4BRRBLCRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML B and Lower Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated B1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BandLowerEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM4BRRBLCRPISYTW";

            ///<summary>
            /// ICE BofAML B and Lower US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM4RBLLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML B and Lower US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated B1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BandLowerUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM4RBLLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML BB Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM3BRRBBCRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML BB Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM3BRRBBCRPISYTW";

            ///<summary>
            /// ICE BofAML BB US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM3RBBLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML BB US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM3RBBLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML BBB Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM2BRRBBBCRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML BBB Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BBB1 through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBBEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM2BRRBBBCRPISYTW";

            ///<summary>
            /// ICE BofAML BBB US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM2RBBBLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML BBB US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BBB1 through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string BBBUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM2RBBBLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML Crossover Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEM5BCOCRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Crossover Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BBB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string CrossoverEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEM5BCOCRPISYTW";

            ///<summary>
            /// ICE BofAML Crossover US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMXOCOLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Crossover US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BBB1 through BB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string CrossoverUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMXOCOLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML Emerging Markets Corporate Plus Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMCBPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413), which tracks the performance of US dollar (USD) and Euro denominated emerging markets non-sovereign debt publicly issued within the major domestic and Eurobond markets. To qualify for inclusion in the index, the issuer of debt must have risk exposure to countries other than members of the FX G10 (US, Japan, New Zealand, Australia, Canada, Sweden, UK, Switzerland, Norway, and Euro Currency Members), all Western European countries, and territories of the US and Western European countries. Each security must also be denominated in USD or Euro with a time to maturity greater than 1 year and have a fixed coupon. For inclusion in the index, investment grade rated bonds of qualifying issuers must have at least 250 million (Euro or USD) in outstanding face value, and below investment grade rated bonds must have at least 100 million (Euro or USD) in outstanding face value. The index includes corporate and quasi-government debt of qualifying countries, but excludes sovereign and supranational debt. Other types of securities acceptable for inclusion in this index are: original issue zero coupon bonds, "global" securities (debt issued in the US domestic bond markets as well the Eurobond Market simultaneously), 144a securities, pay-in-kind securities (includes toggle notes), callable perpetual securities (qualify if they are at least one year from the first call date), fixed-to-floating rate securities (qualify if the securities are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security). Defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of US mortgage pass-throughs and US structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for US mortgage pass-through and US structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payment frequencies of annual, semi-annual, quarterly, and monthly. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EmergingMarketsCorporatePlusIndexSemiAnnualYieldtoWorst = "BAMLEMCBPISYTW";

            ///<summary>
            /// ICE BofAML Euro Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMEBCRPIESYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Euro Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities denominated in Euros. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EuroEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMEBCRPIESYTW";

            ///<summary>
            /// ICE BofAML Euro High Yield Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLHE00EHYISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Euro High Yield Index tracks the performance of Euro denominated below investment grade corporate debt publicly issued in the euro domestic or eurobond markets. Qualifying securities must have a below investment grade rating (based on an average of Moody's, S&P, and Fitch). Qualifying securities must have at least one year remaining term to maturity, a fixed coupon schedule, and a minimum amount outstanding of Euro 100 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and euro domestic markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. Defaulted, warrant-bearing and euro legacy currency securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until
            /// the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payment frequencies of annual, semi-annual, quarterly, and monthly. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EuroHighYieldIndexSemiAnnualYieldtoWorst = "BAMLHE00EHYISYTW";

            ///<summary>
            /// ICE BofAML Europe, the Middle East, and Africa (EMEA) Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRECRPIEMEASYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Europe, the Middle East, and Africa (EMEA) Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Europe, the Middle East and Africa, also including Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EMEAEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMRECRPIEMEASYTW";

            ///<summary>
            /// ICE BofAML Europe, the Middle East, and Africa (EMEA) US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMELLCRPIEMEAUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Europe, the Middle East, and Africa (EMEA) US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Europe, the Middle East and Africa, also including Kazakhstan, Kyrgyzstan, Tajikistan, Turkmenistan, and Uzbekistan. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string EMEAUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMELLCRPIEMEAUSSYTW";

            ///<summary>
            /// ICE BofAML Financial Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMFSFCRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Financial Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes all Financial securities except the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string FinancialEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMFSFCRPISYTW";

            ///<summary>
            /// ICE BofAML Financial US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMFLFLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Financial US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all Financial securities except the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string FinancialUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMFLFLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML High Grade Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMIBHGCRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML High Grade Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated AAA through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighGradeEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMIBHGCRPISYTW";

            ///<summary>
            /// ICE BofAML High Grade US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHGHGLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML High Grade US Emerging Markets Liquid Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated AAA through BBB3. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighGradeUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMHGHGLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML High Yield Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHBHYCRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML High Yield Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities rated BB1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighYieldEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMHBHYCRPISYTW";

            ///<summary>
            /// ICE BofAML High Yield US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMHYHYLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML High Yield US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities rated BB1 or lower. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string HighYieldUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMHYHYLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML Latin America Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMRLCRPILASYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Latin America Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities issued by countries associated with the region of Latin America. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string LatinAmericaEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMRLCRPILASYTW";

            ///<summary>
            /// ICE BofAML Latin America US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMLLLCRPILAUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Latin America US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes only securities issued by countries associated with the region of Latin America. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string LatinAmericaUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMLLLCRPILAUSSYTW";

            ///<summary>
            /// ICE BofAML Non-Financial Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMNSNFCRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Non-Financial Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which excludes all Financial securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string NonFinancialEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMNSNFCRPISYTW";

            ///<summary>
            /// ICE BofAML Non-Financial US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMNFNFLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Non-Financial US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which excludes all Financial securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string NonFinancialUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMNFNFLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML Private Sector Issuers Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMPTPRVICRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Private Sector Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes all corporate securities except for the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string PrivateSectorIssuersEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMPTPRVICRPISYTW";

            ///<summary>
            /// ICE BofAML Private Sector Issuers US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMPVPRIVSLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Private Sector US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all corporate securities except for the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string PrivateSectorIssuersUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMPVPRIVSLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML Public Sector Issuers Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMPBPUBSICRPISYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Public Sector Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes all quasi-government securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string PublicSectorIssuersEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMPBPUBSICRPISYTW";

            ///<summary>
            /// ICE BofAML Public Sector Issuers US Emerging Markets Liquid Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMPUPUBSLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML Public Sector US Emerging Markets Liquid Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Liquid Corporate Plus Index, which includes all quasi-government securities as well as the debt of corporate issuers designated as government owned or controlled by ICE BofAML emerging markets credit research. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string PublicSectorIssuersUSEmergingMarketsLiquidCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMPUPUBSLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML US Corporate 1-3 Year Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC1A0C13YSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate 1-3 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of less than 3 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate1To3YearSemiAnnualYieldtoWorst = "BAMLC1A0C13YSYTW";

            ///<summary>
            /// ICE BofAML US Corporate 10-15 Year Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC7A0C1015YSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate 10-15 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 10 years and less than 15 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate10To15YearSemiAnnualYieldtoWorst = "BAMLC7A0C1015YSYTW";

            ///<summary>
            /// ICE BofAML US Corporate 15+ Year Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC8A0C15PYSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate 15+ Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 15 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateMoreThan15YearSemiAnnualYieldtoWorst = "BAMLC8A0C15PYSYTW";

            ///<summary>
            /// ICE BofAML US Corporate 3-5 Year Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC2A0C35YSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate 3-5 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 3 years and less than 5 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate3To5YearSemiAnnualYieldtoWorst = "BAMLC2A0C35YSYTW";

            ///<summary>
            /// ICE BofAML US Corporate 5-7 Year Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC3A0C57YSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate 5-7 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 5 years and less than 7 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate5To7YearSemiAnnualYieldtoWorst = "BAMLC3A0C57YSYTW";

            ///<summary>
            /// ICE BofAML US Corporate 7-10 Year Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC4A0C710YSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate 7-10 Year Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a remaining term to maturity of greater than or equal to 7 years and less than 10 years. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporate7To10YearSemiAnnualYieldtoWorst = "BAMLC4A0C710YSYTW";

            ///<summary>
            /// ICE BofAML US Corporate A Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A3CASYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate A Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating A. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateASemiAnnualYieldtoWorst = "BAMLC0A3CASYTW";

            ///<summary>
            /// ICE BofAML US Corporate AA Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A2CAASYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate AA Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating AA. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateAASemiAnnualYieldtoWorst = "BAMLC0A2CAASYTW";

            ///<summary>
            /// ICE BofAML US Corporate AAA Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A1CAAASYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate AAA Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating AAA. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateAAASemiAnnualYieldtoWorst = "BAMLC0A1CAAASYTW";

            ///<summary>
            /// ICE BofAML US High Yield B Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A2HYBSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate B Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating B. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldBSemiAnnualYieldtoWorst = "BAMLH0A2HYBSYTW";

            ///<summary>
            /// ICE BofAML US High Yield BB Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A1HYBBSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate BB Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating BB. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldBBSemiAnnualYieldtoWorst = "BAMLH0A1HYBBSYTW";

            ///<summary>
            /// ICE BofAML US Corporate BBB Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A4CBBBSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate BBB Index, a subset of the ICE BofAML US Corporate Master Index tracking the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating BBB. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateBBBSemiAnnualYieldtoWorst = "BAMLC0A4CBBBSYTW";

            ///<summary>
            /// ICE BofAML US High Yield CCC or Below Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A3HYCSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate C Index, a subset of the ICE BofAML US High Yield Master II Index tracking the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. This subset includes all securities with a given investment grade rating CCC or below. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldCCCorBelowSemiAnnualYieldtoWorst = "BAMLH0A3HYCSYTW";

            ///<summary>
            /// ICE BofAML US Corporate Master Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLC0A0CMSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Corporate Master Index, which tracks the performance of US dollar denominated investment grade rated corporate debt publically issued in the US domestic market. To qualify for inclusion in the index, securities must have an investment grade rating (based on an average of Moody's, S&P, and Fitch) and an investment grade rated country of risk (based on an average of Moody's, S&P, and Fitch foreign currency long term sovereign debt ratings). Each security must have greater than 1 year of remaining maturity, a fixed coupon schedule, and a minimum amount outstanding of $250 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and US domestic bond markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. DRD-eligible and defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USCorporateMasterSemiAnnualYieldtoWorst = "BAMLC0A0CMSYTW";

            ///<summary>
            /// ICE BofAML US Emerging Markets Corporate Plus Sub-Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMUBCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Emerging Markets Corporate Plus Index is the subset of the ICE BofAML Emerging Markets Corporate Plus Index, which includes only securities denominated in US Dollars. The same inclusion rules apply for this series as those that apply for ICE BofAML Emerging Markets Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCBPITRIV?cid=32413). When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payments with frequencies of annual, semi-annual, quarterly, and monthly.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USEmergingMarketsCorporatePlusSubIndexSemiAnnualYieldtoWorst = "BAMLEMUBCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML US Emerging Markets Liquid Corporate Plus Index Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSSYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US Emerging Markets Liquid Corporate Plus Index (https://fred.stlouisfed.org/series/BAMLEMCLLCRPIUSTRIV?cid=32413) tracks the performance of US dollar (USD) denominated emerging markets non-sovereign debt publicly issued in the major domestic and Eurobond markets. To qualify for inclusion in the index, the issuer of debt must have risk exposure to countries other than members of the FX G10 (US, Japan, New Zealand, Australia, Canada, Sweden, UK, Switzerland, Norway, and Euro Currency Members), all Western European countries, and territories of the US and Western European countries. Each security must also be denominated in USD with a time to maturity greater than 1 year until final maturity and have a fixed coupon. For inclusion in the index, investment grade rated bonds of qualifying issuers must have at least $500 million in outstanding face value, and below investment grade rated bonds must have at least $300 million in outstanding face value. The index includes corporate and quasi-government debt of qualifying countries, but excludes sovereign and supranational debt. Other types of securities acceptable for inclusion in this index are: original issue zero coupon bonds, "global" securities (debt issued in the US domestic bond markets as well the Eurobond Market simultaneously), 144a securities, pay-in-kind securities (includes toggle notes), callable perpetual securities (qualify if they are at least one year from the first call date), fixed-t-floating rate securities (qualify if the securities are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security). Defaulted securities are excluded from the Index.
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding times the market price plus accrued interest, subject to a 10% country of risk cap and a 2% issuer cap. Countries and issuers that exceed the limits are reduced to 10% and 2%, respectively, and the face value of each of their bonds is adjusted on a pro-rata basis. Similarly, the face values of bonds of all other countries and issuers that fall below their respective caps are increased on a pro-rata basis. In the event there are fewer than 10 countries in the Index, or fewer than 50 issuers, each is equally weighted and the face values of their respective bonds are increased or decreased on a pro-rata basis. Accrued interest is calculated assuming next-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payment frequencies of annual, semi-annual, quarterly, and monthly. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USEmergingMarketsLiquidCorporatePlusIndexSemiAnnualYieldtoWorst = "BAMLEMCLLCRPIUSSYTW";

            ///<summary>
            /// ICE BofAML US High Yield Master II Semi-Annual Yield to Worst (in Percent)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BAMLH0A0HYM2SYTW
            /// This data represents the semi-annual yield to worst of the ICE BofAML US High Yield Master II Index, which tracks the performance of US dollar denominated below investment grade rated corporate debt publically issued in the US domestic market. To qualify for inclusion in the index, securities must have a below investment grade rating (based on an average of Moody's, S&P, and Fitch) and an investment grade rated country of risk (based on an average of Moody's, S&P, and Fitch foreign currency long term sovereign debt ratings). Each security must have greater than 1 year of remaining maturity, a fixed coupon schedule, and a minimum amount outstanding of $100 million. Original issue zero coupon bonds, "global" securities (debt issued simultaneously in the eurobond and US domestic bond markets), 144a securities and pay-in-kind securities, including toggle notes, qualify for inclusion in the Index. Callable perpetual securities qualify provided they are at least one year from the first call date. Fixed-to-floating rate securities also qualify provided they are callable within the fixed rate period and are at least one year from the last call prior to the date the bond transitions from a fixed to a floating rate security. DRD-eligible and defaulted securities are excluded from the Index
            /// ICE BofAML Explains the Construction Methodology of this series as:
            /// Index constituents are capitalization-weighted based on their current amount outstanding. With the exception of U.S. mortgage pass-throughs and U.S. structured products (ABS, CMBS and CMOs), accrued interest is calculated assuming next-day settlement. Accrued interest for U.S. mortgage pass-through and U.S. structured products is calculated assuming same-day settlement. Cash flows from bond payments that are received during the month are retained in the index until the end of the month and then are removed as part of the rebalancing. Cash does not earn any reinvestment income while it is held in the Index. The Index is rebalanced on the last calendar day of the month, based on information available up to and including the third business day before the last business day of the month. Issues that meet the qualifying criteria are included in the Index for the following month. Issues that no longer meet the criteria during the course of the month remain in the Index until the next month-end rebalancing at which point they are removed from the Index.
            /// Yield to worst is the lowest potential yield that a bond can generate without the issuer defaulting. The standard US convention for this series is to use semi-annual coupon payments, whereas the standard in the foreign markets is to use coupon payment frequencies of annual, semi-annual, quarterly, and monthly. When the last calendar day of the month takes place on the weekend, weekend observations will occur as a result of month ending accrued interest adjustments.
            /// The index data referenced herein is the property of ICE Data Indices, LLC, its affiliates, ("ICE") and/or its Third Party Suppliers and has been licensed for use by the Federal Reserve Bank of St. Louis. ICE, its affiliates and Third Party Suppliers accept no liability in connection with its use.
            /// Copyright, 2017, ICE Benchmark Administration. Reprinted with permission.
            /// </remarks>
            public static string USHighYieldMasterIISemiAnnualYieldtoWorst = "BAMLH0A0HYM2SYTW";
        }
    }
}