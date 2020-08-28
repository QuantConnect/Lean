from .____init___2 import *
import typing
import System.Timers
import System.Threading.Tasks
import System.Threading
import System.Text
import System.IO
import System.Globalization
import System.Drawing
import System.Collections.Generic
import System.Collections.Concurrent
import System.Collections
import System
import QuantConnect.Util
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Packets
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect
import Python.Runtime
import NodaTime
import Newtonsoft.Json
import datetime


class Extensions(System.object):
    """ Extensions function collections - group all static extensions functions here. """
    @staticmethod
    @typing.overload
    def Add(dictionary: System.Collections.Generic.IDictionary[QuantConnect.TKey, QuantConnect.TCollection], key: QuantConnect.TKey, element: QuantConnect.TElement) -> None:
        pass

    @staticmethod
    @typing.overload
    def Add(dictionary: QuantConnect.Data.Market.Ticks, key: QuantConnect.Symbol, tick: QuantConnect.Data.Market.Tick) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    @staticmethod
    @typing.overload
    def AddOrUpdate(dictionary: System.Collections.Concurrent.ConcurrentDictionary[QuantConnect.K, QuantConnect.V], key: QuantConnect.K, value: QuantConnect.V) -> None:
        pass

    @staticmethod
    @typing.overload
    def AddOrUpdate(dictionary: System.Collections.Concurrent.ConcurrentDictionary[QuantConnect.TKey, System.Lazy[QuantConnect.TValue]], key: QuantConnect.TKey, addValueFactory: typing.Callable[[QuantConnect.TKey], QuantConnect.TValue], updateValueFactory: typing.Callable[[QuantConnect.TKey, QuantConnect.TValue], QuantConnect.TValue]) -> QuantConnect.TValue:
        pass

    def AddOrUpdate(self, *args) -> QuantConnect.TValue:
        pass

    @staticmethod
    def Batch(resultPackets: typing.List[QuantConnect.Packets.AlphaResultPacket]) -> QuantConnect.Packets.AlphaResultPacket:
        pass

    @staticmethod
    def BatchBy(enumerable: typing.List[QuantConnect.T], batchSize: int) -> typing.List[typing.List[QuantConnect.T]]:
        pass

    @staticmethod
    def Clear(queue: System.Collections.Concurrent.ConcurrentQueue[QuantConnect.T]) -> None:
        pass

    @staticmethod
    def ConvertFromUtc(time: datetime.datetime, to: NodaTime.DateTimeZone, strict: bool) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def ConvertTo(time: datetime.datetime, from_: NodaTime.DateTimeZone, to: NodaTime.DateTimeZone, strict: bool) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def ConvertTo(value: str) -> QuantConnect.T:
        pass

    @staticmethod
    @typing.overload
    def ConvertTo(value: str, type: type) -> object:
        pass

    def ConvertTo(self, *args) -> object:
        pass

    @staticmethod
    def ConvertToDelegate(pyObject: Python.Runtime.PyObject) -> QuantConnect.T:
        pass

    @staticmethod
    def ConvertToDictionary(pyObject: Python.Runtime.PyObject) -> System.Collections.Generic.Dictionary[QuantConnect.TKey, QuantConnect.TValue]:
        pass

    @staticmethod
    def ConvertToSymbolEnumerable(pyObject: Python.Runtime.PyObject) -> typing.List[QuantConnect.Symbol]:
        pass

    @staticmethod
    def ConvertToUniverseSelectionStringDelegate(selector: typing.Callable[[QuantConnect.T], object]) -> typing.Callable[[QuantConnect.T], typing.List[str]]:
        pass

    @staticmethod
    def ConvertToUniverseSelectionSymbolDelegate(selector: typing.Callable[[QuantConnect.T], object]) -> typing.Callable[[QuantConnect.T], typing.List[QuantConnect.Symbol]]:
        pass

    @staticmethod
    def ConvertToUtc(time: datetime.datetime, from_: NodaTime.DateTimeZone, strict: bool) -> datetime.datetime:
        pass

    @staticmethod
    def CreateType(pyObject: Python.Runtime.PyObject) -> type:
        pass

    @staticmethod
    def ExchangeRoundDown(dateTime: datetime.datetime, interval: datetime.timedelta, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, extendedMarket: bool) -> datetime.datetime:
        pass

    @staticmethod
    def ExchangeRoundDownInTimeZone(dateTime: datetime.datetime, interval: datetime.timedelta, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, roundingTimeZone: NodaTime.DateTimeZone, extendedMarket: bool) -> datetime.datetime:
        pass

    @staticmethod
    def GetAndDispose(instance: Python.Runtime.PyObject) -> QuantConnect.T:
        pass

    @staticmethod
    def GetBaseDataInstance(type: type) -> QuantConnect.Data.BaseData:
        pass

    @staticmethod
    def GetBetterTypeName(type: type) -> str:
        pass

    @staticmethod
    def GetBytes(str: str) -> typing.List[bytes]:
        pass

    @staticmethod
    def GetDecimalEpsilon() -> float:
        pass

    @staticmethod
    def GetEnumString(value: int, pyObject: Python.Runtime.PyObject) -> str:
        pass

    @staticmethod
    def GetExtension(str: str) -> str:
        pass

    @staticmethod
    def GetHash(orders: System.Collections.Generic.IDictionary[int, QuantConnect.Orders.Order]) -> int:
        pass

    @staticmethod
    def GetMD5Hash(stream: System.IO.Stream) -> typing.List[bytes]:
        pass

    @staticmethod
    def GetPythonMethod(instance: Python.Runtime.PyObject, name: str) -> object:
        pass

    @staticmethod
    def GetString(bytes: typing.List[bytes], encoding: System.Text.Encoding) -> str:
        pass

    @staticmethod
    def GetStringBetweenChars(value: str, left: str, right: str) -> str:
        pass

    @staticmethod
    def GetZeroPriceMessage(symbol: QuantConnect.Symbol) -> str:
        pass

    @staticmethod
    def IsCommonBusinessDay(date: datetime.datetime) -> bool:
        pass

    @staticmethod
    @typing.overload
    def IsEmpty(series: QuantConnect.Series) -> bool:
        pass

    @staticmethod
    @typing.overload
    def IsEmpty(chart: QuantConnect.Chart) -> bool:
        pass

    def IsEmpty(self, *args) -> bool:
        pass

    @staticmethod
    def IsNaNOrZero(value: float) -> bool:
        pass

    @staticmethod
    def IsSubclassOfGeneric(type: type, possibleSuperType: type) -> bool:
        pass

    @staticmethod
    def IsValid(securityType: QuantConnect.SecurityType) -> bool:
        pass

    @staticmethod
    def LazyToUpper(data: str) -> str:
        pass

    @staticmethod
    def MatchesTypeName(type: type, typeName: str) -> bool:
        pass

    @staticmethod
    def Move(list: typing.List[QuantConnect.T], oldIndex: int, newIndex: int) -> None:
        pass

    @staticmethod
    def Normalize(input: float) -> float:
        pass

    @staticmethod
    def NormalizeToStr(input: float) -> str:
        pass

    @staticmethod
    def OrderTargetsByMarginImpact(targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget], algorithm: QuantConnect.Interfaces.IAlgorithm, targetIsDelta: bool) -> typing.List[QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget]:
        pass

    @staticmethod
    def ProcessUntilEmpty(collection: System.Collections.Concurrent.IProducerConsumerCollection[QuantConnect.T], handler: typing.Callable[[QuantConnect.T], None]) -> None:
        pass

    @staticmethod
    @typing.overload
    def ProtobufSerialize(ticks: typing.List[QuantConnect.Data.Market.Tick]) -> typing.List[bytes]:
        pass

    @staticmethod
    @typing.overload
    def ProtobufSerialize(baseData: QuantConnect.Data.IBaseData) -> typing.List[bytes]:
        pass

    def ProtobufSerialize(self, *args) -> typing.List[bytes]:
        pass

    @staticmethod
    def RemoveFromEnd(s: str, ending: str) -> str:
        pass

    @staticmethod
    def Reset(timer: System.Timers.Timer) -> None:
        pass

    @staticmethod
    def ResolutionToLower(resolution: QuantConnect.Resolution) -> str:
        pass

    @staticmethod
    @typing.overload
    def Round(time: datetime.timedelta, roundingInterval: datetime.timedelta, roundingType: System.MidpointRounding) -> datetime.timedelta:
        pass

    @staticmethod
    @typing.overload
    def Round(time: datetime.timedelta, roundingInterval: datetime.timedelta) -> datetime.timedelta:
        pass

    @staticmethod
    @typing.overload
    def Round(datetime: datetime.datetime, roundingInterval: datetime.timedelta) -> datetime.datetime:
        pass

    def Round(self, *args) -> datetime.datetime:
        pass

    @staticmethod
    def RoundDown(dateTime: datetime.datetime, interval: datetime.timedelta) -> datetime.datetime:
        pass

    @staticmethod
    def RoundDownInTimeZone(dateTime: datetime.datetime, roundingInterval: datetime.timedelta, sourceTimeZone: NodaTime.DateTimeZone, roundingTimeZone: NodaTime.DateTimeZone) -> datetime.datetime:
        pass

    @staticmethod
    @typing.overload
    def RoundToSignificantDigits(d: float, digits: int) -> float:
        pass

    @staticmethod
    @typing.overload
    def RoundToSignificantDigits(d: float, digits: int) -> float:
        pass

    def RoundToSignificantDigits(self, *args) -> float:
        pass

    @staticmethod
    def RoundUp(time: datetime.datetime, d: datetime.timedelta) -> datetime.datetime:
        pass

    @staticmethod
    def SafeDecimalCast(input: float) -> float:
        pass

    @staticmethod
    def SecurityTypeToLower(securityType: QuantConnect.SecurityType) -> str:
        pass

    @staticmethod
    def SingleOrAlgorithmTypeName(names: typing.List[str], algorithmTypeName: str) -> str:
        pass

    @staticmethod
    def SmartRounding(input: float) -> float:
        pass

    @staticmethod
    def StopSafely(thread: System.Threading.Thread, timeout: datetime.timedelta, token: System.Threading.CancellationTokenSource) -> None:
        pass

    @staticmethod
    def SynchronouslyAwaitTask(task: System.Threading.Tasks.Task) -> None:
        pass

    @staticmethod
    def SynchronouslyAwaitTaskResult(task: System.Threading.Tasks.Task[QuantConnect.TResult]) -> QuantConnect.TResult:
        pass

    @staticmethod
    def TickTypeToLower(tickType: QuantConnect.TickType) -> str:
        pass

    @staticmethod
    def ToCamelCase(value: str) -> str:
        pass

    @staticmethod
    def ToCsv(str: str, size: int) -> typing.List[str]:
        pass

    @staticmethod
    def ToCsvData(str: str, size: int, delimiter: str) -> typing.List[str]:
        pass

    @staticmethod
    def ToDecimal(str: str) -> float:
        pass

    @staticmethod
    def ToDecimalAllowExponent(str: str) -> float:
        pass

    @staticmethod
    def ToFunc(dateRule: QuantConnect.Scheduling.IDateRule) -> typing.Callable[[datetime.datetime], typing.Optional[datetime.datetime]]:
        pass

    @staticmethod
    def ToHigherResolutionEquivalent(timeSpan: datetime.timedelta, requireExactMatch: bool) -> QuantConnect.Resolution:
        pass

    @staticmethod
    def ToInt32(str: str) -> int:
        pass

    @staticmethod
    def ToInt64(str: str) -> int:
        pass

    @staticmethod
    def ToLower(enum: System.Enum) -> str:
        pass

    @staticmethod
    def ToMD5(str: str) -> str:
        pass

    @staticmethod
    def ToOrderTicket(order: QuantConnect.Orders.Order, transactionManager: QuantConnect.Securities.SecurityTransactionManager) -> QuantConnect.Orders.OrderTicket:
        pass

    @staticmethod
    def ToPyList(enumerable: System.Collections.IEnumerable) -> Python.Runtime.PyList:
        pass

    @staticmethod
    def ToSafeString(pyObject: Python.Runtime.PyObject) -> str:
        pass

    @staticmethod
    def ToSHA256(data: str) -> str:
        pass

    @staticmethod
    def ToStream(str: str) -> System.IO.Stream:
        pass

    @staticmethod
    def ToStringPerformance(optionRight: QuantConnect.OptionRight) -> str:
        pass

    @staticmethod
    def ToTimeSpan(resolution: QuantConnect.Resolution) -> datetime.timedelta:
        pass

    @staticmethod
    def TruncateTo3DecimalPlaces(value: float) -> float:
        pass

    @staticmethod
    def TryConvert(pyObject: Python.Runtime.PyObject, result: QuantConnect.T, allowPythonDerivative: bool) -> bool:
        pass

    @staticmethod
    def TryConvertToDelegate(pyObject: Python.Runtime.PyObject, result: QuantConnect.T) -> bool:
        pass

    @staticmethod
    @typing.overload
    def WaitOne(waitHandle: System.Threading.WaitHandle, cancellationToken: System.Threading.CancellationToken) -> bool:
        pass

    @staticmethod
    @typing.overload
    def WaitOne(waitHandle: System.Threading.WaitHandle, timeout: datetime.timedelta, cancellationToken: System.Threading.CancellationToken) -> bool:
        pass

    @staticmethod
    @typing.overload
    def WaitOne(waitHandle: System.Threading.WaitHandle, millisecondsTimeout: int, cancellationToken: System.Threading.CancellationToken) -> bool:
        pass

    def WaitOne(self, *args) -> bool:
        pass

    @staticmethod
    def WithEmbeddedHtmlAnchors(source: str) -> str:
        pass

    __all__: list
