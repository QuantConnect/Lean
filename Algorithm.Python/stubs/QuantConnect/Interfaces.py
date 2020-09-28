from .__Interfaces_1 import *
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

# no functions
# classes

class AlgorithmEvent(System.MulticastDelegate, System.Runtime.Serialization.ISerializable, System.ICloneable):
    """ AlgorithmEvent[T](object: object, method: IntPtr) """
    def BeginInvoke(self, algorithm: QuantConnect.Interfaces.IAlgorithm, eventData: QuantConnect.Interfaces.T, callback: System.AsyncCallback, object: object) -> System.IAsyncResult:
        pass

    def EndInvoke(self, result: System.IAsyncResult) -> None:
        pass

    def Invoke(self, algorithm: QuantConnect.Interfaces.IAlgorithm, eventData: QuantConnect.Interfaces.T) -> None:
        pass

    def __init__(self, object: object, method: System.IntPtr) -> QuantConnect.Interfaces.AlgorithmEvent:
        pass


class IAccountCurrencyProvider:
    """ A reduced interface for an account currency provider """
    AccountCurrency: str



class ISecurityInitializerProvider:
    """ Reduced interface which provides an instance which implements QuantConnect.Securities.ISecurityInitializer """
    SecurityInitializer: QuantConnect.Securities.ISecurityInitializer



