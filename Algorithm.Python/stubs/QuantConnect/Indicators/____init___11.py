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


class IReadOnlyWindow(System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[T]):
    # no doc
    Count: int

    IsReady: bool

    MostRecentlyRemoved: QuantConnect.Indicators.T

    Samples: float

    Size: int


    Item: indexer#


class RollingWindow(System.object, QuantConnect.Indicators.IReadOnlyWindow[T], System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[T]):
    """ RollingWindow[T](size: int) """
    def Add(self, item: QuantConnect.Indicators.T) -> None:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Indicators.T]:
        pass

    def Reset(self) -> None:
        pass

    def __init__(self, size: int) -> QuantConnect.Indicators.RollingWindow:
        pass

    Count: int

    IsReady: bool

    MostRecentlyRemoved: QuantConnect.Indicators.T

    Samples: float

    Size: int


    Item: indexer#
