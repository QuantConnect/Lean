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

namespace QuantConnect.Data.Custom.Intrinio
{
    public static class IntrinioEconomicDataSources
    {
        public static class BofAMerrillLynch
        {
            /// <summary>
            ///     This data represents the effective yield of the BofA Merrill Lynch US Corporate BBB Index, a subset of the BofA
            ///     Merrill Lynch US Corporate Master Index tracking the performance of US dollar denominated investment grade rated
            ///     corporate debt publically issued in the US domestic market.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/BAMLC0A4CBBBEY
            /// </remarks>
            public const string USCorporateBBBEffectiveYield = "$BAMLC0A4CBBBEY";

            /// <summary>
            ///     This data represents the Option-Adjusted Spread (OAS) of the BofA Merrill Lynch US Corporate BBB Index, a subset of
            ///     the BofA Merrill Lynch US Corporate Master Index tracking the performance of US dollar denominated investment grade
            ///     rated corporate debt publically issued in the US domestic market.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/BAMLC0A4CBBB
            /// </remarks>
            public const string USCorporateBBBOptionAdjustedSpread = "$BAMLC0A4CBBB";

            /// <summary>
            ///     The BofA Merrill Lynch Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of
            ///     all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent
            ///     bond’s OAS, weighted by market capitalization.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/BAMLC0A0CM
            /// </remarks>
            public const string USCorporateMasterOptionAdjustedSpread = "$BAMLC0A0CM";

            /// <summary>
            ///     This data represents the Option-Adjusted Spread (OAS) of the BofA Merrill Lynch US Corporate BB Index, a subset of
            ///     the BofA Merrill Lynch US High Yield Master II Index tracking the performance of US dollar denominated below
            ///     investment grade rated corporate debt publically issued in the US domestic market.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/BAMLH0A1HYBB
            /// </remarks>
            public const string USHighYieldBBOptionAdjustedSpread = "$BAMLH0A1HYBB";

            /// <summary>
            ///     This data represents the Option-Adjusted Spread (OAS) of the BofA Merrill Lynch US Corporate B Index, a subset of
            ///     the BofA Merrill Lynch US High Yield Master II Index tracking the performance of US dollar denominated below
            ///     investment grade rated corporate debt publically issued in the US domestic market. This subset includes all
            ///     securities with a given investment grade rating B.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/BAMLH0A2HYB
            /// </remarks>
            public const string USHighYieldBOptionAdjustedSpread = "$BAMLH0A2HYB";

            /// <summary>
            ///     This data represents the Option-Adjusted Spread (OAS) of the BofA Merrill Lynch US Corporate C Index, a subset of
            ///     the BofA Merrill Lynch US High Yield Master II Index tracking the performance of US dollar denominated below
            ///     investment grade rated corporate debt publically issued in the US domestic market.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/BAMLH0A3HYC
            /// </remarks>
            public const string USHighYieldCCCorBelowOptionAdjustedSpread = "$BAMLH0A3HYC";

            /// <summary>
            ///     This data represents the effective yield of the BofA Merrill Lynch US High Yield Master II Index, which tracks the
            ///     performance of US dollar denominated below investment grade rated corporate debt publically issued in the US
            ///     domestic market.
            ///     Source: https://fred.stlouisfed.org/series/BAMLH0A0HYM2EY
            /// </summary>
            public const string USHighYieldEffectiveYield = "$BAMLH0A0HYM2EY";

            /// <summary>
            ///     This data represents the BofA Merrill Lynch US High Yield Master II Index value, which tracks the performance of US
            ///     dollar denominated below investment grade rated corporate debt publically issued in the US domestic market.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/BAMLHYH0A0HYM2TRIV
            /// </remarks>
            public const string USHighYieldMasterIITotalReturnIndexValue = "$BAMLHYH0A0HYM2TRIV";

            /// <summary>
            ///     The BofA Merrill Lynch Option-Adjusted Spreads (OASs) are the calculated spreads between a computed OAS index of
            ///     all bonds in a given rating category and a spot Treasury curve. An OAS index is constructed using each constituent
            ///     bond’s OAS, weighted by market capitalization.
            ///     Source: https://fred.stlouisfed.org/series/BAMLH0A0HYM2
            /// </summary>
            public const string USHighYieldOptionAdjustedSpread = "$BAMLH0A0HYM2";
        }

        public static class CBOE
        {
            /// <summary>
            ///     CBOE China ETF Volatility Index
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/VXFXICLS
            /// </remarks>
            public const string ChinaETFVolatilityIndex = "$VXFXICLS";

