import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Python
import QuantConnect.Orders.Slippage
import QuantConnect.Orders.Fills
import QuantConnect.Orders.Fees
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Indicators
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect
import Python.Runtime
import datetime



class PythonConsolidator(System.object):
    """
    Provides a base class for python consolidators, necessary to use event handler.
    
    PythonConsolidator()
    """
    def OnDataConsolidated(self, consolidator: Python.Runtime.PyObject, data: QuantConnect.Data.IBaseData) -> None:
        pass

    DataConsolidated: BoundEvent


class PythonData(QuantConnect.Data.DynamicData, System.Dynamic.IDynamicMetaObjectProvider, QuantConnect.Data.IBaseData):
    """
    Dynamic data class for Python algorithms.
                Stores properties of python instances in DynamicData dictionary
    
    PythonData()
    PythonData(pythonData: PyObject)
    """
    def DefaultResolution(self) -> QuantConnect.Resolution:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    def IsSparseData(self) -> bool:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def RequiresMapping(self) -> bool:
        pass

    def SupportedResolutions(self) -> typing.List[QuantConnect.Resolution]:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Python.PythonData:
        pass

    @typing.overload
    def __init__(self, pythonData: Python.Runtime.PyObject) -> QuantConnect.Python.PythonData:
        pass

    def __init__(self, *args) -> QuantConnect.Python.PythonData:
        pass

    Item: indexer#


class PythonInitializer(System.object):
    """ Helper class for Python initialization """
    @staticmethod
    def AddPythonPaths(paths: typing.List[str]) -> None:
        pass

    @staticmethod
    def Initialize() -> None:
        pass

    @staticmethod
    def SetPythonPathEnvironmentVariable(extraDirectories: typing.List[str]) -> None:
        pass

    __all__: list


class PythonQuandl(QuantConnect.Data.Custom.Quandl, System.Dynamic.IDynamicMetaObjectProvider, QuantConnect.Data.IBaseData):
    """
    Dynamic data class for Python algorithms.
    
    PythonQuandl()
    PythonQuandl(valueColumnName: str)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Python.PythonQuandl:
        pass

    @typing.overload
    def __init__(self, valueColumnName: str) -> QuantConnect.Python.PythonQuandl:
        pass

    def __init__(self, *args) -> QuantConnect.Python.PythonQuandl:
        pass


class PythonSlice(QuantConnect.Data.Slice, System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, object], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, BaseData]]):
    """
    Provides a data structure for all of an algorithm's data at a single time step
    
    PythonSlice(slice: Slice)
    """
    def ContainsKey(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    @typing.overload
    def Get(self, type: Python.Runtime.PyObject, symbol: QuantConnect.Symbol) -> object:
        pass

    @typing.overload
    def Get(self, type: Python.Runtime.PyObject) -> Python.Runtime.PyObject:
        pass

    @typing.overload
    def Get(self) -> QuantConnect.Data.Market.DataDictionary[QuantConnect.Data.T]:
        pass

    @typing.overload
    def Get(self, type: type) -> object:
        pass

    @typing.overload
    def Get(self, symbol: QuantConnect.Symbol) -> QuantConnect.Data.T:
        pass

    def Get(self, *args) -> QuantConnect.Data.T:
        pass

    def TryGetValue(self, symbol: QuantConnect.Symbol, data: object) -> bool:
        pass

    def __init__(self, slice: QuantConnect.Data.Slice) -> QuantConnect.Python.PythonSlice:
        pass

    Count: int

    Keys: typing.List[QuantConnect.Symbol]

    Values: typing.List[QuantConnect.Data.BaseData]


    Item: indexer#


class PythonWrapper(System.object):
    """ Provides extension methods for managing python wrapper classes """
    @staticmethod
    def ValidateImplementationOf(model: Python.Runtime.PyObject) -> None:
        pass

    __all__: list


class SecurityInitializerPythonWrapper(System.object, QuantConnect.Securities.ISecurityInitializer):
    """
    Wraps a Python.Runtime.PyObject object that represents a type capable of initializing a new security
    
    SecurityInitializerPythonWrapper(model: PyObject)
    """
    def Initialize(self, security: QuantConnect.Securities.Security) -> None:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.SecurityInitializerPythonWrapper:
        pass


class SlippageModelPythonWrapper(System.object, QuantConnect.Orders.Slippage.ISlippageModel):
    """
    Wraps a Python.Runtime.PyObject object that represents a model that simulates market order slippage
    
    SlippageModelPythonWrapper(model: PyObject)
    """
    def GetSlippageApproximation(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> float:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.SlippageModelPythonWrapper:
        pass


class VolatilityModelPythonWrapper(QuantConnect.Securities.Volatility.BaseVolatilityModel, QuantConnect.Securities.IVolatilityModel):
    """
    Provides a volatility model that wraps a Python.Runtime.PyObject object that represents a model that computes the volatility of a security
    
    VolatilityModelPythonWrapper(model: PyObject)
    """
    def GetHistoryRequirements(self, security: QuantConnect.Securities.Security, utcTime: datetime.datetime) -> typing.List[QuantConnect.Data.HistoryRequest]:
        pass

    def SetSubscriptionDataConfigProvider(self, subscriptionDataConfigProvider: QuantConnect.Interfaces.ISubscriptionDataConfigProvider) -> None:
        pass

    def Update(self, security: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> None:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Python.VolatilityModelPythonWrapper:
        pass

    Volatility: float

    SubscriptionDataConfigProvider: QuantConnect.Interfaces.ISubscriptionDataConfigProvider