class IAlgorithm(QuantConnect.Interfaces.IAccountCurrencyProvider, QuantConnect.Interfaces.ISecurityInitializerProvider):
    """
    Interface for QuantConnect algorithm implementations. All algorithms must implement these
                basic members to allow interaction with the Lean Backtesting Engine.
    """
    def AddChart(self, chart: QuantConnect.Chart) -> None:
        pass

    def AddFutureContract(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Future.Future:
        pass

    def AddOptionContract(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Option.Option:
        pass

    def AddSecurity(self, securityType: QuantConnect.SecurityType, symbol: str, resolution: typing.Optional[QuantConnect.Resolution], market: str, fillDataForward: bool, leverage: float, extendedMarketHours: bool) -> QuantConnect.Securities.Security:
        pass

    def Debug(self, message: str) -> None:
        pass

    def Error(self, message: str) -> None:
        pass

    def GetChartUpdates(self, clearChartData: bool) -> typing.List[QuantConnect.Chart]:
        pass

    def GetLocked(self) -> bool:
        pass

    def GetParameter(self, name: str) -> str:
        pass

    def GetWarmupHistoryRequests(self) -> typing.List[QuantConnect.Data.HistoryRequest]:
        pass

    def Initialize(self) -> None:
        pass

    def Liquidate(self, symbolToLiquidate: QuantConnect.Symbol, tag: str) -> typing.List[int]:
        pass

    def Log(self, message: str) -> None:
        pass

    def OnAssignmentOrderEvent(self, assignmentEvent: QuantConnect.Orders.OrderEvent) -> None:
        pass

    def OnBrokerageDisconnect(self) -> None:
        pass

    def OnBrokerageMessage(self, messageEvent: QuantConnect.Brokerages.BrokerageMessageEvent) -> None:
        pass

    def OnBrokerageReconnect(self) -> None:
        pass

    def OnData(self, slice: QuantConnect.Data.Slice) -> None:
        pass

    def OnEndOfAlgorithm(self) -> None:
        pass

    @typing.overload
    def OnEndOfDay(self) -> None:
        pass

    @typing.overload
    def OnEndOfDay(self, symbol: QuantConnect.Symbol) -> None:
        pass

    def OnEndOfDay(self, *args) -> None:
        pass

    def OnEndOfTimeStep(self) -> None:
        pass

    def OnFrameworkData(self, slice: QuantConnect.Data.Slice) -> None:
        pass

    def OnFrameworkSecuritiesChanged(self, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def OnMarginCall(self, requests: typing.List[QuantConnect.Orders.SubmitOrderRequest]) -> None:
        pass

    def OnMarginCallWarning(self) -> None:
        pass

    def OnOrderEvent(self, newEvent: QuantConnect.Orders.OrderEvent) -> None:
        pass

    def OnSecuritiesChanged(self, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def OnWarmupFinished(self) -> None:
        pass

    def PostInitialize(self) -> None:
        pass

    def RemoveSecurity(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def SetAccountCurrency(self, accountCurrency: str) -> None:
        pass

    def SetAlgorithmId(self, algorithmId: str) -> None:
        pass

    def SetApi(self, api: QuantConnect.Interfaces.IApi) -> None:
        pass

    def SetAvailableDataTypes(self, availableDataTypes: System.Collections.Generic.Dictionary[QuantConnect.SecurityType, typing.List[QuantConnect.TickType]]) -> None:
        pass

    def SetBrokerageMessageHandler(self, handler: QuantConnect.Brokerages.IBrokerageMessageHandler) -> None:
        pass

    def SetBrokerageModel(self, brokerageModel: QuantConnect.Brokerages.IBrokerageModel) -> None:
        pass

    @typing.overload
    def SetCash(self, startingCash: float) -> None:
        pass

    @typing.overload
    def SetCash(self, symbol: str, startingCash: float, conversionRate: float) -> None:
        pass

    def SetCash(self, *args) -> None:
        pass

    def SetCurrentSlice(self, slice: QuantConnect.Data.Slice) -> None:
        pass

    def SetDateTime(self, time: datetime.datetime) -> None:
        pass

    def SetEndDate(self, end: datetime.datetime) -> None:
        pass

    def SetFinishedWarmingUp(self) -> None:
        pass

    def SetFutureChainProvider(self, futureChainProvider: QuantConnect.Interfaces.IFutureChainProvider) -> None:
        pass

    def SetHistoryProvider(self, historyProvider: QuantConnect.Interfaces.IHistoryProvider) -> None:
        pass

    def SetLiveMode(self, live: bool) -> None:
        pass

    def SetLocked(self) -> None:
        pass

    def SetMaximumOrders(self, max: int) -> None:
        pass

    def SetObjectStore(self, objectStore: QuantConnect.Interfaces.IObjectStore) -> None:
        pass

    def SetOptionChainProvider(self, optionChainProvider: QuantConnect.Interfaces.IOptionChainProvider) -> None:
        pass

    def SetParameters(self, parameters: System.Collections.Generic.Dictionary[str, str]) -> None:
        pass

    def SetRunTimeError(self, exception: System.Exception) -> None:
        pass

    def SetStartDate(self, start: datetime.datetime) -> None:
        pass

    def SetStatus(self, status: QuantConnect.AlgorithmStatus) -> None:
        pass

    AlgorithmId: str

    Benchmark: QuantConnect.Benchmarks.IBenchmark

    BrokerageMessageHandler: QuantConnect.Brokerages.IBrokerageMessageHandler

    BrokerageModel: QuantConnect.Brokerages.IBrokerageModel

    CurrentSlice: QuantConnect.Data.Slice

    DebugMessages: System.Collections.Concurrent.ConcurrentQueue[str]

    EndDate: datetime.datetime

    ErrorMessages: System.Collections.Concurrent.ConcurrentQueue[str]

    FutureChainProvider: QuantConnect.Interfaces.IFutureChainProvider

    HistoryProvider: QuantConnect.Interfaces.IHistoryProvider

    IsWarmingUp: bool

    LiveMode: bool

    LogMessages: System.Collections.Concurrent.ConcurrentQueue[str]

    Name: str

    Notify: QuantConnect.Notifications.NotificationManager

    ObjectStore: QuantConnect.Storage.ObjectStore

    OptionChainProvider: QuantConnect.Interfaces.IOptionChainProvider

    Portfolio: QuantConnect.Securities.SecurityPortfolioManager

    RunTimeError: System.Exception

    RuntimeStatistics: System.Collections.Concurrent.ConcurrentDictionary[str, str]

    Schedule: QuantConnect.Scheduling.ScheduleManager

    Securities: QuantConnect.Securities.SecurityManager

    Settings: QuantConnect.Interfaces.IAlgorithmSettings

    StartDate: datetime.datetime

    Status: QuantConnect.AlgorithmStatus

    SubscriptionManager: QuantConnect.Data.SubscriptionManager

    Time: datetime.datetime

    TimeKeeper: QuantConnect.Interfaces.ITimeKeeper

    TimeZone: NodaTime.DateTimeZone

    TradeBuilder: QuantConnect.Interfaces.ITradeBuilder

    Transactions: QuantConnect.Securities.SecurityTransactionManager

    UniverseManager: QuantConnect.Securities.UniverseManager

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings

    UtcTime: datetime.datetime


    InsightsGenerated: BoundEvent


class IAlgorithmSettings:
    """ User settings for the algorithm which can be changed in the QuantConnect.Interfaces.IAlgorithm.Initialize method """
    DataSubscriptionLimit: int

    FreePortfolioValue: float

    FreePortfolioValuePercentage: float

    LiquidateEnabled: bool

    MaxAbsolutePortfolioTargetPercentage: float

    MinAbsolutePortfolioTargetPercentage: float

    RebalancePortfolioOnInsightChanges: typing.Optional[bool]

    RebalancePortfolioOnSecurityChanges: typing.Optional[bool]

    StalePriceTimeSpan: datetime.timedelta



class ISubscriptionDataConfigProvider:
    """ Reduced interface which provides access to registered QuantConnect.Data.SubscriptionDataConfig """
    def GetSubscriptionDataConfigs(self, symbol: QuantConnect.Symbol, includeInternalConfigs: bool) -> typing.List[QuantConnect.Data.SubscriptionDataConfig]:
        pass


class ISubscriptionDataConfigService(QuantConnect.Interfaces.ISubscriptionDataConfigProvider):
    """
    This interface exposes methods for creating a list of QuantConnect.Data.SubscriptionDataConfig for a given
                configuration
    """
    @typing.overload
    def Add(self, dataType: type, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], fillForward: bool, extendedMarketHours: bool, isFilteredSubscription: bool, isInternalFeed: bool, isCustomData: bool, dataNormalizationMode: QuantConnect.DataNormalizationMode) -> QuantConnect.Data.SubscriptionDataConfig:
        pass

    @typing.overload
    def Add(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], fillForward: bool, extendedMarketHours: bool, isFilteredSubscription: bool, isInternalFeed: bool, isCustomData: bool, subscriptionDataTypes: typing.List[System.Tuple[type, QuantConnect.TickType]], dataNormalizationMode: QuantConnect.DataNormalizationMode) -> typing.List[QuantConnect.Data.SubscriptionDataConfig]:
        pass

    def Add(self, *args) -> typing.List[QuantConnect.Data.SubscriptionDataConfig]:
        pass

    def LookupSubscriptionConfigDataTypes(self, symbolSecurityType: QuantConnect.SecurityType, resolution: QuantConnect.Resolution, isCanonical: bool) -> typing.List[System.Tuple[type, QuantConnect.TickType]]:
        pass

    AvailableDataTypes: System.Collections.Generic.Dictionary[QuantConnect.SecurityType, typing.List[QuantConnect.TickType]]
