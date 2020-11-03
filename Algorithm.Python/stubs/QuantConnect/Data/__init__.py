from .____init___1 import *
import typing
import System.Reflection
import System.Linq.Expressions
import System.IO
import System.Dynamic
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Securities
import QuantConnect.Packets
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import NodaTime
import datetime

# no functions
# classes

class BaseData(System.object, QuantConnect.Data.IBaseData):
    """
    Abstract base data class of QuantConnect. It is intended to be extended to define
                generic user customizable data types while at the same time implementing the basics of data where possible
    
    BaseData()
    """
    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def DataTimeZone(self) -> NodaTime.DateTimeZone:
        pass

    def DefaultResolution(self) -> QuantConnect.Resolution:
        pass

    @staticmethod
    def DeserializeMessage(serialized: str) -> typing.List[QuantConnect.Data.BaseData]:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
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

    def Update(self, lastTrade: float, bidPrice: float, askPrice: float, volume: float, bidSize: float, askSize: float) -> None:
        pass

    def UpdateAsk(self, askPrice: float, askSize: float) -> None:
        pass

    def UpdateBid(self, bidPrice: float, bidSize: float) -> None:
        pass

    def UpdateQuote(self, bidPrice: float, bidSize: float, askPrice: float, askSize: float) -> None:
        pass

    def UpdateTrade(self, lastTrade: float, tradeSize: float) -> None:
        pass

    DataType: QuantConnect.MarketDataType

    EndTime: datetime.datetime

    IsFillForward: bool

    Price: float

    Symbol: QuantConnect.Symbol

    Time: datetime.datetime

    Value: float


    AllResolutions: List[Resolution]
    DailyResolution: List[Resolution]
    MinuteResolution: List[Resolution]


class Channel(System.object):
    """
    Represents a subscription channel
    
    Channel(channelName: str, symbol: Symbol)
    """
    @typing.overload
    def Equals(self, other: QuantConnect.Data.Channel) -> bool:
        pass

    @typing.overload
    def Equals(self, obj: object) -> bool:
        pass

    def Equals(self, *args) -> bool:
        pass

    def GetHashCode(self) -> int:
        pass

    def __init__(self, channelName: str, symbol: QuantConnect.Symbol) -> QuantConnect.Data.Channel:
        pass

    Name: str

    Symbol: QuantConnect.Symbol


    Single: str


class DataQueueHandlerSubscriptionManager(System.object):
    """ Count number of subscribers for each channel (Symbol, Socket) pair """
    def GetSubscribedSymbols(self) -> typing.List[QuantConnect.Symbol]:
        pass

    def IsSubscribed(self, symbol: QuantConnect.Symbol, tickType: QuantConnect.TickType) -> bool:
        pass

    def Subscribe(self, dataConfig: QuantConnect.Data.SubscriptionDataConfig) -> None:
        pass

    def Unsubscribe(self, dataConfig: QuantConnect.Data.SubscriptionDataConfig) -> None:
        pass

    SubscribersByChannel: System.Collections.Concurrent.ConcurrentDictionary[QuantConnect.Data.Channel, int]

