from .____init___1 import *
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

# no functions
# classes

class AccountType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Account type: margin or cash
    
    enum AccountType, values: Cash (1), Margin (0)
    """
    value__: int
    Cash: 'AccountType'
    Margin: 'AccountType'


class AlgorithmControl(System.object):
    """
    Wrapper for algorithm status enum to include the charting subscription.
    
    AlgorithmControl()
    """
    ChartSubscription: str
    HasSubscribers: bool
    Initialized: bool
    Status: QuantConnect.AlgorithmStatus

class AlgorithmSettings(System.object, QuantConnect.Interfaces.IAlgorithmSettings):
    """
    This class includes user settings for the algorithm which can be changed in the QuantConnect.Interfaces.IAlgorithm.Initialize method
    
    AlgorithmSettings()
    """
    DataSubscriptionLimit: int

    FreePortfolioValue: float

    FreePortfolioValuePercentage: float

    LiquidateEnabled: bool

    MaxAbsolutePortfolioTargetPercentage: float

    MinAbsolutePortfolioTargetPercentage: float

    RebalancePortfolioOnInsightChanges: typing.Optional[bool]

    RebalancePortfolioOnSecurityChanges: typing.Optional[bool]

    StalePriceTimeSpan: datetime.timedelta



class AlgorithmStatus(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    States of a live deployment.
    
    enum AlgorithmStatus, values: Completed (6), Deleted (5), DeployError (0), History (11), Initializing (10), InQueue (1), Invalid (8), Liquidated (4), LoggingIn (9), Running (2), RuntimeError (7), Stopped (3)
    """
    value__: int
    Completed: 'AlgorithmStatus'
    Deleted: 'AlgorithmStatus'
    DeployError: 'AlgorithmStatus'
    History: 'AlgorithmStatus'
    Initializing: 'AlgorithmStatus'
    InQueue: 'AlgorithmStatus'
    Invalid: 'AlgorithmStatus'
    Liquidated: 'AlgorithmStatus'
    LoggingIn: 'AlgorithmStatus'
    Running: 'AlgorithmStatus'
    RuntimeError: 'AlgorithmStatus'
    Stopped: 'AlgorithmStatus'


class AlphaRuntimeStatistics(System.object):
    """
    Contains insight population run time statistics
    
    AlphaRuntimeStatistics(accountCurrencyProvider: IAccountCurrencyProvider)
    AlphaRuntimeStatistics()
    """
    def SetDate(self, now: datetime.datetime) -> None:
        pass

    def SetStartDate(self, algorithmStartDate: datetime.datetime) -> None:
        pass

    def ToDictionary(self) -> System.Collections.Generic.Dictionary[str, str]:
        pass

    @typing.overload
    def __init__(self, accountCurrencyProvider: QuantConnect.Interfaces.IAccountCurrencyProvider) -> QuantConnect.AlphaRuntimeStatistics:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.AlphaRuntimeStatistics:
        pass

    def __init__(self, *args) -> QuantConnect.AlphaRuntimeStatistics:
        pass

    EstimatedMonthlyAlphaValue: float

    FitnessScore: float

    KellyCriterionEstimate: float

    KellyCriterionProbabilityValue: float

    LongCount: int

    LongShortRatio: float

    MeanPopulationEstimatedInsightValue: float

    MeanPopulationScore: QuantConnect.Algorithm.Framework.Alphas.InsightScore

    PortfolioTurnover: float

    ReturnOverMaxDrawdown: float

    RollingAveragedPopulationScore: QuantConnect.Algorithm.Framework.Alphas.InsightScore

    ShortCount: int

    SortinoRatio: float

    TotalAccumulatedEstimatedAlphaValue: float

    TotalInsightsAnalysisCompleted: int

    TotalInsightsClosed: int

    TotalInsightsGenerated: int



