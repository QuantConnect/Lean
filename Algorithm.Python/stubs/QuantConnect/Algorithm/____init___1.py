from .____init___2 import *
import typing
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Storage
import QuantConnect.Securities.Option
import QuantConnect.Securities.Future
import QuantConnect.Securities.Forex
import QuantConnect.Securities.Equity
import QuantConnect.Securities.Crypto
import QuantConnect.Securities.Cfd
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Python
import QuantConnect.Orders
import QuantConnect.Notifications
import QuantConnect.Interfaces
import QuantConnect.Indicators.CandlestickPatterns
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Fundamental
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Benchmarks
import QuantConnect.Algorithm.Framework.Selection
import QuantConnect.Algorithm.Framework.Risk
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Execution
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import pandas
import NodaTime
import datetime


class ConstituentUniverseDefinitions(System.object):
    """
    Provides helpers for defining constituent universes based on the Morningstar
                asset classification QuantConnect.Data.Fundamental.AssetClassification https://www.morningstar.com/
    
    ConstituentUniverseDefinitions(algorithm: IAlgorithm)
    """
    def AerospaceAndDefense(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def AggressiveGrowth(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Agriculture(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def AssetManagement(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Banks(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def BeveragesAlcoholic(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def BeveragesNonAlcoholic(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Biotechnology(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def BuildingMaterials(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def BusinessServices(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def CapitalMarkets(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Chemicals(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def ClassicGrowth(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Conglomerates(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Construction(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def ConsumerPackagedGoods(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def CreditServices(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Cyclicals(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Distressed(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def DiversifiedFinancialServices(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def DrugManufacturers(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Education(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def FarmAndHeavyConstructionMachinery(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def FixturesAndAppliances(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def ForestProducts(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def HardAsset(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Hardware(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def HealthcarePlans(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def HealthcareProvidersAndServices(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def HighYield(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def HomebuildingAndConstruction(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def IndustrialDistribution(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def IndustrialProducts(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Insurance(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def InteractiveMedia(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def LargeCore(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def LargeGrowth(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def LargeValue(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def ManufacturingApparelAndAccessories(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def MediaDiversified(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def MedicalDevicesAndInstruments(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def MedicalDiagnosticsAndResearch(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def MedicalDistribution(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def MetalsAndMining(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def MidCore(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def MidGrowth(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def MidValue(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def OilAndGas(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def OtherEnergySources(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def PackagingAndContainers(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def PersonalServices(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def RealEstate(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def REITs(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Restaurants(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def RetailCyclical(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def RetailDefensive(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Semiconductors(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def SlowGrowth(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def SmallCore(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def SmallGrowth(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def SmallValue(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Software(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def SpeculativeGrowth(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Steel(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def TelecommunicationServices(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def TobaccoProducts(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Transportation(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def TravelAndLeisure(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def UtilitiesIndependentPowerProducers(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def UtilitiesRegulated(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def VehiclesAndParts(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def WasteManagement(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def __init__(self, algorithm: QuantConnect.Interfaces.IAlgorithm) -> QuantConnect.Algorithm.ConstituentUniverseDefinitions:
        pass
