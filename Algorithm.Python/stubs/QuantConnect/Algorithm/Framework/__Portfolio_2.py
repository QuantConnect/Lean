from .__Portfolio_3 import *
import typing
import System.Collections.Generic
import System
import QuantConnect.Scheduling
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import datetime


class IPortfolioConstructionModel(QuantConnect.Algorithm.Framework.INotifiedSecurityChanges):
    """ Algorithm framework model that """
    def CreateTargets(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]) -> typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]:
        pass


class IPortfolioOptimizer:
    """ Interface for portfolio optimization algorithms """
    def Optimize(self, historicalReturns: typing.List[typing.List[float]], expectedReturns: typing.List[float], covariance: typing.List[typing.List[float]]) -> typing.List[float]:
        pass


class IPortfolioTarget:
    """
    Represents a portfolio target. This may be a percentage of total portfolio value
                or it may be a fixed number of shares.
    """
    Quantity: float

    Symbol: QuantConnect.Symbol



class MaximumSharpeRatioPortfolioOptimizer(System.object, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer):
    """
    Provides an implementation of a portfolio optimizer that maximizes the portfolio Sharpe Ratio.
                The interval of weights in optimization method can be changed based on the long-short algorithm.
                The default model uses flat risk free rate and weight for an individual security range from -1 to 1.
    
    MaximumSharpeRatioPortfolioOptimizer(lower: float, upper: float, riskFreeRate: float)
    """
    def Optimize(self, historicalReturns: typing.List[typing.List[float]], expectedReturns: typing.List[float], covariance: typing.List[typing.List[float]]) -> typing.List[float]:
        pass

    def __init__(self, lower: float, upper: float, riskFreeRate: float) -> QuantConnect.Algorithm.Framework.Portfolio.MaximumSharpeRatioPortfolioOptimizer:
        pass


class MeanVarianceOptimizationPortfolioConstructionModel(QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel):
    """
    Provides an implementation of Mean-Variance portfolio optimization based on modern portfolio theory.
                The interval of weights in optimization method can be changed based on the long-short algorithm.
                The default model uses the last three months daily price to calculate the optimal weight
                with the weight range from -1 to 1 and minimize the portfolio variance with a target return of 2%
    
    MeanVarianceOptimizationPortfolioConstructionModel(rebalancingDateRules: IDateRule, portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, targetReturn: float, optimizer: IPortfolioOptimizer)
    MeanVarianceOptimizationPortfolioConstructionModel(rebalanceResolution: Resolution, portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, targetReturn: float, optimizer: IPortfolioOptimizer)
    MeanVarianceOptimizationPortfolioConstructionModel(timeSpan: TimeSpan, portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, targetReturn: float, optimizer: IPortfolioOptimizer)
    MeanVarianceOptimizationPortfolioConstructionModel(rebalance: PyObject, portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, targetReturn: float, optimizer: IPortfolioOptimizer)
    MeanVarianceOptimizationPortfolioConstructionModel(rebalancingFunc: Func[DateTime, DateTime], portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, targetReturn: float, optimizer: IPortfolioOptimizer)
    MeanVarianceOptimizationPortfolioConstructionModel(rebalancingFunc: Func[DateTime, Nullable[DateTime]], portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, targetReturn: float, optimizer: IPortfolioOptimizer)
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    @typing.overload
    def __init__(self, rebalancingDateRules: QuantConnect.Scheduling.IDateRule, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, targetReturn: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.MeanVarianceOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalanceResolution: QuantConnect.Resolution, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, targetReturn: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.MeanVarianceOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, timeSpan: datetime.timedelta, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, targetReturn: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.MeanVarianceOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalance: Python.Runtime.PyObject, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, targetReturn: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.MeanVarianceOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], datetime.datetime], portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, targetReturn: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.MeanVarianceOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], typing.Optional[datetime.datetime]], portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, targetReturn: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.MeanVarianceOptimizationPortfolioConstructionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Portfolio.MeanVarianceOptimizationPortfolioConstructionModel:
        pass

    PythonWrapper: QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModelPythonWrapper


class MinimumVariancePortfolioOptimizer(System.object, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer):
    """
    Provides an implementation of a minimum variance portfolio optimizer that calculate the optimal weights
                with the weight range from -1 to 1 and minimize the portfolio variance with a target return of 2%
    
    MinimumVariancePortfolioOptimizer(lower: float, upper: float, targetReturn: float)
    """
    def Optimize(self, historicalReturns: typing.List[typing.List[float]], expectedReturns: typing.List[float], covariance: typing.List[typing.List[float]]) -> typing.List[float]:
        pass

    def __init__(self, lower: float, upper: float, targetReturn: float) -> QuantConnect.Algorithm.Framework.Portfolio.MinimumVariancePortfolioOptimizer:
        pass


class NullPortfolioConstructionModel(QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel that does nothing
    
    NullPortfolioConstructionModel()
    """
    def CreateTargets(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]) -> typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]:
        pass

    PythonWrapper: QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModelPythonWrapper


class PortfolioBias(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the bias of the portfolio (Short, Long/Short, Long)
    
    enum PortfolioBias, values: Long (1), LongShort (0), Short (-1)
    """
    value__: int
    Long: 'PortfolioBias'
    LongShort: 'PortfolioBias'
    Short: 'PortfolioBias'


class PortfolioConstructionModelPythonWrapper(QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel that wraps a Python.Runtime.PyObject object
    
    PortfolioConstructionModelPythonWrapper(model: PyObject)
    """
    def CreateTargets(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]) -> typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModelPythonWrapper:
        pass

    RebalanceOnInsightChanges: bool

    RebalanceOnSecurityChanges: bool

    PythonWrapper: QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModelPythonWrapper
