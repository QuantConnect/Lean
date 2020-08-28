from .____init___4 import *
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


class Period(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    enum Period - Enum of all the analysis periods, AS integers. Reference "Period" Array to access the values
    
    enum Period, values: FifteenMinutes (900), FiveMinutes (300), FourHours (14400), OneHour (3600), OneMinute (60), SixHours (21600), TenMinutes (600), TenSeconds (10), ThirtyMinutes (1800), ThirtySeconds (30), ThreeMinutes (180), TwentyMinutes (1200), TwoHours (7200), TwoMinutes (120)
    """
    value__: int
    FifteenMinutes: 'Period'
    FiveMinutes: 'Period'
    FourHours: 'Period'
    OneHour: 'Period'
    OneMinute: 'Period'
    SixHours: 'Period'
    TenMinutes: 'Period'
    TenSeconds: 'Period'
    ThirtyMinutes: 'Period'
    ThirtySeconds: 'Period'
    ThreeMinutes: 'Period'
    TwentyMinutes: 'Period'
    TwoHours: 'Period'
    TwoMinutes: 'Period'


class ReaderErrorDetectedEventArgs(System.EventArgs):
    """
    Event arguments for the QuantConnect.Interfaces.IDataProviderEvents.ReaderErrorDetected event
    
    ReaderErrorDetectedEventArgs(message: str, stackTrace: str)
    """
    def __init__(self, message: str, stackTrace: str) -> QuantConnect.ReaderErrorDetectedEventArgs:
        pass

    Message: str

    StackTrace: str



class RealTimeProvider(System.object, QuantConnect.ITimeProvider):
    """
    Provides an implementation of QuantConnect.ITimeProvider that
                uses System.DateTime.UtcNow to provide the current time
    
    RealTimeProvider()
    """
    def GetUtcNow(self) -> datetime.datetime:
        pass

    Instance: 'RealTimeProvider'


class RealTimeSynchronizedTimer(System.object):
    """
    Real time timer class for precise callbacks on a millisecond resolution in a self managed thread.
    
    RealTimeSynchronizedTimer()
    RealTimeSynchronizedTimer(period: TimeSpan, callback: Action[DateTime])
    """
    def Pause(self) -> None:
        pass

    def Resume(self) -> None:
        pass

    def Scanner(self) -> None:
        pass

    def Start(self) -> None:
        pass

    def Stop(self) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.RealTimeSynchronizedTimer:
        pass

    @typing.overload
    def __init__(self, period: datetime.timedelta, callback: typing.Callable[[datetime.datetime], None]) -> QuantConnect.RealTimeSynchronizedTimer:
        pass

    def __init__(self, *args) -> QuantConnect.RealTimeSynchronizedTimer:
        pass


class Resolution(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Resolution of data requested.
    
    enum Resolution, values: Daily (4), Hour (3), Minute (2), Second (1), Tick (0)
    """
    value__: int
    Daily: 'Resolution'
    Hour: 'Resolution'
    Minute: 'Resolution'
    Second: 'Resolution'
    Tick: 'Resolution'


class Result(System.object):
    """
    Base class for backtesting and live results that packages result data.
                QuantConnect.Packets.LiveResultQuantConnect.Packets.BacktestResult
    
    Result()
    """
    AlphaRuntimeStatistics: QuantConnect.AlphaRuntimeStatistics
    Charts: System.Collections.Generic.IDictionary[str, QuantConnect.Chart]
    OrderEvents: typing.List[QuantConnect.Orders.OrderEvent]
    Orders: System.Collections.Generic.IDictionary[int, QuantConnect.Orders.Order]
    ProfitLoss: System.Collections.Generic.IDictionary[datetime.datetime, float]
    RuntimeStatistics: System.Collections.Generic.IDictionary[str, str]
    ServerStatistics: System.Collections.Generic.IDictionary[str, str]
    Statistics: System.Collections.Generic.IDictionary[str, str]

class ScatterMarkerSymbol(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Shape or symbol for the marker in a scatter plot
    
    enum ScatterMarkerSymbol, values: Circle (1), Diamond (3), None (0), Square (2), Triangle (4), TriangleDown (5)
    """
    value__: int
    Circle: 'ScatterMarkerSymbol'
    Diamond: 'ScatterMarkerSymbol'
    Square: 'ScatterMarkerSymbol'
    Triangle: 'ScatterMarkerSymbol'
    TriangleDown: 'ScatterMarkerSymbol'
    None_: 'ScatterMarkerSymbol'


class SecurityIdentifier(System.object, System.IEquatable[SecurityIdentifier]):
    """
    Defines a unique identifier for securities
    
    SecurityIdentifier(symbol: str, properties: UInt64)
    SecurityIdentifier(symbol: str, properties: UInt64, underlying: SecurityIdentifier)
    """
    @typing.overload
    def Equals(self, other: QuantConnect.SecurityIdentifier) -> bool:
        pass

    @typing.overload
    def Equals(self, obj: object) -> bool:
        pass

    def Equals(self, *args) -> bool:
        pass

    @staticmethod
    def GenerateBase(dataType: type, symbol: str, market: str, mapSymbol: bool, date: typing.Optional[datetime.datetime]) -> QuantConnect.SecurityIdentifier:
        pass

    @staticmethod
    def GenerateBaseSymbol(dataType: type, symbol: str) -> str:
        pass

    @staticmethod
    def GenerateCfd(symbol: str, market: str) -> QuantConnect.SecurityIdentifier:
        pass

    @staticmethod
    def GenerateConstituentIdentifier(symbol: str, securityType: QuantConnect.SecurityType, market: str) -> QuantConnect.SecurityIdentifier:
        pass

    @staticmethod
    def GenerateCrypto(symbol: str, market: str) -> QuantConnect.SecurityIdentifier:
        pass

    @staticmethod
    @typing.overload
    def GenerateEquity(symbol: str, market: str, mapSymbol: bool, mapFileProvider: QuantConnect.Interfaces.IMapFileProvider, mappingResolveDate: typing.Optional[datetime.datetime]) -> QuantConnect.SecurityIdentifier:
        pass

    @staticmethod
    @typing.overload
    def GenerateEquity(date: datetime.datetime, symbol: str, market: str) -> QuantConnect.SecurityIdentifier:
        pass

    def GenerateEquity(self, *args) -> QuantConnect.SecurityIdentifier:
        pass

    @staticmethod
    def GenerateForex(symbol: str, market: str) -> QuantConnect.SecurityIdentifier:
        pass

    @staticmethod
    def GenerateFuture(expiry: datetime.datetime, symbol: str, market: str) -> QuantConnect.SecurityIdentifier:
        pass

    @staticmethod
    def GenerateOption(expiry: datetime.datetime, underlying: QuantConnect.SecurityIdentifier, market: str, strike: float, optionRight: QuantConnect.OptionRight, optionStyle: QuantConnect.OptionStyle) -> QuantConnect.SecurityIdentifier:
        pass

    def GetHashCode(self) -> int:
        pass

    @staticmethod
    def Parse(value: str) -> QuantConnect.SecurityIdentifier:
        pass

    def ToString(self) -> str:
        pass

    @staticmethod
    def TryParse(value: str, identifier: QuantConnect.SecurityIdentifier) -> bool:
        pass

    @typing.overload
    def __init__(self, symbol: str, properties: int) -> QuantConnect.SecurityIdentifier:
        pass

    @typing.overload
    def __init__(self, symbol: str, properties: int, underlying: QuantConnect.SecurityIdentifier) -> QuantConnect.SecurityIdentifier:
        pass

    def __init__(self, *args) -> QuantConnect.SecurityIdentifier:
        pass

    Date: datetime.datetime

    HasUnderlying: bool

    Market: str

    OptionRight: QuantConnect.OptionRight

    OptionStyle: QuantConnect.OptionStyle

    SecurityType: QuantConnect.SecurityType

    StrikePrice: float

    Symbol: str

    Underlying: QuantConnect.SecurityIdentifier


    DefaultDate: DateTime
    Empty: 'SecurityIdentifier'
    InvalidSymbolCharacters: HashSet[Char]
    None_: HashSet[Char]


class SecurityType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Type of tradable security / underlying asset
    
    enum SecurityType, values: Base (0), Cfd (6), Commodity (3), Crypto (7), Equity (1), Forex (4), Future (5), Option (2)
    """
    value__: int
    Base: 'SecurityType'
    Cfd: 'SecurityType'
    Commodity: 'SecurityType'
    Crypto: 'SecurityType'
    Equity: 'SecurityType'
    Forex: 'SecurityType'
    Future: 'SecurityType'
    Option: 'SecurityType'


class Series(System.object):
    """
    Chart Series Object - Series data and properties for a chart:
    
    Series()
    Series(name: str)
    Series(name: str, type: SeriesType)
    Series(name: str, type: SeriesType, index: int)
    Series(name: str, type: SeriesType, index: int, unit: str)
    Series(name: str, type: SeriesType, unit: str)
    Series(name: str, type: SeriesType, unit: str, color: Color)
    Series(name: str, type: SeriesType, unit: str, color: Color, symbol: ScatterMarkerSymbol)
    """
    @typing.overload
    def AddPoint(self, time: datetime.datetime, value: float) -> None:
        pass

    @typing.overload
    def AddPoint(self, chartPoint: QuantConnect.ChartPoint) -> None:
        pass

    def AddPoint(self, *args) -> None:
        pass

    def Clone(self) -> QuantConnect.Series:
        pass

    def ConsolidateChartPoints(self) -> QuantConnect.ChartPoint:
        pass

    def GetUpdates(self) -> QuantConnect.Series:
        pass

    def Purge(self) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Series:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Series:
        pass

    @typing.overload
    def __init__(self, name: str, type: QuantConnect.SeriesType) -> QuantConnect.Series:
        pass

    @typing.overload
    def __init__(self, name: str, type: QuantConnect.SeriesType, index: int) -> QuantConnect.Series:
        pass

    @typing.overload
    def __init__(self, name: str, type: QuantConnect.SeriesType, index: int, unit: str) -> QuantConnect.Series:
        pass

    @typing.overload
    def __init__(self, name: str, type: QuantConnect.SeriesType, unit: str) -> QuantConnect.Series:
        pass

    @typing.overload
    def __init__(self, name: str, type: QuantConnect.SeriesType, unit: str, color: System.Drawing.Color) -> QuantConnect.Series:
        pass

    @typing.overload
    def __init__(self, name: str, type: QuantConnect.SeriesType, unit: str, color: System.Drawing.Color, symbol: QuantConnect.ScatterMarkerSymbol) -> QuantConnect.Series:
        pass

    def __init__(self, *args) -> QuantConnect.Series:
        pass

    Color: System.Drawing.Color
    Index: int
    Name: str
    ScatterMarkerSymbol: QuantConnect.ScatterMarkerSymbol
    SeriesType: QuantConnect.SeriesType
    Unit: str
    Values: typing.List[QuantConnect.ChartPoint]
