from .__UniverseSelection_2 import *
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



class ConstituentsUniverse(QuantConnect.Data.UniverseSelection.FuncUniverse, System.IDisposable):
    """
    ConstituentsUniverse allows to perform universe selection based on an
                already preselected set of QuantConnect.Symbol.
    
    ConstituentsUniverse(symbol: Symbol, universeSettings: UniverseSettings, type: Type)
    ConstituentsUniverse(subscriptionDataConfig: SubscriptionDataConfig, universeSettings: UniverseSettings)
    """
    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, type: type) -> QuantConnect.Data.UniverseSelection.ConstituentsUniverse:
        pass

    @typing.overload
    def __init__(self, subscriptionDataConfig: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.ConstituentsUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.ConstituentsUniverse:
        pass


class ConstituentsUniverseData(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Custom base data class used for QuantConnect.Data.UniverseSelection.ConstituentsUniverse
    
    ConstituentsUniverseData()
    """
    def DefaultResolution(self) -> QuantConnect.Resolution:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    def IsSparseData(self) -> bool:
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

    def RequiresMapping(self) -> bool:
        pass

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    EndTime: datetime.datetime



class UniverseDecorator(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable):
    """
    Provides an implementation of QuantConnect.Data.UniverseSelection.Universe that redirects all calls to a
                wrapped (or decorated) universe. This provides scaffolding for other decorators who
                only need to override one or two methods.
    """
    def CanRemoveMember(self, utcTime: datetime.datetime, security: QuantConnect.Securities.Security) -> bool:
        pass

    def CreateSecurity(self, symbol: QuantConnect.Symbol, algorithm: QuantConnect.Interfaces.IAlgorithm, marketHoursDatabase: QuantConnect.Securities.MarketHoursDatabase, symbolPropertiesDatabase: QuantConnect.Securities.SymbolPropertiesDatabase) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime, subscriptionService: QuantConnect.Interfaces.ISubscriptionDataConfigService) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def GetSubscriptionRequests(self, *args) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    def __init__(self, *args): #cannot find CLR constructor
        pass

    Securities: System.Collections.Concurrent.ConcurrentDictionary[QuantConnect.Symbol, QuantConnect.Data.UniverseSelection.Member]

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings

    Universe: QuantConnect.Data.UniverseSelection.Universe


class SelectSymbolsUniverseDecorator(QuantConnect.Data.UniverseSelection.UniverseDecorator, System.IDisposable):
    """
    Provides a univese decoration that replaces the implementation of QuantConnect.Data.UniverseSelection.SelectSymbolsUniverseDecorator.SelectSymbols(System.DateTime,QuantConnect.Data.UniverseSelection.BaseDataCollection)
    
    SelectSymbolsUniverseDecorator(universe: Universe, selectSymbols: SelectSymbolsDelegate)
    """
    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    def __init__(self, universe: QuantConnect.Data.UniverseSelection.Universe, selectSymbols: QuantConnect.Data.UniverseSelection.SelectSymbolsDelegate) -> QuantConnect.Data.UniverseSelection.SelectSymbolsUniverseDecorator:
        pass

    Universe: QuantConnect.Data.UniverseSelection.Universe
    SelectSymbolsDelegate: type


class FineFundamentalFilteredUniverse(QuantConnect.Data.UniverseSelection.SelectSymbolsUniverseDecorator, System.IDisposable):
    """
    Provides a universe that can be filtered with a QuantConnect.Data.Fundamental.FineFundamental selection function
    
    FineFundamentalFilteredUniverse(universe: Universe, fineSelector: Func[IEnumerable[FineFundamental], IEnumerable[Symbol]])
    FineFundamentalFilteredUniverse(universe: Universe, fineSelector: PyObject)
    """
    def SetSecurityInitializer(self, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> None:
        pass

    @typing.overload
    def __init__(self, universe: QuantConnect.Data.UniverseSelection.Universe, fineSelector: typing.Callable[[typing.List[QuantConnect.Data.Fundamental.FineFundamental]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Data.UniverseSelection.FineFundamentalFilteredUniverse:
        pass

    @typing.overload
    def __init__(self, universe: QuantConnect.Data.UniverseSelection.Universe, fineSelector: Python.Runtime.PyObject) -> QuantConnect.Data.UniverseSelection.FineFundamentalFilteredUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.FineFundamentalFilteredUniverse:
        pass

    FineFundamentalUniverse: QuantConnect.Data.UniverseSelection.FineFundamentalUniverse

    Universe: QuantConnect.Data.UniverseSelection.Universe


class FineFundamentalUniverse(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable):
    """
    Defines a universe that reads fine us equity data
    
    FineFundamentalUniverse(universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, selector: Func[IEnumerable[FineFundamental], IEnumerable[Symbol]])
    FineFundamentalUniverse(symbol: Symbol, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, selector: Func[IEnumerable[FineFundamental], IEnumerable[Symbol]])
    """
    @staticmethod
    def CreateConfiguration(symbol: QuantConnect.Symbol) -> QuantConnect.Data.SubscriptionDataConfig:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, selector: typing.Callable[[typing.List[QuantConnect.Data.Fundamental.FineFundamental]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Data.UniverseSelection.FineFundamentalUniverse:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, selector: typing.Callable[[typing.List[QuantConnect.Data.Fundamental.FineFundamental]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Data.UniverseSelection.FineFundamentalUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.FineFundamentalUniverse:
        pass

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings



class FuturesChainUniverse(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable):
    """
    Defines a universe for a single futures chain
    
    FuturesChainUniverse(future: Future, universeSettings: UniverseSettings)
    FuturesChainUniverse(future: Future, universeSettings: UniverseSettings, subscriptionManager: SubscriptionManager, securityInitializer: ISecurityInitializer)
    """
    def CanRemoveMember(self, utcTime: datetime.datetime, security: QuantConnect.Securities.Security) -> bool:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self, future: QuantConnect.Securities.Future.Future, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.FuturesChainUniverse:
        pass

    @typing.overload
    def __init__(self, future: QuantConnect.Securities.Future.Future, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, subscriptionManager: QuantConnect.Data.SubscriptionManager, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Data.UniverseSelection.FuturesChainUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.FuturesChainUniverse:
        pass

    Future: QuantConnect.Securities.Future.Future

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings
