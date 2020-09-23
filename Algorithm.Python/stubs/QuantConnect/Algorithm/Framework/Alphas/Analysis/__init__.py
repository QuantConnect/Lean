import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Algorithm.Framework.Alphas.Analysis
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect
import datetime

# no functions
# classes

class IInsightManager(System.IDisposable):
    """ Encapsulates the storage and on-line scoring of insights. """
    def AddExtension(self, extension: QuantConnect.Algorithm.Framework.Alphas.IInsightManagerExtension) -> None:
        pass

    def InitializeExtensionsForRange(self, start: datetime.datetime, end: datetime.datetime, current: datetime.datetime) -> None:
        pass

    def RemoveInsights(self, insightIds: typing.List[System.Guid]) -> None:
        pass

    def Step(self, frontierTimeUtc: datetime.datetime, securityValuesCollection: QuantConnect.Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection, generatedInsights: QuantConnect.Algorithm.Framework.Alphas.GeneratedInsightsCollection) -> None:
        pass

    AllInsights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]

    ClosedInsights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]

    OpenInsights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]



class IInsightScoreFunction:
    """
    Defines a function used to determine how correct a particular insight is.
                The result of calling QuantConnect.Algorithm.Framework.Alphas.Analysis.IInsightScoreFunction.Evaluate(QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext,QuantConnect.Algorithm.Framework.Alphas.InsightScoreType) is expected to be within the range [0, 1]
                where 0 is completely wrong and 1 is completely right
    """
    def Evaluate(self, context: QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext, scoreType: QuantConnect.Algorithm.Framework.Alphas.InsightScoreType) -> float:
        pass


class IInsightScoreFunctionProvider:
    """ Retrieves the registered scoring function for the specified insight/score type """
    def GetScoreFunction(self, insightType: QuantConnect.Algorithm.Framework.Alphas.InsightType, scoreType: QuantConnect.Algorithm.Framework.Alphas.InsightScoreType) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.IInsightScoreFunction:
        pass


class InsightAnalysisContext(System.object):
    """
    Defines a context for performing analysis on a single insight
    
    InsightAnalysisContext(insight: Insight, initialValues: SecurityValues, analysisPeriod: TimeSpan)
    """
    def Equals(self, obj: object) -> bool:
        pass

    def Get(self, key: str) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.T:
        pass

    def GetHashCode(self) -> int:
        pass

    def Set(self, key: str, value: object) -> None:
        pass

    def ShouldAnalyze(self, scoreType: QuantConnect.Algorithm.Framework.Alphas.InsightScoreType) -> bool:
        pass

    def ToString(self) -> str:
        pass

    def __init__(self, insight: QuantConnect.Algorithm.Framework.Alphas.Insight, initialValues: QuantConnect.Algorithm.Framework.Alphas.Analysis.SecurityValues, analysisPeriod: datetime.timedelta) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext:
        pass

    AnalysisEndTimeUtc: datetime.datetime

    CurrentValues: QuantConnect.Algorithm.Framework.Alphas.Analysis.SecurityValues

    Id: System.Guid

    InitialValues: QuantConnect.Algorithm.Framework.Alphas.Analysis.SecurityValues

    Insight: QuantConnect.Algorithm.Framework.Alphas.Insight

    InsightPeriodClosed: bool

    NormalizedTime: float

    NormalizedTimeStep: float

    Score: QuantConnect.Algorithm.Framework.Alphas.InsightScore

    Symbol: QuantConnect.Symbol



