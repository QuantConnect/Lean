from .__Market_4 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Orders
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import datetime



class QuoteBars(QuantConnect.Data.Market.DataDictionary[QuoteBar], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, QuoteBar], System.Collections.Generic.ICollection[KeyValuePair[Symbol, QuoteBar]], System.Collections.Generic.IDictionary[Symbol, QuoteBar], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, QuoteBar]]):
    """
    Collection of QuantConnect.Data.Market.QuoteBar keyed by symbol
    
    QuoteBars()
    QuoteBars(time: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.QuoteBars:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime) -> QuantConnect.Data.Market.QuoteBars:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.QuoteBars:
        pass


    Item: indexer#


class RenkoBar(QuantConnect.Data.BaseData, QuantConnect.Data.Market.IBar, QuantConnect.Data.Market.IBaseDataBar, QuantConnect.Data.IBaseData):
    """
    Represents a bar sectioned not by time, but by some amount of movement in a value (for example, Closing price moving in $10 bar sizes)
    
    RenkoBar()
    RenkoBar(symbol: Symbol, time: DateTime, brickSize: Decimal, open: Decimal, volume: Decimal)
    RenkoBar(symbol: Symbol, start: DateTime, endTime: DateTime, brickSize: Decimal, open: Decimal, high: Decimal, low: Decimal, close: Decimal)
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

    @typing.overload
    def Update(self, time: datetime.datetime, currentValue: float, volumeSinceLastUpdate: float) -> bool:
        pass

    @typing.overload
    def Update(self, lastTrade: float, bidPrice: float, askPrice: float, volume: float, bidSize: float, askSize: float) -> None:
        pass

    def Update(self, *args) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.RenkoBar:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, time: datetime.datetime, brickSize: float, open: float, volume: float) -> QuantConnect.Data.Market.RenkoBar:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, start: datetime.datetime, endTime: datetime.datetime, brickSize: float, open: float, high: float, low: float, close: float) -> QuantConnect.Data.Market.RenkoBar:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.RenkoBar:
        pass

    BrickSize: float

    Close: float

    Direction: QuantConnect.Data.Market.BarDirection

    End: datetime.datetime

    EndTime: datetime.datetime

    High: float

    IsClosed: bool

    Low: float

    Open: float

    Spread: float

    Start: datetime.datetime

    Type: QuantConnect.Data.Market.RenkoType

    Volume: float



class RenkoType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    The type of the RenkoBar
    
    enum RenkoType, values: Classic (0), Wicked (1)
    """
    value__: int
    Classic: 'RenkoType'
    Wicked: 'RenkoType'


class Split(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Split event from a security
    
    Split()
    Split(symbol: Symbol, date: DateTime, price: Decimal, splitFactor: Decimal, type: SplitType)
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

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Split:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, date: datetime.datetime, price: float, splitFactor: float, type: QuantConnect.SplitType) -> QuantConnect.Data.Market.Split:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Split:
        pass

    ReferencePrice: float

    SplitFactor: float

    Type: QuantConnect.SplitType



class Splits(QuantConnect.Data.Market.DataDictionary[Split], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, Split], System.Collections.Generic.ICollection[KeyValuePair[Symbol, Split]], System.Collections.Generic.IDictionary[Symbol, Split], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, Split]]):
    """
    Collection of splits keyed by QuantConnect.Symbol
    
    Splits()
    Splits(frontier: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Splits:
        pass

    @typing.overload
    def __init__(self, frontier: datetime.datetime) -> QuantConnect.Data.Market.Splits:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Splits:
        pass


    Item: indexer#


class SymbolChangedEvent(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Symbol changed event of a security. This is generated when a symbol is remapped for a given
                security, for example, at EOD 2014.04.02 GOOG turned into GOOGL, but are the same
    
    SymbolChangedEvent()
    SymbolChangedEvent(requestedSymbol: Symbol, date: DateTime, oldSymbol: str, newSymbol: str)
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
    def __init__(self) -> QuantConnect.Data.Market.SymbolChangedEvent:
        pass

    @typing.overload
    def __init__(self, requestedSymbol: QuantConnect.Symbol, date: datetime.datetime, oldSymbol: str, newSymbol: str) -> QuantConnect.Data.Market.SymbolChangedEvent:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.SymbolChangedEvent:
        pass

    NewSymbol: str

    OldSymbol: str



class SymbolChangedEvents(QuantConnect.Data.Market.DataDictionary[SymbolChangedEvent], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, SymbolChangedEvent], System.Collections.Generic.ICollection[KeyValuePair[Symbol, SymbolChangedEvent]], System.Collections.Generic.IDictionary[Symbol, SymbolChangedEvent], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, SymbolChangedEvent]]):
    """
    Collection of QuantConnect.Data.Market.SymbolChangedEvent keyed by the original, requested symbol
    
    SymbolChangedEvents()
    SymbolChangedEvents(frontier: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.SymbolChangedEvents:
        pass

    @typing.overload
    def __init__(self, frontier: datetime.datetime) -> QuantConnect.Data.Market.SymbolChangedEvents:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.SymbolChangedEvents:
        pass


    Item: indexer#


class Ticks(QuantConnect.Data.Market.DataDictionary[List[Tick]], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, List[Tick]], System.Collections.Generic.ICollection[KeyValuePair[Symbol, List[Tick]]], System.Collections.Generic.IDictionary[Symbol, List[Tick]], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, List[Tick]]]):
    """
    Ticks collection which implements an IDictionary-string-list of ticks. This way users can iterate over the string indexed ticks of the requested symbol.
    
    Ticks()
    Ticks(frontier: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Ticks:
        pass

    @typing.overload
    def __init__(self, frontier: datetime.datetime) -> QuantConnect.Data.Market.Ticks:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Ticks:
        pass


    Item: indexer#
