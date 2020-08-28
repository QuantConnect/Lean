from .__Auxiliary_1 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Interfaces
import QuantConnect.Data.Market
import QuantConnect.Data.Auxiliary
import QuantConnect.Data
import QuantConnect
import datetime

# no functions
# classes

class FactorFile(System.object, System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[FactorFileRow]):
    """
    Represents an entire factor file for a specified symbol
    
    FactorFile(permtick: str, data: IEnumerable[FactorFileRow], factorFileMinimumDate: Nullable[DateTime])
    """
    def Apply(self, data: typing.List[QuantConnect.Data.BaseData], exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Data.Auxiliary.FactorFile:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Data.Auxiliary.FactorFileRow]:
        pass

    def GetPriceScaleFactor(self, searchDate: datetime.datetime) -> float:
        pass

    def GetScalingFactors(self, searchDate: datetime.datetime) -> QuantConnect.Data.Auxiliary.FactorFileRow:
        pass

    def GetSplitFactor(self, searchDate: datetime.datetime) -> float:
        pass

    def GetSplitsAndDividends(self, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> typing.List[QuantConnect.Data.BaseData]:
        pass

    def HasDividendEventOnNextTradingDay(self, date: datetime.datetime, priceFactorRatio: float) -> bool:
        pass

    @staticmethod
    def HasScalingFactors(permtick: str, market: str) -> bool:
        pass

    def HasSplitEventOnNextTradingDay(self, date: datetime.datetime, splitFactor: float) -> bool:
        pass

    @staticmethod
    def Parse(permtick: str, lines: typing.List[str]) -> QuantConnect.Data.Auxiliary.FactorFile:
        pass

    @staticmethod
    def Read(permtick: str, market: str) -> QuantConnect.Data.Auxiliary.FactorFile:
        pass

    def ToCsvLines(self) -> typing.List[str]:
        pass

    def WriteToCsv(self, symbol: QuantConnect.Symbol) -> None:
        pass

    def __init__(self, permtick: str, data: typing.List[QuantConnect.Data.Auxiliary.FactorFileRow], factorFileMinimumDate: typing.Optional[datetime.datetime]) -> QuantConnect.Data.Auxiliary.FactorFile:
        pass

    FactorFileMinimumDate: typing.Optional[datetime.datetime]

    MostRecentFactorChange: datetime.datetime

    Permtick: str

    SortedFactorFileData: System.Collections.Generic.SortedList[datetime.datetime, QuantConnect.Data.Auxiliary.FactorFileRow]



class FactorFileRow(System.object):
    """
    Defines a single row in a factor_factor file. This is a csv file ordered as {date, price factor, split factor, reference price}
    
    FactorFileRow(date: DateTime, priceFactor: Decimal, splitFactor: Decimal, referencePrice: Decimal)
    """
    @typing.overload
    def Apply(self, dividend: QuantConnect.Data.Market.Dividend, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Data.Auxiliary.FactorFileRow:
        pass

    @typing.overload
    def Apply(self, split: QuantConnect.Data.Market.Split, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Data.Auxiliary.FactorFileRow:
        pass

    def Apply(self, *args) -> QuantConnect.Data.Auxiliary.FactorFileRow:
        pass

    def GetDividend(self, futureFactorFileRow: QuantConnect.Data.Auxiliary.FactorFileRow, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Data.Market.Dividend:
        pass

    def GetSplit(self, futureFactorFileRow: QuantConnect.Data.Auxiliary.FactorFileRow, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Data.Market.Split:
        pass

    @staticmethod
    def Parse(lines: typing.List[str], factorFileMinimumDate: typing.Optional) -> typing.List[QuantConnect.Data.Auxiliary.FactorFileRow]:
        pass

    @staticmethod
    def Read(permtick: str, market: str, factorFileMinimumDate: typing.Optional) -> typing.List[QuantConnect.Data.Auxiliary.FactorFileRow]:
        pass

    def ToCsv(self, source: str) -> str:
        pass

    def ToString(self) -> str:
        pass

    def __init__(self, date: datetime.datetime, priceFactor: float, splitFactor: float, referencePrice: float) -> QuantConnect.Data.Auxiliary.FactorFileRow:
        pass

    Date: datetime.datetime

    PriceFactor: float

    PriceScaleFactor: float

    ReferencePrice: float

    SplitFactor: float



class LocalDiskFactorFileProvider(System.object, QuantConnect.Interfaces.IFactorFileProvider):
    """
    Provides an implementation of QuantConnect.Interfaces.IFactorFileProvider that searches the local disk
    
    LocalDiskFactorFileProvider()
    LocalDiskFactorFileProvider(mapFileProvider: IMapFileProvider)
    """
    def Get(self, symbol: QuantConnect.Symbol) -> QuantConnect.Data.Auxiliary.FactorFile:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider:
        pass

    @typing.overload
    def __init__(self, mapFileProvider: QuantConnect.Interfaces.IMapFileProvider) -> QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider:
        pass


class LocalDiskMapFileProvider(System.object, QuantConnect.Interfaces.IMapFileProvider):
    """
    Provides a default implementation of QuantConnect.Interfaces.IMapFileProvider that reads from
                the local disk
    
    LocalDiskMapFileProvider()
    """
    def Get(self, market: str) -> QuantConnect.Data.Auxiliary.MapFileResolver:
        pass


class MapFile(System.object, System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[MapFileRow]):
    """
    Represents an entire map file for a specified symbol
    
    MapFile(permtick: str, data: IEnumerable[MapFileRow])
    """
    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Data.Auxiliary.MapFileRow]:
        pass

    @staticmethod
    def GetMapFilePath(permtick: str, market: str) -> str:
        pass

    @staticmethod
    def GetMapFiles(mapFileDirectory: str) -> typing.List[QuantConnect.Data.Auxiliary.MapFile]:
        pass

    def GetMappedSymbol(self, searchDate: datetime.datetime, defaultReturnValue: str) -> str:
        pass

    def HasData(self, date: datetime.datetime) -> bool:
        pass

    @staticmethod
    def Read(permtick: str, market: str) -> QuantConnect.Data.Auxiliary.MapFile:
        pass

    def ToCsvLines(self) -> typing.List[str]:
        pass

    def WriteToCsv(self, market: str) -> None:
        pass

    def __init__(self, permtick: str, data: typing.List[QuantConnect.Data.Auxiliary.MapFileRow]) -> QuantConnect.Data.Auxiliary.MapFile:
        pass

    DelistingDate: datetime.datetime

    FirstDate: datetime.datetime

    FirstTicker: str

    Permtick: str



class MapFileResolver(System.object, System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[MapFile]):
    """
    Provides a means of mapping a symbol at a point in time to the map file
                containing that share class's mapping information
    
    MapFileResolver(mapFiles: IEnumerable[MapFile])
    """
    @staticmethod
    @typing.overload
    def Create(dataDirectory: str, market: str) -> QuantConnect.Data.Auxiliary.MapFileResolver:
        pass

    @staticmethod
    @typing.overload
    def Create(mapFileDirectory: str) -> QuantConnect.Data.Auxiliary.MapFileResolver:
        pass

    def Create(self, *args) -> QuantConnect.Data.Auxiliary.MapFileResolver:
        pass

    def GetByPermtick(self, permtick: str) -> QuantConnect.Data.Auxiliary.MapFile:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Data.Auxiliary.MapFile]:
        pass

    def ResolveMapFile(self, symbol: str, date: datetime.datetime) -> QuantConnect.Data.Auxiliary.MapFile:
        pass

    def __init__(self, mapFiles: typing.List[QuantConnect.Data.Auxiliary.MapFile]) -> QuantConnect.Data.Auxiliary.MapFileResolver:
        pass

    Empty: 'MapFileResolver'


class MapFileRow(System.object, System.IEquatable[MapFileRow]):
    """
    Represents a single row in a map_file. This is a csv file ordered as {date, mapped symbol}
    
    MapFileRow(date: DateTime, mappedSymbol: str)
    """
    @typing.overload
    def Equals(self, other: QuantConnect.Data.Auxiliary.MapFileRow) -> bool:
        pass

    @typing.overload
    def Equals(self, obj: object) -> bool:
        pass

    def Equals(self, *args) -> bool:
        pass

    def GetHashCode(self) -> int:
        pass

    @staticmethod
    def Parse(line: str) -> QuantConnect.Data.Auxiliary.MapFileRow:
        pass

    @staticmethod
    @typing.overload
    def Read(permtick: str, market: str) -> typing.List[QuantConnect.Data.Auxiliary.MapFileRow]:
        pass

    @staticmethod
    @typing.overload
    def Read(path: str) -> typing.List[QuantConnect.Data.Auxiliary.MapFileRow]:
        pass

    def Read(self, *args) -> typing.List[QuantConnect.Data.Auxiliary.MapFileRow]:
        pass

    def ToCsv(self) -> str:
        pass

    def ToString(self) -> str:
        pass

    def __init__(self, date: datetime.datetime, mappedSymbol: str) -> QuantConnect.Data.Auxiliary.MapFileRow:
        pass

    Date: datetime.datetime

    MappedSymbol: str



class MappingExtensions(System.object):
    """ Mapping extensions helper methods """
    @staticmethod
    def ResolveMapFile(mapFileResolver: QuantConnect.Data.Auxiliary.MapFileResolver, symbol: QuantConnect.Symbol, dataType: type) -> QuantConnect.Data.Auxiliary.MapFile:
        pass

    __all__: list


class ZipEntryName(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Defines a data type that just produces data points from the zip entry names in a zip file
    
    ZipEntryName()
    """
    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
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
