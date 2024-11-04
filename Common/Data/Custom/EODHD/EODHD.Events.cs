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

namespace QuantConnect.DataSource
{
    /// <summary>
    /// EODHD static class contains shortcut definitions
    /// </summary>
    public static partial class EODHD
    {
        /// <summary>
        /// The Events class contains all events normalized for your convenience
        /// </summary>
        public static class Events
        {
            /// <summary>
            /// Angola
            /// </summary>
            public static class Angola
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "AGO/inflation rate";
            }

            /// <summary>
            /// Argentina
            /// </summary>
            public static class Argentina
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ARG/inflation rate";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "ARG/retail sales";
            }

            /// <summary>
            /// Armenia
            /// </summary>
            public static class Armenia
            {
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "ARM/construction output";
                /// <summary>
                /// Economic Activity
                /// </summary>
                public const string EconomicActivity = "ARM/economic activity";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ARM/industrial production";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "ARM/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "ARM/retail sales";
            }

            /// <summary>
            /// Australia
            /// </summary>
            public static class Australia
            {
                /// <summary>
                /// Aig Performance Industry Index
                /// </summary>
                public const string AigPerformanceIndustryIndex = "AUS/aig performance industry index";
                /// <summary>
                /// Aig Performance Manufacturing Index
                /// </summary>
                public const string AigPerformanceManufacturingIndex = "AUS/aig performance manufacturing index";
                /// <summary>
                /// Aig Performance Services Index
                /// </summary>
                public const string AigPerformanceServicesIndex = "AUS/aig performance services index";
                /// <summary>
                /// Anz Indeed Job Ads
                /// </summary>
                public const string AnzIndeedJobAds = "AUS/anz indeed job ads";
                /// <summary>
                /// Anz Job Advertisements
                /// </summary>
                public const string AnzJobAdvertisements = "AUS/anz job advertisements";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "AUS/balance of trade";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "AUS/building permits";
                /// <summary>
                /// Business Inventories
                /// </summary>
                public const string BusinessInventories = "AUS/business inventories";
                /// <summary>
                /// Commbank Composite Purchasing Managers Index
                /// </summary>
                public const string CommbankCompositePurchasingManagersIndex = "AUS/commbank composite purchasing managers index";
                /// <summary>
                /// Commbank Manufacturing Purchasing Managers Index
                /// </summary>
                public const string CommbankManufacturingPurchasingManagersIndex = "AUS/commbank manufacturing purchasing managers index";
                /// <summary>
                /// Commbank Services Purchasing Managers Index
                /// </summary>
                public const string CommbankServicesPurchasingManagersIndex = "AUS/commbank services purchasing managers index";
                /// <summary>
                /// Commodity Prices
                /// </summary>
                public const string CommodityPrices = "AUS/commodity prices";
                /// <summary>
                /// Company Gross Profits
                /// </summary>
                public const string CompanyGrossProfits = "AUS/company gross profits";
                /// <summary>
                /// Construction Work Done
                /// </summary>
                public const string ConstructionWorkDone = "AUS/construction work done";
                /// <summary>
                /// Consumer Inflation Expectations
                /// </summary>
                public const string ConsumerInflationExpectations = "AUS/consumer inflation expectations";
                /// <summary>
                /// Corelogic Dwelling Prices
                /// </summary>
                public const string CorelogicDwellingPrices = "AUS/corelogic dwelling prices";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "AUS/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "AUS/easter monday";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "AUS/employment change";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "AUS/export prices";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "AUS/exports";
                /// <summary>
                /// Full Time Employment Change
                /// </summary>
                public const string FullTimeEmploymentChange = "AUS/full time employment change";
                /// <summary>
                /// Gdp Capital Expenditure
                /// </summary>
                public const string GdpCapitalExpenditure = "AUS/gdp capital expenditure";
                /// <summary>
                /// Gdp Consumption
                /// </summary>
                public const string GdpConsumption = "AUS/gdp consumption";
                /// <summary>
                /// Gdp Deflator
                /// </summary>
                public const string GdpDeflator = "AUS/gdp deflator";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "AUS/gdp growth rate";
                /// <summary>
                /// Hia New Home Sales
                /// </summary>
                public const string HiaNewHomeSales = "AUS/hia new home sales";
                /// <summary>
                /// Home Loans
                /// </summary>
                public const string HomeLoans = "AUS/home loans";
                /// <summary>
                /// House Price Index
                /// </summary>
                public const string HousePriceIndex = "AUS/house price index";
                /// <summary>
                /// Housing Credit
                /// </summary>
                public const string HousingCredit = "AUS/housing credit";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "AUS/import prices";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "AUS/imports";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "AUS/inflation rate";
                /// <summary>
                /// Investment Lending For Homes
                /// </summary>
                public const string InvestmentLendingForHomes = "AUS/investment lending for homes";
                /// <summary>
                /// Judo Bank Composite Purchasing Managers Index
                /// </summary>
                public const string JudoBankCompositePurchasingManagersIndex = "AUS/judo bank composite purchasing managers index";
                /// <summary>
                /// Judo Bank Manufacturing Purchasing Managers Index
                /// </summary>
                public const string JudoBankManufacturingPurchasingManagersIndex = "AUS/judo bank manufacturing purchasing managers index";
                /// <summary>
                /// Judo Bank Services Purchasing Managers Index
                /// </summary>
                public const string JudoBankServicesPurchasingManagersIndex = "AUS/judo bank services purchasing managers index";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "AUS/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "AUS/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "AUS/markit services purchasing managers index";
                /// <summary>
                /// Monthly Consumer Price Index Indicators
                /// </summary>
                public const string MonthlyConsumerPriceIndexIndicators = "AUS/monthly consumer price index indicators";
                /// <summary>
                /// Nab Business Confidence
                /// </summary>
                public const string NabBusinessConfidence = "AUS/nab business confidence";
                /// <summary>
                /// Part Time Employment Change
                /// </summary>
                public const string PartTimeEmploymentChange = "AUS/part time employment change";
                /// <summary>
                /// Participation Rate
                /// </summary>
                public const string ParticipationRate = "AUS/participation rate";
                /// <summary>
                /// Plant Machinery Capital Expenditure
                /// </summary>
                public const string PlantMachineryCapitalExpenditure = "AUS/plant machinery capital expenditure";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "AUS/ppi";
                /// <summary>
                /// Private Capital Expenditure
                /// </summary>
                public const string PrivateCapitalExpenditure = "AUS/private capital expenditure";
                /// <summary>
                /// Private House Approvals
                /// </summary>
                public const string PrivateHouseApprovals = "AUS/private house approvals";
                /// <summary>
                /// Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "AUS/private sector credit";
                /// <summary>
                /// Rba Bulletin
                /// </summary>
                public const string RbaBulletin = "AUS/rba bulletin";
                /// <summary>
                /// Rba Bullock Speech
                /// </summary>
                public const string RbaBullockSpeech = "AUS/rba bullock speech";
                /// <summary>
                /// Rba Chart Pack
                /// </summary>
                public const string RbaChartPack = "AUS/rba chart pack";
                /// <summary>
                /// Rba Connolly Speech
                /// </summary>
                public const string RbaConnollySpeech = "AUS/rba connolly speech";
                /// <summary>
                /// Rba Debelle Speech
                /// </summary>
                public const string RbaDebelleSpeech = "AUS/rba debelle speech";
                /// <summary>
                /// Rba Ellis Speech
                /// </summary>
                public const string RbaEllisSpeech = "AUS/rba ellis speech";
                /// <summary>
                /// Rba Financial Stability Review
                /// </summary>
                public const string RbaFinancialStabilityReview = "AUS/rba financial stability review";
                /// <summary>
                /// Rba Gov Lowe Speech
                /// </summary>
                public const string RbaGovLoweSpeech = "AUS/rba gov lowe speech";
                /// <summary>
                /// Rba Interest Rate Decision
                /// </summary>
                public const string RbaInterestRateDecision = "AUS/rba interest rate decision";
                /// <summary>
                /// Rba Jones Speech
                /// </summary>
                public const string RbaJonesSpeech = "AUS/rba jones speech";
                /// <summary>
                /// Rba Kearns Speech
                /// </summary>
                public const string RbaKearnsSpeech = "AUS/rba kearns speech";
                /// <summary>
                /// Rba Kent Speech
                /// </summary>
                public const string RbaKentSpeech = "AUS/rba kent speech";
                /// <summary>
                /// Rba Meeting Minutes
                /// </summary>
                public const string RbaMeetingMinutes = "AUS/rba meeting minutes";
                /// <summary>
                /// Rba Payments System Board Meeting
                /// </summary>
                public const string RbaPaymentsSystemBoardMeeting = "AUS/rba payments system board meeting";
                /// <summary>
                /// Rba Statement On Monetary Policy
                /// </summary>
                public const string RbaStatementOnMonetaryPolicy = "AUS/rba statement on monetary policy";
                /// <summary>
                /// Rba Trimmed Mean Consumer Price Index
                /// </summary>
                public const string RbaTrimmedMeanConsumerPriceIndex = "AUS/rba trimmed mean consumer price index";
                /// <summary>
                /// Rba Weighted Mean Consumer Price Index
                /// </summary>
                public const string RbaWeightedMeanConsumerPriceIndex = "AUS/rba weighted mean consumer price index";
                /// <summary>
                /// Rba Weighted Median Consumer Price Index
                /// </summary>
                public const string RbaWeightedMedianConsumerPriceIndex = "AUS/rba weighted median consumer price index";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "AUS/retail sales";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "AUS/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "AUS/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "AUS/sandp global services purchasing managers index";
                /// <summary>
                /// Td Melbourne Institute Inflation Gauge
                /// </summary>
                public const string TdMelbourneInstituteInflationGauge = "AUS/td melbourne institute inflation gauge";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "AUS/unemployment rate";
                /// <summary>
                /// Wage Price Index
                /// </summary>
                public const string WagePriceIndex = "AUS/wage price index";
                /// <summary>
                /// Westpac Consumer Confidence
                /// </summary>
                public const string WestpacConsumerConfidence = "AUS/westpac consumer confidence";
                /// <summary>
                /// Westpac Consumer Confidence Change
                /// </summary>
                public const string WestpacConsumerConfidenceChange = "AUS/westpac consumer confidence change";
                /// <summary>
                /// Westpac Leading Index
                /// </summary>
                public const string WestpacLeadingIndex = "AUS/westpac leading index";
            }

            /// <summary>
            /// Austria
            /// </summary>
            public static class Austria
            {
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "AUT/business confidence";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "AUT/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "AUT/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "AUT/easter monday";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "AUT/gdp growth rate";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "AUT/harmonized inflation rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "AUT/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "AUT/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "AUT/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "AUT/retail sales";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "AUT/unemployed persons";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "AUT/unemployment rate";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "AUT/wholesale prices";
            }

            /// <summary>
            /// Belgium
            /// </summary>
            public static class Belgium
            {
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "BEL/balance of trade";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BEL/business confidence";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "BEL/construction output";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BEL/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "BEL/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "BEL/easter monday";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BEL/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq Adv
                /// </summary>
                public const string GdpGrowthRateQoqAdv = "BEL/gdp growth rate qoq adv";
                /// <summary>
                /// Gdp Growth Rate Yoy Adv
                /// </summary>
                public const string GdpGrowthRateYoyAdv = "BEL/gdp growth rate yoy adv";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BEL/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "BEL/inflation rate";
                /// <summary>
                /// New Car Registrations
                /// </summary>
                public const string NewCarRegistrations = "BEL/new car registrations";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "BEL/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "BEL/retail sales";
            }

