import typing
import System
import QuantConnect.Data.Market
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import datetime



class SequentialConsolidator(System.object, System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    This consolidator wires up the events on its First and Second consolidators
                such that data flows from the First to Second consolidator. It's output comes
                from the Second.
    
    SequentialConsolidator(first: IDataConsolidator, second: IDataConsolidator)
    """
    def Dispose(self) -> None:
        pass

    def Scan(self, currentLocalTime: datetime.datetime) -> None:
        pass

    def Update(self, data: QuantConnect.Data.IBaseData) -> None:
        pass

    def __init__(self, first: QuantConnect.Data.Consolidators.IDataConsolidator, second: QuantConnect.Data.Consolidators.IDataConsolidator) -> QuantConnect.Data.Consolidators.SequentialConsolidator:
        pass

    Consolidated: QuantConnect.Data.IBaseData

    First: QuantConnect.Data.Consolidators.IDataConsolidator

    InputType: type

    OutputType: type

    Second: QuantConnect.Data.Consolidators.IDataConsolidator

    WorkingData: QuantConnect.Data.IBaseData


    DataConsolidated: BoundEvent


class TickConsolidator(QuantConnect.Data.Consolidators.TradeBarConsolidatorBase[Tick], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    A data consolidator that can make bigger bars from ticks over a given
                time span or a count of pieces of data.
    
    TickConsolidator(period: TimeSpan)
    TickConsolidator(maxCount: int)
    TickConsolidator(maxCount: int, period: TimeSpan)
    TickConsolidator(func: Func[DateTime, CalendarInfo])
    TickConsolidator(pyfuncobj: PyObject)
    """
    @typing.overload
    def __init__(self, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.TickConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int) -> QuantConnect.Data.Consolidators.TickConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.TickConsolidator:
        pass

    @typing.overload
    def __init__(self, func: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo]) -> QuantConnect.Data.Consolidators.TickConsolidator:
        pass

    @typing.overload
    def __init__(self, pyfuncobj: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.TickConsolidator:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Consolidators.TickConsolidator:
        pass



class TickQuoteBarConsolidator(QuantConnect.Data.Consolidators.PeriodCountConsolidatorBase[Tick, QuoteBar], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    Consolidates ticks into quote bars. This consolidator ignores trade ticks
    
    TickQuoteBarConsolidator(period: TimeSpan)
    TickQuoteBarConsolidator(maxCount: int)
    TickQuoteBarConsolidator(maxCount: int, period: TimeSpan)
    TickQuoteBarConsolidator(func: Func[DateTime, CalendarInfo])
    TickQuoteBarConsolidator(pyfuncobj: PyObject)
    """
    @typing.overload
    def __init__(self, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.TickQuoteBarConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int) -> QuantConnect.Data.Consolidators.TickQuoteBarConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.TickQuoteBarConsolidator:
        pass

    @typing.overload
    def __init__(self, func: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo]) -> QuantConnect.Data.Consolidators.TickQuoteBarConsolidator:
        pass

    @typing.overload
    def __init__(self, pyfuncobj: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.TickQuoteBarConsolidator:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Consolidators.TickQuoteBarConsolidator:
        pass



class TradeBarConsolidator(QuantConnect.Data.Consolidators.TradeBarConsolidatorBase[TradeBar], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    """
    A data consolidator that can make bigger bars from smaller ones over a given
                 time span or a count of pieces of data.
                
                 Use this consolidator to turn data of a lower resolution into data of a higher resolution,
                 for example, if you subscribe to minute data but want to have a 15 minute bar.
    
    TradeBarConsolidator(period: TimeSpan)
    TradeBarConsolidator(maxCount: int)
    TradeBarConsolidator(maxCount: int, period: TimeSpan)
    TradeBarConsolidator(func: Func[DateTime, CalendarInfo])
    TradeBarConsolidator(pyfuncobj: PyObject)
    """
    @staticmethod
    def FromResolution(resolution: QuantConnect.Resolution) -> QuantConnect.Data.Consolidators.TradeBarConsolidator:
        pass

    @typing.overload
    def __init__(self, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.TradeBarConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int) -> QuantConnect.Data.Consolidators.TradeBarConsolidator:
        pass

    @typing.overload
    def __init__(self, maxCount: int, period: datetime.timedelta) -> QuantConnect.Data.Consolidators.TradeBarConsolidator:
        pass

    @typing.overload
    def __init__(self, func: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo]) -> QuantConnect.Data.Consolidators.TradeBarConsolidator:
        pass

    @typing.overload
    def __init__(self, pyfuncobj: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.TradeBarConsolidator:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Consolidators.TradeBarConsolidator:
        pass



class TradeBarConsolidatorBase(QuantConnect.Data.Consolidators.PeriodCountConsolidatorBase[T, TradeBar], System.IDisposable, QuantConnect.Data.Consolidators.IDataConsolidator):
    # no doc
    def __init__(self, *args): #cannot find CLR constructor
        pass

    WorkingBar: QuantConnect.Data.Market.TradeBar
