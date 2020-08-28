from .____init___1 import *
import typing
import System.Xml.Linq
import System.Threading
import System.Text
import System.Linq.Expressions
import System.Linq
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Util
import QuantConnect.Securities
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import NodaTime
import Newtonsoft.Json
import datetime

# functions

def Ref(*args, **kwargs): # real signature unknown
    """ Provides some helper methods that leverage C# type inference """
    pass

# classes

class BusyBlockingCollection(System.object, System.IDisposable, QuantConnect.Interfaces.IBusyCollection[T]):
    """
    BusyBlockingCollection[T]()
    BusyBlockingCollection[T](boundedCapacity: int)
    """
    @typing.overload
    def Add(self, item: QuantConnect.Util.T) -> None:
        pass

    @typing.overload
    def Add(self, item: QuantConnect.Util.T, cancellationToken: System.Threading.CancellationToken) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def CompleteAdding(self) -> None:
        pass

    def Dispose(self) -> None:
        pass

    @typing.overload
    def GetConsumingEnumerable(self) -> typing.List[QuantConnect.Util.T]:
        pass

    @typing.overload
    def GetConsumingEnumerable(self, cancellationToken: System.Threading.CancellationToken) -> typing.List[QuantConnect.Util.T]:
        pass

    def GetConsumingEnumerable(self, *args) -> typing.List[QuantConnect.Util.T]:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Util.BusyBlockingCollection:
        pass

    @typing.overload
    def __init__(self, boundedCapacity: int) -> QuantConnect.Util.BusyBlockingCollection:
        pass

    def __init__(self, *args) -> QuantConnect.Util.BusyBlockingCollection:
        pass

    Count: int

    IsBusy: bool

    WaitHandle: System.Threading.WaitHandle



class BusyCollection(System.object, System.IDisposable, QuantConnect.Interfaces.IBusyCollection[T]):
    """ BusyCollection[T]() """
    @typing.overload
    def Add(self, item: QuantConnect.Util.T) -> None:
        pass

    @typing.overload
    def Add(self, item: QuantConnect.Util.T, cancellationToken: System.Threading.CancellationToken) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def CompleteAdding(self) -> None:
        pass

    def Dispose(self) -> None:
        pass

    @typing.overload
    def GetConsumingEnumerable(self) -> typing.List[QuantConnect.Util.T]:
        pass

    @typing.overload
    def GetConsumingEnumerable(self, cancellationToken: System.Threading.CancellationToken) -> typing.List[QuantConnect.Util.T]:
        pass

    def GetConsumingEnumerable(self, *args) -> typing.List[QuantConnect.Util.T]:
        pass

    Count: int

    IsBusy: bool

    WaitHandle: System.Threading.WaitHandle



class CircularQueue(System.object):
    """
    CircularQueue[T](*items: Array[T])
    CircularQueue[T](items: IEnumerable[T])
    """
    def Dequeue(self) -> QuantConnect.Util.T:
        pass

    @typing.overload
    def __init__(self, items: typing.List[QuantConnect.Util.T]) -> QuantConnect.Util.CircularQueue:
        pass

    @typing.overload
    def __init__(self, items: typing.List[QuantConnect.Util.T]) -> QuantConnect.Util.CircularQueue:
        pass

    def __init__(self, *args) -> QuantConnect.Util.CircularQueue:
        pass

    CircleCompleted: BoundEvent


class ColorJsonConverter(QuantConnect.Util.TypeChangeJsonConverter[Color, str]):
    """
    A Newtonsoft.Json.JsonConverter implementation that serializes a System.Drawing.Color as a string.
                If Color is empty, string is also empty and vice-versa. Meaning that color is autogen.
    
    ColorJsonConverter()
    """


class Composer(System.object):
    """
    Provides methods for obtaining exported MEF instances
    
    Composer()
    """
    def AddPart(self, instance: QuantConnect.Util.T) -> None:
        pass

    def GetExportedValueByTypeName(self, typeName: str) -> QuantConnect.Util.T:
        pass

    def GetExportedValues(self) -> typing.List[QuantConnect.Util.T]:
        pass

    def Reset(self) -> None:
        pass

    def Single(self, predicate: typing.Callable[[QuantConnect.Util.T], bool]) -> QuantConnect.Util.T:
        pass

    Instance: 'Composer'


