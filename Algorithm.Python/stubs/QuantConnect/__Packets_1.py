from .__Packets_2 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Statistics
import QuantConnect.Securities
import QuantConnect.Packets
import QuantConnect.Orders
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect
import datetime

class Controls(System.object):
    """
    Specifies values used to control algorithm limits
    
    Controls()
    """
    def GetLimit(self, resolution: QuantConnect.Resolution) -> int:
        pass

    BacktestingMaxOrders: int

    BacktestingMaxInsights: int
    BacktestLogLimit: int
    CpuAllocation: float
    DailyLogLimit: int
    DataResolutionPermissions: System.Collections.Generic.HashSet[QuantConnect.Resolution]
    MaximumDataPointsPerChartSeries: int
    MinuteLimit: int
    PersistenceIntervalSeconds: int
    RamAllocation: int
    RemainingLogAllowance: int
    SecondLimit: int
    SecondTimeOut: int
    StorageFileCount: int
    StorageLimitMB: int
    StoragePermissions: System.IO.FileAccess
    StreamingDataPermissions: System.Collections.Generic.HashSet[str]
    TickLimit: int
    TrainingLimits: QuantConnect.Packets.LeakyBucketControlParameters


class DebugPacket(QuantConnect.Packets.Packet):
    """
    Send a simple debug message from the users algorithm to the console.
    
    DebugPacket()
    DebugPacket(projectId: int, algorithmId: str, compileId: str, message: str, toast: bool)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.DebugPacket:
        pass

    @typing.overload
    def __init__(self, projectId: int, algorithmId: str, compileId: str, message: str, toast: bool) -> QuantConnect.Packets.DebugPacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.DebugPacket:
        pass

    AlgorithmId: str
    CompileId: str
    Message: str
    ProjectId: int
    Toast: bool

class ErrorHistoryResult(QuantConnect.Packets.HistoryResult):
    """
    Specfies an error message in a history result
    
    ErrorHistoryResult()
    ErrorHistoryResult(message: str)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.ErrorHistoryResult:
        pass

    @typing.overload
    def __init__(self, message: str) -> QuantConnect.Packets.ErrorHistoryResult:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.ErrorHistoryResult:
        pass

    Message: str

class FileHistoryResult(QuantConnect.Packets.HistoryResult):
    """
    Defines requested file data for a history request
    
    FileHistoryResult()
    FileHistoryResult(filepath: str, file: Array[Byte])
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.FileHistoryResult:
        pass

    @typing.overload
    def __init__(self, filepath: str, file: typing.List[bytes]) -> QuantConnect.Packets.FileHistoryResult:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.FileHistoryResult:
        pass

    File: typing.List[bytes]
    Filepath: str

class HandledErrorPacket(QuantConnect.Packets.Packet):
    """
    Algorithm runtime error packet from the lean engine. 
                This is a managed error which stops the algorithm execution.
    
    HandledErrorPacket()
    HandledErrorPacket(algorithmId: str, message: str, stacktrace: str)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.HandledErrorPacket:
        pass

    @typing.overload
    def __init__(self, algorithmId: str, message: str, stacktrace: str) -> QuantConnect.Packets.HandledErrorPacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.HandledErrorPacket:
        pass

    AlgorithmId: str
    Message: str
    StackTrace: str

class HistoryPacket(QuantConnect.Packets.Packet):
    """
    Packet for history jobs
    
    HistoryPacket()
    """
    QueueName: str
    Requests: typing.List[QuantConnect.Packets.HistoryRequest]

class HistoryRequest(System.object):
    """
    Specifies request parameters for a single historical request.
                A HistoryPacket is made of multiple requests for data. These
                are used to request data during live mode from a data server
    
    HistoryRequest()
    """
    EndTimeUtc: datetime.datetime
    Resolution: QuantConnect.Resolution
    StartTimeUtc: datetime.datetime
    Symbol: QuantConnect.Symbol
    TickType: QuantConnect.TickType

class HistoryResultType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies various types of history results
    
    enum HistoryResultType, values: Completed (2), Error (3), File (0), Status (1)
    """
    value__: int
    Completed: 'HistoryResultType'
    Error: 'HistoryResultType'
    File: 'HistoryResultType'
    Status: 'HistoryResultType'


class LeakyBucketControlParameters(System.object):
    """
    Provides parameters that control the behavior of a leaky bucket rate limiting algorithm. The
                parameter names below are phrased in the positive, such that the bucket is filled up over time
                vs leaking out over time.
    
    LeakyBucketControlParameters()
    LeakyBucketControlParameters(capacity: int, refillAmount: int, timeIntervalMinutes: int)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.LeakyBucketControlParameters:
        pass

    @typing.overload
    def __init__(self, capacity: int, refillAmount: int, timeIntervalMinutes: int) -> QuantConnect.Packets.LeakyBucketControlParameters:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.LeakyBucketControlParameters:
        pass

    Capacity: int
    RefillAmount: int
    TimeIntervalMinutes: int
    DefaultCapacity: int
    DefaultRefillAmount: int
    DefaultTimeInterval: int