            /// <summary>
            ///     CBOE Crude Oil ETF Volatility Index
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/OVXCLS
            /// </remarks>
            public const string CrudeOilETFVolatilityIndex = "$OVXCLS";

            /// <summary>
            ///     CBOE Emerging Markets ETF Volatility Index
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/VXEEMCLS
            /// </remarks>
            public const string EmergingMarketsETFVolatilityIndex = "$VXEEMCLS";

            /// <summary>
            ///     CBOE Gold ETF Volatility Index
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/GVZCLS
            /// </remarks>
            public const string GoldETFVolatilityIndex = "$GVZCLS";

            /// <summary>
            ///     CBOE 10-Year Treasury Note Volatility Futures
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/VXTYN
            /// </remarks>
            public const string TenYearTreasuryNoteVolatilityFutures = "$VXTYN";

            /// <summary>
            ///     CBOE Volatility Index: VIX
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/VIXCLS
            /// </remarks>
            public const string VIX = "$VIXCLS";

            /// <summary>
            ///     CBOE S&amp;P 100 Volatility Index: VXO
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/VXOCLS
            /// </remarks>
            public const string VXO = "$VXOCLS";

            /// <summary>
            ///     CBOE S&amp;P 500 3-Month Volatility Index
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/VXVCLS
            /// </remarks>
            public const string VXV = "$VXVCLS";
        }

        public static class Commodities
        {
            /// <summary>
            ///     Crude Oil Prices: Brent - Europe
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DCOILBRENTEU
            /// </remarks>
            public const string CrudeOilBrent = "$DCOILBRENTEU";

            /// <summary>
            ///     Crude Oil Prices: West Texas Intermediate (WTI) - Cushing, Oklahoma
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DCOILWTICO
            /// </remarks>
            public const string CrudeOilWTI = "$DCOILWTICO";

            /// <summary>
            ///     Conventional Gasoline Prices: U.S. Gulf Coast, Regular
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DGASUSGULF
            /// </remarks>
            public const string GasolineUSGulfCoast = "$DGASUSGULF";

            /// <summary>
            ///     Gold Fixing Price 10:30 A.M. (London time) in London Bullion Market, based in U.S. Dollars
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/GOLDAMGBD228NLBM
            /// </remarks>
            public const string GoldFixingPrice1030amLondon = "$GOLDAMGBD228NLBM";

            /// <summary>
            ///     Gold Fixing Price 3:00 P.M. (London time) in London Bullion Market, based in U.S. Dollars
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/GOLDPMGBD228NLBM
            /// </remarks>
            public const string GoldFixingPrice1500amLondon = "$GOLDPMGBD228NLBM";

            /// <summary>
            ///     Henry Hub Natural Gas Spot Price
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DHHNGSP
            /// </remarks>
            public const string NaturalGas = "$DHHNGSP";

            /// <summary>
            ///     Propane Prices: Mont Belvieu, Texas
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DPROPANEMBTX
            /// </remarks>
            public const string Propane = "$DPROPANEMBTX";
        }

        public static class ExchangeRates
        {
            /// <summary>
            ///     Brazilian Reals to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Brazil_USA = "$DEXBZUS";

            /// <summary>
            ///     Canadian Dollars to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Canada_USA = "$DEXCAUS";

            /// <summary>
            ///     Chinese Yuan to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string China_USA = "$DEXCHUS";

            /// <summary>
            ///     Hong Kong Dollars to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string HongKong_USA = "$DEXHKUS";

            /// <summary>
            ///     Indian Rupees to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string India_USA = "$DEXINUS";

            /// <summary>
            ///     Japanese Yen to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Japan_USA = "$DEXJPUS";

            /// <summary>
            ///     Malaysian Ringgit to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Malaysia_USA = "$DEXMAUS";

            /// <summary>
            ///     Mexican New Pesos to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Mexico_USA = "$DEXMXUS";

            /// <summary>
            ///     Norwegian Kroner to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Norway_USA = "$DEXNOUS";

            /// <summary>
            ///     Singapore Dollars to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Singapore_USA = "$DEXSIUS";

            /// <summary>
            ///     South African Rand to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string SouthAfrica_USA = "$DEXSFUS";

            /// <summary>
            ///     South Korean Won to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string SouthKorea_USA = "$DEXKOUS";

            /// <summary>
            ///     Sri Lankan Rupees to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string SriLanka_USA = "$DEXSLUS";

