from .____init___6 import *
import typing
import System.Timers
import System.Threading.Tasks
import System.Threading
import System.Text
import System.IO
import System.Globalization
import System.Drawing
import System.Collections.Generic
import System.Collections.Concurrent
import System.Collections
import System
import QuantConnect.Util
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Packets
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect
import Python.Runtime
import NodaTime
import Newtonsoft.Json
import datetime


class SymbolValueJsonConverter(Newtonsoft.Json.JsonConverter):
    """
    Defines a Newtonsoft.Json.JsonConverter to be used when you only want to serialize
                the QuantConnect.Symbol.Value property instead of the full QuantConnect.Symbol
                instance
    
    SymbolValueJsonConverter()
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass


class TickType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Types of tick data
    
    enum TickType, values: OpenInterest (2), Quote (1), Trade (0)
    """
    value__: int
    OpenInterest: 'TickType'
    Quote: 'TickType'
    Trade: 'TickType'


class Time(System.object):
    """ Time helper class collection for working with trading dates """
    @staticmethod
    def Abs(timeSpan: datetime.timedelta) -> datetime.timedelta:
        pass

    @staticmethod
    def DateTimeToUnixTimeStamp(time: datetime.datetime) -> float:
        pass

    @staticmethod
    def DateTimeToUnixTimeStampMilliseconds(time: datetime.datetime) -> float:
        pass

    @staticmethod
    def DateTimeToUnixTimeStampNanoseconds(time: datetime.datetime) -> int:
        pass

    @staticmethod
    def EachDay(from_: datetime.datetime, thru: datetime.datetime) -> typing.List[datetime.datetime]:
        pass

    @staticmethod
    @typing.overload
    def EachTradeableDay(securities: typing.List[QuantConnect.Securities.Security], from_: datetime.datetime, thru: datetime.datetime) -> typing.List[datetime.datetime]:
        pass

    @staticmethod
    @typing.overload
    def EachTradeableDay(security: QuantConnect.Securities.Security, from_: datetime.datetime, thru: datetime.datetime) -> typing.List[datetime.datetime]:
        pass

    @staticmethod
    @typing.overload
    def EachTradeableDay(exchange: QuantConnect.Securities.SecurityExchangeHours, from_: datetime.datetime, thru: datetime.datetime) -> typing.List[datetime.datetime]:
        pass

    def EachTradeableDay(self, *args) -> typing.List[datetime.datetime]:
        pass

    @staticmethod
    def EachTradeableDayInTimeZone(exchange: QuantConnect.Securities.SecurityExchangeHours, from_: datetime.datetime, thru: datetime.datetime, timeZone: NodaTime.DateTimeZone, includeExtendedMarketHours: bool) -> typing.List[datetime.datetime]:
        pass

    @staticmethod
    def GetEndTimeForTradeBars(exchangeHours: QuantConnect.Securities.SecurityExchangeHours, start: datetime.datetime, barSize: datetime.timedelta, barCount: int, extendedMarketHours: bool) -> datetime.datetime:
        pass

    @staticmethod
    def GetNumberOfTradeBarsInInterval(exchangeHours: QuantConnect.Securities.SecurityExchangeHours, start: datetime.datetime, end: datetime.datetime, barSize: datetime.timedelta) -> int:
        pass

    @staticmethod
    def GetStartTimeForTradeBars(exchangeHours: QuantConnect.Securities.SecurityExchangeHours, end: datetime.datetime, barSize: datetime.timedelta, barCount: int, extendedMarketHours: bool) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def Max(one: datetime.timedelta, two: datetime.timedelta) -> datetime.timedelta:
        pass

    @staticmethod
    @typing.overload
    def Max(one: datetime.datetime, two: datetime.datetime) -> datetime.datetime:
        pass

    def Max(self, *args) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def Min(one: datetime.timedelta, two: datetime.timedelta) -> datetime.timedelta:
        pass

    @staticmethod
    @typing.overload
    def Min(one: datetime.datetime, two: datetime.datetime) -> datetime.datetime:
        pass

    def Min(self, *args) -> datetime.datetime:
        pass

    @staticmethod
    def Multiply(interval: datetime.timedelta, multiplier: float) -> datetime.timedelta:
        pass

    @staticmethod
    def NormalizeInstantWithinRange(start: datetime.datetime, current: datetime.datetime, period: datetime.timedelta) -> float:
        pass

    @staticmethod
    def NormalizeTimeStep(period: datetime.timedelta, stepSize: datetime.timedelta) -> float:
        pass

    @staticmethod
    def ParseDate(dateToParse: str) -> datetime.datetime:
        pass

    @staticmethod
    def TimeStamp() -> float:
        pass

    @staticmethod
    def TradableDate(securities: typing.List[QuantConnect.Securities.Security], day: datetime.datetime) -> bool:
        pass

    @staticmethod
    def TradeableDates(securities: typing.List[QuantConnect.Securities.Security], start: datetime.datetime, finish: datetime.datetime) -> int:
        pass

    @staticmethod
    def UnixMillisecondTimeStampToDateTime(unixTimeStamp: float) -> datetime.datetime:
        pass

    @staticmethod
    def UnixNanosecondTimeStampToDateTime(unixTimeStamp: int) -> datetime.datetime:
        pass

    @staticmethod
    def UnixTimeStampToDateTime(unixTimeStamp: float) -> datetime.datetime:
        pass

    BeginningOfTime: DateTime
    DateTimeWithZone: type
    EndOfTime: DateTime
    EndOfTimeTimeSpan: TimeSpan
    MaxTimeSpan: TimeSpan
    OneDay: TimeSpan
    OneHour: TimeSpan
    OneMillisecond: TimeSpan
    OneMinute: TimeSpan
    OneSecond: TimeSpan
    OneYear: TimeSpan
    __all__: list


