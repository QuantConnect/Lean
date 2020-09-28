import typing
import QuantConnect.Data.UniverseSelection
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Execution
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import datetime

# no functions
# classes

class ExecutionModel(System.object, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Execution.IExecutionModel):
    """
    Provides a base class for execution models
    
    ExecutionModel()
    """
    def Execute(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass


class ExecutionModelPythonWrapper(QuantConnect.Algorithm.Framework.Execution.ExecutionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Execution.IExecutionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Execution.IExecutionModel that wraps a Python.Runtime.PyObject object
    
    ExecutionModelPythonWrapper(model: PyObject)
    """
    def Execute(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Execution.ExecutionModelPythonWrapper:
        pass


class IExecutionModel(QuantConnect.Algorithm.Framework.INotifiedSecurityChanges):
    """ Algorithm framework model that executes portfolio targets """
    def Execute(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass


class ImmediateExecutionModel(QuantConnect.Algorithm.Framework.Execution.ExecutionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Execution.IExecutionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Execution.IExecutionModel that immediately submits
                market orders to achieve the desired portfolio targets
    
    ImmediateExecutionModel()
    """
    def Execute(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass


class NullExecutionModel(QuantConnect.Algorithm.Framework.Execution.ExecutionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Execution.IExecutionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Execution.IExecutionModel that does nothing
    
    NullExecutionModel()
    """
    def Execute(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass


class StandardDeviationExecutionModel(QuantConnect.Algorithm.Framework.Execution.ExecutionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Execution.IExecutionModel):
    """
    Execution model that submits orders while the current market prices is at least the configured number of standard
                deviations away from the mean in the favorable direction (below/above for buy/sell respectively)
    
    StandardDeviationExecutionModel(period: int, deviations: Decimal, resolution: Resolution)
    """
    def Execute(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def __init__(self, period: int, deviations: float, resolution: QuantConnect.Resolution) -> QuantConnect.Algorithm.Framework.Execution.StandardDeviationExecutionModel:
        pass

    MaximumOrderValue: float


    SymbolData: type


class VolumeWeightedAveragePriceExecutionModel(QuantConnect.Algorithm.Framework.Execution.ExecutionModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Execution.IExecutionModel):
    """
    Execution model that submits orders while the current market price is more favorable that the current volume weighted average price.
    
    VolumeWeightedAveragePriceExecutionModel()
    """
    def Execute(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]) -> None:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    MaximumOrderQuantityPercentVolume: float


    SymbolData: type
