# encoding: utf-8
# module QuantConnect.Data.Custom.Benzinga calls itself Benzinga
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
import QuantConnect.Data.Custom.Benzinga
import System
import System.IO
import typing

# no functions
# classes

class BenzingaNews(QuantConnect.Data.IndexedBaseData, QuantConnect.Data.IBaseData):
    """
    News data powered by Benzinga - https://docs.benzinga.io/benzinga/newsfeed-v2.html
    
    BenzingaNews()
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

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

    def GetSourceForAnIndex(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, index: str, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    def IsSparseData(self) -> bool:
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

    def RequiresMapping(self) -> bool:
        pass

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    def ToString(self) -> str:
        pass

    Author: str

    Categories: typing.List[str]

    Contents: str

    CreatedAt: datetime.datetime

    EndTime: datetime.datetime

    Id: int

    Symbols: typing.List[QuantConnect.Symbol]

    Tags: typing.List[str]

    Teaser: str

    Title: str

    UpdatedAt: datetime.datetime



class BenzingaNewsJsonConverter(Newtonsoft.Json.JsonConverter):
    """
    Helper json converter class used to convert Benzinga news data
                 into QuantConnect.Data.Custom.Benzinga.BenzingaNews
                
                 An example schema of the data in a serialized format is provided
                 to help you better understand this converter.
    
    BenzingaNewsJsonConverter(symbol: Symbol, liveMode: bool)
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    @staticmethod
    def DeserializeNews(item: Newtonsoft.Json.Linq.JToken, enableLogging: bool) -> QuantConnect.Data.Custom.Benzinga.BenzingaNews:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass

    def __init__(self, symbol: QuantConnect.Symbol, liveMode: bool) -> QuantConnect.Data.Custom.Benzinga.BenzingaNewsJsonConverter:
        pass

    ShareClassMappedTickers: Dictionary[str, HashSet[str]]


