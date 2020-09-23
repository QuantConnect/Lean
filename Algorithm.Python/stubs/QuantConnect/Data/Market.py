from .__Market_1 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Orders
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import datetime

# no functions
# classes

class Bar(System.object, QuantConnect.Data.Market.IBar):
    """
    Base Bar Class: Open, High, Low, Close and Period.
    
    Bar()
    Bar(open: Decimal, high: Decimal, low: Decimal, close: Decimal)
    """
    def Clone(self) -> QuantConnect.Data.Market.Bar:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def Update(self, value: float) -> None:
        pass

    @typing.overload
    def Update(self, value: float) -> None:
        pass

    def Update(self, *args) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Bar:
        pass

    @typing.overload
    def __init__(self, open: float, high: float, low: float, close: float) -> QuantConnect.Data.Market.Bar:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Bar:
        pass

    Close: float

    High: float

    Low: float

    Open: float



class BarDirection(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """ enum BarDirection, values: Falling (2), NoDelta (1), Rising (0) """
    value__: int
    Falling: 'BarDirection'
    NoDelta: 'BarDirection'
    Rising: 'BarDirection'


class DataDictionary(QuantConnect.ExtendedDictionary[T], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, T], System.Collections.Generic.ICollection[KeyValuePair[Symbol, T]], System.Collections.Generic.IDictionary[Symbol, T], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, T]]):
    """
    DataDictionary[T]()
    DataDictionary[T](data: IEnumerable[T], keySelector: Func[T, Symbol])
    DataDictionary[T](time: DateTime)
    """
    @typing.overload
    def Add(self, item: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.Market.T]) -> None:
        pass

    @typing.overload
    def Add(self, key: QuantConnect.Symbol, value: QuantConnect.Data.Market.T) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def Clear(self) -> None:
        pass

    def Contains(self, item: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.Market.T]) -> bool:
        pass

    def ContainsKey(self, key: QuantConnect.Symbol) -> bool:
        pass

    def CopyTo(self, array: typing.List[System.Collections.Generic.KeyValuePair], arrayIndex: int) -> None:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.Market.T]]:
        pass

    def GetValue(self, key: QuantConnect.Symbol) -> QuantConnect.Data.Market.T:
        pass

    @typing.overload
    def Remove(self, item: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.Market.T]) -> bool:
        pass

    @typing.overload
    def Remove(self, key: QuantConnect.Symbol) -> bool:
        pass

    def Remove(self, *args) -> bool:
        pass

    def TryGetValue(self, key: QuantConnect.Symbol, value: QuantConnect.Data.Market.T) -> bool:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.DataDictionary:
        pass

    @typing.overload
    def __init__(self, data: typing.List[QuantConnect.Data.Market.T], keySelector: typing.Callable[[QuantConnect.Data.Market.T], QuantConnect.Symbol]) -> QuantConnect.Data.Market.DataDictionary:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime) -> QuantConnect.Data.Market.DataDictionary:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.DataDictionary:
        pass

    Count: int

    IsReadOnly: bool

    Keys: typing.List[QuantConnect.Symbol]

    Time: datetime.datetime

    Values: typing.List[QuantConnect.Data.Market.T]


    Item: indexer#


class DataDictionaryExtensions(System.object):
    """ Provides extension methods for the DataDictionary class """
    @staticmethod
    def Add(dictionary: QuantConnect.Data.Market.DataDictionary[QuantConnect.Data.Market.T], data: QuantConnect.Data.Market.T) -> None:
        pass

    __all__: list


class Delisting(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Delisting event of a security
    
    Delisting()
    Delisting(symbol: Symbol, date: DateTime, price: Decimal, type: DelistingType)
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
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def SetOrderTicket(self, ticket: QuantConnect.Orders.OrderTicket) -> None:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Delisting:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, date: datetime.datetime, price: float, type: QuantConnect.DelistingType) -> QuantConnect.Data.Market.Delisting:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Delisting:
        pass

    Ticket: QuantConnect.Orders.OrderTicket

    Type: QuantConnect.DelistingType



class Delistings(QuantConnect.Data.Market.DataDictionary[Delisting], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, Delisting], System.Collections.Generic.ICollection[KeyValuePair[Symbol, Delisting]], System.Collections.Generic.IDictionary[Symbol, Delisting], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, Delisting]]):
    """
    Collections of QuantConnect.Data.Market.Delisting keyed by QuantConnect.Symbol
    
    Delistings()
    Delistings(frontier: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Delistings:
        pass

    @typing.overload
    def __init__(self, frontier: datetime.datetime) -> QuantConnect.Data.Market.Delistings:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Delistings:
        pass


    Item: indexer#


class Dividend(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Dividend event from a security
    
    Dividend()
    Dividend(symbol: Symbol, date: DateTime, distribution: Decimal, referencePrice: Decimal)
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    @staticmethod
    def ComputeDistribution(close: float, priceFactorRatio: float, decimalPlaces: int) -> float:
        pass

    @staticmethod
    def Create(symbol: QuantConnect.Symbol, date: datetime.datetime, referencePrice: float, priceFactorRatio: float) -> QuantConnect.Data.Market.Dividend:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Dividend:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, date: datetime.datetime, distribution: float, referencePrice: float) -> QuantConnect.Data.Market.Dividend:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Dividend:
        pass

    Distribution: float

    ReferencePrice: float



class Dividends(QuantConnect.Data.Market.DataDictionary[Dividend], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, Dividend], System.Collections.Generic.ICollection[KeyValuePair[Symbol, Dividend]], System.Collections.Generic.IDictionary[Symbol, Dividend], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, Dividend]]):
    """
    Collection of dividends keyed by QuantConnect.Symbol
    
    Dividends()
    Dividends(frontier: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Dividends:
        pass

    @typing.overload
    def __init__(self, frontier: datetime.datetime) -> QuantConnect.Data.Market.Dividends:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Dividends:
        pass


    Item: indexer#
