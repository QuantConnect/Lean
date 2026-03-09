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
        /// <summary>
        /// Companies whose revenues and earnings have both been growing significantly faster than
        /// the general economy.
        /// </summary>
        public readonly static int AggressiveGrowth = 1;

        /// <summary>
        /// Companies that are growing respectably faster than the general economy, and often pay a
        /// steady dividend. They tend to be mature and solidly profitable businesses.
        /// </summary>
        public readonly static int ClassicGrowth = 2;

        /// <summary>
        /// Companies in the cyclicals and durables sectors, except those in the three types below.
        /// The profits of cyclicals tend to rise and fall with the general economy.
        /// </summary>
        public readonly static int Cyclicals = 3;

        /// <summary>
        /// Companies that have had consistently declining cash flows and earnings over the past
        /// three years, and/or very high debt.
        /// </summary>
        public readonly static int Distressed = 4;

        /// <summary>
        /// Companies that deal in assets such as oil, metals, and real estate, which tend to do
        /// well in inflationary environments.
        /// </summary>
        public readonly static int HardAsset = 5;

        /// <summary>
        /// Companies that have dividend yields at least twice the average for large-cap stocks.
        /// They tend to be mature, slow-growing companies.
        /// </summary>
        public readonly static int HighYield = 6;

        /// <summary>
        /// Companies that have shown slow revenue and earnings growth (typically less than the rate
        /// of GDP growth) over at least three years.
        /// </summary>
        public readonly static int SlowGrowth = 7;

        /// <summary>
        /// Companies that have shown strong revenue growth but slower or spotty earnings growth.
        /// Very small or young companies also tend to fall into this class.
        /// </summary>
        public readonly static int SpeculativeGrowth = 8;
    }

    /// <summary>
    /// Helper class for the AssetClassification's StyleBox field <see cref="AssetClassification.StyleBox"/>.
    /// For stocks and stock funds, it classifies securities according to market capitalization and growth and value factor
    /// </summary>
    /// <remarks>Please refer to https://www.morningstar.com/InvGlossary/morningstar_style_box.aspx </remarks>
    public static class StyleBox
    {
#pragma warning disable 1591
        public readonly static int LargeValue = 1;
        public readonly static int LargeCore = 2;
        public readonly static int LargeGrowth = 3;
        public readonly static int MidValue = 4;
        public readonly static int MidCore = 5;
        public readonly static int MidGrowth = 6;
        public readonly static int SmallValue = 7;
        public readonly static int SmallCore = 8;
        public readonly static int SmallGrowth = 9;
#pragma warning restore 1591
    }

    /// <summary>
    /// Helper class for the AssetClassification's MorningstarEconomySphereCode field <see cref="AssetClassification.MorningstarEconomySphereCode"/>.
    /// </summary>
    public static class MorningstarEconomySphereCode
    {
        /// <summary>
        /// The cyclical Super Sector includes industries significantly affected by economic shifts.
        /// When the economy is prosperous, these industries tend to expand, and when the economy is
        /// in a downturn they tend to shrink. In general, the stocks in these industries have betas
        /// of greater than 1.
        /// </summary>
        public readonly static int Cyclical = 1;

        /// <summary>
        /// The defensive Super Sector includes industries that are relatively immune to economic
        /// cycles. These industries provide services that consumers require in both good and bad
        /// times, such as healthcare and utilities. In general, the stocks in these industries have
        /// betas of less than 1.
        /// </summary>
        public readonly static int Defensive = 2;

        /// <summary>
        /// The sensitive Super Sector includes industries that ebb and flow with the overall
        /// economy, but not severely. Sensitive industries fall between defensive and cyclical, as
        /// they are not immune to a poor economy, but they also may not be as severely affected as
        /// industries in the cyclical Super Sector. In general, the stocks in these industries have
        /// betas that are close to 1.
        /// </summary>
        public readonly static int Sensitive = 3;
    }

    /// <summary>
    /// Helper class for the AssetClassification's MorningstarSectorCode field <see cref="AssetClassification.MorningstarSectorCode"/>.
    /// </summary>
    public static class MorningstarSectorCode
    {
        /// <summary>
        /// Companies that manufacture chemicals, building materials, and paper products. This
        /// sector also includes companies engaged in commodities exploration and processing.
        /// </summary>
        public readonly static int BasicMaterials = 101;

        /// <summary>
        /// This sector includes retail stores, auto and auto-parts manufacturers, restaurants,
        /// lodging facilities, specialty retail and travel companies.
        /// </summary>
        public readonly static int ConsumerCyclical = 102;

        /// <summary>
        /// Companies that provide financial services include banks, savings and loans, asset
        /// management companies, credit services, investment brokerage firms, and insurance companies.
        /// </summary>
        public readonly static int FinancialServices = 103;

        /// <summary>
        /// This sector includes companies that develop, acquire, manage, and operate real estate properties.
        /// </summary>
        public readonly static int RealEstate = 104;

        /// <summary>
        /// Companies that manufacture food, beverages, household and personal products, packaging,
        /// or tobacco. Also includes companies that provide services such as education and training services.
        /// </summary>
        public readonly static int ConsumerDefensive = 205;

        /// <summary>
        /// This sector includes biotechnology, pharmaceuticals, research services, home healthcare,
        /// hospitals, long-term-care facilities, and medical equipment and supplies. Also include
        /// pharmaceutical retailers and companies which provide health information services.
        /// </summary>
        public readonly static int Healthcare = 206;

        /// <summary>
        /// Electric, gas, and water utilities.
        /// </summary>
        public readonly static int Utilities = 207;

        /// <summary> Companies that provide communication services using fixed-line networks or
        /// those that provide wireless access and services. Also includes companies that provide
        /// advertising &amp; marketing services, entertainment content and services, as well as
        /// interactive media and content provider over internet or through software. </summary>
        public readonly static int CommunicationServices = 308;

        /// <summary>
        /// Companies that produce or refine oil and gas, oilfield-services and equipment companies,
        /// and pipeline operators. This sector also includes companies that mine thermal coal and uranium.
        /// </summary>
        public readonly static int Energy = 309;

        /// <summary>
        /// Companies that manufacture machinery, hand-held tools, and industrial products. This
        /// sector also includes aerospace and defense firms as well as companies engaged in
        /// transportation services.
        /// </summary>
        public readonly static int Industrials = 310;

        /// <summary>
        /// Companies engaged in the design, development, and support of computer operating systems
        /// and applications. This sector also includes companies that make computer equipment, data
        /// storage products, networking products, semiconductors, and components.
        /// </summary>
        public readonly static int Technology = 311;
    }

    /// <summary>
    /// Helper class for the AssetClassification's MorningstarIndustryGroupCode field <see cref="AssetClassification.MorningstarIndustryGroupCode"/>.
    /// </summary>
    public static class MorningstarIndustryGroupCode
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public readonly static int Agriculture = 10110;
        public readonly static int BuildingMaterials = 10120;
        public readonly static int Chemicals = 10130;
        public readonly static int ForestProducts = 10140;
        public readonly static int MetalsAndMining = 10150;
        public readonly static int Steel = 10160;
        public readonly static int VehiclesAndParts = 10200;
        public readonly static int Furnishings = 10220;
        public readonly static int FixturesAndAppliances = 10220;
        public readonly static int HomebuildingAndConstruction = 10230;
        public readonly static int ManufacturingApparelAndAccessories = 10240;
        public readonly static int PackagingAndContainers = 10250;
        public readonly static int PersonalServices = 10260;
        public readonly static int Restaurants = 10270;
        public readonly static int RetailCyclical = 10280;
        public readonly static int TravelAndLeisure = 10290;
        public readonly static int AssetManagement = 10310;
        public readonly static int Banks = 10320;
        public readonly static int CapitalMarkets = 10330;
        public readonly static int Insurance = 10340;
        public readonly static int DiversifiedFinancialServices = 10350;
        public readonly static int CreditServices = 10360;
        public readonly static int RealEstate = 10410;
        public readonly static int REITs = 10420;
        public readonly static int BeveragesAlcoholic = 20510;
        public readonly static int BeveragesNonAlcoholic = 20520;
        public readonly static int ConsumerPackagedGoods = 20525;
        public readonly static int Education = 20540;
        public readonly static int RetailDefensive = 20550;
        public readonly static int TobaccoProducts = 20560;
        public readonly static int Biotechnology = 20610;
        public readonly static int DrugManufacturers = 20620;
        public readonly static int HealthcarePlans = 20630;
        public readonly static int HealthcareProvidersAndServices = 20645;
        public readonly static int MedicalDevicesAndInstruments = 20650;
        public readonly static int MedicalDiagnosticsAndResearch = 20660;
        public readonly static int MedicalDistribution = 20670;
        public readonly static int UtilitiesIndependentPowerProducers = 20710;
        public readonly static int UtilitiesRegulated = 20720;
        public readonly static int TelecommunicationServices = 30810;
        public readonly static int MediaDiversified = 30820;
        public readonly static int InteractiveMedia = 30830;
        public readonly static int OilAndGas = 30910;
        public readonly static int OtherEnergySources = 30920;
        public readonly static int AerospaceAndDefense = 31010;
        public readonly static int BusinessServices = 31020;
        public readonly static int Conglomerates = 31030;
        public readonly static int Construction = 31040;
        public readonly static int FarmAndHeavyConstructionMachinery = 31050;
        public readonly static int IndustrialDistribution = 31060;
        public readonly static int IndustrialProducts = 31070;
        public readonly static int Transportation = 31080;
        public readonly static int WasteManagement = 31090;
        public readonly static int Software = 31110;
        public readonly static int Hardware = 31120;
        public readonly static int Semiconductors = 31130;
