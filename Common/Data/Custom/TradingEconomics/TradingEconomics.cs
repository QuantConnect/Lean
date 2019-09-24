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
    public static class TradingEconomics
    {
        /// <summary>
        /// Calendar group
        /// </summary>
        public static class Calendar
        {
            public static class Albania
            {
                /// <summary>
                /// Albania Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ALBANIABALRADE";
                /// <summary>
                /// Albania Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ALBANIABUSCON";
                /// <summary>
                /// Albania Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ALBANIACONCON";
                /// <summary>
                /// Albania GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ALBANIAGDPATE";
                /// <summary>
                /// Albania GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ALBANIAGDPRATE";
                /// <summary>
                /// Albania Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "ALBANIAHARCONPRI";
                /// <summary>
                /// Albania Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ALBANIAINDCTION";
                /// <summary>
                /// Albania Inflation Rate
                /// </summary>
                public const string InflationRate = "ALBANIAINFNRATE";
                /// <summary>
                /// Albania Interest Rate
                /// </summary>
                public const string InterestRate = "ALBANIAINTTRATE";
                /// <summary>
                /// Albania Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ALBANIAPROPRICHA";
                /// <summary>
                /// Albania Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ALBANIAUNETRATE";
            }
            public static class Angola
            {
                /// <summary>
                /// Angola Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "ANGOLAFOREXCRES";
                /// <summary>
                /// Angola Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ANGOLAINDPRO";
                /// <summary>
                /// Angola Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ANGOLAINDPROMOM";
                /// <summary>
                /// Angola Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ANGOLAINFRATMOM";
                /// <summary>
                /// Angola Interest Rate
                /// </summary>
                public const string InterestRate = "ANGOLAINTRATE";
                /// <summary>
                /// Angola Inflation Rate
                /// </summary>
                public const string InflationRate = "ANGOLAIR";
                /// <summary>
                /// Angola Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "ANGOLAMONSUPM3";
                /// <summary>
                /// Angola Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ANGOLAPROPRICHA";
            }
            public static class Argentina
            {
                /// <summary>
                /// Argentina Interest Rate
                /// </summary>
                public const string InterestRate = "APDR1T";
                /// <summary>
                /// Argentina Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ARBABAL";
                /// <summary>
                /// Argentina Current Account
                /// </summary>
                public const string CurrentAccount = "ARBPCURR";
                /// <summary>
                /// Argentina Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ARCCIND";
                /// <summary>
                /// Argentina Inflation Rate
                /// </summary>
                public const string InflationRate = "ARCPIYOY";
                /// <summary>
                /// Argentina Calendar
                /// </summary>
                public const string Calendar = "ARG-CALENDAR";
                /// <summary>
                /// Argentina Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ARGENTINAINFRATMOM";
                /// <summary>
                /// Argentina Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "ARGENTINALEAECOIND";
                /// <summary>
                /// Argentina GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ARGQPQOX";
                /// <summary>
                /// Argentina GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ARGQPYOX";
                /// <summary>
                /// Argentina Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ARGRETAILSALESMOM";
                /// <summary>
                /// Argentina Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ARGRETAILSALESYOY";
                /// <summary>
                /// Argentina Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ARIPNSYO";
                /// <summary>
                /// Argentina Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ARUERATE";
            }
            public static class Armenia
            {
                /// <summary>
                /// Armenia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ARMENIABALRADE";
                /// <summary>
                /// Armenia Construction Output
                /// </summary>
                public const string ConstructionOutput = "ARMENIACONOUT";
                /// <summary>
                /// Armenia Current Account
                /// </summary>
                public const string CurrentAccount = "ARMENIACURCOUNT";
                /// <summary>
                /// Armenia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ARMENIAGDPATE";
                /// <summary>
                /// Armenia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ARMENIAINDCTION";
                /// <summary>
                /// Armenia Inflation Rate
                /// </summary>
                public const string InflationRate = "ARMENIAINFNRATE";
                /// <summary>
                /// Armenia Interest Rate
                /// </summary>
                public const string InterestRate = "ARMENIAINTTRATE";
                /// <summary>
                /// Armenia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "ARMENIALEAECOIND";
                /// <summary>
                /// Armenia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ARMENIAPROPRICHA";
                /// <summary>
                /// Armenia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ARMENIARETSYOY";
                /// <summary>
                /// Armenia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ARMENIAUNETRATE";
            }
            public static class Australia
            {
                /// <summary>
                /// Australia Current Account
                /// </summary>
                public const string CurrentAccount = "AUCABAL";
                /// <summary>
                /// Australia Inflation Rate
                /// </summary>
                public const string InflationRate = "AUCPIYOY";
                /// <summary>
                /// Australia Calendar
                /// </summary>
                public const string Calendar = "CALENDARAUSTRALIA";
                /// <summary>
                /// Australia Exports
                /// </summary>
                public const string Exports = "AUITEXP";
                /// <summary>
                /// Australia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "AUITGSB";
                /// <summary>
                /// Australia Imports
                /// </summary>
                public const string Imports = "AUITIMP";
                /// <summary>
                /// Australia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "AULFUNEM";
                /// <summary>
                /// Australia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "AUNAGDPC";
                /// <summary>
                /// Australia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "AUNAGDPY";
                /// <summary>
                /// Australia Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "AUSCORECPIRATE";
                /// <summary>
                /// Australia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "AUSRETAILSALESMOM";
                /// <summary>
                /// Australia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "AUSRETAILSALESYOY";
                /// <summary>
                /// Australia Building Permits
                /// </summary>
                public const string BuildingPermits = "AUSTRALIABUIPER";
                /// <summary>
                /// Australia Business Inventories
                /// </summary>
                public const string BusinessInventories = "AUSTRALIABUSINV";
                /// <summary>
                /// Australia Car Registrations
                /// </summary>
                public const string CarRegistrations = "AUSTRALIACARREG";
                /// <summary>
                /// Australia Composite Pmi
                /// </summary>
                public const string CompositePmi = "AUSTRALIACOMPMI";
                /// <summary>
                /// Australia Construction Output
                /// </summary>
                public const string ConstructionOutput = "AUSTRALIACONOUT";
                /// <summary>
                /// Australia Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "AUSTRALIACONPMI";
                /// <summary>
                /// Australia Consumer Price Index (CPI)
                /// </summary>
                public const string ConsumerPriceIndex = "AUSTRALIACONPRIINDCP";
                /// <summary>
                /// Australia Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "AUSTRALIACONSPE";
                /// <summary>
                /// Australia Corporate Profits
                /// </summary>
                public const string CorporateProfits = "AUSTRALIACORPRO";
                /// <summary>
                /// Australia Employment Change
                /// </summary>
                public const string EmploymentChange = "AUSTRALIAEMPCHA";
                /// <summary>
                /// Australia Export Prices
                /// </summary>
                public const string ExportPrices = "AUSTRALIAEXPPRI";
                /// <summary>
                /// Australia Full Time Employment
                /// </summary>
                public const string FullTimeEmployment = "AUSTRALIAFULTIMEMP";
                /// <summary>
                /// Australia GDP Deflator
                /// </summary>
                public const string GdpDeflator = "AUSTRALIAGDPDEF";
                /// <summary>
                /// Australia Gross Fixed Capital Formation
                /// </summary>
                public const string GrossFixedCapitalFormation = "AUSTRALIAGROFIXCAPFO";
                /// <summary>
                /// Australia Housing Index
                /// </summary>
                public const string HousingIndex = "AUSTRALIAHOUIND";
                /// <summary>
                /// Australia Import Prices
                /// </summary>
                public const string ImportPrices = "AUSTRALIAIMPPRI";
                /// <summary>
                /// Australia Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "AUSTRALIAINFEXP";
                /// <summary>
                /// Australia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "AUSTRALIAINFRATMOM";
                /// <summary>
                /// Australia Job Advertisements
                /// </summary>
                public const string JobAdvertisements = "AUSTRALIAJOBADV";
                /// <summary>
                /// Australia Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "AUSTRALIALABFORPARRA";
                /// <summary>
                /// Australia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "AUSTRALIALEAECOIND";
                /// <summary>
                /// Australia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "AUSTRALIAMANPMI";
                /// <summary>
                /// Australia New Home Sales
                /// </summary>
                public const string NewHomeSales = "AUSTRALIANEWHOMSAL";
                /// <summary>
                /// Australia Part Time Employment
                /// </summary>
                public const string PartTimeEmployment = "AUSTRALIAPARTIMEMP";
                /// <summary>
                /// Australia Private Investment
                /// </summary>
                public const string PrivateInvestment = "AUSTRALIAPRIINV";
                /// <summary>
                /// Australia Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "AUSTRALIAPRISECCRE";
                /// <summary>
                /// Australia Producer Prices
                /// </summary>
                public const string ProducerPrices = "AUSTRALIAPROPRI";
                /// <summary>
                /// Australia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "AUSTRALIAPROPRICHA";
                /// <summary>
                /// Australia Services PMI
                /// </summary>
                public const string ServicesPmi = "AUSTRALIASERPMI";
                /// <summary>
                /// Australia Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "AUSTRALIATOTVEHSAL";
                /// <summary>
                /// Australia Wage Growth
                /// </summary>
                public const string WageGrowth = "AUSTRALIAWAGGRO";
                /// <summary>
                /// Australia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NABSCONF";
                /// <summary>
                /// Australia Interest Rate
                /// </summary>
                public const string InterestRate = "RBATCTR";
                /// <summary>
                /// Australia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "WMCCCONPCT";
            }
            public static class Austria
            {
                /// <summary>
                /// Austria GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ASGPGDPQ";
                /// <summary>
                /// Austria GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ASGPGDPY";
                /// <summary>
                /// Austria Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ATIPIYY";
                /// <summary>
                /// Austria Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ATTBAL";
                /// <summary>
                /// Austria Business Confidence
                /// </summary>
                public const string BusinessConfidence = "AUSTRIABUSCON";
                /// <summary>
                /// Austria Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "AUSTRIACC";
                /// <summary>
                /// Austria Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "AUSTRIAHARCONPRI";
                /// <summary>
                /// Austria Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "AUSTRIAINFRATMOM";
                /// <summary>
                /// Austria Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "AUSTRIAMANPMI";
                /// <summary>
                /// Austria Producer Prices
                /// </summary>
                public const string ProducerPrices = "AUSTRIAPROPRI";
                /// <summary>
                /// Austria Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "AUSTRIAPROPRICHA";
                /// <summary>
                /// Austria Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "AUSTRIAUNEPER";
                /// <summary>
                /// Austria Calendar
                /// </summary>
                public const string Calendar = "CALENDARAUSTRIA";
                /// <summary>
                /// Austria Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "AUTRETAILSALESMOM";
                /// <summary>
                /// Austria Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "AUTRETSALYOY";
                /// <summary>
                /// Austria Inflation Rate
                /// </summary>
                public const string InflationRate = "ECCPATYY";
                /// <summary>
                /// Austria Current Account
                /// </summary>
                public const string CurrentAccount = "OEATB007";
                /// <summary>
                /// Austria Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTAT";
            }
            public static class Azerbaijan
            {
                /// <summary>
                /// Azerbaijan Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "AZERBAIJANINFRATMOM";
                /// <summary>
                /// Azerbaijan Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "AZERBAIJANLEAECOIND";
            }
            public static class Bahrain
            {
                /// <summary>
                /// Bahrain GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BAHRAINGDPATE";
                /// <summary>
                /// Bahrain GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BAHRAINGDPGRORAT";
                /// <summary>
                /// Bahrain Inflation Rate
                /// </summary>
                public const string InflationRate = "BAHRAININFNRATE";
                /// <summary>
                /// Bahrain Interest Rate
                /// </summary>
                public const string InterestRate = "BAHRAININTTRATE";
                /// <summary>
                /// Bahrain Loan Growth
                /// </summary>
                public const string LoanGrowth = "BAHRAINLOAGRO";
                /// <summary>
                /// Bahrain Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "BAHRAINMONSUPM2";
            }
            public static class Bangladesh
            {
                /// <summary>
                /// Bangladesh Inflation Rate
                /// </summary>
                public const string InflationRate = "BANGLADESHIR";
            }
            public static class Belarus
            {
                /// <summary>
                /// Belarus Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BELARUSBALRADE";
                /// <summary>
                /// Belarus Current Account
                /// </summary>
                public const string CurrentAccount = "BELARUSCURCOUNT";
                /// <summary>
                /// Belarus Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BELARUSINDCTION";
                /// <summary>
                /// Belarus Inflation Rate
                /// </summary>
                public const string InflationRate = "BELARUSINFNRATE";
                /// <summary>
                /// Belarus Interest Rate
                /// </summary>
                public const string InterestRate = "BELARUSINTTRATE";
                /// <summary>
                /// Belarus Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "BELARUSLEAECOIND";
            }
            public static class Belgium
            {
                /// <summary>
                /// Belgium Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BEBCI";
                /// <summary>
                /// Belgium Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BECCN";
                /// <summary>
                /// Belgium Inflation Rate
                /// </summary>
                public const string InflationRate = "BECPYOY";
                /// <summary>
                /// Belgium GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BEGDPQS";
                /// <summary>
                /// Belgium GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BEGDPYS";
                /// <summary>
                /// Belgium Calendar
                /// </summary>
                public const string Calendar = "CALENDARBELGIUM";
                /// <summary>
                /// Belgium Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "BELGIUMINDPROMOM";
                /// <summary>
                /// Belgium Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "BELGIUMINFRATMOM";
                /// <summary>
                /// Belgium Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "BELGIUMPROPRICHA";
                /// <summary>
                /// Belgium Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BELRETAILSALESMOM";
                /// <summary>
                /// Belgium Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BELRETAILSALESYOY";
                /// <summary>
                /// Belgium Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BEPDRYY";
                /// <summary>
                /// Belgium Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BETBTBAL";
                /// <summary>
                /// Belgium Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "BEUER";
                /// <summary>
                /// Belgium Current Account
                /// </summary>
                public const string CurrentAccount = "OEBEB026";
            }
            public static class BosniaAndHerzegovina
            {
                /// <summary>
                /// BosniaAndHerzegovina Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BOSNABALRADE";
                /// <summary>
                /// BosniaAndHerzegovina Current Account
                /// </summary>
                public const string CurrentAccount = "BOSNACURCOUNT";
                /// <summary>
                /// BosniaAndHerzegovina GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BOSNAGDPATE";
                /// <summary>
                /// BosniaAndHerzegovina Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BOSNAINDCTION";
                /// <summary>
                /// BosniaAndHerzegovina Inflation Rate
                /// </summary>
                public const string InflationRate = "BOSNAINFNRATE";
                /// <summary>
                /// BosniaAndHerzegovina Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BOSNARETSMOM";
                /// <summary>
                /// BosniaAndHerzegovina Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BOSNARETSYOY";
                /// <summary>
                /// BosniaAndHerzegovina Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "BOSNIAANDPROPRICHA";
            }
            public static class Botswana
            {
                /// <summary>
                /// Botswana Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BOTSWANABT";
                /// <summary>
                /// Botswana GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BOTSWANAGDPQOQ";
                /// <summary>
                /// Botswana GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BOTSWANAGDPYOY";
                /// <summary>
                /// Botswana Interest Rate
                /// </summary>
                public const string InterestRate = "BOTSWANAINTRATE";
                /// <summary>
                /// Botswana Inflation Rate
                /// </summary>
                public const string InflationRate = "BOTSWANAIR";
                /// <summary>
                /// Botswana Mining Production
                /// </summary>
                public const string MiningProduction = "BOTSWANAMINPRO";
            }
            public static class Brazil
            {
                /// <summary>
                /// Brazil Calendar
                /// </summary>
                public const string Calendar = "CALENDARBRAZIL";
                /// <summary>
                /// Brazil Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BRARETAILSALESMOM";
                /// <summary>
                /// Brazil Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BRARETAILSALESYOY";
                /// <summary>
                /// Brazil Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "BRAZILCAPUTI";
                /// <summary>
                /// Brazil Car Production
                /// </summary>
                public const string CarProduction = "BRAZILCARPRO";
                /// <summary>
                /// Brazil Car Registrations
                /// </summary>
                public const string CarRegistrations = "BRAZILCARREG";
                /// <summary>
                /// Brazil Composite Pmi
                /// </summary>
                public const string CompositePmi = "BRAZILCOMPMI";
                /// <summary>
                /// Brazil Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "BRAZILFORDIRINV";
                /// <summary>
                /// Brazil Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "BRAZILGOVBUDVAL";
                /// <summary>
                /// Brazil Government Revenues
                /// </summary>
                public const string GovernmentRevenues = "BRAZILGOVREV";
                /// <summary>
                /// Brazil Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "BRAZILINDPROMOM";
                /// <summary>
                /// Brazil Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "BRAZILINFRATMOM";
                /// <summary>
                /// Brazil Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "BRAZILLEAECOIND";
                /// <summary>
                /// Brazil Loan Growth
                /// </summary>
                public const string LoanGrowth = "BRAZILPRISECCRE";
                /// <summary>
                /// Brazil Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "BRAZILMANPMI";
                /// <summary>
                /// Brazil Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "BRAZILNONFARPAY";
                /// <summary>
                /// Brazil Services Pmi
                /// </summary>
                public const string ServicesPmi = "BRAZILSERPMI";
                /// <summary>
                /// Brazil Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BRLBC";
                /// <summary>
                /// Brazil Current Account
                /// </summary>
                public const string CurrentAccount = "BZCACURR";
                /// <summary>
                /// Brazil Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BZCCI";
                /// <summary>
                /// Brazil Exports
                /// </summary>
                public const string Exports = "BZEXTOTD";
                /// <summary>
                /// Brazil GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BZGDQOQPCT";
                /// <summary>
                /// Brazil GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BZGDYOYPCT";
                /// <summary>
                /// Brazil Imports
                /// </summary>
                public const string Imports = "BZIMTOTD";
                /// <summary>
                /// Brazil Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BZIPYOYPCT";
                /// <summary>
                /// Brazil Inflation Rate
                /// </summary>
                public const string InflationRate = "BZPIIPCY";
                /// <summary>
                /// Brazil Interest Rate
                /// </summary>
                public const string InterestRate = "BZSTSETA";
                /// <summary>
                /// Brazil Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BZTBBALM";
                /// <summary>
                /// Brazil Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "BZUETOTN";
            }
            public static class Brunei
            {
                /// <summary>
                /// Brunei Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BRUUNEIBALRADE";
                /// <summary>
                /// Brunei GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BRUUNEIGDPATE";
                /// <summary>
                /// Brunei Inflation Rate
                /// </summary>
                public const string InflationRate = "BRUUNEIINFNRATE";
            }
            public static class Bulgaria
            {
                /// <summary>
                /// Bulgaria Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BGRRETAILSALESMOM";
                /// <summary>
                /// Bulgaria Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BGRRETAILSALESYOY";
                /// <summary>
                /// Bulgaria Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BULGARIABC";
                /// <summary>
                /// Bulgaria Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BULGARIABT";
                /// <summary>
                /// Bulgaria Current Account
                /// </summary>
                public const string CurrentAccount = "BULGARIACA";
                /// <summary>
                /// Bulgaria Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BULGARIACC";
                /// <summary>
                /// Bulgaria Construction Output
                /// </summary>
                public const string ConstructionOutput = "BULGARIACONOUT";
                /// <summary>
                /// Bulgaria GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BULGARIAGDPQOQ";
                /// <summary>
                /// Bulgaria GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BULGARIAGDPYOY";
                /// <summary>
                /// Bulgaria Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "BULGARIAINDPROMOM";
                /// <summary>
                /// Bulgaria Interest Rate
                /// </summary>
                public const string InterestRate = "BULGARIAINTRATE";
                /// <summary>
                /// Bulgaria Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BULGARIAIP";
                /// <summary>
                /// Bulgaria Inflation Rate
                /// </summary>
                public const string InflationRate = "BULGARIAIR";
                /// <summary>
                /// Bulgaria Producer Prices
                /// </summary>
                public const string ProducerPrices = "BULGARIAPROPRI";
                /// <summary>
                /// Bulgaria Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "BULGARIAPROPRICHA";
                /// <summary>
                /// Bulgaria Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "BULGARIATOUARR";
                /// <summary>
                /// Bulgaria Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "BULGARIAUR";
            }
            public static class Burundi
            {
                /// <summary>
                /// Burundi Inflation Rate
                /// </summary>
                public const string InflationRate = "BURUNDIINFNRATE";
            }
            public static class Canada
            {
                /// <summary>
                /// Canada Inflation Rate
                /// </summary>
                public const string InflationRate = "CACPIYOY";
                /// <summary>
                /// Canada Current Account
                /// </summary>
                public const string CurrentAccount = "CACURENT";
                /// <summary>
                /// Canada Calendar
                /// </summary>
                public const string Calendar = "CALENDARCANADA";
                /// <summary>
                /// Canada Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "CANADAADPEMPCHA";
                /// <summary>
                /// Canada Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "CANADAAVEHOUEAR";
                /// <summary>
                /// Canada Building Permits
                /// </summary>
                public const string BuildingPermits = "CANADABUIPER";
                /// <summary>
                /// Canada Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "CANADACAPUTI";
                /// <summary>
                /// Canada Car Registrations
                /// </summary>
                public const string CarRegistrations = "CANADACARREG";
                /// <summary>
                /// Canada Employment Change
                /// </summary>
                public const string EmploymentChange = "CANADAEMPCHA";
                /// <summary>
                /// Canada Foreign Stock Investment
                /// </summary>
                public const string ForeignStockInvestment = "CANADAFORSTOINV";
                /// <summary>
                /// Canada Full Time Employment
                /// </summary>
                public const string FullTimeEmployment = "CANADAFULTIMEMP";
                /// <summary>
                /// Canada GDP Deflator
                /// </summary>
                public const string GdpDeflator = "CANADAGDPDEF";
                /// <summary>
                /// Canada Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "CANADAGDPGROANN";
                /// <summary>
                /// Canada Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "CANADAGOVBUDVAL";
                /// <summary>
                /// Canada Housing Index
                /// </summary>
                public const string HousingIndex = "CANADAHOUIND";
                /// <summary>
                /// Canada Housing Starts
                /// </summary>
                public const string HousingStarts = "CANADAHOUSTA";
                /// <summary>
                /// Canada Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CANADAINFRATMOM";
                /// <summary>
                /// Canada Job Vacancies
                /// </summary>
                public const string JobVacancies = "CANADAJOBVAC";
                /// <summary>
                /// Canada Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "LABOR-FORCE-PARTICIPATION-RATECANADA";
                /// <summary>
                /// Canada Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "CANADALEAECOIND";
                /// <summary>
                /// Canada Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "CANADAMANPMI";
                /// <summary>
                /// Canada Part Time Employment
                /// </summary>
                public const string PartTimeEmployment = "CANADAPARTIMEMP";
                /// <summary>
                /// Canada Productivity
                /// </summary>
                public const string Productivity = "CANADAPRO";
                /// <summary>
                /// Canada Producer Prices
                /// </summary>
                public const string ProducerPrices = "CANADAPROPRI";
                /// <summary>
                /// Canada Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CANADAPROPRICHA";
                /// <summary>
                /// Canada Retail Sales Ex Autos
                /// </summary>
                public const string RetailSalesExAutos = "RETAIL-SALES-EX-AUTOSCANADA";
                /// <summary>
                /// Canada Wage Growth
                /// </summary>
                public const string WageGrowth = "CANADAWAGGRO";
                /// <summary>
                /// Canada Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "CANCORECPIRATE";
                /// <summary>
                /// Canada Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CANLXEMR";
                /// <summary>
                /// Canada Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CANNADARETSYOY";
                /// <summary>
                /// Canada Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CANRETAILSALESMOM";
                /// <summary>
                /// Canada Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CATBTOTB";
                /// <summary>
                /// Canada Exports
                /// </summary>
                public const string Exports = "CATBTOTE";
                /// <summary>
                /// Canada Imports
                /// </summary>
                public const string Imports = "CATBTOTI";
                /// <summary>
                /// Canada Interest Rate
                /// </summary>
                public const string InterestRate = "CCLR";
                /// <summary>
                /// Canada GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CGE9QOQ";
                /// <summary>
                /// Canada GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CGE9YOY";
                /// <summary>
                /// Canada Business Confidence
                /// </summary>
                public const string BusinessConfidence = "IVEY";
            }
            public static class CapeVerde
            {
                /// <summary>
                /// CapeVerde Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CAPRDEBUSDENCE";
                /// <summary>
                /// CapeVerde Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CAPRDECONDENCE";
                /// <summary>
                /// CapeVerde GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CAPRDEGDPATE";
                /// <summary>
                /// CapeVerde Inflation Rate
                /// </summary>
                public const string InflationRate = "CAPRDEINFNRATE";
            }
            public static class Chile
            {
                /// <summary>
                /// Chile Copper Production
                /// </summary>
                public const string CopperProduction = "CHILECOPPRO";
                /// <summary>
                /// Chile Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "CHILECORCONPRI";
                /// <summary>
                /// Chile Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CHILEINFRATMOM";
                /// <summary>
                /// Chile Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "CHILELEAECOIND";
                /// <summary>
                /// Chile Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "CHILEMANPRO";
                /// <summary>
                /// Chile Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CHILEPROPRICHA";
                /// <summary>
                /// Chile Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CHIPYOY";
                /// <summary>
                /// Chile Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CHLRETAILSALESMOM";
                /// <summary>
                /// Chile Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CHLRETAILSALESYOY";
                /// <summary>
                /// Chile Interest Rate
                /// </summary>
                public const string InterestRate = "CHOVCHOV";
                /// <summary>
                /// Chile Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CHTBBALM";
                /// <summary>
                /// Chile Exports
                /// </summary>
                public const string Exports = "CHTBEXPM";
                /// <summary>
                /// Chile Imports
                /// </summary>
                public const string Imports = "CHTBIMPM";
                /// <summary>
                /// Chile Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CHUETOTL";
                /// <summary>
                /// Chile Current Account
                /// </summary>
                public const string CurrentAccount = "CLBPCURA";
                /// <summary>
                /// Chile GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CLGDPNAPCT";
                /// <summary>
                /// Chile GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CLGDQSA";
                /// <summary>
                /// Chile Inflation Rate
                /// </summary>
                public const string InflationRate = "CNPINSYO";
            }
            public static class China
            {
                /// <summary>
                /// China Calendar
                /// </summary>
                public const string Calendar = "CHN-CALENDAR";
                /// <summary>
                /// China Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CHBUBCIN";
                /// <summary>
                /// China Current Account
                /// </summary>
                public const string CurrentAccount = "CHCURENT";
                /// <summary>
                /// China Banks Balance Sheet
                /// </summary>
                public const string BanksBalanceSheet = "CHINABANBALSHE";
                /// <summary>
                /// China Capital Flows
                /// </summary>
                public const string CapitalFlows = "CHINACAPFLO";
                /// <summary>
                /// China Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "CHINACAPUTI";
                /// <summary>
                /// China Composite Pmi
                /// </summary>
                public const string CompositePmi = "CHINACOMPMI";
                /// <summary>
                /// China Corporate Profits
                /// </summary>
                public const string CorporateProfits = "CHINACORPRO";
                /// <summary>
                /// China Fixed Asset Investment
                /// </summary>
                public const string FixedAssetInvestment = "CHINAFIXASSINV";
                /// <summary>
                /// China Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "CHINAFORDIRINV";
                /// <summary>
                /// China Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "CHINAFOREXCRES";
                /// <summary>
                /// China Housing Index
                /// </summary>
                public const string HousingIndex = "CHINAHOUIND";
                /// <summary>
                /// China Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CHINAINFRATMOM";
                /// <summary>
                /// China Loan Growth
                /// </summary>
                public const string LoanGrowth = "CHINALOAGRO";
                /// <summary>
                /// China Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "CHINALOATOPRISEC";
                /// <summary>
                /// China Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "CHINAMANPMI";
                /// <summary>
                /// China Mni Business Sentiment
                /// </summary>
                public const string MniBusinessSentiment = "CHINAMNIBUSSEN";
                /// <summary>
                /// China Mni Consumer Sentiment
                /// </summary>
                public const string MniConsumerSentiment = "CHINAMNICONSEN";
                /// <summary>
                /// China Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "CHINAMONSUPM2";
                /// <summary>
                /// China Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "CHINANONMANPMI";
                /// <summary>
                /// China Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CHINAPROPRICHA";
                /// <summary>
                /// China Services Pmi
                /// </summary>
                public const string ServicesPmi = "CHINASERPMI";
                /// <summary>
                /// China Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "CHINATOTVEHSAL";
                /// <summary>
                /// China Interest Rate
                /// </summary>
                public const string InterestRate = "CHLR12M";
                /// <summary>
                /// China Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CHNRETAILSALESYOY";
                /// <summary>
                /// China Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CHVAIOY";
                /// <summary>
                /// China Inflation Rate
                /// </summary>
                public const string InflationRate = "CNCPIYOY";
                /// <summary>
                /// China Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CNFRBALD";
                /// <summary>
                /// China Exports
                /// </summary>
                public const string Exports = "CNFREXPD";
                /// <summary>
                /// China Imports
                /// </summary>
                public const string Imports = "CNFRIMPD";
                /// <summary>
                /// China GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CNGDPQOQ";
                /// <summary>
                /// China GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CNGDPYOY";
            }
            public static class Colombia
            {
                /// <summary>
                /// Colombia Current Account
                /// </summary>
                public const string CurrentAccount = "COBPCURR";
                /// <summary>
                /// Colombia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "COCIPIBQ";
                /// <summary>
                /// Colombia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "COCIPIBY";
                /// <summary>
                /// Colombia Inflation Rate
                /// </summary>
                public const string InflationRate = "COCPIYOY";
                /// <summary>
                /// Colombia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "COIPEYOY";
                /// <summary>
                /// Colombia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "COLMBIABUSDENCE";
                /// <summary>
                /// Colombia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "COLMBIACONDENCE";
                /// <summary>
                /// Colombia Cement Production
                /// </summary>
                public const string CementProduction = "COLOMBIACEMPRO";
                /// <summary>
                /// Colombia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "COLOMBIAINFRATMOM";
                /// <summary>
                /// Colombia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "COLOMBIALEAECOIND";
                /// <summary>
                /// Colombia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "COLOMBIAMANPMI";
                /// <summary>
                /// Colombia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "COLOMBIAPROPRICHA";
                /// <summary>
                /// Colombia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "COLRETAILSALESMOM";
                /// <summary>
                /// Colombia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "COLRETAILSALESYOY";
                /// <summary>
                /// Colombia Interest Rate
                /// </summary>
                public const string InterestRate = "CORRRMIN";
                /// <summary>
                /// Colombia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "COTRBALM";
                /// <summary>
                /// Colombia Exports
                /// </summary>
                public const string Exports = "COTREXPM";
                /// <summary>
                /// Colombia Imports
                /// </summary>
                public const string Imports = "COTRIMPM";
                /// <summary>
                /// Colombia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "COUNTOTR";
            }
            public static class CostaRica
            {
                /// <summary>
                /// CostaRica Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "COSICABALRADE";
                /// <summary>
                /// CostaRica GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "COSICAGDPATE";
                /// <summary>
                /// CostaRica Inflation Rate
                /// </summary>
                public const string InflationRate = "COSICAINFNRATE";
                /// <summary>
                /// CostaRica Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "COSICAUNETRATE";
                /// <summary>
                /// CostaRica Current Account
                /// </summary>
                public const string CurrentAccount = "COSTARICACURACC";
                /// <summary>
                /// CostaRica Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "COSTARICAGDPGRORAT";
                /// <summary>
                /// CostaRica Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "COSTARICAINFRATMOM";
            }
            public static class Croatia
            {
                /// <summary>
                /// Croatia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CROATIABALRADE";
                /// <summary>
                /// Croatia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CROATIABUSCON";
                /// <summary>
                /// Croatia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CROATIACONCON";
                /// <summary>
                /// Croatia Current Account
                /// </summary>
                public const string CurrentAccount = "CROATIACURCOUNT";
                /// <summary>
                /// Croatia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CROATIAGDPATE";
                /// <summary>
                /// Croatia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CROATIAGDPRATE";
                /// <summary>
                /// Croatia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CROATIAINDCTION";
                /// <summary>
                /// Croatia Inflation Rate
                /// </summary>
                public const string InflationRate = "CROATIAINFNRATE";
                /// <summary>
                /// Croatia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CROATIAINFRATMOM";
                /// <summary>
                /// Croatia Producer Prices
                /// </summary>
                public const string ProducerPrices = "CROATIAPROPRI";
                /// <summary>
                /// Croatia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CROATIAPROPRICHA";
                /// <summary>
                /// Croatia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CROATIARETSMOM";
                /// <summary>
                /// Croatia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CROATIARETSYOY";
                /// <summary>
                /// Croatia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CROATIAUNETRATE";
                /// <summary>
                /// Croatia Calendar
                /// </summary>
                public const string Calendar = "HRV-CALENDAR";
            }
            public static class Cyprus
            {
                /// <summary>
                /// Cyprus Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CYPPRUSBALRADE";
                /// <summary>
                /// Cyprus Current Account
                /// </summary>
                public const string CurrentAccount = "CYPPRUSCURCOUNT";
                /// <summary>
                /// Cyprus GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CYPPRUSGDPATE";
                /// <summary>
                /// Cyprus GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CYPPRUSGDPRATE";
                /// <summary>
                /// Cyprus Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CYPPRUSINDCTION";
                /// <summary>
                /// Cyprus Inflation Rate
                /// </summary>
                public const string InflationRate = "CYPPRUSINFNRATE";
                /// <summary>
                /// Cyprus Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CYPPRUSRETSYOY";
                /// <summary>
                /// Cyprus Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CYPPRUSUNETRATE";
                /// <summary>
                /// Cyprus Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CYPRUSBUSCON";
                /// <summary>
                /// Cyprus Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CYPRUSCONCON";
                /// <summary>
                /// Cyprus Construction Output
                /// </summary>
                public const string ConstructionOutput = "CYPRUSCONOUT";
                /// <summary>
                /// Cyprus Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "CYPRUSHARCONPRI";
                /// <summary>
                /// Cyprus Wage Growth
                /// </summary>
                public const string WageGrowth = "CYPRUSWAGGRO";
            }
            public static class CzechRepublic
            {
                /// <summary>
                /// CzechRepublic Calendar
                /// </summary>
                public const string Calendar = "CZE-CALENDAR";
                /// <summary>
                /// CzechRepublic Interest Rate
                /// </summary>
                public const string InterestRate = "CZBRREPO";
                /// <summary>
                /// CzechRepublic Current Account
                /// </summary>
                public const string CurrentAccount = "CZCAEUR";
                /// <summary>
                /// CzechRepublic Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CZCCBUS";
                /// <summary>
                /// CzechRepublic Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CZCCCON";
                /// <summary>
                /// CzechRepublic Inflation Rate
                /// </summary>
                public const string InflationRate = "CZCPYOY";
                /// <summary>
                /// CzechRepublic Construction Output
                /// </summary>
                public const string ConstructionOutput = "CZECHREPUCONOUT";
                /// <summary>
                /// CzechRepublic External Debt
                /// </summary>
                public const string ExternalDebt = "CZECHREPUEXTDEB";
                /// <summary>
                /// CzechRepublic Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "CZECHREPUFOREXCRES";
                /// <summary>
                /// CzechRepublic Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "CZECHREPUINDPROMOM";
                /// <summary>
                /// CzechRepublic Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CZECHREPUINFRATMOM";
                /// <summary>
                /// CzechRepublic Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "CZECHREPUMANPMI";
                /// <summary>
                /// CzechRepublic Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "CZECHREPUMONSUPM2";
                /// <summary>
                /// CzechRepublic Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "CZECHREPUMONSUPM3";
                /// <summary>
                /// CzechRepublic Producer Prices
                /// </summary>
                public const string ProducerPrices = "CZECHREPUPROPRI";
                /// <summary>
                /// CzechRepublic Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CZECHREPUPROPRICHA";
                /// <summary>
                /// CzechRepublic Wage Growth
                /// </summary>
                public const string WageGrowth = "CZECHREPUWAGGRO";
                /// <summary>
                /// CzechRepublic Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CZERETAILSALESMOM";
                /// <summary>
                /// CzechRepublic Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CZERETAILSALESYOY";
                /// <summary>
                /// CzechRepublic GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CZGDPQOQ";
                /// <summary>
                /// CzechRepublic GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CZGDPYOY";
                /// <summary>
                /// CzechRepublic Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CZIPITYY";
                /// <summary>
                /// CzechRepublic Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CZJLR";
                /// <summary>
                /// CzechRepublic Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CZTBAL";
                /// <summary>
                /// CzechRepublic Exports
                /// </summary>
                public const string Exports = "CZTBEX";
                /// <summary>
                /// CzechRepublic Imports
                /// </summary>
                public const string Imports = "CZTBIM";
            }
            public static class Denmark
            {
                /// <summary>
                /// Denmark Calendar
                /// </summary>
                public const string Calendar = "CALENDARDENMARK";
                /// <summary>
                /// Denmark Current Account
                /// </summary>
                public const string CurrentAccount = "DECA";
                /// <summary>
                /// Denmark GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "DEGDPNQQ";
                /// <summary>
                /// Denmark GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "DEGDPNYY";
                /// <summary>
                /// Denmark Industrial Production
                /// </summary>
                public const string IndustrialProduction = "DEMFSAYY";
                /// <summary>
                /// Denmark Business Confidence
                /// </summary>
                public const string BusinessConfidence = "DENMARKBC";
                /// <summary>
                /// Denmark Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "DENMARKCC";
                /// <summary>
                /// Denmark Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "DENMARKFOREXCRES";
                /// <summary>
                /// Denmark Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "DENMARKHARCONPRI";
                /// <summary>
                /// Denmark Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "DNK-CALENDAR";
                /// <summary>
                /// Denmark Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "DENMARKMANPMI";
                /// <summary>
                /// Denmark Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "DETBHAL";
                /// <summary>
                /// Denmark Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "DEUE";
                /// <summary>
                /// Denmark Inflation Rate
                /// </summary>
                public const string InflationRate = "DNCPIYOY";
                /// <summary>
                /// Denmark Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "DNKRETAILSALESMOM";
                /// <summary>
                /// Denmark Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "DNKRETAILSALESYOY";
            }
            public static class DominicanRepublic
            {
                /// <summary>
                /// DominicanRepublic Inflation Rate
                /// </summary>
                public const string InflationRate = "DOMLICINFNRATE";
                /// <summary>
                /// DominicanRepublic Interest Rate
                /// </summary>
                public const string InterestRate = "DOMLICINTTRATE";
            }
            public static class EastTimor
            {
                /// <summary>
                /// EastTimor Inflation Rate
                /// </summary>
                public const string InflationRate = "TIMIMORINFNRATE";
            }
            public static class Ecuador
            {
                /// <summary>
                /// Ecuador Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ECUADORBALRADE";
                /// <summary>
                /// Ecuador Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ECUADORCONDENCE";
                /// <summary>
                /// Ecuador GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ECUADORGDPATE";
                /// <summary>
                /// Ecuador GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ECUADORGDPRATE";
                /// <summary>
                /// Ecuador Inflation Rate
                /// </summary>
                public const string InflationRate = "ECUADORINFNRATE";
            }
            public static class Egypt
            {
                /// <summary>
                /// Egypt Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "EGYPTCORINFRAT";
                /// <summary>
                /// Egypt Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "EGYPTFOREXCRES";
                /// <summary>
                /// Egypt Interest Rate
                /// </summary>
                public const string InterestRate = "EGYPTINTRATE";
                /// <summary>
                /// Egypt Inflation Rate
                /// </summary>
                public const string InflationRate = "EGYPTIR";
                /// <summary>
                /// Egypt Lending Rate
                /// </summary>
                public const string LendingRate = "EGYPTLENRAT";
                /// <summary>
                /// Egypt Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "EGYPTMANPMI";
                /// <summary>
                /// Egypt Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "EGYPTMONSUPM2";
                /// <summary>
                /// Egypt Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "EGYPTUR";
            }
            public static class ElSalvador
            {
                /// <summary>
                /// ElSalvador GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ELSALVADOGDPGRORAT";
                /// <summary>
                /// ElSalvador Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ELSALVADOINDPRO";
                /// <summary>
                /// ElSalvador Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ELSDORBALRADE";
                /// <summary>
                /// ElSalvador Current Account
                /// </summary>
                public const string CurrentAccount = "ELSDORCURCOUNT";
                /// <summary>
                /// ElSalvador GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ELSDORGDPATE";
                /// <summary>
                /// ElSalvador Inflation Rate
                /// </summary>
                public const string InflationRate = "ELSDORINFNRATE";
            }
            public static class Estonia
            {
                /// <summary>
                /// Estonia Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ESTONIAINDPROMOM";
                /// <summary>
                /// Estonia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ESTONIABT";
                /// <summary>
                /// Estonia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ESTONIABUSCON";
                /// <summary>
                /// Estonia Current Account
                /// </summary>
                public const string CurrentAccount = "ESTONIACA";
                /// <summary>
                /// Estonia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ESTONIAGDPQOQ";
                /// <summary>
                /// Estonia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ESTONIAGDPYOY";
                /// <summary>
                /// Estonia Imports
                /// </summary>
                public const string Imports = "ESTONIAIM";
                /// <summary>
                /// Estonia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ESTONIAINFRATMOM";
                /// <summary>
                /// Estonia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ESTONIAIP";
                /// <summary>
                /// Estonia Inflation Rate
                /// </summary>
                public const string InflationRate = "ESTONIAIR";
                /// <summary>
                /// Estonia Producer Prices
                /// </summary>
                public const string ProducerPrices = "ESTONIAPROPRI";
                /// <summary>
                /// Estonia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ESTONIAPROPRICHA";
                /// <summary>
                /// Estonia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ESTONIAUR";
                /// <summary>
                /// Estonia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ESTRETAILSALESMOM";
                /// <summary>
                /// Estonia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ESTRETAILSALESYOY";
            }
            public static class Ethiopia
            {
                /// <summary>
                /// Ethiopia Inflation Rate
                /// </summary>
                public const string InflationRate = "ETHOPIAINFNRATE";
            }
            public static class EuroArea
            {
                /// <summary>
                /// EuroArea Calendar
                /// </summary>
                public const string Calendar = "EUR-CALENDAR";
                /// <summary>
                /// EuroArea Consumer Confidence Price Trends
                /// </summary>
                public const string ConsumerConfidencePriceTrends = "CC141";
                /// <summary>
                /// EuroArea Inflation Rate
                /// </summary>
                public const string InflationRate = "ECCPEMUY";
                /// <summary>
                /// EuroArea Government Debt to GDP
                /// </summary>
                public const string GovernmentDebtToGdp = "EMUDEBT2GDP";
                /// <summary>
                /// EuroArea Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "EMUEVOLVLOATOPRISEC";
                /// <summary>
                /// EuroArea Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "EMUEVOLVMONSUPM3";
                /// <summary>
                /// EuroArea Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "EMURETAILSALESMOM";
                /// <summary>
                /// EuroArea Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "EMURETAILSALESYOY";
                /// <summary>
                /// EuroArea Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "EUCCEMU";
                /// <summary>
                /// EuroArea Current Account
                /// </summary>
                public const string CurrentAccount = "EUCQCA11";
                /// <summary>
                /// EuroArea Business Confidence
                /// </summary>
                public const string BusinessConfidence = "EUESEMU";
                /// <summary>
                /// EuroArea GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "EUGNEMUQ";
                /// <summary>
                /// EuroArea GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "EUGNEMUY";
                /// <summary>
                /// EuroArea Industrial Production
                /// </summary>
                public const string IndustrialProduction = "EUIPEMUY";
                /// <summary>
                /// EuroArea Composite Pmi
                /// </summary>
                public const string CompositePmi = "EUROAREACOMPMI";
                /// <summary>
                /// EuroArea Construction Output
                /// </summary>
                public const string ConstructionOutput = "EUROAREACONOUT";
                /// <summary>
                /// EuroArea Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "EUROAREACORINFRAT";
                /// <summary>
                /// EuroArea Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "EUROAREADEPINTRAT";
                /// <summary>
                /// EuroArea Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "EUROAREAECOOPTIND";
                /// <summary>
                /// EuroArea Employment Change
                /// </summary>
                public const string EmploymentChange = "EUROAREAEMPCHA";
                /// <summary>
                /// EuroArea Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "EUROAREAINDPROMOM";
                /// <summary>
                /// EuroArea Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "EUROAREAINFRATMOM";
                /// <summary>
                /// EuroArea Labour Costs
                /// </summary>
                public const string LabourCosts = "EUROAREALABCOS";
                /// <summary>
                /// EuroArea Lending Rate
                /// </summary>
                public const string LendingRate = "EUROAREALENRAT";
                /// <summary>
                /// EuroArea Loan Growth
                /// </summary>
                public const string LoanGrowth = "EUROAREALOAGRO";
                /// <summary>
                /// EuroArea Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "EUROAREAMANPMI";
                /// <summary>
                /// EuroArea Producer Prices
                /// </summary>
                public const string ProducerPrices = "EUROAREAPROPRI";
                /// <summary>
                /// EuroArea Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "EUROAREAPROPRICHA";
                /// <summary>
                /// EuroArea Services Pmi
                /// </summary>
                public const string ServicesPmi = "EUROAREASERPMI";
                /// <summary>
                /// EuroArea Wage Growth
                /// </summary>
                public const string WageGrowth = "EUROAREAWAGGRO";
                /// <summary>
                /// EuroArea Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "EUROAREAZEWECOSENIND";
                /// <summary>
                /// EuroArea Interest Rate
                /// </summary>
                public const string InterestRate = "EURR002W";
                /// <summary>
                /// EuroArea Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTEMU";
                /// <summary>
                /// EuroArea Government Budget
                /// </summary>
                public const string GovernmentBudget = "WDEBEURO";
                /// <summary>
                /// EuroArea Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "XTTBEZ";
            }
            public static class Fiji
            {
                /// <summary>
                /// Fiji Inflation Rate
                /// </summary>
                public const string InflationRate = "FIJFIJIINFNRATE";
                /// <summary>
                /// Fiji Interest Rate
                /// </summary>
                public const string InterestRate = "FIJFIJIINTTRATE";
            }
            public static class Finland
            {
                /// <summary>
                /// Finland Calendar
                /// </summary>
                public const string Calendar = "FIN-CALENDAR";
                /// <summary>
                /// Finland Current Account
                /// </summary>
                public const string CurrentAccount = "FICAEUR";
                /// <summary>
                /// Finland Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "FICCI";
                /// <summary>
                /// Finland Inflation Rate
                /// </summary>
                public const string InflationRate = "FICP2YOY";
                /// <summary>
                /// Finland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "FIGDPCH";
                /// <summary>
                /// Finland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "FIGDPYOY";
                /// <summary>
                /// Finland Industrial Production
                /// </summary>
                public const string IndustrialProduction = "FIIPYOY";
                /// <summary>
                /// Finland Business Confidence
                /// </summary>
                public const string BusinessConfidence = "FINLANDBUSCON";
                /// <summary>
                /// Finland Export Prices
                /// </summary>
                public const string ExportPrices = "FINLANDEXPPRI";
                /// <summary>
                /// Finland Import Prices
                /// </summary>
                public const string ImportPrices = "FINLANDIMPPRI";
                /// <summary>
                /// Finland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "FINLANDINFRATMOM";
                /// <summary>
                /// Finland Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "FINLANDLEAECOIND";
                /// <summary>
                /// Finland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "FINLANDPROPRICHA";
                /// <summary>
                /// Finland Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "FINLANDRETSALMOM";
                /// <summary>
                /// Finland Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "FINLANDRETSALYOY";
                /// <summary>
                /// Finland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "FITBAL";
                /// <summary>
                /// Finland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTFI";
            }
            public static class France
            {
                /// <summary>
                /// France Calendar
                /// </summary>
                public const string Calendar = "FRAINDICATORS";
                /// <summary>
                /// France Industrial Production
                /// </summary>
                public const string IndustrialProduction = "FPIPYOY";
                /// <summary>
                /// France 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "FRANCE3MONBILYIE";
                /// <summary>
                /// France 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "FRANCE3YNY";
                /// <summary>
                /// France 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "FRANCE5WBY";
                /// <summary>
                /// France 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "FRANCE5YNY";
                /// <summary>
                /// France 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "FRANCE6MONBILYIE";
                /// <summary>
                /// France Composite Pmi
                /// </summary>
                public const string CompositePmi = "FRANCECOMPMI";
                /// <summary>
                /// France Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "FRANCEGOVBUDVAL";
                /// <summary>
                /// France Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "FRANCEHARCONPRI";
                /// <summary>
                /// France Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "FRANCEINDPROMOM";
                /// <summary>
                /// France Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "FRANCEINFRATMOM";
                /// <summary>
                /// France Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "FRANCEINIJOBCLA";
                /// <summary>
                /// France Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "FRANCEMANPMI";
                /// <summary>
                /// France Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "FRANCENONFARPAY";
                /// <summary>
                /// France Nonfarm Payrolls Private
                /// </summary>
                public const string NonfarmPayrollsPrivate = "FRANCENONPAYPRI";
                /// <summary>
                /// France Personal Spending
                /// </summary>
                public const string PersonalSpending = "FRANCEPERSPE";
                /// <summary>
                /// France Producer Prices
                /// </summary>
                public const string ProducerPrices = "FRANCEPROPRI";
                /// <summary>
                /// France Services Pmi
                /// </summary>
                public const string ServicesPmi = "FRANCESERPMI";
                /// <summary>
                /// France Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "FRANCEUNEPER";
                /// <summary>
                /// France Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "FRARETAILSALESMOM";
                /// <summary>
                /// France Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "FRARETAILSALESYOY";
                /// <summary>
                /// France Current Account
                /// </summary>
                public const string CurrentAccount = "FRCAEURO";
                /// <summary>
                /// France Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "FRCCI";
                /// <summary>
                /// France Inflation Rate
                /// </summary>
                public const string InflationRate = "FRCPIYOY";
                /// <summary>
                /// France GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "FRGEGDPQ";
                /// <summary>
                /// France GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "FRGEGDPY";
                /// <summary>
                /// France Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "FRTEBAL";
                /// <summary>
                /// France Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GFRN10";
                /// <summary>
                /// France Business Confidence
                /// </summary>
                public const string BusinessConfidence = "INSESYNT";
                /// <summary>
                /// France Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTFR";
            }
            public static class Georgia
            {
                /// <summary>
                /// Georgia Balance of Trade
                /// </summary>BasicTemplateFrameworkAlgorithm
                public const string BalanceOfTrade = "GEORGIABALRADE";
                /// <summary>
                /// Georgia Current Account
                /// </summary>
                public const string CurrentAccount = "GEORGIACURCOUNT";
                /// <summary>
                /// Georgia Current Account to GDP
                /// </summary>
                public const string CurrentAccountToGdp = "GEORGIACURGDP";
                /// <summary>
                /// Georgia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GEORGIAGDPATE";
                /// <summary>
                /// Georgia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GEORGIAINDCTION";
                /// <summary>
                /// Georgia Inflation Rate
                /// </summary>
                public const string InflationRate = "GEORGIAINFNRATE";
                /// <summary>
                /// Georgia Interest Rate
                /// </summary>
                public const string InterestRate = "GEORGIAINTTRATE";
                /// <summary>
                /// Georgia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "GEORGIALEAECOIND";
                /// <summary>
                /// Georgia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GEORGIAPROPRICHA";
                /// <summary>
                /// Georgia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "GEORGIARETSYOY";
                /// <summary>
                /// Georgia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "GEORGIAUNETRATE";
            }
            public static class Germany
            {
                /// <summary>
                /// Germany Calendar
                /// </summary>
                public const string Calendar = "DEUINDICATORS";
                /// <summary>
                /// Germany Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "DEURETAILSALESMOM";
                /// <summary>
                /// Germany Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "DEURETAILSALESYOY";
                /// <summary>
                /// Germany Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GDBR10";
                /// <summary>
                /// Germany 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "GERMANY2YNY";
                /// <summary>
                /// Germany 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "GERMANY3YBY";
                /// <summary>
                /// Germany 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "GERMANY5WBY";
                /// <summary>
                /// Germany 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "GERMANY5YNY";
                /// <summary>
                /// Germany Composite Pmi
                /// </summary>
                public const string CompositePmi = "GERMANYCOMPMI";
                /// <summary>
                /// Germany Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "GERMANYCONPMI";
                /// <summary>
                /// Germany Employed Persons
                /// </summary>
                public const string EmployedPersons = "GERMANYEMPPER";
                /// <summary>
                /// Germany Factory Orders
                /// </summary>
                public const string FactoryOrders = "GERMANYFACORD";
                /// <summary>
                /// Germany Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "GERMANYHARCONPRI";
                /// <summary>
                /// Germany Import Prices
                /// </summary>
                public const string ImportPrices = "GERMANYIMPPRI";
                /// <summary>
                /// Germany Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "GERMANYINDPROMOM";
                /// <summary>
                /// Germany Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "GERMANYINFRATMOM";
                /// <summary>
                /// Germany Job Vacancies
                /// </summary>
                public const string JobVacancies = "GERMANYJOBVAC";
                /// <summary>
                /// Germany Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "GERMANYMANPMI";
                /// <summary>
                /// Germany Producer Prices
                /// </summary>
                public const string ProducerPrices = "GERMANYPROPRI";
                /// <summary>
                /// Germany Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GERMANYPROPRICHA";
                /// <summary>
                /// Germany Services Pmi
                /// </summary>
                public const string ServicesPmi = "GERMANYSERPMI";
                /// <summary>
                /// Germany Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "GERMANYUNECHA";
                /// <summary>
                /// Germany Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "GERMANYUNEPER";
                /// <summary>
                /// Germany Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "GERMANYWHOPRI";
                /// <summary>
                /// Germany Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "GERMANYZEWECOSENIND";
                /// <summary>
                /// Germany Inflation Rate
                /// </summary>
                public const string InflationRate = "GRBC20YY";
                /// <summary>
                /// Germany Current Account
                /// </summary>
                public const string CurrentAccount = "GRCAEU";
                /// <summary>
                /// Germany Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GRCCI";
                /// <summary>
                /// Germany GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "GRGDPPGQ";
                /// <summary>
                /// Germany GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GRGDPPGY";
                /// <summary>
                /// Germany Business Confidence
                /// </summary>
                public const string BusinessConfidence = "GRIFPBUS";
                /// <summary>
                /// Germany Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GRIPIYOY";
                /// <summary>
                /// Germany Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "GRTBALE";
                /// <summary>
                /// Germany Exports
                /// </summary>
                public const string Exports = "GRTBEXE";
                /// <summary>
                /// Germany Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "GRUEPR";
                /// <summary>
                /// Germany Government Budget
                /// </summary>
                public const string GovernmentBudget = "WCSDDEU";
            }
            public static class Ghana
            {
                /// <summary>
                /// Ghana Composite Pmi
                /// </summary>
                public const string CompositePmi = "GHANACOMPMI";
                /// <summary>
                /// Ghana GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GHANAGDPYOY";
                /// <summary>
                /// Ghana Interest Rate
                /// </summary>
                public const string InterestRate = "GHANAINTRATE";
                /// <summary>
                /// Ghana Inflation Rate
                /// </summary>
                public const string InflationRate = "GHANAIR";
                /// <summary>
                /// Ghana Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GHANAPROPRICHA";
            }
            public static class Greece
            {
                /// <summary>
                /// Greece Calendar
                /// </summary>
                public const string Calendar = "GRC-CALENDAR";
                /// <summary>
                /// Greece Current Account
                /// </summary>
                public const string CurrentAccount = "GKCAEUR";
                /// <summary>
                /// Greece Inflation Rate
                /// </summary>
                public const string InflationRate = "GKCPNEWY";
                /// <summary>
                /// Greece GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "GKGNVQQ";
                /// <summary>
                /// Greece GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GKGNVYY";
                /// <summary>
                /// Greece Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GKIPIYOY";
                /// <summary>
                /// Greece Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "GKTBALE";
                /// <summary>
                /// Greece Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "GRCRETAILSALESMOM";
                /// <summary>
                /// Greece Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "GRCRETAILSALESYOY";
                /// <summary>
                /// Greece 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "GREECE3MBY";
                /// <summary>
                /// Greece 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "GREECE6MBY";
                /// <summary>
                /// Greece Business Confidence
                /// </summary>
                public const string BusinessConfidence = "GREECEBC";
                /// <summary>
                /// Greece Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GREECECC";
                /// <summary>
                /// Greece Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "GREECEHARCONPRI";
                /// <summary>
                /// Greece Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "GREECEINFRATMOM";
                /// <summary>
                /// Greece Loan Growth
                /// </summary>
                public const string LoanGrowth = "GREECELOAGRO";
                /// <summary>
                /// Greece Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "GREECELOATOPRISEC";
                /// <summary>
                /// Greece Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "GREECEMANPMI";
                /// <summary>
                /// Greece Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "GREECEPRISECCRE";
                /// <summary>
                /// Greece Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GREECEPROPRICHA";
                /// <summary>
                /// Greece Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTGR";
            }
            public static class Guatemala
            {
                /// <summary>
                /// Guatemala Current Account
                /// </summary>
                public const string CurrentAccount = "GUAMALACURCOUNT";
            }
            public static class Honduras
            {
                /// <summary>
                /// Honduras Interest Rate
                /// </summary>
                public const string InterestRate = "HONURASINTTRATE";
            }
            public static class HongKong
            {
                /// <summary>
                /// HongKong Calendar
                /// </summary>
                public const string Calendar = "HKG-CALENDAR";
                /// <summary>
                /// HongKong Interest Rate
                /// </summary>
                public const string InterestRate = "HKBASE";
                /// <summary>
                /// HongKong Current Account
                /// </summary>
                public const string CurrentAccount = "HKBPCA";
                /// <summary>
                /// HongKong Inflation Rate
                /// </summary>
                public const string InflationRate = "HKCPIY";
                /// <summary>
                /// HongKong Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "HKETBOT";
                /// <summary>
                /// HongKong Exports
                /// </summary>
                public const string Exports = "HKETEXP";
                /// <summary>
                /// HongKong Imports
                /// </summary>
                public const string Imports = "HKETIMP";
                /// <summary>
                /// HongKong GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "HKGDQOQ";
                /// <summary>
                /// HongKong GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "HKGDYOY";
                /// <summary>
                /// HongKong Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "HKGRETAILSALESYOY";
                /// <summary>
                /// HongKong Industrial Production
                /// </summary>
                public const string IndustrialProduction = "HKIPIYOY";
                /// <summary>
                /// HongKong Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "HKUERATE";
                /// <summary>
                /// HongKong Business Confidence
                /// </summary>
                public const string BusinessConfidence = "HONGKONGBUSCON";
                /// <summary>
                /// HongKong Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "HONGKONGFOREXCRES";
                /// <summary>
                /// HongKong Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "HONGKONGMANPMI";
            }
            public static class Hungary
            {
                /// <summary>
                /// Hungary GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ENGKHUQ";
                /// <summary>
                /// Hungary Interest Rate
                /// </summary>
                public const string InterestRate = "HBBRATE";
                /// <summary>
                /// Hungary Inflation Rate
                /// </summary>
                public const string InflationRate = "HUCPIYY";
                /// <summary>
                /// Hungary Current Account
                /// </summary>
                public const string CurrentAccount = "HUCQEURO";
                /// <summary>
                /// Hungary Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "HUEMUNR";
                /// <summary>
                /// Hungary GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "HUGPTOTL";
                /// <summary>
                /// Hungary Industrial Production
                /// </summary>
                public const string IndustrialProduction = "HUIPIYOY";
                /// <summary>
                /// Hungary Calendar
                /// </summary>
                public const string Calendar = "HUN-CALENDAR";
                /// <summary>
                /// Hungary Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "HUNCORECPIRATE";
                /// <summary>
                /// Hungary Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "HUNFRINRDPST";
                /// <summary>
                /// Hungary Construction Output
                /// </summary>
                public const string ConstructionOutput = "HUNGARYCONOUT";
                /// <summary>
                /// Hungary Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "HUNGARYGOVBUDVAL";
                /// <summary>
                /// Hungary Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "HUNGARYINFRATMOM";
                /// <summary>
                /// Hungary Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "HUNGARYMANPMI";
                /// <summary>
                /// Hungary Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "HUNGARYPROPRICHA";
                /// <summary>
                /// Hungary Wage Growth
                /// </summary>
                public const string WageGrowth = "HUNGARYWAGGRO";
                /// <summary>
                /// Hungary Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "HUNRETAILSALESMOM";
                /// <summary>
                /// Hungary Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "HUNRETAILSALESYOY";
                /// <summary>
                /// Hungary Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "HUTRBAL";
            }
            public static class Iceland
            {
                /// <summary>
                /// Iceland Calendar
                /// </summary>
                public const string Calendar = "CALENDARICELAND";
                /// <summary>
                /// Iceland Interest Rate
                /// </summary>
                public const string InterestRate = "ICBRANN";
                /// <summary>
                /// Iceland Current Account
                /// </summary>
                public const string CurrentAccount = "ICCACURR";
                /// <summary>
                /// Iceland Inflation Rate
                /// </summary>
                public const string InflationRate = "ICCPIYOY";
                /// <summary>
                /// Iceland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ISL-CALENDAR";
                /// <summary>
                /// Iceland Producer Prices
                /// </summary>
                public const string ProducerPrices = "ICELANDPROPRI";
                /// <summary>
                /// Iceland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ICELANDPROPRICHA";
                /// <summary>
                /// Iceland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ICETBAL";
                /// <summary>
                /// Iceland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ICGPSAQ";
                /// <summary>
                /// Iceland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ICGPSAY";
                /// <summary>
                /// Iceland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ICUNEMRT";
            }
            public static class India
            {
                /// <summary>
                /// India Calendar
                /// </summary>
                public const string Calendar = "IND-CALENDAR";
                /// <summary>
                /// India Current Account
                /// </summary>
                public const string CurrentAccount = "INBQCUR";
                /// <summary>
                /// India Inflation Rate
                /// </summary>
                public const string InflationRate = "INCPIINY";
                /// <summary>
                /// India Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "INDIACASRESRAT";
                /// <summary>
                /// India Construction Output
                /// </summary>
                public const string ConstructionOutput = "INDIACONOUT";
                /// <summary>
                /// India External Debt
                /// </summary>
                public const string ExternalDebt = "INDIAEXTDEB";
                /// <summary>
                /// India Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "INDIAFOREXCRES";
                /// <summary>
                /// India Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "INDIAGOVBUDVAL";
                /// <summary>
                /// India Loan Growth
                /// </summary>
                public const string LoanGrowth = "INDIALOAGRO";
                /// <summary>
                /// India Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "INDIAMANPMI";
                /// <summary>
                /// India Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "INDIAMANPRO";
                /// <summary>
                /// India Mni Consumer Sentiment
                /// </summary>
                public const string MniConsumerSentiment = "INDIAMNICONSEN";
                /// <summary>
                /// India Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "INDIAPROPRICHA";
                /// <summary>
                /// India Services Pmi
                /// </summary>
                public const string ServicesPmi = "INDIASERPMI";
                /// <summary>
                /// India GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "INGDPY";
                /// <summary>
                /// India Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "INMTBALD";
                /// <summary>
                /// India Industrial Production
                /// </summary>
                public const string IndustrialProduction = "INPIINDY";
                /// <summary>
                /// India Interest Rate
                /// </summary>
                public const string InterestRate = "RSPOYLD";
            }
            public static class Indonesia
            {
                /// <summary>
                /// Indonesia Calendar
                /// </summary>
                public const string Calendar = "IDN-CALENDAR";
                /// <summary>
                /// Indonesia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "IDBALTOL";
                /// <summary>
                /// Indonesia Interest Rate
                /// </summary>
                public const string InterestRate = "IDBIRATE";
                /// <summary>
                /// Indonesia Current Account
                /// </summary>
                public const string CurrentAccount = "IDCABAL";
                /// <summary>
                /// Indonesia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "IDCCI";
                /// <summary>
                /// Indonesia Inflation Rate
                /// </summary>
                public const string InflationRate = "IDCPIY";
                /// <summary>
                /// Indonesia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "IDEMUNEPCT";
                /// <summary>
                /// Indonesia Exports
                /// </summary>
                public const string Exports = "IDEXP";
                /// <summary>
                /// Indonesia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "IDGDPY";
                /// <summary>
                /// Indonesia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "IDGDQY";
                /// <summary>
                /// Indonesia Imports
                /// </summary>
                public const string Imports = "IDIMP";
                /// <summary>
                /// Indonesia Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "IDNFRINRDPST";
                /// <summary>
                /// Indonesia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "IDNRETAILSALESMOM";
                /// <summary>
                /// Indonesia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "IDNRETAILSALESYOY";
                /// <summary>
                /// Indonesia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "INDONESIABC";
                /// <summary>
                /// Indonesia Car Registrations
                /// </summary>
                public const string CarRegistrations = "INDONESIACARREG";
                /// <summary>
                /// Indonesia Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "INDONESIACORINFRAT";
                /// <summary>
                /// Indonesia Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "INDONESIAFORDIRINV";
                /// <summary>
                /// Indonesia Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "INDONESIAFOREXCRES";
                /// <summary>
                /// Indonesia Housing Index
                /// </summary>
                public const string HousingIndex = "INDONESIAHOUIND";
                /// <summary>
                /// Indonesia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "INDONESIAINFRATMOM";
                /// <summary>
                /// Indonesia Lending Rate
                /// </summary>
                public const string LendingRate = "INDONESIALENRAT";
                /// <summary>
                /// Indonesia Loan Growth
                /// </summary>
                public const string LoanGrowth = "INDONESIALOAGRO";
                /// <summary>
                /// Indonesia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "INDONESIAMANPMI";
                /// <summary>
                /// Indonesia Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "INDONESIAMONSUPM2";
                /// <summary>
                /// Indonesia Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "INDONESIATOUARR";
            }
            public static class Iran
            {
                /// <summary>
                /// Iran Inflation Rate
                /// </summary>
                public const string InflationRate = "IRAIRANINFNRATE";
            }
            public static class Iraq
            {
                /// <summary>
                /// Iraq Inflation Rate
                /// </summary>
                public const string InflationRate = "IRAIRAQINFNRATE";
            }
            public static class Ireland
            {
                /// <summary>
                /// Ireland Calendar
                /// </summary>
                public const string Calendar = "IRL-CALENDAR";
                /// <summary>
                /// Ireland Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "EUCCIE";
                /// <summary>
                /// Ireland Current Account
                /// </summary>
                public const string CurrentAccount = "IECA";
                /// <summary>
                /// Ireland Inflation Rate
                /// </summary>
                public const string InflationRate = "IECPIYOY";
                /// <summary>
                /// Ireland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "IEGRPQOQ";
                /// <summary>
                /// Ireland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "IEGRPYOY";
                /// <summary>
                /// Ireland Industrial Production
                /// </summary>
                public const string IndustrialProduction = "IEIPIYOY";
                /// <summary>
                /// Ireland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "IETBALNS";
                /// <summary>
                /// Ireland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "IEUERT";
                /// <summary>
                /// Ireland Construction Output
                /// </summary>
                public const string ConstructionOutput = "IRELANDCONOUT";
                /// <summary>
                /// Ireland Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "IRELANDCONPMI";
                /// <summary>
                /// Ireland Gross National Product
                /// </summary>
                public const string GrossNationalProduct = "IRELANDGRONATPRO";
                /// <summary>
                /// Ireland Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "IRELANDHARCONPRI";
                /// <summary>
                /// Ireland Housing Index
                /// </summary>
                public const string HousingIndex = "IRELANDHOUIND";
                /// <summary>
                /// Ireland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "IRELANDINFRATMOM";
                /// <summary>
                /// Ireland Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "IRELANDMANPMI";
                /// <summary>
                /// Ireland Personal Savings
                /// </summary>
                public const string PersonalSavings = "IRELANDPERSAV";
                /// <summary>
                /// Ireland Producer Prices
                /// </summary>
                public const string ProducerPrices = "IRELANDPROPRI";
                /// <summary>
                /// Ireland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "IRELANDPROPRICHA";
                /// <summary>
                /// Ireland Services Pmi
                /// </summary>
                public const string ServicesPmi = "IRELANDSERPMI";
                /// <summary>
                /// Ireland Wage Growth
                /// </summary>
                public const string WageGrowth = "IRELANDWAGGRO";
                /// <summary>
                /// Ireland Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "IRLCORECPIRATE";
                /// <summary>
                /// Ireland Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "IRLRETAILSALESMOM";
                /// <summary>
                /// Ireland Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "IRLRETAILSALESYOY";
            }
            public static class Israel
            {
                /// <summary>
                /// Israel Interest Rate
                /// </summary>
                public const string InterestRate = "ISBRATE";
                /// <summary>
                /// Israel Current Account
                /// </summary>
                public const string CurrentAccount = "ISCAL";
                /// <summary>
                /// Israel Inflation Rate
                /// </summary>
                public const string InflationRate = "ISCPIYYN";
                /// <summary>
                /// Israel GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ISGDPNQQ";
                /// <summary>
                /// Israel GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ISGDPNYY";
                /// <summary>
                /// Israel Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ISINIYOY";
                /// <summary>
                /// Israel Calendar
                /// </summary>
                public const string Calendar = "ISR-CALENDAR";
                /// <summary>
                /// Israel Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ISRAELBC";
                /// <summary>
                /// Israel Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ISRAELCC";
                /// <summary>
                /// Israel Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "ISRAELFOREXCRES";
                /// <summary>
                /// Israel GDP Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "ISRAELGDPGROANN";
                /// <summary>
                /// Israel Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "ISRAELGOVBUDVAL";
                /// <summary>
                /// Israel Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ISRAELINDPROMOM";
                /// <summary>
                /// Israel Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "ISRAELINFEXP";
                /// <summary>
                /// Israel Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ISRAELINFRATMOM";
                /// <summary>
                /// Israel Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "ISRAELLEAECOIND";
                /// <summary>
                /// Israel Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "ISRAELMANPMI";
                /// <summary>
                /// Israel Money Supply M1
                /// </summary>
                public const string MoneySupplyM1 = "ISRAELMONSUPM1";
                /// <summary>
                /// Israel Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "ISRAELTOUARR";
                /// <summary>
                /// Israel Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ISTBAL";
                /// <summary>
                /// Israel Exports
                /// </summary>
                public const string Exports = "ISTBEX";
                /// <summary>
                /// Israel Imports
                /// </summary>
                public const string Imports = "ISTBIM";
                /// <summary>
                /// Israel Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ISUER";
            }
            public static class Italy
            {
                /// <summary>
                /// Italy Calendar
                /// </summary>
                public const string Calendar = "ITAINDICATORS";
                /// <summary>
                /// Italy Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GBTPGR10";
                /// <summary>
                /// Italy 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "ITALY3YBY";
                /// <summary>
                /// Italy 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "ITALY3YNY";
                /// <summary>
                /// Italy 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "ITALY5WBY";
                /// <summary>
                /// Italy 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "ITALY5YNY";
                /// <summary>
                /// Italy 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "ITALY6MBY";
                /// <summary>
                /// Italy 7 Year Note Yield
                /// </summary>
                public const string SevenYearNoteYield = "ITALY7YNY";
                /// <summary>
                /// Italy Construction Output
                /// </summary>
                public const string ConstructionOutput = "ITALYCONOUT";
                /// <summary>
                /// Italy Factory Orders
                /// </summary>
                public const string FactoryOrders = "ITALYFACORD";
                /// <summary>
                /// Italy Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "ITALYHARCONPRI";
                /// <summary>
                /// Italy Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ITALYINDPROMOM";
                /// <summary>
                /// Italy Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ITALYINFRATMOM";
                /// <summary>
                /// Italy Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "ITALYMANPMI";
                /// <summary>
                /// Italy New Orders
                /// </summary>
                public const string NewOrders = "ITALYNEWORD";
                /// <summary>
                /// Italy Producer Prices
                /// </summary>
                public const string ProducerPrices = "ITALYPROPRI";
                /// <summary>
                /// Italy Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ITALYPROPRICHA";
                /// <summary>
                /// Italy Services Pmi
                /// </summary>
                public const string ServicesPmi = "ITALYSERPMI";
                /// <summary>
                /// Italy Wage Growth
                /// </summary>
                public const string WageGrowth = "ITALYWAGGRO";
                /// <summary>
                /// Italy Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ITARETAILSALESMOM";
                /// <summary>
                /// Italy Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ITARETAILSALESYOY";
                /// <summary>
                /// Italy Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ITBCI";
                /// <summary>
                /// Italy Current Account
                /// </summary>
                public const string CurrentAccount = "ITCAEUR";
                /// <summary>
                /// Italy Inflation Rate
                /// </summary>
                public const string InflationRate = "ITCPNICY";
                /// <summary>
                /// Italy GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ITPIRLQS";
                /// <summary>
                /// Italy GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ITPIRLYS";
                /// <summary>
                /// Italy Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ITPRWAY";
                /// <summary>
                /// Italy Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ITPSSA";
                /// <summary>
                /// Italy Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ITTRBSAT";
                /// <summary>
                /// Italy Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTIT";
                /// <summary>
                /// Italy Government Budget
                /// </summary>
                public const string GovernmentBudget = "WCSDITA";
            }
            public static class IvoryCoast
            {
                /// <summary>
                /// IvoryCoast Industrial Production
                /// </summary>
                public const string IndustrialProduction = "COTREINDCTION";
                /// <summary>
                /// IvoryCoast Inflation Rate
                /// </summary>
                public const string InflationRate = "COTREINFNRATE";
            }
            public static class Jamaica
            {
                /// <summary>
                /// Jamaica Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "JAMAICABALRADE";
                /// <summary>
                /// Jamaica Current Account
                /// </summary>
                public const string CurrentAccount = "JAMAICACURCOUNT";
                /// <summary>
                /// Jamaica GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "JAMAICAGDPATE";
                /// <summary>
                /// Jamaica GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "JAMAICAGDPRATE";
                /// <summary>
                /// Jamaica Inflation Rate
                /// </summary>
                public const string InflationRate = "JAMAICAINFNRATE";
                /// <summary>
                /// Jamaica Interest Rate
                /// </summary>
                public const string InterestRate = "JAMAICAINTTRATE";
            }
            public static class Japan
            {
                /// <summary>
                /// Japan Interest Rate
                /// </summary>
                public const string InterestRate = "BOJDTR";
                /// <summary>
                /// Japan Calendar
                /// </summary>
                public const string Calendar = "JPY-CALENDAR";
                /// <summary>
                /// Japan Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GJGB10";
                /// <summary>
                /// Japan 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "JAPAN3YBY";
                /// <summary>
                /// Japan Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "JAPANCAPUTI";
                /// <summary>
                /// Japan Coincident Index
                /// </summary>
                public const string CoincidentIndex = "JAPANCOIIND";
                /// <summary>
                /// Japan Construction Orders
                /// </summary>
                public const string ConstructionOrders = "JAPANCONORD";
                /// <summary>
                /// Japan Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "JAPANCONSPE";
                /// <summary>
                /// Japan Economy Watchers Survey
                /// </summary>
                public const string EconomyWatchersSurvey = "JAPANECOWATSUR";
                /// <summary>
                /// Japan Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "JAPANFORBONINV";
                /// <summary>
                /// Japan Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "JAPANFOREXCRES";
                /// <summary>
                /// Japan Foreign Stock Investment
                /// </summary>
                public const string ForeignStockInvestment = "JAPANFORSTOINV";
                /// <summary>
                /// Japan GDP Deflator
                /// </summary>
                public const string GdpDeflator = "JAPANGDPDEF";
                /// <summary>
                /// Japan Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "JAPANGDPGROANN";
                /// <summary>
                /// Japan Gross Fixed Capital Formation
                /// </summary>
                public const string GrossFixedCapitalFormation = "JAPANGROFIXCAPFOR";
                /// <summary>
                /// Japan Household Spending
                /// </summary>
                public const string HouseholdSpending = "JAPANHOUSPE";
                /// <summary>
                /// Japan Housing Starts
                /// </summary>
                public const string HousingStarts = "JAPANHOUSTA";
                /// <summary>
                /// Japan Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "JAPANINDPROMOM";
                /// <summary>
                /// Japan Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "JAPANINFRATMOM";
                /// <summary>
                /// Japan Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "JAPANLEACOMIND";
                /// <summary>
                /// Japan Loan Growth
                /// </summary>
                public const string LoanGrowth = "JAPANLOAGRO";
                /// <summary>
                /// Japan Machinery Orders
                /// </summary>
                public const string MachineryOrders = "JAPANMACORD";
                /// <summary>
                /// Japan Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "JAPANMANPMI";
                /// <summary>
                /// Japan Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "JAPANNONMANPMI";
                /// <summary>
                /// Japan Private Investment
                /// </summary>
                public const string PrivateInvestment = "JAPANPRIINV";
                /// <summary>
                /// Japan Producer Prices
                /// </summary>
                public const string ProducerPrices = "JAPANPROPRI";
                /// <summary>
                /// Japan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "JAPANPROPRICHA";
                /// <summary>
                /// Japan Services Pmi
                /// </summary>
                public const string ServicesPmi = "JAPANSERPMI";
                /// <summary>
                /// Japan Small Business Sentiment
                /// </summary>
                public const string SmallBusinessSentiment = "JAPANSMABUSSEN";
                /// <summary>
                /// Japan Wage Growth
                /// </summary>
                public const string WageGrowth = "JAPANWAGGRO";
                /// <summary>
                /// Japan Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "JCOMACF";
                /// <summary>
                /// Japan GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "JGDPAGDP";
                /// <summary>
                /// Japan GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "JGDPNSAQ";
                /// <summary>
                /// Japan Current Account
                /// </summary>
                public const string CurrentAccount = "JNBPAB";
                /// <summary>
                /// Japan Exports
                /// </summary>
                public const string Exports = "JNBPEXP";
                /// <summary>
                /// Japan Imports
                /// </summary>
                public const string Imports = "JNBPIMP";
                /// <summary>
                /// Japan Inflation Rate
                /// </summary>
                public const string InflationRate = "JNCPIYOY";
                /// <summary>
                /// Japan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "JNIPYOY";
                /// <summary>
                /// Japan Business Confidence
                /// </summary>
                public const string BusinessConfidence = "JNSBALLI";
                /// <summary>
                /// Japan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "JNTBAL";
                /// <summary>
                /// Japan Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "JNUE";
                /// <summary>
                /// Japan Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "JPNCORECPIRATE";
                /// <summary>
                /// Japan Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "JPNRETAILSALESMOM";
                /// <summary>
                /// Japan Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "JPNRETAILSALESYOY";
            }
            public static class Jordan
            {
                /// <summary>
                /// Jordan Building Permits
                /// </summary>
                public const string BuildingPermits = "JORDANBUIPER";
                /// <summary>
                /// Jordan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "JORDANPROPRICHA";
                /// <summary>
                /// Jordan Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "JORDANTOUARR";
                /// <summary>
                /// Jordan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "JORRDANBALRADE";
                /// <summary>
                /// Jordan Current Account
                /// </summary>
                public const string CurrentAccount = "JORRDANCURCOUNT";
                /// <summary>
                /// Jordan GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "JORRDANGDPATE";
                /// <summary>
                /// Jordan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "JORRDANINDCTION";
                /// <summary>
                /// Jordan Inflation Rate
                /// </summary>
                public const string InflationRate = "JORRDANINFNRATE";
                /// <summary>
                /// Jordan Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "JORRDANUNETRATE";
            }
            public static class Kazakhstan
            {
                /// <summary>
                /// Kazakhstan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KAZAKHSTANBT";
                /// <summary>
                /// Kazakhstan Business Confidence
                /// </summary>
                public const string BusinessConfidence = "KAZAKHSTANBUSCON";
                /// <summary>
                /// Kazakhstan Current Account
                /// </summary>
                public const string CurrentAccount = "KAZAKHSTANCA";
                /// <summary>
                /// Kazakhstan GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "KAZAKHSTANGDPYOY";
                /// <summary>
                /// Kazakhstan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "KAZAKHSTANINDPRO";
                /// <summary>
                /// Kazakhstan Interest Rate
                /// </summary>
                public const string InterestRate = "KAZAKHSTANINTRATE";
                /// <summary>
                /// Kazakhstan Inflation Rate
                /// </summary>
                public const string InflationRate = "KAZAKHSTANIR";
                /// <summary>
                /// Kazakhstan Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "KAZAKHSTANLEAECOIND";
                /// <summary>
                /// Kazakhstan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "KAZAKHSTANPROPRICHA";
                /// <summary>
                /// Kazakhstan Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "KAZAKHSTANRETSALYOY";
                /// <summary>
                /// Kazakhstan Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "KAZAKHSTANUR";
            }
            public static class Kenya
            {
                /// <summary>
                /// Kenya Calendar
                /// </summary>
                public const string Calendar = "KEN-CALENDAR";
                /// <summary>
                /// Kenya Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KENYABT";
                /// <summary>
                /// Kenya Exports
                /// </summary>
                public const string Exports = "KENYAEX";
                /// <summary>
                /// Kenya GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "KENYAGDPQOQ";
                /// <summary>
                /// Kenya GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "KENYAGDPYOY";
                /// <summary>
                /// Kenya Imports
                /// </summary>
                public const string Imports = "KENYAIM";
                /// <summary>
                /// Kenya Interest Rate
                /// </summary>
                public const string InterestRate = "KENYAINTRATE";
                /// <summary>
                /// Kenya Inflation Rate
                /// </summary>
                public const string InflationRate = "KENYAIR";
                /// <summary>
                /// Kenya Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "KENYAMANPMI";
                /// <summary>
                /// Kenya Mni Business Sentiment
                /// </summary>
                public const string MniBusinessSentiment = "KENYAMNIBUSSEN";
                /// <summary>
                /// Kenya Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "KENYAPROPRICHA";
            }
            public static class Kosovo
            {
                /// <summary>
                /// Kosovo Producer Prices
                /// </summary>
                public const string ProducerPrices = "KOSOVOPROPRI";
                /// <summary>
                /// Kosovo Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "KOSOVOPROPRICHA";
                /// <summary>
                /// Kosovo Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KOSSOVOBALRADE";
                /// <summary>
                /// Kosovo GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "KOSSOVOGDPATE";
                /// <summary>
                /// Kosovo Inflation Rate
                /// </summary>
                public const string InflationRate = "KOSSOVOINFNRATE";
                /// <summary>
                /// Kosovo Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "KOSSOVOUNETRATE";
            }
            public static class Kuwait
            {
                /// <summary>
                /// Kuwait Loan Growth
                /// </summary>
                public const string LoanGrowth = "KUWAITLOAGRO";
                /// <summary>
                /// Kuwait Loans To Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "KUWAITLTPS";
                /// <summary>
                /// Kuwait Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "KUWAITMONSUPM2";
                /// <summary>
                /// Kuwait Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KUWWAITBALRADE";
                /// <summary>
                /// Kuwait Inflation Rate
                /// </summary>
                public const string InflationRate = "KUWWAITINFNRATE";
                /// <summary>
                /// Kuwait Interest Rate
                /// </summary>
                public const string InterestRate = "KUWWAITINTTRATE";
            }
            public static class Kyrgyzstan
            {
                /// <summary>
                /// Kyrgyzstan Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "KYRGYZSTANLEAECOIND";
                /// <summary>
                /// Kyrgyzstan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "KYRGYZSTANPROPRICHA";
                /// <summary>
                /// Kyrgyzstan Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "KYRGYZSTANRETSALYOY";
                /// <summary>
                /// Kyrgyzstan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KYRSTANBALRADE";
                /// <summary>
                /// Kyrgyzstan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "KYRSTANINDCTION";
                /// <summary>
                /// Kyrgyzstan Inflation Rate
                /// </summary>
                public const string InflationRate = "KYRSTANINFNRATE";
                /// <summary>
                /// Kyrgyzstan Interest Rate
                /// </summary>
                public const string InterestRate = "KYRSTANINTTRATE";
            }
            public static class Latvia
            {
                /// <summary>
                /// Latvia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LATVIABT";
                /// <summary>
                /// Latvia Current Account
                /// </summary>
                public const string CurrentAccount = "LATVIACA";
                /// <summary>
                /// Latvia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "LATVIAGDPQOQ";
                /// <summary>
                /// Latvia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "LATVIAGDPYOY";
                /// <summary>
                /// Latvia Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "LATVIAINDPROMOM";
                /// <summary>
                /// Latvia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "LATVIAINFRATMOM";
                /// <summary>
                /// Latvia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LATVIAIP";
                /// <summary>
                /// Latvia Inflation Rate
                /// </summary>
                public const string InflationRate = "LATVIAIR";
                /// <summary>
                /// Latvia Producer Prices
                /// </summary>
                public const string ProducerPrices = "LATVIAPROPRI";
                /// <summary>
                /// Latvia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LATVIAPROPRICHA";
                /// <summary>
                /// Latvia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LATVIAUR";
                /// <summary>
                /// Latvia Calendar
                /// </summary>
                public const string Calendar = "LVA-CALENDAR";
                /// <summary>
                /// Latvia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "LVARETAILSALESMOM";
                /// <summary>
                /// Latvia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "LVARETAILSALESYOY";
            }
            public static class Lebanon
            {
                /// <summary>
                /// Lebanon Inflation Rate
                /// </summary>
                public const string InflationRate = "LEBANONINFNRATE";
                /// <summary>
                /// Lebanon Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "LEBANONMANPMI";
            }
            public static class Liechtenstein
            {
                /// <summary>
                /// Liechtenstein Inflation Rate
                /// </summary>
                public const string InflationRate = "LIETEININFNRATE";
            }
            public static class Lithuania
            {
                /// <summary>
                /// Lithuania Business Confidence
                /// </summary>
                public const string BusinessConfidence = "LITHUANIABC";
                /// <summary>
                /// Lithuania Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LITHUANIABT";
                /// <summary>
                /// Lithuania Current Account
                /// </summary>
                public const string CurrentAccount = "LITHUANIACA";
                /// <summary>
                /// Lithuania Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "LITHUANIACC";
                /// <summary>
                /// Lithuania GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "LITHUANIAGDPQOQ";
                /// <summary>
                /// Lithuania GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "LITHUANIAGDPYOY";
                /// <summary>
                /// Lithuania Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LITHUANIAINDPRO";
                /// <summary>
                /// Lithuania Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "LITHUANIAINDPROMOM";
                /// <summary>
                /// Lithuania Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "LITHUANIAINFRATMOM";
                /// <summary>
                /// Lithuania Inflation Rate
                /// </summary>
                public const string InflationRate = "LITHUANIAIR";
                /// <summary>
                /// Lithuania Producer Prices
                /// </summary>
                public const string ProducerPrices = "LITHUANIAPROPRI";
                /// <summary>
                /// Lithuania Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LITHUANIAPROPRICHA";
                /// <summary>
                /// Lithuania Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LITHUANIAUR";
                /// <summary>
                /// Lithuania Calendar
                /// </summary>
                public const string Calendar = "LTU-CALENDAR";
                /// <summary>
                /// Lithuania Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "LTURETAILSALESMOM";
                /// <summary>
                /// Lithuania Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "LTURETAILSALESYOY";
            }
            public static class Luxembourg
            {
                /// <summary>
                /// Luxembourg GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ENGKLUQ";
                /// <summary>
                /// Luxembourg GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ENGKLUY";
                /// <summary>
                /// Luxembourg Business Confidence
                /// </summary>
                public const string BusinessConfidence = "LUXEMBOURGBUSCON";
                /// <summary>
                /// Luxembourg Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "LUXEMBOURGCC";
                /// <summary>
                /// Luxembourg Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LUXEMBOURGPROPRICHA";
                /// <summary>
                /// Luxembourg Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "LUXEMBOURGRETSALMOM";
                /// <summary>
                /// Luxembourg Current Account
                /// </summary>
                public const string CurrentAccount = "LXCAEU";
                /// <summary>
                /// Luxembourg Inflation Rate
                /// </summary>
                public const string InflationRate = "LXCPIYOY";
                /// <summary>
                /// Luxembourg Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LXIPRYOY";
                /// <summary>
                /// Luxembourg Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LXTBBALL";
                /// <summary>
                /// Luxembourg Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "OELUU003";
            }
            public static class Macau
            {
                /// <summary>
                /// Macau Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MACACAOBALRADE";
                /// <summary>
                /// Macau GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MACACAOGDPATE";
                /// <summary>
                /// Macau Inflation Rate
                /// </summary>
                public const string InflationRate = "MACACAOINFNRATE";
                /// <summary>
                /// Macau Interest Rate
                /// </summary>
                public const string InterestRate = "MACACAOINTTRATE";
                /// <summary>
                /// Macau Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "MACACAORETSMOM";
                /// <summary>
                /// Macau Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "MACACAORETSYOY";
                /// <summary>
                /// Macau Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MACACAOUNETRATE";
                /// <summary>
                /// Macau Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "MACAOTOUARR";
            }
            public static class Macedonia
            {
                /// <summary>
                /// Macedonia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MACEDONIABT";
                /// <summary>
                /// Macedonia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MACEDONIABUSCON";
                /// <summary>
                /// Macedonia Current Account
                /// </summary>
                public const string CurrentAccount = "MACEDONIACA";
                /// <summary>
                /// Macedonia Exports
                /// </summary>
                public const string Exports = "MACEDONIAEX";
                /// <summary>
                /// Macedonia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MACEDONIAGDPYOY";
                /// <summary>
                /// Macedonia Imports
                /// </summary>
                public const string Imports = "MACEDONIAIM";
                /// <summary>
                /// Macedonia Interest Rate
                /// </summary>
                public const string InterestRate = "MACEDONIAINTRATE";
                /// <summary>
                /// Macedonia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MACEDONIAIP";
                /// <summary>
                /// Macedonia Inflation Rate
                /// </summary>
                public const string InflationRate = "MACEDONIAIR";
                /// <summary>
                /// Macedonia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MACEDONIAPROPRICHA";
                /// <summary>
                /// Macedonia Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "MACEDONIARETSALYOY";
                /// <summary>
                /// Macedonia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MACEDONIAUR";
            }
            public static class Malawi
            {
                /// <summary>
                /// Malawi Inflation Rate
                /// </summary>
                public const string InflationRate = "MALLAWIINFNRATE";
                /// <summary>
                /// Malawi Interest Rate
                /// </summary>
                public const string InterestRate = "MALLAWIINTTRATE";
            }
            public static class Malaysia
            {
                /// <summary>
                /// Malaysia Current Account
                /// </summary>
                public const string CurrentAccount = "MACATOT";
                /// <summary>
                /// Malaysia Inflation Rate
                /// </summary>
                public const string InflationRate = "MACPIYOY";
                /// <summary>
                /// Malaysia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MAEPRATE";
                /// <summary>
                /// Malaysia Exports
                /// </summary>
                public const string Exports = "MAETEXP";
                /// <summary>
                /// Malaysia Imports
                /// </summary>
                public const string Imports = "MAETIMP";
                /// <summary>
                /// Malaysia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MAETTRBL";
                /// <summary>
                /// Malaysia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MAGDHIY";
                /// <summary>
                /// Malaysia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MAGDPQPCT";
                /// <summary>
                /// Malaysia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MAIPINDY";
                /// <summary>
                /// Malaysia Coincident Index
                /// </summary>
                public const string CoincidentIndex = "MALAYSIACOIIND";
                /// <summary>
                /// Malaysia Construction Output
                /// </summary>
                public const string ConstructionOutput = "MALAYSIACONOUT";
                /// <summary>
                /// Malaysia Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "MALAYSIAFOREXCRES";
                /// <summary>
                /// Malaysia Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "MALAYSIAINDPROMOM";
                /// <summary>
                /// Malaysia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "MALAYSIALEAECOIND";
                /// <summary>
                /// Malaysia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "MALAYSIAMANPMI";
                /// <summary>
                /// Malaysia Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "MALAYSIAMONSUPM3";
                /// <summary>
                /// Malaysia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MALAYSIAPROPRICHA";
                /// <summary>
                /// Malaysia Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "MALAYSIARETSALYOY";
                /// <summary>
                /// Malaysia Interest Rate
                /// </summary>
                public const string InterestRate = "MAOPRATE";
            }
            public static class Malta
            {
                /// <summary>
                /// Malta Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MALALTABALRADE";
                /// <summary>
                /// Malta GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MALALTAGDPATE";
                /// <summary>
                /// Malta Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MALALTAINDCTION";
                /// <summary>
                /// Malta Inflation Rate
                /// </summary>
                public const string InflationRate = "MALALTAINFNRATE";
                /// <summary>
                /// Malta Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "MALALTARETSMOM";
                /// <summary>
                /// Malta Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "MALALTARETSYOY";
                /// <summary>
                /// Malta Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MALALTAUNETRATE";
                /// <summary>
                /// Malta Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MALTABUSCON";
                /// <summary>
                /// Malta Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "MALTACONCON";
                /// <summary>
                /// Malta Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MALTAPROPRICHA";
            }
            public static class Mauritius
            {
                /// <summary>
                /// Mauritius Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "MAURITIUSTOUARR";
                /// <summary>
                /// Mauritius Tourism Revenues
                /// </summary>
                public const string TourismRevenues = "MAURITIUSTOUREV";
                /// <summary>
                /// Mauritius Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MAUTIUSBALRADE";
                /// <summary>
                /// Mauritius GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MAUTIUSGDPATE";
                /// <summary>
                /// Mauritius Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MAUTIUSINDCTION";
                /// <summary>
                /// Mauritius Inflation Rate
                /// </summary>
                public const string InflationRate = "MAUTIUSINFNRATE";
                /// <summary>
                /// Mauritius Interest Rate
                /// </summary>
                public const string InterestRate = "MAUTIUSINTTRATE";
                /// <summary>
                /// Mauritius Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MAUTIUSUNETRATE";
            }
            public static class Mexico
            {
                /// <summary>
                /// Mexico Calendar
                /// </summary>
                public const string Calendar = "MEX-CALENDAR";
                /// <summary>
                /// Mexico Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MEXICOBC";
                /// <summary>
                /// Mexico Car Production
                /// </summary>
                public const string CarProduction = "MEXICOCARPRO";
                /// <summary>
                /// Mexico Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "MEXICOCONSPE";
                /// <summary>
                /// Mexico Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "MEXICOCORCONPRI";
                /// <summary>
                /// Mexico Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "MEXICOFOREXCRES";
                /// <summary>
                /// Mexico Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "MEXICOGOVBUDVAL";
                /// <summary>
                /// Mexico Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "MEXICOINDPROMOM";
                /// <summary>
                /// Mexico Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "MEXICOINFRATMOM";
                /// <summary>
                /// Mexico Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "MEXICOLEAECOIND";
                /// <summary>
                /// Mexico Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "MEXICOMANPMI";
                /// <summary>
                /// Mexico Private Investment
                /// </summary>
                public const string PrivateInvestment = "MEXICOPRIINV";
                /// <summary>
                /// Mexico Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "MEXRETAILSALESMOM";
                /// <summary>
                /// Mexico Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "MEXRETAILSALESYOY";
                /// <summary>
                /// Mexico Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "MXCFCONF";
                /// <summary>
                /// Mexico Inflation Rate
                /// </summary>
                public const string InflationRate = "MXCPYOY";
                /// <summary>
                /// Mexico GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MXGCTOT";
                /// <summary>
                /// Mexico GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MXGPQTR";
                /// <summary>
                /// Mexico Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MXIPTYOY";
                /// <summary>
                /// Mexico Interest Rate
                /// </summary>
                public const string InterestRate = "MXONBR";
                /// <summary>
                /// Mexico Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MXTBBAL";
                /// <summary>
                /// Mexico Exports
                /// </summary>
                public const string Exports = "MXTBBEXP";
                /// <summary>
                /// Mexico Imports
                /// </summary>
                public const string Imports = "MXTBBIMP";
                /// <summary>
                /// Mexico Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MXUEUNSA";
                /// <summary>
                /// Mexico Current Account
                /// </summary>
                public const string CurrentAccount = "OEMXB039";
            }
            public static class Moldova
            {
                /// <summary>
                /// Moldova Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MOLDOVABALRADE";
                /// <summary>
                /// Moldova Current Account
                /// </summary>
                public const string CurrentAccount = "MOLDOVACURCOUNT";
                /// <summary>
                /// Moldova GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MOLDOVAGDPANNGRORAT";
                /// <summary>
                /// Moldova Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MOLDOVAINDCTION";
                /// <summary>
                /// Moldova Inflation Rate
                /// </summary>
                public const string InflationRate = "MOLDOVAINFNRATE";
                /// <summary>
                /// Moldova Interest Rate
                /// </summary>
                public const string InterestRate = "MOLDOVAINTTRATE";
                /// <summary>
                /// Moldova Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MOLDOVAPROPRICHA";
                /// <summary>
                /// Moldova Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MOLDOVAUNETRATE";
            }
            public static class Mongolia
            {
                /// <summary>
                /// Mongolia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MONGOLIABT";
                /// <summary>
                /// Mongolia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MONGOLIAGDPYOY";
                /// <summary>
                /// Mongolia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MONGOLIAINDPRO";
                /// <summary>
                /// Mongolia Inflation Rate
                /// </summary>
                public const string InflationRate = "MONGOLIAIR";
                /// <summary>
                /// Mongolia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MONGOLIAUR";
            }
            public static class Montenegro
            {
                /// <summary>
                /// Montenegro Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MONEGROBALRADE";
                /// <summary>
                /// Montenegro Current Account
                /// </summary>
                public const string CurrentAccount = "MONEGROCURCOUNT";
                /// <summary>
                /// Montenegro GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MONEGROGDPATE";
                /// <summary>
                /// Montenegro Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MONEGROINDCTION";
                /// <summary>
                /// Montenegro Inflation Rate
                /// </summary>
                public const string InflationRate = "MONEGROINFNRATE";
                /// <summary>
                /// Montenegro Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "MONTENEGROHARCONPRI";
                /// <summary>
                /// Montenegro Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MONTENEGROPROPRICHA";
                /// <summary>
                /// Montenegro Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "MONTENEGRORETSALMOM";
                /// <summary>
                /// Montenegro Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "MONTENEGRORETSALYOY";
                /// <summary>
                /// Montenegro Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "MONTENEGROTOUARR";
            }
            public static class Morocco
            {
                /// <summary>
                /// Morocco Current Account
                /// </summary>
                public const string CurrentAccount = "MOROCCOCA";
                /// <summary>
                /// Morocco GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MOROCCOGDPYOY";
                /// <summary>
                /// Morocco Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MOROCCOINDPRO";
                /// <summary>
                /// Morocco Interest Rate
                /// </summary>
                public const string InterestRate = "MOROCCOINTRATE";
                /// <summary>
                /// Morocco Inflation Rate
                /// </summary>
                public const string InflationRate = "MOROCCOIR";
                /// <summary>
                /// Morocco Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "MOROCCOMONSUPM2";
                /// <summary>
                /// Morocco Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MOROCCOUR";
            }
            public static class Mozambique
            {
                /// <summary>
                /// Mozambique Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MOZAMBIQUEBUSCON";
                /// <summary>
                /// Mozambique Composite Pmi
                /// </summary>
                public const string CompositePmi = "MOZAMBIQUECOMPMI";
                /// <summary>
                /// Mozambique GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MOZAMBIQUEGDPQOQ";
                /// <summary>
                /// Mozambique GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MOZAMBIQUEGDPYOY";
                /// <summary>
                /// Mozambique Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "MOZAMBIQUEINFRATMOM";
                /// <summary>
                /// Mozambique Interest Rate
                /// </summary>
                public const string InterestRate = "MOZAMBIQUEINTRATE";
                /// <summary>
                /// Mozambique Inflation Rate
                /// </summary>
                public const string InflationRate = "MOZAMBIQUEIR";
            }
            public static class Myanmar
            {
                /// <summary>
                /// Myanmar Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "MYANMARMANPMI";
            }
            public static class Namibia
            {
                /// <summary>
                /// Namibia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NAMIBIAGDPQOQ";
                /// <summary>
                /// Namibia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NAMIBIAGDPYOY";
                /// <summary>
                /// Namibia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "NAMIBIAINFRATMOM";
                /// <summary>
                /// Namibia Interest Rate
                /// </summary>
                public const string InterestRate = "NAMIBIAINTRATE";
                /// <summary>
                /// Namibia Inflation Rate
                /// </summary>
                public const string InflationRate = "NAMIBIAIR";
            }
            public static class Netherlands
            {
                /// <summary>
                /// Netherlands Calendar
                /// </summary>
                public const string Calendar = "CALENDARNETHERLANDS";
                /// <summary>
                /// Netherlands Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GNTH10YR";
                /// <summary>
                /// Netherlands Current Account
                /// </summary>
                public const string CurrentAccount = "NECATOTE";
                /// <summary>
                /// Netherlands Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NECCI";
                /// <summary>
                /// Netherlands Inflation Rate
                /// </summary>
                public const string InflationRate = "NECPIYOY";
                /// <summary>
                /// Netherlands GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NEGDPEQ";
                /// <summary>
                /// Netherlands GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NEGDPEY";
                /// <summary>
                /// Netherlands Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NEIP20YY";
                /// <summary>
                /// Netherlands Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NEPRI";
                /// <summary>
                /// Netherlands Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NETBEU";
                /// <summary>
                /// Netherlands 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "NETHERLAND3MBY";
                /// <summary>
                /// Netherlands 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "NETHERLAND6MBY";
                /// <summary>
                /// Netherlands Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "NETHERLANDINDPROMOM";
                /// <summary>
                /// Netherlands Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "NLD-CALENDAR";
                /// <summary>
                /// Netherlands Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "NETHERLANDMANPRO";
                /// <summary>
                /// Netherlands Personal Spending
                /// </summary>
                public const string PersonalSpending = "NETHERLANDPERSPE";
                /// <summary>
                /// Netherlands Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NEUETOTR";
                /// <summary>
                /// Netherlands Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "NLDRETAILSALESMOM";
                /// <summary>
                /// Netherlands Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "NLDRETAILSALESYOY";
            }
            public static class NewZealand
            {
                /// <summary>
                /// NewZealand Calendar
                /// </summary>
                public const string Calendar = "NZD-CALENDAR";
                /// <summary>
                /// NewZealand Building Permits
                /// </summary>
                public const string BuildingPermits = "NEWZEALANBUIPER";
                /// <summary>
                /// NewZealand Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "NEWZEALANCAPUTI";
                /// <summary>
                /// NewZealand Employment Change
                /// </summary>
                public const string EmploymentChange = "NEWZEALANEMPCHA";
                /// <summary>
                /// NewZealand Export Prices
                /// </summary>
                public const string ExportPrices = "NEWZEALANEXPPRI";
                /// <summary>
                /// NewZealand Food Inflation
                /// </summary>
                public const string FoodInflation = "NEWZEALANFOOINF";
                /// <summary>
                /// NewZealand Global Dairy Trade Price Index
                /// </summary>
                public const string GlobalDairyTradePriceIndex = "NEWZEALANGDTPI";
                /// <summary>
                /// NewZealand Housing Index
                /// </summary>
                public const string HousingIndex = "NEWZEALANHOUIND";
                /// <summary>
                /// NewZealand Import Prices
                /// </summary>
                public const string ImportPrices = "NEWZEALANIMPPRI";
                /// <summary>
                /// NewZealand Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "NEWZEALANINFEXP";
                /// <summary>
                /// NewZealand Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "NEWZEALANINFRATMOM";
                /// <summary>
                /// NewZealand Labour Costs
                /// </summary>
                public const string LabourCosts = "NEWZEALANLABCOS";
                /// <summary>
                /// NewZealand Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "NEWZEALANLABFORPARRA";
                /// <summary>
                /// NewZealand Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "NEWZEALANMANPMI";
                /// <summary>
                /// NewZealand Producer Prices
                /// </summary>
                public const string ProducerPrices = "NEWZEALANPROPRI";
                /// <summary>
                /// NewZealand Services Pmi
                /// </summary>
                public const string ServicesPmi = "NEWZEALANSERPMI";
                /// <summary>
                /// NewZealand Terms of Trade
                /// </summary>
                public const string TermsOfTrade = "NEWZEALANTEROFTRA";
                /// <summary>
                /// NewZealand Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "NEWZEALANTOUARR";
                /// <summary>
                /// NewZealand Current Account
                /// </summary>
                public const string CurrentAccount = "NZBPCA";
                /// <summary>
                /// NewZealand Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NZCC";
                /// <summary>
                /// NewZealand Inflation Rate
                /// </summary>
                public const string InflationRate = "NZCPIYOY";
                /// <summary>
                /// NewZealand Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NZIEBCAC";
                /// <summary>
                /// NewZealand Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NZLFUNER";
                /// <summary>
                /// NewZealand Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "NZLRETAILSALESMOM";
                /// <summary>
                /// NewZealand Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "NZLRETAILSALESYOY";
                /// <summary>
                /// NewZealand Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NZMTBAL";
                /// <summary>
                /// NewZealand Exports
                /// </summary>
                public const string Exports = "NZMTEXP";
                /// <summary>
                /// NewZealand Imports
                /// </summary>
                public const string Imports = "NZMTIMP";
                /// <summary>
                /// NewZealand GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NZNTGDPC";
                /// <summary>
                /// NewZealand GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NZNTGDPY";
                /// <summary>
                /// NewZealand Interest Rate
                /// </summary>
                public const string InterestRate = "NZOCRS";
                /// <summary>
                /// NewZealand Industrial Production
                /// </summary>
                public const string IndustrialProduction = "OENZV012";
            }
            public static class Nicaragua
            {
                /// <summary>
                /// Nicaragua Current Account
                /// </summary>
                public const string CurrentAccount = "NICAGUACURCOUNT";
                /// <summary>
                /// Nicaragua GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NICAGUAGDPATE";
                /// <summary>
                /// Nicaragua Inflation Rate
                /// </summary>
                public const string InflationRate = "NICAGUAINFNRATE";
                /// <summary>
                /// Nicaragua Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NICAGUAUNETRATE";
                /// <summary>
                /// Nicaragua Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NICARAGUAINDPRO";
            }
            public static class Nigeria
            {
                /// <summary>
                /// Nigeria Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NIGERIABT";
                /// <summary>
                /// Nigeria Composite Pmi
                /// </summary>
                public const string CompositePmi = "NIGERIACOMPMI";
                /// <summary>
                /// Nigeria Employment Change
                /// </summary>
                public const string EmploymentChange = "NIGERIAEMPCHA";
                /// <summary>
                /// Nigeria Food Inflation
                /// </summary>
                public const string FoodInflation = "NIGERIAFOOINF";
                /// <summary>
                /// Nigeria Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "NIGERIAFOREXCRES";
                /// <summary>
                /// Nigeria GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NIGERIAGDPQOQ";
                /// <summary>
                /// Nigeria GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NIGERIAGDPYOY";
                /// <summary>
                /// Nigeria Interest Rate
                /// </summary>
                public const string InterestRate = "NIGERIAINTRATE";
                /// <summary>
                /// Nigeria Inflation Rate
                /// </summary>
                public const string InflationRate = "NIGERIAIR";
                /// <summary>
                /// Nigeria Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "NIGERIAMANPMI";
                /// <summary>
                /// Nigeria Services Pmi
                /// </summary>
                public const string ServicesPmi = "NIGERIASERPMI";
                /// <summary>
                /// Nigeria Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NIGERIAUR";
            }
            public static class Norway
            {
                /// <summary>
                /// Norway Calendar
                /// </summary>
                public const string Calendar = "NOR-CALENDAR";
                /// <summary>
                /// Norway Current Account
                /// </summary>
                public const string CurrentAccount = "NOBPLVL";
                /// <summary>
                /// Norway Interest Rate
                /// </summary>
                public const string InterestRate = "NOBRDEP";
                /// <summary>
                /// Norway Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NOCONF";
                /// <summary>
                /// Norway Inflation Rate
                /// </summary>
                public const string InflationRate = "NOCPIYOY";
                /// <summary>
                /// Norway GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NOGDCOSQ";
                /// <summary>
                /// Norway GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NOGDCOSY";
                /// <summary>
                /// Norway Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NOIPGYOY";
                /// <summary>
                /// Norway Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NOLBRATE";
                /// <summary>
                /// Norway Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "NORCORECPIRATE";
                /// <summary>
                /// Norway Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "NORRETAILSALESMOM";
                /// <summary>
                /// Norway Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "NORRETAILSALESYOY";
                /// <summary>
                /// Norway Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NORWAYBC";
                /// <summary>
                /// Norway Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "NORWAYCORCONPRI";
                /// <summary>
                /// Norway Household Spending
                /// </summary>
                public const string HouseholdSpending = "NORWAYHOUSPE";
                /// <summary>
                /// Norway Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "NORWAYINDPROMOM";
                /// <summary>
                /// Norway Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "NORWAYINFRATMOM";
                /// <summary>
                /// Norway Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "NORWAYLEAECOIND";
                /// <summary>
                /// Norway Loan Growth
                /// </summary>
                public const string LoanGrowth = "NORWAYLOAGRO";
                /// <summary>
                /// Norway Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "NORWAYMANPMI";
                /// <summary>
                /// Norway Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "NORWAYMANPRO";
                /// <summary>
                /// Norway Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "NORWAYPROPRICHA";
                /// <summary>
                /// Norway Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "NORWAYUNEPER";
                /// <summary>
                /// Norway Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NOTBTOT";
            }
            public static class Oman
            {
                /// <summary>
                /// Oman Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "OMANGOVBUDVAL";
                /// <summary>
                /// Oman Loan Growth
                /// </summary>
                public const string LoanGrowth = "OMANLOAGRO";
                /// <summary>
                /// Oman Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "OMANMONSUPM2";
                /// <summary>
                /// Oman Inflation Rate
                /// </summary>
                public const string InflationRate = "OMAOMANINFNRATE";
            }
            public static class Pakistan
            {
                /// <summary>
                /// Pakistan Inflation Rate
                /// </summary>
                public const string InflationRate = "PACPGENY";
                /// <summary>
                /// Pakistan Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PAKISTANCONCON";
                /// <summary>
                /// Pakistan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PAKISTANPROPRICHA";
                /// <summary>
                /// Pakistan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PTRDBAL";
            }
            public static class Palestine
            {
                /// <summary>
                /// Palestine Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PALESTINEPROPRICHA";
                /// <summary>
                /// Palestine Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PALTINEBALRADE";
                /// <summary>
                /// Palestine Current Account
                /// </summary>
                public const string CurrentAccount = "PALTINECURCOUNT";
                /// <summary>
                /// Palestine GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PALTINEGDPATE";
                /// <summary>
                /// Palestine Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PALTINEINDCTION";
                /// <summary>
                /// Palestine Inflation Rate
                /// </summary>
                public const string InflationRate = "PALTINEINFNRATE";
                /// <summary>
                /// Palestine Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PALTINEUNETRATE";
            }
            public static class Panama
            {
                /// <summary>
                /// Panama Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "PANAMALEAECOIND";
                /// <summary>
                /// Panama Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PANNAMABALRADE";
                /// <summary>
                /// Panama Current Account
                /// </summary>
                public const string CurrentAccount = "PANNAMACURCOUNT";
                /// <summary>
                /// Panama GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PANNAMAGDPATE";
                /// <summary>
                /// Panama Inflation Rate
                /// </summary>
                public const string InflationRate = "PANNAMAINFNRATE";
            }
            public static class Paraguay
            {
                /// <summary>
                /// Paraguay Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PARAGUAYBT";
                /// <summary>
                /// Paraguay Current Account
                /// </summary>
                public const string CurrentAccount = "PARAGUAYCA";
                /// <summary>
                /// Paraguay GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PARAGUAYGDPQOQ";
                /// <summary>
                /// Paraguay GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PARAGUAYGDPYOY";
                /// <summary>
                /// Paraguay Interest Rate
                /// </summary>
                public const string InterestRate = "PARAGUAYINTRATE";
                /// <summary>
                /// Paraguay Inflation Rate
                /// </summary>
                public const string InflationRate = "PARAGUAYIR";
            }
            public static class Peru
            {
                /// <summary>
                /// Peru Calendar
                /// </summary>
                public const string Calendar = "PER-CALENDAR";
                /// <summary>
                /// Peru Business Confidence
                /// </summary>
                public const string BusinessConfidence = "PERPERUBUSDENCE";
                /// <summary>
                /// Peru Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PERPERUINDCTION";
                /// <summary>
                /// Peru Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PERTBAL";
                /// <summary>
                /// Peru Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "PERUINFRATMOM";
                /// <summary>
                /// Peru Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "PERULEAECOIND";
                /// <summary>
                /// Peru Inflation Rate
                /// </summary>
                public const string InflationRate = "PRCPYOY";
                /// <summary>
                /// Peru Current Account
                /// </summary>
                public const string CurrentAccount = "PRCU";
                /// <summary>
                /// Peru GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PRGDPQOQ";
                /// <summary>
                /// Peru GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PRGDPYOY";
                /// <summary>
                /// Peru Interest Rate
                /// </summary>
                public const string InterestRate = "PRRRONUS";
                /// <summary>
                /// Peru Exports
                /// </summary>
                public const string Exports = "PRTREXPT";
                /// <summary>
                /// Peru Imports
                /// </summary>
                public const string Imports = "PRTRIMPT";
                /// <summary>
                /// Peru Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PRUEUER";
            }
            public static class Philippines
            {
                /// <summary>
                /// Philippines Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PHIINESINDCTION";
                /// <summary>
                /// Philippines Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "PHILIPPINECORINFRAT";
                /// <summary>
                /// Philippines Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "PHILIPPINEFORDIRINV";
                /// <summary>
                /// Philippines Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "PHILIPPINEFOREXCRES";
                /// <summary>
                /// Philippines Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "PHILIPPINEGOVBUDVAL";
                /// <summary>
                /// Philippines Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "PHILIPPINEINFRATMOM";
                /// <summary>
                /// Philippines Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "PHILIPPINEMANPMI";
                /// <summary>
                /// Philippines Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PHILIPPINEPROPRICHA";
                /// <summary>
                /// Philippines Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "PHILIPPINERETSALYOY";
                /// <summary>
                /// Philippines Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PHILIPPINESBT";
                /// <summary>
                /// Philippines Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PHILIPPINESCC";
                /// <summary>
                /// Philippines Exports
                /// </summary>
                public const string Exports = "PHILIPPINESEX";
                /// <summary>
                /// Philippines Government Budget
                /// </summary>
                public const string GovernmentBudget = "PHILIPPINESGB";
                /// <summary>
                /// Philippines GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PHILIPPINESGDPQOQ";
                /// <summary>
                /// Philippines GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PHILIPPINESGDPYOY";
                /// <summary>
                /// Philippines Imports
                /// </summary>
                public const string Imports = "PHILIPPINESIM";
                /// <summary>
                /// Philippines Interest Rate
                /// </summary>
                public const string InterestRate = "PHILIPPINESINTRATE";
                /// <summary>
                /// Philippines Inflation Rate
                /// </summary>
                public const string InflationRate = "PHILIPPINESIR";
                /// <summary>
                /// Philippines Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PHILIPPINESUR";
            }
            public static class Poland
            {
                /// <summary>
                /// Poland Calendar
                /// </summary>
                public const string Calendar = "POL-CALENDAR";
                /// <summary>
                /// Poland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "EUGNPLQQ";
                /// <summary>
                /// Poland Inflation Rate
                /// </summary>
                public const string InflationRate = "POCPIYOY";
                /// <summary>
                /// Poland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "POGDYOY";
                /// <summary>
                /// Poland Industrial Production
                /// </summary>
                public const string IndustrialProduction = "POISCYOY";
                /// <summary>
                /// Poland Employment Change
                /// </summary>
                public const string EmploymentChange = "POLANDEMPCHA";
                /// <summary>
                /// Poland Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "POLANDFOREXCRES";
                /// <summary>
                /// Poland Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "POLANDINDPROMOM";
                /// <summary>
                /// Poland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "POLANDINFRATMOM";
                /// <summary>
                /// Poland Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "POLANDMANPMI";
                /// <summary>
                /// Poland Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "POLANDMONSUPM3";
                /// <summary>
                /// Poland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "POLANDPROPRICHA";
                /// <summary>
                /// Poland Wage Growth
                /// </summary>
                public const string WageGrowth = "POLANDWAGGRO";
                /// <summary>
                /// Poland Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "POLCORECPIRATE";
                /// <summary>
                /// Poland Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "POLRETAILSALESMOM";
                /// <summary>
                /// Poland Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "POLRETAILSALESYOY";
                /// <summary>
                /// Poland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "POMECBGB";
                /// <summary>
                /// Poland Current Account
                /// </summary>
                public const string CurrentAccount = "POQECBCA";
                /// <summary>
                /// Poland Interest Rate
                /// </summary>
                public const string InterestRate = "PORERATE";
                /// <summary>
                /// Poland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "POUER";
            }
            public static class Portugal
            {
                /// <summary>
                /// Portugal Inflation Rate
                /// </summary>
                public const string InflationRate = "PLCPYOY";
                /// <summary>
                /// Portugal Business Confidence
                /// </summary>
                public const string BusinessConfidence = "PORTUGALBC";
                /// <summary>
                /// Portugal Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "PORTUGALINDPROMOM";
                /// <summary>
                /// Portugal Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "PORTUGALINFRATMOM";
                /// <summary>
                /// Portugal Producer Prices
                /// </summary>
                public const string ProducerPrices = "PORTUGALPROPRI";
                /// <summary>
                /// Portugal Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PORTUGALPROPRICHA";
                /// <summary>
                /// Portugal Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "PRTRETAILSALESMOM";
                /// <summary>
                /// Portugal Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "PRTRETAILSALESYOY";
                /// <summary>
                /// Portugal Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PTCCI";
                /// <summary>
                /// Portugal Current Account
                /// </summary>
                public const string CurrentAccount = "PTCUEURO";
                /// <summary>
                /// Portugal GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PTGDPQOQ";
                /// <summary>
                /// Portugal GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PTGDPYOY";
                /// <summary>
                /// Portugal Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PTIPTOTY";
                /// <summary>
                /// Portugal Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PTTBEUAL";
                /// <summary>
                /// Portugal Exports
                /// </summary>
                public const string Exports = "PTTBEUEX";
                /// <summary>
                /// Portugal Imports
                /// </summary>
                public const string Imports = "PTTBEUIM";
                /// <summary>
                /// Portugal Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PTUE";
            }
            public static class Qatar
            {
                /// <summary>
                /// Qatar Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "QATARBT";
                /// <summary>
                /// Qatar GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "QATARGDPQOQ";
                /// <summary>
                /// Qatar GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "QATARGDPYOY";
                /// <summary>
                /// Qatar Inflation Rate
                /// </summary>
                public const string InflationRate = "QATARIR";
                /// <summary>
                /// Qatar Loan Growth
                /// </summary>
                public const string LoanGrowth = "QATARLOAGRO";
                /// <summary>
                /// Qatar Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "QATARMANPMI";
                /// <summary>
                /// Qatar Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "QATARMONSUPM2";
                /// <summary>
                /// Qatar Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "QATFRINRDPST";
            }
            public static class Romania
            {
                /// <summary>
                /// Romania Calendar
                /// </summary>
                public const string Calendar = "ROM-CALENDAR";
                /// <summary>
                /// Romania Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ROMANIABT";
                /// <summary>
                /// Romania Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ROMANIABUSCON";
                /// <summary>
                /// Romania Current Account
                /// </summary>
                public const string CurrentAccount = "ROMANIACA";
                /// <summary>
                /// Romania Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ROMANIACONCON";
                /// <summary>
                /// Romania GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ROMANIAGDPQOQ";
                /// <summary>
                /// Romania GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ROMANIAGDPYOY";
                /// <summary>
                /// Romania Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ROMANIAINDPROMOM";
                /// <summary>
                /// Romania Interest Rate
                /// </summary>
                public const string InterestRate = "ROMANIAINTRATE";
                /// <summary>
                /// Romania Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ROMANIAIP";
                /// <summary>
                /// Romania Inflation Rate
                /// </summary>
                public const string InflationRate = "ROMANIAIR";
                /// <summary>
                /// Romania Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ROMANIAPROPRICHA";
                /// <summary>
                /// Romania Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ROMANIAUR";
                /// <summary>
                /// Romania Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ROMRETAILSALESMOM";
                /// <summary>
                /// Romania Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ROMRETAILSALESYOY";
            }
            public static class Russia
            {
                /// <summary>
                /// Russia Calendar
                /// </summary>
                public const string Calendar = "RUS-CALENDAR";
                /// <summary>
                /// Russia Interest Rate
                /// </summary>
                public const string InterestRate = "RREFRATE";
                /// <summary>
                /// Russia Current Account
                /// </summary>
                public const string CurrentAccount = "RUCACAL";
                /// <summary>
                /// Russia Inflation Rate
                /// </summary>
                public const string InflationRate = "RUCPIYOY";
                /// <summary>
                /// Russia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "RUDPRYOY";
                /// <summary>
                /// Russia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "RUIPRNYY";
                /// <summary>
                /// Russia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "RUSRETAILSALESMOM";
                /// <summary>
                /// Russia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "RUSRETAILSALESYOY";
                /// <summary>
                /// Russia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "RUSSIABC";
                /// <summary>
                /// Russia Corporate Profits
                /// </summary>
                public const string CorporateProfits = "RUSSIACORPRO";
                /// <summary>
                /// Russia Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "RUSSIAFOREXCRES";
                /// <summary>
                /// Russia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "RUSSIAINFRATMOM";
                /// <summary>
                /// Russia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "RUSSIALEAECOIND";
                /// <summary>
                /// Russia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "RUSSIAMANPMI";
                /// <summary>
                /// Russia Mni Consumer Sentiment
                /// </summary>
                public const string MniConsumerSentiment = "RUSSIAMNICONSEN";
                /// <summary>
                /// Russia Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "RUSSIAMONSUPM2";
                /// <summary>
                /// Russia Producer Prices
                /// </summary>
                public const string ProducerPrices = "RUSSIAPROPRI";
                /// <summary>
                /// Russia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "RUSSIAPROPRICHA";
                /// <summary>
                /// Russia Services Pmi
                /// </summary>
                public const string ServicesPmi = "RUSSIASERPMI";
                /// <summary>
                /// Russia Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "RUSSIATOTVEHSAL";
                /// <summary>
                /// Russia Wage Growth
                /// </summary>
                public const string WageGrowth = "RUSSIAWAGGRO";
                /// <summary>
                /// Russia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "RUSSSIABUSDENCE";
                /// <summary>
                /// Russia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "RUTBAL";
                /// <summary>
                /// Russia Exports
                /// </summary>
                public const string Exports = "RUTBEX";
                /// <summary>
                /// Russia Imports
                /// </summary>
                public const string Imports = "RUTBIM";
                /// <summary>
                /// Russia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "RUUER";
            }
            public static class Rwanda
            {
                /// <summary>
                /// Rwanda GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "RWANDAGDPQOQ";
                /// <summary>
                /// Rwanda GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "RWANDAGDPYOY";
                /// <summary>
                /// Rwanda Interest Rate
                /// </summary>
                public const string InterestRate = "RWANDAINTRATE";
                /// <summary>
                /// Rwanda Inflation Rate
                /// </summary>
                public const string InflationRate = "RWANDAIR";
                /// <summary>
                /// Rwanda Producer Prices
                /// </summary>
                public const string ProducerPrices = "RWANDAPROPRI";
                /// <summary>
                /// Rwanda Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "RWANDAPROPRICHA";
            }
            public static class SaoTomeAndPrincipe
            {
                /// <summary>
                /// SaoTomeAndPrincipe Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SAOEBALRADE";
                /// <summary>
                /// SaoTomeAndPrincipe Inflation Rate
                /// </summary>
                public const string InflationRate = "SAOEINFNRATE";
            }
            public static class SaudiArabia
            {
                /// <summary>
                /// SaudiArabia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "4567A12";
                /// <summary>
                /// SaudiArabia Loan Growth
                /// </summary>
                public const string LoanGrowth = "SAUDIARABLOAGRO";
                /// <summary>
                /// SaudiArabia Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "SAUDIARABMANPMI";
                /// <summary>
                /// SaudiArabia Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "SAUDIARABMONSUPM3";
                /// <summary>
                /// SaudiArabia Inflation Rate
                /// </summary>
                public const string InflationRate = "SRCPIYOY";
                /// <summary>
                /// SaudiArabia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SRGDPCYY";
                /// <summary>
                /// SaudiArabia Interest Rate
                /// </summary>
                public const string InterestRate = "SRREPO";
            }
            public static class Senegal
            {
                /// <summary>
                /// Senegal Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SENEGALBALRADE";
                /// <summary>
                /// Senegal GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SENEGALGDPATE";
                /// <summary>
                /// Senegal Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SENEGALINDCTION";
                /// <summary>
                /// Senegal Inflation Rate
                /// </summary>
                public const string InflationRate = "SENEGALINFNRATE";
            }
            public static class Serbia
            {
                /// <summary>
                /// Serbia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SERBIAINFRATMOM";
                /// <summary>
                /// Serbia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SERRBIABALRADE";
                /// <summary>
                /// Serbia Current Account
                /// </summary>
                public const string CurrentAccount = "SERRBIACURCOUNT";
                /// <summary>
                /// Serbia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SERRBIAGDPATE";
                /// <summary>
                /// Serbia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SERRBIAGDPRATE";
                /// <summary>
                /// Serbia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SERRBIAINDCTION";
                /// <summary>
                /// Serbia Inflation Rate
                /// </summary>
                public const string InflationRate = "SERRBIAINFNRATE";
                /// <summary>
                /// Serbia Interest Rate
                /// </summary>
                public const string InterestRate = "SERRBIAINTTRATE";
                /// <summary>
                /// Serbia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SERRBIARETSYOY";
                /// <summary>
                /// Serbia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SERRBIAUNETRATE";
            }
            public static class Seychelles
            {
                /// <summary>
                /// Seychelles Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SEYLLESBALRADE";
                /// <summary>
                /// Seychelles Inflation Rate
                /// </summary>
                public const string InflationRate = "SEYLLESINFNRATE";
            }
            public static class Singapore
            {
                /// <summary>
                /// Singapore Calendar
                /// </summary>
                public const string Calendar = "SGP-CALENDAR";
                /// <summary>
                /// Singapore GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SGDPQOQ";
                /// <summary>
                /// Singapore GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SGDPYOY";
                /// <summary>
                /// Singapore Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SGPCORECPIRATE";
                /// <summary>
                /// Singapore Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SGPRETAILSALESMOM";
                /// <summary>
                /// Singapore Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SGPRETAILSALESYOY";
                /// <summary>
                /// Singapore Inflation Rate
                /// </summary>
                public const string InflationRate = "SICPIYOY";
                /// <summary>
                /// Singapore Current Account
                /// </summary>
                public const string CurrentAccount = "SICUBAL";
                /// <summary>
                /// Singapore Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SIIPYOYPCT";
                /// <summary>
                /// Singapore Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SINGAPOREBUSCON";
                /// <summary>
                /// Singapore Composite Pmi
                /// </summary>
                public const string CompositePmi = "SINGAPORECOMPMI";
                /// <summary>
                /// Singapore Export Prices
                /// </summary>
                public const string ExportPrices = "SINGAPOREEXPPRI";
                /// <summary>
                /// Singapore Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SINGAPOREFOREXCRES";
                /// <summary>
                /// Singapore Housing Index
                /// </summary>
                public const string HousingIndex = "SINGAPOREHOUIND";
                /// <summary>
                /// Singapore Import Prices
                /// </summary>
                public const string ImportPrices = "SINGAPOREIMPPRI";
                /// <summary>
                /// Singapore Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SINGAPOREINDPROMOM";
                /// <summary>
                /// Singapore Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SINGAPOREINFRATMOM";
                /// <summary>
                /// Singapore Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "SINGAPORELOATOPRISEC";
                /// <summary>
                /// Singapore Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "SINGAPOREMANPMI";
                /// <summary>
                /// Singapore Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SINGAPOREPROPRICHA";
                /// <summary>
                /// Singapore Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SIQUTOTA";
                /// <summary>
                /// Singapore Exports of Non-oil Domestic Exports Of
                /// </summary>
                public const string ExportsOfNon_oilDomesticExportsOf = "SPEXPNOOA";
                /// <summary>
                /// Singapore Domestic Exports of Non Oil (nodx) (%yoy)
                /// </summary>
                public const string DomesticExportsOfNonOilNODXPercentYoy = "SPEXPNOOR";
                /// <summary>
                /// Singapore Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "STRDE";
            }
            public static class Slovakia
            {
                /// <summary>
                /// Slovakia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SLOVAKIABC";
                /// <summary>
                /// Slovakia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SLOVAKIABT";
                /// <summary>
                /// Slovakia Current Account
                /// </summary>
                public const string CurrentAccount = "SLOVAKIACA";
                /// <summary>
                /// Slovakia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SLOVAKIACC";
                /// <summary>
                /// Slovakia Construction Output
                /// </summary>
                public const string ConstructionOutput = "SLOVAKIACONOUT";
                /// <summary>
                /// Slovakia Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "SLOVAKIACORCONPRI";
                /// <summary>
                /// Slovakia Exports
                /// </summary>
                public const string Exports = "SLOVAKIAEX";
                /// <summary>
                /// Slovakia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SLOVAKIAGDPQOQ";
                /// <summary>
                /// Slovakia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SLOVAKIAGDPYOY";
                /// <summary>
                /// Slovakia Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SLOVAKIAHARCONPRI";
                /// <summary>
                /// Slovakia Imports
                /// </summary>
                public const string Imports = "SLOVAKIAIM";
                /// <summary>
                /// Slovakia Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SLOVAKIAINDPROMOM";
                /// <summary>
                /// Slovakia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SLOVAKIAINFRATMOM";
                /// <summary>
                /// Slovakia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SLOVAKIAIP";
                /// <summary>
                /// Slovakia Inflation Rate
                /// </summary>
                public const string InflationRate = "SLOVAKIAIR";
                /// <summary>
                /// Slovakia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SLOVAKIAUR";
                /// <summary>
                /// Slovakia Wage Growth
                /// </summary>
                public const string WageGrowth = "SLOVAKIAWAGGRO";
                /// <summary>
                /// Slovakia Calendar
                /// </summary>
                public const string Calendar = "SVK-CALENDAR";
                /// <summary>
                /// Slovakia Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SVKCORECPIRATE";
                /// <summary>
                /// Slovakia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SVKRETAILSALESMOM";
                /// <summary>
                /// Slovakia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SVKRETAILSALESYOY";
            }
            public static class Slovenia
            {
                /// <summary>
                /// Slovenia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "EUCCSI";
                /// <summary>
                /// Slovenia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "EUGNSIQQ";
                /// <summary>
                /// Slovenia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SLOVENIABC";
                /// <summary>
                /// Slovenia Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SLOVENIAHARCONPRI";
                /// <summary>
                /// Slovenia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SLOVENIAPROPRICHA";
                /// <summary>
                /// Slovenia Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SLOVENIATOUARR";
                /// <summary>
                /// Slovenia Inflation Rate
                /// </summary>
                public const string InflationRate = "SVCPYOY";
                /// <summary>
                /// Slovenia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SVGDCYOY";
                /// <summary>
                /// Slovenia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SVIPTYOY";
                /// <summary>
                /// Slovenia Calendar
                /// </summary>
                public const string Calendar = "SVN-CALENDAR";
                /// <summary>
                /// Slovenia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SVNRETAILSALESMOM";
                /// <summary>
                /// Slovenia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SVNRETAILSALESYOY";
                /// <summary>
                /// Slovenia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SVTBBALE";
                /// <summary>
                /// Slovenia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SVUER";
            }
            public static class SouthAfrica
            {
                /// <summary>
                /// SouthAfrica Calendar
                /// </summary>
                public const string Calendar = "ZAF-CALENDAR";
                /// <summary>
                /// SouthAfrica Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "EHUPZA";
                /// <summary>
                /// SouthAfrica Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SABZCONF";
                /// <summary>
                /// SouthAfrica Inflation Rate
                /// </summary>
                public const string InflationRate = "SACPIYOY";
                /// <summary>
                /// SouthAfrica Current Account
                /// </summary>
                public const string CurrentAccount = "SACTLVL";
                /// <summary>
                /// SouthAfrica Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SACWC";
                /// <summary>
                /// SouthAfrica GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SAGDPQOQ";
                /// <summary>
                /// SouthAfrica GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SAGDPYOY";
                /// <summary>
                /// SouthAfrica Interest Rate
                /// </summary>
                public const string InterestRate = "SARPRT";
                /// <summary>
                /// SouthAfrica Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SATBAL";
                /// <summary>
                /// SouthAfrica Bank Lending Rate
                /// </summary>
                public const string BankLendingRate = "SOUTHAFRIBANLENRAT";
                /// <summary>
                /// SouthAfrica Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SOUTHAFRICAIP";
                /// <summary>
                /// SouthAfrica Car Registrations
                /// </summary>
                public const string CarRegistrations = "SOUTHAFRICARREG";
                /// <summary>
                /// SouthAfrica Coincident Index
                /// </summary>
                public const string CoincidentIndex = "SOUTHAFRICOIIND";
                /// <summary>
                /// SouthAfrica Composite Pmi
                /// </summary>
                public const string CompositePmi = "SOUTHAFRICOMPMI";
                /// <summary>
                /// SouthAfrica Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SOUTHAFRICORINFRAT";
                /// <summary>
                /// SouthAfrica Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SOUTHAFRIFOREXCRES";
                /// <summary>
                /// SouthAfrica Gold Production
                /// </summary>
                public const string GoldProduction = "SOUTHAFRIGOLPRO";
                /// <summary>
                /// SouthAfrica Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SOUTHAFRIINDPROMOM";
                /// <summary>
                /// SouthAfrica Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SOUTHAFRIINFRATMOM";
                /// <summary>
                /// SouthAfrica Lending Rate
                /// </summary>
                public const string LendingRate = "SOUTHAFRILENRAT";
                /// <summary>
                /// SouthAfrica Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "SOUTHAFRILOATOPRISEC";
                /// <summary>
                /// SouthAfrica Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "SOUTHAFRIMANPMI";
                /// <summary>
                /// SouthAfrica Mining Production
                /// </summary>
                public const string MiningProduction = "SOUTHAFRIMINPRO";
                /// <summary>
                /// SouthAfrica Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "SOUTHAFRIMONSUPM3";
                /// <summary>
                /// SouthAfrica Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "SOUTHAFRIPRISECCRE";
                /// <summary>
                /// SouthAfrica Producer Prices
                /// </summary>
                public const string ProducerPrices = "SOUTHAFRIPROPRI";
                /// <summary>
                /// SouthAfrica Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SOUTHAFRIPROPRICHA";
                /// <summary>
                /// SouthAfrica Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "SOUTHAFRITOTVEHSAL";
                /// <summary>
                /// SouthAfrica Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "SOUTHAFRIUNEPER";
                /// <summary>
                /// SouthAfrica Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ZAFRETAILSALESMOM";
                /// <summary>
                /// SouthAfrica Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ZAFRETAILSALESYOY";
            }
            public static class SouthKorea
            {
                /// <summary>
                /// SouthKorea Calendar
                /// </summary>
                public const string Calendar = "KOR-CALENDAR";
                /// <summary>
                /// SouthKorea Current Account
                /// </summary>
                public const string CurrentAccount = "KOBPCB";
                /// <summary>
                /// SouthKorea Inflation Rate
                /// </summary>
                public const string InflationRate = "KOCPIYOY";
                /// <summary>
                /// SouthKorea Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "KOEAUERS";
                /// <summary>
                /// SouthKorea Exports
                /// </summary>
                public const string Exports = "KOEXTOT";
                /// <summary>
                /// SouthKorea GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "KOGDPQOQ";
                /// <summary>
                /// SouthKorea GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "KOGDPYOY";
                /// <summary>
                /// SouthKorea Imports
                /// </summary>
                public const string Imports = "KOIMTOT";
                /// <summary>
                /// SouthKorea Industrial Production
                /// </summary>
                public const string IndustrialProduction = "KOIPIY";
                /// <summary>
                /// SouthKorea Interest Rate
                /// </summary>
                public const string InterestRate = "KORP7DR";
                /// <summary>
                /// SouthKorea Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "KORRETAILSALESMOM";
                /// <summary>
                /// SouthKorea Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "KORRETAILSALESYOY";
                /// <summary>
                /// SouthKorea Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KOTRBAL";
                /// <summary>
                /// SouthKorea Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SKCOEXPC";
                /// <summary>
                /// SouthKorea Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SOUTHKOREABC";
                /// <summary>
                /// SouthKorea Construction Output
                /// </summary>
                public const string ConstructionOutput = "SOUTHKORECONOUT";
                /// <summary>
                /// SouthKorea Export Prices
                /// </summary>
                public const string ExportPrices = "SOUTHKOREEXPPRI";
                /// <summary>
                /// SouthKorea External Debt
                /// </summary>
                public const string ExternalDebt = "SOUTHKOREEXTDEB";
                /// <summary>
                /// SouthKorea Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SOUTHKOREFOREXCRES";
                /// <summary>
                /// SouthKorea Import Prices
                /// </summary>
                public const string ImportPrices = "SOUTHKOREIMPPRI";
                /// <summary>
                /// SouthKorea Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SOUTHKOREINDPROMOM";
                /// <summary>
                /// SouthKorea Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SOUTHKOREINFRATMOM";
                /// <summary>
                /// SouthKorea Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SOUTHKOREMANPMI";
                /// <summary>
                /// SouthKorea Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "SOUTHKOREMANPRO";
                /// <summary>
                /// SouthKorea Producer Prices
                /// </summary>
                public const string ProducerPrices = "SOUTHKOREPROPRI";
                /// <summary>
                /// SouthKorea Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SOUTHKOREPROPRICHA";
            }
            public static class Spain
            {
                /// <summary>
                /// Spain Calendar
                /// </summary>
                public const string Calendar = "ESP-CALENDAR";
                /// <summary>
                /// Spain Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ESPRETAILSALESMOM";
                /// <summary>
                /// Spain Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ESPRETAILSALESYOY";
                /// <summary>
                /// Spain Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSPG10YR";
                /// <summary>
                /// Spain 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "SPAIN3MBY";
                /// <summary>
                /// Spain 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "SPAIN3YNY";
                /// <summary>
                /// Spain 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "SPAIN5WBY";
                /// <summary>
                /// Spain 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "SPAIN5YNY";
                /// <summary>
                /// Spain 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "SPAIN6MBY";
                /// <summary>
                /// Spain Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SPAINBC";
                /// <summary>
                /// Spain Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SPAINCC";
                /// <summary>
                /// Spain Factory Orders
                /// </summary>
                public const string FactoryOrders = "SPAINFACORD";
                /// <summary>
                /// Spain Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SPAINHARCONPRI";
                /// <summary>
                /// Spain Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SPAININFRATMOM";
                /// <summary>
                /// Spain Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SPAINIP";
                /// <summary>
                /// Spain Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "SPAINMANPMI";
                /// <summary>
                /// Spain Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SPAINPROPRICHA";
                /// <summary>
                /// Spain Services Pmi
                /// </summary>
                public const string ServicesPmi = "SPAINSERPMI";
                /// <summary>
                /// Spain Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "SPAINTOTVEHSAL";
                /// <summary>
                /// Spain Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SPAINTOUARR";
                /// <summary>
                /// Spain Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "SPAINUNECHA";
                /// <summary>
                /// Spain Current Account
                /// </summary>
                public const string CurrentAccount = "SPCAEURO";
                /// <summary>
                /// Spain Inflation Rate
                /// </summary>
                public const string InflationRate = "SPIPCYOY";
                /// <summary>
                /// Spain GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SPNAGDPQ";
                /// <summary>
                /// Spain GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SPNAGDPY";
                /// <summary>
                /// Spain Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SPTBEUBL";
                /// <summary>
                /// Spain Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTES";
            }
            public static class SriLanka
            {
                /// <summary>
                /// SriLanka Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SRI-LANKABT";
                /// <summary>
                /// SriLanka Current Account
                /// </summary>
                public const string CurrentAccount = "SRI-LANKACA";
                /// <summary>
                /// SriLanka GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SRI-LANKAGDPYOY";
                /// <summary>
                /// SriLanka Interest Rate
                /// </summary>
                public const string InterestRate = "SRI-LANKAINTRATE";
                /// <summary>
                /// SriLanka Inflation Rate
                /// </summary>
                public const string InflationRate = "SRI-LANKAIR";
                /// <summary>
                /// SriLanka Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SRI-LANKAUR";
                /// <summary>
                /// SriLanka Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SRILANKAINDPRO";
                /// <summary>
                /// SriLanka Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SRILANKAMANPMI";
                /// <summary>
                /// SriLanka Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "SRILANKAMANPRO";
                /// <summary>
                /// SriLanka Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SRILANKAPROPRICHA";
                /// <summary>
                /// SriLanka Services Pmi
                /// </summary>
                public const string ServicesPmi = "SRILANKASERPMI";
                /// <summary>
                /// SriLanka Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SRILANKATOUARR";
            }
            public static class Suriname
            {
                /// <summary>
                /// Suriname Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SURNAMEBALRADE";
                /// <summary>
                /// Suriname Current Account
                /// </summary>
                public const string CurrentAccount = "SURNAMECURCOUNT";
                /// <summary>
                /// Suriname Inflation Rate
                /// </summary>
                public const string InflationRate = "SURNAMEINFNRATE";
            }
            public static class Sweden
            {
                /// <summary>
                /// Sweden Calendar
                /// </summary>
                public const string Calendar = "CALENDARSWEDEN";
                /// <summary>
                /// Sweden Current Account
                /// </summary>
                public const string CurrentAccount = "SWCA";
                /// <summary>
                /// Sweden Inflation Rate
                /// </summary>
                public const string InflationRate = "SWCPYOY";
                /// <summary>
                /// Sweden Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SWEDENINDPROMOM";
                /// <summary>
                /// Sweden Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SWECCI";
                /// <summary>
                /// Sweden Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SWECORECPIRATE";
                /// <summary>
                /// Sweden Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SWEDENBC";
                /// <summary>
                /// Sweden Business Inventories
                /// </summary>
                public const string BusinessInventories = "SWEDENBUSINV";
                /// <summary>
                /// Sweden Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "SWEDENCAPUTI";
                /// <summary>
                /// Sweden Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "SWEDENCORCONPRI";
                /// <summary>
                /// Sweden Household Spending
                /// </summary>
                public const string HouseholdSpending = "SWEDENHOUSPE";
                /// <summary>
                /// Sweden Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "SWEDENINFEXP";
                /// <summary>
                /// Sweden Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SWEDENINFRATMOM";
                /// <summary>
                /// Sweden Loan Growth
                /// </summary>
                public const string LoanGrowth = "SWEDENLOAGRO";
                /// <summary>
                /// Sweden Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SWEDENMANPMI";
                /// <summary>
                /// Sweden New Orders
                /// </summary>
                public const string NewOrders = "SWEDENNEWORD";
                /// <summary>
                /// Sweden Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "SWEDENPRISECCRE";
                /// <summary>
                /// Sweden Producer Prices
                /// </summary>
                public const string ProducerPrices = "SWEDENPROPRI";
                /// <summary>
                /// Sweden Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SWEDENPROPRICHA";
                /// <summary>
                /// Sweden Services Pmi
                /// </summary>
                public const string ServicesPmi = "SWEDENSERPMI";
                /// <summary>
                /// Sweden Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SWERETAILSALESMOM";
                /// <summary>
                /// Sweden Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SWERETAILSALESYOY";
                /// <summary>
                /// Sweden GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SWGDPAQQ";
                /// <summary>
                /// Sweden GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SWGDPAYY";
                /// <summary>
                /// Sweden Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SWIPIYOY";
                /// <summary>
                /// Sweden Interest Rate
                /// </summary>
                public const string InterestRate = "SWRRATEI";
                /// <summary>
                /// Sweden Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SWTBAL";
                /// <summary>
                /// Sweden Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTSE";
            }
            public static class Switzerland
            {
                /// <summary>
                /// Switzerland Calendar
                /// </summary>
                public const string Calendar = "CHF-CALENDAR";
                /// <summary>
                /// Switzerland Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CHERETAILSALESMOM";
                /// <summary>
                /// Switzerland Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CHERETAILSALESYOY";
                /// <summary>
                /// Switzerland Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SWITZERLANDBC";
                /// <summary>
                /// Switzerland Factory Orders
                /// </summary>
                public const string FactoryOrders = "SWITZERLANFACORD";
                /// <summary>
                /// Switzerland Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SWITZERLANFOREXCRES";
                /// <summary>
                /// Switzerland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SWITZERLANINFRATMOM";
                /// <summary>
                /// Switzerland Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SWITZERLANMANPMI";
                /// <summary>
                /// Switzerland Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "SWITZERLANNONFARPAY";
                /// <summary>
                /// Switzerland Producer Prices
                /// </summary>
                public const string ProducerPrices = "SWITZERLANPROPRI";
                /// <summary>
                /// Switzerland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SWITZERLANPROPRICHA";
                /// <summary>
                /// Switzerland Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "SWITZERLANZEWECOSENI";
                /// <summary>
                /// Switzerland Current Account
                /// </summary>
                public const string CurrentAccount = "SZCA";
                /// <summary>
                /// Switzerland Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SZCCI";
                /// <summary>
                /// Switzerland Inflation Rate
                /// </summary>
                public const string InflationRate = "SZCPIYOY";
                /// <summary>
                /// Switzerland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SZGDPCQQ";
                /// <summary>
                /// Switzerland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SZGDPCYY";
                /// <summary>
                /// Switzerland Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SZIPIYOY";
                /// <summary>
                /// Switzerland Interest Rate
                /// </summary>
                public const string InterestRate = "SZLTTR";
                /// <summary>
                /// Switzerland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SZTBAL";
                /// <summary>
                /// Switzerland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SZUEUEA";
            }
            public static class Taiwan
            {
                /// <summary>
                /// Taiwan Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "TAIIWANCONDENCE";
                /// <summary>
                /// Taiwan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "TAIWANBT";
                /// <summary>
                /// Taiwan Current Account
                /// </summary>
                public const string CurrentAccount = "TAIWANCA";
                /// <summary>
                /// Taiwan Inflation Rate
                /// </summary>
                public const string InflationRate = "TAIWANCPI";
                /// <summary>
                /// Taiwan Exports
                /// </summary>
                public const string Exports = "TAIWANEX";
                /// <summary>
                /// Taiwan Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "TAIWANFOREXCRES";
                /// <summary>
                /// Taiwan GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "TAIWANGDPQOQ";
                /// <summary>
                /// Taiwan GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "TAIWANGDPYOY";
                /// <summary>
                /// Taiwan Imports
                /// </summary>
                public const string Imports = "TAIWANIM";
                /// <summary>
                /// Taiwan Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "TAIWANINFRATMOM";
                /// <summary>
                /// Taiwan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "TAIWANIP";
                /// <summary>
                /// Taiwan Interest Rate
                /// </summary>
                public const string InterestRate = "TAIWANIR";
                /// <summary>
                /// Taiwan Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "TAIWANMANPMI";
                /// <summary>
                /// Taiwan Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "TAIWANMONSUPM2";
                /// <summary>
                /// Taiwan New Orders
                /// </summary>
                public const string NewOrders = "TAIWANNEWORD";
                /// <summary>
                /// Taiwan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "TAIWANPROPRICHA";
                /// <summary>
                /// Taiwan Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "TAIWANUR";
                /// <summary>
                /// Taiwan Calendar
                /// </summary>
                public const string Calendar = "TWN-CALENDAR";
                /// <summary>
                /// Taiwan Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "TWNRETAILSALESYOY";
            }
            public static class Tajikistan
            {
                /// <summary>
                /// Tajikistan Inflation Rate
                /// </summary>
                public const string InflationRate = "TAJSTANINFNRATE";
            }
            public static class Tanzania
            {
                /// <summary>
                /// Tanzania GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "TANZANIAGDPYOY";
                /// <summary>
                /// Tanzania Inflation Rate
                /// </summary>
                public const string InflationRate = "TANZANIAIR";
            }
            public static class Thailand
            {
                /// <summary>
                /// Thailand Interest Rate
                /// </summary>
                public const string InterestRate = "BTRR1DAY";
                /// <summary>
                /// Thailand Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "THACORECPIRATE";
                /// <summary>
                /// Thailand Business Confidence
                /// </summary>
                public const string BusinessConfidence = "THAILANDBC";
                /// <summary>
                /// Thailand Coincident Index
                /// </summary>
                public const string CoincidentIndex = "THAILANDCOIIND";
                /// <summary>
                /// Thailand Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "THAILANDFOREXCRES";
                /// <summary>
                /// Thailand Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "THAILANDMANPMI";
                /// <summary>
                /// Thailand Personal Spending
                /// </summary>
                public const string PersonalSpending = "THAILANDPERSPE";
                /// <summary>
                /// Thailand Private Investment
                /// </summary>
                public const string PrivateInvestment = "THAILANDPRIINV";
                /// <summary>
                /// Thailand Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "THAILANDTOTVEHSAL";
                /// <summary>
                /// Thailand Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "THALANDRETSYOY";
                /// <summary>
                /// Thailand Current Account
                /// </summary>
                public const string CurrentAccount = "THCA";
                /// <summary>
                /// Thailand Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "THCCI";
                /// <summary>
                /// Thailand Inflation Rate
                /// </summary>
                public const string InflationRate = "THCPIYOY";
                /// <summary>
                /// Thailand GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "THG-PQQ";
                /// <summary>
                /// Thailand GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "THG-PQY";
                /// <summary>
                /// Thailand Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "THLMUERT";
                /// <summary>
                /// Thailand Industrial Production
                /// </summary>
                public const string IndustrialProduction = "THMPIYOY";
                /// <summary>
                /// Thailand Exports
                /// </summary>
                public const string Exports = "THNFEXP";
                /// <summary>
                /// Thailand Imports
                /// </summary>
                public const string Imports = "THNFIMP";
                /// <summary>
                /// Thailand Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "THNFTB";
            }
            public static class TrinidadAndTobago
            {
                /// <summary>
                /// TrinidadAndTobago Interest Rate
                /// </summary>
                public const string InterestRate = "TRIGOINTTRATE";
            }
            public static class Tunisia
            {
                /// <summary>
                /// Tunisia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "TUNISIABT";
                /// <summary>
                /// Tunisia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "TUNISIAGDPQOQ";
                /// <summary>
                /// Tunisia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "TUNISIAGDPYOY";
                /// <summary>
                /// Tunisia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "TUNISIAINDPRO";
                /// <summary>
                /// Tunisia Interest Rate
                /// </summary>
                public const string InterestRate = "TUNISIAINTRATE";
                /// <summary>
                /// Tunisia Inflation Rate
                /// </summary>
                public const string InflationRate = "TUNISIAIR";
                /// <summary>
                /// Tunisia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "TUNISIAPROPRICHA";
            }
            public static class Turkey
            {
                /// <summary>
                /// Turkey Calendar
                /// </summary>
                public const string Calendar = "TUR-CALENDAR";
                /// <summary>
                /// Turkey Current Account
                /// </summary>
                public const string CurrentAccount = "TUCAL";
                /// <summary>
                /// Turkey Inflation Rate
                /// </summary>
                public const string InflationRate = "TUCPIY";
                /// <summary>
                /// Turkey GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "TUGPCOQS";
                /// <summary>
                /// Turkey GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "TUGPCOYR";
                /// <summary>
                /// Turkey Interest Rate
                /// </summary>
                public const string InterestRate = "TUIBON";
                /// <summary>
                /// Turkey Industrial Production
                /// </summary>
                public const string IndustrialProduction = "TUIPIYOY";
                /// <summary>
                /// Turkey Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "TURFRINRDPST";
                /// <summary>
                /// Turkey Business Confidence
                /// </summary>
                public const string BusinessConfidence = "TURKEYBC";
                /// <summary>
                /// Turkey Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "TURKEYCAPUTI";
                /// <summary>
                /// Turkey Car Production
                /// </summary>
                public const string CarProduction = "TURKEYCARPRO";
                /// <summary>
                /// Turkey Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "TURKEYCC";
                /// <summary>
                /// Turkey Construction Output
                /// </summary>
                public const string ConstructionOutput = "TURKEYCONOUT";
                /// <summary>
                /// Turkey Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "TURKEYECOOPTIND";
                /// <summary>
                /// Turkey Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "TURKEYGOVBUDVAL";
                /// <summary>
                /// Turkey Government Debt
                /// </summary>
                public const string GovernmentDebt = "TURKEYGOVDEB";
                /// <summary>
                /// Turkey Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "TURKEYINDPROMOM";
                /// <summary>
                /// Turkey Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "TURKEYINFRATMOM";
                /// <summary>
                /// Turkey Lending Rate
                /// </summary>
                public const string LendingRate = "TURKEYLENRAT";
                /// <summary>
                /// Turkey Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "TURKEYMANPMI";
                /// <summary>
                /// Turkey Producer Prices
                /// </summary>
                public const string ProducerPrices = "TURKEYPROPRI";
                /// <summary>
                /// Turkey Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "TURKEYPROPRICHA";
                /// <summary>
                /// Turkey Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "TURKEYTOUARR";
                /// <summary>
                /// Turkey Tourism Revenues
                /// </summary>
                public const string TourismRevenues = "TURKEYTOUREV";
                /// <summary>
                /// Turkey Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "TURRETAILSALESMOM";
                /// <summary>
                /// Turkey Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "TURRETAILSALESYOY";
                /// <summary>
                /// Turkey Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "TUTBAL";
                /// <summary>
                /// Turkey Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "TUUNR";
            }
            public static class Uganda
            {
                /// <summary>
                /// Uganda Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "UGAANDABALRADE";
                /// <summary>
                /// Uganda GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "UGAANDAGDPATE";
                /// <summary>
                /// Uganda Inflation Rate
                /// </summary>
                public const string InflationRate = "UGAANDAINFNRATE";
                /// <summary>
                /// Uganda Interest Rate
                /// </summary>
                public const string InterestRate = "UGAANDAINTTRATE";
                /// <summary>
                /// Uganda Business Confidence
                /// </summary>
                public const string BusinessConfidence = "UGANDABUSCON";
                /// <summary>
                /// Uganda Composite Pmi
                /// </summary>
                public const string CompositePmi = "UGANDACOMPMI";
                /// <summary>
                /// Uganda Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "UGANDAPROPRICHA";
            }
            public static class Ukraine
            {
                /// <summary>
                /// Ukraine Calendar
                /// </summary>
                public const string Calendar = "UKR-CALENDAR";
                /// <summary>
                /// Ukraine Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "UKRAINEBT";
                /// <summary>
                /// Ukraine Current Account
                /// </summary>
                public const string CurrentAccount = "UKRAINECA";
                /// <summary>
                /// Ukraine Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "UKRAINEFOREXCRES";
                /// <summary>
                /// Ukraine GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "UKRAINEGDPQOQ";
                /// <summary>
                /// Ukraine GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "UKRAINEGDPYOY";
                /// <summary>
                /// Ukraine Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "UKRAINEINDPROMOM";
                /// <summary>
                /// Ukraine Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "UKRAINEINFRATMOM";
                /// <summary>
                /// Ukraine Interest Rate
                /// </summary>
                public const string InterestRate = "UKRAINEINTRATE";
                /// <summary>
                /// Ukraine Industrial Production
                /// </summary>
                public const string IndustrialProduction = "UKRAINEIP";
                /// <summary>
                /// Ukraine Inflation Rate
                /// </summary>
                public const string InflationRate = "UKRAINEIR";
                /// <summary>
                /// Ukraine Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "UKRAINEMONSUPM3";
                /// <summary>
                /// Ukraine Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "UKRAINERETSALMOM";
                /// <summary>
                /// Ukraine Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "UKRAINERETSALYOY";
                /// <summary>
                /// Ukraine Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UKRAINEUR";
            }
            public static class UnitedArabEmirates
            {
                /// <summary>
                /// UnitedArabEmirates Interest Rate
                /// </summary>
                public const string InterestRate = "EIBO1M";
                /// <summary>
                /// UnitedArabEmirates Inflation Rate
                /// </summary>
                public const string InflationRate = "UACIGPY";
                /// <summary>
                /// UnitedArabEmirates Loan Growth
                /// </summary>
                public const string LoanGrowth = "UNITEDARALOAGRO";
                /// <summary>
                /// UnitedArabEmirates Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "UNITEDARAMANPMI";
                /// <summary>
                /// UnitedArabEmirates Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "UNITEDARAMONSUPM3";
            }
            public static class UnitedKingdom
            {
                /// <summary>
                /// UnitedKingdom Calendar
                /// </summary>
                public const string Calendar = "GBRINDICATORS";
                /// <summary>
                /// UnitedKingdom Business Confidence
                /// </summary>
                public const string BusinessConfidence = "EUR1UK";
                /// <summary>
                /// UnitedKingdom Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "GBRCORECPIRATE";
                /// <summary>
                /// UnitedKingdom Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "GBRRETAILSALESMOM";
                /// <summary>
                /// UnitedKingdom Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "GBRRETAILSALESYOY";
                /// <summary>
                /// UnitedKingdom Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GUKG10";
                /// <summary>
                /// UnitedKingdom Interest Rate
                /// </summary>
                public const string InterestRate = "UKBRBASE";
                /// <summary>
                /// UnitedKingdom Current Account
                /// </summary>
                public const string CurrentAccount = "UKCA";
                /// <summary>
                /// UnitedKingdom Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "UKCCI";
                /// <summary>
                /// UnitedKingdom GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "UKGRYBZQ";
                /// <summary>
                /// UnitedKingdom GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "UKGRYBZY";
                /// <summary>
                /// UnitedKingdom Industrial Production
                /// </summary>
                public const string IndustrialProduction = "UKIPIYOY";
                /// <summary>
                /// UnitedKingdom Inflation Rate
                /// </summary>
                public const string InflationRate = "UKRPCJYR";
                /// <summary>
                /// UnitedKingdom Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "UKTBTTBA";
                /// <summary>
                /// UnitedKingdom Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UKUEILOR";
                /// <summary>
                /// UnitedKingdom 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "UNITEDKIN3YBY";
                /// <summary>
                /// UnitedKingdom 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "UNITEDKIN5YEANOTYIE";
                /// <summary>
                /// UnitedKingdom Car Registrations
                /// </summary>
                public const string CarRegistrations = "UNITEDKINCARREG";
                /// <summary>
                /// UnitedKingdom Claimant Count Change
                /// </summary>
                public const string ClaimantCountChange = "UNITEDKINCLACOUCHA";
                /// <summary>
                /// UnitedKingdom Composite Pmi
                /// </summary>
                public const string CompositePmi = "UNITEDKINCOMPMI";
                /// <summary>
                /// UnitedKingdom Consumer Credit
                /// </summary>
                public const string ConsumerCredit = "UNITEDKINCONCRE";
                /// <summary>
                /// UnitedKingdom Construction Orders
                /// </summary>
                public const string ConstructionOrders = "UNITEDKINCONORD";
                /// <summary>
                /// UnitedKingdom Construction Output
                /// </summary>
                public const string ConstructionOutput = "UNITEDKINCONOUT";
                /// <summary>
                /// UnitedKingdom Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "UNITEDKINCONPMI";
                /// <summary>
                /// UnitedKingdom Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "UNITEDKINCORCONPRI";
                /// <summary>
                /// UnitedKingdom Core Producer Prices
                /// </summary>
                public const string CoreProducerPrices = "UNITEDKINCORPROPRI";
                /// <summary>
                /// UnitedKingdom Employment Change
                /// </summary>
                public const string EmploymentChange = "UNITEDKINEMPCHA";
                /// <summary>
                /// UnitedKingdom Factory Orders
                /// </summary>
                public const string FactoryOrders = "UNITEDKINFACORD";
                /// <summary>
                /// UnitedKingdom Government Debt
                /// </summary>
                public const string GovernmentDebt = "UNITEDKINGOVDEB";
                /// <summary>
                /// UnitedKingdom Housing Index
                /// </summary>
                public const string HousingIndex = "UNITEDKINHOUIND";
                /// <summary>
                /// UnitedKingdom Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "UNITEDKININDPROMOM";
                /// <summary>
                /// UnitedKingdom Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "UNITEDKININFEXP";
                /// <summary>
                /// UnitedKingdom Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "UNITEDKININFRATMOM";
                /// <summary>
                /// UnitedKingdom Labour Costs
                /// </summary>
                public const string LabourCosts = "UNITEDKINLABCOS";
                /// <summary>
                /// UnitedKingdom Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "UNITEDKINLEAECOIND";
                /// <summary>
                /// UnitedKingdom Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "UNITEDKINMANPMI";
                /// <summary>
                /// UnitedKingdom Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "UNITEDKINMANPRO";
                /// <summary>
                /// UnitedKingdom Mortgage Approvals
                /// </summary>
                public const string MortgageApprovals = "UNITEDKINMORAPP";
                /// <summary>
                /// UnitedKingdom Mortgage Applications
                /// </summary>
                public const string MortgageApplications = "UNITEDKINMORAPPLI";
                /// <summary>
                /// UnitedKingdom Private Investment
                /// </summary>
                public const string PrivateInvestment = "UNITEDKINPRIINV";
                /// <summary>
                /// UnitedKingdom Productivity
                /// </summary>
                public const string Productivity = "UNITEDKINPRO";
                /// <summary>
                /// UnitedKingdom Producer Prices
                /// </summary>
                public const string ProducerPrices = "UNITEDKINPROPRI";
                /// <summary>
                /// UnitedKingdom Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "UNITEDKINPROPRICHA";
                /// <summary>
                /// UnitedKingdom Retail Price Index
                /// </summary>
                public const string RetailPriceIndex = "UNITEDKINRETPRIIND";
                /// <summary>
                /// UnitedKingdom Services Pmi
                /// </summary>
                public const string ServicesPmi = "UNITEDKINSERPMI";
                /// <summary>
                /// UnitedKingdom Wage Growth
                /// </summary>
                public const string WageGrowth = "UNITEDKINWAGGRO";
            }
            public static class UnitedStates
            {
                /// <summary>
                /// UnitedStates API Crude Oil Stock Change
                /// </summary>
                public const string ApiCrudeOilStockChange = "APICRUDEOIL";
                /// <summary>
                /// UnitedStates Calendar
                /// </summary>
                public const string Calendar = "USD-CALENDAR";
                /// <summary>
                /// UnitedStates Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CONCCONF";
                /// <summary>
                /// UnitedStates Inflation Rate
                /// </summary>
                public const string InflationRate = "CPI-YOY";
                /// <summary>
                /// UnitedStates Interest Rate
                /// </summary>
                public const string InterestRate = "FDTR";
                /// <summary>
                /// UnitedStates GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "GDP-CQOQ";
                /// <summary>
                /// UnitedStates GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GDP-CYOY";
                /// <summary>
                /// UnitedStates Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "IJCUSA";
                /// <summary>
                /// UnitedStates Industrial Production
                /// </summary>
                public const string IndustrialProduction = "IP-YOY";
                /// <summary>
                /// UnitedStates Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NAPMPMI";
                /// <summary>
                /// UnitedStates Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "NFP-TCH";
                /// <summary>
                /// UnitedStates Retail Sales Ex Autos
                /// </summary>
                public const string RetailSalesExAutos = "UNITEDSTARETSALEXAUT";
                /// <summary>
                /// UnitedStates Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "RSTAMOM";
                /// <summary>
                /// UnitedStates Exports
                /// </summary>
                public const string Exports = "TBEXTOT";
                /// <summary>
                /// UnitedStates Imports
                /// </summary>
                public const string Imports = "TBIMTOT";
                /// <summary>
                /// UnitedStates 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "UNITEDSTA2YEANOTYIE";
                /// <summary>
                /// UnitedStates 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "UNITEDSTA30YEABONYIE";
                /// <summary>
                /// UnitedStates 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "UNITEDSTA3MONBILYIE";
                /// <summary>
                /// UnitedStates 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "UNITEDSTA3YEANOTYIE";
                /// <summary>
                /// UnitedStates 4 Week Bill Yield
                /// </summary>
                public const string FourWeekBillYield = "UNITEDSTA4WEEBILYIE";
                /// <summary>
                /// UnitedStates 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "UNITEDSTA52WEEBILYIE";
                /// <summary>
                /// UnitedStates 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "UNITEDSTA5YEANOTYIE";
                /// <summary>
                /// UnitedStates 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "UNITEDSTA6MONBILYIE";
                /// <summary>
                /// UnitedStates 7 Year Note Yield
                /// </summary>
                public const string SevenYearNoteYield = "UNITEDSTA7YEANOTYIE";
                /// <summary>
                /// UnitedStates Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "UNITEDSTAADPEMPCHA";
                /// <summary>
                /// UnitedStates Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "UNITEDSTAAVEHOUEAR";
                /// <summary>
                /// UnitedStates Average Weekly Hours
                /// </summary>
                public const string AverageWeeklyHours = "UNITEDSTAAVEWEEHOU";
                /// <summary>
                /// UnitedStates Building Permits
                /// </summary>
                public const string BuildingPermits = "UNITEDSTABUIPER";
                /// <summary>
                /// UnitedStates Business Inventories
                /// </summary>
                public const string BusinessInventories = "UNITEDSTABUSINV";
                /// <summary>
                /// UnitedStates Capital Flows
                /// </summary>
                public const string CapitalFlows = "UNITEDSTACAPFLO";
                /// <summary>
                /// UnitedStates Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "UNITEDSTACAPUTI";
                /// <summary>
                /// UnitedStates Case Shiller Home Price Index
                /// </summary>
                public const string CaseShillerHomePriceIndex = "UNITEDSTACASSHIHOMPR";
                /// <summary>
                /// UnitedStates Challenger Job Cuts
                /// </summary>
                public const string ChallengerJobCuts = "UNITEDSTACHAJOBCUT";
                /// <summary>
                /// UnitedStates Chain Store Sales
                /// </summary>
                public const string ChainStoreSales = "UNITEDSTACHASTOSAL";
                /// <summary>
                /// UnitedStates Chicago Fed National Activity Index
                /// </summary>
                public const string ChicagoFedNationalActivityIndex = "UNITEDSTACHIFEDNATAC";
                /// <summary>
                /// UnitedStates Chicago Pmi
                /// </summary>
                public const string ChicagoPmi = "UNITEDSTACHIPMI";
                /// <summary>
                /// UnitedStates Composite Pmi
                /// </summary>
                public const string CompositePmi = "UNITEDSTACOMPMI";
                /// <summary>
                /// UnitedStates Consumer Credit
                /// </summary>
                public const string ConsumerCredit = "UNITEDSTACONCRE";
                /// <summary>
                /// UnitedStates Continuing Jobless Claims
                /// </summary>
                public const string ContinuingJoblessClaims = "UNITEDSTACONJOBCLA";
                /// <summary>
                /// UnitedStates Consumer Price Index (CPI)
                /// </summary>
                public const string ConsumerPriceIndex = "UNITEDSTACONPRIINDCP";
                /// <summary>
                /// UnitedStates Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "UNITEDSTACONSPE";
                /// <summary>
                /// UnitedStates Construction Spending
                /// </summary>
                public const string ConstructionSpending = "UNITEDSTACONTSPE";
                /// <summary>
                /// UnitedStates Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "UNITEDSTACORCONPRI";
                /// <summary>
                /// UnitedStates Core Pce Price Index
                /// </summary>
                public const string CorePcePriceIndex = "UNITEDSTACORPCEPRIIN";
                /// <summary>
                /// UnitedStates Corporate Profits
                /// </summary>
                public const string CorporateProfits = "UNITEDSTACORPRO";
                /// <summary>
                /// UnitedStates Core Producer Prices
                /// </summary>
                public const string CoreProducerPrices = "UNITEDSTACORPROPRI";
                /// <summary>
                /// UnitedStates Crude Oil Stocks Change
                /// </summary>
                public const string CrudeOilStocksChange = "UNITEDSTACRUOILSTOCH";
                /// <summary>
                /// UnitedStates Dallas Fed Manufacturing Index
                /// </summary>
                public const string DallasFedManufacturingIndex = "UNITEDSTADALFEDMANIN";
                /// <summary>
                /// UnitedStates Durable Goods Orders
                /// </summary>
                public const string DurableGoodsOrders = "UNITEDSTADURGOOORD";
                /// <summary>
                /// UnitedStates Durable Goods Orders Ex Defense
                /// </summary>
                public const string DurableGoodsOrdersExDefense = "UNITEDSTADURGOOORDED";
                /// <summary>
                /// UnitedStates Durable Goods Orders Ex Transportation
                /// </summary>
                public const string DurableGoodsOrdersExTransportation = "UNITEDSTADURGOOORDEX";
                /// <summary>
                /// UnitedStates Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "UNITEDSTAECOOPTIND";
                /// <summary>
                /// UnitedStates Employment Cost Index
                /// </summary>
                public const string EmploymentCostIndex = "UNITEDSTAEMPCOSIND";
                /// <summary>
                /// UnitedStates Existing Home Sales
                /// </summary>
                public const string ExistingHomeSales = "UNITEDSTAEXIHOMSAL";
                /// <summary>
                /// UnitedStates Export Prices
                /// </summary>
                public const string ExportPrices = "UNITEDSTAEXPPRI";
                /// <summary>
                /// UnitedStates Factory Orders
                /// </summary>
                public const string FactoryOrders = "UNITEDSTAFACORD";
                /// <summary>
                /// UnitedStates Factory Orders Ex Transportation
                /// </summary>
                public const string FactoryOrdersExTransportation = "UNITEDSTAFACORDEXTRA";
                /// <summary>
                /// UnitedStates Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "UNITEDSTAFORBONINV";
                /// <summary>
                /// UnitedStates Gasoline Stocks Change
                /// </summary>
                public const string GasolineStocksChange = "UNITEDSTAGASSTOCHA";
                /// <summary>
                /// UnitedStates Gdp Deflator
                /// </summary>
                public const string GdpDeflator = "UNITEDSTAGDPDEF";
                /// <summary>
                /// UnitedStates Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "UNITEDSTAGOVBUDVAL";
                /// <summary>
                /// UnitedStates Government Payrolls
                /// </summary>
                public const string GovernmentPayrolls = "UNITEDSTAGOVPAY";
                /// <summary>
                /// UnitedStates Housing Index
                /// </summary>
                public const string HousingIndex = "UNITEDSTAHOUIND";
                /// <summary>
                /// UnitedStates Housing Starts
                /// </summary>
                public const string HousingStarts = "UNITEDSTAHOUSTA";
                /// <summary>
                /// UnitedStates Import Prices
                /// </summary>
                public const string ImportPrices = "UNITEDSTAIMPPRI";
                /// <summary>
                /// UnitedStates Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "UNITEDSTAINDPROMOM";
                /// <summary>
                /// UnitedStates Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "UNITEDSTAINFEXP";
                /// <summary>
                /// UnitedStates Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "UNITEDSTAINFRATMOM";
                /// <summary>
                /// UnitedStates Ism New York Index
                /// </summary>
                public const string IsmNewYorkIndex = "UNITEDSTAISMNEWYORIN";
                /// <summary>
                /// UnitedStates Job Offers
                /// </summary>
                public const string JobOffers = "UNITEDSTAJOBOFF";
                /// <summary>
                /// UnitedStates Labour Costs
                /// </summary>
                public const string LabourCosts = "UNITEDSTALABCOS";
                /// <summary>
                /// UnitedStates Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "UNITEDSTALABFORPARRA";
                /// <summary>
                /// UnitedStates Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "UNITEDSTALEAECOIND";
                /// <summary>
                /// UnitedStates Labor Market Conditions Index
                /// </summary>
                public const string LaborMarketConditionsIndex = "UNITEDSTALMCI";
                /// <summary>
                /// UnitedStates Manufacturing Payrolls
                /// </summary>
                public const string ManufacturingPayrolls = "UNITEDSTAMANPAY";
                /// <summary>
                /// UnitedStates Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "UNITEDSTAMANPMI";
                /// <summary>
                /// UnitedStates Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "UNITEDSTAMANPRO";
                /// <summary>
                /// UnitedStates Mortgage Applications
                /// </summary>
                public const string MortgageApplications = "UNITEDSTAMORAPP";
                /// <summary>
                /// UnitedStates Mortgage Rate
                /// </summary>
                public const string MortgageRate = "UNITEDSTAMORRAT";
                /// <summary>
                /// UnitedStates Nahb Housing Market Index
                /// </summary>
                public const string NahbHousingMarketIndex = "UNITEDSTANAHHOUMARIN";
                /// <summary>
                /// UnitedStates Natural Gas Stocks Change
                /// </summary>
                public const string NaturalGasStocksChange = "UNITEDSTANATGASSTOCH";
                /// <summary>
                /// UnitedStates Net Long-term Tic Flows
                /// </summary>
                public const string NetLong_termTicFlows = "UNITEDSTANETLONTICFL";
                /// <summary>
                /// UnitedStates New Home Sales
                /// </summary>
                public const string NewHomeSales = "UNITEDSTANEWHOMSAL";
                /// <summary>
                /// UnitedStates Nfib Business Optimism Index
                /// </summary>
                public const string NfibBusinessOptimismIndex = "UNITEDSTANFIBUSOPTIN";
                /// <summary>
                /// UnitedStates Non Manufacturing PMI
                /// </summary>
                public const string NonManufacturingPmi = "UNITEDSTANONMANPMI";
                /// <summary>
                /// UnitedStates Nonfarm Payrolls Private
                /// </summary>
                public const string NonfarmPayrollsPrivate = "UNITEDSTANONPAY-PRI";
                /// <summary>
                /// UnitedStates Ny Empire State Manufacturing Index
                /// </summary>
                public const string NyEmpireStateManufacturingIndex = "UNITEDSTANYEMPSTAMAN";
                /// <summary>
                /// UnitedStates Pce Price Index
                /// </summary>
                public const string PcePriceIndex = "UNITEDSTAPCEPRIIND";
                /// <summary>
                /// UnitedStates Pending Home Sales
                /// </summary>
                public const string PendingHomeSales = "UNITEDSTAPENHOMSAL";
                /// <summary>
                /// UnitedStates Personal Income
                /// </summary>
                public const string PersonalIncome = "UNITEDSTAPERINC";
                /// <summary>
                /// UnitedStates Personal Spending
                /// </summary>
                public const string PersonalSpending = "UNITEDSTAPERSPE";
                /// <summary>
                /// UnitedStates Philadelphia Fed Manufacturing Index
                /// </summary>
                public const string PhiladelphiaFedManufacturingIndex = "UNITEDSTAPHIFEDMANIN";
                /// <summary>
                /// UnitedStates Productivity
                /// </summary>
                public const string Productivity = "UNITEDSTAPRO";
                /// <summary>
                /// UnitedStates Producer Prices
                /// </summary>
                public const string ProducerPrices = "UNITEDSTAPROPRI";
                /// <summary>
                /// UnitedStates Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "UNITEDSTAPROPRICHA";
                /// <summary>
                /// UnitedStates Redbook Index
                /// </summary>
                public const string RedbookIndex = "UNITEDSTAREDIND";
                /// <summary>
                /// UnitedStates Richmond Fed Manufacturing Index
                /// </summary>
                public const string RichmondFedManufacturingIndex = "UNITEDSTARICFEDMANIN";
                /// <summary>
                /// UnitedStates Services Pmi
                /// </summary>
                public const string ServicesPmi = "UNITEDSTASERPMI";
                /// <summary>
                /// UnitedStates Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "UNITEDSTATOTVEHSAL";
                /// <summary>
                /// UnitedStates Wholesale Inventories
                /// </summary>
                public const string WholesaleInventories = "UNITEDSTAWHOINV";
                /// <summary>
                /// UnitedStates Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "USACORECPIRATE";
                /// <summary>
                /// UnitedStates Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "USARETAILSALESYOY";
                /// <summary>
                /// UnitedStates Current Account
                /// </summary>
                public const string CurrentAccount = "USCABAL";
                /// <summary>
                /// UnitedStates Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "USGG10YR";
                /// <summary>
                /// UnitedStates Crude Oil Rigs
                /// </summary>
                public const string CrudeOilRigs = "USOILRIGS";
                /// <summary>
                /// UnitedStates Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "USTBTOT";
                /// <summary>
                /// UnitedStates Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "USURTOT";
            }
            public static class Uruguay
            {
                /// <summary>
                /// Uruguay Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "URUGUAYBALRADE";
                /// <summary>
                /// Uruguay Current Account
                /// </summary>
                public const string CurrentAccount = "URUGUAYCURCOUNT";
                /// <summary>
                /// Uruguay GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "URUGUAYGDPATE";
                /// <summary>
                /// Uruguay GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "URUGUAYGDPGRORAT";
                /// <summary>
                /// Uruguay Industrial Production
                /// </summary>
                public const string IndustrialProduction = "URUGUAYINDCTION";
                /// <summary>
                /// Uruguay Inflation Rate
                /// </summary>
                public const string InflationRate = "URUGUAYINFNRATE";
                /// <summary>
                /// Uruguay Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "URUGUAYUNETRATE";
            }
            public static class Venezuela
            {
                /// <summary>
                /// Venezuela Industrial Production
                /// </summary>
                public const string IndustrialProduction = "VENEZUELAINDPRO";
                /// <summary>
                /// Venezuela Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "VENEZUELARETSALMOM";
                /// <summary>
                /// Venezuela Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "VENUELARETSYOY";
                /// <summary>
                /// Venezuela Inflation Rate
                /// </summary>
                public const string InflationRate = "VNVPIYOY";
            }
            public static class Vietnam
            {
                /// <summary>
                /// Vietnam Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "VIETNAMBT";
                /// <summary>
                /// Vietnam Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "VIETNAMFORDIRINV";
                /// <summary>
                /// Vietnam GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "VIETNAMGDPYOY";
                /// <summary>
                /// Vietnam Industrial Production
                /// </summary>
                public const string IndustrialProduction = "VIETNAMIP";
                /// <summary>
                /// Vietnam Inflation Rate
                /// </summary>
                public const string InflationRate = "VIETNAMIR";
                /// <summary>
                /// Vietnam Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "VIETNAMMANPMI";
                /// <summary>
                /// Vietnam Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "VIETNAMRETSALYOY";
                /// <summary>
                /// Vietnam Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "VIETNAMTOUARR";
            }
            public static class Zambia
            {
                /// <summary>
                /// Zambia Composite Pmi
                /// </summary>
                public const string CompositePmi = "ZAMBIACOMPMI";
                /// <summary>
                /// Zambia Inflation Rate
                /// </summary>
                public const string InflationRate = "ZAMMBIAINFNRATE";
                /// <summary>
                /// Zambia Interest Rate
                /// </summary>
                public const string InterestRate = "ZAMMBIAINTTRATE";
            }
            public static class Zimbabwe
            {
                /// <summary>
                /// Zimbabwe Inflation Rate
                /// </summary>
                public const string InflationRate = "ZIMABWEINFNRATE";
            }
        }

        /// <summary>
        /// Indicator group
        /// </summary>
        public static class Indicator
        {
            public static class Afghanistan
            {
                /// <summary>
                /// Afghanistan Currency
                /// </summary>
                public const string Currency = "USDAFN";
            }
            public static class Albania
            {
                /// <summary>
                /// Albania Currency
                /// </summary>
                public const string Currency = "USDALL";
            }
            public static class Algeria
            {
                /// <summary>
                /// Algeria Currency
                /// </summary>
                public const string Currency = "USDDZD";
            }
            public static class Angola
            {
                /// <summary>
                /// Angola Currency
                /// </summary>
                public const string Currency = "USDAOA";
            }
            public static class Argentina
            {
                /// <summary>
                /// Argentina Interest Rate
                /// </summary>
                public const string InterestRate = "APDR1T";
                /// <summary>
                /// Argentina Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "ARGFRINRDPST";
                /// <summary>
                /// Argentina Stock Market
                /// </summary>
                public const string StockMarket = "MERVAL";
                /// <summary>
                /// Argentina Currency
                /// </summary>
                public const string Currency = "USDARS";
            }
            public static class Armenia
            {
                /// <summary>
                /// Armenia Currency
                /// </summary>
                public const string Currency = "USDAMD";
            }
            public static class Australia
            {
                /// <summary>
                /// Australia Stock Market
                /// </summary>
                public const string StockMarket = "AS51";
                /// <summary>
                /// Australia Currency
                /// </summary>
                public const string Currency = "AUDUSD";
                /// <summary>
                /// Australia 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "AUSTRALIA2YNY";
                /// <summary>
                /// Australia Central Bank Balance Sheet
                /// </summary>
                public const string CentralBankBalanceSheet = "AUSTRALIACENBANBALSH";
                /// <summary>
                /// Australia Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "AUSTRALIAINFEXP";
                /// <summary>
                /// Australia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GACGB10";
                /// <summary>
                /// Australia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "WMCCCONPCT";
            }
            public static class Austria
            {
                /// <summary>
                /// Austria Stock Market
                /// </summary>
                public const string StockMarket = "ATX";
                /// <summary>
                /// Austria Currency
                /// </summary>
                public const string Currency = "EURUSD";
                /// <summary>
                /// Austria Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GAGB10YR";
            }
            public static class Bahamas
            {
                /// <summary>
                /// Bahamas Currency
                /// </summary>
                public const string Currency = "USDBSD";
            }
            public static class Bahrain
            {
                /// <summary>
                /// Bahrain Interbank Rate
                /// </summary>
                public const string InterbankRate = "BAHRAININTRAT";
                /// <summary>
                /// Bahrain Stock Market
                /// </summary>
                public const string StockMarket = "BHSEEI";
                /// <summary>
                /// Bahrain Currency
                /// </summary>
                public const string Currency = "USDBHD";
            }
            public static class Bangladesh
            {
                /// <summary>
                /// Bangladesh Interest Rate
                /// </summary>
                public const string InterestRate = "BANGLADESHINTRATE";
                /// <summary>
                /// Bangladesh Stock Market
                /// </summary>
                public const string StockMarket = "DHAKA";
                /// <summary>
                /// Bangladesh Currency
                /// </summary>
                public const string Currency = "USDBDT";
            }
            public static class Belarus
            {
                /// <summary>
                /// Belarus Currency
                /// </summary>
                public const string Currency = "USDBYR";
            }
            public static class Belgium
            {
                /// <summary>
                /// Belgium Stock Market
                /// </summary>
                public const string StockMarket = "BEL20";
                /// <summary>
                /// Belgium Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GBGB10YR";
            }
            public static class Bermuda
            {
                /// <summary>
                /// Bermuda Stock Market
                /// </summary>
                public const string StockMarket = "BSX";
            }
            public static class Bolivia
            {
                /// <summary>
                /// Bolivia Currency
                /// </summary>
                public const string Currency = "USDBOB";
            }
            public static class BosniaAndHerzegovina
            {
                /// <summary>
                /// BosniaAndHerzegovina Stock Market
                /// </summary>
                public const string StockMarket = "SASX10";
                /// <summary>
                /// BosniaAndHerzegovina Currency
                /// </summary>
                public const string Currency = "USDBIH";
            }
            public static class Botswana
            {
                /// <summary>
                /// Botswana Stock Market
                /// </summary>
                public const string StockMarket = "BGSMDC";
                /// <summary>
                /// Botswana Currency
                /// </summary>
                public const string Currency = "USDBWP";
            }
            public static class Brazil
            {
                /// <summary>
                /// Brazil Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "BRAZILCASRESRAT";
                /// <summary>
                /// Brazil Interbank Rate
                /// </summary>
                public const string InterbankRate = "BRAZILINTRAT";
                /// <summary>
                /// Brazil Minimum Wages
                /// </summary>
                public const string MinimumWages = "BRAZILMINWAG";
                /// <summary>
                /// Brazil Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GEBU10Y";
                /// <summary>
                /// Brazil Stock Market
                /// </summary>
                public const string StockMarket = "IBOV";
                /// <summary>
                /// Brazil Currency
                /// </summary>
                public const string Currency = "USDBRL";
            }
            public static class Brunei
            {
                /// <summary>
                /// Brunei Currency
                /// </summary>
                public const string Currency = "USDBND";
            }
            public static class Bulgaria
            {
                /// <summary>
                /// Bulgaria Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "BULGARIAGOVBON10Y";
                /// <summary>
                /// Bulgaria Interbank Rate
                /// </summary>
                public const string InterbankRate = "BULGARIAINTRAT";
                /// <summary>
                /// Bulgaria Stock Market
                /// </summary>
                public const string StockMarket = "SOFIX";
                /// <summary>
                /// Bulgaria Currency
                /// </summary>
                public const string Currency = "USDBGN";
            }
            public static class Burundi
            {
                /// <summary>
                /// Burundi Currency
                /// </summary>
                public const string Currency = "USDBIF";
            }
            public static class Cambodia
            {
                /// <summary>
                /// Cambodia Currency
                /// </summary>
                public const string Currency = "USDKHR";
            }
            public static class Canada
            {
                /// <summary>
                /// Canada Bank Lending Rate
                /// </summary>
                public const string BankLendingRate = "CANADABANLENRAT";
                /// <summary>
                /// Canada Interbank Rate
                /// </summary>
                public const string InterbankRate = "CANADAINTRAT";
                /// <summary>
                /// Canada Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "CANFRINRDPST";
                /// <summary>
                /// Canada Interest Rate
                /// </summary>
                public const string InterestRate = "CCLR";
                /// <summary>
                /// Canada Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GCAN10YR";
                /// <summary>
                /// Canada Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "OECAI001";
                /// <summary>
                /// Canada Stock Market
                /// </summary>
                public const string StockMarket = "SPTSX";
                /// <summary>
                /// Canada Currency
                /// </summary>
                public const string Currency = "USDCAD";
            }
            public static class CapeVerde
            {
                /// <summary>
                /// CapeVerde Currency
                /// </summary>
                public const string Currency = "USDCVE";
            }
            public static class CaymanIslands
            {
                /// <summary>
                /// CaymanIslands Currency
                /// </summary>
                public const string Currency = "USDKYD";
            }
            public static class Chile
            {
                /// <summary>
                /// Chile Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "CHILEGOVBON10Y";
                /// <summary>
                /// Chile Interbank Rate
                /// </summary>
                public const string InterbankRate = "CHILEINTRAT";
                /// <summary>
                /// Chile Stock Market
                /// </summary>
                public const string StockMarket = "IGPA";
                /// <summary>
                /// Chile Currency
                /// </summary>
                public const string Currency = "USDCLP";
            }
            public static class China
            {
                /// <summary>
                /// China Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "CHINACASRESRAT";
                /// <summary>
                /// China Interbank Rate
                /// </summary>
                public const string InterbankRate = "CHINAINTRAT";
                /// <summary>
                /// China Reverse Repo Rate
                /// </summary>
                public const string ReverseRepoRate = "CHINAREVREPRAT";
                /// <summary>
                /// China Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GCNY10YR";
                /// <summary>
                /// China Stock Market
                /// </summary>
                public const string StockMarket = "SHCOMP";
                /// <summary>
                /// China Currency
                /// </summary>
                public const string Currency = "USDCNY";
            }
            public static class Colombia
            {
                /// <summary>
                /// Colombia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "COGR10Y";
                /// <summary>
                /// Colombia Stock Market
                /// </summary>
                public const string StockMarket = "COLCAP";
                /// <summary>
                /// Colombia Interbank Rate
                /// </summary>
                public const string InterbankRate = "COLOMBIAINTRAT";
                /// <summary>
                /// Colombia Currency
                /// </summary>
                public const string Currency = "USDCOP";
            }
            public static class Commodity
            {
                /// <summary>
                /// Commodity Baltic Exchange Dry Index
                /// </summary>
                public const string BalticExchangeDryIndex = "BALTIC";
                /// <summary>
                /// Commodity Beef
                /// </summary>
                public const string Beef = "BEEF";
                /// <summary>
                /// Commodity Bitumen
                /// </summary>
                public const string Bitumen = "BIT";
                /// <summary>
                /// Commodity Corn
                /// </summary>
                public const string Corn = "C-1";
                /// <summary>
                /// Commodity Cocoa
                /// </summary>
                public const string Cocoa = "CC1";
                /// <summary>
                /// Commodity Cheese
                /// </summary>
                public const string Cheese = "CHEESE1";
                /// <summary>
                /// Commodity Crude oil
                /// </summary>
                public const string CrudeOil = "CL1";
                /// <summary>
                /// Commodity Brent crude oil
                /// </summary>
                public const string BrentCrudeOil = "CO1";
                /// <summary>
                /// Commodity Coal
                /// </summary>
                public const string Coal = "COAL";
                /// <summary>
                /// Commodity Cobalt
                /// </summary>
                public const string Cobalt = "COBALT";
                /// <summary>
                /// Commodity CRB Commodity Index
                /// </summary>
                public const string CrbCommodityIndex = "CRB";
                /// <summary>
                /// Commodity Cotton
                /// </summary>
                public const string Cotton = "CT1";
                /// <summary>
                /// Commodity Ethanol
                /// </summary>
                public const string Ethanol = "DL1";
                /// <summary>
                /// Commodity Feeder Cattle
                /// </summary>
                public const string FeederCattle = "FC1";
                /// <summary>
                /// Commodity GSCI Commodity Index
                /// </summary>
                public const string GsciCommodityIndex = "GSCI";
                /// <summary>
                /// Commodity Copper
                /// </summary>
                public const string Copper = "HG1";
                /// <summary>
                /// Commodity Heating oil
                /// </summary>
                public const string HeatingOil = "HO1";
                /// <summary>
                /// Commodity Manganese Ore
                /// </summary>
                public const string ManganeseOre = "IMR";
                /// <summary>
                /// Commodity Iron Ore
                /// </summary>
                public const string IronOre = "IRONORE";
                /// <summary>
                /// Commodity Rubber
                /// </summary>
                public const string Rubber = "JN1";
                /// <summary>
                /// Commodity Orange Juice
                /// </summary>
                public const string OrangeJuice = "JO1";
                /// <summary>
                /// Commodity Coffee
                /// </summary>
                public const string Coffee = "KC1";
                /// <summary>
                /// Commodity Lumber
                /// </summary>
                public const string Lumber = "LB1";
                /// <summary>
                /// Commodity Live Cattle
                /// </summary>
                public const string LiveCattle = "LC1";
                /// <summary>
                /// Commodity Lean Hogs
                /// </summary>
                public const string LeanHogs = "LH1";
                /// <summary>
                /// Commodity Lithium
                /// </summary>
                public const string Lithium = "LITHIUM";
                /// <summary>
                /// Commodity Aluminum
                /// </summary>
                public const string Aluminum = "LMAHDS03";
                /// <summary>
                /// Commodity LME Index
                /// </summary>
                public const string LmeIndex = "LME";
                /// <summary>
                /// Commodity Molybdenum
                /// </summary>
                public const string Molybdenum = "LMMLDS03";
                /// <summary>
                /// Commodity Nickel
                /// </summary>
                public const string Nickel = "LMNIDS03";
                /// <summary>
                /// Commodity Lead
                /// </summary>
                public const string Lead = "LMPBDS03";
                /// <summary>
                /// Commodity Tin
                /// </summary>
                public const string Tin = "LMSNDS03";
                /// <summary>
                /// Commodity Zinc
                /// </summary>
                public const string Zinc = "LMZSDS03";
                /// <summary>
                /// Commodity Milk
                /// </summary>
                public const string Milk = "MILK";
                /// <summary>
                /// Commodity Naphtha
                /// </summary>
                public const string Naphtha = "NAPHTHA";
                /// <summary>
                /// Commodity Natural gas
                /// </summary>
                public const string NaturalGas = "NG1";
                /// <summary>
                /// Commodity Oat
                /// </summary>
                public const string Oat = "O-1";
                /// <summary>
                /// Commodity Wool
                /// </summary>
                public const string Wool = "OL1";
                /// <summary>
                /// Commodity Palm Oil
                /// </summary>
                public const string PalmOil = "PALMOIL";
                /// <summary>
                /// Commodity Propane
                /// </summary>
                public const string Propane = "PROPANE";
                /// <summary>
                /// Commodity RHODIUM
                /// </summary>
                public const string Rhodium = "RHODIUM";
                /// <summary>
                /// Commodity Rice
                /// </summary>
                public const string Rice = "RR1";
                /// <summary>
                /// Commodity Canola
                /// </summary>
                public const string Canola = "RS1";
                /// <summary>
                /// Commodity Soybeans
                /// </summary>
                public const string Soybeans = "S-1";
                /// <summary>
                /// Commodity Sugar
                /// </summary>
                public const string Sugar = "SB1";
                /// <summary>
                /// Commodity Soda Ash
                /// </summary>
                public const string SodaAsh = "SODASH";
                /// <summary>
                /// Commodity Neodymium
                /// </summary>
                public const string Neodymium = "SREMNDM";
                /// <summary>
                /// Commodity Steel
                /// </summary>
                public const string Steel = "STEEL";
                /// <summary>
                /// Commodity Iron Ore 62% FE
                /// </summary>
                public const string IronOre62PercentFe = "TIOC";
                /// <summary>
                /// Commodity Uranium
                /// </summary>
                public const string Uranium = "URANIUM";
                /// <summary>
                /// Commodity Wheat
                /// </summary>
                public const string Wheat = "W-1";
                /// <summary>
                /// Commodity Silver
                /// </summary>
                public const string Silver = "XAGUSD";
                /// <summary>
                /// Commodity Gold
                /// </summary>
                public const string Gold = "XAUUSD";
                /// <summary>
                /// Commodity Gasoline
                /// </summary>
                public const string Gasoline = "XB1";
                /// <summary>
                /// Commodity Palladium
                /// </summary>
                public const string Palladium = "XPDUSD";
                /// <summary>
                /// Commodity Platinum
                /// </summary>
                public const string Platinum = "XPTUSD";
            }
            public static class Comoros
            {
                /// <summary>
                /// Comoros Currency
                /// </summary>
                public const string Currency = "USDKMF";
            }
            public static class Congo
            {
                /// <summary>
                /// Congo Currency
                /// </summary>
                public const string Currency = "USDCDF";
            }
            public static class CostaRica
            {
                /// <summary>
                /// CostaRica Stock Market
                /// </summary>
                public const string StockMarket = "CRSMBCT";
                /// <summary>
                /// CostaRica Currency
                /// </summary>
                public const string Currency = "USDCRC";
            }
            public static class Croatia
            {
                /// <summary>
                /// Croatia Stock Market
                /// </summary>
                public const string StockMarket = "CRO";
                /// <summary>
                /// Croatia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "CROATIAGOVD10Y";
                /// <summary>
                /// Croatia Interbank Rate
                /// </summary>
                public const string InterbankRate = "CROATIAINTRAT";
                /// <summary>
                /// Croatia Currency
                /// </summary>
                public const string Currency = "USDHRV";
            }
            public static class Cuba
            {
                /// <summary>
                /// Cuba Currency
                /// </summary>
                public const string Currency = "USDCUC";
            }
            public static class Cyprus
            {
                /// <summary>
                /// Cyprus Stock Market
                /// </summary>
                public const string StockMarket = "CYSMMAPA";
            }
            public static class CzechRepublic
            {
                /// <summary>
                /// CzechRepublic Interbank Rate
                /// </summary>
                public const string InterbankRate = "CZECHREPUINTRAT";
                /// <summary>
                /// CzechRepublic Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "CZGB10YR";
                /// <summary>
                /// CzechRepublic Stock Market
                /// </summary>
                public const string StockMarket = "PX";
                /// <summary>
                /// CzechRepublic Currency
                /// </summary>
                public const string Currency = "USDCZK";
            }
            public static class Denmark
            {
                /// <summary>
                /// Denmark Interbank Rate
                /// </summary>
                public const string InterbankRate = "DENMARKINTRAT";
                /// <summary>
                /// Denmark Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GDGB10YR";
                /// <summary>
                /// Denmark Stock Market
                /// </summary>
                public const string StockMarket = "KFX";
                /// <summary>
                /// Denmark Currency
                /// </summary>
                public const string Currency = "USDDKK";
            }
            public static class Djibouti
            {
                /// <summary>
                /// Djibouti Currency
                /// </summary>
                public const string Currency = "USDDJF";
            }
            public static class DominicanRepublic
            {
                /// <summary>
                /// DominicanRepublic Currency
                /// </summary>
                public const string Currency = "USDDOP";
            }
            public static class Ecuador
            {
                /// <summary>
                /// Ecuador Stock Market
                /// </summary>
                public const string StockMarket = "ECU";
                /// <summary>
                /// Ecuador Interbank Rate
                /// </summary>
                public const string InterbankRate = "ECUADORINTRATI";
            }
            public static class Egypt
            {
                /// <summary>
                /// Egypt Stock Market
                /// </summary>
                public const string StockMarket = "CASE";
                /// <summary>
                /// Egypt Interbank Rate
                /// </summary>
                public const string InterbankRate = "EGYPTINTRAT";
                /// <summary>
                /// Egypt Interest Rate
                /// </summary>
                public const string InterestRate = "EGYPTINTRATE";
                /// <summary>
                /// Egypt Lending Rate
                /// </summary>
                public const string LendingRate = "EGYPTLENRAT";
                /// <summary>
                /// Egypt Currency
                /// </summary>
                public const string Currency = "USDEGP";
            }
            public static class Eritrea
            {
                /// <summary>
                /// Eritrea Currency
                /// </summary>
                public const string Currency = "USDERN";
            }
            public static class Estonia
            {
                /// <summary>
                /// Estonia Stock Market
                /// </summary>
                public const string StockMarket = "TALSE";
            }
            public static class Ethiopia
            {
                /// <summary>
                /// Ethiopia Currency
                /// </summary>
                public const string Currency = "USDETB";
            }
            public static class EuroArea
            {
                /// <summary>
                /// EuroArea Interbank Rate
                /// </summary>
                public const string InterbankRate = "EMUEVOLVINTRAT";
                /// <summary>
                /// EuroArea Lending Rate
                /// </summary>
                public const string LendingRate = "EUROAREALENRAT";
                /// <summary>
                /// EuroArea Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "EUROAREAZEWECOSENIND";
                /// <summary>
                /// EuroArea Stock Market
                /// </summary>
                public const string StockMarket = "SX5E";
            }
            public static class Fiji
            {
                /// <summary>
                /// Fiji Currency
                /// </summary>
                public const string Currency = "USDFJD";
            }
            public static class Finland
            {
                /// <summary>
                /// Finland Interbank Rate
                /// </summary>
                public const string InterbankRate = "FINLANDINTRAT";
                /// <summary>
                /// Finland Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GFIN10YR";
                /// <summary>
                /// Finland Stock Market
                /// </summary>
                public const string StockMarket = "HEX25";
            }
            public static class France
            {
                /// <summary>
                /// France Stock Market
                /// </summary>
                public const string StockMarket = "CAC";
                /// <summary>
                /// France Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GFRN10";
            }
            public static class Gambia
            {
                /// <summary>
                /// Gambia Currency
                /// </summary>
                public const string Currency = "USDGMD";
            }
            public static class Georgia
            {
                /// <summary>
                /// Georgia Currency
                /// </summary>
                public const string Currency = "USDGEL";
            }
            public static class Germany
            {
                /// <summary>
                /// Germany Stock Market
                /// </summary>
                public const string StockMarket = "DAX";
                /// <summary>
                /// Germany Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GDBR10";
                /// <summary>
                /// Germany 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "GERMANY2YNY";
                /// <summary>
                /// Germany Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "GERMANYZEWECOSENIND";
                /// <summary>
                /// Germany Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GRCCI";
            }
            public static class Ghana
            {
                /// <summary>
                /// Ghana Stock Market
                /// </summary>
                public const string StockMarket = "GGSECI";
                /// <summary>
                /// Ghana Currency
                /// </summary>
                public const string Currency = "USDGHS";
            }
            public static class Greece
            {
                /// <summary>
                /// Greece Stock Market
                /// </summary>
                public const string StockMarket = "ASE";
                /// <summary>
                /// Greece Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GGGB10YR";
                /// <summary>
                /// Greece 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "GREECE2YNY";
            }
            public static class Guatemala
            {
                /// <summary>
                /// Guatemala Currency
                /// </summary>
                public const string Currency = "USDGTQ";
            }
            public static class Guinea
            {
                /// <summary>
                /// Guinea Currency
                /// </summary>
                public const string Currency = "USDGNF";
            }
            public static class Guyana
            {
                /// <summary>
                /// Guyana Currency
                /// </summary>
                public const string Currency = "USDGYD";
            }
            public static class Haiti
            {
                /// <summary>
                /// Haiti Currency
                /// </summary>
                public const string Currency = "USDHTG";
            }
            public static class Honduras
            {
                /// <summary>
                /// Honduras Interest Rate
                /// </summary>
                public const string InterestRate = "HONURASINTTRATE";
                /// <summary>
                /// Honduras Currency
                /// </summary>
                public const string Currency = "USDHNL";
            }
            public static class HongKong
            {
                /// <summary>
                /// HongKong Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GHKGB10Y";
                /// <summary>
                /// HongKong Interbank Rate
                /// </summary>
                public const string InterbankRate = "HONGKONGINTRAT";
                /// <summary>
                /// HongKong Stock Market
                /// </summary>
                public const string StockMarket = "HSI";
                /// <summary>
                /// HongKong Currency
                /// </summary>
                public const string Currency = "USDHKD";
            }
            public static class Hungary
            {
                /// <summary>
                /// Hungary Stock Market
                /// </summary>
                public const string StockMarket = "BUX";
                /// <summary>
                /// Hungary Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GHGB10YR";
                /// <summary>
                /// Hungary Interbank Rate
                /// </summary>
                public const string InterbankRate = "HUNGARYINTRAT";
                /// <summary>
                /// Hungary Currency
                /// </summary>
                public const string Currency = "USDHUF";
            }
            public static class Iceland
            {
                /// <summary>
                /// Iceland Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "ICELANDGOVBON10Y";
                /// <summary>
                /// Iceland Interbank Rate
                /// </summary>
                public const string InterbankRate = "ICELANDINTRAT";
                /// <summary>
                /// Iceland Stock Market
                /// </summary>
                public const string StockMarket = "ICEXI";
                /// <summary>
                /// Iceland Currency
                /// </summary>
                public const string Currency = "USDISK";
            }
            public static class India
            {
                /// <summary>
                /// India Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GIND10YR";
                /// <summary>
                /// India Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "INDIACASRESRAT";
                /// <summary>
                /// India Interest Rate
                /// </summary>
                public const string InterestRate = "RSPOYLD";
                /// <summary>
                /// India Stock Market
                /// </summary>
                public const string StockMarket = "SENSEX";
                /// <summary>
                /// India Currency
                /// </summary>
                public const string Currency = "USDINR";
            }
            public static class Indonesia
            {
                /// <summary>
                /// Indonesia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GIDN10YR";
                /// <summary>
                /// Indonesia Interbank Rate
                /// </summary>
                public const string InterbankRate = "INDONESIAINTRAT";
                /// <summary>
                /// Indonesia Stock Market
                /// </summary>
                public const string StockMarket = "JCI";
                /// <summary>
                /// Indonesia Currency
                /// </summary>
                public const string Currency = "USDIDR";
            }
            public static class Iraq
            {
                /// <summary>
                /// Iraq Currency
                /// </summary>
                public const string Currency = "USDIQD";
            }
            public static class Ireland
            {
                /// <summary>
                /// Ireland Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GIGB10YR";
                /// <summary>
                /// Ireland Stock Market
                /// </summary>
                public const string StockMarket = "ISEQ";
            }
            public static class Israel
            {
                /// <summary>
                /// Israel Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GISR10YR";
                /// <summary>
                /// Israel Interest Rate
                /// </summary>
                public const string InterestRate = "ISBRATE";
                /// <summary>
                /// Israel Interbank Rate
                /// </summary>
                public const string InterbankRate = "ISRAELINTRAT";
                /// <summary>
                /// Israel Stock Market
                /// </summary>
                public const string StockMarket = "TA-100";
                /// <summary>
                /// Israel Currency
                /// </summary>
                public const string Currency = "USDILS";
            }
            public static class Italy
            {
                /// <summary>
                /// Italy Stock Market
                /// </summary>
                public const string StockMarket = "FTSEMIB";
                /// <summary>
                /// Italy Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GBTPGR10";
                /// <summary>
                /// Italy 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "ITALY2YNY";
            }
            public static class IvoryCoast
            {
                /// <summary>
                /// IvoryCoast Currency
                /// </summary>
                public const string Currency = "USDXOF";
            }
            public static class Jamaica
            {
                /// <summary>
                /// Jamaica Interest Rate
                /// </summary>
                public const string InterestRate = "JAMAICAINTTRATE";
                /// <summary>
                /// Jamaica Stock Market
                /// </summary>
                public const string StockMarket = "JMSMX";
                /// <summary>
                /// Jamaica Currency
                /// </summary>
                public const string Currency = "USDJMD";
            }
            public static class Japan
            {
                /// <summary>
                /// Japan Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GJGB10";
                /// <summary>
                /// Japan 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "JAPAN2YNY";
                /// <summary>
                /// Japan Interbank Rate
                /// </summary>
                public const string InterbankRate = "JAPANINTRAT";
                /// <summary>
                /// Japan Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "JPNFRINRDPST";
                /// <summary>
                /// Japan Stock Market
                /// </summary>
                public const string StockMarket = "NKY";
                /// <summary>
                /// Japan Currency
                /// </summary>
                public const string Currency = "USDJPY";
            }
            public static class Jordan
            {
                /// <summary>
                /// Jordan Stock Market
                /// </summary>
                public const string StockMarket = "JOSMGNFF";
                /// <summary>
                /// Jordan Currency
                /// </summary>
                public const string Currency = "USDJOR";
            }
            public static class Kazakhstan
            {
                /// <summary>
                /// Kazakhstan Interbank Rate
                /// </summary>
                public const string InterbankRate = "KAZAKHSTANINTRAT";
                /// <summary>
                /// Kazakhstan Interest Rate
                /// </summary>
                public const string InterestRate = "KAZAKHSTANINTRATE";
                /// <summary>
                /// Kazakhstan Stock Market
                /// </summary>
                public const string StockMarket = "KZKAK";
                /// <summary>
                /// Kazakhstan Currency
                /// </summary>
                public const string Currency = "USDKZT";
            }
            public static class Kenya
            {
                /// <summary>
                /// Kenya Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "KENYAGOVBON10Y";
                /// <summary>
                /// Kenya Stock Market
                /// </summary>
                public const string StockMarket = "KNSMIDX";
                /// <summary>
                /// Kenya Currency
                /// </summary>
                public const string Currency = "USDKES";
            }
            public static class Kuwait
            {
                /// <summary>
                /// Kuwait Interbank Rate
                /// </summary>
                public const string InterbankRate = "KUWAITINTRAT";
                /// <summary>
                /// Kuwait Currency
                /// </summary>
                public const string Currency = "USDKWD";
            }
            public static class Kyrgyzstan
            {
                /// <summary>
                /// Kyrgyzstan Interest Rate
                /// </summary>
                public const string InterestRate = "KYRSTANINTTRATE";
                /// <summary>
                /// Kyrgyzstan Currency
                /// </summary>
                public const string Currency = "USDKGS";
            }
            public static class Laos
            {
                /// <summary>
                /// Laos Stock Market
                /// </summary>
                public const string StockMarket = "LSXC";
                /// <summary>
                /// Laos Currency
                /// </summary>
                public const string Currency = "USDLAK";
            }
            public static class Latvia
            {
                /// <summary>
                /// Latvia Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "LATVIAGOVBON10Y";
                /// <summary>
                /// Latvia Interbank Rate
                /// </summary>
                public const string InterbankRate = "LATVIAINTRAT";
                /// <summary>
                /// Latvia Stock Market
                /// </summary>
                public const string StockMarket = "RIGSE";
            }
            public static class Lebanon
            {
                /// <summary>
                /// Lebanon Stock Market
                /// </summary>
                public const string StockMarket = "BLOM";
                /// <summary>
                /// Lebanon Currency
                /// </summary>
                public const string Currency = "USDLBP";
            }
            public static class Lesotho
            {
                /// <summary>
                /// Lesotho Currency
                /// </summary>
                public const string Currency = "USDLSL";
            }
            public static class Liberia
            {
                /// <summary>
                /// Liberia Currency
                /// </summary>
                public const string Currency = "USDLRD";
            }
            public static class Libya
            {
                /// <summary>
                /// Libya Currency
                /// </summary>
                public const string Currency = "USDLYD";
            }
            public static class Lithuania
            {
                /// <summary>
                /// Lithuania Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "LITHUANIAGOVBON10Y";
                /// <summary>
                /// Lithuania Stock Market
                /// </summary>
                public const string StockMarket = "VILSE";
            }
            public static class Luxembourg
            {
                /// <summary>
                /// Luxembourg Stock Market
                /// </summary>
                public const string StockMarket = "LUXXX";
            }
            public static class Macau
            {
                /// <summary>
                /// Macau Interbank Rate
                /// </summary>
                public const string InterbankRate = "MACAOINTRAT";
                /// <summary>
                /// Macau Currency
                /// </summary>
                public const string Currency = "USDMOP";
            }
            public static class Macedonia
            {
                /// <summary>
                /// Macedonia Stock Market
                /// </summary>
                public const string StockMarket = "MBI";
                /// <summary>
                /// Macedonia Currency
                /// </summary>
                public const string Currency = "USDMKD";
            }
            public static class Madagascar
            {
                /// <summary>
                /// Madagascar Currency
                /// </summary>
                public const string Currency = "USDMGA";
            }
            public static class Malawi
            {
                /// <summary>
                /// Malawi Currency
                /// </summary>
                public const string Currency = "USDMWK";
            }
            public static class Malaysia
            {
                /// <summary>
                /// Malaysia Stock Market
                /// </summary>
                public const string StockMarket = "FBMKLCI";
                /// <summary>
                /// Malaysia Interbank Rate
                /// </summary>
                public const string InterbankRate = "MALAYSIAINTRAT";
                /// <summary>
                /// Malaysia Interest Rate
                /// </summary>
                public const string InterestRate = "MAOPRATE";
                /// <summary>
                /// Malaysia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "MGIY10Y";
                /// <summary>
                /// Malaysia Currency
                /// </summary>
                public const string Currency = "USDMYR";
            }
            public static class Maldives
            {
                /// <summary>
                /// Maldives Currency
                /// </summary>
                public const string Currency = "USDMVR";
            }
            public static class Malta
            {
                /// <summary>
                /// Malta Stock Market
                /// </summary>
                public const string StockMarket = "MALTEX";
            }
            public static class Mauritius
            {
                /// <summary>
                /// Mauritius Stock Market
                /// </summary>
                public const string StockMarket = "SEMDEX";
                /// <summary>
                /// Mauritius Currency
                /// </summary>
                public const string Currency = "USDMUR";
            }
            public static class Mexico
            {
                /// <summary>
                /// Mexico Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GMXN10YR";
                /// <summary>
                /// Mexico Stock Market
                /// </summary>
                public const string StockMarket = "MEXBOL";
                /// <summary>
                /// Mexico Interest Rate
                /// </summary>
                public const string InterestRate = "MXONBR";
                /// <summary>
                /// Mexico Currency
                /// </summary>
                public const string Currency = "USDMXN";
            }
            public static class Moldova
            {
                /// <summary>
                /// Moldova Currency
                /// </summary>
                public const string Currency = "USDMDL";
            }
            public static class Mongolia
            {
                /// <summary>
                /// Mongolia Stock Market
                /// </summary>
                public const string StockMarket = "MSETOP";
                /// <summary>
                /// Mongolia Currency
                /// </summary>
                public const string Currency = "USDMNT";
            }
            public static class Morocco
            {
                /// <summary>
                /// Morocco Stock Market
                /// </summary>
                public const string StockMarket = "MOSENEW";
                /// <summary>
                /// Morocco Currency
                /// </summary>
                public const string Currency = "USDMAD";
            }
            public static class Mozambique
            {
                /// <summary>
                /// Mozambique Currency
                /// </summary>
                public const string Currency = "USDMZN";
            }
            public static class Myanmar
            {
                /// <summary>
                /// Myanmar Currency
                /// </summary>
                public const string Currency = "USDMMK";
            }
            public static class Namibia
            {
                /// <summary>
                /// Namibia Stock Market
                /// </summary>
                public const string StockMarket = "FTN098";
                /// <summary>
                /// Namibia Currency
                /// </summary>
                public const string Currency = "USDNAD";
            }
            public static class Nepal
            {
                /// <summary>
                /// Nepal Currency
                /// </summary>
                public const string Currency = "USDNPR";
            }
            public static class Netherlands
            {
                /// <summary>
                /// Netherlands Stock Market
                /// </summary>
                public const string StockMarket = "AEX";
                /// <summary>
                /// Netherlands Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GNTH10YR";
            }
            public static class NewCaledonia
            {
                /// <summary>
                /// NewCaledonia Currency
                /// </summary>
                public const string Currency = "USDXPF";
            }
            public static class NewZealand
            {
                /// <summary>
                /// NewZealand Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GNZGB10";
                /// <summary>
                /// NewZealand Global Dairy Trade Price Index
                /// </summary>
                public const string GlobalDairyTradePriceIndex = "NEWZEALANGDTPI";
                /// <summary>
                /// NewZealand Currency
                /// </summary>
                public const string Currency = "NZDUSD";
                /// <summary>
                /// NewZealand Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "NZLFRINRDPST";
                /// <summary>
                /// NewZealand Interest Rate
                /// </summary>
                public const string InterestRate = "NZOCRS";
                /// <summary>
                /// NewZealand Stock Market
                /// </summary>
                public const string StockMarket = "NZSE50FG";
            }
            public static class Nicaragua
            {
                /// <summary>
                /// Nicaragua Currency
                /// </summary>
                public const string Currency = "USDNIO";
            }
            public static class Nigeria
            {
                /// <summary>
                /// Nigeria Stock Market
                /// </summary>
                public const string StockMarket = "NGSEINDX";
                /// <summary>
                /// Nigeria Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "NIGERIACASRESRAT";
                /// <summary>
                /// Nigeria Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "NIGERIAGOVBON10Y";
                /// <summary>
                /// Nigeria Interbank Rate
                /// </summary>
                public const string InterbankRate = "NIGERIAINTRAT";
                /// <summary>
                /// Nigeria Currency
                /// </summary>
                public const string Currency = "USDNGN";
            }
            public static class NorthKorea
            {
                /// <summary>
                /// NorthKorea Currency
                /// </summary>
                public const string Currency = "NORTHKORECUR";
            }
            public static class Norway
            {
                /// <summary>
                /// Norway Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GNOR10YR";
                /// <summary>
                /// Norway Interbank Rate
                /// </summary>
                public const string InterbankRate = "NORWAYINTRAT";
                /// <summary>
                /// Norway Stock Market
                /// </summary>
                public const string StockMarket = "OSEAX";
                /// <summary>
                /// Norway Currency
                /// </summary>
                public const string Currency = "USDNOK";
            }
            public static class Oman
            {
                /// <summary>
                /// Oman Stock Market
                /// </summary>
                public const string StockMarket = "MSM30";
                /// <summary>
                /// Oman Currency
                /// </summary>
                public const string Currency = "USDOMR";
            }
            public static class Pakistan
            {
                /// <summary>
                /// Pakistan Stock Market
                /// </summary>
                public const string StockMarket = "KSE100";
                /// <summary>
                /// Pakistan Interest Rate
                /// </summary>
                public const string InterestRate = "PAPRSBP";
                /// <summary>
                /// Pakistan Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "PKIB10YR";
                /// <summary>
                /// Pakistan Currency
                /// </summary>
                public const string Currency = "USDPKR";
            }
            public static class Panama
            {
                /// <summary>
                /// Panama Stock Market
                /// </summary>
                public const string StockMarket = "BVPSBVPS";
                /// <summary>
                /// Panama Currency
                /// </summary>
                public const string Currency = "USDPAB";
            }
            public static class PapuaNewGuinea
            {
                /// <summary>
                /// PapuaNewGuinea Currency
                /// </summary>
                public const string Currency = "USDPGK";
            }
            public static class Paraguay
            {
                /// <summary>
                /// Paraguay Currency
                /// </summary>
                public const string Currency = "USDPYG";
            }
            public static class Peru
            {
                /// <summary>
                /// Peru Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GRPE10Y";
                /// <summary>
                /// Peru Stock Market
                /// </summary>
                public const string StockMarket = "IGBVL";
                /// <summary>
                /// Peru Interbank Rate
                /// </summary>
                public const string InterbankRate = "PERUINTRAT";
                /// <summary>
                /// Peru Interest Rate
                /// </summary>
                public const string InterestRate = "PRRRONUS";
                /// <summary>
                /// Peru Currency
                /// </summary>
                public const string Currency = "USDPEN";
            }
            public static class Philippines
            {
                /// <summary>
                /// Philippines Stock Market
                /// </summary>
                public const string StockMarket = "PCOMP";
                /// <summary>
                /// Philippines Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "PHILIPPINEGOVBON10Y";
                /// <summary>
                /// Philippines Interbank Rate
                /// </summary>
                public const string InterbankRate = "PHILIPPINEINTRAT";
                /// <summary>
                /// Philippines Interest Rate
                /// </summary>
                public const string InterestRate = "PHILIPPINESINTRATE";
                /// <summary>
                /// Philippines Currency
                /// </summary>
                public const string Currency = "USDPHP";
            }
            public static class Poland
            {
                /// <summary>
                /// Poland Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "POGB10YR";
                /// <summary>
                /// Poland Interbank Rate
                /// </summary>
                public const string InterbankRate = "POLANDINTRAT";
                /// <summary>
                /// Poland Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "POLFRINRDPST";
                /// <summary>
                /// Poland Currency
                /// </summary>
                public const string Currency = "USDPLN";
                /// <summary>
                /// Poland Stock Market
                /// </summary>
                public const string StockMarket = "WIG";
            }
            public static class Portugal
            {
                /// <summary>
                /// Portugal Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSPT10YR";
                /// <summary>
                /// Portugal 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "PORTUGAL2YNY";
                /// <summary>
                /// Portugal Stock Market
                /// </summary>
                public const string StockMarket = "PSI20";
            }
            public static class Qatar
            {
                /// <summary>
                /// Qatar Stock Market
                /// </summary>
                public const string StockMarket = "DSM";
                /// <summary>
                /// Qatar Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "QATARGOVBON10Y";
                /// <summary>
                /// Qatar Reverse Repo Rate
                /// </summary>
                public const string ReverseRepoRate = "QATARREVREPRAT";
                /// <summary>
                /// Qatar Currency
                /// </summary>
                public const string Currency = "USDQAR";
            }
            public static class Romania
            {
                /// <summary>
                /// Romania Stock Market
                /// </summary>
                public const string StockMarket = "BET";
                /// <summary>
                /// Romania Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "ROMANIAGOVBON10Y";
                /// <summary>
                /// Romania Interbank Rate
                /// </summary>
                public const string InterbankRate = "ROMANIAINTRAT";
                /// <summary>
                /// Romania Currency
                /// </summary>
                public const string Currency = "USDRON";
            }
            public static class Russia
            {
                /// <summary>
                /// Russia Stock Market
                /// </summary>
                public const string StockMarket = "INDEXCF";
                /// <summary>
                /// Russia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "RUGE10Y";
                /// <summary>
                /// Russia Central Bank Balance Sheet
                /// </summary>
                public const string CentralBankBalanceSheet = "RUSSIACENBANBALSHE";
                /// <summary>
                /// Russia Currency
                /// </summary>
                public const string Currency = "USDRUB";
            }
            public static class Rwanda
            {
                /// <summary>
                /// Rwanda Currency
                /// </summary>
                public const string Currency = "USDRWF";
            }
            public static class SaoTomeAndPrincipe
            {
                /// <summary>
                /// SaoTomeAndPrincipe Currency
                /// </summary>
                public const string Currency = "USDSTD";
            }
            public static class SaudiArabia
            {
                /// <summary>
                /// SaudiArabia Stock Market
                /// </summary>
                public const string StockMarket = "SASEIDX";
                /// <summary>
                /// SaudiArabia Reverse Repo Rate
                /// </summary>
                public const string ReverseRepoRate = "SAUDIARABREVREPRAT";
                /// <summary>
                /// SaudiArabia Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "SAUFRINRDPST";
                /// <summary>
                /// SaudiArabia Currency
                /// </summary>
                public const string Currency = "USDSAR";
            }
            public static class Serbia
            {
                /// <summary>
                /// Serbia Stock Market
                /// </summary>
                public const string StockMarket = "BELEX15";
                /// <summary>
                /// Serbia Interbank Rate
                /// </summary>
                public const string InterbankRate = "SERBIAINTRAT";
                /// <summary>
                /// Serbia Interest Rate
                /// </summary>
                public const string InterestRate = "SERRBIAINTTRATE";
                /// <summary>
                /// Serbia Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "SRBFRINRDPST";
                /// <summary>
                /// Serbia Currency
                /// </summary>
                public const string Currency = "USDSRB";
            }
            public static class Seychelles
            {
                /// <summary>
                /// Seychelles Currency
                /// </summary>
                public const string Currency = "USDSCR";
            }
            public static class SierraLeone
            {
                /// <summary>
                /// SierraLeone Currency
                /// </summary>
                public const string Currency = "USDSLL";
            }
            public static class Singapore
            {
                /// <summary>
                /// Singapore Stock Market
                /// </summary>
                public const string StockMarket = "FSSTI";
                /// <summary>
                /// Singapore Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "MASB10Y";
                /// <summary>
                /// Singapore Interbank Rate
                /// </summary>
                public const string InterbankRate = "SINGAPOREINTRAT";
                /// <summary>
                /// Singapore Currency
                /// </summary>
                public const string Currency = "USDSGD";
            }
            public static class Slovakia
            {
                /// <summary>
                /// Slovakia Stock Market
                /// </summary>
                public const string StockMarket = "SKSM";
                /// <summary>
                /// Slovakia Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "SLOVAKIAGOVBON10Y";
            }
            public static class Slovenia
            {
                /// <summary>
                /// Slovenia Stock Market
                /// </summary>
                public const string StockMarket = "SBITOP";
                /// <summary>
                /// Slovenia Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "SLOVENIAGOVBON10Y";
            }
            public static class Somalia
            {
                /// <summary>
                /// Somalia Currency
                /// </summary>
                public const string Currency = "USDSOS";
            }
            public static class SouthAfrica
            {
                /// <summary>
                /// SouthAfrica Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSAB10YR";
                /// <summary>
                /// SouthAfrica Stock Market
                /// </summary>
                public const string StockMarket = "JALSH";
                /// <summary>
                /// SouthAfrica Interbank Rate
                /// </summary>
                public const string InterbankRate = "SOUTHAFRIINTRAT";
                /// <summary>
                /// SouthAfrica Currency
                /// </summary>
                public const string Currency = "USDZAR";
            }
            public static class SouthKorea
            {
                /// <summary>
                /// SouthKorea Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GVSK10YR";
                /// <summary>
                /// SouthKorea Stock Market
                /// </summary>
                public const string StockMarket = "KOSPI";
                /// <summary>
                /// SouthKorea Interbank Rate
                /// </summary>
                public const string InterbankRate = "SOUTHKOREINTRAT";
                /// <summary>
                /// SouthKorea Currency
                /// </summary>
                public const string Currency = "USDKRW";
            }
            public static class Spain
            {
                /// <summary>
                /// Spain Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSPG10YR";
                /// <summary>
                /// Spain Stock Market
                /// </summary>
                public const string StockMarket = "IBEX";
                /// <summary>
                /// Spain Interbank Rate
                /// </summary>
                public const string InterbankRate = "SPAININTRAT";
            }
            public static class SriLanka
            {
                /// <summary>
                /// SriLanka Stock Market
                /// </summary>
                public const string StockMarket = "CSEALL";
                /// <summary>
                /// SriLanka Interest Rate
                /// </summary>
                public const string InterestRate = "SRI-LANKAINTRATE";
                /// <summary>
                /// SriLanka Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "SRILANKACASRESRAT";
                /// <summary>
                /// SriLanka Interbank Rate
                /// </summary>
                public const string InterbankRate = "SRILANKAINTRAT";
                /// <summary>
                /// SriLanka Lending Rate
                /// </summary>
                public const string LendingRate = "SRILANKALENRAT";
                /// <summary>
                /// SriLanka Currency
                /// </summary>
                public const string Currency = "USDLKR";
            }
            public static class Sudan
            {
                /// <summary>
                /// Sudan Currency
                /// </summary>
                public const string Currency = "USDSDG";
            }
            public static class Suriname
            {
                /// <summary>
                /// Suriname Currency
                /// </summary>
                public const string Currency = "USDSRD";
            }
            public static class Swaziland
            {
                /// <summary>
                /// Swaziland Currency
                /// </summary>
                public const string Currency = "USDSZL";
            }
            public static class Sweden
            {
                /// <summary>
                /// Sweden Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSGB10YR";
                /// <summary>
                /// Sweden Stock Market
                /// </summary>
                public const string StockMarket = "OMX";
                /// <summary>
                /// Sweden Interbank Rate
                /// </summary>
                public const string InterbankRate = "SWEDENINTRAT";
                /// <summary>
                /// Sweden Currency
                /// </summary>
                public const string Currency = "USDSEK";
            }
            public static class Switzerland
            {
                /// <summary>
                /// Switzerland Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSWISS10";
                /// <summary>
                /// Switzerland Stock Market
                /// </summary>
                public const string StockMarket = "SMI";
                /// <summary>
                /// Switzerland Interbank Rate
                /// </summary>
                public const string InterbankRate = "SWITZERLANINTRAT";
                /// <summary>
                /// Switzerland Currency
                /// </summary>
                public const string Currency = "USDCHF";
            }
            public static class Syria
            {
                /// <summary>
                /// Syria Currency
                /// </summary>
                public const string Currency = "USDSYP";
            }
            public static class Taiwan
            {
                /// <summary>
                /// Taiwan Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "TAIWANGOVBON10Y";
                /// <summary>
                /// Taiwan Interbank Rate
                /// </summary>
                public const string InterbankRate = "TAIWANINTRAT";
                /// <summary>
                /// Taiwan Interest Rate
                /// </summary>
                public const string InterestRate = "TAIWANIR";
                /// <summary>
                /// Taiwan Stock Market
                /// </summary>
                public const string StockMarket = "TWSE";
                /// <summary>
                /// Taiwan Currency
                /// </summary>
                public const string Currency = "USDTWD";
            }
            public static class Tajikistan
            {
                /// <summary>
                /// Tajikistan Currency
                /// </summary>
                public const string Currency = "USDTJS";
            }
            public static class Tanzania
            {
                /// <summary>
                /// Tanzania Stock Market
                /// </summary>
                public const string StockMarket = "DARSDSEI";
                /// <summary>
                /// Tanzania Currency
                /// </summary>
                public const string Currency = "USDTZS";
            }
            public static class Thailand
            {
                /// <summary>
                /// Thailand Interest Rate
                /// </summary>
                public const string InterestRate = "BTRR1DAY";
                /// <summary>
                /// Thailand Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GVTL10YR";
                /// <summary>
                /// Thailand Stock Market
                /// </summary>
                public const string StockMarket = "SET50";
                /// <summary>
                /// Thailand Interbank Rate
                /// </summary>
                public const string InterbankRate = "THAILANDINTRAT";
                /// <summary>
                /// Thailand Currency
                /// </summary>
                public const string Currency = "USDTHB";
            }
            public static class TrinidadAndTobago
            {
                /// <summary>
                /// TrinidadAndTobago Currency
                /// </summary>
                public const string Currency = "USDTTD";
            }
            public static class Tunisia
            {
                /// <summary>
                /// Tunisia Stock Market
                /// </summary>
                public const string StockMarket = "TUSISE";
                /// <summary>
                /// Tunisia Currency
                /// </summary>
                public const string Currency = "USDTND";
            }
            public static class Turkey
            {
                /// <summary>
                /// Turkey Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "TURKEYGOVBON10Y";
                /// <summary>
                /// Turkey Interbank Rate
                /// </summary>
                public const string InterbankRate = "TURKEYINTRAT";
                /// <summary>
                /// Turkey Currency
                /// </summary>
                public const string Currency = "USDTRY";
                /// <summary>
                /// Turkey Stock Market
                /// </summary>
                public const string StockMarket = "XU100";
            }
            public static class Turkmenistan
            {
                /// <summary>
                /// Turkmenistan Currency
                /// </summary>
                public const string Currency = "USDTMT";
            }
            public static class Uganda
            {
                /// <summary>
                /// Uganda Currency
                /// </summary>
                public const string Currency = "USDUGX";
            }
            public static class Ukraine
            {
                /// <summary>
                /// Ukraine Stock Market
                /// </summary>
                public const string StockMarket = "PFTS";
                /// <summary>
                /// Ukraine Interbank Rate
                /// </summary>
                public const string InterbankRate = "UKRAINEINTRAT";
                /// <summary>
                /// Ukraine Currency
                /// </summary>
                public const string Currency = "USDUAH";
            }
            public static class UnitedArabEmirates
            {
                /// <summary>
                /// UnitedArabEmirates Stock Market
                /// </summary>
                public const string StockMarket = "ADSMI";
                /// <summary>
                /// UnitedArabEmirates Interbank Rate
                /// </summary>
                public const string InterbankRate = "UNITEDARAINTRAT";
                /// <summary>
                /// UnitedArabEmirates Currency
                /// </summary>
                public const string Currency = "USDAED";
            }
            public static class UnitedKingdom
            {
                /// <summary>
                /// UnitedKingdom Currency
                /// </summary>
                public const string Currency = "GBPUSD";
                /// <summary>
                /// UnitedKingdom Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GUKG10";
                /// <summary>
                /// UnitedKingdom Stock Market
                /// </summary>
                public const string StockMarket = "UKX";
                /// <summary>
                /// UnitedKingdom 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "UNITEDKIN2YNY";
                /// <summary>
                /// UnitedKingdom Interbank Rate
                /// </summary>
                public const string InterbankRate = "UNITEDKININTRAT";
            }
            public static class UnitedStates
            {
                /// <summary>
                /// UnitedStates Currency
                /// </summary>
                public const string Currency = "DXY";
                /// <summary>
                /// UnitedStates Interest Rate
                /// </summary>
                public const string InterestRate = "FDTR";
                /// <summary>
                /// UnitedStates Stock Market
                /// </summary>
                public const string StockMarket = "INDU";
                /// <summary>
                /// UnitedStates 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "UNITEDSTA2YEANOTYIE";
                /// <summary>
                /// UnitedStates Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "UNITEDSTAECOOPTIND";
                /// <summary>
                /// UnitedStates Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "UNITEDSTAFORDIRINV";
                /// <summary>
                /// UnitedStates Interbank Rate
                /// </summary>
                public const string InterbankRate = "UNITEDSTAINTRAT";
                /// <summary>
                /// UnitedStates Nahb Housing Market Index
                /// </summary>
                public const string NahbHousingMarketIndex = "UNITEDSTANAHHOUMARIN";
                /// <summary>
                /// UnitedStates NY Empire State Manufacturing Index
                /// </summary>
                public const string NyEmpireStateManufacturingIndex = "UNITEDSTANYEMPSTAMAN";
                /// <summary>
                /// UnitedStates Redbook Index
                /// </summary>
                public const string RedbookIndex = "UNITEDSTAREDIND";
                /// <summary>
                /// UnitedStates Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "USGG10YR";
                /// <summary>
                /// UnitedStates Crude Oil Rigs
                /// </summary>
                public const string CrudeOilRigs = "USOILRIGS";
            }
            public static class Uruguay
            {
                /// <summary>
                /// Uruguay Currency
                /// </summary>
                public const string Currency = "USDURY";
            }
            public static class Uzbekistan
            {
                /// <summary>
                /// Uzbekistan Currency
                /// </summary>
                public const string Currency = "USDUZS";
            }
            public static class Venezuela
            {
                /// <summary>
                /// Venezuela Stock Market
                /// </summary>
                public const string StockMarket = "IBVC";
                /// <summary>
                /// Venezuela Currency
                /// </summary>
                public const string Currency = "USDVES";
                /// <summary>
                /// Venezuela Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "VENEZUELAGOVBON10Y";
                /// <summary>
                /// Venezuela Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "VENFRINRDPST";
            }
            public static class Vietnam
            {
                /// <summary>
                /// Vietnam Currency
                /// </summary>
                public const string Currency = "USDVND";
                /// <summary>
                /// Vietnam Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "VIETNAMGOVBON10Y";
                /// <summary>
                /// Vietnam Interbank Rate
                /// </summary>
                public const string InterbankRate = "VIETNAMINTRAT";
                /// <summary>
                /// Vietnam Stock Market
                /// </summary>
                public const string StockMarket = "VNINDEX";
            }
            public static class Yemen
            {
                /// <summary>
                /// Yemen Currency
                /// </summary>
                public const string Currency = "USDYER";
            }
            public static class Zambia
            {
                /// <summary>
                /// Zambia Currency
                /// </summary>
                public const string Currency = "USDZMK";
                /// <summary>
                /// Zambia Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "ZAMBIAGOVBON10Y";
                /// <summary>
                /// Zambia Interest Rate
                /// </summary>
                public const string InterestRate = "ZAMMBIAINTTRATE";
            }
            public static class Zimbabwe
            {
                /// <summary>
                /// Zimbabwe Stock Market
                /// </summary>
                public const string StockMarket = "INDZI";
            }
        }
    }
}