from .____init___2 import *
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


class IReadOnlyRef:
    # no doc
    Value: QuantConnect.Util.T



class JsonRoundingConverter(Newtonsoft.Json.JsonConverter):
    """
    Helper Newtonsoft.Json.JsonConverter that will round decimal and double types,
                to QuantConnect.Util.JsonRoundingConverter.FractionalDigits fractional digits
    
    JsonRoundingConverter()
    """
    def CanConvert(self, objectType: type) -> bool:
        pass

    def ReadJson(self, reader: Newtonsoft.Json.JsonReader, objectType: type, existingValue: object, serializer: Newtonsoft.Json.JsonSerializer) -> object:
        pass

    def WriteJson(self, writer: Newtonsoft.Json.JsonWriter, value: object, serializer: Newtonsoft.Json.JsonSerializer) -> None:
        pass

    CanRead: bool


    FractionalDigits: int


class LeanData(System.object):
    """ Provides methods for generating lean data file content """
    @staticmethod
    @typing.overload
    def GenerateLine(data: QuantConnect.Data.IBaseData, resolution: QuantConnect.Resolution, exchangeTimeZone: NodaTime.DateTimeZone, dataTimeZone: NodaTime.DateTimeZone) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateLine(data: QuantConnect.Data.IBaseData, securityType: QuantConnect.SecurityType, resolution: QuantConnect.Resolution) -> str:
        pass

    def GenerateLine(self, *args) -> str:
        pass

    @staticmethod
    def GenerateRelativeFactorFilePath(symbol: QuantConnect.Symbol) -> str:
        pass

    @staticmethod
    def GenerateRelativeZipFileDirectory(symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateRelativeZipFilePath(symbol: QuantConnect.Symbol, date: datetime.datetime, resolution: QuantConnect.Resolution, tickType: QuantConnect.TickType) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateRelativeZipFilePath(symbol: str, securityType: QuantConnect.SecurityType, market: str, date: datetime.datetime, resolution: QuantConnect.Resolution) -> str:
        pass

    def GenerateRelativeZipFilePath(self, *args) -> str:
        pass

    @staticmethod
    def GenerateZipEntryName(symbol: QuantConnect.Symbol, date: datetime.datetime, resolution: QuantConnect.Resolution, tickType: QuantConnect.TickType) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateZipFileName(symbol: QuantConnect.Symbol, date: datetime.datetime, resolution: QuantConnect.Resolution, tickType: QuantConnect.TickType) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateZipFileName(symbol: str, securityType: QuantConnect.SecurityType, date: datetime.datetime, resolution: QuantConnect.Resolution, tickType: typing.Optional[QuantConnect.TickType]) -> str:
        pass

    def GenerateZipFileName(self, *args) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateZipFilePath(dataDirectory: str, symbol: QuantConnect.Symbol, date: datetime.datetime, resolution: QuantConnect.Resolution, tickType: QuantConnect.TickType) -> str:
        pass

    @staticmethod
    @typing.overload
    def GenerateZipFilePath(dataDirectory: str, symbol: str, securityType: QuantConnect.SecurityType, market: str, date: datetime.datetime, resolution: QuantConnect.Resolution) -> str:
        pass

    def GenerateZipFilePath(self, *args) -> str:
        pass

    @staticmethod
    def GetCommonTickType(securityType: QuantConnect.SecurityType) -> QuantConnect.TickType:
        pass

    @staticmethod
    def GetCommonTickTypeForCommonDataTypes(type: type, securityType: QuantConnect.SecurityType) -> QuantConnect.TickType:
        pass

    @staticmethod
    def GetDataType(resolution: QuantConnect.Resolution, tickType: QuantConnect.TickType) -> type:
        pass

    @staticmethod
    def IsCommonLeanDataType(baseDataType: type) -> bool:
        pass

    @staticmethod
    def ParseDataSecurityType(securityType: str) -> QuantConnect.SecurityType:
        pass

    @staticmethod
    def ReadSymbolFromZipEntry(symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, zipEntryName: str) -> QuantConnect.Symbol:
        pass

    @staticmethod
    def TryParsePath(fileName: str, symbol: QuantConnect.Symbol, date: datetime.datetime, resolution: QuantConnect.Resolution) -> bool:
        pass

    SecurityTypeAsDataPath: List[str]
    __all__: list


class LeanDataPathComponents(System.object):
    """
    Type representing the various pieces of information emebedded into a lean data file path
    
    LeanDataPathComponents(securityType: SecurityType, market: str, resolution: Resolution, symbol: Symbol, filename: str, date: DateTime, tickType: TickType)
    """
    @staticmethod
    def Parse(path: str) -> QuantConnect.Util.LeanDataPathComponents:
        pass

    def __init__(self, securityType: QuantConnect.SecurityType, market: str, resolution: QuantConnect.Resolution, symbol: QuantConnect.Symbol, filename: str, date: datetime.datetime, tickType: QuantConnect.TickType) -> QuantConnect.Util.LeanDataPathComponents:
        pass

    Date: datetime.datetime

    Filename: str

    Market: str

    Resolution: QuantConnect.Resolution

    SecurityType: QuantConnect.SecurityType

    Symbol: QuantConnect.Symbol

    TickType: QuantConnect.TickType



class LinqExtensions(System.object):
    """ Provides more extension methods for the enumerable types """
    @staticmethod
    def AreDifferent(left: System.Collections.Generic.ISet[QuantConnect.Util.T], right: System.Collections.Generic.ISet[QuantConnect.Util.T]) -> bool:
        pass

    @staticmethod
    def AsEnumerable(enumerator: System.Collections.Generic.IEnumerator[QuantConnect.Util.T]) -> typing.List[QuantConnect.Util.T]:
        pass

    @staticmethod
    @typing.overload
    def BinarySearch(list: typing.List[QuantConnect.Util.TItem], value: QuantConnect.Util.TSearch, comparer: typing.Callable[[QuantConnect.Util.TSearch, QuantConnect.Util.TItem], int]) -> int:
        pass

    @staticmethod
    @typing.overload
    def BinarySearch(list: typing.List[QuantConnect.Util.TItem], value: QuantConnect.Util.TItem) -> int:
        pass

    @staticmethod
    @typing.overload
    def BinarySearch(list: typing.List[QuantConnect.Util.TItem], value: QuantConnect.Util.TItem, comparer: System.Collections.Generic.IComparer[QuantConnect.Util.TItem]) -> int:
        pass

    def BinarySearch(self, *args) -> int:
        pass

    @staticmethod
    def DefaultIfEmpty(enumerable: typing.List[QuantConnect.Util.T], selector: typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.TResult], defaultValue: QuantConnect.Util.TResult) -> typing.List[QuantConnect.Util.TResult]:
        pass

    @staticmethod
    def DistinctBy(enumerable: typing.List[QuantConnect.Util.T], selector: typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.TPropery]) -> typing.List[QuantConnect.Util.T]:
        pass

    @staticmethod
    def Except(enumerable: typing.List[QuantConnect.Util.T], set: System.Collections.Generic.ISet[QuantConnect.Util.T]) -> typing.List[QuantConnect.Util.T]:
        pass

    @staticmethod
    def GetValueOrDefault(dictionary: System.Collections.Generic.IDictionary[QuantConnect.Util.K, QuantConnect.Util.V], key: QuantConnect.Util.K, defaultValue: QuantConnect.Util.V) -> QuantConnect.Util.V:
        pass

    @staticmethod
    def GroupAdjacentBy(enumerable: typing.List[QuantConnect.Util.T], grouper: typing.Callable[[QuantConnect.Util.T, QuantConnect.Util.T], bool]) -> typing.List[typing.List[QuantConnect.Util.T]]:
        pass

    @staticmethod
    def IsNullOrEmpty(enumerable: typing.List[QuantConnect.Util.T]) -> bool:
        pass

    @staticmethod
    @typing.overload
    def Median(enumerable: typing.List[QuantConnect.Util.T]) -> QuantConnect.Util.T:
        pass

    @staticmethod
    @typing.overload
    def Median(collection: typing.List[QuantConnect.Util.T], selector: typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.TProperty]) -> QuantConnect.Util.TProperty:
        pass

    def Median(self, *args) -> QuantConnect.Util.TProperty:
        pass

    @staticmethod
    def Memoize(enumerable: typing.List[QuantConnect.Util.T]) -> typing.List[QuantConnect.Util.T]:
        pass

    @staticmethod
    def Range(start: QuantConnect.Util.T, end: QuantConnect.Util.T, incrementer: typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.T], includeEndPoint: bool) -> typing.List[QuantConnect.Util.T]:
        pass

    @staticmethod
    @typing.overload
    def ToDictionary(lookup: System.Linq.ILookup[QuantConnect.Util.K, QuantConnect.Util.V]) -> System.Collections.Generic.Dictionary[QuantConnect.Util.K, typing.List[QuantConnect.Util.V]]:
        pass

    @staticmethod
    @typing.overload
    def ToDictionary(enumerable: typing.List[System.Collections.Generic.KeyValuePair[QuantConnect.Util.K, QuantConnect.Util.V]]) -> System.Collections.Generic.Dictionary[QuantConnect.Util.K, QuantConnect.Util.V]:
        pass

    def ToDictionary(self, *args) -> System.Collections.Generic.Dictionary[QuantConnect.Util.K, QuantConnect.Util.V]:
        pass

    @staticmethod
    @typing.overload
    def ToHashSet(enumerable: typing.List[QuantConnect.Util.T]) -> System.Collections.Generic.HashSet[QuantConnect.Util.T]:
        pass

    @staticmethod
    @typing.overload
    def ToHashSet(enumerable: typing.List[QuantConnect.Util.T], selector: typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.TResult]) -> System.Collections.Generic.HashSet[QuantConnect.Util.TResult]:
        pass

    def ToHashSet(self, *args) -> System.Collections.Generic.HashSet[QuantConnect.Util.TResult]:
        pass

    @staticmethod
    def ToList(enumerable: typing.List[QuantConnect.Util.T], selector: typing.Callable[[QuantConnect.Util.T], QuantConnect.Util.TResult]) -> typing.List[QuantConnect.Util.TResult]:
        pass

    @staticmethod
    def ToReadOnlyDictionary(enumerable: typing.List[System.Collections.Generic.KeyValuePair[QuantConnect.Util.K, QuantConnect.Util.V]]) -> System.Collections.Generic.IReadOnlyDictionary[QuantConnect.Util.K, QuantConnect.Util.V]:
        pass

    __all__: list
