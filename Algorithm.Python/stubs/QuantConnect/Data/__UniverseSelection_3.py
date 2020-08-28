from .__UniverseSelection_4 import *
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



class SecurityChanges(System.object):
    """
    Defines the additions and subtractions to the algorithm's security subscriptions
    
    SecurityChanges(addedSecurities: IEnumerable[Security], removedSecurities: IEnumerable[Security])
    SecurityChanges(changes: SecurityChanges)
    """
    @staticmethod
    def Added(securities: typing.List[QuantConnect.Securities.Security]) -> QuantConnect.Data.UniverseSelection.SecurityChanges:
        pass

    @staticmethod
    def Removed(securities: typing.List[QuantConnect.Securities.Security]) -> QuantConnect.Data.UniverseSelection.SecurityChanges:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self, addedSecurities: typing.List[QuantConnect.Securities.Security], removedSecurities: typing.List[QuantConnect.Securities.Security]) -> QuantConnect.Data.UniverseSelection.SecurityChanges:
        pass

    @typing.overload
    def __init__(self, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> QuantConnect.Data.UniverseSelection.SecurityChanges:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.SecurityChanges:
        pass

    AddedSecurities: typing.List[QuantConnect.Securities.Security]

    Count: int

    FilterCustomSecurities: bool

    RemovedSecurities: typing.List[QuantConnect.Securities.Security]


    None_: getset_descriptor


class SubscriptionRequest(System.object):
    """
    Defines the parameters required to add a subscription to a data feed.
    
    SubscriptionRequest(isUniverseSubscription: bool, universe: Universe, security: Security, configuration: SubscriptionDataConfig, startTimeUtc: DateTime, endTimeUtc: DateTime)
    SubscriptionRequest(template: SubscriptionRequest, isUniverseSubscription: Nullable[bool], universe: Universe, security: Security, configuration: SubscriptionDataConfig, startTimeUtc: Nullable[DateTime], endTimeUtc: Nullable[DateTime])
    """
    @typing.overload
    def __init__(self, isUniverseSubscription: bool, universe: QuantConnect.Data.UniverseSelection.Universe, security: QuantConnect.Securities.Security, configuration: QuantConnect.Data.SubscriptionDataConfig, startTimeUtc: datetime.datetime, endTimeUtc: datetime.datetime) -> QuantConnect.Data.UniverseSelection.SubscriptionRequest:
        pass

    @typing.overload
    def __init__(self, template: QuantConnect.Data.UniverseSelection.SubscriptionRequest, isUniverseSubscription: typing.Optional[bool], universe: QuantConnect.Data.UniverseSelection.Universe, security: QuantConnect.Securities.Security, configuration: QuantConnect.Data.SubscriptionDataConfig, startTimeUtc: typing.Optional[datetime.datetime], endTimeUtc: typing.Optional[datetime.datetime]) -> QuantConnect.Data.UniverseSelection.SubscriptionRequest:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.SubscriptionRequest:
        pass

    Configuration: QuantConnect.Data.SubscriptionDataConfig

    EndTimeLocal: datetime.datetime

    EndTimeUtc: datetime.datetime

    IsUniverseSubscription: bool

    Security: QuantConnect.Securities.Security

    StartTimeLocal: datetime.datetime

    StartTimeUtc: datetime.datetime

    TradableDays: typing.List[datetime.datetime]

    Universe: QuantConnect.Data.UniverseSelection.Universe



class UniverseExtensions(System.object):
    """ Provides extension methods for the QuantConnect.Data.UniverseSelection.Universe class """
    @staticmethod
    def ChainedTo(first: QuantConnect.Data.UniverseSelection.Universe, second: QuantConnect.Data.UniverseSelection.Universe, configurationPerSymbol: bool) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    @staticmethod
    def PrefilterUsing(second: QuantConnect.Data.UniverseSelection.Universe, first: QuantConnect.Data.UniverseSelection.Universe) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    __all__: list


class UniversePythonWrapper(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable):
    """
    Provides an implementation of QuantConnect.Data.UniverseSelection.Universe that wraps a Python.Runtime.PyObject object
    
    UniversePythonWrapper(universe: PyObject)
    """
    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime, subscriptionService: QuantConnect.Interfaces.ISubscriptionDataConfigService) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def GetSubscriptionRequests(self, *args) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    def __init__(self, universe: Python.Runtime.PyObject) -> QuantConnect.Data.UniverseSelection.UniversePythonWrapper:
        pass

    Configuration: QuantConnect.Data.SubscriptionDataConfig

    DisposeRequested: bool

    Securities: System.Collections.Concurrent.ConcurrentDictionary[QuantConnect.Symbol, QuantConnect.Data.UniverseSelection.Member]

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings



