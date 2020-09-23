from .__Interfaces_3 import *
import typing
import System.Threading
import System.IO
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Storage
import QuantConnect.Statistics
import QuantConnect.Securities.Option
import QuantConnect.Securities.Future
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Packets
import QuantConnect.Orders
import QuantConnect.Notifications
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Auxiliary
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Benchmarks
import QuantConnect.Api
import QuantConnect.API
import QuantConnect
import Python.Runtime
import NodaTime
import datetime



class IDataQueueUniverseProvider:
    """
    This interface allows interested parties to lookup or enumerate the available symbols. Data source exposes it if this feature is available.
                Availability of a symbol doesn't imply that it is possible to trade it. This is a data source specific interface, not broker specific.
    """
    def CanAdvanceTime(self, securityType: QuantConnect.SecurityType) -> bool:
        pass

    def LookupSymbols(self, lookupName: str, securityType: QuantConnect.SecurityType, includeExpired: bool, securityCurrency: str, securityExchange: str) -> typing.List[QuantConnect.Symbol]:
        pass


class IDownloadProvider:
    """ Wrapper on the API for downloading data for an algorithm. """
    def Download(self, address: str, headers: typing.List[System.Collections.Generic.KeyValuePair[str, str]], userName: str, password: str) -> str:
        pass


class IExtendedDictionary:
    # no doc
    def clear(self) -> None:
        pass

    def copy(self) -> Python.Runtime.PyDict:
        pass

    @typing.overload
    def fromkeys(self, sequence: typing.List[QuantConnect.Interfaces.TKey]) -> Python.Runtime.PyDict:
        pass

    @typing.overload
    def fromkeys(self, sequence: typing.List[QuantConnect.Interfaces.TKey], value: QuantConnect.Interfaces.TValue) -> Python.Runtime.PyDict:
        pass

    def fromkeys(self, *args) -> Python.Runtime.PyDict:
        pass

    @typing.overload
    def get(self, key: QuantConnect.Interfaces.TKey) -> QuantConnect.Interfaces.TValue:
        pass

    @typing.overload
    def get(self, key: QuantConnect.Interfaces.TKey, value: QuantConnect.Interfaces.TValue) -> QuantConnect.Interfaces.TValue:
        pass

    def get(self, *args) -> QuantConnect.Interfaces.TValue:
        pass

    def items(self) -> Python.Runtime.PyList:
        pass

    def keys(self) -> Python.Runtime.PyList:
        pass

    @typing.overload
    def pop(self, key: QuantConnect.Interfaces.TKey) -> QuantConnect.Interfaces.TValue:
        pass

    @typing.overload
    def pop(self, key: QuantConnect.Interfaces.TKey, default_value: QuantConnect.Interfaces.TValue) -> QuantConnect.Interfaces.TValue:
        pass

    def pop(self, *args) -> QuantConnect.Interfaces.TValue:
        pass

    def popitem(self) -> Python.Runtime.PyTuple:
        pass

    @typing.overload
    def setdefault(self, key: QuantConnect.Interfaces.TKey) -> QuantConnect.Interfaces.TValue:
        pass

    @typing.overload
    def setdefault(self, key: QuantConnect.Interfaces.TKey, default_value: QuantConnect.Interfaces.TValue) -> QuantConnect.Interfaces.TValue:
        pass

    def setdefault(self, *args) -> QuantConnect.Interfaces.TValue:
        pass

    def update(self, other: Python.Runtime.PyObject) -> None:
        pass

    def values(self) -> Python.Runtime.PyList:
        pass


class IFactorFileProvider:
    """ Provides instances of QuantConnect.Data.Auxiliary.FactorFile at run time """
    def Get(self, symbol: QuantConnect.Symbol) -> QuantConnect.Data.Auxiliary.FactorFile:
        pass


class IFutureChainProvider:
    """ Provides the full future chain for a given underlying. """
    def GetFutureContractList(self, symbol: QuantConnect.Symbol, date: datetime.datetime) -> typing.List[QuantConnect.Symbol]:
        pass


class IHistoryProvider(QuantConnect.Interfaces.IDataProviderEvents):
    """ Provides historical data to an algorithm at runtime """
    def GetHistory(self, requests: typing.List[QuantConnect.Data.HistoryRequest], sliceTimeZone: NodaTime.DateTimeZone) -> typing.List[QuantConnect.Data.Slice]:
        pass

    def Initialize(self, parameters: QuantConnect.Data.HistoryProviderInitializeParameters) -> None:
        pass

    DataPointCount: int



class IJobQueueHandler:
    """ Task requestor interface with cloud system """
    def AcknowledgeJob(self, job: QuantConnect.Packets.AlgorithmNodePacket) -> None:
        pass

    def Initialize(self, api: QuantConnect.Interfaces.IApi) -> None:
        pass

    def NextJob(self, algorithmPath: str) -> QuantConnect.Packets.AlgorithmNodePacket:
        pass


class IMapFileProvider:
    """ Provides instances of QuantConnect.Data.Auxiliary.MapFileResolver at run time """
    def Get(self, market: str) -> QuantConnect.Data.Auxiliary.MapFileResolver:
        pass


