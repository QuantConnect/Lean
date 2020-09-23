# encoding: utf-8
# module QuantConnect.Data.Custom.USTreasury calls itself USTreasury
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Data
import System
import System.IO
import typing

# no functions
# classes

class USTreasuryYieldCurveRate(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    U.S. Treasury yield curve data
    
    USTreasuryYieldCurveRate()
    """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
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

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    FiveYear: typing.Optional[float]

    OneMonth: typing.Optional[float]

    OneYear: typing.Optional[float]

    SevenYear: typing.Optional[float]

    SixMonth: typing.Optional[float]

    TenYear: typing.Optional[float]

    ThirtyYear: typing.Optional[float]

    ThreeMonth: typing.Optional[float]

    ThreeYear: typing.Optional[float]

    TwentyYear: typing.Optional[float]

    TwoMonth: typing.Optional[float]

    TwoYear: typing.Optional[float]