class LiveResult(QuantConnect.Result):
    """
    Live results object class for packaging live result data.
    
    LiveResult()
    LiveResult(parameters: LiveResultParameters)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.LiveResult:
        pass

    @typing.overload
    def __init__(self, parameters: QuantConnect.Packets.LiveResultParameters) -> QuantConnect.Packets.LiveResult:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.LiveResult:
        pass

    Cash: QuantConnect.Securities.CashBook
    Holdings: System.Collections.Generic.IDictionary[str, QuantConnect.Holding]

class LiveResultPacket(QuantConnect.Packets.Packet):
    """
    Live result packet from a lean engine algorithm.
    
    LiveResultPacket()
    LiveResultPacket(json: str)
    LiveResultPacket(job: LiveNodePacket, results: LiveResult)
    """
    @staticmethod
    def CreateEmpty(job: QuantConnect.Packets.LiveNodePacket) -> QuantConnect.Packets.LiveResultPacket:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Packets.LiveResultPacket:
        pass

    @typing.overload
    def __init__(self, json: str) -> QuantConnect.Packets.LiveResultPacket:
        pass

    @typing.overload
    def __init__(self, job: QuantConnect.Packets.LiveNodePacket, results: QuantConnect.Packets.LiveResult) -> QuantConnect.Packets.LiveResultPacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.LiveResultPacket:
        pass

    CompileId: str
    DeployId: str
    ProcessingTime: float
    ProjectId: int
    Results: QuantConnect.Packets.LiveResult
    SessionId: str
    UserId: int

class LiveResultParameters(QuantConnect.Packets.BaseResultParameters):
    """
    Defines the parameters for QuantConnect.Packets.LiveResult
    
    LiveResultParameters(charts: IDictionary[str, Chart], orders: IDictionary[int, Order], profitLoss: IDictionary[DateTime, Decimal], holdings: IDictionary[str, Holding], cashBook: CashBook, statistics: IDictionary[str, str], runtimeStatistics: IDictionary[str, str], orderEvents: List[OrderEvent], serverStatistics: IDictionary[str, str], alphaRuntimeStatistics: AlphaRuntimeStatistics)
    """
    def __init__(self, charts: System.Collections.Generic.IDictionary[str, QuantConnect.Chart], orders: System.Collections.Generic.IDictionary[int, QuantConnect.Orders.Order], profitLoss: System.Collections.Generic.IDictionary[datetime.datetime, float], holdings: System.Collections.Generic.IDictionary[str, QuantConnect.Holding], cashBook: QuantConnect.Securities.CashBook, statistics: System.Collections.Generic.IDictionary[str, str], runtimeStatistics: System.Collections.Generic.IDictionary[str, str], orderEvents: typing.List[QuantConnect.Orders.OrderEvent], serverStatistics: System.Collections.Generic.IDictionary[str, str], alphaRuntimeStatistics: QuantConnect.AlphaRuntimeStatistics) -> QuantConnect.Packets.LiveResultParameters:
        pass

    CashBook: QuantConnect.Securities.CashBook

    Holdings: System.Collections.Generic.IDictionary[str, QuantConnect.Holding]

    ServerStatistics: System.Collections.Generic.IDictionary[str, str]



class LogPacket(QuantConnect.Packets.Packet):
    """
    Simple log message instruction from the lean engine.
    
    LogPacket()
    LogPacket(algorithmId: str, message: str)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.LogPacket:
        pass

    @typing.overload
    def __init__(self, algorithmId: str, message: str) -> QuantConnect.Packets.LogPacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.LogPacket:
        pass

    AlgorithmId: str
    Message: str

class MarketHours(System.object):
    """
    Market open hours model for pre, normal and post market hour definitions.
    
    MarketHours(referenceDate: DateTime, defaultStart: float, defaultEnd: float)
    """
    def __init__(self, referenceDate: datetime.datetime, defaultStart: float, defaultEnd: float) -> QuantConnect.Packets.MarketHours:
        pass

    End: datetime.datetime
    Start: datetime.datetime

class MarketToday(System.object):
    """
    Market today information class
    
    MarketToday()
    """
    Date: datetime.datetime

    Open: QuantConnect.Packets.MarketHours
    PostMarket: QuantConnect.Packets.MarketHours
    PreMarket: QuantConnect.Packets.MarketHours
    Status: str


class OrderEventPacket(QuantConnect.Packets.Packet):
    """
    Order event packet for passing updates on the state of an order to the portfolio.
    
    OrderEventPacket()
    OrderEventPacket(algorithmId: str, eventOrder: OrderEvent)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.OrderEventPacket:
        pass

    @typing.overload
    def __init__(self, algorithmId: str, eventOrder: QuantConnect.Orders.OrderEvent) -> QuantConnect.Packets.OrderEventPacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.OrderEventPacket:
        pass

    AlgorithmId: str
    Event: QuantConnect.Orders.OrderEvent
