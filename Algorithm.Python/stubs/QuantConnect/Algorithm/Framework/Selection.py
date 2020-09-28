from .__Selection_1 import *
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

# no functions
# classes

class UniverseSelectionModel(System.object, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides a base class for universe selection models.
    
    UniverseSelectionModel()
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def GetNextRefreshTimeUtc(self) -> datetime.datetime:
        pass


class FundamentalUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """ Provides a base class for defining equity coarse/fine fundamental selection models """
    @staticmethod
    def Coarse(coarseSelector: typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel:
        pass

    def CreateCoarseFundamentalUniverse(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    @staticmethod
    def Fine(coarseSelector: typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]], fineSelector: typing.Callable[[typing.List[QuantConnect.Data.Fundamental.FineFundamental]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel:
        pass

    def SelectCoarse(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, coarse: typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]) -> typing.List[QuantConnect.Symbol]:
        pass

    def SelectFine(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, fine: typing.List[QuantConnect.Data.Fundamental.FineFundamental]) -> typing.List[QuantConnect.Symbol]:
        pass

    def __init__(self, *args): #cannot find CLR constructor
        pass


class CoarseFundamentalUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.FundamentalUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Portfolio selection model that uses coarse selectors. For US equities only.
    
    CoarseFundamentalUniverseSelectionModel(coarseSelector: Func[IEnumerable[CoarseFundamental], IEnumerable[Symbol]], universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    CoarseFundamentalUniverseSelectionModel(coarseSelector: PyObject, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    """
    def SelectCoarse(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, coarse: typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self, coarseSelector: typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.CoarseFundamentalUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, coarseSelector: Python.Runtime.PyObject, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.CoarseFundamentalUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.CoarseFundamentalUniverseSelectionModel:
        pass


class CompositeUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel that combines multiple universe
                selection models into a single model.
    
    CompositeUniverseSelectionModel(*universeSelectionModels: Array[IUniverseSelectionModel])
    CompositeUniverseSelectionModel(*universeSelectionModels: Array[PyObject])
    CompositeUniverseSelectionModel(universeSelectionModel: PyObject)
    """
    @typing.overload
    def AddUniverseSelection(self, universeSelectionModel: QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel) -> None:
        pass

    @typing.overload
    def AddUniverseSelection(self, pyUniverseSelectionModel: Python.Runtime.PyObject) -> None:
        pass

    def AddUniverseSelection(self, *args) -> None:
        pass

    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def GetNextRefreshTimeUtc(self) -> datetime.datetime:
        pass

    @typing.overload
    def __init__(self, universeSelectionModels: typing.List[QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel]) -> QuantConnect.Algorithm.Framework.Selection.CompositeUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, universeSelectionModels: typing.List[Python.Runtime.PyObject]) -> QuantConnect.Algorithm.Framework.Selection.CompositeUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, universeSelectionModel: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Selection.CompositeUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.CompositeUniverseSelectionModel:
        pass


class CustomUniverse(QuantConnect.Data.UniverseSelection.UserDefinedUniverse, System.IDisposable, QuantConnect.Data.UniverseSelection.ITimeTriggeredUniverse, System.Collections.Specialized.INotifyCollectionChanged):
    """
    Defines a universe as a set of dynamically set symbols.
    
    CustomUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, interval: TimeSpan, selector: Func[DateTime, IEnumerable[str]])
    """
    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime, subscriptionService: QuantConnect.Interfaces.ISubscriptionDataConfigService) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def GetSubscriptionRequests(self, *args) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, interval: datetime.timedelta, selector: typing.Callable[[datetime.datetime], typing.List[str]]) -> QuantConnect.Algorithm.Framework.Selection.CustomUniverse:
        pass


class CustomUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel that simply
                subscribes to the specified set of symbols
    
    CustomUniverseSelectionModel(name: str, selector: Func[DateTime, IEnumerable[str]])
    CustomUniverseSelectionModel(name: str, selector: PyObject)
    CustomUniverseSelectionModel(securityType: SecurityType, name: str, market: str, selector: Func[DateTime, IEnumerable[str]], universeSettings: UniverseSettings, interval: TimeSpan)
    CustomUniverseSelectionModel(securityType: SecurityType, name: str, market: str, selector: PyObject, universeSettings: UniverseSettings, interval: TimeSpan)
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def Select(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, date: datetime.datetime) -> typing.List[str]:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self, name: str, selector: typing.Callable[[datetime.datetime], typing.List[str]]) -> QuantConnect.Algorithm.Framework.Selection.CustomUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, name: str, selector: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Selection.CustomUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, securityType: QuantConnect.SecurityType, name: str, market: str, selector: typing.Callable[[datetime.datetime], typing.List[str]], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, interval: datetime.timedelta) -> QuantConnect.Algorithm.Framework.Selection.CustomUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, securityType: QuantConnect.SecurityType, name: str, market: str, selector: Python.Runtime.PyObject, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, interval: datetime.timedelta) -> QuantConnect.Algorithm.Framework.Selection.CustomUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.CustomUniverseSelectionModel:
        pass


class EmaCrossUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.FundamentalUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Selection.FundamentalUniverseSelectionModel that subscribes
                to symbols with the larger delta by percentage between the two exponential moving average
    
    EmaCrossUniverseSelectionModel(fastPeriod: int, slowPeriod: int, universeCount: int, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    """
    def SelectCoarse(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, coarse: typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]) -> typing.List[QuantConnect.Symbol]:
        pass

    def __init__(self, fastPeriod: int, slowPeriod: int, universeCount: int, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.EmaCrossUniverseSelectionModel:
        pass
