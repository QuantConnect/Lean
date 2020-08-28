from .__Consolidators_1 import *
import typing
import System
import QuantConnect.Data.Market
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import datetime

# functions

def FilteredIdentityDataConsolidator(*args, **kwargs): # real signature unknown
    """ Provides factory methods for creating instances of QuantConnect.Data.Consolidators.FilteredIdentityDataConsolidator """
    pass

def RenkoConsolidator(barSize, type): # real signature unknown; restored from __doc__
    """
    This consolidator can transform a stream of QuantConnect.Data.BaseData instances into a stream of QuantConnect.Data.Market.RenkoBar
    
    RenkoConsolidator(barSize: Decimal, type: RenkoType)
    RenkoConsolidator(barSize: Decimal, evenBars: bool)
    RenkoConsolidator(barSize: Decimal, selector: Func[IBaseData, Decimal], volumeSelector: Func[IBaseData, Decimal], evenBars: bool)
    RenkoConsolidator(barSize: Decimal, selector: PyObject, volumeSelector: PyObject, evenBars: bool)
    """
    pass

# classes

class BaseDataConsolidator(QuantConnect.Data.Consolidators.TradeBarConsolidatorBase[BaseData], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    Type capable of consolidating trade bars from any base data instance
    
    BaseDataConsolidator(period: TimeSpan)
    BaseDataConsolidator(maxCount: int)
    BaseDataConsolidator(maxCount: int, period: TimeSpan)
    BaseDataConsolidator(func: Func[DateTime, CalendarInfo])
    BaseDataConsolidator(pyfuncobj: PyObject)
    """
    @staticmethod
    def FromResolution(resolution: QuantConnect.Resolution) -> QuantConnect.Data.Consolidators.BaseDataConsolidator:
        pass

    @typing.overload
    def __init__(self, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.BaseDataConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int) -> QuantConnect.Data.Consolidators.BaseDataConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.BaseDataConsolidator:
        pass

    @typing.overload
    def __init__(self, func: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo]) -> QuantConnect.Data.Consolidators.BaseDataConsolidator:
        pass

    @typing.overload
    def __init__(self, pyfuncobj: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.BaseDataConsolidator:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Consolidators.BaseDataConsolidator:
        pass



class Calendar(System.object):
    """ Helper class that provides System.Func used to define consolidation calendar """
    __all__: list


class CalendarInfo(System.object):
    """ CalendarInfo(start: DateTime, period: TimeSpan) """
    def __init__(self, start: datetime.datetime, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.CalendarInfo:
        pass

    Period: datetime.timedelta
    Start: datetime.datetime

class CalendarType(System.object):
    # no doc
    __all__: list


class DataConsolidatedHandler(System.MulticastDelegate, System.Runtime.Serialization.ISerializable, System.ICloneable):
    """
    Event handler type for the IDataConsolidator.DataConsolidated event
    
    DataConsolidatedHandler(object: object, method: IntPtr)
    """
    def BeginInvoke(self, sender: object, consolidated: QuantConnect.Data.IBaseData, callback: System.AsyncCallback, object: object) -> System.IAsyncResult:
        pass

    def EndInvoke(self, result: System.IAsyncResult) -> None:
        pass

    def Invoke(self, sender: object, consolidated: QuantConnect.Data.IBaseData) -> None:
        pass

    def __init__(self, object: object, method: System.IntPtr) -> QuantConnect.Data.Consolidators.DataConsolidatedHandler:
        pass


class DataConsolidator(System.object, System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    # no doc
    def Dispose(self) -> None:
        pass

    def Scan(self, currentLocalTime: datetime.datetime) -> None:
        pass

    @typing.overload
    def Update(self, data: QuantConnect.Data.IBaseData) -> None:
        pass

    @typing.overload
    def Update(self, data: QuantConnect.Data.Consolidators.TInput) -> None:
        pass

    def Update(self, *args) -> None:
        pass

    Consolidated: QuantConnect.Data.IBaseData

    InputType: type

    OutputType: type

    WorkingData: QuantConnect.Data.IBaseData


    DataConsolidated: BoundEvent


class DynamicDataConsolidator(QuantConnect.Data.Consolidators.TradeBarConsolidatorBase[DynamicData], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    A data csolidator that can make trade bars from DynamicData derived types. This is useful for
                aggregating Quandl and other highly flexible dynamic custom data types.
    
    DynamicDataConsolidator(period: TimeSpan)
    DynamicDataConsolidator(maxCount: int)
    DynamicDataConsolidator(maxCount: int, period: TimeSpan)
    DynamicDataConsolidator(func: Func[DateTime, CalendarInfo])
    """
    @typing.overload
    def __init__(self, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.DynamicDataConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int) -> QuantConnect.Data.Consolidators.DynamicDataConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.DynamicDataConsolidator:
        pass

    @typing.overload
    def __init__(self, func: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo]) -> QuantConnect.Data.Consolidators.DynamicDataConsolidator:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Consolidators.DynamicDataConsolidator:
        pass



