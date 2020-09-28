# encoding: utf-8
# module QuantConnect.Data.Custom.Intrinio calls itself Intrinio
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Data.Custom.Intrinio
import System
import System.IO
import typing

# no functions
# classes

class IntrinioConfig(System.object):
    """ Auxiliary class to access all Intrinio API data. """
    @staticmethod
    def SetTimeIntervalBetweenCalls(timeSpan: datetime.timedelta) -> None:
        pass

    @staticmethod
    def SetUserAndPassword(user: str, password: str) -> None:
        pass

    IsInitialized: bool
    Password: str
    RateGate: RateGate
    User: str
    __all__: list


class IntrinioDataTransformation(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    TRanformation available for the Economic data.
    
    enum IntrinioDataTransformation, values: AnnualyCCRoc (3), AnnualyPc (8), AnnualyRoc (1), CCRoc (4), CompoundedAnnualRoc (2), Level (5), Ln (6), Pc (7), Roc (0)
    """
    value__: int
    AnnualyCCRoc: 'IntrinioDataTransformation'
    AnnualyPc: 'IntrinioDataTransformation'
    AnnualyRoc: 'IntrinioDataTransformation'
    CCRoc: 'IntrinioDataTransformation'
    CompoundedAnnualRoc: 'IntrinioDataTransformation'
    Level: 'IntrinioDataTransformation'
    Ln: 'IntrinioDataTransformation'
    Pc: 'IntrinioDataTransformation'
    Roc: 'IntrinioDataTransformation'


class IntrinioEconomicData(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Access the massive repository of economic data from the Federal Reserve Economic Data system via the Intrinio API.
    
    IntrinioEconomicData()
    IntrinioEconomicData(dataTransformation: IntrinioDataTransformation)
    """
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
    def __init__(self) -> QuantConnect.Data.Custom.Intrinio.IntrinioEconomicData:
        pass

    @typing.overload
    def __init__(self, dataTransformation: QuantConnect.Data.Custom.Intrinio.IntrinioDataTransformation) -> QuantConnect.Data.Custom.Intrinio.IntrinioEconomicData:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.Intrinio.IntrinioEconomicData:
        pass


class IntrinioEconomicDataSources(System.object):
    # no doc
    BofAMerrillLynch: type
    CBOE: type
    Commodities: type
    ExchangeRates: type
    Moodys: type
    TradeWeightedUsDollaIndex: type
    __all__: list