class ConcurrentSet(System.object, System.Collections.IEnumerable, System.Collections.Generic.ISet[T], System.Collections.Generic.ICollection[T], System.Collections.Generic.IEnumerable[T]):
    """ ConcurrentSet[T]() """
    def Add(self, item: QuantConnect.Util.T) -> None:
        pass

    def Clear(self) -> None:
        pass

    def Contains(self, item: QuantConnect.Util.T) -> bool:
        pass

    def CopyTo(self, array: typing.List[QuantConnect.Util.T], arrayIndex: int) -> None:
        pass

    def ExceptWith(self, other: typing.List[QuantConnect.Util.T]) -> None:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Util.T]:
        pass

    def IntersectWith(self, other: typing.List[QuantConnect.Util.T]) -> None:
        pass

    def IsProperSubsetOf(self, other: typing.List[QuantConnect.Util.T]) -> bool:
        pass

    def IsProperSupersetOf(self, other: typing.List[QuantConnect.Util.T]) -> bool:
        pass

    def IsSubsetOf(self, other: typing.List[QuantConnect.Util.T]) -> bool:
        pass

    def IsSupersetOf(self, other: typing.List[QuantConnect.Util.T]) -> bool:
        pass

    def Overlaps(self, other: typing.List[QuantConnect.Util.T]) -> bool:
        pass

    def Remove(self, item: QuantConnect.Util.T) -> bool:
        pass

    def SetEquals(self, other: typing.List[QuantConnect.Util.T]) -> bool:
        pass

    def SymmetricExceptWith(self, other: typing.List[QuantConnect.Util.T]) -> None:
        pass

    def UnionWith(self, other: typing.List[QuantConnect.Util.T]) -> None:
        pass

    Count: int

    IsReadOnly: bool



class DateTimeJsonConverter(Newtonsoft.Json.Converters.IsoDateTimeConverter):
    """
    Provides a json converter that allows defining the date time format used
    
    DateTimeJsonConverter(format: str)
    """
    def __init__(self, format: str) -> QuantConnect.Util.DateTimeJsonConverter:
        pass


class DisposableExtensions(System.object):
    """ Provides extensions methods for System.IDisposable """
    @staticmethod
    @typing.overload
    def DisposeSafely(disposable: System.IDisposable) -> bool:
        pass

    @staticmethod
    @typing.overload
    def DisposeSafely(disposable: System.IDisposable, errorHandler: typing.Callable[[System.Exception], None]) -> bool:
        pass

    def DisposeSafely(self, *args) -> bool:
        pass

    __all__: list


class DoubleUnixSecondsDateTimeJsonConverter(QuantConnect.Util.TypeChangeJsonConverter[Nullable[DateTime], Nullable[float]]):
    """
    Defines a Newtonsoft.Json.JsonConverter that serializes System.DateTime use the number of whole and fractional seconds since unix epoch
    
    DoubleUnixSecondsDateTimeJsonConverter()
    """
    def CanConvert(self, objectType: type) -> bool:
        pass



class EnumeratorExtensions(System.object):
    """ Provides convenience of linq extension methods for System.Collections.Generic.IEnumerator types """
    @staticmethod
    def Select(enumerator: System.Collections.Generic.IEnumerator[QuantConnect.Util.T], selector: typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.TResult]) -> System.Collections.Generic.IEnumerator[QuantConnect.Util.TResult]:
        pass

    @staticmethod
    def SelectMany(enumerator: System.Collections.Generic.IEnumerator[QuantConnect.Util.T], selector: typing.Callable[[QuantConnect.Util.T], System.Collections.Generic.IEnumerator[QuantConnect.Util.TResult]]) -> System.Collections.Generic.IEnumerator[QuantConnect.Util.TResult]:
        pass

    @staticmethod
    def Where(enumerator: System.Collections.Generic.IEnumerator[QuantConnect.Util.T], predicate: typing.Callable[[QuantConnect.Util.T], bool]) -> System.Collections.Generic.IEnumerator[QuantConnect.Util.T]:
        pass

    __all__: list


class ExpressionBuilder(System.object):
    """ Provides methods for constructing expressions at runtime """
    @staticmethod
    def AsEnumerable(expression: System.Linq.Expressions.Expression) -> typing.List[System.Linq.Expressions.Expression]:
        pass

    @staticmethod
    @typing.overload
    def MakePropertyOrFieldSelector(type: type, propertyOrField: str) -> System.Linq.Expressions.LambdaExpression:
        pass

    @staticmethod
    @typing.overload
    def MakePropertyOrFieldSelector(propertyOrField: str) -> System.Linq.Expressions.Expression[typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.TProperty]]:
        pass

    def MakePropertyOrFieldSelector(self, *args) -> System.Linq.Expressions.Expression[typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.TProperty]]:
        pass

    @staticmethod
    def OfType(expression: System.Linq.Expressions.Expression) -> typing.List[QuantConnect.Util.T]:
        pass

    __all__: list


