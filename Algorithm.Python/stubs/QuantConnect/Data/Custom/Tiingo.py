# encoding: utf-8
# module QuantConnect.Data.Custom.Tiingo calls itself Tiingo
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import Newtonsoft.Json
import Newtonsoft.Json.Linq
import NodaTime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Data.Custom.Tiingo
import System
import System.IO
import typing

# no functions
# classes

class Tiingo(System.object):
    """ Helper class for Tiingo configuration """
    @staticmethod
    def SetAuthCode(authCode: str) -> None:
        pass

    AuthCode: str
    IsAuthCodeSet: bool
    __all__: list


class TiingoPrice(QuantConnect.Data.Market.TradeBar, QuantConnect.Data.Market.IBar, QuantConnect.Data.Market.IBaseDataBar, QuantConnect.Data.IBaseData):
    """
    Tiingo daily price data
                https://api.tiingo.com/docs/tiingo/daily
    
    TiingoPrice()
    """
    def DataTimeZone(self) -> NodaTime.DateTimeZone:
        pass

    def DefaultResolution(self) -> QuantConnect.Resolution:
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
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, content: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def RequiresMapping(self) -> bool:
        pass

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    AdjustedClose: float

    AdjustedHigh: float

    AdjustedLow: float

    AdjustedOpen: float

    AdjustedVolume: int

    Close: float

    Date: datetime.datetime

    Dividend: float

    EndTime: datetime.datetime

    High: float

    Low: float

    Open: float

    Period: datetime.timedelta

    SplitFactor: float

    Volume: float



class TiingoDailyData(QuantConnect.Data.Custom.Tiingo.TiingoPrice, QuantConnect.Data.Market.IBar, QuantConnect.Data.Market.IBaseDataBar, QuantConnect.Data.IBaseData):
    """
    Tiingo daily price data
                https://api.tiingo.com/docs/tiingo/daily
    
    TiingoDailyData()
    """

class TiingoNews(QuantConnect.Data.IndexedBaseData, QuantConnect.Data.IBaseData):
    """
    Tiingo news data
                https://api.tiingo.com/documentation/news
    
    TiingoNews()
    """
    def DataTimeZone(self) -> NodaTime.DateTimeZone:
        pass

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

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, content: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def RequiresMapping(self) -> bool:
        pass

    ArticleID: str

    CrawlDate: datetime.datetime

    Description: str

    PublishedDate: datetime.datetime

    Source: str

    Symbols: typing.List[QuantConnect.Symbol]

    Tags: typing.List[str]

    Title: str

    Url: str



class TiingoNewsJsonConverter(Newtonsoft.Json.JsonConverter):
    """
    Helper json converter class used to convert a list of Tiingo news data
                into System.Collections.Generic.List
    
    TiingoNewsJsonConverter(symbol: Symbol)
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    @staticmethod
    def DeserializeNews(token: Newtonsoft.Json.Linq.JToken) -> QuantConnect.Data.Custom.Tiingo.TiingoNews:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass

    def __init__(self, symbol: QuantConnect.Symbol) -> QuantConnect.Data.Custom.Tiingo.TiingoNewsJsonConverter:
        pass


class TiingoSymbolMapper(System.object):
    """ Helper class to map a Lean format ticker to Tiingo format """
    @staticmethod
    def GetLeanTicker(ticker: str) -> str:
        pass

    @staticmethod
    def GetTiingoTicker(symbol: QuantConnect.Symbol) -> str:
        pass

    __all__: list