#pragma warning restore CS1591 
    }

    /// <summary>
    /// Helper class for the AssetClassification's MorningstarIndustryCode field <see cref="AssetClassification.MorningstarIndustryCode"/>.
    /// </summary>
    public static class MorningstarIndustryCode
    {
        /// <summary>
        /// Companies that manufacture nitrogenous and phosphatic fertilizers, pesticides, seed, and
        /// other agricultural chemical products.
        /// </summary>
        public readonly static int AgriculturalInputs = 10110010;

        /// <summary>
        /// Companies that manufacture construction materials, including stone, clay, and brick
        /// products, cement, lime, gypsum, and concrete and other construction products. Excludes
        /// companies that manufacture finished and semi finished building products.
        /// </summary>
        public readonly static int BuildingMaterials = 10120010;

        /// <summary>
        /// Chemical manufacturing companies engaged in diverse chemical and chemical-related
        /// operations, and basic chemicals manufacturing.
        /// </summary>
        public readonly static int Chemicals = 10130010;

        /// <summary>
        /// Companies that use base chemicals to produce value-added chemicals that are used in a
        /// variety of products. Includes companies that produce polishes, adhesives and sealants,
        /// explosives, printing ink, paint, carbon black, acids, repellants, and cleaning solutions.
        /// </summary>
        public readonly static int SpecialtyChemicals = 10130020;

        /// <summary>
        /// Companies that grow timber, mill lumber, and manufacture wood and wood products for construction.
        /// </summary>
        public readonly static int LumberAndWoodProduction = 10140010;

        /// <summary>
        /// Companies that manufacture and market paper and paper-related products from wood pulp
        /// and other fibers. Excludes companies that produce paper packaging products and are
        /// classified in the packaging and containers industry.
        /// </summary>
        public readonly static int PaperAndPaperProducts = 10140020;

        /// <summary>
        /// Companies that produce, refine, market, and distribute aluminum and related
        /// aluminum-based products.
        /// </summary>
        public readonly static int Aluminum = 10150010;

        /// <summary>
        /// Companies engaged in the exploration, mining, smelting, and refining of copper ores and
        /// related copper products.
        /// </summary>
        public readonly static int Copper = 10150020;

        /// <summary>
        /// Companies that mine, refine, produce, smelt, and mill industrial ores, including copper,
        /// lead, zinc, radium, vanadium, nickel, tin, titanium, and other related materials.
        /// </summary>
        public readonly static int OtherIndustrialMetalsAndMining = 10150030;

        /// <summary>
        /// Companies principally engaged in gold exploration, mining, processing, extraction, and smelting.
        /// </summary>
        public readonly static int Gold = 10150040;

        /// <summary>
        /// Companies principally engaged in silver exploration, mining, processing, extraction, and smelting.
        /// </summary>
        public readonly static int Silver = 10150050;

        /// <summary>
        /// Companies that mine, refine, produce, smelt, and mill precious metals, including silver,
        /// platinum, palladium, diamond, and other related minerals.
        /// </summary>
        public readonly static int OtherPreciousMetalsAndMining = 10150060;

        /// <summary>
        /// Companies that produce coking coal.
        /// </summary>
        public readonly static int CokingCoal = 10160010;

        /// <summary>
        /// Companies that produce steel plates, steel sheets, bar and rod materials, structural
        /// steel, steel pipes and tubes, and stainless steel.
        /// </summary>
        public readonly static int Steel = 10160020;

        /// <summary>
        /// Companies engaged in the specialty retail of new and used automobiles, trucks, and other
        /// vehicles through the operation and/or franchising of dealerships.
        /// </summary>
        public readonly static int AutoAndTruckDealerships = 10200010;

        /// <summary>
        /// Leading worldwide designers and manufacturers of passenger cars and trucks.
        /// </summary>
        public readonly static int AutoManufacturers = 10200020;

        /// <summary>
        /// Companies that manufacture motor vehicle parts and accessories, including rubber and
        /// plastic products, such as tires and inner tubes, but do not manufacture complete car bodies.
        /// </summary>
        public readonly static int AutoParts = 10200030;

        /// <summary>
        /// Companies that manufacture recreational vehicles, including self-contained motor homes,
        /// campers, all-terrain vehicles, travel/camper trailers, and snowmobiles.
        /// </summary>
        public readonly static int RecreationalVehicles = 10200040;

        /// <summary>
        /// Companies that manufacture and market wooden, metal, and upholstered furniture,
        /// mattresses, bedsprings, lighting fixtures, wooden flooring, wallpaper, and household
        /// products, such as utensils, cutlery, tableware, and appliances.
        /// </summary>
        public readonly static int Furnishings = 10220010;

        /// <summary>
        /// Companies that manufacture and market wooden, metal, and upholstered furniture,
        /// mattresses, bedsprings, lighting fixtures, wooden flooring, wallpaper, and household
        /// products, such as utensils, cutlery, tableware, and appliances.
        /// </summary>
        public readonly static int FixturesAndAppliances = 10220010;

        /// <summary>
        /// Companies that build and renovate residential buildings, such as single-family houses,
        /// apartments, and hotels.
        /// </summary>
        public readonly static int ResidentialConstruction = 10230010;

        /// <summary>
        /// Companies that manufacture and mill textile products, including apparel fabrics, bedding
        /// and accessories, home furnishing fabrics, carpets and rugs, curtains and drapes, and
        /// other milled textile products.
        /// </summary>
        public readonly static int TextileManufacturing = 10240010;

        /// <summary>
        /// Companies that manufacture, design, market, source, and sell all lines of clothing for
        /// men, women, and children. Also includes companies that manufacture or distribute
        /// buttons, buckles, and other apparel parts.
        /// </summary>
        public readonly static int ApparelManufacturing = 10240020;

        /// <summary>
        /// Companies that manufacture, design, market, and sell lines of footwear and luggage,
        /// handbags and purses, belts, and other leather goods.
        /// </summary>
        public readonly static int FootwearAndAccessories = 10240030;

        /// <summary>
        /// Companies that manufacture and market paper, plastic, metal, and glass packaging
        /// products, including corrugated boxes, cardboard containers, bottles, cans, and plastic
        /// foam and containers.
        /// </summary>
        public readonly static int PackagingAndContainers = 10250010;

        /// <summary>
        /// Companies that provide services of a personal nature or that are particularly structured
        /// for the individual or group being served, including veterinary services, funeral and
        /// cemetery services, child care services, portrait and photo services, tax preparation and
        /// legal services, and other miscellaneous personal services.
        /// </summary>
        public readonly static int PersonalServices = 10260010;

        /// <summary>
        /// Companies that own, operate, and franchise full-service restaurants that engage in the
        /// retail sale of prepared food and drink.
        /// </summary>
        public readonly static int Restaurants = 10270010;

        /// <summary>
        /// Companies engaged in the retail sale of clothing, shoes, and accessories.
        /// </summary>
        public readonly static int ApparelRetail = 10280010;

        /// <summary>
        /// Companies engaged in the retail sale of a diverse mix of merchandise, emphasizing
        /// fashion apparel and accessories, home furnishings, electronics, and cosmetics.
        /// </summary>
        public readonly static int DepartmentStores = 10280020;

        /// <summary>
        /// Companies engaged in the retail sale of home improvement items, such as lumber, carpets,
        /// hardware and other building materials, plants and other garden supplies, and various
        /// other items for the home.
        /// </summary>
        public readonly static int HomeImprovementRetail = 10280030;

        /// <summary>
        /// Companies engaged in the specialty retail of luxury items, including jewelry, watches,
        /// crystal, clothing, handbags, and accessories.
        /// </summary>
        public readonly static int LuxuryGoods = 10280040;

        /// <summary>
        /// Companies engaged in the online retail sale of a diverse mix of merchandise. Excludes
        /// companies that target the travel industry and are classified in travel services.
        /// </summary>
        public readonly static int InternetRetail = 10280050;

        /// <summary>
        /// Companies engaged in the specialty retail of various goods and products not covered in a
        /// specific industry group. This group includes retailers such as bookstores, office-supply
        /// stores, gas stations, pawn shops, novelty shops, auto-parts stores, electronics stores,
        /// home furnishing stores, sporting goods stores, toy and hobby stores, music and video
        /// stores, and many other miscellaneous retailers.
        /// </summary>
        public readonly static int SpecialtyRetail = 10280060;

        /// <summary>
        /// Companies that own, operate, or manage lawful gaming activities and events, such as
        /// horse and dog racing, online gaming, bingo, and video lottery, as well as companies that
        /// supply products or services to gaming operators. It excludes companies operating casinos.
        /// </summary>
        public readonly static int Gambling = 10290010;

        /// <summary>
        /// Companies that manufacture, design, market, and sell bicycles, sporting goods,
        /// photographic equipment, recreational equipment, toys, and other leisure products or services.
        /// </summary>
        public readonly static int Leisure = 10290020;

        /// <summary>
        /// Companies that develop, manage, own, and operate lodging facilities, including motels,
        /// extended-stay and full-service hotels, and economy hotels and inns.
        /// </summary>
        public readonly static int Lodging = 10290030;

        /// <summary>
        /// Companies that own, operate, and manage resort properties, including beach clubs,
        /// time-share properties, and luxury resort hotels and that conduct casino gaming operations.
        /// </summary>
        public readonly static int ResortsAndCasinos = 10290040;

        /// <summary>
        /// Companies that offer travel-related products or services, including online travel services.
        /// </summary>
        public readonly static int TravelServices = 10290050;

        /// <summary>
        /// Investment management firms offering diversified services such as asset administration,
        /// investment advice, portfolio or mutual fund management, money management, and venture capital.
        /// </summary>
        public readonly static int AssetManagement = 10310010;

        /// <summary>
        /// Global, diverse financial institutions serving the corporate and consumer needs of
        /// retail banking, investment banking, trust management, credit cards and mortgage banking.
        /// </summary>
        public readonly static int BanksDiversified = 10320010;

        /// <summary>
        /// Regional, diverse financial institutions serving the corporate, government, and consumer
        /// needs of retail banking, investment banking, trust management, credit cards, mortgage
        /// banking, savings and loan associations, building societies, cooperative banks, and homestead.
        /// </summary>
        public readonly static int BanksRegional = 10320020;

        /// <summary>
        /// Companies that originate, purchase, sell, and service home mortgage and equity loans.
        /// </summary>
        public readonly static int MortgageFinance = 10320030;

        /// <summary>
        /// Large, major investment houses offering investment banking, merchant banking,
        /// underwriting, brokerage, research, advisory, and trading services to broad-based
        /// national and international markets.
        /// </summary>
        public readonly static int CapitalMarkets = 10330010;

        /// <summary>
        /// Companies that operate security exchanges, including companies that offer financial data
        /// such as ratings, investment research, and other research solutions.
        /// </summary>
        public readonly static int FinancialDataAndStockExchanges = 10330020;

        /// <summary>
        /// Companies that underwrite, market, and distribute life insurance and related products to
        /// individuals and families.
        /// </summary>
        public readonly static int InsuranceLife = 10340010;

        /// <summary>
        /// Companies that underwrite, market, and distribute fire, marine, and casualty insurance
        /// for property and other tangible assets.
        /// </summary>
        public readonly static int InsurancePropertyAndCasualty = 10340020;

        /// <summary>
        /// Companies that underwrite and sell reinsurance.
        /// </summary>
        public readonly static int InsuranceReinsurance = 10340030;

        /// <summary>
        /// Companies that underwrite, market, and distribute accident and health, sickness,
        /// mortgage, and other specialty or supplemental insurance to individuals and families.
        /// </summary>
        public readonly static int InsuranceSpecialty = 10340040;

        /// <summary>
        /// Companies acting primarily as agents or intermediaries in creating insurance contracts
        /// between clients and insurance companies.
        /// </summary>
        public readonly static int InsuranceBrokers = 10340050;

        /// <summary>
        /// Insurance companies with diversified interests in life, health, and property and
        /// casualty insurance.
        /// </summary>
        public readonly static int InsuranceDiversified = 10340060;

        /// <summary>
        /// A development-stage company with no or minimal revenue. Includes capital pool, blank
        /// check, shell, and holding companies.
        /// </summary>
        public readonly static int ShellCompanies = 10350010;

        /// <summary>
        /// Companies that provide financial services, including banking, insurance, and capital
        /// markets, but with no dominant business line or source of revenue.
        /// </summary>
        public readonly static int FinancialConglomerates = 10350020;

        /// <summary>
        /// Companies that extend credit and make loans to individuals and businesses through credit
        /// cards, installment loans, student loans, and business loans that are associated with
        /// other consumer and business credit instruments.
        /// </summary>
        public readonly static int CreditServices = 10360010;

        /// <summary>
        /// Companies that develop real estate and same properties held as inventory, or sold to
        /// others after development, with no specific portfolio composition.
        /// </summary>
        public readonly static int RealEstateDevelopment = 10410010;

        /// <summary>
        /// Companies that operate, manage, and lease real property with no specific portfolio
        /// composition. Includes real estate services like brokers and agents but excludes
        /// companies classified in the real estate â€“ development industry.
        /// </summary>
        public readonly static int RealEstateServices = 10410020;

        /// <summary>
        /// Companies engaged in multiple real estate activities, including development, sales,
        /// management, and related services. Excludes companies classified in real estate
        /// development and real estate services.
        /// </summary>
        public readonly static int RealEstateDiversified = 10410030;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the development, acquisition,
        /// management, and disposition of healthcare properties, including long-term-care
        /// facilities, acute-care and rehab hospitals, psychiatric facilities, and substance-abuse centers.
        /// </summary>
        public readonly static int REITHealthcareFacilities = 10420010;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the development, acquisition,
        /// management, and disposition of lodging properties, including full- and limited-service
        /// hotels and motels.
        /// </summary>
        public readonly static int REITHotelAndMotel = 10420020;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the development, acquisition,
        /// management, and disposition of industrial properties, including bulk warehouses,
        /// self-storage facilities, distribution facilities, and other light industrial facilities.
        /// </summary>
        public readonly static int REITIndustrial = 10420030;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the development, acquisition,
        /// management, and disposition of office properties, including office buildings, complexes,
        /// and centers.
        /// </summary>
        public readonly static int REITOffice = 10420040;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the development, acquisition,
        /// management, and disposition of residential properties, including apartments, multifamily
        /// and single-family mortgage loans, manufactured housing, mobile-home parks, and other
        /// residential properties.
        /// </summary>
        public readonly static int REITResidential = 10420050;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the development, acquisition,
        /// management, and disposition of retail properties, including community shopping centers,
        /// factory outlet shopping centers, enclosed shopping malls, strip centers, restaurants,
        /// and other retail properties.
        /// </summary>
        public readonly static int REITRetail = 10420060;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the acquisition, management,
        /// and disposition of mortgage-backed securities. Also includes companies that provide
        /// financing for income-producing real estate by purchasing or originating mortgages and
        /// mortgage-backed securities; and earns income from the interest on these investments.
        /// </summary>
        public readonly static int REITMortgage = 10420070;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the acquisition, management,
        /// and disposition of properties not classified elsewhere. Includes trusts that operate,
        /// lease, and invest in telecom towers, advertising spaces, timberland properties, and
        /// other properties not classified elsewhere.
        /// </summary>
        public readonly static int REITSpecialty = 10420080;

        /// <summary>
        /// Self-administered real estate investment trusts engaged in the acquisition, management,
        /// and disposition of diversified property holdings, with no specific portfolio composition.
        /// </summary>
        public readonly static int REITDiversified = 10420090;

        /// <summary>
        /// Companies that manufacture, sell, and distribute malt-based beverages, beers, and ales.
        /// </summary>
        public readonly static int BeveragesBrewers = 20510010;

        /// <summary>
        /// Companies that manufacture, sell, and distribute distilled liquors and wine.
        /// </summary>
        public readonly static int BeveragesWineriesAndDistilleries = 20510020;

        /// <summary>
        /// Companies that manufacture, sell, and distribute soft drinks, carbonated and spring
        /// water, fruit juices, and other nonalcoholic beverages.
        /// </summary>
        public readonly static int BeveragesNonAlcoholic = 20520010;

        /// <summary>
        /// Companies that manufacture and refine raw sugar, syrup or finished cane and beet sugar,
        /// candy and chewing gum, chocolate, and cocoa products.
        /// </summary>
        public readonly static int Confectioners = 20525010;

        /// <summary>
        /// Companies that produce, raise, and grow agricultural and farm-based food products,
        /// including fruits, vegetables, other crop products, cattle, and eggs. Also includes
        /// seafood products.
        /// </summary>
        public readonly static int FarmProducts = 20525020;

        /// <summary>
        /// Companies that manufacture and market soaps and other detergents, polishing and
        /// sanitation goods and produce glycerin from vegetable and animal fats and oils. Also
        /// includes companies that manufacture and market personal-care products, including
        /// perfume, cosmetics, and other toilet preparations, infant and adult sanitary paper
        /// products, shaving razors and blades.
        /// </summary>
        public readonly static int HouseholdAndPersonalProducts = 20525030;

        /// <summary>
        /// Companies that process and package food products, including frozen foods, grain
        /// products, canned foods, snack foods, health supplements, vitamins, and pet products.
        /// </summary>
        public readonly static int PackagedFoods = 20525040;

        /// <summary>
        /// Companies that provide educational and training services, including college and
        /// junior-college courses, higher-education programs, technical and vocational training,
        /// and other education services.
        /// </summary>
        public readonly static int EducationAndTrainingServices = 20540010;

        /// <summary>
        /// Companies engaged in the retail sale of a variety of merchandise at low and discounted prices.
        /// </summary>
        public readonly static int DiscountStores = 20550010;

        /// <summary>
        /// Companies primarily engaged in the distribution and supply of food products, including
        /// packaged goods, meat, dairy products, frozen foods, and related groceries to grocery
        /// stores, restaurants, and other food-service centers.
        /// </summary>
        public readonly static int FoodDistribution = 20550020;

        /// <summary>
        /// Companies engaged in the retail sale of groceries, including dry goods, meat, produce,
        /// frozen foods, dairy products, seafood, deli/bakery, and nonfood items.
        /// </summary>
        public readonly static int GroceryStores = 20550030;

        /// <summary>
        /// Companies that manufacture and market cigarettes, e-cigarettes, snuff, cigars, chewing
        /// tobacco, and all other tobacco products.
        /// </summary>
        public readonly static int Tobacco = 20560010;

        /// <summary>
        /// Biotech and biopharmaceutical companies engaged in research, discovery, development, and
        /// production of innovative drug and drug-related technologies.
        /// </summary>
        public readonly static int Biotechnology = 20610010;

        /// <summary>
        /// Major, global pharmaceutical manufacturers offering a broad and diverse line of drug and
        /// healthcare products; industry leaders that have made a significant commitment to the
        /// research and development of a long pipeline of drugs.
        /// </summary>
        public readonly static int DrugManufacturersGeneral = 20620010;

        /// <summary>
        /// Companies engaged in development and discovery of branded forms of drug, drug-related
        /// products, generic drug products, and animal-related drugs. Also includes companies that
        /// cultivate, process, and farm cannabis or marijuana.
        /// </summary>
        public readonly static int DrugManufacturersSpecialtyAndGeneric = 20620020;

        /// <summary>
        /// Companies that offer a wide variety of managed health products and services, including
        /// HMOs, PPOs, and other health insurance plans, and are largely U.S.-domiciled.
        /// </summary>
        public readonly static int HealthcarePlans = 20630010;

        /// <summary>
        /// Companies that provide medical services through the ownership and operation of
        /// hospitals, and other auxiliary healthcare-related services, including healthcare
        /// staffing and ambulatory services. Also, companies that operate long-term-care
        /// facilities, such as assisted-living centers, nursing and retirement homes,
        /// substance-abuse centers, and centers for rehabilitation services.
        /// </summary>
        public readonly static int MedicalCareFacilities = 20645010;

        /// <summary>
        /// Companies engaged in the retail sale of prescription drugs and patent medicines as well
        /// as a number of related lines, such as cosmetics, toiletries, and novelty merchandise.
        /// </summary>
        public readonly static int PharmaceuticalRetailers = 20645020;

        /// <summary>
        /// Companies that develop and provide comprehensive physician practice management systems
        /// and software for hospitals, medical practices, and managed-care organizations.
        /// </summary>
        public readonly static int HealthInformationServices = 20645030;

        /// <summary>
        /// Companies that develop, manufacture, and market medical and surgical equipment and
        /// machinery, including orthopedic products, respiratory care equipment, x-ray equipment,
        /// laser systems, implants, pacemakers, and other major medical machines and apparatus.
        /// </summary>
        public readonly static int MedicalDevices = 20650010;

        /// <summary>
        /// Companies that develop, design, manufacture, and market medical and dental instruments
        /// and supplies, including wheelchairs, catheters, needles, syringes, eyewear and eyecare
        /// products, and other health-related supplies.
        /// </summary>
        public readonly static int MedicalInstrumentsAndSupplies = 20650020;

        /// <summary>
        /// Companies that provide laboratory testing services through imaging and other diagnostic
        /// services to the medical industry.
        /// </summary>
        public readonly static int DiagnosticsAndResearch = 20660010;

        /// <summary>
        /// Companies primarily engaged in the distribution and supply of medical instruments and
        /// supplies, ophthalmic goods, and other health-related items to the medical and healthcare industry.
        /// </summary>
        public readonly static int MedicalDistribution = 20670010;

        /// <summary>
        /// Companies that own and operate merchant power generation facilities and sell electricity
        /// into retail and wholesale markets.
        /// </summary>
        public readonly static int UtilitiesIndependentPowerProducers = 20710010;

        /// <summary>
        /// Companies that generate, produce, or transmit electric energy from renewable sources,
        /// including hydropower, wind, geothermal, biomass, solar, tidal, and wave.
        /// </summary>
        public readonly static int UtilitiesRenewable = 20710020;

        /// <summary>
        /// Companies that distribute water for sale, including water-treatment companies.
        /// </summary>
        public readonly static int UtilitiesRegulatedWater = 20720010;

        /// <summary>
        /// Companies that generate, transmit, or distribute electric energy for sale.
        /// </summary>
        public readonly static int UtilitiesRegulatedElectric = 20720020;

        /// <summary>
        /// Companies that transmit, store, or distribute natural gas.
        /// </summary>
        public readonly static int UtilitiesRegulatedGas = 20720030;

        /// <summary>
        /// Companies engaged in the regulated generation, transmission, or distribution of
        /// electricity and natural gas, merchant power generation facilities, and energy marketing operations.
        /// </summary>
        public readonly static int UtilitiesDiversified = 20720040;

        /// <summary>
        /// Companies that provide local, national, international, and long-distance phone services,
        /// as well as companies offering wireless services. Also includes companies that provide
        /// services for faxing, prepaid phone cards, pay phones, and directory assistance, or that
        /// provide Internet access services.
        /// </summary>
        public readonly static int TelecomServices = 30810010;

        /// <summary>
        /// Companies engaged in full-service advertising operations, including the planning,
        /// creating, producing, and placing of advertising in media such as TV, radio, and print.
        /// Also includes companies providing marketing services, including outdoor advertising,
        /// promotional materials, direct-mail services, digital marketing, event management
        /// services, and marketing research services.
        /// </summary>
        public readonly static int AdvertisingAgencies = 30820010;

        /// <summary>
        /// Companies that publish periodicals, such as magazines, comic books, trade journals,
        /// books, pamphlets, e-publications, and newspapers.
        /// </summary>
        public readonly static int Publishing = 30820020;

        /// <summary>
        /// Companies that own or operate radio broadcast stations and provide and produce radio
        /// programming services, television programming services, and television broadcast
        /// stations. Also includes companies providing Internet-based video on demand and
        /// pay-per-view programming services.
        /// </summary>
        public readonly static int Broadcasting = 30820030;

        /// <summary>
        /// Companies primarily operating with diversified holdings in movies, television, and other
        /// media-based entertainment. Also includes companies that produce and distribute motion
        /// pictures, television programmers, video, and the operation of movie theaters; and
        /// provide cable television services.
        /// </summary>
        public readonly static int Entertainment = 30820040;

        /// <summary>
        /// Companies that provide content, Internet navigation services, and reference guide
        /// information for the World Wide Web through its platforms, including social media, search
        /// engines, and networking platform companies.
        /// </summary>
        public readonly static int InternetContentAndInformation = 30830010;

        /// <summary>
        /// Companies that primarily develop or publish video games and other multimedia software
        /// applications for devices that include personal computers, video game systems,
        /// cellphones, tablets, and other portable media players.
        /// </summary>
        public readonly static int ElectronicGamingAndMultimedia = 30830020;

        /// <summary>
        /// Companies primarily engaged in the drilling for petroleum and natural gas.
        /// </summary>
        public readonly static int OilAndGasDrilling = 30910010;

        /// <summary>
        /// Energy companies which are primarily engaged in oil and gas exploration and production.
        /// </summary>
        public readonly static int OilAndGasEAndP = 30910020;

        /// <summary>
        /// Major energy companies engaged in the diverse aspects of oil and gas operations,
        /// including crude oil and gas exploration, production, manufacturing, refining, marketing,
        /// and transportation.
        /// </summary>
        public readonly static int OilAndGasIntegrated = 30910030;

        /// <summary>
        /// Companies that own and operate oilfield pipelines and are involved in the gathering,
        /// processing, and transportation of natural crude petroleum.
        /// </summary>
        public readonly static int OilAndGasMidstream = 30910040;

        /// <summary>
        /// Companies that refine, gather, market, and sell petroleum and petroleum products.
        /// </summary>
        public readonly static int OilAndGasRefiningAndMarketing = 30910050;

        /// <summary>
        /// Companies that provide oilfield services and equipment for activities such as contract
        /// drilling and seismic surveys. Also includes equipment and tool rental, pumping and
        /// processing services, and inspection and contracting services.
        /// </summary>
        public readonly static int OilAndGasEquipmentAndServices = 30910060;

        /// <summary>
        /// Companies that mine thermal coal, which is used for generating energy. Excludes
        /// companies that mine coking coal to make steel.
        /// </summary>
        public readonly static int ThermalCoal = 30920010;

        /// <summary>
        /// Companies that mine, refine, produce, and mill uranium and uranium-related materials.
        /// </summary>
        public readonly static int Uranium = 30920020;

        /// <summary>
        /// Companies that manufacture aerospace and defense products, including aircraft and
        /// aircraft parts, tanks, guided missiles, space vehicles, ships and marine equipment, and
        /// other aerospace and defense components and systems, as well as companies supporting
        /// these products through repair and maintenance services.
        /// </summary>
        public readonly static int AerospaceAndDefense = 31010010;

        /// <summary>
        /// Companies that provide services to the commercial or business market, including
        /// information distribution, warehousing, graphic design, accounting, printing, and
        /// miscellaneous services.
        /// </summary>
        public readonly static int SpecialtyBusinessServices = 31020010;

        /// <summary>
        /// Companies that provide management, research, and consulting services to businesses and
        /// other agencies. Includes companies engaged in strategic and management consulting
        /// services, interior design, and information and analytics.
        /// </summary>
        public readonly static int ConsultingServices = 31020020;

        /// <summary>
        /// Companies that rent or lease durable goods to the commercial and consumer market,
        /// including cars and trucks, medical and industrial equipment, appliances and tools, and
        /// miscellaneous goods.
        /// </summary>
        public readonly static int RentalAndLeasingServices = 31020030;

        /// <summary>
        /// Companies that provide security and protective services, including protective or
        /// preventive devices, security guards and inspection services, security alarm and
        /// monitoring systems, detention and correction facilities, and other security-based services.
        /// </summary>
        public readonly static int SecurityAndProtectionServices = 31020040;

        /// <summary>
        /// Companies that provide staffing and employment services, including temporary staffing
        /// and permanent placement, outsources workforce and other employment-related services to
        /// businesses and government. Also includes companies providing online staffing services.
        /// </summary>
        public readonly static int StaffingAndEmploymentServices = 31020050;

        /// <summary>
        /// Companies that are in several separate lines of business with no single line providing
        /// the dominant source of revenue or income.
        /// </summary>
        public readonly static int Conglomerates = 31030010;

        /// <summary>
        /// Companies engaged in the design, construction, or contracting of industrial and
        /// nonresidential structures, streets and highways, bridges and tunnels, docks and piers,
        /// dams and water projects, utility lines, and other large building projects. Also includes
        /// companies that provide engineering consulting and architectural services to consumer and
        /// commercial clients.
        /// </summary>
        public readonly static int EngineeringAndConstruction = 31040010;

        /// <summary>
        /// Companies that develop, finance, maintain, or manage infrastructure operations such as
        /// ports, airports, and roadways.
        /// </summary>
        public readonly static int InfrastructureOperations = 31040020;

        /// <summary>
        /// Companies that manufacture building and construction products and materials, including
        /// ceramic floor and wall tiles, plumbing, HVAC, framing structures, and doors. Excludes
        /// companies that are classified in the building materials industry.
        /// </summary>
        public readonly static int BuildingProductsAndEquipment = 31040030;

        /// <summary>
        /// Companies that manufacture agricultural and construction machinery, including tractors,
        /// planting and harvesting machines, cranes, earthmovers, excavators, and related equipment
        /// and machinery. Includes truck manufacturers that provide local and long-haul trucking
        /// and transfer services for freight and cargo.
        /// </summary>
        public readonly static int FarmAndHeavyConstructionMachinery = 31050010;

        /// <summary>
        /// Companies primarily engaged in the distribution and supply of industrial equipment,
        /// including construction and farming machinery, garden equipment and supplies, and other
        /// industrial items to a diversified market of redistributors and end users.
        /// </summary>
        public readonly static int IndustrialDistribution = 31060010;

        /// <summary>
        /// Companies that manufacture and market office and business machines, such as copiers, fax
        /// machines, postage meters, point-of-sale terminals, and ATMs.
        /// </summary>
        public readonly static int BusinessEquipmentAndSupplies = 31070010;

        /// <summary>
        /// Companies engaged in diversified machinery manufacturing operations, including paper and
        /// food production machines, printing machinery, engines and turbines, air and gas
        /// processors, blowers and fans, furnaces and ovens, and other general and special industry machines.
        /// </summary>
        public readonly static int SpecialtyIndustrialMachinery = 31070020;

        /// <summary>
        /// Companies that fabricate, stamp, or form iron, steel, or other metals into products such
        /// as structured components by cutting, bending, and assembling processes of basic metals
        /// to create a final product.
        /// </summary>
        public readonly static int MetalFabrication = 31070030;

        /// <summary>
        /// Companies that manufacture equipment designed to control pollution, including control
        /// systems, hazardous waste disposal systems, recovery systems, treatment processes,
        /// filtration systems, cleaning and separation applications, and recycling machinery.
        /// </summary>
        public readonly static int PollutionAndTreatmentControls = 31070040;

        /// <summary>
        /// Companies that manufacture small, hand-held tools, including power-driven drills,
        /// welding apparatus, lawn and garden equipment, and other power or manually operated tools
        /// and accessories, such as hardware, nuts, bolts, rivets, and other fasteners.
        /// </summary>
        public readonly static int ToolsAndAccessories = 31070050;

        /// <summary>
        /// Companies that manufacture electrical equipment (such as smart-grid electrical
        /// equipment, utility metering, high- and low-voltage electrical equipment, transmission
        /// control devices, switches, and lighting distribution boxes), batteries, electrical wires
        /// and cables, and automation control.
        /// </summary>
        public readonly static int ElectricalEquipmentAndParts = 31070060;

        /// <summary>
        /// Air transportation companies that provide related air services, including helicopter
        /// transportation, air-charter services, in-flight catering services, and air emergency and
        /// business-related services.
        /// </summary>
        public readonly static int AirportsAndAirServices = 31080010;

        /// <summary>
        /// Major international passenger airline companies that fly a wide range of domestic and
        /// international routes.
        /// </summary>
        public readonly static int Airlines = 31080020;

        /// <summary>
        /// Companies that provide transportation of freight by line-haul railroad as well as
        /// related railroad equipment and repair services. Includes companies offering passenger
        /// services via railway and roadways.
        /// </summary>
        public readonly static int Railroads = 31080030;

        /// <summary>
        /// Companies that transport freight and cargo via water and operate marine ports. Includes
        /// companies that provide passenger services via water.
        /// </summary>
        public readonly static int MarineShipping = 31080040;

        /// <summary>
        /// Companies that provide local and long-haul trucking and transfer services for freight
        /// and cargo.
        /// </summary>
        public readonly static int Trucking = 31080050;

        /// <summary>
        /// Companies that transport freight and cargo via diversified methods such as railroads,
        /// airlines, and waterways.
        /// </summary>
        public readonly static int IntegratedFreightAndLogistics = 31080060;

        /// <summary>
        /// Companies that collect, treat, store, transfer, recycle, and dispose of waste materials,
        /// as well as companies that provide supporting environmental, engineering, and consulting services.
        /// </summary>
        public readonly static int WasteManagement = 31090010;

        /// <summary>
        /// Companies that provide computer-system design, network and systems operations, repair
        /// services, technical support, computer technology consulting, development, and
        /// implementation services.
        /// </summary>
        public readonly static int InformationTechnologyServices = 31110010;

        /// <summary>
        /// Companies that primarily design, develop, market, and support application software
        /// programs, including those that are cloud-based, for specific consumer and business functions.
        /// </summary>
        public readonly static int SoftwareApplication = 31110020;

        /// <summary>
        /// Companies that develop, design, support, and provide system software and services,
        /// including operating systems, networking software and devices, web portal services, cloud
        /// storage, and related services.
        /// </summary>
        public readonly static int SoftwareInfrastructure = 31110030;

        /// <summary>
        /// Companies that design, develop, manufacture, and market equipment for the communication
        /// industry, including fiber-optic cable; telecom peripherals; voice and data transmission
        /// and processing equipment; satellite products and equipment; video-conferencing systems
        /// and equipment; and interactive communication systems. Also includes companies that offer
        /// networking products that provide connectivity solutions for multi-use computing environments.
        /// </summary>
        public readonly static int CommunicationEquipment = 31120010;

        /// <summary>
        /// Companies that design, manufacture, and market computer systems, high mainframe servers,
        /// supercomputer, and 3D printers and scanners. Also includes companies that manufacture
        /// and market data storage products and other storage and backup devices for computers.
        /// </summary>
        public readonly static int ComputerHardware = 31120020;

        /// <summary>
        /// Companies that manufacture and market mobile communication products and household audio
        /// and video equipment, including radios, stereos, televisions, DVD player and personal use
        /// Drones. Excludes electric household appliances.
        /// </summary>
        public readonly static int ConsumerElectronics = 31120030;

        /// <summary>
        /// Companies that design, develop, manufacture, and market electronic devices, including
        /// electron tubes; electronic capacitors; electronic resistors; electronic coil and
        /// transformers; sensors; LED, TFT, and LCD displays; electronic connectors; printed
        /// circuit boards; circuit assemblies; and other general-purpose electronics components and products.
        /// </summary>
        public readonly static int ElectronicComponents = 31120040;

        /// <summary>
        /// Companies primarily engaged in the distribution, supply, and support of computers and
        /// computer systems, peripheral equipment, and software and other technological goods,
        /// including electronic equipment and appliances, electrical cable, wires, and other
        /// components to various consumer, commercial, and manufacturing customers.
        /// </summary>
        public readonly static int ElectronicsAndComputerDistribution = 31120050;

        /// <summary>
        /// Companies that design, develop, manufacture, and market sophisticated electronics of a
        /// technical nature, including lab apparatus, process and flow control devices, precise
        /// measurement and signal processing tools, search and navigation equipment, and other
        /// scientific or technical analytical or measuring devices.
        /// </summary>
        public readonly static int ScientificAndTechnicalInstruments = 31120060;

        /// <summary>
        /// Companies that design, develop, manufacture, and market equipment, spare parts, tools,
        /// cleaning devices, and related materials for the semiconductor industry.
        /// </summary>
        public readonly static int SemiconductorEquipmentAndMaterials = 31130010;

        /// <summary>
        /// Semiconductor companies that design, manufacture, and market integrated circuits,
        /// microprocessors, logic devices, chipsets, and memory chips for a wide variety of users.
        /// Includes companies that design, manufacture, and market general-application integrated
        /// circuits and memory and memory-intensive products.
        /// </summary>
        public readonly static int Semiconductors = 31130020;

        /// <summary>
        /// Companies that design, manufacture, market, or install solar power systems and components.
        /// </summary>
        public readonly static int Solar = 31130030;
    }
}
