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

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Helper class for the AssetClassification's StockType field <see cref="AssetClassification.StockType"/>
    /// </summary>
    public static class StockType
    {
        public static int AggressiveGrowth = 1;
        public static int ClassicGrowth = 2;
        public static int Cyclicals = 3;
        public static int Distressed = 4;
        public static int HardAsset = 5;
        public static int HighYield = 6;
        public static int SlowGrowth = 7;
        public static int SpeculativeGrowth = 8;
    }

    /// <summary>
    /// Helper class for the AssetClassification's StyleBox field <see cref="AssetClassification.StyleBox"/>.
    /// </summary>
    public static class StyleBox
    {
        public static int LargeValue = 1;
        public static int LargeCore = 2;
        public static int LargeGrowth = 3;
        public static int MidValue = 4;
        public static int MidCore = 5;
        public static int MidGrowth = 6;
        public static int SmallValue = 7;
        public static int SmallCore = 8;
        public static int SmallGrowth = 9;
    }

    /// <summary>
    /// Helper class for the AssetClassification's MorningstarEconomySphereCode field <see cref="AssetClassification.MorningstarEconomySphereCode"/>.
    /// </summary>
    public static class MorningstarEconomySphereCode
    {
        public static int Cyclical = 1;
        public static int Defensive = 2;
        public static int Sensitive = 3;
    }

    /// <summary>
    /// Helper class for the AssetClassification's MorningstarSectorCode field <see cref="AssetClassification.MorningstarSectorCode"/>.
    /// </summary>
    public static class MorningstarSectorCode
    {
        public static int BasicMaterials = 101;
        public static int ConsumerCyclical = 102;
        public static int FinancialServices = 103;
        public static int RealEstate = 104;
        public static int ConsumerDefensive = 205;
        public static int Healthcare = 206;
        public static int Utilities = 207;
        public static int CommunicationServices = 308;
        public static int Energy = 309;
        public static int Industrials = 310;
        public static int Technology = 311;
    }

    /// <summary>
    /// Helper class for the AssetClassification's MorningstarIndustryGroupCode field <see cref="AssetClassification.MorningstarIndustryGroupCode"/>.
    /// </summary>
    public static class MorningstarIndustryGroupCode
    {
        public static int Agriculture = 10101;
        public static int BuildingMaterials = 10102;
        public static int Chemicals = 10103;
        public static int Coal = 10104;
        public static int ForestProducts = 10105;
        public static int MetalsAndMining = 10106;
        public static int Steel = 10107;
        public static int AdvertisingAndMarketingServices = 10208;
        public static int Autos = 10209;
        public static int Entertainment = 10210;
        public static int HomebuildingAndConstruction = 10211;
        public static int ManufacturingApparelAndFurniture = 10212;
        public static int PackagingAndContainers = 10213;
        public static int PersonalServices = 10214;
        public static int Publishing = 10215;
        public static int Restaurants = 10216;
        public static int RetailApparelAndSpecialty = 10217;
        public static int TravelAndLeisure = 10218;
        public static int AssetManagement = 10319;
        public static int Banks = 10320;
        public static int BrokersAndExchanges = 10321;
        public static int CreditServices = 10322;
        public static int Insurance = 10323;
        public static int InsuranceLife = 10324;
        public static int InsurancePropertyAndCasualty = 10325;
        public static int InsuranceSpecialty = 10326;
        public static int RealEstateServices = 10427;
        public static int REITs = 10428;
        public static int BeveragesAlcoholic = 20529;
        public static int BeveragesNonAlcoholic = 20530;
        public static int ConsumerPackagedGoods = 20531;
        public static int Education = 20532;
        public static int RetailDefensive = 20533;
        public static int TobaccoProducts = 20534;
        public static int Biotechnology = 20635;
        public static int DrugManufacturers = 20636;
        public static int HealthCarePlans = 20637;
        public static int HealthCareProviders = 20638;
        public static int MedicalDevices = 20639;
        public static int MedicalDiagnosticsAndResearch = 20640;
        public static int MedicalDistribution = 20641;
        public static int MedicalInstrumentsAndEquipment = 20642;
        public static int UtilitiesIndependentPowerProducers = 20743;
        public static int UtilitiesRegulated = 20744;
        public static int CommunicationServices = 30845;
        public static int OilAndGasDrilling = 30946;
        public static int OilAndGasExplorationAndProduction = 30947;
        public static int OilAndGasIntegrated = 30948;
        public static int OilAndGasMidstream = 30949;
        public static int OilAndGasRefiningAndMarketing = 30950;
        public static int OilAndGasServices = 30951;
        public static int AerospaceAndDefense = 31052;
        public static int Airlines = 31053;
        public static int BusinessServices = 31054;
        public static int Conglomerates = 31055;
        public static int ConsultingAndOutsourcing = 31056;
        public static int EmploymentServices = 31057;
        public static int EngineeringAndConstruction = 31058;
        public static int FarmAndConstructionMachinery = 31059;
        public static int IndustrialDistribution = 31060;
        public static int IndustrialProducts = 31061;
        public static int TransportationAndLogistics = 31062;
        public static int TruckManufacturing = 31063;
        public static int WasteManagement = 31064;
        public static int ApplicationSoftware = 31165;
        public static int CommunicationEquipment = 31166;
        public static int ComputerHardware = 31167;
        public static int OnlineMedia = 31168;
        public static int Semiconductors = 31169;
    }

    /// <summary>
    /// Helper class for the AssetClassification's MorningstarIndustryCode field <see cref="AssetClassification.MorningstarIndustryCode"/>.
    /// </summary>
    public static class MorningstarIndustryCode
    {
        public static int AgriculturalInputs = 10101001;
        public static int BuildingMaterials = 10102002;
        public static int Chemicals = 10103003;
        public static int SpecialtyChemicals = 10103004;
        public static int Coal = 10104005;
        public static int LumberAndWoodProduction = 10105006;
        public static int PaperAndPaperProducts = 10105007;
        public static int Aluminum = 10106008;
        public static int Copper = 10106009;
        public static int Gold = 10106010;
        public static int IndustrialMetalsAndMinerals = 10106011;
        public static int Silver = 10106012;
        public static int Steel = 10107013;
        public static int AdvertisingAgencies = 10208014;
        public static int MarketingServices = 10208015;
        public static int AutoAndTruckDealerships = 10209016;
        public static int AutoManufacturers = 10209017;
        public static int AutoParts = 10209018;
        public static int RecreationalVehicles = 10209019;
        public static int RubberAndPlastics = 10209020;
        public static int BroadcastingRadio = 10210021;
        public static int BroadcastingTV = 10210022;
        public static int MediaDiversified = 10210023;
        public static int ResidentialConstruction = 10211024;
        public static int TextileManufacturing = 10211025;
        public static int ApparelManufacturing = 10212026;
        public static int FootwearAndAccessories = 10212027;
        public static int HomeFurnishingsAndFixtures = 10212028;
        public static int PackagingAndContainers = 10213029;
        public static int PersonalServices = 10214030;
        public static int Publishing = 10215031;
        public static int Restaurants = 10216032;
        public static int ApparelStores = 10217033;
        public static int DepartmentStores = 10217034;
        public static int HomeImprovementStores = 10217035;
        public static int LuxuryGoods = 10217036;
        public static int SpecialtyRetail = 10217037;
        public static int Gambling = 10218038;
        public static int Leisure = 10218039;
        public static int Lodging = 10218040;
        public static int ResortsAndCasinos = 10218041;
        public static int AssetManagement = 10319042;
        public static int BanksGlobal = 10320043;
        public static int BanksRegionalAfrica = 10320044;
        public static int BanksRegionalAsia = 10320045;
        public static int BanksRegionalAustralia = 10320046;
        public static int BanksRegionalCanada = 10320047;
        public static int BanksRegionalEurope = 10320048;
        public static int BanksRegionalLatinAmerica = 10320049;
        public static int BanksRegionalUS = 10320050;
        public static int SavingsAndCooperativeBanks = 10320051;
        public static int SpecialtyFinance = 10320052;
        public static int CapitalMarkets = 10321053;
        public static int FinancialExchanges = 10321054;
        public static int InsuranceBrokers = 10321055;
        public static int CreditServices = 10322056;
        public static int InsuranceDiversified = 10323057;
        public static int InsuranceLife = 10324058;
        public static int InsurancePropertyAndCasualty = 10325059;
        public static int InsuranceReinsurance = 10326060;
        public static int InsuranceSpecialty = 10326061;
        public static int RealEstateGeneral = 10427062;
        public static int RealEstateServices = 10427063;
        public static int REITDiversified = 10428064;
        public static int REITHealthcareFacilities = 10428065;
        public static int REITHotelAndMotel = 10428066;
        public static int REITIndustrial = 10428067;
        public static int REITOffice = 10428068;
        public static int REITResidential = 10428069;
        public static int REITRetail = 10428070;
        public static int BeveragesBrewers = 20529071;
        public static int BeveragesWineriesAndDistilleries = 20529072;
        public static int BeveragesSoftDrinks = 20530073;
        public static int Confectioners = 20531074;
        public static int FarmProducts = 20531075;
        public static int HouseholdAndPersonalProducts = 20531076;
        public static int PackagedFoods = 20531077;
        public static int EducationAndTrainingServices = 20532078;
        public static int DiscountStores = 20533079;
        public static int PharmaceuticalRetailers = 20533080;
        public static int FoodDistribution = 20533081;
        public static int GroceryStores = 20533082;
        public static int Tobacco = 20534083;
        public static int Biotechnology = 20635084;
        public static int DrugManufacturersMajor = 20636085;
        public static int DrugManufacturersSpecialtyAndGeneric = 20636086;
        public static int HealthCarePlans = 20637087;
        public static int LongTermCareFacilities = 20638088;
        public static int MedicalCare = 20638089;
        public static int MedicalDevices = 20639090;
        public static int DiagnosticsAndResearch = 20640091;
        public static int MedicalDistribution = 20641092;
        public static int MedicalInstrumentsAndSupplies = 20642093;
        public static int UtilitiesIndependentPowerProducers = 20743094;
        public static int UtilitiesDiversified = 20744095;
        public static int UtilitiesRegulatedElectric = 20744096;
        public static int UtilitiesRegulatedGas = 20744097;
        public static int UtilitiesRegulatedWater = 20744098;
        public static int PayTV = 30845099;
        public static int TelecomServices = 30845100;
        public static int OilAndGasDrilling = 30946101;
        public static int OilAndGasEAndP = 30947102;
        public static int OilAndGasIntegrated = 30948103;
        public static int OilAndGasMidstream = 30949104;
        public static int OilAndGasRefiningAndMarketing = 30950105;
        public static int OilAndGasEquipmentAndServices = 30951106;
        public static int AerospaceAndDefense = 31052107;
        public static int Airlines = 31053108;
        public static int BusinessServices = 31054109;
        public static int Conglomerates = 31055110;
        public static int RentalAndLeasingServices = 31056111;
        public static int SecurityAndProtectionServices = 31056112;
        public static int StaffingAndOutsourcingServices = 31057113;
        public static int EngineeringAndConstruction = 31058114;
        public static int InfrastructureOperations = 31058115;
        public static int FarmAndConstructionEquipment = 31059116;
        public static int IndustrialDistribution = 31060117;
        public static int BusinessEquipment = 31061118;
        public static int DiversifiedIndustrials = 31061119;
        public static int MetalFabrication = 31061120;
        public static int PollutionAndTreatmentControls = 31061121;
        public static int ToolsAndAccessories = 31061122;
        public static int AirportsAndAirServices = 31062123;
        public static int IntegratedShippingAndLogistics = 31062124;
        public static int Railroads = 31062125;
        public static int ShippingAndPorts = 31062126;
        public static int Trucking = 31062127;
        public static int TruckManufacturing = 31063128;
        public static int WasteManagement = 31064129;
        public static int ElectronicGamingAndMultimedia = 31165130;
        public static int HealthInformationServices = 31165131;
        public static int InformationTechnologyServices = 31165132;
        public static int SoftwareApplication = 31165133;
        public static int SoftwareInfrastructure = 31165134;
        public static int CommunicationEquipment = 31166135;
        public static int ComputerDistribution = 31167136;
        public static int ComputerSystems = 31167137;
        public static int ConsumerElectronics = 31167138;
        public static int ContractManufacturers = 31167139;
        public static int DataStorage = 31167140;
        public static int ElectronicComponents = 31167141;
        public static int ElectronicsDistribution = 31167142;
        public static int ScientificAndTechnicalInstruments = 31167143;
        public static int InternetContentAndInformation = 31168144;
        public static int SemiconductorEquipmentAndMaterials = 31169145;
        public static int SemiconductorMemory = 31169146;
        public static int Semiconductors = 31169147;
        public static int Solar = 31169148;
    }
}