class IDataConsolidator(System.IDisposable):
    """
    Represents a type capable of taking BaseData updates and firing events containing new
                'consolidated' data. These types can be used to produce larger bars, or even be used to
                transform the data before being sent to another component. The most common usage of these
                types is with indicators.
    """
    def Scan(self, currentLocalTime: datetime.datetime) -> None:
        pass

    def Update(self, data: QuantConnect.Data.IBaseData) -> None:
        pass

    Consolidated: QuantConnect.Data.IBaseData

    InputType: type

    OutputType: type

    WorkingData: QuantConnect.Data.IBaseData


    DataConsolidated: BoundEvent


class IdentityDataConsolidator(QuantConnect.Data.Consolidators.DataConsolidator[T], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """ IdentityDataConsolidator[T]() """
    def Scan(self, currentLocalTime: datetime.datetime) -> None:
        pass

    @typing.overload
    def Update(self, data: QuantConnect.Data.Consolidators.T) -> None:
        pass

    @typing.overload
    def Update(self, data: QuantConnect.Data.IBaseData) -> None:
        pass

    def Update(self, *args) -> None:
        pass

    OutputType: type

    WorkingData: QuantConnect.Data.IBaseData



class OpenInterestConsolidator(QuantConnect.Data.Consolidators.PeriodCountConsolidatorBase[Tick, OpenInterest], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    Type capable of consolidating open interest
    
    OpenInterestConsolidator(period: TimeSpan)
    OpenInterestConsolidator(maxCount: int)
    OpenInterestConsolidator(maxCount: int, period: TimeSpan)
    OpenInterestConsolidator(func: Func[DateTime, CalendarInfo])
    OpenInterestConsolidator(pyfuncobj: PyObject)
    """
    @staticmethod
    def FromResolution(resolution: QuantConnect.Resolution) -> QuantConnect.Data.Consolidators.OpenInterestConsolidator:
        pass

    @typing.overload
    def __init__(self, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.OpenInterestConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int) -> QuantConnect.Data.Consolidators.OpenInterestConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.OpenInterestConsolidator:
        pass

    @typing.overload
    def __init__(self, func: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo]) -> QuantConnect.Data.Consolidators.OpenInterestConsolidator:
        pass

    @typing.overload
    def __init__(self, pyfuncobj: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.OpenInterestConsolidator:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Consolidators.OpenInterestConsolidator:
        pass



class PeriodCountConsolidatorBase(QuantConnect.Data.Consolidators.DataConsolidator[T], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    # no doc
    def Scan(self, currentLocalTime: datetime.datetime) -> None:
        pass

    @typing.overload
    def Update(self, data: QuantConnect.Data.Consolidators.T) -> None:
        pass

    @typing.overload
    def Update(self, data: QuantConnect.Data.IBaseData) -> None:
        pass

    def Update(self, *args) -> None:
        pass

    def __init__(self, *args): #cannot find CLR constructor
        pass

    OutputType: type

    WorkingData: QuantConnect.Data.IBaseData


    DataConsolidated: BoundEvent


class QuoteBarConsolidator(QuantConnect.Data.Consolidators.PeriodCountConsolidatorBase[QuoteBar, QuoteBar], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    Consolidates QuoteBars into larger QuoteBars
    
    QuoteBarConsolidator(period: TimeSpan)
    QuoteBarConsolidator(maxCount: int)
    QuoteBarConsolidator(maxCount: int, period: TimeSpan)
    QuoteBarConsolidator(func: Func[DateTime, CalendarInfo])
    QuoteBarConsolidator(pyfuncobj: PyObject)
    """
    @typing.overload
    def __init__(self, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.QuoteBarConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int) -> QuantConnect.Data.Consolidators.QuoteBarConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.QuoteBarConsolidator:
        pass

    @typing.overload
    def __init__(self, func: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo]) -> QuantConnect.Data.Consolidators.QuoteBarConsolidator:
        pass

    @typing.overload
    def __init__(self, pyfuncobj: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.QuoteBarConsolidator:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Consolidators.QuoteBarConsolidator:
        pass