            /// <summary>
            ///     Swiss Francs to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Switzerland_USA = "$DEXSZUS";

            /// <summary>
            ///     New Taiwan Dollars to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Taiwan_USA = "$DEXTAUS";

            /// <summary>
            ///     Thai Baht to One U.S. Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string Thailand_USA = "$DEXTHUS";

            /// <summary>
            ///     U.S. Dollars to One Australian Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string USA_Australia = "$DEXUSAL";

            /// <summary>
            ///     U.S. Dollars to One Euro
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string USA_Euro = "$DEXUSEU";

            /// <summary>
            ///     U.S. Dollars to One New Zealand Dollar
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string USA_NewZealand = "$DEXUSNZ";

            /// <summary>
            ///     U.S. Dollars to One British Pound
            /// </summary>
            /// <remarks>
            ///     Source: Board of Governors of the Federal Reserve System https://www.federalreserve.gov/releases/h10/
            /// </remarks>
            public const string USA_UK = "$DEXUSUK";
        }

        public static class Moodys
        {
            /// <summary>
            ///     Moody's Seasoned Aaa Corporate Bond© and 10-Year Treasury Constant Maturity.
            ///     These instruments are based on bonds with maturities 20 years and above.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DAAA
            /// </remarks>
            public const string SeasonedAaaCorporateBondYield = "$DAAA";

            /// <summary>
            ///     Series is calculated as the spread between Moody's Seasoned Aaa Corporate Bond© and 10-Year Treasury Constant
            ///     Maturity
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/AAA10Y
            /// </remarks>
            public const string SeasonedAaaCorporateBondYieldRelativeTo10YearTreasuryConstantMaturity = "$AAA10Y";

            /// <summary>
            ///     Moody's Seasoned Baa Corporate Bond© and 10-Year Treasury Constant Maturity.
            ///     These instruments are based on bonds with maturities 20 years and above.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DBAA
            /// </remarks>
            public const string SeasonedBaaCorporateBondYield = "$DBAA";

            /// <summary>
            /// Series is calculated as the spread between Moody's Seasoned Baa Corporate Bond© and 10-Year Treasury Constant Maturity
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/BAA10Y
            /// </remarks>
            public const string SeasonedBaaCorporateBondYieldRelativeTo10YearTreasuryConstantMaturity = "$BAA10Y";
        }

        public static class TradeWeightedUsDollaIndex
        {
            /// <summary>
            ///     A weighted average of the foreign exchange value of the U.S. dollar against the currencies of a broad group of
            ///     major U.S. trading partners. Broad currency index includes the Euro Area, Canada, Japan, Mexico, China, United
            ///     Kingdom, Taiwan, Korea, Singapore, Hong Kong, Malaysia, Brazil, Switzerland, Thailand, Philippines, Australia,
            ///     Indonesia, India, Israel, Saudi Arabia, Russia, Sweden, Argentina, Venezuela, Chile and Colombia.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DTWEXB
            ///     For more information about trade-weighted indexes see
            ///     http://www.federalreserve.gov/pubs/bulletin/2005/winter05_index.pdf.
            /// </remarks>
            public const string Broad = "$DTWEXB";

            /// <summary>
            ///     A weighted average of the foreign exchange value of the U.S. dollar against a subset of the broad index currencies
            ///     that circulate widely outside the country of issue. Major currencies index includes the Euro Area, Canada, Japan,
            ///     United Kingdom, Switzerland, Australia, and Sweden.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DTWEXM
            ///     For more information about trade-weighted indexes see
            ///     http://www.federalreserve.gov/pubs/bulletin/2005/winter05_index.pdf.
            /// </remarks>
            public const string MajorCurrencies = "$DTWEXM";

            /// <summary>
            ///     A weighted average of the foreign exchange value of the U.S. dollar against a subset of the broad index currencies
            ///     that do not circulate widely outside the country of issue. Countries whose currencies are included in the other
            ///     important trading partners index are Mexico, China, Taiwan, Korea, Singapore, Hong Kong, Malaysia, Brazil,
            ///     Thailand, Philippines, Indonesia, India, Israel, Saudi Arabia, Russia, Argentina, Venezuela, Chile and Colombia.
            /// </summary>
            /// <remarks>
            ///     Source: https://fred.stlouisfed.org/series/DTWEXO
            ///     For more information about trade-weighted indexes see
            ///     http://www.federalreserve.gov/pubs/bulletin/2005/winter05_index.pdf.
            /// </remarks>
            public const string OtherImportantTradingPartners = "$DTWEXO";
        }
    }
}