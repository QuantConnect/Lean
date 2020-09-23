from .____init___1 import *
import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data
import QuantConnect.Algorithm.Framework.Alphas.Serialization
import QuantConnect.Algorithm.Framework.Alphas.Analysis
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import datetime

# no functions
# classes

class AlphaModel(System.object, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Provides a base class for alpha models.
    
    AlphaModel()
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    Name: str



class AlphaModelExtensions(System.object):
    """ Provides extension methods for alpha models """
    @staticmethod
    def GetModelName(model: QuantConnect.Algorithm.Framework.Alphas.IAlphaModel) -> str:
        pass

    __all__: list


class AlphaModelPythonWrapper(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Alphas.IAlphaModel that wraps a Python.Runtime.PyObject object
    
    AlphaModelPythonWrapper(model: PyObject)
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Alphas.AlphaModelPythonWrapper:
        pass

    Name: str



class BasePairsTradingAlphaModel(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    This alpha model is designed to accept every possible pair combination
                from securities selected by the universe selection model
                This model generates alternating long ratio/short ratio insights emitted as a group
    
    BasePairsTradingAlphaModel(lookback: int, resolution: Resolution, threshold: Decimal)
    """
    def HasPassedTest(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, asset1: QuantConnect.Symbol, asset2: QuantConnect.Symbol) -> bool:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    def __init__(self, lookback: int, resolution: QuantConnect.Resolution, threshold: float) -> QuantConnect.Algorithm.Framework.Alphas.BasePairsTradingAlphaModel:
        pass

    Securities: System.Collections.Generic.HashSet[QuantConnect.Securities.Security]



class CompositeAlphaModel(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Alphas.IAlphaModel that combines multiple alpha
                models into a single alpha model and properly sets each insights 'SourceModel' property.
    
    CompositeAlphaModel(*alphaModels: Array[IAlphaModel])
    CompositeAlphaModel(*alphaModels: Array[PyObject])
    CompositeAlphaModel(alphaModel: PyObject)
    """
    @typing.overload
    def AddAlpha(self, alphaModel: QuantConnect.Algorithm.Framework.Alphas.IAlphaModel) -> None:
        pass

    @typing.overload
    def AddAlpha(self, pyAlphaModel: Python.Runtime.PyObject) -> None:
        pass

    def AddAlpha(self, *args) -> None:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    @typing.overload
    def __init__(self, alphaModels: typing.List[QuantConnect.Algorithm.Framework.Alphas.IAlphaModel]) -> QuantConnect.Algorithm.Framework.Alphas.CompositeAlphaModel:
        pass

    @typing.overload
    def __init__(self, alphaModels: typing.List[Python.Runtime.PyObject]) -> QuantConnect.Algorithm.Framework.Alphas.CompositeAlphaModel:
        pass

    @typing.overload
    def __init__(self, alphaModel: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Alphas.CompositeAlphaModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Alphas.CompositeAlphaModel:
        pass


class ConstantAlphaModel(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Alphas.IAlphaModel that always returns the same insight for each security
    
    ConstantAlphaModel(type: InsightType, direction: InsightDirection, period: TimeSpan)
    ConstantAlphaModel(type: InsightType, direction: InsightDirection, period: TimeSpan, magnitude: Nullable[float], confidence: Nullable[float], weight: Nullable[float])
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    @typing.overload
    def __init__(self, type: QuantConnect.Algorithm.Framework.Alphas.InsightType, direction: QuantConnect.Algorithm.Framework.Alphas.InsightDirection, period: datetime.timedelta) -> QuantConnect.Algorithm.Framework.Alphas.ConstantAlphaModel:
        pass

    @typing.overload
    def __init__(self, type: QuantConnect.Algorithm.Framework.Alphas.InsightType, direction: QuantConnect.Algorithm.Framework.Alphas.InsightDirection, period: datetime.timedelta, magnitude: typing.Optional[float], confidence: typing.Optional[float], weight: typing.Optional[float]) -> QuantConnect.Algorithm.Framework.Alphas.ConstantAlphaModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Alphas.ConstantAlphaModel:
        pass


class EmaCrossAlphaModel(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Alpha model that uses an EMA cross to create insights
    
    EmaCrossAlphaModel(fastPeriod: int, slowPeriod: int, resolution: Resolution)
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    def __init__(self, fastPeriod: int, slowPeriod: int, resolution: QuantConnect.Resolution) -> QuantConnect.Algorithm.Framework.Alphas.EmaCrossAlphaModel:
        pass


class GeneratedInsightsCollection(System.object):
    """
    Defines a collection of insights that were generated at the same time step
    
    GeneratedInsightsCollection(dateTimeUtc: DateTime, insights: IEnumerable[Insight], clone: bool)
    """
    def __init__(self, dateTimeUtc: datetime.datetime, insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight], clone: bool) -> QuantConnect.Algorithm.Framework.Alphas.GeneratedInsightsCollection:
        pass

    DateTimeUtc: datetime.datetime

    Insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]



class HistoricalReturnsAlphaModel(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Alpha model that uses historical returns to create insights
    
    HistoricalReturnsAlphaModel(lookback: int, resolution: Resolution)
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    def __init__(self, lookback: int, resolution: QuantConnect.Resolution) -> QuantConnect.Algorithm.Framework.Alphas.HistoricalReturnsAlphaModel:
        pass


class IAlphaModel(QuantConnect.Algorithm.Framework.INotifiedSecurityChanges):
    """ Algorithm framework model that produces insights """
    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass


class IInsightManagerExtension:
    """
    Abstraction point to handle the various concerns from a common api.
                At the time of writing, these concerns are charting, scoring, perisistence and messaging.
    """
    def InitializeForRange(self, algorithmStartDate: datetime.datetime, algorithmEndDate: datetime.datetime, algorithmUtcTime: datetime.datetime) -> None:
        pass

    def OnInsightAnalysisCompleted(self, context: QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext) -> None:
        pass

    def OnInsightClosed(self, context: QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext) -> None:
        pass

    def OnInsightGenerated(self, context: QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext) -> None:
        pass

    def Step(self, frontierTimeUtc: datetime.datetime) -> None:
        pass
