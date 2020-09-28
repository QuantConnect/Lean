import typing
import System.Collections.Generic
import System
import QuantConnect.Statistics
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect
import datetime



class TradeDirection(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Direction of a trade
    
    enum TradeDirection, values: Long (0), Short (1)
    """
    value__: int
    Long: 'TradeDirection'
    Short: 'TradeDirection'


class TradeStatistics(System.object):
    """
    The QuantConnect.Statistics.TradeStatistics class represents a set of statistics calculated from a list of closed trades
    
    TradeStatistics(trades: IEnumerable[Trade])
    TradeStatistics()
    """
    @typing.overload
    def __init__(self, trades: typing.List[QuantConnect.Statistics.Trade]) -> QuantConnect.Statistics.TradeStatistics:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Statistics.TradeStatistics:
        pass

    def __init__(self, *args) -> QuantConnect.Statistics.TradeStatistics:
        pass

    AverageEndTradeDrawdown: float

    AverageLosingTradeDuration: datetime.timedelta

    AverageLoss: float

    AverageMAE: float

    AverageMFE: float

    AverageProfit: float

    AverageProfitLoss: float

    AverageTradeDuration: datetime.timedelta

    AverageWinningTradeDuration: datetime.timedelta

    EndDateTime: typing.Optional[datetime.datetime]

    LargestLoss: float

    LargestMAE: float

    LargestMFE: float

    LargestProfit: float

    LossRate: float

    MaxConsecutiveLosingTrades: int

    MaxConsecutiveWinningTrades: int

    MaximumClosedTradeDrawdown: float

    MaximumDrawdownDuration: datetime.timedelta

    MaximumEndTradeDrawdown: float

    MaximumIntraTradeDrawdown: float

    MedianLosingTradeDuration: datetime.timedelta

    MedianTradeDuration: datetime.timedelta

    MedianWinningTradeDuration: datetime.timedelta

    NumberOfLosingTrades: int

    NumberOfWinningTrades: int

    ProfitFactor: float

    ProfitLossDownsideDeviation: float

    ProfitLossRatio: float

    ProfitLossStandardDeviation: float

    ProfitToMaxDrawdownRatio: float

    SharpeRatio: float

    SortinoRatio: float

    StartDateTime: typing.Optional[datetime.datetime]

    TotalFees: float

    TotalLoss: float

    TotalNumberOfTrades: int

    TotalProfit: float

    TotalProfitLoss: float

    WinLossRatio: float

    WinRate: float
