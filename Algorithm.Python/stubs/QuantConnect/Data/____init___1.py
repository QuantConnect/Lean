from .____init___2 import *
import typing
import System.Reflection
import System.Linq.Expressions
import System.IO
import System.Dynamic
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Securities
import QuantConnect.Packets
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import NodaTime
import datetime



class HistoryRequest(System.object):
    """
    Represents a request for historical data
    
    HistoryRequest(startTimeUtc: DateTime, endTimeUtc: DateTime, dataType: Type, symbol: Symbol, resolution: Resolution, exchangeHours: SecurityExchangeHours, dataTimeZone: DateTimeZone, fillForwardResolution: Nullable[Resolution], includeExtendedMarketHours: bool, isCustomData: bool, dataNormalizationMode: DataNormalizationMode, tickType: TickType)
    HistoryRequest(config: SubscriptionDataConfig, hours: SecurityExchangeHours, startTimeUtc: DateTime, endTimeUtc: DateTime)
    """
    @typing.overload
    def __init__(self, startTimeUtc: datetime.datetime, endTimeUtc: datetime.datetime, dataType: type, symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, dataTimeZone: NodaTime.DateTimeZone, fillForwardResolution: typing.Optional[QuantConnect.Resolution], includeExtendedMarketHours: bool, isCustomData: bool, dataNormalizationMode: QuantConnect.DataNormalizationMode, tickType: QuantConnect.TickType) -> QuantConnect.Data.HistoryRequest:
        pass

    @typing.overload
    def __init__(self, config: QuantConnect.Data.SubscriptionDataConfig, hours: QuantConnect.Securities.SecurityExchangeHours, startTimeUtc: datetime.datetime, endTimeUtc: datetime.datetime) -> QuantConnect.Data.HistoryRequest:
        pass

    def __init__(self, *args) -> QuantConnect.Data.HistoryRequest:
        pass

    DataNormalizationMode: QuantConnect.DataNormalizationMode

    DataTimeZone: NodaTime.DateTimeZone

    DataType: type

    EndTimeUtc: datetime.datetime

    ExchangeHours: QuantConnect.Securities.SecurityExchangeHours

    FillForwardResolution: typing.Optional[QuantConnect.Resolution]

    IncludeExtendedMarketHours: bool

    IsCustomData: bool

    Resolution: QuantConnect.Resolution

    StartTimeUtc: datetime.datetime

    Symbol: QuantConnect.Symbol

    TickType: QuantConnect.TickType