class IMessagingHandler(System.IDisposable):
    """
    Messaging System Plugin Interface. 
                Provides a common messaging pattern between desktop and cloud implementations of QuantConnect.
    """
    def Initialize(self) -> None:
        pass

    def Send(self, packet: QuantConnect.Packets.Packet) -> None:
        pass

    def SendNotification(self, notification: QuantConnect.Notifications.Notification) -> None:
        pass

    def SetAuthentication(self, job: QuantConnect.Packets.AlgorithmNodePacket) -> None:
        pass

    HasSubscribers: bool



class IObjectStore(System.IDisposable, System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[KeyValuePair[str, Array[Byte]]]):
    """ Provides object storage for data persistence. """
    def ContainsKey(self, key: str) -> bool:
        pass

    def Delete(self, key: str) -> bool:
        pass

    def GetFilePath(self, key: str) -> str:
        pass

    def Initialize(self, algorithmName: str, userId: int, projectId: int, userToken: str, controls: QuantConnect.Packets.Controls) -> None:
        pass

    def ReadBytes(self, key: str) -> typing.List[bytes]:
        pass

    def SaveBytes(self, key: str, contents: typing.List[bytes]) -> bool:
        pass

    ErrorRaised: BoundEvent


class IOptionChainProvider:
    """ Provides the full option chain for a given underlying. """
    def GetOptionContractList(self, symbol: QuantConnect.Symbol, date: datetime.datetime) -> typing.List[QuantConnect.Symbol]:
        pass


class ISecurityPrice:
    """
    Reduced interface which allows setting and accessing
                price properties for a QuantConnect.Securities.Security
    """
    def GetLastData(self) -> QuantConnect.Data.BaseData:
        pass

    def SetMarketPrice(self, data: QuantConnect.Data.BaseData) -> None:
        pass

    def Update(self, data: typing.List[QuantConnect.Data.BaseData], dataType: type, containsFillForwardData: typing.Optional[bool]) -> None:
        pass

    AskPrice: float

    AskSize: float

    BidPrice: float

    BidSize: float

    Close: float

    OpenInterest: int

    Price: float

    Symbol: QuantConnect.Symbol

    Volume: float



class IOptionPrice(QuantConnect.Interfaces.ISecurityPrice):
    """
    Reduced interface for accessing QuantConnect.Securities.Option.Option
                specific price properties and methods
    """
    def EvaluatePriceModel(self, slice: QuantConnect.Data.Slice, contract: QuantConnect.Data.Market.OptionContract) -> QuantConnect.Securities.Option.OptionPriceModelResult:
        pass

    Underlying: QuantConnect.Interfaces.ISecurityPrice



class IOrderProperties:
    """ Contains additional properties and settings for an order """
    def Clone(self) -> QuantConnect.Interfaces.IOrderProperties:
        pass

    TimeInForce: QuantConnect.Orders.TimeInForce



class IPriceProvider:
    """ Provides access to price data for a given asset """
    def GetLastPrice(self, symbol: QuantConnect.Symbol) -> float:
        pass


class IRegressionAlgorithmDefinition:
    """
    Defines a C# algorithm as a regression algorithm to be run as part of the test suite.
                This interface also allows the algorithm to declare that it has versions in other languages
                that should yield identical results.
    """
    CanRunLocally: bool

    ExpectedStatistics: System.Collections.Generic.Dictionary[str, str]

    Languages: typing.List[QuantConnect.Language]



class ISecurityService:
    """ This interface exposes methods for creating a new QuantConnect.Securities.Security """
    @typing.overload
    def CreateSecurity(self, symbol: QuantConnect.Symbol, subscriptionDataConfigList: typing.List[QuantConnect.Data.SubscriptionDataConfig], leverage: float, addToSymbolCache: bool) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def CreateSecurity(self, symbol: QuantConnect.Symbol, subscriptionDataConfig: QuantConnect.Data.SubscriptionDataConfig, leverage: float, addToSymbolCache: bool) -> QuantConnect.Securities.Security:
        pass

    def CreateSecurity(self, *args) -> QuantConnect.Securities.Security:
        pass


class IStreamReader(System.IDisposable):
    """ Defines a transport mechanism for data from its source into various reader methods """
    def ReadLine(self) -> str:
        pass

    EndOfStream: bool

    ShouldBeRateLimited: bool

    StreamReader: System.IO.StreamReader

    TransportMedium: QuantConnect.SubscriptionTransportMedium



class ITimeInForceHandler:
    """ Handles the time in force for an order """
    def IsFillValid(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, fill: QuantConnect.Orders.OrderEvent) -> bool:
        pass

    def IsOrderExpired(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> bool:
        pass


class ITimeKeeper:
    """ Interface implemented by QuantConnect.TimeKeeper """
    def AddTimeZone(self, timeZone: NodaTime.DateTimeZone) -> None:
        pass

    def GetLocalTimeKeeper(self, timeZone: NodaTime.DateTimeZone) -> QuantConnect.LocalTimeKeeper:
        pass

    UtcTime: datetime.datetime



class ITradeBuilder:
    """ Generates trades from executions and market price updates """
    def HasOpenPosition(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def ProcessFill(self, fill: QuantConnect.Orders.OrderEvent, securityConversionRate: float, feeInAccountCurrency: float, multiplier: float) -> None:
        pass

    def SetLiveMode(self, live: bool) -> None:
        pass

    def SetMarketPrice(self, symbol: QuantConnect.Symbol, price: float) -> None:
        pass

    ClosedTrades: typing.List[QuantConnect.Statistics.Trade]
