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

namespace QuantConnect.Data.Custom.TradingEconomics
{
    /// <summary>
    /// TradingEconomics static class contains shortcut definitions of major Trading Economics Indicators available");
    /// </summary>
    public static partial class TradingEconomics
    {
        /// <summary>
        /// The Event class contains all events normalized for your convenience
        /// </summary>
        public static class Event
        {
            /// <summary>
            /// Australia
            /// </summary>
            public static class Australia
            {
                /// <summary>
                /// AiG Performance Construction Index
                /// </summary>
                public const string AigPerformanceConstructionIndex = "aig performance construction index";
                /// <summary>
                /// AiG Performance of Mfg Index
                /// </summary>
                public const string AigPerformanceManufacturingIndex = "aig performance manufacturing index";
                /// <summary>
                /// AiG Performance of Services Index
                /// </summary>
                public const string AigPerformanceServicesIndex = "aig performance services index";
                /// <summary>
                /// ANZAC Day
                /// </summary>
                public const string AnzacDay = "anzac day";
                /// <summary>
                /// ANZ Internet Job Ads MoM
                /// </summary>
                public const string AnzInternetJobAdvertisementsMoM = "anz internet job advertisements mom";
                /// <summary>
                /// ANZ Job Advertisements
                /// </summary>
                public const string AnzJobAdvertisements = "anz job advertisements";
                /// <summary>
                /// ANZ Job Advertisement MoM
                /// </summary>
                public const string AnzJobAdvertisementsMoM = "anz job advertisements mom";
                /// <summary>
                /// ANZ Newspaper Job Ads MoM
                /// </summary>
                public const string AnzNewspaperJobAdvertisementsMoM = "anz newspaper job advertisements mom";
                /// <summary>
                /// Australia Day
                /// </summary>
                public const string AustraliaDay = "australia day";
                /// <summary>
                /// Australia Day (Observed)
                /// </summary>
                public const string AustraliaDayObserved = "australia day observed";
                /// <summary>
                /// Australia Day (substitute day)
                /// </summary>
                public const string AustraliaDaySubstituteDay = "australia day substitute day";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Building Permits (MoM)
                /// </summary>
                public const string BuildingPermitsMoM = "building permits mom";
                /// <summary>
                /// Building Permits (YoY)
                /// </summary>
                public const string BuildingPermitsYoY = "building permits yoy";
                /// <summary>
                /// Business Inventories QoQ
                /// </summary>
                public const string BusinessInventoriesQoQ = "business inventories qoq";
                /// <summary>
                /// Capital Expenditure QoQ
                /// </summary>
                public const string CapitalExpenditureQoQ = "capital expenditure qoq";
                /// <summary>
                /// CB Leading Index
                /// </summary>
                public const string CbLeadingEconomicIndex = "cb leading economic index";
                /// <summary>
                /// CB Leading Index MoM
                /// </summary>
                public const string CbLeadingEconomicIndexMoM = "cb leading economic index mom";
                /// <summary>
                /// CB Leading Indicator
                /// </summary>
                public const string CbLeadingEconomicIndicators = "cb leading economic indicators";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// CommBank Composite PMI Final
                /// </summary>
                public const string CommbankCompositePurchasingManagersIndexFinal = "commbank composite purchasing managers index final";
                /// <summary>
                /// CommBank Composite PMI Flash
                /// </summary>
                public const string CommbankCompositePurchasingManagersIndexFlash = "commbank composite purchasing managers index flash";
                /// <summary>
                /// CommBank Manufacturing PMI Final
                /// </summary>
                public const string CommbankManufacturingPurchasingManagersIndexFinal = "commbank manufacturing purchasing managers index final";
                /// <summary>
                /// CommBank Manufacturing PMI Flash
                /// </summary>
                public const string CommbankManufacturingPurchasingManagersIndexFlash = "commbank manufacturing purchasing managers index flash";
                /// <summary>
                /// CommBank Services PMI Final
                /// </summary>
                public const string CommbankServicesPurchasingManagersIndexFinal = "commbank services purchasing managers index final";
                /// <summary>
                /// CommBank Services PMI Flash
                /// </summary>
                public const string CommbankServicesPurchasingManagersIndexFlash = "commbank services purchasing managers index flash";
                /// <summary>
                /// Company Gross Operating Profits (QoQ)
                /// </summary>
                public const string CompanyGrossOperatingProfitsQoQ = "company gross operating profits qoq";
                /// <summary>
                /// Company Gross Profits QoQ
                /// </summary>
                public const string CompanyGrossProfitsQoQ = "company gross profits qoq";
                /// <summary>
                /// Construction Work Done
                /// </summary>
                public const string ConstructionWorkDone = "construction work done";
                /// <summary>
                /// Construction Work Done QoQ
                /// </summary>
                public const string ConstructionWorkDoneQoQ = "construction work done qoq";
                /// <summary>
                /// Consumer Inflation Expectation
                /// </summary>
                public const string ConsumerInflationExpectations = "consumer inflation expectations";
                /// <summary>
                /// Consumer Price Index
                /// </summary>
                public const string ConsumerPriceIndex = "consumer price index";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Saturday
                /// </summary>
                public const string EasterSaturday = "easter saturday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "employment change";
                /// <summary>
                /// Employment Change s.a.
                /// </summary>
                public const string EmploymentChangeSeasonallyAdjusted = "employment change seasonally adjusted";
                /// <summary>
                /// Export Price Index QoQ
                /// </summary>
                public const string ExportPricesQoQ = "export prices qoq";
                /// <summary>
                /// Exports MoM
                /// </summary>
                public const string ExportsMoM = "exports mom";
                /// <summary>
                /// Exports YoY
                /// </summary>
                public const string ExportsYoY = "exports yoy";
                /// <summary>
                /// Family Day
                /// </summary>
                public const string FamilyDay = "family day";
                /// <summary>
                /// Federal Election
                /// </summary>
                public const string FederalElection = "federal election";
                /// <summary>
                /// Fulltime Employment
                /// </summary>
                public const string FullTimeEmploymentChange = "full time employment change";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Capital Expenditure
                /// </summary>
                public const string GdpCapitalExpenditure = "gdp capital expenditure";
                /// <summary>
                /// GDP Capital Expenditure QoQ
                /// </summary>
                public const string GdpCapitalExpenditureQoQ = "gdp capital expenditure qoq";
                /// <summary>
                /// GDP Deflator
                /// </summary>
                public const string GdpDeflator = "gdp deflator";
                /// <summary>
                /// GDP Deflator QoQ
                /// </summary>
                public const string GdpDeflatorQoQ = "gdp deflator qoq";
                /// <summary>
                /// GDP Final Consumption
                /// </summary>
                public const string GdpFinalConsumption = "gdp final consumption";
                /// <summary>
                /// GDP Final Consumption QoQ
                /// </summary>
                public const string GdpFinalConsumptionQoQ = "gdp final consumption qoq";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// G20 Meeting
                /// </summary>
                public const string GTwentyMeeting = "g20 meeting";
                /// <summary>
                /// HIA New Home Sales (MoM)
                /// </summary>
                public const string HiaNewHomeSalesMoM = "hia new home sales mom";
                /// <summary>
                /// Home Loans MoM
                /// </summary>
                public const string HomeLoansMoM = "home loans mom";
                /// <summary>
                /// House Price Index QoQ
                /// </summary>
                public const string HousePriceIndexQoQ = "house price index qoq";
                /// <summary>
                /// House Price Index YoY
                /// </summary>
                public const string HousePriceIndexYoY = "house price index yoy";
                /// <summary>
                /// Import Price Index QoQ
                /// </summary>
                public const string ImportPricesQoQ = "import prices qoq";
                /// <summary>
                /// Imports MoM
                /// </summary>
                public const string ImportsMoM = "imports mom";
                /// <summary>
                /// Imports YoY
                /// </summary>
                public const string ImportsYoY = "imports yoy";
                /// <summary>
                /// Inflation Rate QoQ
                /// </summary>
                public const string InflationRateQoQ = "inflation rate qoq";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Investment Lending for Homes
                /// </summary>
                public const string InvestmentLendingForHomes = "investment lending for homes";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Mid-Year Economic and Fiscal Outlook
                /// </summary>
                public const string MidYearEconomicAndFiscalOutlook = "mid year economic and fiscal outlook";
                /// <summary>
                /// NAB Business Confidence
                /// </summary>
                public const string NabBusinessConfidence = "nab business confidence";
                /// <summary>
                /// NAB Business Survey
                /// </summary>
                public const string NabBusinessSurvey = "nab business survey";
                /// <summary>
                /// New Motor Vehicle Sales (MoM)
                /// </summary>
                public const string NewMotorVehicleSalesMoM = "new motor vehicle sales mom";
                /// <summary>
                /// New Motor Vehicle Sales (YoY)
                /// </summary>
                public const string NewMotorVehicleSalesYoY = "new motor vehicle sales yoy";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year's Day (Substitute Day)
                /// </summary>
                public const string NewYearsDaySubstituteDay = "new years day substitute day";
                /// <summary>
                /// Participation Rate
                /// </summary>
                public const string ParticipationRate = "participation rate";
                /// <summary>
                /// Part Time Employment Chg
                /// </summary>
                public const string PartTimeEmploymentChange = "part time employment change";
                /// <summary>
                /// Private Capital Expenditure QoQ
                /// </summary>
                public const string PrivateCapitalExpenditureQoQ = "private capital expenditure qoq";
                /// <summary>
                /// Private New Capital Expenditure QoQ
                /// </summary>
                public const string PrivateNewCapitalExpenditureQoQ = "private new capital expenditure qoq";
                /// <summary>
                /// Private Sector Credit (MoM)
                /// </summary>
                public const string PrivateSectorCreditMoM = "private sector credit mom";
                /// <summary>
                /// Private Sector Credit (YoY)
                /// </summary>
                public const string PrivateSectorCreditYoY = "private sector credit yoy";
                /// <summary>
                /// Producer Price Index QoQ
                /// </summary>
                public const string ProducerPriceIndexQoQ = "producer price index qoq";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Queen's Birthday
                /// </summary>
                public const string QueensBirthday = "queens birthday";
                /// <summary>
                /// Queen's Birthday Day
                /// </summary>
                public const string QueensBirthdayDay = "queens birthday day";
                /// <summary>
                /// RBA Assist Gov Edey Speech
                /// </summary>
                public const string RbaAssistGovEdeySpeech = "rba assist gov edey speech";
                /// <summary>
                /// RBA Assist Gov Kent Speech
                /// </summary>
                public const string RbaAssistGovKentSpeech = "rba assist gov kent speech";
                /// <summary>
                /// RBA Aylmer Speech
                /// </summary>
                public const string RbaAylmerSpeech = "rba aylmer speech";
                /// <summary>
                /// RBA Boulton Speech
                /// </summary>
                public const string RbaBoultonSpeech = "rba boulton speech";
                /// <summary>
                /// RBA Bulletin
                /// </summary>
                public const string RbaBulletin = "rba bulletin";
                /// <summary>
                /// RBA Bullock Speech
                /// </summary>
                public const string RbaBullockSpeech = "rba bullock speech";
                /// <summary>
                /// RBA Chart Pack
                /// </summary>
                public const string RbaChartPack = "rba chart pack";
                /// <summary>
                /// RBA Commodity Index SDR (YoY)
                /// </summary>
                public const string RbaCommodityIndexSdrYoY = "rba commodity index sdr yoy";
                /// <summary>
                /// RBA Coombs Speech
                /// </summary>
                public const string RbaCoombsSpeech = "rba coombs speech";
                /// <summary>
                /// RBA Debelle Speech
                /// </summary>
                public const string RbaDebelleSpeech = "rba debelle speech";
                /// <summary>
                /// RBA Deputy Gov Lowe Speech
                /// </summary>
                public const string RbaDeputyGovLoweSpeech = "rba deputy gov lowe speech";
                /// <summary>
                /// RBA Edey Speech
                /// </summary>
                public const string RbaEdeySpeech = "rba edey speech";
                /// <summary>
                /// RBA Ellis Speech
                /// </summary>
                public const string RbaEllisSpeech = "rba ellis speech";
                /// <summary>
                /// RBA Financial Stability Review
                /// </summary>
                public const string RbaFinancialStabilityReview = "rba financial stability review";
                /// <summary>
                /// RBA Girn Speech
                /// </summary>
                public const string RbaGirnSpeech = "rba girn speech";
                /// <summary>
                /// RBA Gov Debelle Speech
                /// </summary>
                public const string RbaGovDebelleSpeech = "rba gov debelle speech";
                /// <summary>
                /// RBA Governor Stevens Speaks
                /// </summary>
                public const string RbaGovernorStevensSpeaks = "rba governor stevens speaks";
                /// <summary>
                /// RBA Gov Glenn Speech
                /// </summary>
                public const string RbaGovGlennSpeech = "rba gov glenn speech";
                /// <summary>
                /// RBA Gov. Glenn Stevens Speech
                /// </summary>
                public const string RbaGovGlennStevensSpeech = "rba gov glenn stevens speech";
                /// <summary>
                /// RBA Gov Kent Speech
                /// </summary>
                public const string RbaGovKentSpeech = "rba gov kent speech";
                /// <summary>
                /// RBA Gov Lowe Speech
                /// </summary>
                public const string RbaGovLoweSpeech = "rba gov lowe speech";
                /// <summary>
                /// RBA Heath Speech
                /// </summary>
                public const string RbaHeathSpeech = "rba heath speech";
                /// <summary>
                /// RBA Interest Rate Decision
                /// </summary>
                public const string RbaInterestRateDecision = "rba interest rate decision";
                /// <summary>
                /// RBA Kent Speech
                /// </summary>
                public const string RbaKentSpeech = "rba kent speech";
                /// <summary>
                /// RBA Kohler Speech
                /// </summary>
                public const string RbaKohlerSpeech = "rba kohler speech";
                /// <summary>
                /// RBA Lowe Speaks before Parliament
                /// </summary>
                public const string RbaLoweSpeaksBeforeParliament = "rba lowe speaks before parliament";
                /// <summary>
                /// RBA Lowe Speech
                /// </summary>
                public const string RbaLoweSpeech = "rba lowe speech";
                /// <summary>
                /// RBA Meeting Minutes
                /// </summary>
                public const string RbaMeetingMinutes = "rba meeting minutes";
                /// <summary>
                /// RBA Meetings Minutes
                /// </summary>
                public const string RbaMeetingsMinutes = "rba meetings minutes";
                /// <summary>
                /// RBA Monetary Policy Statement
                /// </summary>
                public const string RbaMonetaryPolicyStatement = "rba monetary policy statement";
                /// <summary>
                /// RBA Rate Statement
                /// </summary>
                public const string RbaRateStatement = "rba rate statement";
                /// <summary>
                /// RBA Richards Speech
                /// </summary>
                public const string RbaRichardsSpeech = "rba richards speech";
                /// <summary>
                /// RBA Ryan Speech
                /// </summary>
                public const string RbaRyanSpeech = "rba ryan speech";
                /// <summary>
                /// RBA's Governor Glenn Stevens Speech
                /// </summary>
                public const string RbasGovernorGlennStevensSpeech = "rbas governor glenn stevens speech";
                /// <summary>
                /// RBA's Gov Stevens Speech
                /// </summary>
                public const string RbasGovStevensSpeech = "rbas gov stevens speech";
                /// <summary>
                /// RBA Simon Speech
                /// </summary>
                public const string RbaSimonSpeech = "rba simon speech";
                /// <summary>
                /// RBA Statement on Monetary Policy
                /// </summary>
                public const string RbaStatementOnMonetaryPolicy = "rba statement on monetary policy";
                /// <summary>
                /// RBA trimmed mean CPI QoQ
                /// </summary>
                public const string RbaTrimmedMeanConsumerPriceIndexQoQ = "rba trimmed mean consumer price index qoq";
                /// <summary>
                /// RBA trimmed mean CPI YoY
                /// </summary>
                public const string RbaTrimmedMeanConsumerPriceIndexYoY = "rba trimmed mean consumer price index yoy";
                /// <summary>
                /// RBA Weighted Mean CPI QoQ
                /// </summary>
                public const string RbaWeightedMeanConsumerPriceIndexQoQ = "rba weighted mean consumer price index qoq";
                /// <summary>
                /// RBA Weighted Mean CPI YoY
                /// </summary>
                public const string RbaWeightedMeanConsumerPriceIndexYoY = "rba weighted mean consumer price index yoy";
                /// <summary>
                /// Reconciliation Day
                /// </summary>
                public const string ReconciliationDay = "reconciliation day";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Statement on Monetary Policy
                /// </summary>
                public const string StatementOnMonetaryPolicy = "statement on monetary policy";
                /// <summary>
                /// TD-MI Inflation Gauge MoM
                /// </summary>
                public const string TdMelbourneInstituteInflationGaugeMoM = "td melbourne institute inflation gauge mom";
                /// <summary>
                /// TD Securities Inflation MoM
                /// </summary>
                public const string TdSecuritiesInflationMoM = "td securities inflation mom";
                /// <summary>
                /// TD Securities Inflation (YoY)
                /// </summary>
                public const string TdSecuritiesInflationYoY = "td securities inflation yoy";
                /// <summary>
                /// 2017-18 Budget Release
                /// </summary>
                public const string TwoThousandSeventeenEighteenBudgetRelease = "2017 18 budget release";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Wage Price Index QoQ
                /// </summary>
                public const string WagePriceIndexQoQ = "wage price index qoq";
                /// <summary>
                /// Wage Price Index YoY
                /// </summary>
                public const string WagePriceIndexYoY = "wage price index yoy";
                /// <summary>
                /// Westpac Consumer Confidence
                /// </summary>
                public const string WestpacConsumerConfidence = "westpac consumer confidence";
                /// <summary>
                /// Westpac Cons. Conf. Change
                /// </summary>
                public const string WestpacConsumerConfidenceChange = "westpac consumer confidence change";
                /// <summary>
                /// Westpac Consumer Confidence MoM
                /// </summary>
                public const string WestpacConsumerConfidenceMoM = "westpac consumer confidence mom";
                /// <summary>
                /// Westpac Leading Index
                /// </summary>
                public const string WestpacLeadingIndex = "westpac leading index";
                /// <summary>
                /// Westpac Leading Index MoM
                /// </summary>
                public const string WestpacLeadingIndexMoM = "westpac leading index mom";
            }
            /// <summary>
            /// Austria
            /// </summary>
            public static class Austria
            {
                /// <summary>
                /// All Saint's Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Bank Austria Manufacturing PMI
                /// </summary>
                public const string BankOfAustriaManufacturingPurchasingManagersIndex = "bank of austria manufacturing purchasing managers index";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Eve
                /// </summary>
                public const string ChristmasEve = "christmas eve";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Corpus Christi
                /// </summary>
                public const string CorpusChristi = "corpus christi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// Finance Minister Schelling Speech
                /// </summary>
                public const string FinanceMinisterSchellingSpeech = "finance minister schelling speech";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Harmonised Inflation Rate MoM
                /// </summary>
                public const string HarmonizedInflationRateMoM = "harmonized inflation rate mom";
                /// <summary>
                /// Harmonised Inflation Rate YoY
                /// </summary>
                public const string HarmonizedInflationRateYoY = "harmonized inflation rate yoy";
                /// <summary>
                /// IMF Lagarde Speech
                /// </summary>
                public const string ImfLagardeSpeech = "imf lagarde speech";
                /// <summary>
                /// Immaculate Conception
                /// </summary>
                public const string ImmaculateConception = "immaculate conception";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// OeNB Gov Nowotny Speech
                /// </summary>
                public const string OenbGovNowotnySpeech = "oenb gov nowotny speech";
                /// <summary>
                /// OPEC Meeting
                /// </summary>
                public const string OpecMeeting = "opec meeting";
                /// <summary>
                /// Presidential Election
                /// </summary>
                public const string PresidentialElection = "presidential election";
                /// <summary>
                /// Presidential Elections
                /// </summary>
                public const string PresidentialElections = "presidential elections";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// PMI Manufacturing
                /// </summary>
                public const string PurchasingManagersIndexManufacturing = "purchasing managers index manufacturing";
                /// <summary>
                /// Regional Elections
                /// </summary>
                public const string RegionalElections = "regional elections";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// St Stephen's Day
                /// </summary>
                public const string StStephensDay = "st stephens day";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "unemployed persons";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Whit Monday
                /// </summary>
                public const string WhitMonday = "whit monday";
                /// <summary>
                /// Wholesale Prices MoM
                /// </summary>
                public const string WholesalePricesMoM = "wholesale prices mom";
                /// <summary>
                /// Wholesale Prices n.s.a (MoM)
                /// </summary>
                public const string WholesalePricesNotSeasonallyAdjustedMoM = "wholesale prices not seasonally adjusted mom";
                /// <summary>
                /// Wholesale Prices n.s.a (YoY)
                /// </summary>
                public const string WholesalePricesNotSeasonallyAdjustedYoY = "wholesale prices not seasonally adjusted yoy";
                /// <summary>
                /// Wholesale Prices YoY
                /// </summary>
                public const string WholesalePricesYoY = "wholesale prices yoy";
            }
            /// <summary>
            /// Belgium
            /// </summary>
            public static class Belgium
            {
                /// <summary>
                /// All Saints' Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Armistice Day
                /// </summary>
                public const string ArmisticeDay = "armistice day";
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// EU-China Summit
                /// </summary>
                public const string EuChinaSummit = "eu china summit";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Adv
                /// </summary>
                public const string GdpGrowthRateQoQAdv = "gdp growth rate qoq adv";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Adv
                /// </summary>
                public const string GdpGrowthRateYoYAdv = "gdp growth rate yoy adv";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Local Elections
                /// </summary>
                public const string LocalElections = "local elections";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// NATO Summit
                /// </summary>
                public const string NatoSummit = "nato summit";
                /// <summary>
                /// NBB Business Climate
                /// </summary>
                public const string NbbBusinessClimate = "nbb business climate";
                /// <summary>
                /// NBB Business Confidence
                /// </summary>
                public const string NbbBusinessConfidence = "nbb business confidence";
                /// <summary>
                /// New Year’s Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Whit Monday
                /// </summary>
                public const string WhitMonday = "whit monday";
            }
            /// <summary>
            /// Canada
            /// </summary>
            public static class Canada
            {
                /// <summary>
                /// ADP Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "adp employment change";
                /// <summary>
                /// Alberta General Election
                /// </summary>
                public const string AlbertaGeneralElection = "alberta general election";
                /// <summary>
                /// Average Hourly Wages YoY
                /// </summary>
                public const string AverageHourlyWagesYoY = "average hourly wages yoy";
                /// <summary>
                /// Average Weekly Earnings YoY
                /// </summary>
                public const string AverageWeeklyEarningsYoY = "average weekly earnings yoy";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// BoC Beaudry Speech
                /// </summary>
                public const string BankOfCanadaBeaudrySpeech = "bank of canada beaudry speech";
                /// <summary>
                /// Bank of Canada Business Outlook Survey
                /// </summary>
                public const string BankOfCanadaBusinessOutlookSurvey = "bank of canada business outlook survey";
                /// <summary>
                /// Bank of Canada Consumer Price Index Core MoM
                /// </summary>
                public const string BankOfCanadaConsumerPriceIndexCoreMoM = "bank of canada consumer price index core mom";
                /// <summary>
                /// Bank of Canada Consumer Price Index Core YoY
                /// </summary>
                public const string BankOfCanadaConsumerPriceIndexCoreYoY = "bank of canada consumer price index core yoy";
                /// <summary>
                /// Bank of Canada Core Inflation MoM
                /// </summary>
                public const string BankOfCanadaCoreInflationMoM = "bank of canada core inflation mom";
                /// <summary>
                /// Bank of Canada Core Inflation YoY
                /// </summary>
                public const string BankOfCanadaCoreInflationYoY = "bank of canada core inflation yoy";
                /// <summary>
                /// BoC Deputy Gov Patterson Speech
                /// </summary>
                public const string BankOfCanadaDeputyGovPattersonSpeech = "bank of canada deputy gov patterson speech";
                /// <summary>
                /// BoC Deputy Gov Schembri Speech
                /// </summary>
                public const string BankOfCanadaDeputyGovSchembriSpeech = "bank of canada deputy gov schembri speech";
                /// <summary>
                /// BoC Deputy Gov Wilkins Speech
                /// </summary>
                public const string BankOfCanadaDeputyGovWilkinsSpeech = "bank of canada deputy gov wilkins speech";
                /// <summary>
                /// BoC Financial System Review
                /// </summary>
                public const string BankOfCanadaFinancialSystemReview = "bank of canada financial system review";
                /// <summary>
                /// BoC Gov Carney Speaks
                /// </summary>
                public const string BankOfCanadaGovCarneySpeaks = "bank of canada gov carney speaks";
                /// <summary>
                /// BoC Gov Poloz Press Conference
                /// </summary>
                public const string BankOfCanadaGovPolozPressConference = "bank of canada gov poloz press conference";
                /// <summary>
                /// BoC Gov Poloz Speaks
                /// </summary>
                public const string BankOfCanadaGovPolozSpeaks = "bank of canada gov poloz speaks";
                /// <summary>
                /// BoC Gov Poloz Speech
                /// </summary>
                public const string BankOfCanadaGovPolozSpeech = "bank of canada gov poloz speech";
                /// <summary>
                /// BoC Gravelle Speech
                /// </summary>
                public const string BankOfCanadaGravelleSpeech = "bank of canada gravelle speech";
                /// <summary>
                /// BoC Interest Rate Decision
                /// </summary>
                public const string BankOfCanadaInterestRateDecision = "bank of canada interest rate decision";
                /// <summary>
                /// BoC Lane Speech
                /// </summary>
                public const string BankOfCanadaLaneSpeech = "bank of canada lane speech";
                /// <summary>
                /// BoC Leduc Speech
                /// </summary>
                public const string BankOfCanadaLeducSpeech = "bank of canada leduc speech";
                /// <summary>
                /// BoC Monetary Policy Report
                /// </summary>
                public const string BankOfCanadaMonetaryPolicyReport = "bank of canada monetary policy report";
                /// <summary>
                /// BoC Patterson Speech
                /// </summary>
                public const string BankOfCanadaPattersonSpeech = "bank of canada patterson speech";
                /// <summary>
                /// BoC Press Conference
                /// </summary>
                public const string BankOfCanadaPressConference = "bank of canada press conference";
                /// <summary>
                /// BoC Rate Statement
                /// </summary>
                public const string BankOfCanadaRateStatement = "bank of canada rate statement";
                /// <summary>
                /// BoC Review
                /// </summary>
                public const string BankOfCanadaReview = "bank of canada review";
                /// <summary>
                /// BoC Schembri Speech
                /// </summary>
                public const string BankOfCanadaSchembriSpeech = "bank of canada schembri speech";
                /// <summary>
                /// BoC Wilkins Speech
                /// </summary>
                public const string BankOfCanadaWilkinsSpeech = "bank of canada wilkins speech";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "budget balance";
                /// <summary>
                /// Building Permits (MoM)
                /// </summary>
                public const string BuildingPermitsMoM = "building permits mom";
                /// <summary>
                /// Canada Day
                /// </summary>
                public const string CanadaDay = "canada day";
                /// <summary>
                /// Canada Day (Substitute Day)
                /// </summary>
                public const string CanadaDaySubstituteDay = "canada day substitute day";
                /// <summary>
                /// Canadian portfolio investment in foreign securities
                /// </summary>
                public const string CanadianInvestmentInForeignSecurities = "canadian investment in foreign securities";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "capacity utilization";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Civic Holiday
                /// </summary>
                public const string CivicHoliday = "civic holiday";
                /// <summary>
                /// Consumer Price Index - Core MoM
                /// </summary>
                public const string ConsumerPriceIndexCoreMoM = "consumer price index core mom";
                /// <summary>
                /// Core CPI YoY
                /// </summary>
                public const string CoreConsumerPriceIndexYoY = "core consumer price index yoy";
                /// <summary>
                /// Core Inflation Rate MoM
                /// </summary>
                public const string CoreInflationRateMoM = "core inflation rate mom";
                /// <summary>
                /// Core Inflation Rate YoY
                /// </summary>
                public const string CoreInflationRateYoY = "core inflation rate yoy";
                /// <summary>
                /// Core Retail Sales MoM
                /// </summary>
                public const string CoreRetailSalesMoM = "core retail sales mom";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "employment change";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "exports";
                /// <summary>
                /// Family Day
                /// </summary>
                public const string FamilyDay = "family day";
                /// <summary>
                /// Federal Election
                /// </summary>
                public const string FederalElection = "federal election";
                /// <summary>
                /// Finance Minister Morneau Speech
                /// </summary>
                public const string FinanceMinisterMorneauSpeech = "finance minister morneau speech";
                /// <summary>
                /// Foreign portfolio investment in Canadian securities
                /// </summary>
                public const string ForeignInvestmentInCanadianSecurities = "foreign investment in canadian securities";
                /// <summary>
                /// Foreign Securities Purchases
                /// </summary>
                public const string ForeignSecuritiesPurchases = "foreign securities purchases";
                /// <summary>
                /// Foreign Securities Purchases by Canadians
                /// </summary>
                public const string ForeignSecuritiesPurchasesByCanadians = "foreign securities purchases by canadians";
                /// <summary>
                /// Full Employment Change
                /// </summary>
                public const string FullEmploymentChange = "full employment change";
                /// <summary>
                /// Full Time Employment Chg
                /// </summary>
                public const string FullTimeEmploymentChange = "full time employment change";
                /// <summary>
                /// GDP Growth Rate Annualized
                /// </summary>
                public const string GdpGrowthRateAnnualized = "gdp growth rate annualized";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Implicit Price QoQ
                /// </summary>
                public const string GdpImplicitPriceQoQ = "gdp implicit price qoq";
                /// <summary>
                /// GDP MoM
                /// </summary>
                public const string GdpMoM = "gdp mom";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Gross Domestic Product (MoM)
                /// </summary>
                public const string GrossDomesticProductMoM = "gross domestic product mom";
                /// <summary>
                /// G7 Summit
                /// </summary>
                public const string GSevenSummit = "g7 summit";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "housing starts";
                /// <summary>
                /// Housing Starts s.a YoY
                /// </summary>
                public const string HousingStartsSeasonallyAdjustedYoY = "housing starts seasonally adjusted yoy";
                /// <summary>
                /// Housing Starts YoY
                /// </summary>
                public const string HousingStartsYoY = "housing starts yoy";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "imports";
                /// <summary>
                /// Industrial Product Price MoM
                /// </summary>
                public const string IndustrialProductPriceMoM = "industrial product price mom";
                /// <summary>
                /// Industrial Product Price YoY
                /// </summary>
                public const string IndustrialProductPriceYoY = "industrial product price yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Portfolio investment in foreign securities
                /// </summary>
                public const string InvestmentInForeignSecurities = "investment in foreign securities";
                /// <summary>
                /// Ivey PMI s.a
                /// </summary>
                public const string IveyPurchasingManagersIndexSeasonallyAdjusted = "ivey purchasing managers index seasonally adjusted";
                /// <summary>
                /// Job Vacancies
                /// </summary>
                public const string JobVacancies = "job vacancies";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Labor Productivity QoQ
                /// </summary>
                public const string LaborProductivityQoQ = "labor productivity qoq";
                /// <summary>
                /// Manufacturing Sales MoM
                /// </summary>
                public const string ManufacturingSalesMoM = "manufacturing sales mom";
                /// <summary>
                /// Manufacturing Sales YoY
                /// </summary>
                public const string ManufacturingSalesYoY = "manufacturing sales yoy";
                /// <summary>
                /// Manufacturing Shipments MoM
                /// </summary>
                public const string ManufacturingShipmentsMoM = "manufacturing shipments mom";
                /// <summary>
                /// Manufacturing Shipments YoY
                /// </summary>
                public const string ManufacturingShipmentsYoY = "manufacturing shipments yoy";
                /// <summary>
                /// Markit Manufacturing PMI
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "markit manufacturing purchasing managers index";
                /// <summary>
                /// Net Change in Employment
                /// </summary>
                public const string NetChangeInEmployment = "net change in employment";
                /// <summary>
                /// New Brunswick Provincial Elections
                /// </summary>
                public const string NewBrunswickProvincialElections = "new brunswick provincial elections";
                /// <summary>
                /// New Housing Price Index (MoM)
                /// </summary>
                public const string NewHousingPriceIndexMoM = "new housing price index mom";
                /// <summary>
                /// New Housing Price Index (YoY)
                /// </summary>
                public const string NewHousingPriceIndexYoY = "new housing price index yoy";
                /// <summary>
                /// New Motor Vehicle Sales
                /// </summary>
                public const string NewMotorVehicleSales = "new motor vehicle sales";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year's Day (Substitute Day)
                /// </summary>
                public const string NewYearsDaySubstituteDay = "new years day substitute day";
                /// <summary>
                /// Ontario Provincial Elections
                /// </summary>
                public const string OntarioProvincialElections = "ontario provincial elections";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Participation rate
                /// </summary>
                public const string ParticipationRate = "participation rate";
                /// <summary>
                /// Part Time Employment Chg
                /// </summary>
                public const string PartTimeEmploymentChange = "part time employment change";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Quebec Provincial Elections
                /// </summary>
                public const string QuebecProvincialElections = "quebec provincial elections";
                /// <summary>
                /// Raw Materials Price Index MoM
                /// </summary>
                public const string RawMaterialsPriceIndexMoM = "raw materials price index mom";
                /// <summary>
                /// Raw Materials Price Index YoY
                /// </summary>
                public const string RawMaterialsPriceIndexYoY = "raw materials price index yoy";
                /// <summary>
                /// RBC Manufacturing PMI
                /// </summary>
                public const string RbcManufacturingPurchasingManagersIndex = "rbc manufacturing purchasing managers index";
                /// <summary>
                /// Remembrance Day
                /// </summary>
                public const string RemembranceDay = "remembrance day";
                /// <summary>
                /// Retail Sales Ex Autos MoM
                /// </summary>
                public const string RetailSalesExcludingAutosMoM = "retail sales excluding autos mom";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Stock Investment by Foreigners
                /// </summary>
                public const string StockInvestmentByForeigners = "stock investment by foreigners";
                /// <summary>
                /// Thanksgiving Day
                /// </summary>
                public const string ThanksgivingDay = "thanksgiving day";
                /// <summary>
                /// 2016 Budget Announcement
                /// </summary>
                public const string TwoThousandSixteenBudgetAnnouncement = "2016 budget announcement";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Victoria Day
                /// </summary>
                public const string VictoriaDay = "victoria day";
                /// <summary>
                /// Wholesale Sales MoM
                /// </summary>
                public const string WholesaleSalesMoM = "wholesale sales mom";
                /// <summary>
                /// Wholesale Sales YoY
                /// </summary>
                public const string WholesaleSalesYoY = "wholesale sales yoy";
            }
            /// <summary>
            /// China
            /// </summary>
            public static class China
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Belt and Road Forum
                /// </summary>
                public const string BeltAndRoadForum = "belt and road forum";
                /// <summary>
                /// Caixin Composite PMI
                /// </summary>
                public const string CaixinCompositePurchasingManagersIndex = "caixin composite purchasing managers index";
                /// <summary>
                /// Caixin Manufacturing PMI
                /// </summary>
                public const string CaixinManufacturingPurchasingManagersIndex = "caixin manufacturing purchasing managers index";
                /// <summary>
                /// Caixin Manufacturing PMI Final
                /// </summary>
                public const string CaixinManufacturingPurchasingManagersIndexFinal = "caixin manufacturing purchasing managers index final";
                /// <summary>
                /// Caixin Manufacturing PMI Flash
                /// </summary>
                public const string CaixinManufacturingPurchasingManagersIndexFlash = "caixin manufacturing purchasing managers index flash";
                /// <summary>
                /// Caixin Services PMI
                /// </summary>
                public const string CaixinServicesPurchasingManagersIndex = "caixin services purchasing managers index";
                /// <summary>
                /// Capital Flows
                /// </summary>
                public const string CapitalFlows = "capital flows";
                /// <summary>
                /// CB Leading Economic Index
                /// </summary>
                public const string CbLeadingEconomicIndex = "cb leading economic index";
                /// <summary>
                /// Central Economic Work Conference
                /// </summary>
                public const string CentralEconomicWorkConference = "central economic work conference";
                /// <summary>
                /// Chinese New Year
                /// </summary>
                public const string ChineseNewYear = "chinese new year";
                /// <summary>
                /// Chinese People’s Political Consultative Conference
                /// </summary>
                public const string ChinesePeoplesPoliticalConsultativeConference = "chinese peoples political consultative conference";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Dragon Boat Festival
                /// </summary>
                public const string DragonBoatFestival = "dragon boat festival";
                /// <summary>
                /// EU-China Summit
                /// </summary>
                public const string EuChinaSummit = "eu china summit";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "exports";
                /// <summary>
                /// Exports YoY
                /// </summary>
                public const string ExportsYoY = "exports yoy";
                /// <summary>
                /// 1st Day National People's Congress
                /// </summary>
                public const string FirstDayNationalPeoplesCongress = "first day national peoples congress";
                /// <summary>
                /// Fixed Asset Investment (YTD) YoY
                /// </summary>
                public const string FixedAssetInvestmentYtdYoY = "fixed asset investment ytd yoy";
                /// <summary>
                /// FDI (YTD) YoY
                /// </summary>
                public const string ForeignDirectInvestmentYtdYoY = "foreign direct investment ytd yoy";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "foreign exchange reserves";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// G20 Fin Ministers and CB Governors Meeting
                /// </summary>
                public const string GTwentyFinMinistersAndCbGovernorsMeeting = "g20 fin ministers and cb governors meeting";
                /// <summary>
                /// G20 Meeting
                /// </summary>
                public const string GTwentyMeeting = "g20 meeting";
                /// <summary>
                /// House Price Index MoM
                /// </summary>
                public const string HousePriceIndexMoM = "house price index mom";
                /// <summary>
                /// House Price Index YoY
                /// </summary>
                public const string HousePriceIndexYoY = "house price index yoy";
                /// <summary>
                /// HSBC China Services PMI
                /// </summary>
                public const string HsbcChinaServicesPurchasingManagersIndex = "hsbc china services purchasing managers index";
                /// <summary>
                /// HSBC Manufacturing PMI
                /// </summary>
                public const string HsbcManufacturingPurchasingManagersIndex = "hsbc manufacturing purchasing managers index";
                /// <summary>
                /// HSBC Manufacturing PMI Final
                /// </summary>
                public const string HsbcManufacturingPurchasingManagersIndexFinal = "hsbc manufacturing purchasing managers index final";
                /// <summary>
                /// HSBC Manufacturing PMI Flash
                /// </summary>
                public const string HsbcManufacturingPurchasingManagersIndexFlash = "hsbc manufacturing purchasing managers index flash";
                /// <summary>
                /// HSBC Manufacturing PMI Prel.
                /// </summary>
                public const string HsbcManufacturingPurchasingManagersIndexPreliminary = "hsbc manufacturing purchasing managers index preliminary";
                /// <summary>
                /// HSBC Services PMI
                /// </summary>
                public const string HsbcServicesPurchasingManagersIndex = "hsbc services purchasing managers index";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "imports";
                /// <summary>
                /// Imports YoY
                /// </summary>
                public const string ImportsYoY = "imports yoy";
                /// <summary>
                /// Industrial Capacity Capacity Utilization
                /// </summary>
                public const string IndustrialCapacityUtilization = "industrial capacity utilization";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Industrial Profits (YTD) YoY
                /// </summary>
                public const string IndustrialProfitsYtdYoY = "industrial profits ytd yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "interest rate decision";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Loan Prime Rate 5Y
                /// </summary>
                public const string LoanPrimeRateFiveYear = "loan prime rate 5y";
                /// <summary>
                /// Loan Prime Rate 1Y
                /// </summary>
                public const string LoanPrimeRateOneYear = "loan prime rate 1y";
                /// <summary>
                /// Mid-Autumn Festival
                /// </summary>
                public const string MidAutumnFestival = "mid autumn festival";
                /// <summary>
                /// Mid-Autumn Holiday
                /// </summary>
                public const string MidAutumnHoliday = "mid autumn holiday";
                /// <summary>
                /// MNI Business Sentiment
                /// </summary>
                public const string MniBusinessSentiment = "mni business sentiment";
                /// <summary>
                /// M2 Money Supply YoY
                /// </summary>
                public const string MTwoMoneySupplyYoY = "m2 money supply yoy";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// National People’s Congress
                /// </summary>
                public const string NationalPeoplesCongress = "national peoples congress";
                /// <summary>
                /// NBS Manufacturing PMI
                /// </summary>
                public const string NbsManufacturingPurchasingManagersIndex = "nbs manufacturing purchasing managers index";
                /// <summary>
                /// NBS Press Conference
                /// </summary>
                public const string NbsPressConference = "nbs press conference";
                /// <summary>
                /// New Loans
                /// </summary>
                public const string NewLoans = "new loans";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Yuan Loans
                /// </summary>
                public const string NewYuanLoans = "new yuan loans";
                /// <summary>
                /// Non Manufacturing PMI
                /// </summary>
                public const string NonManufacturingPurchasingManagersIndex = "non manufacturing purchasing managers index";
                /// <summary>
                /// Outstanding Loan Growth YoY
                /// </summary>
                public const string OutstandingLoanGrowthYoY = "outstanding loan growth yoy";
                /// <summary>
                /// President Jinping Speech at Boao Forum
                /// </summary>
                public const string PresidentJinpingSpeechAtBoaoForum = "president jinping speech at boao forum";
                /// <summary>
                /// President Xi Jinping Speech
                /// </summary>
                public const string PresidentXiJinpingSpeech = "president xi jinping speech";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Qing Ming Jie
                /// </summary>
                public const string QingMingJie = "qing ming jie";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Total Social Financing
                /// </summary>
                public const string TotalSocialFinancing = "total social financing";
                /// <summary>
                /// Urban Investment YoY
                /// </summary>
                public const string UrbanInvestmentYoY = "urban investment yoy";
                /// <summary>
                /// Urban Investment YTD
                /// </summary>
                public const string UrbanInvestmentYtd = "urban investment ytd";
                /// <summary>
                /// Urban investment (YTD) (YoY)
                /// </summary>
                public const string UrbanInvestmentYtdYoY = "urban investment ytd yoy";
                /// <summary>
                /// US-China Trade Talks
                /// </summary>
                public const string UsChinaTradeTalks = "us china trade talks";
                /// <summary>
                /// Vehicle Sales YoY
                /// </summary>
                public const string VehicleSalesYoY = "vehicle sales yoy";
                /// <summary>
                /// Victory Day
                /// </summary>
                public const string VictoryDay = "victory day";
                /// <summary>
                /// Westpac MNI Consumer Sentiment Indicator
                /// </summary>
                public const string WestpacMniConsumerSentiment = "westpac mni consumer sentiment";
            }
            /// <summary>
            /// Cyprus
            /// </summary>
            public static class Cyprus
            {
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Catholic Christmas Day
                /// </summary>
                public const string CatholicChristmasDay = "catholic christmas day";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Clean Monday
                /// </summary>
                public const string CleanMonday = "clean monday";
                /// <summary>
                /// Construction Output YoY
                /// </summary>
                public const string ConstructionOutputYoY = "construction output yoy";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Growth Rate - Final
                /// </summary>
                public const string GdpGrowthRateFinal = "gdp growth rate final";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// Greek Independence Day
                /// </summary>
                public const string GreekIndependenceDay = "greek independence day";
                /// <summary>
                /// Harmonised Inflation Rate YoY
                /// </summary>
                public const string HarmonizedInflationRateYoY = "harmonized inflation rate yoy";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "independence day";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "industrial production";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Ochi Day
                /// </summary>
                public const string OchiDay = "ochi day";
                /// <summary>
                /// Orthodox Easter Monday
                /// </summary>
                public const string OrthodoxEasterMonday = "orthodox easter monday";
                /// <summary>
                /// Orthodox Easter Sunday
                /// </summary>
                public const string OrthodoxEasterSunday = "orthodox easter sunday";
                /// <summary>
                /// Orthodox Easter Tuesday
                /// </summary>
                public const string OrthodoxEasterTuesday = "orthodox easter tuesday";
                /// <summary>
                /// Orthodox Good Friday
                /// </summary>
                public const string OrthodoxGoodFriday = "orthodox good friday";
                /// <summary>
                /// Orthodox Whit Monday
                /// </summary>
                public const string OrthodoxWhitMonday = "orthodox whit monday";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Parliament Vote on Deposit Levy for Bailout Plan
                /// </summary>
                public const string ParliamentVoteOnDepositLevyForBailoutPlan = "parliament vote on deposit levy for bailout plan";
                /// <summary>
                /// Presidential Election
                /// </summary>
                public const string PresidentialElection = "presidential election";
                /// <summary>
                /// Presidential Election Round 2
                /// </summary>
                public const string PresidentialElectionRoundTwo = "presidential election round 2";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Wage Growth YoY
                /// </summary>
                public const string WageGrowthYoY = "wage growth yoy";
            }
            /// <summary>
            /// Estonia
            /// </summary>
            public static class Estonia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Eve
                /// </summary>
                public const string ChristmasEve = "christmas eve";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Eesti Pank Economic Forecast
                /// </summary>
                public const string EestiPankEconomicForecast = "eesti pank economic forecast";
                /// <summary>
                /// Eesti Pank Gov Hansson Speech
                /// </summary>
                public const string EestiPankGovHanssonSpeech = "eesti pank gov hansson speech";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ Second Estimate
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "imports";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "independence day";
                /// <summary>
                /// Independence Restoration Day
                /// </summary>
                public const string IndependenceRestorationDay = "independence restoration day";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Midsummer Day
                /// </summary>
                public const string MidsummerDay = "midsummer day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Pentecost
                /// </summary>
                public const string Pentecost = "pentecost";
                /// <summary>
                /// Presidential Elections
                /// </summary>
                public const string PresidentialElections = "presidential elections";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Spring Day
                /// </summary>
                public const string SpringDay = "spring day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Victory Day
                /// </summary>
                public const string VictoryDay = "victory day";
            }
            /// <summary>
            /// Finland
            /// </summary>
            public static class Finland
            {
                /// <summary>
                /// All Saints' Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// BoF Gov Liikanen Speech
                /// </summary>
                public const string BofGovLiikanenSpeech = "bof gov liikanen speech";
                /// <summary>
                /// BoF Liikanen Speech
                /// </summary>
                public const string BofLiikanenSpeech = "bof liikanen speech";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Eve
                /// </summary>
                public const string ChristmasEve = "christmas eve";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// Epiphany Day
                /// </summary>
                public const string EpiphanyDay = "epiphany day";
                /// <summary>
                /// Export Prices YoY
                /// </summary>
                public const string ExportPricesYoY = "export prices yoy";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "gdp growth rate";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ - Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP YoY
                /// </summary>
                public const string GdpYoY = "gdp yoy";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "import prices";
                /// <summary>
                /// Import Prices YoY
                /// </summary>
                public const string ImportPricesYoY = "import prices yoy";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "independence day";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Midsummer Day
                /// </summary>
                public const string MidsummerDay = "midsummer day";
                /// <summary>
                /// Midsummer Eve
                /// </summary>
                public const string MidsummerEve = "midsummer eve";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Pentecost
                /// </summary>
                public const string Pentecost = "pentecost";
                /// <summary>
                /// Presidential Election
                /// </summary>
                public const string PresidentialElection = "presidential election";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Vappu (May Day)
                /// </summary>
                public const string VappuMayDay = "vappu may day";
            }
            /// <summary>
            /// France
            /// </summary>
            public static class France
            {
                /// <summary>
                /// All Saints' Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Armistice Day
                /// </summary>
                public const string ArmisticeDay = "armistice day";
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Bastille Day
                /// </summary>
                public const string BastilleDay = "bastille day";
                /// <summary>
                /// Budget balance
                /// </summary>
                public const string BudgetBalance = "budget balance";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Consumer Spending MoM
                /// </summary>
                public const string ConsumerSpendingMoM = "consumer spending mom";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Finance Summit 2019
                /// </summary>
                public const string FinanceSummitTwoThousandNineteen = "finance summit 2019";
                /// <summary>
                /// 5-Year BTAN Auction
                /// </summary>
                public const string FiveYearBtanAuction = "5 year btan auction";
                /// <summary>
                /// 4-Year BTAN Auction
                /// </summary>
                public const string FourYearBtanAuction = "4 year btan auction";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ 1st Est
                /// </summary>
                public const string GdpGrowthRateQoQFirstEstimate = "gdp growth rate qoq first estimate";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY 1st Est
                /// </summary>
                public const string GdpGrowthRateYoYFirstEstimate = "gdp growth rate yoy first estimate";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// G7 Finance Ministers and Central Bank Governors Meeting
                /// </summary>
                public const string GSevenFinanceMinistersAndCentralBankGovernorsMeeting = "g7 finance ministers and central bank governors meeting";
                /// <summary>
                /// G7 Summit
                /// </summary>
                public const string GSevenSummit = "g7 summit";
                /// <summary>
                /// Harmonised Inflation Rate MoM
                /// </summary>
                public const string HarmonizedInflationRateMoM = "harmonized inflation rate mom";
                /// <summary>
                /// Harmonised Inflation Rate MoM Final
                /// </summary>
                public const string HarmonizedInflationRateMoMFinal = "harmonized inflation rate mom final";
                /// <summary>
                /// Harmonised Inflation Rate MoM Prel
                /// </summary>
                public const string HarmonizedInflationRateMoMPreliminary = "harmonized inflation rate mom preliminary";
                /// <summary>
                /// Harmonised Inflation Rate YoY
                /// </summary>
                public const string HarmonizedInflationRateYoY = "harmonized inflation rate yoy";
                /// <summary>
                /// Harmonised Inflation Rate YoY Final
                /// </summary>
                public const string HarmonizedInflationRateYoYFinal = "harmonized inflation rate yoy final";
                /// <summary>
                /// Harmonised Inflation Rate YoY Prel
                /// </summary>
                public const string HarmonizedInflationRateYoYPreliminary = "harmonized inflation rate yoy preliminary";
                /// <summary>
                /// Household Consumption MoM
                /// </summary>
                public const string HouseholdConsumptionMoM = "household consumption mom";
                /// <summary>
                /// IEA Oil Market Report
                /// </summary>
                public const string IeaOilMarketReport = "iea oil market report";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate MoM Final
                /// </summary>
                public const string InflationRateMoMFinal = "inflation rate mom final";
                /// <summary>
                /// Inflation Rate MoM Prel
                /// </summary>
                public const string InflationRateMoMPreliminary = "inflation rate mom preliminary";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Inflation Rate YoY Final
                /// </summary>
                public const string InflationRateYoYFinal = "inflation rate yoy final";
                /// <summary>
                /// Inflation Rate YoY Prel
                /// </summary>
                public const string InflationRateYoYPreliminary = "inflation rate yoy preliminary";
                /// <summary>
                /// Jobseekers Total
                /// </summary>
                public const string JobseekersTotal = "jobseekers total";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Manufacturing PMI
                /// </summary>
                public const string ManufacturingPurchasingManagersIndex = "manufacturing purchasing managers index";
                /// <summary>
                /// Markit Composite PMI Final
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndexFinal = "markit composite purchasing managers index final";
                /// <summary>
                /// Markit Composite PMI Flash
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndexFlash = "markit composite purchasing managers index flash";
                /// <summary>
                /// Markit Manufacturing PMI
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Pmi Final
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndexFinal = "markit manufacturing purchasing managers index final";
                /// <summary>
                /// Markit Manufacturing PMI Flash
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndexFlash = "markit manufacturing purchasing managers index flash";
                /// <summary>
                /// Markit Services PMI
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "markit services purchasing managers index";
                /// <summary>
                /// Markit Services PMI Final
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndexFinal = "markit services purchasing managers index final";
                /// <summary>
                /// Markit Services PMI Flash
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndexFlash = "markit services purchasing managers index flash";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Nonfarm Payrolls QoQ
                /// </summary>
                public const string NonFarmPayrollsQoQ = "non farm payrolls qoq";
                /// <summary>
                /// Non Farm Payrolls QoQ Final
                /// </summary>
                public const string NonFarmPayrollsQoQFinal = "non farm payrolls qoq final";
                /// <summary>
                /// Non Farm Payrolls QoQ Prel
                /// </summary>
                public const string NonFarmPayrollsQoQPreliminary = "non farm payrolls qoq preliminary";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Pentecost
                /// </summary>
                public const string Pentecost = "pentecost";
                /// <summary>
                /// Presidential Elections
                /// </summary>
                public const string PresidentialElections = "presidential elections";
                /// <summary>
                /// Private Non Farm Payrolls QoQ Final
                /// </summary>
                public const string PrivateNonFarmPayrollsQoQFinal = "private non farm payrolls qoq final";
                /// <summary>
                /// Private Non Farm Payrolls QoQ Prel
                /// </summary>
                public const string PrivateNonFarmPayrollsQoQPreliminary = "private non farm payrolls qoq preliminary";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Services PMI
                /// </summary>
                public const string ServicesPurchasingManagersIndex = "services purchasing managers index";
                /// <summary>
                /// 6-Month BTF Auction
                /// </summary>
                public const string SixMonthBtfAuction = "6 month btf auction";
                /// <summary>
                /// 10-Year OAT Auction
                /// </summary>
                public const string TenYearOatAuction = "10 year oat auction";
                /// <summary>
                /// 3-Month BTF Auction
                /// </summary>
                public const string ThreeMonthBtfAuction = "3 month btf auction";
                /// <summary>
                /// 3-Year BTAN Auction
                /// </summary>
                public const string ThreeYearBtanAuction = "3 year btan auction";
                /// <summary>
                /// 12-Month BTF Auction
                /// </summary>
                public const string TwelveMonthBtfAuction = "12 month btf auction";
                /// <summary>
                /// 2-Year BTAN Auction
                /// </summary>
                public const string TwoYearBtanAuction = "2 year btan auction";
                /// <summary>
                /// Unemployment Benefit Claims
                /// </summary>
                public const string UnemploymentBenefitClaims = "unemployment benefit claims";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Whit Monday
                /// </summary>
                public const string WhitMonday = "whit monday";
                /// <summary>
                /// WWII Victory Day
                /// </summary>
                public const string WwiiVictoryDay = "wwii victory day";
            }
            /// <summary>
            /// Germany
            /// </summary>
            public static class Germany
            {
                /// <summary>
                /// All Saints' Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Asian Development Bank Annual Meeting
                /// </summary>
                public const string AsianDevelopmentBankAnnualMeeting = "asian development bank annual meeting";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Baden-Württemberg State Election
                /// </summary>
                public const string BadenWrttembergStateElection = "baden wrttemberg state election";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Balance of Trade s.a
                /// </summary>
                public const string BalanceOfTradeSeasonallyAdjusted = "balance of trade seasonally adjusted";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Bundesbank Annual Report
                /// </summary>
                public const string BundesbankAnnualReport = "bundesbank annual report";
                /// <summary>
                /// Bundesbank Annual Report 2017
                /// </summary>
                public const string BundesbankAnnualReportTwoThousandSeventeen = "bundesbank annual report 2017";
                /// <summary>
                /// Bundesbank Balz Speech
                /// </summary>
                public const string BundesbankBalzSpeech = "bundesbank balz speech";
                /// <summary>
                /// Bundesbank Beermann Speech
                /// </summary>
                public const string BundesbankBeermannSpeech = "bundesbank beermann speech";
                /// <summary>
                /// Bundesbank Buch Speech
                /// </summary>
                public const string BundesbankBuchSpeech = "bundesbank buch speech";
                /// <summary>
                /// Bundesbank Dombret Speech
                /// </summary>
                public const string BundesbankDombretSpeech = "bundesbank dombret speech";
                /// <summary>
                /// Bundesbank Mauderer Speech
                /// </summary>
                public const string BundesbankMaudererSpeech = "bundesbank mauderer speech";
                /// <summary>
                /// Bundesbank Monthly Report
                /// </summary>
                public const string BundesbankMonthlyReport = "bundesbank monthly report";
                /// <summary>
                /// Bundesbank President Weidmann Speech
                /// </summary>
                public const string BundesbankPresidentWeidmannSpeech = "bundesbank president weidmann speech";
                /// <summary>
                /// Bundesbank Semi-Annual Forecasts
                /// </summary>
                public const string BundesbankSemiAnnualForecasts = "bundesbank semi annual forecasts";
                /// <summary>
                /// Bundesbank Thiele Speech
                /// </summary>
                public const string BundesbankThieleSpeech = "bundesbank thiele speech";
                /// <summary>
                /// Bundesbank Weidmann Speech
                /// </summary>
                public const string BundesbankWeidmannSpeech = "bundesbank weidmann speech";
                /// <summary>
                /// Bundesbank Wuermeling Speech
                /// </summary>
                public const string BundesbankWuermelingSpeech = "bundesbank wuermeling speech";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Construction PMI
                /// </summary>
                public const string ConstructionPurchasingManagersIndex = "construction purchasing managers index";
                /// <summary>
                /// Corpus Christi
                /// </summary>
                public const string CorpusChristi = "corpus christi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Day of German Unity
                /// </summary>
                public const string DayOfGermanUnity = "day of german unity";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Employed Persons
                /// </summary>
                public const string EmployedPersons = "employed persons";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// Euro Finance Week
                /// </summary>
                public const string EuroFinanceWeek = "euro finance week";
                /// <summary>
                /// Euro Finance Week 2017
                /// </summary>
                public const string EuroFinanceWeekTwoThousandSeventeen = "euro finance week 2017";
                /// <summary>
                /// Exports MoM
                /// </summary>
                public const string ExportsMoM = "exports mom";
                /// <summary>
                /// Exports MoM s.a
                /// </summary>
                public const string ExportsMoMSeasonallyAdjusted = "exports mom seasonally adjusted";
                /// <summary>
                /// Factory Orders MoM
                /// </summary>
                public const string FactoryOrdersMoM = "factory orders mom";
                /// <summary>
                /// Factory Orders YoY
                /// </summary>
                public const string FactoryOrdersYoY = "factory orders yoy";
                /// <summary>
                /// Federal election
                /// </summary>
                public const string FederalElection = "federal election";
                /// <summary>
                /// Finance Minister Schäuble Speech
                /// </summary>
                public const string FinanceMinisterSchubleSpeech = "finance minister schuble speech";
                /// <summary>
                /// 5-Year Bobl Auction
                /// </summary>
                public const string FiveYearBoblAuction = "5 year bobl auction";
                /// <summary>
                /// Frankfurt Finance Summit 2017
                /// </summary>
                public const string FrankfurtFinanceSummitTwoThousandSeventeen = "frankfurt finance summit 2017";
                /// <summary>
                /// Frankfurt Finance Summit 2016
                /// </summary>
                public const string FrankfurtFinanceSummitTwoThousandSixteen = "frankfurt finance summit 2016";
                /// <summary>
                /// Full Year GDP Growth
                /// </summary>
                public const string FullYearGdpGrowth = "full year gdp growth";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "gdp growth rate";
                /// <summary>
                /// GDP Growth Rate 2015 Flash
                /// </summary>
                public const string GdpGrowthRateFlash = "gdp growth rate flash";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// German Buba President Weidmann speech
                /// </summary>
                public const string GermanBubaPresidentWeidmannSpeech = "german buba president weidmann speech";
                /// <summary>
                /// Gfk Consumer Confidence
                /// </summary>
                public const string GfkConsumerConfidence = "gfk consumer confidence";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Government Budget
                /// </summary>
                public const string GovernmentBudget = "government budget";
                /// <summary>
                /// G7 Finance Ministers' Meeting
                /// </summary>
                public const string GSevenFinanceMinistersMeeting = "g7 finance ministers meeting";
                /// <summary>
                /// G20-Conference
                /// </summary>
                public const string GTwentyConference = "g20 conference";
                /// <summary>
                /// G20 Finance Ministers and CB Governors Meeting
                /// </summary>
                public const string GTwentyFinanceMinistersAndCbGovernorsMeeting = "g20 finance ministers and cb governors meeting";
                /// <summary>
                /// G20 Foreign Ministers Meeting
                /// </summary>
                public const string GTwentyForeignMinistersMeeting = "g20 foreign ministers meeting";
                /// <summary>
                /// G20 Summit
                /// </summary>
                public const string GTwentySummit = "g20 summit";
                /// <summary>
                /// Harmonised Inflation Rate MoM
                /// </summary>
                public const string HarmonizedInflationRateMoM = "harmonized inflation rate mom";
                /// <summary>
                /// Harmonised Inflation Rate MoM Final
                /// </summary>
                public const string HarmonizedInflationRateMoMFinal = "harmonized inflation rate mom final";
                /// <summary>
                /// Harmonised Inflation Rate MoM Prel
                /// </summary>
                public const string HarmonizedInflationRateMoMPreliminary = "harmonized inflation rate mom preliminary";
                /// <summary>
                /// Harmonised Inflation Rate YoY
                /// </summary>
                public const string HarmonizedInflationRateYoY = "harmonized inflation rate yoy";
                /// <summary>
                /// Harmonised Inflation Rate YoY Final
                /// </summary>
                public const string HarmonizedInflationRateYoYFinal = "harmonized inflation rate yoy final";
                /// <summary>
                /// Harmonised Inflation Rate YoY Prel
                /// </summary>
                public const string HarmonizedInflationRateYoYPreliminary = "harmonized inflation rate yoy preliminary";
                /// <summary>
                /// IFO Business Climate
                /// </summary>
                public const string IfoBusinessClimate = "ifo business climate";
                /// <summary>
                /// IFO - Current Assessment
                /// </summary>
                public const string IfoCurrentAssessment = "ifo current assessment";
                /// <summary>
                /// IFO Current Conditions
                /// </summary>
                public const string IfoCurrentConditions = "ifo current conditions";
                /// <summary>
                /// IFO Expectations
                /// </summary>
                public const string IfoExpectations = "ifo expectations";
                /// <summary>
                /// IMF Lagarde Speech
                /// </summary>
                public const string ImfLagardeSpeech = "imf lagarde speech";
                /// <summary>
                /// Import Prices MoM
                /// </summary>
                public const string ImportPricesMoM = "import prices mom";
                /// <summary>
                /// Import Prices YoY
                /// </summary>
                public const string ImportPricesYoY = "import prices yoy";
                /// <summary>
                /// Imports MoM s.a
                /// </summary>
                public const string ImportsMoMSeasonallyAdjusted = "imports mom seasonally adjusted";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate MoM Final
                /// </summary>
                public const string InflationRateMoMFinal = "inflation rate mom final";
                /// <summary>
                /// Inflation Rate MoM Flash
                /// </summary>
                public const string InflationRateMoMFlash = "inflation rate mom flash";
                /// <summary>
                /// Inflation Rate MoM Prel
                /// </summary>
                public const string InflationRateMoMPreliminary = "inflation rate mom preliminary";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Inflation Rate YoY Final
                /// </summary>
                public const string InflationRateYoYFinal = "inflation rate yoy final";
                /// <summary>
                /// Inflation Rate YoY Prel
                /// </summary>
                public const string InflationRateYoYPreliminary = "inflation rate yoy preliminary";
                /// <summary>
                /// Job Vacancies
                /// </summary>
                public const string JobVacancies = "job vacancies";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Markit/BME Manufacturing Pmi Final
                /// </summary>
                public const string MarkitBmeManufacturingPurchasingManagersIndexFinal = "markit bme manufacturing purchasing managers index final";
                /// <summary>
                /// Markit/BME Manufacturing PMI Flash
                /// </summary>
                public const string MarkitBmeManufacturingPurchasingManagersIndexFlash = "markit bme manufacturing purchasing managers index flash";
                /// <summary>
                /// Markit Composite PMI Final
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndexFinal = "markit composite purchasing managers index final";
                /// <summary>
                /// Markit Composite PMI Flash
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndexFlash = "markit composite purchasing managers index flash";
                /// <summary>
                /// Markit Manufacturing PMI
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Manufacturing PMI Final
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndexFinal = "markit manufacturing purchasing managers index final";
                /// <summary>
                /// Markit Manufacturing PMI Flash
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndexFlash = "markit manufacturing purchasing managers index flash";
                /// <summary>
                /// Markit Services PMI
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "markit services purchasing managers index";
                /// <summary>
                /// Markit Services PMI Final
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndexFinal = "markit services purchasing managers index final";
                /// <summary>
                /// Markit Services PMI Flash
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndexFlash = "markit services purchasing managers index flash";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Presidential Elections
                /// </summary>
                public const string PresidentialElections = "presidential elections";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Reformation Day
                /// </summary>
                public const string ReformationDay = "reformation day";
                /// <summary>
                /// Repentance Day
                /// </summary>
                public const string RepentanceDay = "repentance day";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Rhineland-Palatinate State Election
                /// </summary>
                public const string RhinelandPalatinateStateElection = "rhineland palatinate state election";
                /// <summary>
                /// Saxony-Anhalt State Election
                /// </summary>
                public const string SaxonyAnhaltStateElection = "saxony anhalt state election";
                /// <summary>
                /// Services PMI
                /// </summary>
                public const string ServicesPurchasingManagersIndex = "services purchasing managers index";
                /// <summary>
                /// St Stephen's Day
                /// </summary>
                public const string StStephensDay = "st stephens day";
                /// <summary>
                /// 10-Year Bund Auction
                /// </summary>
                public const string TenYearBundAuction = "10 year bund auction";
                /// <summary>
                /// 30-Year Bund Auction
                /// </summary>
                public const string ThirtyYearBundAuction = "30 year bund auction";
                /// <summary>
                /// 12-Month Bubill Auction
                /// </summary>
                public const string TwelveMonthBubillAuction = "12 month bubill auction";
                /// <summary>
                /// 2-Year Schatz Auction
                /// </summary>
                public const string TwoYearSchatzAuction = "2 year schatz auction";
                /// <summary>
                /// Unemployed Persons NSA
                /// </summary>
                public const string UnemployedPersonsNotSeasonallyAdjusted = "unemployed persons not seasonally adjusted";
                /// <summary>
                /// Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "unemployment change";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Unemployment Rate Harmonised
                /// </summary>
                public const string UnemploymentRateHarmonized = "unemployment rate harmonized";
                /// <summary>
                /// Whit Monday
                /// </summary>
                public const string WhitMonday = "whit monday";
                /// <summary>
                /// Wholesale Prices MoM
                /// </summary>
                public const string WholesalePricesMoM = "wholesale prices mom";
                /// <summary>
                /// Wholesale Prices YoY
                /// </summary>
                public const string WholesalePricesYoY = "wholesale prices yoy";
                /// <summary>
                /// ZEW Current Conditions
                /// </summary>
                public const string ZewCurrentConditions = "zew current conditions";
                /// <summary>
                /// ZEW Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "zew economic sentiment index";
            }
            /// <summary>
            /// Greece
            /// </summary>
            public static class Greece
            {
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Clean Monday
                /// </summary>
                public const string CleanMonday = "clean monday";
                /// <summary>
                /// Construction Output YoY
                /// </summary>
                public const string ConstructionOutputYoY = "construction output yoy";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Credit Expansion YoY
                /// </summary>
                public const string CreditExpansionYoY = "credit expansion yoy";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Debt Repayment of EUR 3.5B Due to ECB
                /// </summary>
                public const string DebtRepaymentOfEurThirtyFiveBDueToEcb = "debt repayment of eur 35b due to ecb";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// Epiphany Day
                /// </summary>
                public const string EpiphanyDay = "epiphany day";
                /// <summary>
                /// EU Bailout Expiration
                /// </summary>
                public const string EuBailoutExpiration = "eu bailout expiration";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Annual Growth Rate YoY - Preliminary
                /// </summary>
                public const string GdpAnnualGrowthRateYoYPreliminary = "gdp annual growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Adv
                /// </summary>
                public const string GdpGrowthRateYoYAdv = "gdp growth rate yoy adv";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// GDP Growth Rate YoY - P.
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// Greek Parliament Elections
                /// </summary>
                public const string GreekParliamentElections = "greek parliament elections";
                /// <summary>
                /// Greek Third Bailout Program's Vote
                /// </summary>
                public const string GreekThirdBailoutProgramsVote = "greek third bailout programs vote";
                /// <summary>
                /// Harmonized Inflation Rate YoY
                /// </summary>
                public const string HarmonizedInflationRateYoY = "harmonized inflation rate yoy";
                /// <summary>
                /// Holy Spirit Monday
                /// </summary>
                public const string HolySpiritMonday = "holy spirit monday";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "independence day";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Labour Day (Substitute Day)
                /// </summary>
                public const string LaborDaySubstituteDay = "labor day substitute day";
                /// <summary>
                /// Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "loans to private sector";
                /// <summary>
                /// Loans to Private Sector YoY
                /// </summary>
                public const string LoansToPrivateSectorYoY = "loans to private sector yoy";
                /// <summary>
                /// Markit Manufacturing PMI
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "markit manufacturing purchasing managers index";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Ochi Day
                /// </summary>
                public const string OchiDay = "ochi day";
                /// <summary>
                /// Orthodox Easter Monday
                /// </summary>
                public const string OrthodoxEasterMonday = "orthodox easter monday";
                /// <summary>
                /// Orthodox Easter Sunday
                /// </summary>
                public const string OrthodoxEasterSunday = "orthodox easter sunday";
                /// <summary>
                /// Orthodox Good Friday
                /// </summary>
                public const string OrthodoxGoodFriday = "orthodox good friday";
                /// <summary>
                /// Orthodox Whit Monday
                /// </summary>
                public const string OrthodoxWhitMonday = "orthodox whit monday";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Referendum on Bailout Terms
                /// </summary>
                public const string ReferendumOnBailoutTerms = "referendum on bailout terms";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// St Stephen's Day
                /// </summary>
                public const string StStephensDay = "st stephens day";
                /// <summary>
                /// The Ochi Day
                /// </summary>
                public const string TheOchiDay = "the ochi day";
                /// <summary>
                /// 13-Weeks Bill Auction
                /// </summary>
                public const string ThirteenWeekBillAuction = "13 week bill auction";
                /// <summary>
                /// Total Credit YoY
                /// </summary>
                public const string TotalCreditYoY = "total credit yoy";
                /// <summary>
                /// 26-Weeks Bill Auction
                /// </summary>
                public const string TwentySixWeekBillAuction = "26 week bill auction";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
            }
            /// <summary>
            /// Ireland
            /// </summary>
            public static class Ireland
            {
                /// <summary>
                /// AIB Manufacturing PMI
                /// </summary>
                public const string AibManufacturingPurchasingManagersIndex = "aib manufacturing purchasing managers index";
                /// <summary>
                /// AIB Services PMI
                /// </summary>
                public const string AibServicesPurchasingManagersIndex = "aib services purchasing managers index";
                /// <summary>
                /// August Bank Holiday
                /// </summary>
                public const string AugustBankHoliday = "august bank holiday";
                /// <summary>
                /// Average Weekly Earnings YoY
                /// </summary>
                public const string AverageWeeklyEarningsYoY = "average weekly earnings yoy";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Balance of Trade-Final
                /// </summary>
                public const string BalanceOfTradeFinal = "balance of trade final";
                /// <summary>
                /// Bank Holiday
                /// </summary>
                public const string BankHoliday = "bank holiday";
                /// <summary>
                /// CBI Gov Lane Speech
                /// </summary>
                public const string CbiGovLaneSpeech = "cbi gov lane speech";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Construction Output YoY
                /// </summary>
                public const string ConstructionOutputYoY = "construction output yoy";
                /// <summary>
                /// Construction PMI
                /// </summary>
                public const string ConstructionPurchasingManagersIndex = "construction purchasing managers index";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "core inflation rate";
                /// <summary>
                /// Core Inflation Rate YoY
                /// </summary>
                public const string CoreInflationRateYoY = "core inflation rate yoy";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// ESRI Consumer Sentiment Index
                /// </summary>
                public const string EsriConsumerSentimentIndex = "esri consumer sentiment index";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GNP QoQ
                /// </summary>
                public const string GnpQoQ = "gnp qoq";
                /// <summary>
                /// GNP YoY
                /// </summary>
                public const string GnpYoY = "gnp yoy";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Harmonised Inflation Rate MoM
                /// </summary>
                public const string HarmonizedInflationRateMoM = "harmonized inflation rate mom";
                /// <summary>
                /// Harmonised Inflation Rate YoY
                /// </summary>
                public const string HarmonizedInflationRateYoY = "harmonized inflation rate yoy";
                /// <summary>
                /// Household Saving Ratio
                /// </summary>
                public const string HouseholdSavingRatio = "household saving ratio";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "inflation rate";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Investec Manufacturing Pmi
                /// </summary>
                public const string InvestecManufacturingPurchasingManagersIndex = "investec manufacturing purchasing managers index";
                /// <summary>
                /// Investec Services PMI
                /// </summary>
                public const string InvestecServicesPurchasingManagersIndex = "investec services purchasing managers index";
                /// <summary>
                /// June Bank Holiday
                /// </summary>
                public const string JuneBankHoliday = "june bank holiday";
                /// <summary>
                /// Markit Services PMI
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "markit services purchasing managers index";
                /// <summary>
                /// May Bank Holiday
                /// </summary>
                public const string MayBankHoliday = "may bank holiday";
                /// <summary>
                /// May Day
                /// </summary>
                public const string MayDay = "may day";
                /// <summary>
                /// New Year’s Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year’s Day (Substitute Day)
                /// </summary>
                public const string NewYearsDaySubstituteDay = "new years day substitute day";
                /// <summary>
                /// October Bank Holiday
                /// </summary>
                public const string OctoberBankHoliday = "october bank holiday";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Purchasing Manager Index Manufacturing
                /// </summary>
                public const string PurchasingManagerIndexManufacturing = "purchasing manager index manufacturing";
                /// <summary>
                /// Purchasing Manager Index Services
                /// </summary>
                public const string PurchasingManagerIndexServices = "purchasing manager index services";
                /// <summary>
                /// PMI services
                /// </summary>
                public const string PurchasingManagersIndexServices = "purchasing managers index services";
                /// <summary>
                /// Residential Property Prices MoM
                /// </summary>
                public const string ResidentialPropertyPricesMoM = "residential property prices mom";
                /// <summary>
                /// Residential Property Prices YoY
                /// </summary>
                public const string ResidentialPropertyPricesYoY = "residential property prices yoy";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// St. Patricks Day
                /// </summary>
                public const string StPatricksDay = "st patricks day";
                /// <summary>
                /// St Stephen's Day
                /// </summary>
                public const string StStephensDay = "st stephens day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Wholesale Prices MoM
                /// </summary>
                public const string WholesalePricesMoM = "wholesale prices mom";
                /// <summary>
                /// Wholesale Prices YoY
                /// </summary>
                public const string WholesalePricesYoY = "wholesale prices yoy";
            }
            /// <summary>
            /// Italy
            /// </summary>
            public static class Italy
            {
                /// <summary>
                /// All Saints' Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// BoI Rossi Speech
                /// </summary>
                public const string BoiRossiSpeech = "boi rossi speech";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Construction Output YoY
                /// </summary>
                public const string ConstructionOutputYoY = "construction output yoy";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Consumer Price Index (EU Norm) (MoM)
                /// </summary>
                public const string ConsumerPriceIndexEuNormMoM = "consumer price index eu norm mom";
                /// <summary>
                /// Consumer Price Index (EU Norm) (YoY)
                /// </summary>
                public const string ConsumerPriceIndexEuNormYoY = "consumer price index eu norm yoy";
                /// <summary>
                /// Consumer Price Index (MoM)
                /// </summary>
                public const string ConsumerPriceIndexMoM = "consumer price index mom";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Economy and Finance Minister Padoan Speech
                /// </summary>
                public const string EconomyAndFinanceMinisterPadoanSpeech = "economy and finance minister padoan speech";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// 5-y Bond Auction
                /// </summary>
                public const string FiveYBondAuction = "5 y bond auction";
                /// <summary>
                /// 5-Year BTP Auction
                /// </summary>
                public const string FiveYearBtpAuction = "5 year btp auction";
                /// <summary>
                /// Full Year GDP Growth
                /// </summary>
                public const string FullYearGdpGrowth = "full year gdp growth";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Growth Rate 2015
                /// </summary>
                public const string GdpGrowthRate = "gdp growth rate";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Adv
                /// </summary>
                public const string GdpGrowthRateQoQAdv = "gdp growth rate qoq adv";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Adv
                /// </summary>
                public const string GdpGrowthRateYoYAdv = "gdp growth rate yoy adv";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// General Elections
                /// </summary>
                public const string GeneralElections = "general elections";
                /// <summary>
                /// Government Budget
                /// </summary>
                public const string GovernmentBudget = "government budget";
                /// <summary>
                /// G7 Fin Ministers and CB Governors Meeting
                /// </summary>
                public const string GSevenFinMinistersAndCbGovernorsMeeting = "g7 fin ministers and cb governors meeting";
                /// <summary>
                /// G7 Foreign Ministers Meeting
                /// </summary>
                public const string GSevenForeignMinistersMeeting = "g7 foreign ministers meeting";
                /// <summary>
                /// G7 Summit
                /// </summary>
                public const string GSevenSummit = "g7 summit";
                /// <summary>
                /// Harmonised Inflation Rate MoM Final
                /// </summary>
                public const string HarmonizedInflationRateMoMFinal = "harmonized inflation rate mom final";
                /// <summary>
                /// Harmonised Inflation Rate MoM Prel
                /// </summary>
                public const string HarmonizedInflationRateMoMPreliminary = "harmonized inflation rate mom preliminary";
                /// <summary>
                /// Harmonised Inflation Rate YoY Final
                /// </summary>
                public const string HarmonizedInflationRateYoYFinal = "harmonized inflation rate yoy final";
                /// <summary>
                /// Harmonised Inflation Rate YoY Prel
                /// </summary>
                public const string HarmonizedInflationRateYoYPreliminary = "harmonized inflation rate yoy preliminary";
                /// <summary>
                /// Immaculate Conception
                /// </summary>
                public const string ImmaculateConception = "immaculate conception";
                /// <summary>
                /// Industrial Orders MoM
                /// </summary>
                public const string IndustrialOrdersMoM = "industrial orders mom";
                /// <summary>
                /// Industrial Orders YoY
                /// </summary>
                public const string IndustrialOrdersYoY = "industrial orders yoy";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Industrial Sales MoM
                /// </summary>
                public const string IndustrialSalesMoM = "industrial sales mom";
                /// <summary>
                /// Industrial Sales YoY
                /// </summary>
                public const string IndustrialSalesYoY = "industrial sales yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate MoM Final
                /// </summary>
                public const string InflationRateMoMFinal = "inflation rate mom final";
                /// <summary>
                /// Inflation Rate MoM Prel
                /// </summary>
                public const string InflationRateMoMPreliminary = "inflation rate mom preliminary";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Inflation Rate YoY Final
                /// </summary>
                public const string InflationRateYoYFinal = "inflation rate yoy final";
                /// <summary>
                /// Inflation Rate YoY Prel
                /// </summary>
                public const string InflationRateYoYPreliminary = "inflation rate yoy preliminary";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Liberation Day
                /// </summary>
                public const string LiberationDay = "liberation day";
                /// <summary>
                /// Manufacturing PMI
                /// </summary>
                public const string ManufacturingPurchasingManagersIndex = "manufacturing purchasing managers index";
                /// <summary>
                /// Markit/ADACI Manufacturing PMI
                /// </summary>
                public const string MarkitAdaciManufacturingPurchasingManagersIndex = "markit adaci manufacturing purchasing managers index";
                /// <summary>
                /// Markit/ADACI Services PMI
                /// </summary>
                public const string MarkitAdaciServicesPurchasingManagersIndex = "markit adaci services purchasing managers index";
                /// <summary>
                /// New Year’s Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// PM Conte Press Conference
                /// </summary>
                public const string PmContePressConference = "pm conte press conference";
                /// <summary>
                /// PM Conte Speech in the Senate
                /// </summary>
                public const string PmConteSpeechInTheSenate = "pm conte speech in the senate";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Referendum on Constitutional Reform
                /// </summary>
                public const string ReferendumOnConstitutionalReform = "referendum on constitutional reform";
                /// <summary>
                /// Regional Elections
                /// </summary>
                public const string RegionalElections = "regional elections";
                /// <summary>
                /// Renzi, Merkel, Hollande Meet on Brexit
                /// </summary>
                public const string RenziMerkelHollandeMeetOnBrexit = "renzi merkel hollande meet on brexit";
                /// <summary>
                /// Republic Day
                /// </summary>
                public const string RepublicDay = "republic day";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// 7-Year BTP Auction
                /// </summary>
                public const string SevenYearBtpAuction = "7 year btp auction";
                /// <summary>
                /// 6-Month BOT Auction
                /// </summary>
                public const string SixMonthBotAuction = "6 month bot auction";
                /// <summary>
                /// St Stephen's Day
                /// </summary>
                public const string StStephensDay = "st stephens day";
                /// <summary>
                /// 10-Year Bond Auction
                /// </summary>
                public const string TenYearBondAuction = "10 year bond auction";
                /// <summary>
                /// 10-Year BTP Auction
                /// </summary>
                public const string TenYearBtpAuction = "10 year btp auction";
                /// <summary>
                /// 30-Year BTP Auction
                /// </summary>
                public const string ThirtyYearBtpAuction = "30 year btp auction";
                /// <summary>
                /// 3-Year BTP Auction
                /// </summary>
                public const string ThreeYearBtpAuction = "3 year btp auction";
                /// <summary>
                /// 12-Month BOT Auction
                /// </summary>
                public const string TwelveMonthBotAuction = "12 month bot auction";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Wage Inflation MoM
                /// </summary>
                public const string WageInflationMoM = "wage inflation mom";
                /// <summary>
                /// Wage Inflation YoY
                /// </summary>
                public const string WageInflationYoY = "wage inflation yoy";
            }
            /// <summary>
            /// Japan
            /// </summary>
            public static class Japan
            {
                /// <summary>
                /// All Industry Activity Index MoM
                /// </summary>
                public const string AllIndustryActivityIndexMoM = "all industry activity index mom";
                /// <summary>
                /// Autumn Equinox Day
                /// </summary>
                public const string AutumnEquinoxDay = "autumn equinox day";
                /// <summary>
                /// Average Cash Earnings YoY
                /// </summary>
                public const string AverageCashEarningsYoY = "average cash earnings yoy";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Bank Holiday
                /// </summary>
                public const string BankHoliday = "bank holiday";
                /// <summary>
                /// Bank lending YoY
                /// </summary>
                public const string BankLendingYoY = "bank lending yoy";
                /// <summary>
                /// BoJ Amamiya Speech
                /// </summary>
                public const string BankOfJapanAmamiyaSpeech = "bank of japan amamiya speech";
                /// <summary>
                /// BoJ Deputy Gov Iwata Speech
                /// </summary>
                public const string BankOfJapanDeputyGovIwataSpeech = "bank of japan deputy gov iwata speech";
                /// <summary>
                /// BoJ Deputy Gov Nakaso Speech
                /// </summary>
                public const string BankOfJapanDeputyGovNakasoSpeech = "bank of japan deputy gov nakaso speech";
                /// <summary>
                /// BoJ Funo Speech
                /// </summary>
                public const string BankOfJapanFunoSpeech = "bank of japan funo speech";
                /// <summary>
                /// BoJ Gov Kuroda Speaks
                /// </summary>
                public const string BankOfJapanGovKurodaSpeaks = "bank of japan gov kuroda speaks";
                /// <summary>
                /// BoJ Gov Kuroda Speech
                /// </summary>
                public const string BankOfJapanGovKurodaSpeech = "bank of japan gov kuroda speech";
                /// <summary>
                /// BoJ Harada Speech
                /// </summary>
                public const string BankOfJapanHaradaSpeech = "bank of japan harada speech";
                /// <summary>
                /// BoJ Interest Rate Decision
                /// </summary>
                public const string BankOfJapanInterestRateDecision = "bank of japan interest rate decision";
                /// <summary>
                /// BoJ Iwata Speech
                /// </summary>
                public const string BankOfJapanIwataSpeech = "bank of japan iwata speech";
                /// <summary>
                /// BoJ Kataoka Speech
                /// </summary>
                public const string BankOfJapanKataokaSpeech = "bank of japan kataoka speech";
                /// <summary>
                /// BoJ Kiuchi Speech
                /// </summary>
                public const string BankOfJapanKiuchiSpeech = "bank of japan kiuchi speech";
                /// <summary>
                /// BoJ Kuroda Speech
                /// </summary>
                public const string BankOfJapanKurodaSpeech = "bank of japan kuroda speech";
                /// <summary>
                /// BoJ Kuwabara Speech
                /// </summary>
                public const string BankOfJapanKuwabaraSpeech = "bank of japan kuwabara speech";
                /// <summary>
                /// BoJ Masai Speech
                /// </summary>
                public const string BankOfJapanMasaiSpeech = "bank of japan masai speech";
                /// <summary>
                /// BoJ Monetary Policy Meeting Minutes
                /// </summary>
                public const string BankOfJapanMonetaryPolicyMeetingMinutes = "bank of japan monetary policy meeting minutes";
                /// <summary>
                /// BOJ Monetary Policy Statement
                /// </summary>
                public const string BankOfJapanMonetaryPolicyStatement = "bank of japan monetary policy statement";
                /// <summary>
                /// BoJ Monetary Policy Statement and press conference
                /// </summary>
                public const string BankOfJapanMonetaryPolicyStatementAndPressConference = "bank of japan monetary policy statement and press conference";
                /// <summary>
                /// BoJ Monthly Report
                /// </summary>
                public const string BankOfJapanMonthlyReport = "bank of japan monthly report";
                /// <summary>
                /// BoJ Nakaso Speech
                /// </summary>
                public const string BankOfJapanNakasoSpeech = "bank of japan nakaso speech";
                /// <summary>
                /// BoJ Press Conference
                /// </summary>
                public const string BankOfJapanPressConference = "bank of japan press conference";
                /// <summary>
                /// BoJ Quarterly Outlook Report
                /// </summary>
                public const string BankOfJapanQuarterlyOutlookReport = "bank of japan quarterly outlook report";
                /// <summary>
                /// BoJ Quarterly Report
                /// </summary>
                public const string BankOfJapanQuarterlyReport = "bank of japan quarterly report";
                /// <summary>
                /// BoJ Sakurai Speech
                /// </summary>
                public const string BankOfJapanSakuraiSpeech = "bank of japan sakurai speech";
                /// <summary>
                /// BoJ Sato Speech
                /// </summary>
                public const string BankOfJapanSatoSpeech = "bank of japan sato speech";
                /// <summary>
                /// BoJ Summary of Opinions
                /// </summary>
                public const string BankOfJapanSummaryOfOpinions = "bank of japan summary of opinions";
                /// <summary>
                /// BoJ Suzuki Speech
                /// </summary>
                public const string BankOfJapanSuzukiSpeech = "bank of japan suzuki speech";
                /// <summary>
                /// BoJ Wakatabe Speech
                /// </summary>
                public const string BankOfJapanWakatabeSpeech = "bank of japan wakatabe speech";
                /// <summary>
                /// BSI Large Manufacturing QoQ
                /// </summary>
                public const string BusinessSurveyIndexLargeManufacturingQoQ = "business survey index large manufacturing qoq";
                /// <summary>
                /// Capacity Utilization MoM
                /// </summary>
                public const string CapacityUtilizationMoM = "capacity utilization mom";
                /// <summary>
                /// Capital Spending YoY
                /// </summary>
                public const string CapitalSpendingYoY = "capital spending yoy";
                /// <summary>
                /// Children's Day
                /// </summary>
                public const string ChildrensDay = "childrens day";
                /// <summary>
                /// Coincident Index
                /// </summary>
                public const string CoincidentIndex = "coincident index";
                /// <summary>
                /// Coincident Index Final
                /// </summary>
                public const string CoincidentIndexFinal = "coincident index final";
                /// <summary>
                /// Coincident Index Prel
                /// </summary>
                public const string CoincidentIndexPreliminary = "coincident index preliminary";
                /// <summary>
                /// Coming of Age Day
                /// </summary>
                public const string ComingOfAgeDay = "coming of age day";
                /// <summary>
                /// Constitution Day
                /// </summary>
                public const string ConstitutionDay = "constitution day";
                /// <summary>
                /// Construction Orders YoY
                /// </summary>
                public const string ConstructionOrdersYoY = "construction orders yoy";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Consumer Confidence Households
                /// </summary>
                public const string ConsumerConfidenceHouseholds = "consumer confidence households";
                /// <summary>
                /// CPI Ex-Fresh Food YoY
                /// </summary>
                public const string ConsumerPriceIndexExcludingFreshFoodYoY = "consumer price index excluding fresh food yoy";
                /// <summary>
                /// Core Inflation Rate YoY
                /// </summary>
                public const string CoreInflationRateYoY = "core inflation rate yoy";
                /// <summary>
                /// Corporate Service Price YoY
                /// </summary>
                public const string CorporateServicePriceYoY = "corporate service price yoy";
                /// <summary>
                /// Culture Day
                /// </summary>
                public const string CultureDay = "culture day";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Domestic Corporate Goods Price Index (MoM)
                /// </summary>
                public const string DomesticCorporateGoodsPriceIndexMoM = "domestic corporate goods price index mom";
                /// <summary>
                /// Domestic Corporate Goods Price Index (YoY)
                /// </summary>
                public const string DomesticCorporateGoodsPriceIndexYoY = "domestic corporate goods price index yoy";
                /// <summary>
                /// Eco Watchers Survey Current
                /// </summary>
                public const string EcoWatchersSurveyCurrent = "eco watchers survey current";
                /// <summary>
                /// Eco Watchers Survey Outlook
                /// </summary>
                public const string EcoWatchersSurveyOutlook = "eco watchers survey outlook";
                /// <summary>
                /// Emperor's Birthday
                /// </summary>
                public const string EmperorsBirthday = "emperors birthday";
                /// <summary>
                /// EU-Japan Summit
                /// </summary>
                public const string EuJapanSummit = "eu japan summit";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "exports";
                /// <summary>
                /// Exports YoY
                /// </summary>
                public const string ExportsYoY = "exports yoy";
                /// <summary>
                /// Finance Minister Aso Speech
                /// </summary>
                public const string FinanceMinisterAsoSpeech = "finance minister aso speech";
                /// <summary>
                /// Foreign bond investment
                /// </summary>
                public const string ForeignBondInvestment = "foreign bond investment";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "foreign exchange reserves";
                /// <summary>
                /// Foreign investment in Japan stocks
                /// </summary>
                public const string ForeignInvestmentInJapanStocks = "foreign investment in japan stocks";
                /// <summary>
                /// GDP Annual Growth Rate YoY Final
                /// </summary>
                public const string GdpAnnualGrowthRateYoYFinal = "gdp annual growth rate yoy final";
                /// <summary>
                /// GDP Capital Expenditure QoQ
                /// </summary>
                public const string GdpCapitalExpenditureQoQ = "gdp capital expenditure qoq";
                /// <summary>
                /// GDP Capital Expenditure QoQ Final
                /// </summary>
                public const string GdpCapitalExpenditureQoQFinal = "gdp capital expenditure qoq final";
                /// <summary>
                /// GDP Capital Expenditure QoQ Prel
                /// </summary>
                public const string GdpCapitalExpenditureQoQPreliminary = "gdp capital expenditure qoq preliminary";
                /// <summary>
                /// GDP Deflator YoY Final
                /// </summary>
                public const string GdpDeflatorYoYFinal = "gdp deflator yoy final";
                /// <summary>
                /// GDP Deflator YoY Prel
                /// </summary>
                public const string GdpDeflatorYoYPreliminary = "gdp deflator yoy preliminary";
                /// <summary>
                /// GDP External Demand QoQ
                /// </summary>
                public const string GdpExternalDemandQoQ = "gdp external demand qoq";
                /// <summary>
                /// GDP External Demand QoQ Final
                /// </summary>
                public const string GdpExternalDemandQoQFinal = "gdp external demand qoq final";
                /// <summary>
                /// GDP External Demand QoQ Prel
                /// </summary>
                public const string GdpExternalDemandQoQPreliminary = "gdp external demand qoq preliminary";
                /// <summary>
                /// Gdp Growth Annualized Final
                /// </summary>
                public const string GdpGrowthAnnualizedFinal = "gdp growth annualized final";
                /// <summary>
                /// Gdp Growth Annualized Prel.
                /// </summary>
                public const string GdpGrowthAnnualizedPreliminary = "gdp growth annualized preliminary";
                /// <summary>
                /// GDP Growth Annualized QoQ
                /// </summary>
                public const string GdpGrowthAnnualizedQoQ = "gdp growth annualized qoq";
                /// <summary>
                /// GDP Growth Annualized QoQ Final
                /// </summary>
                public const string GdpGrowthAnnualizedQoQFinal = "gdp growth annualized qoq final";
                /// <summary>
                /// GDP Growth Annualized QoQ Prel.
                /// </summary>
                public const string GdpGrowthAnnualizedQoQPreliminary = "gdp growth annualized qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Price Index YoY Final
                /// </summary>
                public const string GdpPriceIndexYoYFinal = "gdp price index yoy final";
                /// <summary>
                /// GDP Price Index YoY Prel
                /// </summary>
                public const string GdpPriceIndexYoYPreliminary = "gdp price index yoy preliminary";
                /// <summary>
                /// GDP Private Consumption QoQ
                /// </summary>
                public const string GdpPrivateConsumptionQoQ = "gdp private consumption qoq";
                /// <summary>
                /// GDP Private Consumption QoQ Final
                /// </summary>
                public const string GdpPrivateConsumptionQoQFinal = "gdp private consumption qoq final";
                /// <summary>
                /// GDP Private Consumption QoQ Prel
                /// </summary>
                public const string GdpPrivateConsumptionQoQPreliminary = "gdp private consumption qoq preliminary";
                /// <summary>
                /// General Elections
                /// </summary>
                public const string GeneralElections = "general elections";
                /// <summary>
                /// Greenery Day
                /// </summary>
                public const string GreeneryDay = "greenery day";
                /// <summary>
                /// Gross Domestic Product Annualized Final
                /// </summary>
                public const string GrossDomesticProductAnnualizedFinal = "gross domestic product annualized final";
                /// <summary>
                /// Gross Domestic Product Annualized (R)
                /// </summary>
                public const string GrossDomesticProductAnnualizedRevised = "gross domestic product annualized revised";
                /// <summary>
                /// G7 Finance Ministers and Central Bank Governors’ Meeting
                /// </summary>
                public const string GSevenFinanceMinistersAndCentralBankGovernorsMeeting = "g7 finance ministers and central bank governors meeting";
                /// <summary>
                /// G7 Summit
                /// </summary>
                public const string GSevenSummit = "g7 summit";
                /// <summary>
                /// G20 Meeting
                /// </summary>
                public const string GTwentyMeeting = "g20 meeting";
                /// <summary>
                /// G20 Summit Meeting
                /// </summary>
                public const string GTwentySummitMeeting = "g20 summit meeting";
                /// <summary>
                /// Health-Sports Day
                /// </summary>
                public const string HealthSportsDay = "health sports day";
                /// <summary>
                /// Holiday
                /// </summary>
                public const string Holiday = "holiday";
                /// <summary>
                /// Household Spending MoM
                /// </summary>
                public const string HouseholdSpendingMoM = "household spending mom";
                /// <summary>
                /// Household Spending YoY
                /// </summary>
                public const string HouseholdSpendingYoY = "household spending yoy";
                /// <summary>
                /// House of Councillors Election
                /// </summary>
                public const string HouseOfCouncillorsElection = "house of councillors election";
                /// <summary>
                /// Housing Starts YoY
                /// </summary>
                public const string HousingStartsYoY = "housing starts yoy";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "imports";
                /// <summary>
                /// Imports YoY
                /// </summary>
                public const string ImportsYoY = "imports yoy";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production MoM Final
                /// </summary>
                public const string IndustrialProductionMoMFinal = "industrial production mom final";
                /// <summary>
                /// Industrial Production MoM Prel.
                /// </summary>
                public const string IndustrialProductionMoMPreliminary = "industrial production mom preliminary";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Industrial Production YoY Final
                /// </summary>
                public const string IndustrialProductionYoYFinal = "industrial production yoy final";
                /// <summary>
                /// Industrial Production YoY Prel
                /// </summary>
                public const string IndustrialProductionYoYPreliminary = "industrial production yoy preliminary";
                /// <summary>
                /// Inflation Rate Ex-Food and Energy YoY
                /// </summary>
                public const string InflationRateExcludingFoodAndEnergyYoY = "inflation rate excluding food and energy yoy";
                /// <summary>
                /// Inflation Rate Ex-Fresh Food YoY
                /// </summary>
                public const string InflationRateExcludingFreshFoodYoY = "inflation rate excluding fresh food yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Jibun Bank Composite PMI Final
                /// </summary>
                public const string JibunBankCompositePurchasingManagersIndexFinal = "jibun bank composite purchasing managers index final";
                /// <summary>
                /// Jibun Bank Composite PMI Flash
                /// </summary>
                public const string JibunBankCompositePurchasingManagersIndexFlash = "jibun bank composite purchasing managers index flash";
                /// <summary>
                /// Jibun Bank Manufacturing PMI Final
                /// </summary>
                public const string JibunBankManufacturingPurchasingManagersIndexFinal = "jibun bank manufacturing purchasing managers index final";
                /// <summary>
                /// Jibun Bank Manufacturing PMI Flash
                /// </summary>
                public const string JibunBankManufacturingPurchasingManagersIndexFlash = "jibun bank manufacturing purchasing managers index flash";
                /// <summary>
                /// Jibun Bank Services PMI
                /// </summary>
                public const string JibunBankServicesPurchasingManagersIndex = "jibun bank services purchasing managers index";
                /// <summary>
                /// Jibun Bank Services PMI Final
                /// </summary>
                public const string JibunBankServicesPurchasingManagersIndexFinal = "jibun bank services purchasing managers index final";
                /// <summary>
                /// Jibun Bank Services PMI Flash
                /// </summary>
                public const string JibunBankServicesPurchasingManagersIndexFlash = "jibun bank services purchasing managers index flash";
                /// <summary>
                /// Jobs/applications ratio
                /// </summary>
                public const string JobsApplicationsRatio = "jobs applications ratio";
                /// <summary>
                /// JP Foreign Reserves
                /// </summary>
                public const string JpForeignReserves = "jp foreign reserves";
                /// <summary>
                /// Labour Thanksgiving Day
                /// </summary>
                public const string LaborThanksgivingDay = "labor thanksgiving day";
                /// <summary>
                /// Large Retailer Sales
                /// </summary>
                public const string LargeRetailerSales = "large retailer sales";
                /// <summary>
                /// Large Retailer's Sales
                /// </summary>
                public const string LargeRetailersSales = "large retailers sales";
                /// <summary>
                /// Leading Composite Index Final
                /// </summary>
                public const string LeadingCompositeIndexFinal = "leading composite index final";
                /// <summary>
                /// Leading Composite Index Prel
                /// </summary>
                public const string LeadingCompositeIndexPreliminary = "leading composite index preliminary";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "leading economic index";
                /// <summary>
                /// Leading Economic Index Final
                /// </summary>
                public const string LeadingEconomicIndexFinal = "leading economic index final";
                /// <summary>
                /// Leading Economic Index Prel
                /// </summary>
                public const string LeadingEconomicIndexPreliminary = "leading economic index preliminary";
                /// <summary>
                /// Machinery Orders (MoM)
                /// </summary>
                public const string MachineryOrdersMoM = "machinery orders mom";
                /// <summary>
                /// Machinery Orders (YoY)
                /// </summary>
                public const string MachineryOrdersYoY = "machinery orders yoy";
                /// <summary>
                /// Machine Tool Orders YoY
                /// </summary>
                public const string MachineToolOrdersYoY = "machine tool orders yoy";
                /// <summary>
                /// Manufacturing PMI
                /// </summary>
                public const string ManufacturingPurchasingManagersIndex = "manufacturing purchasing managers index";
                /// <summary>
                /// Markit/JMMA Manufacturing PMI
                /// </summary>
                public const string MarkitJmmaManufacturingPurchasingManagersIndex = "markit jmma manufacturing purchasing managers index";
                /// <summary>
                /// Markit/JMMA Manufacturing PMI Final
                /// </summary>
                public const string MarkitJmmaManufacturingPurchasingManagersIndexFinal = "markit jmma manufacturing purchasing managers index final";
                /// <summary>
                /// Markit/JMMA Manufacturing PMI Flash
                /// </summary>
                public const string MarkitJmmaManufacturingPurchasingManagersIndexFlash = "markit jmma manufacturing purchasing managers index flash";
                /// <summary>
                /// Nikkei Markit Services PMI
                /// </summary>
                public const string MarkitNikkeiServicesPurchasingManagersIndex = "markit nikkei services purchasing managers index";
                /// <summary>
                /// Markit Services PMI
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "markit services purchasing managers index";
                /// <summary>
                /// MOF Asakawa Speech
                /// </summary>
                public const string MofAsakawaSpeech = "mof asakawa speech";
                /// <summary>
                /// Monetary Base (YoY)
                /// </summary>
                public const string MonetaryBaseYoY = "monetary base yoy";
                /// <summary>
                /// Money Supply M2+CD (YoY)
                /// </summary>
                public const string MoneySupplyMTwocdYoY = "money supply m2cd yoy";
                /// <summary>
                /// Mountain Day
                /// </summary>
                public const string MountainDay = "mountain day";
                /// <summary>
                /// National Core Inflation Rate YoY
                /// </summary>
                public const string NationalCoreInflationRateYoY = "national core inflation rate yoy";
                /// <summary>
                /// National Founding Day
                /// </summary>
                public const string NationalFoundingDay = "national founding day";
                /// <summary>
                /// National Holiday
                /// </summary>
                public const string NationalHoliday = "national holiday";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Years's Day
                /// </summary>
                public const string NewYearssDay = "new yearss day";
                /// <summary>
                /// Nikkei Manufacturing PMI Final
                /// </summary>
                public const string NikkeiManufacturingPurchasingManagersIndexFinal = "nikkei manufacturing purchasing managers index final";
                /// <summary>
                /// Nikkei Manufacturing PMI Flash
                /// </summary>
                public const string NikkeiManufacturingPurchasingManagersIndexFlash = "nikkei manufacturing purchasing managers index flash";
                /// <summary>
                /// Nikkei Services PMI
                /// </summary>
                public const string NikkeiServicesPurchasingManagersIndex = "nikkei services purchasing managers index";
                /// <summary>
                /// Nomura/JMMA Manufacturing PMI
                /// </summary>
                public const string NomuraJmmaManufacturingPurchasingManagersIndex = "nomura jmma manufacturing purchasing managers index";
                /// <summary>
                /// Ocean Day
                /// </summary>
                public const string OceanDay = "ocean day";
                /// <summary>
                /// OECD Head Angel Gurria Speech
                /// </summary>
                public const string OecdHeadAngelGurriaSpeech = "oecd head angel gurria speech";
                /// <summary>
                /// Overall Household Spending YoY
                /// </summary>
                public const string OverallHouseholdSpendingYoY = "overall household spending yoy";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// PM Shinzo Abe to Dissolve Parliament
                /// </summary>
                public const string PmShinzoAbeToDissolveParliament = "pm shinzo abe to dissolve parliament";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Respect for the Aged Day
                /// </summary>
                public const string RespectForTheAgedDay = "respect for the aged day";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Retail Trade s.a (MoM)
                /// </summary>
                public const string RetailTradeSeasonallyAdjustedMoM = "retail trade seasonally adjusted mom";
                /// <summary>
                /// Retail Trade (YoY)
                /// </summary>
                public const string RetailTradeYoY = "retail trade yoy";
                /// <summary>
                /// Reuters Tankan Index
                /// </summary>
                public const string ReutersTankanIndex = "reuters tankan index";
                /// <summary>
                /// Showa Day
                /// </summary>
                public const string ShowaDay = "showa day";
                /// <summary>
                /// Spring Equinox Day
                /// </summary>
                public const string SpringEquinoxDay = "spring equinox day";
                /// <summary>
                /// Stock Investment by Foreigners
                /// </summary>
                public const string StockInvestmentByForeigners = "stock investment by foreigners";
                /// <summary>
                /// Tankan Large All Industry Capex
                /// </summary>
                public const string TankanAllLargeIndustryCapitalExpenditure = "tankan all large industry capital expenditure";
                /// <summary>
                /// Tankan All Small Industry CAPEX
                /// </summary>
                public const string TankanAllSmallIndustryCapitalExpenditure = "tankan all small industry capital expenditure";
                /// <summary>
                /// Tankan Large Manufacturers Index
                /// </summary>
                public const string TankanLargeManufacturingIndex = "tankan large manufacturing index";
                /// <summary>
                /// Tankan Large Manufacturing Outlook
                /// </summary>
                public const string TankanLargeManufacturingOutlook = "tankan large manufacturing outlook";
                /// <summary>
                /// Tankan Large Non-Manufacturing Index
                /// </summary>
                public const string TankanLargeNonManufacturingIndex = "tankan large non manufacturing index";
                /// <summary>
                /// Tankan Non-Manufacturing Index
                /// </summary>
                public const string TankanNonManufacturingIndex = "tankan non manufacturing index";
                /// <summary>
                /// Tankan Non-Manufacturing Outlook
                /// </summary>
                public const string TankanNonManufacturingOutlook = "tankan non manufacturing outlook";
                /// <summary>
                /// Tankan Small Manufacturers Index
                /// </summary>
                public const string TankanSmallManufacturingIndex = "tankan small manufacturing index";
                /// <summary>
                /// Tankan Small Manufacturing Outlook
                /// </summary>
                public const string TankanSmallManufacturingOutlook = "tankan small manufacturing outlook";
                /// <summary>
                /// Tankan Small Non-Manufacturing Index
                /// </summary>
                public const string TankanSmallNonManufacturingIndex = "tankan small non manufacturing index";
                /// <summary>
                /// Tankan Small Non-Manufacturing Outlook
                /// </summary>
                public const string TankanSmallNonManufacturingOutlook = "tankan small non manufacturing outlook";
                /// <summary>
                /// 10-Year JGB Auction
                /// </summary>
                public const string TenYearJgbAuction = "10 year jgb auction";
                /// <summary>
                /// Tertiary Industry Index MoM
                /// </summary>
                public const string TertiaryIndustryIndexMoM = "tertiary industry index mom";
                /// <summary>
                /// The Emperor's Birthday
                /// </summary>
                public const string TheEmperorsBirthday = "the emperors birthday";
                /// <summary>
                /// 30-Year JGB Auction
                /// </summary>
                public const string ThirtyYearJgbAuction = "30 year jgb auction";
                /// <summary>
                /// Tokyo CPI YoY
                /// </summary>
                public const string TokyoConsumerPriceIndexYoY = "tokyo consumer price index yoy";
                /// <summary>
                /// Tokyo Core CPI YoY
                /// </summary>
                public const string TokyoCoreConsumerPriceIndexYoY = "tokyo core consumer price index yoy";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Upper House Elections
                /// </summary>
                public const string UpperHouseElections = "upper house elections";
                /// <summary>
                /// US-Japan Trade Talks
                /// </summary>
                public const string UsJapanTradeTalks = "us japan trade talks";
                /// <summary>
                /// Vehicle Sales (YoY)
                /// </summary>
                public const string VehicleSalesYoY = "vehicle sales yoy";
                /// <summary>
                /// Yutaka Harada Speech
                /// </summary>
                public const string YutakaHaradaSpeech = "yutaka harada speech";
            }
            /// <summary>
            /// Latvia
            /// </summary>
            public static class Latvia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Bank Holiday
                /// </summary>
                public const string BankHoliday = "bank holiday";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Eve
                /// </summary>
                public const string ChristmasEve = "christmas eve";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Declaration of Independence
                /// </summary>
                public const string DeclarationOfIndependence = "declaration of independence";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Midsummer Day
                /// </summary>
                public const string MidsummerDay = "midsummer day";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year's Eve
                /// </summary>
                public const string NewYearsEve = "new years eve";
                /// <summary>
                /// Proclemation of The Republic
                /// </summary>
                public const string ProclemationOfTheRepublic = "proclemation of the republic";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// St John's Day
                /// </summary>
                public const string StJohnsDay = "st johns day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
            }
            /// <summary>
            /// Lithuania
            /// </summary>
            public static class Lithuania
            {
                /// <summary>
                /// All Saint's Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Eve
                /// </summary>
                public const string ChristmasEve = "christmas eve";
                /// <summary>
                /// Christmas Holiday
                /// </summary>
                public const string ChristmasHoliday = "christmas holiday";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Father’s Day
                /// </summary>
                public const string FathersDay = "fathers day";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ - F.
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY - F.
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "independence day";
                /// <summary>
                /// Independence Restoration Day
                /// </summary>
                public const string IndependenceRestorationDay = "independence restoration day";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "industrial production";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// King Mindaugas’ Coronation Day
                /// </summary>
                public const string KingMindaugasCoronationDay = "king mindaugas coronation day";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Mother’s Day
                /// </summary>
                public const string MothersDay = "mothers day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Parliamentary Elections 2nd Round
                /// </summary>
                public const string ParliamentaryElectionsSecondRound = "parliamentary elections second round";
                /// <summary>
                /// Presidential Election
                /// </summary>
                public const string PresidentialElection = "presidential election";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Restoration of Independence Day
                /// </summary>
                public const string RestorationOfIndependenceDay = "restoration of independence day";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// 2nd Day of Christmas
                /// </summary>
                public const string SecondDayOfChristmas = "second day of christmas";
                /// <summary>
                /// St John's Day
                /// </summary>
                public const string StJohnsDay = "st johns day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
            }
            /// <summary>
            /// Luxembourg
            /// </summary>
            public static class Luxembourg
            {
                /// <summary>
                /// All Saint's Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Eve
                /// </summary>
                public const string ChristmasEve = "christmas eve";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Europe Day
                /// </summary>
                public const string EuropeDay = "europe day";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// St Stephen's Day
                /// </summary>
                public const string StStephensDay = "st stephens day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Whit Monday
                /// </summary>
                public const string WhitMonday = "whit monday";
            }
            /// <summary>
            /// Malta
            /// </summary>
            public static class Malta
            {
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Feast of our Lady of Victories
                /// </summary>
                public const string FeastOfOurLadyOfVictories = "feast of our lady of victories";
                /// <summary>
                /// Feast of St Joseph
                /// </summary>
                public const string FeastOfStJoseph = "feast of st joseph";
                /// <summary>
                /// Feast of St Paul's Shipwreck
                /// </summary>
                public const string FeastOfStPaulsShipwreck = "feast of st pauls shipwreck";
                /// <summary>
                /// Feast of St Peter and St Paul
                /// </summary>
                public const string FeastOfStPeterAndStPaul = "feast of st peter and st paul";
                /// <summary>
                /// Freedom Day
                /// </summary>
                public const string FreedomDay = "freedom day";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Immaculate Conception
                /// </summary>
                public const string ImmaculateConception = "immaculate conception";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "independence day";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "inflation rate";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Republic Day
                /// </summary>
                public const string RepublicDay = "republic day";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Sette Giugno
                /// </summary>
                public const string SetteGiugno = "sette giugno";
                /// <summary>
                /// St Joseph's Day
                /// </summary>
                public const string StJosephsDay = "st josephs day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
            }
            /// <summary>
            /// Netherlands
            /// </summary>
            public static class Netherlands
            {
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Consumer Spending Volume
                /// </summary>
                public const string ConsumerSpendingVolume = "consumer spending volume";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Household Consumption YoY
                /// </summary>
                public const string HouseholdConsumptionYoY = "household consumption yoy";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// King's Day
                /// </summary>
                public const string KingsDay = "kings day";
                /// <summary>
                /// Liberation Day
                /// </summary>
                public const string LiberationDay = "liberation day";
                /// <summary>
                /// Local Elections
                /// </summary>
                public const string LocalElections = "local elections";
                /// <summary>
                /// Manufacturing Confidence
                /// </summary>
                public const string ManufacturingConfidence = "manufacturing confidence";
                /// <summary>
                /// Manufacturing Prod YoY
                /// </summary>
                public const string ManufacturingProductionYoY = "manufacturing production yoy";
                /// <summary>
                /// NEVI Manufacturing PMI
                /// </summary>
                public const string NeviManufacturingPurchasingManagersIndex = "nevi manufacturing purchasing managers index";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Pentecost
                /// </summary>
                public const string Pentecost = "pentecost";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// 6-Month Bill Auction
                /// </summary>
                public const string SixMonthBillAuction = "6 month bill auction";
                /// <summary>
                /// 10-Year Bond Auction
                /// </summary>
                public const string TenYearBondAuction = "10 year bond auction";
                /// <summary>
                /// 3-Month Bill Auction
                /// </summary>
                public const string ThreeMonthBillAuction = "3 month bill auction";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Whit Monday
                /// </summary>
                public const string WhitMonday = "whit monday";
            }
            /// <summary>
            /// New Zealand
            /// </summary>
            public static class NewZealand
            {
                /// <summary>
                /// ANZAC Day
                /// </summary>
                public const string AnzacDay = "anzac day";
                /// <summary>
                /// ANZ Business Confidence
                /// </summary>
                public const string AnzBusinessConfidence = "anz business confidence";
                /// <summary>
                /// ANZ Commodity Price
                /// </summary>
                public const string AnzCommodityPrice = "anz commodity price";
                /// <summary>
                /// ANZ Roy Morgan Consumer Confidence
                /// </summary>
                public const string AnzRoyMorganConsumerConfidence = "anz roy morgan consumer confidence";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Building Permits MoM
                /// </summary>
                public const string BuildingPermitsMoM = "building permits mom";
                /// <summary>
                /// Building Permits s.a. (MoM)
                /// </summary>
                public const string BuildingPermitsSeasonallyAdjustedMoM = "building permits seasonally adjusted mom";
                /// <summary>
                /// Business Inflation Expectations
                /// </summary>
                public const string BusinessInflationExpectations = "business inflation expectations";
                /// <summary>
                /// Business NZ PMI
                /// </summary>
                public const string BusinessNzPurchasingManagersIndex = "business nz purchasing managers index";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Day After New Year's Day
                /// </summary>
                public const string DayAfterNewYearsDay = "day after new years day";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Electronic Card Retail Sales  (MoM)
                /// </summary>
                public const string ElectronicCardRetailSalesMoM = "electronic card retail sales mom";
                /// <summary>
                /// Electronic Card Retail Sales (YoY)
                /// </summary>
                public const string ElectronicCardRetailSalesYoY = "electronic card retail sales yoy";
                /// <summary>
                /// Electronic Retail Card Spending MoM
                /// </summary>
                public const string ElectronicRetailCardSpendingMoM = "electronic retail card spending mom";
                /// <summary>
                /// Electronic Retail Card Spending YoY
                /// </summary>
                public const string ElectronicRetailCardSpendingYoY = "electronic retail card spending yoy";
                /// <summary>
                /// Employment Change QoQ
                /// </summary>
                public const string EmploymentChangeQoQ = "employment change qoq";
                /// <summary>
                /// Export Prices QoQ
                /// </summary>
                public const string ExportPricesQoQ = "export prices qoq";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "exports";
                /// <summary>
                /// Financial Stability Report
                /// </summary>
                public const string FinancialStabilityReport = "financial stability report";
                /// <summary>
                /// 1st Day Flag Referendum
                /// </summary>
                public const string FirstDayFlagReferendum = "first day flag referendum";
                /// <summary>
                /// Food Inflation YoY
                /// </summary>
                public const string FoodInflationYoY = "food inflation yoy";
                /// <summary>
                /// Food Price Index (MoM)
                /// </summary>
                public const string FoodPriceIndexMoM = "food price index mom";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// General Elections
                /// </summary>
                public const string GeneralElections = "general elections";
                /// <summary>
                /// Global Dairy Trade Price Index
                /// </summary>
                public const string GlobalDairyTradePriceIndex = "global dairy trade price index";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Import Prices QoQ
                /// </summary>
                public const string ImportPricesQoQ = "import prices qoq";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "imports";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate QoQ
                /// </summary>
                public const string InflationRateQoQ = "inflation rate qoq";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "interest rate decision";
                /// <summary>
                /// Labour cost index QoQ
                /// </summary>
                public const string LaborCostsIndexQoQ = "labor costs index qoq";
                /// <summary>
                /// Labour cost index YoY
                /// </summary>
                public const string LaborCostsIndexYoY = "labor costs index yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Last Day Flag Referendum
                /// </summary>
                public const string LastDayFlagReferendum = "last day flag referendum";
                /// <summary>
                /// Manufacturing Production YoY
                /// </summary>
                public const string ManufacturingProductionYoY = "manufacturing production yoy";
                /// <summary>
                /// Manufacturing sales
                /// </summary>
                public const string ManufacturingSales = "manufacturing sales";
                /// <summary>
                /// Manufacturing Sales YoY
                /// </summary>
                public const string ManufacturingSalesYoY = "manufacturing sales yoy";
                /// <summary>
                /// Business NZ/Markit PMI
                /// </summary>
                public const string MarkitBusinessNzPurchasingManagersIndex = "markit business nz purchasing managers index";
                /// <summary>
                /// Monetary Policy Statement
                /// </summary>
                public const string MonetaryPolicyStatement = "monetary policy statement";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// NZIER Business Confidence
                /// </summary>
                public const string NzierBusinessConfidence = "nzier business confidence";
                /// <summary>
                /// NZIER Business Confidence QoQ
                /// </summary>
                public const string NzierBusinessConfidenceQoQ = "nzier business confidence qoq";
                /// <summary>
                /// NZIER Capacity Utilization
                /// </summary>
                public const string NzierCapacityUtilization = "nzier capacity utilization";
                /// <summary>
                /// NZIER QSBO Capacity Utilization
                /// </summary>
                public const string NzierQsboCapacityUtilization = "nzier qsbo capacity utilization";
                /// <summary>
                /// Participation Rate
                /// </summary>
                public const string ParticipationRate = "participation rate";
                /// <summary>
                /// PPI Input QoQ
                /// </summary>
                public const string ProducerPriceIndexInputQoQ = "producer price index input qoq";
                /// <summary>
                /// PPI Output QoQ
                /// </summary>
                public const string ProducerPriceIndexOutputQoQ = "producer price index output qoq";
                /// <summary>
                /// Queen's Birthday Day
                /// </summary>
                public const string QueensBirthdayDay = "queens birthday day";
                /// <summary>
                /// RBNZ Economic Assesment
                /// </summary>
                public const string RbnzEconomicAssesment = "rbnz economic assesment";
                /// <summary>
                /// RBNZ Financial Stability Report
                /// </summary>
                public const string RbnzFinancialStabilityReport = "rbnz financial stability report";
                /// <summary>
                /// RBNZ Gov Orr Speech
                /// </summary>
                public const string RbnzGovOrrSpeech = "rbnz gov orr speech";
                /// <summary>
                /// RBNZ Gov Wheeler Speech
                /// </summary>
                public const string RbnzGovWheelerSpeech = "rbnz gov wheeler speech";
                /// <summary>
                /// RBNZ Inflation Expectations (YoY)
                /// </summary>
                public const string RbnzInflationExpectationsYoY = "rbnz inflation expectations yoy";
                /// <summary>
                /// RBNZ McDermott Speech
                /// </summary>
                public const string RbnzMcdermottSpeech = "rbnz mcdermott speech";
                /// <summary>
                /// RBNZ Press Conference
                /// </summary>
                public const string RbnzPressConference = "rbnz press conference";
                /// <summary>
                /// RBNZ Wheeler Speech
                /// </summary>
                public const string RbnzWheelerSpeech = "rbnz wheeler speech";
                /// <summary>
                /// REINZ House Price Index MoM
                /// </summary>
                public const string ReinzHousePriceIndexMoM = "reinz house price index mom";
                /// <summary>
                /// Retail Sales QoQ
                /// </summary>
                public const string RetailSalesQoQ = "retail sales qoq";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Services NZ PSI
                /// </summary>
                public const string ServicesNzPerformanceOfServicesIndex = "services nz performance of services index";
                /// <summary>
                /// Terms of Trade QoQ
                /// </summary>
                public const string TermsOfTradeQoQ = "terms of trade qoq";
                /// <summary>
                /// Trade Balance (MoM)
                /// </summary>
                public const string TradeBalanceMoM = "trade balance mom";
                /// <summary>
                /// Trade Balance (YoY)
                /// </summary>
                public const string TradeBalanceYoY = "trade balance yoy";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Visitor Arrivals MoM
                /// </summary>
                public const string VisitorArrivalsMoM = "visitor arrivals mom";
                /// <summary>
                /// Visitor Arrivals (YoY)
                /// </summary>
                public const string VisitorArrivalsYoY = "visitor arrivals yoy";
                /// <summary>
                /// Waitangi Day
                /// </summary>
                public const string WaitangiDay = "waitangi day";
                /// <summary>
                /// Consumer Confidence WESTPAC
                /// </summary>
                public const string WestpacConsumerConfidence = "westpac consumer confidence";
                /// <summary>
                /// Westpac consumer survey
                /// </summary>
                public const string WestpacConsumerSurvey = "westpac consumer survey";
            }
            /// <summary>
            /// Portugal
            /// </summary>
            public static class Portugal
            {
                /// <summary>
                /// All Saints' Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// BdP Gov Costa Speech
                /// </summary>
                public const string BdpGovCostaSpeech = "bdp gov costa speech";
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "budget balance";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Corpus Christi
                /// </summary>
                public const string CorpusChristi = "corpus christi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// ECB Forum on Central Banking
                /// </summary>
                public const string EcbForumOnCentralBanking = "ecb forum on central banking";
                /// <summary>
                /// Economic Activity YoY
                /// </summary>
                public const string EconomicActivityYoY = "economic activity yoy";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "exports";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Immaculate Conception
                /// </summary>
                public const string ImmaculateConception = "immaculate conception";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "industrial production";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate MoM Final
                /// </summary>
                public const string InflationRateMoMFinal = "inflation rate mom final";
                /// <summary>
                /// Inflation Rate MoM Prel
                /// </summary>
                public const string InflationRateMoMPreliminary = "inflation rate mom preliminary";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Inflation Rate YoY Final
                /// </summary>
                public const string InflationRateYoYFinal = "inflation rate yoy final";
                /// <summary>
                /// Inflation Rate YoY Prel
                /// </summary>
                public const string InflationRateYoYPreliminary = "inflation rate yoy preliminary";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Liberty Day
                /// </summary>
                public const string LibertyDay = "liberty day";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Private Consumption YoY
                /// </summary>
                public const string PrivateConsumptionYoY = "private consumption yoy";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Republic Implantation
                /// </summary>
                public const string RepublicImplantation = "republic implantation";
                /// <summary>
                /// Restoration of Independence
                /// </summary>
                public const string RestorationOfIndependence = "restoration of independence";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
            }
            /// <summary>
            /// Slovakia
            /// </summary>
            public static class Slovakia
            {
                /// <summary>
                /// All Saint's Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Eve
                /// </summary>
                public const string ChristmasEve = "christmas eve";
                /// <summary>
                /// Constitution Day
                /// </summary>
                public const string ConstitutionDay = "constitution day";
                /// <summary>
                /// Construction Output YoY
                /// </summary>
                public const string ConstructionOutputYoY = "construction output yoy";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Core Inflation Rate MoM
                /// </summary>
                public const string CoreInflationRateMoM = "core inflation rate mom";
                /// <summary>
                /// Core Inflation Rate YoY
                /// </summary>
                public const string CoreInflationRateYoY = "core inflation rate yoy";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Day of Our Lady of Sorrows
                /// </summary>
                public const string DayOfOurLadyOfSorrows = "day of our lady of sorrows";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// End of World War II
                /// </summary>
                public const string EndOfWorldWarIi = "end of world war ii";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// Epiphany Day
                /// </summary>
                public const string EpiphanyDay = "epiphany day";
                /// <summary>
                /// Establishment of the Slovak Republic
                /// </summary>
                public const string EstablishmentOfTheSlovakRepublic = "establishment of the slovak republic";
                /// <summary>
                /// European Council Meeting
                /// </summary>
                public const string EuropeanCouncilMeeting = "european council meeting";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "exports";
                /// <summary>
                /// Finance Ministry Economic Forecasts
                /// </summary>
                public const string FinanceMinistryEconomicForecasts = "finance ministry economic forecasts";
                /// <summary>
                /// GDP Annual Growth Rate YoY
                /// </summary>
                public const string GdpAnnualGrowthRateYoY = "gdp annual growth rate yoy";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Final
                /// </summary>
                public const string GdpGrowthRateYoYSecondFinal = "gdp growth rate yoy second final";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Harmonised Inflation Rate MoM
                /// </summary>
                public const string HarmonizedInflationRateMoM = "harmonized inflation rate mom";
                /// <summary>
                /// Harmonised Inflation Rate YoY
                /// </summary>
                public const string HarmonizedInflationRateYoY = "harmonized inflation rate yoy";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "imports";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Informal Ecofin Meeting
                /// </summary>
                public const string InformalEcofinMeeting = "informal ecofin meeting";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// National Uprising Day
                /// </summary>
                public const string NationalUprisingDay = "national uprising day";
                /// <summary>
                /// NBS Gov Makuch Speech
                /// </summary>
                public const string NbsGovMakuchSpeech = "nbs gov makuch speech";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Presidential Election
                /// </summary>
                public const string PresidentialElection = "presidential election";
                /// <summary>
                /// Real Wages YoY
                /// </summary>
                public const string RealWagesYoY = "real wages yoy";
                /// <summary>
                /// Republic Day
                /// </summary>
                public const string RepublicDay = "republic day";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// St Cyril and Methodius Day
                /// </summary>
                public const string StCyrilAndMethodiusDay = "st cyril and methodius day";
                /// <summary>
                /// Struggle for Freedom and Democracy Day
                /// </summary>
                public const string StruggleForFreedomAndDemocracyDay = "struggle for freedom and democracy day";
                /// <summary>
                /// St Stephen's Day
                /// </summary>
                public const string StStephensDay = "st stephens day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
            }
            /// <summary>
            /// Slovenia
            /// </summary>
            public static class Slovenia
            {
                /// <summary>
                /// All Saint's Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// BoS Festic Speech
                /// </summary>
                public const string BosFesticSpeech = "bos festic speech";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Day of Uprising Against Occupation
                /// </summary>
                public const string DayOfUprisingAgainstOccupation = "day of uprising against occupation";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Finance Minister Mramor Speech
                /// </summary>
                public const string FinanceMinisterMramorSpeech = "finance minister mramor speech";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// Harmonised Inflation Rate YoY
                /// </summary>
                public const string HarmonizedInflationRateYoY = "harmonized inflation rate yoy";
                /// <summary>
                /// Independence and Unit Day
                /// </summary>
                public const string IndependenceAndUnitDay = "independence and unit day";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Labour Day (Substitute Day)
                /// </summary>
                public const string LaborDaySubstituteDay = "labor day substitute day";
                /// <summary>
                /// May Day
                /// </summary>
                public const string MayDay = "may day";
                /// <summary>
                /// May Day Holiday
                /// </summary>
                public const string MayDayHoliday = "may day holiday";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year's Holiday
                /// </summary>
                public const string NewYearsHoliday = "new years holiday";
                /// <summary>
                /// Pentecost
                /// </summary>
                public const string Pentecost = "pentecost";
                /// <summary>
                /// Prešeren Day
                /// </summary>
                public const string PreerenDay = "preeren day";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Reformation Day
                /// </summary>
                public const string ReformationDay = "reformation day";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Rethinking Monetary-Fiscal Policy Coordination Seminar
                /// </summary>
                public const string RethinkingMonetaryFiscalPolicyCoordinationSeminar = "rethinking monetary fiscal policy coordination seminar";
                /// <summary>
                /// Tourist Arrivals YoY
                /// </summary>
                public const string TouristArrivalsYoY = "tourist arrivals yoy";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Uprising Against the Occupation Day
                /// </summary>
                public const string UprisingAgainstTheOccupationDay = "uprising against the occupation day";
            }
            /// <summary>
            /// Spain
            /// </summary>
            public static class Spain
            {
                /// <summary>
                /// All Saints' Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Assumption of Mary
                /// </summary>
                public const string AssumptionOfMary = "assumption of mary";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// BdE Gov Linde Speech
                /// </summary>
                public const string BdeGovLindeSpeech = "bde gov linde speech";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Catalonian Parlimentary Elections
                /// </summary>
                public const string CatalonianParlimentaryElections = "catalonian parlimentary elections";
                /// <summary>
                /// Catalonia Parliamentary Election
                /// </summary>
                public const string CataloniaParliamentaryElection = "catalonia parliamentary election";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Constitution Day
                /// </summary>
                public const string ConstitutionDay = "constitution day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// Epiphany Holiday
                /// </summary>
                public const string EpiphanyHoliday = "epiphany holiday";
                /// <summary>
                /// Finance Minister Guindos Speech
                /// </summary>
                public const string FinanceMinisterGuindosSpeech = "finance minister guindos speech";
                /// <summary>
                /// 5-Year Bonos Auction
                /// </summary>
                public const string FiveYearBonosAuction = "5 year bonos auction";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// General Election
                /// </summary>
                public const string GeneralElection = "general election";
                /// <summary>
                /// General Elections
                /// </summary>
                public const string GeneralElections = "general elections";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Harmonised Inflation Rate MoM Final
                /// </summary>
                public const string HarmonizedInflationRateMoMFinal = "harmonized inflation rate mom final";
                /// <summary>
                /// Harmonised Inflation Rate MoM Prel
                /// </summary>
                public const string HarmonizedInflationRateMoMPreliminary = "harmonized inflation rate mom preliminary";
                /// <summary>
                /// Harmonised Inflation Rate YoY Final
                /// </summary>
                public const string HarmonizedInflationRateYoYFinal = "harmonized inflation rate yoy final";
                /// <summary>
                /// Harmonised Inflation Rate YoY Prel
                /// </summary>
                public const string HarmonizedInflationRateYoYPreliminary = "harmonized inflation rate yoy preliminary";
                /// <summary>
                /// Hispanic Day
                /// </summary>
                public const string HispanicDay = "hispanic day";
                /// <summary>
                /// IIF Spring Membership Meeting 2016
                /// </summary>
                public const string IifSpringMembershipMeetingTwoThousandSixteen = "iif spring membership meeting 2016";
                /// <summary>
                /// Immaculate Conception
                /// </summary>
                public const string ImmaculateConception = "immaculate conception";
                /// <summary>
                /// Industrial Orders YoY
                /// </summary>
                public const string IndustrialOrdersYoY = "industrial orders yoy";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate MoM Final
                /// </summary>
                public const string InflationRateMoMFinal = "inflation rate mom final";
                /// <summary>
                /// Inflation Rate MoM Prel
                /// </summary>
                public const string InflationRateMoMPreliminary = "inflation rate mom preliminary";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Inflation Rate YoY Final
                /// </summary>
                public const string InflationRateYoYFinal = "inflation rate yoy final";
                /// <summary>
                /// Inflation Rate YoY Prel
                /// </summary>
                public const string InflationRateYoYPreliminary = "inflation rate yoy preliminary";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Labour Day (Substitute Day)
                /// </summary>
                public const string LaborDaySubstituteDay = "labor day substitute day";
                /// <summary>
                /// Markit Manufacturing PMI
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services PMI
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "markit services purchasing managers index";
                /// <summary>
                /// Maundy Thursday
                /// </summary>
                public const string MaundyThursday = "maundy thursday";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Car Sales YoY
                /// </summary>
                public const string NewCarSalesYoY = "new car sales yoy";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year's Day (Substitute Day)
                /// </summary>
                public const string NewYearsDaySubstituteDay = "new years day substitute day";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Parliamentary Vote on 2019 Budget
                /// </summary>
                public const string ParliamentaryVoteOnTwoThousandNineteenBudget = "parliamentary vote on 2019 budget";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Services PMI
                /// </summary>
                public const string ServicesPurchasingManagersIndex = "services purchasing managers index";
                /// <summary>
                /// 6-Month Letras Auction
                /// </summary>
                public const string SixMonthLetrasAuction = "6 month letras auction";
                /// <summary>
                /// St. Joseph's Day
                /// </summary>
                public const string StJosephsDay = "st josephs day";
                /// <summary>
                /// 10-Year Obligacion Auction
                /// </summary>
                public const string TenYearObligacionAuction = "10 year obligacion auction";
                /// <summary>
                /// 3-Month Letras Auction
                /// </summary>
                public const string ThreeMonthLetrasAuction = "3 month letras auction";
                /// <summary>
                /// 3-Year Bonos Auction
                /// </summary>
                public const string ThreeYearBonosAuction = "3 year bonos auction";
                /// <summary>
                /// Tourist Arrivals YoY
                /// </summary>
                public const string TouristArrivalsYoY = "tourist arrivals yoy";
                /// <summary>
                /// 12-Month Letras Auction
                /// </summary>
                public const string TwelveMonthLetrasAuction = "12 month letras auction";
                /// <summary>
                /// Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "unemployment change";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
            }
            /// <summary>
            /// Sweden
            /// </summary>
            public static class Sweden
            {
                /// <summary>
                /// All Saint's Day
                /// </summary>
                public const string AllSaintsDay = "all saints day";
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Capacity Utilization QoQ
                /// </summary>
                public const string CapacityUtilizationQoQ = "capacity utilization qoq";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Eve
                /// </summary>
                public const string ChristmasEve = "christmas eve";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Consumer Inflation Expectations
                /// </summary>
                public const string ConsumerInflationExpectations = "consumer inflation expectations";
                /// <summary>
                /// CPIF MoM
                /// </summary>
                public const string ConsumerPriceIndexFixedInterestRateMoM = "consumer price index fixed interest rate mom";
                /// <summary>
                /// CPIF YoY
                /// </summary>
                public const string ConsumerPriceIndexFixedInterestRateYoY = "consumer price index fixed interest rate yoy";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Eve
                /// </summary>
                public const string EasterEve = "easter eve";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Easter Sunday
                /// </summary>
                public const string EasterSunday = "easter sunday";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "epiphany";
                /// <summary>
                /// Epiphany Day
                /// </summary>
                public const string EpiphanyDay = "epiphany day";
                /// <summary>
                /// Financial Stability Report 2018
                /// </summary>
                public const string FinancialStabilityReportTwoThousandEighteen = "financial stability report 2018";
                /// <summary>
                /// Financial Stability Report 2017
                /// </summary>
                public const string FinancialStabilityReportTwoThousandSeventeen = "financial stability report 2017";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Flash
                /// </summary>
                public const string GdpGrowthRateQoQFlash = "gdp growth rate qoq flash";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Flash
                /// </summary>
                public const string GdpGrowthRateYoYFlash = "gdp growth rate yoy flash";
                /// <summary>
                /// General Elections
                /// </summary>
                public const string GeneralElections = "general elections";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Household Consumption MM
                /// </summary>
                public const string HouseholdConsumptionMoM = "household consumption mom";
                /// <summary>
                /// Household Lending Growth YoY
                /// </summary>
                public const string HouseholdLendingGrowthYoY = "household lending growth yoy";
                /// <summary>
                /// Industrial Inventories QoQ
                /// </summary>
                public const string IndustrialInventoriesQoQ = "industrial inventories qoq";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Lending to Households YoY
                /// </summary>
                public const string LendingToHouseholdsYoY = "lending to households yoy";
                /// <summary>
                /// Markit Services PMI
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "markit services purchasing managers index";
                /// <summary>
                /// Midsummer Day
                /// </summary>
                public const string MidsummerDay = "midsummer day";
                /// <summary>
                /// Midsummer Eve
                /// </summary>
                public const string MidsummerEve = "midsummer eve";
                /// <summary>
                /// Monetary Policy Meeting Minutes
                /// </summary>
                public const string MonetaryPolicyMeetingMinutes = "monetary policy meeting minutes";
                /// <summary>
                /// Monetary Policy Report
                /// </summary>
                public const string MonetaryPolicyReport = "monetary policy report";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Orders
                /// </summary>
                public const string NewOrders = "new orders";
                /// <summary>
                /// New Orders Manufacturing YoY
                /// </summary>
                public const string NewOrdersYoY = "new orders yoy";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year's Eve
                /// </summary>
                public const string NewYearsEve = "new years eve";
                /// <summary>
                /// Pentecost
                /// </summary>
                public const string Pentecost = "pentecost";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// PMI Services
                /// </summary>
                public const string PurchasingManagersIndexServices = "purchasing managers index services";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Riksbank Gov Ingves Speech
                /// </summary>
                public const string RiksbankGovIngvesSpeech = "riksbank gov ingves speech";
                /// <summary>
                /// Riksbank Ingves Speaks before Parliament
                /// </summary>
                public const string RiksbankIngvesSpeaksBeforeParliament = "riksbank ingves speaks before parliament";
                /// <summary>
                /// Riksbank Ingves Speech
                /// </summary>
                public const string RiksbankIngvesSpeech = "riksbank ingves speech";
                /// <summary>
                /// Riksbank Interest Rate
                /// </summary>
                public const string RiksbankInterestRate = "riksbank interest rate";
                /// <summary>
                /// Riksbank Meeting Minutes
                /// </summary>
                public const string RiksbankMeetingMinutes = "riksbank meeting minutes";
                /// <summary>
                /// Riksbank Monetary Policy Report
                /// </summary>
                public const string RiksbankMonetaryPolicyReport = "riksbank monetary policy report";
                /// <summary>
                /// Riksbank Rate Decision
                /// </summary>
                public const string RiksbankRateDecision = "riksbank rate decision";
                /// <summary>
                /// Riksbank Skingsley Speech
                /// </summary>
                public const string RiksbankSkingsleySpeech = "riksbank skingsley speech";
                /// <summary>
                /// Services PMI
                /// </summary>
                public const string ServicesPurchasingManagersIndex = "services purchasing managers index";
                /// <summary>
                /// Swedbank Manufacturing PMI
                /// </summary>
                public const string SwedbankManufacturingPurchasingManagersIndex = "swedbank manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// Whitsun
                /// </summary>
                public const string Whitsun = "whitsun";
            }
            /// <summary>
            /// Switzerland
            /// </summary>
            public static class Switzerland
            {
                /// <summary>
                /// Ascension Day
                /// </summary>
                public const string AscensionDay = "ascension day";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Economic Sentiment Index
                /// </summary>
                public const string EconomicSentimentIndex = "economic sentiment index";
                /// <summary>
                /// Employment Level (QoQ)
                /// </summary>
                public const string EmploymentLevelQoQ = "employment level qoq";
                /// <summary>
                /// Federal Fast Day
                /// </summary>
                public const string FederalFastDay = "federal fast day";
                /// <summary>
                /// Foreign Currency Reserves
                /// </summary>
                public const string ForeignCurrencyReserves = "foreign currency reserves";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "foreign exchange reserves";
                /// <summary>
                /// FX Reserves
                /// </summary>
                public const string FxReserves = "fx reserves";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Industrial Orders YoY
                /// </summary>
                public const string IndustrialOrdersYoY = "industrial orders yoy";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// KOF Leading Indicator
                /// </summary>
                public const string KofLeadingIndicators = "kof leading indicators";
                /// <summary>
                /// Labour Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "national day";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "non farm payrolls";
                /// <summary>
                /// Nuclear Withdrawal Initiative Referendum
                /// </summary>
                public const string NuclearWithdrawalInitiativeReferendum = "nuclear withdrawal initiative referendum";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// procure.ch Manufacturing PMI
                /// </summary>
                public const string ProcurechManufacturingPurchasingManagersIndex = "procurech manufacturing purchasing managers index";
                /// <summary>
                /// Producer and Import Prices MoM
                /// </summary>
                public const string ProducerAndImportPricesMoM = "producer and import prices mom";
                /// <summary>
                /// Producer and Import Prices YoY
                /// </summary>
                public const string ProducerAndImportPricesYoY = "producer and import prices yoy";
                /// <summary>
                /// Producer/Import Prices MoM
                /// </summary>
                public const string ProducerImportPricesMoM = "producer import prices mom";
                /// <summary>
                /// Producer/Import Prices YoY
                /// </summary>
                public const string ProducerImportPricesYoY = "producer import prices yoy";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// Referendum on Corporate Tax Unification
                /// </summary>
                public const string ReferendumOnCorporateTaxUnification = "referendum on corporate tax unification";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// SECO Economic Forecasts
                /// </summary>
                public const string SecoEconomicForecasts = "seco economic forecasts";
                /// <summary>
                /// SNB Annual Report
                /// </summary>
                public const string SnbAnnualReport = "snb annual report";
                /// <summary>
                /// SNB Chair Jordan Speaks
                /// </summary>
                public const string SnbChairJordanSpeaks = "snb chair jordan speaks";
                /// <summary>
                /// SNB Chair Jordan Speech
                /// </summary>
                public const string SnbChairJordanSpeech = "snb chair jordan speech";
                /// <summary>
                /// SNB Chairman Jordan Speech
                /// </summary>
                public const string SnbChairmanJordanSpeech = "snb chairman jordan speech";
                /// <summary>
                /// SNB Financial Stability Report
                /// </summary>
                public const string SnbFinancialStabilityReport = "snb financial stability report";
                /// <summary>
                /// SNB Interest Rate Decison
                /// </summary>
                public const string SnbInterestRateDecison = "snb interest rate decison";
                /// <summary>
                /// SNB Jordan Speech
                /// </summary>
                public const string SnbJordanSpeech = "snb jordan speech";
                /// <summary>
                /// SNB Maechler Speech
                /// </summary>
                public const string SnbMaechlerSpeech = "snb maechler speech";
                /// <summary>
                /// SNB Monthly Bulletin
                /// </summary>
                public const string SnbMonthlyBulletin = "snb monthly bulletin";
                /// <summary>
                /// SNB Moser Speech
                /// </summary>
                public const string SnbMoserSpeech = "snb moser speech";
                /// <summary>
                /// SNB President Studer Speech
                /// </summary>
                public const string SnbPresidentStuderSpeech = "snb president studer speech";
                /// <summary>
                /// SNB press conference
                /// </summary>
                public const string SnbPressConference = "snb press conference";
                /// <summary>
                /// SNB Quarterly Bulletin
                /// </summary>
                public const string SnbQuarterlyBulletin = "snb quarterly bulletin";
                /// <summary>
                /// SNB Studer Speech
                /// </summary>
                public const string SnbStuderSpeech = "snb studer speech";
                /// <summary>
                /// SNB Zurbrügg Speech
                /// </summary>
                public const string SnbZurbrggSpeech = "snb zurbrgg speech";
                /// <summary>
                /// SNB Zurbruegg Speech
                /// </summary>
                public const string SnbZurbrueggSpeech = "snb zurbruegg speech";
                /// <summary>
                /// St. Berchtold
                /// </summary>
                public const string StBerchtold = "st berchtold";
                /// <summary>
                /// St Stephen's Day
                /// </summary>
                public const string StStephensDay = "st stephens day";
                /// <summary>
                /// SVME - Purchasing Managers Index
                /// </summary>
                public const string SvmeManufacturingPurchasingManagersIndex = "svme manufacturing purchasing managers index";
                /// <summary>
                /// SVME - Purchasing Managers  Index
                /// </summary>
                public const string SvmePurchasingManagersIndex = "svme manufacturing purchasing managers index";
                /// <summary>
                /// UBS Consumption Indicator
                /// </summary>
                public const string UbsConsumptionIndicators = "ubs consumption indicators";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// USD Consumption Indicator
                /// </summary>
                public const string UsdConsumptionIndicators = "usd consumption indicators";
                /// <summary>
                /// US President Trump Speech
                /// </summary>
                public const string UsPresidentTrumpSpeech = "us president trump speech";
                /// <summary>
                /// Whit Monday
                /// </summary>
                public const string WhitMonday = "whit monday";
                /// <summary>
                /// World Economic Forum Annual Meeting
                /// </summary>
                public const string WorldEconomicForumAnnualMeeting = "world economic forum annual meeting";
                /// <summary>
                /// World Economic Forum - Davos
                /// </summary>
                public const string WorldEconomicForumDavos = "world economic forum davos";
                /// <summary>
                /// ZEW Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "zew economic sentiment index";
                /// <summary>
                /// ZEW Expectations
                /// </summary>
                public const string ZewExpectations = "zew expectations";
                /// <summary>
                /// ZEW investor sentiment
                /// </summary>
                public const string ZewInvestorSentiment = "zew investor sentiment";
                /// <summary>
                /// ZEW Survey - Expectations
                /// </summary>
                public const string ZewSurveyExpectations = "zew survey expectations";
            }
            /// <summary>
            /// United Kingdom
            /// </summary>
            public static class UnitedKingdom
            {
                /// <summary>
                /// Article 50 Brexit Process Starts
                /// </summary>
                public const string ArticleFiftyBrexitProcessStarts = "article 50 brexit process starts";
                /// <summary>
                /// Autumn Budget
                /// </summary>
                public const string AutumnBudget = "autumn budget";
                /// <summary>
                /// Autumn Budget 2018
                /// </summary>
                public const string AutumnBudgetTwoThousandEighteen = "autumn budget 2018";
                /// <summary>
                /// Average Earnings excl. Bonus
                /// </summary>
                public const string AverageEarningsExcludingBonus = "average earnings excluding bonus";
                /// <summary>
                /// Average Earnings incl. Bonus
                /// </summary>
                public const string AverageEarningsIncludingBonus = "average earnings including bonus";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Bank Holiday
                /// </summary>
                public const string BankHoliday = "bank holiday";
                /// <summary>
                /// BoE Asset Purchase Facility
                /// </summary>
                public const string BankOfEnglandAssetPurchaseFacility = "bank of england asset purchase facility";
                /// <summary>
                /// BoE Bailey Speech
                /// </summary>
                public const string BankOfEnglandBaileySpeech = "bank of england bailey speech";
                /// <summary>
                /// BoE Brazier Speech
                /// </summary>
                public const string BankOfEnglandBrazierSpeech = "bank of england brazier speech";
                /// <summary>
                /// BoE Breeden Speech
                /// </summary>
                public const string BankOfEnglandBreedenSpeech = "bank of england breeden speech";
                /// <summary>
                /// BoE Broadbent Speech
                /// </summary>
                public const string BankOfEnglandBroadbentSpeech = "bank of england broadbent speech";
                /// <summary>
                /// BoE Carney Speaks
                /// </summary>
                public const string BankOfEnglandCarneySpeaks = "bank of england carney speaks";
                /// <summary>
                /// BoE Carney Speaks in Parliament on Brexit
                /// </summary>
                public const string BankOfEnglandCarneySpeaksInParliamentOnBrexit = "bank of england carney speaks in parliament on brexit";
                /// <summary>
                /// BoE Carney Speech
                /// </summary>
                public const string BankOfEnglandCarneySpeech = "bank of england carney speech";
                /// <summary>
                /// BoE Chief Economist Haldane Speech
                /// </summary>
                public const string BankOfEnglandChiefEconomistHaldaneSpeech = "bank of england chief economist haldane speech";
                /// <summary>
                /// BoE Churm Speech
                /// </summary>
                public const string BankOfEnglandChurmSpeech = "bank of england churm speech";
                /// <summary>
                /// BoE Cleland Speech
                /// </summary>
                public const string BankOfEnglandClelandSpeech = "bank of england cleland speech";
                /// <summary>
                /// BoE Consumer Credit
                /// </summary>
                public const string BankOfEnglandConsumerCredit = "bank of england consumer credit";
                /// <summary>
                /// BOE Credit Conditions Survey
                /// </summary>
                public const string BankOfEnglandCreditConditionsSurvey = "bank of england credit conditions survey";
                /// <summary>
                /// BoE Cunliffe Speech
                /// </summary>
                public const string BankOfEnglandCunliffeSpeech = "bank of england cunliffe speech";
                /// <summary>
                /// BoE Deputy Gov Bailey Speech
                /// </summary>
                public const string BankOfEnglandDeputyGovBaileySpeech = "bank of england deputy gov bailey speech";
                /// <summary>
                /// BoE Deputy Gov Broadbent Speech
                /// </summary>
                public const string BankOfEnglandDeputyGovBroadbentSpeech = "bank of england deputy gov broadbent speech";
                /// <summary>
                /// BoE Deputy Gov Cunliffe Speech
                /// </summary>
                public const string BankOfEnglandDeputyGovCunliffeSpeech = "bank of england deputy gov cunliffe speech";
                /// <summary>
                /// BoE Deputy Gov Woods Speech
                /// </summary>
                public const string BankOfEnglandDeputyGovWoodsSpeech = "bank of england deputy gov woods speech";
                /// <summary>
                /// BoE Financial Stability Report
                /// </summary>
                public const string BankOfEnglandFinancialStabilityReport = "bank of england financial stability report";
                /// <summary>
                /// BoE Forbes Speech
                /// </summary>
                public const string BankOfEnglandForbesSpeech = "bank of england forbes speech";
                /// <summary>
                /// BoE FPC Meeting
                /// </summary>
                public const string BankOfEnglandFpcMeeting = "bank of england fpc meeting";
                /// <summary>
                /// BoE FPC Minutes
                /// </summary>
                public const string BankOfEnglandFpcMinutes = "bank of england fpc minutes";
                /// <summary>
                /// BoE FPC Record
                /// </summary>
                public const string BankOfEnglandFpcRecord = "bank of england fpc record";
                /// <summary>
                /// BoE FPC Statement
                /// </summary>
                public const string BankOfEnglandFpcStatement = "bank of england fpc statement";
                /// <summary>
                /// BoE Gov Carney Speech
                /// </summary>
                public const string BankOfEnglandGovCarneySpeech = "bank of england gov carney speech";
                /// <summary>
                /// BoE Governor King Speech
                /// </summary>
                public const string BankOfEnglandGovernorKingSpeech = "bank of england governor king speech";
                /// <summary>
                /// BoE Haldane Speech
                /// </summary>
                public const string BankOfEnglandHaldaneSpeech = "bank of england haldane speech";
                /// <summary>
                /// BoE Haskel Speech
                /// </summary>
                public const string BankOfEnglandHaskelSpeech = "bank of england haskel speech";
                /// <summary>
                /// BoE Hauser Speech
                /// </summary>
                public const string BankOfEnglandHauserSpeech = "bank of england hauser speech";
                /// <summary>
                /// BoE Hogg Speech
                /// </summary>
                public const string BankOfEnglandHoggSpeech = "bank of england hogg speech";
                /// <summary>
                /// BoE Inflation Report
                /// </summary>
                public const string BankOfEnglandInflationReport = "bank of england inflation report";
                /// <summary>
                /// BoE Inflation Report Hearings
                /// </summary>
                public const string BankOfEnglandInflationReportHearings = "bank of england inflation report hearings";
                /// <summary>
                /// BoE Interest Rate Decision
                /// </summary>
                public const string BankOfEnglandInterestRateDecision = "bank of england interest rate decision";
                /// <summary>
                /// BoE Kohn Speech
                /// </summary>
                public const string BankOfEnglandKohnSpeech = "bank of england kohn speech";
                /// <summary>
                /// BoE McCafferty Speech
                /// </summary>
                public const string BankOfEnglandMccaffertySpeech = "bank of england mccafferty speech";
                /// <summary>
                /// BoE Moulder Speech
                /// </summary>
                public const string BankOfEnglandMoulderSpeech = "bank of england moulder speech";
                /// <summary>
                /// BoE MPC Vote Cut
                /// </summary>
                public const string BankOfEnglandMpcVoteCut = "bank of england mpc vote cut";
                /// <summary>
                /// BoE MPC Vote Hike
                /// </summary>
                public const string BankOfEnglandMpcVoteHike = "bank of england mpc vote hike";
                /// <summary>
                /// BoE MPC Vote Unchanged
                /// </summary>
                public const string BankOfEnglandMpcVoteUnchanged = "bank of england mpc vote unchanged";
                /// <summary>
                /// BoE Press Conference
                /// </summary>
                public const string BankOfEnglandPressConference = "bank of england press conference";
                /// <summary>
                /// BoE Proudman Speech
                /// </summary>
                public const string BankOfEnglandProudmanSpeech = "bank of england proudman speech";
                /// <summary>
                /// BOE Quantitative Easing
                /// </summary>
                public const string BankOfEnglandQuantitativeEasing = "bank of england quantitative easing";
                /// <summary>
                /// BoE Quarterly Bulletin
                /// </summary>
                public const string BankOfEnglandQuarterlyBulletin = "bank of england quarterly bulletin";
                /// <summary>
                /// BoE Quarterly Bulletin Q2
                /// </summary>
                public const string BankOfEnglandQuarterlyBulletinQTwo = "bank of england quarterly bulletin q2";
                /// <summary>
                /// Bank of England Quarterly Inflation Report
                /// </summary>
                public const string BankOfEnglandQuarterlyInflationReport = "bank of england quarterly inflation report";
                /// <summary>
                /// BoE Ramsden Speech
                /// </summary>
                public const string BankOfEnglandRamsdenSpeech = "bank of england ramsden speech";
                /// <summary>
                /// BoE Report on EU Withdrawal Scenarios
                /// </summary>
                public const string BankOfEnglandReportOnEuWithdrawalScenarios = "bank of england report on eu withdrawal scenarios";
                /// <summary>
                /// BoE Rule Speech
                /// </summary>
                public const string BankOfEnglandRuleSpeech = "bank of england rule speech";
                /// <summary>
                /// BoE Salmon Speech
                /// </summary>
                public const string BankOfEnglandSalmonSpeech = "bank of england salmon speech";
                /// <summary>
                /// BoE Saporta Speech
                /// </summary>
                public const string BankOfEnglandSaportaSpeech = "bank of england saporta speech";
                /// <summary>
                /// BoE Saunders Speech
                /// </summary>
                public const string BankOfEnglandSaundersSpeech = "bank of england saunders speech";
                /// <summary>
                /// BoE Shafik Speech
                /// </summary>
                public const string BankOfEnglandShafikSpeech = "bank of england shafik speech";
                /// <summary>
                /// BoE Sharp Speech
                /// </summary>
                public const string BankOfEnglandSharpSpeech = "bank of england sharp speech";
                /// <summary>
                /// BoE Stress Test Results
                /// </summary>
                public const string BankOfEnglandStressTestResults = "bank of england stress test results";
                /// <summary>
                /// BoE Systemic Risk Survey Results
                /// </summary>
                public const string BankOfEnglandSystemicRiskSurveyResults = "bank of england systemic risk survey results";
                /// <summary>
                /// BoE Tenreyro Speech
                /// </summary>
                public const string BankOfEnglandTenreyroSpeech = "bank of england tenreyro speech";
                /// <summary>
                /// BoE Vlieghe Speech
                /// </summary>
                public const string BankOfEnglandVliegheSpeech = "bank of england vlieghe speech";
                /// <summary>
                /// BoE Weale Speech
                /// </summary>
                public const string BankOfEnglandWealeSpeech = "bank of england weale speech";
                /// <summary>
                /// BoE Woods Speech
                /// </summary>
                public const string BankOfEnglandWoodsSpeech = "bank of england woods speech";
                /// <summary>
                /// Bank Stress Tests
                /// </summary>
                public const string BankStressTests = "bank stress tests";
                /// <summary>
                /// BBA Mortgage Approvals
                /// </summary>
                public const string BbaMortgageApprovals = "bba mortgage approvals";
                /// <summary>
                /// BCC Quarterly Economic Survey
                /// </summary>
                public const string BccQuarterlyEconomicSurvey = "bcc quarterly economic survey";
                /// <summary>
                /// Benn Bill Debate
                /// </summary>
                public const string BennBillDebate = "benn bill debate";
                /// <summary>
                /// BoE's Carney and Cunliffe Speak in Parliament
                /// </summary>
                public const string BoesCarneyAndCunliffeSpeakInParliament = "boes carney and cunliffe speak in parliament";
                /// <summary>
                /// BOE's Gov Carney speech
                /// </summary>
                public const string BoesGovCarneySpeech = "boes gov carney speech";
                /// <summary>
                /// BoE's Governor Carney Speaks
                /// </summary>
                public const string BoesGovernorCarneySpeaks = "boes governor carney speaks";
                /// <summary>
                /// BoE's Governor Carney Speaks at Parliament
                /// </summary>
                public const string BoesGovernorCarneySpeaksAtParliament = "boes governor carney speaks at parliament";
                /// <summary>
                /// BOE's Governor Carney Speech
                /// </summary>
                public const string BoesGovernorCarneySpeech = "boes governor carney speech";
                /// <summary>
                /// BoEs Governor King Speech
                /// </summary>
                public const string BoesGovernorKingSpeech = "boes governor king speech";
                /// <summary>
                /// Boxing Day
                /// </summary>
                public const string BoxingDay = "boxing day";
                /// <summary>
                /// BRC Retail Sales Monitor - All (YoY)
                /// </summary>
                public const string BrcRetailSalesYoY = "brc retail sales yoy";
                /// <summary>
                /// BRC Shop Price Index (MoM)
                /// </summary>
                public const string BrcShopPriceIndexMoM = "brc shop price index mom";
                /// <summary>
                /// BRC Shop Price Index YoY
                /// </summary>
                public const string BrcShopPriceIndexYoY = "brc shop price index yoy";
                /// <summary>
                /// Brexit Bill Debate
                /// </summary>
                public const string BrexitBillDebate = "brexit bill debate";
                /// <summary>
                /// Budget Report
                /// </summary>
                public const string BudgetReport = "budget report";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "business confidence";
                /// <summary>
                /// Business Investment QoQ
                /// </summary>
                public const string BusinessInvestmentQoQ = "business investment qoq";
                /// <summary>
                /// Business Investment QoQ Final
                /// </summary>
                public const string BusinessInvestmentQoQFinal = "business investment qoq final";
                /// <summary>
                /// Business Investment QoQ Prel
                /// </summary>
                public const string BusinessInvestmentQoQPreliminary = "business investment qoq preliminary";
                /// <summary>
                /// Business Investment YoY
                /// </summary>
                public const string BusinessInvestmentYoY = "business investment yoy";
                /// <summary>
                /// Business Investment YoY Final
                /// </summary>
                public const string BusinessInvestmentYoYFinal = "business investment yoy final";
                /// <summary>
                /// Business Investment YoY Prel
                /// </summary>
                public const string BusinessInvestmentYoYPreliminary = "business investment yoy preliminary";
                /// <summary>
                /// Cabinet Meeting on Brexit Draft Deal
                /// </summary>
                public const string CabinetMeetingOnBrexitDraftDeal = "cabinet meeting on brexit draft deal";
                /// <summary>
                /// CBI Business Optimism Index
                /// </summary>
                public const string CbiBusinessOptimismIndex = "cbi business optimism index";
                /// <summary>
                /// CBI Distributive Trades
                /// </summary>
                public const string CbiDistributiveTrades = "cbi distributive trades";
                /// <summary>
                /// CBI Industrial Trends Orders
                /// </summary>
                public const string CbiIndustrialTrendsOrders = "cbi industrial trends orders";
                /// <summary>
                /// CB Leading Index
                /// </summary>
                public const string CbLeadingEconomicIndex = "cb leading economic index";
                /// <summary>
                /// CB Leading Economic Index MoM
                /// </summary>
                public const string CbLeadingEconomicIndexMoM = "cb leading economic index mom";
                /// <summary>
                /// Chancellor Hammond Budget Statement
                /// </summary>
                public const string ChancellorHammondBudgetStatement = "chancellor hammond budget statement";
                /// <summary>
                /// Chancellor Osborne Budget Statement
                /// </summary>
                public const string ChancellorOsborneBudgetStatement = "chancellor osborne budget statement";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Day (Substitute Day)
                /// </summary>
                public const string ChristmasDaySubstituteDay = "christmas day substitute day";
                /// <summary>
                /// Claimant Count Change
                /// </summary>
                public const string ClaimantCountChange = "claimant count change";
                /// <summary>
                /// Claimant Count Rate
                /// </summary>
                public const string ClaimantCountRate = "claimant count rate";
                /// <summary>
                /// Construction Orders YoY
                /// </summary>
                public const string ConstructionOrdersYoY = "construction orders yoy";
                /// <summary>
                /// Construction Output YoY
                /// </summary>
                public const string ConstructionOutputYoY = "construction output yoy";
                /// <summary>
                /// Construction PMI
                /// </summary>
                public const string ConstructionPurchasingManagersIndex = "construction purchasing managers index";
                /// <summary>
                /// Consumer Inflation Expectations
                /// </summary>
                public const string ConsumerInflationExpectations = "consumer inflation expectations";
                /// <summary>
                /// Core Inflation Rate MoM
                /// </summary>
                public const string CoreInflationRateMoM = "core inflation rate mom";
                /// <summary>
                /// Core Inflation Rate YoY
                /// </summary>
                public const string CoreInflationRateYoY = "core inflation rate yoy";
                /// <summary>
                /// Core RPI MoM
                /// </summary>
                public const string CoreRetailPriceIndexMoM = "core retail price index mom";
                /// <summary>
                /// Core RPI YoY
                /// </summary>
                public const string CoreRetailPriceIndexYoY = "core retail price index yoy";
                /// <summary>
                /// Core Retail Sales YoY
                /// </summary>
                public const string CoreRetailSalesYoY = "core retail sales yoy";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Early May Bank Holiday
                /// </summary>
                public const string EarlyMayBankHoliday = "early may bank holiday";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "easter monday";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "employment change";
                /// <summary>
                /// EU Membership Referendum - Final Results
                /// </summary>
                public const string EuMembershipReferendumFinalResults = "eu membership referendum final results";
                /// <summary>
                /// European Union Membership Referendum
                /// </summary>
                public const string EuropeanUnionMembershipReferendum = "european union membership referendum";
                /// <summary>
                /// Finance Minister Osborne Speech
                /// </summary>
                public const string FinanceMinisterOsborneSpeech = "finance minister osborne speech";
                /// <summary>
                /// Finance Min Osborne Speaks on Brexit
                /// </summary>
                public const string FinanceMinOsborneSpeaksOnBrexit = "finance min osborne speaks on brexit";
                /// <summary>
                /// Financial Stability Report
                /// </summary>
                public const string FinancialStabilityReport = "financial stability report";
                /// <summary>
                /// Fin Minister Hammond Speech
                /// </summary>
                public const string FinMinisterHammondSpeech = "fin minister hammond speech";
                /// <summary>
                /// Fin Minister Osborne Speaks in Parliament
                /// </summary>
                public const string FinMinisterOsborneSpeaksInParliament = "fin minister osborne speaks in parliament";
                /// <summary>
                /// 5-Year Treasury Gilt Auction
                /// </summary>
                public const string FiveYearTreasuryGiltAuction = "5 year treasury gilt auction";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "gdp growth rate";
                /// <summary>
                /// GDP Growth Rate QoQ
                /// </summary>
                public const string GdpGrowthRateQoQ = "gdp growth rate qoq";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ Prel
                /// </summary>
                public const string GdpGrowthRateQoQPreliminary = "gdp growth rate qoq preliminary";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate YoY
                /// </summary>
                public const string GdpGrowthRateYoY = "gdp growth rate yoy";
                /// <summary>
                /// GDP Growth Rate YoY Final
                /// </summary>
                public const string GdpGrowthRateYoYFinal = "gdp growth rate yoy final";
                /// <summary>
                /// GDP Growth Rate YoY Prel
                /// </summary>
                public const string GdpGrowthRateYoYPreliminary = "gdp growth rate yoy preliminary";
                /// <summary>
                /// GDP Growth Rate YoY 2nd Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// GDP Growth YoY
                /// </summary>
                public const string GdpGrowthYoY = "gdp growth yoy";
                /// <summary>
                /// GDP MoM
                /// </summary>
                public const string GdpMoM = "gdp mom";
                /// <summary>
                /// GDP 3-Month Avg
                /// </summary>
                public const string GdpThreeMonthAvg = "gdp 3 month avg";
                /// <summary>
                /// GDP YoY
                /// </summary>
                public const string GdpYoY = "gdp yoy";
                /// <summary>
                /// General Election
                /// </summary>
                public const string GeneralElection = "general election";
                /// <summary>
                /// Gfk Consumer Confidence
                /// </summary>
                public const string GfkConsumerConfidence = "gfk consumer confidence";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Goods Trade Balance
                /// </summary>
                public const string GoodsTradeBalance = "goods trade balance";
                /// <summary>
                /// Government Autumn Forecasts
                /// </summary>
                public const string GovernmentAutumnForecasts = "government autumn forecasts";
                /// <summary>
                /// Halifax House Price Index MoM
                /// </summary>
                public const string HalifaxHousePriceIndexMoM = "halifax house price index mom";
                /// <summary>
                /// Halifax House Price Index YoY
                /// </summary>
                public const string HalifaxHousePriceIndexYoY = "halifax house price index yoy";
                /// <summary>
                /// Hammond Presents Autumn Statement
                /// </summary>
                public const string HammondPresentsAutumnStatement = "hammond presents autumn statement";
                /// <summary>
                /// Hometrack Housing Prices MoM
                /// </summary>
                public const string HometrackHousingPricesMoM = "hometrack housing prices mom";
                /// <summary>
                /// Hometrack Housing Prices s.a
                /// </summary>
                public const string HometrackHousingPricesSeasonallyAdjusted = "hometrack housing prices seasonally adjusted";
                /// <summary>
                /// House Price Index MoM
                /// </summary>
                public const string HousePriceIndexMoM = "house price index mom";
                /// <summary>
                /// House Price Index YoY
                /// </summary>
                public const string HousePriceIndexYoY = "house price index yoy";
                /// <summary>
                /// Index of Services (3M/3M)
                /// </summary>
                public const string IndexOfServicesThreeMThreeM = "index of services 3m 3m";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Inflation Report Hearings
                /// </summary>
                public const string InflationReportHearings = "inflation report hearings";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "interest rate decision";
                /// <summary>
                /// Labour Costs QoQ
                /// </summary>
                public const string LaborCostsIndexQoQ = "labor costs index qoq";
                /// <summary>
                /// Labour Productivity QoQ
                /// </summary>
                public const string LaborProductivityQoQ = "labor productivity qoq";
                /// <summary>
                /// Labour Productivity QoQ Final
                /// </summary>
                public const string LaborProductivityQoQFinal = "labor productivity qoq final";
                /// <summary>
                /// Labour Productivity QoQ Prel
                /// </summary>
                public const string LaborProductivityQoQPreliminary = "labor productivity qoq preliminary";
                /// <summary>
                /// Local Elections
                /// </summary>
                public const string LocalElections = "local elections";
                /// <summary>
                /// London Assembly Election
                /// </summary>
                public const string LondonAssemblyElection = "london assembly election";
                /// <summary>
                /// London Mayoral Election
                /// </summary>
                public const string LondonMayoralElection = "london mayoral election";
                /// <summary>
                /// Manufacturing Production MoM
                /// </summary>
                public const string ManufacturingProductionMoM = "manufacturing production mom";
                /// <summary>
                /// Manufacturing Production YoY
                /// </summary>
                public const string ManufacturingProductionYoY = "manufacturing production yoy";
                /// <summary>
                /// Mark Carney will become the new Bank of England Governor
                /// </summary>
                public const string MarkCarneyWillBecomeTheNewBankOfEnglandGovernor = "mark carney will become the new bank of england governor";
                /// <summary>
                /// Markit/CIPS Composite PMI Final
                /// </summary>
                public const string MarkitCipsCompositePurchasingManagersIndexFinal = "markit cips composite purchasing managers index final";
                /// <summary>
                /// Markit/CIPS Composite PMI Flash
                /// </summary>
                public const string MarkitCipsCompositePurchasingManagersIndexFlash = "markit cips composite purchasing managers index flash";
                /// <summary>
                /// Markit/CIPS Manufacturing PMI
                /// </summary>
                public const string MarkitCipsManufacturingPurchasingManagersIndex = "markit cips manufacturing purchasing managers index";
                /// <summary>
                /// Markit/CIPS Manufacturing PMI Final
                /// </summary>
                public const string MarkitCipsManufacturingPurchasingManagersIndexFinal = "markit cips manufacturing purchasing managers index final";
                /// <summary>
                /// Markit/CIPS Manufacturing PMI Flash
                /// </summary>
                public const string MarkitCipsManufacturingPurchasingManagersIndexFlash = "markit cips manufacturing purchasing managers index flash";
                /// <summary>
                /// Markit/CIPS UK Services PMI
                /// </summary>
                public const string MarkitCipsUkServicesPurchasingManagersIndex = "markit cips uk services purchasing managers index";
                /// <summary>
                /// Markit/CIPS UK Services PMI Final
                /// </summary>
                public const string MarkitCipsUkServicesPurchasingManagersIndexFinal = "markit cips uk services purchasing managers index final";
                /// <summary>
                /// Markit/CIPS UK Services PMI Flash
                /// </summary>
                public const string MarkitCipsUkServicesPurchasingManagersIndexFlash = "markit cips uk services purchasing managers index flash";
                /// <summary>
                /// Markit Manufacturing PMI
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services PMI
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "markit services purchasing managers index";
                /// <summary>
                /// May Day
                /// </summary>
                public const string MayDay = "may day";
                /// <summary>
                /// May No Confidence Vote Result
                /// </summary>
                public const string MayNoConfidenceVoteResult = "may no confidence vote result";
                /// <summary>
                /// M4 Money Supply (MoM)
                /// </summary>
                public const string MFourMoneySupplyMoM = "m4 money supply mom";
                /// <summary>
                /// M4 Money Supply (YoY)
                /// </summary>
                public const string MFourMoneySupplyYoY = "m4 money supply yoy";
                /// <summary>
                /// Monthly GDP
                /// </summary>
                public const string MonthlyGdp = "monthly gdp";
                /// <summary>
                /// Monthly GDP 3-Month Avg
                /// </summary>
                public const string MonthlyGdpThreeMonthAvg = "monthly gdp 3 month avg";
                /// <summary>
                /// Mortgage Approvals
                /// </summary>
                public const string MortgageApprovals = "mortgage approvals";
                /// <summary>
                /// Mortgage Lending
                /// </summary>
                public const string MortgageLending = "mortgage lending";
                /// <summary>
                /// MPC Meeting Minutes
                /// </summary>
                public const string MpcMeetingMinutes = "mpc meeting minutes";
                /// <summary>
                /// MPC Member Bean Speech
                /// </summary>
                public const string MpcMemberBeanSpeech = "mpc member bean speech";
                /// <summary>
                /// MPC Member Paul Fisher Speech
                /// </summary>
                public const string MpcMemberPaulFisherSpeech = "mpc member paul fisher speech";
                /// <summary>
                /// MPs Vote on Benn Bill
                /// </summary>
                public const string MpsVoteOnBennBill = "mps vote on benn bill";
                /// <summary>
                /// MPs Vote on Brexit Bill
                /// </summary>
                public const string MpsVoteOnBrexitBill = "mps vote on brexit bill";
                /// <summary>
                /// MPs Vote on Brexit Timetable
                /// </summary>
                public const string MpsVoteOnBrexitTimetable = "mps vote on brexit timetable";
                /// <summary>
                /// MPs Vote on PM Election Call
                /// </summary>
                public const string MpsVoteOnPmElectionCall = "mps vote on pm election call";
                /// <summary>
                /// MPs Vote on Withdrawal Agreement Bill
                /// </summary>
                public const string MpsVoteOnWithdrawalAgreementBill = "mps vote on withdrawal agreement bill";
                /// <summary>
                /// National Assembly for Wales Elections
                /// </summary>
                public const string NationalAssemblyForWalesElections = "national assembly for wales elections";
                /// <summary>
                /// Nationwide Housing Prices MoM
                /// </summary>
                public const string NationwideHousingPricesMoM = "nationwide housing prices mom";
                /// <summary>
                /// Nationwide Housing Prices YoY
                /// </summary>
                public const string NationwideHousingPricesYoY = "nationwide housing prices yoy";
                /// <summary>
                /// Net Lending to Individuals MoM
                /// </summary>
                public const string NetLendingToIndividualsMoM = "net lending to individuals mom";
                /// <summary>
                /// New Car Sales YoY
                /// </summary>
                public const string NewCarSalesYoY = "new car sales yoy";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year’s Day (Substitute Day)
                /// </summary>
                public const string NewYearsDaySubstituteDay = "new years day substitute day";
                /// <summary>
                /// NIESR GDP Est (3M)
                /// </summary>
                public const string NiesrGdpEstimateThreeMonths = "niesr gdp estimate 3m";
                /// <summary>
                /// NIESR Monthly GDP Tracker
                /// </summary>
                public const string NiesrMonthlyGdpTracker = "niesr monthly gdp tracker";
                /// <summary>
                /// Northern Ireland Assembly Election
                /// </summary>
                public const string NorthernIrelandAssemblyElection = "northern ireland assembly election";
                /// <summary>
                /// OBR Fiscal Forecasts
                /// </summary>
                public const string ObrFiscalForecasts = "obr fiscal forecasts";
                /// <summary>
                /// OBR Fiscal Risks Report
                /// </summary>
                public const string ObrFiscalRisksReport = "obr fiscal risks report";
                /// <summary>
                /// OECD Gurria Speech on UK Referendum
                /// </summary>
                public const string OecdGurriaSpeechOnUkReferendum = "oecd gurria speech on uk referendum";
                /// <summary>
                /// Parliamentary Debate & Vote on Part of Brexit Deal
                /// </summary>
                public const string ParliamentaryDebateAndVoteOnPartOfBrexitDeal = "parliamentary debate and vote on part of brexit deal";
                /// <summary>
                /// Parliamentary Elections
                /// </summary>
                public const string ParliamentaryElections = "parliamentary elections";
                /// <summary>
                /// Parliamentary Vote on Art 50 Extension
                /// </summary>
                public const string ParliamentaryVoteOnArtFiftyExtension = "parliamentary vote on art 50 extension";
                /// <summary>
                /// Parliamentary Vote on Brexit Amendments
                /// </summary>
                public const string ParliamentaryVoteOnBrexitAmendments = "parliamentary vote on brexit amendments";
                /// <summary>
                /// Parliamentary Vote on Brexit Deal
                /// </summary>
                public const string ParliamentaryVoteOnBrexitDeal = "parliamentary vote on brexit deal";
                /// <summary>
                /// Parliamentary Vote on No-Deal Brexit
                /// </summary>
                public const string ParliamentaryVoteOnNoDealBrexit = "parliamentary vote on no deal brexit";
                /// <summary>
                /// Parliamentary Votes on Brexit Alternatives
                /// </summary>
                public const string ParliamentaryVotesOnBrexitAlternatives = "parliamentary votes on brexit alternatives";
                /// <summary>
                /// Parliament Debate on Brexit
                /// </summary>
                public const string ParliamentDebateOnBrexit = "parliament debate on brexit";
                /// <summary>
                /// Parliament Debate on Brexit Alternatives
                /// </summary>
                public const string ParliamentDebateOnBrexitAlternatives = "parliament debate on brexit alternatives";
                /// <summary>
                /// PM May No Confidence Vote
                /// </summary>
                public const string PmMayNoConfidenceVote = "pm may no confidence vote";
                /// <summary>
                /// PM May's Plan B Statement on Brexit
                /// </summary>
                public const string PmMaysPlanBStatementOnBrexit = "pm mays plan b statement on brexit";
                /// <summary>
                /// PM May's Statement on Brexit
                /// </summary>
                public const string PmMaysStatementOnBrexit = "pm mays statement on brexit";
                /// <summary>
                /// PM May Statement on New Brexit Deal
                /// </summary>
                public const string PmMayStatementOnNewBrexitDeal = "pm may statement on new brexit deal";
                /// <summary>
                /// Prime Minister May Speech
                /// </summary>
                public const string PrimeMinisterMaySpeech = "prime minister may speech";
                /// <summary>
                /// Prime Minister May Speech on Brexit
                /// </summary>
                public const string PrimeMinisterMaySpeechOnBrexit = "prime minister may speech on brexit";
                /// <summary>
                /// Prime Minister Theresa May Speech
                /// </summary>
                public const string PrimeMinisterTheresaMaySpeech = "prime minister theresa may speech";
                /// <summary>
                /// PPI Core Output MoM
                /// </summary>
                public const string ProducerPriceIndexCoreOutputMoM = "producer price index core output mom";
                /// <summary>
                /// PPI Core Output YoY
                /// </summary>
                public const string ProducerPriceIndexCoreOutputYoY = "producer price index core output yoy";
                /// <summary>
                /// PPI Input MoM
                /// </summary>
                public const string ProducerPriceIndexInputMoM = "producer price index input mom";
                /// <summary>
                /// PPI Input YoY
                /// </summary>
                public const string ProducerPriceIndexInputYoY = "producer price index input yoy";
                /// <summary>
                /// PPI Output MoM
                /// </summary>
                public const string ProducerPriceIndexOutputMoM = "producer price index output mom";
                /// <summary>
                /// PPI Output YoY
                /// </summary>
                public const string ProducerPriceIndexOutputYoY = "producer price index output yoy";
                /// <summary>
                /// Public Sector Borrowing MoM
                /// </summary>
                public const string PublicSectorBorrowingMoM = "public sector borrowing mom";
                /// <summary>
                /// Public Sector Net Borrowing
                /// </summary>
                public const string PublicSectorNetBorrowing = "public sector net borrowing";
                /// <summary>
                /// Queen's Speech at The Parliament
                /// </summary>
                public const string QueensSpeechAtTheParliament = "queens speech at the parliament";
                /// <summary>
                /// Retail Price Index MoM
                /// </summary>
                public const string RetailPriceIndexMoM = "retail price index mom";
                /// <summary>
                /// Retail Price Index YoY
                /// </summary>
                public const string RetailPriceIndexYoY = "retail price index yoy";
                /// <summary>
                /// Retail Sales ex Fuel MoM
                /// </summary>
                public const string RetailSalesExcludingFuelMoM = "retail sales excluding fuel mom";
                /// <summary>
                /// Retail Sales ex Fuel YoY
                /// </summary>
                public const string RetailSalesExcludingFuelYoY = "retail sales excluding fuel yoy";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// RICS House Price Balance
                /// </summary>
                public const string RicsHousePriceBalance = "rics house price balance";
                /// <summary>
                /// Scottish Independence Referendum
                /// </summary>
                public const string ScottishIndependenceReferendum = "scottish independence referendum";
                /// <summary>
                /// Scottish Parliament Election
                /// </summary>
                public const string ScottishParliamentElection = "scottish parliament election";
                /// <summary>
                /// Spring Bank Holiday
                /// </summary>
                public const string SpringBankHoliday = "spring bank holiday";
                /// <summary>
                /// Spring Budget 2018
                /// </summary>
                public const string SpringBudgetTwoThousandEighteen = "spring budget 2018";
                /// <summary>
                /// Spring Budget 2019
                /// </summary>
                public const string SpringBudgetTwoThousandNineteen = "spring budget 2019";
                /// <summary>
                /// Summer Bank Holiday
                /// </summary>
                public const string SummerBankHoliday = "summer bank holiday";
                /// <summary>
                /// 10-Year Treasury Gilt Auction
                /// </summary>
                public const string TenYearTreasuryGiltAuction = "10 year treasury gilt auction";
                /// <summary>
                /// 30-Year Treasury Gilt Auction
                /// </summary>
                public const string ThirtyYearTreasuryGiltAuction = "30 year treasury gilt auction";
                /// <summary>
                /// Total Business Investment (QoQ)
                /// </summary>
                public const string TotalBusinessInvestmentQoQ = "total business investment qoq";
                /// <summary>
                /// Total Business Investment (YoY)
                /// </summary>
                public const string TotalBusinessInvestmentYoY = "total business investment yoy";
                /// <summary>
                /// Treasury Committee Hearing – Inflation Report
                /// </summary>
                public const string TreasuryCommitteeHearingInflationReport = "treasury committee hearing inflation report";
                /// <summary>
                /// Treasury Select Committee Hearing
                /// </summary>
                public const string TreasurySelectCommitteeHearing = "treasury select committee hearing";
                /// <summary>
                /// 2016 Annual Budget Announcement
                /// </summary>
                public const string TwoThousandSixteenAnnualBudgetAnnouncement = "2016 annual budget announcement";
                /// <summary>
                /// UK Finance Mortgage Approvals
                /// </summary>
                public const string UkFinanceMortgageApprovals = "uk finance mortgage approvals";
                /// <summary>
                /// UK General Election
                /// </summary>
                public const string UkGeneralElection = "uk general election";
                /// <summary>
                /// UK May - EU Juncker Meeting
                /// </summary>
                public const string UkMayEuJunckerMeeting = "uk may eu juncker meeting";
                /// <summary>
                /// UK PM Statement on Brexit
                /// </summary>
                public const string UkPmStatementOnBrexit = "uk pm statement on brexit";
                /// <summary>
                /// UK Supreme Court Ruling On Article 50
                /// </summary>
                public const string UkSupremeCourtRulingOnArticleFifty = "uk supreme court ruling on article 50";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
            }
            /// <summary>
            /// United States
            /// </summary>
            public static class UnitedStates
            {
                /// <summary>
                /// ADP Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "adp employment change";
                /// <summary>
                /// All Car Sales
                /// </summary>
                public const string AllCarSales = "all car sales";
                /// <summary>
                /// All Truck Sales
                /// </summary>
                public const string AllTruckSales = "all truck sales";
                /// <summary>
                /// API Weekly Crude Stock Change
                /// </summary>
                public const string ApiWeeklyCrudeStockChange = "api weekly crude stock change";
                /// <summary>
                /// API Weekly Gasoline Stock Change
                /// </summary>
                public const string ApiWeeklyGasolineStockChange = "api weekly gasoline stock change";
                /// <summary>
                /// Average Hourly Earnings MoM
                /// </summary>
                public const string AverageHourlyEarningsMoM = "average hourly earnings mom";
                /// <summary>
                /// Average Hourly Earnings YoY
                /// </summary>
                public const string AverageHourlyEarningsYoY = "average hourly earnings yoy";
                /// <summary>
                /// Average Weekly Hours
                /// </summary>
                public const string AverageWeeklyHours = "average weekly hours";
                /// <summary>
                /// Baker Hughes Oil Rig Count
                /// </summary>
                public const string BakerHughesOilRigCount = "baker hughes oil rig count";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "balance of trade";
                /// <summary>
                /// Beige Book
                /// </summary>
                public const string BeigeBook = "beige book";
                /// <summary>
                /// Birthday of Martin Luther King, Jr.
                /// </summary>
                public const string BirthdayOfMartinLutherKingJr = "birthday of martin luther king jr";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "building permits";
                /// <summary>
                /// Building Permits MoM
                /// </summary>
                public const string BuildingPermitsMoM = "building permits mom";
                /// <summary>
                /// Business Inventories MoM
                /// </summary>
                public const string BusinessInventoriesMoM = "business inventories mom";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "capacity utilization";
                /// <summary>
                /// CB Consumer Confidence
                /// </summary>
                public const string CbConsumerConfidence = "cb consumer confidence";
                /// <summary>
                /// CB Employment Trends Index
                /// </summary>
                public const string CbEmploymentTrendsIndex = "cb employment trends index";
                /// <summary>
                /// CB Leading Index MoM
                /// </summary>
                public const string CbLeadingEconomicIndexMoM = "cb leading economic index mom";
                /// <summary>
                /// Chain stores WoW
                /// </summary>
                public const string ChainStoresSalesWoW = "chain stores sales wow";
                /// <summary>
                /// Chain Stores Sales YoY
                /// </summary>
                public const string ChainStoresSalesYoY = "chain stores sales yoy";
                /// <summary>
                /// Challenger Job Cuts
                /// </summary>
                public const string ChallengerJobCuts = "challenger job cuts";
                /// <summary>
                /// Chicago Fed National Activity Index
                /// </summary>
                public const string ChicagoFedNationalActivityIndex = "chicago fed national activity index";
                /// <summary>
                /// Chicago PMI
                /// </summary>
                public const string ChicagoPurchasingManagersIndex = "chicago purchasing managers index";
                /// <summary>
                /// Christmas Day
                /// </summary>
                public const string ChristmasDay = "christmas day";
                /// <summary>
                /// Christmas Day (Substitute Day)
                /// </summary>
                public const string ChristmasDaySubstituteDay = "christmas day substitute day";
                /// <summary>
                /// Columbus Day
                /// </summary>
                public const string ColumbusDay = "columbus day";
                /// <summary>
                /// Construction Spending MoM
                /// </summary>
                public const string ConstructionSpendingMoM = "construction spending mom";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "consumer confidence";
                /// <summary>
                /// Consumer Credit Change
                /// </summary>
                public const string ConsumerCreditChange = "consumer credit change";
                /// <summary>
                /// Consumer Inflation Expectations
                /// </summary>
                public const string ConsumerInflationExpectations = "consumer inflation expectations";
                /// <summary>
                /// Consumer Price Index
                /// </summary>
                public const string ConsumerPriceIndex = "consumer price index";
                /// <summary>
                /// Consumer Price Index Ex Food & Energy (MoM)
                /// </summary>
                public const string ConsumerPriceIndexExcludingFoodAndEnergyMoM = "consumer price index excluding food and energy mom";
                /// <summary>
                /// Consumer Price Index Ex Food & Energy (YoY)
                /// </summary>
                public const string ConsumerPriceIndexExcludingFoodAndEnergyYoY = "consumer price index excluding food and energy yoy";
                /// <summary>
                /// CPI Real Earnings MoM
                /// </summary>
                public const string ConsumerPriceIndexRealEarningsMoM = "consumer price index real earnings mom";
                /// <summary>
                /// Continuing Jobless Claims
                /// </summary>
                public const string ContinuingJoblessClaims = "continuing jobless claims";
                /// <summary>
                /// Core Consumer Price Index
                /// </summary>
                public const string CoreConsumerPriceIndex = "core consumer price index";
                /// <summary>
                /// Core Durable Goods Orders
                /// </summary>
                public const string CoreDurableGoodsOrders = "core durable goods orders";
                /// <summary>
                /// Core Durable Goods Orders MoM
                /// </summary>
                public const string CoreDurableGoodsOrdersMoM = "core durable goods orders mom";
                /// <summary>
                /// Core Inflation Rate MoM
                /// </summary>
                public const string CoreInflationRateMoM = "core inflation rate mom";
                /// <summary>
                /// Core Inflation Rate YoY
                /// </summary>
                public const string CoreInflationRateYoY = "core inflation rate yoy";
                /// <summary>
                /// Core Personal Consumption Expenditure - Price Index (MoM)
                /// </summary>
                public const string CorePersonalConsumptionExpenditurePriceIndexMoM = "core personal consumption expenditure price index mom";
                /// <summary>
                /// Core Personal Consumption Expenditures QoQ
                /// </summary>
                public const string CorePersonalConsumptionExpenditurePriceIndexQoQ = "core personal consumption expenditure price index qoq";
                /// <summary>
                /// Core PCE Prices QoQ Adv
                /// </summary>
                public const string CorePersonalConsumptionExpenditurePriceIndexQoQAdv = "core personal consumption expenditure price index qoq adv";
                /// <summary>
                /// Core PCE Prices QoQ Final
                /// </summary>
                public const string CorePersonalConsumptionExpenditurePriceIndexQoQFinal = "core personal consumption expenditure price index qoq final";
                /// <summary>
                /// Core PCE Prices QoQ 2 Est
                /// </summary>
                public const string CorePersonalConsumptionExpenditurePriceIndexQoQSecondEstimate = "core personal consumption expenditure price index qoq second estimate";
                /// <summary>
                /// Core PCE Price Index YoY
                /// </summary>
                public const string CorePersonalConsumptionExpenditurePriceIndexYoY = "core personal consumption expenditure price index yoy";
                /// <summary>
                /// Core PPI MoM
                /// </summary>
                public const string CoreProducerPriceIndexMoM = "core producer price index mom";
                /// <summary>
                /// Core PPI YoY
                /// </summary>
                public const string CoreProducerPriceIndexYoY = "core producer price index yoy";
                /// <summary>
                /// Core Retail Sales MoM
                /// </summary>
                public const string CoreRetailSalesMoM = "core retail sales mom";
                /// <summary>
                /// Corporate Profits QoQ
                /// </summary>
                public const string CorporateProfitsQoQ = "corporate profits qoq";
                /// <summary>
                /// Corporate Profits QoQ Final
                /// </summary>
                public const string CorporateProfitsQoQFinal = "corporate profits qoq final";
                /// <summary>
                /// Corporate Profits QoQ Prel
                /// </summary>
                public const string CorporateProfitsQoQPreliminary = "corporate profits qoq preliminary";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "current account";
                /// <summary>
                /// Dallas Fed Manufacturing Index
                /// </summary>
                public const string DallasFedManufacturingIndex = "dallas fed manufacturing index";
                /// <summary>
                /// Deadline for Funding For New Fiscal Year
                /// </summary>
                public const string DeadlineForFundingForNewFiscalYear = "deadline for funding for new fiscal year";
                /// <summary>
                /// Domestic Car Sales
                /// </summary>
                public const string DomesticCarSales = "domestic car sales";
                /// <summary>
                /// Domestic Truck Sales
                /// </summary>
                public const string DomesticTruckSales = "domestic truck sales";
                /// <summary>
                /// Durable Goods Orders Ex Defense MoM
                /// </summary>
                public const string DurableGoodsOrdersExcludingDefenseMoM = "durable goods orders excluding defense mom";
                /// <summary>
                /// Durable Goods Orders Ex Transp MoM
                /// </summary>
                public const string DurableGoodsOrdersExcludingTransportationMoM = "durable goods orders excluding transportation mom";
                /// <summary>
                /// Durable Goods Orders MoM
                /// </summary>
                public const string DurableGoodsOrdersMoM = "durable goods orders mom";
                /// <summary>
                /// EIA Crude Oil Imports
                /// </summary>
                public const string EiaCrudeOilImports = "eia crude oil imports";
                /// <summary>
                /// EIA Crude Oil Imports Change
                /// </summary>
                public const string EiaCrudeOilImportsChange = "eia crude oil imports change";
                /// <summary>
                /// EIA Crude Oil Stocks Change
                /// </summary>
                public const string EiaCrudeOilStocksChange = "eia crude oil stocks change";
                /// <summary>
                /// EIA Cushing Crude Oil Stocks Change
                /// </summary>
                public const string EiaCushingCrudeOilStocksChange = "eia cushing crude oil stocks change";
                /// <summary>
                /// EIA Distillate Fuel Production Change
                /// </summary>
                public const string EiaDistillateFuelProductionChange = "eia distillate fuel production change";
                /// <summary>
                /// EIA Distillate Stocks
                /// </summary>
                public const string EiaDistillateStocks = "eia distillate stocks";
                /// <summary>
                /// EIA Distillate Stocks Change
                /// </summary>
                public const string EiaDistillateStocksChange = "eia distillate stocks change";
                /// <summary>
                /// EIA Gasoline Production
                /// </summary>
                public const string EiaGasolineProduction = "eia gasoline production";
                /// <summary>
                /// EIA Gasoline Production Change
                /// </summary>
                public const string EiaGasolineProductionChange = "eia gasoline production change";
                /// <summary>
                /// EIA Gasoline Stocks Change
                /// </summary>
                public const string EiaGasolineStocksChange = "eia gasoline stocks change";
                /// <summary>
                /// EIA Natural Gas Stocks Change
                /// </summary>
                public const string EiaNaturalGasStocksChange = "eia natural gas stocks change";
                /// <summary>
                /// EIA Refinery Crude Runs Change
                /// </summary>
                public const string EiaRefineryCrudeRunsChange = "eia refinery crude runs change";
                /// <summary>
                /// 8-Week Bill Auction
                /// </summary>
                public const string EightWeekBillAuction = "8 week bill auction";
                /// <summary>
                /// Employment Benefits QoQ
                /// </summary>
                public const string EmploymentBenefitsQoQ = "employment benefits qoq";
                /// <summary>
                /// Employment Cost - Benefits QoQ
                /// </summary>
                public const string EmploymentCostsBenefitsQoQ = "employment costs benefits qoq";
                /// <summary>
                /// Employment Cost Index QoQ
                /// </summary>
                public const string EmploymentCostsIndexQoQ = "employment costs index qoq";
                /// <summary>
                /// Employment Cost - Wages QoQ
                /// </summary>
                public const string EmploymentCostsWagesQoQ = "employment costs wages qoq";
                /// <summary>
                /// Employment Wages QoQ
                /// </summary>
                public const string EmploymentWagesQoQ = "employment wages qoq";
                /// <summary>
                /// Existing Home Sales
                /// </summary>
                public const string ExistingHomeSales = "existing home sales";
                /// <summary>
                /// Existing Home Sales MoM
                /// </summary>
                public const string ExistingHomeSalesMoM = "existing home sales mom";
                /// <summary>
                /// Export Prices MoM
                /// </summary>
                public const string ExportPricesMoM = "export prices mom";
                /// <summary>
                /// Export Prices YoY
                /// </summary>
                public const string ExportPricesYoY = "export prices yoy";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "exports";
                /// <summary>
                /// Factory Orders ex Transportation
                /// </summary>
                public const string FactoryOrdersExcludingTransportation = "factory orders excluding transportation";
                /// <summary>
                /// Factory Orders MoM
                /// </summary>
                public const string FactoryOrdersMoM = "factory orders mom";
                /// <summary>
                /// Fed Barkin Speech
                /// </summary>
                public const string FedBarkinSpeech = "fed barkin speech";
                /// <summary>
                /// Fed Beige Book
                /// </summary>
                public const string FedBeigeBook = "fed beige book";
                /// <summary>
                /// Fed Bernanke Speech
                /// </summary>
                public const string FedBernankeSpeech = "fed bernanke speech";
                /// <summary>
                /// Fed Bernanke testifies
                /// </summary>
                public const string FedBernankeTestifies = "fed bernanke testifies";
                /// <summary>
                /// Fed Bostic Speech
                /// </summary>
                public const string FedBosticSpeech = "fed bostic speech";
                /// <summary>
                /// Fed Bowman Speech
                /// </summary>
                public const string FedBowmanSpeech = "fed bowman speech";
                /// <summary>
                /// Fed Bowman Testimony
                /// </summary>
                public const string FedBowmanTestimony = "fed bowman testimony";
                /// <summary>
                /// Fed Brainard Speaks
                /// </summary>
                public const string FedBrainardSpeaks = "fed brainard speaks";
                /// <summary>
                /// Fed Brainard Speech
                /// </summary>
                public const string FedBrainardSpeech = "fed brainard speech";
                /// <summary>
                /// Fed Brainard Testimony
                /// </summary>
                public const string FedBrainardTestimony = "fed brainard testimony";
                /// <summary>
                /// Fed Bullard Speaks
                /// </summary>
                public const string FedBullardSpeaks = "fed bullard speaks";
                /// <summary>
                /// Fed Bullard Speech
                /// </summary>
                public const string FedBullardSpeech = "fed bullard speech";
                /// <summary>
                /// Fed CCAR For Big Banks
                /// </summary>
                public const string FedCcarForBigBanks = "fed ccar for big banks";
                /// <summary>
                /// Fed CCAR Results For Big Banks
                /// </summary>
                public const string FedCcarResultsForBigBanks = "fed ccar results for big banks";
                /// <summary>
                /// Fed Chairman Bernanke Speaks
                /// </summary>
                public const string FedChairmanBernankeSpeaks = "fed chairman bernanke speaks";
                /// <summary>
                /// Fed Chairman Bernanke Speech
                /// </summary>
                public const string FedChairmanBernankeSpeech = "fed chairman bernanke speech";
                /// <summary>
                /// Fed Chairman Bernanke Testifies
                /// </summary>
                public const string FedChairmanBernankeTestifies = "fed chairman bernanke testifies";
                /// <summary>
                /// Fed Chairman Nomination Vote
                /// </summary>
                public const string FedChairmanNominationVote = "fed chairman nomination vote";
                /// <summary>
                /// Fed Chairman Yellen Speaks
                /// </summary>
                public const string FedChairmanYellenSpeaks = "fed chairman yellen speaks";
                /// <summary>
                /// Fed Chair Powell Speech
                /// </summary>
                public const string FedChairPowellSpeech = "fed chair powell speech";
                /// <summary>
                /// Fed Chair Powell Testimony
                /// </summary>
                public const string FedChairPowellTestimony = "fed chair powell testimony";
                /// <summary>
                /// Fed Chair Yellen Speaks
                /// </summary>
                public const string FedChairYellenSpeaks = "fed chair yellen speaks";
                /// <summary>
                /// Fed Clarida Speech
                /// </summary>
                public const string FedClaridaSpeech = "fed clarida speech";
                /// <summary>
                /// Fed Daly Speech
                /// </summary>
                public const string FedDalySpeech = "fed daly speech";
                /// <summary>
                /// Fed Dudley Speech
                /// </summary>
                public const string FedDudleySpeech = "fed dudley speech";
                /// <summary>
                /// Fed Duke Speech
                /// </summary>
                public const string FedDukeSpeech = "fed duke speech";
                /// <summary>
                /// Federal Budget Balance
                /// </summary>
                public const string FederalBudgetBalance = "federal budget balance";
                /// <summary>
                /// Fed Evans Speaks
                /// </summary>
                public const string FedEvansSpeaks = "fed evans speaks";
                /// <summary>
                /// Fed Evans Speech
                /// </summary>
                public const string FedEvansSpeech = "fed evans speech";
                /// <summary>
                /// Fed Fischer Speech
                /// </summary>
                public const string FedFischerSpeech = "fed fischer speech";
                /// <summary>
                /// Fed George Speech
                /// </summary>
                public const string FedGeorgeSpeech = "fed george speech";
                /// <summary>
                /// Fed George Testimony
                /// </summary>
                public const string FedGeorgeTestimony = "fed george testimony";
                /// <summary>
                /// Fed Harker, Mester & Lockhart Speech
                /// </summary>
                public const string FedHarkerMesterAndLockhartSpeech = "fed harker mester and lockhart speech";
                /// <summary>
                /// Fed Harker Speech
                /// </summary>
                public const string FedHarkerSpeech = "fed harker speech";
                /// <summary>
                /// Fed Interest Rate Decision
                /// </summary>
                public const string FedInterestRateDecision = "fed interest rate decision";
                /// <summary>
                /// Fed Kaplan Speech
                /// </summary>
                public const string FedKaplanSpeech = "fed kaplan speech";
                /// <summary>
                /// Fed Kashkari Speech
                /// </summary>
                public const string FedKashkariSpeech = "fed kashkari speech";
                /// <summary>
                /// Fed Kocherlakota Speech
                /// </summary>
                public const string FedKocherlakotaSpeech = "fed kocherlakota speech";
                /// <summary>
                /// Fed Labor Market Conditions Index (MoM)
                /// </summary>
                public const string FedLaborMarketConditionsIndexMoM = "fed labor market conditions index mom";
                /// <summary>
                /// Fed Lacker Speech
                /// </summary>
                public const string FedLackerSpeech = "fed lacker speech";
                /// <summary>
                /// Fed Lockhart speech
                /// </summary>
                public const string FedLockhartSpeech = "fed lockhart speech";
                /// <summary>
                /// Fed Meeting under Expedited Procedures
                /// </summary>
                public const string FedMeetingUnderExpeditedProcedures = "fed meeting under expedited procedures";
                /// <summary>
                /// Fed Mester Speech
                /// </summary>
                public const string FedMesterSpeech = "fed mester speech";
                /// <summary>
                /// Fed Monetary Policy Statement and press conference
                /// </summary>
                public const string FedMonetaryPolicyStatementAndPressConference = "fed monetary policy statement and press conference";
                /// <summary>
                /// Fed Open Board Meeting
                /// </summary>
                public const string FedOpenBoardMeeting = "fed open board meeting";
                /// <summary>
                /// Fed Pace of MBS Purchase Program
                /// </summary>
                public const string FedPaceOfMortgageBackedSecuritiesPurchaseProgram = "fed pace of mortgage backed securities purchase program";
                /// <summary>
                /// Fed Pace of Treasury Purchase Program
                /// </summary>
                public const string FedPaceOfTreasuryPurchaseProgram = "fed pace of treasury purchase program";
                /// <summary>
                /// Fed Powell Speech
                /// </summary>
                public const string FedPowellSpeech = "fed powell speech";
                /// <summary>
                /// Fed press conference
                /// </summary>
                public const string FedPressConference = "fed press conference";
                /// <summary>
                /// Fed Quarles Speech
                /// </summary>
                public const string FedQuarlesSpeech = "fed quarles speech";
                /// <summary>
                /// Fed Quarles Testimony
                /// </summary>
                public const string FedQuarlesTestimony = "fed quarles testimony";
                /// <summary>
                /// Fed Richard Fisher speech
                /// </summary>
                public const string FedRichardFisherSpeech = "fed richard fisher speech";
                /// <summary>
                /// Fed Rosengren Speech
                /// </summary>
                public const string FedRosengrenSpeech = "fed rosengren speech";
                /// <summary>
                /// FED´s Beige Book
                /// </summary>
                public const string FedsBeigeBook = "feds beige book";
                /// <summary>
                /// Fed Stress Test Results For Big Banks
                /// </summary>
                public const string FedStressTestResultsForBigBanks = "fed stress test results for big banks";
                /// <summary>
                /// Fed Tarullo Speech
                /// </summary>
                public const string FedTarulloSpeech = "fed tarullo speech";
                /// <summary>
                /// Fed Tarullo Testifies
                /// </summary>
                public const string FedTarulloTestifies = "fed tarullo testifies";
                /// <summary>
                /// Fed Williams Speech
                /// </summary>
                public const string FedWilliamsSpeech = "fed williams speech";
                /// <summary>
                /// Fed Yellen & IMF's Lagarde Speech
                /// </summary>
                public const string FedYellenAndImfsLagardeSpeech = "fed yellen and imfs lagarde speech";
                /// <summary>
                /// Fed Yellen Speaks
                /// </summary>
                public const string FedYellenSpeaks = "fed yellen speaks";
                /// <summary>
                /// Fed Yellen Speaks At Jackson Hole
                /// </summary>
                public const string FedYellenSpeaksAtJacksonHole = "fed yellen speaks at jackson hole";
                /// <summary>
                /// Fed Yellen Speech
                /// </summary>
                public const string FedYellenSpeech = "fed yellen speech";
                /// <summary>
                /// Fed Yellen testifies
                /// </summary>
                public const string FedYellenTestifies = "fed yellen testifies";
                /// <summary>
                /// Fed Yellen Testimony
                /// </summary>
                public const string FedYellenTestimony = "fed yellen testimony";
                /// <summary>
                /// Fed Yellen Testimony Before Congress
                /// </summary>
                public const string FedYellenTestimonyBeforeCongress = "fed yellen testimony before congress";
                /// <summary>
                /// Fed Yellen Testimony HFSC
                /// </summary>
                public const string FedYellenTestimonyHfsc = "fed yellen testimony hfsc";
                /// <summary>
                /// Fed Yellen Testimony SBC
                /// </summary>
                public const string FedYellenTestimonySbc = "fed yellen testimony sbc";
                /// <summary>
                /// Fed Yellen Testimony to the Congress
                /// </summary>
                public const string FedYellenTestimonyToTheCongress = "fed yellen testimony to the congress";
                /// <summary>
                /// Fed Yellen Testimony to the Senate
                /// </summary>
                public const string FedYellenTestimonyToTheSenate = "fed yellen testimony to the senate";
                /// <summary>
                /// Fed Yellen Testymony - House
                /// </summary>
                public const string FedYellenTestymonyHouse = "fed yellen testymony house";
                /// <summary>
                /// Fed Yellen Testymony - Senate
                /// </summary>
                public const string FedYellenTestymonySenate = "fed yellen testymony senate";
                /// <summary>
                /// 52-Week Bill Auction
                /// </summary>
                public const string FiftyTwoWeekBillAuction = "52 week bill auction";
                /// <summary>
                /// Financial Accounts of the US
                /// </summary>
                public const string FinancialAccountsOfTheUs = "financial accounts of the us";
                /// <summary>
                /// 5-Year Note Auction
                /// </summary>
                public const string FiveYearNoteAuction = "5 year note auction";
                /// <summary>
                /// 5-Year TIPS Auction
                /// </summary>
                public const string FiveYearTipsAuction = "5 year tips auction";
                /// <summary>
                /// FOMC Economic Projections
                /// </summary>
                public const string FomcEconomicProjections = "fomc economic projections";
                /// <summary>
                /// FOMC Member Yellen Speech
                /// </summary>
                public const string FomcMemberYellenSpeech = "fomc member yellen speech";
                /// <summary>
                /// FOMC Minutes
                /// </summary>
                public const string FomcMinutes = "fomc minutes";
                /// <summary>
                /// FOMC Policy Statement and press conference
                /// </summary>
                public const string FomcPolicyStatementAndPressConference = "fomc policy statement and press conference";
                /// <summary>
                /// FOMC Press Conference
                /// </summary>
                public const string FomcPressConference = "fomc press conference";
                /// <summary>
                /// Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "foreign bond investment";
                /// <summary>
                /// 4-Week Bill Auction
                /// </summary>
                public const string FourWeekBillAuction = "4 week bill auction";
                /// <summary>
                /// Gasoline Inventories
                /// </summary>
                public const string GasolineInventories = "gasoline inventories";
                /// <summary>
                /// GDP Consumer Spending QoQ Adv
                /// </summary>
                public const string GdpConsumerSpendingQoQAdv = "gdp consumer spending qoq adv";
                /// <summary>
                /// GDP Consumer Spending QoQ Final
                /// </summary>
                public const string GdpConsumerSpendingQoQFinal = "gdp consumer spending qoq final";
                /// <summary>
                /// GDP Consumer Spending YoY
                /// </summary>
                public const string GdpConsumerSpendingYoY = "gdp consumer spending yoy";
                /// <summary>
                /// GDP Deflator QoQ
                /// </summary>
                public const string GdpDeflatorQoQ = "gdp deflator qoq";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "gdp growth rate";
                /// <summary>
                /// GDP Growth Rate QoQ Adv
                /// </summary>
                public const string GdpGrowthRateQoQAdv = "gdp growth rate qoq adv";
                /// <summary>
                /// GDP Growth Rate QoQ Final
                /// </summary>
                public const string GdpGrowthRateQoQFinal = "gdp growth rate qoq final";
                /// <summary>
                /// GDP Growth Rate QoQ 2nd Est
                /// </summary>
                public const string GdpGrowthRateQoQSecondEstimate = "gdp growth rate qoq second estimate";
                /// <summary>
                /// GDP Growth Rate 2nd Est
                /// </summary>
                public const string GdpGrowthRateSecondEstimate = "gdp growth rate second estimate";
                /// <summary>
                /// GDP Growth Rate YoY Adv
                /// </summary>
                public const string GdpGrowthRateYoYAdv = "gdp growth rate yoy adv";
                /// <summary>
                /// GDP Growth Rate YoY Second Est
                /// </summary>
                public const string GdpGrowthRateYoYSecondEstimate = "gdp growth rate yoy second estimate";
                /// <summary>
                /// GDP Growth Rate YoY Third Estimate
                /// </summary>
                public const string GdpGrowthRateYoYThirdEstimate = "gdp growth rate yoy third estimate";
                /// <summary>
                /// GDP Price Index
                /// </summary>
                public const string GdpPriceIndex = "gdp price index";
                /// <summary>
                /// GDP Price Index QoQ
                /// </summary>
                public const string GdpPriceIndexQoQ = "gdp price index qoq";
                /// <summary>
                /// GDP Price Index QoQ Adv
                /// </summary>
                public const string GdpPriceIndexQoQAdv = "gdp price index qoq adv";
                /// <summary>
                /// GDP Price Index QoQ Final
                /// </summary>
                public const string GdpPriceIndexQoQFinal = "gdp price index qoq final";
                /// <summary>
                /// GDP Price Index QoQ 2nd Est
                /// </summary>
                public const string GdpPriceIndexQoQSecondEstimate = "gdp price index qoq second estimate";
                /// <summary>
                /// Good Friday
                /// </summary>
                public const string GoodFriday = "good friday";
                /// <summary>
                /// Goods Trade Balance
                /// </summary>
                public const string GoodsTradeBalance = "goods trade balance";
                /// <summary>
                /// Goods Trade Balance Adv
                /// </summary>
                public const string GoodsTradeBalanceAdv = "goods trade balance adv";
                /// <summary>
                /// Government Payrolls
                /// </summary>
                public const string GovernmentPayrolls = "government payrolls";
                /// <summary>
                /// Gross Domestic Product Price Index
                /// </summary>
                public const string GrossDomesticProductPriceIndex = "gross domestic product price index";
                /// <summary>
                /// G30 International Banking Seminar
                /// </summary>
                public const string GThirtyInternationalBankingSeminar = "g30 international banking seminar";
                /// <summary>
                /// G20 Finance Ministers and CB Governors Meeting
                /// </summary>
                public const string GTwentyFinanceMinistersAndCbGovernorsMeeting = "g20 finance ministers and cb governors meeting";
                /// <summary>
                /// G20 Fin Ministers and CB Governors Meeting
                /// </summary>
                public const string GTwentyFinMinistersAndCbGovernorsMeeting = "g20 fin ministers and cb governors meeting";
                /// <summary>
                /// G20 Meeting
                /// </summary>
                public const string GTwentyMeeting = "g20 meeting";
                /// <summary>
                /// House Price Index MoM
                /// </summary>
                public const string HousePriceIndexMoM = "house price index mom";
                /// <summary>
                /// House Price Index YoY
                /// </summary>
                public const string HousePriceIndexYoY = "house price index yoy";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "housing starts";
                /// <summary>
                /// Housing Starts MoM
                /// </summary>
                public const string HousingStartsMoM = "housing starts mom";
                /// <summary>
                /// HSBC Services PMI
                /// </summary>
                public const string HsbcServicesPurchasingManagersIndex = "hsbc services purchasing managers index";
                /// <summary>
                /// IBD/TIPP Economic Optimism
                /// </summary>
                public const string IbdTippEconomicOptimism = "ibd tipp economic optimism";
                /// <summary>
                /// IEA Oil Market Report
                /// </summary>
                public const string IeaOilMarketReport = "iea oil market report";
                /// <summary>
                /// IMF Meeting
                /// </summary>
                public const string ImfMeeting = "imf meeting";
                /// <summary>
                /// IMF Spring Meeting
                /// </summary>
                public const string ImfSpringMeeting = "imf spring meeting";
                /// <summary>
                /// IMF/World Bank Annual Meetings
                /// </summary>
                public const string ImfWorldBankAnnualMeetings = "imf world bank annual meetings";
                /// <summary>
                /// Import Prices MoM
                /// </summary>
                public const string ImportPricesMoM = "import prices mom";
                /// <summary>
                /// Import Prices YoY
                /// </summary>
                public const string ImportPricesYoY = "import prices yoy";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "imports";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "independence day";
                /// <summary>
                /// Independence Day Observed
                /// </summary>
                public const string IndependenceDayObserved = "independence day observed";
                /// <summary>
                /// Industrial Production MoM
                /// </summary>
                public const string IndustrialProductionMoM = "industrial production mom";
                /// <summary>
                /// Industrial Production YoY
                /// </summary>
                public const string IndustrialProductionYoY = "industrial production yoy";
                /// <summary>
                /// Inflation Rate MoM
                /// </summary>
                public const string InflationRateMoM = "inflation rate mom";
                /// <summary>
                /// Inflation Rate YoY
                /// </summary>
                public const string InflationRateYoY = "inflation rate yoy";
                /// <summary>
                /// Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "initial jobless claims";
                /// <summary>
                /// ISM Manufacturing Employment
                /// </summary>
                public const string IsmManufacturingEmployment = "ism manufacturing employment";
                /// <summary>
                /// ISM Manufacturing New Orders
                /// </summary>
                public const string IsmManufacturingNewOrders = "ism manufacturing new orders";
                /// <summary>
                /// ISM Manufacturing Prices
                /// </summary>
                public const string IsmManufacturingPrices = "ism manufacturing prices";
                /// <summary>
                /// ISM Manufacturing Prices Paid
                /// </summary>
                public const string IsmManufacturingPricesPaid = "ism manufacturing prices paid";
                /// <summary>
                /// ISM Manufacturing PMI
                /// </summary>
                public const string IsmManufacturingPurchasingManagersIndex = "ism manufacturing purchasing managers index";
                /// <summary>
                /// ISM New York index
                /// </summary>
                public const string IsmNewYorkIndex = "ism new york index";
                /// <summary>
                /// ISM Non-Manufacturing Business Activity
                /// </summary>
                public const string IsmNonManufacturingBusinessActivity = "ism non manufacturing business activity";
                /// <summary>
                /// ISM Non-Manufacturing Employment
                /// </summary>
                public const string IsmNonManufacturingEmployment = "ism non manufacturing employment";
                /// <summary>
                /// ISM Non-Manufacturing New Orders
                /// </summary>
                public const string IsmNonManufacturingNewOrders = "ism non manufacturing new orders";
                /// <summary>
                /// ISM Non-Manufacturing Prices
                /// </summary>
                public const string IsmNonManufacturingPrices = "ism non manufacturing prices";
                /// <summary>
                /// ISM Non-Manufacturing PMI
                /// </summary>
                public const string IsmNonManufacturingPurchasingManagersIndex = "ism non manufacturing purchasing managers index";
                /// <summary>
                /// ISM Prices Paid
                /// </summary>
                public const string IsmPricesPaid = "ism prices paid";
                /// <summary>
                /// Jackson Hole Economic Policy Symposium
                /// </summary>
                public const string JacksonHoleEconomicPolicySymposium = "jackson hole economic policy symposium";
                /// <summary>
                /// Jackson Hole Symposium
                /// </summary>
                public const string JacksonHoleSymposium = "jackson hole symposium";
                /// <summary>
                /// JOLTs Job Openings
                /// </summary>
                public const string JoltsJobOpenings = "jolts job openings";
                /// <summary>
                /// Kansas Fed Manufacturing Index
                /// </summary>
                public const string KansasFedManufacturingIndex = "kansas fed manufacturing index";
                /// <summary>
                /// Labor Costs QoQ
                /// </summary>
                public const string LaborCostsIndexQoQ = "labor costs index qoq";
                /// <summary>
                /// Labor Day
                /// </summary>
                public const string LaborDay = "labor day";
                /// <summary>
                /// Loan Officer Survey
                /// </summary>
                public const string LoanOfficerSurvey = "loan officer survey";
                /// <summary>
                /// Manufacturing Payrolls
                /// </summary>
                public const string ManufacturingPayrolls = "manufacturing payrolls";
                /// <summary>
                /// Manufacturing Production MoM
                /// </summary>
                public const string ManufacturingProductionMoM = "manufacturing production mom";
                /// <summary>
                /// Manufacturing Production YoY
                /// </summary>
                public const string ManufacturingProductionYoY = "manufacturing production yoy";
                /// <summary>
                /// Markit Composite PMI Final
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndexFinal = "markit composite purchasing managers index final";
                /// <summary>
                /// Markit Composite PMI Flash
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndexFlash = "markit composite purchasing managers index flash";
                /// <summary>
                /// Markit Manufacturing PMI
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Manufacturing PMI Final
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndexFinal = "markit manufacturing purchasing managers index final";
                /// <summary>
                /// Markit Manufacturing PMI Flash
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndexFlash = "markit manufacturing purchasing managers index flash";
                /// <summary>
                /// Markit Services PMI Final
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndexFinal = "markit services purchasing managers index final";
                /// <summary>
                /// Markit Services PMI Flash
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndexFlash = "markit services purchasing managers index flash";
                /// <summary>
                /// Markit Services PMI Prel
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndexPreliminary = "markit services purchasing managers index preliminary";
                /// <summary>
                /// Martin L. King Day
                /// </summary>
                public const string MartinLKingDay = "martin l king day";
                /// <summary>
                /// Martin L. King's Birthday
                /// </summary>
                public const string MartinLKingsBirthday = "martin l kings birthday";
                /// <summary>
                /// Martin Luther King, Jr. Day
                /// </summary>
                public const string MartinLutherKingJrDay = "martin luther king jr day";
                /// <summary>
                /// Mass Layoffs
                /// </summary>
                public const string MassLayoffs = "mass layoffs";
                /// <summary>
                /// MBA Mortgage Applications
                /// </summary>
                public const string MbaMortgageApplications = "mba mortgage applications";
                /// <summary>
                /// MBA Mortgage Applications WoW
                /// </summary>
                public const string MbaMortgageApplicationsWoW = "mba mortgage applications wow";
                /// <summary>
                /// MBA 30-Year Mortgage Rate
                /// </summary>
                public const string MbaThirtyYearMortgageRate = "mba 30 year mortgage rate";
                /// <summary>
                /// Memorial Day
                /// </summary>
                public const string MemorialDay = "memorial day";
                /// <summary>
                /// Memorial Day Holiday
                /// </summary>
                public const string MemorialDayHoliday = "memorial day holiday";
                /// <summary>
                /// Michigan Consumer Expectations Final
                /// </summary>
                public const string MichiganConsumerExpectationsFinal = "michigan consumer expectations final";
                /// <summary>
                /// Michigan Consumer Expectations Prel
                /// </summary>
                public const string MichiganConsumerExpectationsPreliminary = "michigan consumer expectations preliminary";
                /// <summary>
                /// Michigan Consumer Sentiment Final
                /// </summary>
                public const string MichiganConsumerSentimentFinal = "michigan consumer sentiment final";
                /// <summary>
                /// Michigan Consumer Sentiment Prel
                /// </summary>
                public const string MichiganConsumerSentimentPreliminary = "michigan consumer sentiment preliminary";
                /// <summary>
                /// Michigan Current Conditions Final
                /// </summary>
                public const string MichiganCurrentConditionsFinal = "michigan current conditions final";
                /// <summary>
                /// Michigan Current Conditions Prel
                /// </summary>
                public const string MichiganCurrentConditionsPreliminary = "michigan current conditions preliminary";
                /// <summary>
                /// Michigan 5 Year Inflation Expectations Final
                /// </summary>
                public const string MichiganFiveYearInflationExpectationsFinal = "michigan 5 year inflation expectations final";
                /// <summary>
                /// Michigan 5 Year Inflation Expectations Prel
                /// </summary>
                public const string MichiganFiveYearInflationExpectationsPreliminary = "michigan 5 year inflation expectations preliminary";
                /// <summary>
                /// Michigan Inflation Expectations Final
                /// </summary>
                public const string MichiganInflationExpectationsFinal = "michigan inflation expectations final";
                /// <summary>
                /// Michigan Inflation Expectations Prel
                /// </summary>
                public const string MichiganInflationExpectationsPreliminary = "michigan inflation expectations preliminary";
                /// <summary>
                /// Midterm Elections
                /// </summary>
                public const string MidtermElections = "midterm elections";
                /// <summary>
                /// Mid-Term Elections
                /// </summary>
                public const string MidTermElections = "mid term elections";
                /// <summary>
                /// Monetary Policy Report
                /// </summary>
                public const string MonetaryPolicyReport = "monetary policy report";
                /// <summary>
                /// Monthly Budget Statement
                /// </summary>
                public const string MonthlyBudgetStatement = "monthly budget statement";
                /// <summary>
                /// NAHB Housing Market Index
                /// </summary>
                public const string NahbHousingMarketIndex = "nahb housing market index";
                /// <summary>
                /// National activity index
                /// </summary>
                public const string NationalActivityIndex = "national activity index";
                /// <summary>
                /// Net Long-Term Tic Flows
                /// </summary>
                public const string NetLongTermTicFlows = "net long term tic flows";
                /// <summary>
                /// New Home Sales
                /// </summary>
                public const string NewHomeSales = "new home sales";
                /// <summary>
                /// New Home Sales MoM
                /// </summary>
                public const string NewHomeSalesMoM = "new home sales mom";
                /// <summary>
                /// New Year's Day
                /// </summary>
                public const string NewYearsDay = "new years day";
                /// <summary>
                /// New Year's Day (Substitute Day)
                /// </summary>
                public const string NewYearsDaySubstituteDay = "new years day substitute day";
                /// <summary>
                /// NFIB Business Optimism Index
                /// </summary>
                public const string NfibBusinessOptimismIndex = "nfib business optimism index";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "non farm payrolls";
                /// <summary>
                /// Nonfarm Payrolls Private
                /// </summary>
                public const string NonFarmPayrollsPrivate = "non farm payrolls private";
                /// <summary>
                /// Nonfarm Productivity QoQ
                /// </summary>
                public const string NonFarmProductivityQoQ = "non farm productivity qoq";
                /// <summary>
                /// Nonfarm Productivity QoQ Final
                /// </summary>
                public const string NonFarmProductivityQoQFinal = "non farm productivity qoq final";
                /// <summary>
                /// Nonfarm Productivity QoQ Prel
                /// </summary>
                public const string NonFarmProductivityQoQPreliminary = "non farm productivity qoq preliminary";
                /// <summary>
                /// Nonfarm Productivity YoY
                /// </summary>
                public const string NonFarmProductivityYoY = "non farm productivity yoy";
                /// <summary>
                /// Non-Manufacturing Business Activity
                /// </summary>
                public const string NonManufacturingBusinessActivity = "non manufacturing business activity";
                /// <summary>
                /// NY Empire State Manufacturing Index
                /// </summary>
                public const string NyEmpireStateManufacturingIndex = "ny empire state manufacturing index";
                /// <summary>
                /// Obamacare Repeal Vote - Pulled
                /// </summary>
                public const string ObamacareRepealVotePulled = "obamacare repeal vote pulled";
                /// <summary>
                /// Overall Net Capital Flows
                /// </summary>
                public const string OverallNetCapitalFlows = "overall net capital flows";
                /// <summary>
                /// Participation Rate
                /// </summary>
                public const string ParticipationRate = "participation rate";
                /// <summary>
                /// Pending Home Sales MoM
                /// </summary>
                public const string PendingHomeSalesMoM = "pending home sales mom";
                /// <summary>
                /// Pending Home Sales YoY
                /// </summary>
                public const string PendingHomeSalesYoY = "pending home sales yoy";
                /// <summary>
                /// PCE Price Index MoM
                /// </summary>
                public const string PersonalConsumptionExpenditurePriceIndexMoM = "personal consumption expenditure price index mom";
                /// <summary>
                /// PCE Prices QoQ
                /// </summary>
                public const string PersonalConsumptionExpenditurePriceIndexQoQ = "personal consumption expenditure price index qoq";
                /// <summary>
                /// PCE Prices QoQ Adv
                /// </summary>
                public const string PersonalConsumptionExpenditurePriceIndexQoQAdv = "personal consumption expenditure price index qoq adv";
                /// <summary>
                /// PCE Prices QoQ Final
                /// </summary>
                public const string PersonalConsumptionExpenditurePriceIndexQoQFinal = "personal consumption expenditure price index qoq final";
                /// <summary>
                /// PCE Prices QoQ 2 Est
                /// </summary>
                public const string PersonalConsumptionExpenditurePriceIndexQoQSecondEstimate = "personal consumption expenditure price index qoq second estimate";
                /// <summary>
                /// PCE Price Index YoY
                /// </summary>
                public const string PersonalConsumptionExpenditurePriceIndexYoY = "personal consumption expenditure price index yoy";
                /// <summary>
                /// Personal Income (MoM)
                /// </summary>
                public const string PersonalIncomeMoM = "personal income mom";
                /// <summary>
                /// Personal Spending MoM
                /// </summary>
                public const string PersonalSpendingMoM = "personal spending mom";
                /// <summary>
                /// Philadelphia Fed Manufacturing Index
                /// </summary>
                public const string PhiladelphiaFedManufacturingIndex = "philadelphia fed manufacturing index";
                /// <summary>
                /// Philadelphia Fed Plosser speech
                /// </summary>
                public const string PhiladelphiaFedPlosserSpeech = "philadelphia fed plosser speech";
                /// <summary>
                /// President-Elect Trump Speech
                /// </summary>
                public const string PresidentElectTrumpSpeech = "president elect trump speech";
                /// <summary>
                /// Presidential Election Results
                /// </summary>
                public const string PresidentialElectionResults = "presidential election results";
                /// <summary>
                /// Presidential Elections
                /// </summary>
                public const string PresidentialElections = "presidential elections";
                /// <summary>
                /// President Obama Speech
                /// </summary>
                public const string PresidentObamaSpeech = "president obama speech";
                /// <summary>
                /// President Obama Statement on the Economy
                /// </summary>
                public const string PresidentObamaStatementOnTheEconomy = "president obama statement on the economy";
                /// <summary>
                /// President's Day
                /// </summary>
                public const string PresidentsDay = "presidents day";
                /// <summary>
                /// President Trump and President Xi Jinping Meeting
                /// </summary>
                public const string PresidentTrumpAndPresidentXiJinpingMeeting = "president trump and president xi jinping meeting";
                /// <summary>
                /// President Trump Speech
                /// </summary>
                public const string PresidentTrumpSpeech = "president trump speech";
                /// <summary>
                /// President Trump Speech on Iran Deal
                /// </summary>
                public const string PresidentTrumpSpeechOnIranDeal = "president trump speech on iran deal";
                /// <summary>
                /// President Trump State of the Union Speech
                /// </summary>
                public const string PresidentTrumpStateOfTheUnionSpeech = "president trump state of the union speech";
                /// <summary>
                /// President Trump tax speech
                /// </summary>
                public const string PresidentTrumpTaxSpeech = "president trump tax speech";
                /// <summary>
                /// Producer Price Index ex Food & Energy (MoM)
                /// </summary>
                public const string ProducerPriceIndexExcludingFoodAndEnergyMoM = "producer price index excluding food and energy mom";
                /// <summary>
                /// Producer Price Index ex Food & Energy (YoY)
                /// </summary>
                public const string ProducerPriceIndexExcludingFoodAndEnergyYoY = "producer price index excluding food and energy yoy";
                /// <summary>
                /// PPI MoM
                /// </summary>
                public const string ProducerPriceIndexMoM = "producer price index mom";
                /// <summary>
                /// PPI YoY
                /// </summary>
                public const string ProducerPriceIndexYoY = "producer price index yoy";
                /// <summary>
                /// QE MBS
                /// </summary>
                public const string QuantitativeEasingMortgageBackedSecurities = "quantitative easing mortgage backed securities";
                /// <summary>
                /// QE total
                /// </summary>
                public const string QuantitativeEasingTotal = "quantitative easing total";
                /// <summary>
                /// QE Treasuries
                /// </summary>
                public const string QuantitativeEasingTreasuries = "quantitative easing treasuries";
                /// <summary>
                /// Real Consumer Spending
                /// </summary>
                public const string RealConsumerSpending = "real consumer spending";
                /// <summary>
                /// Redbook MoM
                /// </summary>
                public const string RedbookMoM = "redbook mom";
                /// <summary>
                /// Redbook YoY
                /// </summary>
                public const string RedbookYoY = "redbook yoy";
                /// <summary>
                /// Retail Sales Ex Autos MoM
                /// </summary>
                public const string RetailSalesExcludingAutosMoM = "retail sales excluding autos mom";
                /// <summary>
                /// Retail Sales Ex Gas/Autos  MoM
                /// </summary>
                public const string RetailSalesExcludingGasAutosMoM = "retail sales excluding gas autos mom";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMoM = "retail sales mom";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoY = "retail sales yoy";
                /// <summary>
                /// Reuters Michigan Consumer Expectations
                /// </summary>
                public const string ReutersMichiganConsumerExpectations = "reuters michigan consumer expectations";
                /// <summary>
                /// Reuters Michigan Consumer Expectations Final
                /// </summary>
                public const string ReutersMichiganConsumerExpectationsFinal = "reuters michigan consumer expectations final";
                /// <summary>
                /// Reuters Michigan Consumer Expectations Prel
                /// </summary>
                public const string ReutersMichiganConsumerExpectationsPreliminary = "reuters michigan consumer expectations preliminary";
                /// <summary>
                /// Reuters Michigan Consumer Sentiment Final
                /// </summary>
                public const string ReutersMichiganConsumerSentimentFinal = "reuters michigan consumer sentiment final";
                /// <summary>
                /// Reuters/Michigan Consumer Sentiment Index
                /// </summary>
                public const string ReutersMichiganConsumerSentimentIndex = "reuters michigan consumer sentiment index";
                /// <summary>
                /// Reuters Michigan Consumer Sentiment  Prel
                /// </summary>
                public const string ReutersMichiganConsumerSentimentPreliminary = "reuters michigan consumer sentiment preliminary";
                /// <summary>
                /// Reuters Michigan Current Conditions
                /// </summary>
                public const string ReutersMichiganCurrentConditions = "reuters michigan current conditions";
                /// <summary>
                /// Reuters Michigan Current Conditions Final
                /// </summary>
                public const string ReutersMichiganCurrentConditionsFinal = "reuters michigan current conditions final";
                /// <summary>
                /// Reuters Michigan Current Conditions Prel
                /// </summary>
                public const string ReutersMichiganCurrentConditionsPreliminary = "reuters michigan current conditions preliminary";
                /// <summary>
                /// Reuters Michigan Inflation Expectations Prel
                /// </summary>
                public const string ReutersMichiganInflationExpectationsPreliminary = "reuters michigan inflation expectations preliminary";
                /// <summary>
                /// Richmond Fed Manufacturing Index
                /// </summary>
                public const string RichmondFedManufacturingIndex = "richmond fed manufacturing index";
                /// <summary>
                /// S&P/Case-Shiller Home Price MoM
                /// </summary>
                public const string SandpCaseShillerHomePriceMoM = "sandp case shiller home price mom";
                /// <summary>
                /// S&P/Case-Shiller Home Price YoY
                /// </summary>
                public const string SandpCaseShillerHomePriceYoY = "sandp case shiller home price yoy";
                /// <summary>
                /// 7-Year Note Auction
                /// </summary>
                public const string SevenYearNoteAuction = "7 year note auction";
                /// <summary>
                /// 6-Month Bill Auction
                /// </summary>
                public const string SixMonthBillAuction = "6 month bill auction";
                /// <summary>
                /// Spring Meeting of the World Bank Group-IMF
                /// </summary>
                public const string SpringMeetingOfTheWorldBankGroupImf = "spring meeting of the world bank group imf";
                /// <summary>
                /// 10-Year Note Auction
                /// </summary>
                public const string TenYearNoteAuction = "10 year note auction";
                /// <summary>
                /// 10-Year TIPS Auction
                /// </summary>
                public const string TenYearTipsAuction = "10 year tips auction";
                /// <summary>
                /// Thanksgiving Day
                /// </summary>
                public const string ThanksgivingDay = "thanksgiving day";
                /// <summary>
                /// 30-Year Bond Auction
                /// </summary>
                public const string ThirtyYearBondAuction = "30 year bond auction";
                /// <summary>
                /// 30-Year Note Auction
                /// </summary>
                public const string ThirtyYearNoteAuction = "30 year note auction";
                /// <summary>
                /// 3-Month Bill Auction
                /// </summary>
                public const string ThreeMonthBillAuction = "3 month bill auction";
                /// <summary>
                /// 3-Year Note Auction
                /// </summary>
                public const string ThreeYearNoteAuction = "3 year note auction";
                /// <summary>
                /// Total Net TIC Flows
                /// </summary>
                public const string TotalNetTicFlows = "total net tic flows";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "total vehicle sales";
                /// <summary>
                /// Treasury Sec Lew Speaks
                /// </summary>
                public const string TreasurySecLewSpeaks = "treasury sec lew speaks";
                /// <summary>
                /// Treasury Secretary Lew Speaks
                /// </summary>
                public const string TreasurySecretaryLewSpeaks = "treasury secretary lew speaks";
                /// <summary>
                /// Treasury Secretary Mnuchin Speech
                /// </summary>
                public const string TreasurySecretaryMnuchinSpeech = "treasury secretary mnuchin speech";
                /// <summary>
                /// 2019 US Monetary Policy Forum
                /// </summary>
                public const string TwoThousandNineteenUsMonetaryPolicyForum = "2019 us monetary policy forum";
                /// <summary>
                /// 2-Year FRN Auction
                /// </summary>
                public const string TwoYearFrnAuction = "2 year frn auction";
                /// <summary>
                /// 2-Year Note Auction
                /// </summary>
                public const string TwoYearNoteAuction = "2 year note auction";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "unemployment rate";
                /// <summary>
                /// UN General Assembly
                /// </summary>
                public const string UnGeneralAssembly = "un general assembly";
                /// <summary>
                /// Unit Labor Costs QoQ
                /// </summary>
                public const string UnitLaborCostsIndexQoQ = "unit labor costs index qoq";
                /// <summary>
                /// Unit Labor Costs QoQ Final
                /// </summary>
                public const string UnitLaborCostsIndexQoQFinal = "unit labor costs index qoq final";
                /// <summary>
                /// Unit Labor Costs QoQ Prel
                /// </summary>
                public const string UnitLaborCostsIndexQoQPreliminary = "unit labor costs index qoq preliminary";
                /// <summary>
                /// Unit Labor Costs YoY
                /// </summary>
                public const string UnitLaborCostsYoY = "unit labor costs yoy";
                /// <summary>
                /// US-China Phase 1 Trade Deal Signature
                /// </summary>
                public const string UsChinaPhaseOneTradeDealSignature = "us china phase 1 trade deal signature";
                /// <summary>
                /// US-China Trade Talks
                /// </summary>
                public const string UsChinaTradeTalks = "us china trade talks";
                /// <summary>
                /// US Gov Temporary Funding Expires
                /// </summary>
                public const string UsGovTemporaryFundingExpires = "us gov temporary funding expires";
                /// <summary>
                /// USMCA Trade Deal Signature
                /// </summary>
                public const string UsmcaTradeDealSignature = "usmca trade deal signature";
                /// <summary>
                /// US-Mexico Trade Talks
                /// </summary>
                public const string UsMexicoTradeTalks = "us mexico trade talks";
                /// <summary>
                /// US Monetary Policy Forum
                /// </summary>
                public const string UsMonetaryPolicyForum = "us monetary policy forum";
                /// <summary>
                /// US-North Korea Summit
                /// </summary>
                public const string UsNorthKoreaSummit = "us north korea summit";
                /// <summary>
                /// US-Russia Summit
                /// </summary>
                public const string UsRussiaSummit = "us russia summit";
                /// <summary>
                /// Veterans Day
                /// </summary>
                public const string VeteransDay = "veterans day";
                /// <summary>
                /// Veterans Day (Substitute Day)
                /// </summary>
                public const string VeteransDaySubstituteDay = "veterans day substitute day";
                /// <summary>
                /// WASDE Report
                /// </summary>
                public const string WasdeReport = "wasde report";
                /// <summary>
                /// Washington's Birthday
                /// </summary>
                public const string WashingtonsBirthday = "washingtons birthday";
                /// <summary>
                /// White House Jobs Announcement
                /// </summary>
                public const string WhiteHouseJobsAnnouncement = "white house jobs announcement";
                /// <summary>
                /// Wholesale Inventories MoM
                /// </summary>
                public const string WholesaleInventoriesMoM = "wholesale inventories mom";
                /// <summary>
                /// Wholesale Inventories MoM Adv
                /// </summary>
                public const string WholesaleInventoriesMoMAdv = "wholesale inventories mom adv";
            }
        }
    }
}