class InsightManager(System.object, System.IDisposable, QuantConnect.Algorithm.Framework.Alphas.Analysis.IInsightManager):
    """
    Encapsulates the storage and on-line scoring of insights.
    
    InsightManager(scoreFunctionProvider: IInsightScoreFunctionProvider, extraAnalysisPeriodRatio: float, *extensions: Array[IInsightManagerExtension])
    """
    def AddExtension(self, extension: QuantConnect.Algorithm.Framework.Alphas.IInsightManagerExtension) -> None:
        pass

    def ContextsOpenAt(self, frontierTimeUtc: datetime.datetime) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext]:
        pass

    def Dispose(self) -> None:
        pass

    def GetUpdatedContexts(self) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext]:
        pass

    def InitializeExtensionsForRange(self, start: datetime.datetime, end: datetime.datetime, current: datetime.datetime) -> None:
        pass

    def RemoveInsights(self, insightIds: typing.List[System.Guid]) -> None:
        pass

    def Step(self, frontierTimeUtc: datetime.datetime, securityValuesCollection: QuantConnect.Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection, generatedInsights: QuantConnect.Algorithm.Framework.Alphas.GeneratedInsightsCollection) -> None:
        pass

    def __init__(self, scoreFunctionProvider: QuantConnect.Algorithm.Framework.Alphas.Analysis.IInsightScoreFunctionProvider, extraAnalysisPeriodRatio: float, extensions: typing.List[QuantConnect.Algorithm.Framework.Alphas.IInsightManagerExtension]) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightManager:
        pass

    AllInsights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]

    ClosedInsights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]

    OpenInsights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]


    ScoreTypes: Array[InsightScoreType]


class ISecurityValuesProvider:
    """
    Provides a simple abstraction that returns a security's current price and volatility.
                This facilitates testing by removing the dependency of IAlgorithm on the analysis components
    """
    def GetAllValues(self) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection:
        pass

    def GetValues(self, symbol: QuantConnect.Symbol) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.SecurityValues:
        pass


class ReadOnlySecurityValuesCollection(System.object):
    """
    Defines the security values at a given instant. This is analagous
                to TimeSlice/Slice, but decoupled from the algorithm thread and is
                intended to contain all of the information necessary to score all
                insight at this particular time step
    
    ReadOnlySecurityValuesCollection(securityValuesBySymbol: Dictionary[Symbol, SecurityValues])
    ReadOnlySecurityValuesCollection(securityValuesBySymbolFunc: Func[Symbol, SecurityValues])
    """
    @typing.overload
    def __init__(self, securityValuesBySymbol: System.Collections.Generic.Dictionary[QuantConnect.Symbol, QuantConnect.Algorithm.Framework.Alphas.Analysis.SecurityValues]) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection:
        pass

    @typing.overload
    def __init__(self, securityValuesBySymbolFunc: typing.Callable[[QuantConnect.Symbol], QuantConnect.Algorithm.Framework.Alphas.Analysis.SecurityValues]) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection:
        pass

    Item: indexer#


class SecurityValues(System.object):
    """
    Contains security values required by insight analysis components
    
    SecurityValues(symbol: Symbol, timeUtc: DateTime, exchangeHours: SecurityExchangeHours, price: Decimal, volatility: Decimal, volume: Decimal, quoteCurrencyConversionRate: Decimal)
    """
    def Get(self, type: QuantConnect.Algorithm.Framework.Alphas.InsightType) -> float:
        pass

    def __init__(self, symbol: QuantConnect.Symbol, timeUtc: datetime.datetime, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, price: float, volatility: float, volume: float, quoteCurrencyConversionRate: float) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.SecurityValues:
        pass

    ExchangeHours: QuantConnect.Securities.SecurityExchangeHours

    Price: float

    QuoteCurrencyConversionRate: float

    Symbol: QuantConnect.Symbol

    TimeUtc: datetime.datetime

    Volatility: float

    Volume: float



class SecurityValuesProviderExtensions(System.object):
    """ Provides extension methods for QuantConnect.Algorithm.Framework.Alphas.Analysis.ISecurityValuesProvider """
    @staticmethod
    def GetValues(securityValuesProvider: QuantConnect.Algorithm.Framework.Alphas.Analysis.ISecurityValuesProvider, symbols: typing.List[QuantConnect.Symbol]) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection:
        pass

    __all__: list
