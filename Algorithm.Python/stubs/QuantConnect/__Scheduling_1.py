import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect
import Python.Runtime
import NodaTime
import datetime


class ITimeRule:
    """ Specifies times times on dates for events, used in conjunction with QuantConnect.Scheduling.IDateRule """
    def CreateUtcEventTimes(self, dates: typing.List[datetime.datetime]) -> typing.List[datetime.datetime]:
        pass

    Name: str



class ScheduledEvent(System.object, System.IDisposable):
    """
    Real time self scheduling event
    
    ScheduledEvent(name: str, eventUtcTime: DateTime, callback: Action[str, DateTime])
    ScheduledEvent(name: str, orderedEventUtcTimes: IEnumerable[DateTime], callback: Action[str, DateTime])
    ScheduledEvent(name: str, orderedEventUtcTimes: IEnumerator[DateTime], callback: Action[str, DateTime])
    """
    def Equals(self, obj: object) -> bool:
        pass

    def GetHashCode(self) -> int:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self, name: str, eventUtcTime: datetime.datetime, callback: typing.Callable[[str, datetime.datetime], None]) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def __init__(self, name: str, orderedEventUtcTimes: typing.List[datetime.datetime], callback: typing.Callable[[str, datetime.datetime], None]) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def __init__(self, name: str, orderedEventUtcTimes: System.Collections.Generic.IEnumerator[datetime.datetime], callback: typing.Callable[[str, datetime.datetime], None]) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def __init__(self, *args) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    Enabled: bool

    Name: str

    NextEventUtcTime: datetime.datetime


    AlgorithmEndOfDayDelta: TimeSpan
    EventFired: BoundEvent
    SecurityEndOfDayDelta: TimeSpan


class ScheduledEventException(System.Exception, System.Runtime.Serialization.ISerializable, System.Runtime.InteropServices._Exception):
    """
    Throw this if there is an exception in the callback function of the scheduled event
    
    ScheduledEventException(name: str, message: str, innerException: Exception)
    """
    def __init__(self, name: str, message: str, innerException: System.Exception) -> QuantConnect.Scheduling.ScheduledEventException:
        pass

    ScheduledEventName: str


    SerializeObjectState: BoundEvent


class ScheduleManager(System.object, QuantConnect.Scheduling.IEventSchedule):
    """
    Provides access to the real time handler's event scheduling feature
    
    ScheduleManager(securities: SecurityManager, timeZone: DateTimeZone)
    """
    def Add(self, scheduledEvent: QuantConnect.Scheduling.ScheduledEvent) -> None:
        pass

    @typing.overload
    def Event(self) -> QuantConnect.Scheduling.IFluentSchedulingDateSpecifier:
        pass

    @typing.overload
    def Event(self, name: str) -> QuantConnect.Scheduling.IFluentSchedulingDateSpecifier:
        pass

    def Event(self, *args) -> QuantConnect.Scheduling.IFluentSchedulingDateSpecifier:
        pass

    @typing.overload
    def On(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, callback: System.Action) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def On(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, callback: Python.Runtime.PyObject) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def On(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, callback: typing.Callable[[str, datetime.datetime], None]) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def On(self, name: str, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, callback: System.Action) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def On(self, name: str, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, callback: Python.Runtime.PyObject) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def On(self, name: str, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, callback: typing.Callable[[str, datetime.datetime], None]) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def On(self, *args) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def Remove(self, scheduledEvent: QuantConnect.Scheduling.ScheduledEvent) -> None:
        pass

    @typing.overload
    def Training(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, trainingCode: System.Action) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def Training(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, trainingCode: Python.Runtime.PyObject) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def Training(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, trainingCode: typing.Callable[[datetime.datetime], None]) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def Training(self, *args) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def TrainingNow(self, trainingCode: System.Action) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def TrainingNow(self, trainingCode: Python.Runtime.PyObject) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def TrainingNow(self, *args) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def __init__(self, securities: QuantConnect.Securities.SecurityManager, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.ScheduleManager:
        pass

    DateRules: QuantConnect.Scheduling.DateRules

    TimeRules: QuantConnect.Scheduling.TimeRules



class TimeConsumer(System.object):
    """
    Represents a timer consumer instance
    
    TimeConsumer()
    """
    Finished: bool

    IsolatorLimitProvider: QuantConnect.IIsolatorLimitResultProvider

    NextTimeRequest: typing.Optional[datetime.datetime]

    TimeProvider: QuantConnect.ITimeProvider



class TimeMonitor(System.object, System.IDisposable):
    """
    Helper class that will monitor timer consumers and request more time if required.
                Used by QuantConnect.IsolatorLimitResultProvider
    
    TimeMonitor(monitorIntervalMs: int)
    """
    def Add(self, consumer: QuantConnect.Scheduling.TimeConsumer) -> None:
        pass

    def Dispose(self) -> None:
        pass

    def __init__(self, monitorIntervalMs: int) -> QuantConnect.Scheduling.TimeMonitor:
        pass

    Count: int



class TimeRules(System.object):
    """
    Helper class used to provide better syntax when defining time rules
    
    TimeRules(securities: SecurityManager, timeZone: DateTimeZone)
    """
    def AfterMarketOpen(self, symbol: QuantConnect.Symbol, minutesAfterOpen: float, extendedMarketOpen: bool) -> QuantConnect.Scheduling.ITimeRule:
        pass

    @typing.overload
    def At(self, timeOfDay: datetime.timedelta) -> QuantConnect.Scheduling.ITimeRule:
        pass

    @typing.overload
    def At(self, hour: int, minute: int, second: int) -> QuantConnect.Scheduling.ITimeRule:
        pass

    @typing.overload
    def At(self, hour: int, minute: int, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.ITimeRule:
        pass

    @typing.overload
    def At(self, hour: int, minute: int, second: int, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.ITimeRule:
        pass

    @typing.overload
    def At(self, timeOfDay: datetime.timedelta, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.ITimeRule:
        pass

    def At(self, *args) -> QuantConnect.Scheduling.ITimeRule:
        pass

    def BeforeMarketClose(self, symbol: QuantConnect.Symbol, minutesBeforeClose: float, extendedMarketClose: bool) -> QuantConnect.Scheduling.ITimeRule:
        pass

    def Every(self, interval: datetime.timedelta) -> QuantConnect.Scheduling.ITimeRule:
        pass

    def SetDefaultTimeZone(self, timeZone: NodaTime.DateTimeZone) -> None:
        pass

    def __init__(self, securities: QuantConnect.Securities.SecurityManager, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.TimeRules:
        pass

    Midnight: QuantConnect.Scheduling.ITimeRule

    Noon: QuantConnect.Scheduling.ITimeRule

    Now: QuantConnect.Scheduling.ITimeRule