class TimeKeeper(System.object, QuantConnect.Interfaces.ITimeKeeper):
    """
    Provides a means of centralizing time for various time zones.
    
    TimeKeeper(utcDateTime: DateTime, *timeZones: Array[DateTimeZone])
    TimeKeeper(utcDateTime: DateTime, timeZones: IEnumerable[DateTimeZone])
    """
    def AddTimeZone(self, timeZone: NodaTime.DateTimeZone) -> None:
        pass

    def GetLocalTimeKeeper(self, timeZone: NodaTime.DateTimeZone) -> QuantConnect.LocalTimeKeeper:
        pass

    def GetTimeIn(self, timeZone: NodaTime.DateTimeZone) -> datetime.datetime:
        pass

    def SetUtcDateTime(self, utcDateTime: datetime.datetime) -> None:
        pass

    @typing.overload
    def __init__(self, utcDateTime: datetime.datetime, timeZones: typing.List[NodaTime.DateTimeZone]) -> QuantConnect.TimeKeeper:
        pass

    @typing.overload
    def __init__(self, utcDateTime: datetime.datetime, timeZones: typing.List[NodaTime.DateTimeZone]) -> QuantConnect.TimeKeeper:
        pass

    def __init__(self, *args) -> QuantConnect.TimeKeeper:
        pass

    UtcTime: datetime.datetime



class TimeUpdatedEventArgs(System.EventArgs):
    """
    Event arguments class for the QuantConnect.LocalTimeKeeper.TimeUpdated event
    
    TimeUpdatedEventArgs(time: DateTime, timeZone: DateTimeZone)
    """
    def __init__(self, time: datetime.datetime, timeZone: NodaTime.DateTimeZone) -> QuantConnect.TimeUpdatedEventArgs:
        pass

    Time: datetime.datetime
    TimeZone: NodaTime.DateTimeZone

class TimeZoneOffsetProvider(System.object):
    """
    Represents the discontinuties in a single time zone and provides offsets to UTC.
                This type assumes that times will be asked in a forward marching manner.
                This type is not thread safe.
    
    TimeZoneOffsetProvider(timeZone: DateTimeZone, utcStartTime: DateTime, utcEndTime: DateTime)
    """
    def ConvertFromUtc(self, utcTime: datetime.datetime) -> datetime.datetime:
        pass

    def ConvertToUtc(self, localTime: datetime.datetime) -> datetime.datetime:
        pass

    def GetNextDiscontinuity(self) -> int:
        pass

    def GetOffsetTicks(self, utcTime: datetime.datetime) -> int:
        pass

    def __init__(self, timeZone: NodaTime.DateTimeZone, utcStartTime: datetime.datetime, utcEndTime: datetime.datetime) -> QuantConnect.TimeZoneOffsetProvider:
        pass

    TimeZone: NodaTime.DateTimeZone



class TimeZones(System.object):
    """ Provides access to common time zones """
    Amsterdam: CachedDateTimeZone
    Anchorage: CachedDateTimeZone
    Athens: CachedDateTimeZone
    Auckland: CachedDateTimeZone
    Berlin: CachedDateTimeZone
    Brisbane: CachedDateTimeZone
    Bucharest: CachedDateTimeZone
    BuenosAires: CachedDateTimeZone
    Cairo: CachedDateTimeZone
    Chicago: CachedDateTimeZone
    Denver: CachedDateTimeZone
    Detroit: CachedDateTimeZone
    Dublin: CachedDateTimeZone
    EasternStandard: FixedDateTimeZone
    Helsinki: CachedDateTimeZone
    HongKong: CachedDateTimeZone
    Honolulu: CachedDateTimeZone
    Istanbul: CachedDateTimeZone
    Jerusalem: CachedDateTimeZone
    Johannesburg: CachedDateTimeZone
    London: CachedDateTimeZone
    LosAngeles: CachedDateTimeZone
    Madrid: CachedDateTimeZone
    Melbourne: CachedDateTimeZone
    MexicoCity: CachedDateTimeZone
    Minsk: CachedDateTimeZone
    Moscow: CachedDateTimeZone
    NewYork: CachedDateTimeZone
    Paris: CachedDateTimeZone
    Phoenix: CachedDateTimeZone
    Rome: CachedDateTimeZone
    SaoPaulo: CachedDateTimeZone
    Shanghai: CachedDateTimeZone
    Sydney: CachedDateTimeZone
    Tokyo: CachedDateTimeZone
    Toronto: CachedDateTimeZone
    Utc: FixedDateTimeZone
    Vancouver: CachedDateTimeZone
    Zurich: CachedDateTimeZone
    __all__: list


class TradingCalendar(System.object):
    """
    Class represents trading calendar, populated with variety of events relevant to currently trading instruments
    
    TradingCalendar(securityManager: SecurityManager, marketHoursDatabase: MarketHoursDatabase)
    """
    def GetDaysByType(self, type: QuantConnect.TradingDayType, start: datetime.datetime, end: datetime.datetime) -> typing.List[QuantConnect.TradingDay]:
        pass

    @typing.overload
    def GetTradingDay(self) -> QuantConnect.TradingDay:
        pass

    @typing.overload
    def GetTradingDay(self, day: datetime.datetime) -> QuantConnect.TradingDay:
        pass

    def GetTradingDay(self, *args) -> QuantConnect.TradingDay:
        pass

    def GetTradingDays(self, start: datetime.datetime, end: datetime.datetime) -> typing.List[QuantConnect.TradingDay]:
        pass

    def __init__(self, securityManager: QuantConnect.Securities.SecurityManager, marketHoursDatabase: QuantConnect.Securities.MarketHoursDatabase) -> QuantConnect.TradingCalendar:
        pass
