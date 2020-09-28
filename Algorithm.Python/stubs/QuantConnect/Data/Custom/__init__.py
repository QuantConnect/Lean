import typing
import System.IO
import System
import QuantConnect.Data.Custom
import QuantConnect.Data
import QuantConnect
import datetime

# no functions
# classes

class FxcmVolume(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    FXCM Real FOREX Volume and Transaction data from its clients base, available for the following pairs:
                    - EURUSD, USDJPY, GBPUSD, USDCHF, EURCHF, AUDUSD, USDCAD,
                      NZDUSD, EURGBP, EURJPY, GBPJPY, EURAUD, EURCAD, AUDJPY
                    FXCM only provides support for FX symbols which produced over 110 million average daily volume (ADV) during 2013.
                    This limit is imposed to ensure we do not highlight low volume/low ticket symbols in addition to other financial
                    reporting concerns.
    
    FxcmVolume()
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

    Transactions: int

    Volume: int



class NullData(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Represents a custom data type that works as a heartbeat of data in live mode
    
    NullData()
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

    EndTime: datetime.datetime



class Quandl(QuantConnect.Data.DynamicData, System.Dynamic.IDynamicMetaObjectProvider, QuantConnect.Data.IBaseData):
    """
    Quandl Data Type - Import generic data from quandl, without needing to define Reader methods.
                This reads the headers of the data imported, and dynamically creates properties for the imported data.
    
    Quandl()
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

    @staticmethod
    def SetAuthCode(authCode: str) -> None:
        pass

    def __init__(self) -> QuantConnect.Data.Custom.Quandl:
        pass

    EndTime: datetime.datetime

    Period: datetime.timedelta


    IsAuthCodeSet: bool


class USEnergyAPI(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    US Energy Information Administration provides extensive data on energy usage, import, export,
                and forecasting across all US energy sectors.
                https://www.eia.gov/opendata/
    
    USEnergyAPI()
    """
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

    @staticmethod
    def SetAuthCode(authCode: str) -> None:
        pass

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    EndTime: datetime.datetime

    EnergyDataPointCloseTime: datetime.datetime


    AuthCode: str
    IsAuthCodeSet: bool
