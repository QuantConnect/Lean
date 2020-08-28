from .____init___3 import *
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


class Field(System.object):
    """ Provides static properties to be used as selectors with the indicator system """
    __all__: list


class Globals(System.object):
    """ Provides application level constant values """
    @staticmethod
    def Reset() -> None:
        pass

    Cache: str
    CacheDataFolder: str
    DataFolder: str
    Version: str
    __all__: list


class Holding(System.object):
    """
    Singular holding of assets from backend live nodes:
    
    Holding()
    Holding(security: Security)
    """
    def Clone(self) -> QuantConnect.Holding:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Holding:
        pass

    @typing.overload
    def __init__(self, security: QuantConnect.Securities.Security) -> QuantConnect.Holding:
        pass

    def __init__(self, *args) -> QuantConnect.Holding:
        pass

    AveragePrice: float
    ConversionRate: typing.Optional[float]
    CurrencySymbol: str
    MarketPrice: float
    MarketValue: float
    Quantity: float
    Symbol: QuantConnect.Symbol
    Type: QuantConnect.SecurityType
    UnrealizedPnL: float

class IIsolatorLimitResultProvider:
    """
    Provides an abstraction for managing isolator limit results.
                This is originally intended to be used by the training feature to permit a single
                algorithm time loop to extend past the default of ten minutes
    """
    def IsWithinLimit(self) -> QuantConnect.IsolatorLimitResult:
        pass

    def RequestAdditionalTime(self, minutes: int) -> None:
        pass

    def TryRequestAdditionalTime(self, minutes: int) -> bool:
        pass


class InvalidConfigurationDetectedEventArgs(System.EventArgs):
    """
    Event arguments for the QuantConnect.Interfaces.IDataProviderEvents.InvalidConfigurationDetected event
    
    InvalidConfigurationDetectedEventArgs(message: str)
    """
    def __init__(self, message: str) -> QuantConnect.InvalidConfigurationDetectedEventArgs:
        pass

    Message: str



class Isolator(System.object):
    """
    Isolator class - create a new instance of the algorithm and ensure it doesn't
                exceed memory or time execution limits.
    
    Isolator()
    """
    @typing.overload
    def ExecuteWithTimeLimit(self, timeSpan: datetime.timedelta, withinCustomLimits: typing.Callable[[], QuantConnect.IsolatorLimitResult], codeBlock: System.Action, memoryCap: int, sleepIntervalMillis: int, workerThread: QuantConnect.Util.WorkerThread) -> bool:
        pass

    @typing.overload
    def ExecuteWithTimeLimit(self, timeSpan: datetime.timedelta, codeBlock: System.Action, memoryCap: int, sleepIntervalMillis: int, workerThread: QuantConnect.Util.WorkerThread) -> bool:
        pass

    def ExecuteWithTimeLimit(self, *args) -> bool:
        pass

    CancellationToken: System.Threading.CancellationToken

    CancellationTokenSource: System.Threading.CancellationTokenSource

    IsCancellationRequested: bool



class IsolatorLimitResult(System.object):
    """
    Represents the result of the QuantConnect.Isolator limiter callback
    
    IsolatorLimitResult(currentTimeStepElapsed: TimeSpan, errorMessage: str)
    """
    def __init__(self, currentTimeStepElapsed: datetime.timedelta, errorMessage: str) -> QuantConnect.IsolatorLimitResult:
        pass

    CurrentTimeStepElapsed: datetime.timedelta

    ErrorMessage: str

    IsWithinCustomLimits: bool



class IsolatorLimitResultProvider(System.object):
    """ Provides access to the QuantConnect.IsolatorLimitResultProvider.NullIsolatorLimitResultProvider and extension methods supporting QuantConnect.Scheduling.ScheduledEvent """
    @staticmethod
    @typing.overload
    def Consume(isolatorLimitProvider: QuantConnect.IIsolatorLimitResultProvider, scheduledEvent: QuantConnect.Scheduling.ScheduledEvent, scanTimeUtc: datetime.datetime, timeMonitor: QuantConnect.Scheduling.TimeMonitor) -> None:
        pass

    @staticmethod
    @typing.overload
    def Consume(isolatorLimitProvider: QuantConnect.IIsolatorLimitResultProvider, timeProvider: QuantConnect.ITimeProvider, code: System.Action, timeMonitor: QuantConnect.Scheduling.TimeMonitor) -> None:
        pass

    def Consume(self, *args) -> None:
        pass

    Null: NullIsolatorLimitResultProvider
    __all__: list


class ITimeProvider:
    """
    Provides access to the current time in UTC. This doesn't necessarily
                need to be wall-clock time, but rather the current time in some system
    """
    def GetUtcNow(self) -> datetime.datetime:
        pass


