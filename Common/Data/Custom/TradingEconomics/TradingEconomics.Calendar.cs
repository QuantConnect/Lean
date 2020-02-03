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
    /// TradingEconomics static class contains shortcut definitions of major Trading Economics Indicators available
    /// </summary>
    public static partial class TradingEconomics
    {
        /// <summary>
        /// Calendar group
        /// </summary>
        public static class Calendar
        {
            /// <summary>
            /// Delimiter used to separate country from ticker in <see cref="TradingEconomics.Calendar"/> entries
            /// </summary>
            public const string Delimiter = "//";

            /// <summary>
            /// Australia
            /// </summary>
            public static class Australia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Australia//AUITGSB";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "Australia//AUSTRALIABUIPER";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Australia//NABSCONF";
                /// <summary>
                /// Business Inventories
                /// </summary>
                public const string BusinessInventories = "Australia//AUSTRALIABUSINV";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Australia//AUD-CALENDAR";
                /// <summary>
                /// Car Registrations
                /// </summary>
                public const string CarRegistrations = "Australia//AUSTRALIACARREG";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "Australia//AUSTRALIACOMPMI";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "Australia//AUSTRALIACONOUT";
                /// <summary>
                /// Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "Australia//AUSTRALIACONPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Australia//WMCCCONPCT";
                /// <summary>
                /// Consumer Price Index (CPI)
                /// </summary>
                public const string ConsumerPriceIndexCPI = "Australia//AUSTRALIACONPRIINDCP";
                /// <summary>
                /// Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "Australia//AUSTRALIACONSPE";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "Australia//AUSCORECPIRATE";
                /// <summary>
                /// Corporate Profits
                /// </summary>
                public const string CorporateProfits = "Australia//AUSTRALIACORPRO";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Australia//AUCABAL";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "Australia//AUSTRALIAEMPCHA";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "Australia//AUSTRALIAEXPPRI";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "Australia//AUITEXP";
                /// <summary>
                /// Full Time Employment
                /// </summary>
                public const string FullTimeEmployment = "Australia//AUSTRALIAFULTIMEMP";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Australia//AUNAGDPY";
                /// <summary>
                /// GDP Deflator
                /// </summary>
                public const string GDPDeflator = "Australia//AUSTRALIAGDPDEF";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Australia//AUNAGDPC";
                /// <summary>
                /// Gross Fixed Capital Formation
                /// </summary>
                public const string GrossFixedCapitalFormation = "Australia//AUSTRALIAGROFIXCAPFO";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Australia//HOLIDAYSAUSTRALIA";
                /// <summary>
                /// Home Loans
                /// </summary>
                public const string HomeLoans = "Australia//AUSTRALIAHOMLOA";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "Australia//AUSTRALIAHOUIND";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "Australia//AUSTRALIAIMPPRI";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "Australia//AUITIMP";
                /// <summary>
                /// Industrial Sentiment
                /// </summary>
                public const string IndustrialSentiment = "Australia//AUSTRALIAINDSEN";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "Australia//AUSTRALIAINFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Australia//AUCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Australia//AUSTRALIAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Australia//RBATCTR";
                /// <summary>
                /// Job Advertisements
                /// </summary>
                public const string JobAdvertisements = "Australia//AUSTRALIAJOBADV";
                /// <summary>
                /// Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "Australia//AUSTRALIALABFORPARRA";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "Australia//AUSTRALIALEAECOIND";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Australia//AUSTRALIAMANPMI";
                /// <summary>
                /// New Home Sales
                /// </summary>
                public const string NewHomeSales = "Australia//AUSTRALIANEWHOMSAL";
                /// <summary>
                /// Part Time Employment
                /// </summary>
                public const string PartTimeEmployment = "Australia//AUSTRALIAPARTIMEMP";
                /// <summary>
                /// Private Investment
                /// </summary>
                public const string PrivateInvestment = "Australia//AUSTRALIAPRIINV";
                /// <summary>
                /// Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "Australia//AUSTRALIAPRISECCRE";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Australia//AUSTRALIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Australia//AUSTRALIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Australia//AUSRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Australia//AUSRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "Australia//AUSTRALIASERPMI";
                /// <summary>
                /// Services Sentiment
                /// </summary>
                public const string ServicesSentiment = "Australia//AUSTRALIASERSEN";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "Australia//AUSTRALIATOTVEHSAL";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Australia//AULFUNEM";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "Australia//AUSTRALIAWAGGRO";
            }
            /// <summary>
            /// Austria
            /// </summary>
            public static class Austria
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Austria//ATTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Austria//AUSTRIABUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Austria//AUT-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Austria//AUSTRIACC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Austria//OEATB007";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Austria//ASGPGDPY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Austria//ASGPGDPQ";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Austria//AUSTRIAHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Austria//HOLIDAYSAUSTRIA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Austria//ATIPIYY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Austria//ECCPATYY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Austria//AUSTRIAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Austria//EURR002W-ATS";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Austria//AUSTRIAMANPMI";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Austria//AUSTRIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Austria//AUSTRIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Austria//AUTRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Austria//AUTRETSALYOY";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "Austria//AUSTRIAUNEPER";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Austria//UMRTAT";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "Austria//AUSTRIAWHOPRI";
            }
            /// <summary>
            /// Belgium
            /// </summary>
            public static class Belgium
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Belgium//BETBTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Belgium//BEBCI";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Belgium//BEL-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Belgium//BECCN";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Belgium//OEBEB026";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Belgium//BEGDPYS";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Belgium//BEGDPQS";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Belgium//HOLIDAYSBELGIUM";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Belgium//BEPDRYY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Belgium//BELGIUMINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Belgium//BECPYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Belgium//BELGIUMINFRATMOM";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Belgium//BELGIUMPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Belgium//BELRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Belgium//BELRETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Belgium//BEUER";
            }
            /// <summary>
            /// Canada
            /// </summary>
            public static class Canada
            {
                /// <summary>
                /// Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "Canada//CANADAADPEMPCHA";
                /// <summary>
                /// Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "Canada//CANADAAVEHOUEAR";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Canada//CATBTOTB";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "Canada//CANADABUIPER";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Canada//IVEY";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Canada//CAD-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "Canada//CANADACAPUTI";
                /// <summary>
                /// Car Registrations
                /// </summary>
                public const string CarRegistrations = "Canada//CANADACARREG";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "Canada//CANCORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Canada//CACURENT";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "Canada//CANADAEMPCHA";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "Canada//CATBTOTE";
                /// <summary>
                /// Foreign Stock Investment
                /// </summary>
                public const string ForeignStockInvestment = "Canada//CANADAFORSTOINV";
                /// <summary>
                /// Full Time Employment
                /// </summary>
                public const string FullTimeEmployment = "Canada//CANADAFULTIMEMP";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Canada//CGE9YOY";
                /// <summary>
                /// GDP Deflator
                /// </summary>
                public const string GDPDeflator = "Canada//CANADAGDPDEF";
                /// <summary>
                /// Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "Canada//CANADAGDPGROANN";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Canada//CGE9QOQ";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "Canada//CANADAGOVBUDVAL";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Canada//HOLIDAYSCANADA";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "Canada//CANADAHOUIND";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "Canada//CANADAHOUSTA";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "Canada//CATBTOTI";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Canada//CACPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Canada//CANADAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Canada//CCLR";
                /// <summary>
                /// Job Vacancies
                /// </summary>
                public const string JobVacancies = "Canada//CANADAJOBVAC";
                /// <summary>
                /// Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "Canada//CANADALABFORPARRAT";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "Canada//CANADALEAECOIND";
                /// <summary>
                /// Manufacturing PMI
                /// </summary>
                public const string ManufacturingPMI = "Canada//CANADAMANPMI";
                /// <summary>
                /// Manufacturing Sales
                /// </summary>
                public const string ManufacturingSales = "Canada//CANADAMANSAL";
                /// <summary>
                /// Part Time Employment
                /// </summary>
                public const string PartTimeEmployment = "Canada//CANADAPARTIMEMP";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Canada//CANADAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Canada//CANADAPROPRICHA";
                /// <summary>
                /// Productivity
                /// </summary>
                public const string Productivity = "Canada//CANADAPRO";
                /// <summary>
                /// Retail Sales Ex Autos
                /// </summary>
                public const string RetailSalesExAutos = "Canada//CANADARETSALEXAUT";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Canada//CANRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Canada//CANNADARETSYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Canada//CANLXEMR";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "Canada//CANADAWAGGRO";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "Canada//CANADAWHOPRI";
                /// <summary>
                /// Wholesale Sales
                /// </summary>
                public const string WholesaleSales = "Canada//CANADAWHOSAL";
            }
            /// <summary>
            /// China
            /// </summary>
            public static class China
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "China//CNFRBALD";
                /// <summary>
                /// Banks Balance Sheet
                /// </summary>
                public const string BanksBalanceSheet = "China//CHINABANBALSHE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "China//CHBUBCIN";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "China//CHN-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "China//CHINACAPUTI";
                /// <summary>
                /// Capital Flows
                /// </summary>
                public const string CapitalFlows = "China//CHINACAPFLO";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "China//CHINACOMPMI";
                /// <summary>
                /// Corporate Profits
                /// </summary>
                public const string CorporateProfits = "China//CHINACORPRO";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "China//CHCURENT";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "China//CNFREXPD";
                /// <summary>
                /// Fixed Asset Investment
                /// </summary>
                public const string FixedAssetInvestment = "China//CHINAFIXASSINV";
                /// <summary>
                /// Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "China//CHINAFORDIRINV";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "China//CHINAFOREXCRES";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "China//CNGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "China//CNGDPQOQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "China//HOLIDAYSCHINA";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "China//CHINAHOUIND";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "China//CNFRIMPD";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "China//CHVAIOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "China//CNCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "China//CHINAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "China//CHLR12M";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "China//CHINALOAGRO";
                /// <summary>
                /// Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "China//CHINALOATOPRISEC";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "China//CHINAMANPMI";
                /// <summary>
                /// Mni Business Sentiment
                /// </summary>
                public const string MniBusinessSentiment = "China//CHINAMNIBUSSEN";
                /// <summary>
                /// Mni Consumer Sentiment
                /// </summary>
                public const string MniConsumerSentiment = "China//CHINAMNICONSEN";
                /// <summary>
                /// Money Supply M2
                /// </summary>
                public const string MoneySupplyMTwo = "China//CHINAMONSUPM2";
                /// <summary>
                /// Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "China//CHINANONMANPMI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "China//CHINAPROPRICHA";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "China//CHNRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "China//CHINASERPMI";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "China//CHINATOTVEHSAL";
            }
            /// <summary>
            /// Cyprus
            /// </summary>
            public static class Cyprus
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Cyprus//CYPPRUSBALRADE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Cyprus//CYPRUSBUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Cyprus//CYP-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "Cyprus//CYPRUSCONOUT";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Cyprus//CYPRUSCONCON";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Cyprus//CYPPRUSCURCOUNT";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Cyprus//CYPPRUSGDPATE";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Cyprus//CYPPRUSGDPRATE";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Cyprus//CYPRUSHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Cyprus//HOLIDAYSCYPRUS";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Cyprus//CYPPRUSINDCTION";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Cyprus//CYPPRUSINFNRATE";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Cyprus//CYPPRUSRETSYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Cyprus//CYPPRUSUNETRATE";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "Cyprus//CYPRUSWAGGRO";
            }
            /// <summary>
            /// Estonia
            /// </summary>
            public static class Estonia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Estonia//ESTONIABT";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Estonia//ESTONIABUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Estonia//EST-CALENDAR";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Estonia//ESTONIACA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Estonia//ESTONIAGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Estonia//ESTONIAGDPQOQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Estonia//HOLIDAYSESTONIA";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "Estonia//ESTONIAIM";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Estonia//ESTONIAIP";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Estonia//ESTONIAINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Estonia//ESTONIAIR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Estonia//ESTONIAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Estonia//EURR002W-EEK";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Estonia//ESTONIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Estonia//ESTONIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Estonia//ESTRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Estonia//ESTRETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Estonia//ESTONIAUR";
            }
            /// <summary>
            /// Finland
            /// </summary>
            public static class Finland
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Finland//FITBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Finland//FINLANDBUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Finland//EUR-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Finland//FICCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Finland//FICAEUR";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "Finland//FINLANDEXPPRI";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Finland//FIGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Finland//FIGDPCH";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Finland//HOLIDAYSFINLAND";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "Finland//FINLANDIMPPRI";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Finland//FIIPYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Finland//FICP2YOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Finland//FINLANDINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Finland//EURR002W-FIM";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "Finland//FINLANDLEAECOIND";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Finland//FINLANDPROPRICHA";
                /// <summary>
                /// Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "Finland//FINLANDRETSALMOM";
                /// <summary>
                /// Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "Finland//FINLANDRETSALYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Finland//UMRTFI";
            }
            /// <summary>
            /// France
            /// </summary>
            public static class France
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "France//FRTEBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "France//INSESYNT";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "France//EUR-CALENDAR";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "France//FRANCECOMPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "France//FRCCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "France//FRCAEURO";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "France//FRANCE5WBY";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "France//FRANCE5YNY";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "France//FRGEGDPY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "France//FRGEGDPQ";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "France//GFRN10";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "France//FRANCEGOVBUDVAL";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "France//FRANCEHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "France//HOLIDAYSFRANCE";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "France//FPIPYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "France//FRANCEINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "France//FRCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "France//FRANCEINFRATMOM";
                /// <summary>
                /// Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "France//FRANCEINIJOBCLA";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "France//FRANCEMANPMI";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "France//FRANCENONFARPAY";
                /// <summary>
                /// Nonfarm Payrolls Private
                /// </summary>
                public const string NonfarmPayrollsPrivate = "France//FRANCENONPAYPRI";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "France//FRANCEPERSPE";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "France//FRANCEPROPRI";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "France//FRARETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "France//FRARETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "France//FRANCESERPMI";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "France//FRANCE6MONBILYIE";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "France//FRANCE3MONBILYIE";
                /// <summary>
                /// 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "France//FRANCE3YNY";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "France//FRANCEUNEPER";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "France//UMRTFR";
            }
            /// <summary>
            /// Germany
            /// </summary>
            public static class Germany
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Germany//GRTBALE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Germany//GRIFPBUS";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Germany//DEU-CALENDAR";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "Germany//GERMANYCOMPMI";
                /// <summary>
                /// Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "Germany//GERMANYCONPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Germany//GRCCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Germany//GRCAEU";
                /// <summary>
                /// Employed Persons
                /// </summary>
                public const string EmployedPersons = "Germany//GERMANYEMPPER";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "Germany//GRTBEXE";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "Germany//GERMANYFACORD";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "Germany//GERMANY5WBY";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "Germany//GERMANY5YNY";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Germany//GRGDPPGY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Germany//GRGDPPGQ";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "Germany//GDBR10";
                /// <summary>
                /// Government Budget
                /// </summary>
                public const string GovernmentBudget = "Germany//WCSDDEU";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Germany//GERMANYHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Germany//HOLIDAYSGERMANY";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "Germany//GERMANYIMPPRI";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Germany//GRIPIYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Germany//GERMANYINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Germany//GRBC20YY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Germany//GERMANYINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Germany//EURR002W-DEM";
                /// <summary>
                /// Job Vacancies
                /// </summary>
                public const string JobVacancies = "Germany//GERMANYJOBVAC";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Germany//GERMANYMANPMI";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Germany//GERMANYPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Germany//GERMANYPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Germany//DEURETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Germany//DEURETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "Germany//GERMANYSERPMI";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "Germany//GERMANY3YBY";
                /// <summary>
                /// 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "Germany//GERMANY2YNY";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "Germany//GERMANYUNEPER";
                /// <summary>
                /// Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "Germany//GERMANYUNECHA";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Germany//GRUEPR";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "Germany//GERMANYWHOPRI";
                /// <summary>
                /// Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "Germany//GERMANYZEWECOSENIND";
            }
            /// <summary>
            /// Greece
            /// </summary>
            public static class Greece
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Greece//GKTBALE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Greece//GREECEBC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Greece//EUR-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "Greece//GREECECONOUT";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Greece//GREECECC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Greece//GKCAEUR";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Greece//GKGNVYY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Greece//GKGNVQQ";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Greece//GREECEHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Greece//HOLIDAYSGREECE";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Greece//GKIPIYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Greece//GKCPNEWY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Greece//GREECEINFRATMOM";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "Greece//GREECELOAGRO";
                /// <summary>
                /// Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "Greece//GREECELOATOPRISEC";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Greece//GREECEMANPMI";
                /// <summary>
                /// Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "Greece//GREECEPRISECCRE";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Greece//GREECEPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Greece//GRCRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Greece//GRCRETAILSALESYOY";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "Greece//GREECE6MBY";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "Greece//GREECE3MBY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Greece//UMRTGR";
            }
            /// <summary>
            /// Ireland
            /// </summary>
            public static class Ireland
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Ireland//IETBALNS";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Ireland//EUR-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "Ireland//IRELANDCONOUT";
                /// <summary>
                /// Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "Ireland//IRELANDCONPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Ireland//EUCCIE";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "Ireland//IRLCORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Ireland//IECA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Ireland//IEGRPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Ireland//IEGRPQOQ";
                /// <summary>
                /// Gross National Product
                /// </summary>
                public const string GrossNationalProduct = "Ireland//IRELANDGRONATPRO";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Ireland//IRELANDHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Ireland//HOLIDAYSIRELAND";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "Ireland//IRELANDHOUIND";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Ireland//IEIPIYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Ireland//IECPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Ireland//IRELANDINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Ireland//EURR002W-IEP";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Ireland//IRELANDMANPMI";
                /// <summary>
                /// Personal Savings
                /// </summary>
                public const string PersonalSavings = "Ireland//IRELANDPERSAV";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Ireland//IRELANDPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Ireland//IRELANDPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Ireland//IRLRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Ireland//IRLRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "Ireland//IRELANDSERPMI";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Ireland//IEUERT";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "Ireland//IRELANDWAGGRO";
            }
            /// <summary>
            /// Italy
            /// </summary>
            public static class Italy
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Italy//ITTRBSAT";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Italy//ITBCI";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Italy//EUR-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "Italy//ITALYCONOUT";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Italy//ITPSSA";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Italy//ITCAEUR";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "Italy//ITALYFACORD";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "Italy//ITALY5WBY";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "Italy//ITALY5YNY";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Italy//ITPIRLYS";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Italy//ITPIRLQS";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "Italy//GBTPGR10";
                /// <summary>
                /// Government Budget
                /// </summary>
                public const string GovernmentBudget = "Italy//WCSDITA";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Italy//ITALYHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Italy//HOLIDAYSITALY";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Italy//ITPRWAY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Italy//ITALYINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Italy//ITCPNICY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Italy//ITALYINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Italy//EURR002W-ITL";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Italy//ITALYMANPMI";
                /// <summary>
                /// Manufacturing Sales
                /// </summary>
                public const string ManufacturingSales = "Italy//ITALYMANSAL";
                /// <summary>
                /// New Orders
                /// </summary>
                public const string NewOrders = "Italy//ITALYNEWORD";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Italy//ITALYPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Italy//ITALYPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Italy//ITARETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Italy//ITARETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "Italy//ITALYSERPMI";
                /// <summary>
                /// 7 Year Note Yield
                /// </summary>
                public const string SevenYearNoteYield = "Italy//ITALY7YNY";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "Italy//ITALY6MBY";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "Italy//ITALY3YBY";
                /// <summary>
                /// 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "Italy//ITALY3YNY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Italy//UMRTIT";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "Italy//ITALYWAGGRO";
            }
            /// <summary>
            /// Japan
            /// </summary>
            public static class Japan
            {
                /// <summary>
                /// All Industry Activity Index
                /// </summary>
                public const string AllIndustryActivityIndex = "Japan//JAPANAIAI";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Japan//JNTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Japan//JNSBALLI";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Japan//JPY-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "Japan//JAPANCAPUTI";
                /// <summary>
                /// Coincident Index
                /// </summary>
                public const string CoincidentIndex = "Japan//JAPANCOIIND";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "Japan//JAPANCOMPMI";
                /// <summary>
                /// Construction Orders
                /// </summary>
                public const string ConstructionOrders = "Japan//JAPANCONORD";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Japan//JCOMACF";
                /// <summary>
                /// Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "Japan//JAPANCONSPE";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "Japan//JPNCORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Japan//JNBPAB";
                /// <summary>
                /// Economy Watchers Survey
                /// </summary>
                public const string EconomyWatchersSurvey = "Japan//JAPANECOWATSUR";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "Japan//JNBPEXP";
                /// <summary>
                /// Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "Japan//JAPANFORBONINV";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "Japan//JAPANFOREXCRES";
                /// <summary>
                /// Foreign Stock Investment
                /// </summary>
                public const string ForeignStockInvestment = "Japan//JAPANFORSTOINV";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Japan//JGDPNSAQ";
                /// <summary>
                /// GDP Deflator
                /// </summary>
                public const string GDPDeflator = "Japan//JAPANGDPDEF";
                /// <summary>
                /// Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "Japan//JAPANGDPGROANN";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Japan//JGDPAGDP";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "Japan//GJGB10";
                /// <summary>
                /// Gross Fixed Capital Formation
                /// </summary>
                public const string GrossFixedCapitalFormation = "Japan//JAPANGROFIXCAPFOR";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Japan//HOLIDAYSJAPAN";
                /// <summary>
                /// Household Spending
                /// </summary>
                public const string HouseholdSpending = "Japan//JAPANHOUSPE";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "Japan//JAPANHOUSTA";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "Japan//JNBPIMP";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Japan//JNIPYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Japan//JAPANINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Japan//JNCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Japan//JAPANINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Japan//BOJDTR";
                /// <summary>
                /// Jobs To Applications Ratio
                /// </summary>
                public const string JobsToApplicationsRatio = "Japan//JAPANJTAR";
                /// <summary>
                /// Leading Composite Index
                /// </summary>
                public const string LeadingCompositeIndex = "Japan//JAPANLEACOMIND";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "Japan//JAPANLEACOMIND";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "Japan//JAPANLOAGRO";
                /// <summary>
                /// Machinery Orders
                /// </summary>
                public const string MachineryOrders = "Japan//JAPANMACORD";
                /// <summary>
                /// Machine Tool Orders
                /// </summary>
                public const string MachineToolOrders = "Japan//JAPANMACTOOORD";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Japan//JAPANMANPMI";
                /// <summary>
                /// Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "Japan//JAPANNONMANPMI";
                /// <summary>
                /// Private Investment
                /// </summary>
                public const string PrivateInvestment = "Japan//JAPANPRIINV";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Japan//JAPANPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Japan//JAPANPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Japan//JPNRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Japan//JPNRETAILSALESYOY";
                /// <summary>
                /// Reuters Tankan Index
                /// </summary>
                public const string ReutersTankanIndex = "Japan//JAPANREUTANIND";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "Japan//JAPANSERPMI";
                /// <summary>
                /// Small Business Sentiment
                /// </summary>
                public const string SmallBusinessSentiment = "Japan//JAPANSMABUSSEN";
                /// <summary>
                /// Tertiary Industry Index
                /// </summary>
                public const string TertiaryIndustryIndex = "Japan//JAPANTERINDIND";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "Japan//JAPAN3YBY";
                /// <summary>
                /// Tokyo Cpi
                /// </summary>
                public const string TokyoCpi = "Japan//JAPANTOKCPI";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Japan//JNUE";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "Japan//JAPANWAGGRO";
            }
            /// <summary>
            /// Latvia
            /// </summary>
            public static class Latvia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Latvia//LATVIABT";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Latvia//LVA-CALENDAR";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Latvia//LATVIACA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Latvia//LATVIAGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Latvia//LATVIAGDPQOQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Latvia//HOLIDAYSLATVIA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Latvia//LATVIAIP";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Latvia//LATVIAINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Latvia//LATVIAIR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Latvia//LATVIAINFRATMOM";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Latvia//LATVIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Latvia//LATVIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Latvia//LVARETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Latvia//LVARETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Latvia//LATVIAUR";
            }
            /// <summary>
            /// Lithuania
            /// </summary>
            public static class Lithuania
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Lithuania//LITHUANIABT";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Lithuania//LITHUANIABC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Lithuania//LTU-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Lithuania//LITHUANIACC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Lithuania//LITHUANIACA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Lithuania//LITHUANIAGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Lithuania//LITHUANIAGDPQOQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Lithuania//HOLIDAYSLITHUANIA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Lithuania//LITHUANIAINDPRO";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Lithuania//LITHUANIAINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Lithuania//LITHUANIAIR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Lithuania//LITHUANIAINFRATMOM";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Lithuania//LITHUANIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Lithuania//LITHUANIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Lithuania//LTURETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Lithuania//LTURETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Lithuania//LITHUANIAUR";
            }
            /// <summary>
            /// Luxembourg
            /// </summary>
            public static class Luxembourg
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Luxembourg//LXTBBALL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Luxembourg//LUXEMBOURGBUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Luxembourg//LUX-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Luxembourg//LUXEMBOURGCC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Luxembourg//LXCAEU";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Luxembourg//ENGKLUY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Luxembourg//ENGKLUQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Luxembourg//HOLIDAYSLUXEMBOURG";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Luxembourg//LXIPRYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Luxembourg//LXCPIYOY";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Luxembourg//LUXEMBOURGPROPRICHA";
                /// <summary>
                /// Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "Luxembourg//LUXEMBOURGRETSALMOM";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Luxembourg//OELUU003";
            }
            /// <summary>
            /// Malta
            /// </summary>
            public static class Malta
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Malta//MALALTABALRADE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Malta//MALTABUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Malta//MLT-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Malta//MALTACONCON";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Malta//MALALTAGDPATE";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Malta//HOLIDAYSMALTA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Malta//MALALTAINDCTION";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Malta//MALALTAINFNRATE";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Malta//MALTAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Malta//MALALTARETSMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Malta//MALALTARETSYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Malta//MALALTAUNETRATE";
            }
            /// <summary>
            /// Netherlands
            /// </summary>
            public static class Netherlands
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Netherlands//NETBEU";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Netherlands//NEPRI";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Netherlands//EUR-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Netherlands//NECCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Netherlands//NECATOTE";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Netherlands//NEGDPEY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Netherlands//NEGDPEQ";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "Netherlands//GNTH10YR";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Netherlands//HOLIDAYSNETHERLANDS";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Netherlands//NEIP20YY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Netherlands//NETHERLANDINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Netherlands//NECPIYOY";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Netherlands//NETHERLANDMANPMI";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "Netherlands//NETHERLANDMANPRO";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "Netherlands//NETHERLANDPERSPE";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Netherlands//NLDRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Netherlands//NLDRETAILSALESYOY";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "Netherlands//NETHERLAND6MBY";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "Netherlands//NETHERLAND3MBY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Netherlands//NEUETOTR";
            }
            /// <summary>
            /// New Zealand
            /// </summary>
            public static class NewZealand
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "New-Zealand//NZMTBAL";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "New-Zealand//NEWZEALANBUIPER";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "New-Zealand//NZIEBCAC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "New-Zealand//NZD-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "New-Zealand//NEWZEALANCAPUTI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "New-Zealand//NZCC";
                /// <summary>
                /// Credit Card Spending
                /// </summary>
                public const string CreditCardSpending = "New-Zealand//NEWZEALANCRECARSPE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "New-Zealand//NZBPCA";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "New-Zealand//NEWZEALANEMPCHA";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "New-Zealand//NEWZEALANEXPPRI";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "New-Zealand//NZMTEXP";
                /// <summary>
                /// Food Inflation
                /// </summary>
                public const string FoodInflation = "New-Zealand//NEWZEALANFOOINF";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "New-Zealand//NZNTGDPY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "New-Zealand//NZNTGDPC";
                /// <summary>
                /// Global Dairy Trade Price Index
                /// </summary>
                public const string GlobalDairyTradePriceIndex = "New-Zealand//NEWZEALANGDTPI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "New-Zealand//HOLIDAYSNEW-ZEALAND";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "New-Zealand//NEWZEALANHOUIND";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "New-Zealand//NEWZEALANIMPPRI";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "New-Zealand//NZMTIMP";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "New-Zealand//OENZV012";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "New-Zealand//NEWZEALANINFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "New-Zealand//NZCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "New-Zealand//NEWZEALANINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "New-Zealand//NZOCRS";
                /// <summary>
                /// Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "New-Zealand//NEWZEALANLABFORPARRA";
                /// <summary>
                /// Labour Costs
                /// </summary>
                public const string LabourCosts = "New-Zealand//NEWZEALANLABCOS";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "New-Zealand//NEWZEALANMANPMI";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "New-Zealand//NEWZEALANPROPRI";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "New-Zealand//NZLRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "New-Zealand//NZLRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "New-Zealand//NEWZEALANSERPMI";
                /// <summary>
                /// Terms of Trade
                /// </summary>
                public const string TermsOfTrade = "New-Zealand//NEWZEALANTEROFTRA";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "New-Zealand//NEWZEALANTOUARR";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "New-Zealand//NZLFUNER";
            }
            /// <summary>
            /// Portugal
            /// </summary>
            public static class Portugal
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Portugal//PTTBEUAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Portugal//PORTUGALBC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Portugal//EUR-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Portugal//PTCCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Portugal//PTCUEURO";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "Portugal//PTTBEUEX";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Portugal//PTGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Portugal//PTGDPQOQ";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "Portugal//PORTUGALGOVBUDVAL";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Portugal//HOLIDAYSPORTUGAL";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "Portugal//PTTBEUIM";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Portugal//PTIPTOTY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Portugal//PORTUGALINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Portugal//PLCPYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Portugal//PORTUGALINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Portugal//EURR002W-PTE";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "Portugal//PORTUGALLEAECOIND";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "Portugal//PORTUGALPERSPE";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Portugal//PORTUGALPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Portugal//PORTUGALPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Portugal//PRTRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Portugal//PRTRETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Portugal//PTUE";
            }
            /// <summary>
            /// Slovakia
            /// </summary>
            public static class Slovakia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Slovakia//SLOVAKIABT";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Slovakia//SLOVAKIABC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Slovakia//SVK-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "Slovakia//SLOVAKIACONOUT";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Slovakia//SLOVAKIACC";
                /// <summary>
                /// Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "Slovakia//SLOVAKIACORCONPRI";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "Slovakia//SVKCORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Slovakia//SLOVAKIACA";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "Slovakia//SLOVAKIAEX";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Slovakia//SLOVAKIAGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Slovakia//SLOVAKIAGDPQOQ";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Slovakia//SLOVAKIAHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Slovakia//HOLIDAYSSLOVAKIA";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "Slovakia//SLOVAKIAIM";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Slovakia//SLOVAKIAIP";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Slovakia//SLOVAKIAINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Slovakia//SLOVAKIAIR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Slovakia//SLOVAKIAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Slovakia//EURR002W-SKK";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Slovakia//SVKRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Slovakia//SVKRETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Slovakia//SLOVAKIAUR";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "Slovakia//SLOVAKIAWAGGRO";
            }
            /// <summary>
            /// Slovenia
            /// </summary>
            public static class Slovenia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Slovenia//SVTBBALE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Slovenia//SLOVENIABC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Slovenia//SVN-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Slovenia//EUCCSI";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Slovenia//SVGDCYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Slovenia//EUGNSIQQ";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Slovenia//SLOVENIAHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Slovenia//HOLIDAYSSLOVENIA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Slovenia//SVIPTYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Slovenia//SVCPYOY";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Slovenia//EURR002W-SIT";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Slovenia//SLOVENIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Slovenia//SVNRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Slovenia//SVNRETAILSALESYOY";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "Slovenia//SLOVENIATOUARR";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Slovenia//SVUER";
            }
            /// <summary>
            /// Spain
            /// </summary>
            public static class Spain
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Spain//SPTBEUBL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Spain//SPAINBC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Spain//ESP-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Spain//SPAINCC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Spain//SPCAEURO";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "Spain//SPAINFACORD";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "Spain//SPAIN5WBY";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "Spain//SPAIN5YNY";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Spain//SPNAGDPY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Spain//SPNAGDPQ";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "Spain//GSPG10YR";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "Spain//SPAINHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Spain//HOLIDAYSSPAIN";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Spain//SPAINIP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Spain//SPIPCYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Spain//SPAININFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Spain//EURR002W-ESP";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Spain//SPAINMANPMI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Spain//SPAINPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Spain//ESPRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Spain//ESPRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "Spain//SPAINSERPMI";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "Spain//SPAIN6MBY";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "Spain//SPAIN3MBY";
                /// <summary>
                /// 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "Spain//SPAIN3YNY";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "Spain//SPAINTOTVEHSAL";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "Spain//SPAINTOUARR";
                /// <summary>
                /// Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "Spain//SPAINUNECHA";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Spain//UMRTES";
            }
            /// <summary>
            /// Sweden
            /// </summary>
            public static class Sweden
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Sweden//SWTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Sweden//SWEDENBC";
                /// <summary>
                /// Business Inventories
                /// </summary>
                public const string BusinessInventories = "Sweden//SWEDENBUSINV";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Sweden//SEK-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "Sweden//SWEDENCAPUTI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Sweden//SWECCI";
                /// <summary>
                /// Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "Sweden//SWEDENCORCONPRI";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "Sweden//SWECORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Sweden//SWCA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Sweden//SWGDPAYY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Sweden//SWGDPAQQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Sweden//HOLIDAYSSWEDEN";
                /// <summary>
                /// Household Spending
                /// </summary>
                public const string HouseholdSpending = "Sweden//SWEDENHOUSPE";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Sweden//SWIPIYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "Sweden//SWEDENINDPROMOM";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "Sweden//SWEDENINFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Sweden//SWCPYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Sweden//SWEDENINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Sweden//SWRRATEI";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "Sweden//SWEDENLOAGRO";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Sweden//SWEDENMANPMI";
                /// <summary>
                /// New Orders
                /// </summary>
                public const string NewOrders = "Sweden//SWEDENNEWORD";
                /// <summary>
                /// Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "Sweden//SWEDENPRISECCRE";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Sweden//SWEDENPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Sweden//SWEDENPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Sweden//SWERETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Sweden//SWERETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "Sweden//SWEDENSERPMI";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Sweden//UMRTSE";
            }
            /// <summary>
            /// Switzerland
            /// </summary>
            public static class Switzerland
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "Switzerland//SZTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "Switzerland//SWITZERLANDBC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "Switzerland//CHEINDICATORS";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "Switzerland//SZCCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "Switzerland//SZCA";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "Switzerland//SWITZERLANFACORD";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "Switzerland//SWITZERLANFOREXCRES";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "Switzerland//SZGDPCYY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "Switzerland//SZGDPCQQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "Switzerland//HOLIDAYSSWITZERLAND";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "Switzerland//SZIPIYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "Switzerland//SZCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "Switzerland//SWITZERLANINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "Switzerland//SZLTTR";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "Switzerland//SWITZERLANMANPMI";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "Switzerland//SWITZERLANNONFARPAY";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "Switzerland//SWITZERLANPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "Switzerland//SWITZERLANPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "Switzerland//CHERETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "Switzerland//CHERETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "Switzerland//SZUEUEA";
                /// <summary>
                /// Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "Switzerland//SWITZERLANZEWECOSENI";
            }
            /// <summary>
            /// United Kingdom
            /// </summary>
            public static class UnitedKingdom
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "United-Kingdom//UKTBTTBA";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "United-Kingdom//EUR1UK";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "United-Kingdom//GBP-CALENDAR";
                /// <summary>
                /// Car Registrations
                /// </summary>
                public const string CarRegistrations = "United-Kingdom//UNITEDKINCARREG";
                /// <summary>
                /// CBI Distributive Trades
                /// </summary>
                public const string CBIDistributiveTrades = "United-Kingdom//UNITEDKINCBIDISTRA";
                /// <summary>
                /// Claimant Count Change
                /// </summary>
                public const string ClaimantCountChange = "United-Kingdom//UNITEDKINCLACOUCHA";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "United-Kingdom//UNITEDKINCOMPMI";
                /// <summary>
                /// Construction Orders
                /// </summary>
                public const string ConstructionOrders = "United-Kingdom//UNITEDKINCONORD";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "United-Kingdom//UNITEDKINCONOUT";
                /// <summary>
                /// Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "United-Kingdom//UNITEDKINCONPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "United-Kingdom//UKCCI";
                /// <summary>
                /// Consumer Credit
                /// </summary>
                public const string ConsumerCredit = "United-Kingdom//UNITEDKINCONCRE";
                /// <summary>
                /// Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "United-Kingdom//UNITEDKINCORCONPRI";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "United-Kingdom//GBRCORECPIRATE";
                /// <summary>
                /// Core Producer Prices
                /// </summary>
                public const string CoreProducerPrices = "United-Kingdom//UNITEDKINCORPROPRI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "United-Kingdom//UKCA";
                /// <summary>
                /// Economic Activity Index
                /// </summary>
                public const string EconomicActivityIndex = "United-Kingdom//UNITEDKINECOACTIND";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "United-Kingdom//UNITEDKINEMPCHA";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "United-Kingdom//UNITEDKINFACORD";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "United-Kingdom//UNITEDKIN5YEANOTYIE";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "United-Kingdom//UKGRYBZY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "United-Kingdom//UKGRYBZQ";
                /// <summary>
                /// Goods Trade Balance
                /// </summary>
                public const string GoodsTradeBalance = "United-Kingdom//UNITEDKINGOOTRABAL";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "United-Kingdom//GUKG10";
                /// <summary>
                /// Government Debt
                /// </summary>
                public const string GovernmentDebt = "United-Kingdom//UNITEDKINGOVDEB";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "United-Kingdom//HOLIDAYSUNITED-KING";
                /// <summary>
                /// Home Loans
                /// </summary>
                public const string HomeLoans = "United-Kingdom//UNITEDKINHOMLOA";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "United-Kingdom//UNITEDKINHOUIND";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "United-Kingdom//UKIPIYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "United-Kingdom//UNITEDKININDPROMOM";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "United-Kingdom//UNITEDKININFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "United-Kingdom//UKRPCJYR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "United-Kingdom//UNITEDKININFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "United-Kingdom//UKBRBASE";
                /// <summary>
                /// Labour Costs
                /// </summary>
                public const string LabourCosts = "United-Kingdom//UNITEDKINLABCOS";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "United-Kingdom//UNITEDKINLEAECOIND";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "United-Kingdom//UNITEDKINMANPMI";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "United-Kingdom//UNITEDKINMANPRO";
                /// <summary>
                /// Mortgage Applications
                /// </summary>
                public const string MortgageApplications = "United-Kingdom//UNITEDKINMORAPPLI";
                /// <summary>
                /// Mortgage Approvals
                /// </summary>
                public const string MortgageApprovals = "United-Kingdom//UNITEDKINMORAPP";
                /// <summary>
                /// Nationwide Housing Prices
                /// </summary>
                public const string NationwideHousingPrices = "United-Kingdom//UNITEDKINNATHOUPRI";
                /// <summary>
                /// Private Investment
                /// </summary>
                public const string PrivateInvestment = "United-Kingdom//UNITEDKINPRIINV";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "United-Kingdom//UNITEDKINPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "United-Kingdom//UNITEDKINPROPRICHA";
                /// <summary>
                /// Productivity
                /// </summary>
                public const string Productivity = "United-Kingdom//UNITEDKINPRO";
                /// <summary>
                /// Retail Price Index
                /// </summary>
                public const string RetailPriceIndex = "United-Kingdom//UNITEDKINRETPRIIND";
                /// <summary>
                /// Retail Sales Ex Fuel
                /// </summary>
                public const string RetailSalesExFuel = "United-Kingdom//UNITEDKINRSEF";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "United-Kingdom//GBRRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "United-Kingdom//GBRRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "United-Kingdom//UNITEDKINSERPMI";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "United-Kingdom//UNITEDKIN3YBY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "United-Kingdom//UKUEILOR";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "United-Kingdom//UNITEDKINWAGGRO";
            }
            /// <summary>
            /// United States
            /// </summary>
            public static class UnitedStates
            {
                /// <summary>
                /// Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "United-States//UNITEDSTAADPEMPCHA";
                /// <summary>
                /// API Crude Oil Stock Change
                /// </summary>
                public const string APICrudeOilStockChange = "United-States//APICRUDEOIL";
                /// <summary>
                /// Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "United-States//UNITEDSTAAVEHOUEAR";
                /// <summary>
                /// Average Weekly Hours
                /// </summary>
                public const string AverageWeeklyHours = "United-States//UNITEDSTAAVEWEEHOU";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "United-States//USTBTOT";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "United-States//UNITEDSTABUIPER";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "United-States//NAPMPMI";
                /// <summary>
                /// Business Inventories
                /// </summary>
                public const string BusinessInventories = "United-States//UNITEDSTABUSINV";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "United-States//USD-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "United-States//UNITEDSTACAPUTI";
                /// <summary>
                /// Capital Flows
                /// </summary>
                public const string CapitalFlows = "United-States//UNITEDSTACAPFLO";
                /// <summary>
                /// Case Shiller Home Price Index
                /// </summary>
                public const string CaseShillerHomePriceIndex = "United-States//UNITEDSTACASSHIHOMPR";
                /// <summary>
                /// Chain Store Sales
                /// </summary>
                public const string ChainStoreSales = "United-States//UNITEDSTACHASTOSAL";
                /// <summary>
                /// Challenger Job Cuts
                /// </summary>
                public const string ChallengerJobCuts = "United-States//UNITEDSTACHAJOBCUT";
                /// <summary>
                /// Chicago Fed National Activity Index
                /// </summary>
                public const string ChicagoFedNationalActivityIndex = "United-States//UNITEDSTACHIFEDNATAC";
                /// <summary>
                /// Chicago Pmi
                /// </summary>
                public const string ChicagoPmi = "United-States//UNITEDSTACHIPMI";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "United-States//UNITEDSTACOMPMI";
                /// <summary>
                /// Construction Spending
                /// </summary>
                public const string ConstructionSpending = "United-States//UNITEDSTACONTSPE";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "United-States//CONCCONF";
                /// <summary>
                /// Consumer Credit
                /// </summary>
                public const string ConsumerCredit = "United-States//UNITEDSTACONCRE";
                /// <summary>
                /// Consumer Price Index (CPI)
                /// </summary>
                public const string ConsumerPriceIndexCPI = "United-States//UNITEDSTACONPRIINDCP";
                /// <summary>
                /// Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "United-States//UNITEDSTACONSPE";
                /// <summary>
                /// Continuing Jobless Claims
                /// </summary>
                public const string ContinuingJoblessClaims = "United-States//UNITEDSTACONJOBCLA";
                /// <summary>
                /// Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "United-States//UNITEDSTACORCONPRI";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "United-States//USACORECPIRATE";
                /// <summary>
                /// Core Pce Price Index
                /// </summary>
                public const string CorePcePriceIndex = "United-States//UNITEDSTACORPCEPRIIN";
                /// <summary>
                /// Core Producer Prices
                /// </summary>
                public const string CoreProducerPrices = "United-States//UNITEDSTACORPROPRI";
                /// <summary>
                /// Corporate Profits
                /// </summary>
                public const string CorporateProfits = "United-States//UNITEDSTACORPRO";
                /// <summary>
                /// Crude Oil Imports
                /// </summary>
                public const string CrudeOilImports = "United-States//UNITEDSTACRUOILIMP";
                /// <summary>
                /// Crude Oil Rigs
                /// </summary>
                public const string CrudeOilRigs = "United-States//USOILRIGS";
                /// <summary>
                /// Crude Oil Stocks Change
                /// </summary>
                public const string CrudeOilStocksChange = "United-States//UNITEDSTACRUOILSTOCH";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "United-States//USCABAL";
                /// <summary>
                /// Cushing Crude Oil Stocks
                /// </summary>
                public const string CushingCrudeOilStocks = "United-States//UNITEDSTACCOS";
                /// <summary>
                /// Dallas Fed Manufacturing Index
                /// </summary>
                public const string DallasFedManufacturingIndex = "United-States//UNITEDSTADALFEDMANIN";
                /// <summary>
                /// Distillate Fuel Production
                /// </summary>
                public const string DistillateFuelProduction = "United-States//UNITEDSTADISFUEPRO";
                /// <summary>
                /// Distillate Stocks
                /// </summary>
                public const string DistillateStocks = "United-States//UNITEDSTADISSTO";
                /// <summary>
                /// Durable Goods Orders
                /// </summary>
                public const string DurableGoodsOrders = "United-States//UNITEDSTADURGOOORD";
                /// <summary>
                /// Durable Goods Orders Ex Defense
                /// </summary>
                public const string DurableGoodsOrdersExDefense = "United-States//UNITEDSTADURGOOORDED";
                /// <summary>
                /// Durable Goods Orders Ex Transportation
                /// </summary>
                public const string DurableGoodsOrdersExTransportation = "United-States//UNITEDSTADURGOOORDEX";
                /// <summary>
                /// Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "United-States//UNITEDSTAECOOPTIND";
                /// <summary>
                /// Employment Cost Index
                /// </summary>
                public const string EmploymentCostIndex = "United-States//UNITEDSTAEMPCOSIND";
                /// <summary>
                /// Existing Home Sales
                /// </summary>
                public const string ExistingHomeSales = "United-States//UNITEDSTAEXIHOMSAL";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "United-States//UNITEDSTAEXPPRI";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "United-States//TBEXTOT";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "United-States//UNITEDSTAFACORD";
                /// <summary>
                /// Factory Orders Ex Transportation
                /// </summary>
                public const string FactoryOrdersExTransportation = "United-States//UNITEDSTAFACORDEXTRA";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "United-States//UNITEDSTA52WEEBILYIE";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "United-States//UNITEDSTA5YEANOTYIE";
                /// <summary>
                /// Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "United-States//UNITEDSTAFORBONINV";
                /// <summary>
                /// 4 Week Bill Yield
                /// </summary>
                public const string FourWeekBillYield = "United-States//UNITEDSTA4WEEBILYIE";
                /// <summary>
                /// Gasoline Production
                /// </summary>
                public const string GasolineProduction = "United-States//UNITEDSTAGASPRO";
                /// <summary>
                /// Gasoline Stocks Change
                /// </summary>
                public const string GasolineStocksChange = "United-States//UNITEDSTAGASSTOCHA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "United-States//GDP-CYOY";
                /// <summary>
                /// Gdp Deflator
                /// </summary>
                public const string GdpDeflator = "United-States//UNITEDSTAGDPDEF";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "United-States//GDP-CQOQ";
                /// <summary>
                /// Goods Trade Balance
                /// </summary>
                public const string GoodsTradeBalance = "United-States//UNITEDSTAGOOTRABAL";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "United-States//USGG10YR";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "United-States//UNITEDSTAGOVBUDVAL";
                /// <summary>
                /// Government Payrolls
                /// </summary>
                public const string GovernmentPayrolls = "United-States//UNITEDSTAGOVPAY";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "United-States//HOLIDAYSUNITED-STAT";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "United-States//UNITEDSTAHOUIND";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "United-States//UNITEDSTAHOUSTA";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "United-States//UNITEDSTAIMPPRI";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "United-States//TBIMTOT";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "United-States//IP-YOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "United-States//UNITEDSTAINDPROMOM";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "United-States//UNITEDSTAINFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "United-States//CPI-YOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "United-States//UNITEDSTAINFRATMOM";
                /// <summary>
                /// Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "United-States//IJCUSA";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "United-States//FDTR";
                /// <summary>
                /// Ism New York Index
                /// </summary>
                public const string IsmNewYorkIndex = "United-States//UNITEDSTAISMNEWYORIN";
                /// <summary>
                /// Job Offers
                /// </summary>
                public const string JobOffers = "United-States//UNITEDSTAJOBOFF";
                /// <summary>
                /// Kansas Fed Manufacturing Index
                /// </summary>
                public const string KansasFedManufacturingIndex = "United-States//UNITEDSTAKFMI";
                /// <summary>
                /// Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "United-States//UNITEDSTALABFORPARRA";
                /// <summary>
                /// Labor Market Conditions Index
                /// </summary>
                public const string LaborMarketConditionsIndex = "United-States//UNITEDSTALMCI";
                /// <summary>
                /// Labour Costs
                /// </summary>
                public const string LabourCosts = "United-States//UNITEDSTALABCOS";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "United-States//UNITEDSTALEAECOIND";
                /// <summary>
                /// Manufacturing Payrolls
                /// </summary>
                public const string ManufacturingPayrolls = "United-States//UNITEDSTAMANPAY";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "United-States//UNITEDSTAMANPMI";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "United-States//UNITEDSTAMANPRO";
                /// <summary>
                /// Mortgage Applications
                /// </summary>
                public const string MortgageApplications = "United-States//UNITEDSTAMORAPP";
                /// <summary>
                /// Mortgage Rate
                /// </summary>
                public const string MortgageRate = "United-States//UNITEDSTAMORRAT";
                /// <summary>
                /// Nahb Housing Market Index
                /// </summary>
                public const string NahbHousingMarketIndex = "United-States//UNITEDSTANAHHOUMARIN";
                /// <summary>
                /// Natural Gas Stocks Change
                /// </summary>
                public const string NaturalGasStocksChange = "United-States//UNITEDSTANATGASSTOCH";
                /// <summary>
                /// Net Long-term Tic Flows
                /// </summary>
                public const string NetLongTermTicFlows = "United-States//UNITEDSTANETLONTICFL";
                /// <summary>
                /// New Home Sales
                /// </summary>
                public const string NewHomeSales = "United-States//UNITEDSTANEWHOMSAL";
                /// <summary>
                /// Nfib Business Optimism Index
                /// </summary>
                public const string NfibBusinessOptimismIndex = "United-States//UNITEDSTANFIBUSOPTIN";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "United-States//NFP-TCH";
                /// <summary>
                /// Nonfarm Payrolls - Private
                /// </summary>
                public const string NonfarmPayrollsPrivate = "United-States//UNITEDSTANONPAY-PRI";
                /// <summary>
                /// Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "United-States//UNITEDSTANONMANPMI";
                /// <summary>
                /// Ny Empire State Manufacturing Index
                /// </summary>
                public const string NyEmpireStateManufacturingIndex = "United-States//UNITEDSTANYEMPSTAMAN";
                /// <summary>
                /// Pce Price Index
                /// </summary>
                public const string PcePriceIndex = "United-States//UNITEDSTAPCEPRIIND";
                /// <summary>
                /// Pending Home Sales
                /// </summary>
                public const string PendingHomeSales = "United-States//UNITEDSTAPENHOMSAL";
                /// <summary>
                /// Personal Income
                /// </summary>
                public const string PersonalIncome = "United-States//UNITEDSTAPERINC";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "United-States//UNITEDSTAPERSPE";
                /// <summary>
                /// Philadelphia Fed Manufacturing Index
                /// </summary>
                public const string PhiladelphiaFedManufacturingIndex = "United-States//UNITEDSTAPHIFEDMANIN";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "United-States//UNITEDSTAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "United-States//UNITEDSTAPROPRICHA";
                /// <summary>
                /// Productivity
                /// </summary>
                public const string Productivity = "United-States//UNITEDSTAPRO";
                /// <summary>
                /// Redbook Index
                /// </summary>
                public const string RedbookIndex = "United-States//UNITEDSTAREDIND";
                /// <summary>
                /// Refinery Crude Runs
                /// </summary>
                public const string RefineryCrudeRuns = "United-States//UNITEDSTAREFCRURUN";
                /// <summary>
                /// Retail Sales Ex Autos
                /// </summary>
                public const string RetailSalesExAutos = "United-States//UNITEDSTARETSALEXAUT";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "United-States//RSTAMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "United-States//USARETAILSALESYOY";
                /// <summary>
                /// Richmond Fed Manufacturing Index
                /// </summary>
                public const string RichmondFedManufacturingIndex = "United-States//UNITEDSTARICFEDMANIN";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "United-States//UNITEDSTASERPMI";
                /// <summary>
                /// 7 Year Note Yield
                /// </summary>
                public const string SevenYearNoteYield = "United-States//UNITEDSTA7YEANOTYIE";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "United-States//UNITEDSTA6MONBILYIE";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "United-States//UNITEDSTA30YEABONYIE";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "United-States//UNITEDSTA3MONBILYIE";
                /// <summary>
                /// 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "United-States//UNITEDSTA3YEANOTYIE";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "United-States//UNITEDSTATOTVEHSAL";
                /// <summary>
                /// 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "United-States//UNITEDSTA2YEANOTYIE";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "United-States//USURTOT";
                /// <summary>
                /// Wholesale Inventories
                /// </summary>
                public const string WholesaleInventories = "United-States//UNITEDSTAWHOINV";
            }
        }
    }
}