class BrokerageEnvironment(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Represents the types of environments supported by brokerages for trading
    
    enum BrokerageEnvironment, values: Live (0), Paper (1)
    """
    value__: int
    Live: 'BrokerageEnvironment'
    Paper: 'BrokerageEnvironment'


class ChannelStatus(System.object):
    """ Defines the different channel status values """
    Occupied: str
    Vacated: str
    __all__: list


class Chart(System.object):
    """
    Single Parent Chart Object for Custom Charting
    
    Chart()
    Chart(name: str, type: ChartType)
    Chart(name: str)
    """
    def AddSeries(self, series: QuantConnect.Series) -> None:
        pass

    def Clone(self) -> QuantConnect.Chart:
        pass

    def GetUpdates(self) -> QuantConnect.Chart:
        pass

    def TryAddAndGetSeries(self, name: str, type: QuantConnect.SeriesType, index: int, unit: str, color: System.Drawing.Color, symbol: QuantConnect.ScatterMarkerSymbol, forceAddNew: bool) -> QuantConnect.Series:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Chart:
        pass

    @typing.overload
    def __init__(self, name: str, type: QuantConnect.ChartType) -> QuantConnect.Chart:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Chart:
        pass

    def __init__(self, *args) -> QuantConnect.Chart:
        pass

    ChartType: QuantConnect.ChartType
    Name: str
    Series: System.Collections.Generic.Dictionary[str, QuantConnect.Series]

class ChartPoint(System.object):
    """
    Single Chart Point Value Type for QCAlgorithm.Plot();
    
    ChartPoint()
    ChartPoint(xValue: Int64, yValue: Decimal)
    ChartPoint(time: DateTime, value: Decimal)
    ChartPoint(point: ChartPoint)
    """
    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.ChartPoint:
        pass

    @typing.overload
    def __init__(self, xValue: int, yValue: float) -> QuantConnect.ChartPoint:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, value: float) -> QuantConnect.ChartPoint:
        pass

    @typing.overload
    def __init__(self, point: QuantConnect.ChartPoint) -> QuantConnect.ChartPoint:
        pass

    def __init__(self, *args) -> QuantConnect.ChartPoint:
        pass

    x: int
    y: float

class ChartType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Type of chart - should we draw the series as overlayed or stacked
    
    enum ChartType, values: Overlay (0), Stacked (1)
    """
    value__: int
    Overlay: 'ChartType'
    Stacked: 'ChartType'


class Currencies(System.object):
    """ Provides commonly used currency pairs and symbols """
    @staticmethod
    def GetCurrencySymbol(currency: str) -> str:
        pass

    CfdCurrencyPairs: List[str]
    CryptoCurrencyPairs: List[str]
    CurrencyPairs: List[str]
    CurrencySymbols: Dictionary[str, str]
    NullCurrency: str
    USD: str
    __all__: list


class DataFeedEndpoint(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Datafeed enum options for selecting the source of the datafeed.
    
    enum DataFeedEndpoint, values: Backtesting (0), Database (3), FileSystem (1), LiveTrading (2)
    """
    value__: int
    Backtesting: 'DataFeedEndpoint'
    Database: 'DataFeedEndpoint'
    FileSystem: 'DataFeedEndpoint'
    LiveTrading: 'DataFeedEndpoint'


class DataNormalizationMode(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies how data is normalized before being sent into an algorithm
    
    enum DataNormalizationMode, values: Adjusted (1), Raw (0), SplitAdjusted (2), TotalReturn (3)
    """
    value__: int
    Adjusted: 'DataNormalizationMode'
    Raw: 'DataNormalizationMode'
    SplitAdjusted: 'DataNormalizationMode'
    TotalReturn: 'DataNormalizationMode'


class DateFormat(System.object):
    """ Shortcut date format strings """
    DB: str
    EightCharacter: str
    Forex: str
    JsonFormat: str
    SixCharacter: str
    TwelveCharacter: str
    UI: str
    US: str
    USDateOnly: str
    USShort: str
    USShortDateOnly: str
    YearMonth: str
    __all__: list


class DelistingType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the type of QuantConnect.Data.Market.Delisting data
    
    enum DelistingType, values: Delisted (1), Warning (0)
    """
    value__: int
    Delisted: 'DelistingType'
    Warning: 'DelistingType'


class DownloadFailedEventArgs(System.EventArgs):
    """
    Event arguments for the QuantConnect.Interfaces.IDataProviderEvents.DownloadFailed event
    
    DownloadFailedEventArgs(message: str, stackTrace: str)
    """
    def __init__(self, message: str, stackTrace: str) -> QuantConnect.DownloadFailedEventArgs:
        pass

    Message: str

    StackTrace: str



class Expiry(System.object):
    """ Provides static functions that can be used to compute a future System.DateTime (expiry) given a System.DateTime. """
    __all__: list


class ExtendedDictionary(System.object, QuantConnect.Interfaces.IExtendedDictionary[Symbol, T]):
    # no doc
    def clear(self) -> None:
        pass

    def Clear(self) -> None:
        pass

    def copy(self) -> Python.Runtime.PyDict:
        pass

    @typing.overload
    def fromkeys(self, sequence: typing.List[QuantConnect.Symbol]) -> Python.Runtime.PyDict:
        pass

    @typing.overload
    def fromkeys(self, sequence: typing.List[QuantConnect.Symbol], value: QuantConnect.T) -> Python.Runtime.PyDict:
        pass

    def fromkeys(self, *args) -> Python.Runtime.PyDict:
        pass

    @typing.overload
    def get(self, symbol: QuantConnect.Symbol) -> QuantConnect.T:
        pass

    @typing.overload
    def get(self, symbol: QuantConnect.Symbol, value: QuantConnect.T) -> QuantConnect.T:
        pass

    def get(self, *args) -> QuantConnect.T:
        pass

    def items(self) -> Python.Runtime.PyList:
        pass

    def keys(self) -> Python.Runtime.PyList:
        pass

    @typing.overload
    def pop(self, symbol: QuantConnect.Symbol) -> QuantConnect.T:
        pass

    @typing.overload
    def pop(self, symbol: QuantConnect.Symbol, default_value: QuantConnect.T) -> QuantConnect.T:
        pass

    def pop(self, *args) -> QuantConnect.T:
        pass

    def popitem(self) -> Python.Runtime.PyTuple:
        pass

    def Remove(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    @typing.overload
    def setdefault(self, symbol: QuantConnect.Symbol) -> QuantConnect.T:
        pass

    @typing.overload
    def setdefault(self, symbol: QuantConnect.Symbol, default_value: QuantConnect.T) -> QuantConnect.T:
        pass

    def setdefault(self, *args) -> QuantConnect.T:
        pass

    def TryGetValue(self, symbol: QuantConnect.Symbol, value: QuantConnect.T) -> bool:
        pass

    def update(self, other: Python.Runtime.PyObject) -> None:
        pass

    def values(self) -> Python.Runtime.PyList:
        pass

    IsReadOnly: bool


    Item: indexer#