class Language(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Multilanguage support enum: which language is this project for the interop bridge converter.
    
    enum Language, values: CSharp (0), FSharp (1), Java (3), Python (4), VisualBasic (2)
    """
    value__: int
    CSharp: 'Language'
    FSharp: 'Language'
    Java: 'Language'
    Python: 'Language'
    VisualBasic: 'Language'


class LocalTimeKeeper(System.object):
    """
    Represents the current local time. This object is created via the QuantConnect.TimeKeeper to
                manage conversions to local time.
    """
    LocalTime: datetime.datetime

    TimeZone: NodaTime.DateTimeZone


    TimeUpdated: BoundEvent


class Market(System.object):
    """ Markets Collection: Soon to be expanded to a collection of items specifying the market hour, timezones and country codes. """
    @staticmethod
    def Add(market: str, identifier: int) -> None:
        pass

    @staticmethod
    def Decode(code: int) -> str:
        pass

    @staticmethod
    def Encode(market: str) -> typing.Optional[int]:
        pass

    Binance: str
    Bitfinex: str
    Bithumb: str
    Bitstamp: str
    Bittrex: str
    CBOE: str
    CBOT: str
    CME: str
    Coinone: str
    COMEX: str
    Dukascopy: str
    FXCM: str
    GDAX: str
    Globex: str
    HitBTC: str
    HKFE: str
    ICE: str
    Kraken: str
    NSE: str
    NYMEX: str
    Oanda: str
    OkCoin: str
    Poloniex: str
    SGX: str
    USA: str
    __all__: list


class MarketCodes(System.object):
    """ Global Market Short Codes and their full versions: (used in tick objects) """
    Canada: Dictionary[str, str]
    US: Dictionary[str, str]
    __all__: list


class MarketDataType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Market data style: is the market data a summary (OHLC style) bar, or is it a time-price value.
    
    enum MarketDataType, values: Auxiliary (3), Base (0), FuturesChain (6), OptionChain (5), QuoteBar (4), Tick (2), TradeBar (1)
    """
    value__: int
    Auxiliary: 'MarketDataType'
    Base: 'MarketDataType'
    FuturesChain: 'MarketDataType'
    OptionChain: 'MarketDataType'
    QuoteBar: 'MarketDataType'
    Tick: 'MarketDataType'
    TradeBar: 'MarketDataType'


class NewTradableDateEventArgs(System.EventArgs):
    """
    Event arguments for the NewTradableDate event
    
    NewTradableDateEventArgs(date: DateTime, lastBaseData: BaseData, symbol: Symbol)
    """
    def __init__(self, date: datetime.datetime, lastBaseData: QuantConnect.Data.BaseData, symbol: QuantConnect.Symbol) -> QuantConnect.NewTradableDateEventArgs:
        pass

    Date: datetime.datetime

    LastBaseData: QuantConnect.Data.BaseData

    Symbol: QuantConnect.Symbol



class NumericalPrecisionLimitedEventArgs(System.EventArgs):
    """
    Event arguments for the QuantConnect.Interfaces.IDataProviderEvents.NumericalPrecisionLimited event
    
    NumericalPrecisionLimitedEventArgs(message: str)
    """
    def __init__(self, message: str) -> QuantConnect.NumericalPrecisionLimitedEventArgs:
        pass

    Message: str



class OptionRight(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the different types of options
    
    enum OptionRight, values: Call (0), Put (1)
    """
    value__: int

class OptionStyle(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the style of an option
    
    enum OptionStyle, values: American (0), European (1)
    """
    value__: int
    American: 'OptionStyle'
    European: 'OptionStyle'


class OS(System.object):
    """ Operating systems class for managing anything that is operation system specific. """
    @staticmethod
    def GetServerStatistics() -> System.Collections.Generic.Dictionary[str, str]:
        pass

    ApplicationMemoryUsed: Int64
    CpuUsage: Decimal
    DriveSpaceRemaining: Int64
    DriveSpaceUsed: Int64
    DriveTotalSpace: Int64
    IsLinux: bool
    IsWindows: bool
    PathSeparation: str
    TotalPhysicalMemoryUsed: Int64
    __all__: list


class Parse(System.object):
    """ Provides methods for parsing strings using System.Globalization.CultureInfo.InvariantCulture """
    @staticmethod
    def DateTime(value: str) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def DateTimeExact(value: str, format: str) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def DateTimeExact(value: str, format: str, dateTimeStyles: System.Globalization.DateTimeStyles) -> datetime.datetime:
        pass

    def DateTimeExact(self, *args) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def Decimal(value: str) -> float:
        pass

    @staticmethod
    @typing.overload
    def Decimal(value: str, numberStyles: System.Globalization.NumberStyles) -> float:
        pass

    def Decimal(self, *args) -> float:
        pass

    @staticmethod
    def Double(value: str) -> float:
        pass

    @staticmethod
    def Int(value: str) -> int:
        pass

    @staticmethod
    @typing.overload
    def Long(value: str) -> int:
        pass

    @staticmethod
    @typing.overload
    def Long(value: str, numberStyles: System.Globalization.NumberStyles) -> int:
        pass

    def Long(self, *args) -> int:
        pass

    @staticmethod
    def TimeSpan(value: str) -> datetime.timedelta:
        pass

    @staticmethod
    @typing.overload
    def TryParse(input: str, value: datetime.timedelta) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryParse(input: str, dateTimeStyle: System.Globalization.DateTimeStyles, value: datetime.datetime) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryParse(input: str, numberStyle: System.Globalization.NumberStyles, value: float) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryParse(input: str, numberStyle: System.Globalization.NumberStyles, value: float) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryParse(input: str, numberStyle: System.Globalization.NumberStyles, value: int) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryParse(input: str, numberStyle: System.Globalization.NumberStyles, value: int) -> bool:
        pass

    def TryParse(self, *args) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryParseExact(input: str, format: str, timeSpanStyle: System.Globalization.TimeSpanStyles, value: datetime.timedelta) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryParseExact(input: str, format: str, dateTimeStyle: System.Globalization.DateTimeStyles, value: datetime.datetime) -> bool:
        pass

    def TryParseExact(self, *args) -> bool:
        pass

    __all__: list