class HistoryRequestFactory(System.object):
    """
    Helper class used to create new QuantConnect.Data.HistoryRequest
    
    HistoryRequestFactory(algorithm: IAlgorithm)
    """
    def CreateHistoryRequest(self, subscription: QuantConnect.Data.SubscriptionDataConfig, startAlgoTz: datetime.datetime, endAlgoTz: datetime.datetime, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Data.HistoryRequest:
        pass

    def GetStartTimeAlgoTz(self, symbol: QuantConnect.Symbol, periods: int, resolution: QuantConnect.Resolution, exchange: QuantConnect.Securities.SecurityExchangeHours, dataTimeZone: NodaTime.DateTimeZone) -> datetime.datetime:
        pass

    def __init__(self, algorithm: QuantConnect.Interfaces.IAlgorithm) -> QuantConnect.Data.HistoryRequestFactory:
        pass


class IBaseData:
    """ Base Data Class: Type, Timestamp, Key -- Base Features. """
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, dataFeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def RequiresMapping(self) -> bool:
        pass

    DataType: QuantConnect.MarketDataType

    EndTime: datetime.datetime

    Price: float

    Symbol: QuantConnect.Symbol

    Time: datetime.datetime

    Value: float



class IDataAggregator(System.IDisposable):
    """ Aggregates ticks and bars based on given subscriptions. """
    def Add(self, dataConfig: QuantConnect.Data.SubscriptionDataConfig, newDataAvailableHandler: System.EventHandler) -> System.Collections.Generic.IEnumerator[QuantConnect.Data.BaseData]:
        pass

    def Remove(self, dataConfig: QuantConnect.Data.SubscriptionDataConfig) -> bool:
        pass

    def Update(self, input: QuantConnect.Data.BaseData) -> None:
        pass


class IndexedBaseData(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Abstract indexed base data class of QuantConnect.
                It is intended to be extended to define customizable data types which are stored
                using an intermediate index source
    """
    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    def GetSourceForAnIndex(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, index: str, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass


class ISubscriptionEnumeratorFactory:
    """ Create an System.Collections.Generic.IEnumerator """
    def CreateEnumerator(self, request: QuantConnect.Data.UniverseSelection.SubscriptionRequest, dataProvider: QuantConnect.Interfaces.IDataProvider) -> System.Collections.Generic.IEnumerator[QuantConnect.Data.BaseData]:
        pass


class Slice(QuantConnect.ExtendedDictionary[object], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, object], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, BaseData]]):
    """
    Provides a data structure for all of an algorithm's data at a single time step
    
    Slice(time: DateTime, data: IEnumerable[BaseData])
    Slice(time: DateTime, data: List[BaseData])
    Slice(time: DateTime, data: IEnumerable[BaseData], tradeBars: TradeBars, quoteBars: QuoteBars, ticks: Ticks, optionChains: OptionChains, futuresChains: FuturesChains, splits: Splits, dividends: Dividends, delistings: Delistings, symbolChanges: SymbolChangedEvents, hasData: Nullable[bool])
    """
    def ContainsKey(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    @typing.overload
    def Get(self) -> QuantConnect.Data.Market.DataDictionary[QuantConnect.Data.T]:
        pass

    @typing.overload
    def Get(self, type: type) -> object:
        pass

    @typing.overload
    def Get(self, symbol: QuantConnect.Symbol) -> QuantConnect.Data.T:
        pass

    def Get(self, *args) -> QuantConnect.Data.T:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.BaseData]]:
        pass

    def TryGetValue(self, symbol: QuantConnect.Symbol, data: object) -> bool:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, data: typing.List[QuantConnect.Data.BaseData]) -> QuantConnect.Data.Slice:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, data: typing.List[QuantConnect.Data.BaseData]) -> QuantConnect.Data.Slice:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, data: typing.List[QuantConnect.Data.BaseData], tradeBars: QuantConnect.Data.Market.TradeBars, quoteBars: QuantConnect.Data.Market.QuoteBars, ticks: QuantConnect.Data.Market.Ticks, optionChains: QuantConnect.Data.Market.OptionChains, futuresChains: QuantConnect.Data.Market.FuturesChains, splits: QuantConnect.Data.Market.Splits, dividends: QuantConnect.Data.Market.Dividends, delistings: QuantConnect.Data.Market.Delistings, symbolChanges: QuantConnect.Data.Market.SymbolChangedEvents, hasData: typing.Optional[bool]) -> QuantConnect.Data.Slice:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Slice:
        pass

    Bars: QuantConnect.Data.Market.TradeBars

    Count: int

    Delistings: QuantConnect.Data.Market.Delistings

    Dividends: QuantConnect.Data.Market.Dividends

    FutureChains: QuantConnect.Data.Market.FuturesChains

    FuturesChains: QuantConnect.Data.Market.FuturesChains

    HasData: bool

    Keys: typing.List[QuantConnect.Symbol]

    OptionChains: QuantConnect.Data.Market.OptionChains

    QuoteBars: QuantConnect.Data.Market.QuoteBars

    Splits: QuantConnect.Data.Market.Splits

    SymbolChangedEvents: QuantConnect.Data.Market.SymbolChangedEvents

    Ticks: QuantConnect.Data.Market.Ticks

    Time: datetime.datetime

    Values: typing.List[QuantConnect.Data.BaseData]


    Item: indexer#


class SliceExtensions(System.object):
    """ Provides extension methods to slice enumerables """
    @staticmethod
    @typing.overload
    def Get(slices: typing.List[QuantConnect.Data.Slice], symbol: QuantConnect.Symbol) -> typing.List[QuantConnect.Data.Market.TradeBar]:
        pass

    @staticmethod
    @typing.overload
    def Get(dataDictionaries: typing.List[QuantConnect.Data.Market.DataDictionary[QuantConnect.Data.T]], symbol: QuantConnect.Symbol) -> typing.List[QuantConnect.Data.T]:
        pass

    @staticmethod
    @typing.overload
    def Get(dataDictionaries: typing.List[QuantConnect.Data.Market.DataDictionary[QuantConnect.Data.T]], symbol: QuantConnect.Symbol, field: str) -> typing.List[float]:
        pass

    @staticmethod
    @typing.overload
    def Get(slices: typing.List[QuantConnect.Data.Slice]) -> typing.List[QuantConnect.Data.Market.DataDictionary[QuantConnect.Data.T]]:
        pass

    @staticmethod
    @typing.overload
    def Get(slices: typing.List[QuantConnect.Data.Slice], symbol: QuantConnect.Symbol) -> typing.List[QuantConnect.Data.T]:
        pass

    @staticmethod
    @typing.overload
    def Get(slices: typing.List[QuantConnect.Data.Slice], symbol: QuantConnect.Symbol, field: typing.Callable[[QuantConnect.Data.BaseData], float]) -> typing.List[float]:
        pass

    def Get(self, *args) -> typing.List[float]:
        pass

    @staticmethod
    def PushThrough(slices: typing.List[QuantConnect.Data.Slice], handler: typing.Callable[[QuantConnect.Data.BaseData], None]) -> None:
        pass

    @staticmethod
    @typing.overload
    def PushThroughConsolidators(slices: typing.List[QuantConnect.Data.Slice], consolidatorsBySymbol: System.Collections.Generic.Dictionary[QuantConnect.Symbol, QuantConnect.Data.Consolidators.IDataConsolidator]) -> None:
        pass

    @staticmethod
    @typing.overload
    def PushThroughConsolidators(slices: typing.List[QuantConnect.Data.Slice], consolidatorsProvider: typing.Callable[[QuantConnect.Symbol], QuantConnect.Data.Consolidators.IDataConsolidator]) -> None:
        pass

    def PushThroughConsolidators(self, *args) -> None:
        pass

    @staticmethod
    def Ticks(slices: typing.List[QuantConnect.Data.Slice]) -> typing.List[QuantConnect.Data.Market.Ticks]:
        pass

    @staticmethod
    def ToDoubleArray(decimals: typing.List[float]) -> typing.List[float]:
        pass

    @staticmethod
    def TradeBars(slices: typing.List[QuantConnect.Data.Slice]) -> typing.List[QuantConnect.Data.Market.TradeBars]:
        pass

    __all__: list
