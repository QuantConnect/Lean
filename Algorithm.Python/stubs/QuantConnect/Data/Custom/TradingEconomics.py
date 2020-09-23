# encoding: utf-8
# module QuantConnect.Data.Custom.TradingEconomics calls itself TradingEconomics
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import Newtonsoft.Json
import NodaTime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Data.Custom.TradingEconomics
import System
import System.IO
import typing

# no functions
# classes

class EarningsType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Earnings type: earnings, ipo, dividends
    
    enum EarningsType, values: Dividends (2), Earnings (0), IPO (1), Split (3)
    """
    value__: int
    Dividends: 'EarningsType'
    Earnings: 'EarningsType'
    IPO: 'EarningsType'
    Split: 'EarningsType'


class TradingEconomics(System.object):
    """ TradingEconomics static class contains shortcut definitions of major Trading Economics Indicators available """
    Calendar: type
    Event: type
    Indicator: type
    __all__: list


class TradingEconomicsCalendar(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Represents the Trading Economics Calendar information:
                The economic calendar covers around 1600 events for more than 150 countries a month.
                https://docs.tradingeconomics.com/#events
    
    TradingEconomicsCalendar()
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
    def CountryToCurrencyCode(country: str) -> str:
        pass

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

    @staticmethod
    def ParseDecimal(value: str, inPercent: bool) -> typing.Optional[float]:
        pass

    @staticmethod
    def ProcessAPIResponse(content: str) -> typing.List[QuantConnect.Data.Custom.TradingEconomics.TradingEconomicsCalendar]:
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

    @staticmethod
    def SetAuthCode(authCode: str) -> None:
        pass

    def ToCsv(self) -> str:
        pass

    def ToString(self) -> str:
        pass

    Actual: typing.Optional[float]

    CalendarId: str

    Category: str

    Country: str

    DateSpan: str

    EndTime: datetime.datetime

    Event: str

    EventRaw: str

    Forecast: typing.Optional[float]

    Importance: QuantConnect.Data.Custom.TradingEconomics.TradingEconomicsImportance

    IsPercentage: bool

    LastUpdate: datetime.datetime

    OCategory: str

    OCountry: str

    Previous: typing.Optional[float]

    Reference: str

    Revised: typing.Optional[float]

    Source: str

    Ticker: str

    TradingEconomicsForecast: typing.Optional[float]


    AuthCode: str
    IsAuthCodeSet: bool


class TradingEconomicsDateTimeConverter(Newtonsoft.Json.JsonConverter):
    """
    DateTime JSON Converter that handles null value
    
    TradingEconomicsDateTimeConverter()
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass


class TradingEconomicsEarnings(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Represents the Trading Economics Earnings information.
                https://docs.tradingeconomics.com/#earnings
    
    TradingEconomicsEarnings()
    """
    def DataTimeZone(self) -> NodaTime.DateTimeZone:
        pass

    Actual: typing.Optional[float]

    CalendarReference: str

    Country: str

    Currency: str

    EarningsType: QuantConnect.Data.Custom.TradingEconomics.EarningsType

    EndTime: datetime.datetime

    FiscalReference: str

    FiscalTag: str

    Forecast: typing.Optional[float]

    LastUpdate: datetime.datetime

    Name: str

    Symbol: str

    Value: float



class TradingEconomicsEventFilter(System.object):
    """ Provides methods to filter and standardize Trading Economics calendar event names. """
    @staticmethod
    def FilterEvent(eventName: str) -> str:
        pass

    __all__: list


class TradingEconomicsImportance(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Importance of a TradingEconomics information
    
    enum TradingEconomicsImportance, values: High (2), Low (0), Medium (1)
    """
    value__: int
    High: 'TradingEconomicsImportance'
    Low: 'TradingEconomicsImportance'
    Medium: 'TradingEconomicsImportance'


class TradingEconomicsIndicator(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Represents the Trading Economics Indicator information.
                https://docs.tradingeconomics.com/#indicators
    
    TradingEconomicsIndicator()
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

    def ToString(self) -> str:
        pass

    Category: str

    Country: str

    EndTime: datetime.datetime

    Frequency: str

    HistoricalDataSymbol: str

    LastUpdate: datetime.datetime

    Value: float



