from .__Market_2 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Orders
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import datetime


class FuturesChain(QuantConnect.Data.BaseData, System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[FuturesContract], QuantConnect.Data.IBaseData):
    """
    Represents an entire chain of futures contracts for a single underlying
                This type is System.Collections.Generic.IEnumerable
    
    FuturesChain(canonicalFutureSymbol: Symbol, time: DateTime)
    FuturesChain(canonicalFutureSymbol: Symbol, time: DateTime, trades: IEnumerable[BaseData], quotes: IEnumerable[BaseData], contracts: IEnumerable[FuturesContract], filteredContracts: IEnumerable[Symbol])
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
    def GetAux(self, symbol: QuantConnect.Symbol) -> QuantConnect.Data.Market.T:
        pass

    @typing.overload
    def GetAux(self) -> QuantConnect.Data.Market.DataDictionary[QuantConnect.Data.Market.T]:
        pass

    def GetAux(self, *args) -> QuantConnect.Data.Market.DataDictionary[QuantConnect.Data.Market.T]:
        pass

    @typing.overload
    def GetAuxList(self) -> System.Collections.Generic.Dictionary[QuantConnect.Symbol, typing.List[QuantConnect.Data.BaseData]]:
        pass

    @typing.overload
    def GetAuxList(self, symbol: QuantConnect.Symbol) -> typing.List[QuantConnect.Data.Market.T]:
        pass

    def GetAuxList(self, *args) -> typing.List[QuantConnect.Data.Market.T]:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Data.Market.FuturesContract]:
        pass

    @typing.overload
    def __init__(self, canonicalFutureSymbol: QuantConnect.Symbol, time: datetime.datetime) -> QuantConnect.Data.Market.FuturesChain:
        pass

    @typing.overload
    def __init__(self, canonicalFutureSymbol: QuantConnect.Symbol, time: datetime.datetime, trades: typing.List[QuantConnect.Data.BaseData], quotes: typing.List[QuantConnect.Data.BaseData], contracts: typing.List[QuantConnect.Data.Market.FuturesContract], filteredContracts: typing.List[QuantConnect.Symbol]) -> QuantConnect.Data.Market.FuturesChain:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.FuturesChain:
        pass

    Contracts: QuantConnect.Data.Market.FuturesContracts

    FilteredContracts: System.Collections.Generic.HashSet[QuantConnect.Symbol]

    QuoteBars: QuantConnect.Data.Market.QuoteBars

    Ticks: QuantConnect.Data.Market.Ticks

    TradeBars: QuantConnect.Data.Market.TradeBars

    Underlying: QuantConnect.Data.BaseData



class FuturesChains(QuantConnect.Data.Market.DataDictionary[FuturesChain], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, FuturesChain], System.Collections.Generic.ICollection[KeyValuePair[Symbol, FuturesChain]], System.Collections.Generic.IDictionary[Symbol, FuturesChain], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, FuturesChain]]):
    """
    Collection of QuantConnect.Data.Market.FuturesChain keyed by canonical futures symbol
    
    FuturesChains()
    FuturesChains(time: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.FuturesChains:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime) -> QuantConnect.Data.Market.FuturesChains:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.FuturesChains:
        pass


    Item: indexer#


class FuturesContract(System.object):
    """
    Defines a single futures contract at a specific expiration
    
    FuturesContract(symbol: Symbol, underlyingSymbol: Symbol)
    """
    def ToString(self) -> str:
        pass

    def __init__(self, symbol: QuantConnect.Symbol, underlyingSymbol: QuantConnect.Symbol) -> QuantConnect.Data.Market.FuturesContract:
        pass

    AskPrice: float

    AskSize: int

    BidPrice: float

    BidSize: int

    Expiry: datetime.datetime

    LastPrice: float

    OpenInterest: float

    Symbol: QuantConnect.Symbol

    Time: datetime.datetime

    UnderlyingSymbol: QuantConnect.Symbol

    Volume: int



