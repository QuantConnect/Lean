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
                public const string BalanceOfTrade = "AUSTRALIA" + Delimiter + "AUITGSB";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "AUSTRALIA" + Delimiter + "AUSTRALIABUIPER";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "AUSTRALIA" + Delimiter + "NABSCONF";
                /// <summary>
                /// Business Inventories
                /// </summary>
                public const string BusinessInventories = "AUSTRALIA" + Delimiter + "AUSTRALIABUSINV";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "AUSTRALIA" + Delimiter + "AUD-CALENDAR";
                /// <summary>
                /// Car Registrations
                /// </summary>
                public const string CarRegistrations = "AUSTRALIA" + Delimiter + "AUSTRALIACARREG";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "AUSTRALIA" + Delimiter + "AUSTRALIACOMPMI";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "AUSTRALIA" + Delimiter + "AUSTRALIACONOUT";
                /// <summary>
                /// Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "AUSTRALIA" + Delimiter + "AUSTRALIACONPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "AUSTRALIA" + Delimiter + "WMCCCONPCT";
                /// <summary>
                /// Consumer Price Index (CPI)
                /// </summary>
                public const string ConsumerPriceIndexCPI = "AUSTRALIA" + Delimiter + "AUSTRALIACONPRIINDCP";
                /// <summary>
                /// Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "AUSTRALIA" + Delimiter + "AUSTRALIACONSPE";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "AUSTRALIA" + Delimiter + "AUSCORECPIRATE";
                /// <summary>
                /// Corporate Profits
                /// </summary>
                public const string CorporateProfits = "AUSTRALIA" + Delimiter + "AUSTRALIACORPRO";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "AUSTRALIA" + Delimiter + "AUCABAL";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "AUSTRALIA" + Delimiter + "AUSTRALIAEMPCHA";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "AUSTRALIA" + Delimiter + "AUSTRALIAEXPPRI";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "AUSTRALIA" + Delimiter + "AUITEXP";
                /// <summary>
                /// Full Time Employment
                /// </summary>
                public const string FullTimeEmployment = "AUSTRALIA" + Delimiter + "AUSTRALIAFULTIMEMP";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "AUSTRALIA" + Delimiter + "AUNAGDPY";
                /// <summary>
                /// GDP Deflator
                /// </summary>
                public const string GDPDeflator = "AUSTRALIA" + Delimiter + "AUSTRALIAGDPDEF";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "AUSTRALIA" + Delimiter + "AUNAGDPC";
                /// <summary>
                /// Gross Fixed Capital Formation
                /// </summary>
                public const string GrossFixedCapitalFormation = "AUSTRALIA" + Delimiter + "AUSTRALIAGROFIXCAPFO";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "AUSTRALIA" + Delimiter + "HOLIDAYSAUSTRALIA";
                /// <summary>
                /// Home Loans
                /// </summary>
                public const string HomeLoans = "AUSTRALIA" + Delimiter + "AUSTRALIAHOMLOA";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "AUSTRALIA" + Delimiter + "AUSTRALIAHOUIND";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "AUSTRALIA" + Delimiter + "AUSTRALIAIMPPRI";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "AUSTRALIA" + Delimiter + "AUITIMP";
                /// <summary>
                /// Industrial Sentiment
                /// </summary>
                public const string IndustrialSentiment = "AUSTRALIA" + Delimiter + "AUSTRALIAINDSEN";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "AUSTRALIA" + Delimiter + "AUSTRALIAINFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "AUSTRALIA" + Delimiter + "AUCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "AUSTRALIA" + Delimiter + "AUSTRALIAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "AUSTRALIA" + Delimiter + "RBATCTR";
                /// <summary>
                /// Job Advertisements
                /// </summary>
                public const string JobAdvertisements = "AUSTRALIA" + Delimiter + "AUSTRALIAJOBADV";
                /// <summary>
                /// Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "AUSTRALIA" + Delimiter + "AUSTRALIALABFORPARRA";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "AUSTRALIA" + Delimiter + "AUSTRALIALEAECOIND";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "AUSTRALIA" + Delimiter + "AUSTRALIAMANPMI";
                /// <summary>
                /// New Home Sales
                /// </summary>
                public const string NewHomeSales = "AUSTRALIA" + Delimiter + "AUSTRALIANEWHOMSAL";
                /// <summary>
                /// Part Time Employment
                /// </summary>
                public const string PartTimeEmployment = "AUSTRALIA" + Delimiter + "AUSTRALIAPARTIMEMP";
                /// <summary>
                /// Private Investment
                /// </summary>
                public const string PrivateInvestment = "AUSTRALIA" + Delimiter + "AUSTRALIAPRIINV";
                /// <summary>
                /// Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "AUSTRALIA" + Delimiter + "AUSTRALIAPRISECCRE";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "AUSTRALIA" + Delimiter + "AUSTRALIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "AUSTRALIA" + Delimiter + "AUSTRALIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "AUSTRALIA" + Delimiter + "AUSRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "AUSTRALIA" + Delimiter + "AUSRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "AUSTRALIA" + Delimiter + "AUSTRALIASERPMI";
                /// <summary>
                /// Services Sentiment
                /// </summary>
                public const string ServicesSentiment = "AUSTRALIA" + Delimiter + "AUSTRALIASERSEN";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "AUSTRALIA" + Delimiter + "AUSTRALIATOTVEHSAL";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "AUSTRALIA" + Delimiter + "AULFUNEM";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "AUSTRALIA" + Delimiter + "AUSTRALIAWAGGRO";
            }
            /// <summary>
            /// Austria
            /// </summary>
            public static class Austria
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "AUSTRIA" + Delimiter + "ATTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "AUSTRIA" + Delimiter + "AUSTRIABUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "AUSTRIA" + Delimiter + "AUT-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "AUSTRIA" + Delimiter + "AUSTRIACC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "AUSTRIA" + Delimiter + "OEATB007";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "AUSTRIA" + Delimiter + "ASGPGDPY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "AUSTRIA" + Delimiter + "ASGPGDPQ";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "AUSTRIA" + Delimiter + "AUSTRIAHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "AUSTRIA" + Delimiter + "HOLIDAYSAUSTRIA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "AUSTRIA" + Delimiter + "ATIPIYY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "AUSTRIA" + Delimiter + "ECCPATYY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "AUSTRIA" + Delimiter + "AUSTRIAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "AUSTRIA" + Delimiter + "EURR002W-ATS";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "AUSTRIA" + Delimiter + "AUSTRIAMANPMI";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "AUSTRIA" + Delimiter + "AUSTRIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "AUSTRIA" + Delimiter + "AUSTRIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "AUSTRIA" + Delimiter + "AUTRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "AUSTRIA" + Delimiter + "AUTRETSALYOY";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "AUSTRIA" + Delimiter + "AUSTRIAUNEPER";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "AUSTRIA" + Delimiter + "UMRTAT";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "AUSTRIA" + Delimiter + "AUSTRIAWHOPRI";
            }
            /// <summary>
            /// Belgium
            /// </summary>
            public static class Belgium
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BELGIUM" + Delimiter + "BETBTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BELGIUM" + Delimiter + "BEBCI";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "BELGIUM" + Delimiter + "BEL-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BELGIUM" + Delimiter + "BECCN";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "BELGIUM" + Delimiter + "OEBEB026";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "BELGIUM" + Delimiter + "BEGDPYS";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "BELGIUM" + Delimiter + "BEGDPQS";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "BELGIUM" + Delimiter + "HOLIDAYSBELGIUM";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BELGIUM" + Delimiter + "BEPDRYY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "BELGIUM" + Delimiter + "BELGIUMINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "BELGIUM" + Delimiter + "BECPYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "BELGIUM" + Delimiter + "BELGIUMINFRATMOM";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "BELGIUM" + Delimiter + "BELGIUMPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BELGIUM" + Delimiter + "BELRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BELGIUM" + Delimiter + "BELRETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "BELGIUM" + Delimiter + "BEUER";
            }
            /// <summary>
            /// Canada
            /// </summary>
            public static class Canada
            {
                /// <summary>
                /// Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "CANADA" + Delimiter + "CANADAADPEMPCHA";
                /// <summary>
                /// Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "CANADA" + Delimiter + "CANADAAVEHOUEAR";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CANADA" + Delimiter + "CATBTOTB";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "CANADA" + Delimiter + "CANADABUIPER";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CANADA" + Delimiter + "IVEY";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "CANADA" + Delimiter + "CAD-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "CANADA" + Delimiter + "CANADACAPUTI";
                /// <summary>
                /// Car Registrations
                /// </summary>
                public const string CarRegistrations = "CANADA" + Delimiter + "CANADACARREG";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "CANADA" + Delimiter + "CANCORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "CANADA" + Delimiter + "CACURENT";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "CANADA" + Delimiter + "CANADAEMPCHA";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "CANADA" + Delimiter + "CATBTOTE";
                /// <summary>
                /// Foreign Stock Investment
                /// </summary>
                public const string ForeignStockInvestment = "CANADA" + Delimiter + "CANADAFORSTOINV";
                /// <summary>
                /// Full Time Employment
                /// </summary>
                public const string FullTimeEmployment = "CANADA" + Delimiter + "CANADAFULTIMEMP";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "CANADA" + Delimiter + "CGE9YOY";
                /// <summary>
                /// GDP Deflator
                /// </summary>
                public const string GDPDeflator = "CANADA" + Delimiter + "CANADAGDPDEF";
                /// <summary>
                /// Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "CANADA" + Delimiter + "CANADAGDPGROANN";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "CANADA" + Delimiter + "CGE9QOQ";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "CANADA" + Delimiter + "CANADAGOVBUDVAL";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "CANADA" + Delimiter + "HOLIDAYSCANADA";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "CANADA" + Delimiter + "CANADAHOUIND";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "CANADA" + Delimiter + "CANADAHOUSTA";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "CANADA" + Delimiter + "CATBTOTI";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CANADA" + Delimiter + "CACPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CANADA" + Delimiter + "CANADAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "CANADA" + Delimiter + "CCLR";
                /// <summary>
                /// Job Vacancies
                /// </summary>
                public const string JobVacancies = "CANADA" + Delimiter + "CANADAJOBVAC";
                /// <summary>
                /// Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "CANADA" + Delimiter + "CANADALABFORPARRAT";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "CANADA" + Delimiter + "CANADALEAECOIND";
                /// <summary>
                /// Manufacturing PMI
                /// </summary>
                public const string ManufacturingPMI = "CANADA" + Delimiter + "CANADAMANPMI";
                /// <summary>
                /// Manufacturing Sales
                /// </summary>
                public const string ManufacturingSales = "CANADA" + Delimiter + "CANADAMANSAL";
                /// <summary>
                /// Part Time Employment
                /// </summary>
                public const string PartTimeEmployment = "CANADA" + Delimiter + "CANADAPARTIMEMP";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "CANADA" + Delimiter + "CANADAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CANADA" + Delimiter + "CANADAPROPRICHA";
                /// <summary>
                /// Productivity
                /// </summary>
                public const string Productivity = "CANADA" + Delimiter + "CANADAPRO";
                /// <summary>
                /// Retail Sales Ex Autos
                /// </summary>
                public const string RetailSalesExAutos = "CANADA" + Delimiter + "CANADARETSALEXAUT";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CANADA" + Delimiter + "CANRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CANADA" + Delimiter + "CANNADARETSYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CANADA" + Delimiter + "CANLXEMR";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "CANADA" + Delimiter + "CANADAWAGGRO";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "CANADA" + Delimiter + "CANADAWHOPRI";
                /// <summary>
                /// Wholesale Sales
                /// </summary>
                public const string WholesaleSales = "CANADA" + Delimiter + "CANADAWHOSAL";
            }
            /// <summary>
            /// China
            /// </summary>
            public static class China
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CHINA" + Delimiter + "CNFRBALD";
                /// <summary>
                /// Banks Balance Sheet
                /// </summary>
                public const string BanksBalanceSheet = "CHINA" + Delimiter + "CHINABANBALSHE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CHINA" + Delimiter + "CHBUBCIN";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "CHINA" + Delimiter + "CHN-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "CHINA" + Delimiter + "CHINACAPUTI";
                /// <summary>
                /// Capital Flows
                /// </summary>
                public const string CapitalFlows = "CHINA" + Delimiter + "CHINACAPFLO";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "CHINA" + Delimiter + "CHINACOMPMI";
                /// <summary>
                /// Corporate Profits
                /// </summary>
                public const string CorporateProfits = "CHINA" + Delimiter + "CHINACORPRO";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "CHINA" + Delimiter + "CHCURENT";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "CHINA" + Delimiter + "CNFREXPD";
                /// <summary>
                /// Fixed Asset Investment
                /// </summary>
                public const string FixedAssetInvestment = "CHINA" + Delimiter + "CHINAFIXASSINV";
                /// <summary>
                /// Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "CHINA" + Delimiter + "CHINAFORDIRINV";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "CHINA" + Delimiter + "CHINAFOREXCRES";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "CHINA" + Delimiter + "CNGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "CHINA" + Delimiter + "CNGDPQOQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "CHINA" + Delimiter + "HOLIDAYSCHINA";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "CHINA" + Delimiter + "CHINAHOUIND";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "CHINA" + Delimiter + "CNFRIMPD";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CHINA" + Delimiter + "CHVAIOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CHINA" + Delimiter + "CNCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CHINA" + Delimiter + "CHINAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "CHINA" + Delimiter + "CHLR12M";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "CHINA" + Delimiter + "CHINALOAGRO";
                /// <summary>
                /// Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "CHINA" + Delimiter + "CHINALOATOPRISEC";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "CHINA" + Delimiter + "CHINAMANPMI";
                /// <summary>
                /// Mni Business Sentiment
                /// </summary>
                public const string MniBusinessSentiment = "CHINA" + Delimiter + "CHINAMNIBUSSEN";
                /// <summary>
                /// Mni Consumer Sentiment
                /// </summary>
                public const string MniConsumerSentiment = "CHINA" + Delimiter + "CHINAMNICONSEN";
                /// <summary>
                /// Money Supply M2
                /// </summary>
                public const string MoneySupplyMTwo = "CHINA" + Delimiter + "CHINAMONSUPM2";
                /// <summary>
                /// Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "CHINA" + Delimiter + "CHINANONMANPMI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CHINA" + Delimiter + "CHINAPROPRICHA";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CHINA" + Delimiter + "CHNRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "CHINA" + Delimiter + "CHINASERPMI";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "CHINA" + Delimiter + "CHINATOTVEHSAL";
            }
            /// <summary>
            /// Cyprus
            /// </summary>
            public static class Cyprus
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CYPRUS" + Delimiter + "CYPPRUSBALRADE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CYPRUS" + Delimiter + "CYPRUSBUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "CYPRUS" + Delimiter + "CYP-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "CYPRUS" + Delimiter + "CYPRUSCONOUT";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CYPRUS" + Delimiter + "CYPRUSCONCON";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "CYPRUS" + Delimiter + "CYPPRUSCURCOUNT";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "CYPRUS" + Delimiter + "CYPPRUSGDPATE";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "CYPRUS" + Delimiter + "CYPPRUSGDPRATE";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "CYPRUS" + Delimiter + "CYPRUSHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "CYPRUS" + Delimiter + "HOLIDAYSCYPRUS";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CYPRUS" + Delimiter + "CYPPRUSINDCTION";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "CYPRUS" + Delimiter + "CYPPRUSINFNRATE";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CYPRUS" + Delimiter + "CYPPRUSRETSYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CYPRUS" + Delimiter + "CYPPRUSUNETRATE";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "CYPRUS" + Delimiter + "CYPRUSWAGGRO";
            }
            /// <summary>
            /// Estonia
            /// </summary>
            public static class Estonia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ESTONIA" + Delimiter + "ESTONIABT";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ESTONIA" + Delimiter + "ESTONIABUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "ESTONIA" + Delimiter + "EST-CALENDAR";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "ESTONIA" + Delimiter + "ESTONIACA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "ESTONIA" + Delimiter + "ESTONIAGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "ESTONIA" + Delimiter + "ESTONIAGDPQOQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "ESTONIA" + Delimiter + "HOLIDAYSESTONIA";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "ESTONIA" + Delimiter + "ESTONIAIM";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ESTONIA" + Delimiter + "ESTONIAIP";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ESTONIA" + Delimiter + "ESTONIAINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ESTONIA" + Delimiter + "ESTONIAIR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ESTONIA" + Delimiter + "ESTONIAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "ESTONIA" + Delimiter + "EURR002W-EEK";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "ESTONIA" + Delimiter + "ESTONIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ESTONIA" + Delimiter + "ESTONIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ESTONIA" + Delimiter + "ESTRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ESTONIA" + Delimiter + "ESTRETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ESTONIA" + Delimiter + "ESTONIAUR";
            }
            /// <summary>
            /// Finland
            /// </summary>
            public static class Finland
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "FINLAND" + Delimiter + "FITBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "FINLAND" + Delimiter + "FINLANDBUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "FINLAND" + Delimiter + "EUR-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "FINLAND" + Delimiter + "FICCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "FINLAND" + Delimiter + "FICAEUR";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "FINLAND" + Delimiter + "FINLANDEXPPRI";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "FINLAND" + Delimiter + "FIGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "FINLAND" + Delimiter + "FIGDPCH";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "FINLAND" + Delimiter + "HOLIDAYSFINLAND";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "FINLAND" + Delimiter + "FINLANDIMPPRI";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "FINLAND" + Delimiter + "FIIPYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "FINLAND" + Delimiter + "FICP2YOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "FINLAND" + Delimiter + "FINLANDINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "FINLAND" + Delimiter + "EURR002W-FIM";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "FINLAND" + Delimiter + "FINLANDLEAECOIND";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "FINLAND" + Delimiter + "FINLANDPROPRICHA";
                /// <summary>
                /// Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "FINLAND" + Delimiter + "FINLANDRETSALMOM";
                /// <summary>
                /// Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "FINLAND" + Delimiter + "FINLANDRETSALYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "FINLAND" + Delimiter + "UMRTFI";
            }
            /// <summary>
            /// France
            /// </summary>
            public static class France
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "FRANCE" + Delimiter + "FRTEBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "FRANCE" + Delimiter + "INSESYNT";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "FRANCE" + Delimiter + "EUR-CALENDAR";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "FRANCE" + Delimiter + "FRANCECOMPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "FRANCE" + Delimiter + "FRCCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "FRANCE" + Delimiter + "FRCAEURO";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "FRANCE" + Delimiter + "FRANCE5WBY";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "FRANCE" + Delimiter + "FRANCE5YNY";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "FRANCE" + Delimiter + "FRGEGDPY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "FRANCE" + Delimiter + "FRGEGDPQ";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "FRANCE" + Delimiter + "GFRN10";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "FRANCE" + Delimiter + "FRANCEGOVBUDVAL";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "FRANCE" + Delimiter + "FRANCEHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "FRANCE" + Delimiter + "HOLIDAYSFRANCE";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "FRANCE" + Delimiter + "FPIPYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "FRANCE" + Delimiter + "FRANCEINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "FRANCE" + Delimiter + "FRCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "FRANCE" + Delimiter + "FRANCEINFRATMOM";
                /// <summary>
                /// Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "FRANCE" + Delimiter + "FRANCEINIJOBCLA";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "FRANCE" + Delimiter + "FRANCEMANPMI";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "FRANCE" + Delimiter + "FRANCENONFARPAY";
                /// <summary>
                /// Nonfarm Payrolls Private
                /// </summary>
                public const string NonfarmPayrollsPrivate = "FRANCE" + Delimiter + "FRANCENONPAYPRI";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "FRANCE" + Delimiter + "FRANCEPERSPE";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "FRANCE" + Delimiter + "FRANCEPROPRI";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "FRANCE" + Delimiter + "FRARETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "FRANCE" + Delimiter + "FRARETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "FRANCE" + Delimiter + "FRANCESERPMI";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "FRANCE" + Delimiter + "FRANCE6MONBILYIE";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "FRANCE" + Delimiter + "FRANCE3MONBILYIE";
                /// <summary>
                /// 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "FRANCE" + Delimiter + "FRANCE3YNY";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "FRANCE" + Delimiter + "FRANCEUNEPER";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "FRANCE" + Delimiter + "UMRTFR";
            }
            /// <summary>
            /// Germany
            /// </summary>
            public static class Germany
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "GERMANY" + Delimiter + "GRTBALE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "GERMANY" + Delimiter + "GRIFPBUS";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "GERMANY" + Delimiter + "DEU-CALENDAR";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "GERMANY" + Delimiter + "GERMANYCOMPMI";
                /// <summary>
                /// Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "GERMANY" + Delimiter + "GERMANYCONPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GERMANY" + Delimiter + "GRCCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "GERMANY" + Delimiter + "GRCAEU";
                /// <summary>
                /// Employed Persons
                /// </summary>
                public const string EmployedPersons = "GERMANY" + Delimiter + "GERMANYEMPPER";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "GERMANY" + Delimiter + "GRTBEXE";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "GERMANY" + Delimiter + "GERMANYFACORD";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "GERMANY" + Delimiter + "GERMANY5WBY";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "GERMANY" + Delimiter + "GERMANY5YNY";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "GERMANY" + Delimiter + "GRGDPPGY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "GERMANY" + Delimiter + "GRGDPPGQ";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "GERMANY" + Delimiter + "GDBR10";
                /// <summary>
                /// Government Budget
                /// </summary>
                public const string GovernmentBudget = "GERMANY" + Delimiter + "WCSDDEU";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "GERMANY" + Delimiter + "GERMANYHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "GERMANY" + Delimiter + "HOLIDAYSGERMANY";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "GERMANY" + Delimiter + "GERMANYIMPPRI";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GERMANY" + Delimiter + "GRIPIYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "GERMANY" + Delimiter + "GERMANYINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "GERMANY" + Delimiter + "GRBC20YY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "GERMANY" + Delimiter + "GERMANYINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "GERMANY" + Delimiter + "EURR002W-DEM";
                /// <summary>
                /// Job Vacancies
                /// </summary>
                public const string JobVacancies = "GERMANY" + Delimiter + "GERMANYJOBVAC";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "GERMANY" + Delimiter + "GERMANYMANPMI";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "GERMANY" + Delimiter + "GERMANYPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GERMANY" + Delimiter + "GERMANYPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "GERMANY" + Delimiter + "DEURETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "GERMANY" + Delimiter + "DEURETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "GERMANY" + Delimiter + "GERMANYSERPMI";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "GERMANY" + Delimiter + "GERMANY3YBY";
                /// <summary>
                /// 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "GERMANY" + Delimiter + "GERMANY2YNY";
                /// <summary>
                /// Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "GERMANY" + Delimiter + "GERMANYUNEPER";
                /// <summary>
                /// Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "GERMANY" + Delimiter + "GERMANYUNECHA";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "GERMANY" + Delimiter + "GRUEPR";
                /// <summary>
                /// Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "GERMANY" + Delimiter + "GERMANYWHOPRI";
                /// <summary>
                /// Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "GERMANY" + Delimiter + "GERMANYZEWECOSENIND";
            }
            /// <summary>
            /// Greece
            /// </summary>
            public static class Greece
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "GREECE" + Delimiter + "GKTBALE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "GREECE" + Delimiter + "GREECEBC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "GREECE" + Delimiter + "EUR-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "GREECE" + Delimiter + "GREECECONOUT";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GREECE" + Delimiter + "GREECECC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "GREECE" + Delimiter + "GKCAEUR";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "GREECE" + Delimiter + "GKGNVYY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "GREECE" + Delimiter + "GKGNVQQ";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "GREECE" + Delimiter + "GREECEHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "GREECE" + Delimiter + "HOLIDAYSGREECE";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GREECE" + Delimiter + "GKIPIYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "GREECE" + Delimiter + "GKCPNEWY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "GREECE" + Delimiter + "GREECEINFRATMOM";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "GREECE" + Delimiter + "GREECELOAGRO";
                /// <summary>
                /// Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "GREECE" + Delimiter + "GREECELOATOPRISEC";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "GREECE" + Delimiter + "GREECEMANPMI";
                /// <summary>
                /// Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "GREECE" + Delimiter + "GREECEPRISECCRE";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GREECE" + Delimiter + "GREECEPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "GREECE" + Delimiter + "GRCRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "GREECE" + Delimiter + "GRCRETAILSALESYOY";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "GREECE" + Delimiter + "GREECE6MBY";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "GREECE" + Delimiter + "GREECE3MBY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "GREECE" + Delimiter + "UMRTGR";
            }
            /// <summary>
            /// Ireland
            /// </summary>
            public static class Ireland
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "IRELAND" + Delimiter + "IETBALNS";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "IRELAND" + Delimiter + "EUR-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "IRELAND" + Delimiter + "IRELANDCONOUT";
                /// <summary>
                /// Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "IRELAND" + Delimiter + "IRELANDCONPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "IRELAND" + Delimiter + "EUCCIE";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "IRELAND" + Delimiter + "IRLCORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "IRELAND" + Delimiter + "IECA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "IRELAND" + Delimiter + "IEGRPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "IRELAND" + Delimiter + "IEGRPQOQ";
                /// <summary>
                /// Gross National Product
                /// </summary>
                public const string GrossNationalProduct = "IRELAND" + Delimiter + "IRELANDGRONATPRO";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "IRELAND" + Delimiter + "IRELANDHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "IRELAND" + Delimiter + "HOLIDAYSIRELAND";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "IRELAND" + Delimiter + "IRELANDHOUIND";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "IRELAND" + Delimiter + "IEIPIYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "IRELAND" + Delimiter + "IECPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "IRELAND" + Delimiter + "IRELANDINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "IRELAND" + Delimiter + "EURR002W-IEP";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "IRELAND" + Delimiter + "IRELANDMANPMI";
                /// <summary>
                /// Personal Savings
                /// </summary>
                public const string PersonalSavings = "IRELAND" + Delimiter + "IRELANDPERSAV";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "IRELAND" + Delimiter + "IRELANDPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "IRELAND" + Delimiter + "IRELANDPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "IRELAND" + Delimiter + "IRLRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "IRELAND" + Delimiter + "IRLRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "IRELAND" + Delimiter + "IRELANDSERPMI";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "IRELAND" + Delimiter + "IEUERT";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "IRELAND" + Delimiter + "IRELANDWAGGRO";
            }
            /// <summary>
            /// Italy
            /// </summary>
            public static class Italy
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ITALY" + Delimiter + "ITTRBSAT";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ITALY" + Delimiter + "ITBCI";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "ITALY" + Delimiter + "EUR-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "ITALY" + Delimiter + "ITALYCONOUT";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ITALY" + Delimiter + "ITPSSA";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "ITALY" + Delimiter + "ITCAEUR";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "ITALY" + Delimiter + "ITALYFACORD";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "ITALY" + Delimiter + "ITALY5WBY";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "ITALY" + Delimiter + "ITALY5YNY";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "ITALY" + Delimiter + "ITPIRLYS";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "ITALY" + Delimiter + "ITPIRLQS";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "ITALY" + Delimiter + "GBTPGR10";
                /// <summary>
                /// Government Budget
                /// </summary>
                public const string GovernmentBudget = "ITALY" + Delimiter + "WCSDITA";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "ITALY" + Delimiter + "ITALYHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "ITALY" + Delimiter + "HOLIDAYSITALY";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ITALY" + Delimiter + "ITPRWAY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ITALY" + Delimiter + "ITALYINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "ITALY" + Delimiter + "ITCPNICY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ITALY" + Delimiter + "ITALYINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "ITALY" + Delimiter + "EURR002W-ITL";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "ITALY" + Delimiter + "ITALYMANPMI";
                /// <summary>
                /// Manufacturing Sales
                /// </summary>
                public const string ManufacturingSales = "ITALY" + Delimiter + "ITALYMANSAL";
                /// <summary>
                /// New Orders
                /// </summary>
                public const string NewOrders = "ITALY" + Delimiter + "ITALYNEWORD";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "ITALY" + Delimiter + "ITALYPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ITALY" + Delimiter + "ITALYPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ITALY" + Delimiter + "ITARETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ITALY" + Delimiter + "ITARETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "ITALY" + Delimiter + "ITALYSERPMI";
                /// <summary>
                /// 7 Year Note Yield
                /// </summary>
                public const string SevenYearNoteYield = "ITALY" + Delimiter + "ITALY7YNY";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "ITALY" + Delimiter + "ITALY6MBY";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "ITALY" + Delimiter + "ITALY3YBY";
                /// <summary>
                /// 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "ITALY" + Delimiter + "ITALY3YNY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ITALY" + Delimiter + "UMRTIT";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "ITALY" + Delimiter + "ITALYWAGGRO";
            }
            /// <summary>
            /// Japan
            /// </summary>
            public static class Japan
            {
                /// <summary>
                /// All Industry Activity Index
                /// </summary>
                public const string AllIndustryActivityIndex = "JAPAN" + Delimiter + "JAPANAIAI";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "JAPAN" + Delimiter + "JNTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "JAPAN" + Delimiter + "JNSBALLI";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "JAPAN" + Delimiter + "JPY-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "JAPAN" + Delimiter + "JAPANCAPUTI";
                /// <summary>
                /// Coincident Index
                /// </summary>
                public const string CoincidentIndex = "JAPAN" + Delimiter + "JAPANCOIIND";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "JAPAN" + Delimiter + "JAPANCOMPMI";
                /// <summary>
                /// Construction Orders
                /// </summary>
                public const string ConstructionOrders = "JAPAN" + Delimiter + "JAPANCONORD";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "JAPAN" + Delimiter + "JCOMACF";
                /// <summary>
                /// Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "JAPAN" + Delimiter + "JAPANCONSPE";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "JAPAN" + Delimiter + "JPNCORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "JAPAN" + Delimiter + "JNBPAB";
                /// <summary>
                /// Economy Watchers Survey
                /// </summary>
                public const string EconomyWatchersSurvey = "JAPAN" + Delimiter + "JAPANECOWATSUR";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "JAPAN" + Delimiter + "JNBPEXP";
                /// <summary>
                /// Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "JAPAN" + Delimiter + "JAPANFORBONINV";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "JAPAN" + Delimiter + "JAPANFOREXCRES";
                /// <summary>
                /// Foreign Stock Investment
                /// </summary>
                public const string ForeignStockInvestment = "JAPAN" + Delimiter + "JAPANFORSTOINV";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "JAPAN" + Delimiter + "JGDPNSAQ";
                /// <summary>
                /// GDP Deflator
                /// </summary>
                public const string GDPDeflator = "JAPAN" + Delimiter + "JAPANGDPDEF";
                /// <summary>
                /// Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "JAPAN" + Delimiter + "JAPANGDPGROANN";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "JAPAN" + Delimiter + "JGDPAGDP";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "JAPAN" + Delimiter + "GJGB10";
                /// <summary>
                /// Gross Fixed Capital Formation
                /// </summary>
                public const string GrossFixedCapitalFormation = "JAPAN" + Delimiter + "JAPANGROFIXCAPFOR";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "JAPAN" + Delimiter + "HOLIDAYSJAPAN";
                /// <summary>
                /// Household Spending
                /// </summary>
                public const string HouseholdSpending = "JAPAN" + Delimiter + "JAPANHOUSPE";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "JAPAN" + Delimiter + "JAPANHOUSTA";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "JAPAN" + Delimiter + "JNBPIMP";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "JAPAN" + Delimiter + "JNIPYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "JAPAN" + Delimiter + "JAPANINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "JAPAN" + Delimiter + "JNCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "JAPAN" + Delimiter + "JAPANINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "JAPAN" + Delimiter + "BOJDTR";
                /// <summary>
                /// Jobs To Applications Ratio
                /// </summary>
                public const string JobsToApplicationsRatio = "JAPAN" + Delimiter + "JAPANJTAR";
                /// <summary>
                /// Leading Composite Index
                /// </summary>
                public const string LeadingCompositeIndex = "JAPAN" + Delimiter + "JAPANLEACOMIND";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "JAPAN" + Delimiter + "JAPANLEACOMIND";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "JAPAN" + Delimiter + "JAPANLOAGRO";
                /// <summary>
                /// Machinery Orders
                /// </summary>
                public const string MachineryOrders = "JAPAN" + Delimiter + "JAPANMACORD";
                /// <summary>
                /// Machine Tool Orders
                /// </summary>
                public const string MachineToolOrders = "JAPAN" + Delimiter + "JAPANMACTOOORD";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "JAPAN" + Delimiter + "JAPANMANPMI";
                /// <summary>
                /// Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "JAPAN" + Delimiter + "JAPANNONMANPMI";
                /// <summary>
                /// Private Investment
                /// </summary>
                public const string PrivateInvestment = "JAPAN" + Delimiter + "JAPANPRIINV";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "JAPAN" + Delimiter + "JAPANPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "JAPAN" + Delimiter + "JAPANPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "JAPAN" + Delimiter + "JPNRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "JAPAN" + Delimiter + "JPNRETAILSALESYOY";
                /// <summary>
                /// Reuters Tankan Index
                /// </summary>
                public const string ReutersTankanIndex = "JAPAN" + Delimiter + "JAPANREUTANIND";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "JAPAN" + Delimiter + "JAPANSERPMI";
                /// <summary>
                /// Small Business Sentiment
                /// </summary>
                public const string SmallBusinessSentiment = "JAPAN" + Delimiter + "JAPANSMABUSSEN";
                /// <summary>
                /// Tertiary Industry Index
                /// </summary>
                public const string TertiaryIndustryIndex = "JAPAN" + Delimiter + "JAPANTERINDIND";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "JAPAN" + Delimiter + "JAPAN3YBY";
                /// <summary>
                /// Tokyo Cpi
                /// </summary>
                public const string TokyoCpi = "JAPAN" + Delimiter + "JAPANTOKCPI";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "JAPAN" + Delimiter + "JNUE";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "JAPAN" + Delimiter + "JAPANWAGGRO";
            }
            /// <summary>
            /// Latvia
            /// </summary>
            public static class Latvia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LATVIA" + Delimiter + "LATVIABT";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "LATVIA" + Delimiter + "LVA-CALENDAR";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "LATVIA" + Delimiter + "LATVIACA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "LATVIA" + Delimiter + "LATVIAGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "LATVIA" + Delimiter + "LATVIAGDPQOQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "LATVIA" + Delimiter + "HOLIDAYSLATVIA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LATVIA" + Delimiter + "LATVIAIP";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "LATVIA" + Delimiter + "LATVIAINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "LATVIA" + Delimiter + "LATVIAIR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "LATVIA" + Delimiter + "LATVIAINFRATMOM";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "LATVIA" + Delimiter + "LATVIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LATVIA" + Delimiter + "LATVIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "LATVIA" + Delimiter + "LVARETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "LATVIA" + Delimiter + "LVARETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LATVIA" + Delimiter + "LATVIAUR";
            }
            /// <summary>
            /// Lithuania
            /// </summary>
            public static class Lithuania
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LITHUANIA" + Delimiter + "LITHUANIABT";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "LITHUANIA" + Delimiter + "LITHUANIABC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "LITHUANIA" + Delimiter + "LTU-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "LITHUANIA" + Delimiter + "LITHUANIACC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "LITHUANIA" + Delimiter + "LITHUANIACA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "LITHUANIA" + Delimiter + "LITHUANIAGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "LITHUANIA" + Delimiter + "LITHUANIAGDPQOQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "LITHUANIA" + Delimiter + "HOLIDAYSLITHUANIA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LITHUANIA" + Delimiter + "LITHUANIAINDPRO";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "LITHUANIA" + Delimiter + "LITHUANIAINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "LITHUANIA" + Delimiter + "LITHUANIAIR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "LITHUANIA" + Delimiter + "LITHUANIAINFRATMOM";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "LITHUANIA" + Delimiter + "LITHUANIAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LITHUANIA" + Delimiter + "LITHUANIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "LITHUANIA" + Delimiter + "LTURETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "LITHUANIA" + Delimiter + "LTURETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LITHUANIA" + Delimiter + "LITHUANIAUR";
            }
            /// <summary>
            /// Luxembourg
            /// </summary>
            public static class Luxembourg
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LUXEMBOURG" + Delimiter + "LXTBBALL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "LUXEMBOURG" + Delimiter + "LUXEMBOURGBUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "LUXEMBOURG" + Delimiter + "LUX-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "LUXEMBOURG" + Delimiter + "LUXEMBOURGCC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "LUXEMBOURG" + Delimiter + "LXCAEU";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "LUXEMBOURG" + Delimiter + "ENGKLUY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "LUXEMBOURG" + Delimiter + "ENGKLUQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "LUXEMBOURG" + Delimiter + "HOLIDAYSLUXEMBOURG";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LUXEMBOURG" + Delimiter + "LXIPRYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "LUXEMBOURG" + Delimiter + "LXCPIYOY";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LUXEMBOURG" + Delimiter + "LUXEMBOURGPROPRICHA";
                /// <summary>
                /// Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "LUXEMBOURG" + Delimiter + "LUXEMBOURGRETSALMOM";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LUXEMBOURG" + Delimiter + "OELUU003";
            }
            /// <summary>
            /// Malta
            /// </summary>
            public static class Malta
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MALTA" + Delimiter + "MALALTABALRADE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MALTA" + Delimiter + "MALTABUSCON";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "MALTA" + Delimiter + "MLT-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "MALTA" + Delimiter + "MALTACONCON";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "MALTA" + Delimiter + "MALALTAGDPATE";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "MALTA" + Delimiter + "HOLIDAYSMALTA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MALTA" + Delimiter + "MALALTAINDCTION";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "MALTA" + Delimiter + "MALALTAINFNRATE";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MALTA" + Delimiter + "MALTAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "MALTA" + Delimiter + "MALALTARETSMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "MALTA" + Delimiter + "MALALTARETSYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MALTA" + Delimiter + "MALALTAUNETRATE";
            }
            /// <summary>
            /// Netherlands
            /// </summary>
            public static class Netherlands
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NETHERLANDS" + Delimiter + "NETBEU";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NETHERLANDS" + Delimiter + "NEPRI";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "NETHERLANDS" + Delimiter + "EUR-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NETHERLANDS" + Delimiter + "NECCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "NETHERLANDS" + Delimiter + "NECATOTE";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "NETHERLANDS" + Delimiter + "NEGDPEY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "NETHERLANDS" + Delimiter + "NEGDPEQ";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "NETHERLANDS" + Delimiter + "GNTH10YR";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "NETHERLANDS" + Delimiter + "HOLIDAYSNETHERLANDS";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NETHERLANDS" + Delimiter + "NEIP20YY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "NETHERLANDS" + Delimiter + "NETHERLANDINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "NETHERLANDS" + Delimiter + "NECPIYOY";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "NETHERLANDS" + Delimiter + "NETHERLANDMANPMI";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "NETHERLANDS" + Delimiter + "NETHERLANDMANPRO";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "NETHERLANDS" + Delimiter + "NETHERLANDPERSPE";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "NETHERLANDS" + Delimiter + "NLDRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "NETHERLANDS" + Delimiter + "NLDRETAILSALESYOY";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "NETHERLANDS" + Delimiter + "NETHERLAND6MBY";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "NETHERLANDS" + Delimiter + "NETHERLAND3MBY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NETHERLANDS" + Delimiter + "NEUETOTR";
            }
            /// <summary>
            /// New Zealand
            /// </summary>
            public static class NewZealand
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NEW-ZEALAND" + Delimiter + "NZMTBAL";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "NEW-ZEALAND" + Delimiter + "NEWZEALANBUIPER";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NEW-ZEALAND" + Delimiter + "NZIEBCAC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "NEW-ZEALAND" + Delimiter + "NZD-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "NEW-ZEALAND" + Delimiter + "NEWZEALANCAPUTI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NEW-ZEALAND" + Delimiter + "NZCC";
                /// <summary>
                /// Credit Card Spending
                /// </summary>
                public const string CreditCardSpending = "NEW-ZEALAND" + Delimiter + "NEWZEALANCRECARSPE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "NEW-ZEALAND" + Delimiter + "NZBPCA";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "NEW-ZEALAND" + Delimiter + "NEWZEALANEMPCHA";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "NEW-ZEALAND" + Delimiter + "NEWZEALANEXPPRI";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "NEW-ZEALAND" + Delimiter + "NZMTEXP";
                /// <summary>
                /// Food Inflation
                /// </summary>
                public const string FoodInflation = "NEW-ZEALAND" + Delimiter + "NEWZEALANFOOINF";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "NEW-ZEALAND" + Delimiter + "NZNTGDPY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "NEW-ZEALAND" + Delimiter + "NZNTGDPC";
                /// <summary>
                /// Global Dairy Trade Price Index
                /// </summary>
                public const string GlobalDairyTradePriceIndex = "NEW-ZEALAND" + Delimiter + "NEWZEALANGDTPI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "NEW-ZEALAND" + Delimiter + "HOLIDAYSNEW-ZEALAND";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "NEW-ZEALAND" + Delimiter + "NEWZEALANHOUIND";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "NEW-ZEALAND" + Delimiter + "NEWZEALANIMPPRI";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "NEW-ZEALAND" + Delimiter + "NZMTIMP";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NEW-ZEALAND" + Delimiter + "OENZV012";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "NEW-ZEALAND" + Delimiter + "NEWZEALANINFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "NEW-ZEALAND" + Delimiter + "NZCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "NEW-ZEALAND" + Delimiter + "NEWZEALANINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "NEW-ZEALAND" + Delimiter + "NZOCRS";
                /// <summary>
                /// Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "NEW-ZEALAND" + Delimiter + "NEWZEALANLABFORPARRA";
                /// <summary>
                /// Labour Costs
                /// </summary>
                public const string LabourCosts = "NEW-ZEALAND" + Delimiter + "NEWZEALANLABCOS";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "NEW-ZEALAND" + Delimiter + "NEWZEALANMANPMI";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "NEW-ZEALAND" + Delimiter + "NEWZEALANPROPRI";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "NEW-ZEALAND" + Delimiter + "NZLRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "NEW-ZEALAND" + Delimiter + "NZLRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "NEW-ZEALAND" + Delimiter + "NEWZEALANSERPMI";
                /// <summary>
                /// Terms of Trade
                /// </summary>
                public const string TermsOfTrade = "NEW-ZEALAND" + Delimiter + "NEWZEALANTEROFTRA";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "NEW-ZEALAND" + Delimiter + "NEWZEALANTOUARR";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NEW-ZEALAND" + Delimiter + "NZLFUNER";
            }
            /// <summary>
            /// Portugal
            /// </summary>
            public static class Portugal
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PORTUGAL" + Delimiter + "PTTBEUAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "PORTUGAL" + Delimiter + "PORTUGALBC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "PORTUGAL" + Delimiter + "EUR-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PORTUGAL" + Delimiter + "PTCCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "PORTUGAL" + Delimiter + "PTCUEURO";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "PORTUGAL" + Delimiter + "PTTBEUEX";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "PORTUGAL" + Delimiter + "PTGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "PORTUGAL" + Delimiter + "PTGDPQOQ";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "PORTUGAL" + Delimiter + "PORTUGALGOVBUDVAL";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "PORTUGAL" + Delimiter + "HOLIDAYSPORTUGAL";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "PORTUGAL" + Delimiter + "PTTBEUIM";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PORTUGAL" + Delimiter + "PTIPTOTY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "PORTUGAL" + Delimiter + "PORTUGALINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "PORTUGAL" + Delimiter + "PLCPYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "PORTUGAL" + Delimiter + "PORTUGALINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "PORTUGAL" + Delimiter + "EURR002W-PTE";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "PORTUGAL" + Delimiter + "PORTUGALLEAECOIND";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "PORTUGAL" + Delimiter + "PORTUGALPERSPE";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "PORTUGAL" + Delimiter + "PORTUGALPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PORTUGAL" + Delimiter + "PORTUGALPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "PORTUGAL" + Delimiter + "PRTRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "PORTUGAL" + Delimiter + "PRTRETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PORTUGAL" + Delimiter + "PTUE";
            }
            /// <summary>
            /// Slovakia
            /// </summary>
            public static class Slovakia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SLOVAKIA" + Delimiter + "SLOVAKIABT";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SLOVAKIA" + Delimiter + "SLOVAKIABC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "SLOVAKIA" + Delimiter + "SVK-CALENDAR";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "SLOVAKIA" + Delimiter + "SLOVAKIACONOUT";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SLOVAKIA" + Delimiter + "SLOVAKIACC";
                /// <summary>
                /// Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "SLOVAKIA" + Delimiter + "SLOVAKIACORCONPRI";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SLOVAKIA" + Delimiter + "SVKCORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "SLOVAKIA" + Delimiter + "SLOVAKIACA";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "SLOVAKIA" + Delimiter + "SLOVAKIAEX";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "SLOVAKIA" + Delimiter + "SLOVAKIAGDPYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "SLOVAKIA" + Delimiter + "SLOVAKIAGDPQOQ";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SLOVAKIA" + Delimiter + "SLOVAKIAHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "SLOVAKIA" + Delimiter + "HOLIDAYSSLOVAKIA";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "SLOVAKIA" + Delimiter + "SLOVAKIAIM";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SLOVAKIA" + Delimiter + "SLOVAKIAIP";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SLOVAKIA" + Delimiter + "SLOVAKIAINDPROMOM";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SLOVAKIA" + Delimiter + "SLOVAKIAIR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SLOVAKIA" + Delimiter + "SLOVAKIAINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "SLOVAKIA" + Delimiter + "EURR002W-SKK";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SLOVAKIA" + Delimiter + "SVKRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SLOVAKIA" + Delimiter + "SVKRETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SLOVAKIA" + Delimiter + "SLOVAKIAUR";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "SLOVAKIA" + Delimiter + "SLOVAKIAWAGGRO";
            }
            /// <summary>
            /// Slovenia
            /// </summary>
            public static class Slovenia
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SLOVENIA" + Delimiter + "SVTBBALE";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SLOVENIA" + Delimiter + "SLOVENIABC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "SLOVENIA" + Delimiter + "SVN-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SLOVENIA" + Delimiter + "EUCCSI";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "SLOVENIA" + Delimiter + "SVGDCYOY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "SLOVENIA" + Delimiter + "EUGNSIQQ";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SLOVENIA" + Delimiter + "SLOVENIAHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "SLOVENIA" + Delimiter + "HOLIDAYSSLOVENIA";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SLOVENIA" + Delimiter + "SVIPTYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SLOVENIA" + Delimiter + "SVCPYOY";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "SLOVENIA" + Delimiter + "EURR002W-SIT";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SLOVENIA" + Delimiter + "SLOVENIAPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SLOVENIA" + Delimiter + "SVNRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SLOVENIA" + Delimiter + "SVNRETAILSALESYOY";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SLOVENIA" + Delimiter + "SLOVENIATOUARR";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SLOVENIA" + Delimiter + "SVUER";
            }
            /// <summary>
            /// Spain
            /// </summary>
            public static class Spain
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SPAIN" + Delimiter + "SPTBEUBL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SPAIN" + Delimiter + "SPAINBC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "SPAIN" + Delimiter + "ESP-CALENDAR";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SPAIN" + Delimiter + "SPAINCC";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "SPAIN" + Delimiter + "SPCAEURO";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "SPAIN" + Delimiter + "SPAINFACORD";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "SPAIN" + Delimiter + "SPAIN5WBY";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "SPAIN" + Delimiter + "SPAIN5YNY";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "SPAIN" + Delimiter + "SPNAGDPY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "SPAIN" + Delimiter + "SPNAGDPQ";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "SPAIN" + Delimiter + "GSPG10YR";
                /// <summary>
                /// Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SPAIN" + Delimiter + "SPAINHARCONPRI";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "SPAIN" + Delimiter + "HOLIDAYSSPAIN";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SPAIN" + Delimiter + "SPAINIP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SPAIN" + Delimiter + "SPIPCYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SPAIN" + Delimiter + "SPAININFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "SPAIN" + Delimiter + "EURR002W-ESP";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SPAIN" + Delimiter + "SPAINMANPMI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SPAIN" + Delimiter + "SPAINPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SPAIN" + Delimiter + "ESPRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SPAIN" + Delimiter + "ESPRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "SPAIN" + Delimiter + "SPAINSERPMI";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "SPAIN" + Delimiter + "SPAIN6MBY";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "SPAIN" + Delimiter + "SPAIN3MBY";
                /// <summary>
                /// 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "SPAIN" + Delimiter + "SPAIN3YNY";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "SPAIN" + Delimiter + "SPAINTOTVEHSAL";
                /// <summary>
                /// Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SPAIN" + Delimiter + "SPAINTOUARR";
                /// <summary>
                /// Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "SPAIN" + Delimiter + "SPAINUNECHA";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SPAIN" + Delimiter + "UMRTES";
            }
            /// <summary>
            /// Sweden
            /// </summary>
            public static class Sweden
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SWEDEN" + Delimiter + "SWTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SWEDEN" + Delimiter + "SWEDENBC";
                /// <summary>
                /// Business Inventories
                /// </summary>
                public const string BusinessInventories = "SWEDEN" + Delimiter + "SWEDENBUSINV";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "SWEDEN" + Delimiter + "SEK-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "SWEDEN" + Delimiter + "SWEDENCAPUTI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SWEDEN" + Delimiter + "SWECCI";
                /// <summary>
                /// Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "SWEDEN" + Delimiter + "SWEDENCORCONPRI";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SWEDEN" + Delimiter + "SWECORECPIRATE";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "SWEDEN" + Delimiter + "SWCA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "SWEDEN" + Delimiter + "SWGDPAYY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "SWEDEN" + Delimiter + "SWGDPAQQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "SWEDEN" + Delimiter + "HOLIDAYSSWEDEN";
                /// <summary>
                /// Household Spending
                /// </summary>
                public const string HouseholdSpending = "SWEDEN" + Delimiter + "SWEDENHOUSPE";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SWEDEN" + Delimiter + "SWIPIYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SWEDEN" + Delimiter + "SWEDENINDPROMOM";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "SWEDEN" + Delimiter + "SWEDENINFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SWEDEN" + Delimiter + "SWCPYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SWEDEN" + Delimiter + "SWEDENINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "SWEDEN" + Delimiter + "SWRRATEI";
                /// <summary>
                /// Loan Growth
                /// </summary>
                public const string LoanGrowth = "SWEDEN" + Delimiter + "SWEDENLOAGRO";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SWEDEN" + Delimiter + "SWEDENMANPMI";
                /// <summary>
                /// New Orders
                /// </summary>
                public const string NewOrders = "SWEDEN" + Delimiter + "SWEDENNEWORD";
                /// <summary>
                /// Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "SWEDEN" + Delimiter + "SWEDENPRISECCRE";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "SWEDEN" + Delimiter + "SWEDENPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SWEDEN" + Delimiter + "SWEDENPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SWEDEN" + Delimiter + "SWERETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SWEDEN" + Delimiter + "SWERETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "SWEDEN" + Delimiter + "SWEDENSERPMI";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SWEDEN" + Delimiter + "UMRTSE";
            }
            /// <summary>
            /// Switzerland
            /// </summary>
            public static class Switzerland
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SWITZERLAND" + Delimiter + "SZTBAL";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SWITZERLAND" + Delimiter + "SWITZERLANDBC";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "SWITZERLAND" + Delimiter + "CHEINDICATORS";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SWITZERLAND" + Delimiter + "SZCCI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "SWITZERLAND" + Delimiter + "SZCA";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "SWITZERLAND" + Delimiter + "SWITZERLANFACORD";
                /// <summary>
                /// Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SWITZERLAND" + Delimiter + "SWITZERLANFOREXCRES";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "SWITZERLAND" + Delimiter + "SZGDPCYY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "SWITZERLAND" + Delimiter + "SZGDPCQQ";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "SWITZERLAND" + Delimiter + "HOLIDAYSSWITZERLAND";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SWITZERLAND" + Delimiter + "SZIPIYOY";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "SWITZERLAND" + Delimiter + "SZCPIYOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SWITZERLAND" + Delimiter + "SWITZERLANINFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "SWITZERLAND" + Delimiter + "SZLTTR";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SWITZERLAND" + Delimiter + "SWITZERLANMANPMI";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "SWITZERLAND" + Delimiter + "SWITZERLANNONFARPAY";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "SWITZERLAND" + Delimiter + "SWITZERLANPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SWITZERLAND" + Delimiter + "SWITZERLANPROPRICHA";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SWITZERLAND" + Delimiter + "CHERETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SWITZERLAND" + Delimiter + "CHERETAILSALESYOY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SWITZERLAND" + Delimiter + "SZUEUEA";
                /// <summary>
                /// Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "SWITZERLAND" + Delimiter + "SWITZERLANZEWECOSENI";
            }
            /// <summary>
            /// United Kingdom
            /// </summary>
            public static class UnitedKingdom
            {
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "UNITED-KINGDOM" + Delimiter + "UKTBTTBA";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "UNITED-KINGDOM" + Delimiter + "EUR1UK";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "UNITED-KINGDOM" + Delimiter + "GBP-CALENDAR";
                /// <summary>
                /// Car Registrations
                /// </summary>
                public const string CarRegistrations = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCARREG";
                /// <summary>
                /// CBI Distributive Trades
                /// </summary>
                public const string CBIDistributiveTrades = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCBIDISTRA";
                /// <summary>
                /// Claimant Count Change
                /// </summary>
                public const string ClaimantCountChange = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCLACOUCHA";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCOMPMI";
                /// <summary>
                /// Construction Orders
                /// </summary>
                public const string ConstructionOrders = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCONORD";
                /// <summary>
                /// Construction Output
                /// </summary>
                public const string ConstructionOutput = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCONOUT";
                /// <summary>
                /// Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCONPMI";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "UNITED-KINGDOM" + Delimiter + "UKCCI";
                /// <summary>
                /// Consumer Credit
                /// </summary>
                public const string ConsumerCredit = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCONCRE";
                /// <summary>
                /// Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCORCONPRI";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "UNITED-KINGDOM" + Delimiter + "GBRCORECPIRATE";
                /// <summary>
                /// Core Producer Prices
                /// </summary>
                public const string CoreProducerPrices = "UNITED-KINGDOM" + Delimiter + "UNITEDKINCORPROPRI";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "UNITED-KINGDOM" + Delimiter + "UKCA";
                /// <summary>
                /// Economic Activity Index
                /// </summary>
                public const string EconomicActivityIndex = "UNITED-KINGDOM" + Delimiter + "UNITEDKINECOACTIND";
                /// <summary>
                /// Employment Change
                /// </summary>
                public const string EmploymentChange = "UNITED-KINGDOM" + Delimiter + "UNITEDKINEMPCHA";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "UNITED-KINGDOM" + Delimiter + "UNITEDKINFACORD";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "UNITED-KINGDOM" + Delimiter + "UNITEDKIN5YEANOTYIE";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "UNITED-KINGDOM" + Delimiter + "UKGRYBZY";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "UNITED-KINGDOM" + Delimiter + "UKGRYBZQ";
                /// <summary>
                /// Goods Trade Balance
                /// </summary>
                public const string GoodsTradeBalance = "UNITED-KINGDOM" + Delimiter + "UNITEDKINGOOTRABAL";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "UNITED-KINGDOM" + Delimiter + "GUKG10";
                /// <summary>
                /// Government Debt
                /// </summary>
                public const string GovernmentDebt = "UNITED-KINGDOM" + Delimiter + "UNITEDKINGOVDEB";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "UNITED-KINGDOM" + Delimiter + "HOLIDAYSUNITED-KING";
                /// <summary>
                /// Home Loans
                /// </summary>
                public const string HomeLoans = "UNITED-KINGDOM" + Delimiter + "UNITEDKINHOMLOA";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "UNITED-KINGDOM" + Delimiter + "UNITEDKINHOUIND";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "UNITED-KINGDOM" + Delimiter + "UKIPIYOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "UNITED-KINGDOM" + Delimiter + "UNITEDKININDPROMOM";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "UNITED-KINGDOM" + Delimiter + "UNITEDKININFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "UNITED-KINGDOM" + Delimiter + "UKRPCJYR";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "UNITED-KINGDOM" + Delimiter + "UNITEDKININFRATMOM";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "UNITED-KINGDOM" + Delimiter + "UKBRBASE";
                /// <summary>
                /// Labour Costs
                /// </summary>
                public const string LabourCosts = "UNITED-KINGDOM" + Delimiter + "UNITEDKINLABCOS";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "UNITED-KINGDOM" + Delimiter + "UNITEDKINLEAECOIND";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "UNITED-KINGDOM" + Delimiter + "UNITEDKINMANPMI";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "UNITED-KINGDOM" + Delimiter + "UNITEDKINMANPRO";
                /// <summary>
                /// Mortgage Applications
                /// </summary>
                public const string MortgageApplications = "UNITED-KINGDOM" + Delimiter + "UNITEDKINMORAPPLI";
                /// <summary>
                /// Mortgage Approvals
                /// </summary>
                public const string MortgageApprovals = "UNITED-KINGDOM" + Delimiter + "UNITEDKINMORAPP";
                /// <summary>
                /// Nationwide Housing Prices
                /// </summary>
                public const string NationwideHousingPrices = "UNITED-KINGDOM" + Delimiter + "UNITEDKINNATHOUPRI";
                /// <summary>
                /// Private Investment
                /// </summary>
                public const string PrivateInvestment = "UNITED-KINGDOM" + Delimiter + "UNITEDKINPRIINV";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "UNITED-KINGDOM" + Delimiter + "UNITEDKINPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "UNITED-KINGDOM" + Delimiter + "UNITEDKINPROPRICHA";
                /// <summary>
                /// Productivity
                /// </summary>
                public const string Productivity = "UNITED-KINGDOM" + Delimiter + "UNITEDKINPRO";
                /// <summary>
                /// Retail Price Index
                /// </summary>
                public const string RetailPriceIndex = "UNITED-KINGDOM" + Delimiter + "UNITEDKINRETPRIIND";
                /// <summary>
                /// Retail Sales Ex Fuel
                /// </summary>
                public const string RetailSalesExFuel = "UNITED-KINGDOM" + Delimiter + "UNITEDKINRSEF";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "UNITED-KINGDOM" + Delimiter + "GBRRETAILSALESMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "UNITED-KINGDOM" + Delimiter + "GBRRETAILSALESYOY";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "UNITED-KINGDOM" + Delimiter + "UNITEDKINSERPMI";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "UNITED-KINGDOM" + Delimiter + "UNITEDKIN3YBY";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UNITED-KINGDOM" + Delimiter + "UKUEILOR";
                /// <summary>
                /// Wage Growth
                /// </summary>
                public const string WageGrowth = "UNITED-KINGDOM" + Delimiter + "UNITEDKINWAGGRO";
            }
            /// <summary>
            /// United States
            /// </summary>
            public static class UnitedStates
            {
                /// <summary>
                /// Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "UNITED-STATES" + Delimiter + "UNITEDSTAADPEMPCHA";
                /// <summary>
                /// API Crude Oil Stock Change
                /// </summary>
                public const string APICrudeOilStockChange = "UNITED-STATES" + Delimiter + "APICRUDEOIL";
                /// <summary>
                /// Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "UNITED-STATES" + Delimiter + "UNITEDSTAAVEHOUEAR";
                /// <summary>
                /// Average Weekly Hours
                /// </summary>
                public const string AverageWeeklyHours = "UNITED-STATES" + Delimiter + "UNITEDSTAAVEWEEHOU";
                /// <summary>
                /// Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "UNITED-STATES" + Delimiter + "USTBTOT";
                /// <summary>
                /// Building Permits
                /// </summary>
                public const string BuildingPermits = "UNITED-STATES" + Delimiter + "UNITEDSTABUIPER";
                /// <summary>
                /// Business Confidence
                /// </summary>
                public const string BusinessConfidence = "UNITED-STATES" + Delimiter + "NAPMPMI";
                /// <summary>
                /// Business Inventories
                /// </summary>
                public const string BusinessInventories = "UNITED-STATES" + Delimiter + "UNITEDSTABUSINV";
                /// <summary>
                /// Calendar
                /// </summary>
                public const string Calendar = "UNITED-STATES" + Delimiter + "USD-CALENDAR";
                /// <summary>
                /// Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "UNITED-STATES" + Delimiter + "UNITEDSTACAPUTI";
                /// <summary>
                /// Capital Flows
                /// </summary>
                public const string CapitalFlows = "UNITED-STATES" + Delimiter + "UNITEDSTACAPFLO";
                /// <summary>
                /// Case Shiller Home Price Index
                /// </summary>
                public const string CaseShillerHomePriceIndex = "UNITED-STATES" + Delimiter + "UNITEDSTACASSHIHOMPR";
                /// <summary>
                /// Chain Store Sales
                /// </summary>
                public const string ChainStoreSales = "UNITED-STATES" + Delimiter + "UNITEDSTACHASTOSAL";
                /// <summary>
                /// Challenger Job Cuts
                /// </summary>
                public const string ChallengerJobCuts = "UNITED-STATES" + Delimiter + "UNITEDSTACHAJOBCUT";
                /// <summary>
                /// Chicago Fed National Activity Index
                /// </summary>
                public const string ChicagoFedNationalActivityIndex = "UNITED-STATES" + Delimiter + "UNITEDSTACHIFEDNATAC";
                /// <summary>
                /// Chicago Pmi
                /// </summary>
                public const string ChicagoPmi = "UNITED-STATES" + Delimiter + "UNITEDSTACHIPMI";
                /// <summary>
                /// Composite Pmi
                /// </summary>
                public const string CompositePmi = "UNITED-STATES" + Delimiter + "UNITEDSTACOMPMI";
                /// <summary>
                /// Construction Spending
                /// </summary>
                public const string ConstructionSpending = "UNITED-STATES" + Delimiter + "UNITEDSTACONTSPE";
                /// <summary>
                /// Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "UNITED-STATES" + Delimiter + "CONCCONF";
                /// <summary>
                /// Consumer Credit
                /// </summary>
                public const string ConsumerCredit = "UNITED-STATES" + Delimiter + "UNITEDSTACONCRE";
                /// <summary>
                /// Consumer Price Index (CPI)
                /// </summary>
                public const string ConsumerPriceIndexCPI = "UNITED-STATES" + Delimiter + "UNITEDSTACONPRIINDCP";
                /// <summary>
                /// Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "UNITED-STATES" + Delimiter + "UNITEDSTACONSPE";
                /// <summary>
                /// Continuing Jobless Claims
                /// </summary>
                public const string ContinuingJoblessClaims = "UNITED-STATES" + Delimiter + "UNITEDSTACONJOBCLA";
                /// <summary>
                /// Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "UNITED-STATES" + Delimiter + "UNITEDSTACORCONPRI";
                /// <summary>
                /// Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "UNITED-STATES" + Delimiter + "USACORECPIRATE";
                /// <summary>
                /// Core Pce Price Index
                /// </summary>
                public const string CorePcePriceIndex = "UNITED-STATES" + Delimiter + "UNITEDSTACORPCEPRIIN";
                /// <summary>
                /// Core Producer Prices
                /// </summary>
                public const string CoreProducerPrices = "UNITED-STATES" + Delimiter + "UNITEDSTACORPROPRI";
                /// <summary>
                /// Corporate Profits
                /// </summary>
                public const string CorporateProfits = "UNITED-STATES" + Delimiter + "UNITEDSTACORPRO";
                /// <summary>
                /// Crude Oil Imports
                /// </summary>
                public const string CrudeOilImports = "UNITED-STATES" + Delimiter + "UNITEDSTACRUOILIMP";
                /// <summary>
                /// Crude Oil Rigs
                /// </summary>
                public const string CrudeOilRigs = "UNITED-STATES" + Delimiter + "USOILRIGS";
                /// <summary>
                /// Crude Oil Stocks Change
                /// </summary>
                public const string CrudeOilStocksChange = "UNITED-STATES" + Delimiter + "UNITEDSTACRUOILSTOCH";
                /// <summary>
                /// Current Account
                /// </summary>
                public const string CurrentAccount = "UNITED-STATES" + Delimiter + "USCABAL";
                /// <summary>
                /// Cushing Crude Oil Stocks
                /// </summary>
                public const string CushingCrudeOilStocks = "UNITED-STATES" + Delimiter + "UNITEDSTACCOS";
                /// <summary>
                /// Dallas Fed Manufacturing Index
                /// </summary>
                public const string DallasFedManufacturingIndex = "UNITED-STATES" + Delimiter + "UNITEDSTADALFEDMANIN";
                /// <summary>
                /// Distillate Fuel Production
                /// </summary>
                public const string DistillateFuelProduction = "UNITED-STATES" + Delimiter + "UNITEDSTADISFUEPRO";
                /// <summary>
                /// Distillate Stocks
                /// </summary>
                public const string DistillateStocks = "UNITED-STATES" + Delimiter + "UNITEDSTADISSTO";
                /// <summary>
                /// Durable Goods Orders
                /// </summary>
                public const string DurableGoodsOrders = "UNITED-STATES" + Delimiter + "UNITEDSTADURGOOORD";
                /// <summary>
                /// Durable Goods Orders Ex Defense
                /// </summary>
                public const string DurableGoodsOrdersExDefense = "UNITED-STATES" + Delimiter + "UNITEDSTADURGOOORDED";
                /// <summary>
                /// Durable Goods Orders Ex Transportation
                /// </summary>
                public const string DurableGoodsOrdersExTransportation = "UNITED-STATES" + Delimiter + "UNITEDSTADURGOOORDEX";
                /// <summary>
                /// Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "UNITED-STATES" + Delimiter + "UNITEDSTAECOOPTIND";
                /// <summary>
                /// Employment Cost Index
                /// </summary>
                public const string EmploymentCostIndex = "UNITED-STATES" + Delimiter + "UNITEDSTAEMPCOSIND";
                /// <summary>
                /// Existing Home Sales
                /// </summary>
                public const string ExistingHomeSales = "UNITED-STATES" + Delimiter + "UNITEDSTAEXIHOMSAL";
                /// <summary>
                /// Export Prices
                /// </summary>
                public const string ExportPrices = "UNITED-STATES" + Delimiter + "UNITEDSTAEXPPRI";
                /// <summary>
                /// Exports
                /// </summary>
                public const string Exports = "UNITED-STATES" + Delimiter + "TBEXTOT";
                /// <summary>
                /// Factory Orders
                /// </summary>
                public const string FactoryOrders = "UNITED-STATES" + Delimiter + "UNITEDSTAFACORD";
                /// <summary>
                /// Factory Orders Ex Transportation
                /// </summary>
                public const string FactoryOrdersExTransportation = "UNITED-STATES" + Delimiter + "UNITEDSTAFACORDEXTRA";
                /// <summary>
                /// 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "UNITED-STATES" + Delimiter + "UNITEDSTA52WEEBILYIE";
                /// <summary>
                /// 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "UNITED-STATES" + Delimiter + "UNITEDSTA5YEANOTYIE";
                /// <summary>
                /// Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "UNITED-STATES" + Delimiter + "UNITEDSTAFORBONINV";
                /// <summary>
                /// 4 Week Bill Yield
                /// </summary>
                public const string FourWeekBillYield = "UNITED-STATES" + Delimiter + "UNITEDSTA4WEEBILYIE";
                /// <summary>
                /// Gasoline Production
                /// </summary>
                public const string GasolineProduction = "UNITED-STATES" + Delimiter + "UNITEDSTAGASPRO";
                /// <summary>
                /// Gasoline Stocks Change
                /// </summary>
                public const string GasolineStocksChange = "UNITED-STATES" + Delimiter + "UNITEDSTAGASSTOCHA";
                /// <summary>
                /// GDP Annual Growth Rate
                /// </summary>
                public const string GDPAnnualGrowthRate = "UNITED-STATES" + Delimiter + "GDP-CYOY";
                /// <summary>
                /// Gdp Deflator
                /// </summary>
                public const string GdpDeflator = "UNITED-STATES" + Delimiter + "UNITEDSTAGDPDEF";
                /// <summary>
                /// GDP Growth Rate
                /// </summary>
                public const string GDPGrowthRate = "UNITED-STATES" + Delimiter + "GDP-CQOQ";
                /// <summary>
                /// Goods Trade Balance
                /// </summary>
                public const string GoodsTradeBalance = "UNITED-STATES" + Delimiter + "UNITEDSTAGOOTRABAL";
                /// <summary>
                /// Government Bond 10Y
                /// </summary>
                public const string GovernmentBondTenY = "UNITED-STATES" + Delimiter + "USGG10YR";
                /// <summary>
                /// Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "UNITED-STATES" + Delimiter + "UNITEDSTAGOVBUDVAL";
                /// <summary>
                /// Government Payrolls
                /// </summary>
                public const string GovernmentPayrolls = "UNITED-STATES" + Delimiter + "UNITEDSTAGOVPAY";
                /// <summary>
                /// Holidays
                /// </summary>
                public const string Holidays = "UNITED-STATES" + Delimiter + "HOLIDAYSUNITED-STAT";
                /// <summary>
                /// Housing Index
                /// </summary>
                public const string HousingIndex = "UNITED-STATES" + Delimiter + "UNITEDSTAHOUIND";
                /// <summary>
                /// Housing Starts
                /// </summary>
                public const string HousingStarts = "UNITED-STATES" + Delimiter + "UNITEDSTAHOUSTA";
                /// <summary>
                /// Import Prices
                /// </summary>
                public const string ImportPrices = "UNITED-STATES" + Delimiter + "UNITEDSTAIMPPRI";
                /// <summary>
                /// Imports
                /// </summary>
                public const string Imports = "UNITED-STATES" + Delimiter + "TBIMTOT";
                /// <summary>
                /// Industrial Production
                /// </summary>
                public const string IndustrialProduction = "UNITED-STATES" + Delimiter + "IP-YOY";
                /// <summary>
                /// Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "UNITED-STATES" + Delimiter + "UNITEDSTAINDPROMOM";
                /// <summary>
                /// Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "UNITED-STATES" + Delimiter + "UNITEDSTAINFEXP";
                /// <summary>
                /// Inflation Rate
                /// </summary>
                public const string InflationRate = "UNITED-STATES" + Delimiter + "CPI-YOY";
                /// <summary>
                /// Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "UNITED-STATES" + Delimiter + "UNITEDSTAINFRATMOM";
                /// <summary>
                /// Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "UNITED-STATES" + Delimiter + "IJCUSA";
                /// <summary>
                /// Interest Rate
                /// </summary>
                public const string InterestRate = "UNITED-STATES" + Delimiter + "FDTR";
                /// <summary>
                /// Ism New York Index
                /// </summary>
                public const string IsmNewYorkIndex = "UNITED-STATES" + Delimiter + "UNITEDSTAISMNEWYORIN";
                /// <summary>
                /// Job Offers
                /// </summary>
                public const string JobOffers = "UNITED-STATES" + Delimiter + "UNITEDSTAJOBOFF";
                /// <summary>
                /// Kansas Fed Manufacturing Index
                /// </summary>
                public const string KansasFedManufacturingIndex = "UNITED-STATES" + Delimiter + "UNITEDSTAKFMI";
                /// <summary>
                /// Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "UNITED-STATES" + Delimiter + "UNITEDSTALABFORPARRA";
                /// <summary>
                /// Labor Market Conditions Index
                /// </summary>
                public const string LaborMarketConditionsIndex = "UNITED-STATES" + Delimiter + "UNITEDSTALMCI";
                /// <summary>
                /// Labour Costs
                /// </summary>
                public const string LabourCosts = "UNITED-STATES" + Delimiter + "UNITEDSTALABCOS";
                /// <summary>
                /// Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "UNITED-STATES" + Delimiter + "UNITEDSTALEAECOIND";
                /// <summary>
                /// Manufacturing Payrolls
                /// </summary>
                public const string ManufacturingPayrolls = "UNITED-STATES" + Delimiter + "UNITEDSTAMANPAY";
                /// <summary>
                /// Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "UNITED-STATES" + Delimiter + "UNITEDSTAMANPMI";
                /// <summary>
                /// Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "UNITED-STATES" + Delimiter + "UNITEDSTAMANPRO";
                /// <summary>
                /// Mortgage Applications
                /// </summary>
                public const string MortgageApplications = "UNITED-STATES" + Delimiter + "UNITEDSTAMORAPP";
                /// <summary>
                /// Mortgage Rate
                /// </summary>
                public const string MortgageRate = "UNITED-STATES" + Delimiter + "UNITEDSTAMORRAT";
                /// <summary>
                /// Nahb Housing Market Index
                /// </summary>
                public const string NahbHousingMarketIndex = "UNITED-STATES" + Delimiter + "UNITEDSTANAHHOUMARIN";
                /// <summary>
                /// Natural Gas Stocks Change
                /// </summary>
                public const string NaturalGasStocksChange = "UNITED-STATES" + Delimiter + "UNITEDSTANATGASSTOCH";
                /// <summary>
                /// Net Long-term Tic Flows
                /// </summary>
                public const string NetLongTermTicFlows = "UNITED-STATES" + Delimiter + "UNITEDSTANETLONTICFL";
                /// <summary>
                /// New Home Sales
                /// </summary>
                public const string NewHomeSales = "UNITED-STATES" + Delimiter + "UNITEDSTANEWHOMSAL";
                /// <summary>
                /// Nfib Business Optimism Index
                /// </summary>
                public const string NfibBusinessOptimismIndex = "UNITED-STATES" + Delimiter + "UNITEDSTANFIBUSOPTIN";
                /// <summary>
                /// Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "UNITED-STATES" + Delimiter + "NFP-TCH";
                /// <summary>
                /// Nonfarm Payrolls - Private
                /// </summary>
                public const string NonfarmPayrollsPrivate = "UNITED-STATES" + Delimiter + "UNITEDSTANONPAY-PRI";
                /// <summary>
                /// Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "UNITED-STATES" + Delimiter + "UNITEDSTANONMANPMI";
                /// <summary>
                /// Ny Empire State Manufacturing Index
                /// </summary>
                public const string NyEmpireStateManufacturingIndex = "UNITED-STATES" + Delimiter + "UNITEDSTANYEMPSTAMAN";
                /// <summary>
                /// Pce Price Index
                /// </summary>
                public const string PcePriceIndex = "UNITED-STATES" + Delimiter + "UNITEDSTAPCEPRIIND";
                /// <summary>
                /// Pending Home Sales
                /// </summary>
                public const string PendingHomeSales = "UNITED-STATES" + Delimiter + "UNITEDSTAPENHOMSAL";
                /// <summary>
                /// Personal Income
                /// </summary>
                public const string PersonalIncome = "UNITED-STATES" + Delimiter + "UNITEDSTAPERINC";
                /// <summary>
                /// Personal Spending
                /// </summary>
                public const string PersonalSpending = "UNITED-STATES" + Delimiter + "UNITEDSTAPERSPE";
                /// <summary>
                /// Philadelphia Fed Manufacturing Index
                /// </summary>
                public const string PhiladelphiaFedManufacturingIndex = "UNITED-STATES" + Delimiter + "UNITEDSTAPHIFEDMANIN";
                /// <summary>
                /// Producer Prices
                /// </summary>
                public const string ProducerPrices = "UNITED-STATES" + Delimiter + "UNITEDSTAPROPRI";
                /// <summary>
                /// Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "UNITED-STATES" + Delimiter + "UNITEDSTAPROPRICHA";
                /// <summary>
                /// Productivity
                /// </summary>
                public const string Productivity = "UNITED-STATES" + Delimiter + "UNITEDSTAPRO";
                /// <summary>
                /// Redbook Index
                /// </summary>
                public const string RedbookIndex = "UNITED-STATES" + Delimiter + "UNITEDSTAREDIND";
                /// <summary>
                /// Refinery Crude Runs
                /// </summary>
                public const string RefineryCrudeRuns = "UNITED-STATES" + Delimiter + "UNITEDSTAREFCRURUN";
                /// <summary>
                /// Retail Sales Ex Autos
                /// </summary>
                public const string RetailSalesExAutos = "UNITED-STATES" + Delimiter + "UNITEDSTARETSALEXAUT";
                /// <summary>
                /// Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "UNITED-STATES" + Delimiter + "RSTAMOM";
                /// <summary>
                /// Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "UNITED-STATES" + Delimiter + "USARETAILSALESYOY";
                /// <summary>
                /// Richmond Fed Manufacturing Index
                /// </summary>
                public const string RichmondFedManufacturingIndex = "UNITED-STATES" + Delimiter + "UNITEDSTARICFEDMANIN";
                /// <summary>
                /// Services Pmi
                /// </summary>
                public const string ServicesPmi = "UNITED-STATES" + Delimiter + "UNITEDSTASERPMI";
                /// <summary>
                /// 7 Year Note Yield
                /// </summary>
                public const string SevenYearNoteYield = "UNITED-STATES" + Delimiter + "UNITEDSTA7YEANOTYIE";
                /// <summary>
                /// 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "UNITED-STATES" + Delimiter + "UNITEDSTA6MONBILYIE";
                /// <summary>
                /// 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "UNITED-STATES" + Delimiter + "UNITEDSTA30YEABONYIE";
                /// <summary>
                /// 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "UNITED-STATES" + Delimiter + "UNITEDSTA3MONBILYIE";
                /// <summary>
                /// 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "UNITED-STATES" + Delimiter + "UNITEDSTA3YEANOTYIE";
                /// <summary>
                /// Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "UNITED-STATES" + Delimiter + "UNITEDSTATOTVEHSAL";
                /// <summary>
                /// 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "UNITED-STATES" + Delimiter + "UNITEDSTA2YEANOTYIE";
                /// <summary>
                /// Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UNITED-STATES" + Delimiter + "USURTOT";
                /// <summary>
                /// Wholesale Inventories
                /// </summary>
                public const string WholesaleInventories = "UNITED-STATES" + Delimiter + "UNITEDSTAWHOINV";
            }
        }
    }
}
