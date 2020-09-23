import typing
import System.Reflection
import System.Linq.Expressions
import System.IO
import System.Dynamic
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Packets
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import NodaTime
import datetime


class SubscriptionDataConfigExtensions(System.object):
    """
    Helper methods used to determine different configurations properties
                for a given set of QuantConnect.Data.SubscriptionDataConfig
    """
    @staticmethod
    def DataNormalizationMode(subscriptionDataConfigs: typing.List[QuantConnect.Data.SubscriptionDataConfig]) -> QuantConnect.DataNormalizationMode:
        pass

    @staticmethod
    def GetBaseDataInstance(config: QuantConnect.Data.SubscriptionDataConfig) -> QuantConnect.Data.BaseData:
        pass

    @staticmethod
    def GetHighestResolution(subscriptionDataConfigs: typing.List[QuantConnect.Data.SubscriptionDataConfig]) -> QuantConnect.Resolution:
        pass

    @staticmethod
    def IsCustomData(subscriptionDataConfigs: typing.List[QuantConnect.Data.SubscriptionDataConfig]) -> bool:
        pass

    @staticmethod
    def IsExtendedMarketHours(subscriptionDataConfigs: typing.List[QuantConnect.Data.SubscriptionDataConfig]) -> bool:
        pass

    @staticmethod
    def IsFillForward(subscriptionDataConfigs: typing.List[QuantConnect.Data.SubscriptionDataConfig]) -> bool:
        pass

    @staticmethod
    def SetDataNormalizationMode(subscriptionDataConfigs: typing.List[QuantConnect.Data.SubscriptionDataConfig], mode: QuantConnect.DataNormalizationMode) -> None:
        pass

    @staticmethod
    def TickerShouldBeMapped(config: QuantConnect.Data.SubscriptionDataConfig) -> bool:
        pass

    __all__: list


class SubscriptionDataConfigList(System.Collections.Generic.List[SubscriptionDataConfig], System.Collections.Generic.IList[SubscriptionDataConfig], System.Collections.Generic.IReadOnlyCollection[SubscriptionDataConfig], System.Collections.Generic.IReadOnlyList[SubscriptionDataConfig], System.Collections.IEnumerable, System.Collections.Generic.ICollection[SubscriptionDataConfig], System.Collections.IList, System.Collections.Generic.IEnumerable[SubscriptionDataConfig], System.Collections.ICollection):
    """
    Provides convenient methods for holding several QuantConnect.Data.SubscriptionDataConfig
    
    SubscriptionDataConfigList(symbol: Symbol)
    """
    def SetDataNormalizationMode(self, normalizationMode: QuantConnect.DataNormalizationMode) -> None:
        pass

    def __init__(self, symbol: QuantConnect.Symbol) -> QuantConnect.Data.SubscriptionDataConfigList:
        pass

    IsInternalFeed: bool

    Symbol: QuantConnect.Symbol



class SubscriptionDataSource(System.object, System.IEquatable[SubscriptionDataSource]):
    """
    Represents the source location and transport medium for a subscription
    
    SubscriptionDataSource(source: str, transportMedium: SubscriptionTransportMedium)
    SubscriptionDataSource(source: str, transportMedium: SubscriptionTransportMedium, format: FileFormat)
    SubscriptionDataSource(source: str, transportMedium: SubscriptionTransportMedium, format: FileFormat, headers: IEnumerable[KeyValuePair[str, str]])
    """
    @typing.overload
    def Equals(self, other: QuantConnect.Data.SubscriptionDataSource) -> bool:
        pass

    @typing.overload
    def Equals(self, obj: object) -> bool:
        pass

    def Equals(self, *args) -> bool:
        pass

    def GetHashCode(self) -> int:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self, source: str, transportMedium: QuantConnect.SubscriptionTransportMedium) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def __init__(self, source: str, transportMedium: QuantConnect.SubscriptionTransportMedium, format: QuantConnect.Data.FileFormat) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def __init__(self, source: str, transportMedium: QuantConnect.SubscriptionTransportMedium, format: QuantConnect.Data.FileFormat, headers: typing.List[System.Collections.Generic.KeyValuePair[str, str]]) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    def __init__(self, *args) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    Format: QuantConnect.Data.FileFormat
    Headers: typing.List[System.Collections.Generic.KeyValuePair[str, str]]
    Source: str
    TransportMedium: QuantConnect.SubscriptionTransportMedium

class SubscriptionManager(System.object):
    """
    Enumerable Subscription Management Class
    
    SubscriptionManager()
    """
    @typing.overload
    def Add(self, symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, timeZone: NodaTime.DateTimeZone, exchangeTimeZone: NodaTime.DateTimeZone, isCustomData: bool, fillDataForward: bool, extendedMarketHours: bool) -> QuantConnect.Data.SubscriptionDataConfig:
        pass

    @typing.overload
    def Add(self, dataType: type, tickType: QuantConnect.TickType, symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, dataTimeZone: NodaTime.DateTimeZone, exchangeTimeZone: NodaTime.DateTimeZone, isCustomData: bool, fillDataForward: bool, extendedMarketHours: bool, isInternalFeed: bool, isFilteredSubscription: bool, dataNormalizationMode: QuantConnect.DataNormalizationMode) -> QuantConnect.Data.SubscriptionDataConfig:
        pass

    def Add(self, *args) -> QuantConnect.Data.SubscriptionDataConfig:
        pass

    @typing.overload
    def AddConsolidator(self, symbol: QuantConnect.Symbol, consolidator: QuantConnect.Data.Consolidators.IDataConsolidator) -> None:
        pass

    @typing.overload
    def AddConsolidator(self, symbol: QuantConnect.Symbol, pyConsolidator: Python.Runtime.PyObject) -> None:
        pass

    def AddConsolidator(self, *args) -> None:
        pass

    @staticmethod
    def DefaultDataTypes() -> System.Collections.Generic.Dictionary[QuantConnect.SecurityType, typing.List[QuantConnect.TickType]]:
        pass

    def GetDataTypesForSecurity(self, securityType: QuantConnect.SecurityType) -> typing.List[QuantConnect.TickType]:
        pass

    @staticmethod
    def IsSubscriptionValidForConsolidator(subscription: QuantConnect.Data.SubscriptionDataConfig, consolidator: QuantConnect.Data.Consolidators.IDataConsolidator) -> bool:
        pass

    def LookupSubscriptionConfigDataTypes(self, symbolSecurityType: QuantConnect.SecurityType, resolution: QuantConnect.Resolution, isCanonical: bool) -> typing.List[System.Tuple[type, QuantConnect.TickType]]:
        pass

    def RemoveConsolidator(self, symbol: QuantConnect.Symbol, consolidator: QuantConnect.Data.Consolidators.IDataConsolidator) -> None:
        pass

    def SetDataManager(self, subscriptionManager: QuantConnect.Interfaces.IAlgorithmSubscriptionManager) -> None:
        pass

    AvailableDataTypes: System.Collections.Generic.Dictionary[QuantConnect.SecurityType, typing.List[QuantConnect.TickType]]

    Count: int

    SubscriptionDataConfigService: QuantConnect.Interfaces.ISubscriptionDataConfigService

    Subscriptions: typing.List[QuantConnect.Data.SubscriptionDataConfig]