class FuturesContracts(QuantConnect.Data.Market.DataDictionary[FuturesContract], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, FuturesContract], System.Collections.Generic.ICollection[KeyValuePair[Symbol, FuturesContract]], System.Collections.Generic.IDictionary[Symbol, FuturesContract], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, FuturesContract]]):
    """
    Collection of QuantConnect.Data.Market.FuturesContract keyed by futures symbol
    
    FuturesContracts()
    FuturesContracts(time: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.FuturesContracts:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime) -> QuantConnect.Data.Market.FuturesContracts:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.FuturesContracts:
        pass


    Item: indexer#


class Greeks(System.object):
    """
    Defines the greeks
    
    Greeks()
    Greeks(delta: Decimal, gamma: Decimal, vega: Decimal, theta: Decimal, rho: Decimal, lambda: Decimal)
    Greeks(delta: Func[Decimal], gamma: Func[Decimal], vega: Func[Decimal], theta: Func[Decimal], rho: Func[Decimal], lambda: Func[Decimal])
    Greeks(deltaGamma: Func[Tuple[Decimal, Decimal]], vega: Func[Decimal], theta: Func[Decimal], rho: Func[Decimal], lambda: Func[Decimal])
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Greeks:
        pass

    @typing.overload
    def __init__(self, delta: float, gamma: float, vega: float, theta: float, rho: float, lambda_: float) -> QuantConnect.Data.Market.Greeks:
        pass

    @typing.overload
    def __init__(self, delta: typing.Callable[[], float], gamma: typing.Callable[[], float], vega: typing.Callable[[], float], theta: typing.Callable[[], float], rho: typing.Callable[[], float], lambda_: typing.Callable[[], float]) -> QuantConnect.Data.Market.Greeks:
        pass

    @typing.overload
    def __init__(self, deltaGamma: typing.Callable[[], System.Tuple[float, float]], vega: typing.Callable[[], float], theta: typing.Callable[[], float], rho: typing.Callable[[], float], lambda_: typing.Callable[[], float]) -> QuantConnect.Data.Market.Greeks:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Greeks:
        pass

    Delta: float

    Gamma: float

    Lambda: float

    Rho: float

    Theta: float

    Vega: float



class IBar:
    """ Generic bar interface with Open, High, Low and Close. """
    Close: float

    High: float

    Low: float

    Open: float



class IBaseDataBar(QuantConnect.Data.Market.IBar, QuantConnect.Data.IBaseData):
    """ Represents a type that is both a bar and base data """

class Tick(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Tick class is the base representation for tick data. It is grouped into a Ticks object
                which implements IDictionary and passed into an OnData event handler.
    
    Tick()
    Tick(original: Tick)
    Tick(time: DateTime, symbol: Symbol, bid: Decimal, ask: Decimal)
    Tick(time: DateTime, symbol: Symbol, last: Decimal, bid: Decimal, ask: Decimal)
    Tick(time: DateTime, symbol: Symbol, saleCondition: str, exchange: str, quantity: Decimal, price: Decimal)
    Tick(time: DateTime, symbol: Symbol, saleCondition: str, exchange: str, bidSize: Decimal, bidPrice: Decimal, askSize: Decimal, askPrice: Decimal)
    Tick(symbol: Symbol, line: str)
    Tick(symbol: Symbol, line: str, baseDate: DateTime)
    Tick(config: SubscriptionDataConfig, reader: StreamReader, date: DateTime)
    Tick(config: SubscriptionDataConfig, line: str, date: DateTime)
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

    def IsValid(self) -> bool:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, reader: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def ToString(self) -> str:
        pass

    def Update(self, lastTrade: float, bidPrice: float, askPrice: float, volume: float, bidSize: float, askSize: float) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, original: QuantConnect.Data.Market.Tick) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, symbol: QuantConnect.Symbol, bid: float, ask: float) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, symbol: QuantConnect.Symbol, last: float, bid: float, ask: float) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, symbol: QuantConnect.Symbol, saleCondition: str, exchange: str, quantity: float, price: float) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, symbol: QuantConnect.Symbol, saleCondition: str, exchange: str, bidSize: float, bidPrice: float, askSize: float, askPrice: float) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, line: str) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, line: str, baseDate: datetime.datetime) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, config: QuantConnect.Data.SubscriptionDataConfig, reader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.Tick:
        pass

    @typing.overload
    def __init__(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.Tick:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.Tick:
        pass

    LastPrice: float

    AskPrice: float
    AskSize: float
    BidPrice: float
    BidSize: float
    Exchange: str
    Quantity: float
    SaleCondition: str
    Suspicious: bool
    TickType: QuantConnect.TickType
