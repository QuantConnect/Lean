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
        /// These time series is an interpretation of Organisation of Economic Development (OECD) Composite Leading Indicators: Reference Turning Points and Component Series data, which can be found at http://www.oecd.org/std/leading-indicators/oecdcompositeleadingindicatorsreferenceturningpointsandcomponentseries.htm. The OECD identifies months of turning points without designating a date within the month that turning points occurred. The dummy variable adopts an arbitrary convention that the turning point occurred at a specific date within the month. The arbitrary convention does not reflect any judgment on this issue by the OECD. Our time series is composed of dummy variables that represent periods of expansion and recession. A value of 1 is a recessionary period, while a value of 0 is an expansionary period. For this time series, the recession begins on the 15th day of the month of the peak and ends on the 15th day of the month of the trough. This time series is a disaggregation of the monthly series. For more options on recession shading, see the note and links below.
        /// The recession shading data that we provide initially comes from the source as a list of dates that are either an economic peak or trough. We interpret dates into recession shading data using one of three arbitrary methods. All of our recession shading data is available using all three interpretations. The period between a peak and trough is always shaded as a recession. The peak and trough are collectively extrema. Depending on the application, the extrema, both individually and collectively, may be included in the recession period in whole or in part. In situations where a portion of a period is included in the recession, the whole period is deemed to be included in the recession period.
        /// The first interpretation, known as the midpoint method, is to show a recession from the midpoint of the peak through the midpoint of the trough for monthly and quarterly data. For daily data, the recession begins on the 15th of the month of the peak and ends on the 15th of the month of the trough. Daily data is a disaggregation of monthly data. For monthly and quarterly data, the entire peak and trough periods are included in the recession shading. This method shows the maximum number of periods as a recession for monthly and quarterly data. The Federal Reserve Bank of St. Louis uses this method in its own publications. The midpoint method is used for this series.
        /// The second interpretation, known as the trough method, is to show a recession from the period following the peak through the trough (i.e. the peak is not included in the recession shading, but the trough is). For daily data, the recession begins on the first day of the first month following the peak and ends on the last day of the month of the trough. Daily data is a disaggregation of monthly data. The trough method is used when displaying data on FRED graphs. A version of this time series represented using the trough method can be found at:
        /// The third interpretation, known as the peak method, is to show a recession from the period of the peak to the trough (i.e. the peak is included in the recession shading, but the trough is not). For daily data, the recession begins on the first day of the month of the peak and ends on the last day of the month preceding the trough. Daily data is a disaggregation of monthly data. A version of this time series represented using the peak method can be found at:
        /// The OECD CLI system is based on the "growth cycle" approach, where business cycles and turning points are measured and identified in the deviation-from-trend series. The main reference series used in the OECD CLI system for the majority of countries is industrial production (IIP) covering all industry sectors excluding construction. This series is used because of its cyclical sensitivity and monthly availability, while the broad based Gross Domestic Product (GDP) is used to supplement the IIP series for identification of the final reference turning points in the growth cycle.
        /// Zones aggregates of the CLIs and the reference series are calculated as weighted averages of the corresponding zone member series (i.e. CLIs and IIPs).
        /// Up to December 2008 the turning points chronologies shown for regional/zone area aggregates or individual countries are determined by the rules established by the National Bureau of Economic Research (NBER) in the United States, which have been formalized and incorporated in a computer routine (Bry and Boschan) and included in the Phase-Average Trend (PAT) de-trending procedure. Starting from December 2008 the turning point detection algorithm is decoupled from the de-trending procedure, and is a simplified version of the original Bry and Boschan routine. (The routine parses local minima and maxima in the cycle series and applies censor rules to guarantee alternating peaks and troughs, as well as phase and cycle length constraints.)
        /// The components of the CLI are time series which exhibit leading relationship with the reference series (IIP) at turning points. Country CLIs are compiled by combining de-trended smoothed and normalized components. The component series for each country are selected based on various criteria such as economic significance; cyclical behavior; data quality; timeliness and availability.
        /// OECD data should be cited as follows: OECD Composite Leading Indicators, "Composite Leading Indicators: Reference Turning Points and Component Series", http://www.oecd.org/std/leading-indicators/oecdcompositeleadingindicatorsreferenceturningpointsandcomponentseries.htm
        /// </summary>
        public static class OECDRecessionIndicators
        {
            ///<summary>
            /// OECD based Recession Indicators for Four Big European Countries from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/4BIGEURORECDM
            /// The Four Big European Countries are: France, Germany, Italy, and United Kingdom.
            /// </remarks>
            public static string FourBigEuropeanCountriesFromPeakThroughTheTrough = "4BIGEURORECDM";

            ///<summary>
            /// OECD based Recession Indicators for Australia from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AUSRECDM
			/// </remarks>
            public static string AustraliaFromPeakThroughTheTrough = "AUSRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Austria from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AUTRECDM
			/// </remarks>
            public static string AustriaFromPeakThroughTheTrough = "AUTRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Belgium from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BELRECDM
			/// </remarks>
            public static string BelgiumFromPeakThroughTheTrough = "BELRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Brazil from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BRARECDM
			/// </remarks>
            public static string BrazilFromPeakThroughTheTrough = "BRARECDM";

            ///<summary>
            /// OECD based Recession Indicators for Canada from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CANRECDM
			/// </remarks>
            public static string CanadaFromPeakThroughTheTrough = "CANRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Switzerland from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHERECDM
			/// </remarks>
            public static string SwitzerlandFromPeakThroughTheTrough = "CHERECDM";

            ///<summary>
            /// OECD based Recession Indicators for Chile from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHLRECDM
			/// </remarks>
            public static string ChileFromPeakThroughTheTrough = "CHLRECDM";

            ///<summary>
            /// OECD based Recession Indicators for China from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHNRECDM
			/// </remarks>
            public static string ChinaFromPeakThroughTheTrough = "CHNRECDM";

            ///<summary>
            /// OECD based Recession Indicators for the Czech Republic from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CZERECDM
			/// </remarks>
            public static string CzechRepublicFromPeakThroughTheTrough = "CZERECDM";

            ///<summary>
            /// OECD based Recession Indicators for Germany from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DEURECDM
			/// </remarks>
            public static string GermanyFromPeakThroughTheTrough = "DEURECDM";

            ///<summary>
            /// OECD based Recession Indicators for Denmark from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DNKRECDM
			/// </remarks>
            public static string DenmarkFromPeakThroughTheTrough = "DNKRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Spain from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ESPRECDM
			/// </remarks>
            public static string SpainFromPeakThroughTheTrough = "ESPRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Estonia from the Peak through the Trough (in +1 or 0)
            /// </summary>
            public static string EstoniaFromPeakThroughTheTrough = "ESTRECDM";

            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ESTRECDM

            ///<summary>
            /// OECD based Recession Indicators for Euro Area from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EURORECDM
            /// The Euro area covers the Europe 16 area excluding Denmark, Sweden, and United Kingdom.
            /// </remarks>
            public static string EuroAreaFromPeakThroughTheTrough = "EURORECDM";

            ///<summary>
            /// OECD based Recession Indicators for Finland from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FINRECDM
			/// </remarks>
            public static string FinlandFromPeakThroughTheTrough = "FINRECDM";

            ///<summary>
            /// OECD based Recession Indicators for France from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FRARECDM
			/// </remarks>
            public static string FranceFromPeakThroughTheTrough = "FRARECDM";

            ///<summary>
            /// OECD based Recession Indicators for the United Kingdom from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBRRECDM
			/// </remarks>
            public static string UnitedKingdomFromPeakThroughTheTrough = "GBRRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Greece from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GRCRECDM
			/// </remarks>
            public static string GreeceFromPeakThroughTheTrough = "GRCRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Hungary from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/HUNRECDM
			/// </remarks>
            public static string HungaryFromPeakThroughTheTrough = "HUNRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Indonesia from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/IDNRECDM
			/// </remarks>
            public static string IndonesiaFromPeakThroughTheTrough = "IDNRECDM";

            ///<summary>
            /// OECD based Recession Indicators for India from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/INDRECDM
			/// </remarks>
            public static string IndiaFromPeakThroughTheTrough = "INDRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Ireland from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/IRLRECDM
			/// </remarks>
            public static string IrelandFromPeakThroughTheTrough = "IRLRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Israel from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ISRRECDM
			/// </remarks>
            public static string IsraelFromPeakThroughTheTrough = "ISRRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Italy from the Peak through the Trough (in +1 or 0)
            /// </summary>
			/// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ITARECDM
			/// </remarks>
            public static string ItalyFromPeakThroughTheTrough = "ITARECDM";

            ///<summary>
            /// OECD based Recession Indicators for Japan from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPNRECDM
            /// </remarks>
            public static string JapanFromPeakThroughTheTrough = "JPNRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Korea from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/KORRECDM
            /// </remarks>
            public static string KoreaFromPeakThroughTheTrough = "KORRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Luxembourg from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/LUXRECDM
            /// </remarks>
            public static string LuxembourgFromPeakThroughTheTrough = "LUXRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Major 5 Asia from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MAJOR5ASIARECDM
            /// The Major 5 Asia countries are: China, India, Indonesia, Japan and Korea.
            /// </remarks>
            public static string MajorFiveAsiaFromPeakThroughTheTrough = "MAJOR5ASIARECDM";

            ///<summary>
            /// OECD based Recession Indicators for Mexico from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MEXRECDM
            /// </remarks>
            public static string MexicoFromPeakThroughTheTrough = "MEXRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Major Seven Countries from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MSCRECDM
            /// The Major Seven countries are: Canada, France, Germany, Italy, Japan, United Kingdom, and United States.
            /// </remarks>
            public static string MajorSevenCountriesFromPeakThroughTheTrough = "MSCRECDM";

            ///<summary>
            /// OECD based Recession Indicators for NAFTA Area from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NAFTARECDM
            /// The NAFTA area covers the following 3 countries: Canada, Mexico, and United States.
            /// </remarks>
            public static string NAFTAAreaFromPeakThroughTheTrough = "NAFTARECDM";

            ///<summary>
            /// OECD based Recession Indicators for Netherlands from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NDLRECDM
            /// </remarks>
            public static string NetherlandsFromPeakThroughTheTrough = "NDLRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Norway from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NORRECDM
            /// </remarks>
            public static string NorwayFromPeakThroughTheTrough = "NORRECDM";

            ///<summary>
            /// OECD based Recession Indicators for New Zealand from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NZLRECDM
            /// </remarks>
            public static string NewZealandFromPeakThroughTheTrough = "NZLRECDM";

            ///<summary>
            /// OECD based Recession Indicators for OECD Europe from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDEUROPERECDM
            /// </remarks>
            public static string OECDEuropeFromPeakThroughTheTrough = "OECDEUROPERECDM";

            ///<summary>
            /// OECD based Recession Indicators for OECD and Non-member Economies from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDNMERECDM
            /// The OECD and Non-member economies covers the following 35 countries: Australia, Austria, Belgium, Brazil, Canada, China, Czech Republic, Denmark, Finland, France, Germany, Greece, Hungary, India, Indonesia, Ireland, Italy, Japan, Korea, Luxembourg, Mexico, Netherlands, New Zealand, Norway, Poland, Portugal, Russian Federation, Slovak Republic, Spain, South Africa, Sweden, Switzerland, Turkey, United Kingdom and United States.
            /// </remarks>
            public static string OECDAndNonmemberEconomiesFromPeakThroughTheTrough = "OECDNMERECDM";

            ///<summary>
            /// OECD based Recession Indicators for the OECD Total Area from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDRECDM
            /// The OECD-Total covers the following 29 countries: Australia, Austria, Belgium, Canada, Czech Republic, Denmark, Finland, France, Germany, Greece, Hungary, Ireland, Italy, Japan, Korea, Luxembourg, Mexico, Netherlands, New Zealand, Norway, Poland, Portugal, Slovak Republic, Spain, Sweden, Switzerland, Turkey, United Kingdom, and United States.
            /// </remarks>
            public static string OECDTotalAreaFromPeakThroughTheTrough = "OECDRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Poland from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/POLRECDM
            /// </remarks>
            public static string PolandFromPeakThroughTheTrough = "POLRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Portugal from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/PRTRECDM
            /// </remarks>
            public static string PortugalFromPeakThroughTheTrough = "PRTRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Russian Federation from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RUSRECDM
            /// </remarks>
            public static string RussianFederationFromPeakThroughTheTrough = "RUSRECDM";

            ///<summary>
            /// OECD based Recession Indicators for the Slovak Republic from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SVKRECDM
            /// </remarks>
            public static string SlovakRepublicFromPeakThroughTheTrough = "SVKRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Slovenia from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SVNRECDM
            /// </remarks>
            public static string SloveniaFromPeakThroughTheTrough = "SVNRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Sweden from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SWERECDM
            /// </remarks>
            public static string SwedenFromPeakThroughTheTrough = "SWERECDM";

            ///<summary>
            /// OECD based Recession Indicators for Turkey from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/TURRECDM
            /// </remarks>
            public static string TurkeyFromPeakThroughTheTrough = "TURRECDM";

            ///<summary>
            /// OECD based Recession Indicators for the United States from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USARECDM
            /// </remarks>
            public static string UnitedStatesFromPeakThroughTheTrough = "USARECDM";

            ///<summary>
            /// OECD based Recession Indicators for South Africa from the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ZAFRECDM
            /// </remarks>
            public static string SouthAfricaFromPeakThroughTheTrough = "ZAFRECDM";

            ///<summary>
            /// OECD based Recession Indicators for Four Big European Countries from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/4BIGEURORECD
            /// The Four Big European Countries are: France, Germany, Italy, and United Kingdom.
            /// </remarks>
            public static string FourBigEuropeanCountriesFromPeriodFollowingPeakThroughTheTrough = "4BIGEURORECD";

            ///<summary>
            /// OECD based Recession Indicators for Australia from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AUSRECD
            /// </remarks>
            public static string AustraliaFromPeriodFollowingPeakThroughTheTrough = "AUSRECD";

            ///<summary>
            /// OECD based Recession Indicators for Austria from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AUTRECD
            /// </remarks>
            public static string AustriaFromPeriodFollowingPeakThroughTheTrough = "AUTRECD";

            ///<summary>
            /// OECD based Recession Indicators for Belgium from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BELRECD
            /// </remarks>
            public static string BelgiumFromPeriodFollowingPeakThroughTheTrough = "BELRECD";

            ///<summary>
            /// OECD based Recession Indicators for Brazil from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BRARECD
            /// </remarks>
            public static string BrazilFromPeriodFollowingPeakThroughTheTrough = "BRARECD";

            ///<summary>
            /// OECD based Recession Indicators for Canada from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CANRECD
            /// </remarks>
            public static string CanadaFromPeriodFollowingPeakThroughTheTrough = "CANRECD";

            ///<summary>
            /// OECD based Recession Indicators for Switzerland from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHERECD
            /// </remarks>
            public static string SwitzerlandFromPeriodFollowingPeakThroughTheTrough = "CHERECD";

            ///<summary>
            /// OECD based Recession Indicators for Chile from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHLRECD
            /// </remarks>
            public static string ChileFromPeriodFollowingPeakThroughTheTrough = "CHLRECD";

            ///<summary>
            /// OECD based Recession Indicators for China from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHNRECD
            /// </remarks>
            public static string ChinaFromPeriodFollowingPeakThroughTheTrough = "CHNRECD";

            ///<summary>
            /// OECD based Recession Indicators for the Czech Republic from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CZERECD
            /// </remarks>
            public static string CzechRepublicFromPeriodFollowingPeakThroughTheTrough = "CZERECD";

            ///<summary>
            /// OECD based Recession Indicators for Germany from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DEURECD
            /// </remarks>
            public static string GermanyFromPeriodFollowingPeakThroughTheTrough = "DEURECD";

            ///<summary>
            /// OECD based Recession Indicators for Denmark from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DNKRECD
            /// </remarks>
            public static string DenmarkFromPeriodFollowingPeakThroughTheTrough = "DNKRECD";

            ///<summary>
            /// OECD based Recession Indicators for Spain from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ESPRECD
            /// </remarks>
            public static string SpainFromPeriodFollowingPeakThroughTheTrough = "ESPRECD";

            ///<summary>
            /// OECD based Recession Indicators for Estonia from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ESTRECD
            /// </remarks>
            public static string EstoniaFromPeriodFollowingPeakThroughTheTrough = "ESTRECD";

            ///<summary>
            /// OECD based Recession Indicators for Euro Area from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EURORECD
            /// The Euro area covers the Europe 16 area excluding Denmark, Sweden, and United Kingdom.
            /// </remarks>
            public static string EuroAreaFromPeriodFollowingPeakThroughTheTrough = "EURORECD";

            ///<summary>
            /// OECD based Recession Indicators for Finland from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FINRECD
            /// </remarks>
            public static string FinlandFromPeriodFollowingPeakThroughTheTrough = "FINRECD";

            ///<summary>
            /// OECD based Recession Indicators for France from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FRARECD
            /// </remarks>
            public static string FranceFromPeriodFollowingPeakThroughTheTrough = "FRARECD";

            ///<summary>
            /// OECD based Recession Indicators for the United Kingdom from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBRRECD
            /// </remarks>
            public static string UnitedKingdomFromPeriodFollowingPeakThroughTheTrough = "GBRRECD";

            ///<summary>
            /// OECD based Recession Indicators for Greece from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GRCRECD
            /// </remarks>
            public static string GreeceFromPeriodFollowingPeakThroughTheTrough = "GRCRECD";

            ///<summary>
            /// OECD based Recession Indicators for Hungary from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/HUNRECD
            /// </remarks>
            public static string HungaryFromPeriodFollowingPeakThroughTheTrough = "HUNRECD";

            ///<summary>
            /// OECD based Recession Indicators for Indonesia from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/IDNRECD
            /// </remarks>
            public static string IndonesiaFromPeriodFollowingPeakThroughTheTrough = "IDNRECD";

            ///<summary>
            /// OECD based Recession Indicators for India from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/INDRECD
            /// </remarks>
            public static string IndiaFromPeriodFollowingPeakThroughTheTrough = "INDRECD";

            ///<summary>
            /// OECD based Recession Indicators for Ireland from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/IRLRECD
            /// </remarks>
            public static string IrelandFromPeriodFollowingPeakThroughTheTrough = "IRLRECD";

            ///<summary>
            /// OECD based Recession Indicators for Israel from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ISRRECD
            /// </remarks>
            public static string IsraelFromPeriodFollowingPeakThroughTheTrough = "ISRRECD";

            ///<summary>
            /// OECD based Recession Indicators for Italy from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ITARECD
            /// </remarks>
            public static string ItalyFromPeriodFollowingPeakThroughTheTrough = "ITARECD";

            ///<summary>
            /// OECD based Recession Indicators for Japan from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPNRECD
            /// </remarks>
            public static string JapanFromPeriodFollowingPeakThroughTheTrough = "JPNRECD";

            ///<summary>
            /// OECD based Recession Indicators for Korea from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/KORRECD
            /// </remarks>
            public static string KoreaFromPeriodFollowingPeakThroughTheTrough = "KORRECD";

            ///<summary>
            /// OECD based Recession Indicators for Luxembourg from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/LUXRECD
            /// </remarks>
            public static string LuxembourgFromPeriodFollowingPeakThroughTheTrough = "LUXRECD";

            ///<summary>
            /// OECD based Recession Indicators for Major 5 Asia from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MAJOR5ASIARECD
            /// The Major 5 Asia countries are: China, India, Indonesia, Japan and Korea.
            /// </remarks>
            public static string MajorFiveAsiaFromPeriodFollowingPeakThroughTheTrough = "MAJOR5ASIARECD";

            ///<summary>
            /// OECD based Recession Indicators for Mexico from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MEXRECD
            /// </remarks>
            public static string MexicoFromPeriodFollowingPeakThroughTheTrough = "MEXRECD";

            ///<summary>
            /// OECD based Recession Indicators for Major Seven Countries from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MSCRECD
            /// The Major Seven countries are: Canada, France, Germany, Italy, Japan, United Kingdom, and United States.
            /// </remarks>
            public static string MajorSevenCountriesFromPeriodFollowingPeakThroughTheTrough = "MSCRECD";

            ///<summary>
            /// OECD based Recession Indicators for NAFTA Area from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NAFTARECD
            /// The NAFTA area covers the following 3 countries: Canada, Mexico, and United States.
            /// </remarks>
            public static string NAFTAAreaFromPeriodFollowingPeakThroughTheTrough = "NAFTARECD";

            ///<summary>
            /// OECD based Recession Indicators for Netherlands from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NDLRECD
            /// </remarks>
            public static string NetherlandsFromPeriodFollowingPeakThroughTheTrough = "NDLRECD";

            ///<summary>
            /// OECD based Recession Indicators for Norway from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NORRECD
            /// </remarks>
            public static string NorwayFromPeriodFollowingPeakThroughTheTrough = "NORRECD";

            ///<summary>
            /// OECD based Recession Indicators for New Zealand from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NZLRECD
            /// </remarks>
            public static string NewZealandFromPeriodFollowingPeakThroughTheTrough = "NZLRECD";

            ///<summary>
            /// OECD based Recession Indicators for OECD Europe from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDEUROPERECD
            /// </remarks>
            public static string OECDEuropeFromPeriodFollowingPeakThroughTheTrough = "OECDEUROPERECD";

            ///<summary>
            /// OECD based Recession Indicators for OECD and Non-member Economies from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDNMERECD
            /// The OECD and Non-member economies covers the following 35 countries: Australia, Austria, Belgium, Brazil, Canada, China, Czech Republic, Denmark, Finland, France, Germany, Greece, Hungary, India, Indonesia, Ireland, Italy, Japan, Korea, Luxembourg, Mexico, Netherlands, New Zealand, Norway, Poland, Portugal, Russian Federation, Slovak Republic, Spain, South Africa, Sweden, Switzerland, Turkey, United Kingdom and United States.
            /// </remarks>
            public static string OECDandNonmemberEconomiesFromPeriodFollowingPeakThroughTheTrough = "OECDNMERECD";

            ///<summary>
            /// OECD based Recession Indicators for the OECD Total Area from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDRECD
            /// The OECD-Total covers the following 29 countries: Australia, Austria, Belgium, Canada, Czech Republic, Denmark, Finland, France, Germany, Greece, Hungary, Ireland, Italy, Japan, Korea, Luxembourg, Mexico, Netherlands, New Zealand, Norway, Poland, Portugal, Slovak Republic, Spain, Sweden, Switzerland, Turkey, United Kingdom, and United States.
            /// </remarks>
            public static string OECDTotalAreaFromPeriodFollowingPeakThroughTheTrough = "OECDRECD";

            ///<summary>
            /// OECD based Recession Indicators for Poland from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            public static string PolandFromPeriodFollowingPeakThroughTheTrough = "POLRECD";

            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/POLRECD

            ///<summary>
            /// OECD based Recession Indicators for Portugal from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/PRTRECD
            /// </remarks>
            public static string PortugalFromPeriodFollowingPeakThroughTheTrough = "PRTRECD";

            ///<summary>
            /// OECD based Recession Indicators for Russian Federation from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RUSRECD
            /// </remarks>
            public static string RussianFederationFromPeriodFollowingPeakThroughTheTrough = "RUSRECD";

            ///<summary>
            /// OECD based Recession Indicators for the Slovak Republic from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SVKRECD
            /// </remarks>
            public static string SlovakRepublicFromPeriodFollowingPeakThroughTheTrough = "SVKRECD";

            ///<summary>
            /// OECD based Recession Indicators for Slovenia from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SVNRECD
            /// </remarks>
            public static string SloveniaFromPeriodFollowingPeakThroughTheTrough = "SVNRECD";

            ///<summary>
            /// OECD based Recession Indicators for Sweden from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SWERECD
            /// </remarks>
            public static string SwedenFromPeriodFollowingPeakThroughTheTrough = "SWERECD";

            ///<summary>
            /// OECD based Recession Indicators for Turkey from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/TURRECD
            /// </remarks>
            public static string TurkeyFromPeriodFollowingPeakThroughTheTrough = "TURRECD";

            ///<summary>
            /// OECD based Recession Indicators for the United States from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USARECD
            /// </remarks>
            public static string UnitedStatesFromPeriodFollowingPeakThroughTheTrough = "USARECD";

            ///<summary>
            /// OECD based Recession Indicators for South Africa from the Period following the Peak through the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ZAFRECD
            /// </remarks>
            public static string SouthAfricaFromPeriodFollowingPeakThroughTheTrough = "ZAFRECD";

            ///<summary>
            /// OECD based Recession Indicators for Four Big European Countries from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/4BIGEURORECDP
            /// The Four Big European Countries are: France, Germany, Italy, and United Kingdom.
            /// </remarks>
            public static string FourBigEuropeanCountriesFromPeakThroughThePeriodPrecedingtheTrough = "4BIGEURORECDP";

            ///<summary>
            /// OECD based Recession Indicators for Australia from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AUSRECDP
            /// </remarks>
            public static string AustraliaFromPeakThroughThePeriodPrecedingtheTrough = "AUSRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Austria from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/AUTRECDP
            /// </remarks>
            public static string AustriaFromPeakThroughThePeriodPrecedingtheTrough = "AUTRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Belgium from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BELRECDP
            /// </remarks>
            public static string BelgiumFromPeakThroughThePeriodPrecedingtheTrough = "BELRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Brazil from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/BRARECDP
            /// </remarks>
            public static string BrazilFromPeakThroughThePeriodPrecedingtheTrough = "BRARECDP";

            ///<summary>
            /// OECD based Recession Indicators for Canada from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CANRECDP
            /// </remarks>
            public static string CanadaFromPeakThroughThePeriodPrecedingtheTrough = "CANRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Switzerland from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHERECDP
            /// </remarks>
            public static string SwitzerlandFromPeakThroughThePeriodPrecedingtheTrough = "CHERECDP";

            ///<summary>
            /// OECD based Recession Indicators for Chile from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHLRECDP
            /// </remarks>
            public static string ChileFromPeakThroughThePeriodPrecedingtheTrough = "CHLRECDP";

            ///<summary>
            /// OECD based Recession Indicators for China from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CHNRECDP
            /// </remarks>
            public static string ChinaFromPeakThroughThePeriodPrecedingtheTrough = "CHNRECDP";

            ///<summary>
            /// OECD based Recession Indicators for the Czech Republic from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/CZERECDP
            /// </remarks>
            public static string CzechRepublicFromPeakThroughThePeriodPrecedingtheTrough = "CZERECDP";

            ///<summary>
            /// OECD based Recession Indicators for Germany from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DEURECDP
            /// </remarks>
            public static string GermanyFromPeakThroughThePeriodPrecedingtheTrough = "DEURECDP";

            ///<summary>
            /// OECD based Recession Indicators for Denmark from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/DNKRECDP
            /// </remarks>
            public static string DenmarkFromPeakThroughThePeriodPrecedingtheTrough = "DNKRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Spain from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ESPRECDP
            /// </remarks>
            public static string SpainFromPeakThroughThePeriodPrecedingtheTrough = "ESPRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Estonia from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ESTRECDP
            /// </remarks>
            public static string EstoniaFromPeakThroughThePeriodPrecedingtheTrough = "ESTRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Euro Area from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/EURORECDP
            /// The Euro area covers the Europe 16 area excluding Denmark, Sweden, and United Kingdom.
            /// </remarks>
            public static string EuroAreaFromPeakThroughThePeriodPrecedingtheTrough = "EURORECDP";

            ///<summary>
            /// OECD based Recession Indicators for Finland from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FINRECDP
            /// </remarks>
            public static string FinlandFromPeakThroughThePeriodPrecedingtheTrough = "FINRECDP";

            ///<summary>
            /// OECD based Recession Indicators for France from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/FRARECDP
            /// </remarks>
            public static string FranceFromPeakThroughThePeriodPrecedingtheTrough = "FRARECDP";

            ///<summary>
            /// OECD based Recession Indicators for the United Kingdom from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GBRRECDP
            /// </remarks>
            public static string UnitedKingdomFromPeakThroughThePeriodPrecedingtheTrough = "GBRRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Greece from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/GRCRECDP
            /// </remarks>
            public static string GreeceFromPeakThroughThePeriodPrecedingtheTrough = "GRCRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Hungary from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/HUNRECDP
            /// </remarks>
            public static string HungaryFromPeakThroughThePeriodPrecedingtheTrough = "HUNRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Indonesia from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/IDNRECDP
            /// </remarks>
            public static string IndonesiaFromPeakThroughThePeriodPrecedingtheTrough = "IDNRECDP";

            ///<summary>
            /// OECD based Recession Indicators for India from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/INDRECDP
            /// </remarks>
            public static string IndiaFromPeakThroughThePeriodPrecedingtheTrough = "INDRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Ireland from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/IRLRECDP
            /// </remarks>
            public static string IrelandFromPeakThroughThePeriodPrecedingtheTrough = "IRLRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Israel from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ISRRECDP
            /// </remarks>
            public static string IsraelFromPeakThroughThePeriodPrecedingtheTrough = "ISRRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Italy from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ITARECDP
            /// </remarks>
            public static string ItalyFromPeakThroughThePeriodPrecedingtheTrough = "ITARECDP";

            ///<summary>
            /// OECD based Recession Indicators for Japan from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/JPNRECDP
            /// </remarks>
            public static string JapanFromPeakThroughThePeriodPrecedingtheTrough = "JPNRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Korea from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/KORRECDP
            /// </remarks>
            public static string KoreaFromPeakThroughThePeriodPrecedingtheTrough = "KORRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Luxembourg from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/LUXRECDP
            /// </remarks>
            public static string LuxembourgFromPeakThroughThePeriodPrecedingtheTrough = "LUXRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Major 5 Asia from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MAJOR5ASIARECDP
            /// The Major 5 Asia countries are: China, India, Indonesia, Japan and Korea.
            /// </remarks>
            public static string MajorFiveAsiaFromPeakThroughThePeriodPrecedingtheTrough = "MAJOR5ASIARECDP";

            ///<summary>
            /// OECD based Recession Indicators for Mexico from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MEXRECDP
            /// </remarks>
            public static string MexicoFromPeakThroughThePeriodPrecedingtheTrough = "MEXRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Major Seven Countries from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/MSCRECDP
            /// The Major Seven countries are: Canada, France, Germany, Italy, Japan, United Kingdom, and United States.
            /// </remarks>
            public static string MajorSevenCountriesFromPeakThroughThePeriodPrecedingtheTrough = "MSCRECDP";

            ///<summary>
            /// OECD based Recession Indicators for NAFTA Area from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NAFTARECDP
            /// The NAFTA area covers the following 3 countries: Canada, Mexico, and United States.
            /// </remarks>
            public static string NAFTAAreaFromPeakThroughThePeriodPrecedingtheTrough = "NAFTARECDP";

            ///<summary>
            /// OECD based Recession Indicators for Netherlands from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NDLRECDP
            /// </remarks>
            public static string NetherlandsFromPeakThroughThePeriodPrecedingtheTrough = "NDLRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Norway from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            public static string NorwayFromPeakThroughThePeriodPrecedingtheTrough = "NORRECDP";

            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NORRECDP

            ///<summary>
            /// OECD based Recession Indicators for New Zealand from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/NZLRECDP
            /// </remarks>
            public static string NewZealandFromPeakThroughThePeriodPrecedingtheTrough = "NZLRECDP";

            ///<summary>
            /// OECD based Recession Indicators for OECD Europe from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDEUROPERECDP
            /// </remarks>
            public static string OECDEuropeFromPeakThroughThePeriodPrecedingtheTrough = "OECDEUROPERECDP";

            ///<summary>
            /// OECD based Recession Indicators for OECD and Non-member Economies from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDNMERECDP
            /// The OECD and Non-member economies covers the following 35 countries: Australia, Austria, Belgium, Brazil, Canada, China, Czech Republic, Denmark, Finland, France, Germany, Greece, Hungary, India, Indonesia, Ireland, Italy, Japan, Korea, Luxembourg, Mexico, Netherlands, New Zealand, Norway, Poland, Portugal, Russian Federation, Slovak Republic, Spain, South Africa, Sweden, Switzerland, Turkey, United Kingdom and United States.
            /// </remarks>
            public static string OECDandNonmemberEconomiesFromPeakThroughThePeriodPrecedingtheTrough = "OECDNMERECDP";

            ///<summary>
            /// OECD based Recession Indicators for the OECD Total Area from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/OECDRECDP
            /// The OECD-Total covers the following 29 countries: Australia, Austria, Belgium, Canada, Czech Republic, Denmark, Finland, France, Germany, Greece, Hungary, Ireland, Italy, Japan, Korea, Luxembourg, Mexico, Netherlands, New Zealand, Norway, Poland, Portugal, Slovak Republic, Spain, Sweden, Switzerland, Turkey, United Kingdom, and United States.
            /// </remarks>
            public static string OECDTotalAreaFromPeakThroughThePeriodPrecedingtheTrough = "OECDRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Poland from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/POLRECDP
            /// </remarks>
            public static string PolandFromPeakThroughThePeriodPrecedingtheTrough = "POLRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Portugal from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/PRTRECDP
            /// </remarks>
            public static string PortugalFromPeakThroughThePeriodPrecedingtheTrough = "PRTRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Russian Federation from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/RUSRECDP
            /// </remarks>
            public static string RussianFederationFromPeakThroughThePeriodPrecedingtheTrough = "RUSRECDP";

            ///<summary>
            /// OECD based Recession Indicators for the Slovak Republic from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SVKRECDP
            /// </remarks>
            public static string SlovakRepublicFromPeakThroughThePeriodPrecedingtheTrough = "SVKRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Slovenia from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SVNRECDP
            /// </remarks>
            public static string SloveniaFromPeakThroughThePeriodPrecedingtheTrough = "SVNRECDP";

            ///<summary>
            /// OECD based Recession Indicators for Sweden from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/SWERECDP
            /// </remarks>
            public static string SwedenFromPeakThroughThePeriodPrecedingtheTrough = "SWERECDP";

            ///<summary>
            /// OECD based Recession Indicators for Turkey from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/TURRECDP
            /// </remarks>
            public static string TurkeyFromPeakThroughThePeriodPrecedingtheTrough = "TURRECDP";

            ///<summary>
            /// OECD based Recession Indicators for the United States from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/USARECDP
            /// </remarks>
            public static string UnitedStatesFromPeakThroughThePeriodPrecedingtheTrough = "USARECDP";

            ///<summary>
            /// OECD based Recession Indicators for South Africa from the Peak through the Period preceding the Trough (in +1 or 0)
            /// </summary>
            /// <remarks>
            /// Retrieved from FRED, Federal Reserve Bank of St. Louis; https://fred.stlouisfed.org/series/ZAFRECDP
            /// </remarks>
            public static string SouthAfricaFromPeakThroughThePeriodPrecedingtheTrough = "ZAFRECDP";
        }
    }
}