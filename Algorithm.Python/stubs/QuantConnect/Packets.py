from .__Packets_1 import *
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

# no functions
# classes

class Packet(System.object):
    """
    Base class for packet messaging system
    
    Packet(type: PacketType)
    """
    def __init__(self, type: QuantConnect.Packets.PacketType) -> QuantConnect.Packets.Packet:
        pass

    Channel: str

    Type: QuantConnect.Packets.PacketType



class AlgorithmNodePacket(QuantConnect.Packets.Packet):
    """
    Algorithm Node Packet is a work task for the Lean Engine
    
    AlgorithmNodePacket(type: PacketType)
    """
    def GetAlgorithmName(self) -> str:
        pass

    def __init__(self, type: QuantConnect.Packets.PacketType) -> QuantConnect.Packets.AlgorithmNodePacket:
        pass

    AlgorithmId: str

    RamAllocation: int

    Algorithm: typing.List[bytes]
    CompileId: str
    Controls: QuantConnect.Packets.Controls
    HistoryProvider: str
    Language: QuantConnect.Language
    Parameters: System.Collections.Generic.Dictionary[str, str]
    ProjectId: int
    Redelivered: bool
    RequestSource: str
    ServerType: QuantConnect.ServerType
    SessionId: str
    UserId: int
    UserPlan: QuantConnect.UserPlan
    UserToken: str
    Version: str


class AlgorithmStatusPacket(QuantConnect.Packets.Packet):
    """
    Algorithm status update information packet
    
    AlgorithmStatusPacket()
    AlgorithmStatusPacket(algorithmId: str, projectId: int, status: AlgorithmStatus, message: str)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.AlgorithmStatusPacket:
        pass

    @typing.overload
    def __init__(self, algorithmId: str, projectId: int, status: QuantConnect.AlgorithmStatus, message: str) -> QuantConnect.Packets.AlgorithmStatusPacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.AlgorithmStatusPacket:
        pass

    AlgorithmId: str
    ChannelStatus: str
    ChartSubscription: str
    Message: str
    ProjectId: int
    Status: QuantConnect.AlgorithmStatus

class LiveNodePacket(QuantConnect.Packets.AlgorithmNodePacket):
    """
    Live job task packet: container for any live specific job variables
    
    LiveNodePacket()
    """
    Brokerage: str
    BrokerageData: System.Collections.Generic.Dictionary[str, str]
    DataChannelProvider: str
    DataQueueHandler: str
    DeployId: str
    DisableAcknowledgement: bool

class AlphaNodePacket(QuantConnect.Packets.LiveNodePacket):
    """
    Alpha job packet
    
    AlphaNodePacket()
    """
    AlphaId: str



class AlphaResultPacket(QuantConnect.Packets.Packet):
    """
    Provides a packet type for transmitting alpha insights data
    
    AlphaResultPacket()
    AlphaResultPacket(algorithmId: str, userId: int, insights: List[Insight], orderEvents: List[OrderEvent], orders: List[Order])
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.AlphaResultPacket:
        pass

    @typing.overload
    def __init__(self, algorithmId: str, userId: int, insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight], orderEvents: typing.List[QuantConnect.Orders.OrderEvent], orders: typing.List[QuantConnect.Orders.Order]) -> QuantConnect.Packets.AlphaResultPacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.AlphaResultPacket:
        pass

    AlgorithmId: str

    AlphaId: str

    Insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]

    OrderEvents: typing.List[QuantConnect.Orders.OrderEvent]

    Orders: typing.List[QuantConnect.Orders.Order]

    UserId: int



class BacktestNodePacket(QuantConnect.Packets.AlgorithmNodePacket):
    """
    Algorithm backtest task information packet.
    
    BacktestNodePacket()
    BacktestNodePacket(userId: int, projectId: int, sessionId: str, algorithmData: Array[Byte], startingCapital: Decimal, name: str, userPlan: UserPlan)
    BacktestNodePacket(userId: int, projectId: int, sessionId: str, algorithmData: Array[Byte], name: str, userPlan: UserPlan, startingCapital: Nullable[CashAmount])
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.BacktestNodePacket:
        pass

    @typing.overload
    def __init__(self, userId: int, projectId: int, sessionId: str, algorithmData: typing.List[bytes], startingCapital: float, name: str, userPlan: QuantConnect.UserPlan) -> QuantConnect.Packets.BacktestNodePacket:
        pass

    @typing.overload
    def __init__(self, userId: int, projectId: int, sessionId: str, algorithmData: typing.List[bytes], name: str, userPlan: QuantConnect.UserPlan, startingCapital: typing.Optional[QuantConnect.Securities.CashAmount]) -> QuantConnect.Packets.BacktestNodePacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.BacktestNodePacket:
        pass

    IsDebugging: bool

    BacktestId: str
    Breakpoints: typing.List[QuantConnect.Packets.Breakpoint]
    CashAmount: typing.Optional[QuantConnect.Securities.CashAmount]
    Name: str
    PeriodFinish: typing.Optional[datetime.datetime]
    PeriodStart: typing.Optional[datetime.datetime]
    TradeableDates: int
    Watchlist: typing.List[str]


class BacktestResult(QuantConnect.Result):
    """
    Backtest results object class - result specific items from the packet.
    
    BacktestResult()
    BacktestResult(parameters: BacktestResultParameters)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Packets.BacktestResult:
        pass

    @typing.overload
    def __init__(self, parameters: QuantConnect.Packets.BacktestResultParameters) -> QuantConnect.Packets.BacktestResult:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.BacktestResult:
        pass

    RollingWindow: System.Collections.Generic.Dictionary[str, QuantConnect.Statistics.AlgorithmPerformance]
    TotalPerformance: QuantConnect.Statistics.AlgorithmPerformance