class FixedSizeHashQueue(System.object, System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[T]):
    """ FixedSizeHashQueue[T](size: int) """
    def Add(self, item: QuantConnect.Util.T) -> bool:
        pass

    def Contains(self, item: QuantConnect.Util.T) -> bool:
        pass

    def Dequeue(self) -> QuantConnect.Util.T:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Util.T]:
        pass

    def TryPeek(self, item: QuantConnect.Util.T) -> bool:
        pass

    def __init__(self, size: int) -> QuantConnect.Util.FixedSizeHashQueue:
        pass


class FixedSizeQueue(System.Collections.Generic.Queue[T], System.Collections.Generic.IReadOnlyCollection[T], System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[T], System.Collections.ICollection):
    """ FixedSizeQueue[T](limit: int) """
    @typing.overload
    def Enqueue(self, item: QuantConnect.Util.T) -> None:
        pass

    @typing.overload
    def Enqueue(self, item: QuantConnect.Util.T) -> None:
        pass

    def Enqueue(self, *args) -> None:
        pass

    def __init__(self, limit: int) -> QuantConnect.Util.FixedSizeQueue:
        pass

    Limit: int



class FuncTextWriter(System.IO.TextWriter, System.IDisposable):
    """
    Provides an implementation of System.IO.TextWriter that redirects Write(string) and WriteLine(string)
    
    FuncTextWriter(writer: Action[str])
    """
    def Dispose(self) -> None:
        pass

    @typing.overload
    def Write(self, value: str) -> None:
        pass

    @typing.overload
    def Write(self, value: str) -> None:
        pass

    @typing.overload
    def Write(self, buffer: typing.List[str]) -> None:
        pass

    @typing.overload
    def Write(self, buffer: typing.List[str], index: int, count: int) -> None:
        pass

    @typing.overload
    def Write(self, value: bool) -> None:
        pass

    @typing.overload
    def Write(self, value: int) -> None:
        pass

    @typing.overload
    def Write(self, value: int) -> None:
        pass

    @typing.overload
    def Write(self, value: int) -> None:
        pass

    @typing.overload
    def Write(self, value: int) -> None:
        pass

    @typing.overload
    def Write(self, value: float) -> None:
        pass

    @typing.overload
    def Write(self, value: float) -> None:
        pass

    @typing.overload
    def Write(self, value: float) -> None:
        pass

    @typing.overload
    def Write(self, value: object) -> None:
        pass

    @typing.overload
    def Write(self, format: str, arg0: object) -> None:
        pass

    @typing.overload
    def Write(self, format: str, arg0: object, arg1: object) -> None:
        pass

    @typing.overload
    def Write(self, format: str, arg0: object, arg1: object, arg2: object) -> None:
        pass

    @typing.overload
    def Write(self, format: str, arg: typing.List[object]) -> None:
        pass

    def Write(self, *args) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: str) -> None:
        pass

    @typing.overload
    def WriteLine(self) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: str) -> None:
        pass

    @typing.overload
    def WriteLine(self, buffer: typing.List[str]) -> None:
        pass

    @typing.overload
    def WriteLine(self, buffer: typing.List[str], index: int, count: int) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: bool) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: int) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: int) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: int) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: int) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: float) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: float) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: float) -> None:
        pass

    @typing.overload
    def WriteLine(self, value: object) -> None:
        pass

    @typing.overload
    def WriteLine(self, format: str, arg0: object) -> None:
        pass

    @typing.overload
    def WriteLine(self, format: str, arg0: object, arg1: object) -> None:
        pass

    @typing.overload
    def WriteLine(self, format: str, arg0: object, arg1: object, arg2: object) -> None:
        pass

    @typing.overload
    def WriteLine(self, format: str, arg: typing.List[object]) -> None:
        pass

    def WriteLine(self, *args) -> None:
        pass

    def __init__(self, writer: typing.Callable[[str], None]) -> QuantConnect.Util.FuncTextWriter:
        pass

    Encoding: System.Text.Encoding

    CoreNewLine: typing.List[str]
