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
                public const string BalanceOfTrade = "ALBANIABALRADE.C";
                /// <summary>
                /// Albania Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ALBANIABUSCON.C";
                /// <summary>
                /// Albania Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ALBANIACONCON.C";
                /// <summary>
                /// Albania GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ALBANIAGDPATE.C";
                /// <summary>
                /// Albania GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ALBANIAGDPRATE.C";
                /// <summary>
                /// Albania Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "ALBANIAHARCONPRI.C";
                /// <summary>
                /// Albania Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ALBANIAINDCTION.C";
                /// <summary>
                /// Albania Inflation Rate
                /// </summary>
                public const string InflationRate = "ALBANIAINFNRATE.C";
                /// <summary>
                /// Albania Interest Rate
                /// </summary>
                public const string InterestRate = "ALBANIAINTTRATE.C";
                /// <summary>
                /// Albania Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ALBANIAPROPRICHA.C";
                /// <summary>
                /// Albania Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ALBANIAUNETRATE.C";
            }
            public static class Angola
            {
                /// <summary>
                /// Angola Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "ANGOLAFOREXCRES.C";
                /// <summary>
                /// Angola Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ANGOLAINDPRO.C";
                /// <summary>
                /// Angola Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ANGOLAINDPROMOM.C";
                /// <summary>
                /// Angola Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ANGOLAINFRATMOM.C";
                /// <summary>
                /// Angola Interest Rate
                /// </summary>
                public const string InterestRate = "ANGOLAINTRATE.C";
                /// <summary>
                /// Angola Inflation Rate
                /// </summary>
                public const string InflationRate = "ANGOLAIR.C";
                /// <summary>
                /// Angola Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "ANGOLAMONSUPM3.C";
                /// <summary>
                /// Angola Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ANGOLAPROPRICHA.C";
            }
            public static class Argentina
            {
                /// <summary>
                /// Argentina Interest Rate
                /// </summary>
                public const string InterestRate = "APDR1T.C";
                /// <summary>
                /// Argentina Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ARBABAL.C";
                /// <summary>
                /// Argentina Current Account
                /// </summary>
                public const string CurrentAccount = "ARBPCURR.C";
                /// <summary>
                /// Argentina Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ARCCIND.C";
                /// <summary>
                /// Argentina Inflation Rate
                /// </summary>
                public const string InflationRate = "ARCPIYOY.C";
                /// <summary>
                /// Argentina Calendar
                /// </summary>
                public const string Calendar = "ARG-CALENDAR.C";
                /// <summary>
                /// Argentina Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ARGENTINAINFRATMOM.C";
                /// <summary>
                /// Argentina Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "ARGENTINALEAECOIND.C";
                /// <summary>
                /// Argentina GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ARGQPQOX.C";
                /// <summary>
                /// Argentina GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ARGQPYOX.C";
                /// <summary>
                /// Argentina Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ARGRETAILSALESMOM.C";
                /// <summary>
                /// Argentina Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ARGRETAILSALESYOY.C";
                /// <summary>
                /// Argentina Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ARIPNSYO.C";
                /// <summary>
                /// Argentina Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ARUERATE.C";
            }
            public static class Armenia
            {
                /// <summary>
                /// Armenia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ARMENIABALRADE.C";
                /// <summary>
                /// Armenia Construction Output
                /// </summary>
                public const string ConstructionOutput = "ARMENIACONOUT.C";
                /// <summary>
                /// Armenia Current Account
                /// </summary>
                public const string CurrentAccount = "ARMENIACURCOUNT.C";
                /// <summary>
                /// Armenia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ARMENIAGDPATE.C";
                /// <summary>
                /// Armenia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ARMENIAINDCTION.C";
                /// <summary>
                /// Armenia Inflation Rate
                /// </summary>
                public const string InflationRate = "ARMENIAINFNRATE.C";
                /// <summary>
                /// Armenia Interest Rate
                /// </summary>
                public const string InterestRate = "ARMENIAINTTRATE.C";
                /// <summary>
                /// Armenia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "ARMENIALEAECOIND.C";
                /// <summary>
                /// Armenia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ARMENIAPROPRICHA.C";
                /// <summary>
                /// Armenia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ARMENIARETSYOY.C";
                /// <summary>
                /// Armenia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ARMENIAUNETRATE.C";
            }
            public static class Australia
            {
                /// <summary>
                /// Australia Current Account
                /// </summary>
                public const string CurrentAccount = "AUCABAL.C";
                /// <summary>
                /// Australia Inflation Rate
                /// </summary>
                public const string InflationRate = "AUCPIYOY.C";
                /// <summary>
                /// Australia Calendar
                /// </summary>
                public const string Calendar = "CALENDARAUSTRALIA.C";
                /// <summary>
                /// Australia Exports
                /// </summary>
                public const string Exports = "AUITEXP.C";
                /// <summary>
                /// Australia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "AUITGSB.C";
                /// <summary>
                /// Australia Imports
                /// </summary>
                public const string Imports = "AUITIMP.C";
                /// <summary>
                /// Australia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "AULFUNEM.C";
                /// <summary>
                /// Australia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "AUNAGDPC.C";
                /// <summary>
                /// Australia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "AUNAGDPY.C";
                /// <summary>
                /// Australia Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "AUSCORECPIRATE.C";
                /// <summary>
                /// Australia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "AUSRETAILSALESMOM.C";
                /// <summary>
                /// Australia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "AUSRETAILSALESYOY.C";
                /// <summary>
                /// Australia Building Permits
                /// </summary>
                public const string BuildingPermits = "AUSTRALIABUIPER.C";
                /// <summary>
                /// Australia Business Inventories
                /// </summary>
                public const string BusinessInventories = "AUSTRALIABUSINV.C";
                /// <summary>
                /// Australia Car Registrations
                /// </summary>
                public const string CarRegistrations = "AUSTRALIACARREG.C";
                /// <summary>
                /// Australia Composite Pmi
                /// </summary>
                public const string CompositePmi = "AUSTRALIACOMPMI.C";
                /// <summary>
                /// Australia Construction Output
                /// </summary>
                public const string ConstructionOutput = "AUSTRALIACONOUT.C";
                /// <summary>
                /// Australia Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "AUSTRALIACONPMI.C";
                /// <summary>
                /// Australia Consumer Price Index (CPI)
                /// </summary>
                public const string ConsumerPriceIndex = "AUSTRALIACONPRIINDCP.C";
                /// <summary>
                /// Australia Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "AUSTRALIACONSPE.C";
                /// <summary>
                /// Australia Corporate Profits
                /// </summary>
                public const string CorporateProfits = "AUSTRALIACORPRO.C";
                /// <summary>
                /// Australia Employment Change
                /// </summary>
                public const string EmploymentChange = "AUSTRALIAEMPCHA.C";
                /// <summary>
                /// Australia Export Prices
                /// </summary>
                public const string ExportPrices = "AUSTRALIAEXPPRI.C";
                /// <summary>
                /// Australia Full Time Employment
                /// </summary>
                public const string FullTimeEmployment = "AUSTRALIAFULTIMEMP.C";
                /// <summary>
                /// Australia GDP Deflator
                /// </summary>
                public const string GdpDeflator = "AUSTRALIAGDPDEF.C";
                /// <summary>
                /// Australia Gross Fixed Capital Formation
                /// </summary>
                public const string GrossFixedCapitalFormation = "AUSTRALIAGROFIXCAPFO.C";
                /// <summary>
                /// Australia Housing Index
                /// </summary>
                public const string HousingIndex = "AUSTRALIAHOUIND.C";
                /// <summary>
                /// Australia Import Prices
                /// </summary>
                public const string ImportPrices = "AUSTRALIAIMPPRI.C";
                /// <summary>
                /// Australia Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "AUSTRALIAINFEXP.C";
                /// <summary>
                /// Australia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "AUSTRALIAINFRATMOM.C";
                /// <summary>
                /// Australia Job Advertisements
                /// </summary>
                public const string JobAdvertisements = "AUSTRALIAJOBADV.C";
                /// <summary>
                /// Australia Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "AUSTRALIALABFORPARRA.C";
                /// <summary>
                /// Australia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "AUSTRALIALEAECOIND.C";
                /// <summary>
                /// Australia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "AUSTRALIAMANPMI.C";
                /// <summary>
                /// Australia New Home Sales
                /// </summary>
                public const string NewHomeSales = "AUSTRALIANEWHOMSAL.C";
                /// <summary>
                /// Australia Part Time Employment
                /// </summary>
                public const string PartTimeEmployment = "AUSTRALIAPARTIMEMP.C";
                /// <summary>
                /// Australia Private Investment
                /// </summary>
                public const string PrivateInvestment = "AUSTRALIAPRIINV.C";
                /// <summary>
                /// Australia Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "AUSTRALIAPRISECCRE.C";
                /// <summary>
                /// Australia Producer Prices
                /// </summary>
                public const string ProducerPrices = "AUSTRALIAPROPRI.C";
                /// <summary>
                /// Australia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "AUSTRALIAPROPRICHA.C";
                /// <summary>
                /// Australia Services PMI
                /// </summary>
                public const string ServicesPmi = "AUSTRALIASERPMI.C";
                /// <summary>
                /// Australia Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "AUSTRALIATOTVEHSAL.C";
                /// <summary>
                /// Australia Wage Growth
                /// </summary>
                public const string WageGrowth = "AUSTRALIAWAGGRO.C";
                /// <summary>
                /// Australia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NABSCONF.C";
                /// <summary>
                /// Australia Interest Rate
                /// </summary>
                public const string InterestRate = "RBATCTR.C";
                /// <summary>
                /// Australia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "WMCCCONPCT.C";
            }
            public static class Austria
            {
                /// <summary>
                /// Austria GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ASGPGDPQ.C";
                /// <summary>
                /// Austria GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ASGPGDPY.C";
                /// <summary>
                /// Austria Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ATIPIYY.C";
                /// <summary>
                /// Austria Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ATTBAL.C";
                /// <summary>
                /// Austria Business Confidence
                /// </summary>
                public const string BusinessConfidence = "AUSTRIABUSCON.C";
                /// <summary>
                /// Austria Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "AUSTRIACC.C";
                /// <summary>
                /// Austria Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "AUSTRIAHARCONPRI.C";
                /// <summary>
                /// Austria Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "AUSTRIAINFRATMOM.C";
                /// <summary>
                /// Austria Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "AUSTRIAMANPMI.C";
                /// <summary>
                /// Austria Producer Prices
                /// </summary>
                public const string ProducerPrices = "AUSTRIAPROPRI.C";
                /// <summary>
                /// Austria Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "AUSTRIAPROPRICHA.C";
                /// <summary>
                /// Austria Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "AUSTRIAUNEPER.C";
                /// <summary>
                /// Austria Calendar
                /// </summary>
                public const string Calendar = "CALENDARAUSTRIA.C";
                /// <summary>
                /// Austria Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "AUTRETAILSALESMOM.C";
                /// <summary>
                /// Austria Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "AUTRETSALYOY.C";
                /// <summary>
                /// Austria Inflation Rate
                /// </summary>
                public const string InflationRate = "ECCPATYY.C";
                /// <summary>
                /// Austria Current Account
                /// </summary>
                public const string CurrentAccount = "OEATB007.C";
                /// <summary>
                /// Austria Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTAT.C";
            }
            public static class Azerbaijan
            {
                /// <summary>
                /// Azerbaijan Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "AZERBAIJANINFRATMOM.C";
                /// <summary>
                /// Azerbaijan Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "AZERBAIJANLEAECOIND.C";
            }
            public static class Bahrain
            {
                /// <summary>
                /// Bahrain GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BAHRAINGDPATE.C";
                /// <summary>
                /// Bahrain GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BAHRAINGDPGRORAT.C";
                /// <summary>
                /// Bahrain Inflation Rate
                /// </summary>
                public const string InflationRate = "BAHRAININFNRATE.C";
                /// <summary>
                /// Bahrain Interest Rate
                /// </summary>
                public const string InterestRate = "BAHRAININTTRATE.C";
                /// <summary>
                /// Bahrain Loan Growth
                /// </summary>
                public const string LoanGrowth = "BAHRAINLOAGRO.C";
                /// <summary>
                /// Bahrain Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "BAHRAINMONSUPM2.C";
            }
            public static class Bangladesh
            {
                /// <summary>
                /// Bangladesh Inflation Rate
                /// </summary>
                public const string InflationRate = "BANGLADESHIR.C";
            }
            public static class Belarus
            {
                /// <summary>
                /// Belarus Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BELARUSBALRADE.C";
                /// <summary>
                /// Belarus Current Account
                /// </summary>
                public const string CurrentAccount = "BELARUSCURCOUNT.C";
                /// <summary>
                /// Belarus Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BELARUSINDCTION.C";
                /// <summary>
                /// Belarus Inflation Rate
                /// </summary>
                public const string InflationRate = "BELARUSINFNRATE.C";
                /// <summary>
                /// Belarus Interest Rate
                /// </summary>
                public const string InterestRate = "BELARUSINTTRATE.C";
                /// <summary>
                /// Belarus Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "BELARUSLEAECOIND.C";
            }
            public static class Belgium
            {
                /// <summary>
                /// Belgium Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BEBCI.C";
                /// <summary>
                /// Belgium Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BECCN.C";
                /// <summary>
                /// Belgium Inflation Rate
                /// </summary>
                public const string InflationRate = "BECPYOY.C";
                /// <summary>
                /// Belgium GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BEGDPQS.C";
                /// <summary>
                /// Belgium GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BEGDPYS.C";
                /// <summary>
                /// Belgium Calendar
                /// </summary>
                public const string Calendar = "CALENDARBELGIUM.C";
                /// <summary>
                /// Belgium Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "BELGIUMINDPROMOM.C";
                /// <summary>
                /// Belgium Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "BELGIUMINFRATMOM.C";
                /// <summary>
                /// Belgium Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "BELGIUMPROPRICHA.C";
                /// <summary>
                /// Belgium Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BELRETAILSALESMOM.C";
                /// <summary>
                /// Belgium Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BELRETAILSALESYOY.C";
                /// <summary>
                /// Belgium Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BEPDRYY.C";
                /// <summary>
                /// Belgium Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BETBTBAL.C";
                /// <summary>
                /// Belgium Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "BEUER.C";
                /// <summary>
                /// Belgium Current Account
                /// </summary>
                public const string CurrentAccount = "OEBEB026.C";
            }
            public static class BosniaAndHerzegovina
            {
                /// <summary>
                /// BosniaAndHerzegovina Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BOSNABALRADE.C";
                /// <summary>
                /// BosniaAndHerzegovina Current Account
                /// </summary>
                public const string CurrentAccount = "BOSNACURCOUNT.C";
                /// <summary>
                /// BosniaAndHerzegovina GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BOSNAGDPATE.C";
                /// <summary>
                /// BosniaAndHerzegovina Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BOSNAINDCTION.C";
                /// <summary>
                /// BosniaAndHerzegovina Inflation Rate
                /// </summary>
                public const string InflationRate = "BOSNAINFNRATE.C";
                /// <summary>
                /// BosniaAndHerzegovina Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BOSNARETSMOM.C";
                /// <summary>
                /// BosniaAndHerzegovina Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BOSNARETSYOY.C";
                /// <summary>
                /// BosniaAndHerzegovina Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "BOSNIAANDPROPRICHA.C";
            }
            public static class Botswana
            {
                /// <summary>
                /// Botswana Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BOTSWANABT.C";
                /// <summary>
                /// Botswana GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BOTSWANAGDPQOQ.C";
                /// <summary>
                /// Botswana GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BOTSWANAGDPYOY.C";
                /// <summary>
                /// Botswana Interest Rate
                /// </summary>
                public const string InterestRate = "BOTSWANAINTRATE.C";
                /// <summary>
                /// Botswana Inflation Rate
                /// </summary>
                public const string InflationRate = "BOTSWANAIR.C";
                /// <summary>
                /// Botswana Mining Production
                /// </summary>
                public const string MiningProduction = "BOTSWANAMINPRO.C";
            }
            public static class Brazil
            {
                /// <summary>
                /// Brazil Calendar
                /// </summary>
                public const string Calendar = "CALENDARBRAZIL.C";
                /// <summary>
                /// Brazil Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BRARETAILSALESMOM.C";
                /// <summary>
                /// Brazil Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BRARETAILSALESYOY.C";
                /// <summary>
                /// Brazil Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "BRAZILCAPUTI.C";
                /// <summary>
                /// Brazil Car Production
                /// </summary>
                public const string CarProduction = "BRAZILCARPRO.C";
                /// <summary>
                /// Brazil Car Registrations
                /// </summary>
                public const string CarRegistrations = "BRAZILCARREG.C";
                /// <summary>
                /// Brazil Composite Pmi
                /// </summary>
                public const string CompositePmi = "BRAZILCOMPMI.C";
                /// <summary>
                /// Brazil Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "BRAZILFORDIRINV.C";
                /// <summary>
                /// Brazil Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "BRAZILGOVBUDVAL.C";
                /// <summary>
                /// Brazil Government Revenues
                /// </summary>
                public const string GovernmentRevenues = "BRAZILGOVREV.C";
                /// <summary>
                /// Brazil Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "BRAZILINDPROMOM.C";
                /// <summary>
                /// Brazil Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "BRAZILINFRATMOM.C";
                /// <summary>
                /// Brazil Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "BRAZILLEAECOIND.C";
                /// <summary>
                /// Brazil Loan Growth
                /// </summary>
                public const string LoanGrowth = "BRAZILPRISECCRE.C";
                /// <summary>
                /// Brazil Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "BRAZILMANPMI.C";
                /// <summary>
                /// Brazil Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "BRAZILNONFARPAY.C";
                /// <summary>
                /// Brazil Services Pmi
                /// </summary>
                public const string ServicesPmi = "BRAZILSERPMI.C";
                /// <summary>
                /// Brazil Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BRLBC.C";
                /// <summary>
                /// Brazil Current Account
                /// </summary>
                public const string CurrentAccount = "BZCACURR.C";
                /// <summary>
                /// Brazil Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BZCCI.C";
                /// <summary>
                /// Brazil Exports
                /// </summary>
                public const string Exports = "BZEXTOTD.C";
                /// <summary>
                /// Brazil GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BZGDQOQPCT.C";
                /// <summary>
                /// Brazil GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BZGDYOYPCT.C";
                /// <summary>
                /// Brazil Imports
                /// </summary>
                public const string Imports = "BZIMTOTD.C";
                /// <summary>
                /// Brazil Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BZIPYOYPCT.C";
                /// <summary>
                /// Brazil Inflation Rate
                /// </summary>
                public const string InflationRate = "BZPIIPCY.C";
                /// <summary>
                /// Brazil Interest Rate
                /// </summary>
                public const string InterestRate = "BZSTSETA.C";
                /// <summary>
                /// Brazil Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BZTBBALM.C";
                /// <summary>
                /// Brazil Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "BZUETOTN.C";
            }
            public static class Brunei
            {
                /// <summary>
                /// Brunei Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BRUUNEIBALRADE.C";
                /// <summary>
                /// Brunei GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BRUUNEIGDPATE.C";
                /// <summary>
                /// Brunei Inflation Rate
                /// </summary>
                public const string InflationRate = "BRUUNEIINFNRATE.C";
            }
            public static class Bulgaria
            {
                /// <summary>
                /// Bulgaria Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "BGRRETAILSALESMOM.C";
                /// <summary>
                /// Bulgaria Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "BGRRETAILSALESYOY.C";
                /// <summary>
                /// Bulgaria Business Confidence
                /// </summary>
                public const string BusinessConfidence = "BULGARIABC.C";
                /// <summary>
                /// Bulgaria Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "BULGARIABT.C";
                /// <summary>
                /// Bulgaria Current Account
                /// </summary>
                public const string CurrentAccount = "BULGARIACA.C";
                /// <summary>
                /// Bulgaria Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "BULGARIACC.C";
                /// <summary>
                /// Bulgaria Construction Output
                /// </summary>
                public const string ConstructionOutput = "BULGARIACONOUT.C";
                /// <summary>
                /// Bulgaria GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "BULGARIAGDPQOQ.C";
                /// <summary>
                /// Bulgaria GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "BULGARIAGDPYOY.C";
                /// <summary>
                /// Bulgaria Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "BULGARIAINDPROMOM.C";
                /// <summary>
                /// Bulgaria Interest Rate
                /// </summary>
                public const string InterestRate = "BULGARIAINTRATE.C";
                /// <summary>
                /// Bulgaria Industrial Production
                /// </summary>
                public const string IndustrialProduction = "BULGARIAIP.C";
                /// <summary>
                /// Bulgaria Inflation Rate
                /// </summary>
                public const string InflationRate = "BULGARIAIR.C";
                /// <summary>
                /// Bulgaria Producer Prices
                /// </summary>
                public const string ProducerPrices = "BULGARIAPROPRI.C";
                /// <summary>
                /// Bulgaria Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "BULGARIAPROPRICHA.C";
                /// <summary>
                /// Bulgaria Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "BULGARIATOUARR.C";
                /// <summary>
                /// Bulgaria Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "BULGARIAUR.C";
            }
            public static class Burundi
            {
                /// <summary>
                /// Burundi Inflation Rate
                /// </summary>
                public const string InflationRate = "BURUNDIINFNRATE.C";
            }
            public static class Canada
            {
                /// <summary>
                /// Canada Inflation Rate
                /// </summary>
                public const string InflationRate = "CACPIYOY.C";
                /// <summary>
                /// Canada Current Account
                /// </summary>
                public const string CurrentAccount = "CACURENT.C";
                /// <summary>
                /// Canada Calendar
                /// </summary>
                public const string Calendar = "CALENDARCANADA.C";
                /// <summary>
                /// Canada Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "CANADAADPEMPCHA.C";
                /// <summary>
                /// Canada Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "CANADAAVEHOUEAR.C";
                /// <summary>
                /// Canada Building Permits
                /// </summary>
                public const string BuildingPermits = "CANADABUIPER.C";
                /// <summary>
                /// Canada Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "CANADACAPUTI.C";
                /// <summary>
                /// Canada Car Registrations
                /// </summary>
                public const string CarRegistrations = "CANADACARREG.C";
                /// <summary>
                /// Canada Employment Change
                /// </summary>
                public const string EmploymentChange = "CANADAEMPCHA.C";
                /// <summary>
                /// Canada Foreign Stock Investment
                /// </summary>
                public const string ForeignStockInvestment = "CANADAFORSTOINV.C";
                /// <summary>
                /// Canada Full Time Employment
                /// </summary>
                public const string FullTimeEmployment = "CANADAFULTIMEMP.C";
                /// <summary>
                /// Canada GDP Deflator
                /// </summary>
                public const string GdpDeflator = "CANADAGDPDEF.C";
                /// <summary>
                /// Canada Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "CANADAGDPGROANN.C";
                /// <summary>
                /// Canada Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "CANADAGOVBUDVAL.C";
                /// <summary>
                /// Canada Housing Index
                /// </summary>
                public const string HousingIndex = "CANADAHOUIND.C";
                /// <summary>
                /// Canada Housing Starts
                /// </summary>
                public const string HousingStarts = "CANADAHOUSTA.C";
                /// <summary>
                /// Canada Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CANADAINFRATMOM.C";
                /// <summary>
                /// Canada Job Vacancies
                /// </summary>
                public const string JobVacancies = "CANADAJOBVAC.C";
                /// <summary>
                /// Canada Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "LABOR-FORCE-PARTICIPATION-RATECANADA.C";
                /// <summary>
                /// Canada Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "CANADALEAECOIND.C";
                /// <summary>
                /// Canada Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "CANADAMANPMI.C";
                /// <summary>
                /// Canada Part Time Employment
                /// </summary>
                public const string PartTimeEmployment = "CANADAPARTIMEMP.C";
                /// <summary>
                /// Canada Productivity
                /// </summary>
                public const string Productivity = "CANADAPRO.C";
                /// <summary>
                /// Canada Producer Prices
                /// </summary>
                public const string ProducerPrices = "CANADAPROPRI.C";
                /// <summary>
                /// Canada Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CANADAPROPRICHA.C";
                /// <summary>
                /// Canada Retail Sales Ex Autos
                /// </summary>
                public const string RetailSalesExAutos = "RETAIL-SALES-EX-AUTOSCANADA.C";
                /// <summary>
                /// Canada Wage Growth
                /// </summary>
                public const string WageGrowth = "CANADAWAGGRO.C";
                /// <summary>
                /// Canada Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "CANCORECPIRATE.C";
                /// <summary>
                /// Canada Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CANLXEMR.C";
                /// <summary>
                /// Canada Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CANNADARETSYOY.C";
                /// <summary>
                /// Canada Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CANRETAILSALESMOM.C";
                /// <summary>
                /// Canada Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CATBTOTB.C";
                /// <summary>
                /// Canada Exports
                /// </summary>
                public const string Exports = "CATBTOTE.C";
                /// <summary>
                /// Canada Imports
                /// </summary>
                public const string Imports = "CATBTOTI.C";
                /// <summary>
                /// Canada Interest Rate
                /// </summary>
                public const string InterestRate = "CCLR.C";
                /// <summary>
                /// Canada GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CGE9QOQ.C";
                /// <summary>
                /// Canada GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CGE9YOY.C";
                /// <summary>
                /// Canada Business Confidence
                /// </summary>
                public const string BusinessConfidence = "IVEY.C";
            }
            public static class CapeVerde
            {
                /// <summary>
                /// CapeVerde Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CAPRDEBUSDENCE.C";
                /// <summary>
                /// CapeVerde Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CAPRDECONDENCE.C";
                /// <summary>
                /// CapeVerde GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CAPRDEGDPATE.C";
                /// <summary>
                /// CapeVerde Inflation Rate
                /// </summary>
                public const string InflationRate = "CAPRDEINFNRATE.C";
            }
            public static class Chile
            {
                /// <summary>
                /// Chile Copper Production
                /// </summary>
                public const string CopperProduction = "CHILECOPPRO.C";
                /// <summary>
                /// Chile Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "CHILECORCONPRI.C";
                /// <summary>
                /// Chile Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CHILEINFRATMOM.C";
                /// <summary>
                /// Chile Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "CHILELEAECOIND.C";
                /// <summary>
                /// Chile Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "CHILEMANPRO.C";
                /// <summary>
                /// Chile Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CHILEPROPRICHA.C";
                /// <summary>
                /// Chile Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CHIPYOY.C";
                /// <summary>
                /// Chile Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CHLRETAILSALESMOM.C";
                /// <summary>
                /// Chile Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CHLRETAILSALESYOY.C";
                /// <summary>
                /// Chile Interest Rate
                /// </summary>
                public const string InterestRate = "CHOVCHOV.C";
                /// <summary>
                /// Chile Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CHTBBALM.C";
                /// <summary>
                /// Chile Exports
                /// </summary>
                public const string Exports = "CHTBEXPM.C";
                /// <summary>
                /// Chile Imports
                /// </summary>
                public const string Imports = "CHTBIMPM.C";
                /// <summary>
                /// Chile Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CHUETOTL.C";
                /// <summary>
                /// Chile Current Account
                /// </summary>
                public const string CurrentAccount = "CLBPCURA.C";
                /// <summary>
                /// Chile GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CLGDPNAPCT.C";
                /// <summary>
                /// Chile GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CLGDQSA.C";
                /// <summary>
                /// Chile Inflation Rate
                /// </summary>
                public const string InflationRate = "CNPINSYO.C";
            }
            public static class China
            {
                /// <summary>
                /// China Calendar
                /// </summary>
                public const string Calendar = "CHN-CALENDAR.C";
                /// <summary>
                /// China Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CHBUBCIN.C";
                /// <summary>
                /// China Current Account
                /// </summary>
                public const string CurrentAccount = "CHCURENT.C";
                /// <summary>
                /// China Banks Balance Sheet
                /// </summary>
                public const string BanksBalanceSheet = "CHINABANBALSHE.C";
                /// <summary>
                /// China Capital Flows
                /// </summary>
                public const string CapitalFlows = "CHINACAPFLO.C";
                /// <summary>
                /// China Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "CHINACAPUTI.C";
                /// <summary>
                /// China Composite Pmi
                /// </summary>
                public const string CompositePmi = "CHINACOMPMI.C";
                /// <summary>
                /// China Corporate Profits
                /// </summary>
                public const string CorporateProfits = "CHINACORPRO.C";
                /// <summary>
                /// China Fixed Asset Investment
                /// </summary>
                public const string FixedAssetInvestment = "CHINAFIXASSINV.C";
                /// <summary>
                /// China Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "CHINAFORDIRINV.C";
                /// <summary>
                /// China Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "CHINAFOREXCRES.C";
                /// <summary>
                /// China Housing Index
                /// </summary>
                public const string HousingIndex = "CHINAHOUIND.C";
                /// <summary>
                /// China Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CHINAINFRATMOM.C";
                /// <summary>
                /// China Loan Growth
                /// </summary>
                public const string LoanGrowth = "CHINALOAGRO.C";
                /// <summary>
                /// China Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "CHINALOATOPRISEC.C";
                /// <summary>
                /// China Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "CHINAMANPMI.C";
                /// <summary>
                /// China Mni Business Sentiment
                /// </summary>
                public const string MniBusinessSentiment = "CHINAMNIBUSSEN.C";
                /// <summary>
                /// China Mni Consumer Sentiment
                /// </summary>
                public const string MniConsumerSentiment = "CHINAMNICONSEN.C";
                /// <summary>
                /// China Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "CHINAMONSUPM2.C";
                /// <summary>
                /// China Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "CHINANONMANPMI.C";
                /// <summary>
                /// China Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CHINAPROPRICHA.C";
                /// <summary>
                /// China Services Pmi
                /// </summary>
                public const string ServicesPmi = "CHINASERPMI.C";
                /// <summary>
                /// China Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "CHINATOTVEHSAL.C";
                /// <summary>
                /// China Interest Rate
                /// </summary>
                public const string InterestRate = "CHLR12M.C";
                /// <summary>
                /// China Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CHNRETAILSALESYOY.C";
                /// <summary>
                /// China Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CHVAIOY.C";
                /// <summary>
                /// China Inflation Rate
                /// </summary>
                public const string InflationRate = "CNCPIYOY.C";
                /// <summary>
                /// China Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CNFRBALD.C";
                /// <summary>
                /// China Exports
                /// </summary>
                public const string Exports = "CNFREXPD.C";
                /// <summary>
                /// China Imports
                /// </summary>
                public const string Imports = "CNFRIMPD.C";
                /// <summary>
                /// China GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CNGDPQOQ.C";
                /// <summary>
                /// China GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CNGDPYOY.C";
            }
            public static class Colombia
            {
                /// <summary>
                /// Colombia Current Account
                /// </summary>
                public const string CurrentAccount = "COBPCURR.C";
                /// <summary>
                /// Colombia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "COCIPIBQ.C";
                /// <summary>
                /// Colombia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "COCIPIBY.C";
                /// <summary>
                /// Colombia Inflation Rate
                /// </summary>
                public const string InflationRate = "COCPIYOY.C";
                /// <summary>
                /// Colombia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "COIPEYOY.C";
                /// <summary>
                /// Colombia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "COLMBIABUSDENCE.C";
                /// <summary>
                /// Colombia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "COLMBIACONDENCE.C";
                /// <summary>
                /// Colombia Cement Production
                /// </summary>
                public const string CementProduction = "COLOMBIACEMPRO.C";
                /// <summary>
                /// Colombia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "COLOMBIAINFRATMOM.C";
                /// <summary>
                /// Colombia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "COLOMBIALEAECOIND.C";
                /// <summary>
                /// Colombia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "COLOMBIAMANPMI.C";
                /// <summary>
                /// Colombia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "COLOMBIAPROPRICHA.C";
                /// <summary>
                /// Colombia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "COLRETAILSALESMOM.C";
                /// <summary>
                /// Colombia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "COLRETAILSALESYOY.C";
                /// <summary>
                /// Colombia Interest Rate
                /// </summary>
                public const string InterestRate = "CORRRMIN.C";
                /// <summary>
                /// Colombia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "COTRBALM.C";
                /// <summary>
                /// Colombia Exports
                /// </summary>
                public const string Exports = "COTREXPM.C";
                /// <summary>
                /// Colombia Imports
                /// </summary>
                public const string Imports = "COTRIMPM.C";
                /// <summary>
                /// Colombia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "COUNTOTR.C";
            }
            public static class CostaRica
            {
                /// <summary>
                /// CostaRica Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "COSICABALRADE.C";
                /// <summary>
                /// CostaRica GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "COSICAGDPATE.C";
                /// <summary>
                /// CostaRica Inflation Rate
                /// </summary>
                public const string InflationRate = "COSICAINFNRATE.C";
                /// <summary>
                /// CostaRica Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "COSICAUNETRATE.C";
                /// <summary>
                /// CostaRica Current Account
                /// </summary>
                public const string CurrentAccount = "COSTARICACURACC.C";
                /// <summary>
                /// CostaRica Gdp Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "COSTARICAGDPGRORAT.C";
                /// <summary>
                /// CostaRica Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "COSTARICAINFRATMOM.C";
            }
            public static class Croatia
            {
                /// <summary>
                /// Croatia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CROATIABALRADE.C";
                /// <summary>
                /// Croatia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CROATIABUSCON.C";
                /// <summary>
                /// Croatia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CROATIACONCON.C";
                /// <summary>
                /// Croatia Current Account
                /// </summary>
                public const string CurrentAccount = "CROATIACURCOUNT.C";
                /// <summary>
                /// Croatia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CROATIAGDPATE.C";
                /// <summary>
                /// Croatia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CROATIAGDPRATE.C";
                /// <summary>
                /// Croatia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CROATIAINDCTION.C";
                /// <summary>
                /// Croatia Inflation Rate
                /// </summary>
                public const string InflationRate = "CROATIAINFNRATE.C";
                /// <summary>
                /// Croatia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CROATIAINFRATMOM.C";
                /// <summary>
                /// Croatia Producer Prices
                /// </summary>
                public const string ProducerPrices = "CROATIAPROPRI.C";
                /// <summary>
                /// Croatia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CROATIAPROPRICHA.C";
                /// <summary>
                /// Croatia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CROATIARETSMOM.C";
                /// <summary>
                /// Croatia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CROATIARETSYOY.C";
                /// <summary>
                /// Croatia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CROATIAUNETRATE.C";
                /// <summary>
                /// Croatia Calendar
                /// </summary>
                public const string Calendar = "HRV-CALENDAR.C";
            }
            public static class Cyprus
            {
                /// <summary>
                /// Cyprus Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CYPPRUSBALRADE.C";
                /// <summary>
                /// Cyprus Current Account
                /// </summary>
                public const string CurrentAccount = "CYPPRUSCURCOUNT.C";
                /// <summary>
                /// Cyprus GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CYPPRUSGDPATE.C";
                /// <summary>
                /// Cyprus GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CYPPRUSGDPRATE.C";
                /// <summary>
                /// Cyprus Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CYPPRUSINDCTION.C";
                /// <summary>
                /// Cyprus Inflation Rate
                /// </summary>
                public const string InflationRate = "CYPPRUSINFNRATE.C";
                /// <summary>
                /// Cyprus Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CYPPRUSRETSYOY.C";
                /// <summary>
                /// Cyprus Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CYPPRUSUNETRATE.C";
                /// <summary>
                /// Cyprus Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CYPRUSBUSCON.C";
                /// <summary>
                /// Cyprus Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CYPRUSCONCON.C";
                /// <summary>
                /// Cyprus Construction Output
                /// </summary>
                public const string ConstructionOutput = "CYPRUSCONOUT.C";
                /// <summary>
                /// Cyprus Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "CYPRUSHARCONPRI.C";
                /// <summary>
                /// Cyprus Wage Growth
                /// </summary>
                public const string WageGrowth = "CYPRUSWAGGRO.C";
            }
            public static class CzechRepublic
            {
                /// <summary>
                /// CzechRepublic Calendar
                /// </summary>
                public const string Calendar = "CZE-CALENDAR.C";
                /// <summary>
                /// CzechRepublic Interest Rate
                /// </summary>
                public const string InterestRate = "CZBRREPO.C";
                /// <summary>
                /// CzechRepublic Current Account
                /// </summary>
                public const string CurrentAccount = "CZCAEUR.C";
                /// <summary>
                /// CzechRepublic Business Confidence
                /// </summary>
                public const string BusinessConfidence = "CZCCBUS.C";
                /// <summary>
                /// CzechRepublic Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CZCCCON.C";
                /// <summary>
                /// CzechRepublic Inflation Rate
                /// </summary>
                public const string InflationRate = "CZCPYOY.C";
                /// <summary>
                /// CzechRepublic Construction Output
                /// </summary>
                public const string ConstructionOutput = "CZECHREPUCONOUT.C";
                /// <summary>
                /// CzechRepublic External Debt
                /// </summary>
                public const string ExternalDebt = "CZECHREPUEXTDEB.C";
                /// <summary>
                /// CzechRepublic Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "CZECHREPUFOREXCRES.C";
                /// <summary>
                /// CzechRepublic Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "CZECHREPUINDPROMOM.C";
                /// <summary>
                /// CzechRepublic Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "CZECHREPUINFRATMOM.C";
                /// <summary>
                /// CzechRepublic Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "CZECHREPUMANPMI.C";
                /// <summary>
                /// CzechRepublic Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "CZECHREPUMONSUPM2.C";
                /// <summary>
                /// CzechRepublic Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "CZECHREPUMONSUPM3.C";
                /// <summary>
                /// CzechRepublic Producer Prices
                /// </summary>
                public const string ProducerPrices = "CZECHREPUPROPRI.C";
                /// <summary>
                /// CzechRepublic Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "CZECHREPUPROPRICHA.C";
                /// <summary>
                /// CzechRepublic Wage Growth
                /// </summary>
                public const string WageGrowth = "CZECHREPUWAGGRO.C";
                /// <summary>
                /// CzechRepublic Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CZERETAILSALESMOM.C";
                /// <summary>
                /// CzechRepublic Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CZERETAILSALESYOY.C";
                /// <summary>
                /// CzechRepublic GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "CZGDPQOQ.C";
                /// <summary>
                /// CzechRepublic GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "CZGDPYOY.C";
                /// <summary>
                /// CzechRepublic Industrial Production
                /// </summary>
                public const string IndustrialProduction = "CZIPITYY.C";
                /// <summary>
                /// CzechRepublic Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "CZJLR.C";
                /// <summary>
                /// CzechRepublic Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "CZTBAL.C";
                /// <summary>
                /// CzechRepublic Exports
                /// </summary>
                public const string Exports = "CZTBEX.C";
                /// <summary>
                /// CzechRepublic Imports
                /// </summary>
                public const string Imports = "CZTBIM.C";
            }
            public static class Denmark
            {
                /// <summary>
                /// Denmark Calendar
                /// </summary>
                public const string Calendar = "CALENDARDENMARK.C";
                /// <summary>
                /// Denmark Current Account
                /// </summary>
                public const string CurrentAccount = "DECA.C";
                /// <summary>
                /// Denmark GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "DEGDPNQQ.C";
                /// <summary>
                /// Denmark GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "DEGDPNYY.C";
                /// <summary>
                /// Denmark Industrial Production
                /// </summary>
                public const string IndustrialProduction = "DEMFSAYY.C";
                /// <summary>
                /// Denmark Business Confidence
                /// </summary>
                public const string BusinessConfidence = "DENMARKBC.C";
                /// <summary>
                /// Denmark Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "DENMARKCC.C";
                /// <summary>
                /// Denmark Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "DENMARKFOREXCRES.C";
                /// <summary>
                /// Denmark Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "DENMARKHARCONPRI.C";
                /// <summary>
                /// Denmark Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "DNK-CALENDAR.C";
                /// <summary>
                /// Denmark Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "DENMARKMANPMI.C";
                /// <summary>
                /// Denmark Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "DETBHAL.C";
                /// <summary>
                /// Denmark Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "DEUE.C";
                /// <summary>
                /// Denmark Inflation Rate
                /// </summary>
                public const string InflationRate = "DNCPIYOY.C";
                /// <summary>
                /// Denmark Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "DNKRETAILSALESMOM.C";
                /// <summary>
                /// Denmark Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "DNKRETAILSALESYOY.C";
            }
            public static class DominicanRepublic
            {
                /// <summary>
                /// DominicanRepublic Inflation Rate
                /// </summary>
                public const string InflationRate = "DOMLICINFNRATE.C";
                /// <summary>
                /// DominicanRepublic Interest Rate
                /// </summary>
                public const string InterestRate = "DOMLICINTTRATE.C";
            }
            public static class EastTimor
            {
                /// <summary>
                /// EastTimor Inflation Rate
                /// </summary>
                public const string InflationRate = "TIMIMORINFNRATE.C";
            }
            public static class Ecuador
            {
                /// <summary>
                /// Ecuador Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ECUADORBALRADE.C";
                /// <summary>
                /// Ecuador Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ECUADORCONDENCE.C";
                /// <summary>
                /// Ecuador GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ECUADORGDPATE.C";
                /// <summary>
                /// Ecuador GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ECUADORGDPRATE.C";
                /// <summary>
                /// Ecuador Inflation Rate
                /// </summary>
                public const string InflationRate = "ECUADORINFNRATE.C";
            }
            public static class Egypt
            {
                /// <summary>
                /// Egypt Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "EGYPTCORINFRAT.C";
                /// <summary>
                /// Egypt Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "EGYPTFOREXCRES.C";
                /// <summary>
                /// Egypt Interest Rate
                /// </summary>
                public const string InterestRate = "EGYPTINTRATE.C";
                /// <summary>
                /// Egypt Inflation Rate
                /// </summary>
                public const string InflationRate = "EGYPTIR.C";
                /// <summary>
                /// Egypt Lending Rate
                /// </summary>
                public const string LendingRate = "EGYPTLENRAT.C";
                /// <summary>
                /// Egypt Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "EGYPTMANPMI.C";
                /// <summary>
                /// Egypt Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "EGYPTMONSUPM2.C";
                /// <summary>
                /// Egypt Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "EGYPTUR.C";
            }
            public static class ElSalvador
            {
                /// <summary>
                /// ElSalvador GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ELSALVADOGDPGRORAT.C";
                /// <summary>
                /// ElSalvador Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ELSALVADOINDPRO.C";
                /// <summary>
                /// ElSalvador Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ELSDORBALRADE.C";
                /// <summary>
                /// ElSalvador Current Account
                /// </summary>
                public const string CurrentAccount = "ELSDORCURCOUNT.C";
                /// <summary>
                /// ElSalvador GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ELSDORGDPATE.C";
                /// <summary>
                /// ElSalvador Inflation Rate
                /// </summary>
                public const string InflationRate = "ELSDORINFNRATE.C";
            }
            public static class Estonia
            {
                /// <summary>
                /// Estonia Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ESTONIAINDPROMOM.C";
                /// <summary>
                /// Estonia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ESTONIABT.C";
                /// <summary>
                /// Estonia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ESTONIABUSCON.C";
                /// <summary>
                /// Estonia Current Account
                /// </summary>
                public const string CurrentAccount = "ESTONIACA.C";
                /// <summary>
                /// Estonia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ESTONIAGDPQOQ.C";
                /// <summary>
                /// Estonia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ESTONIAGDPYOY.C";
                /// <summary>
                /// Estonia Imports
                /// </summary>
                public const string Imports = "ESTONIAIM.C";
                /// <summary>
                /// Estonia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ESTONIAINFRATMOM.C";
                /// <summary>
                /// Estonia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ESTONIAIP.C";
                /// <summary>
                /// Estonia Inflation Rate
                /// </summary>
                public const string InflationRate = "ESTONIAIR.C";
                /// <summary>
                /// Estonia Producer Prices
                /// </summary>
                public const string ProducerPrices = "ESTONIAPROPRI.C";
                /// <summary>
                /// Estonia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ESTONIAPROPRICHA.C";
                /// <summary>
                /// Estonia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ESTONIAUR.C";
                /// <summary>
                /// Estonia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ESTRETAILSALESMOM.C";
                /// <summary>
                /// Estonia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ESTRETAILSALESYOY.C";
            }
            public static class Ethiopia
            {
                /// <summary>
                /// Ethiopia Inflation Rate
                /// </summary>
                public const string InflationRate = "ETHOPIAINFNRATE.C";
            }
            public static class EuroArea
            {
                /// <summary>
                /// EuroArea Calendar
                /// </summary>
                public const string Calendar = "EUR-CALENDAR.C";
                /// <summary>
                /// EuroArea Consumer Confidence Price Trends
                /// </summary>
                public const string ConsumerConfidencePriceTrends = "CC141.C";
                /// <summary>
                /// EuroArea Inflation Rate
                /// </summary>
                public const string InflationRate = "ECCPEMUY.C";
                /// <summary>
                /// EuroArea Government Debt to GDP
                /// </summary>
                public const string GovernmentDebtToGdp = "EMUDEBT2GDP.C";
                /// <summary>
                /// EuroArea Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "EMUEVOLVLOATOPRISEC.C";
                /// <summary>
                /// EuroArea Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "EMUEVOLVMONSUPM3.C";
                /// <summary>
                /// EuroArea Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "EMURETAILSALESMOM.C";
                /// <summary>
                /// EuroArea Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "EMURETAILSALESYOY.C";
                /// <summary>
                /// EuroArea Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "EUCCEMU.C";
                /// <summary>
                /// EuroArea Current Account
                /// </summary>
                public const string CurrentAccount = "EUCQCA11.C";
                /// <summary>
                /// EuroArea Business Confidence
                /// </summary>
                public const string BusinessConfidence = "EUESEMU.C";
                /// <summary>
                /// EuroArea GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "EUGNEMUQ.C";
                /// <summary>
                /// EuroArea GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "EUGNEMUY.C";
                /// <summary>
                /// EuroArea Industrial Production
                /// </summary>
                public const string IndustrialProduction = "EUIPEMUY.C";
                /// <summary>
                /// EuroArea Composite Pmi
                /// </summary>
                public const string CompositePmi = "EUROAREACOMPMI.C";
                /// <summary>
                /// EuroArea Construction Output
                /// </summary>
                public const string ConstructionOutput = "EUROAREACONOUT.C";
                /// <summary>
                /// EuroArea Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "EUROAREACORINFRAT.C";
                /// <summary>
                /// EuroArea Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "EUROAREADEPINTRAT.C";
                /// <summary>
                /// EuroArea Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "EUROAREAECOOPTIND.C";
                /// <summary>
                /// EuroArea Employment Change
                /// </summary>
                public const string EmploymentChange = "EUROAREAEMPCHA.C";
                /// <summary>
                /// EuroArea Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "EUROAREAINDPROMOM.C";
                /// <summary>
                /// EuroArea Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "EUROAREAINFRATMOM.C";
                /// <summary>
                /// EuroArea Labour Costs
                /// </summary>
                public const string LabourCosts = "EUROAREALABCOS.C";
                /// <summary>
                /// EuroArea Lending Rate
                /// </summary>
                public const string LendingRate = "EUROAREALENRAT.C";
                /// <summary>
                /// EuroArea Loan Growth
                /// </summary>
                public const string LoanGrowth = "EUROAREALOAGRO.C";
                /// <summary>
                /// EuroArea Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "EUROAREAMANPMI.C";
                /// <summary>
                /// EuroArea Producer Prices
                /// </summary>
                public const string ProducerPrices = "EUROAREAPROPRI.C";
                /// <summary>
                /// EuroArea Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "EUROAREAPROPRICHA.C";
                /// <summary>
                /// EuroArea Services Pmi
                /// </summary>
                public const string ServicesPmi = "EUROAREASERPMI.C";
                /// <summary>
                /// EuroArea Wage Growth
                /// </summary>
                public const string WageGrowth = "EUROAREAWAGGRO.C";
                /// <summary>
                /// EuroArea Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "EUROAREAZEWECOSENIND.C";
                /// <summary>
                /// EuroArea Interest Rate
                /// </summary>
                public const string InterestRate = "EURR002W.C";
                /// <summary>
                /// EuroArea Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTEMU.C";
                /// <summary>
                /// EuroArea Government Budget
                /// </summary>
                public const string GovernmentBudget = "WDEBEURO.C";
                /// <summary>
                /// EuroArea Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "XTTBEZ.C";
            }
            public static class Fiji
            {
                /// <summary>
                /// Fiji Inflation Rate
                /// </summary>
                public const string InflationRate = "FIJFIJIINFNRATE.C";
                /// <summary>
                /// Fiji Interest Rate
                /// </summary>
                public const string InterestRate = "FIJFIJIINTTRATE.C";
            }
            public static class Finland
            {
                /// <summary>
                /// Finland Calendar
                /// </summary>
                public const string Calendar = "FIN-CALENDAR.C";
                /// <summary>
                /// Finland Current Account
                /// </summary>
                public const string CurrentAccount = "FICAEUR.C";
                /// <summary>
                /// Finland Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "FICCI.C";
                /// <summary>
                /// Finland Inflation Rate
                /// </summary>
                public const string InflationRate = "FICP2YOY.C";
                /// <summary>
                /// Finland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "FIGDPCH.C";
                /// <summary>
                /// Finland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "FIGDPYOY.C";
                /// <summary>
                /// Finland Industrial Production
                /// </summary>
                public const string IndustrialProduction = "FIIPYOY.C";
                /// <summary>
                /// Finland Business Confidence
                /// </summary>
                public const string BusinessConfidence = "FINLANDBUSCON.C";
                /// <summary>
                /// Finland Export Prices
                /// </summary>
                public const string ExportPrices = "FINLANDEXPPRI.C";
                /// <summary>
                /// Finland Import Prices
                /// </summary>
                public const string ImportPrices = "FINLANDIMPPRI.C";
                /// <summary>
                /// Finland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "FINLANDINFRATMOM.C";
                /// <summary>
                /// Finland Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "FINLANDLEAECOIND.C";
                /// <summary>
                /// Finland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "FINLANDPROPRICHA.C";
                /// <summary>
                /// Finland Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "FINLANDRETSALMOM.C";
                /// <summary>
                /// Finland Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "FINLANDRETSALYOY.C";
                /// <summary>
                /// Finland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "FITBAL.C";
                /// <summary>
                /// Finland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTFI.C";
            }
            public static class France
            {
                /// <summary>
                /// France Calendar
                /// </summary>
                public const string Calendar = "FRAINDICATORS.C";
                /// <summary>
                /// France Industrial Production
                /// </summary>
                public const string IndustrialProduction = "FPIPYOY.C";
                /// <summary>
                /// France 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "FRANCE3MONBILYIE.C";
                /// <summary>
                /// France 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "FRANCE3YNY.C";
                /// <summary>
                /// France 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "FRANCE5WBY.C";
                /// <summary>
                /// France 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "FRANCE5YNY.C";
                /// <summary>
                /// France 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "FRANCE6MONBILYIE.C";
                /// <summary>
                /// France Composite Pmi
                /// </summary>
                public const string CompositePmi = "FRANCECOMPMI.C";
                /// <summary>
                /// France Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "FRANCEGOVBUDVAL.C";
                /// <summary>
                /// France Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "FRANCEHARCONPRI.C";
                /// <summary>
                /// France Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "FRANCEINDPROMOM.C";
                /// <summary>
                /// France Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "FRANCEINFRATMOM.C";
                /// <summary>
                /// France Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "FRANCEINIJOBCLA.C";
                /// <summary>
                /// France Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "FRANCEMANPMI.C";
                /// <summary>
                /// France Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "FRANCENONFARPAY.C";
                /// <summary>
                /// France Nonfarm Payrolls Private
                /// </summary>
                public const string NonfarmPayrollsPrivate = "FRANCENONPAYPRI.C";
                /// <summary>
                /// France Personal Spending
                /// </summary>
                public const string PersonalSpending = "FRANCEPERSPE.C";
                /// <summary>
                /// France Producer Prices
                /// </summary>
                public const string ProducerPrices = "FRANCEPROPRI.C";
                /// <summary>
                /// France Services Pmi
                /// </summary>
                public const string ServicesPmi = "FRANCESERPMI.C";
                /// <summary>
                /// France Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "FRANCEUNEPER.C";
                /// <summary>
                /// France Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "FRARETAILSALESMOM.C";
                /// <summary>
                /// France Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "FRARETAILSALESYOY.C";
                /// <summary>
                /// France Current Account
                /// </summary>
                public const string CurrentAccount = "FRCAEURO.C";
                /// <summary>
                /// France Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "FRCCI.C";
                /// <summary>
                /// France Inflation Rate
                /// </summary>
                public const string InflationRate = "FRCPIYOY.C";
                /// <summary>
                /// France GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "FRGEGDPQ.C";
                /// <summary>
                /// France GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "FRGEGDPY.C";
                /// <summary>
                /// France Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "FRTEBAL.C";
                /// <summary>
                /// France Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GFRN10.C";
                /// <summary>
                /// France Business Confidence
                /// </summary>
                public const string BusinessConfidence = "INSESYNT.C";
                /// <summary>
                /// France Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTFR.C";
            }
            public static class Georgia
            {
                /// <summary>
                /// Georgia Balance of Trade
                /// </summary>BasicTemplateFrameworkAlgorithm
                public const string BalanceOfTrade = "GEORGIABALRADE.C";
                /// <summary>
                /// Georgia Current Account
                /// </summary>
                public const string CurrentAccount = "GEORGIACURCOUNT.C";
                /// <summary>
                /// Georgia Current Account to GDP
                /// </summary>
                public const string CurrentAccountToGdp = "GEORGIACURGDP.C";
                /// <summary>
                /// Georgia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GEORGIAGDPATE.C";
                /// <summary>
                /// Georgia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GEORGIAINDCTION.C";
                /// <summary>
                /// Georgia Inflation Rate
                /// </summary>
                public const string InflationRate = "GEORGIAINFNRATE.C";
                /// <summary>
                /// Georgia Interest Rate
                /// </summary>
                public const string InterestRate = "GEORGIAINTTRATE.C";
                /// <summary>
                /// Georgia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "GEORGIALEAECOIND.C";
                /// <summary>
                /// Georgia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GEORGIAPROPRICHA.C";
                /// <summary>
                /// Georgia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "GEORGIARETSYOY.C";
                /// <summary>
                /// Georgia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "GEORGIAUNETRATE.C";
            }
            public static class Germany
            {
                /// <summary>
                /// Germany Calendar
                /// </summary>
                public const string Calendar = "DEUINDICATORS.C";
                /// <summary>
                /// Germany Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "DEURETAILSALESMOM.C";
                /// <summary>
                /// Germany Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "DEURETAILSALESYOY.C";
                /// <summary>
                /// Germany Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GDBR10.C";
                /// <summary>
                /// Germany 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "GERMANY2YNY.C";
                /// <summary>
                /// Germany 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "GERMANY3YBY.C";
                /// <summary>
                /// Germany 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "GERMANY5WBY.C";
                /// <summary>
                /// Germany 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "GERMANY5YNY.C";
                /// <summary>
                /// Germany Composite Pmi
                /// </summary>
                public const string CompositePmi = "GERMANYCOMPMI.C";
                /// <summary>
                /// Germany Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "GERMANYCONPMI.C";
                /// <summary>
                /// Germany Employed Persons
                /// </summary>
                public const string EmployedPersons = "GERMANYEMPPER.C";
                /// <summary>
                /// Germany Factory Orders
                /// </summary>
                public const string FactoryOrders = "GERMANYFACORD.C";
                /// <summary>
                /// Germany Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "GERMANYHARCONPRI.C";
                /// <summary>
                /// Germany Import Prices
                /// </summary>
                public const string ImportPrices = "GERMANYIMPPRI.C";
                /// <summary>
                /// Germany Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "GERMANYINDPROMOM.C";
                /// <summary>
                /// Germany Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "GERMANYINFRATMOM.C";
                /// <summary>
                /// Germany Job Vacancies
                /// </summary>
                public const string JobVacancies = "GERMANYJOBVAC.C";
                /// <summary>
                /// Germany Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "GERMANYMANPMI.C";
                /// <summary>
                /// Germany Producer Prices
                /// </summary>
                public const string ProducerPrices = "GERMANYPROPRI.C";
                /// <summary>
                /// Germany Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GERMANYPROPRICHA.C";
                /// <summary>
                /// Germany Services Pmi
                /// </summary>
                public const string ServicesPmi = "GERMANYSERPMI.C";
                /// <summary>
                /// Germany Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "GERMANYUNECHA.C";
                /// <summary>
                /// Germany Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "GERMANYUNEPER.C";
                /// <summary>
                /// Germany Wholesale Prices
                /// </summary>
                public const string WholesalePrices = "GERMANYWHOPRI.C";
                /// <summary>
                /// Germany Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "GERMANYZEWECOSENIND.C";
                /// <summary>
                /// Germany Inflation Rate
                /// </summary>
                public const string InflationRate = "GRBC20YY.C";
                /// <summary>
                /// Germany Current Account
                /// </summary>
                public const string CurrentAccount = "GRCAEU.C";
                /// <summary>
                /// Germany Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GRCCI.C";
                /// <summary>
                /// Germany GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "GRGDPPGQ.C";
                /// <summary>
                /// Germany GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GRGDPPGY.C";
                /// <summary>
                /// Germany Business Confidence
                /// </summary>
                public const string BusinessConfidence = "GRIFPBUS.C";
                /// <summary>
                /// Germany Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GRIPIYOY.C";
                /// <summary>
                /// Germany Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "GRTBALE.C";
                /// <summary>
                /// Germany Exports
                /// </summary>
                public const string Exports = "GRTBEXE.C";
                /// <summary>
                /// Germany Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "GRUEPR.C";
                /// <summary>
                /// Germany Government Budget
                /// </summary>
                public const string GovernmentBudget = "WCSDDEU.C";
            }
            public static class Ghana
            {
                /// <summary>
                /// Ghana Composite Pmi
                /// </summary>
                public const string CompositePmi = "GHANACOMPMI.C";
                /// <summary>
                /// Ghana GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GHANAGDPYOY.C";
                /// <summary>
                /// Ghana Interest Rate
                /// </summary>
                public const string InterestRate = "GHANAINTRATE.C";
                /// <summary>
                /// Ghana Inflation Rate
                /// </summary>
                public const string InflationRate = "GHANAIR.C";
                /// <summary>
                /// Ghana Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GHANAPROPRICHA.C";
            }
            public static class Greece
            {
                /// <summary>
                /// Greece Calendar
                /// </summary>
                public const string Calendar = "GRC-CALENDAR.C";
                /// <summary>
                /// Greece Current Account
                /// </summary>
                public const string CurrentAccount = "GKCAEUR.C";
                /// <summary>
                /// Greece Inflation Rate
                /// </summary>
                public const string InflationRate = "GKCPNEWY.C";
                /// <summary>
                /// Greece GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "GKGNVQQ.C";
                /// <summary>
                /// Greece GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GKGNVYY.C";
                /// <summary>
                /// Greece Industrial Production
                /// </summary>
                public const string IndustrialProduction = "GKIPIYOY.C";
                /// <summary>
                /// Greece Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "GKTBALE.C";
                /// <summary>
                /// Greece Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "GRCRETAILSALESMOM.C";
                /// <summary>
                /// Greece Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "GRCRETAILSALESYOY.C";
                /// <summary>
                /// Greece 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "GREECE3MBY.C";
                /// <summary>
                /// Greece 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "GREECE6MBY.C";
                /// <summary>
                /// Greece Business Confidence
                /// </summary>
                public const string BusinessConfidence = "GREECEBC.C";
                /// <summary>
                /// Greece Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GREECECC.C";
                /// <summary>
                /// Greece Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "GREECEHARCONPRI.C";
                /// <summary>
                /// Greece Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "GREECEINFRATMOM.C";
                /// <summary>
                /// Greece Loan Growth
                /// </summary>
                public const string LoanGrowth = "GREECELOAGRO.C";
                /// <summary>
                /// Greece Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "GREECELOATOPRISEC.C";
                /// <summary>
                /// Greece Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "GREECEMANPMI.C";
                /// <summary>
                /// Greece Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "GREECEPRISECCRE.C";
                /// <summary>
                /// Greece Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "GREECEPROPRICHA.C";
                /// <summary>
                /// Greece Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTGR.C";
            }
            public static class Guatemala
            {
                /// <summary>
                /// Guatemala Current Account
                /// </summary>
                public const string CurrentAccount = "GUAMALACURCOUNT.C";
            }
            public static class Honduras
            {
                /// <summary>
                /// Honduras Interest Rate
                /// </summary>
                public const string InterestRate = "HONURASINTTRATE.C";
            }
            public static class HongKong
            {
                /// <summary>
                /// HongKong Calendar
                /// </summary>
                public const string Calendar = "HKG-CALENDAR.C";
                /// <summary>
                /// HongKong Interest Rate
                /// </summary>
                public const string InterestRate = "HKBASE.C";
                /// <summary>
                /// HongKong Current Account
                /// </summary>
                public const string CurrentAccount = "HKBPCA.C";
                /// <summary>
                /// HongKong Inflation Rate
                /// </summary>
                public const string InflationRate = "HKCPIY.C";
                /// <summary>
                /// HongKong Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "HKETBOT.C";
                /// <summary>
                /// HongKong Exports
                /// </summary>
                public const string Exports = "HKETEXP.C";
                /// <summary>
                /// HongKong Imports
                /// </summary>
                public const string Imports = "HKETIMP.C";
                /// <summary>
                /// HongKong GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "HKGDQOQ.C";
                /// <summary>
                /// HongKong GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "HKGDYOY.C";
                /// <summary>
                /// HongKong Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "HKGRETAILSALESYOY.C";
                /// <summary>
                /// HongKong Industrial Production
                /// </summary>
                public const string IndustrialProduction = "HKIPIYOY.C";
                /// <summary>
                /// HongKong Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "HKUERATE.C";
                /// <summary>
                /// HongKong Business Confidence
                /// </summary>
                public const string BusinessConfidence = "HONGKONGBUSCON.C";
                /// <summary>
                /// HongKong Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "HONGKONGFOREXCRES.C";
                /// <summary>
                /// HongKong Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "HONGKONGMANPMI.C";
            }
            public static class Hungary
            {
                /// <summary>
                /// Hungary GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ENGKHUQ.C";
                /// <summary>
                /// Hungary Interest Rate
                /// </summary>
                public const string InterestRate = "HBBRATE.C";
                /// <summary>
                /// Hungary Inflation Rate
                /// </summary>
                public const string InflationRate = "HUCPIYY.C";
                /// <summary>
                /// Hungary Current Account
                /// </summary>
                public const string CurrentAccount = "HUCQEURO.C";
                /// <summary>
                /// Hungary Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "HUEMUNR.C";
                /// <summary>
                /// Hungary GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "HUGPTOTL.C";
                /// <summary>
                /// Hungary Industrial Production
                /// </summary>
                public const string IndustrialProduction = "HUIPIYOY.C";
                /// <summary>
                /// Hungary Calendar
                /// </summary>
                public const string Calendar = "HUN-CALENDAR.C";
                /// <summary>
                /// Hungary Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "HUNCORECPIRATE.C";
                /// <summary>
                /// Hungary Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "HUNFRINRDPST.C";
                /// <summary>
                /// Hungary Construction Output
                /// </summary>
                public const string ConstructionOutput = "HUNGARYCONOUT.C";
                /// <summary>
                /// Hungary Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "HUNGARYGOVBUDVAL.C";
                /// <summary>
                /// Hungary Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "HUNGARYINFRATMOM.C";
                /// <summary>
                /// Hungary Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "HUNGARYMANPMI.C";
                /// <summary>
                /// Hungary Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "HUNGARYPROPRICHA.C";
                /// <summary>
                /// Hungary Wage Growth
                /// </summary>
                public const string WageGrowth = "HUNGARYWAGGRO.C";
                /// <summary>
                /// Hungary Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "HUNRETAILSALESMOM.C";
                /// <summary>
                /// Hungary Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "HUNRETAILSALESYOY.C";
                /// <summary>
                /// Hungary Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "HUTRBAL.C";
            }
            public static class Iceland
            {
                /// <summary>
                /// Iceland Calendar
                /// </summary>
                public const string Calendar = "CALENDARICELAND.C";
                /// <summary>
                /// Iceland Interest Rate
                /// </summary>
                public const string InterestRate = "ICBRANN.C";
                /// <summary>
                /// Iceland Current Account
                /// </summary>
                public const string CurrentAccount = "ICCACURR.C";
                /// <summary>
                /// Iceland Inflation Rate
                /// </summary>
                public const string InflationRate = "ICCPIYOY.C";
                /// <summary>
                /// Iceland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ISL-CALENDAR.C";
                /// <summary>
                /// Iceland Producer Prices
                /// </summary>
                public const string ProducerPrices = "ICELANDPROPRI.C";
                /// <summary>
                /// Iceland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ICELANDPROPRICHA.C";
                /// <summary>
                /// Iceland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ICETBAL.C";
                /// <summary>
                /// Iceland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ICGPSAQ.C";
                /// <summary>
                /// Iceland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ICGPSAY.C";
                /// <summary>
                /// Iceland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ICUNEMRT.C";
            }
            public static class India
            {
                /// <summary>
                /// India Calendar
                /// </summary>
                public const string Calendar = "IND-CALENDAR.C";
                /// <summary>
                /// India Current Account
                /// </summary>
                public const string CurrentAccount = "INBQCUR.C";
                /// <summary>
                /// India Inflation Rate
                /// </summary>
                public const string InflationRate = "INCPIINY.C";
                /// <summary>
                /// India Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "INDIACASRESRAT.C";
                /// <summary>
                /// India Construction Output
                /// </summary>
                public const string ConstructionOutput = "INDIACONOUT.C";
                /// <summary>
                /// India External Debt
                /// </summary>
                public const string ExternalDebt = "INDIAEXTDEB.C";
                /// <summary>
                /// India Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "INDIAFOREXCRES.C";
                /// <summary>
                /// India Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "INDIAGOVBUDVAL.C";
                /// <summary>
                /// India Loan Growth
                /// </summary>
                public const string LoanGrowth = "INDIALOAGRO.C";
                /// <summary>
                /// India Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "INDIAMANPMI.C";
                /// <summary>
                /// India Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "INDIAMANPRO.C";
                /// <summary>
                /// India Mni Consumer Sentiment
                /// </summary>
                public const string MniConsumerSentiment = "INDIAMNICONSEN.C";
                /// <summary>
                /// India Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "INDIAPROPRICHA.C";
                /// <summary>
                /// India Services Pmi
                /// </summary>
                public const string ServicesPmi = "INDIASERPMI.C";
                /// <summary>
                /// India GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "INGDPY.C";
                /// <summary>
                /// India Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "INMTBALD.C";
                /// <summary>
                /// India Industrial Production
                /// </summary>
                public const string IndustrialProduction = "INPIINDY.C";
                /// <summary>
                /// India Interest Rate
                /// </summary>
                public const string InterestRate = "RSPOYLD.C";
            }
            public static class Indonesia
            {
                /// <summary>
                /// Indonesia Calendar
                /// </summary>
                public const string Calendar = "IDN-CALENDAR.C";
                /// <summary>
                /// Indonesia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "IDBALTOL.C";
                /// <summary>
                /// Indonesia Interest Rate
                /// </summary>
                public const string InterestRate = "IDBIRATE.C";
                /// <summary>
                /// Indonesia Current Account
                /// </summary>
                public const string CurrentAccount = "IDCABAL.C";
                /// <summary>
                /// Indonesia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "IDCCI.C";
                /// <summary>
                /// Indonesia Inflation Rate
                /// </summary>
                public const string InflationRate = "IDCPIY.C";
                /// <summary>
                /// Indonesia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "IDEMUNEPCT.C";
                /// <summary>
                /// Indonesia Exports
                /// </summary>
                public const string Exports = "IDEXP.C";
                /// <summary>
                /// Indonesia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "IDGDPY.C";
                /// <summary>
                /// Indonesia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "IDGDQY.C";
                /// <summary>
                /// Indonesia Imports
                /// </summary>
                public const string Imports = "IDIMP.C";
                /// <summary>
                /// Indonesia Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "IDNFRINRDPST.C";
                /// <summary>
                /// Indonesia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "IDNRETAILSALESMOM.C";
                /// <summary>
                /// Indonesia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "IDNRETAILSALESYOY.C";
                /// <summary>
                /// Indonesia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "INDONESIABC.C";
                /// <summary>
                /// Indonesia Car Registrations
                /// </summary>
                public const string CarRegistrations = "INDONESIACARREG.C";
                /// <summary>
                /// Indonesia Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "INDONESIACORINFRAT.C";
                /// <summary>
                /// Indonesia Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "INDONESIAFORDIRINV.C";
                /// <summary>
                /// Indonesia Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "INDONESIAFOREXCRES.C";
                /// <summary>
                /// Indonesia Housing Index
                /// </summary>
                public const string HousingIndex = "INDONESIAHOUIND.C";
                /// <summary>
                /// Indonesia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "INDONESIAINFRATMOM.C";
                /// <summary>
                /// Indonesia Lending Rate
                /// </summary>
                public const string LendingRate = "INDONESIALENRAT.C";
                /// <summary>
                /// Indonesia Loan Growth
                /// </summary>
                public const string LoanGrowth = "INDONESIALOAGRO.C";
                /// <summary>
                /// Indonesia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "INDONESIAMANPMI.C";
                /// <summary>
                /// Indonesia Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "INDONESIAMONSUPM2.C";
                /// <summary>
                /// Indonesia Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "INDONESIATOUARR.C";
            }
            public static class Iran
            {
                /// <summary>
                /// Iran Inflation Rate
                /// </summary>
                public const string InflationRate = "IRAIRANINFNRATE.C";
            }
            public static class Iraq
            {
                /// <summary>
                /// Iraq Inflation Rate
                /// </summary>
                public const string InflationRate = "IRAIRAQINFNRATE.C";
            }
            public static class Ireland
            {
                /// <summary>
                /// Ireland Calendar
                /// </summary>
                public const string Calendar = "IRL-CALENDAR.C";
                /// <summary>
                /// Ireland Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "EUCCIE.C";
                /// <summary>
                /// Ireland Current Account
                /// </summary>
                public const string CurrentAccount = "IECA.C";
                /// <summary>
                /// Ireland Inflation Rate
                /// </summary>
                public const string InflationRate = "IECPIYOY.C";
                /// <summary>
                /// Ireland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "IEGRPQOQ.C";
                /// <summary>
                /// Ireland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "IEGRPYOY.C";
                /// <summary>
                /// Ireland Industrial Production
                /// </summary>
                public const string IndustrialProduction = "IEIPIYOY.C";
                /// <summary>
                /// Ireland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "IETBALNS.C";
                /// <summary>
                /// Ireland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "IEUERT.C";
                /// <summary>
                /// Ireland Construction Output
                /// </summary>
                public const string ConstructionOutput = "IRELANDCONOUT.C";
                /// <summary>
                /// Ireland Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "IRELANDCONPMI.C";
                /// <summary>
                /// Ireland Gross National Product
                /// </summary>
                public const string GrossNationalProduct = "IRELANDGRONATPRO.C";
                /// <summary>
                /// Ireland Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "IRELANDHARCONPRI.C";
                /// <summary>
                /// Ireland Housing Index
                /// </summary>
                public const string HousingIndex = "IRELANDHOUIND.C";
                /// <summary>
                /// Ireland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "IRELANDINFRATMOM.C";
                /// <summary>
                /// Ireland Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "IRELANDMANPMI.C";
                /// <summary>
                /// Ireland Personal Savings
                /// </summary>
                public const string PersonalSavings = "IRELANDPERSAV.C";
                /// <summary>
                /// Ireland Producer Prices
                /// </summary>
                public const string ProducerPrices = "IRELANDPROPRI.C";
                /// <summary>
                /// Ireland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "IRELANDPROPRICHA.C";
                /// <summary>
                /// Ireland Services Pmi
                /// </summary>
                public const string ServicesPmi = "IRELANDSERPMI.C";
                /// <summary>
                /// Ireland Wage Growth
                /// </summary>
                public const string WageGrowth = "IRELANDWAGGRO.C";
                /// <summary>
                /// Ireland Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "IRLCORECPIRATE.C";
                /// <summary>
                /// Ireland Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "IRLRETAILSALESMOM.C";
                /// <summary>
                /// Ireland Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "IRLRETAILSALESYOY.C";
            }
            public static class Israel
            {
                /// <summary>
                /// Israel Interest Rate
                /// </summary>
                public const string InterestRate = "ISBRATE.C";
                /// <summary>
                /// Israel Current Account
                /// </summary>
                public const string CurrentAccount = "ISCAL.C";
                /// <summary>
                /// Israel Inflation Rate
                /// </summary>
                public const string InflationRate = "ISCPIYYN.C";
                /// <summary>
                /// Israel GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ISGDPNQQ.C";
                /// <summary>
                /// Israel GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ISGDPNYY.C";
                /// <summary>
                /// Israel Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ISINIYOY.C";
                /// <summary>
                /// Israel Calendar
                /// </summary>
                public const string Calendar = "ISR-CALENDAR.C";
                /// <summary>
                /// Israel Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ISRAELBC.C";
                /// <summary>
                /// Israel Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ISRAELCC.C";
                /// <summary>
                /// Israel Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "ISRAELFOREXCRES.C";
                /// <summary>
                /// Israel GDP Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "ISRAELGDPGROANN.C";
                /// <summary>
                /// Israel Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "ISRAELGOVBUDVAL.C";
                /// <summary>
                /// Israel Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ISRAELINDPROMOM.C";
                /// <summary>
                /// Israel Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "ISRAELINFEXP.C";
                /// <summary>
                /// Israel Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ISRAELINFRATMOM.C";
                /// <summary>
                /// Israel Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "ISRAELLEAECOIND.C";
                /// <summary>
                /// Israel Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "ISRAELMANPMI.C";
                /// <summary>
                /// Israel Money Supply M1
                /// </summary>
                public const string MoneySupplyM1 = "ISRAELMONSUPM1.C";
                /// <summary>
                /// Israel Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "ISRAELTOUARR.C";
                /// <summary>
                /// Israel Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ISTBAL.C";
                /// <summary>
                /// Israel Exports
                /// </summary>
                public const string Exports = "ISTBEX.C";
                /// <summary>
                /// Israel Imports
                /// </summary>
                public const string Imports = "ISTBIM.C";
                /// <summary>
                /// Israel Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ISUER.C";
            }
            public static class Italy
            {
                /// <summary>
                /// Italy Calendar
                /// </summary>
                public const string Calendar = "ITAINDICATORS.C";
                /// <summary>
                /// Italy Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GBTPGR10.C";
                /// <summary>
                /// Italy 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "ITALY3YBY.C";
                /// <summary>
                /// Italy 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "ITALY3YNY.C";
                /// <summary>
                /// Italy 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "ITALY5WBY.C";
                /// <summary>
                /// Italy 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "ITALY5YNY.C";
                /// <summary>
                /// Italy 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "ITALY6MBY.C";
                /// <summary>
                /// Italy 7 Year Note Yield
                /// </summary>
                public const string SevenYearNoteYield = "ITALY7YNY.C";
                /// <summary>
                /// Italy Construction Output
                /// </summary>
                public const string ConstructionOutput = "ITALYCONOUT.C";
                /// <summary>
                /// Italy Factory Orders
                /// </summary>
                public const string FactoryOrders = "ITALYFACORD.C";
                /// <summary>
                /// Italy Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "ITALYHARCONPRI.C";
                /// <summary>
                /// Italy Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ITALYINDPROMOM.C";
                /// <summary>
                /// Italy Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "ITALYINFRATMOM.C";
                /// <summary>
                /// Italy Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "ITALYMANPMI.C";
                /// <summary>
                /// Italy New Orders
                /// </summary>
                public const string NewOrders = "ITALYNEWORD.C";
                /// <summary>
                /// Italy Producer Prices
                /// </summary>
                public const string ProducerPrices = "ITALYPROPRI.C";
                /// <summary>
                /// Italy Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ITALYPROPRICHA.C";
                /// <summary>
                /// Italy Services Pmi
                /// </summary>
                public const string ServicesPmi = "ITALYSERPMI.C";
                /// <summary>
                /// Italy Wage Growth
                /// </summary>
                public const string WageGrowth = "ITALYWAGGRO.C";
                /// <summary>
                /// Italy Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ITARETAILSALESMOM.C";
                /// <summary>
                /// Italy Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ITARETAILSALESYOY.C";
                /// <summary>
                /// Italy Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ITBCI.C";
                /// <summary>
                /// Italy Current Account
                /// </summary>
                public const string CurrentAccount = "ITCAEUR.C";
                /// <summary>
                /// Italy Inflation Rate
                /// </summary>
                public const string InflationRate = "ITCPNICY.C";
                /// <summary>
                /// Italy GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ITPIRLQS.C";
                /// <summary>
                /// Italy GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ITPIRLYS.C";
                /// <summary>
                /// Italy Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ITPRWAY.C";
                /// <summary>
                /// Italy Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ITPSSA.C";
                /// <summary>
                /// Italy Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ITTRBSAT.C";
                /// <summary>
                /// Italy Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTIT.C";
                /// <summary>
                /// Italy Government Budget
                /// </summary>
                public const string GovernmentBudget = "WCSDITA.C";
            }
            public static class IvoryCoast
            {
                /// <summary>
                /// IvoryCoast Industrial Production
                /// </summary>
                public const string IndustrialProduction = "COTREINDCTION.C";
                /// <summary>
                /// IvoryCoast Inflation Rate
                /// </summary>
                public const string InflationRate = "COTREINFNRATE.C";
            }
            public static class Jamaica
            {
                /// <summary>
                /// Jamaica Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "JAMAICABALRADE.C";
                /// <summary>
                /// Jamaica Current Account
                /// </summary>
                public const string CurrentAccount = "JAMAICACURCOUNT.C";
                /// <summary>
                /// Jamaica GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "JAMAICAGDPATE.C";
                /// <summary>
                /// Jamaica GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "JAMAICAGDPRATE.C";
                /// <summary>
                /// Jamaica Inflation Rate
                /// </summary>
                public const string InflationRate = "JAMAICAINFNRATE.C";
                /// <summary>
                /// Jamaica Interest Rate
                /// </summary>
                public const string InterestRate = "JAMAICAINTTRATE.C";
            }
            public static class Japan
            {
                /// <summary>
                /// Japan Interest Rate
                /// </summary>
                public const string InterestRate = "BOJDTR.C";
                /// <summary>
                /// Japan Calendar
                /// </summary>
                public const string Calendar = "JPY-CALENDAR.C";
                /// <summary>
                /// Japan Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GJGB10.C";
                /// <summary>
                /// Japan 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "JAPAN3YBY.C";
                /// <summary>
                /// Japan Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "JAPANCAPUTI.C";
                /// <summary>
                /// Japan Coincident Index
                /// </summary>
                public const string CoincidentIndex = "JAPANCOIIND.C";
                /// <summary>
                /// Japan Construction Orders
                /// </summary>
                public const string ConstructionOrders = "JAPANCONORD.C";
                /// <summary>
                /// Japan Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "JAPANCONSPE.C";
                /// <summary>
                /// Japan Economy Watchers Survey
                /// </summary>
                public const string EconomyWatchersSurvey = "JAPANECOWATSUR.C";
                /// <summary>
                /// Japan Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "JAPANFORBONINV.C";
                /// <summary>
                /// Japan Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "JAPANFOREXCRES.C";
                /// <summary>
                /// Japan Foreign Stock Investment
                /// </summary>
                public const string ForeignStockInvestment = "JAPANFORSTOINV.C";
                /// <summary>
                /// Japan GDP Deflator
                /// </summary>
                public const string GdpDeflator = "JAPANGDPDEF.C";
                /// <summary>
                /// Japan Gdp Growth Annualized
                /// </summary>
                public const string GdpGrowthAnnualized = "JAPANGDPGROANN.C";
                /// <summary>
                /// Japan Gross Fixed Capital Formation
                /// </summary>
                public const string GrossFixedCapitalFormation = "JAPANGROFIXCAPFOR.C";
                /// <summary>
                /// Japan Household Spending
                /// </summary>
                public const string HouseholdSpending = "JAPANHOUSPE.C";
                /// <summary>
                /// Japan Housing Starts
                /// </summary>
                public const string HousingStarts = "JAPANHOUSTA.C";
                /// <summary>
                /// Japan Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "JAPANINDPROMOM.C";
                /// <summary>
                /// Japan Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "JAPANINFRATMOM.C";
                /// <summary>
                /// Japan Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "JAPANLEACOMIND.C";
                /// <summary>
                /// Japan Loan Growth
                /// </summary>
                public const string LoanGrowth = "JAPANLOAGRO.C";
                /// <summary>
                /// Japan Machinery Orders
                /// </summary>
                public const string MachineryOrders = "JAPANMACORD.C";
                /// <summary>
                /// Japan Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "JAPANMANPMI.C";
                /// <summary>
                /// Japan Non Manufacturing Pmi
                /// </summary>
                public const string NonManufacturingPmi = "JAPANNONMANPMI.C";
                /// <summary>
                /// Japan Private Investment
                /// </summary>
                public const string PrivateInvestment = "JAPANPRIINV.C";
                /// <summary>
                /// Japan Producer Prices
                /// </summary>
                public const string ProducerPrices = "JAPANPROPRI.C";
                /// <summary>
                /// Japan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "JAPANPROPRICHA.C";
                /// <summary>
                /// Japan Services Pmi
                /// </summary>
                public const string ServicesPmi = "JAPANSERPMI.C";
                /// <summary>
                /// Japan Small Business Sentiment
                /// </summary>
                public const string SmallBusinessSentiment = "JAPANSMABUSSEN.C";
                /// <summary>
                /// Japan Wage Growth
                /// </summary>
                public const string WageGrowth = "JAPANWAGGRO.C";
                /// <summary>
                /// Japan Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "JCOMACF.C";
                /// <summary>
                /// Japan GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "JGDPAGDP.C";
                /// <summary>
                /// Japan GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "JGDPNSAQ.C";
                /// <summary>
                /// Japan Current Account
                /// </summary>
                public const string CurrentAccount = "JNBPAB.C";
                /// <summary>
                /// Japan Exports
                /// </summary>
                public const string Exports = "JNBPEXP.C";
                /// <summary>
                /// Japan Imports
                /// </summary>
                public const string Imports = "JNBPIMP.C";
                /// <summary>
                /// Japan Inflation Rate
                /// </summary>
                public const string InflationRate = "JNCPIYOY.C";
                /// <summary>
                /// Japan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "JNIPYOY.C";
                /// <summary>
                /// Japan Business Confidence
                /// </summary>
                public const string BusinessConfidence = "JNSBALLI.C";
                /// <summary>
                /// Japan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "JNTBAL.C";
                /// <summary>
                /// Japan Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "JNUE.C";
                /// <summary>
                /// Japan Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "JPNCORECPIRATE.C";
                /// <summary>
                /// Japan Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "JPNRETAILSALESMOM.C";
                /// <summary>
                /// Japan Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "JPNRETAILSALESYOY.C";
            }
            public static class Jordan
            {
                /// <summary>
                /// Jordan Building Permits
                /// </summary>
                public const string BuildingPermits = "JORDANBUIPER.C";
                /// <summary>
                /// Jordan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "JORDANPROPRICHA.C";
                /// <summary>
                /// Jordan Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "JORDANTOUARR.C";
                /// <summary>
                /// Jordan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "JORRDANBALRADE.C";
                /// <summary>
                /// Jordan Current Account
                /// </summary>
                public const string CurrentAccount = "JORRDANCURCOUNT.C";
                /// <summary>
                /// Jordan GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "JORRDANGDPATE.C";
                /// <summary>
                /// Jordan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "JORRDANINDCTION.C";
                /// <summary>
                /// Jordan Inflation Rate
                /// </summary>
                public const string InflationRate = "JORRDANINFNRATE.C";
                /// <summary>
                /// Jordan Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "JORRDANUNETRATE.C";
            }
            public static class Kazakhstan
            {
                /// <summary>
                /// Kazakhstan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KAZAKHSTANBT.C";
                /// <summary>
                /// Kazakhstan Business Confidence
                /// </summary>
                public const string BusinessConfidence = "KAZAKHSTANBUSCON.C";
                /// <summary>
                /// Kazakhstan Current Account
                /// </summary>
                public const string CurrentAccount = "KAZAKHSTANCA.C";
                /// <summary>
                /// Kazakhstan GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "KAZAKHSTANGDPYOY.C";
                /// <summary>
                /// Kazakhstan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "KAZAKHSTANINDPRO.C";
                /// <summary>
                /// Kazakhstan Interest Rate
                /// </summary>
                public const string InterestRate = "KAZAKHSTANINTRATE.C";
                /// <summary>
                /// Kazakhstan Inflation Rate
                /// </summary>
                public const string InflationRate = "KAZAKHSTANIR.C";
                /// <summary>
                /// Kazakhstan Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "KAZAKHSTANLEAECOIND.C";
                /// <summary>
                /// Kazakhstan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "KAZAKHSTANPROPRICHA.C";
                /// <summary>
                /// Kazakhstan Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "KAZAKHSTANRETSALYOY.C";
                /// <summary>
                /// Kazakhstan Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "KAZAKHSTANUR.C";
            }
            public static class Kenya
            {
                /// <summary>
                /// Kenya Calendar
                /// </summary>
                public const string Calendar = "KEN-CALENDAR.C";
                /// <summary>
                /// Kenya Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KENYABT.C";
                /// <summary>
                /// Kenya Exports
                /// </summary>
                public const string Exports = "KENYAEX.C";
                /// <summary>
                /// Kenya GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "KENYAGDPQOQ.C";
                /// <summary>
                /// Kenya GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "KENYAGDPYOY.C";
                /// <summary>
                /// Kenya Imports
                /// </summary>
                public const string Imports = "KENYAIM.C";
                /// <summary>
                /// Kenya Interest Rate
                /// </summary>
                public const string InterestRate = "KENYAINTRATE.C";
                /// <summary>
                /// Kenya Inflation Rate
                /// </summary>
                public const string InflationRate = "KENYAIR.C";
                /// <summary>
                /// Kenya Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "KENYAMANPMI.C";
                /// <summary>
                /// Kenya Mni Business Sentiment
                /// </summary>
                public const string MniBusinessSentiment = "KENYAMNIBUSSEN.C";
                /// <summary>
                /// Kenya Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "KENYAPROPRICHA.C";
            }
            public static class Kosovo
            {
                /// <summary>
                /// Kosovo Producer Prices
                /// </summary>
                public const string ProducerPrices = "KOSOVOPROPRI.C";
                /// <summary>
                /// Kosovo Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "KOSOVOPROPRICHA.C";
                /// <summary>
                /// Kosovo Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KOSSOVOBALRADE.C";
                /// <summary>
                /// Kosovo GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "KOSSOVOGDPATE.C";
                /// <summary>
                /// Kosovo Inflation Rate
                /// </summary>
                public const string InflationRate = "KOSSOVOINFNRATE.C";
                /// <summary>
                /// Kosovo Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "KOSSOVOUNETRATE.C";
            }
            public static class Kuwait
            {
                /// <summary>
                /// Kuwait Loan Growth
                /// </summary>
                public const string LoanGrowth = "KUWAITLOAGRO.C";
                /// <summary>
                /// Kuwait Loans To Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "KUWAITLTPS.C";
                /// <summary>
                /// Kuwait Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "KUWAITMONSUPM2.C";
                /// <summary>
                /// Kuwait Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KUWWAITBALRADE.C";
                /// <summary>
                /// Kuwait Inflation Rate
                /// </summary>
                public const string InflationRate = "KUWWAITINFNRATE.C";
                /// <summary>
                /// Kuwait Interest Rate
                /// </summary>
                public const string InterestRate = "KUWWAITINTTRATE.C";
            }
            public static class Kyrgyzstan
            {
                /// <summary>
                /// Kyrgyzstan Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "KYRGYZSTANLEAECOIND.C";
                /// <summary>
                /// Kyrgyzstan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "KYRGYZSTANPROPRICHA.C";
                /// <summary>
                /// Kyrgyzstan Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "KYRGYZSTANRETSALYOY.C";
                /// <summary>
                /// Kyrgyzstan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KYRSTANBALRADE.C";
                /// <summary>
                /// Kyrgyzstan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "KYRSTANINDCTION.C";
                /// <summary>
                /// Kyrgyzstan Inflation Rate
                /// </summary>
                public const string InflationRate = "KYRSTANINFNRATE.C";
                /// <summary>
                /// Kyrgyzstan Interest Rate
                /// </summary>
                public const string InterestRate = "KYRSTANINTTRATE.C";
            }
            public static class Latvia
            {
                /// <summary>
                /// Latvia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LATVIABT.C";
                /// <summary>
                /// Latvia Current Account
                /// </summary>
                public const string CurrentAccount = "LATVIACA.C";
                /// <summary>
                /// Latvia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "LATVIAGDPQOQ.C";
                /// <summary>
                /// Latvia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "LATVIAGDPYOY.C";
                /// <summary>
                /// Latvia Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "LATVIAINDPROMOM.C";
                /// <summary>
                /// Latvia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "LATVIAINFRATMOM.C";
                /// <summary>
                /// Latvia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LATVIAIP.C";
                /// <summary>
                /// Latvia Inflation Rate
                /// </summary>
                public const string InflationRate = "LATVIAIR.C";
                /// <summary>
                /// Latvia Producer Prices
                /// </summary>
                public const string ProducerPrices = "LATVIAPROPRI.C";
                /// <summary>
                /// Latvia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LATVIAPROPRICHA.C";
                /// <summary>
                /// Latvia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LATVIAUR.C";
                /// <summary>
                /// Latvia Calendar
                /// </summary>
                public const string Calendar = "LVA-CALENDAR.C";
                /// <summary>
                /// Latvia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "LVARETAILSALESMOM.C";
                /// <summary>
                /// Latvia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "LVARETAILSALESYOY.C";
            }
            public static class Lebanon
            {
                /// <summary>
                /// Lebanon Inflation Rate
                /// </summary>
                public const string InflationRate = "LEBANONINFNRATE.C";
                /// <summary>
                /// Lebanon Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "LEBANONMANPMI.C";
            }
            public static class Liechtenstein
            {
                /// <summary>
                /// Liechtenstein Inflation Rate
                /// </summary>
                public const string InflationRate = "LIETEININFNRATE.C";
            }
            public static class Lithuania
            {
                /// <summary>
                /// Lithuania Business Confidence
                /// </summary>
                public const string BusinessConfidence = "LITHUANIABC.C";
                /// <summary>
                /// Lithuania Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LITHUANIABT.C";
                /// <summary>
                /// Lithuania Current Account
                /// </summary>
                public const string CurrentAccount = "LITHUANIACA.C";
                /// <summary>
                /// Lithuania Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "LITHUANIACC.C";
                /// <summary>
                /// Lithuania GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "LITHUANIAGDPQOQ.C";
                /// <summary>
                /// Lithuania GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "LITHUANIAGDPYOY.C";
                /// <summary>
                /// Lithuania Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LITHUANIAINDPRO.C";
                /// <summary>
                /// Lithuania Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "LITHUANIAINDPROMOM.C";
                /// <summary>
                /// Lithuania Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "LITHUANIAINFRATMOM.C";
                /// <summary>
                /// Lithuania Inflation Rate
                /// </summary>
                public const string InflationRate = "LITHUANIAIR.C";
                /// <summary>
                /// Lithuania Producer Prices
                /// </summary>
                public const string ProducerPrices = "LITHUANIAPROPRI.C";
                /// <summary>
                /// Lithuania Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LITHUANIAPROPRICHA.C";
                /// <summary>
                /// Lithuania Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "LITHUANIAUR.C";
                /// <summary>
                /// Lithuania Calendar
                /// </summary>
                public const string Calendar = "LTU-CALENDAR.C";
                /// <summary>
                /// Lithuania Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "LTURETAILSALESMOM.C";
                /// <summary>
                /// Lithuania Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "LTURETAILSALESYOY.C";
            }
            public static class Luxembourg
            {
                /// <summary>
                /// Luxembourg GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ENGKLUQ.C";
                /// <summary>
                /// Luxembourg GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ENGKLUY.C";
                /// <summary>
                /// Luxembourg Business Confidence
                /// </summary>
                public const string BusinessConfidence = "LUXEMBOURGBUSCON.C";
                /// <summary>
                /// Luxembourg Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "LUXEMBOURGCC.C";
                /// <summary>
                /// Luxembourg Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "LUXEMBOURGPROPRICHA.C";
                /// <summary>
                /// Luxembourg Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "LUXEMBOURGRETSALMOM.C";
                /// <summary>
                /// Luxembourg Current Account
                /// </summary>
                public const string CurrentAccount = "LXCAEU.C";
                /// <summary>
                /// Luxembourg Inflation Rate
                /// </summary>
                public const string InflationRate = "LXCPIYOY.C";
                /// <summary>
                /// Luxembourg Industrial Production
                /// </summary>
                public const string IndustrialProduction = "LXIPRYOY.C";
                /// <summary>
                /// Luxembourg Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "LXTBBALL.C";
                /// <summary>
                /// Luxembourg Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "OELUU003.C";
            }
            public static class Macau
            {
                /// <summary>
                /// Macau Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MACACAOBALRADE.C";
                /// <summary>
                /// Macau GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MACACAOGDPATE.C";
                /// <summary>
                /// Macau Inflation Rate
                /// </summary>
                public const string InflationRate = "MACACAOINFNRATE.C";
                /// <summary>
                /// Macau Interest Rate
                /// </summary>
                public const string InterestRate = "MACACAOINTTRATE.C";
                /// <summary>
                /// Macau Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "MACACAORETSMOM.C";
                /// <summary>
                /// Macau Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "MACACAORETSYOY.C";
                /// <summary>
                /// Macau Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MACACAOUNETRATE.C";
                /// <summary>
                /// Macau Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "MACAOTOUARR.C";
            }
            public static class Macedonia
            {
                /// <summary>
                /// Macedonia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MACEDONIABT.C";
                /// <summary>
                /// Macedonia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MACEDONIABUSCON.C";
                /// <summary>
                /// Macedonia Current Account
                /// </summary>
                public const string CurrentAccount = "MACEDONIACA.C";
                /// <summary>
                /// Macedonia Exports
                /// </summary>
                public const string Exports = "MACEDONIAEX.C";
                /// <summary>
                /// Macedonia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MACEDONIAGDPYOY.C";
                /// <summary>
                /// Macedonia Imports
                /// </summary>
                public const string Imports = "MACEDONIAIM.C";
                /// <summary>
                /// Macedonia Interest Rate
                /// </summary>
                public const string InterestRate = "MACEDONIAINTRATE.C";
                /// <summary>
                /// Macedonia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MACEDONIAIP.C";
                /// <summary>
                /// Macedonia Inflation Rate
                /// </summary>
                public const string InflationRate = "MACEDONIAIR.C";
                /// <summary>
                /// Macedonia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MACEDONIAPROPRICHA.C";
                /// <summary>
                /// Macedonia Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "MACEDONIARETSALYOY.C";
                /// <summary>
                /// Macedonia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MACEDONIAUR.C";
            }
            public static class Malawi
            {
                /// <summary>
                /// Malawi Inflation Rate
                /// </summary>
                public const string InflationRate = "MALLAWIINFNRATE.C";
                /// <summary>
                /// Malawi Interest Rate
                /// </summary>
                public const string InterestRate = "MALLAWIINTTRATE.C";
            }
            public static class Malaysia
            {
                /// <summary>
                /// Malaysia Current Account
                /// </summary>
                public const string CurrentAccount = "MACATOT.C";
                /// <summary>
                /// Malaysia Inflation Rate
                /// </summary>
                public const string InflationRate = "MACPIYOY.C";
                /// <summary>
                /// Malaysia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MAEPRATE.C";
                /// <summary>
                /// Malaysia Exports
                /// </summary>
                public const string Exports = "MAETEXP.C";
                /// <summary>
                /// Malaysia Imports
                /// </summary>
                public const string Imports = "MAETIMP.C";
                /// <summary>
                /// Malaysia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MAETTRBL.C";
                /// <summary>
                /// Malaysia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MAGDHIY.C";
                /// <summary>
                /// Malaysia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MAGDPQPCT.C";
                /// <summary>
                /// Malaysia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MAIPINDY.C";
                /// <summary>
                /// Malaysia Coincident Index
                /// </summary>
                public const string CoincidentIndex = "MALAYSIACOIIND.C";
                /// <summary>
                /// Malaysia Construction Output
                /// </summary>
                public const string ConstructionOutput = "MALAYSIACONOUT.C";
                /// <summary>
                /// Malaysia Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "MALAYSIAFOREXCRES.C";
                /// <summary>
                /// Malaysia Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "MALAYSIAINDPROMOM.C";
                /// <summary>
                /// Malaysia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "MALAYSIALEAECOIND.C";
                /// <summary>
                /// Malaysia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "MALAYSIAMANPMI.C";
                /// <summary>
                /// Malaysia Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "MALAYSIAMONSUPM3.C";
                /// <summary>
                /// Malaysia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MALAYSIAPROPRICHA.C";
                /// <summary>
                /// Malaysia Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "MALAYSIARETSALYOY.C";
                /// <summary>
                /// Malaysia Interest Rate
                /// </summary>
                public const string InterestRate = "MAOPRATE.C";
            }
            public static class Malta
            {
                /// <summary>
                /// Malta Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MALALTABALRADE.C";
                /// <summary>
                /// Malta GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MALALTAGDPATE.C";
                /// <summary>
                /// Malta Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MALALTAINDCTION.C";
                /// <summary>
                /// Malta Inflation Rate
                /// </summary>
                public const string InflationRate = "MALALTAINFNRATE.C";
                /// <summary>
                /// Malta Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "MALALTARETSMOM.C";
                /// <summary>
                /// Malta Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "MALALTARETSYOY.C";
                /// <summary>
                /// Malta Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MALALTAUNETRATE.C";
                /// <summary>
                /// Malta Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MALTABUSCON.C";
                /// <summary>
                /// Malta Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "MALTACONCON.C";
                /// <summary>
                /// Malta Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MALTAPROPRICHA.C";
            }
            public static class Mauritius
            {
                /// <summary>
                /// Mauritius Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "MAURITIUSTOUARR.C";
                /// <summary>
                /// Mauritius Tourism Revenues
                /// </summary>
                public const string TourismRevenues = "MAURITIUSTOUREV.C";
                /// <summary>
                /// Mauritius Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MAUTIUSBALRADE.C";
                /// <summary>
                /// Mauritius GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MAUTIUSGDPATE.C";
                /// <summary>
                /// Mauritius Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MAUTIUSINDCTION.C";
                /// <summary>
                /// Mauritius Inflation Rate
                /// </summary>
                public const string InflationRate = "MAUTIUSINFNRATE.C";
                /// <summary>
                /// Mauritius Interest Rate
                /// </summary>
                public const string InterestRate = "MAUTIUSINTTRATE.C";
                /// <summary>
                /// Mauritius Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MAUTIUSUNETRATE.C";
            }
            public static class Mexico
            {
                /// <summary>
                /// Mexico Calendar
                /// </summary>
                public const string Calendar = "MEX-CALENDAR.C";
                /// <summary>
                /// Mexico Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MEXICOBC.C";
                /// <summary>
                /// Mexico Car Production
                /// </summary>
                public const string CarProduction = "MEXICOCARPRO.C";
                /// <summary>
                /// Mexico Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "MEXICOCONSPE.C";
                /// <summary>
                /// Mexico Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "MEXICOCORCONPRI.C";
                /// <summary>
                /// Mexico Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "MEXICOFOREXCRES.C";
                /// <summary>
                /// Mexico Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "MEXICOGOVBUDVAL.C";
                /// <summary>
                /// Mexico Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "MEXICOINDPROMOM.C";
                /// <summary>
                /// Mexico Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "MEXICOINFRATMOM.C";
                /// <summary>
                /// Mexico Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "MEXICOLEAECOIND.C";
                /// <summary>
                /// Mexico Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "MEXICOMANPMI.C";
                /// <summary>
                /// Mexico Private Investment
                /// </summary>
                public const string PrivateInvestment = "MEXICOPRIINV.C";
                /// <summary>
                /// Mexico Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "MEXRETAILSALESMOM.C";
                /// <summary>
                /// Mexico Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "MEXRETAILSALESYOY.C";
                /// <summary>
                /// Mexico Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "MXCFCONF.C";
                /// <summary>
                /// Mexico Inflation Rate
                /// </summary>
                public const string InflationRate = "MXCPYOY.C";
                /// <summary>
                /// Mexico GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MXGCTOT.C";
                /// <summary>
                /// Mexico GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MXGPQTR.C";
                /// <summary>
                /// Mexico Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MXIPTYOY.C";
                /// <summary>
                /// Mexico Interest Rate
                /// </summary>
                public const string InterestRate = "MXONBR.C";
                /// <summary>
                /// Mexico Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MXTBBAL.C";
                /// <summary>
                /// Mexico Exports
                /// </summary>
                public const string Exports = "MXTBBEXP.C";
                /// <summary>
                /// Mexico Imports
                /// </summary>
                public const string Imports = "MXTBBIMP.C";
                /// <summary>
                /// Mexico Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MXUEUNSA.C";
                /// <summary>
                /// Mexico Current Account
                /// </summary>
                public const string CurrentAccount = "OEMXB039.C";
            }
            public static class Moldova
            {
                /// <summary>
                /// Moldova Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MOLDOVABALRADE.C";
                /// <summary>
                /// Moldova Current Account
                /// </summary>
                public const string CurrentAccount = "MOLDOVACURCOUNT.C";
                /// <summary>
                /// Moldova GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MOLDOVAGDPANNGRORAT.C";
                /// <summary>
                /// Moldova Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MOLDOVAINDCTION.C";
                /// <summary>
                /// Moldova Inflation Rate
                /// </summary>
                public const string InflationRate = "MOLDOVAINFNRATE.C";
                /// <summary>
                /// Moldova Interest Rate
                /// </summary>
                public const string InterestRate = "MOLDOVAINTTRATE.C";
                /// <summary>
                /// Moldova Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MOLDOVAPROPRICHA.C";
                /// <summary>
                /// Moldova Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MOLDOVAUNETRATE.C";
            }
            public static class Mongolia
            {
                /// <summary>
                /// Mongolia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MONGOLIABT.C";
                /// <summary>
                /// Mongolia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MONGOLIAGDPYOY.C";
                /// <summary>
                /// Mongolia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MONGOLIAINDPRO.C";
                /// <summary>
                /// Mongolia Inflation Rate
                /// </summary>
                public const string InflationRate = "MONGOLIAIR.C";
                /// <summary>
                /// Mongolia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MONGOLIAUR.C";
            }
            public static class Montenegro
            {
                /// <summary>
                /// Montenegro Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "MONEGROBALRADE.C";
                /// <summary>
                /// Montenegro Current Account
                /// </summary>
                public const string CurrentAccount = "MONEGROCURCOUNT.C";
                /// <summary>
                /// Montenegro GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MONEGROGDPATE.C";
                /// <summary>
                /// Montenegro Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MONEGROINDCTION.C";
                /// <summary>
                /// Montenegro Inflation Rate
                /// </summary>
                public const string InflationRate = "MONEGROINFNRATE.C";
                /// <summary>
                /// Montenegro Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "MONTENEGROHARCONPRI.C";
                /// <summary>
                /// Montenegro Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "MONTENEGROPROPRICHA.C";
                /// <summary>
                /// Montenegro Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "MONTENEGRORETSALMOM.C";
                /// <summary>
                /// Montenegro Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "MONTENEGRORETSALYOY.C";
                /// <summary>
                /// Montenegro Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "MONTENEGROTOUARR.C";
            }
            public static class Morocco
            {
                /// <summary>
                /// Morocco Current Account
                /// </summary>
                public const string CurrentAccount = "MOROCCOCA.C";
                /// <summary>
                /// Morocco GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MOROCCOGDPYOY.C";
                /// <summary>
                /// Morocco Industrial Production
                /// </summary>
                public const string IndustrialProduction = "MOROCCOINDPRO.C";
                /// <summary>
                /// Morocco Interest Rate
                /// </summary>
                public const string InterestRate = "MOROCCOINTRATE.C";
                /// <summary>
                /// Morocco Inflation Rate
                /// </summary>
                public const string InflationRate = "MOROCCOIR.C";
                /// <summary>
                /// Morocco Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "MOROCCOMONSUPM2.C";
                /// <summary>
                /// Morocco Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "MOROCCOUR.C";
            }
            public static class Mozambique
            {
                /// <summary>
                /// Mozambique Business Confidence
                /// </summary>
                public const string BusinessConfidence = "MOZAMBIQUEBUSCON.C";
                /// <summary>
                /// Mozambique Composite Pmi
                /// </summary>
                public const string CompositePmi = "MOZAMBIQUECOMPMI.C";
                /// <summary>
                /// Mozambique GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "MOZAMBIQUEGDPQOQ.C";
                /// <summary>
                /// Mozambique GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "MOZAMBIQUEGDPYOY.C";
                /// <summary>
                /// Mozambique Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "MOZAMBIQUEINFRATMOM.C";
                /// <summary>
                /// Mozambique Interest Rate
                /// </summary>
                public const string InterestRate = "MOZAMBIQUEINTRATE.C";
                /// <summary>
                /// Mozambique Inflation Rate
                /// </summary>
                public const string InflationRate = "MOZAMBIQUEIR.C";
            }
            public static class Myanmar
            {
                /// <summary>
                /// Myanmar Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "MYANMARMANPMI.C";
            }
            public static class Namibia
            {
                /// <summary>
                /// Namibia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NAMIBIAGDPQOQ.C";
                /// <summary>
                /// Namibia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NAMIBIAGDPYOY.C";
                /// <summary>
                /// Namibia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "NAMIBIAINFRATMOM.C";
                /// <summary>
                /// Namibia Interest Rate
                /// </summary>
                public const string InterestRate = "NAMIBIAINTRATE.C";
                /// <summary>
                /// Namibia Inflation Rate
                /// </summary>
                public const string InflationRate = "NAMIBIAIR.C";
            }
            public static class Netherlands
            {
                /// <summary>
                /// Netherlands Calendar
                /// </summary>
                public const string Calendar = "CALENDARNETHERLANDS.C";
                /// <summary>
                /// Netherlands Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GNTH10YR.C";
                /// <summary>
                /// Netherlands Current Account
                /// </summary>
                public const string CurrentAccount = "NECATOTE.C";
                /// <summary>
                /// Netherlands Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NECCI.C";
                /// <summary>
                /// Netherlands Inflation Rate
                /// </summary>
                public const string InflationRate = "NECPIYOY.C";
                /// <summary>
                /// Netherlands GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NEGDPEQ.C";
                /// <summary>
                /// Netherlands GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NEGDPEY.C";
                /// <summary>
                /// Netherlands Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NEIP20YY.C";
                /// <summary>
                /// Netherlands Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NEPRI.C";
                /// <summary>
                /// Netherlands Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NETBEU.C";
                /// <summary>
                /// Netherlands 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "NETHERLAND3MBY.C";
                /// <summary>
                /// Netherlands 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "NETHERLAND6MBY.C";
                /// <summary>
                /// Netherlands Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "NETHERLANDINDPROMOM.C";
                /// <summary>
                /// Netherlands Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "NLD-CALENDAR.C";
                /// <summary>
                /// Netherlands Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "NETHERLANDMANPRO.C";
                /// <summary>
                /// Netherlands Personal Spending
                /// </summary>
                public const string PersonalSpending = "NETHERLANDPERSPE.C";
                /// <summary>
                /// Netherlands Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NEUETOTR.C";
                /// <summary>
                /// Netherlands Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "NLDRETAILSALESMOM.C";
                /// <summary>
                /// Netherlands Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "NLDRETAILSALESYOY.C";
            }
            public static class NewZealand
            {
                /// <summary>
                /// NewZealand Calendar
                /// </summary>
                public const string Calendar = "NZD-CALENDAR.C";
                /// <summary>
                /// NewZealand Building Permits
                /// </summary>
                public const string BuildingPermits = "NEWZEALANBUIPER.C";
                /// <summary>
                /// NewZealand Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "NEWZEALANCAPUTI.C";
                /// <summary>
                /// NewZealand Employment Change
                /// </summary>
                public const string EmploymentChange = "NEWZEALANEMPCHA.C";
                /// <summary>
                /// NewZealand Export Prices
                /// </summary>
                public const string ExportPrices = "NEWZEALANEXPPRI.C";
                /// <summary>
                /// NewZealand Food Inflation
                /// </summary>
                public const string FoodInflation = "NEWZEALANFOOINF.C";
                /// <summary>
                /// NewZealand Global Dairy Trade Price Index
                /// </summary>
                public const string GlobalDairyTradePriceIndex = "NEWZEALANGDTPI.C";
                /// <summary>
                /// NewZealand Housing Index
                /// </summary>
                public const string HousingIndex = "NEWZEALANHOUIND.C";
                /// <summary>
                /// NewZealand Import Prices
                /// </summary>
                public const string ImportPrices = "NEWZEALANIMPPRI.C";
                /// <summary>
                /// NewZealand Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "NEWZEALANINFEXP.C";
                /// <summary>
                /// NewZealand Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "NEWZEALANINFRATMOM.C";
                /// <summary>
                /// NewZealand Labour Costs
                /// </summary>
                public const string LabourCosts = "NEWZEALANLABCOS.C";
                /// <summary>
                /// NewZealand Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "NEWZEALANLABFORPARRA.C";
                /// <summary>
                /// NewZealand Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "NEWZEALANMANPMI.C";
                /// <summary>
                /// NewZealand Producer Prices
                /// </summary>
                public const string ProducerPrices = "NEWZEALANPROPRI.C";
                /// <summary>
                /// NewZealand Services Pmi
                /// </summary>
                public const string ServicesPmi = "NEWZEALANSERPMI.C";
                /// <summary>
                /// NewZealand Terms of Trade
                /// </summary>
                public const string TermsOfTrade = "NEWZEALANTEROFTRA.C";
                /// <summary>
                /// NewZealand Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "NEWZEALANTOUARR.C";
                /// <summary>
                /// NewZealand Current Account
                /// </summary>
                public const string CurrentAccount = "NZBPCA.C";
                /// <summary>
                /// NewZealand Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NZCC.C";
                /// <summary>
                /// NewZealand Inflation Rate
                /// </summary>
                public const string InflationRate = "NZCPIYOY.C";
                /// <summary>
                /// NewZealand Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NZIEBCAC.C";
                /// <summary>
                /// NewZealand Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NZLFUNER.C";
                /// <summary>
                /// NewZealand Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "NZLRETAILSALESMOM.C";
                /// <summary>
                /// NewZealand Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "NZLRETAILSALESYOY.C";
                /// <summary>
                /// NewZealand Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NZMTBAL.C";
                /// <summary>
                /// NewZealand Exports
                /// </summary>
                public const string Exports = "NZMTEXP.C";
                /// <summary>
                /// NewZealand Imports
                /// </summary>
                public const string Imports = "NZMTIMP.C";
                /// <summary>
                /// NewZealand GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NZNTGDPC.C";
                /// <summary>
                /// NewZealand GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NZNTGDPY.C";
                /// <summary>
                /// NewZealand Interest Rate
                /// </summary>
                public const string InterestRate = "NZOCRS.C";
                /// <summary>
                /// NewZealand Industrial Production
                /// </summary>
                public const string IndustrialProduction = "OENZV012.C";
            }
            public static class Nicaragua
            {
                /// <summary>
                /// Nicaragua Current Account
                /// </summary>
                public const string CurrentAccount = "NICAGUACURCOUNT.C";
                /// <summary>
                /// Nicaragua GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NICAGUAGDPATE.C";
                /// <summary>
                /// Nicaragua Inflation Rate
                /// </summary>
                public const string InflationRate = "NICAGUAINFNRATE.C";
                /// <summary>
                /// Nicaragua Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NICAGUAUNETRATE.C";
                /// <summary>
                /// Nicaragua Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NICARAGUAINDPRO.C";
            }
            public static class Nigeria
            {
                /// <summary>
                /// Nigeria Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NIGERIABT.C";
                /// <summary>
                /// Nigeria Composite Pmi
                /// </summary>
                public const string CompositePmi = "NIGERIACOMPMI.C";
                /// <summary>
                /// Nigeria Employment Change
                /// </summary>
                public const string EmploymentChange = "NIGERIAEMPCHA.C";
                /// <summary>
                /// Nigeria Food Inflation
                /// </summary>
                public const string FoodInflation = "NIGERIAFOOINF.C";
                /// <summary>
                /// Nigeria Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "NIGERIAFOREXCRES.C";
                /// <summary>
                /// Nigeria GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NIGERIAGDPQOQ.C";
                /// <summary>
                /// Nigeria GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NIGERIAGDPYOY.C";
                /// <summary>
                /// Nigeria Interest Rate
                /// </summary>
                public const string InterestRate = "NIGERIAINTRATE.C";
                /// <summary>
                /// Nigeria Inflation Rate
                /// </summary>
                public const string InflationRate = "NIGERIAIR.C";
                /// <summary>
                /// Nigeria Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "NIGERIAMANPMI.C";
                /// <summary>
                /// Nigeria Services Pmi
                /// </summary>
                public const string ServicesPmi = "NIGERIASERPMI.C";
                /// <summary>
                /// Nigeria Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NIGERIAUR.C";
            }
            public static class Norway
            {
                /// <summary>
                /// Norway Calendar
                /// </summary>
                public const string Calendar = "NOR-CALENDAR.C";
                /// <summary>
                /// Norway Current Account
                /// </summary>
                public const string CurrentAccount = "NOBPLVL.C";
                /// <summary>
                /// Norway Interest Rate
                /// </summary>
                public const string InterestRate = "NOBRDEP.C";
                /// <summary>
                /// Norway Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "NOCONF.C";
                /// <summary>
                /// Norway Inflation Rate
                /// </summary>
                public const string InflationRate = "NOCPIYOY.C";
                /// <summary>
                /// Norway GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "NOGDCOSQ.C";
                /// <summary>
                /// Norway GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "NOGDCOSY.C";
                /// <summary>
                /// Norway Industrial Production
                /// </summary>
                public const string IndustrialProduction = "NOIPGYOY.C";
                /// <summary>
                /// Norway Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "NOLBRATE.C";
                /// <summary>
                /// Norway Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "NORCORECPIRATE.C";
                /// <summary>
                /// Norway Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "NORRETAILSALESMOM.C";
                /// <summary>
                /// Norway Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "NORRETAILSALESYOY.C";
                /// <summary>
                /// Norway Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NORWAYBC.C";
                /// <summary>
                /// Norway Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "NORWAYCORCONPRI.C";
                /// <summary>
                /// Norway Household Spending
                /// </summary>
                public const string HouseholdSpending = "NORWAYHOUSPE.C";
                /// <summary>
                /// Norway Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "NORWAYINDPROMOM.C";
                /// <summary>
                /// Norway Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "NORWAYINFRATMOM.C";
                /// <summary>
                /// Norway Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "NORWAYLEAECOIND.C";
                /// <summary>
                /// Norway Loan Growth
                /// </summary>
                public const string LoanGrowth = "NORWAYLOAGRO.C";
                /// <summary>
                /// Norway Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "NORWAYMANPMI.C";
                /// <summary>
                /// Norway Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "NORWAYMANPRO.C";
                /// <summary>
                /// Norway Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "NORWAYPROPRICHA.C";
                /// <summary>
                /// Norway Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "NORWAYUNEPER.C";
                /// <summary>
                /// Norway Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "NOTBTOT.C";
            }
            public static class Oman
            {
                /// <summary>
                /// Oman Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "OMANGOVBUDVAL.C";
                /// <summary>
                /// Oman Loan Growth
                /// </summary>
                public const string LoanGrowth = "OMANLOAGRO.C";
                /// <summary>
                /// Oman Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "OMANMONSUPM2.C";
                /// <summary>
                /// Oman Inflation Rate
                /// </summary>
                public const string InflationRate = "OMAOMANINFNRATE.C";
            }
            public static class Pakistan
            {
                /// <summary>
                /// Pakistan Inflation Rate
                /// </summary>
                public const string InflationRate = "PACPGENY.C";
                /// <summary>
                /// Pakistan Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PAKISTANCONCON.C";
                /// <summary>
                /// Pakistan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PAKISTANPROPRICHA.C";
                /// <summary>
                /// Pakistan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PTRDBAL.C";
            }
            public static class Palestine
            {
                /// <summary>
                /// Palestine Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PALESTINEPROPRICHA.C";
                /// <summary>
                /// Palestine Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PALTINEBALRADE.C";
                /// <summary>
                /// Palestine Current Account
                /// </summary>
                public const string CurrentAccount = "PALTINECURCOUNT.C";
                /// <summary>
                /// Palestine GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PALTINEGDPATE.C";
                /// <summary>
                /// Palestine Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PALTINEINDCTION.C";
                /// <summary>
                /// Palestine Inflation Rate
                /// </summary>
                public const string InflationRate = "PALTINEINFNRATE.C";
                /// <summary>
                /// Palestine Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PALTINEUNETRATE.C";
            }
            public static class Panama
            {
                /// <summary>
                /// Panama Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "PANAMALEAECOIND.C";
                /// <summary>
                /// Panama Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PANNAMABALRADE.C";
                /// <summary>
                /// Panama Current Account
                /// </summary>
                public const string CurrentAccount = "PANNAMACURCOUNT.C";
                /// <summary>
                /// Panama GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PANNAMAGDPATE.C";
                /// <summary>
                /// Panama Inflation Rate
                /// </summary>
                public const string InflationRate = "PANNAMAINFNRATE.C";
            }
            public static class Paraguay
            {
                /// <summary>
                /// Paraguay Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PARAGUAYBT.C";
                /// <summary>
                /// Paraguay Current Account
                /// </summary>
                public const string CurrentAccount = "PARAGUAYCA.C";
                /// <summary>
                /// Paraguay GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PARAGUAYGDPQOQ.C";
                /// <summary>
                /// Paraguay GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PARAGUAYGDPYOY.C";
                /// <summary>
                /// Paraguay Interest Rate
                /// </summary>
                public const string InterestRate = "PARAGUAYINTRATE.C";
                /// <summary>
                /// Paraguay Inflation Rate
                /// </summary>
                public const string InflationRate = "PARAGUAYIR.C";
            }
            public static class Peru
            {
                /// <summary>
                /// Peru Calendar
                /// </summary>
                public const string Calendar = "PER-CALENDAR.C";
                /// <summary>
                /// Peru Business Confidence
                /// </summary>
                public const string BusinessConfidence = "PERPERUBUSDENCE.C";
                /// <summary>
                /// Peru Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PERPERUINDCTION.C";
                /// <summary>
                /// Peru Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PERTBAL.C";
                /// <summary>
                /// Peru Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "PERUINFRATMOM.C";
                /// <summary>
                /// Peru Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "PERULEAECOIND.C";
                /// <summary>
                /// Peru Inflation Rate
                /// </summary>
                public const string InflationRate = "PRCPYOY.C";
                /// <summary>
                /// Peru Current Account
                /// </summary>
                public const string CurrentAccount = "PRCU.C";
                /// <summary>
                /// Peru GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PRGDPQOQ.C";
                /// <summary>
                /// Peru GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PRGDPYOY.C";
                /// <summary>
                /// Peru Interest Rate
                /// </summary>
                public const string InterestRate = "PRRRONUS.C";
                /// <summary>
                /// Peru Exports
                /// </summary>
                public const string Exports = "PRTREXPT.C";
                /// <summary>
                /// Peru Imports
                /// </summary>
                public const string Imports = "PRTRIMPT.C";
                /// <summary>
                /// Peru Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PRUEUER.C";
            }
            public static class Philippines
            {
                /// <summary>
                /// Philippines Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PHIINESINDCTION.C";
                /// <summary>
                /// Philippines Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "PHILIPPINECORINFRAT.C";
                /// <summary>
                /// Philippines Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "PHILIPPINEFORDIRINV.C";
                /// <summary>
                /// Philippines Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "PHILIPPINEFOREXCRES.C";
                /// <summary>
                /// Philippines Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "PHILIPPINEGOVBUDVAL.C";
                /// <summary>
                /// Philippines Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "PHILIPPINEINFRATMOM.C";
                /// <summary>
                /// Philippines Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "PHILIPPINEMANPMI.C";
                /// <summary>
                /// Philippines Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PHILIPPINEPROPRICHA.C";
                /// <summary>
                /// Philippines Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "PHILIPPINERETSALYOY.C";
                /// <summary>
                /// Philippines Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PHILIPPINESBT.C";
                /// <summary>
                /// Philippines Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PHILIPPINESCC.C";
                /// <summary>
                /// Philippines Exports
                /// </summary>
                public const string Exports = "PHILIPPINESEX.C";
                /// <summary>
                /// Philippines Government Budget
                /// </summary>
                public const string GovernmentBudget = "PHILIPPINESGB.C";
                /// <summary>
                /// Philippines GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PHILIPPINESGDPQOQ.C";
                /// <summary>
                /// Philippines GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PHILIPPINESGDPYOY.C";
                /// <summary>
                /// Philippines Imports
                /// </summary>
                public const string Imports = "PHILIPPINESIM.C";
                /// <summary>
                /// Philippines Interest Rate
                /// </summary>
                public const string InterestRate = "PHILIPPINESINTRATE.C";
                /// <summary>
                /// Philippines Inflation Rate
                /// </summary>
                public const string InflationRate = "PHILIPPINESIR.C";
                /// <summary>
                /// Philippines Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PHILIPPINESUR.C";
            }
            public static class Poland
            {
                /// <summary>
                /// Poland Calendar
                /// </summary>
                public const string Calendar = "POL-CALENDAR.C";
                /// <summary>
                /// Poland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "EUGNPLQQ.C";
                /// <summary>
                /// Poland Inflation Rate
                /// </summary>
                public const string InflationRate = "POCPIYOY.C";
                /// <summary>
                /// Poland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "POGDYOY.C";
                /// <summary>
                /// Poland Industrial Production
                /// </summary>
                public const string IndustrialProduction = "POISCYOY.C";
                /// <summary>
                /// Poland Employment Change
                /// </summary>
                public const string EmploymentChange = "POLANDEMPCHA.C";
                /// <summary>
                /// Poland Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "POLANDFOREXCRES.C";
                /// <summary>
                /// Poland Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "POLANDINDPROMOM.C";
                /// <summary>
                /// Poland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "POLANDINFRATMOM.C";
                /// <summary>
                /// Poland Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "POLANDMANPMI.C";
                /// <summary>
                /// Poland Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "POLANDMONSUPM3.C";
                /// <summary>
                /// Poland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "POLANDPROPRICHA.C";
                /// <summary>
                /// Poland Wage Growth
                /// </summary>
                public const string WageGrowth = "POLANDWAGGRO.C";
                /// <summary>
                /// Poland Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "POLCORECPIRATE.C";
                /// <summary>
                /// Poland Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "POLRETAILSALESMOM.C";
                /// <summary>
                /// Poland Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "POLRETAILSALESYOY.C";
                /// <summary>
                /// Poland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "POMECBGB.C";
                /// <summary>
                /// Poland Current Account
                /// </summary>
                public const string CurrentAccount = "POQECBCA.C";
                /// <summary>
                /// Poland Interest Rate
                /// </summary>
                public const string InterestRate = "PORERATE.C";
                /// <summary>
                /// Poland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "POUER.C";
            }
            public static class Portugal
            {
                /// <summary>
                /// Portugal Inflation Rate
                /// </summary>
                public const string InflationRate = "PLCPYOY.C";
                /// <summary>
                /// Portugal Business Confidence
                /// </summary>
                public const string BusinessConfidence = "PORTUGALBC.C";
                /// <summary>
                /// Portugal Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "PORTUGALINDPROMOM.C";
                /// <summary>
                /// Portugal Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "PORTUGALINFRATMOM.C";
                /// <summary>
                /// Portugal Producer Prices
                /// </summary>
                public const string ProducerPrices = "PORTUGALPROPRI.C";
                /// <summary>
                /// Portugal Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "PORTUGALPROPRICHA.C";
                /// <summary>
                /// Portugal Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "PRTRETAILSALESMOM.C";
                /// <summary>
                /// Portugal Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "PRTRETAILSALESYOY.C";
                /// <summary>
                /// Portugal Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "PTCCI.C";
                /// <summary>
                /// Portugal Current Account
                /// </summary>
                public const string CurrentAccount = "PTCUEURO.C";
                /// <summary>
                /// Portugal GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "PTGDPQOQ.C";
                /// <summary>
                /// Portugal GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "PTGDPYOY.C";
                /// <summary>
                /// Portugal Industrial Production
                /// </summary>
                public const string IndustrialProduction = "PTIPTOTY.C";
                /// <summary>
                /// Portugal Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "PTTBEUAL.C";
                /// <summary>
                /// Portugal Exports
                /// </summary>
                public const string Exports = "PTTBEUEX.C";
                /// <summary>
                /// Portugal Imports
                /// </summary>
                public const string Imports = "PTTBEUIM.C";
                /// <summary>
                /// Portugal Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "PTUE.C";
            }
            public static class Qatar
            {
                /// <summary>
                /// Qatar Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "QATARBT.C";
                /// <summary>
                /// Qatar GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "QATARGDPQOQ.C";
                /// <summary>
                /// Qatar GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "QATARGDPYOY.C";
                /// <summary>
                /// Qatar Inflation Rate
                /// </summary>
                public const string InflationRate = "QATARIR.C";
                /// <summary>
                /// Qatar Loan Growth
                /// </summary>
                public const string LoanGrowth = "QATARLOAGRO.C";
                /// <summary>
                /// Qatar Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "QATARMANPMI.C";
                /// <summary>
                /// Qatar Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "QATARMONSUPM2.C";
                /// <summary>
                /// Qatar Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "QATFRINRDPST.C";
            }
            public static class Romania
            {
                /// <summary>
                /// Romania Calendar
                /// </summary>
                public const string Calendar = "ROM-CALENDAR.C";
                /// <summary>
                /// Romania Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "ROMANIABT.C";
                /// <summary>
                /// Romania Business Confidence
                /// </summary>
                public const string BusinessConfidence = "ROMANIABUSCON.C";
                /// <summary>
                /// Romania Current Account
                /// </summary>
                public const string CurrentAccount = "ROMANIACA.C";
                /// <summary>
                /// Romania Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "ROMANIACONCON.C";
                /// <summary>
                /// Romania GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "ROMANIAGDPQOQ.C";
                /// <summary>
                /// Romania GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "ROMANIAGDPYOY.C";
                /// <summary>
                /// Romania Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "ROMANIAINDPROMOM.C";
                /// <summary>
                /// Romania Interest Rate
                /// </summary>
                public const string InterestRate = "ROMANIAINTRATE.C";
                /// <summary>
                /// Romania Industrial Production
                /// </summary>
                public const string IndustrialProduction = "ROMANIAIP.C";
                /// <summary>
                /// Romania Inflation Rate
                /// </summary>
                public const string InflationRate = "ROMANIAIR.C";
                /// <summary>
                /// Romania Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "ROMANIAPROPRICHA.C";
                /// <summary>
                /// Romania Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "ROMANIAUR.C";
                /// <summary>
                /// Romania Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ROMRETAILSALESMOM.C";
                /// <summary>
                /// Romania Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ROMRETAILSALESYOY.C";
            }
            public static class Russia
            {
                /// <summary>
                /// Russia Calendar
                /// </summary>
                public const string Calendar = "RUS-CALENDAR.C";
                /// <summary>
                /// Russia Interest Rate
                /// </summary>
                public const string InterestRate = "RREFRATE.C";
                /// <summary>
                /// Russia Current Account
                /// </summary>
                public const string CurrentAccount = "RUCACAL.C";
                /// <summary>
                /// Russia Inflation Rate
                /// </summary>
                public const string InflationRate = "RUCPIYOY.C";
                /// <summary>
                /// Russia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "RUDPRYOY.C";
                /// <summary>
                /// Russia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "RUIPRNYY.C";
                /// <summary>
                /// Russia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "RUSRETAILSALESMOM.C";
                /// <summary>
                /// Russia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "RUSRETAILSALESYOY.C";
                /// <summary>
                /// Russia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "RUSSIABC.C";
                /// <summary>
                /// Russia Corporate Profits
                /// </summary>
                public const string CorporateProfits = "RUSSIACORPRO.C";
                /// <summary>
                /// Russia Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "RUSSIAFOREXCRES.C";
                /// <summary>
                /// Russia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "RUSSIAINFRATMOM.C";
                /// <summary>
                /// Russia Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "RUSSIALEAECOIND.C";
                /// <summary>
                /// Russia Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "RUSSIAMANPMI.C";
                /// <summary>
                /// Russia Mni Consumer Sentiment
                /// </summary>
                public const string MniConsumerSentiment = "RUSSIAMNICONSEN.C";
                /// <summary>
                /// Russia Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "RUSSIAMONSUPM2.C";
                /// <summary>
                /// Russia Producer Prices
                /// </summary>
                public const string ProducerPrices = "RUSSIAPROPRI.C";
                /// <summary>
                /// Russia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "RUSSIAPROPRICHA.C";
                /// <summary>
                /// Russia Services Pmi
                /// </summary>
                public const string ServicesPmi = "RUSSIASERPMI.C";
                /// <summary>
                /// Russia Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "RUSSIATOTVEHSAL.C";
                /// <summary>
                /// Russia Wage Growth
                /// </summary>
                public const string WageGrowth = "RUSSIAWAGGRO.C";
                /// <summary>
                /// Russia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "RUSSSIABUSDENCE.C";
                /// <summary>
                /// Russia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "RUTBAL.C";
                /// <summary>
                /// Russia Exports
                /// </summary>
                public const string Exports = "RUTBEX.C";
                /// <summary>
                /// Russia Imports
                /// </summary>
                public const string Imports = "RUTBIM.C";
                /// <summary>
                /// Russia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "RUUER.C";
            }
            public static class Rwanda
            {
                /// <summary>
                /// Rwanda GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "RWANDAGDPQOQ.C";
                /// <summary>
                /// Rwanda GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "RWANDAGDPYOY.C";
                /// <summary>
                /// Rwanda Interest Rate
                /// </summary>
                public const string InterestRate = "RWANDAINTRATE.C";
                /// <summary>
                /// Rwanda Inflation Rate
                /// </summary>
                public const string InflationRate = "RWANDAIR.C";
                /// <summary>
                /// Rwanda Producer Prices
                /// </summary>
                public const string ProducerPrices = "RWANDAPROPRI.C";
                /// <summary>
                /// Rwanda Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "RWANDAPROPRICHA.C";
            }
            public static class SaoTomeAndPrincipe
            {
                /// <summary>
                /// SaoTomeAndPrincipe Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SAOEBALRADE.C";
                /// <summary>
                /// SaoTomeAndPrincipe Inflation Rate
                /// </summary>
                public const string InflationRate = "SAOEINFNRATE.C";
            }
            public static class SaudiArabia
            {
                /// <summary>
                /// SaudiArabia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "4567A12.C";
                /// <summary>
                /// SaudiArabia Loan Growth
                /// </summary>
                public const string LoanGrowth = "SAUDIARABLOAGRO.C";
                /// <summary>
                /// SaudiArabia Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "SAUDIARABMANPMI.C";
                /// <summary>
                /// SaudiArabia Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "SAUDIARABMONSUPM3.C";
                /// <summary>
                /// SaudiArabia Inflation Rate
                /// </summary>
                public const string InflationRate = "SRCPIYOY.C";
                /// <summary>
                /// SaudiArabia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SRGDPCYY.C";
                /// <summary>
                /// SaudiArabia Interest Rate
                /// </summary>
                public const string InterestRate = "SRREPO.C";
            }
            public static class Senegal
            {
                /// <summary>
                /// Senegal Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SENEGALBALRADE.C";
                /// <summary>
                /// Senegal GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SENEGALGDPATE.C";
                /// <summary>
                /// Senegal Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SENEGALINDCTION.C";
                /// <summary>
                /// Senegal Inflation Rate
                /// </summary>
                public const string InflationRate = "SENEGALINFNRATE.C";
            }
            public static class Serbia
            {
                /// <summary>
                /// Serbia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SERBIAINFRATMOM.C";
                /// <summary>
                /// Serbia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SERRBIABALRADE.C";
                /// <summary>
                /// Serbia Current Account
                /// </summary>
                public const string CurrentAccount = "SERRBIACURCOUNT.C";
                /// <summary>
                /// Serbia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SERRBIAGDPATE.C";
                /// <summary>
                /// Serbia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SERRBIAGDPRATE.C";
                /// <summary>
                /// Serbia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SERRBIAINDCTION.C";
                /// <summary>
                /// Serbia Inflation Rate
                /// </summary>
                public const string InflationRate = "SERRBIAINFNRATE.C";
                /// <summary>
                /// Serbia Interest Rate
                /// </summary>
                public const string InterestRate = "SERRBIAINTTRATE.C";
                /// <summary>
                /// Serbia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SERRBIARETSYOY.C";
                /// <summary>
                /// Serbia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SERRBIAUNETRATE.C";
            }
            public static class Seychelles
            {
                /// <summary>
                /// Seychelles Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SEYLLESBALRADE.C";
                /// <summary>
                /// Seychelles Inflation Rate
                /// </summary>
                public const string InflationRate = "SEYLLESINFNRATE.C";
            }
            public static class Singapore
            {
                /// <summary>
                /// Singapore Calendar
                /// </summary>
                public const string Calendar = "SGP-CALENDAR.C";
                /// <summary>
                /// Singapore GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SGDPQOQ.C";
                /// <summary>
                /// Singapore GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SGDPYOY.C";
                /// <summary>
                /// Singapore Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SGPCORECPIRATE.C";
                /// <summary>
                /// Singapore Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SGPRETAILSALESMOM.C";
                /// <summary>
                /// Singapore Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SGPRETAILSALESYOY.C";
                /// <summary>
                /// Singapore Inflation Rate
                /// </summary>
                public const string InflationRate = "SICPIYOY.C";
                /// <summary>
                /// Singapore Current Account
                /// </summary>
                public const string CurrentAccount = "SICUBAL.C";
                /// <summary>
                /// Singapore Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SIIPYOYPCT.C";
                /// <summary>
                /// Singapore Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SINGAPOREBUSCON.C";
                /// <summary>
                /// Singapore Composite Pmi
                /// </summary>
                public const string CompositePmi = "SINGAPORECOMPMI.C";
                /// <summary>
                /// Singapore Export Prices
                /// </summary>
                public const string ExportPrices = "SINGAPOREEXPPRI.C";
                /// <summary>
                /// Singapore Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SINGAPOREFOREXCRES.C";
                /// <summary>
                /// Singapore Housing Index
                /// </summary>
                public const string HousingIndex = "SINGAPOREHOUIND.C";
                /// <summary>
                /// Singapore Import Prices
                /// </summary>
                public const string ImportPrices = "SINGAPOREIMPPRI.C";
                /// <summary>
                /// Singapore Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SINGAPOREINDPROMOM.C";
                /// <summary>
                /// Singapore Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SINGAPOREINFRATMOM.C";
                /// <summary>
                /// Singapore Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "SINGAPORELOATOPRISEC.C";
                /// <summary>
                /// Singapore Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "SINGAPOREMANPMI.C";
                /// <summary>
                /// Singapore Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SINGAPOREPROPRICHA.C";
                /// <summary>
                /// Singapore Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SIQUTOTA.C";
                /// <summary>
                /// Singapore Exports of Non-oil Domestic Exports Of
                /// </summary>
                public const string ExportsOfNon_oilDomesticExportsOf = "SPEXPNOOA.C";
                /// <summary>
                /// Singapore Domestic Exports of Non Oil (nodx) (%yoy)
                /// </summary>
                public const string DomesticExportsOfNonOilNODXPercentYoy = "SPEXPNOOR.C";
                /// <summary>
                /// Singapore Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "STRDE.C";
            }
            public static class Slovakia
            {
                /// <summary>
                /// Slovakia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SLOVAKIABC.C";
                /// <summary>
                /// Slovakia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SLOVAKIABT.C";
                /// <summary>
                /// Slovakia Current Account
                /// </summary>
                public const string CurrentAccount = "SLOVAKIACA.C";
                /// <summary>
                /// Slovakia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SLOVAKIACC.C";
                /// <summary>
                /// Slovakia Construction Output
                /// </summary>
                public const string ConstructionOutput = "SLOVAKIACONOUT.C";
                /// <summary>
                /// Slovakia Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "SLOVAKIACORCONPRI.C";
                /// <summary>
                /// Slovakia Exports
                /// </summary>
                public const string Exports = "SLOVAKIAEX.C";
                /// <summary>
                /// Slovakia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SLOVAKIAGDPQOQ.C";
                /// <summary>
                /// Slovakia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SLOVAKIAGDPYOY.C";
                /// <summary>
                /// Slovakia Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SLOVAKIAHARCONPRI.C";
                /// <summary>
                /// Slovakia Imports
                /// </summary>
                public const string Imports = "SLOVAKIAIM.C";
                /// <summary>
                /// Slovakia Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SLOVAKIAINDPROMOM.C";
                /// <summary>
                /// Slovakia Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SLOVAKIAINFRATMOM.C";
                /// <summary>
                /// Slovakia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SLOVAKIAIP.C";
                /// <summary>
                /// Slovakia Inflation Rate
                /// </summary>
                public const string InflationRate = "SLOVAKIAIR.C";
                /// <summary>
                /// Slovakia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SLOVAKIAUR.C";
                /// <summary>
                /// Slovakia Wage Growth
                /// </summary>
                public const string WageGrowth = "SLOVAKIAWAGGRO.C";
                /// <summary>
                /// Slovakia Calendar
                /// </summary>
                public const string Calendar = "SVK-CALENDAR.C";
                /// <summary>
                /// Slovakia Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SVKCORECPIRATE.C";
                /// <summary>
                /// Slovakia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SVKRETAILSALESMOM.C";
                /// <summary>
                /// Slovakia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SVKRETAILSALESYOY.C";
            }
            public static class Slovenia
            {
                /// <summary>
                /// Slovenia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "EUCCSI.C";
                /// <summary>
                /// Slovenia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "EUGNSIQQ.C";
                /// <summary>
                /// Slovenia Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SLOVENIABC.C";
                /// <summary>
                /// Slovenia Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SLOVENIAHARCONPRI.C";
                /// <summary>
                /// Slovenia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SLOVENIAPROPRICHA.C";
                /// <summary>
                /// Slovenia Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SLOVENIATOUARR.C";
                /// <summary>
                /// Slovenia Inflation Rate
                /// </summary>
                public const string InflationRate = "SVCPYOY.C";
                /// <summary>
                /// Slovenia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SVGDCYOY.C";
                /// <summary>
                /// Slovenia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SVIPTYOY.C";
                /// <summary>
                /// Slovenia Calendar
                /// </summary>
                public const string Calendar = "SVN-CALENDAR.C";
                /// <summary>
                /// Slovenia Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SVNRETAILSALESMOM.C";
                /// <summary>
                /// Slovenia Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SVNRETAILSALESYOY.C";
                /// <summary>
                /// Slovenia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SVTBBALE.C";
                /// <summary>
                /// Slovenia Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SVUER.C";
            }
            public static class SouthAfrica
            {
                /// <summary>
                /// SouthAfrica Calendar
                /// </summary>
                public const string Calendar = "ZAF-CALENDAR.C";
                /// <summary>
                /// SouthAfrica Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "EHUPZA.C";
                /// <summary>
                /// SouthAfrica Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SABZCONF.C";
                /// <summary>
                /// SouthAfrica Inflation Rate
                /// </summary>
                public const string InflationRate = "SACPIYOY.C";
                /// <summary>
                /// SouthAfrica Current Account
                /// </summary>
                public const string CurrentAccount = "SACTLVL.C";
                /// <summary>
                /// SouthAfrica Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SACWC.C";
                /// <summary>
                /// SouthAfrica GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SAGDPQOQ.C";
                /// <summary>
                /// SouthAfrica GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SAGDPYOY.C";
                /// <summary>
                /// SouthAfrica Interest Rate
                /// </summary>
                public const string InterestRate = "SARPRT.C";
                /// <summary>
                /// SouthAfrica Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SATBAL.C";
                /// <summary>
                /// SouthAfrica Bank Lending Rate
                /// </summary>
                public const string BankLendingRate = "SOUTHAFRIBANLENRAT.C";
                /// <summary>
                /// SouthAfrica Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SOUTHAFRICAIP.C";
                /// <summary>
                /// SouthAfrica Car Registrations
                /// </summary>
                public const string CarRegistrations = "SOUTHAFRICARREG.C";
                /// <summary>
                /// SouthAfrica Coincident Index
                /// </summary>
                public const string CoincidentIndex = "SOUTHAFRICOIIND.C";
                /// <summary>
                /// SouthAfrica Composite Pmi
                /// </summary>
                public const string CompositePmi = "SOUTHAFRICOMPMI.C";
                /// <summary>
                /// SouthAfrica Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SOUTHAFRICORINFRAT.C";
                /// <summary>
                /// SouthAfrica Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SOUTHAFRIFOREXCRES.C";
                /// <summary>
                /// SouthAfrica Gold Production
                /// </summary>
                public const string GoldProduction = "SOUTHAFRIGOLPRO.C";
                /// <summary>
                /// SouthAfrica Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SOUTHAFRIINDPROMOM.C";
                /// <summary>
                /// SouthAfrica Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SOUTHAFRIINFRATMOM.C";
                /// <summary>
                /// SouthAfrica Lending Rate
                /// </summary>
                public const string LendingRate = "SOUTHAFRILENRAT.C";
                /// <summary>
                /// SouthAfrica Loans to Private Sector
                /// </summary>
                public const string LoansToPrivateSector = "SOUTHAFRILOATOPRISEC.C";
                /// <summary>
                /// SouthAfrica Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "SOUTHAFRIMANPMI.C";
                /// <summary>
                /// SouthAfrica Mining Production
                /// </summary>
                public const string MiningProduction = "SOUTHAFRIMINPRO.C";
                /// <summary>
                /// SouthAfrica Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "SOUTHAFRIMONSUPM3.C";
                /// <summary>
                /// SouthAfrica Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "SOUTHAFRIPRISECCRE.C";
                /// <summary>
                /// SouthAfrica Producer Prices
                /// </summary>
                public const string ProducerPrices = "SOUTHAFRIPROPRI.C";
                /// <summary>
                /// SouthAfrica Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SOUTHAFRIPROPRICHA.C";
                /// <summary>
                /// SouthAfrica Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "SOUTHAFRITOTVEHSAL.C";
                /// <summary>
                /// SouthAfrica Unemployed Persons
                /// </summary>
                public const string UnemployedPersons = "SOUTHAFRIUNEPER.C";
                /// <summary>
                /// SouthAfrica Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ZAFRETAILSALESMOM.C";
                /// <summary>
                /// SouthAfrica Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ZAFRETAILSALESYOY.C";
            }
            public static class SouthKorea
            {
                /// <summary>
                /// SouthKorea Calendar
                /// </summary>
                public const string Calendar = "KOR-CALENDAR.C";
                /// <summary>
                /// SouthKorea Current Account
                /// </summary>
                public const string CurrentAccount = "KOBPCB.C";
                /// <summary>
                /// SouthKorea Inflation Rate
                /// </summary>
                public const string InflationRate = "KOCPIYOY.C";
                /// <summary>
                /// SouthKorea Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "KOEAUERS.C";
                /// <summary>
                /// SouthKorea Exports
                /// </summary>
                public const string Exports = "KOEXTOT.C";
                /// <summary>
                /// SouthKorea GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "KOGDPQOQ.C";
                /// <summary>
                /// SouthKorea GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "KOGDPYOY.C";
                /// <summary>
                /// SouthKorea Imports
                /// </summary>
                public const string Imports = "KOIMTOT.C";
                /// <summary>
                /// SouthKorea Industrial Production
                /// </summary>
                public const string IndustrialProduction = "KOIPIY.C";
                /// <summary>
                /// SouthKorea Interest Rate
                /// </summary>
                public const string InterestRate = "KORP7DR.C";
                /// <summary>
                /// SouthKorea Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "KORRETAILSALESMOM.C";
                /// <summary>
                /// SouthKorea Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "KORRETAILSALESYOY.C";
                /// <summary>
                /// SouthKorea Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "KOTRBAL.C";
                /// <summary>
                /// SouthKorea Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SKCOEXPC.C";
                /// <summary>
                /// SouthKorea Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SOUTHKOREABC.C";
                /// <summary>
                /// SouthKorea Construction Output
                /// </summary>
                public const string ConstructionOutput = "SOUTHKORECONOUT.C";
                /// <summary>
                /// SouthKorea Export Prices
                /// </summary>
                public const string ExportPrices = "SOUTHKOREEXPPRI.C";
                /// <summary>
                /// SouthKorea External Debt
                /// </summary>
                public const string ExternalDebt = "SOUTHKOREEXTDEB.C";
                /// <summary>
                /// SouthKorea Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SOUTHKOREFOREXCRES.C";
                /// <summary>
                /// SouthKorea Import Prices
                /// </summary>
                public const string ImportPrices = "SOUTHKOREIMPPRI.C";
                /// <summary>
                /// SouthKorea Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SOUTHKOREINDPROMOM.C";
                /// <summary>
                /// SouthKorea Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SOUTHKOREINFRATMOM.C";
                /// <summary>
                /// SouthKorea Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SOUTHKOREMANPMI.C";
                /// <summary>
                /// SouthKorea Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "SOUTHKOREMANPRO.C";
                /// <summary>
                /// SouthKorea Producer Prices
                /// </summary>
                public const string ProducerPrices = "SOUTHKOREPROPRI.C";
                /// <summary>
                /// SouthKorea Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SOUTHKOREPROPRICHA.C";
            }
            public static class Spain
            {
                /// <summary>
                /// Spain Calendar
                /// </summary>
                public const string Calendar = "ESP-CALENDAR.C";
                /// <summary>
                /// Spain Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "ESPRETAILSALESMOM.C";
                /// <summary>
                /// Spain Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "ESPRETAILSALESYOY.C";
                /// <summary>
                /// Spain Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSPG10YR.C";
                /// <summary>
                /// Spain 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "SPAIN3MBY.C";
                /// <summary>
                /// Spain 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "SPAIN3YNY.C";
                /// <summary>
                /// Spain 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "SPAIN5WBY.C";
                /// <summary>
                /// Spain 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "SPAIN5YNY.C";
                /// <summary>
                /// Spain 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "SPAIN6MBY.C";
                /// <summary>
                /// Spain Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SPAINBC.C";
                /// <summary>
                /// Spain Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SPAINCC.C";
                /// <summary>
                /// Spain Factory Orders
                /// </summary>
                public const string FactoryOrders = "SPAINFACORD.C";
                /// <summary>
                /// Spain Harmonised Consumer Prices
                /// </summary>
                public const string HarmonisedConsumerPrices = "SPAINHARCONPRI.C";
                /// <summary>
                /// Spain Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SPAININFRATMOM.C";
                /// <summary>
                /// Spain Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SPAINIP.C";
                /// <summary>
                /// Spain Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "SPAINMANPMI.C";
                /// <summary>
                /// Spain Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SPAINPROPRICHA.C";
                /// <summary>
                /// Spain Services Pmi
                /// </summary>
                public const string ServicesPmi = "SPAINSERPMI.C";
                /// <summary>
                /// Spain Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "SPAINTOTVEHSAL.C";
                /// <summary>
                /// Spain Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SPAINTOUARR.C";
                /// <summary>
                /// Spain Unemployment Change
                /// </summary>
                public const string UnemploymentChange = "SPAINUNECHA.C";
                /// <summary>
                /// Spain Current Account
                /// </summary>
                public const string CurrentAccount = "SPCAEURO.C";
                /// <summary>
                /// Spain Inflation Rate
                /// </summary>
                public const string InflationRate = "SPIPCYOY.C";
                /// <summary>
                /// Spain GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SPNAGDPQ.C";
                /// <summary>
                /// Spain GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SPNAGDPY.C";
                /// <summary>
                /// Spain Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SPTBEUBL.C";
                /// <summary>
                /// Spain Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTES.C";
            }
            public static class SriLanka
            {
                /// <summary>
                /// SriLanka Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SRI-LANKABT.C";
                /// <summary>
                /// SriLanka Current Account
                /// </summary>
                public const string CurrentAccount = "SRI-LANKACA.C";
                /// <summary>
                /// SriLanka GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SRI-LANKAGDPYOY.C";
                /// <summary>
                /// SriLanka Interest Rate
                /// </summary>
                public const string InterestRate = "SRI-LANKAINTRATE.C";
                /// <summary>
                /// SriLanka Inflation Rate
                /// </summary>
                public const string InflationRate = "SRI-LANKAIR.C";
                /// <summary>
                /// SriLanka Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SRI-LANKAUR.C";
                /// <summary>
                /// SriLanka Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SRILANKAINDPRO.C";
                /// <summary>
                /// SriLanka Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SRILANKAMANPMI.C";
                /// <summary>
                /// SriLanka Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "SRILANKAMANPRO.C";
                /// <summary>
                /// SriLanka Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SRILANKAPROPRICHA.C";
                /// <summary>
                /// SriLanka Services Pmi
                /// </summary>
                public const string ServicesPmi = "SRILANKASERPMI.C";
                /// <summary>
                /// SriLanka Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "SRILANKATOUARR.C";
            }
            public static class Suriname
            {
                /// <summary>
                /// Suriname Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SURNAMEBALRADE.C";
                /// <summary>
                /// Suriname Current Account
                /// </summary>
                public const string CurrentAccount = "SURNAMECURCOUNT.C";
                /// <summary>
                /// Suriname Inflation Rate
                /// </summary>
                public const string InflationRate = "SURNAMEINFNRATE.C";
            }
            public static class Sweden
            {
                /// <summary>
                /// Sweden Calendar
                /// </summary>
                public const string Calendar = "CALENDARSWEDEN.C";
                /// <summary>
                /// Sweden Current Account
                /// </summary>
                public const string CurrentAccount = "SWCA.C";
                /// <summary>
                /// Sweden Inflation Rate
                /// </summary>
                public const string InflationRate = "SWCPYOY.C";
                /// <summary>
                /// Sweden Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "SWEDENINDPROMOM.C";
                /// <summary>
                /// Sweden Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SWECCI.C";
                /// <summary>
                /// Sweden Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "SWECORECPIRATE.C";
                /// <summary>
                /// Sweden Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SWEDENBC.C";
                /// <summary>
                /// Sweden Business Inventories
                /// </summary>
                public const string BusinessInventories = "SWEDENBUSINV.C";
                /// <summary>
                /// Sweden Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "SWEDENCAPUTI.C";
                /// <summary>
                /// Sweden Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "SWEDENCORCONPRI.C";
                /// <summary>
                /// Sweden Household Spending
                /// </summary>
                public const string HouseholdSpending = "SWEDENHOUSPE.C";
                /// <summary>
                /// Sweden Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "SWEDENINFEXP.C";
                /// <summary>
                /// Sweden Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SWEDENINFRATMOM.C";
                /// <summary>
                /// Sweden Loan Growth
                /// </summary>
                public const string LoanGrowth = "SWEDENLOAGRO.C";
                /// <summary>
                /// Sweden Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SWEDENMANPMI.C";
                /// <summary>
                /// Sweden New Orders
                /// </summary>
                public const string NewOrders = "SWEDENNEWORD.C";
                /// <summary>
                /// Sweden Private Sector Credit
                /// </summary>
                public const string PrivateSectorCredit = "SWEDENPRISECCRE.C";
                /// <summary>
                /// Sweden Producer Prices
                /// </summary>
                public const string ProducerPrices = "SWEDENPROPRI.C";
                /// <summary>
                /// Sweden Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SWEDENPROPRICHA.C";
                /// <summary>
                /// Sweden Services Pmi
                /// </summary>
                public const string ServicesPmi = "SWEDENSERPMI.C";
                /// <summary>
                /// Sweden Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "SWERETAILSALESMOM.C";
                /// <summary>
                /// Sweden Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "SWERETAILSALESYOY.C";
                /// <summary>
                /// Sweden GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SWGDPAQQ.C";
                /// <summary>
                /// Sweden GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SWGDPAYY.C";
                /// <summary>
                /// Sweden Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SWIPIYOY.C";
                /// <summary>
                /// Sweden Interest Rate
                /// </summary>
                public const string InterestRate = "SWRRATEI.C";
                /// <summary>
                /// Sweden Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SWTBAL.C";
                /// <summary>
                /// Sweden Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UMRTSE.C";
            }
            public static class Switzerland
            {
                /// <summary>
                /// Switzerland Calendar
                /// </summary>
                public const string Calendar = "CHF-CALENDAR.C";
                /// <summary>
                /// Switzerland Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "CHERETAILSALESMOM.C";
                /// <summary>
                /// Switzerland Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "CHERETAILSALESYOY.C";
                /// <summary>
                /// Switzerland Business Confidence
                /// </summary>
                public const string BusinessConfidence = "SWITZERLANDBC.C";
                /// <summary>
                /// Switzerland Factory Orders
                /// </summary>
                public const string FactoryOrders = "SWITZERLANFACORD.C";
                /// <summary>
                /// Switzerland Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "SWITZERLANFOREXCRES.C";
                /// <summary>
                /// Switzerland Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "SWITZERLANINFRATMOM.C";
                /// <summary>
                /// Switzerland Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "SWITZERLANMANPMI.C";
                /// <summary>
                /// Switzerland Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "SWITZERLANNONFARPAY.C";
                /// <summary>
                /// Switzerland Producer Prices
                /// </summary>
                public const string ProducerPrices = "SWITZERLANPROPRI.C";
                /// <summary>
                /// Switzerland Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "SWITZERLANPROPRICHA.C";
                /// <summary>
                /// Switzerland Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "SWITZERLANZEWECOSENI.C";
                /// <summary>
                /// Switzerland Current Account
                /// </summary>
                public const string CurrentAccount = "SZCA.C";
                /// <summary>
                /// Switzerland Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "SZCCI.C";
                /// <summary>
                /// Switzerland Inflation Rate
                /// </summary>
                public const string InflationRate = "SZCPIYOY.C";
                /// <summary>
                /// Switzerland GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "SZGDPCQQ.C";
                /// <summary>
                /// Switzerland GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "SZGDPCYY.C";
                /// <summary>
                /// Switzerland Industrial Production
                /// </summary>
                public const string IndustrialProduction = "SZIPIYOY.C";
                /// <summary>
                /// Switzerland Interest Rate
                /// </summary>
                public const string InterestRate = "SZLTTR.C";
                /// <summary>
                /// Switzerland Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "SZTBAL.C";
                /// <summary>
                /// Switzerland Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "SZUEUEA.C";
            }
            public static class Taiwan
            {
                /// <summary>
                /// Taiwan Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "TAIIWANCONDENCE.C";
                /// <summary>
                /// Taiwan Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "TAIWANBT.C";
                /// <summary>
                /// Taiwan Current Account
                /// </summary>
                public const string CurrentAccount = "TAIWANCA.C";
                /// <summary>
                /// Taiwan Inflation Rate
                /// </summary>
                public const string InflationRate = "TAIWANCPI.C";
                /// <summary>
                /// Taiwan Exports
                /// </summary>
                public const string Exports = "TAIWANEX.C";
                /// <summary>
                /// Taiwan Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "TAIWANFOREXCRES.C";
                /// <summary>
                /// Taiwan GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "TAIWANGDPQOQ.C";
                /// <summary>
                /// Taiwan GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "TAIWANGDPYOY.C";
                /// <summary>
                /// Taiwan Imports
                /// </summary>
                public const string Imports = "TAIWANIM.C";
                /// <summary>
                /// Taiwan Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "TAIWANINFRATMOM.C";
                /// <summary>
                /// Taiwan Industrial Production
                /// </summary>
                public const string IndustrialProduction = "TAIWANIP.C";
                /// <summary>
                /// Taiwan Interest Rate
                /// </summary>
                public const string InterestRate = "TAIWANIR.C";
                /// <summary>
                /// Taiwan Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "TAIWANMANPMI.C";
                /// <summary>
                /// Taiwan Money Supply M2
                /// </summary>
                public const string MoneySupplyM2 = "TAIWANMONSUPM2.C";
                /// <summary>
                /// Taiwan New Orders
                /// </summary>
                public const string NewOrders = "TAIWANNEWORD.C";
                /// <summary>
                /// Taiwan Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "TAIWANPROPRICHA.C";
                /// <summary>
                /// Taiwan Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "TAIWANUR.C";
                /// <summary>
                /// Taiwan Calendar
                /// </summary>
                public const string Calendar = "TWN-CALENDAR.C";
                /// <summary>
                /// Taiwan Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "TWNRETAILSALESYOY.C";
            }
            public static class Tajikistan
            {
                /// <summary>
                /// Tajikistan Inflation Rate
                /// </summary>
                public const string InflationRate = "TAJSTANINFNRATE.C";
            }
            public static class Tanzania
            {
                /// <summary>
                /// Tanzania GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "TANZANIAGDPYOY.C";
                /// <summary>
                /// Tanzania Inflation Rate
                /// </summary>
                public const string InflationRate = "TANZANIAIR.C";
            }
            public static class Thailand
            {
                /// <summary>
                /// Thailand Interest Rate
                /// </summary>
                public const string InterestRate = "BTRR1DAY.C";
                /// <summary>
                /// Thailand Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "THACORECPIRATE.C";
                /// <summary>
                /// Thailand Business Confidence
                /// </summary>
                public const string BusinessConfidence = "THAILANDBC.C";
                /// <summary>
                /// Thailand Coincident Index
                /// </summary>
                public const string CoincidentIndex = "THAILANDCOIIND.C";
                /// <summary>
                /// Thailand Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "THAILANDFOREXCRES.C";
                /// <summary>
                /// Thailand Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "THAILANDMANPMI.C";
                /// <summary>
                /// Thailand Personal Spending
                /// </summary>
                public const string PersonalSpending = "THAILANDPERSPE.C";
                /// <summary>
                /// Thailand Private Investment
                /// </summary>
                public const string PrivateInvestment = "THAILANDPRIINV.C";
                /// <summary>
                /// Thailand Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "THAILANDTOTVEHSAL.C";
                /// <summary>
                /// Thailand Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "THALANDRETSYOY.C";
                /// <summary>
                /// Thailand Current Account
                /// </summary>
                public const string CurrentAccount = "THCA.C";
                /// <summary>
                /// Thailand Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "THCCI.C";
                /// <summary>
                /// Thailand Inflation Rate
                /// </summary>
                public const string InflationRate = "THCPIYOY.C";
                /// <summary>
                /// Thailand GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "THG-PQQ.C";
                /// <summary>
                /// Thailand GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "THG-PQY.C";
                /// <summary>
                /// Thailand Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "THLMUERT.C";
                /// <summary>
                /// Thailand Industrial Production
                /// </summary>
                public const string IndustrialProduction = "THMPIYOY.C";
                /// <summary>
                /// Thailand Exports
                /// </summary>
                public const string Exports = "THNFEXP.C";
                /// <summary>
                /// Thailand Imports
                /// </summary>
                public const string Imports = "THNFIMP.C";
                /// <summary>
                /// Thailand Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "THNFTB.C";
            }
            public static class TrinidadAndTobago
            {
                /// <summary>
                /// TrinidadAndTobago Interest Rate
                /// </summary>
                public const string InterestRate = "TRIGOINTTRATE.C";
            }
            public static class Tunisia
            {
                /// <summary>
                /// Tunisia Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "TUNISIABT.C";
                /// <summary>
                /// Tunisia GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "TUNISIAGDPQOQ.C";
                /// <summary>
                /// Tunisia GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "TUNISIAGDPYOY.C";
                /// <summary>
                /// Tunisia Industrial Production
                /// </summary>
                public const string IndustrialProduction = "TUNISIAINDPRO.C";
                /// <summary>
                /// Tunisia Interest Rate
                /// </summary>
                public const string InterestRate = "TUNISIAINTRATE.C";
                /// <summary>
                /// Tunisia Inflation Rate
                /// </summary>
                public const string InflationRate = "TUNISIAIR.C";
                /// <summary>
                /// Tunisia Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "TUNISIAPROPRICHA.C";
            }
            public static class Turkey
            {
                /// <summary>
                /// Turkey Calendar
                /// </summary>
                public const string Calendar = "TUR-CALENDAR.C";
                /// <summary>
                /// Turkey Current Account
                /// </summary>
                public const string CurrentAccount = "TUCAL.C";
                /// <summary>
                /// Turkey Inflation Rate
                /// </summary>
                public const string InflationRate = "TUCPIY.C";
                /// <summary>
                /// Turkey GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "TUGPCOQS.C";
                /// <summary>
                /// Turkey GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "TUGPCOYR.C";
                /// <summary>
                /// Turkey Interest Rate
                /// </summary>
                public const string InterestRate = "TUIBON.C";
                /// <summary>
                /// Turkey Industrial Production
                /// </summary>
                public const string IndustrialProduction = "TUIPIYOY.C";
                /// <summary>
                /// Turkey Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "TURFRINRDPST.C";
                /// <summary>
                /// Turkey Business Confidence
                /// </summary>
                public const string BusinessConfidence = "TURKEYBC.C";
                /// <summary>
                /// Turkey Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "TURKEYCAPUTI.C";
                /// <summary>
                /// Turkey Car Production
                /// </summary>
                public const string CarProduction = "TURKEYCARPRO.C";
                /// <summary>
                /// Turkey Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "TURKEYCC.C";
                /// <summary>
                /// Turkey Construction Output
                /// </summary>
                public const string ConstructionOutput = "TURKEYCONOUT.C";
                /// <summary>
                /// Turkey Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "TURKEYECOOPTIND.C";
                /// <summary>
                /// Turkey Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "TURKEYGOVBUDVAL.C";
                /// <summary>
                /// Turkey Government Debt
                /// </summary>
                public const string GovernmentDebt = "TURKEYGOVDEB.C";
                /// <summary>
                /// Turkey Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "TURKEYINDPROMOM.C";
                /// <summary>
                /// Turkey Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "TURKEYINFRATMOM.C";
                /// <summary>
                /// Turkey Lending Rate
                /// </summary>
                public const string LendingRate = "TURKEYLENRAT.C";
                /// <summary>
                /// Turkey Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "TURKEYMANPMI.C";
                /// <summary>
                /// Turkey Producer Prices
                /// </summary>
                public const string ProducerPrices = "TURKEYPROPRI.C";
                /// <summary>
                /// Turkey Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "TURKEYPROPRICHA.C";
                /// <summary>
                /// Turkey Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "TURKEYTOUARR.C";
                /// <summary>
                /// Turkey Tourism Revenues
                /// </summary>
                public const string TourismRevenues = "TURKEYTOUREV.C";
                /// <summary>
                /// Turkey Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "TURRETAILSALESMOM.C";
                /// <summary>
                /// Turkey Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "TURRETAILSALESYOY.C";
                /// <summary>
                /// Turkey Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "TUTBAL.C";
                /// <summary>
                /// Turkey Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "TUUNR.C";
            }
            public static class Uganda
            {
                /// <summary>
                /// Uganda Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "UGAANDABALRADE.C";
                /// <summary>
                /// Uganda GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "UGAANDAGDPATE.C";
                /// <summary>
                /// Uganda Inflation Rate
                /// </summary>
                public const string InflationRate = "UGAANDAINFNRATE.C";
                /// <summary>
                /// Uganda Interest Rate
                /// </summary>
                public const string InterestRate = "UGAANDAINTTRATE.C";
                /// <summary>
                /// Uganda Business Confidence
                /// </summary>
                public const string BusinessConfidence = "UGANDABUSCON.C";
                /// <summary>
                /// Uganda Composite Pmi
                /// </summary>
                public const string CompositePmi = "UGANDACOMPMI.C";
                /// <summary>
                /// Uganda Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "UGANDAPROPRICHA.C";
            }
            public static class Ukraine
            {
                /// <summary>
                /// Ukraine Calendar
                /// </summary>
                public const string Calendar = "UKR-CALENDAR.C";
                /// <summary>
                /// Ukraine Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "UKRAINEBT.C";
                /// <summary>
                /// Ukraine Current Account
                /// </summary>
                public const string CurrentAccount = "UKRAINECA.C";
                /// <summary>
                /// Ukraine Foreign Exchange Reserves
                /// </summary>
                public const string ForeignExchangeReserves = "UKRAINEFOREXCRES.C";
                /// <summary>
                /// Ukraine GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "UKRAINEGDPQOQ.C";
                /// <summary>
                /// Ukraine GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "UKRAINEGDPYOY.C";
                /// <summary>
                /// Ukraine Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "UKRAINEINDPROMOM.C";
                /// <summary>
                /// Ukraine Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "UKRAINEINFRATMOM.C";
                /// <summary>
                /// Ukraine Interest Rate
                /// </summary>
                public const string InterestRate = "UKRAINEINTRATE.C";
                /// <summary>
                /// Ukraine Industrial Production
                /// </summary>
                public const string IndustrialProduction = "UKRAINEIP.C";
                /// <summary>
                /// Ukraine Inflation Rate
                /// </summary>
                public const string InflationRate = "UKRAINEIR.C";
                /// <summary>
                /// Ukraine Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "UKRAINEMONSUPM3.C";
                /// <summary>
                /// Ukraine Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "UKRAINERETSALMOM.C";
                /// <summary>
                /// Ukraine Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "UKRAINERETSALYOY.C";
                /// <summary>
                /// Ukraine Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UKRAINEUR.C";
            }
            public static class UnitedArabEmirates
            {
                /// <summary>
                /// UnitedArabEmirates Interest Rate
                /// </summary>
                public const string InterestRate = "EIBO1M.C";
                /// <summary>
                /// UnitedArabEmirates Inflation Rate
                /// </summary>
                public const string InflationRate = "UACIGPY.C";
                /// <summary>
                /// UnitedArabEmirates Loan Growth
                /// </summary>
                public const string LoanGrowth = "UNITEDARALOAGRO.C";
                /// <summary>
                /// UnitedArabEmirates Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "UNITEDARAMANPMI.C";
                /// <summary>
                /// UnitedArabEmirates Money Supply M3
                /// </summary>
                public const string MoneySupplyM3 = "UNITEDARAMONSUPM3.C";
            }
            public static class UnitedKingdom
            {
                /// <summary>
                /// UnitedKingdom Calendar
                /// </summary>
                public const string Calendar = "GBRINDICATORS.C";
                /// <summary>
                /// UnitedKingdom Business Confidence
                /// </summary>
                public const string BusinessConfidence = "EUR1UK.C";
                /// <summary>
                /// UnitedKingdom Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "GBRCORECPIRATE.C";
                /// <summary>
                /// UnitedKingdom Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "GBRRETAILSALESMOM.C";
                /// <summary>
                /// UnitedKingdom Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "GBRRETAILSALESYOY.C";
                /// <summary>
                /// UnitedKingdom Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GUKG10.C";
                /// <summary>
                /// UnitedKingdom Interest Rate
                /// </summary>
                public const string InterestRate = "UKBRBASE.C";
                /// <summary>
                /// UnitedKingdom Current Account
                /// </summary>
                public const string CurrentAccount = "UKCA.C";
                /// <summary>
                /// UnitedKingdom Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "UKCCI.C";
                /// <summary>
                /// UnitedKingdom GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "UKGRYBZQ.C";
                /// <summary>
                /// UnitedKingdom GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "UKGRYBZY.C";
                /// <summary>
                /// UnitedKingdom Industrial Production
                /// </summary>
                public const string IndustrialProduction = "UKIPIYOY.C";
                /// <summary>
                /// UnitedKingdom Inflation Rate
                /// </summary>
                public const string InflationRate = "UKRPCJYR.C";
                /// <summary>
                /// UnitedKingdom Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "UKTBTTBA.C";
                /// <summary>
                /// UnitedKingdom Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "UKUEILOR.C";
                /// <summary>
                /// UnitedKingdom 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "UNITEDKIN3YBY.C";
                /// <summary>
                /// UnitedKingdom 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "UNITEDKIN5YEANOTYIE.C";
                /// <summary>
                /// UnitedKingdom Car Registrations
                /// </summary>
                public const string CarRegistrations = "UNITEDKINCARREG.C";
                /// <summary>
                /// UnitedKingdom Claimant Count Change
                /// </summary>
                public const string ClaimantCountChange = "UNITEDKINCLACOUCHA.C";
                /// <summary>
                /// UnitedKingdom Composite Pmi
                /// </summary>
                public const string CompositePmi = "UNITEDKINCOMPMI.C";
                /// <summary>
                /// UnitedKingdom Consumer Credit
                /// </summary>
                public const string ConsumerCredit = "UNITEDKINCONCRE.C";
                /// <summary>
                /// UnitedKingdom Construction Orders
                /// </summary>
                public const string ConstructionOrders = "UNITEDKINCONORD.C";
                /// <summary>
                /// UnitedKingdom Construction Output
                /// </summary>
                public const string ConstructionOutput = "UNITEDKINCONOUT.C";
                /// <summary>
                /// UnitedKingdom Construction Pmi
                /// </summary>
                public const string ConstructionPmi = "UNITEDKINCONPMI.C";
                /// <summary>
                /// UnitedKingdom Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "UNITEDKINCORCONPRI.C";
                /// <summary>
                /// UnitedKingdom Core Producer Prices
                /// </summary>
                public const string CoreProducerPrices = "UNITEDKINCORPROPRI.C";
                /// <summary>
                /// UnitedKingdom Employment Change
                /// </summary>
                public const string EmploymentChange = "UNITEDKINEMPCHA.C";
                /// <summary>
                /// UnitedKingdom Factory Orders
                /// </summary>
                public const string FactoryOrders = "UNITEDKINFACORD.C";
                /// <summary>
                /// UnitedKingdom Government Debt
                /// </summary>
                public const string GovernmentDebt = "UNITEDKINGOVDEB.C";
                /// <summary>
                /// UnitedKingdom Housing Index
                /// </summary>
                public const string HousingIndex = "UNITEDKINHOUIND.C";
                /// <summary>
                /// UnitedKingdom Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "UNITEDKININDPROMOM.C";
                /// <summary>
                /// UnitedKingdom Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "UNITEDKININFEXP.C";
                /// <summary>
                /// UnitedKingdom Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "UNITEDKININFRATMOM.C";
                /// <summary>
                /// UnitedKingdom Labour Costs
                /// </summary>
                public const string LabourCosts = "UNITEDKINLABCOS.C";
                /// <summary>
                /// UnitedKingdom Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "UNITEDKINLEAECOIND.C";
                /// <summary>
                /// UnitedKingdom Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "UNITEDKINMANPMI.C";
                /// <summary>
                /// UnitedKingdom Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "UNITEDKINMANPRO.C";
                /// <summary>
                /// UnitedKingdom Mortgage Approvals
                /// </summary>
                public const string MortgageApprovals = "UNITEDKINMORAPP.C";
                /// <summary>
                /// UnitedKingdom Mortgage Applications
                /// </summary>
                public const string MortgageApplications = "UNITEDKINMORAPPLI.C";
                /// <summary>
                /// UnitedKingdom Private Investment
                /// </summary>
                public const string PrivateInvestment = "UNITEDKINPRIINV.C";
                /// <summary>
                /// UnitedKingdom Productivity
                /// </summary>
                public const string Productivity = "UNITEDKINPRO.C";
                /// <summary>
                /// UnitedKingdom Producer Prices
                /// </summary>
                public const string ProducerPrices = "UNITEDKINPROPRI.C";
                /// <summary>
                /// UnitedKingdom Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "UNITEDKINPROPRICHA.C";
                /// <summary>
                /// UnitedKingdom Retail Price Index
                /// </summary>
                public const string RetailPriceIndex = "UNITEDKINRETPRIIND.C";
                /// <summary>
                /// UnitedKingdom Services Pmi
                /// </summary>
                public const string ServicesPmi = "UNITEDKINSERPMI.C";
                /// <summary>
                /// UnitedKingdom Wage Growth
                /// </summary>
                public const string WageGrowth = "UNITEDKINWAGGRO.C";
            }
            public static class UnitedStates
            {
                /// <summary>
                /// UnitedStates API Crude Oil Stock Change
                /// </summary>
                public const string ApiCrudeOilStockChange = "APICRUDEOIL.C";
                /// <summary>
                /// UnitedStates Calendar
                /// </summary>
                public const string Calendar = "USD-CALENDAR.C";
                /// <summary>
                /// UnitedStates Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "CONCCONF.C";
                /// <summary>
                /// UnitedStates Inflation Rate
                /// </summary>
                public const string InflationRate = "CPI-YOY.C";
                /// <summary>
                /// UnitedStates Interest Rate
                /// </summary>
                public const string InterestRate = "FDTR.C";
                /// <summary>
                /// UnitedStates GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "GDP-CQOQ.C";
                /// <summary>
                /// UnitedStates GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "GDP-CYOY.C";
                /// <summary>
                /// UnitedStates Initial Jobless Claims
                /// </summary>
                public const string InitialJoblessClaims = "IJCUSA.C";
                /// <summary>
                /// UnitedStates Industrial Production
                /// </summary>
                public const string IndustrialProduction = "IP-YOY.C";
                /// <summary>
                /// UnitedStates Business Confidence
                /// </summary>
                public const string BusinessConfidence = "NAPMPMI.C";
                /// <summary>
                /// UnitedStates Non Farm Payrolls
                /// </summary>
                public const string NonFarmPayrolls = "NFP-TCH.C";
                /// <summary>
                /// UnitedStates Retail Sales Ex Autos
                /// </summary>
                public const string RetailSalesExAutos = "UNITEDSTARETSALEXAUT.C";
                /// <summary>
                /// UnitedStates Retail Sales MoM
                /// </summary>
                public const string RetailSalesMom = "RSTAMOM.C";
                /// <summary>
                /// UnitedStates Exports
                /// </summary>
                public const string Exports = "TBEXTOT.C";
                /// <summary>
                /// UnitedStates Imports
                /// </summary>
                public const string Imports = "TBIMTOT.C";
                /// <summary>
                /// UnitedStates 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "UNITEDSTA2YEANOTYIE.C";
                /// <summary>
                /// UnitedStates 30 Year Bond Yield
                /// </summary>
                public const string ThirtyYearBondYield = "UNITEDSTA30YEABONYIE.C";
                /// <summary>
                /// UnitedStates 3 Month Bill Yield
                /// </summary>
                public const string ThreeMonthBillYield = "UNITEDSTA3MONBILYIE.C";
                /// <summary>
                /// UnitedStates 3 Year Note Yield
                /// </summary>
                public const string ThreeYearNoteYield = "UNITEDSTA3YEANOTYIE.C";
                /// <summary>
                /// UnitedStates 4 Week Bill Yield
                /// </summary>
                public const string FourWeekBillYield = "UNITEDSTA4WEEBILYIE.C";
                /// <summary>
                /// UnitedStates 52 Week Bill Yield
                /// </summary>
                public const string FiftyTwoWeekBillYield = "UNITEDSTA52WEEBILYIE.C";
                /// <summary>
                /// UnitedStates 5 Year Note Yield
                /// </summary>
                public const string FiveYearNoteYield = "UNITEDSTA5YEANOTYIE.C";
                /// <summary>
                /// UnitedStates 6 Month Bill Yield
                /// </summary>
                public const string SixMonthBillYield = "UNITEDSTA6MONBILYIE.C";
                /// <summary>
                /// UnitedStates 7 Year Note Yield
                /// </summary>
                public const string SevenYearNoteYield = "UNITEDSTA7YEANOTYIE.C";
                /// <summary>
                /// UnitedStates Adp Employment Change
                /// </summary>
                public const string AdpEmploymentChange = "UNITEDSTAADPEMPCHA.C";
                /// <summary>
                /// UnitedStates Average Hourly Earnings
                /// </summary>
                public const string AverageHourlyEarnings = "UNITEDSTAAVEHOUEAR.C";
                /// <summary>
                /// UnitedStates Average Weekly Hours
                /// </summary>
                public const string AverageWeeklyHours = "UNITEDSTAAVEWEEHOU.C";
                /// <summary>
                /// UnitedStates Building Permits
                /// </summary>
                public const string BuildingPermits = "UNITEDSTABUIPER.C";
                /// <summary>
                /// UnitedStates Business Inventories
                /// </summary>
                public const string BusinessInventories = "UNITEDSTABUSINV.C";
                /// <summary>
                /// UnitedStates Capital Flows
                /// </summary>
                public const string CapitalFlows = "UNITEDSTACAPFLO.C";
                /// <summary>
                /// UnitedStates Capacity Utilization
                /// </summary>
                public const string CapacityUtilization = "UNITEDSTACAPUTI.C";
                /// <summary>
                /// UnitedStates Case Shiller Home Price Index
                /// </summary>
                public const string CaseShillerHomePriceIndex = "UNITEDSTACASSHIHOMPR.C";
                /// <summary>
                /// UnitedStates Challenger Job Cuts
                /// </summary>
                public const string ChallengerJobCuts = "UNITEDSTACHAJOBCUT.C";
                /// <summary>
                /// UnitedStates Chain Store Sales
                /// </summary>
                public const string ChainStoreSales = "UNITEDSTACHASTOSAL.C";
                /// <summary>
                /// UnitedStates Chicago Fed National Activity Index
                /// </summary>
                public const string ChicagoFedNationalActivityIndex = "UNITEDSTACHIFEDNATAC.C";
                /// <summary>
                /// UnitedStates Chicago Pmi
                /// </summary>
                public const string ChicagoPmi = "UNITEDSTACHIPMI.C";
                /// <summary>
                /// UnitedStates Composite Pmi
                /// </summary>
                public const string CompositePmi = "UNITEDSTACOMPMI.C";
                /// <summary>
                /// UnitedStates Consumer Credit
                /// </summary>
                public const string ConsumerCredit = "UNITEDSTACONCRE.C";
                /// <summary>
                /// UnitedStates Continuing Jobless Claims
                /// </summary>
                public const string ContinuingJoblessClaims = "UNITEDSTACONJOBCLA.C";
                /// <summary>
                /// UnitedStates Consumer Price Index (CPI)
                /// </summary>
                public const string ConsumerPriceIndex = "UNITEDSTACONPRIINDCP.C";
                /// <summary>
                /// UnitedStates Consumer Spending
                /// </summary>
                public const string ConsumerSpending = "UNITEDSTACONSPE.C";
                /// <summary>
                /// UnitedStates Construction Spending
                /// </summary>
                public const string ConstructionSpending = "UNITEDSTACONTSPE.C";
                /// <summary>
                /// UnitedStates Core Consumer Prices
                /// </summary>
                public const string CoreConsumerPrices = "UNITEDSTACORCONPRI.C";
                /// <summary>
                /// UnitedStates Core Pce Price Index
                /// </summary>
                public const string CorePcePriceIndex = "UNITEDSTACORPCEPRIIN.C";
                /// <summary>
                /// UnitedStates Corporate Profits
                /// </summary>
                public const string CorporateProfits = "UNITEDSTACORPRO.C";
                /// <summary>
                /// UnitedStates Core Producer Prices
                /// </summary>
                public const string CoreProducerPrices = "UNITEDSTACORPROPRI.C";
                /// <summary>
                /// UnitedStates Crude Oil Stocks Change
                /// </summary>
                public const string CrudeOilStocksChange = "UNITEDSTACRUOILSTOCH.C";
                /// <summary>
                /// UnitedStates Dallas Fed Manufacturing Index
                /// </summary>
                public const string DallasFedManufacturingIndex = "UNITEDSTADALFEDMANIN.C";
                /// <summary>
                /// UnitedStates Durable Goods Orders
                /// </summary>
                public const string DurableGoodsOrders = "UNITEDSTADURGOOORD.C";
                /// <summary>
                /// UnitedStates Durable Goods Orders Ex Defense
                /// </summary>
                public const string DurableGoodsOrdersExDefense = "UNITEDSTADURGOOORDED.C";
                /// <summary>
                /// UnitedStates Durable Goods Orders Ex Transportation
                /// </summary>
                public const string DurableGoodsOrdersExTransportation = "UNITEDSTADURGOOORDEX.C";
                /// <summary>
                /// UnitedStates Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "UNITEDSTAECOOPTIND.C";
                /// <summary>
                /// UnitedStates Employment Cost Index
                /// </summary>
                public const string EmploymentCostIndex = "UNITEDSTAEMPCOSIND.C";
                /// <summary>
                /// UnitedStates Existing Home Sales
                /// </summary>
                public const string ExistingHomeSales = "UNITEDSTAEXIHOMSAL.C";
                /// <summary>
                /// UnitedStates Export Prices
                /// </summary>
                public const string ExportPrices = "UNITEDSTAEXPPRI.C";
                /// <summary>
                /// UnitedStates Factory Orders
                /// </summary>
                public const string FactoryOrders = "UNITEDSTAFACORD.C";
                /// <summary>
                /// UnitedStates Factory Orders Ex Transportation
                /// </summary>
                public const string FactoryOrdersExTransportation = "UNITEDSTAFACORDEXTRA.C";
                /// <summary>
                /// UnitedStates Foreign Bond Investment
                /// </summary>
                public const string ForeignBondInvestment = "UNITEDSTAFORBONINV.C";
                /// <summary>
                /// UnitedStates Gasoline Stocks Change
                /// </summary>
                public const string GasolineStocksChange = "UNITEDSTAGASSTOCHA.C";
                /// <summary>
                /// UnitedStates Gdp Deflator
                /// </summary>
                public const string GdpDeflator = "UNITEDSTAGDPDEF.C";
                /// <summary>
                /// UnitedStates Government Budget Value
                /// </summary>
                public const string GovernmentBudgetValue = "UNITEDSTAGOVBUDVAL.C";
                /// <summary>
                /// UnitedStates Government Payrolls
                /// </summary>
                public const string GovernmentPayrolls = "UNITEDSTAGOVPAY.C";
                /// <summary>
                /// UnitedStates Housing Index
                /// </summary>
                public const string HousingIndex = "UNITEDSTAHOUIND.C";
                /// <summary>
                /// UnitedStates Housing Starts
                /// </summary>
                public const string HousingStarts = "UNITEDSTAHOUSTA.C";
                /// <summary>
                /// UnitedStates Import Prices
                /// </summary>
                public const string ImportPrices = "UNITEDSTAIMPPRI.C";
                /// <summary>
                /// UnitedStates Industrial Production Mom
                /// </summary>
                public const string IndustrialProductionMom = "UNITEDSTAINDPROMOM.C";
                /// <summary>
                /// UnitedStates Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "UNITEDSTAINFEXP.C";
                /// <summary>
                /// UnitedStates Inflation Rate Mom
                /// </summary>
                public const string InflationRateMom = "UNITEDSTAINFRATMOM.C";
                /// <summary>
                /// UnitedStates Ism New York Index
                /// </summary>
                public const string IsmNewYorkIndex = "UNITEDSTAISMNEWYORIN.C";
                /// <summary>
                /// UnitedStates Job Offers
                /// </summary>
                public const string JobOffers = "UNITEDSTAJOBOFF.C";
                /// <summary>
                /// UnitedStates Labour Costs
                /// </summary>
                public const string LabourCosts = "UNITEDSTALABCOS.C";
                /// <summary>
                /// UnitedStates Labor Force Participation Rate
                /// </summary>
                public const string LaborForceParticipationRate = "UNITEDSTALABFORPARRA.C";
                /// <summary>
                /// UnitedStates Leading Economic Index
                /// </summary>
                public const string LeadingEconomicIndex = "UNITEDSTALEAECOIND.C";
                /// <summary>
                /// UnitedStates Labor Market Conditions Index
                /// </summary>
                public const string LaborMarketConditionsIndex = "UNITEDSTALMCI.C";
                /// <summary>
                /// UnitedStates Manufacturing Payrolls
                /// </summary>
                public const string ManufacturingPayrolls = "UNITEDSTAMANPAY.C";
                /// <summary>
                /// UnitedStates Manufacturing Pmi
                /// </summary>
                public const string ManufacturingPmi = "UNITEDSTAMANPMI.C";
                /// <summary>
                /// UnitedStates Manufacturing Production
                /// </summary>
                public const string ManufacturingProduction = "UNITEDSTAMANPRO.C";
                /// <summary>
                /// UnitedStates Mortgage Applications
                /// </summary>
                public const string MortgageApplications = "UNITEDSTAMORAPP.C";
                /// <summary>
                /// UnitedStates Mortgage Rate
                /// </summary>
                public const string MortgageRate = "UNITEDSTAMORRAT.C";
                /// <summary>
                /// UnitedStates Nahb Housing Market Index
                /// </summary>
                public const string NahbHousingMarketIndex = "UNITEDSTANAHHOUMARIN.C";
                /// <summary>
                /// UnitedStates Natural Gas Stocks Change
                /// </summary>
                public const string NaturalGasStocksChange = "UNITEDSTANATGASSTOCH.C";
                /// <summary>
                /// UnitedStates Net Long-term Tic Flows
                /// </summary>
                public const string NetLong_termTicFlows = "UNITEDSTANETLONTICFL.C";
                /// <summary>
                /// UnitedStates New Home Sales
                /// </summary>
                public const string NewHomeSales = "UNITEDSTANEWHOMSAL.C";
                /// <summary>
                /// UnitedStates Nfib Business Optimism Index
                /// </summary>
                public const string NfibBusinessOptimismIndex = "UNITEDSTANFIBUSOPTIN.C";
                /// <summary>
                /// UnitedStates Non Manufacturing PMI
                /// </summary>
                public const string NonManufacturingPmi = "UNITEDSTANONMANPMI.C";
                /// <summary>
                /// UnitedStates Nonfarm Payrolls Private
                /// </summary>
                public const string NonfarmPayrollsPrivate = "UNITEDSTANONPAY-PRI.C";
                /// <summary>
                /// UnitedStates Ny Empire State Manufacturing Index
                /// </summary>
                public const string NyEmpireStateManufacturingIndex = "UNITEDSTANYEMPSTAMAN.C";
                /// <summary>
                /// UnitedStates Pce Price Index
                /// </summary>
                public const string PcePriceIndex = "UNITEDSTAPCEPRIIND.C";
                /// <summary>
                /// UnitedStates Pending Home Sales
                /// </summary>
                public const string PendingHomeSales = "UNITEDSTAPENHOMSAL.C";
                /// <summary>
                /// UnitedStates Personal Income
                /// </summary>
                public const string PersonalIncome = "UNITEDSTAPERINC.C";
                /// <summary>
                /// UnitedStates Personal Spending
                /// </summary>
                public const string PersonalSpending = "UNITEDSTAPERSPE.C";
                /// <summary>
                /// UnitedStates Philadelphia Fed Manufacturing Index
                /// </summary>
                public const string PhiladelphiaFedManufacturingIndex = "UNITEDSTAPHIFEDMANIN.C";
                /// <summary>
                /// UnitedStates Productivity
                /// </summary>
                public const string Productivity = "UNITEDSTAPRO.C";
                /// <summary>
                /// UnitedStates Producer Prices
                /// </summary>
                public const string ProducerPrices = "UNITEDSTAPROPRI.C";
                /// <summary>
                /// UnitedStates Producer Prices Change
                /// </summary>
                public const string ProducerPricesChange = "UNITEDSTAPROPRICHA.C";
                /// <summary>
                /// UnitedStates Redbook Index
                /// </summary>
                public const string RedbookIndex = "UNITEDSTAREDIND.C";
                /// <summary>
                /// UnitedStates Richmond Fed Manufacturing Index
                /// </summary>
                public const string RichmondFedManufacturingIndex = "UNITEDSTARICFEDMANIN.C";
                /// <summary>
                /// UnitedStates Services Pmi
                /// </summary>
                public const string ServicesPmi = "UNITEDSTASERPMI.C";
                /// <summary>
                /// UnitedStates Total Vehicle Sales
                /// </summary>
                public const string TotalVehicleSales = "UNITEDSTATOTVEHSAL.C";
                /// <summary>
                /// UnitedStates Wholesale Inventories
                /// </summary>
                public const string WholesaleInventories = "UNITEDSTAWHOINV.C";
                /// <summary>
                /// UnitedStates Core Inflation Rate
                /// </summary>
                public const string CoreInflationRate = "USACORECPIRATE.C";
                /// <summary>
                /// UnitedStates Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "USARETAILSALESYOY.C";
                /// <summary>
                /// UnitedStates Current Account
                /// </summary>
                public const string CurrentAccount = "USCABAL.C";
                /// <summary>
                /// UnitedStates Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "USGG10YR.C";
                /// <summary>
                /// UnitedStates Crude Oil Rigs
                /// </summary>
                public const string CrudeOilRigs = "USOILRIGS.C";
                /// <summary>
                /// UnitedStates Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "USTBTOT.C";
                /// <summary>
                /// UnitedStates Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "USURTOT.C";
            }
            public static class Uruguay
            {
                /// <summary>
                /// Uruguay Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "URUGUAYBALRADE.C";
                /// <summary>
                /// Uruguay Current Account
                /// </summary>
                public const string CurrentAccount = "URUGUAYCURCOUNT.C";
                /// <summary>
                /// Uruguay GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "URUGUAYGDPATE.C";
                /// <summary>
                /// Uruguay GDP Growth Rate
                /// </summary>
                public const string GdpGrowthRate = "URUGUAYGDPGRORAT.C";
                /// <summary>
                /// Uruguay Industrial Production
                /// </summary>
                public const string IndustrialProduction = "URUGUAYINDCTION.C";
                /// <summary>
                /// Uruguay Inflation Rate
                /// </summary>
                public const string InflationRate = "URUGUAYINFNRATE.C";
                /// <summary>
                /// Uruguay Unemployment Rate
                /// </summary>
                public const string UnemploymentRate = "URUGUAYUNETRATE.C";
            }
            public static class Venezuela
            {
                /// <summary>
                /// Venezuela Industrial Production
                /// </summary>
                public const string IndustrialProduction = "VENEZUELAINDPRO.C";
                /// <summary>
                /// Venezuela Retail Sales Mom
                /// </summary>
                public const string RetailSalesMom = "VENEZUELARETSALMOM.C";
                /// <summary>
                /// Venezuela Retail Sales YoY
                /// </summary>
                public const string RetailSalesYoy = "VENUELARETSYOY.C";
                /// <summary>
                /// Venezuela Inflation Rate
                /// </summary>
                public const string InflationRate = "VNVPIYOY.C";
            }
            public static class Vietnam
            {
                /// <summary>
                /// Vietnam Balance of Trade
                /// </summary>
                public const string BalanceOfTrade = "VIETNAMBT.C";
                /// <summary>
                /// Vietnam Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "VIETNAMFORDIRINV.C";
                /// <summary>
                /// Vietnam GDP Annual Growth Rate
                /// </summary>
                public const string GdpAnnualGrowthRate = "VIETNAMGDPYOY.C";
                /// <summary>
                /// Vietnam Industrial Production
                /// </summary>
                public const string IndustrialProduction = "VIETNAMIP.C";
                /// <summary>
                /// Vietnam Inflation Rate
                /// </summary>
                public const string InflationRate = "VIETNAMIR.C";
                /// <summary>
                /// Vietnam Manufacturing PMI
                /// </summary>
                public const string ManufacturingPmi = "VIETNAMMANPMI.C";
                /// <summary>
                /// Vietnam Retail Sales Yoy
                /// </summary>
                public const string RetailSalesYoy = "VIETNAMRETSALYOY.C";
                /// <summary>
                /// Vietnam Tourist Arrivals
                /// </summary>
                public const string TouristArrivals = "VIETNAMTOUARR.C";
            }
            public static class Zambia
            {
                /// <summary>
                /// Zambia Composite Pmi
                /// </summary>
                public const string CompositePmi = "ZAMBIACOMPMI.C";
                /// <summary>
                /// Zambia Inflation Rate
                /// </summary>
                public const string InflationRate = "ZAMMBIAINFNRATE.C";
                /// <summary>
                /// Zambia Interest Rate
                /// </summary>
                public const string InterestRate = "ZAMMBIAINTTRATE.C";
            }
            public static class Zimbabwe
            {
                /// <summary>
                /// Zimbabwe Inflation Rate
                /// </summary>
                public const string InflationRate = "ZIMABWEINFNRATE.C";
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
                public const string Currency = "USDAFN.I";
            }
            public static class Albania
            {
                /// <summary>
                /// Albania Currency
                /// </summary>
                public const string Currency = "USDALL.I";
            }
            public static class Algeria
            {
                /// <summary>
                /// Algeria Currency
                /// </summary>
                public const string Currency = "USDDZD.I";
            }
            public static class Angola
            {
                /// <summary>
                /// Angola Currency
                /// </summary>
                public const string Currency = "USDAOA.I";
            }
            public static class Argentina
            {
                /// <summary>
                /// Argentina Interest Rate
                /// </summary>
                public const string InterestRate = "APDR1T.I";
                /// <summary>
                /// Argentina Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "ARGFRINRDPST.I";
                /// <summary>
                /// Argentina Stock Market
                /// </summary>
                public const string StockMarket = "MERVAL.I";
                /// <summary>
                /// Argentina Currency
                /// </summary>
                public const string Currency = "USDARS.I";
            }
            public static class Armenia
            {
                /// <summary>
                /// Armenia Currency
                /// </summary>
                public const string Currency = "USDAMD.I";
            }
            public static class Australia
            {
                /// <summary>
                /// Australia Stock Market
                /// </summary>
                public const string StockMarket = "AS51.I";
                /// <summary>
                /// Australia Currency
                /// </summary>
                public const string Currency = "AUDUSD.I";
                /// <summary>
                /// Australia 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "AUSTRALIA2YNY.I";
                /// <summary>
                /// Australia Central Bank Balance Sheet
                /// </summary>
                public const string CentralBankBalanceSheet = "AUSTRALIACENBANBALSH.I";
                /// <summary>
                /// Australia Inflation Expectations
                /// </summary>
                public const string InflationExpectations = "AUSTRALIAINFEXP.I";
                /// <summary>
                /// Australia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GACGB10.I";
                /// <summary>
                /// Australia Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "WMCCCONPCT.I";
            }
            public static class Austria
            {
                /// <summary>
                /// Austria Stock Market
                /// </summary>
                public const string StockMarket = "ATX.I";
                /// <summary>
                /// Austria Currency
                /// </summary>
                public const string Currency = "EURUSD.I";
                /// <summary>
                /// Austria Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GAGB10YR.I";
            }
            public static class Bahamas
            {
                /// <summary>
                /// Bahamas Currency
                /// </summary>
                public const string Currency = "USDBSD.I";
            }
            public static class Bahrain
            {
                /// <summary>
                /// Bahrain Interbank Rate
                /// </summary>
                public const string InterbankRate = "BAHRAININTRAT.I";
                /// <summary>
                /// Bahrain Stock Market
                /// </summary>
                public const string StockMarket = "BHSEEI.I";
                /// <summary>
                /// Bahrain Currency
                /// </summary>
                public const string Currency = "USDBHD.I";
            }
            public static class Bangladesh
            {
                /// <summary>
                /// Bangladesh Interest Rate
                /// </summary>
                public const string InterestRate = "BANGLADESHINTRATE.I";
                /// <summary>
                /// Bangladesh Stock Market
                /// </summary>
                public const string StockMarket = "DHAKA.I";
                /// <summary>
                /// Bangladesh Currency
                /// </summary>
                public const string Currency = "USDBDT.I";
            }
            public static class Belarus
            {
                /// <summary>
                /// Belarus Currency
                /// </summary>
                public const string Currency = "USDBYR.I";
            }
            public static class Belgium
            {
                /// <summary>
                /// Belgium Stock Market
                /// </summary>
                public const string StockMarket = "BEL20.I";
                /// <summary>
                /// Belgium Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GBGB10YR.I";
            }
            public static class Bermuda
            {
                /// <summary>
                /// Bermuda Stock Market
                /// </summary>
                public const string StockMarket = "BSX.I";
            }
            public static class Bolivia
            {
                /// <summary>
                /// Bolivia Currency
                /// </summary>
                public const string Currency = "USDBOB.I";
            }
            public static class BosniaAndHerzegovina
            {
                /// <summary>
                /// BosniaAndHerzegovina Stock Market
                /// </summary>
                public const string StockMarket = "SASX10.I";
                /// <summary>
                /// BosniaAndHerzegovina Currency
                /// </summary>
                public const string Currency = "USDBIH.I";
            }
            public static class Botswana
            {
                /// <summary>
                /// Botswana Stock Market
                /// </summary>
                public const string StockMarket = "BGSMDC.I";
                /// <summary>
                /// Botswana Currency
                /// </summary>
                public const string Currency = "USDBWP.I";
            }
            public static class Brazil
            {
                /// <summary>
                /// Brazil Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "BRAZILCASRESRAT.I";
                /// <summary>
                /// Brazil Interbank Rate
                /// </summary>
                public const string InterbankRate = "BRAZILINTRAT.I";
                /// <summary>
                /// Brazil Minimum Wages
                /// </summary>
                public const string MinimumWages = "BRAZILMINWAG.I";
                /// <summary>
                /// Brazil Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GEBU10Y.I";
                /// <summary>
                /// Brazil Stock Market
                /// </summary>
                public const string StockMarket = "IBOV.I";
                /// <summary>
                /// Brazil Currency
                /// </summary>
                public const string Currency = "USDBRL.I";
            }
            public static class Brunei
            {
                /// <summary>
                /// Brunei Currency
                /// </summary>
                public const string Currency = "USDBND.I";
            }
            public static class Bulgaria
            {
                /// <summary>
                /// Bulgaria Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "BULGARIAGOVBON10Y.I";
                /// <summary>
                /// Bulgaria Interbank Rate
                /// </summary>
                public const string InterbankRate = "BULGARIAINTRAT.I";
                /// <summary>
                /// Bulgaria Stock Market
                /// </summary>
                public const string StockMarket = "SOFIX.I";
                /// <summary>
                /// Bulgaria Currency
                /// </summary>
                public const string Currency = "USDBGN.I";
            }
            public static class Burundi
            {
                /// <summary>
                /// Burundi Currency
                /// </summary>
                public const string Currency = "USDBIF.I";
            }
            public static class Cambodia
            {
                /// <summary>
                /// Cambodia Currency
                /// </summary>
                public const string Currency = "USDKHR.I";
            }
            public static class Canada
            {
                /// <summary>
                /// Canada Bank Lending Rate
                /// </summary>
                public const string BankLendingRate = "CANADABANLENRAT.I";
                /// <summary>
                /// Canada Interbank Rate
                /// </summary>
                public const string InterbankRate = "CANADAINTRAT.I";
                /// <summary>
                /// Canada Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "CANFRINRDPST.I";
                /// <summary>
                /// Canada Interest Rate
                /// </summary>
                public const string InterestRate = "CCLR.I";
                /// <summary>
                /// Canada Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GCAN10YR.I";
                /// <summary>
                /// Canada Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "OECAI001.I";
                /// <summary>
                /// Canada Stock Market
                /// </summary>
                public const string StockMarket = "SPTSX.I";
                /// <summary>
                /// Canada Currency
                /// </summary>
                public const string Currency = "USDCAD.I";
            }
            public static class CapeVerde
            {
                /// <summary>
                /// CapeVerde Currency
                /// </summary>
                public const string Currency = "USDCVE.I";
            }
            public static class CaymanIslands
            {
                /// <summary>
                /// CaymanIslands Currency
                /// </summary>
                public const string Currency = "USDKYD.I";
            }
            public static class Chile
            {
                /// <summary>
                /// Chile Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "CHILEGOVBON10Y.I";
                /// <summary>
                /// Chile Interbank Rate
                /// </summary>
                public const string InterbankRate = "CHILEINTRAT.I";
                /// <summary>
                /// Chile Stock Market
                /// </summary>
                public const string StockMarket = "IGPA.I";
                /// <summary>
                /// Chile Currency
                /// </summary>
                public const string Currency = "USDCLP.I";
            }
            public static class China
            {
                /// <summary>
                /// China Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "CHINACASRESRAT.I";
                /// <summary>
                /// China Interbank Rate
                /// </summary>
                public const string InterbankRate = "CHINAINTRAT.I";
                /// <summary>
                /// China Reverse Repo Rate
                /// </summary>
                public const string ReverseRepoRate = "CHINAREVREPRAT.I";
                /// <summary>
                /// China Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GCNY10YR.I";
                /// <summary>
                /// China Stock Market
                /// </summary>
                public const string StockMarket = "SHCOMP.I";
                /// <summary>
                /// China Currency
                /// </summary>
                public const string Currency = "USDCNY.I";
            }
            public static class Colombia
            {
                /// <summary>
                /// Colombia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "COGR10Y.I";
                /// <summary>
                /// Colombia Stock Market
                /// </summary>
                public const string StockMarket = "COLCAP.I";
                /// <summary>
                /// Colombia Interbank Rate
                /// </summary>
                public const string InterbankRate = "COLOMBIAINTRAT.I";
                /// <summary>
                /// Colombia Currency
                /// </summary>
                public const string Currency = "USDCOP.I";
            }
            public static class Commodity
            {
                /// <summary>
                /// Commodity Baltic Exchange Dry Index
                /// </summary>
                public const string BalticExchangeDryIndex = "BALTIC.I";
                /// <summary>
                /// Commodity Beef
                /// </summary>
                public const string Beef = "BEEF.I";
                /// <summary>
                /// Commodity Bitumen
                /// </summary>
                public const string Bitumen = "BIT.I";
                /// <summary>
                /// Commodity Corn
                /// </summary>
                public const string Corn = "C-1.I";
                /// <summary>
                /// Commodity Cocoa
                /// </summary>
                public const string Cocoa = "CC1.I";
                /// <summary>
                /// Commodity Cheese
                /// </summary>
                public const string Cheese = "CHEESE1.I";
                /// <summary>
                /// Commodity Crude oil
                /// </summary>
                public const string CrudeOil = "CL1.I";
                /// <summary>
                /// Commodity Brent crude oil
                /// </summary>
                public const string BrentCrudeOil = "CO1.I";
                /// <summary>
                /// Commodity Coal
                /// </summary>
                public const string Coal = "COAL.I";
                /// <summary>
                /// Commodity Cobalt
                /// </summary>
                public const string Cobalt = "COBALT.I";
                /// <summary>
                /// Commodity CRB Commodity Index
                /// </summary>
                public const string CrbCommodityIndex = "CRB.I";
                /// <summary>
                /// Commodity Cotton
                /// </summary>
                public const string Cotton = "CT1.I";
                /// <summary>
                /// Commodity Ethanol
                /// </summary>
                public const string Ethanol = "DL1.I";
                /// <summary>
                /// Commodity Feeder Cattle
                /// </summary>
                public const string FeederCattle = "FC1.I";
                /// <summary>
                /// Commodity GSCI Commodity Index
                /// </summary>
                public const string GsciCommodityIndex = "GSCI.I";
                /// <summary>
                /// Commodity Copper
                /// </summary>
                public const string Copper = "HG1.I";
                /// <summary>
                /// Commodity Heating oil
                /// </summary>
                public const string HeatingOil = "HO1.I";
                /// <summary>
                /// Commodity Manganese Ore
                /// </summary>
                public const string ManganeseOre = "IMR.I";
                /// <summary>
                /// Commodity Iron Ore
                /// </summary>
                public const string IronOre = "IRONORE.I";
                /// <summary>
                /// Commodity Rubber
                /// </summary>
                public const string Rubber = "JN1.I";
                /// <summary>
                /// Commodity Orange Juice
                /// </summary>
                public const string OrangeJuice = "JO1.I";
                /// <summary>
                /// Commodity Coffee
                /// </summary>
                public const string Coffee = "KC1.I";
                /// <summary>
                /// Commodity Lumber
                /// </summary>
                public const string Lumber = "LB1.I";
                /// <summary>
                /// Commodity Live Cattle
                /// </summary>
                public const string LiveCattle = "LC1.I";
                /// <summary>
                /// Commodity Lean Hogs
                /// </summary>
                public const string LeanHogs = "LH1.I";
                /// <summary>
                /// Commodity Lithium
                /// </summary>
                public const string Lithium = "LITHIUM.I";
                /// <summary>
                /// Commodity Aluminum
                /// </summary>
                public const string Aluminum = "LMAHDS03.I";
                /// <summary>
                /// Commodity LME Index
                /// </summary>
                public const string LmeIndex = "LME.I";
                /// <summary>
                /// Commodity Molybdenum
                /// </summary>
                public const string Molybdenum = "LMMLDS03.I";
                /// <summary>
                /// Commodity Nickel
                /// </summary>
                public const string Nickel = "LMNIDS03.I";
                /// <summary>
                /// Commodity Lead
                /// </summary>
                public const string Lead = "LMPBDS03.I";
                /// <summary>
                /// Commodity Tin
                /// </summary>
                public const string Tin = "LMSNDS03.I";
                /// <summary>
                /// Commodity Zinc
                /// </summary>
                public const string Zinc = "LMZSDS03.I";
                /// <summary>
                /// Commodity Milk
                /// </summary>
                public const string Milk = "MILK.I";
                /// <summary>
                /// Commodity Naphtha
                /// </summary>
                public const string Naphtha = "NAPHTHA.I";
                /// <summary>
                /// Commodity Natural gas
                /// </summary>
                public const string NaturalGas = "NG1.I";
                /// <summary>
                /// Commodity Oat
                /// </summary>
                public const string Oat = "O-1.I";
                /// <summary>
                /// Commodity Wool
                /// </summary>
                public const string Wool = "OL1.I";
                /// <summary>
                /// Commodity Palm Oil
                /// </summary>
                public const string PalmOil = "PALMOIL.I";
                /// <summary>
                /// Commodity Propane
                /// </summary>
                public const string Propane = "PROPANE.I";
                /// <summary>
                /// Commodity RHODIUM
                /// </summary>
                public const string Rhodium = "RHODIUM.I";
                /// <summary>
                /// Commodity Rice
                /// </summary>
                public const string Rice = "RR1.I";
                /// <summary>
                /// Commodity Canola
                /// </summary>
                public const string Canola = "RS1.I";
                /// <summary>
                /// Commodity Soybeans
                /// </summary>
                public const string Soybeans = "S-1.I";
                /// <summary>
                /// Commodity Sugar
                /// </summary>
                public const string Sugar = "SB1.I";
                /// <summary>
                /// Commodity Soda Ash
                /// </summary>
                public const string SodaAsh = "SODASH.I";
                /// <summary>
                /// Commodity Neodymium
                /// </summary>
                public const string Neodymium = "SREMNDM.I";
                /// <summary>
                /// Commodity Steel
                /// </summary>
                public const string Steel = "STEEL.I";
                /// <summary>
                /// Commodity Iron Ore 62% FE
                /// </summary>
                public const string IronOre62PercentFe = "TIOC.I";
                /// <summary>
                /// Commodity Uranium
                /// </summary>
                public const string Uranium = "URANIUM.I";
                /// <summary>
                /// Commodity Wheat
                /// </summary>
                public const string Wheat = "W-1.I";
                /// <summary>
                /// Commodity Silver
                /// </summary>
                public const string Silver = "XAGUSD.I";
                /// <summary>
                /// Commodity Gold
                /// </summary>
                public const string Gold = "XAUUSD.I";
                /// <summary>
                /// Commodity Gasoline
                /// </summary>
                public const string Gasoline = "XB1.I";
                /// <summary>
                /// Commodity Palladium
                /// </summary>
                public const string Palladium = "XPDUSD.I";
                /// <summary>
                /// Commodity Platinum
                /// </summary>
                public const string Platinum = "XPTUSD.I";
            }
            public static class Comoros
            {
                /// <summary>
                /// Comoros Currency
                /// </summary>
                public const string Currency = "USDKMF.I";
            }
            public static class Congo
            {
                /// <summary>
                /// Congo Currency
                /// </summary>
                public const string Currency = "USDCDF.I";
            }
            public static class CostaRica
            {
                /// <summary>
                /// CostaRica Stock Market
                /// </summary>
                public const string StockMarket = "CRSMBCT.I";
                /// <summary>
                /// CostaRica Currency
                /// </summary>
                public const string Currency = "USDCRC.I";
            }
            public static class Croatia
            {
                /// <summary>
                /// Croatia Stock Market
                /// </summary>
                public const string StockMarket = "CRO.I";
                /// <summary>
                /// Croatia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "CROATIAGOVD10Y.I";
                /// <summary>
                /// Croatia Interbank Rate
                /// </summary>
                public const string InterbankRate = "CROATIAINTRAT.I";
                /// <summary>
                /// Croatia Currency
                /// </summary>
                public const string Currency = "USDHRV.I";
            }
            public static class Cuba
            {
                /// <summary>
                /// Cuba Currency
                /// </summary>
                public const string Currency = "USDCUC.I";
            }
            public static class Cyprus
            {
                /// <summary>
                /// Cyprus Stock Market
                /// </summary>
                public const string StockMarket = "CYSMMAPA.I";
            }
            public static class CzechRepublic
            {
                /// <summary>
                /// CzechRepublic Interbank Rate
                /// </summary>
                public const string InterbankRate = "CZECHREPUINTRAT.I";
                /// <summary>
                /// CzechRepublic Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "CZGB10YR.I";
                /// <summary>
                /// CzechRepublic Stock Market
                /// </summary>
                public const string StockMarket = "PX.I";
                /// <summary>
                /// CzechRepublic Currency
                /// </summary>
                public const string Currency = "USDCZK.I";
            }
            public static class Denmark
            {
                /// <summary>
                /// Denmark Interbank Rate
                /// </summary>
                public const string InterbankRate = "DENMARKINTRAT.I";
                /// <summary>
                /// Denmark Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GDGB10YR.I";
                /// <summary>
                /// Denmark Stock Market
                /// </summary>
                public const string StockMarket = "KFX.I";
                /// <summary>
                /// Denmark Currency
                /// </summary>
                public const string Currency = "USDDKK.I";
            }
            public static class Djibouti
            {
                /// <summary>
                /// Djibouti Currency
                /// </summary>
                public const string Currency = "USDDJF.I";
            }
            public static class DominicanRepublic
            {
                /// <summary>
                /// DominicanRepublic Currency
                /// </summary>
                public const string Currency = "USDDOP.I";
            }
            public static class Ecuador
            {
                /// <summary>
                /// Ecuador Stock Market
                /// </summary>
                public const string StockMarket = "ECU.I";
                /// <summary>
                /// Ecuador Interbank Rate
                /// </summary>
                public const string InterbankRate = "ECUADORINTRATI.I";
            }
            public static class Egypt
            {
                /// <summary>
                /// Egypt Stock Market
                /// </summary>
                public const string StockMarket = "CASE.I";
                /// <summary>
                /// Egypt Interbank Rate
                /// </summary>
                public const string InterbankRate = "EGYPTINTRAT.I";
                /// <summary>
                /// Egypt Interest Rate
                /// </summary>
                public const string InterestRate = "EGYPTINTRATE.I";
                /// <summary>
                /// Egypt Lending Rate
                /// </summary>
                public const string LendingRate = "EGYPTLENRAT.I";
                /// <summary>
                /// Egypt Currency
                /// </summary>
                public const string Currency = "USDEGP.I";
            }
            public static class Eritrea
            {
                /// <summary>
                /// Eritrea Currency
                /// </summary>
                public const string Currency = "USDERN.I";
            }
            public static class Estonia
            {
                /// <summary>
                /// Estonia Stock Market
                /// </summary>
                public const string StockMarket = "TALSE.I";
            }
            public static class Ethiopia
            {
                /// <summary>
                /// Ethiopia Currency
                /// </summary>
                public const string Currency = "USDETB.I";
            }
            public static class EuroArea
            {
                /// <summary>
                /// EuroArea Interbank Rate
                /// </summary>
                public const string InterbankRate = "EMUEVOLVINTRAT.I";
                /// <summary>
                /// EuroArea Lending Rate
                /// </summary>
                public const string LendingRate = "EUROAREALENRAT.I";
                /// <summary>
                /// EuroArea Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "EUROAREAZEWECOSENIND.I";
                /// <summary>
                /// EuroArea Stock Market
                /// </summary>
                public const string StockMarket = "SX5E.I";
            }
            public static class Fiji
            {
                /// <summary>
                /// Fiji Currency
                /// </summary>
                public const string Currency = "USDFJD.I";
            }
            public static class Finland
            {
                /// <summary>
                /// Finland Interbank Rate
                /// </summary>
                public const string InterbankRate = "FINLANDINTRAT.I";
                /// <summary>
                /// Finland Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GFIN10YR.I";
                /// <summary>
                /// Finland Stock Market
                /// </summary>
                public const string StockMarket = "HEX25.I";
            }
            public static class France
            {
                /// <summary>
                /// France Stock Market
                /// </summary>
                public const string StockMarket = "CAC.I";
                /// <summary>
                /// France Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GFRN10.I";
            }
            public static class Gambia
            {
                /// <summary>
                /// Gambia Currency
                /// </summary>
                public const string Currency = "USDGMD.I";
            }
            public static class Georgia
            {
                /// <summary>
                /// Georgia Currency
                /// </summary>
                public const string Currency = "USDGEL.I";
            }
            public static class Germany
            {
                /// <summary>
                /// Germany Stock Market
                /// </summary>
                public const string StockMarket = "DAX.I";
                /// <summary>
                /// Germany Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GDBR10.I";
                /// <summary>
                /// Germany 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "GERMANY2YNY.I";
                /// <summary>
                /// Germany Zew Economic Sentiment Index
                /// </summary>
                public const string ZewEconomicSentimentIndex = "GERMANYZEWECOSENIND.I";
                /// <summary>
                /// Germany Consumer Confidence
                /// </summary>
                public const string ConsumerConfidence = "GRCCI.I";
            }
            public static class Ghana
            {
                /// <summary>
                /// Ghana Stock Market
                /// </summary>
                public const string StockMarket = "GGSECI.I";
                /// <summary>
                /// Ghana Currency
                /// </summary>
                public const string Currency = "USDGHS.I";
            }
            public static class Greece
            {
                /// <summary>
                /// Greece Stock Market
                /// </summary>
                public const string StockMarket = "ASE.I";
                /// <summary>
                /// Greece Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GGGB10YR.I";
                /// <summary>
                /// Greece 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "GREECE2YNY.I";
            }
            public static class Guatemala
            {
                /// <summary>
                /// Guatemala Currency
                /// </summary>
                public const string Currency = "USDGTQ.I";
            }
            public static class Guinea
            {
                /// <summary>
                /// Guinea Currency
                /// </summary>
                public const string Currency = "USDGNF.I";
            }
            public static class Guyana
            {
                /// <summary>
                /// Guyana Currency
                /// </summary>
                public const string Currency = "USDGYD.I";
            }
            public static class Haiti
            {
                /// <summary>
                /// Haiti Currency
                /// </summary>
                public const string Currency = "USDHTG.I";
            }
            public static class Honduras
            {
                /// <summary>
                /// Honduras Interest Rate
                /// </summary>
                public const string InterestRate = "HONURASINTTRATE.I";
                /// <summary>
                /// Honduras Currency
                /// </summary>
                public const string Currency = "USDHNL.I";
            }
            public static class HongKong
            {
                /// <summary>
                /// HongKong Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GHKGB10Y.I";
                /// <summary>
                /// HongKong Interbank Rate
                /// </summary>
                public const string InterbankRate = "HONGKONGINTRAT.I";
                /// <summary>
                /// HongKong Stock Market
                /// </summary>
                public const string StockMarket = "HSI.I";
                /// <summary>
                /// HongKong Currency
                /// </summary>
                public const string Currency = "USDHKD.I";
            }
            public static class Hungary
            {
                /// <summary>
                /// Hungary Stock Market
                /// </summary>
                public const string StockMarket = "BUX.I";
                /// <summary>
                /// Hungary Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GHGB10YR.I";
                /// <summary>
                /// Hungary Interbank Rate
                /// </summary>
                public const string InterbankRate = "HUNGARYINTRAT.I";
                /// <summary>
                /// Hungary Currency
                /// </summary>
                public const string Currency = "USDHUF.I";
            }
            public static class Iceland
            {
                /// <summary>
                /// Iceland Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "ICELANDGOVBON10Y.I";
                /// <summary>
                /// Iceland Interbank Rate
                /// </summary>
                public const string InterbankRate = "ICELANDINTRAT.I";
                /// <summary>
                /// Iceland Stock Market
                /// </summary>
                public const string StockMarket = "ICEXI.I";
                /// <summary>
                /// Iceland Currency
                /// </summary>
                public const string Currency = "USDISK.I";
            }
            public static class India
            {
                /// <summary>
                /// India Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GIND10YR.I";
                /// <summary>
                /// India Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "INDIACASRESRAT.I";
                /// <summary>
                /// India Interest Rate
                /// </summary>
                public const string InterestRate = "RSPOYLD.I";
                /// <summary>
                /// India Stock Market
                /// </summary>
                public const string StockMarket = "SENSEX.I";
                /// <summary>
                /// India Currency
                /// </summary>
                public const string Currency = "USDINR.I";
            }
            public static class Indonesia
            {
                /// <summary>
                /// Indonesia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GIDN10YR.I";
                /// <summary>
                /// Indonesia Interbank Rate
                /// </summary>
                public const string InterbankRate = "INDONESIAINTRAT.I";
                /// <summary>
                /// Indonesia Stock Market
                /// </summary>
                public const string StockMarket = "JCI.I";
                /// <summary>
                /// Indonesia Currency
                /// </summary>
                public const string Currency = "USDIDR.I";
            }
            public static class Iraq
            {
                /// <summary>
                /// Iraq Currency
                /// </summary>
                public const string Currency = "USDIQD.I";
            }
            public static class Ireland
            {
                /// <summary>
                /// Ireland Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GIGB10YR.I";
                /// <summary>
                /// Ireland Stock Market
                /// </summary>
                public const string StockMarket = "ISEQ.I";
            }
            public static class Israel
            {
                /// <summary>
                /// Israel Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GISR10YR.I";
                /// <summary>
                /// Israel Interest Rate
                /// </summary>
                public const string InterestRate = "ISBRATE.I";
                /// <summary>
                /// Israel Interbank Rate
                /// </summary>
                public const string InterbankRate = "ISRAELINTRAT.I";
                /// <summary>
                /// Israel Stock Market
                /// </summary>
                public const string StockMarket = "TA-100.I";
                /// <summary>
                /// Israel Currency
                /// </summary>
                public const string Currency = "USDILS.I";
            }
            public static class Italy
            {
                /// <summary>
                /// Italy Stock Market
                /// </summary>
                public const string StockMarket = "FTSEMIB.I";
                /// <summary>
                /// Italy Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GBTPGR10.I";
                /// <summary>
                /// Italy 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "ITALY2YNY.I";
            }
            public static class IvoryCoast
            {
                /// <summary>
                /// IvoryCoast Currency
                /// </summary>
                public const string Currency = "USDXOF.I";
            }
            public static class Jamaica
            {
                /// <summary>
                /// Jamaica Interest Rate
                /// </summary>
                public const string InterestRate = "JAMAICAINTTRATE.I";
                /// <summary>
                /// Jamaica Stock Market
                /// </summary>
                public const string StockMarket = "JMSMX.I";
                /// <summary>
                /// Jamaica Currency
                /// </summary>
                public const string Currency = "USDJMD.I";
            }
            public static class Japan
            {
                /// <summary>
                /// Japan Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GJGB10.I";
                /// <summary>
                /// Japan 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "JAPAN2YNY.I";
                /// <summary>
                /// Japan Interbank Rate
                /// </summary>
                public const string InterbankRate = "JAPANINTRAT.I";
                /// <summary>
                /// Japan Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "JPNFRINRDPST.I";
                /// <summary>
                /// Japan Stock Market
                /// </summary>
                public const string StockMarket = "NKY.I";
                /// <summary>
                /// Japan Currency
                /// </summary>
                public const string Currency = "USDJPY.I";
            }
            public static class Jordan
            {
                /// <summary>
                /// Jordan Stock Market
                /// </summary>
                public const string StockMarket = "JOSMGNFF.I";
                /// <summary>
                /// Jordan Currency
                /// </summary>
                public const string Currency = "USDJOR.I";
            }
            public static class Kazakhstan
            {
                /// <summary>
                /// Kazakhstan Interbank Rate
                /// </summary>
                public const string InterbankRate = "KAZAKHSTANINTRAT.I";
                /// <summary>
                /// Kazakhstan Interest Rate
                /// </summary>
                public const string InterestRate = "KAZAKHSTANINTRATE.I";
                /// <summary>
                /// Kazakhstan Stock Market
                /// </summary>
                public const string StockMarket = "KZKAK.I";
                /// <summary>
                /// Kazakhstan Currency
                /// </summary>
                public const string Currency = "USDKZT.I";
            }
            public static class Kenya
            {
                /// <summary>
                /// Kenya Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "KENYAGOVBON10Y.I";
                /// <summary>
                /// Kenya Stock Market
                /// </summary>
                public const string StockMarket = "KNSMIDX.I";
                /// <summary>
                /// Kenya Currency
                /// </summary>
                public const string Currency = "USDKES.I";
            }
            public static class Kuwait
            {
                /// <summary>
                /// Kuwait Interbank Rate
                /// </summary>
                public const string InterbankRate = "KUWAITINTRAT.I";
                /// <summary>
                /// Kuwait Currency
                /// </summary>
                public const string Currency = "USDKWD.I";
            }
            public static class Kyrgyzstan
            {
                /// <summary>
                /// Kyrgyzstan Interest Rate
                /// </summary>
                public const string InterestRate = "KYRSTANINTTRATE.I";
                /// <summary>
                /// Kyrgyzstan Currency
                /// </summary>
                public const string Currency = "USDKGS.I";
            }
            public static class Laos
            {
                /// <summary>
                /// Laos Stock Market
                /// </summary>
                public const string StockMarket = "LSXC.I";
                /// <summary>
                /// Laos Currency
                /// </summary>
                public const string Currency = "USDLAK.I";
            }
            public static class Latvia
            {
                /// <summary>
                /// Latvia Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "LATVIAGOVBON10Y.I";
                /// <summary>
                /// Latvia Interbank Rate
                /// </summary>
                public const string InterbankRate = "LATVIAINTRAT.I";
                /// <summary>
                /// Latvia Stock Market
                /// </summary>
                public const string StockMarket = "RIGSE.I";
            }
            public static class Lebanon
            {
                /// <summary>
                /// Lebanon Stock Market
                /// </summary>
                public const string StockMarket = "BLOM.I";
                /// <summary>
                /// Lebanon Currency
                /// </summary>
                public const string Currency = "USDLBP.I";
            }
            public static class Lesotho
            {
                /// <summary>
                /// Lesotho Currency
                /// </summary>
                public const string Currency = "USDLSL.I";
            }
            public static class Liberia
            {
                /// <summary>
                /// Liberia Currency
                /// </summary>
                public const string Currency = "USDLRD.I";
            }
            public static class Libya
            {
                /// <summary>
                /// Libya Currency
                /// </summary>
                public const string Currency = "USDLYD.I";
            }
            public static class Lithuania
            {
                /// <summary>
                /// Lithuania Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "LITHUANIAGOVBON10Y.I";
                /// <summary>
                /// Lithuania Stock Market
                /// </summary>
                public const string StockMarket = "VILSE.I";
            }
            public static class Luxembourg
            {
                /// <summary>
                /// Luxembourg Stock Market
                /// </summary>
                public const string StockMarket = "LUXXX.I";
            }
            public static class Macau
            {
                /// <summary>
                /// Macau Interbank Rate
                /// </summary>
                public const string InterbankRate = "MACAOINTRAT.I";
                /// <summary>
                /// Macau Currency
                /// </summary>
                public const string Currency = "USDMOP.I";
            }
            public static class Macedonia
            {
                /// <summary>
                /// Macedonia Stock Market
                /// </summary>
                public const string StockMarket = "MBI.I";
                /// <summary>
                /// Macedonia Currency
                /// </summary>
                public const string Currency = "USDMKD.I";
            }
            public static class Madagascar
            {
                /// <summary>
                /// Madagascar Currency
                /// </summary>
                public const string Currency = "USDMGA.I";
            }
            public static class Malawi
            {
                /// <summary>
                /// Malawi Currency
                /// </summary>
                public const string Currency = "USDMWK.I";
            }
            public static class Malaysia
            {
                /// <summary>
                /// Malaysia Stock Market
                /// </summary>
                public const string StockMarket = "FBMKLCI.I";
                /// <summary>
                /// Malaysia Interbank Rate
                /// </summary>
                public const string InterbankRate = "MALAYSIAINTRAT.I";
                /// <summary>
                /// Malaysia Interest Rate
                /// </summary>
                public const string InterestRate = "MAOPRATE.I";
                /// <summary>
                /// Malaysia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "MGIY10Y.I";
                /// <summary>
                /// Malaysia Currency
                /// </summary>
                public const string Currency = "USDMYR.I";
            }
            public static class Maldives
            {
                /// <summary>
                /// Maldives Currency
                /// </summary>
                public const string Currency = "USDMVR.I";
            }
            public static class Malta
            {
                /// <summary>
                /// Malta Stock Market
                /// </summary>
                public const string StockMarket = "MALTEX.I";
            }
            public static class Mauritius
            {
                /// <summary>
                /// Mauritius Stock Market
                /// </summary>
                public const string StockMarket = "SEMDEX.I";
                /// <summary>
                /// Mauritius Currency
                /// </summary>
                public const string Currency = "USDMUR.I";
            }
            public static class Mexico
            {
                /// <summary>
                /// Mexico Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GMXN10YR.I";
                /// <summary>
                /// Mexico Stock Market
                /// </summary>
                public const string StockMarket = "MEXBOL.I";
                /// <summary>
                /// Mexico Interest Rate
                /// </summary>
                public const string InterestRate = "MXONBR.I";
                /// <summary>
                /// Mexico Currency
                /// </summary>
                public const string Currency = "USDMXN.I";
            }
            public static class Moldova
            {
                /// <summary>
                /// Moldova Currency
                /// </summary>
                public const string Currency = "USDMDL.I";
            }
            public static class Mongolia
            {
                /// <summary>
                /// Mongolia Stock Market
                /// </summary>
                public const string StockMarket = "MSETOP.I";
                /// <summary>
                /// Mongolia Currency
                /// </summary>
                public const string Currency = "USDMNT.I";
            }
            public static class Morocco
            {
                /// <summary>
                /// Morocco Stock Market
                /// </summary>
                public const string StockMarket = "MOSENEW.I";
                /// <summary>
                /// Morocco Currency
                /// </summary>
                public const string Currency = "USDMAD.I";
            }
            public static class Mozambique
            {
                /// <summary>
                /// Mozambique Currency
                /// </summary>
                public const string Currency = "USDMZN.I";
            }
            public static class Myanmar
            {
                /// <summary>
                /// Myanmar Currency
                /// </summary>
                public const string Currency = "USDMMK.I";
            }
            public static class Namibia
            {
                /// <summary>
                /// Namibia Stock Market
                /// </summary>
                public const string StockMarket = "FTN098.I";
                /// <summary>
                /// Namibia Currency
                /// </summary>
                public const string Currency = "USDNAD.I";
            }
            public static class Nepal
            {
                /// <summary>
                /// Nepal Currency
                /// </summary>
                public const string Currency = "USDNPR.I";
            }
            public static class Netherlands
            {
                /// <summary>
                /// Netherlands Stock Market
                /// </summary>
                public const string StockMarket = "AEX.I";
                /// <summary>
                /// Netherlands Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GNTH10YR.I";
            }
            public static class NewCaledonia
            {
                /// <summary>
                /// NewCaledonia Currency
                /// </summary>
                public const string Currency = "USDXPF.I";
            }
            public static class NewZealand
            {
                /// <summary>
                /// NewZealand Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GNZGB10.I";
                /// <summary>
                /// NewZealand Global Dairy Trade Price Index
                /// </summary>
                public const string GlobalDairyTradePriceIndex = "NEWZEALANGDTPI.I";
                /// <summary>
                /// NewZealand Currency
                /// </summary>
                public const string Currency = "NZDUSD.I";
                /// <summary>
                /// NewZealand Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "NZLFRINRDPST.I";
                /// <summary>
                /// NewZealand Interest Rate
                /// </summary>
                public const string InterestRate = "NZOCRS.I";
                /// <summary>
                /// NewZealand Stock Market
                /// </summary>
                public const string StockMarket = "NZSE50FG.I";
            }
            public static class Nicaragua
            {
                /// <summary>
                /// Nicaragua Currency
                /// </summary>
                public const string Currency = "USDNIO.I";
            }
            public static class Nigeria
            {
                /// <summary>
                /// Nigeria Stock Market
                /// </summary>
                public const string StockMarket = "NGSEINDX.I";
                /// <summary>
                /// Nigeria Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "NIGERIACASRESRAT.I";
                /// <summary>
                /// Nigeria Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "NIGERIAGOVBON10Y.I";
                /// <summary>
                /// Nigeria Interbank Rate
                /// </summary>
                public const string InterbankRate = "NIGERIAINTRAT.I";
                /// <summary>
                /// Nigeria Currency
                /// </summary>
                public const string Currency = "USDNGN.I";
            }
            public static class NorthKorea
            {
                /// <summary>
                /// NorthKorea Currency
                /// </summary>
                public const string Currency = "NORTHKORECUR.I";
            }
            public static class Norway
            {
                /// <summary>
                /// Norway Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GNOR10YR.I";
                /// <summary>
                /// Norway Interbank Rate
                /// </summary>
                public const string InterbankRate = "NORWAYINTRAT.I";
                /// <summary>
                /// Norway Stock Market
                /// </summary>
                public const string StockMarket = "OSEAX.I";
                /// <summary>
                /// Norway Currency
                /// </summary>
                public const string Currency = "USDNOK.I";
            }
            public static class Oman
            {
                /// <summary>
                /// Oman Stock Market
                /// </summary>
                public const string StockMarket = "MSM30.I";
                /// <summary>
                /// Oman Currency
                /// </summary>
                public const string Currency = "USDOMR.I";
            }
            public static class Pakistan
            {
                /// <summary>
                /// Pakistan Stock Market
                /// </summary>
                public const string StockMarket = "KSE100.I";
                /// <summary>
                /// Pakistan Interest Rate
                /// </summary>
                public const string InterestRate = "PAPRSBP.I";
                /// <summary>
                /// Pakistan Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "PKIB10YR.I";
                /// <summary>
                /// Pakistan Currency
                /// </summary>
                public const string Currency = "USDPKR.I";
            }
            public static class Panama
            {
                /// <summary>
                /// Panama Stock Market
                /// </summary>
                public const string StockMarket = "BVPSBVPS.I";
                /// <summary>
                /// Panama Currency
                /// </summary>
                public const string Currency = "USDPAB.I";
            }
            public static class PapuaNewGuinea
            {
                /// <summary>
                /// PapuaNewGuinea Currency
                /// </summary>
                public const string Currency = "USDPGK.I";
            }
            public static class Paraguay
            {
                /// <summary>
                /// Paraguay Currency
                /// </summary>
                public const string Currency = "USDPYG.I";
            }
            public static class Peru
            {
                /// <summary>
                /// Peru Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GRPE10Y.I";
                /// <summary>
                /// Peru Stock Market
                /// </summary>
                public const string StockMarket = "IGBVL.I";
                /// <summary>
                /// Peru Interbank Rate
                /// </summary>
                public const string InterbankRate = "PERUINTRAT.I";
                /// <summary>
                /// Peru Interest Rate
                /// </summary>
                public const string InterestRate = "PRRRONUS.I";
                /// <summary>
                /// Peru Currency
                /// </summary>
                public const string Currency = "USDPEN.I";
            }
            public static class Philippines
            {
                /// <summary>
                /// Philippines Stock Market
                /// </summary>
                public const string StockMarket = "PCOMP.I";
                /// <summary>
                /// Philippines Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "PHILIPPINEGOVBON10Y.I";
                /// <summary>
                /// Philippines Interbank Rate
                /// </summary>
                public const string InterbankRate = "PHILIPPINEINTRAT.I";
                /// <summary>
                /// Philippines Interest Rate
                /// </summary>
                public const string InterestRate = "PHILIPPINESINTRATE.I";
                /// <summary>
                /// Philippines Currency
                /// </summary>
                public const string Currency = "USDPHP.I";
            }
            public static class Poland
            {
                /// <summary>
                /// Poland Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "POGB10YR.I";
                /// <summary>
                /// Poland Interbank Rate
                /// </summary>
                public const string InterbankRate = "POLANDINTRAT.I";
                /// <summary>
                /// Poland Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "POLFRINRDPST.I";
                /// <summary>
                /// Poland Currency
                /// </summary>
                public const string Currency = "USDPLN.I";
                /// <summary>
                /// Poland Stock Market
                /// </summary>
                public const string StockMarket = "WIG.I";
            }
            public static class Portugal
            {
                /// <summary>
                /// Portugal Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSPT10YR.I";
                /// <summary>
                /// Portugal 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "PORTUGAL2YNY.I";
                /// <summary>
                /// Portugal Stock Market
                /// </summary>
                public const string StockMarket = "PSI20.I";
            }
            public static class Qatar
            {
                /// <summary>
                /// Qatar Stock Market
                /// </summary>
                public const string StockMarket = "DSM.I";
                /// <summary>
                /// Qatar Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "QATARGOVBON10Y.I";
                /// <summary>
                /// Qatar Reverse Repo Rate
                /// </summary>
                public const string ReverseRepoRate = "QATARREVREPRAT.I";
                /// <summary>
                /// Qatar Currency
                /// </summary>
                public const string Currency = "USDQAR.I";
            }
            public static class Romania
            {
                /// <summary>
                /// Romania Stock Market
                /// </summary>
                public const string StockMarket = "BET.I";
                /// <summary>
                /// Romania Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "ROMANIAGOVBON10Y.I";
                /// <summary>
                /// Romania Interbank Rate
                /// </summary>
                public const string InterbankRate = "ROMANIAINTRAT.I";
                /// <summary>
                /// Romania Currency
                /// </summary>
                public const string Currency = "USDRON.I";
            }
            public static class Russia
            {
                /// <summary>
                /// Russia Stock Market
                /// </summary>
                public const string StockMarket = "INDEXCF.I";
                /// <summary>
                /// Russia Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "RUGE10Y.I";
                /// <summary>
                /// Russia Central Bank Balance Sheet
                /// </summary>
                public const string CentralBankBalanceSheet = "RUSSIACENBANBALSHE.I";
                /// <summary>
                /// Russia Currency
                /// </summary>
                public const string Currency = "USDRUB.I";
            }
            public static class Rwanda
            {
                /// <summary>
                /// Rwanda Currency
                /// </summary>
                public const string Currency = "USDRWF.I";
            }
            public static class SaoTomeAndPrincipe
            {
                /// <summary>
                /// SaoTomeAndPrincipe Currency
                /// </summary>
                public const string Currency = "USDSTD.I";
            }
            public static class SaudiArabia
            {
                /// <summary>
                /// SaudiArabia Stock Market
                /// </summary>
                public const string StockMarket = "SASEIDX.I";
                /// <summary>
                /// SaudiArabia Reverse Repo Rate
                /// </summary>
                public const string ReverseRepoRate = "SAUDIARABREVREPRAT.I";
                /// <summary>
                /// SaudiArabia Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "SAUFRINRDPST.I";
                /// <summary>
                /// SaudiArabia Currency
                /// </summary>
                public const string Currency = "USDSAR.I";
            }
            public static class Serbia
            {
                /// <summary>
                /// Serbia Stock Market
                /// </summary>
                public const string StockMarket = "BELEX15.I";
                /// <summary>
                /// Serbia Interbank Rate
                /// </summary>
                public const string InterbankRate = "SERBIAINTRAT.I";
                /// <summary>
                /// Serbia Interest Rate
                /// </summary>
                public const string InterestRate = "SERRBIAINTTRATE.I";
                /// <summary>
                /// Serbia Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "SRBFRINRDPST.I";
                /// <summary>
                /// Serbia Currency
                /// </summary>
                public const string Currency = "USDSRB.I";
            }
            public static class Seychelles
            {
                /// <summary>
                /// Seychelles Currency
                /// </summary>
                public const string Currency = "USDSCR.I";
            }
            public static class SierraLeone
            {
                /// <summary>
                /// SierraLeone Currency
                /// </summary>
                public const string Currency = "USDSLL.I";
            }
            public static class Singapore
            {
                /// <summary>
                /// Singapore Stock Market
                /// </summary>
                public const string StockMarket = "FSSTI.I";
                /// <summary>
                /// Singapore Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "MASB10Y.I";
                /// <summary>
                /// Singapore Interbank Rate
                /// </summary>
                public const string InterbankRate = "SINGAPOREINTRAT.I";
                /// <summary>
                /// Singapore Currency
                /// </summary>
                public const string Currency = "USDSGD.I";
            }
            public static class Slovakia
            {
                /// <summary>
                /// Slovakia Stock Market
                /// </summary>
                public const string StockMarket = "SKSM.I";
                /// <summary>
                /// Slovakia Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "SLOVAKIAGOVBON10Y.I";
            }
            public static class Slovenia
            {
                /// <summary>
                /// Slovenia Stock Market
                /// </summary>
                public const string StockMarket = "SBITOP.I";
                /// <summary>
                /// Slovenia Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "SLOVENIAGOVBON10Y.I";
            }
            public static class Somalia
            {
                /// <summary>
                /// Somalia Currency
                /// </summary>
                public const string Currency = "USDSOS.I";
            }
            public static class SouthAfrica
            {
                /// <summary>
                /// SouthAfrica Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSAB10YR.I";
                /// <summary>
                /// SouthAfrica Stock Market
                /// </summary>
                public const string StockMarket = "JALSH.I";
                /// <summary>
                /// SouthAfrica Interbank Rate
                /// </summary>
                public const string InterbankRate = "SOUTHAFRIINTRAT.I";
                /// <summary>
                /// SouthAfrica Currency
                /// </summary>
                public const string Currency = "USDZAR.I";
            }
            public static class SouthKorea
            {
                /// <summary>
                /// SouthKorea Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GVSK10YR.I";
                /// <summary>
                /// SouthKorea Stock Market
                /// </summary>
                public const string StockMarket = "KOSPI.I";
                /// <summary>
                /// SouthKorea Interbank Rate
                /// </summary>
                public const string InterbankRate = "SOUTHKOREINTRAT.I";
                /// <summary>
                /// SouthKorea Currency
                /// </summary>
                public const string Currency = "USDKRW.I";
            }
            public static class Spain
            {
                /// <summary>
                /// Spain Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSPG10YR.I";
                /// <summary>
                /// Spain Stock Market
                /// </summary>
                public const string StockMarket = "IBEX.I";
                /// <summary>
                /// Spain Interbank Rate
                /// </summary>
                public const string InterbankRate = "SPAININTRAT.I";
            }
            public static class SriLanka
            {
                /// <summary>
                /// SriLanka Stock Market
                /// </summary>
                public const string StockMarket = "CSEALL.I";
                /// <summary>
                /// SriLanka Interest Rate
                /// </summary>
                public const string InterestRate = "SRI-LANKAINTRATE.I";
                /// <summary>
                /// SriLanka Cash Reserve Ratio
                /// </summary>
                public const string CashReserveRatio = "SRILANKACASRESRAT.I";
                /// <summary>
                /// SriLanka Interbank Rate
                /// </summary>
                public const string InterbankRate = "SRILANKAINTRAT.I";
                /// <summary>
                /// SriLanka Lending Rate
                /// </summary>
                public const string LendingRate = "SRILANKALENRAT.I";
                /// <summary>
                /// SriLanka Currency
                /// </summary>
                public const string Currency = "USDLKR.I";
            }
            public static class Sudan
            {
                /// <summary>
                /// Sudan Currency
                /// </summary>
                public const string Currency = "USDSDG.I";
            }
            public static class Suriname
            {
                /// <summary>
                /// Suriname Currency
                /// </summary>
                public const string Currency = "USDSRD.I";
            }
            public static class Swaziland
            {
                /// <summary>
                /// Swaziland Currency
                /// </summary>
                public const string Currency = "USDSZL.I";
            }
            public static class Sweden
            {
                /// <summary>
                /// Sweden Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSGB10YR.I";
                /// <summary>
                /// Sweden Stock Market
                /// </summary>
                public const string StockMarket = "OMX.I";
                /// <summary>
                /// Sweden Interbank Rate
                /// </summary>
                public const string InterbankRate = "SWEDENINTRAT.I";
                /// <summary>
                /// Sweden Currency
                /// </summary>
                public const string Currency = "USDSEK.I";
            }
            public static class Switzerland
            {
                /// <summary>
                /// Switzerland Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GSWISS10.I";
                /// <summary>
                /// Switzerland Stock Market
                /// </summary>
                public const string StockMarket = "SMI.I";
                /// <summary>
                /// Switzerland Interbank Rate
                /// </summary>
                public const string InterbankRate = "SWITZERLANINTRAT.I";
                /// <summary>
                /// Switzerland Currency
                /// </summary>
                public const string Currency = "USDCHF.I";
            }
            public static class Syria
            {
                /// <summary>
                /// Syria Currency
                /// </summary>
                public const string Currency = "USDSYP.I";
            }
            public static class Taiwan
            {
                /// <summary>
                /// Taiwan Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "TAIWANGOVBON10Y.I";
                /// <summary>
                /// Taiwan Interbank Rate
                /// </summary>
                public const string InterbankRate = "TAIWANINTRAT.I";
                /// <summary>
                /// Taiwan Interest Rate
                /// </summary>
                public const string InterestRate = "TAIWANIR.I";
                /// <summary>
                /// Taiwan Stock Market
                /// </summary>
                public const string StockMarket = "TWSE.I";
                /// <summary>
                /// Taiwan Currency
                /// </summary>
                public const string Currency = "USDTWD.I";
            }
            public static class Tajikistan
            {
                /// <summary>
                /// Tajikistan Currency
                /// </summary>
                public const string Currency = "USDTJS.I";
            }
            public static class Tanzania
            {
                /// <summary>
                /// Tanzania Stock Market
                /// </summary>
                public const string StockMarket = "DARSDSEI.I";
                /// <summary>
                /// Tanzania Currency
                /// </summary>
                public const string Currency = "USDTZS.I";
            }
            public static class Thailand
            {
                /// <summary>
                /// Thailand Interest Rate
                /// </summary>
                public const string InterestRate = "BTRR1DAY.I";
                /// <summary>
                /// Thailand Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GVTL10YR.I";
                /// <summary>
                /// Thailand Stock Market
                /// </summary>
                public const string StockMarket = "SET50.I";
                /// <summary>
                /// Thailand Interbank Rate
                /// </summary>
                public const string InterbankRate = "THAILANDINTRAT.I";
                /// <summary>
                /// Thailand Currency
                /// </summary>
                public const string Currency = "USDTHB.I";
            }
            public static class TrinidadAndTobago
            {
                /// <summary>
                /// TrinidadAndTobago Currency
                /// </summary>
                public const string Currency = "USDTTD.I";
            }
            public static class Tunisia
            {
                /// <summary>
                /// Tunisia Stock Market
                /// </summary>
                public const string StockMarket = "TUSISE.I";
                /// <summary>
                /// Tunisia Currency
                /// </summary>
                public const string Currency = "USDTND.I";
            }
            public static class Turkey
            {
                /// <summary>
                /// Turkey Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "TURKEYGOVBON10Y.I";
                /// <summary>
                /// Turkey Interbank Rate
                /// </summary>
                public const string InterbankRate = "TURKEYINTRAT.I";
                /// <summary>
                /// Turkey Currency
                /// </summary>
                public const string Currency = "USDTRY.I";
                /// <summary>
                /// Turkey Stock Market
                /// </summary>
                public const string StockMarket = "XU100.I";
            }
            public static class Turkmenistan
            {
                /// <summary>
                /// Turkmenistan Currency
                /// </summary>
                public const string Currency = "USDTMT.I";
            }
            public static class Uganda
            {
                /// <summary>
                /// Uganda Currency
                /// </summary>
                public const string Currency = "USDUGX.I";
            }
            public static class Ukraine
            {
                /// <summary>
                /// Ukraine Stock Market
                /// </summary>
                public const string StockMarket = "PFTS.I";
                /// <summary>
                /// Ukraine Interbank Rate
                /// </summary>
                public const string InterbankRate = "UKRAINEINTRAT.I";
                /// <summary>
                /// Ukraine Currency
                /// </summary>
                public const string Currency = "USDUAH.I";
            }
            public static class UnitedArabEmirates
            {
                /// <summary>
                /// UnitedArabEmirates Stock Market
                /// </summary>
                public const string StockMarket = "ADSMI.I";
                /// <summary>
                /// UnitedArabEmirates Interbank Rate
                /// </summary>
                public const string InterbankRate = "UNITEDARAINTRAT.I";
                /// <summary>
                /// UnitedArabEmirates Currency
                /// </summary>
                public const string Currency = "USDAED.I";
            }
            public static class UnitedKingdom
            {
                /// <summary>
                /// UnitedKingdom Currency
                /// </summary>
                public const string Currency = "GBPUSD.I";
                /// <summary>
                /// UnitedKingdom Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "GUKG10.I";
                /// <summary>
                /// UnitedKingdom Stock Market
                /// </summary>
                public const string StockMarket = "UKX.I";
                /// <summary>
                /// UnitedKingdom 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "UNITEDKIN2YNY.I";
                /// <summary>
                /// UnitedKingdom Interbank Rate
                /// </summary>
                public const string InterbankRate = "UNITEDKININTRAT.I";
            }
            public static class UnitedStates
            {
                /// <summary>
                /// UnitedStates Currency
                /// </summary>
                public const string Currency = "DXY.I";
                /// <summary>
                /// UnitedStates Interest Rate
                /// </summary>
                public const string InterestRate = "FDTR.I";
                /// <summary>
                /// UnitedStates Stock Market
                /// </summary>
                public const string StockMarket = "INDU.I";
                /// <summary>
                /// UnitedStates 2 Year Note Yield
                /// </summary>
                public const string TwoYearNoteYield = "UNITEDSTA2YEANOTYIE.I";
                /// <summary>
                /// UnitedStates Economic Optimism Index
                /// </summary>
                public const string EconomicOptimismIndex = "UNITEDSTAECOOPTIND.I";
                /// <summary>
                /// UnitedStates Foreign Direct Investment
                /// </summary>
                public const string ForeignDirectInvestment = "UNITEDSTAFORDIRINV.I";
                /// <summary>
                /// UnitedStates Interbank Rate
                /// </summary>
                public const string InterbankRate = "UNITEDSTAINTRAT.I";
                /// <summary>
                /// UnitedStates Nahb Housing Market Index
                /// </summary>
                public const string NahbHousingMarketIndex = "UNITEDSTANAHHOUMARIN.I";
                /// <summary>
                /// UnitedStates NY Empire State Manufacturing Index
                /// </summary>
                public const string NyEmpireStateManufacturingIndex = "UNITEDSTANYEMPSTAMAN.I";
                /// <summary>
                /// UnitedStates Redbook Index
                /// </summary>
                public const string RedbookIndex = "UNITEDSTAREDIND.I";
                /// <summary>
                /// UnitedStates Government Bond 10Y
                /// </summary>
                public const string GovernmentBond10y = "USGG10YR.I";
                /// <summary>
                /// UnitedStates Crude Oil Rigs
                /// </summary>
                public const string CrudeOilRigs = "USOILRIGS.I";
            }
            public static class Uruguay
            {
                /// <summary>
                /// Uruguay Currency
                /// </summary>
                public const string Currency = "USDURY.I";
            }
            public static class Uzbekistan
            {
                /// <summary>
                /// Uzbekistan Currency
                /// </summary>
                public const string Currency = "USDUZS.I";
            }
            public static class Venezuela
            {
                /// <summary>
                /// Venezuela Stock Market
                /// </summary>
                public const string StockMarket = "IBVC.I";
                /// <summary>
                /// Venezuela Currency
                /// </summary>
                public const string Currency = "USDVES.I";
                /// <summary>
                /// Venezuela Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "VENEZUELAGOVBON10Y.I";
                /// <summary>
                /// Venezuela Deposit Interest Rate
                /// </summary>
                public const string DepositInterestRate = "VENFRINRDPST.I";
            }
            public static class Vietnam
            {
                /// <summary>
                /// Vietnam Currency
                /// </summary>
                public const string Currency = "USDVND.I";
                /// <summary>
                /// Vietnam Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "VIETNAMGOVBON10Y.I";
                /// <summary>
                /// Vietnam Interbank Rate
                /// </summary>
                public const string InterbankRate = "VIETNAMINTRAT.I";
                /// <summary>
                /// Vietnam Stock Market
                /// </summary>
                public const string StockMarket = "VNINDEX.I";
            }
            public static class Yemen
            {
                /// <summary>
                /// Yemen Currency
                /// </summary>
                public const string Currency = "USDYER.I";
            }
            public static class Zambia
            {
                /// <summary>
                /// Zambia Currency
                /// </summary>
                public const string Currency = "USDZMK.I";
                /// <summary>
                /// Zambia Government Bond 10y
                /// </summary>
                public const string GovernmentBond10y = "ZAMBIAGOVBON10Y.I";
                /// <summary>
                /// Zambia Interest Rate
                /// </summary>
                public const string InterestRate = "ZAMMBIAINTTRATE.I";
            }
            public static class Zimbabwe
            {
                /// <summary>
                /// Zimbabwe Stock Market
                /// </summary>
                public const string StockMarket = "INDZI.I";
            }
        }
    }
}