class UniverseSettings(System.object):
    """
    Defines settings required when adding a subscription
    
    UniverseSettings(resolution: Resolution, leverage: Decimal, fillForward: bool, extendedMarketHours: bool, minimumTimeInUniverse: TimeSpan, dataNormalizationMode: DataNormalizationMode)
    """
    def __init__(self, resolution: QuantConnect.Resolution, leverage: float, fillForward: bool, extendedMarketHours: bool, minimumTimeInUniverse: datetime.timedelta, dataNormalizationMode: QuantConnect.DataNormalizationMode) -> QuantConnect.Data.UniverseSelection.UniverseSettings:
        pass

    DataNormalizationMode: QuantConnect.DataNormalizationMode
    ExtendedMarketHours: bool
    FillForward: bool
    Leverage: float
    MinimumTimeInUniverse: datetime.timedelta
    Resolution: QuantConnect.Resolution

class UserDefinedUniverse(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable, QuantConnect.Data.UniverseSelection.ITimeTriggeredUniverse, System.Collections.Specialized.INotifyCollectionChanged):
    """
    Represents the universe defined by the user's algorithm. This is
                the default universe where manually added securities live by
                market/security type. They can also be manually generated and
                can be configured to fire on certain interval and will always
                return the internal list of symbols.
    
    UserDefinedUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, interval: TimeSpan, symbols: IEnumerable[Symbol])
    UserDefinedUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, interval: TimeSpan, selector: Func[DateTime, IEnumerable[str]])
    UserDefinedUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, interval: TimeSpan, symbols: IEnumerable[Symbol])
    UserDefinedUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings, interval: TimeSpan, selector: Func[DateTime, IEnumerable[str]])
    """
    @typing.overload
    def Add(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    @typing.overload
    def Add(self, subscriptionDataConfig: QuantConnect.Data.SubscriptionDataConfig) -> bool:
        pass

    def Add(self, *args) -> bool:
        pass

    @staticmethod
    def CreateSymbol(securityType: QuantConnect.SecurityType, market: str) -> QuantConnect.Symbol:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime, subscriptionService: QuantConnect.Interfaces.ISubscriptionDataConfigService) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def GetSubscriptionRequests(self, *args) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def GetTriggerTimes(self, startTimeUtc: datetime.datetime, endTimeUtc: datetime.datetime, marketHoursDatabase: QuantConnect.Securities.MarketHoursDatabase) -> typing.List[datetime.datetime]:
        pass

    def Remove(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, interval: datetime.timedelta, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.Data.UniverseSelection.UserDefinedUniverse:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, interval: datetime.timedelta, selector: typing.Callable[[datetime.datetime], typing.List[str]]) -> QuantConnect.Data.UniverseSelection.UserDefinedUniverse:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, interval: datetime.timedelta, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.Data.UniverseSelection.UserDefinedUniverse:
        pass

    @typing.overload
    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, interval: datetime.timedelta, selector: typing.Callable[[datetime.datetime], typing.List[str]]) -> QuantConnect.Data.UniverseSelection.UserDefinedUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.UserDefinedUniverse:
        pass

    Interval: datetime.timedelta

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings


    CollectionChanged: BoundEvent
