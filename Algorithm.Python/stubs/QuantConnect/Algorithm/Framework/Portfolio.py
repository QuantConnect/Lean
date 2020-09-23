from .__Portfolio_1 import *
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

# no functions
# classes

class PortfolioConstructionModel(System.object, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel):
    """
    Provides a base class for portfolio construction models
    
    PortfolioConstructionModel(rebalancingFunc: Func[DateTime, Nullable[DateTime]])
    PortfolioConstructionModel(rebalancingFunc: Func[DateTime, DateTime])
    """
    def CreateTargets(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]) -> typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], typing.Optional[datetime.datetime]]) -> QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], datetime.datetime]) -> QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModel:
        pass

    RebalanceOnInsightChanges: bool

    RebalanceOnSecurityChanges: bool

    PythonWrapper: QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModelPythonWrapper


class AccumulativeInsightPortfolioConstructionModel(QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel that allocates percent of account
                to each insight, defaulting to 3%.
                For insights of direction QuantConnect.Algorithm.Framework.Alphas.InsightDirection.Up, long targets are returned and
                for insights of direction QuantConnect.Algorithm.Framework.Alphas.InsightDirection.Down, short targets are returned.
                By default, no rebalancing shall be done.
                Rules:
                   1. On active Up insight, increase position size by percent
                   2. On active Down insight, decrease position size by percent
                   3. On active Flat insight, move by percent towards 0
                   4. On expired insight, and no other active insight, emits a 0 target'''
    
    AccumulativeInsightPortfolioConstructionModel(rebalancingDateRules: IDateRule, portfolioBias: PortfolioBias, percent: float)
    AccumulativeInsightPortfolioConstructionModel(rebalancingFunc: Func[DateTime, Nullable[DateTime]], portfolioBias: PortfolioBias, percent: float)
    AccumulativeInsightPortfolioConstructionModel(rebalancingFunc: Func[DateTime, DateTime], portfolioBias: PortfolioBias, percent: float)
    AccumulativeInsightPortfolioConstructionModel(rebalance: PyObject, portfolioBias: PortfolioBias, percent: float)
    AccumulativeInsightPortfolioConstructionModel(timeSpan: TimeSpan, portfolioBias: PortfolioBias, percent: float)
    AccumulativeInsightPortfolioConstructionModel(resolution: Resolution, portfolioBias: PortfolioBias, percent: float)
    """
    @typing.overload
    def __init__(self, rebalancingDateRules: QuantConnect.Scheduling.IDateRule, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, percent: float) -> QuantConnect.Algorithm.Framework.Portfolio.AccumulativeInsightPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], typing.Optional[datetime.datetime]], portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, percent: float) -> QuantConnect.Algorithm.Framework.Portfolio.AccumulativeInsightPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], datetime.datetime], portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, percent: float) -> QuantConnect.Algorithm.Framework.Portfolio.AccumulativeInsightPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalance: Python.Runtime.PyObject, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, percent: float) -> QuantConnect.Algorithm.Framework.Portfolio.AccumulativeInsightPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, timeSpan: datetime.timedelta, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, percent: float) -> QuantConnect.Algorithm.Framework.Portfolio.AccumulativeInsightPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, resolution: QuantConnect.Resolution, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, percent: float) -> QuantConnect.Algorithm.Framework.Portfolio.AccumulativeInsightPortfolioConstructionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Portfolio.AccumulativeInsightPortfolioConstructionModel:
        pass

    PythonWrapper: QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModelPythonWrapper


class BlackLittermanOptimizationPortfolioConstructionModel(QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel):
    """
    Provides an implementation of Black-Litterman portfolio optimization. The model adjusts equilibrium market
                returns by incorporating views from multiple alpha models and therefore to get the optimal risky portfolio
                reflecting those views. If insights of all alpha models have None magnitude or there are linearly dependent
                vectors in link matrix of views, the expected return would be the implied excess equilibrium return.
                The interval of weights in optimization method can be changed based on the long-short algorithm.
                The default model uses the 0.0025 as weight-on-views scalar parameter tau. The optimization method
                maximizes the Sharpe ratio with the weight range from -1 to 1.
    
    BlackLittermanOptimizationPortfolioConstructionModel(timeSpan: TimeSpan, portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: IPortfolioOptimizer)
    BlackLittermanOptimizationPortfolioConstructionModel(rebalanceResolution: Resolution, portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: IPortfolioOptimizer)
    BlackLittermanOptimizationPortfolioConstructionModel(rebalancingFunc: Func[DateTime, DateTime], portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: IPortfolioOptimizer)
    BlackLittermanOptimizationPortfolioConstructionModel(rebalancingDateRules: IDateRule, portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: IPortfolioOptimizer)
    BlackLittermanOptimizationPortfolioConstructionModel(rebalance: PyObject, portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: IPortfolioOptimizer)
    BlackLittermanOptimizationPortfolioConstructionModel(rebalancingFunc: Func[DateTime, Nullable[DateTime]], portfolioBias: PortfolioBias, lookback: int, period: int, resolution: Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: IPortfolioOptimizer)
    """
    def GetEquilibriumReturns(self, returns: typing.List[typing.List[float]], Σ: typing.List[typing.List[float]]) -> typing.List[float]:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    @typing.overload
    def __init__(self, timeSpan: datetime.timedelta, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.BlackLittermanOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalanceResolution: QuantConnect.Resolution, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.BlackLittermanOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], datetime.datetime], portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.BlackLittermanOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingDateRules: QuantConnect.Scheduling.IDateRule, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.BlackLittermanOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalance: Python.Runtime.PyObject, portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.BlackLittermanOptimizationPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], typing.Optional[datetime.datetime]], portfolioBias: QuantConnect.Algorithm.Framework.Portfolio.PortfolioBias, lookback: int, period: int, resolution: QuantConnect.Resolution, riskFreeRate: float, delta: float, tau: float, optimizer: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer) -> QuantConnect.Algorithm.Framework.Portfolio.BlackLittermanOptimizationPortfolioConstructionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Portfolio.BlackLittermanOptimizationPortfolioConstructionModel:
        pass

    PythonWrapper: QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModelPythonWrapper
