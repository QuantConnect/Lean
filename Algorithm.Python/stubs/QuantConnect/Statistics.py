from .__Statistics_1 import *
import typing
import System.Collections.Generic
import System
import QuantConnect.Statistics
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect
import datetime

# no functions
# classes

class AlgorithmPerformance(System.object):
    """
    The QuantConnect.Statistics.AlgorithmPerformance class is a wrapper for QuantConnect.Statistics.AlgorithmPerformance.TradeStatistics and QuantConnect.Statistics.AlgorithmPerformance.PortfolioStatistics
    
    AlgorithmPerformance(trades: List[Trade], profitLoss: SortedDictionary[DateTime, Decimal], equity: SortedDictionary[DateTime, Decimal], listPerformance: List[float], listBenchmark: List[float], startingCapital: Decimal)
    AlgorithmPerformance()
    """
    @typing.overload
    def __init__(self, trades: typing.List[QuantConnect.Statistics.Trade], profitLoss: System.Collections.Generic.SortedDictionary[datetime.datetime, float], equity: System.Collections.Generic.SortedDictionary[datetime.datetime, float], listPerformance: typing.List[float], listBenchmark: typing.List[float], startingCapital: float) -> QuantConnect.Statistics.AlgorithmPerformance:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Statistics.AlgorithmPerformance:
        pass

    def __init__(self, *args) -> QuantConnect.Statistics.AlgorithmPerformance:
        pass

    ClosedTrades: typing.List[QuantConnect.Statistics.Trade]

    PortfolioStatistics: QuantConnect.Statistics.PortfolioStatistics

    TradeStatistics: QuantConnect.Statistics.TradeStatistics



