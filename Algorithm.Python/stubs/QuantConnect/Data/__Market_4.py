import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Orders
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import datetime


class TradeBar(QuantConnect.Data.BaseData, QuantConnect.Data.Market.IBar, QuantConnect.Data.Market.IBaseDataBar, QuantConnect.Data.IBaseData):
    """
    TradeBar class for second and minute resolution data:
                An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    
    TradeBar()
    TradeBar(original: TradeBar)
    TradeBar(time: DateTime, symbol: Symbol, open: Decimal, high: Decimal, low: Decimal, close: Decimal, volume: Decimal, period: Nullable[TimeSpan])
    """
    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
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

    @staticmethod
    def Parse(config: QuantConnect.Data.SubscriptionDataConfig, line: str, baseDate: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseCfd(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.T:
        pass

    @staticmethod
    @typing.overload
    def ParseCfd(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseCfd(config: QuantConnect.Data.SubscriptionDataConfig, streamReader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    def ParseCfd(self, *args) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseCrypto(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.T:
        pass

    @staticmethod
    @typing.overload
    def ParseCrypto(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseCrypto(config: QuantConnect.Data.SubscriptionDataConfig, streamReader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    def ParseCrypto(self, *args) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseEquity(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.T:
        pass

    @staticmethod
    @typing.overload
    def ParseEquity(config: QuantConnect.Data.SubscriptionDataConfig, streamReader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseEquity(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    def ParseEquity(self, *args) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseForex(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.T:
        pass

    @staticmethod
    @typing.overload
    def ParseForex(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseForex(config: QuantConnect.Data.SubscriptionDataConfig, streamReader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    def ParseForex(self, *args) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseFuture(config: QuantConnect.Data.SubscriptionDataConfig, streamReader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.T:
        pass

    @staticmethod
    @typing.overload
    def ParseFuture(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.T:
        pass

    @staticmethod
    @typing.overload
    def ParseFuture(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseFuture(config: QuantConnect.Data.SubscriptionDataConfig, streamReader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    def ParseFuture(self, *args) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseOption(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.T:
        pass

    @staticmethod
    @typing.overload
    def ParseOption(config: QuantConnect.Data.SubscriptionDataConfig, streamReader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.T:
        pass

    @staticmethod
    @typing.overload
    def ParseOption(config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    @staticmethod
    @typing.overload
    def ParseOption(config: QuantConnect.Data.SubscriptionDataConfig, streamReader: System.IO.StreamReader, date: datetime.datetime) -> QuantConnect.Data.Market.TradeBar:
        pass

    def ParseOption(self, *args) -> QuantConnect.Data.Market.TradeBar:
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

    def Update(self, lastTrade: float, bidPrice: float, askPrice: float, volume: float, bidSize: float, askSize: float) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.TradeBar:
        pass

    @typing.overload
    def __init__(self, original: QuantConnect.Data.Market.TradeBar) -> QuantConnect.Data.Market.TradeBar:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, symbol: QuantConnect.Symbol, open: float, high: float, low: float, close: float, volume: float, period: typing.Optional[datetime.timedelta]) -> QuantConnect.Data.Market.TradeBar:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.TradeBar:
        pass

    Close: float

    EndTime: datetime.datetime

    High: float

    Low: float

    Open: float

    Period: datetime.timedelta

    Volume: float



class TradeBars(QuantConnect.Data.Market.DataDictionary[TradeBar], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, TradeBar], System.Collections.Generic.ICollection[KeyValuePair[Symbol, TradeBar]], System.Collections.Generic.IDictionary[Symbol, TradeBar], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, TradeBar]]):
    """
    Collection of TradeBars to create a data type for generic data handler:
    
    TradeBars()
    TradeBars(frontier: DateTime)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Data.Market.TradeBars:
        pass

    @typing.overload
    def __init__(self, frontier: datetime.datetime) -> QuantConnect.Data.Market.TradeBars:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Market.TradeBars:
        pass


    Item: indexer#
