from .____init___5 import *
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

class SeriesSampler(System.object):
    """
    A type capable of taking a chart and resampling using a linear interpolation strategy
    
    SeriesSampler(resolution: TimeSpan)
    """
    def Sample(self, series: QuantConnect.Series, start: datetime.datetime, stop: datetime.datetime) -> QuantConnect.Series:
        pass

    def SampleCharts(self, charts: System.Collections.Generic.IDictionary[str, QuantConnect.Chart], start: datetime.datetime, stop: datetime.datetime) -> System.Collections.Generic.Dictionary[str, QuantConnect.Chart]:
        pass

    def __init__(self, resolution: datetime.timedelta) -> QuantConnect.SeriesSampler:
        pass


class SeriesType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Available types of charts
    
    enum SeriesType, values: Bar (3), Candle (2), Flag (4), Line (0), Pie (6), Scatter (1), StackedArea (5), Treemap (7)
    """
    value__: int
    Bar: 'SeriesType'
    Candle: 'SeriesType'
    Flag: 'SeriesType'
    Line: 'SeriesType'
    Pie: 'SeriesType'
    Scatter: 'SeriesType'
    StackedArea: 'SeriesType'
    Treemap: 'SeriesType'


class ServerType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Live server types available through the web IDE. / QC deployment.
    
    enum ServerType, values: Server1024 (1), Server2048 (2), Server512 (0)
    """
    value__: int
    Server1024: 'ServerType'
    Server2048: 'ServerType'
    Server512: 'ServerType'


class SettlementType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the type of settlement in derivative deals
    
    enum SettlementType, values: Cash (1), PhysicalDelivery (0)
    """
    value__: int
    Cash: 'SettlementType'
    PhysicalDelivery: 'SettlementType'


class SplitType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the type of QuantConnect.Data.Market.Split data
    
    enum SplitType, values: SplitOccurred (1), Warning (0)
    """
    value__: int
    SplitOccurred: 'SplitType'
    Warning: 'SplitType'


class StartDateLimitedEventArgs(System.EventArgs):
    """
    Event arguments for the QuantConnect.Interfaces.IDataProviderEvents.StartDateLimited event
    
    StartDateLimitedEventArgs(message: str)
    """
    def __init__(self, message: str) -> QuantConnect.StartDateLimitedEventArgs:
        pass

    Message: str



