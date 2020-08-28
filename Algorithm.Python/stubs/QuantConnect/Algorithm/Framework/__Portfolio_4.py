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
