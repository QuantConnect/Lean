from .__Selection_3 import *
import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect.Algorithm.Framework.Selection
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import NodaTime
import datetime


class MetalsETFUniverse(QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Universe Selection Model that adds the following Metals ETFs at their inception date
                2004-11-18   GLD    SPDR Gold Trust
                2005-01-28   IAU    iShares Gold Trust
                2006-04-28   SLV    iShares Silver Trust
                2006-05-22   GDX    VanEck Vectors Gold Miners ETF
                2008-12-04   AGQ    ProShares Ultra Silver
                2009-11-11   GDXJ   VanEck Vectors Junior Gold Miners ETF
                2010-01-08   PPLT   Aberdeen Standard Platinum Shares ETF
                2010-12-08   NUGT   Direxion Daily Gold Miners Bull 3X Shares
                2010-12-08   DUST   Direxion Daily Gold Miners Bear 3X Shares
                2011-10-17   USLV   VelocityShares 3x Long Silver ETN
                2011-10-17   UGLD   VelocityShares 3x Long Gold ETN
                2013-10-03   JNUG   Direxion Daily Junior Gold Miners Index Bull 3x Shares
                2013-10-03   JDST   Direxion Daily Junior Gold Miners Index Bear 3X Shares
    
    MetalsETFUniverse()
    """

class NullUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides a null implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel
    
    NullUniverseSelectionModel()
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass


class OpenInterestFutureUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.FutureUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Selects contracts in a futures universe, sorted by open interest.  This allows the selection to identifiy current
                    active contract.
    
    OpenInterestFutureUniverseSelectionModel(algorithm: IAlgorithm, futureChainSymbolSelector: Func[DateTime, IEnumerable[Symbol]], chainContractsLookupLimit: Nullable[int], resultsLimit: Nullable[int])
    """
    def FilterByOpenInterest(self, contracts: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.Symbol, QuantConnect.Securities.SecurityExchangeHours]) -> typing.List[QuantConnect.Symbol]:
        pass

    def __init__(self, algorithm: QuantConnect.Interfaces.IAlgorithm, futureChainSymbolSelector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], chainContractsLookupLimit: typing.Optional[int], resultsLimit: typing.Optional[int]) -> QuantConnect.Algorithm.Framework.Selection.OpenInterestFutureUniverseSelectionModel:
        pass


class OptionUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel that subscribes to option chains
    
    OptionUniverseSelectionModel(refreshInterval: TimeSpan, optionChainSymbolSelector: Func[DateTime, IEnumerable[Symbol]])
    OptionUniverseSelectionModel(refreshInterval: TimeSpan, optionChainSymbolSelector: Func[DateTime, IEnumerable[Symbol]], universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    OptionUniverseSelectionModel(refreshInterval: TimeSpan, optionChainSymbolSelector: Func[DateTime, IEnumerable[Symbol]], universeSettings: UniverseSettings)
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def GetNextRefreshTimeUtc(self) -> datetime.datetime:
        pass

    @typing.overload
    def __init__(self, refreshInterval: datetime.timedelta, optionChainSymbolSelector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Algorithm.Framework.Selection.OptionUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, refreshInterval: datetime.timedelta, optionChainSymbolSelector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.OptionUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, refreshInterval: datetime.timedelta, optionChainSymbolSelector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Algorithm.Framework.Selection.OptionUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.OptionUniverseSelectionModel:
        pass


class QC500UniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.FundamentalUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Defines the QC500 universe as a universe selection model for framework algorithm
                For details: https://github.com/QuantConnect/Lean/pull/1663
    
    QC500UniverseSelectionModel()
    QC500UniverseSelectionModel(universeSettings: UniverseSettings, securityInitializer: ISecurityInitializer)
    """
    def SelectCoarse(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, coarse: typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]) -> typing.List[QuantConnect.Symbol]:
        pass

    def SelectFine(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, fine: typing.List[QuantConnect.Data.Fundamental.FineFundamental]) -> typing.List[QuantConnect.Symbol]:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Algorithm.Framework.Selection.QC500UniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.QC500UniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.QC500UniverseSelectionModel:
        pass


class ScheduledUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Defines a universe selection model that invokes a selector function on a specific scheduled given by an QuantConnect.Scheduling.IDateRule and an QuantConnect.Scheduling.ITimeRule
    
    ScheduledUniverseSelectionModel(dateRule: IDateRule, timeRule: ITimeRule, selector: Func[DateTime, IEnumerable[Symbol]], settings: UniverseSettings, initializer: ISecurityInitializer)
    ScheduledUniverseSelectionModel(timeZone: DateTimeZone, dateRule: IDateRule, timeRule: ITimeRule, selector: Func[DateTime, IEnumerable[Symbol]], settings: UniverseSettings, initializer: ISecurityInitializer)
    ScheduledUniverseSelectionModel(dateRule: IDateRule, timeRule: ITimeRule, selector: PyObject, settings: UniverseSettings, initializer: ISecurityInitializer)
    ScheduledUniverseSelectionModel(timeZone: DateTimeZone, dateRule: IDateRule, timeRule: ITimeRule, selector: PyObject, settings: UniverseSettings, initializer: ISecurityInitializer)
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    @typing.overload
    def __init__(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, selector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], settings: QuantConnect.Data.UniverseSelection.UniverseSettings, initializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.ScheduledUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, timeZone: NodaTime.DateTimeZone, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, selector: typing.Callable[[datetime.datetime], typing.List[QuantConnect.Symbol]], settings: QuantConnect.Data.UniverseSelection.UniverseSettings, initializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.ScheduledUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, selector: Python.Runtime.PyObject, settings: QuantConnect.Data.UniverseSelection.UniverseSettings, initializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.ScheduledUniverseSelectionModel:
        pass

    @typing.overload
    def __init__(self, timeZone: NodaTime.DateTimeZone, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, selector: Python.Runtime.PyObject, settings: QuantConnect.Data.UniverseSelection.UniverseSettings, initializer: QuantConnect.Securities.ISecurityInitializer) -> QuantConnect.Algorithm.Framework.Selection.ScheduledUniverseSelectionModel:
        pass

    def __init__(self, *args) -> QuantConnect.Algorithm.Framework.Selection.ScheduledUniverseSelectionModel:
        pass


class SP500SectorsETFUniverse(QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Universe Selection Model that adds the following SP500 Sectors ETFs at their inception date
                1998-12-22   XLB   Materials Select Sector SPDR ETF
                1998-12-22   XLE   Energy Select Sector SPDR Fund
                1998-12-22   XLF   Financial Select Sector SPDR Fund
                1998-12-22   XLI   Industrial Select Sector SPDR Fund
                1998-12-22   XLK   Technology Select Sector SPDR Fund
                1998-12-22   XLP   Consumer Staples Select Sector SPDR Fund
                1998-12-22   XLU   Utilities Select Sector SPDR Fund
                1998-12-22   XLV   Health Care Select Sector SPDR Fund
                1998-12-22   XLY   Consumer Discretionary Select Sector SPDR Fund
    
    SP500SectorsETFUniverse()
    """
