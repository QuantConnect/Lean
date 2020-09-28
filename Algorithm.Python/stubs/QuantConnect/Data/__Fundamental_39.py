from .__Fundamental_40 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class MachineryFurnitureEquipmentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Fixed assets specifically dealing with tools, equipment and office furniture. This item is usually not available for the insurance and
                utility industries.
    
    MachineryFurnitureEquipmentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MachineryFurnitureEquipmentBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MaintenanceAndRepairsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of maintenance and repair expenses in the current period associated with the revenue generation. Mainly
                for fixed assets. This item is usually only available for transportation industry.
    
    MaintenanceAndRepairsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MaintenanceAndRepairsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MaterialsAndSuppliesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Aggregated amount of unprocessed materials to be used in manufacturing or production process and supplies that will be
                consumed. This item is typically available for the utility industry.
    
    MaterialsAndSuppliesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MaterialsAndSuppliesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MineralPropertiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A fixed asset that represents strictly mineral type properties.  This item is typically available for mining industry.
    
    MineralPropertiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MineralPropertiesBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MinimumPensionLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The company's minimum pension obligations to its former employees, paid into a defined pension plan to satisfy all pension
                entitlements that have been earned by employees to date.
    
    MinimumPensionLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MinimumPensionLiabilitiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MinorityInterestBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Carrying amount of the equity interests owned by non-controlling shareholders, partners, or other equity holders in one or more of
                the entities included in the reporting entity's consolidated financial statements.
    
    MinorityInterestBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MinorityInterestBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MinorityInterestCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Amount of net income (loss) for the period allocated to non-controlling shareholders, partners, or other equity holders in one or
                more of the entities included.
    
    MinorityInterestCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MinorityInterestCashFlowStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MinorityInterestsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents par or stated value of the subsidiary stock not owned by the parent company plus the minority interest's equity in the
                surplus of the subsidiary. This item includes preferred dividend averages on the minority preferred stock (preferred shares not
                owned by the reporting parent company). Minority interest also refers to stockholders who own less than 50% of a subsidiary's
                outstanding voting common stock. The minority stockholders hold an interest in the subsidiary's net assets and share earnings with
                the parent company.
    
    MinorityInterestsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MinorityInterestsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MoneyMarketInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Short-term (typical maturity is less than one year), highly liquid government or corporate debt instrument such as bankers'
                acceptance, promissory notes, and treasury bills.
    
    MoneyMarketInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.MoneyMarketInvestmentsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class MorningstarEconomySphereCode(System.object):
    """ Helper class for the AssetClassification's MorningstarEconomySphereCode field QuantConnect.Data.Fundamental.AssetClassification.MorningstarEconomySphereCode. """
    Cyclical: int
    Defensive: int
    Sensitive: int
    __all__: list


