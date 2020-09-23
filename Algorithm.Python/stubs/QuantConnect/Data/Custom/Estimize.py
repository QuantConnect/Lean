# encoding: utf-8
# module QuantConnect.Data.Custom.Estimize calls itself Estimize
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import NodaTime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Data.Custom.Estimize
import System
import System.IO
import typing

# no functions
# classes

class EstimizeConsensus(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Consensus of the specified release
    
    EstimizeConsensus()
    EstimizeConsensus(csvLine: str)
    """
    def DataTimeZone(self) -> NodaTime.DateTimeZone:
        pass

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

    def RequiresMapping(self) -> bool:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.Estimize.EstimizeConsensus:
        pass

    @typing.overload
    def __init__(self, csvLine: str) -> QuantConnect.Data.Custom.Estimize.EstimizeConsensus:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.Estimize.EstimizeConsensus:
        pass

    Count: typing.Optional[int]

    EndTime: datetime.datetime

    FiscalQuarter: typing.Optional[int]

    FiscalYear: typing.Optional[int]

    High: typing.Optional[float]

    Id: str

    Low: typing.Optional[float]

    Mean: typing.Optional[float]

    Source: typing.Optional[QuantConnect.Data.Custom.Estimize.Source]

    StandardDeviation: typing.Optional[float]

    Type: typing.Optional[QuantConnect.Data.Custom.Estimize.Type]

    UpdatedAt: datetime.datetime

    Value: float



class EstimizeEstimate(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Financial estimates for the specified company
    
    EstimizeEstimate()
    EstimizeEstimate(csvLine: str)
    """
    def DataTimeZone(self) -> NodaTime.DateTimeZone:
        pass

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

    def RequiresMapping(self) -> bool:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.Estimize.EstimizeEstimate:
        pass

    @typing.overload
    def __init__(self, csvLine: str) -> QuantConnect.Data.Custom.Estimize.EstimizeEstimate:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.Estimize.EstimizeEstimate:
        pass

    AnalystId: str

    CreatedAt: datetime.datetime

    EndTime: datetime.datetime

    Eps: typing.Optional[float]

    FiscalQuarter: int

    FiscalYear: int

    Flagged: bool

    Id: str

    Revenue: typing.Optional[float]

    Ticker: str

    UserName: str

    Value: float



class EstimizeRelease(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Financial releases for the specified company
    
    EstimizeRelease()
    EstimizeRelease(csvLine: str)
    """
    def DataTimeZone(self) -> NodaTime.DateTimeZone:
        pass

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

    def RequiresMapping(self) -> bool:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Data.Custom.Estimize.EstimizeRelease:
        pass

    @typing.overload
    def __init__(self, csvLine: str) -> QuantConnect.Data.Custom.Estimize.EstimizeRelease:
        pass

    def __init__(self, *args) -> QuantConnect.Data.Custom.Estimize.EstimizeRelease:
        pass

    ConsensusEpsEstimate: typing.Optional[float]

    ConsensusRevenueEstimate: typing.Optional[float]

    ConsensusWeightedEpsEstimate: typing.Optional[float]

    ConsensusWeightedRevenueEstimate: typing.Optional[float]

    EndTime: datetime.datetime

    Eps: typing.Optional[float]

    FiscalQuarter: int

    FiscalYear: int

    Id: str

    ReleaseDate: datetime.datetime

    Revenue: typing.Optional[float]

    Value: float

    WallStreetEpsEstimate: typing.Optional[float]

    WallStreetRevenueEstimate: typing.Optional[float]



class Source(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Source of the Consensus
    
    enum Source, values: Estimize (1), WallStreet (0)
    """
    value__: int
    Estimize: 'Source'
    WallStreet: 'Source'


class Type(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Type of the consensus
    
    enum Type, values: Eps (0), Revenue (1)
    """
    value__: int
    Eps: 'Type'
    Revenue: 'Type'