class BacktestResultPacket(QuantConnect.Packets.Packet):
    """
    Backtest result packet: send backtest information to GUI for user consumption.
    
    BacktestResultPacket()
    BacktestResultPacket(json: str)
    BacktestResultPacket(job: BacktestNodePacket, results: BacktestResult, endDate: DateTime, startDate: DateTime, progress: Decimal)
    """
    @staticmethod
    def CreateEmpty(job: QuantConnect.Packets.BacktestNodePacket) -> QuantConnect.Packets.BacktestResultPacket:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Packets.BacktestResultPacket:
        pass

    @typing.overload
    def __init__(self, json: str) -> QuantConnect.Packets.BacktestResultPacket:
        pass

    @typing.overload
    def __init__(self, job: QuantConnect.Packets.BacktestNodePacket, results: QuantConnect.Packets.BacktestResult, endDate: datetime.datetime, startDate: datetime.datetime, progress: float) -> QuantConnect.Packets.BacktestResultPacket:
        pass

    def __init__(self, *args) -> QuantConnect.Packets.BacktestResultPacket:
        pass

    BacktestId: str
    CompileId: str
    DateFinished: datetime.datetime
    DateRequested: datetime.datetime
    Name: str
    PeriodFinish: datetime.datetime
    PeriodStart: datetime.datetime
    ProcessingTime: float
    Progress: float
    ProjectId: int
    Results: QuantConnect.Packets.BacktestResult
    SessionId: str
    TradeableDates: int
    UserId: int

class BaseResultParameters(System.object):
    """
    Base parameters used by QuantConnect.Packets.LiveResultParameters and QuantConnect.Packets.BacktestResultParameters
    
    BaseResultParameters()
    """
    AlphaRuntimeStatistics: QuantConnect.AlphaRuntimeStatistics

    Charts: System.Collections.Generic.IDictionary[str, QuantConnect.Chart]

    OrderEvents: typing.List[QuantConnect.Orders.OrderEvent]

    Orders: System.Collections.Generic.IDictionary[int, QuantConnect.Orders.Order]

    ProfitLoss: System.Collections.Generic.IDictionary[datetime.datetime, float]

    RuntimeStatistics: System.Collections.Generic.IDictionary[str, str]

    Statistics: System.Collections.Generic.IDictionary[str, str]



class BacktestResultParameters(QuantConnect.Packets.BaseResultParameters):
    """
    Defines the parameters for QuantConnect.Packets.BacktestResult
    
    BacktestResultParameters(charts: IDictionary[str, Chart], orders: IDictionary[int, Order], profitLoss: IDictionary[DateTime, Decimal], statistics: IDictionary[str, str], runtimeStatistics: IDictionary[str, str], rollingWindow: Dictionary[str, AlgorithmPerformance], orderEvents: List[OrderEvent], totalPerformance: AlgorithmPerformance, alphaRuntimeStatistics: AlphaRuntimeStatistics)
    """
    def __init__(self, charts: System.Collections.Generic.IDictionary[str, QuantConnect.Chart], orders: System.Collections.Generic.IDictionary[int, QuantConnect.Orders.Order], profitLoss: System.Collections.Generic.IDictionary[datetime.datetime, float], statistics: System.Collections.Generic.IDictionary[str, str], runtimeStatistics: System.Collections.Generic.IDictionary[str, str], rollingWindow: System.Collections.Generic.Dictionary[str, QuantConnect.Statistics.AlgorithmPerformance], orderEvents: typing.List[QuantConnect.Orders.OrderEvent], totalPerformance: QuantConnect.Statistics.AlgorithmPerformance, alphaRuntimeStatistics: QuantConnect.AlphaRuntimeStatistics) -> QuantConnect.Packets.BacktestResultParameters:
        pass

    RollingWindow: System.Collections.Generic.Dictionary[str, QuantConnect.Statistics.AlgorithmPerformance]

    TotalPerformance: QuantConnect.Statistics.AlgorithmPerformance



class Breakpoint(System.object):
    """
    A debugging breakpoint
    
    Breakpoint()
    """
    FileName: str

    LineNumber: int



class HistoryResult(System.object):
    """
    Provides a container for results from history requests. This contains
                the file path relative to the /Data folder where the data can be written
    """
    def __init__(self, *args): #cannot find CLR constructor
        pass

    Type: QuantConnect.Packets.HistoryResultType



class CompletedHistoryResult(QuantConnect.Packets.HistoryResult):
    """
    Specifies the completed message from a history result
    
    CompletedHistoryResult()
    """