class MorningstarIndustryCode(System.object):
    """ Helper class for the AssetClassification's MorningstarIndustryCode field QuantConnect.Data.Fundamental.AssetClassification.MorningstarIndustryCode. """
    AdvertisingAgencies: int
    AerospaceAndDefense: int
    AgriculturalInputs: int
    Airlines: int
    AirportsAndAirServices: int
    Aluminum: int
    ApparelManufacturing: int
    ApparelRetail: int
    AssetManagement: int
    AutoAndTruckDealerships: int
    AutoManufacturers: int
    AutoParts: int
    BanksDiversified: int
    BanksRegional: int
    BeveragesBrewers: int
    BeveragesNonAlcoholic: int
    BeveragesWineriesAndDistilleries: int
    Biotechnology: int
    Broadcasting: int
    BuildingMaterials: int
    BuildingProductsAndEquipment: int
    BusinessEquipmentAndSupplies: int
    CapitalMarkets: int
    Chemicals: int
    CokingCoal: int
    CommunicationEquipment: int
    ComputerHardware: int
    Confectioners: int
    Conglomerates: int
    ConsultingServices: int
    ConsumerElectronics: int
    Copper: int
    CreditServices: int
    DepartmentStores: int
    DiagnosticsAndResearch: int
    DiscountStores: int
    DrugManufacturersGeneral: int
    DrugManufacturersSpecialtyAndGeneric: int
    EducationAndTrainingServices: int
    ElectricalEquipmentAndParts: int
    ElectronicComponents: int
    ElectronicGamingAndMultimedia: int
    ElectronicsAndComputerDistribution: int
    EngineeringAndConstruction: int
    Entertainment: int
    FarmAndHeavyConstructionMachinery: int
    FarmProducts: int
    FinancialConglomerates: int
    FinancialDataAndStockExchanges: int
    FixturesAndAppliances: int
    FoodDistribution: int
    FootwearAndAccessories: int
    Furnishings: int
    Gambling: int
    Gold: int
    GroceryStores: int
    HealthcarePlans: int
    HealthInformationServices: int
    HomeImprovementRetail: int
    HouseholdAndPersonalProducts: int
    IndustrialDistribution: int
    InformationTechnologyServices: int
    InfrastructureOperations: int
    InsuranceBrokers: int
    InsuranceDiversified: int
    InsuranceLife: int
    InsurancePropertyAndCasualty: int
    InsuranceReinsurance: int
    InsuranceSpecialty: int
    IntegratedFreightAndLogistics: int
    InternetContentAndInformation: int
    InternetRetail: int
    Leisure: int
    Lodging: int
    LumberAndWoodProduction: int
    LuxuryGoods: int
    MarineShipping: int
    MedicalCareFacilities: int
    MedicalDevices: int
    MedicalDistribution: int
    MedicalInstrumentsAndSupplies: int
    MetalFabrication: int
    MortgageFinance: int
    OilAndGasDrilling: int
    OilAndGasEAndP: int
    OilAndGasEquipmentAndServices: int
    OilAndGasIntegrated: int
    OilAndGasMidstream: int
    OilAndGasRefiningAndMarketing: int
    OtherIndustrialMetalsAndMining: int
    OtherPreciousMetalsAndMining: int
    PackagedFoods: int
    PackagingAndContainers: int
    PaperAndPaperProducts: int
    PersonalServices: int
    PharmaceuticalRetailers: int
    PollutionAndTreatmentControls: int
    Publishing: int
    Railroads: int
    RealEstateDevelopment: int
    RealEstateDiversified: int
    RealEstateServices: int
    RecreationalVehicles: int
    REITDiversified: int
    REITHealthcareFacilities: int
    REITHotelAndMotel: int
    REITIndustrial: int
    REITMortgage: int
    REITOffice: int
    REITResidential: int
    REITRetail: int
    REITSpecialty: int
    RentalAndLeasingServices: int
    ResidentialConstruction: int
    ResortsAndCasinos: int
    Restaurants: int
    ScientificAndTechnicalInstruments: int
    SecurityAndProtectionServices: int
    SemiconductorEquipmentAndMaterials: int
    Semiconductors: int
    ShellCompanies: int
    Silver: int
    SoftwareApplication: int
    SoftwareInfrastructure: int
    Solar: int
    SpecialtyBusinessServices: int
    SpecialtyChemicals: int
    SpecialtyIndustrialMachinery: int
    SpecialtyRetail: int
    StaffingAndEmploymentServices: int
    Steel: int
    TelecomServices: int
    TextileManufacturing: int
    ThermalCoal: int
    Tobacco: int
    ToolsAndAccessories: int
    TravelServices: int
    Trucking: int
    Uranium: int
    UtilitiesDiversified: int
    UtilitiesIndependentPowerProducers: int
    UtilitiesRegulatedElectric: int
    UtilitiesRegulatedGas: int
    UtilitiesRegulatedWater: int
    UtilitiesRenewable: int
    WasteManagement: int
    __all__: list
