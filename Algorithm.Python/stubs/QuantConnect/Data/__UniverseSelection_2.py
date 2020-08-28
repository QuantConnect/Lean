from .__UniverseSelection_3 import *
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



class FuturesChainUniverseDataCollection(QuantConnect.Data.UniverseSelection.BaseDataCollection, QuantConnect.Data.IBaseData):
    """
    Defines the universe selection data type for QuantConnect.Data.UniverseSelection.FuturesChainUniverse
    
    FuturesChainUniverseDataCollection()
    FuturesChainUniverseDataCollection(time: DateTime, symbol: Symbol, data: List[BaseData])
    FuturesChainUniverseDataCollection(time: DateTime, endTime: DateTime, symbol: Symbol, data: List[BaseData])
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
    def __init__(self) -> QuantConnect.Data.UniverseSelection.FuturesChainUniverseDataCollection:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, symbol: QuantConnect.Symbol, data: typing.List[QuantConnect.Data.BaseData]) -> QuantConnect.Data.UniverseSelection.FuturesChainUniverseDataCollection:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, endTime: datetime.datetime, symbol: QuantConnect.Symbol, data: typing.List[QuantConnect.Data.BaseData]) -> QuantConnect.Data.UniverseSelection.FuturesChainUniverseDataCollection:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.FuturesChainUniverseDataCollection:
        pass

    FilteredContracts: System.Collections.Generic.HashSet[QuantConnect.Symbol]



class GetSubscriptionRequestsUniverseDecorator(QuantConnect.Data.UniverseSelection.UniverseDecorator, System.IDisposable):
    """
    Provides a universe decoration that replaces the implementation of QuantConnect.Data.UniverseSelection.GetSubscriptionRequestsUniverseDecorator.GetSubscriptionRequests(QuantConnect.Securities.Security,System.DateTime,System.DateTime)
    
    GetSubscriptionRequestsUniverseDecorator(universe: Universe, getRequests: GetSubscriptionRequestsDelegate)
    """
    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    @typing.overload
    def GetSubscriptionRequests(self, security: QuantConnect.Securities.Security, currentTimeUtc: datetime.datetime, maximumEndTimeUtc: datetime.datetime, subscriptionService: QuantConnect.Interfaces.ISubscriptionDataConfigService) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def GetSubscriptionRequests(self, *args) -> typing.List[QuantConnect.Data.UniverseSelection.SubscriptionRequest]:
        pass

    def __init__(self, universe: QuantConnect.Data.UniverseSelection.Universe, getRequests: QuantConnect.Data.UniverseSelection.GetSubscriptionRequestsDelegate) -> QuantConnect.Data.UniverseSelection.GetSubscriptionRequestsUniverseDecorator:
        pass

    Universe: QuantConnect.Data.UniverseSelection.Universe
    GetSubscriptionRequestsDelegate: type


class ITimeTriggeredUniverse:
    """
    A universe implementing this interface will NOT use it's SubscriptionDataConfig to generate data
                that is used to 'pulse' the universe selection function -- instead, the times output by
                GetTriggerTimes are used to 'pulse' the universe selection function WITHOUT data.
    """
    def GetTriggerTimes(self, startTimeUtc: datetime.datetime, endTimeUtc: datetime.datetime, marketHoursDatabase: QuantConnect.Securities.MarketHoursDatabase) -> typing.List[datetime.datetime]:
        pass


class OptionChainUniverse(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable):
    """
    Defines a universe for a single option chain
    
    OptionChainUniverse(option: Option, universeSettings: UniverseSettings, liveMode: bool)
    OptionChainUniverse(option: Option, universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer, liveMode: bool)
    """
    def CanRemoveMember(self, utcTime: datetime.datetime, security: QuantConnect.Securities.Security) -> bool:
        pass

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

    @typing.overload
    def __init__(self, option: QuantConnect.Securities.Option.Option, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, liveMode: bool) -> QuantConnect.Data.UniverseSelection.OptionChainUniverse:
        pass

    @typing.overload
    def __init__(self, option: QuantConnect.Securities.Option.Option, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer, liveMode: bool) -> QuantConnect.Data.UniverseSelection.OptionChainUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.OptionChainUniverse:
        pass

    Option: QuantConnect.Securities.Option.Option

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings



class OptionChainUniverseDataCollection(QuantConnect.Data.UniverseSelection.BaseDataCollection, QuantConnect.Data.IBaseData):
    """
    Defines the universe selection data type for QuantConnect.Data.UniverseSelection.OptionChainUniverse
    
    OptionChainUniverseDataCollection()
    OptionChainUniverseDataCollection(time: DateTime, symbol: Symbol, data: List[BaseData], underlying: BaseData)
    OptionChainUniverseDataCollection(time: DateTime, endTime: DateTime, symbol: Symbol, data: List[BaseData], underlying: BaseData)
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
    def __init__(self) -> QuantConnect.Data.UniverseSelection.OptionChainUniverseDataCollection:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, symbol: QuantConnect.Symbol, data: typing.List[QuantConnect.Data.BaseData], underlying: QuantConnect.Data.BaseData) -> QuantConnect.Data.UniverseSelection.OptionChainUniverseDataCollection:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, endTime: datetime.datetime, symbol: QuantConnect.Symbol, data: typing.List[QuantConnect.Data.BaseData], underlying: QuantConnect.Data.BaseData) -> QuantConnect.Data.UniverseSelection.OptionChainUniverseDataCollection:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.OptionChainUniverseDataCollection:
        pass

    FilteredContracts: System.Collections.Generic.HashSet[QuantConnect.Symbol]

    Underlying: QuantConnect.Data.BaseData



