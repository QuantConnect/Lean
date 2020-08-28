from .__Scheduling_1 import *
import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect
import Python.Runtime
import NodaTime
import datetime

# no functions
# classes

class CompositeTimeRule(System.object, QuantConnect.Scheduling.ITimeRule):
    """
    Combines multiple time rules into a single rule that emits for each rule
    
    CompositeTimeRule(*timeRules: Array[ITimeRule])
    CompositeTimeRule(timeRules: IEnumerable[ITimeRule])
    """
    def CreateUtcEventTimes(self, dates: typing.List[datetime.datetime]) -> typing.List[datetime.datetime]:
        pass

    @typing.overload
    def __init__(self, timeRules: typing.List[QuantConnect.Scheduling.ITimeRule]) -> QuantConnect.Scheduling.CompositeTimeRule:
        pass

    @typing.overload
    def __init__(self, timeRules: typing.List[QuantConnect.Scheduling.ITimeRule]) -> QuantConnect.Scheduling.CompositeTimeRule:
        pass

    def __init__(self, *args) -> QuantConnect.Scheduling.CompositeTimeRule:
        pass

    Name: str

    Rules: typing.List[QuantConnect.Scheduling.ITimeRule]


class DateRules(System.object):
    """
    Helper class used to provide better syntax when defining date rules
    
    DateRules(securities: SecurityManager, timeZone: DateTimeZone)
    """
    @typing.overload
    def Every(self, day: System.DayOfWeek) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def Every(self, days: typing.List[System.DayOfWeek]) -> QuantConnect.Scheduling.IDateRule:
        pass

    def Every(self, *args) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def EveryDay(self) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def EveryDay(self, symbol: QuantConnect.Symbol) -> QuantConnect.Scheduling.IDateRule:
        pass

    def EveryDay(self, *args) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def MonthEnd(self) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def MonthEnd(self, symbol: QuantConnect.Symbol) -> QuantConnect.Scheduling.IDateRule:
        pass

    def MonthEnd(self, *args) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def MonthStart(self) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def MonthStart(self, symbol: QuantConnect.Symbol) -> QuantConnect.Scheduling.IDateRule:
        pass

    def MonthStart(self, *args) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def On(self, year: int, month: int, day: int) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def On(self, dates: typing.List[datetime.datetime]) -> QuantConnect.Scheduling.IDateRule:
        pass

    def On(self, *args) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def WeekEnd(self) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def WeekEnd(self, symbol: QuantConnect.Symbol) -> QuantConnect.Scheduling.IDateRule:
        pass

    def WeekEnd(self, *args) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def WeekStart(self) -> QuantConnect.Scheduling.IDateRule:
        pass

    @typing.overload
    def WeekStart(self, symbol: QuantConnect.Symbol) -> QuantConnect.Scheduling.IDateRule:
        pass

    def WeekStart(self, *args) -> QuantConnect.Scheduling.IDateRule:
        pass

    def __init__(self, securities: QuantConnect.Securities.SecurityManager, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.DateRules:
        pass

    Today: QuantConnect.Scheduling.IDateRule

    Tomorrow: QuantConnect.Scheduling.IDateRule



class FluentScheduledEventBuilder(System.object, QuantConnect.Scheduling.IFluentSchedulingDateSpecifier, QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier, QuantConnect.Scheduling.IFluentSchedulingRunnable):
    """
    Provides a builder class to allow for fluent syntax when constructing new events
    
    FluentScheduledEventBuilder(schedule: ScheduleManager, securities: SecurityManager, name: str)
    """
    def __init__(self, schedule: QuantConnect.Scheduling.ScheduleManager, securities: QuantConnect.Securities.SecurityManager, name: str) -> QuantConnect.Scheduling.FluentScheduledEventBuilder:
        pass


class FuncDateRule(System.object, QuantConnect.Scheduling.IDateRule):
    """
    Uses a function to define an enumerable of dates over a requested start/end period
    
    FuncDateRule(name: str, getDatesFunction: Func[DateTime, DateTime, IEnumerable[DateTime]])
    """
    def GetDates(self, start: datetime.datetime, end: datetime.datetime) -> typing.List[datetime.datetime]:
        pass

    def __init__(self, name: str, getDatesFunction: typing.Callable[[datetime.datetime, datetime.datetime], typing.List[datetime.datetime]]) -> QuantConnect.Scheduling.FuncDateRule:
        pass

    Name: str



class FuncTimeRule(System.object, QuantConnect.Scheduling.ITimeRule):
    """
    Uses a function to define a time rule as a projection of date times to date times
    
    FuncTimeRule(name: str, createUtcEventTimesFunction: Func[IEnumerable[DateTime], IEnumerable[DateTime]])
    """
    def CreateUtcEventTimes(self, dates: typing.List[datetime.datetime]) -> typing.List[datetime.datetime]:
        pass

    def __init__(self, name: str, createUtcEventTimesFunction: typing.Callable[[typing.List[datetime.datetime]], typing.List[datetime.datetime]]) -> QuantConnect.Scheduling.FuncTimeRule:
        pass

    Name: str



class IDateRule:
    """ Specifies dates that events should be fired, used in conjunction with the QuantConnect.Scheduling.ITimeRule """
    def GetDates(self, start: datetime.datetime, end: datetime.datetime) -> typing.List[datetime.datetime]:
        pass

    Name: str



class IEventSchedule:
    """ Provides the ability to add/remove scheduled events from the real time handler """
    def Add(self, scheduledEvent: QuantConnect.Scheduling.ScheduledEvent) -> None:
        pass

    def Remove(self, scheduledEvent: QuantConnect.Scheduling.ScheduledEvent) -> None:
        pass


class IFluentSchedulingDateSpecifier:
    """ Specifies the date rule component of a scheduled event """
    def Every(self, days: typing.List[System.DayOfWeek]) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    @typing.overload
    def EveryDay(self) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    @typing.overload
    def EveryDay(self, symbol: QuantConnect.Symbol) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    def EveryDay(self, *args) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    @typing.overload
    def MonthStart(self) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    @typing.overload
    def MonthStart(self, symbol: QuantConnect.Symbol) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    def MonthStart(self, *args) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    @typing.overload
    def On(self, year: int, month: int, day: int) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    @typing.overload
    def On(self, dates: typing.List[datetime.datetime]) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    def On(self, *args) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass

    def Where(self, predicate: typing.Callable[[datetime.datetime], bool]) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass


class IFluentSchedulingTimeSpecifier:
    """ Specifies the time rule component of a scheduled event """
    def AfterMarketOpen(self, symbol: QuantConnect.Symbol, minutesAfterOpen: float, extendedMarketOpen: bool) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    @typing.overload
    def At(self, hour: int, minute: int, second: int) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    @typing.overload
    def At(self, hour: int, minute: int, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    @typing.overload
    def At(self, hour: int, minute: int, second: int, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    @typing.overload
    def At(self, timeOfDay: datetime.timedelta, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    @typing.overload
    def At(self, timeOfDay: datetime.timedelta) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    def At(self, *args) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    def BeforeMarketClose(self, symbol: QuantConnect.Symbol, minuteBeforeClose: float, extendedMarketClose: bool) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    def Every(self, interval: datetime.timedelta) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    def Where(self, predicate: typing.Callable[[datetime.datetime], bool]) -> QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier:
        pass


class IFluentSchedulingRunnable(QuantConnect.Scheduling.IFluentSchedulingTimeSpecifier):
    """ Specifies the callback component of a scheduled event, as well as final filters """
    def DuringMarketHours(self, symbol: QuantConnect.Symbol, extendedMarket: bool) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass

    @typing.overload
    def Run(self, callback: System.Action) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def Run(self, callback: typing.Callable[[datetime.datetime], None]) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def Run(self, callback: typing.Callable[[str, datetime.datetime], None]) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def Run(self, *args) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def Where(self, predicate: typing.Callable[[datetime.datetime], bool]) -> QuantConnect.Scheduling.IFluentSchedulingRunnable:
        pass