class DynamicData(QuantConnect.Data.BaseData, System.Dynamic.IDynamicMetaObjectProvider, QuantConnect.Data.IBaseData):
    """ Dynamic Data Class: Accept flexible data, adapting to the columns provided by source. """
    @typing.overload
    def Clone(self) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Clone(self, fillForward: bool) -> QuantConnect.Data.BaseData:
        pass

    def Clone(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def GetMetaObject(self, parameter: System.Linq.Expressions.Expression) -> System.Dynamic.DynamicMetaObject:
        pass

    def GetProperty(self, name: str) -> object:
        pass

    def GetStorageDictionary(self) -> System.Collections.Generic.IDictionary[str, object]:
        pass

    def HasProperty(self, name: str) -> bool:
        pass

    def SetProperty(self, name: str, value: object) -> object:
        pass


class EventBasedDataQueueHandlerSubscriptionManager(QuantConnect.Data.DataQueueHandlerSubscriptionManager):
    """
    Overrides QuantConnect.Data.DataQueueHandlerSubscriptionManager methods using events
    
    EventBasedDataQueueHandlerSubscriptionManager()
    EventBasedDataQueueHandlerSubscriptionManager(getChannelName: Func[TickType, str])
    """
    def Subscribe(self, dataConfig: QuantConnect.Data.SubscriptionDataConfig) -> None:
        pass

    def Unsubscribe(self, dataConfig: QuantConnect.Data.SubscriptionDataConfig) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.EventBasedDataQueueHandlerSubscriptionManager:
        pass

    @typing.overload
    def __init__(self, getChannelName: typing.Callable[[QuantConnect.TickType], str]) -> QuantConnect.Data.EventBasedDataQueueHandlerSubscriptionManager:
        pass

    def __init__(self, *args) -> QuantConnect.Data.EventBasedDataQueueHandlerSubscriptionManager:
        pass

    SubscribeImpl: typing.Callable[[typing.List[QuantConnect.Symbol], QuantConnect.TickType], bool]
    SubscribersByChannel: System.Collections.Concurrent.ConcurrentDictionary[QuantConnect.Data.Channel, int]
    UnsubscribeImpl: typing.Callable[[typing.List[QuantConnect.Symbol], QuantConnect.TickType], bool]

class FileFormat(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the format of data in a subscription
    
    enum FileFormat, values: Binary (1), Collection (3), Csv (0), Index (4), ZipEntryName (2)
    """
    value__: int
    Binary: 'FileFormat'
    Collection: 'FileFormat'
    Csv: 'FileFormat'
    Index: 'FileFormat'
    ZipEntryName: 'FileFormat'


class GetSetPropertyDynamicMetaObject(System.Dynamic.DynamicMetaObject):
    """
    Provides an implementation of System.Dynamic.DynamicMetaObject that uses get/set methods to update
                values in the dynamic object.
    
    GetSetPropertyDynamicMetaObject(expression: Expression, value: object, setPropertyMethodInfo: MethodInfo, getPropertyMethodInfo: MethodInfo)
    """
    def BindGetMember(self, binder: System.Dynamic.GetMemberBinder) -> System.Dynamic.DynamicMetaObject:
        pass

    def BindSetMember(self, binder: System.Dynamic.SetMemberBinder, value: System.Dynamic.DynamicMetaObject) -> System.Dynamic.DynamicMetaObject:
        pass

    def __init__(self, expression: System.Linq.Expressions.Expression, value: object, setPropertyMethodInfo: System.Reflection.MethodInfo, getPropertyMethodInfo: System.Reflection.MethodInfo) -> QuantConnect.Data.GetSetPropertyDynamicMetaObject:
        pass


class HistoryProviderBase(System.object, QuantConnect.Interfaces.IDataProviderEvents, QuantConnect.Interfaces.IHistoryProvider):
    """ Provides a base type for all history providers """
    def GetHistory(self, requests: typing.List[QuantConnect.Data.HistoryRequest], sliceTimeZone: NodaTime.DateTimeZone) -> typing.List[QuantConnect.Data.Slice]:
        pass

    def Initialize(self, parameters: QuantConnect.Data.HistoryProviderInitializeParameters) -> None:
        pass

    DataPointCount: int


    DownloadFailed: BoundEvent
    InvalidConfigurationDetected: BoundEvent
    NumericalPrecisionLimited: BoundEvent
    ReaderErrorDetected: BoundEvent
    StartDateLimited: BoundEvent


class HistoryProviderInitializeParameters(System.object):
    """
    Represents the set of parameters for the QuantConnect.Interfaces.IHistoryProvider.Initialize(QuantConnect.Data.HistoryProviderInitializeParameters) method
    
    HistoryProviderInitializeParameters(job: AlgorithmNodePacket, api: IApi, dataProvider: IDataProvider, dataCacheProvider: IDataCacheProvider, mapFileProvider: IMapFileProvider, factorFileProvider: IFactorFileProvider, statusUpdateAction: Action[int], parallelHistoryRequestsEnabled: bool, dataPermissionManager: IDataPermissionManager)
    """
    def __init__(self, job: QuantConnect.Packets.AlgorithmNodePacket, api: QuantConnect.Interfaces.IApi, dataProvider: QuantConnect.Interfaces.IDataProvider, dataCacheProvider: QuantConnect.Interfaces.IDataCacheProvider, mapFileProvider: QuantConnect.Interfaces.IMapFileProvider, factorFileProvider: QuantConnect.Interfaces.IFactorFileProvider, statusUpdateAction: typing.Callable[[int], None], parallelHistoryRequestsEnabled: bool, dataPermissionManager: QuantConnect.Interfaces.IDataPermissionManager) -> QuantConnect.Data.HistoryProviderInitializeParameters:
        pass

    Api: QuantConnect.Interfaces.IApi

    DataCacheProvider: QuantConnect.Interfaces.IDataCacheProvider

    DataPermissionManager: QuantConnect.Interfaces.IDataPermissionManager

    DataProvider: QuantConnect.Interfaces.IDataProvider

    FactorFileProvider: QuantConnect.Interfaces.IFactorFileProvider

    Job: QuantConnect.Packets.AlgorithmNodePacket

    MapFileProvider: QuantConnect.Interfaces.IMapFileProvider

    ParallelHistoryRequestsEnabled: bool

    StatusUpdateAction: typing.Callable[[int], None]