class ScheduledUniverse(QuantConnect.Data.UniverseSelection.Universe, System.IDisposable, QuantConnect.Data.UniverseSelection.ITimeTriggeredUniverse):
    """
    Defines a user that is fired based on a specified QuantConnect.Scheduling.IDateRule and QuantConnect.Scheduling.ITimeRule
    
    ScheduledUniverse(timeZone: DateTimeZone, dateRule: IDateRule, timeRule: ITimeRule, selector: Func[DateTime, IEnumerable[Symbol]], settings: UniverseSettings, securityInitializer: ISecurityInitializer)
    ScheduledUniverse(dateRule: IDateRule, timeRule: ITimeRule, selector: Func[DateTime, IEnumerable[Symbol]], settings: UniverseSettings, securityInitializer: ISecurityInitializer)
    ScheduledUniverse(timeZone: DateTimeZone, dateRule: IDateRule, timeRule: ITimeRule, selector: PyObject, settings: UniverseSettings, securityInitializer: ISecurityInitializer)
    ScheduledUniverse(dateRule: IDateRule, timeRule: ITimeRule, selector: PyObject, settings: UniverseSettings, securityInitializer: ISecurityInitializer)
    """
    def GetTriggerTimes(self, startTimeUtc: datetime.datetime, endTimeUtc: datetime.datetime, marketHoursDatabase: QuantConnect.Securities.MarketHoursDatabase) -> typing.List[datetime.datetime]:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self, timeZone: NodaTime.DateTimeZone, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, selector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], settings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Data.UniverseSelection.ScheduledUniverse:
        pass

    @typing.overload
    def __init__(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, selector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], settings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Data.UniverseSelection.ScheduledUniverse:
        pass

    @typing.overload
    def __init__(self, timeZone: NodaTime.DateTimeZone, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, selector: Python.Runtime.PyObject, settings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Data.UniverseSelection.ScheduledUniverse:
        pass

    @typing.overload
    def __init__(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, selector: Python.Runtime.PyObject, settings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Data.UniverseSelection.ScheduledUniverse:
        pass

    def __init__(self, *args) -> QuantConnect.Data.UniverseSelection.ScheduledUniverse:
        pass

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings
