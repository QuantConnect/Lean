from .__Interfaces_2 import *
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



class IAlgorithmSubscriptionManager(QuantConnect.Interfaces.ISubscriptionDataConfigProvider, QuantConnect.Interfaces.ISubscriptionDataConfigService):
    """ AlgorithmSubscriptionManager interface will manage the subscriptions for the SubscriptionManager """
    def SubscriptionManagerCount(self) -> int:
        pass

    SubscriptionManagerSubscriptions: typing.List[QuantConnect.Data.SubscriptionDataConfig]



class IApi(System.IDisposable):
    """ API for QuantConnect.com """
    def AddProjectFile(self, projectId: int, name: str, content: str) -> QuantConnect.Api.ProjectFilesResponse:
        pass

    def CreateBacktest(self, projectId: int, compileId: str, backtestName: str) -> QuantConnect.Api.Backtest:
        pass

    def CreateCompile(self, projectId: int) -> QuantConnect.Api.Compile:
        pass

    def CreateLiveAlgorithm(self, projectId: int, compileId: str, serverType: str, baseLiveAlgorithmSettings: QuantConnect.API.BaseLiveAlgorithmSettings, versionId: str) -> QuantConnect.API.LiveAlgorithm:
        pass

    def CreateProject(self, name: str, language: QuantConnect.Language) -> QuantConnect.Api.ProjectResponse:
        pass

    def DeleteBacktest(self, projectId: int, backtestId: str) -> QuantConnect.Api.RestResponse:
        pass

    def DeleteProject(self, projectId: int) -> QuantConnect.Api.RestResponse:
        pass

    def DeleteProjectFile(self, projectId: int, name: str) -> QuantConnect.Api.RestResponse:
        pass

    def Download(self, address: str, headers: typing.List[System.Collections.Generic.KeyValuePair[str, str]], userName: str, password: str) -> str:
        pass

    def DownloadData(self, symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, date: datetime.datetime) -> bool:
        pass

    def GetAlgorithmStatus(self, algorithmId: str) -> QuantConnect.AlgorithmControl:
        pass

    def GetDividends(self, from_: datetime.datetime, to: datetime.datetime) -> typing.List[QuantConnect.Data.Market.Dividend]:
        pass

    def GetSplits(self, from_: datetime.datetime, to: datetime.datetime) -> typing.List[QuantConnect.Data.Market.Split]:
        pass

    def Initialize(self, userId: int, token: str, dataFolder: str) -> None:
        pass

    def LiquidateLiveAlgorithm(self, projectId: int) -> QuantConnect.Api.RestResponse:
        pass

    def ListBacktests(self, projectId: int) -> QuantConnect.Api.BacktestList:
        pass

    def ListLiveAlgorithms(self, status: typing.Optional[QuantConnect.AlgorithmStatus], startTime: typing.Optional[datetime.datetime], endTime: typing.Optional[datetime.datetime]) -> QuantConnect.API.LiveList:
        pass

    def ListProjects(self) -> QuantConnect.Api.ProjectResponse:
        pass

    def ReadBacktest(self, projectId: int, backtestId: str) -> QuantConnect.Api.Backtest:
        pass

    def ReadCompile(self, projectId: int, compileId: str) -> QuantConnect.Api.Compile:
        pass

    def ReadDataLink(self, symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, date: datetime.datetime) -> QuantConnect.Api.Link:
        pass

    def ReadLiveAlgorithm(self, projectId: int, deployId: str) -> QuantConnect.API.LiveAlgorithmResults:
        pass

    def ReadLiveLogs(self, projectId: int, algorithmId: str, startTime: typing.Optional[datetime.datetime], endTime: typing.Optional[datetime.datetime]) -> QuantConnect.API.LiveLog:
        pass

    def ReadPrices(self, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.API.PricesList:
        pass

    def ReadProject(self, projectId: int) -> QuantConnect.Api.ProjectResponse:
        pass

    def ReadProjectFile(self, projectId: int, fileName: str) -> QuantConnect.Api.ProjectFilesResponse:
        pass

    def ReadProjectFiles(self, projectId: int) -> QuantConnect.Api.ProjectFilesResponse:
        pass

    def SendStatistics(self, algorithmId: str, unrealized: float, fees: float, netProfit: float, holdings: float, equity: float, netReturn: float, volume: float, trades: int, sharpe: float) -> None:
        pass

    def SendUserEmail(self, algorithmId: str, subject: str, body: str) -> None:
        pass

    def SetAlgorithmStatus(self, algorithmId: str, status: QuantConnect.AlgorithmStatus, message: str) -> None:
        pass

    def StopLiveAlgorithm(self, projectId: int) -> QuantConnect.Api.RestResponse:
        pass

    def UpdateBacktest(self, projectId: int, backtestId: str, backtestName: str, backtestNote: str) -> QuantConnect.Api.RestResponse:
        pass

    def UpdateProjectFileContent(self, projectId: int, fileName: str, newFileContents: str) -> QuantConnect.Api.RestResponse:
        pass

    def UpdateProjectFileName(self, projectId: int, oldFileName: str, newFileName: str) -> QuantConnect.Api.RestResponse:
        pass


class IBrokerageCashSynchronizer:
    """ Defines live brokerage cash synchronization operations. """
    def PerformCashSync(self, algorithm: QuantConnect.Interfaces.IAlgorithm, currentTimeUtc: datetime.datetime, getTimeSinceLastFill: typing.Callable[[], datetime.timedelta]) -> bool:
        pass

    def ShouldPerformCashSync(self, currentTimeUtc: datetime.datetime) -> bool:
        pass

    LastSyncDateTimeUtc: datetime.datetime



class IBrokerage(System.IDisposable, QuantConnect.Interfaces.IBrokerageCashSynchronizer):
    """
    Brokerage interface that defines the operations all brokerages must implement. The IBrokerage implementation
                must have a matching IBrokerageFactory implementation.
    """
    def CancelOrder(self, order: QuantConnect.Orders.Order) -> bool:
        pass

    def Connect(self) -> None:
        pass

    def Disconnect(self) -> None:
        pass

    def GetAccountHoldings(self) -> typing.List[QuantConnect.Holding]:
        pass

    def GetCashBalance(self) -> typing.List[QuantConnect.Securities.CashAmount]:
        pass

    def GetHistory(self, request: QuantConnect.Data.HistoryRequest) -> typing.List[QuantConnect.Data.BaseData]:
        pass

    def GetOpenOrders(self) -> typing.List[QuantConnect.Orders.Order]:
        pass

    def PlaceOrder(self, order: QuantConnect.Orders.Order) -> bool:
        pass

    def UpdateOrder(self, order: QuantConnect.Orders.Order) -> bool:
        pass

    AccountInstantlyUpdated: bool

    IsConnected: bool

    Name: str


    AccountChanged: BoundEvent
    Message: BoundEvent
    OptionPositionAssigned: BoundEvent
    OrderStatusChanged: BoundEvent


class IBrokerageFactory(System.IDisposable):
    """ Defines factory types for brokerages. Every IBrokerage is expected to also implement an IBrokerageFactory. """
    def CreateBrokerage(self, job: QuantConnect.Packets.LiveNodePacket, algorithm: QuantConnect.Interfaces.IAlgorithm) -> QuantConnect.Interfaces.IBrokerage:
        pass

    def CreateBrokerageMessageHandler(self, algorithm: QuantConnect.Interfaces.IAlgorithm, job: QuantConnect.Packets.AlgorithmNodePacket, api: QuantConnect.Interfaces.IApi) -> QuantConnect.Brokerages.IBrokerageMessageHandler:
        pass

    def GetBrokerageModel(self, orderProvider: QuantConnect.Securities.IOrderProvider) -> QuantConnect.Brokerages.IBrokerageModel:
        pass

    BrokerageData: System.Collections.Generic.Dictionary[str, str]

    BrokerageType: type



class IBusyCollection(System.IDisposable):
    # no doc
    @typing.overload
    def Add(self, item: QuantConnect.Interfaces.T) -> None:
        pass

    @typing.overload
    def Add(self, item: QuantConnect.Interfaces.T, cancellationToken: System.Threading.CancellationToken) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def CompleteAdding(self) -> None:
        pass

    @typing.overload
    def GetConsumingEnumerable(self) -> typing.List[QuantConnect.Interfaces.T]:
        pass

    @typing.overload
    def GetConsumingEnumerable(self, cancellationToken: System.Threading.CancellationToken) -> typing.List[QuantConnect.Interfaces.T]:
        pass

    def GetConsumingEnumerable(self, *args) -> typing.List[QuantConnect.Interfaces.T]:
        pass

    Count: int

    IsBusy: bool

    WaitHandle: System.Threading.WaitHandle



class IDataCacheProvider(System.IDisposable):
    """ Defines a cache for data """
    def Fetch(self, key: str) -> System.IO.Stream:
        pass

    def Store(self, key: str, data: typing.List[bytes]) -> None:
        pass

    IsDataEphemeral: bool



class IDataChannelProvider:
    """ Specifies data channel settings """
    def ShouldStreamSubscription(self, job: QuantConnect.Packets.LiveNodePacket, config: QuantConnect.Data.SubscriptionDataConfig) -> bool:
        pass


class IDataPermissionManager:
    """ Entity in charge of handling data permissions """
    def AssertConfiguration(self, subscriptionDataConfig: QuantConnect.Data.SubscriptionDataConfig) -> None:
        pass

    def GetResolution(self, preferredResolution: QuantConnect.Resolution) -> QuantConnect.Resolution:
        pass

    def Initialize(self, job: QuantConnect.Packets.AlgorithmNodePacket) -> None:
        pass

    DataChannelProvider: QuantConnect.Interfaces.IDataChannelProvider



class IDataProvider:
    """
    Fetches a remote file for a security.
                Must save the file to Globals.DataFolder.
    """
    def Fetch(self, key: str) -> System.IO.Stream:
        pass


class IDataProviderEvents:
    """ Events related to data providers """
    DownloadFailed: BoundEvent
    InvalidConfigurationDetected: BoundEvent
    NumericalPrecisionLimited: BoundEvent
    ReaderErrorDetected: BoundEvent
    StartDateLimited: BoundEvent


class IDataQueueHandler(System.IDisposable):
    """ Task requestor interface with cloud system """
    def SetJob(self, job: QuantConnect.Packets.LiveNodePacket) -> None:
        pass

    def Subscribe(self, dataConfig: QuantConnect.Data.SubscriptionDataConfig, newDataAvailableHandler: System.EventHandler) -> System.Collections.Generic.IEnumerator[QuantConnect.Data.BaseData]:
        pass

    def Unsubscribe(self, dataConfig: QuantConnect.Data.SubscriptionDataConfig) -> None:
        pass

    IsConnected: bool