            /// <summary>
            /// Bangladesh
            /// </summary>
            public static class Bangladesh
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "BGD/inflation rate";
            }

            /// <summary>
            /// Bulgaria
            /// </summary>
            public static class Bulgaria
            {
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BGR/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BGR/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "BGR/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "BGR/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "BGR/retail sales";
            }

            /// <summary>
            /// Bosniaandherzegovina
            /// </summary>
            public static class BosniaAndHerzegovina
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "BIH/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "BIH/ppi";
            }

            /// <summary>
            /// Brazil
            /// </summary>
            public static class Brazil
            {
                /// <summary>
                /// 2 Year Ltn Auction
                /// </summary>
                public const string TwoYearLtnAuction = "BRA/2 year ltn auction";
                /// <summary>
                /// 6 Month Ltn Auction
                /// </summary>
                public const string SixMonthLtnAuction = "BRA/6 month ltn auction";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "BRA/balance of trade";
                /// <summary>
                /// Bank Lending
                /// </summary>
                public const string BankLending = "BRA/bank lending";
                /// <summary>
                /// Bcb Copom Meeting Minutes
                /// </summary>
                public const string BcbCopomMeetingMinutes = "BRA/bcb copom meeting minutes";
                /// <summary>
                /// Bcb Focus Market Readout
                /// </summary>
                public const string BcbFocusMarketReadout = "BRA/bcb focus market readout";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BRA/business confidence";
                /// <summary>
                /// Car Production
                /// </summary>
                public const string CarProduction = "BRA/car production";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BRA/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "BRA/current account";
                /// <summary>
                /// Federal Tax Revenues
                /// </summary>
                public const string FederalTaxRevenues = "BRA/federal tax revenues";
                /// <summary>
                /// Fgv Consumer Confidence
                /// </summary>
                public const string FgvConsumerConfidence = "BRA/fgv consumer confidence";
                /// <summary>
                /// Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "BRA/foreign direct investment";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BRA/gdp growth rate";
                /// <summary>
                /// Gross Debt To Gdp
                /// </summary>
                public const string GrossDebtToGdp = "BRA/gross debt to gdp";
                /// <summary>
                /// Ibc Br Economic Activity
                /// </summary>
                public const string IbcBrEconomicActivity = "BRA/ibc br economic activity";
                /// <summary>
                /// Igp M Inflation
                /// </summary>
                public const string IgpMInflation = "BRA/igp m inflation";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "BRA/independence day";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BRA/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "BRA/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "BRA/interest rate decision";
                /// <summary>
                /// Ipc Fipe Inflation
                /// </summary>
                public const string IpcFipeInflation = "BRA/ipc fipe inflation";
                /// <summary>
                /// Ipca Mid Month Consumer Price Index
                /// </summary>
                public const string IpcaMidMonthConsumerPriceIndex = "BRA/ipca mid month consumer price index";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "BRA/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "BRA/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "BRA/markit services purchasing managers index";
                /// <summary>
                /// Net Payrolls
                /// </summary>
                public const string NetPayrolls = "BRA/net payrolls";
                /// <summary>
                /// New Car Registrations
                /// </summary>
                public const string NewCarRegistrations = "BRA/new car registrations";
                /// <summary>
                /// Nominal Budget Balance
                /// </summary>
                public const string NominalBudgetBalance = "BRA/nominal budget balance";
                /// <summary>
                /// Our Lady Of Aparecida Day
                /// </summary>
                public const string OurLadyOfAparecidaDay = "BRA/our lady of aparecida day";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "BRA/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "BRA/retail sales";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "BRA/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "BRA/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "BRA/sandp global services purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "BRA/unemployment rate";
            }

            /// <summary>
            /// Canada
            /// </summary>
            public static class Canada
            {
                /// <summary>
                /// 2 Year Bond Auction
                /// </summary>
                public const string TwoYearBondAuction = "CAN/2 year bond auction";
                /// <summary>
                /// 30 Year Bond Auction
                /// </summary>
                public const string ThirtyYearBondAuction = "CAN/30 year bond auction";
                /// <summary>
                /// 5 Year Bond Auction
                /// </summary>
                public const string FiveYearBondAuction = "CAN/5 year bond auction";
                /// <summary>
                /// Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "CAN/adp employment change";
                /// <summary>
                /// Average Hourly Wages
                /// </summary>
                public const string AverageHourlyWages = "CAN/average hourly wages";
                /// <summary>
                /// Average Weekly Earnings
                /// </summary>
                public const string AverageWeeklyEarnings = "CAN/average weekly earnings";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "CAN/balance of trade";
                /// <summary>
                /// Bank Of Canada Beaudry Speech
                /// </summary>
                public const string BankOfCanadaBeaudrySpeech = "CAN/bank of canada beaudry speech";
                /// <summary>
                /// Bank Of Canada Business Outlook Survey
                /// </summary>
                public const string BankOfCanadaBusinessOutlookSurvey = "CAN/bank of canada business outlook survey";
                /// <summary>
                /// Bank Of Canada Gov Macklem Speech
                /// </summary>
                public const string BankOfCanadaGovMacklemSpeech = "CAN/bank of canada gov macklem speech";
                /// <summary>
                /// Bank Of Canada Gov Poloz Speech
                /// </summary>
                public const string BankOfCanadaGovPolozSpeech = "CAN/bank of canada gov poloz speech";
                /// <summary>
                /// Bank Of Canada Gov Rogers Speech
                /// </summary>
                public const string BankOfCanadaGovRogersSpeech = "CAN/bank of canada gov rogers speech";
                /// <summary>
                /// Bank Of Canada Interest Rate Decision
                /// </summary>
                public const string BankOfCanadaInterestRateDecision = "CAN/bank of canada interest rate decision";
                /// <summary>
                /// Bank Of Canada Lane Speech
                /// </summary>
                public const string BankOfCanadaLaneSpeech = "CAN/bank of canada lane speech";
                /// <summary>
                /// Bank Of Canada Monetary Policy Report
                /// </summary>
                public const string BankOfCanadaMonetaryPolicyReport = "CAN/bank of canada monetary policy report";
                /// <summary>
                /// Bank Of Canada Press Conference
                /// </summary>
                public const string BankOfCanadaPressConference = "CAN/bank of canada press conference";
                /// <summary>
                /// Bank Of Canada Schembri Speech
                /// </summary>
                public const string BankOfCanadaSchembriSpeech = "CAN/bank of canada schembri speech";
                /// <summary>
                /// Bank Of Canada Summary Of Deliberations
                /// </summary>
                public const string BankOfCanadaSummaryOfDeliberations = "CAN/bank of canada summary of deliberations";
                /// <summary>
                /// Bank Of Canada Wilkins Speech
                /// </summary>
                public const string BankOfCanadaWilkinsSpeech = "CAN/bank of canada wilkins speech";
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "CAN/budget balance";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "CAN/building permits";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "CAN/capacity utilization";
                /// <summary>
                /// Cfib Business Barometer
                /// </summary>
                public const string CfibBusinessBarometer = "CAN/cfib business barometer";
                /// <summary>
                /// Civic Holiday
                /// </summary>
                public const string CivicHoliday = "CAN/civic holiday";
                /// <summary>
                /// Consumer Price Index Median
                /// </summary>
                public const string ConsumerPriceIndexMedian = "CAN/consumer price index median";
                /// <summary>
                /// Consumer Price Index Trimmed Mean
                /// </summary>
                public const string ConsumerPriceIndexTrimmedMean = "CAN/consumer price index trimmed mean";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "CAN/core inflation rate";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "CAN/current account";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "CAN/employment change";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "CAN/exports";
                /// <summary>
                /// Family Day
                /// </summary>
                public const string FamilyDay = "CAN/family day";
                /// <summary>
                /// Foreign Securities Purchases
                /// </summary>
                public const string ForeignSecuritiesPurchases = "CAN/foreign securities purchases";
                /// <summary>
                /// Foreign Securities Purchases By Canadians
                /// </summary>
                public const string ForeignSecuritiesPurchasesByCanadians = "CAN/foreign securities purchases by canadians";
                /// <summary>
                /// Full Time Employment Change
                /// </summary>
                public const string FullTimeEmploymentChange = "CAN/full time employment change";
                /// <summary>
                /// Gdp
                /// </summary>
                public const string Gdp = "CAN/gdp";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CAN/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Annualized
                /// </summary>
                public const string GdpGrowthRateAnnualized = "CAN/gdp growth rate annualized";
                /// <summary>
                /// Gdp Implicit Price
                /// </summary>
                public const string GdpImplicitPrice = "CAN/gdp implicit price";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "CAN/housing starts";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "CAN/imports";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CAN/inflation rate";
                /// <summary>
                /// Ivey Purchasing Managers Index Seasonally Adjusted
                /// </summary>
                public const string IveyPurchasingManagersIndexSeasonallyAdjusted = "CAN/ivey purchasing managers index seasonally adjusted";
                /// <summary>
                /// Labor Day
                /// </summary>
                public const string LaborDay = "CAN/labor day";
                /// <summary>
                /// Labor Productivity
                /// </summary>
                public const string LaborProductivity = "CAN/labor productivity";
                /// <summary>
                /// Manufacturing Sales
                /// </summary>
                public const string ManufacturingSales = "CAN/manufacturing sales";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "CAN/markit manufacturing purchasing managers index";
                /// <summary>
                /// New Housing Price Index
                /// </summary>
                public const string NewHousingPriceIndex = "CAN/new housing price index";
                /// <summary>
                /// New Motor Vehicle Sales
                /// </summary>
                public const string NewMotorVehicleSales = "CAN/new motor vehicle sales";
                /// <summary>
                /// New Years Day
                /// </summary>
                public const string NewYearsDay = "CAN/new years day";
                /// <summary>
                /// Part Time Employment Change
                /// </summary>
                public const string PartTimeEmploymentChange = "CAN/part time employment change";
                /// <summary>
                /// Participation Rate
                /// </summary>
                public const string ParticipationRate = "CAN/participation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "CAN/ppi";
                /// <summary>
                /// Raw Materials Price Index
                /// </summary>
                public const string RawMaterialsPriceIndex = "CAN/raw materials price index";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "CAN/retail sales";
                /// <summary>
                /// Retail Sales Excluding Autos
                /// </summary>
                public const string RetailSalesExcludingAutos = "CAN/retail sales excluding autos";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "CAN/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "CAN/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "CAN/sandp global services purchasing managers index";
                /// <summary>
                /// Senior Loan Officer Survey
                /// </summary>
                public const string SeniorLoanOfficerSurvey = "CAN/senior loan officer survey";
                /// <summary>
                /// Thanksgiving Day
                /// </summary>
                public const string ThanksgivingDay = "CAN/thanksgiving day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CAN/unemployment rate";
                /// <summary>
                /// Victoria Day
                /// </summary>
                public const string VictoriaDay = "CAN/victoria day";
                /// <summary>
                /// Wholesale Sales
                /// </summary>
                public const string WholesaleSales = "CAN/wholesale sales";
            }

            /// <summary>
            /// Switzerland
            /// </summary>
            public static class Switzerland
            {
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "CHE/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "CHE/easter monday";
                /// <summary>
                /// Economic Sentiment Index
                /// </summary>
                public const string EconomicSentimentIndex = "CHE/economic sentiment index";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "CHE/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CHE/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CHE/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CHE/inflation rate";
                /// <summary>
                /// Kof Leading Indicators
                /// </summary>
                public const string KofLeadingIndicators = "CHE/kof leading indicators";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "CHE/non farm payrolls";
                /// <summary>
                /// Pentecost Monday
                /// </summary>
                public const string PentecostMonday = "CHE/pentecost monday";
                /// <summary>
                /// Procure Ch Manufacturing Purchasing Managers Index
                /// </summary>
                public const string ProcureChManufacturingPurchasingManagersIndex = "CHE/procure ch manufacturing purchasing managers index";
                /// <summary>
                /// Producer And Import Prices
                /// </summary>
                public const string ProducerAndImportPrices = "CHE/producer and import prices";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "CHE/retail sales";
                /// <summary>
                /// Seco Economic Forecasts
                /// </summary>
                public const string SecoEconomicForecasts = "CHE/seco economic forecasts";
                /// <summary>
                /// Snb Chair Jordan Speech
                /// </summary>
                public const string SnbChairJordanSpeech = "CHE/snb chair jordan speech";
                /// <summary>
                /// Snb Interest Rate Decision
                /// </summary>
                public const string SnbInterestRateDecision = "CHE/snb interest rate decision";
                /// <summary>
                /// Snb Jordan Speech
                /// </summary>
                public const string SnbJordanSpeech = "CHE/snb jordan speech";
                /// <summary>
                /// Snb Press Conference
                /// </summary>
                public const string SnbPressConference = "CHE/snb press conference";
                /// <summary>
                /// Snb Quarterly Bulletin
                /// </summary>
                public const string SnbQuarterlyBulletin = "CHE/snb quarterly bulletin";
                /// <summary>
                /// St Berchtold
                /// </summary>
                public const string StBerchtold = "CHE/st berchtold";
                /// <summary>
                /// Svme Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SvmeManufacturingPurchasingManagersIndex = "CHE/svme manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CHE/unemployment rate";
                /// <summary>
                /// World Economic Forum Annual Meeting
                /// </summary>
                public const string WorldEconomicForumAnnualMeeting = "CHE/world economic forum annual meeting";
            }

            /// <summary>
            /// Chile
            /// </summary>
            public static class Chile
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CHL/inflation rate";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "CHL/retail sales";
            }

            /// <summary>
            /// China
            /// </summary>
            public static class China
            {
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "CHN/balance of trade";
                /// <summary>
                /// Caixin Composite Purchasing Managers Index
                /// </summary>
                public const string CaixinCompositePurchasingManagersIndex = "CHN/caixin composite purchasing managers index";
                /// <summary>
                /// Caixin Manufacturing Purchasing Managers Index
                /// </summary>
                public const string CaixinManufacturingPurchasingManagersIndex = "CHN/caixin manufacturing purchasing managers index";
                /// <summary>
                /// Caixin Services Purchasing Managers Index
                /// </summary>
                public const string CaixinServicesPurchasingManagersIndex = "CHN/caixin services purchasing managers index";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "CHN/current account";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "CHN/exports";
                /// <summary>
                /// Fdi
                /// </summary>
                public const string Fdi = "CHN/fdi";
                /// <summary>
                /// Fixed Asset Investment
                /// </summary>
                public const string FixedAssetInvestment = "CHN/fixed asset investment";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "CHN/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CHN/gdp growth rate";
                /// <summary>
                /// House Price Index
                /// </summary>
                public const string HousePriceIndex = "CHN/house price index";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "CHN/imports";
                /// <summary>
                /// Industrial Capacity Utilization
                /// </summary>
                public const string IndustrialCapacityUtilization = "CHN/industrial capacity utilization";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CHN/industrial production";
                /// <summary>
                /// Industrial Profits
                /// </summary>
                public const string IndustrialProfits = "CHN/industrial profits";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CHN/inflation rate";
                /// <summary>
                /// Loan Prime Rate 1Y
                /// </summary>
                public const string LoanPrimeRate1Y = "CHN/loan prime rate 1y";
                /// <summary>
                /// Loan Prime Rate 5Y
                /// </summary>
                public const string LoanPrimeRate5Y = "CHN/loan prime rate 5y";
                /// <summary>
                /// M2 Money Supply
                /// </summary>
                public const string M2MoneySupply = "CHN/m2 money supply";
                /// <summary>
                /// May Day
                /// </summary>
                public const string MayDay = "CHN/may day";
                /// <summary>
                /// National Day Golden Week
                /// </summary>
                public const string NationalDayGoldenWeek = "CHN/national day golden week";
                /// <summary>
                /// National Peoples Congress
                /// </summary>
                public const string NationalPeoplesCongress = "CHN/national peoples congress";
                /// <summary>
                /// Nbs General Purchasing Managers Index
                /// </summary>
                public const string NbsGeneralPurchasingManagersIndex = "CHN/nbs general purchasing managers index";
                /// <summary>
                /// Nbs Manufacturing Purchasing Managers Index
                /// </summary>
                public const string NbsManufacturingPurchasingManagersIndex = "CHN/nbs manufacturing purchasing managers index";
                /// <summary>
                /// Nbs Non Manufacturing Purchasing Managers Index
                /// </summary>
                public const string NbsNonManufacturingPurchasingManagersIndex = "CHN/nbs non manufacturing purchasing managers index";
                /// <summary>
                /// Nbs Press Conference
                /// </summary>
                public const string NbsPressConference = "CHN/nbs press conference";
                /// <summary>
                /// New Years Day
                /// </summary>
                public const string NewYearsDay = "CHN/new years day";
                /// <summary>
                /// New Yuan Loans
                /// </summary>
                public const string NewYuanLoans = "CHN/new yuan loans";
                /// <summary>
                /// Non Manufacturing Purchasing Managers Index
                /// </summary>
                public const string NonManufacturingPurchasingManagersIndex = "CHN/non manufacturing purchasing managers index";
                /// <summary>
                /// Outstanding Loan Growth
                /// </summary>
                public const string OutstandingLoanGrowth = "CHN/outstanding loan growth";
                /// <summary>
                /// Pboc 1 Year Mlf Announcement
                /// </summary>
                public const string PbocOneYearMlfAnnouncement = "CHN/pboc 1 year mlf announcement";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "CHN/ppi";
                /// <summary>
                /// Qingming Festival
                /// </summary>
                public const string QingmingFestival = "CHN/qingming festival";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "CHN/retail sales";
                /// <summary>
                /// Total Social Financing
                /// </summary>
                public const string TotalSocialFinancing = "CHN/total social financing";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CHN/unemployment rate";
                /// <summary>
                /// Vehicle Sales
                /// </summary>
                public const string VehicleSales = "CHN/vehicle sales";
            }

            /// <summary>
            /// Ivorycoast
            /// </summary>
            public static class IvoryCoast
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CIV/inflation rate";
            }

            /// <summary>
            /// Colombia
            /// </summary>
            public static class Colombia
            {
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "COL/business confidence";
                /// <summary>
                /// Cement Production
                /// </summary>
                public const string CementProduction = "COL/cement production";
                /// <summary>
                /// Columbus Day
                /// </summary>
                public const string ColumbusDay = "COL/columbus day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "COL/consumer confidence";
                /// <summary>
                /// Corpus Christi
                /// </summary>
                public const string CorpusChristi = "COL/corpus christi";
                /// <summary>
                /// Davivienda Manufacturing Purchasing Managers Index
                /// </summary>
                public const string DaviviendaManufacturingPurchasingManagersIndex = "COL/davivienda manufacturing purchasing managers index";
                /// <summary>
                /// Epiphany
                /// </summary>
                public const string Epiphany = "COL/epiphany";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "COL/exports";
                /// <summary>
                /// Feast Of Saint Peter And Saint Paul
                /// </summary>
                public const string FeastOfSaintPeterAndSaintPaul = "COL/feast of saint peter and saint paul";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "COL/gdp growth rate";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "COL/imports";
                /// <summary>
                /// Independence Of Cartagena
                /// </summary>
                public const string IndependenceOfCartagena = "COL/independence of cartagena";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "COL/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "COL/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "COL/interest rate decision";
                /// <summary>
                /// Ise Economic Activity
                /// </summary>
                public const string IseEconomicActivity = "COL/ise economic activity";
                /// <summary>
                /// Monetary Policy Minutes
                /// </summary>
                public const string MonetaryPolicyMinutes = "COL/monetary policy minutes";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "COL/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "COL/retail sales";
                /// <summary>
                /// Sacred Heart
                /// </summary>
                public const string SacredHeart = "COL/sacred heart";
                /// <summary>
                /// Saint Josephs Day
                /// </summary>
                public const string SaintJosephsDay = "COL/saint josephs day";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "COL/unemployment rate";
            }

            /// <summary>
            /// Cyprus
            /// </summary>
            public static class Cyprus
            {
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "CYP/construction output";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CYP/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "CYP/current account";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CYP/gdp growth rate";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "CYP/harmonized inflation rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CYP/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CYP/inflation rate";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "CYP/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CYP/unemployment rate";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "CYP/wage growth";
            }

            /// <summary>
            /// Czechia
            /// </summary>
            public static class Czechia
            {
                /// <summary>
                /// Cnb Interest Rate Decision
                /// </summary>
                public const string CnbInterestRateDecision = "CZE/cnb interest rate decision";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "CZE/construction output";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CZE/consumer confidence";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "CZE/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "CZE/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "CZE/easter monday";
                /// <summary>
                /// External Debt
                /// </summary>
                public const string ExternalDebt = "CZE/external debt";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "CZE/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CZE/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CZE/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CZE/inflation rate";
                /// <summary>
                /// M3 Money Supply
                /// </summary>
                public const string M3MoneySupply = "CZE/m3 money supply";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "CZE/markit manufacturing purchasing managers index";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "CZE/ppi";
                /// <summary>
                /// Real Wages
                /// </summary>
                public const string RealWages = "CZE/real wages";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "CZE/retail sales";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "CZE/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CZE/unemployment rate";
            }

            /// <summary>
            /// Germany
            /// </summary>
            public static class Germany
            {
                /// <summary>
                /// 12 Month Bubill Auction
                /// </summary>
                public const string TwelveMonthBubillAuction = "DEU/12 month bubill auction";
                /// <summary>
                /// 15 Year Bund Auction
                /// </summary>
                public const string FifteenYearBundAuction = "DEU/15 year bund auction";
                /// <summary>
                /// 2 Year Schatz Auction
                /// </summary>
                public const string TwoYearSchatzAuction = "DEU/2 year schatz auction";
                /// <summary>
                /// 3 Month Bubill Auction
                /// </summary>
                public const string ThreeMonthBubillAuction = "DEU/3 month bubill auction";
                /// <summary>
                /// 30 Year Bund Auction
                /// </summary>
                public const string ThirtyYearBundAuction = "DEU/30 year bund auction";
                /// <summary>
                /// 5 Year Bobl Auction
                /// </summary>
                public const string FiveYearBoblAuction = "DEU/5 year bobl auction";
                /// <summary>
                /// 7 Year Bund Auction
                /// </summary>
                public const string SevenYearBundAuction = "DEU/7 year bund auction";
                /// <summary>
                /// 9 Month Bubill Auction
                /// </summary>
                public const string NineMonthBubillAuction = "DEU/9 month bubill auction";
                /// <summary>
                /// Baden Wuerttemberg Consumer Price Index
                /// </summary>
                public const string BadenWuerttembergConsumerPriceIndex = "DEU/baden wuerttemberg consumer price index";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "DEU/balance of trade";
                /// <summary>
                /// Balance Of Trade Seasonally Adjusted
                /// </summary>
                public const string BalanceOfTradeSeasonallyAdjusted = "DEU/balance of trade seasonally adjusted";
                /// <summary>
                /// Bavaria Consumer Price Index
                /// </summary>
                public const string BavariaConsumerPriceIndex = "DEU/bavaria consumer price index";
                /// <summary>
                /// Brandenburg Consumer Price Index
                /// </summary>
                public const string BrandenburgConsumerPriceIndex = "DEU/brandenburg consumer price index";
                /// <summary>
                /// Bundesbank Balz Speech
                /// </summary>
                public const string BundesbankBalzSpeech = "DEU/bundesbank balz speech";
                /// <summary>
                /// Bundesbank Beermann Speech
                /// </summary>
                public const string BundesbankBeermannSpeech = "DEU/bundesbank beermann speech";
                /// <summary>
                /// Bundesbank Buch Speech
                /// </summary>
                public const string BundesbankBuchSpeech = "DEU/bundesbank buch speech";
                /// <summary>
                /// Bundesbank Mauderer Speech
                /// </summary>
                public const string BundesbankMaudererSpeech = "DEU/bundesbank mauderer speech";
                /// <summary>
                /// Bundesbank Monthly Report
                /// </summary>
                public const string BundesbankMonthlyReport = "DEU/bundesbank monthly report";
                /// <summary>
                /// Bundesbank Nagel Speech
                /// </summary>
                public const string BundesbankNagelSpeech = "DEU/bundesbank nagel speech";
                /// <summary>
                /// Bundesbank President Nagel Speech
                /// </summary>
                public const string BundesbankPresidentNagelSpeech = "DEU/bundesbank president nagel speech";
                /// <summary>
                /// Bundesbank Weidmann Speech
                /// </summary>
                public const string BundesbankWeidmannSpeech = "DEU/bundesbank weidmann speech";
                /// <summary>
                /// Bundesbank Wuermeling Speech
                /// </summary>
                public const string BundesbankWuermelingSpeech = "DEU/bundesbank wuermeling speech";
                /// <summary>
                /// Construction Purchasing Managers Index
                /// </summary>
                public const string ConstructionPurchasingManagersIndex = "DEU/construction purchasing managers index";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "DEU/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "DEU/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "DEU/easter monday";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "DEU/exports";
                /// <summary>
                /// Exports Mom Seasonally Adjusted
                /// </summary>
                public const string ExportsMomSeasonallyAdjusted = "DEU/exports mom seasonally adjusted";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "DEU/factory orders";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "DEU/gdp growth rate";
                /// <summary>
                /// Gfk Consumer Confidence
                /// </summary>
                public const string GfkConsumerConfidence = "DEU/gfk consumer confidence";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "DEU/harmonized inflation rate";
                /// <summary>
                /// Hcob Composite Purchasing Managers Index
                /// </summary>
                public const string HcobCompositePurchasingManagersIndex = "DEU/hcob composite purchasing managers index";
                /// <summary>
                /// Hcob Construction Purchasing Managers Index
                /// </summary>
                public const string HcobConstructionPurchasingManagersIndex = "DEU/hcob construction purchasing managers index";
                /// <summary>
                /// Hcob Manufacturing Purchasing Managers Index
                /// </summary>
                public const string HcobManufacturingPurchasingManagersIndex = "DEU/hcob manufacturing purchasing managers index";
                /// <summary>
                /// Hcob Services Purchasing Managers Index
                /// </summary>
                public const string HcobServicesPurchasingManagersIndex = "DEU/hcob services purchasing managers index";
                /// <summary>
                /// Hesse Consumer Price Index
                /// </summary>
                public const string HesseConsumerPriceIndex = "DEU/hesse consumer price index";
                /// <summary>
                /// Ifo Business Climate
                /// </summary>
                public const string IfoBusinessClimate = "DEU/ifo business climate";
                /// <summary>
                /// Ifo Current Conditions
                /// </summary>
                public const string IfoCurrentConditions = "DEU/ifo current conditions";
                /// <summary>
                /// Ifo Expectations
                /// </summary>
                public const string IfoExpectations = "DEU/ifo expectations";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "DEU/import prices";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "DEU/imports";
                /// <summary>
                /// Imports Mom Seasonally Adjusted
                /// </summary>
                public const string ImportsMomSeasonallyAdjusted = "DEU/imports mom seasonally adjusted";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "DEU/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "DEU/inflation rate";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "DEU/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "DEU/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "DEU/markit services purchasing managers index";
                /// <summary>
                /// New Car Registrations
                /// </summary>
                public const string NewCarRegistrations = "DEU/new car registrations";
                /// <summary>
                /// New Years Day
                /// </summary>
                public const string NewYearsDay = "DEU/new years day";
                /// <summary>
                /// North Rhine Westphalia Consumer Price Index
                /// </summary>
                public const string NorthRhineWestphaliaConsumerPriceIndex = "DEU/north rhine westphalia consumer price index";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "DEU/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "DEU/retail sales";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "DEU/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Construction Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalConstructionPurchasingManagersIndex = "DEU/sandp global construction purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "DEU/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "DEU/sandp global services purchasing managers index";
                /// <summary>
                /// Saxony Consumer Price Index
                /// </summary>
                public const string SaxonyConsumerPriceIndex = "DEU/saxony consumer price index";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "DEU/unemployed persons";
                /// <summary>
                /// Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "DEU/unemployment change";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "DEU/unemployment rate";
                /// <summary>
                /// Unemployment Rate Harmonized
                /// </summary>
                public const string UnemploymentRateHarmonized = "DEU/unemployment rate harmonized";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "DEU/wholesale prices";
                /// <summary>
                /// Zew Current Conditions
                /// </summary>
                public const string ZewCurrentConditions = "DEU/zew current conditions";
                /// <summary>
                /// Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "DEU/zew economic sentiment index";
            }

            /// <summary>
            /// Denmark
            /// </summary>
            public static class Denmark
            {
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "DNK/consumer confidence";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "DNK/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "DNK/current account";
                /// <summary>
                /// Dilf Manufacturing Purchasing Managers Index
                /// </summary>
                public const string DilfManufacturingPurchasingManagersIndex = "DNK/dilf manufacturing purchasing managers index";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "DNK/easter monday";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "DNK/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "DNK/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq Adv
                /// </summary>
                public const string GdpGrowthRateQoqAdv = "DNK/gdp growth rate qoq adv";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "DNK/harmonized inflation rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "DNK/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "DNK/inflation rate";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "DNK/manufacturing production";
                /// <summary>
                /// New Years Day
                /// </summary>
                public const string NewYearsDay = "DNK/new years day";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "DNK/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "DNK/unemployment rate";
            }

            /// <summary>
            /// Dominicanrepublic
            /// </summary>
            public static class DominicanRepublic
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "DOM/inflation rate";
            }

            /// <summary>
            /// Ecuador
            /// </summary>
            public static class Ecuador
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ECU/inflation rate";
            }

            /// <summary>
            /// Egypt
            /// </summary>
            public static class Egypt
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "EGY/inflation rate";
            }

            /// <summary>
            /// Spain
            /// </summary>
            public static class Spain
            {
                /// <summary>
                /// 12 Month Letras Auction
                /// </summary>
                public const string TwelveMonthLetrasAuction = "ESP/12 month letras auction";
                /// <summary>
                /// 15 Year Obligacion Auction
                /// </summary>
                public const string FifteenYearObligacionAuction = "ESP/15 year obligacion auction";
                /// <summary>
                /// 3 Month Letras Auction
                /// </summary>
                public const string ThreeMonthLetrasAuction = "ESP/3 month letras auction";
                /// <summary>
                /// 3 Year Bonos Auction
                /// </summary>
                public const string ThreeYearBonosAuction = "ESP/3 year bonos auction";
                /// <summary>
                /// 30 Year Obligacion Auction
                /// </summary>
                public const string ThirtyYearObligacionAuction = "ESP/30 year obligacion auction";
                /// <summary>
                /// 5 Year Bonos Auction
                /// </summary>
                public const string FiveYearBonosAuction = "ESP/5 year bonos auction";
                /// <summary>
                /// 50 Year Obligacion Auction
                /// </summary>
                public const string FiftyYearObligacionAuction = "ESP/50 year obligacion auction";
                /// <summary>
                /// 6 Month Letras Auction
                /// </summary>
                public const string SixMonthLetrasAuction = "ESP/6 month letras auction";
                /// <summary>
                /// 7 Year Obligacion Auction
                /// </summary>
                public const string SevenYearObligacionAuction = "ESP/7 year obligacion auction";
                /// <summary>
                /// 9 Month Letras Auction
                /// </summary>
                public const string NineMonthLetrasAuction = "ESP/9 month letras auction";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "ESP/balance of trade";
                /// <summary>
                /// Bonos Auction
                /// </summary>
                public const string BonosAuction = "ESP/bonos auction";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ESP/business confidence";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ESP/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "ESP/core inflation rate";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "ESP/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "ESP/current account";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ESP/gdp growth rate";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "ESP/harmonized inflation rate";
                /// <summary>
                /// Hcob Composite Purchasing Managers Index
                /// </summary>
                public const string HcobCompositePurchasingManagersIndex = "ESP/hcob composite purchasing managers index";
                /// <summary>
                /// Hcob Manufacturing Purchasing Managers Index
                /// </summary>
                public const string HcobManufacturingPurchasingManagersIndex = "ESP/hcob manufacturing purchasing managers index";
                /// <summary>
                /// Hcob Services Purchasing Managers Index
                /// </summary>
                public const string HcobServicesPurchasingManagersIndex = "ESP/hcob services purchasing managers index";
                /// <summary>
                /// Index Linked Obligacion Auction
                /// </summary>
                public const string IndexLinkedObligacionAuction = "ESP/index linked obligacion auction";
                /// <summary>
                /// Industrial Orders
                /// </summary>
                public const string IndustrialOrders = "ESP/industrial orders";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ESP/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ESP/inflation rate";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "ESP/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "ESP/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "ESP/markit services purchasing managers index";
                /// <summary>
                /// New Car Sales
                /// </summary>
                public const string NewCarSales = "ESP/new car sales";
                /// <summary>
                /// Obligacion Auction
                /// </summary>
                public const string ObligacionAuction = "ESP/obligacion auction";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "ESP/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "ESP/retail sales";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "ESP/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "ESP/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "ESP/sandp global services purchasing managers index";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "ESP/tourist arrivals";
                /// <summary>
                /// Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "ESP/unemployment change";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ESP/unemployment rate";
            }

            /// <summary>
            /// Estonia
            /// </summary>
            public static class Estonia
            {
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "EST/cpi";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "EST/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "EST/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "EST/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "EST/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "EST/retail sales";
            }

            /// <summary>
            /// Europeanunion
            /// </summary>
            public static class EuropeanUnion
            {
                /// <summary>
                /// 3 Month Bill Auction
                /// </summary>
                public const string ThreeMonthBillAuction = "EUR/3 month bill auction";
                /// <summary>
                /// 3 Year Bond Auction
                /// </summary>
                public const string ThreeYearBondAuction = "EUR/3 year bond auction";
                /// <summary>
                /// 5 Year Bond Auction
                /// </summary>
                public const string FiveYearBondAuction = "EUR/5 year bond auction";
                /// <summary>
                /// 6 Month Bill Auction
                /// </summary>
                public const string SixMonthBillAuction = "EUR/6 month bill auction";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "EUR/balance of trade";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "EUR/construction output";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "EUR/consumer confidence";
                /// <summary>
                /// Consumer Inflation Expectations
                /// </summary>
                public const string ConsumerInflationExpectations = "EUR/consumer inflation expectations";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "EUR/core inflation rate";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "EUR/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "EUR/current account";
                /// <summary>
                /// Current Account Seasonally Adjusted
                /// </summary>
                public const string CurrentAccountSeasonallyAdjusted = "EUR/current account seasonally adjusted";
                /// <summary>
                /// Deposit Facility Rate
                /// </summary>
                public const string DepositFacilityRate = "EUR/deposit facility rate";
                /// <summary>
                /// Ecb Af Jochnick Speech
                /// </summary>
                public const string EcbAfJochnickSpeech = "EUR/ecb af jochnick speech";
                /// <summary>
                /// Ecb Bank Lending Survey
                /// </summary>
                public const string EcbBankLendingSurvey = "EUR/ecb bank lending survey";
                /// <summary>
                /// Ecb Buch Speech
                /// </summary>
                public const string EcbBuchSpeech = "EUR/ecb buch speech";
                /// <summary>
                /// Ecb Cipollone Speech
                /// </summary>
                public const string EcbCipolloneSpeech = "EUR/ecb cipollone speech";
                /// <summary>
                /// Ecb Cur Speech
                /// </summary>
                public const string EcbCurSpeech = "EUR/ecb cur speech";
                /// <summary>
                /// Ecb De Guindos Speech
                /// </summary>
                public const string EcbDeGuindosSpeech = "EUR/ecb de guindos speech";
                /// <summary>
                /// Ecb Economic Bulletin
                /// </summary>
                public const string EcbEconomicBulletin = "EUR/ecb economic bulletin";
                /// <summary>
                /// Ecb Elderson Speech
                /// </summary>
                public const string EcbEldersonSpeech = "EUR/ecb elderson speech";
                /// <summary>
                /// Ecb Enria Speech
                /// </summary>
                public const string EcbEnriaSpeech = "EUR/ecb enria speech";
                /// <summary>
                /// Ecb Fernandez Bollo Speech
                /// </summary>
                public const string EcbFernandezBolloSpeech = "EUR/ecb fernandez bollo speech";
                /// <summary>
                /// Ecb Financial Stability Review
                /// </summary>
                public const string EcbFinancialStabilityReview = "EUR/ecb financial stability review";
                /// <summary>
                /// Ecb Forum On Central Banking
                /// </summary>
                public const string EcbForumOnCentralBanking = "EUR/ecb forum on central banking";
                /// <summary>
                /// Ecb General Council Meeting
                /// </summary>
                public const string EcbGeneralCouncilMeeting = "EUR/ecb general council meeting";
                /// <summary>
                /// Ecb Guindos Speech
                /// </summary>
                public const string EcbGuindosSpeech = "EUR/ecb guindos speech";
                /// <summary>
                /// Ecb Hakkarainen Speech
                /// </summary>
                public const string EcbHakkarainenSpeech = "EUR/ecb hakkarainen speech";
                /// <summary>
                /// Ecb Interest Rate Decision
                /// </summary>
                public const string EcbInterestRateDecision = "EUR/ecb interest rate decision";
                /// <summary>
                /// Ecb Jochnick Speech
                /// </summary>
                public const string EcbJochnickSpeech = "EUR/ecb jochnick speech";
                /// <summary>
                /// Ecb Lane Speech
                /// </summary>
                public const string EcbLaneSpeech = "EUR/ecb lane speech";
                /// <summary>
                /// Ecb Lautenschlger Speech
                /// </summary>
                public const string EcbLautenschlgerSpeech = "EUR/ecb lautenschlger speech";
                /// <summary>
                /// Ecb Mccaul Speech
                /// </summary>
                public const string EcbMccaulSpeech = "EUR/ecb mccaul speech";
                /// <summary>
                /// Ecb Mersch Speech
                /// </summary>
                public const string EcbMerschSpeech = "EUR/ecb mersch speech";
                /// <summary>
                /// Ecb Monetary Policy Meeting Accounts
                /// </summary>
                public const string EcbMonetaryPolicyMeetingAccounts = "EUR/ecb monetary policy meeting accounts";
                /// <summary>
                /// Ecb Non Monetary Policy Meeting
                /// </summary>
                public const string EcbNonMonetaryPolicyMeeting = "EUR/ecb non monetary policy meeting";
                /// <summary>
                /// Ecb Panetta Speech
                /// </summary>
                public const string EcbPanettaSpeech = "EUR/ecb panetta speech";
                /// <summary>
                /// Ecb Praet Speech
                /// </summary>
                public const string EcbPraetSpeech = "EUR/ecb praet speech";
                /// <summary>
                /// Ecb President Draghi Speech
                /// </summary>
                public const string EcbPresidentDraghiSpeech = "EUR/ecb president draghi speech";
                /// <summary>
                /// Ecb President Lagarde Speech
                /// </summary>
                public const string EcbPresidentLagardeSpeech = "EUR/ecb president lagarde speech";
                /// <summary>
                /// Ecb Press Conference
                /// </summary>
                public const string EcbPressConference = "EUR/ecb press conference";
                /// <summary>
                /// Ecb Schnabel Speech
                /// </summary>
                public const string EcbSchnabelSpeech = "EUR/ecb schnabel speech";
                /// <summary>
                /// Ecb Survey Of Professional Forecasters
                /// </summary>
                public const string EcbSurveyOfProfessionalForecasters = "EUR/ecb survey of professional forecasters";
                /// <summary>
                /// Ecb Tuominen Speech
                /// </summary>
                public const string EcbTuominenSpeech = "EUR/ecb tuominen speech";
                /// <summary>
                /// Ecofin Meeting
                /// </summary>
                public const string EcofinMeeting = "EUR/ecofin meeting";
                /// <summary>
                /// Economic Sentiment
                /// </summary>
                public const string EconomicSentiment = "EUR/economic sentiment";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "EUR/employment change";
                /// <summary>
                /// Eu Bonds Auction
                /// </summary>
                public const string EuBondsAuction = "EUR/eu bonds auction";
                /// <summary>
                /// Eurogroup Meeting
                /// </summary>
                public const string EurogroupMeeting = "EUR/eurogroup meeting";
                /// <summary>
                /// Eurogroup Video Conference
                /// </summary>
                public const string EurogroupVideoConference = "EUR/eurogroup video conference";
                /// <summary>
                /// European Commission Forecasts
                /// </summary>
                public const string EuropeanCommissionForecasts = "EUR/european commission forecasts";
                /// <summary>
                /// European Council Meeting
                /// </summary>
                public const string EuropeanCouncilMeeting = "EUR/european council meeting";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "EUR/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq 3Rd
                /// </summary>
                public const string GdpGrowthRateQoq3Rd = "EUR/gdp growth rate qoq 3rd";
                /// <summary>
                /// Gdp Growth Rate Yoy 3Rd
                /// </summary>
                public const string GdpGrowthRateYoy3Rd = "EUR/gdp growth rate yoy 3rd";
                /// <summary>
                /// Government Debt To Gdp
                /// </summary>
                public const string GovernmentDebtToGdp = "EUR/government debt to gdp";
                /// <summary>
                /// Hcob Composite Purchasing Managers Index
                /// </summary>
                public const string HcobCompositePurchasingManagersIndex = "EUR/hcob composite purchasing managers index";
                /// <summary>
                /// Hcob Construction Purchasing Managers Index
                /// </summary>
                public const string HcobConstructionPurchasingManagersIndex = "EUR/hcob construction purchasing managers index";
                /// <summary>
                /// Hcob Manufacturing Purchasing Managers Index
                /// </summary>
                public const string HcobManufacturingPurchasingManagersIndex = "EUR/hcob manufacturing purchasing managers index";
                /// <summary>
                /// Hcob Services Purchasing Managers Index
                /// </summary>
                public const string HcobServicesPurchasingManagersIndex = "EUR/hcob services purchasing managers index";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "EUR/industrial production";
                /// <summary>
                /// Industrial Sentiment
                /// </summary>
                public const string IndustrialSentiment = "EUR/industrial sentiment";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "EUR/inflation rate";
                /// <summary>
                /// Labor Costs Index
                /// </summary>
                public const string LaborCostsIndex = "EUR/labor costs index";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "EUR/loan growth";
                /// <summary>
                /// Loans To Companies
                /// </summary>
                public const string LoansToCompanies = "EUR/loans to companies";
                /// <summary>
                /// Loans To Households
                /// </summary>
                public const string LoansToHouseholds = "EUR/loans to households";
                /// <summary>
                /// M3 Money Supply
                /// </summary>
                public const string M3MoneySupply = "EUR/m3 money supply";
                /// <summary>
                /// Marginal Lending Rate
                /// </summary>
                public const string MarginalLendingRate = "EUR/marginal lending rate";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "EUR/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "EUR/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "EUR/markit services purchasing managers index";
                /// <summary>
                /// New Car Registrations
                /// </summary>
                public const string NewCarRegistrations = "EUR/new car registrations";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "EUR/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "EUR/retail sales";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "EUR/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Construction Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalConstructionPurchasingManagersIndex = "EUR/sandp global construction purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "EUR/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "EUR/sandp global services purchasing managers index";
                /// <summary>
                /// Selling Price Expectations
                /// </summary>
                public const string SellingPriceExpectations = "EUR/selling price expectations";
                /// <summary>
                /// Services Sentiment
                /// </summary>
                public const string ServicesSentiment = "EUR/services sentiment";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "EUR/unemployment rate";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "EUR/wage growth";
                /// <summary>
                /// Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "EUR/zew economic sentiment index";
            }

            /// <summary>
            /// Finland
            /// </summary>
            public static class Finland
            {
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "FIN/business confidence";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "FIN/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "FIN/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "FIN/easter monday";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "FIN/export prices";
                /// <summary>
                /// Gdp
                /// </summary>
                public const string Gdp = "FIN/gdp";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "FIN/gdp growth rate";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "FIN/import prices";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "FIN/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "FIN/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "FIN/ppi";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "FIN/unemployment rate";
            }

            /// <summary>
            /// France
            /// </summary>
            public static class France
            {
                /// <summary>
                /// 12 Month Btf Auction
                /// </summary>
                public const string TwelveMonthBtfAuction = "FRA/12 month btf auction";
                /// <summary>
                /// 20 Year Oat Auction
                /// </summary>
                public const string TwentyYearOatAuction = "FRA/20 year oat auction";
                /// <summary>
                /// 3 Month Btf Auction
                /// </summary>
                public const string ThreeMonthBtfAuction = "FRA/3 month btf auction";
                /// <summary>
                /// 3 Year Btan Auction
                /// </summary>
                public const string ThreeYearBtanAuction = "FRA/3 year btan auction";
                /// <summary>
                /// 3 Year Oat Auction
                /// </summary>
                public const string ThreeYearOatAuction = "FRA/3 year oat auction";
                /// <summary>
                /// 30 Year Oat Auction
                /// </summary>
                public const string ThirtyYearOatAuction = "FRA/30 year oat auction";
                /// <summary>
                /// 5 Year Btan Auction
                /// </summary>
                public const string FiveYearBtanAuction = "FRA/5 year btan auction";
                /// <summary>
                /// 5 Year Oat Auction
                /// </summary>
                public const string FiveYearOatAuction = "FRA/5 year oat auction";
                /// <summary>
                /// 6 Month Btf Auction
                /// </summary>
                public const string SixMonthBtfAuction = "FRA/6 month btf auction";
                /// <summary>
                /// Assumption Of Mary
                /// </summary>
                public const string AssumptionOfMary = "FRA/assumption of mary";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "FRA/balance of trade";
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "FRA/budget balance";
                /// <summary>
                /// Business Climate Indicators
                /// </summary>
                public const string BusinessClimateIndicators = "FRA/business climate indicators";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "FRA/business confidence";
                /// <summary>
                /// Construction Purchasing Managers Index
                /// </summary>
                public const string ConstructionPurchasingManagersIndex = "FRA/construction purchasing managers index";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "FRA/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "FRA/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "FRA/easter monday";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "FRA/exports";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "FRA/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "FRA/gdp growth rate";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "FRA/harmonized inflation rate";
                /// <summary>
                /// Hcob Composite Purchasing Managers Index
                /// </summary>
                public const string HcobCompositePurchasingManagersIndex = "FRA/hcob composite purchasing managers index";
                /// <summary>
                /// Hcob Construction Purchasing Managers Index
                /// </summary>
                public const string HcobConstructionPurchasingManagersIndex = "FRA/hcob construction purchasing managers index";
                /// <summary>
                /// Hcob Manufacturing Purchasing Managers Index
                /// </summary>
                public const string HcobManufacturingPurchasingManagersIndex = "FRA/hcob manufacturing purchasing managers index";
                /// <summary>
                /// Hcob Services Purchasing Managers Index
                /// </summary>
                public const string HcobServicesPurchasingManagersIndex = "FRA/hcob services purchasing managers index";
                /// <summary>
                /// Household Consumption
                /// </summary>
                public const string HouseholdConsumption = "FRA/household consumption";
                /// <summary>
                /// Iea Oil Market Report
                /// </summary>
                public const string IeaOilMarketReport = "FRA/iea oil market report";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "FRA/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "FRA/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "FRA/inflation rate";
                /// <summary>
                /// Jobseekers Total
                /// </summary>
                public const string JobseekersTotal = "FRA/jobseekers total";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "FRA/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "FRA/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "FRA/markit services purchasing managers index";
                /// <summary>
                /// New Car Registrations
                /// </summary>
                public const string NewCarRegistrations = "FRA/new car registrations";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "FRA/non farm payrolls";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "FRA/ppi";
                /// <summary>
                /// Private Non Farm Payrolls
                /// </summary>
                public const string PrivateNonFarmPayrolls = "FRA/private non farm payrolls";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "FRA/retail sales";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "FRA/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Construction Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalConstructionPurchasingManagersIndex = "FRA/sandp global construction purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "FRA/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "FRA/sandp global services purchasing managers index";
                /// <summary>
                /// Unemployment Benefit Claims
                /// </summary>
                public const string UnemploymentBenefitClaims = "FRA/unemployment benefit claims";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "FRA/unemployment rate";
            }

            /// <summary>
            /// Unitedkingdom
            /// </summary>
            public static class UnitedKingdom
            {
                /// <summary>
                /// 3 Year Treasury Gilt Auction
                /// </summary>
                public const string ThreeYearTreasuryGiltAuction = "GBR/3 year treasury gilt auction";
                /// <summary>
                /// 30 Year Treasury Gilt Auction
                /// </summary>
                public const string ThirtyYearTreasuryGiltAuction = "GBR/30 year treasury gilt auction";
                /// <summary>
                /// 5 Year Treasury Gilt Auction
                /// </summary>
                public const string FiveYearTreasuryGiltAuction = "GBR/5 year treasury gilt auction";
                /// <summary>
                /// Average Earnings Excluding Bonus
                /// </summary>
                public const string AverageEarningsExcludingBonus = "GBR/average earnings excluding bonus";
                /// <summary>
                /// Average Earnings Including Bonus
                /// </summary>
                public const string AverageEarningsIncludingBonus = "GBR/average earnings including bonus";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "GBR/balance of trade";
                /// <summary>
                /// Bank Of England Breeden Speech
                /// </summary>
                public const string BankOfEnglandBreedenSpeech = "GBR/bank of england breeden speech";
                /// <summary>
                /// Bank Of England Broadbent Speech
                /// </summary>
                public const string BankOfEnglandBroadbentSpeech = "GBR/bank of england broadbent speech";
                /// <summary>
                /// Bank Of England Consumer Credit
                /// </summary>
                public const string BankOfEnglandConsumerCredit = "GBR/bank of england consumer credit";
                /// <summary>
                /// Bank Of England Cunliffe Speech
                /// </summary>
                public const string BankOfEnglandCunliffeSpeech = "GBR/bank of england cunliffe speech";
                /// <summary>
                /// Bank Of England Dhingra Speech
                /// </summary>
                public const string BankOfEnglandDhingraSpeech = "GBR/bank of england dhingra speech";
                /// <summary>
                /// Bank Of England Fpc Meeting
                /// </summary>
                public const string BankOfEnglandFpcMeeting = "GBR/bank of england fpc meeting";
                /// <summary>
                /// Bank Of England Gov Bailey Speech
                /// </summary>
                public const string BankOfEnglandGovBaileySpeech = "GBR/bank of england gov bailey speech";
                /// <summary>
                /// Bank Of England Gov Carney Speech
                /// </summary>
                public const string BankOfEnglandGovCarneySpeech = "GBR/bank of england gov carney speech";
                /// <summary>
                /// Bank Of England Haldane Speech
                /// </summary>
                public const string BankOfEnglandHaldaneSpeech = "GBR/bank of england haldane speech";
                /// <summary>
                /// Bank Of England Haskel Speech
                /// </summary>
                public const string BankOfEnglandHaskelSpeech = "GBR/bank of england haskel speech";
                /// <summary>
                /// Bank Of England Interest Rate Decision
                /// </summary>
                public const string BankOfEnglandInterestRateDecision = "GBR/bank of england interest rate decision";
                /// <summary>
                /// Bank Of England L Mann Speech
                /// </summary>
                public const string BankOfEnglandLMannSpeech = "GBR/bank of england l mann speech";
                /// <summary>
                /// Bank Of England Mann Speech
                /// </summary>
                public const string BankOfEnglandMannSpeech = "GBR/bank of england mann speech";
                /// <summary>
                /// Bank Of England Monetary Policy Report
                /// </summary>
                public const string BankOfEnglandMonetaryPolicyReport = "GBR/bank of england monetary policy report";
                /// <summary>
                /// Bank Of England Mpc Vote Cut
                /// </summary>
                public const string BankOfEnglandMpcVoteCut = "GBR/bank of england mpc vote cut";
                /// <summary>
                /// Bank Of England Mpc Vote Hike
                /// </summary>
                public const string BankOfEnglandMpcVoteHike = "GBR/bank of england mpc vote hike";
                /// <summary>
                /// Bank Of England Mpc Vote Unchanged
                /// </summary>
                public const string BankOfEnglandMpcVoteUnchanged = "GBR/bank of england mpc vote unchanged";
                /// <summary>
                /// Bank Of England Pill Speech
                /// </summary>
                public const string BankOfEnglandPillSpeech = "GBR/bank of england pill speech";
                /// <summary>
                /// Bank Of England Quantitative Easing
                /// </summary>
                public const string BankOfEnglandQuantitativeEasing = "GBR/bank of england quantitative easing";
                /// <summary>
                /// Bank Of England Quarterly Bulletin
                /// </summary>
                public const string BankOfEnglandQuarterlyBulletin = "GBR/bank of england quarterly bulletin";
                /// <summary>
                /// Bank Of England Ramsden Speech
                /// </summary>
                public const string BankOfEnglandRamsdenSpeech = "GBR/bank of england ramsden speech";
                /// <summary>
                /// Bank Of England Saunders Speech
                /// </summary>
                public const string BankOfEnglandSaundersSpeech = "GBR/bank of england saunders speech";
                /// <summary>
                /// Bank Of England Tenreyro Speech
                /// </summary>
                public const string BankOfEnglandTenreyroSpeech = "GBR/bank of england tenreyro speech";
                /// <summary>
                /// Bank Of England Vlieghe Speech
                /// </summary>
                public const string BankOfEnglandVliegheSpeech = "GBR/bank of england vlieghe speech";
                /// <summary>
                /// Bank Of England Woods Speech
                /// </summary>
                public const string BankOfEnglandWoodsSpeech = "GBR/bank of england woods speech";
                /// <summary>
                /// Bba Mortgage Rate
                /// </summary>
                public const string BbaMortgageRate = "GBR/bba mortgage rate";
                /// <summary>
                /// Brc Retail Sales Monitor
                /// </summary>
                public const string BrcRetailSalesMonitor = "GBR/brc retail sales monitor";
                /// <summary>
                /// Business Investment
                /// </summary>
                public const string BusinessInvestment = "GBR/business investment";
                /// <summary>
                /// Car Production
                /// </summary>
                public const string CarProduction = "GBR/car production";
                /// <summary>
                /// Cbi Business Optimism Index
                /// </summary>
                public const string CbiBusinessOptimismIndex = "GBR/cbi business optimism index";
                /// <summary>
                /// Cbi Distributive Trades
                /// </summary>
                public const string CbiDistributiveTrades = "GBR/cbi distributive trades";
                /// <summary>
                /// Cbi Industrial Trends Orders
                /// </summary>
                public const string CbiIndustrialTrendsOrders = "GBR/cbi industrial trends orders";
                /// <summary>
                /// Claimant Count Change
                /// </summary>
                public const string ClaimantCountChange = "GBR/claimant count change";
                /// <summary>
                /// Construction Orders
                /// </summary>
                public const string ConstructionOrders = "GBR/construction orders";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "GBR/construction output";
                /// <summary>
                /// Construction Purchasing Managers Index
                /// </summary>
                public const string ConstructionPurchasingManagersIndex = "GBR/construction purchasing managers index";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "GBR/core inflation rate";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "GBR/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "GBR/easter monday";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "GBR/employment change";
                /// <summary>
                /// Gdp
                /// </summary>
                public const string Gdp = "GBR/gdp";
                /// <summary>
                /// Gdp 3 Month Average
                /// </summary>
                public const string GdpThreeMonthAverage = "GBR/gdp 3 month average";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "GBR/gdp growth rate";
                /// <summary>
                /// Gfk Consumer Confidence
                /// </summary>
                public const string GfkConsumerConfidence = "GBR/gfk consumer confidence";
                /// <summary>
                /// Goods Trade Balance
                /// </summary>
                public const string GoodsTradeBalance = "GBR/goods trade balance";
                /// <summary>
                /// Goods Trade Balance Non Eu
                /// </summary>
                public const string GoodsTradeBalanceNonEu = "GBR/goods trade balance non eu";
                /// <summary>
                /// Halifax House Price Index
                /// </summary>
                public const string HalifaxHousePriceIndex = "GBR/halifax house price index";
                /// <summary>
                /// Hmrc Payrolls Change
                /// </summary>
                public const string HmrcPayrollsChange = "GBR/hmrc payrolls change";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GBR/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "GBR/inflation rate";
                /// <summary>
                /// Labor Productivity
                /// </summary>
                public const string LaborProductivity = "GBR/labor productivity";
                /// <summary>
                /// Late Summer Bank Holiday
                /// </summary>
                public const string LateSummerBankHoliday = "GBR/late summer bank holiday";
                /// <summary>
                /// M4 Money Supply
                /// </summary>
                public const string M4MoneySupply = "GBR/m4 money supply";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "GBR/manufacturing production";
                /// <summary>
                /// Markit Cips Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCipsCompositePurchasingManagersIndex = "GBR/markit cips composite purchasing managers index";
                /// <summary>
                /// Markit Cips Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitCipsManufacturingPurchasingManagersIndex = "GBR/markit cips manufacturing purchasing managers index";
                /// <summary>
                /// Markit Cips Uk Services Purchasing Managers Index
                /// </summary>
                public const string MarkitCipsUkServicesPurchasingManagersIndex = "GBR/markit cips uk services purchasing managers index";
                /// <summary>
                /// Monetary Policy Report
                /// </summary>
                public const string MonetaryPolicyReport = "GBR/monetary policy report";
                /// <summary>
                /// Mortgage Approvals
                /// </summary>
                public const string MortgageApprovals = "GBR/mortgage approvals";
                /// <summary>
                /// Mortgage Lending
                /// </summary>
                public const string MortgageLending = "GBR/mortgage lending";
                /// <summary>
                /// Mpc Meeting Minutes
                /// </summary>
                public const string MpcMeetingMinutes = "GBR/mpc meeting minutes";
                /// <summary>
                /// Nationwide Housing Prices
                /// </summary>
                public const string NationwideHousingPrices = "GBR/nationwide housing prices";
                /// <summary>
                /// Net Lending To Individuals
                /// </summary>
                public const string NetLendingToIndividuals = "GBR/net lending to individuals";
                /// <summary>
                /// New Car Sales
                /// </summary>
                public const string NewCarSales = "GBR/new car sales";
                /// <summary>
                /// Niesr Monthly Gdp Tracker
                /// </summary>
                public const string NiesrMonthlyGdpTracker = "GBR/niesr monthly gdp tracker";
                /// <summary>
                /// Producer Price Index Core Output
                /// </summary>
                public const string ProducerPriceIndexCoreOutput = "GBR/producer price index core output";
                /// <summary>
                /// Producer Price Index Input
                /// </summary>
                public const string ProducerPriceIndexInput = "GBR/producer price index input";
                /// <summary>
                /// Producer Price Index Output
                /// </summary>
                public const string ProducerPriceIndexOutput = "GBR/producer price index output";
                /// <summary>
                /// Public Sector Net Borrowing
                /// </summary>
                public const string PublicSectorNetBorrowing = "GBR/public sector net borrowing";
                /// <summary>
                /// Public Sector Net Borrowing Excluding Banks
                /// </summary>
                public const string PublicSectorNetBorrowingExcludingBanks = "GBR/public sector net borrowing excluding banks";
                /// <summary>
                /// Retail Price Index
                /// </summary>
                public const string RetailPriceIndex = "GBR/retail price index";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "GBR/retail sales";
                /// <summary>
                /// Retail Sales Excluding Fuel
                /// </summary>
                public const string RetailSalesExcludingFuel = "GBR/retail sales excluding fuel";
                /// <summary>
                /// Rics House Price Balance
                /// </summary>
                public const string RicsHousePriceBalance = "GBR/rics house price balance";
                /// <summary>
                /// Sandp Global Cips Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCipsCompositePurchasingManagersIndex = "GBR/sandp global cips composite purchasing managers index";
                /// <summary>
                /// Sandp Global Cips Construction Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCipsConstructionPurchasingManagersIndex = "GBR/sandp global cips construction purchasing managers index";
                /// <summary>
                /// Sandp Global Cips Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCipsManufacturingPurchasingManagersIndex = "GBR/sandp global cips manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Cips Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCipsServicesPurchasingManagersIndex = "GBR/sandp global cips services purchasing managers index";
                /// <summary>
                /// Sandp Global Cips Uk Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCipsUkServicesPurchasingManagersIndex = "GBR/sandp global cips uk services purchasing managers index";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "GBR/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Construction Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalConstructionPurchasingManagersIndex = "GBR/sandp global construction purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "GBR/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "GBR/sandp global services purchasing managers index";
                /// <summary>
                /// Spring Bank Holiday
                /// </summary>
                public const string SpringBankHoliday = "GBR/spring bank holiday";
                /// <summary>
                /// Uk Finance Mortgage Approvals
                /// </summary>
                public const string UkFinanceMortgageApprovals = "GBR/uk finance mortgage approvals";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "GBR/unemployment rate";
            }

            /// <summary>
            /// Georgia
            /// </summary>
            public static class Georgia
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "GEO/inflation rate";
            }

            /// <summary>
            /// Greece
            /// </summary>
            public static class Greece
            {
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GRC/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "GRC/current account";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "GRC/gdp growth rate";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "GRC/harmonized inflation rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GRC/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "GRC/inflation rate";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "GRC/markit manufacturing purchasing managers index";
                /// <summary>
                /// Orthodox Easter Monday
                /// </summary>
                public const string OrthodoxEasterMonday = "GRC/orthodox easter monday";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "GRC/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "GRC/retail sales";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "GRC/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Total Credit
                /// </summary>
                public const string TotalCredit = "GRC/total credit";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "GRC/unemployment rate";
            }

            /// <summary>
            /// Hongkong
            /// </summary>
            public static class HongKong
            {
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "HKG/business confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "HKG/current account";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "HKG/exports";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "HKG/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "HKG/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq Adv
                /// </summary>
                public const string GdpGrowthRateQoqAdv = "HKG/gdp growth rate qoq adv";
                /// <summary>
                /// Gdp Growth Rate Yoy Adv
                /// </summary>
                public const string GdpGrowthRateYoyAdv = "HKG/gdp growth rate yoy adv";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "HKG/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "HKG/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "HKG/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "HKG/interest rate decision";
                /// <summary>
                /// Markit Purchasing Managers Index
                /// </summary>
                public const string MarkitPurchasingManagersIndex = "HKG/markit purchasing managers index";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "HKG/retail sales";
                /// <summary>
                /// Sandp Global Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalPurchasingManagersIndex = "HKG/sandp global purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "HKG/unemployment rate";
            }

            /// <summary>
            /// Croatia
            /// </summary>
            public static class Croatia
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "HRV/inflation rate";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "HRV/retail sales";
            }

            /// <summary>
            /// Hungary
            /// </summary>
            public static class Hungary
            {
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "HUN/balance of trade";
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "HUN/budget balance";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "HUN/construction output";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "HUN/core inflation rate";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "HUN/current account";
                /// <summary>
                /// Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "HUN/deposit interest rate";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "HUN/easter monday";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "HUN/gdp growth rate";
                /// <summary>
                /// Gross Wage
                /// </summary>
                public const string GrossWage = "HUN/gross wage";
                /// <summary>
                /// Halpim Manufacturing Purchasing Managers Index
                /// </summary>
                public const string HalpimManufacturingPurchasingManagersIndex = "HUN/halpim manufacturing purchasing managers index";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "HUN/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "HUN/inflation rate";
                /// <summary>
                /// Inflation Report
                /// </summary>
                public const string InflationReport = "HUN/inflation report";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "HUN/interest rate decision";
                /// <summary>
                /// Monetary Policy Meeting Minutes
                /// </summary>
                public const string MonetaryPolicyMeetingMinutes = "HUN/monetary policy meeting minutes";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "HUN/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "HUN/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "HUN/unemployment rate";
            }

            /// <summary>
            /// Indonesia
            /// </summary>
            public static class Indonesia
            {
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "IDN/balance of trade";
                /// <summary>
                /// Car Sales
                /// </summary>
                public const string CarSales = "IDN/car sales";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "IDN/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "IDN/core inflation rate";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "IDN/current account";
                /// <summary>
                /// Deposit Facility Rate
                /// </summary>
                public const string DepositFacilityRate = "IDN/deposit facility rate";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "IDN/exports";
                /// <summary>
                /// Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "IDN/foreign direct investment";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "IDN/foreign exchange reserves";
                /// <summary>
                /// Full Year Gdp Growth
                /// </summary>
                public const string FullYearGdpGrowth = "IDN/full year gdp growth";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "IDN/gdp growth rate";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "IDN/imports";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "IDN/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "IDN/interest rate decision";
                /// <summary>
                /// Lending Facility Rate
                /// </summary>
                public const string LendingFacilityRate = "IDN/lending facility rate";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "IDN/loan growth";
                /// <summary>
                /// M2 Money Supply
                /// </summary>
                public const string M2MoneySupply = "IDN/m2 money supply";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "IDN/markit manufacturing purchasing managers index";
                /// <summary>
                /// Motorbike Sales
                /// </summary>
                public const string MotorbikeSales = "IDN/motorbike sales";
                /// <summary>
                /// Property Price Index
                /// </summary>
                public const string PropertyPriceIndex = "IDN/property price index";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "IDN/retail sales";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "IDN/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "IDN/tourist arrivals";
            }

            /// <summary>
            /// India
            /// </summary>
            public static class India
            {
                /// <summary>
                /// Bank Loan Growth
                /// </summary>
                public const string BankLoanGrowth = "IND/bank loan growth";
                /// <summary>
                /// Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "IND/cash reserve ratio";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "IND/current account";
                /// <summary>
                /// Deposit Growth
                /// </summary>
                public const string DepositGrowth = "IND/deposit growth";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "IND/exports";
                /// <summary>
                /// External Debt
                /// </summary>
                public const string ExternalDebt = "IND/external debt";
                /// <summary>
                /// Fiscal Year Gdp Growth
                /// </summary>
                public const string FiscalYearGdpGrowth = "IND/fiscal year gdp growth";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "IND/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "IND/gdp growth rate";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "IND/government budget value";
                /// <summary>
                /// Hsbc Composite Purchasing Managers Index
                /// </summary>
                public const string HsbcCompositePurchasingManagersIndex = "IND/hsbc composite purchasing managers index";
                /// <summary>
                /// Hsbc Manufacturing Purchasing Managers Index
                /// </summary>
                public const string HsbcManufacturingPurchasingManagersIndex = "IND/hsbc manufacturing purchasing managers index";
                /// <summary>
                /// Hsbc Services Purchasing Managers Index
                /// </summary>
                public const string HsbcServicesPurchasingManagersIndex = "IND/hsbc services purchasing managers index";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "IND/imports";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "IND/independence day";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "IND/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "IND/inflation rate";
                /// <summary>
                /// Infrastructure Output
                /// </summary>
                public const string InfrastructureOutput = "IND/infrastructure output";
                /// <summary>
                /// M3 Money Supply
                /// </summary>
                public const string M3MoneySupply = "IND/m3 money supply";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "IND/manufacturing production";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "IND/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "IND/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "IND/markit services purchasing managers index";
                /// <summary>
                /// Monetary Policy Meeting Minutes
                /// </summary>
                public const string MonetaryPolicyMeetingMinutes = "IND/monetary policy meeting minutes";
                /// <summary>
                /// Nikkei Services Purchasing Managers Index
                /// </summary>
                public const string NikkeiServicesPurchasingManagersIndex = "IND/nikkei services purchasing managers index";
                /// <summary>
                /// Passenger Vehicles Sales
                /// </summary>
                public const string PassengerVehiclesSales = "IND/passenger vehicles sales";
                /// <summary>
                /// Rbi Interest Rate Decision
                /// </summary>
                public const string RbiInterestRateDecision = "IND/rbi interest rate decision";
                /// <summary>
                /// Reverse Repo Rate
                /// </summary>
                public const string ReverseRepoRate = "IND/reverse repo rate";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "IND/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "IND/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "IND/sandp global services purchasing managers index";
                /// <summary>
                /// Wpi Food
                /// </summary>
                public const string WpiFood = "IND/wpi food";
                /// <summary>
                /// Wpi Food Index
                /// </summary>
                public const string WpiFoodIndex = "IND/wpi food index";
                /// <summary>
                /// Wpi Fuel
                /// </summary>
                public const string WpiFuel = "IND/wpi fuel";
                /// <summary>
                /// Wpi Inflation
                /// </summary>
                public const string WpiInflation = "IND/wpi inflation";
                /// <summary>
                /// Wpi Manufacturing
                /// </summary>
                public const string WpiManufacturing = "IND/wpi manufacturing";
            }

            /// <summary>
            /// Ireland
            /// </summary>
            public static class Ireland
            {
                /// <summary>
                /// Aib Manufacturing Purchasing Managers Index
                /// </summary>
                public const string AibManufacturingPurchasingManagersIndex = "IRL/aib manufacturing purchasing managers index";
                /// <summary>
                /// Aib Services Purchasing Managers Index
                /// </summary>
                public const string AibServicesPurchasingManagersIndex = "IRL/aib services purchasing managers index";
                /// <summary>
                /// August Bank Holiday
                /// </summary>
                public const string AugustBankHoliday = "IRL/august bank holiday";
                /// <summary>
                /// Average Weekly Earnings
                /// </summary>
                public const string AverageWeeklyEarnings = "IRL/average weekly earnings";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "IRL/construction output";
                /// <summary>
                /// Construction Purchasing Managers Index
                /// </summary>
                public const string ConstructionPurchasingManagersIndex = "IRL/construction purchasing managers index";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "IRL/consumer confidence";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "IRL/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "IRL/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "IRL/easter monday";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "IRL/gdp growth rate";
                /// <summary>
                /// Gnp
                /// </summary>
                public const string Gnp = "IRL/gnp";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "IRL/harmonized inflation rate";
                /// <summary>
                /// Household Saving Ratio
                /// </summary>
                public const string HouseholdSavingRatio = "IRL/household saving ratio";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "IRL/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "IRL/inflation rate";
                /// <summary>
                /// June Bank Holiday
                /// </summary>
                public const string JuneBankHoliday = "IRL/june bank holiday";
                /// <summary>
                /// Residential Property Prices
                /// </summary>
                public const string ResidentialPropertyPrices = "IRL/residential property prices";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "IRL/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "IRL/unemployment rate";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "IRL/wholesale prices";
            }

            /// <summary>
            /// Iceland
            /// </summary>
            public static class Iceland
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ISL/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "ISL/ppi";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ISL/unemployment rate";
            }

            /// <summary>
            /// Israel
            /// </summary>
            public static class Israel
            {
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "ISR/budget balance";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ISR/business confidence";
                /// <summary>
                /// Composite Economic Index
                /// </summary>
                public const string CompositeEconomicIndex = "ISR/composite economic index";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ISR/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "ISR/current account";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "ISR/exports";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "ISR/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "ISR/gdp growth annualized";
                /// <summary>
                /// Gdp Growth Annualized 3Rd
                /// </summary>
                public const string GdpGrowthAnnualized3Rd = "ISR/gdp growth annualized 3rd";
                /// <summary>
                /// Gdp Growth Annualized First
                /// </summary>
                public const string GdpGrowthAnnualizedFirst = "ISR/gdp growth annualized first";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ISR/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq 2St
                /// </summary>
                public const string GdpGrowthRateQoq2St = "ISR/gdp growth rate qoq 2st";
                /// <summary>
                /// Gdp Growth Rate Qoq 3Rd
                /// </summary>
                public const string GdpGrowthRateQoq3Rd = "ISR/gdp growth rate qoq 3rd";
                /// <summary>
                /// Gdp Growth Rate Qoq First
                /// </summary>
                public const string GdpGrowthRateQoqFirst = "ISR/gdp growth rate qoq first";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "ISR/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ISR/industrial production";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "ISR/inflation expectations";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ISR/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "ISR/interest rate decision";
                /// <summary>
                /// M1 Money Supply
                /// </summary>
                public const string M1MoneySupply = "ISR/m1 money supply";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "ISR/manufacturing production";
                /// <summary>
                /// Manufacturing Purchasing Managers Index
                /// </summary>
                public const string ManufacturingPurchasingManagersIndex = "ISR/manufacturing purchasing managers index";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "ISR/tourist arrivals";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ISR/unemployment rate";
                /// <summary>
                /// Yom Kippur
                /// </summary>
                public const string YomKippur = "ISR/yom kippur";
            }

            /// <summary>
            /// Italy
            /// </summary>
            public static class Italy
            {
                /// <summary>
                /// 15 Year Btp Auction
                /// </summary>
                public const string FifteenYearBtpAuction = "ITA/15 year btp auction";
                /// <summary>
                /// 2 Year Btp Short Term Auction
                /// </summary>
                public const string TwoYearBtpShortTermAuction = "ITA/2 year btp short term auction";
                /// <summary>
                /// 20 Year Btp Auction
                /// </summary>
                public const string TwentyYearBtpAuction = "ITA/20 year btp auction";
                /// <summary>
                /// 3 Year Btp Auction
                /// </summary>
                public const string ThreeYearBtpAuction = "ITA/3 year btp auction";
                /// <summary>
                /// 30 Year Btp Auction
                /// </summary>
                public const string ThirtyYearBtpAuction = "ITA/30 year btp auction";
                /// <summary>
                /// 5 Year Btp Auction
                /// </summary>
                public const string FiveYearBtpAuction = "ITA/5 year btp auction";
                /// <summary>
                /// 5 Year Btpi Auction
                /// </summary>
                public const string FiveYearBtpiAuction = "ITA/5 year btpi auction";
                /// <summary>
                /// 6 Month Bot Auction
                /// </summary>
                public const string SixMonthBotAuction = "ITA/6 month bot auction";
                /// <summary>
                /// 7 Year Btp Auction
                /// </summary>
                public const string SevenYearBtpAuction = "ITA/7 year btp auction";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "ITA/balance of trade";
                /// <summary>
                /// Btp Index Linked Auction
                /// </summary>
                public const string BtpIndexLinkedAuction = "ITA/btp index linked auction";
                /// <summary>
                /// Btp Short Term
                /// </summary>
                public const string BtpShortTerm = "ITA/btp short term";
                /// <summary>
                /// Btp Short Term Auction
                /// </summary>
                public const string BtpShortTermAuction = "ITA/btp short term auction";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ITA/business confidence";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "ITA/construction output";
                /// <summary>
                /// Construction Purchasing Managers Index
                /// </summary>
                public const string ConstructionPurchasingManagersIndex = "ITA/construction purchasing managers index";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ITA/consumer confidence";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "ITA/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "ITA/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "ITA/easter monday";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ITA/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq Adv
                /// </summary>
                public const string GdpGrowthRateQoqAdv = "ITA/gdp growth rate qoq adv";
                /// <summary>
                /// Gdp Growth Rate Yoy Adv
                /// </summary>
                public const string GdpGrowthRateYoyAdv = "ITA/gdp growth rate yoy adv";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "ITA/harmonized inflation rate";
                /// <summary>
                /// Hcob Composite Purchasing Managers Index
                /// </summary>
                public const string HcobCompositePurchasingManagersIndex = "ITA/hcob composite purchasing managers index";
                /// <summary>
                /// Hcob Construction Purchasing Managers Index
                /// </summary>
                public const string HcobConstructionPurchasingManagersIndex = "ITA/hcob construction purchasing managers index";
                /// <summary>
                /// Hcob Manufacturing Purchasing Managers Index
                /// </summary>
                public const string HcobManufacturingPurchasingManagersIndex = "ITA/hcob manufacturing purchasing managers index";
                /// <summary>
                /// Hcob Services Purchasing Managers Index
                /// </summary>
                public const string HcobServicesPurchasingManagersIndex = "ITA/hcob services purchasing managers index";
                /// <summary>
                /// Industrial Orders
                /// </summary>
                public const string IndustrialOrders = "ITA/industrial orders";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ITA/industrial production";
                /// <summary>
                /// Industrial Sales
                /// </summary>
                public const string IndustrialSales = "ITA/industrial sales";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ITA/inflation rate";
                /// <summary>
                /// Liberation Day
                /// </summary>
                public const string LiberationDay = "ITA/liberation day";
                /// <summary>
                /// Markit Adaci Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitAdaciManufacturingPurchasingManagersIndex = "ITA/markit adaci manufacturing purchasing managers index";
                /// <summary>
                /// Markit Adaci Services Purchasing Managers Index
                /// </summary>
                public const string MarkitAdaciServicesPurchasingManagersIndex = "ITA/markit adaci services purchasing managers index";
                /// <summary>
                /// Markit Ihs Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitIhsCompositePurchasingManagersIndex = "ITA/markit ihs composite purchasing managers index";
                /// <summary>
                /// Markit Ihs Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitIhsManufacturingPurchasingManagersIndex = "ITA/markit ihs manufacturing purchasing managers index";
                /// <summary>
                /// Markit Ihs Services Purchasing Managers Index
                /// </summary>
                public const string MarkitIhsServicesPurchasingManagersIndex = "ITA/markit ihs services purchasing managers index";
                /// <summary>
                /// New Car Registrations
                /// </summary>
                public const string NewCarRegistrations = "ITA/new car registrations";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "ITA/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "ITA/retail sales";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "ITA/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Construction Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalConstructionPurchasingManagersIndex = "ITA/sandp global construction purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "ITA/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "ITA/sandp global services purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ITA/unemployment rate";
            }

            /// <summary>
            /// Japan
            /// </summary>
            public static class Japan
            {
                /// <summary>
                /// 2 Year Jgb Auction
                /// </summary>
                public const string TwoYearJgbAuction = "JPN/2 year jgb auction";
                /// <summary>
                /// 3 Month Bill Auction
                /// </summary>
                public const string ThreeMonthBillAuction = "JPN/3 month bill auction";
                /// <summary>
                /// 30 Year Jgb Auction
                /// </summary>
                public const string ThirtyYearJgbAuction = "JPN/30 year jgb auction";
                /// <summary>
                /// 40 Year Jgb Auction
                /// </summary>
                public const string FortyYearJgbAuction = "JPN/40 year jgb auction";
                /// <summary>
                /// 5 Year Jgb Auction
                /// </summary>
                public const string FiveYearJgbAuction = "JPN/5 year jgb auction";
                /// <summary>
                /// 52 Week Bill Auction
                /// </summary>
                public const string FiftyTwoWeekBillAuction = "JPN/52 week bill auction";
                /// <summary>
                /// 6 Month Bill Auction
                /// </summary>
                public const string SixMonthBillAuction = "JPN/6 month bill auction";
                /// <summary>
                /// Average Cash Earnings
                /// </summary>
                public const string AverageCashEarnings = "JPN/average cash earnings";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "JPN/balance of trade";
                /// <summary>
                /// Bank Lending
                /// </summary>
                public const string BankLending = "JPN/bank lending";
                /// <summary>
                /// Bank Of Japan Adachi Speech
                /// </summary>
                public const string BankOfJapanAdachiSpeech = "JPN/bank of japan adachi speech";
                /// <summary>
                /// Bank Of Japan Amamiya Speech
                /// </summary>
                public const string BankOfJapanAmamiyaSpeech = "JPN/bank of japan amamiya speech";
                /// <summary>
                /// Bank Of Japan Gov Kuroda Speech
                /// </summary>
                public const string BankOfJapanGovKurodaSpeech = "JPN/bank of japan gov kuroda speech";
                /// <summary>
                /// Bank Of Japan Gov Ueda Speech
                /// </summary>
                public const string BankOfJapanGovUedaSpeech = "JPN/bank of japan gov ueda speech";
                /// <summary>
                /// Bank Of Japan Interest Rate Decision
                /// </summary>
                public const string BankOfJapanInterestRateDecision = "JPN/bank of japan interest rate decision";
                /// <summary>
                /// Bank Of Japan Kataoka Speech
                /// </summary>
                public const string BankOfJapanKataokaSpeech = "JPN/bank of japan kataoka speech";
                /// <summary>
                /// Bank Of Japan Kuroda Speech
                /// </summary>
                public const string BankOfJapanKurodaSpeech = "JPN/bank of japan kuroda speech";
                /// <summary>
                /// Bank Of Japan Monetary Policy Meeting Minutes
                /// </summary>
                public const string BankOfJapanMonetaryPolicyMeetingMinutes = "JPN/bank of japan monetary policy meeting minutes";
                /// <summary>
                /// Bank Of Japan Nakamura Speech
                /// </summary>
                public const string BankOfJapanNakamuraSpeech = "JPN/bank of japan nakamura speech";
                /// <summary>
                /// Bank Of Japan Quarterly Outlook Report
                /// </summary>
                public const string BankOfJapanQuarterlyOutlookReport = "JPN/bank of japan quarterly outlook report";
                /// <summary>
                /// Bank Of Japan Summary Of Opinions
                /// </summary>
                public const string BankOfJapanSummaryOfOpinions = "JPN/bank of japan summary of opinions";
                /// <summary>
                /// Bank Of Japan Suzuki Speech
                /// </summary>
                public const string BankOfJapanSuzukiSpeech = "JPN/bank of japan suzuki speech";
                /// <summary>
                /// Business Survey Index Large Manufacturing
                /// </summary>
                public const string BusinessSurveyIndexLargeManufacturing = "JPN/business survey index large manufacturing";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "JPN/capacity utilization";
                /// <summary>
                /// Capital Spending
                /// </summary>
                public const string CapitalSpending = "JPN/capital spending";
                /// <summary>
                /// Coincident Index
                /// </summary>
                public const string CoincidentIndex = "JPN/coincident index";
                /// <summary>
                /// Coming Of Age Day
                /// </summary>
                public const string ComingOfAgeDay = "JPN/coming of age day";
                /// <summary>
                /// Constitution Memorial Day
                /// </summary>
                public const string ConstitutionMemorialDay = "JPN/constitution memorial day";
                /// <summary>
                /// Construction Orders
                /// </summary>
                public const string ConstructionOrders = "JPN/construction orders";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "JPN/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "JPN/core inflation rate";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "JPN/current account";
                /// <summary>
                /// Eco Watchers Survey Current
                /// </summary>
                public const string EcoWatchersSurveyCurrent = "JPN/eco watchers survey current";
                /// <summary>
                /// Eco Watchers Survey Outlook
                /// </summary>
                public const string EcoWatchersSurveyOutlook = "JPN/eco watchers survey outlook";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "JPN/exports";
                /// <summary>
                /// Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "JPN/foreign bond investment";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "JPN/foreign exchange reserves";
                /// <summary>
                /// Gdp Capital Expenditure
                /// </summary>
                public const string GdpCapitalExpenditure = "JPN/gdp capital expenditure";
                /// <summary>
                /// Gdp External Demand
                /// </summary>
                public const string GdpExternalDemand = "JPN/gdp external demand";
                /// <summary>
                /// Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "JPN/gdp growth annualized";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "JPN/gdp growth rate";
                /// <summary>
                /// Gdp Price Index
                /// </summary>
                public const string GdpPriceIndex = "JPN/gdp price index";
                /// <summary>
                /// Gdp Private Consumption
                /// </summary>
                public const string GdpPrivateConsumption = "JPN/gdp private consumption";
                /// <summary>
                /// Greenery Day
                /// </summary>
                public const string GreeneryDay = "JPN/greenery day";
                /// <summary>
                /// Household Spending
                /// </summary>
                public const string HouseholdSpending = "JPN/household spending";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "JPN/housing starts";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "JPN/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "JPN/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "JPN/inflation rate";
                /// <summary>
                /// Inflation Rate Excluding Food And Energy
                /// </summary>
                public const string InflationRateExcludingFoodAndEnergy = "JPN/inflation rate excluding food and energy";
                /// <summary>
                /// Jibun Bank Composite Purchasing Managers Index
                /// </summary>
                public const string JibunBankCompositePurchasingManagersIndex = "JPN/jibun bank composite purchasing managers index";
                /// <summary>
                /// Jibun Bank Manufacturing Purchasing Managers Index
                /// </summary>
                public const string JibunBankManufacturingPurchasingManagersIndex = "JPN/jibun bank manufacturing purchasing managers index";
                /// <summary>
                /// Jibun Bank Services Purchasing Managers Index
                /// </summary>
                public const string JibunBankServicesPurchasingManagersIndex = "JPN/jibun bank services purchasing managers index";
                /// <summary>
                /// Jobs Applications Ratio
                /// </summary>
                public const string JobsApplicationsRatio = "JPN/jobs applications ratio";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "JPN/leading economic index";
                /// <summary>
                /// Machine Tool Orders
                /// </summary>
                public const string MachineToolOrders = "JPN/machine tool orders";
                /// <summary>
                /// Machinery Orders
                /// </summary>
                public const string MachineryOrders = "JPN/machinery orders";
                /// <summary>
                /// Mountain Day
                /// </summary>
                public const string MountainDay = "JPN/mountain day";
                /// <summary>
                /// New Years Day
                /// </summary>
                public const string NewYearsDay = "JPN/new years day";
                /// <summary>
                /// Nikkei Manufacturing Purchasing Managers Index
                /// </summary>
                public const string NikkeiManufacturingPurchasingManagersIndex = "JPN/nikkei manufacturing purchasing managers index";
                /// <summary>
                /// Nikkei Services Purchasing Managers Index
                /// </summary>
                public const string NikkeiServicesPurchasingManagersIndex = "JPN/nikkei services purchasing managers index";
                /// <summary>
                /// Overtime Pay
                /// </summary>
                public const string OvertimePay = "JPN/overtime pay";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "JPN/ppi";
                /// <summary>
                /// Respect For The Aged Day
                /// </summary>
                public const string RespectForTheAgedDay = "JPN/respect for the aged day";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "JPN/retail sales";
                /// <summary>
                /// Reuters Tankan Index
                /// </summary>
                public const string ReutersTankanIndex = "JPN/reuters tankan index";
                /// <summary>
                /// Showa Day
                /// </summary>
                public const string ShowaDay = "JPN/showa day";
                /// <summary>
                /// Stock Investment By Foreigners
                /// </summary>
                public const string StockInvestmentByForeigners = "JPN/stock investment by foreigners";
                /// <summary>
                /// Tankan All Large Industry Capital Expenditure
                /// </summary>
                public const string TankanAllLargeIndustryCapitalExpenditure = "JPN/tankan all large industry capital expenditure";
                /// <summary>
                /// Tankan Large Manufacturing Index
                /// </summary>
                public const string TankanLargeManufacturingIndex = "JPN/tankan large manufacturing index";
                /// <summary>
                /// Tankan Large Manufacturing Outlook
                /// </summary>
                public const string TankanLargeManufacturingOutlook = "JPN/tankan large manufacturing outlook";
                /// <summary>
                /// Tankan Large Non Manufacturing Index
                /// </summary>
                public const string TankanLargeNonManufacturingIndex = "JPN/tankan large non manufacturing index";
                /// <summary>
                /// Tankan Non Manufacturing Outlook
                /// </summary>
                public const string TankanNonManufacturingOutlook = "JPN/tankan non manufacturing outlook";
                /// <summary>
                /// Tankan Small Manufacturing Index
                /// </summary>
                public const string TankanSmallManufacturingIndex = "JPN/tankan small manufacturing index";
                /// <summary>
                /// Tertiary Industry Index
                /// </summary>
                public const string TertiaryIndustryIndex = "JPN/tertiary industry index";
                /// <summary>
                /// The Emperors Birthday
                /// </summary>
                public const string TheEmperorsBirthday = "JPN/the emperors birthday";
                /// <summary>
                /// Tokyo Consumer Price Index
                /// </summary>
                public const string TokyoConsumerPriceIndex = "JPN/tokyo consumer price index";
                /// <summary>
                /// Tokyo Consumer Price Index Excluding Food And Energy
                /// </summary>
                public const string TokyoConsumerPriceIndexExcludingFoodAndEnergy = "JPN/tokyo consumer price index excluding food and energy";
                /// <summary>
                /// Tokyo Core Consumer Price Index
                /// </summary>
                public const string TokyoCoreConsumerPriceIndex = "JPN/tokyo core consumer price index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "JPN/unemployment rate";
            }

            /// <summary>
            /// Kazakhstan
            /// </summary>
            public static class Kazakhstan
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "KAZ/inflation rate";
            }

            /// <summary>
            /// Kyrgyzstan
            /// </summary>
            public static class Kyrgyzstan
            {
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "KGZ/ppi";
            }

            /// <summary>
            /// Korea
            /// </summary>
            public static class Korea
            {
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "KOR/business confidence";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "KOR/construction output";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "KOR/consumer confidence";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "KOR/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "KOR/current account";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "KOR/export prices";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "KOR/exports";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "KOR/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "KOR/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq Adv
                /// </summary>
                public const string GdpGrowthRateQoqAdv = "KOR/gdp growth rate qoq adv";
                /// <summary>
                /// Gdp Growth Rate Yoy Adv
                /// </summary>
                public const string GdpGrowthRateYoyAdv = "KOR/gdp growth rate yoy adv";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "KOR/import prices";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "KOR/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "KOR/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "KOR/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "KOR/interest rate decision";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "KOR/manufacturing production";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "KOR/markit manufacturing purchasing managers index";
                /// <summary>
                /// Nikkei Manufacturing Purchasing Managers Index
                /// </summary>
                public const string NikkeiManufacturingPurchasingManagersIndex = "KOR/nikkei manufacturing purchasing managers index";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "KOR/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "KOR/retail sales";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "KOR/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "KOR/unemployment rate";
            }

            /// <summary>
            /// Kuwait
            /// </summary>
            public static class Kuwait
            {
                /// <summary>
                /// Private Bank Lending
                /// </summary>
                public const string PrivateBankLending = "KWT/private bank lending";
            }

            /// <summary>
            /// Lithuania
            /// </summary>
            public static class Lithuania
            {
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "LTU/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "LTU/current account";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "LTU/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LTU/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "LTU/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "LTU/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "LTU/retail sales";
            }

            /// <summary>
            /// Luxembourg
            /// </summary>
            public static class Luxembourg
            {
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "LUX/business confidence";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "LUX/consumer confidence";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "LUX/current account";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "LUX/gdp growth rate";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "LUX/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "LUX/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "LUX/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LUX/unemployment rate";
            }

            /// <summary>
            /// Latvia
            /// </summary>
            public static class Latvia
            {
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "LVA/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "LVA/current account";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "LVA/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LVA/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "LVA/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "LVA/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "LVA/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LVA/unemployment rate";
            }

            /// <summary>
            /// Macao
            /// </summary>
            public static class Macao
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "MAC/inflation rate";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MAC/unemployment rate";
            }

            /// <summary>
            /// Moldova
            /// </summary>
            public static class Moldova
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "MDA/inflation rate";
            }

            /// <summary>
            /// Mexico
            /// </summary>
            public static class Mexico
            {
                /// <summary>
                /// Aggregate Demand
                /// </summary>
                public const string AggregateDemand = "MEX/aggregate demand";
                /// <summary>
                /// Auto Production
                /// </summary>
                public const string AutoProduction = "MEX/auto production";
                /// <summary>
                /// Birth Of Benito Jurez
                /// </summary>
                public const string BirthOfBenitoJurez = "MEX/birth of benito jurez";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MEX/business confidence";
                /// <summary>
                /// Constitution Day
                /// </summary>
                public const string ConstitutionDay = "MEX/constitution day";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "MEX/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "MEX/core inflation rate";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "MEX/current account";
                /// <summary>
                /// Economic Activity
                /// </summary>
                public const string EconomicActivity = "MEX/economic activity";
                /// <summary>
                /// Fiscal Balance
                /// </summary>
                public const string FiscalBalance = "MEX/fiscal balance";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "MEX/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MEX/gdp growth rate";
                /// <summary>
                /// Gross Fixed Investment
                /// </summary>
                public const string GrossFixedInvestment = "MEX/gross fixed investment";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MEX/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "MEX/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "MEX/interest rate decision";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "MEX/markit manufacturing purchasing managers index";
                /// <summary>
                /// Mid Month Core Inflation Rate
                /// </summary>
                public const string MidMonthCoreInflationRate = "MEX/mid month core inflation rate";
                /// <summary>
                /// Mid Month Inflation Rate
                /// </summary>
                public const string MidMonthInflationRate = "MEX/mid month inflation rate";
                /// <summary>
                /// Monetary Policy Meeting Minutes
                /// </summary>
                public const string MonetaryPolicyMeetingMinutes = "MEX/monetary policy meeting minutes";
                /// <summary>
                /// Private Spending
                /// </summary>
                public const string PrivateSpending = "MEX/private spending";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "MEX/retail sales";
                /// <summary>
                /// Revolution Day
                /// </summary>
                public const string RevolutionDay = "MEX/revolution day";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "MEX/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MEX/unemployment rate";
            }

            /// <summary>
            /// Malta
            /// </summary>
            public static class Malta
            {
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "MLT/consumer confidence";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MLT/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MLT/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "MLT/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "MLT/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "MLT/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MLT/unemployment rate";
            }

            /// <summary>
            /// Montenegro
            /// </summary>
            public static class Montenegro
            {
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "MNE/harmonized inflation rate";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "MNE/inflation rate";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "MNE/retail sales";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "MNE/tourist arrivals";
            }

            /// <summary>
            /// Malaysia
            /// </summary>
            public static class Malaysia
            {
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MYS/gdp growth rate";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "MYS/inflation rate";
                /// <summary>
                /// Leading Index
                /// </summary>
                public const string LeadingIndex = "MYS/leading index";
            }

            /// <summary>
            /// Netherlands
            /// </summary>
            public static class Netherlands
            {
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NLD/consumer confidence";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "NLD/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "NLD/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "NLD/easter monday";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NLD/gdp growth rate";
                /// <summary>
                /// Household Consumption
                /// </summary>
                public const string HouseholdConsumption = "NLD/household consumption";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "NLD/inflation rate";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "NLD/manufacturing production";
                /// <summary>
                /// Nevi Manufacturing Purchasing Managers Index
                /// </summary>
                public const string NeviManufacturingPurchasingManagersIndex = "NLD/nevi manufacturing purchasing managers index";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "NLD/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NLD/unemployment rate";
            }

            /// <summary>
            /// Norway
            /// </summary>
            public static class Norway
            {
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NOR/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "NOR/core inflation rate";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "NOR/cpi";
                /// <summary>
                /// Dnb Manufacturing Purchasing Managers Index
                /// </summary>
                public const string DnbManufacturingPurchasingManagersIndex = "NOR/dnb manufacturing purchasing managers index";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "NOR/easter monday";
                /// <summary>
                /// Gdp
                /// </summary>
                public const string Gdp = "NOR/gdp";
                /// <summary>
                /// Gdp Growth Mainland
                /// </summary>
                public const string GdpGrowthMainland = "NOR/gdp growth mainland";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NOR/gdp growth rate";
                /// <summary>
                /// Gdp Mainland
                /// </summary>
                public const string GdpMainland = "NOR/gdp mainland";
                /// <summary>
                /// House Price Index
                /// </summary>
                public const string HousePriceIndex = "NOR/house price index";
                /// <summary>
                /// Household Consumption
                /// </summary>
                public const string HouseholdConsumption = "NOR/household consumption";
                /// <summary>
                /// Industrial Confidence
                /// </summary>
                public const string IndustrialConfidence = "NOR/industrial confidence";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NOR/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "NOR/inflation rate";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "NOR/loan growth";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "NOR/manufacturing production";
                /// <summary>
                /// Nima Manufacturing Purchasing Managers Index
                /// </summary>
                public const string NimaManufacturingPurchasingManagersIndex = "NOR/nima manufacturing purchasing managers index";
                /// <summary>
                /// Norges Bank Interest Rate Decision
                /// </summary>
                public const string NorgesBankInterestRateDecision = "NOR/norges bank interest rate decision";
                /// <summary>
                /// Norges Bank Interest Rate Decison
                /// </summary>
                public const string NorgesBankInterestRateDecison = "NOR/norges bank interest rate decison";
                /// <summary>
                /// Norges Bank Monetary Policy Report
                /// </summary>
                public const string NorgesBankMonetaryPolicyReport = "NOR/norges bank monetary policy report";
                /// <summary>
                /// Norges Bank Press Conference
                /// </summary>
                public const string NorgesBankPressConference = "NOR/norges bank press conference";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "NOR/ppi";
                /// <summary>
                /// Registered Jobless Rate
                /// </summary>
                public const string RegisteredJoblessRate = "NOR/registered jobless rate";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "NOR/retail sales";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "NOR/unemployed persons";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NOR/unemployment rate";
            }

            /// <summary>
            /// Newzealand
            /// </summary>
            public static class NewZealand
            {
                /// <summary>
                /// Anz Business Confidence
                /// </summary>
                public const string AnzBusinessConfidence = "NZL/anz business confidence";
                /// <summary>
                /// Anz Roy Morgan Consumer Confidence
                /// </summary>
                public const string AnzRoyMorganConsumerConfidence = "NZL/anz roy morgan consumer confidence";
                /// <summary>
                /// Anzac Day
                /// </summary>
                public const string AnzacDay = "NZL/anzac day";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "NZL/balance of trade";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "NZL/building permits";
                /// <summary>
                /// Business Inflation Expectations
                /// </summary>
                public const string BusinessInflationExpectations = "NZL/business inflation expectations";
                /// <summary>
                /// Business Nz Purchasing Managers Index
                /// </summary>
                public const string BusinessNzPurchasingManagersIndex = "NZL/business nz purchasing managers index";
                /// <summary>
                /// Composite Nz Pci
                /// </summary>
                public const string CompositeNzPci = "NZL/composite nz pci";
                /// <summary>
                /// Credit Card Spending
                /// </summary>
                public const string CreditCardSpending = "NZL/credit card spending";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "NZL/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "NZL/easter monday";
                /// <summary>
                /// Electronic Retail Card Spending
                /// </summary>
                public const string ElectronicRetailCardSpending = "NZL/electronic retail card spending";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "NZL/employment change";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "NZL/export prices";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "NZL/exports";
                /// <summary>
                /// Food Inflation
                /// </summary>
                public const string FoodInflation = "NZL/food inflation";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NZL/gdp growth rate";
                /// <summary>
                /// Global Dairy Trade Price Index
                /// </summary>
                public const string GlobalDairyTradePriceIndex = "NZL/global dairy trade price index";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "NZL/import prices";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "NZL/imports";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "NZL/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "NZL/interest rate decision";
                /// <summary>
                /// Labor Costs Index
                /// </summary>
                public const string LaborCostsIndex = "NZL/labor costs index";
                /// <summary>
                /// Labor Day
                /// </summary>
                public const string LaborDay = "NZL/labor day";
                /// <summary>
                /// Manufacturing Sales
                /// </summary>
                public const string ManufacturingSales = "NZL/manufacturing sales";
                /// <summary>
                /// New Years Day
                /// </summary>
                public const string NewYearsDay = "NZL/new years day";
                /// <summary>
                /// Nzier Business Confidence
                /// </summary>
                public const string NzierBusinessConfidence = "NZL/nzier business confidence";
                /// <summary>
                /// Nzier Capacity Utilization
                /// </summary>
                public const string NzierCapacityUtilization = "NZL/nzier capacity utilization";
                /// <summary>
                /// Participation Rate
                /// </summary>
                public const string ParticipationRate = "NZL/participation rate";
                /// <summary>
                /// Producer Price Index Input
                /// </summary>
                public const string ProducerPriceIndexInput = "NZL/producer price index input";
                /// <summary>
                /// Producer Price Index Output
                /// </summary>
                public const string ProducerPriceIndexOutput = "NZL/producer price index output";
                /// <summary>
                /// Rbnz Financial Stability Report
                /// </summary>
                public const string RbnzFinancialStabilityReport = "NZL/rbnz financial stability report";
                /// <summary>
                /// Rbnz Interest Rate Decision
                /// </summary>
                public const string RbnzInterestRateDecision = "NZL/rbnz interest rate decision";
                /// <summary>
                /// Rbnz Press Conference
                /// </summary>
                public const string RbnzPressConference = "NZL/rbnz press conference";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "NZL/retail sales";
                /// <summary>
                /// Services Nz Performance Of Services Index
                /// </summary>
                public const string ServicesNzPerformanceOfServicesIndex = "NZL/services nz performance of services index";
                /// <summary>
                /// Terms Of Trade
                /// </summary>
                public const string TermsOfTrade = "NZL/terms of trade";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NZL/unemployment rate";
                /// <summary>
                /// Visitor Arrivals
                /// </summary>
                public const string VisitorArrivals = "NZL/visitor arrivals";
                /// <summary>
                /// Waitangi Day
                /// </summary>
                public const string WaitangiDay = "NZL/waitangi day";
                /// <summary>
                /// Westpac Consumer Confidence
                /// </summary>
                public const string WestpacConsumerConfidence = "NZL/westpac consumer confidence";
            }

            /// <summary>
            /// Oman
            /// </summary>
            public static class Oman
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "OMN/inflation rate";
                /// <summary>
                /// Total Credit
                /// </summary>
                public const string TotalCredit = "OMN/total credit";
            }

            /// <summary>
            /// Pakistan
            /// </summary>
            public static class Pakistan
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "PAK/inflation rate";
            }

            /// <summary>
            /// Philippines
            /// </summary>
            public static class Philippines
            {
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "PHL/budget balance";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "PHL/business confidence";
                /// <summary>
                /// Cash Remittances
                /// </summary>
                public const string CashRemittances = "PHL/cash remittances";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PHL/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "PHL/core inflation rate";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "PHL/exports";
                /// <summary>
                /// Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "PHL/foreign direct investment";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "PHL/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PHL/gdp growth rate";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "PHL/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PHL/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "PHL/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "PHL/interest rate decision";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "PHL/markit manufacturing purchasing managers index";
                /// <summary>
                /// National Heroes Day
                /// </summary>
                public const string NationalHeroesDay = "PHL/national heroes day";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "PHL/ppi";
                /// <summary>
                /// Retail Price Index
                /// </summary>
                public const string RetailPriceIndex = "PHL/retail price index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "PHL/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PHL/unemployment rate";
            }

            /// <summary>
            /// Poland
            /// </summary>
            public static class Poland
            {
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "POL/core inflation rate";
                /// <summary>
                /// Corporate Sector Wages
                /// </summary>
                public const string CorporateSectorWages = "POL/corporate sector wages";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "POL/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "POL/easter monday";
                /// <summary>
                /// Employment Growth
                /// </summary>
                public const string EmploymentGrowth = "POL/employment growth";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "POL/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "POL/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "POL/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "POL/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "POL/interest rate decision";
                /// <summary>
                /// M3 Money Supply
                /// </summary>
                public const string M3MoneySupply = "POL/m3 money supply";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "POL/markit manufacturing purchasing managers index";
                /// <summary>
                /// Monetary Policy Meeting Minutes
                /// </summary>
                public const string MonetaryPolicyMeetingMinutes = "POL/monetary policy meeting minutes";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "POL/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "POL/retail sales";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "POL/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "POL/unemployment rate";
            }

            /// <summary>
            /// Portugal
            /// </summary>
            public static class Portugal
            {
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "PRT/budget balance";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "PRT/business confidence";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PRT/consumer confidence";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "PRT/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "PRT/current account";
                /// <summary>
                /// Economic Activity
                /// </summary>
                public const string EconomicActivity = "PRT/economic activity";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PRT/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PRT/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "PRT/inflation rate";
                /// <summary>
                /// Liberty Day
                /// </summary>
                public const string LibertyDay = "PRT/liberty day";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "PRT/ppi";
                /// <summary>
                /// Private Consumption
                /// </summary>
                public const string PrivateConsumption = "PRT/private consumption";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "PRT/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PRT/unemployment rate";
            }

            /// <summary>
            /// Qatar
            /// </summary>
            public static class Qatar
            {
                /// <summary>
                /// Total Credit Growth
                /// </summary>
                public const string TotalCreditGrowth = "QAT/total credit growth";
            }

            /// <summary>
            /// Romania
            /// </summary>
            public static class Romania
            {
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ROU/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ROU/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ROU/inflation rate";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "ROU/retail sales";
            }

            /// <summary>
            /// Russia
            /// </summary>
            public static class Russia
            {
                /// <summary>
                /// Cbr Press Conference
                /// </summary>
                public const string CbrPressConference = "RUS/cbr press conference";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "RUS/consumer confidence";
                /// <summary>
                /// Corporate Profits
                /// </summary>
                public const string CorporateProfits = "RUS/corporate profits";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "RUS/current account";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "RUS/foreign exchange reserves";
                /// <summary>
                /// Gdp
                /// </summary>
                public const string Gdp = "RUS/gdp";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "RUS/gdp growth rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "RUS/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "RUS/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "RUS/interest rate decision";
                /// <summary>
                /// International Womens Day
                /// </summary>
                public const string InternationalWomensDay = "RUS/international womens day";
                /// <summary>
                /// M2 Money Supply
                /// </summary>
                public const string M2MoneySupply = "RUS/m2 money supply";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "RUS/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "RUS/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "RUS/markit services purchasing managers index";
                /// <summary>
                /// Monetary Policy Report
                /// </summary>
                public const string MonetaryPolicyReport = "RUS/monetary policy report";
                /// <summary>
                /// Orthodox Christmas Day
                /// </summary>
                public const string OrthodoxChristmasDay = "RUS/orthodox christmas day";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "RUS/ppi";
                /// <summary>
                /// Real Wage Growth
                /// </summary>
                public const string RealWageGrowth = "RUS/real wage growth";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "RUS/retail sales";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "RUS/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "RUS/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "RUS/sandp global services purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "RUS/unemployment rate";
                /// <summary>
                /// Vehicle Sales
                /// </summary>
                public const string VehicleSales = "RUS/vehicle sales";
                /// <summary>
                /// Victory Day
                /// </summary>
                public const string VictoryDay = "RUS/victory day";
            }

            /// <summary>
            /// Saudiarabia
            /// </summary>
            public static class SaudiArabia
            {
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "SAU/exports";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "SAU/imports";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SAU/inflation rate";
            }

            /// <summary>
            /// Singapore
            /// </summary>
            public static class Singapore
            {
                /// <summary>
                /// 6 Month Bill Auction
                /// </summary>
                public const string SixMonthBillAuction = "SGP/6 month bill auction";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "SGP/balance of trade";
                /// <summary>
                /// Bank Lending
                /// </summary>
                public const string BankLending = "SGP/bank lending";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SGP/business confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SGP/core inflation rate";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "SGP/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "SGP/current account";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "SGP/export prices";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SGP/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SGP/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq Adv
                /// </summary>
                public const string GdpGrowthRateQoqAdv = "SGP/gdp growth rate qoq adv";
                /// <summary>
                /// Gdp Growth Rate Yoy Adv
                /// </summary>
                public const string GdpGrowthRateYoyAdv = "SGP/gdp growth rate yoy adv";
                /// <summary>
                /// Hari Raya Puasa
                /// </summary>
                public const string HariRayaPuasa = "SGP/hari raya puasa";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "SGP/import prices";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SGP/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SGP/inflation rate";
                /// <summary>
                /// Markit Purchasing Managers Index
                /// </summary>
                public const string MarkitPurchasingManagersIndex = "SGP/markit purchasing managers index";
                /// <summary>
                /// Mas 12 Week Bill Auction
                /// </summary>
                public const string MasTwelveWeekBillAuction = "SGP/mas 12 week bill auction";
                /// <summary>
                /// Mas 4 Week Bill Auction
                /// </summary>
                public const string MasFourWeekBillAuction = "SGP/mas 4 week bill auction";
                /// <summary>
                /// Mas12
                /// </summary>
                public const string Mas12 = "SGP/mas12";
                /// <summary>
                /// Mas4
                /// </summary>
                public const string Mas4 = "SGP/mas4";
                /// <summary>
                /// Monetary Policy Statement
                /// </summary>
                public const string MonetaryPolicyStatement = "SGP/monetary policy statement";
                /// <summary>
                /// National Day
                /// </summary>
                public const string NationalDay = "SGP/national day";
                /// <summary>
                /// Non Oil Exports
                /// </summary>
                public const string NonOilExports = "SGP/non oil exports";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "SGP/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "SGP/retail sales";
                /// <summary>
                /// Sandp Global Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalPurchasingManagersIndex = "SGP/sandp global purchasing managers index";
                /// <summary>
                /// Sipmm Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SipmmManufacturingPurchasingManagersIndex = "SGP/sipmm manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SGP/unemployment rate";
                /// <summary>
                /// Ura Property Index
                /// </summary>
                public const string UraPropertyIndex = "SGP/ura property index";
            }

            /// <summary>
            /// Elsalvador
            /// </summary>
            public static class ElSalvador
            {
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SLV/gdp growth rate";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SLV/inflation rate";
            }

            /// <summary>
            /// Somalia
            /// </summary>
            public static class Somalia
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SOM/inflation rate";
            }

            /// <summary>
            /// Serbia
            /// </summary>
            public static class Serbia
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SRB/inflation rate";
            }

            /// <summary>
            /// Slovakia
            /// </summary>
            public static class Slovakia
            {
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "SVK/construction output";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SVK/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SVK/core inflation rate";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "SVK/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "SVK/current account";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SVK/gdp growth rate";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "SVK/harmonized inflation rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SVK/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SVK/inflation rate";
                /// <summary>
                /// Real Wages
                /// </summary>
                public const string RealWages = "SVK/real wages";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "SVK/retail sales";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SVK/unemployment rate";
            }

            /// <summary>
            /// Slovenia
            /// </summary>
            public static class Slovenia
            {
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SVN/consumer confidence";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "SVN/cpi";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SVN/gdp growth rate";
                /// <summary>
                /// Harmonized Inflation Rate
                /// </summary>
                public const string HarmonizedInflationRate = "SVN/harmonized inflation rate";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SVN/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SVN/inflation rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "SVN/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "SVN/retail sales";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SVN/tourist arrivals";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SVN/unemployment rate";
            }

            /// <summary>
            /// Sweden
            /// </summary>
            public static class Sweden
            {
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SWE/business confidence";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SWE/consumer confidence";
                /// <summary>
                /// Consumer Inflation Expectations
                /// </summary>
                public const string ConsumerInflationExpectations = "SWE/consumer inflation expectations";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "SWE/cpi";
                /// <summary>
                /// Cpif
                /// </summary>
                public const string Cpif = "SWE/cpif";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "SWE/current account";
                /// <summary>
                /// Easter Monday
                /// </summary>
                public const string EasterMonday = "SWE/easter monday";
                /// <summary>
                /// Employed Persons
                /// </summary>
                public const string EmployedPersons = "SWE/employed persons";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "SWE/exports";
                /// <summary>
                /// Gdp
                /// </summary>
                public const string Gdp = "SWE/gdp";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SWE/gdp growth rate";
                /// <summary>
                /// House Price Index
                /// </summary>
                public const string HousePriceIndex = "SWE/house price index";
                /// <summary>
                /// Household Consumption
                /// </summary>
                public const string HouseholdConsumption = "SWE/household consumption";
                /// <summary>
                /// Household Lending Growth
                /// </summary>
                public const string HouseholdLendingGrowth = "SWE/household lending growth";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "SWE/imports";
                /// <summary>
                /// Industrial Inventories
                /// </summary>
                public const string IndustrialInventories = "SWE/industrial inventories";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SWE/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SWE/inflation rate";
                /// <summary>
                /// Monetary Policy Meeting Minutes
                /// </summary>
                public const string MonetaryPolicyMeetingMinutes = "SWE/monetary policy meeting minutes";
                /// <summary>
                /// Monetary Policy Report
                /// </summary>
                public const string MonetaryPolicyReport = "SWE/monetary policy report";
                /// <summary>
                /// New Orders
                /// </summary>
                public const string NewOrders = "SWE/new orders";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "SWE/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "SWE/retail sales";
                /// <summary>
                /// Riksbank Rate Decision
                /// </summary>
                public const string RiksbankRateDecision = "SWE/riksbank rate decision";
                /// <summary>
                /// Services Purchasing Managers Index
                /// </summary>
                public const string ServicesPurchasingManagersIndex = "SWE/services purchasing managers index";
                /// <summary>
                /// Swedbank Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SwedbankManufacturingPurchasingManagersIndex = "SWE/swedbank manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SWE/unemployment rate";
            }

            /// <summary>
            /// Thailand
            /// </summary>
            public static class Thailand
            {
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "THA/balance of trade";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "THA/business confidence";
                /// <summary>
                /// Coincident Index
                /// </summary>
                public const string CoincidentIndex = "THA/coincident index";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "THA/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "THA/core inflation rate";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "THA/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "THA/current account";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "THA/exports";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "THA/foreign exchange reserves";
                /// <summary>
                /// Full Year Gdp Growth
                /// </summary>
                public const string FullYearGdpGrowth = "THA/full year gdp growth";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "THA/gdp growth rate";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "THA/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "THA/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "THA/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "THA/interest rate decision";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "THA/markit manufacturing purchasing managers index";
                /// <summary>
                /// New Car Sales
                /// </summary>
                public const string NewCarSales = "THA/new car sales";
                /// <summary>
                /// Private Consumption
                /// </summary>
                public const string PrivateConsumption = "THA/private consumption";
                /// <summary>
                /// Private Investment
                /// </summary>
                public const string PrivateInvestment = "THA/private investment";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "THA/retail sales";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "THA/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "THA/unemployment rate";
            }

            /// <summary>
            /// Timorleste
            /// </summary>
            public static class TimorLeste
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "TLS/inflation rate";
            }

            /// <summary>
            /// Turkey
            /// </summary>
            public static class Turkey
            {
                /// <summary>
                /// Auto Sales
                /// </summary>
                public const string AutoSales = "TUR/auto sales";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "TUR/capacity utilization";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "TUR/exports";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "TUR/foreign exchange reserves";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "TUR/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "TUR/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "TUR/inflation rate";
                /// <summary>
                /// Mpc Meeting Summary
                /// </summary>
                public const string MpcMeetingSummary = "TUR/mpc meeting summary";
                /// <summary>
                /// Overnight Borrowing Rate
                /// </summary>
                public const string OvernightBorrowingRate = "TUR/overnight borrowing rate";
                /// <summary>
                /// Overnight Lending Rate
                /// </summary>
                public const string OvernightLendingRate = "TUR/overnight lending rate";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "TUR/ppi";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "TUR/retail sales";
                /// <summary>
                /// Tcmb Interest Rate Decision
                /// </summary>
                public const string TcmbInterestRateDecision = "TUR/tcmb interest rate decision";
            }

            /// <summary>
            /// Taiwan
            /// </summary>
            public static class Taiwan
            {
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "TWN/consumer confidence";
                /// <summary>
                /// Export Orders
                /// </summary>
                public const string ExportOrders = "TWN/export orders";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "TWN/exports";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "TWN/foreign exchange reserves";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "TWN/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Yoy Adv
                /// </summary>
                public const string GdpGrowthRateYoyAdv = "TWN/gdp growth rate yoy adv";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "TWN/imports";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "TWN/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "TWN/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "TWN/interest rate decision";
                /// <summary>
                /// M2 Money Supply
                /// </summary>
                public const string M2MoneySupply = "TWN/m2 money supply";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "TWN/markit manufacturing purchasing managers index";
                /// <summary>
                /// New Years Day
                /// </summary>
                public const string NewYearsDay = "TWN/new years day";
                /// <summary>
                /// Nikkei Manufacturing Purchasing Managers Index
                /// </summary>
                public const string NikkeiManufacturingPurchasingManagersIndex = "TWN/nikkei manufacturing purchasing managers index";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "TWN/retail sales";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "TWN/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "TWN/unemployment rate";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "TWN/wholesale prices";
            }

            /// <summary>
            /// Ukraine
            /// </summary>
            public static class Ukraine
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "UKR/inflation rate";
            }

            /// <summary>
            /// Unitedstates
            /// </summary>
            public static class UnitedStates
            {
                /// <summary>
                /// 105 Day Bill Auction
                /// </summary>
                public const string OneHundredAndFiveDayBillAuction = "USA/105 day bill auction";
                /// <summary>
                /// 119 Day Bill Auction
                /// </summary>
                public const string OneHundredAndNineteenDayBillAuction = "USA/119 day bill auction";
                /// <summary>
                /// 15 Year Mortgage Rate
                /// </summary>
                public const string FifteenYearMortgageRate = "USA/15 year mortgage rate";
                /// <summary>
                /// 154 Day Bill Auction
                /// </summary>
                public const string OneHundredAndFiftyFourDayBillAuction = "USA/154 day bill auction";
                /// <summary>
                /// 17 Week Bill Auction
                /// </summary>
                public const string SeventeenWeekBillAuction = "USA/17 week bill auction";
                /// <summary>
                /// 2 Year Frn Auction
                /// </summary>
                public const string TwoYearFrnAuction = "USA/2 year frn auction";
                /// <summary>
                /// 2 Year Note Auction
                /// </summary>
                public const string TwoYearNoteAuction = "USA/2 year note auction";
                /// <summary>
                /// 20 Year Bond Auction
                /// </summary>
                public const string TwentyYearBondAuction = "USA/20 year bond auction";
                /// <summary>
                /// 3 Month Bill Auction
                /// </summary>
                public const string ThreeMonthBillAuction = "USA/3 month bill auction";
                /// <summary>
                /// 3 Year Note Auction
                /// </summary>
                public const string ThreeYearNoteAuction = "USA/3 year note auction";
                /// <summary>
                /// 30 Year Bond Auction
                /// </summary>
                public const string ThirtyYearBondAuction = "USA/30 year bond auction";
                /// <summary>
                /// 30 Year Mortgage Rate
                /// </summary>
                public const string ThirtyYearMortgageRate = "USA/30 year mortgage rate";
                /// <summary>
                /// 30 Year Tips Auction
                /// </summary>
                public const string ThirtyYearTipsAuction = "USA/30 year tips auction";
                /// <summary>
                /// 4 Week Bill Auction
                /// </summary>
                public const string FourWeekBillAuction = "USA/4 week bill auction";
                /// <summary>
                /// 42 Day Bill Auction
                /// </summary>
                public const string FortyTwoDayBillAuction = "USA/42 day bill auction";
                /// <summary>
                /// 5 Year Note Auction
                /// </summary>
                public const string FiveYearNoteAuction = "USA/5 year note auction";
                /// <summary>
                /// 5 Year Tips Auction
                /// </summary>
                public const string FiveYearTipsAuction = "USA/5 year tips auction";
                /// <summary>
                /// 52 Week Bill Auction
                /// </summary>
                public const string FiftyTwoWeekBillAuction = "USA/52 week bill auction";
                /// <summary>
                /// 6 Month Bill Auction
                /// </summary>
                public const string SixMonthBillAuction = "USA/6 month bill auction";
                /// <summary>
                /// 7 Year Note Auction
                /// </summary>
                public const string SevenYearNoteAuction = "USA/7 year note auction";
                /// <summary>
                /// 8 Week Bill Auction
                /// </summary>
                public const string EightWeekBillAuction = "USA/8 week bill auction";
                /// <summary>
                /// Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "USA/adp employment change";
                /// <summary>
                /// Api Weekly Crude Stock Change
                /// </summary>
                public const string ApiWeeklyCrudeStockChange = "USA/api weekly crude stock change";
                /// <summary>
                /// Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "USA/average hourly earnings";
                /// <summary>
                /// Average Weekly Hours
                /// </summary>
                public const string AverageWeeklyHours = "USA/average weekly hours";
                /// <summary>
                /// Baker Hughes Oil Rig Count
                /// </summary>
                public const string BakerHughesOilRigCount = "USA/baker hughes oil rig count";
                /// <summary>
                /// Baker Hughes Total Rigs Count
                /// </summary>
                public const string BakerHughesTotalRigsCount = "USA/baker hughes total rigs count";
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "USA/balance of trade";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "USA/building permits";
                /// <summary>
                /// Business Inventories
                /// </summary>
                public const string BusinessInventories = "USA/business inventories";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "USA/capacity utilization";
                /// <summary>
                /// Cb Consumer Confidence
                /// </summary>
                public const string CbConsumerConfidence = "USA/cb consumer confidence";
                /// <summary>
                /// Cb Leading Economic Index
                /// </summary>
                public const string CbLeadingEconomicIndex = "USA/cb leading economic index";
                /// <summary>
                /// Central Bank Balance Sheet
                /// </summary>
                public const string CentralBankBalanceSheet = "USA/central bank balance sheet";
                /// <summary>
                /// Challenger Job Cuts
                /// </summary>
                public const string ChallengerJobCuts = "USA/challenger job cuts";
                /// <summary>
                /// Chicago Fed National Activity Index
                /// </summary>
                public const string ChicagoFedNationalActivityIndex = "USA/chicago fed national activity index";
                /// <summary>
                /// Chicago Purchasing Managers Index
                /// </summary>
                public const string ChicagoPurchasingManagersIndex = "USA/chicago purchasing managers index";
                /// <summary>
                /// Columbus Day
                /// </summary>
                public const string ColumbusDay = "USA/columbus day";
                /// <summary>
                /// Construction Spending
                /// </summary>
                public const string ConstructionSpending = "USA/construction spending";
                /// <summary>
                /// Consumer Credit Change
                /// </summary>
                public const string ConsumerCreditChange = "USA/consumer credit change";
                /// <summary>
                /// Consumer Inflation Expectations
                /// </summary>
                public const string ConsumerInflationExpectations = "USA/consumer inflation expectations";
                /// <summary>
                /// Consumer Price Index Seasonally Adjusted
                /// </summary>
                public const string ConsumerPriceIndexSeasonallyAdjusted = "USA/consumer price index seasonally adjusted";
                /// <summary>
                /// Continuing Jobless Claims
                /// </summary>
                public const string ContinuingJoblessClaims = "USA/continuing jobless claims";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "USA/core inflation rate";
                /// <summary>
                /// Core Personal Consumption Expenditure Price Index
                /// </summary>
                public const string CorePersonalConsumptionExpenditurePriceIndex = "USA/core personal consumption expenditure price index";
                /// <summary>
                /// Core Personal Consumption Expenditure Price Index Qoq Adv
                /// </summary>
                public const string CorePersonalConsumptionExpenditurePriceIndexQoqAdv = "USA/core personal consumption expenditure price index qoq adv";
                /// <summary>
                /// Core Producer Price Index
                /// </summary>
                public const string CoreProducerPriceIndex = "USA/core producer price index";
                /// <summary>
                /// Corporate Profits
                /// </summary>
                public const string CorporateProfits = "USA/corporate profits";
                /// <summary>
                /// Cpi
                /// </summary>
                public const string Cpi = "USA/cpi";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "USA/current account";
                /// <summary>
                /// Dallas Fed Manufacturing Index
                /// </summary>
                public const string DallasFedManufacturingIndex = "USA/dallas fed manufacturing index";
                /// <summary>
                /// Dallas Fed Services Index
                /// </summary>
                public const string DallasFedServicesIndex = "USA/dallas fed services index";
                /// <summary>
                /// Dallas Fed Services Revenues Index
                /// </summary>
                public const string DallasFedServicesRevenuesIndex = "USA/dallas fed services revenues index";
                /// <summary>
                /// Durable Goods Orders
                /// </summary>
                public const string DurableGoodsOrders = "USA/durable goods orders";
                /// <summary>
                /// Durable Goods Orders Excluding Defense
                /// </summary>
                public const string DurableGoodsOrdersExcludingDefense = "USA/durable goods orders excluding defense";
                /// <summary>
                /// Durable Goods Orders Excluding Transp
                /// </summary>
                public const string DurableGoodsOrdersExcludingTransp = "USA/durable goods orders excluding transp";
                /// <summary>
                /// Eia Crude Oil Imports Change
                /// </summary>
                public const string EiaCrudeOilImportsChange = "USA/eia crude oil imports change";
                /// <summary>
                /// Eia Crude Oil Stocks Change
                /// </summary>
                public const string EiaCrudeOilStocksChange = "USA/eia crude oil stocks change";
                /// <summary>
                /// Eia Cushing Crude Oil Stocks Change
                /// </summary>
                public const string EiaCushingCrudeOilStocksChange = "USA/eia cushing crude oil stocks change";
                /// <summary>
                /// Eia Distillate Fuel Production Change
                /// </summary>
                public const string EiaDistillateFuelProductionChange = "USA/eia distillate fuel production change";
                /// <summary>
                /// Eia Distillate Stocks Change
                /// </summary>
                public const string EiaDistillateStocksChange = "USA/eia distillate stocks change";
                /// <summary>
                /// Eia Gasoline Production Change
                /// </summary>
                public const string EiaGasolineProductionChange = "USA/eia gasoline production change";
                /// <summary>
                /// Eia Gasoline Stocks Change
                /// </summary>
                public const string EiaGasolineStocksChange = "USA/eia gasoline stocks change";
                /// <summary>
                /// Eia Heating Oil Stocks Change
                /// </summary>
                public const string EiaHeatingOilStocksChange = "USA/eia heating oil stocks change";
                /// <summary>
                /// Eia Natural Gas Stocks Change
                /// </summary>
                public const string EiaNaturalGasStocksChange = "USA/eia natural gas stocks change";
                /// <summary>
                /// Eia Refinery Crude Runs Change
                /// </summary>
                public const string EiaRefineryCrudeRunsChange = "USA/eia refinery crude runs change";
                /// <summary>
                /// Employment Costs Benefits
                /// </summary>
                public const string EmploymentCostsBenefits = "USA/employment costs benefits";
                /// <summary>
                /// Employment Costs Index
                /// </summary>
                public const string EmploymentCostsIndex = "USA/employment costs index";
                /// <summary>
                /// Employment Costs Wages
                /// </summary>
                public const string EmploymentCostsWages = "USA/employment costs wages";
                /// <summary>
                /// Existing Home Sales
                /// </summary>
                public const string ExistingHomeSales = "USA/existing home sales";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "USA/export prices";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "USA/exports";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "USA/factory orders";
                /// <summary>
                /// Factory Orders Excluding Transportation
                /// </summary>
                public const string FactoryOrdersExcludingTransportation = "USA/factory orders excluding transportation";
                /// <summary>
                /// Fed Balance Sheet
                /// </summary>
                public const string FedBalanceSheet = "USA/fed balance sheet";
                /// <summary>
                /// Fed Barkin Speech
                /// </summary>
                public const string FedBarkinSpeech = "USA/fed barkin speech";
                /// <summary>
                /// Fed Barkins Speech
                /// </summary>
                public const string FedBarkinsSpeech = "USA/fed barkins speech";
                /// <summary>
                /// Fed Barr Speech
                /// </summary>
                public const string FedBarrSpeech = "USA/fed barr speech";
                /// <summary>
                /// Fed Beige Book
                /// </summary>
                public const string FedBeigeBook = "USA/fed beige book";
                /// <summary>
                /// Fed Bostic Speech
                /// </summary>
                public const string FedBosticSpeech = "USA/fed bostic speech";
                /// <summary>
                /// Fed Bowman Speech
                /// </summary>
                public const string FedBowmanSpeech = "USA/fed bowman speech";
                /// <summary>
                /// Fed Brainard Speech
                /// </summary>
                public const string FedBrainardSpeech = "USA/fed brainard speech";
                /// <summary>
                /// Fed Bullard Speech
                /// </summary>
                public const string FedBullardSpeech = "USA/fed bullard speech";
                /// <summary>
                /// Fed Chair Powell Speech
                /// </summary>
                public const string FedChairPowellSpeech = "USA/fed chair powell speech";
                /// <summary>
                /// Fed Chair Powell Testimony
                /// </summary>
                public const string FedChairPowellTestimony = "USA/fed chair powell testimony";
                /// <summary>
                /// Fed Clarida Speech
                /// </summary>
                public const string FedClaridaSpeech = "USA/fed clarida speech";
                /// <summary>
                /// Fed Collins Speech
                /// </summary>
                public const string FedCollinsSpeech = "USA/fed collins speech";
                /// <summary>
                /// Fed Cook Speech
                /// </summary>
                public const string FedCookSpeech = "USA/fed cook speech";
                /// <summary>
                /// Fed Daly Speech
                /// </summary>
                public const string FedDalySpeech = "USA/fed daly speech";
                /// <summary>
                /// Fed Evans Speech
                /// </summary>
                public const string FedEvansSpeech = "USA/fed evans speech";
                /// <summary>
                /// Fed George Speech
                /// </summary>
                public const string FedGeorgeSpeech = "USA/fed george speech";
                /// <summary>
                /// Fed Goolsbee Speech
                /// </summary>
                public const string FedGoolsbeeSpeech = "USA/fed goolsbee speech";
                /// <summary>
                /// Fed Harker Speech
                /// </summary>
                public const string FedHarkerSpeech = "USA/fed harker speech";
                /// <summary>
                /// Fed Interest Rate Decision
                /// </summary>
                public const string FedInterestRateDecision = "USA/fed interest rate decision";
                /// <summary>
                /// Fed Jefferson Speech
                /// </summary>
                public const string FedJeffersonSpeech = "USA/fed jefferson speech";
                /// <summary>
                /// Fed Kaplan Speech
                /// </summary>
                public const string FedKaplanSpeech = "USA/fed kaplan speech";
                /// <summary>
                /// Fed Kashkari Speech
                /// </summary>
                public const string FedKashkariSpeech = "USA/fed kashkari speech";
                /// <summary>
                /// Fed Kugler Speech
                /// </summary>
                public const string FedKuglerSpeech = "USA/fed kugler speech";
                /// <summary>
                /// Fed Logan Speech
                /// </summary>
                public const string FedLoganSpeech = "USA/fed logan speech";
                /// <summary>
                /// Fed Mester Speech
                /// </summary>
                public const string FedMesterSpeech = "USA/fed mester speech";
                /// <summary>
                /// Fed Press Conference
                /// </summary>
                public const string FedPressConference = "USA/fed press conference";
                /// <summary>
                /// Fed Quarles Speech
                /// </summary>
                public const string FedQuarlesSpeech = "USA/fed quarles speech";
                /// <summary>
                /// Fed Rosengren Speech
                /// </summary>
                public const string FedRosengrenSpeech = "USA/fed rosengren speech";
                /// <summary>
                /// Fed Waller Speech
                /// </summary>
                public const string FedWallerSpeech = "USA/fed waller speech";
                /// <summary>
                /// Fed Williams Speech
                /// </summary>
                public const string FedWilliamsSpeech = "USA/fed williams speech";
                /// <summary>
                /// Fomc Economic Projections
                /// </summary>
                public const string FomcEconomicProjections = "USA/fomc economic projections";
                /// <summary>
                /// Fomc Minutes
                /// </summary>
                public const string FomcMinutes = "USA/fomc minutes";
                /// <summary>
                /// Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "USA/foreign bond investment";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "USA/gdp growth rate";
                /// <summary>
                /// Gdp Growth Rate Qoq Adv
                /// </summary>
                public const string GdpGrowthRateQoqAdv = "USA/gdp growth rate qoq adv";
                /// <summary>
                /// Gdp Price Index
                /// </summary>
                public const string GdpPriceIndex = "USA/gdp price index";
                /// <summary>
                /// Gdp Price Index Qoq Adv
                /// </summary>
                public const string GdpPriceIndexQoqAdv = "USA/gdp price index qoq adv";
                /// <summary>
                /// Gdp Sales
                /// </summary>
                public const string GdpSales = "USA/gdp sales";
                /// <summary>
                /// Gdp Sales Qoq Adv
                /// </summary>
                public const string GdpSalesQoqAdv = "USA/gdp sales qoq adv";
                /// <summary>
                /// Goods Trade Balance
                /// </summary>
                public const string GoodsTradeBalance = "USA/goods trade balance";
                /// <summary>
                /// Goods Trade Balance Adv
                /// </summary>
                public const string GoodsTradeBalanceAdv = "USA/goods trade balance adv";
                /// <summary>
                /// Government Payrolls
                /// </summary>
                public const string GovernmentPayrolls = "USA/government payrolls";
                /// <summary>
                /// House Price Index
                /// </summary>
                public const string HousePriceIndex = "USA/house price index";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "USA/housing starts";
                /// <summary>
                /// Ibd Tipp Economic Optimism
                /// </summary>
                public const string IbdTippEconomicOptimism = "USA/ibd tipp economic optimism";
                /// <summary>
                /// Imf World Bank Spring Meetings
                /// </summary>
                public const string ImfWorldBankSpringMeetings = "USA/imf world bank spring meetings";
                /// <summary>
                /// Imf World Bank Virtual Annual Meeting
                /// </summary>
                public const string ImfWorldBankVirtualAnnualMeeting = "USA/imf world bank virtual annual meeting";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "USA/import prices";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "USA/imports";
                /// <summary>
                /// Independence Day
                /// </summary>
                public const string IndependenceDay = "USA/independence day";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "USA/industrial production";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "USA/inflation rate";
                /// <summary>
                /// Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "USA/initial jobless claims";
                /// <summary>
                /// Interest Rate Projection 3Rd Yr
                /// </summary>
                public const string InterestRateProjection3RdYr = "USA/interest rate projection 3rd yr";
                /// <summary>
                /// Interest Rate Projection Current
                /// </summary>
                public const string InterestRateProjectionCurrent = "USA/interest rate projection current";
                /// <summary>
                /// Interest Rate Projection First Yr
                /// </summary>
                public const string InterestRateProjectionFirstYr = "USA/interest rate projection first yr";
                /// <summary>
                /// Interest Rate Projection Longer
                /// </summary>
                public const string InterestRateProjectionLonger = "USA/interest rate projection longer";
                /// <summary>
                /// Interest Rate Projection Second Yr
                /// </summary>
                public const string InterestRateProjectionSecondYr = "USA/interest rate projection second yr";
                /// <summary>
                /// International Monetary Market Date
                /// </summary>
                public const string InternationalMonetaryMarketDate = "USA/international monetary market date";
                /// <summary>
                /// Ism Manufacturing Employment
                /// </summary>
                public const string IsmManufacturingEmployment = "USA/ism manufacturing employment";
                /// <summary>
                /// Ism Manufacturing New Orders
                /// </summary>
                public const string IsmManufacturingNewOrders = "USA/ism manufacturing new orders";
                /// <summary>
                /// Ism Manufacturing Prices
                /// </summary>
                public const string IsmManufacturingPrices = "USA/ism manufacturing prices";
                /// <summary>
                /// Ism Manufacturing Purchasing Managers Index
                /// </summary>
                public const string IsmManufacturingPurchasingManagersIndex = "USA/ism manufacturing purchasing managers index";
                /// <summary>
                /// Ism New York Index
                /// </summary>
                public const string IsmNewYorkIndex = "USA/ism new york index";
                /// <summary>
                /// Ism Non Manufacturing Business Activity
                /// </summary>
                public const string IsmNonManufacturingBusinessActivity = "USA/ism non manufacturing business activity";
                /// <summary>
                /// Ism Non Manufacturing Employment
                /// </summary>
                public const string IsmNonManufacturingEmployment = "USA/ism non manufacturing employment";
                /// <summary>
                /// Ism Non Manufacturing New Orders
                /// </summary>
                public const string IsmNonManufacturingNewOrders = "USA/ism non manufacturing new orders";
                /// <summary>
                /// Ism Non Manufacturing Prices
                /// </summary>
                public const string IsmNonManufacturingPrices = "USA/ism non manufacturing prices";
                /// <summary>
                /// Ism Non Manufacturing Purchasing Managers Index
                /// </summary>
                public const string IsmNonManufacturingPurchasingManagersIndex = "USA/ism non manufacturing purchasing managers index";
                /// <summary>
                /// Ism Services Business Activity
                /// </summary>
                public const string IsmServicesBusinessActivity = "USA/ism services business activity";
                /// <summary>
                /// Ism Services Employment
                /// </summary>
                public const string IsmServicesEmployment = "USA/ism services employment";
                /// <summary>
                /// Ism Services New Orders
                /// </summary>
                public const string IsmServicesNewOrders = "USA/ism services new orders";
                /// <summary>
                /// Ism Services Prices
                /// </summary>
                public const string IsmServicesPrices = "USA/ism services prices";
                /// <summary>
                /// Ism Services Purchasing Managers Index
                /// </summary>
                public const string IsmServicesPurchasingManagersIndex = "USA/ism services purchasing managers index";
                /// <summary>
                /// Jobless Claims 4 Week Average
                /// </summary>
                public const string JoblessClaimsFourWeekAverage = "USA/jobless claims 4 week average";
                /// <summary>
                /// Jolts Job Openings
                /// </summary>
                public const string JoltsJobOpenings = "USA/jolts job openings";
                /// <summary>
                /// Jolts Job Quits
                /// </summary>
                public const string JoltsJobQuits = "USA/jolts job quits";
                /// <summary>
                /// Kansas Fed Composite Index
                /// </summary>
                public const string KansasFedCompositeIndex = "USA/kansas fed composite index";
                /// <summary>
                /// Kansas Fed Manufacturing Index
                /// </summary>
                public const string KansasFedManufacturingIndex = "USA/kansas fed manufacturing index";
                /// <summary>
                /// Labor Day
                /// </summary>
                public const string LaborDay = "USA/labor day";
                /// <summary>
                /// Lmi Logistics Managers Index
                /// </summary>
                public const string LmiLogisticsManagersIndex = "USA/lmi logistics managers index";
                /// <summary>
                /// Lmi Logistics Managers Index Current
                /// </summary>
                public const string LmiLogisticsManagersIndexCurrent = "USA/lmi logistics managers index current";
                /// <summary>
                /// Loan Officer Survey
                /// </summary>
                public const string LoanOfficerSurvey = "USA/loan officer survey";
                /// <summary>
                /// Manufacturing Payrolls
                /// </summary>
                public const string ManufacturingPayrolls = "USA/manufacturing payrolls";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "USA/manufacturing production";
                /// <summary>
                /// Markit Composite Purchasing Managers Index
                /// </summary>
                public const string MarkitCompositePurchasingManagersIndex = "USA/markit composite purchasing managers index";
                /// <summary>
                /// Markit Manufacturing Purchasing Managers Index
                /// </summary>
                public const string MarkitManufacturingPurchasingManagersIndex = "USA/markit manufacturing purchasing managers index";
                /// <summary>
                /// Markit Services Purchasing Managers Index
                /// </summary>
                public const string MarkitServicesPurchasingManagersIndex = "USA/markit services purchasing managers index";
                /// <summary>
                /// Martin Luther King Jr Day
                /// </summary>
                public const string MartinLutherKingJrDay = "USA/martin luther king jr day";
                /// <summary>
                /// Mba 30 Year Mortgage Rate
                /// </summary>
                public const string MbaThirtyYearMortgageRate = "USA/mba 30 year mortgage rate";
                /// <summary>
                /// Mba Mortgage Applications
                /// </summary>
                public const string MbaMortgageApplications = "USA/mba mortgage applications";
                /// <summary>
                /// Mba Mortgage Market Index
                /// </summary>
                public const string MbaMortgageMarketIndex = "USA/mba mortgage market index";
                /// <summary>
                /// Mba Mortgage Refinance Index
                /// </summary>
                public const string MbaMortgageRefinanceIndex = "USA/mba mortgage refinance index";
                /// <summary>
                /// Mba Purchase Index
                /// </summary>
                public const string MbaPurchaseIndex = "USA/mba purchase index";
                /// <summary>
                /// Memorial Day
                /// </summary>
                public const string MemorialDay = "USA/memorial day";
                /// <summary>
                /// Michigan 5 Year Inflation Expectations
                /// </summary>
                public const string MichiganFiveYearInflationExpectations = "USA/michigan 5 year inflation expectations";
                /// <summary>
                /// Michigan Consumer Expectations
                /// </summary>
                public const string MichiganConsumerExpectations = "USA/michigan consumer expectations";
                /// <summary>
                /// Michigan Consumer Sentiment
                /// </summary>
                public const string MichiganConsumerSentiment = "USA/michigan consumer sentiment";
                /// <summary>
                /// Michigan Current Conditions
                /// </summary>
                public const string MichiganCurrentConditions = "USA/michigan current conditions";
                /// <summary>
                /// Michigan Inflation Expectations
                /// </summary>
                public const string MichiganInflationExpectations = "USA/michigan inflation expectations";
                /// <summary>
                /// Money Supply
                /// </summary>
                public const string MoneySupply = "USA/money supply";
                /// <summary>
                /// Monthly Budget Statement
                /// </summary>
                public const string MonthlyBudgetStatement = "USA/monthly budget statement";
                /// <summary>
                /// Nahb Housing Market Index
                /// </summary>
                public const string NahbHousingMarketIndex = "USA/nahb housing market index";
                /// <summary>
                /// Net Long Term Tic Flows
                /// </summary>
                public const string NetLongTermTicFlows = "USA/net long term tic flows";
                /// <summary>
                /// New Home Sales
                /// </summary>
                public const string NewHomeSales = "USA/new home sales";
                /// <summary>
                /// Nfib Business Optimism Index
                /// </summary>
                public const string NfibBusinessOptimismIndex = "USA/nfib business optimism index";
                /// <summary>
                /// Non Defense Goods Orders Excluding Air
                /// </summary>
                public const string NonDefenseGoodsOrdersExcludingAir = "USA/non defense goods orders excluding air";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "USA/non farm payrolls";
                /// <summary>
                /// Non Farm Payrolls Private
                /// </summary>
                public const string NonFarmPayrollsPrivate = "USA/non farm payrolls private";
                /// <summary>
                /// Non Farm Productivity
                /// </summary>
                public const string NonFarmProductivity = "USA/non farm productivity";
                /// <summary>
                /// Nopa Crush Report
                /// </summary>
                public const string NopaCrushReport = "USA/nopa crush report";
                /// <summary>
                /// Ny Empire State Manufacturing Index
                /// </summary>
                public const string NyEmpireStateManufacturingIndex = "USA/ny empire state manufacturing index";
                /// <summary>
                /// Ny Fed Treasury Purchases 0 To 2 25 Yrs
                /// </summary>
                public const string NyFedTreasuryPurchasesZeroToTwoTwentyFiveYrs = "USA/ny fed treasury purchases 0 to 2 25 yrs";
                /// <summary>
                /// Ny Fed Treasury Purchases 10 To 22 5 Yrs
                /// </summary>
                public const string NyFedTreasuryPurchasesTenToTwentyTwoFiveYrs = "USA/ny fed treasury purchases 10 to 22 5 yrs";
                /// <summary>
                /// Ny Fed Treasury Purchases 2 25 To 4 5 Yrs
                /// </summary>
                public const string NyFedTreasuryPurchasesTwoTwentyFiveToFourFiveYrs = "USA/ny fed treasury purchases 2 25 to 4 5 yrs";
                /// <summary>
                /// Ny Fed Treasury Purchases 22 5 To 30 Yrs
                /// </summary>
                public const string NyFedTreasuryPurchasesTwentyTwoFiveToThirtyYrs = "USA/ny fed treasury purchases 22 5 to 30 yrs";
                /// <summary>
                /// Ny Fed Treasury Purchases 4 5 To 7 Yrs
                /// </summary>
                public const string NyFedTreasuryPurchasesFourFiveToSevenYrs = "USA/ny fed treasury purchases 4 5 to 7 yrs";
                /// <summary>
                /// Ny Fed Treasury Purchases 7 To 10 Yrs
                /// </summary>
                public const string NyFedTreasuryPurchasesSevenToTenYrs = "USA/ny fed treasury purchases 7 to 10 yrs";
                /// <summary>
                /// Ny Fed Treasury Purchases Tips 1 To 7 5 Yrs
                /// </summary>
                public const string NyFedTreasuryPurchasesTipsOneToSevenFiveYrs = "USA/ny fed treasury purchases tips 1 to 7 5 yrs";
                /// <summary>
                /// Ny Fed Treasury Purchases Tips 7 5 To 30 Yrs
                /// </summary>
                public const string NyFedTreasuryPurchasesTipsSevenFiveToThirtyYrs = "USA/ny fed treasury purchases tips 7 5 to 30 yrs";
                /// <summary>
                /// Overall Net Capital Flows
                /// </summary>
                public const string OverallNetCapitalFlows = "USA/overall net capital flows";
                /// <summary>
                /// Participation Rate
                /// </summary>
                public const string ParticipationRate = "USA/participation rate";
                /// <summary>
                /// Pending Home Sales
                /// </summary>
                public const string PendingHomeSales = "USA/pending home sales";
                /// <summary>
                /// Personal Consumption Expenditure Price Index
                /// </summary>
                public const string PersonalConsumptionExpenditurePriceIndex = "USA/personal consumption expenditure price index";
                /// <summary>
                /// Personal Consumption Expenditure Price Index Qoq Adv
                /// </summary>
                public const string PersonalConsumptionExpenditurePriceIndexQoqAdv = "USA/personal consumption expenditure price index qoq adv";
                /// <summary>
                /// Personal Income
                /// </summary>
                public const string PersonalIncome = "USA/personal income";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "USA/personal spending";
                /// <summary>
                /// Philadelphia Fed Manufacturing Index
                /// </summary>
                public const string PhiladelphiaFedManufacturingIndex = "USA/philadelphia fed manufacturing index";
                /// <summary>
                /// Philly Fed Business Conditions
                /// </summary>
                public const string PhillyFedBusinessConditions = "USA/philly fed business conditions";
                /// <summary>
                /// Philly Fed Capital Expenditure Index
                /// </summary>
                public const string PhillyFedCapitalExpenditureIndex = "USA/philly fed capital expenditure index";
                /// <summary>
                /// Philly Fed Employment
                /// </summary>
                public const string PhillyFedEmployment = "USA/philly fed employment";
                /// <summary>
                /// Philly Fed New Orders
                /// </summary>
                public const string PhillyFedNewOrders = "USA/philly fed new orders";
                /// <summary>
                /// Philly Fed Prices Paid
                /// </summary>
                public const string PhillyFedPricesPaid = "USA/philly fed prices paid";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "USA/ppi";
                /// <summary>
                /// Producer Price Index Excluding Food Energy And Trade
                /// </summary>
                public const string ProducerPriceIndexExcludingFoodEnergyAndTrade = "USA/producer price index excluding food energy and trade";
                /// <summary>
                /// Quarterly Grain Stocks Corn
                /// </summary>
                public const string QuarterlyGrainStocksCorn = "USA/quarterly grain stocks corn";
                /// <summary>
                /// Quarterly Grain Stocks Soy
                /// </summary>
                public const string QuarterlyGrainStocksSoy = "USA/quarterly grain stocks soy";
                /// <summary>
                /// Quarterly Grain Stocks Wheat
                /// </summary>
                public const string QuarterlyGrainStocksWheat = "USA/quarterly grain stocks wheat";
                /// <summary>
                /// Rcm Tipp Economic Optimism Index
                /// </summary>
                public const string RcmTippEconomicOptimismIndex = "USA/rcm tipp economic optimism index";
                /// <summary>
                /// Real Consumer Spending
                /// </summary>
                public const string RealConsumerSpending = "USA/real consumer spending";
                /// <summary>
                /// Real Consumer Spending Qoq Adv
                /// </summary>
                public const string RealConsumerSpendingQoqAdv = "USA/real consumer spending qoq adv";
                /// <summary>
                /// Redbook
                /// </summary>
                public const string Redbook = "USA/redbook";
                /// <summary>
                /// Retail Inventories Excluding Autos
                /// </summary>
                public const string RetailInventoriesExcludingAutos = "USA/retail inventories excluding autos";
                /// <summary>
                /// Retail Inventories Excluding Autos Mom Adv
                /// </summary>
                public const string RetailInventoriesExcludingAutosMomAdv = "USA/retail inventories excluding autos mom adv";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "USA/retail sales";
                /// <summary>
                /// Retail Sales Excluding Autos
                /// </summary>
                public const string RetailSalesExcludingAutos = "USA/retail sales excluding autos";
                /// <summary>
                /// Retail Sales Excluding Gas Autos
                /// </summary>
                public const string RetailSalesExcludingGasAutos = "USA/retail sales excluding gas autos";
                /// <summary>
                /// Richmond Fed Manufacturing Index
                /// </summary>
                public const string RichmondFedManufacturingIndex = "USA/richmond fed manufacturing index";
                /// <summary>
                /// Richmond Fed Manufacturing Shipments Index
                /// </summary>
                public const string RichmondFedManufacturingShipmentsIndex = "USA/richmond fed manufacturing shipments index";
                /// <summary>
                /// Richmond Fed Services Index
                /// </summary>
                public const string RichmondFedServicesIndex = "USA/richmond fed services index";
                /// <summary>
                /// Sandp Case Shiller Home Price
                /// </summary>
                public const string SandpCaseShillerHomePrice = "USA/sandp case shiller home price";
                /// <summary>
                /// Sandp Global Composite Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalCompositePurchasingManagersIndex = "USA/sandp global composite purchasing managers index";
                /// <summary>
                /// Sandp Global Manufacturing Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalManufacturingPurchasingManagersIndex = "USA/sandp global manufacturing purchasing managers index";
                /// <summary>
                /// Sandp Global Services Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalServicesPurchasingManagersIndex = "USA/sandp global services purchasing managers index";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "USA/total vehicle sales";
                /// <summary>
                /// Treasury Refunding Announcement
                /// </summary>
                public const string TreasuryRefundingAnnouncement = "USA/treasury refunding announcement";
                /// <summary>
                /// U 6 Unemployment Rate
                /// </summary>
                public const string USixUnemploymentRate = "USA/u 6 unemployment rate";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "USA/unemployment rate";
                /// <summary>
                /// Unit Labor Costs
                /// </summary>
                public const string UnitLaborCosts = "USA/unit labor costs";
                /// <summary>
                /// Us China Trade Talks
                /// </summary>
                public const string UsChinaTradeTalks = "USA/us china trade talks";
                /// <summary>
                /// Used Car Prices
                /// </summary>
                public const string UsedCarPrices = "USA/used car prices";
                /// <summary>
                /// Wasde Report
                /// </summary>
                public const string WasdeReport = "USA/wasde report";
                /// <summary>
                /// Washingtons Birthday
                /// </summary>
                public const string WashingtonsBirthday = "USA/washingtons birthday";
                /// <summary>
                /// Wholesale Inventories
                /// </summary>
                public const string WholesaleInventories = "USA/wholesale inventories";
                /// <summary>
                /// Wholesale Inventories Mom Adv
                /// </summary>
                public const string WholesaleInventoriesMomAdv = "USA/wholesale inventories mom adv";
                /// <summary>
                /// World Bank Imf Annual Meeting
                /// </summary>
                public const string WorldBankImfAnnualMeeting = "USA/world bank imf annual meeting";
            }

            /// <summary>
            /// Southafrica
            /// </summary>
            public static class SouthAfrica
            {
                /// <summary>
                /// Balance Of Trade
                /// </summary>
                public const string BalanceOfTrade = "ZAF/balance of trade";
                /// <summary>
                /// Budget Balance
                /// </summary>
                public const string BudgetBalance = "ZAF/budget balance";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ZAF/consumer confidence";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "ZAF/core inflation rate";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "ZAF/current account";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "ZAF/foreign exchange reserves";
                /// <summary>
                /// Freedom Day
                /// </summary>
                public const string FreedomDay = "ZAF/freedom day";
                /// <summary>
                /// Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ZAF/gdp growth rate";
                /// <summary>
                /// Gold Production
                /// </summary>
                public const string GoldProduction = "ZAF/gold production";
                /// <summary>
                /// Human Rights Day
                /// </summary>
                public const string HumanRightsDay = "ZAF/human rights day";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ZAF/inflation rate";
                /// <summary>
                /// Interest Rate Decision
                /// </summary>
                public const string InterestRateDecision = "ZAF/interest rate decision";
                /// <summary>
                /// Leading Business Cycle Indicators
                /// </summary>
                public const string LeadingBusinessCycleIndicators = "ZAF/leading business cycle indicators";
                /// <summary>
                /// M3 Money Supply
                /// </summary>
                public const string M3MoneySupply = "ZAF/m3 money supply";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "ZAF/manufacturing production";
                /// <summary>
                /// Markit Ihs Purchasing Managers Index
                /// </summary>
                public const string MarkitIhsPurchasingManagersIndex = "ZAF/markit ihs purchasing managers index";
                /// <summary>
                /// Mining Production
                /// </summary>
                public const string MiningProduction = "ZAF/mining production";
                /// <summary>
                /// National Womens Day
                /// </summary>
                public const string NationalWomensDay = "ZAF/national womens day";
                /// <summary>
                /// Ppi
                /// </summary>
                public const string Ppi = "ZAF/ppi";
                /// <summary>
                /// Prime Overdraft Rate
                /// </summary>
                public const string PrimeOverdraftRate = "ZAF/prime overdraft rate";
                /// <summary>
                /// Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "ZAF/private sector credit";
                /// <summary>
                /// Retail Sales
                /// </summary>
                public const string RetailSales = "ZAF/retail sales";
                /// <summary>
                /// Sacci Business Confidence
                /// </summary>
                public const string SacciBusinessConfidence = "ZAF/sacci business confidence";
                /// <summary>
                /// Sandp Global Purchasing Managers Index
                /// </summary>
                public const string SandpGlobalPurchasingManagersIndex = "ZAF/sandp global purchasing managers index";
                /// <summary>
                /// Standard Bank Purchasing Managers Index
                /// </summary>
                public const string StandardBankPurchasingManagersIndex = "ZAF/standard bank purchasing managers index";
                /// <summary>
                /// Total New Vehicle Sales
                /// </summary>
                public const string TotalNewVehicleSales = "ZAF/total new vehicle sales";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "ZAF/unemployed persons";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ZAF/unemployment rate";
            }

            /// <summary>
            /// Zambia
            /// </summary>
            public static class Zambia
            {
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ZMB/inflation rate";
            }
        }
    }
}