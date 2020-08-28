from .__Portfolio_4 import *
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


class PortfolioTarget(System.object, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget that specifies a
                specified quantity of a security to be held by the algorithm
    
    PortfolioTarget(symbol: Symbol, quantity: Decimal)
    """
    @staticmethod
    @typing.overload
    def Percent(algorithm: QuantConnect.Interfaces.IAlgorithm, symbol: QuantConnect.Symbol, percent: float) -> QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget:
        pass

    @staticmethod
    @typing.overload
    def Percent(algorithm: QuantConnect.Interfaces.IAlgorithm, symbol: QuantConnect.Symbol, percent: float, returnDeltaQuantity: bool) -> QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget:
        pass

    def Percent(self, *args) -> QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget:
        pass

    def ToString(self) -> str:
        pass

    def __init__(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Algorithm.Framework.Portfolio.PortfolioTarget:
        pass

    Quantity: float

    Symbol: QuantConnect.Symbol



class PortfolioTargetCollection(System.object, System.Collections.IEnumerable, System.Collections.Generic.ICollection[KeyValuePair[Symbol, IPortfolioTarget]], System.Collections.Generic.ICollection[IPortfolioTarget], System.Collections.Generic.IDictionary[Symbol, IPortfolioTarget], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, IPortfolioTarget]], System.Collections.Generic.IEnumerable[IPortfolioTarget]):
    """
    Provides a collection for managing QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTargets for each symbol
    
    PortfolioTargetCollection()
    """
    @typing.overload
    def Add(self, target: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget) -> None:
        pass

    @typing.overload
    def Add(self, target: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass

    @typing.overload
    def Add(self, symbol: QuantConnect.Symbol, target: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    @typing.overload
    def AddRange(self, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass

    @typing.overload
    def AddRange(self, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass

    def AddRange(self, *args) -> None:
        pass

    def Clear(self) -> None:
        pass

    def ClearFulfilled(self, algorithm: QuantConnect.Interfaces.IAlgorithm) -> None:
        pass

    @typing.overload
    def Contains(self, target: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget) -> bool:
        pass

    @typing.overload
    def Contains(self, target: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> bool:
        pass

    def Contains(self, *args) -> bool:
        pass

    def ContainsKey(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    @typing.overload
    def CopyTo(self, array: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget], arrayIndex: int) -> None:
        pass

    @typing.overload
    def CopyTo(self, array: typing.List[System.Collections.Generic.KeyValuePair], arrayIndex: int) -> None:
        pass

    def CopyTo(self, *args) -> None:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]:
        pass

    def OrderByMarginImpact(self, algorithm: QuantConnect.Interfaces.IAlgorithm) -> typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]:
        pass

    @typing.overload
    def Remove(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    @typing.overload
    def Remove(self, target: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> bool:
        pass

    @typing.overload
    def Remove(self, target: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget) -> bool:
        pass

    def Remove(self, *args) -> bool:
        pass

    def TryGetValue(self, symbol: QuantConnect.Symbol, target: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget) -> bool:
        pass

    Count: int

    IsReadOnly: bool

    Keys: typing.List[QuantConnect.Symbol]

    Values: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]


    Item: indexer#


class ReturnsSymbolData(System.object):
    """
    Contains returns specific to a symbol required for optimization model
    
    ReturnsSymbolData(symbol: Symbol, lookback: int, period: int)
    """
    def Add(self, time: datetime.datetime, value: float) -> None:
        pass

    def Reset(self) -> None:
        pass

    def Update(self, time: datetime.datetime, value: float) -> bool:
        pass

    def __init__(self, symbol: QuantConnect.Symbol, lookback: int, period: int) -> QuantConnect.Algorithm.Framework.Portfolio.ReturnsSymbolData:
        pass

    Returns: System.Collections.Generic.Dictionary[datetime.datetime, float]



class ReturnsSymbolDataExtensions(System.object):
    """ Extension methods for QuantConnect.Algorithm.Framework.Portfolio.ReturnsSymbolData """
    @staticmethod
    def FormReturnsMatrix(symbolData: System.Collections.Generic.Dictionary[QuantConnect.Symbol, QuantConnect.Algorithm.Framework.Portfolio.ReturnsSymbolData], symbols: typing.List[QuantConnect.Symbol]) -> typing.List[typing.List[float]]:
        pass

    __all__: list


class SectorWeightingPortfolioConstructionModel(QuantConnect.Algorithm.Framework.Portfolio.EqualWeightingPortfolioConstructionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel that generates percent targets based on the
                QuantConnect.Data.Fundamental.CompanyReference.IndustryTemplateCode. 
                The target percent holdings of each sector is 1/S where S is the number of sectors and
                the target percent holdings of each security is 1/N where N is the number of securities of each sector.
                For insights of direction QuantConnect.Algorithm.Framework.Alphas.InsightDirection.Up, long targets are returned and for insights of direction
                QuantConnect.Algorithm.Framework.Alphas.InsightDirection.Down, short targets are returned.
                It will ignore QuantConnect.Algorithm.Framework.Alphas.Insight for symbols that have no QuantConnect.Data.Fundamental.CompanyReference.IndustryTemplateCode value.
    
    SectorWeightingPortfolioConstructionModel(rebalancingDateRules: IDateRule)
    SectorWeightingPortfolioConstructionModel(rebalancingFunc: Func[DateTime, Nullable[DateTime]])
    SectorWeightingPortfolioConstructionModel(rebalancingFunc: Func[DateTime, DateTime])
    SectorWeightingPortfolioConstructionModel(rebalance: PyObject)
    SectorWeightingPortfolioConstructionModel(timeSpan: TimeSpan)
    SectorWeightingPortfolioConstructionModel(resolution: Resolution)
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    @typing.overload
    def __init__(self, rebalancingDateRules: QuantConnect.Scheduling.IDateRule) -> QuantConnect.Algorithm.Framework.Portfolio.SectorWeightingPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], typing.Optional[datetime.datetime]]) -> QuantConnect.Algorithm.Framework.Portfolio.SectorWeightingPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalancingFunc: typing.Callable[[datetime.datetime], datetime.datetime]) -> QuantConnect.Algorithm.Framework.Portfolio.SectorWeightingPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, rebalance: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Portfolio.SectorWeightingPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, timeSpan: datetime.timedelta) -> QuantConnect.Algorithm.Framework.Portfolio.SectorWeightingPortfolioConstructionModel:
        pass

    @typing.overload
    def __init__(self, resolution: QuantConnect.Resolution) -> QuantConnect.Algorithm.Framework.Portfolio.SectorWeightingPortfolioConstructionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Portfolio.SectorWeightingPortfolioConstructionModel:
        pass

    PythonWrapper: QuantConnect.Algorithm.Framework.Portfolio.PortfolioConstructionModelPythonWrapper


class UnconstrainedMeanVariancePortfolioOptimizer(System.object, QuantConnect.Algorithm.Framework.Portfolio.IPortfolioOptimizer):
    """
    Provides an implementation of a portfolio optimizer with unconstrained mean variance.
    
    UnconstrainedMeanVariancePortfolioOptimizer()
    """
    def Optimize(self, historicalReturns: typing.List[typing.List[float]], expectedReturns: typing.List[float], covariance: typing.List[typing.List[float]]) -> typing.List[float]:
        pass

# no functions
# classes

class IPortfolioConstructionModel(QuantConnect.Algorithm.Framework.INotifiedSecurityChanges):
    """ Algorithm framework model that """
    def CreateTargets(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]) -> typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]:
        pass
