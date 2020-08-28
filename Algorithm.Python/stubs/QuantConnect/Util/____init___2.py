from .____init___3 import *
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


class ListComparer(System.object, System.Collections.Generic.IEqualityComparer[List[T]]):
    """ ListComparer[T]() """
    @typing.overload
    def Equals(self, x: typing.List[QuantConnect.Util.T], y: typing.List[QuantConnect.Util.T]) -> bool:
        pass

    @typing.overload
    def Equals(self, obj: object) -> bool:
        pass

    def Equals(self, *args) -> bool:
        pass

    @typing.overload
    def GetHashCode(self, obj: typing.List[QuantConnect.Util.T]) -> int:
        pass

    @typing.overload
    def GetHashCode(self) -> int:
        pass

    def GetHashCode(self, *args) -> int:
        pass


class MarketHoursDatabaseJsonConverter(QuantConnect.Util.TypeChangeJsonConverter[MarketHoursDatabase, MarketHoursDatabaseJson]):
    """
    Provides json conversion for the QuantConnect.Securities.MarketHoursDatabase class
    
    MarketHoursDatabaseJsonConverter()
    """

    MarketHoursDatabaseEntryJson: type
    MarketHoursDatabaseJson: type


class MemoizingEnumerable(System.object, System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[T]):
    """
    MemoizingEnumerable[T](enumerable: IEnumerable[T])
    MemoizingEnumerable[T](enumerator: IEnumerator[T])
    """
    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Util.T]:
        pass

    @typing.overload
    def __init__(self, enumerable: typing.List[QuantConnect.Util.T]) -> QuantConnect.Util.MemoizingEnumerable:
        pass

    @typing.overload
    def __init__(self, enumerator: System.Collections.Generic.IEnumerator[QuantConnect.Util.T]) -> QuantConnect.Util.MemoizingEnumerable:
        pass

    def __init__(self, *args) -> QuantConnect.Util.MemoizingEnumerable:
        pass


class NullStringValueConverter(Newtonsoft.Json.JsonConverter):
    """ NullStringValueConverter[T]() """
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass


class ObjectActivator(System.object):
    """ Provides methods for creating new instances of objects """
    @staticmethod
    def AddActivator(key: type, value: typing.Callable[[typing.List[object]], object]) -> None:
        pass

    @staticmethod
    @typing.overload
    def Clone(instanceToClone: object) -> object:
        pass

    @staticmethod
    @typing.overload
    def Clone(instanceToClone: QuantConnect.Util.T) -> QuantConnect.Util.T:
        pass

    def Clone(self, *args) -> QuantConnect.Util.T:
        pass

    @staticmethod
    def GetActivator(dataType: type) -> typing.Callable[[typing.List[object]], object]:
        pass

    @staticmethod
    def ResetActivators() -> None:
        pass

    __all__: list


class PythonUtil(System.object):
    """
    Collection of utils for python objects processing
    
    PythonUtil()
    """
    @staticmethod
    def PythonExceptionStackParser(value: str) -> str:
        pass

    @staticmethod
    @typing.overload
    def ToAction(pyObject: Python.Runtime.PyObject) -> typing.Callable[[QuantConnect.Util.T1], None]:
        pass

    @staticmethod
    @typing.overload
    def ToAction(pyObject: Python.Runtime.PyObject) -> typing.Callable[[QuantConnect.Util.T1, QuantConnect.Util.T2], None]:
        pass

    def ToAction(self, *args) -> typing.Callable[[QuantConnect.Util.T1, QuantConnect.Util.T2], None]:
        pass

    @staticmethod
    def ToCoarseFundamentalSelector(pyObject: Python.Runtime.PyObject) -> typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]]:
        pass

    @staticmethod
    def ToFineFundamentalSelector(pyObject: Python.Runtime.PyObject) -> typing.Callable[[typing.List[QuantConnect.Data.Fundamental.FineFundamental]], typing.List[QuantConnect.Symbol]]:
        pass

    @staticmethod
    def ToFunc(pyObject: Python.Runtime.PyObject) -> typing.Callable[[QuantConnect.Util.T1], QuantConnect.Util.T2]:
        pass


class RateGate(System.object, System.IDisposable):
    """
    Used to control the rate of some occurrence per unit of time.
    
    RateGate(occurrences: int, timeUnit: TimeSpan)
    """
    def Dispose(self) -> None:
        pass

    @typing.overload
    def WaitToProceed(self, millisecondsTimeout: int) -> bool:
        pass

    @typing.overload
    def WaitToProceed(self, timeout: datetime.timedelta) -> bool:
        pass

    @typing.overload
    def WaitToProceed(self) -> None:
        pass

    def WaitToProceed(self, *args) -> None:
        pass

    def __init__(self, occurrences: int, timeUnit: datetime.timedelta) -> QuantConnect.Util.RateGate:
        pass

    IsRateLimited: bool

    Occurrences: int

    TimeUnitMilliseconds: int



