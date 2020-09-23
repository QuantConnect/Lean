from .__UniverseSelection_1 import *
import typing
import System.IO
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Securities.Option
import QuantConnect.Securities.Future
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import NodaTime
import datetime

# no functions
# classes

class BaseDataCollection(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    This type exists for transport of data as a single packet
    
    BaseDataCollection()
    BaseDataCollection(time: DateTime, symbol: Symbol, data: IEnumerable[BaseData])
    BaseDataCollection(time: DateTime, endTime: DateTime, symbol: Symbol, data: IEnumerable[BaseData])
    BaseDataCollection(time: DateTime, endTime: DateTime, symbol: Symbol, data: List[BaseData])
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.UniverseSelection.BaseDataCollection:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, symbol: QuantConnect.Symbol, data: typing.List[QuantConnect.Data.BaseData]) -> QuantConnect.Data.UniverseSelection.BaseDataCollection:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, endTime: datetime.datetime, symbol: QuantConnect.Symbol, data: typing.List[QuantConnect.Data.BaseData]) -> QuantConnect.Data.UniverseSelection.BaseDataCollection:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, endTime: datetime.datetime, symbol: QuantConnect.Symbol, data: typing.List[QuantConnect.Data.BaseData]) -> QuantConnect.Data.UniverseSelection.BaseDataCollection:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.BaseDataCollection:
        pass

    Data: typing.List[QuantConnect.Data.BaseData]

    EndTime: datetime.datetime



class CoarseFundamental(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Defines summary information about a single symbol for a given date
    
    CoarseFundamental()
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    @staticmethod
    def CreateUniverseSymbol(market: str, addGuid: bool) -> QuantConnect.Symbol:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    AdjustedPrice: float

    DollarVolume: float

    EndTime: datetime.datetime

    HasFundamentalData: bool

    Market: str

    PriceFactor: float

    PriceScaleFactor: float

    SplitFactor: float

    Volume: int



class Universe(System.object, System.IDisposable):
    """ Provides a base class for all universes to derive from. """
    def CanRemoveMember(self, utcTime: datetime.datetime, security: QuantConnect.Securities.Security) -> bool:
        pass

    def ContainsMember(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def CreateSecurity(self, symbol: QuantConnect.Symbol, algorithm: QuantConnect.Interfaces.IAlgorithm, marketHoursDatabase: QuantConnect.Securities.MarketHoursDatabase, symbolPropertiesDatabase: QuantConnect.Securities.SymbolPropertiesDatabase) -> QuantConnect.Securities.Security:
        pass

    def Dispose(self) -> None:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime, subscriptionService: QuantConnect.Interfaces.ISubscriptionDataConfigService) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def GetSubscriptionRequests(self, *args) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def PerformSelection(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    def SetSecurityInitializer(self, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> None:
        pass

    def __init__(self, *args): #cannot find CLR constructor
        pass

    Configuration: QuantConnect.Data.SubscriptionDataConfig

    DisposeRequested: bool

    Market: str

    Members: System.Collections.Generic.Dictionary[QuantConnect.Symbol, QuantConnect.Securities.Security]

    Securities: System.Collections.Concurrent.ConcurrentDictionary[QuantConnect.Symbol, QuantConnect.Data.UniverseSelection.Member]

    SecurityInitializer: QuantConnect.Securities.ISecurityInitializer

    SecurityType: QuantConnect.SecurityType

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings


    Member: type
    Unchanged: UnchangedUniverse
    UnchangedUniverse: type


class CoarseFundamentalUniverse(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable):
    """
    Defines a universe that reads coarse us equity data
    
    CoarseFundamentalUniverse(universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, selector: Func[IEnumerable[CoarseFundamental], IEnumerable[Symbol]])
    CoarseFundamentalUniverse(universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, selector: PyObject)
    CoarseFundamentalUniverse(symbol: Symbol, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, selector: Func[IEnumerable[CoarseFundamental], IEnumerable[Symbol]])
    CoarseFundamentalUniverse(symbol: Symbol, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, selector: PyObject)
    """
    @staticmethod
    def CreateConfiguration(symbol: QuantConnect.Symbol) -> QuantConnect.Data.SubscriptionDataConfig:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, selector: typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Data.UniverseSelection.CoarseFundamentalUniverse:
        pass

    @typing.overload
    def __init__(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, selector: Python.Runtime.PyObject) -> QuantConnect.Data.UniverseSelection.CoarseFundamentalUniverse:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, selector: typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Data.UniverseSelection.CoarseFundamentalUniverse:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, selector: Python.Runtime.PyObject) -> QuantConnect.Data.UniverseSelection.CoarseFundamentalUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.CoarseFundamentalUniverse:
        pass

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings



class FuncUniverse(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable):
    """
    Provides a functional implementation of QuantConnect.Data.UniverseSelection.Universe
    
    FuncUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, universeSelector: Func[IEnumerable[BaseData], IEnumerable[Symbol]])
    FuncUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, universeSelector: Func[IEnumerable[BaseData], IEnumerable[Symbol]])
    """
    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, universeSelector: typing.Callable[[typing.List[QuantConnect.Data.BaseData]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Data.UniverseSelection.FuncUniverse:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, universeSelector: typing.Callable[[typing.List[QuantConnect.Data.BaseData]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Data.UniverseSelection.FuncUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.FuncUniverse:
        pass

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings
