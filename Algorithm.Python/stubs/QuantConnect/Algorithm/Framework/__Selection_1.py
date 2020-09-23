from .__Selection_2 import *
import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect.Algorithm.Framework.Selection
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import NodaTime
import datetime


class InceptionDateUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.CustomUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Inception Date Universe that accepts a Dictionary of DateTime keyed by String that represent
                the Inception date for each ticker
    
    InceptionDateUniverseSelectionModel(name: str, tickersByDate: Dictionary[str, DateTime])
    InceptionDateUniverseSelectionModel(name: str, tickersByDate: PyObject)
    """
    def Select(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, date: datetime.datetime) -> typing.List[str]:
        pass

    @typing.overload
    def __init__(self, name: str, tickersByDate: System.Collections.Generic.Dictionary[str, datetime.datetime]) -> QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, name: str, tickersByDate: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel:
        pass


class EnergyETFUniverse(QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """ EnergyETFUniverse() """

class FineFundamentalUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.FundamentalUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Portfolio selection model that uses coarse/fine selectors. For US equities only.
    
    FineFundamentalUniverseSelectionModel(coarseSelector: Func[IEnumerable[CoarseFundamental], IEnumerable[Symbol]], fineSelector: Func[IEnumerable[FineFundamental], IEnumerable[Symbol]], universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    FineFundamentalUniverseSelectionModel(coarseSelector: PyObject, fineSelector: PyObject, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    """
    def SelectCoarse(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, coarse: typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]) -> typing.List[QuantConnect.Symbol]:
        pass

    def SelectFine(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, fine: typing.List[QuantConnect.Data.Fundamental.FineFundamental]) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self, coarseSelector: typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]], fineSelector: typing.Callable[[typing.List[QuantConnect.Data.Fundamental.FineFundamental]], typing.List[QuantConnect.Symbol]], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.FineFundamentalUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, coarseSelector: Python.Runtime.PyObject, fineSelector: Python.Runtime.PyObject, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.FineFundamentalUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.FineFundamentalUniverseSelectionModel:
        pass


class FutureUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel that subscribes to future chains
    
    FutureUniverseSelectionModel(refreshInterval: TimeSpan, futureChainSymbolSelector: Func[DateTime, IEnumerable[Symbol]])
    FutureUniverseSelectionModel(refreshInterval: TimeSpan, futureChainSymbolSelector: Func[DateTime, IEnumerable[Symbol]], universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    FutureUniverseSelectionModel(refreshInterval: TimeSpan, futureChainSymbolSelector: Func[DateTime, IEnumerable[Symbol]], universeSettings: UniverseSettings)
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def GetNextRefreshTimeUtc(self) -> datetime.datetime:
        pass

    @typing.overload
    def __init__(self, refreshInterval: datetime.timedelta, futureChainSymbolSelector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Algorithm.Framework.Selection.FutureUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, refreshInterval: datetime.timedelta, futureChainSymbolSelector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.FutureUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, refreshInterval: datetime.timedelta, futureChainSymbolSelector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Algorithm.Framework.Selection.FutureUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.FutureUniverseSelectionModel:
        pass


class IUniverseSelectionModel:
    """ Algorithm framework model that defines the universes to be used by an algorithm """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def GetNextRefreshTimeUtc(self) -> datetime.datetime:
        pass


class LiquidETFUniverse(QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Universe Selection Model that adds the following ETFs at their inception date
    
    LiquidETFUniverse()
    """
    Grouping: type


class ManualUniverse(QuantConnect.Data.UniverseSelection.UserDefinedUniverse, System.IDisposable, QuantConnect.Data.UniverseSelection.ITimeTriggeredUniverse, System.Collections.Specialized.INotifyCollectionChanged):
    """
    Defines a universe as a set of manually set symbols. This differs from QuantConnect.Data.UniverseSelection.UserDefinedUniverse
                in that these securities were not added via AddSecurity.
    
    ManualUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, symbols: IEnumerable[Symbol])
    ManualUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, symbols: IEnumerable[Symbol])
    ManualUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, symbols: Array[Symbol])
    """
    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime, subscriptionService: QuantConnect.Interfaces.ISubscriptionDataConfigService) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def GetSubscriptionRequests(self, *args) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverse:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverse:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverse:
        pass


class ManualUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel that simply
                subscribes to the specified set of symbols
    
    ManualUniverseSelectionModel()
    ManualUniverseSelectionModel(symbols: IEnumerable[Symbol])
    ManualUniverseSelectionModel(*symbols: Array[Symbol])
    ManualUniverseSelectionModel(symbols: IEnumerable[Symbol], universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, symbols: typing.List[QuantConnect.Symbol], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.ManualUniverseSelectionModel:
        pass