class ReaderWriterLockSlimExtensions(System.object):
    """ Provides extension methods to make working with the System.Threading.ReaderWriterLockSlim class easier """
    @staticmethod
    def Read(readerWriterLockSlim: System.Threading.ReaderWriterLockSlim) -> System.IDisposable:
        pass

    @staticmethod
    def Write(readerWriterLockSlim: System.Threading.ReaderWriterLockSlim) -> System.IDisposable:
        pass

    __all__: list


class ReferenceWrapper(System.object):
    """ ReferenceWrapper[T](value: T) """
    def __init__(self, value: QuantConnect.Util.T) -> QuantConnect.Util.ReferenceWrapper:
        pass

    Value: QuantConnect.Util.T

class SecurityExtensions(System.object):
    """
    Provides useful infrastructure methods to the QuantConnect.Securities.Security class.
                These are added in this way to avoid mudding the class's public API
    """
    @staticmethod
    def IsInternalFeed(security: QuantConnect.Securities.Security) -> bool:
        pass

    __all__: list


class SecurityIdentifierJsonConverter(QuantConnect.Util.TypeChangeJsonConverter[SecurityIdentifier, str]):
    """
    A Newtonsoft.Json.JsonConverter implementation that serializes a QuantConnect.SecurityIdentifier as a string
    
    SecurityIdentifierJsonConverter()
    """


class SeriesJsonConverter(Newtonsoft.Json.JsonConverter):
    """
    Json Converter for Series which handles special Pie Series serialization case
    
    SeriesJsonConverter()
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass

    CanRead: bool



class SingleValueListConverter(Newtonsoft.Json.JsonConverter):
    """ SingleValueListConverter[T]() """
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass


class StreamReaderEnumerable(System.object, System.IDisposable, System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[str]):
    """
    Converts a System.IO.StreamReader into an enumerable of string
    
    StreamReaderEnumerable(stream: Stream, *disposables: Array[IDisposable])
    StreamReaderEnumerable(reader: StreamReader, *disposables: Array[IDisposable])
    """
    def Dispose(self) -> None:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[str]:
        pass

    @typing.overload
    def __init__(self, stream: System.IO.Stream, disposables: typing.List[System.IDisposable]) -> QuantConnect.Util.StreamReaderEnumerable:
        pass

    @typing.overload
    def __init__(self, reader: System.IO.StreamReader, disposables: typing.List[System.IDisposable]) -> QuantConnect.Util.StreamReaderEnumerable:
        pass

    def __init__(self, *args) -> QuantConnect.Util.StreamReaderEnumerable:
        pass


class StreamReaderExtensions(System.object):
    """ Extension methods to fetch data from a System.IO.StreamReader instance """
    @staticmethod
    def GetDateTime(stream: System.IO.StreamReader, format: str, delimiter: str) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def GetDecimal(stream: System.IO.StreamReader, delimiter: str) -> float:
        pass

    @staticmethod
    @typing.overload
    def GetDecimal(stream: System.IO.StreamReader, pastEndLine: bool, delimiter: str) -> float:
        pass

    def GetDecimal(self, *args) -> float:
        pass

    @staticmethod
    def GetInt32(stream: System.IO.StreamReader, delimiter: str) -> int:
        pass

    @staticmethod
    def GetString(stream: System.IO.StreamReader, delimiter: str) -> str:
        pass

    __all__: list


class TypeChangeJsonConverter(Newtonsoft.Json.JsonConverter):
    # no doc
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass



class Validate(System.object):
    """ Provides methods for validating strings following a certain format, such as an email address """
    @staticmethod
    def EmailAddress(emailAddress: str) -> bool:
        pass

    RegularExpression: type
    __all__: list


class VersionHelper(System.object):
    """ Provides methods for dealing with lean assembly versions """
    @staticmethod
    def CompareVersions(left: str, right: str) -> int:
        pass

    @staticmethod
    def IsEqualVersion(version: str) -> bool:
        pass

    @staticmethod
    def IsNewerVersion(version: str) -> bool:
        pass

    @staticmethod
    def IsNotEqualVersion(version: str) -> bool:
        pass

    @staticmethod
    def IsOlderVersion(version: str) -> bool:
        pass

    __all__: list
