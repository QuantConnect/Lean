from .____init___6 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Indicators
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import datetime


class IndicatorExtensions(System.object):
    """ Provides extension methods for Indicator """
    @staticmethod
    @typing.overload
    def EMA(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], period: int, smoothingFactor: typing.Optional[float], waitForFirstToReady: bool) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    @staticmethod
    @typing.overload
    def EMA(left: Python.Runtime.PyObject, period: int, smoothingFactor: typing.Optional[float], waitForFirstToReady: bool) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    def EMA(self, *args) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    @staticmethod
    @typing.overload
    def MAX(left: QuantConnect.Indicators.IIndicator, period: int, waitForFirstToReady: bool) -> QuantConnect.Indicators.Maximum:
        pass

    @staticmethod
    @typing.overload
    def MAX(left: Python.Runtime.PyObject, period: int, waitForFirstToReady: bool) -> QuantConnect.Indicators.Maximum:
        pass

    def MAX(self, *args) -> QuantConnect.Indicators.Maximum:
        pass

    @staticmethod
    @typing.overload
    def MIN(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], period: int, waitForFirstToReady: bool) -> QuantConnect.Indicators.Minimum:
        pass

    @staticmethod
    @typing.overload
    def MIN(left: Python.Runtime.PyObject, period: int, waitForFirstToReady: bool) -> QuantConnect.Indicators.Minimum:
        pass

    def MIN(self, *args) -> QuantConnect.Indicators.Minimum:
        pass

    @staticmethod
    @typing.overload
    def Minus(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], constant: float) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Minus(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T]) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Minus(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], name: str) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Minus(left: Python.Runtime.PyObject, constant: float) -> object:
        pass

    @staticmethod
    @typing.overload
    def Minus(left: Python.Runtime.PyObject, right: Python.Runtime.PyObject, name: str) -> object:
        pass

    def Minus(self, *args) -> object:
        pass

    @staticmethod
    @typing.overload
    def Of(second: QuantConnect.Indicators.T, first: QuantConnect.Indicators.IIndicator, waitForFirstToReady: bool) -> QuantConnect.Indicators.T:
        pass

    @staticmethod
    @typing.overload
    def Of(second: Python.Runtime.PyObject, first: Python.Runtime.PyObject, waitForFirstToReady: bool) -> object:
        pass

    def Of(self, *args) -> object:
        pass

    @staticmethod
    @typing.overload
    def Over(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], constant: float) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Over(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T]) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Over(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], name: str) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Over(left: Python.Runtime.PyObject, constant: float) -> object:
        pass

    @staticmethod
    @typing.overload
    def Over(left: Python.Runtime.PyObject, right: Python.Runtime.PyObject, name: str) -> object:
        pass

    def Over(self, *args) -> object:
        pass

    @staticmethod
    @typing.overload
    def Plus(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], constant: float) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Plus(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T]) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Plus(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], name: str) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Plus(left: Python.Runtime.PyObject, constant: float) -> object:
        pass

    @staticmethod
    @typing.overload
    def Plus(left: Python.Runtime.PyObject, right: Python.Runtime.PyObject, name: str) -> object:
        pass

    def Plus(self, *args) -> object:
        pass

    @staticmethod
    @typing.overload
    def SMA(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], period: int, waitForFirstToReady: bool) -> QuantConnect.Indicators.SimpleMovingAverage:
        pass

    @staticmethod
    @typing.overload
    def SMA(left: Python.Runtime.PyObject, period: int, waitForFirstToReady: bool) -> QuantConnect.Indicators.SimpleMovingAverage:
        pass

    def SMA(self, *args) -> QuantConnect.Indicators.SimpleMovingAverage:
        pass

    @staticmethod
    @typing.overload
    def Times(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], constant: float) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Times(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T]) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Times(left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], name: str) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.T]:
        pass

    @staticmethod
    @typing.overload
    def Times(left: Python.Runtime.PyObject, constant: float) -> object:
        pass

    @staticmethod
    @typing.overload
    def Times(left: Python.Runtime.PyObject, right: Python.Runtime.PyObject, name: str) -> object:
        pass

    def Times(self, *args) -> object:
        pass

    @staticmethod
    def Update(indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint], time: datetime.datetime, value: float) -> bool:
        pass

    @staticmethod
    @typing.overload
    def WeightedBy(value: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], weight: QuantConnect.Indicators.TWeight, period: int) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.IndicatorDataPoint]:
        pass

    @staticmethod
    @typing.overload
    def WeightedBy(value: Python.Runtime.PyObject, weight: Python.Runtime.PyObject, period: int) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.IndicatorDataPoint]:
        pass

    def WeightedBy(self, *args) -> QuantConnect.Indicators.CompositeIndicator[QuantConnect.Indicators.IndicatorDataPoint]:
        pass

    __all__: list


class IndicatorResult(System.object):
    """
    Represents the result of an indicator's calculations
    
    IndicatorResult(value: Decimal, status: IndicatorStatus)
    """
    def __init__(self, value: float, status: QuantConnect.Indicators.IndicatorStatus) -> QuantConnect.Indicators.IndicatorResult:
        pass

    Status: QuantConnect.Indicators.IndicatorStatus

    Value: float



class IndicatorStatus(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    The possible states returned by QuantConnect.Indicators.IndicatorBase
    
    enum IndicatorStatus, values: InvalidInput (1), MathError (2), Success (0), ValueNotReady (3)
    """
    value__: int
    InvalidInput: 'IndicatorStatus'
    MathError: 'IndicatorStatus'
    Success: 'IndicatorStatus'
    ValueNotReady: 'IndicatorStatus'


class IndicatorUpdatedHandler(System.MulticastDelegate, System.Runtime.Serialization.ISerializable, System.ICloneable):
    """
    Event handler type for the IndicatorBase.Updated event
    
    IndicatorUpdatedHandler(object: object, method: IntPtr)
    """
    def BeginInvoke(self, sender: object, updated: QuantConnect.Indicators.IndicatorDataPoint, callback: System.AsyncCallback, object: object) -> System.IAsyncResult:
        pass

    def EndInvoke(self, result: System.IAsyncResult) -> None:
        pass

    def Invoke(self, sender: object, updated: QuantConnect.Indicators.IndicatorDataPoint) -> None:
        pass

    def __init__(self, object: object, method: System.IntPtr) -> QuantConnect.Indicators.IndicatorUpdatedHandler:
        pass


class IntradayVwap(QuantConnect.Indicators.IndicatorBase[BaseData], System.IComparable, QuantConnect.Indicators.IIndicator[BaseData], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[BaseData]]):
    """
    Defines the canonical intraday VWAP indicator
    
    IntradayVwap(name: str)
    """
    def __init__(self, name: str) -> QuantConnect.Indicators.IntradayVwap:
        pass

    IsReady: bool



class IReadOnlyWindow(System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[T]):
    # no doc
    Count: int

    IsReady: bool

    MostRecentlyRemoved: QuantConnect.Indicators.T

    Samples: float

    Size: int


    Item: indexer#