class FillGroupingMethod(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    The method used to group order fills into trades
    
    enum FillGroupingMethod, values: FillToFill (0), FlatToFlat (1), FlatToReduced (2)
    """
    value__: int
    FillToFill: 'FillGroupingMethod'
    FlatToFlat: 'FillGroupingMethod'
    FlatToReduced: 'FillGroupingMethod'


class FillMatchingMethod(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    The method used to match offsetting order fills
    
    enum FillMatchingMethod, values: FIFO (0), LIFO (1)
    """
    value__: int
    FIFO: 'FillMatchingMethod'
    LIFO: 'FillMatchingMethod'


class FitnessScoreManager(System.object):
    """
    Implements a fitness score calculator needed to account for strategy volatility,
                returns, drawdown, and factor in the turnover to ensure the algorithm engagement
                is statistically significant
    
    FitnessScoreManager()
    """
    def Initialize(self, algorithm: QuantConnect.Interfaces.IAlgorithm) -> None:
        pass

    @staticmethod
    def SigmoidalScale(valueToScale: float) -> float:
        pass

    def UpdateScores(self) -> None:
        pass

    FitnessScore: float

    PortfolioTurnover: float

    ReturnOverMaxDrawdown: float

    SortinoRatio: float



class KellyCriterionManager(System.object):
    """
    Class in charge of calculating the Kelly Criterion values.
                Will use the sample values of the last year.
    
    KellyCriterionManager()
    """
    def AddNewValue(self, newValue: float, time: datetime.datetime) -> None:
        pass

    def UpdateScores(self) -> None:
        pass

    KellyCriterionEstimate: float

    KellyCriterionProbabilityValue: float



class PortfolioStatistics(System.object):
    """
    The QuantConnect.Statistics.PortfolioStatistics class represents a set of statistics calculated from equity and benchmark samples
    
    PortfolioStatistics(profitLoss: SortedDictionary[DateTime, Decimal], equity: SortedDictionary[DateTime, Decimal], listPerformance: List[float], listBenchmark: List[float], startingCapital: Decimal, tradingDaysPerYear: int)
    PortfolioStatistics()
    """
    @staticmethod
    def GetRiskFreeRate() -> float:
        pass

    @typing.overload
    def __init__(self, profitLoss: System.Collections.Generic.SortedDictionary[datetime.datetime, float], equity: System.Collections.Generic.SortedDictionary[datetime.datetime, float], listPerformance: typing.List[float], listBenchmark: typing.List[float], startingCapital: float, tradingDaysPerYear: int) -> QuantConnect.Statistics.PortfolioStatistics:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Statistics.PortfolioStatistics:
        pass

    def __init__(self, *args) -> QuantConnect.Statistics.PortfolioStatistics:
        pass

    Alpha: float

    AnnualStandardDeviation: float

    AnnualVariance: float

    AverageLossRate: float

    AverageWinRate: float

    Beta: float

    CompoundingAnnualReturn: float

    Drawdown: float

    Expectancy: float

    InformationRatio: float

    LossRate: float

    ProbabilisticSharpeRatio: float

    ProfitLossRatio: float

    SharpeRatio: float

    TotalNetProfit: float

    TrackingError: float

    TreynorRatio: float

    WinRate: float



class Statistics(System.object):
    """
    Calculate all the statistics required from the backtest, based on the equity curve and the profit loss statement.
    
    Statistics()
    """
    @staticmethod
    def Alpha(algoPerformance: typing.List[float], benchmarkPerformance: typing.List[float], riskFreeRate: float) -> float:
        pass

    @staticmethod
    def AnnualPerformance(performance: typing.List[float], tradingDaysPerYear: float) -> float:
        pass

    @staticmethod
    def AnnualStandardDeviation(performance: typing.List[float], tradingDaysPerYear: float) -> float:
        pass

    @staticmethod
    def AnnualVariance(performance: typing.List[float], tradingDaysPerYear: float) -> float:
        pass

    @staticmethod
    def Beta(algoPerformance: typing.List[float], benchmarkPerformance: typing.List[float]) -> float:
        pass

    @staticmethod
    def CompoundingAnnualPerformance(startingCapital: float, finalCapital: float, years: float) -> float:
        pass

    @staticmethod
    def DrawdownPercent(equityOverTime: System.Collections.Generic.SortedDictionary[datetime.datetime, float], rounding: int) -> float:
        pass

    @staticmethod
    def DrawdownValue(equityOverTime: System.Collections.Generic.SortedDictionary[datetime.datetime, float], rounding: int) -> float:
        pass

    @staticmethod
    def Generate(pointsEquity: typing.List[QuantConnect.ChartPoint], profitLoss: System.Collections.Generic.SortedDictionary[datetime.datetime, float], pointsPerformance: typing.List[QuantConnect.ChartPoint], unsortedBenchmark: System.Collections.Generic.Dictionary[datetime.datetime, float], startingCash: float, totalFees: float, totalTrades: float, tradingDaysPerYear: float) -> System.Collections.Generic.Dictionary[str, str]:
        pass

    @staticmethod
    def InformationRatio(algoPerformance: typing.List[float], benchmarkPerformance: typing.List[float]) -> float:
        pass

    @staticmethod
    def ObservedSharpeRatio(listPerformance: typing.List[float]) -> float:
        pass

    @staticmethod
    def ProbabilisticSharpeRatio(listPerformance: typing.List[float], benchmarkSharpeRatio: float) -> float:
        pass

    @staticmethod
    def ProfitLossRatio(averageWin: float, averageLoss: float) -> float:
        pass

    @staticmethod
    def SharpeRatio(algoPerformance: typing.List[float], riskFreeRate: float) -> float:
        pass

    @staticmethod
    def TrackingError(algoPerformance: typing.List[float], benchmarkPerformance: typing.List[float], tradingDaysPerYear: float) -> float:
        pass

    @staticmethod
    def TreynorRatio(algoPerformance: typing.List[float], benchmarkPerformance: typing.List[float], riskFreeRate: float) -> float:
        pass


class StatisticsBuilder(System.object):
    """ The QuantConnect.Statistics.StatisticsBuilder class creates summary and rolling statistics from trades, equity and benchmark points """
    @staticmethod
    def Generate(trades: typing.List[QuantConnect.Statistics.Trade], profitLoss: System.Collections.Generic.SortedDictionary[datetime.datetime, float], pointsEquity: typing.List[QuantConnect.ChartPoint], pointsPerformance: typing.List[QuantConnect.ChartPoint], pointsBenchmark: typing.List[QuantConnect.ChartPoint], startingCapital: float, totalFees: float, totalTransactions: int) -> QuantConnect.Statistics.StatisticsResults:
        pass

    __all__: list


class StatisticsResults(System.object):
    """
    The QuantConnect.Statistics.StatisticsResults class represents total and rolling statistics for an algorithm
    
    StatisticsResults(totalPerformance: AlgorithmPerformance, rollingPerformances: Dictionary[str, AlgorithmPerformance], summary: Dictionary[str, str])
    StatisticsResults()
    """
    @typing.overload
    def __init__(self, totalPerformance: QuantConnect.Statistics.AlgorithmPerformance, rollingPerformances: System.Collections.Generic.Dictionary[str, QuantConnect.Statistics.AlgorithmPerformance], summary: System.Collections.Generic.Dictionary[str, str]) -> QuantConnect.Statistics.StatisticsResults:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Statistics.StatisticsResults:
        pass

    def __init__(self, *args) -> QuantConnect.Statistics.StatisticsResults:
        pass

    RollingPerformances: System.Collections.Generic.Dictionary[str, QuantConnect.Statistics.AlgorithmPerformance]

    Summary: System.Collections.Generic.Dictionary[str, str]

    TotalPerformance: QuantConnect.Statistics.AlgorithmPerformance



class Trade(System.object):
    """
    Represents a closed trade
    
    Trade()
    """
    Direction: QuantConnect.Statistics.TradeDirection

    Duration: datetime.timedelta

    EndTradeDrawdown: float

    EntryPrice: float

    EntryTime: datetime.datetime

    ExitPrice: float

    ExitTime: datetime.datetime

    MAE: float

    MFE: float

    ProfitLoss: float

    Quantity: float

    Symbol: QuantConnect.Symbol

    TotalFees: float



class TradeBuilder(System.object, QuantConnect.Interfaces.ITradeBuilder):
    """
    The QuantConnect.Statistics.TradeBuilder class generates trades from executions and market price updates
    
    TradeBuilder(groupingMethod: FillGroupingMethod, matchingMethod: FillMatchingMethod)
    """
    def HasOpenPosition(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def ProcessFill(self, fill: QuantConnect.Orders.OrderEvent, conversionRate: float, feeInAccountCurrency: float, multiplier: float) -> None:
        pass

    def SetLiveMode(self, live: bool) -> None:
        pass

    def SetMarketPrice(self, symbol: QuantConnect.Symbol, price: float) -> None:
        pass

    def __init__(self, groupingMethod: QuantConnect.Statistics.FillGroupingMethod, matchingMethod: QuantConnect.Statistics.FillMatchingMethod) -> QuantConnect.Statistics.TradeBuilder:
        pass

    ClosedTrades: typing.List[QuantConnect.Statistics.Trade]