class StoragePermissions(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Cloud storage permission options.
    
    enum StoragePermissions, values: Authenticated (1), Public (0)
    """
    value__: int
    Authenticated: 'StoragePermissions'
    Public: 'StoragePermissions'


class StringExtensions(System.object):
    """
    Provides extension methods for properly parsing and serializing values while properly using
                an IFormatProvider/CultureInfo when applicable
    """
    @staticmethod
    @typing.overload
    def ConvertInvariant(value: object) -> QuantConnect.T:
        pass

    @staticmethod
    @typing.overload
    def ConvertInvariant(value: object, conversionType: type) -> object:
        pass

    def ConvertInvariant(self, *args) -> object:
        pass

    @staticmethod
    def EndsWithInvariant(value: str, ending: str, ignoreCase: bool) -> bool:
        pass

    @staticmethod
    @typing.overload
    def IfNotNullOrEmpty(value: str, defaultValue: QuantConnect.T, func: typing.Callable[[str], QuantConnect.T]) -> QuantConnect.T:
        pass

    @staticmethod
    @typing.overload
    def IfNotNullOrEmpty(value: str, func: typing.Callable[[str], QuantConnect.T]) -> QuantConnect.T:
        pass

    def IfNotNullOrEmpty(self, *args) -> QuantConnect.T:
        pass

    @staticmethod
    @typing.overload
    def IndexOfInvariant(value: str, character: str) -> int:
        pass

    @staticmethod
    @typing.overload
    def IndexOfInvariant(value: str, substring: str, ignoreCase: bool) -> int:
        pass

    def IndexOfInvariant(self, *args) -> int:
        pass

    @staticmethod
    def Invariant(formattable: System.FormattableString) -> str:
        pass

    @staticmethod
    def LastIndexOfInvariant(value: str, substring: str, ignoreCase: bool) -> int:
        pass

    @staticmethod
    def SafeSubstring(value: str, startIndex: int, length: int) -> str:
        pass

    @staticmethod
    def StartsWithInvariant(value: str, beginning: str, ignoreCase: bool) -> bool:
        pass

    @staticmethod
    def ToIso8601Invariant(dateTime: datetime.datetime) -> str:
        pass

    @staticmethod
    @typing.overload
    def ToStringInvariant(convertible: System.IConvertible) -> str:
        pass

    @staticmethod
    @typing.overload
    def ToStringInvariant(formattable: System.IFormattable, format: str) -> str:
        pass

    def ToStringInvariant(self, *args) -> str:
        pass

    __all__: list


class SubscriptionTransportMedium(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies where a subscription's data comes from
    
    enum SubscriptionTransportMedium, values: LocalFile (0), RemoteFile (1), Rest (2), Streaming (3)
    """
    value__: int
    LocalFile: 'SubscriptionTransportMedium'
    RemoteFile: 'SubscriptionTransportMedium'
    Rest: 'SubscriptionTransportMedium'
    Streaming: 'SubscriptionTransportMedium'


class Symbol(System.object, System.IEquatable[Symbol], System.IComparable):
    """
    Represents a unique security identifier. This is made of two components,
                the unique SID and the Value. The value is the current ticker symbol while
                the SID is constant over the life of a security
    
    Symbol(sid: SecurityIdentifier, value: str)
    """
    def CompareTo(self, obj: object) -> int:
        pass

    def Contains(self, value: str) -> bool:
        pass

    @staticmethod
    def Create(ticker: str, securityType: QuantConnect.SecurityType, market: str, alias: str, baseDataType: type) -> QuantConnect.Symbol:
        pass

    @staticmethod
    def CreateBase(baseType: type, underlying: QuantConnect.Symbol, market: str) -> QuantConnect.Symbol:
        pass

    @staticmethod
    def CreateFuture(ticker: str, market: str, expiry: datetime.datetime, alias: str) -> QuantConnect.Symbol:
        pass

    @staticmethod
    @typing.overload
    def CreateOption(underlying: str, market: str, style: QuantConnect.OptionStyle, right: QuantConnect.OptionRight, strike: float, expiry: datetime.datetime, alias: str, mapSymbol: bool) -> QuantConnect.Symbol:
        pass

    @staticmethod
    @typing.overload
    def CreateOption(underlyingSymbol: QuantConnect.Symbol, market: str, style: QuantConnect.OptionStyle, right: QuantConnect.OptionRight, strike: float, expiry: datetime.datetime, alias: str) -> QuantConnect.Symbol:
        pass

    def CreateOption(self, *args) -> QuantConnect.Symbol:
        pass

    def EndsWith(self, value: str) -> bool:
        pass

    @typing.overload
    def Equals(self, obj: object) -> bool:
        pass

    @typing.overload
    def Equals(self, other: QuantConnect.Symbol) -> bool:
        pass

    def Equals(self, *args) -> bool:
        pass

    def GetHashCode(self) -> int:
        pass

    def HasUnderlyingSymbol(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def IsCanonical(self) -> bool:
        pass

    def StartsWith(self, value: str) -> bool:
        pass

    def ToLower(self) -> str:
        pass

    def ToString(self) -> str:
        pass

    def ToUpper(self) -> str:
        pass

    def UpdateMappedSymbol(self, mappedSymbol: str) -> QuantConnect.Symbol:
        pass

    def __init__(self, sid: QuantConnect.SecurityIdentifier, value: str) -> QuantConnect.Symbol:
        pass

    HasUnderlying: bool

    ID: QuantConnect.SecurityIdentifier

    SecurityType: QuantConnect.SecurityType

    Underlying: QuantConnect.Symbol

    Value: str


    Empty: 'Symbol'
    None_: 'Symbol'


class SymbolCache(System.object):
    """
    Provides a string->Symbol mapping to allow for user defined strings to be lifted into a Symbol
                This is mainly used via the Symbol implicit operator, but also functions that create securities
                should also call Set to add new mappings
    """
    @staticmethod
    def Clear() -> None:
        pass

    @staticmethod
    def GetSymbol(ticker: str) -> QuantConnect.Symbol:
        pass

    @staticmethod
    def GetTicker(symbol: QuantConnect.Symbol) -> str:
        pass

    @staticmethod
    def Set(ticker: str, symbol: QuantConnect.Symbol) -> None:
        pass

    @staticmethod
    def TryGetSymbol(ticker: str, symbol: QuantConnect.Symbol) -> bool:
        pass

    @staticmethod
    def TryGetTicker(symbol: QuantConnect.Symbol, ticker: str) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryRemove(symbol: QuantConnect.Symbol) -> bool:
        pass

    @staticmethod
    @typing.overload
    def TryRemove(ticker: str) -> bool:
        pass

    def TryRemove(self, *args) -> bool:
        pass

    __all__: list


class SymbolJsonConverter(Newtonsoft.Json.JsonConverter):
    """
    Defines a Newtonsoft.Json.JsonConverter to be used when deserializing to
                the QuantConnect.Symbol class.
    
    SymbolJsonConverter()
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass


class SymbolRepresentation(System.object):
    """ Public static helper class that does parsing/generation of symbol representations (options, futures) """
    @staticmethod
    def GenerateFutureTicker(underlying: str, expiration: datetime.datetime, doubleDigitsYear: bool) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateOptionTickerOSI(symbol: QuantConnect.Symbol) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateOptionTickerOSI(underlying: str, right: QuantConnect.OptionRight, strikePrice: float, expiration: datetime.datetime) -> str:
        pass

    def GenerateOptionTickerOSI(self, *args) -> str:
        pass

    @staticmethod
    def ParseFutureTicker(ticker: str) -> QuantConnect.FutureTickerProperties:
        pass

    @staticmethod
    def ParseOptionTickerIQFeed(ticker: str) -> QuantConnect.OptionTickerProperties:
        pass

    @staticmethod
    def ParseOptionTickerOSI(ticker: str) -> QuantConnect.Symbol:
        pass

    FutureTickerProperties: type
    OptionTickerProperties: type
    __all__: list
