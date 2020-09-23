from .____init___3 import *
import typing
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Storage
import QuantConnect.Securities.Option
import QuantConnect.Securities.Future
import QuantConnect.Securities.Forex
import QuantConnect.Securities.Equity
import QuantConnect.Securities.Crypto
import QuantConnect.Securities.Cfd
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Python
import QuantConnect.Orders
import QuantConnect.Notifications
import QuantConnect.Interfaces
import QuantConnect.Indicators.CandlestickPatterns
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Fundamental
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Benchmarks
import QuantConnect.Algorithm.Framework.Selection
import QuantConnect.Algorithm.Framework.Risk
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Execution
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import pandas
import NodaTime
import datetime


class DollarVolumeUniverseDefinitions(System.object):
    """
    Provides helpers for defining universes based on the daily dollar volume
    
    DollarVolumeUniverseDefinitions(algorithm: QCAlgorithm)
    """
    def Bottom(self, count: int, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    @typing.overload
    def Percentile(self, percentile: float, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    @typing.overload
    def Percentile(self, lowerPercentile: float, upperPercentile: float, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Percentile(self, *args) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def Top(self, count: int, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Data.UniverseSelection.Universe:
        pass

    def __init__(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> QuantConnect.Algorithm.DollarVolumeUniverseDefinitions:
        pass


class IndexUniverseDefinitions(System.object):
    """
    Provides helpers for defining universes based on index definitions
    
    IndexUniverseDefinitions(algorithm: QCAlgorithm)
    """
    def __init__(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> QuantConnect.Algorithm.IndexUniverseDefinitions:
        pass

    QC500: QuantConnect.Data.UniverseSelection.Universe



class QCAlgorithm(System.MarshalByRefObject, QuantConnect.Interfaces.IAccountCurrencyProvider, QuantConnect.Interfaces.ISecurityInitializerProvider, QuantConnect.Interfaces.IAlgorithm):
    """
    QC Algorithm Base Class - Handle the basic requirements of a trading algorithm,
                allowing user to focus on event methods. The QCAlgorithm class implements Portfolio,
                Securities, Transactions and Data Subscription Management.
    
    QCAlgorithm()
    """
    def ABANDS(self, symbol: QuantConnect.Symbol, period: int, width: float, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.AccelerationBands:
        pass

    def AD(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.AccumulationDistribution:
        pass

    @typing.overload
    def AddAlpha(self, alpha: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddAlpha(self, alpha: QuantConnect.Algorithm.Framework.Alphas.IAlphaModel) -> None:
        pass

    def AddAlpha(self, *args) -> None:
        pass

    def AddCfd(self, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], market: str, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Cfd.Cfd:
        pass

    def AddChart(self, chart: QuantConnect.Chart) -> None:
        pass

    def AddCrypto(self, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], market: str, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Crypto.Crypto:
        pass

    @typing.overload
    def AddData(self, ticker: str, resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, underlying: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, underlying: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], timeZone: NodaTime.DateTimeZone, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, underlying: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], timeZone: NodaTime.DateTimeZone, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, type: Python.Runtime.PyObject, ticker: str, resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, type: Python.Runtime.PyObject, underlying: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, type: Python.Runtime.PyObject, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], timeZone: NodaTime.DateTimeZone, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, type: Python.Runtime.PyObject, underlying: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], timeZone: NodaTime.DateTimeZone, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, dataType: type, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], timeZone: NodaTime.DateTimeZone, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddData(self, dataType: type, underlying: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], timeZone: NodaTime.DateTimeZone, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Security:
        pass

    def AddData(self, *args) -> QuantConnect.Securities.Security:
        pass

    def AddEquity(self, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], market: str, fillDataForward: bool, leverage: float, extendedMarketHours: bool) -> QuantConnect.Securities.Equity.Equity:
        pass

    def AddForex(self, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], market: str, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Forex.Forex:
        pass

    def AddFuture(self, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], market: str, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Future.Future:
        pass

    def AddFutureContract(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Future.Future:
        pass

    def AddOption(self, underlying: str, resolution: typing.Optional[QuantConnect.Resolution], market: str, fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Option.Option:
        pass

    def AddOptionContract(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, leverage: float) -> QuantConnect.Securities.Option.Option:
        pass

    @typing.overload
    def AddRiskManagement(self, riskManagement: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddRiskManagement(self, riskManagement: QuantConnect.Algorithm.Framework.Risk.IRiskManagementModel) -> None:
        pass

    def AddRiskManagement(self, *args) -> None:
        pass

    @typing.overload
    def AddSecurity(self, securityType: QuantConnect.SecurityType, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, extendedMarketHours: bool) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddSecurity(self, securityType: QuantConnect.SecurityType, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, leverage: float, extendedMarketHours: bool) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddSecurity(self, securityType: QuantConnect.SecurityType, ticker: str, resolution: typing.Optional[QuantConnect.Resolution], market: str, fillDataForward: bool, leverage: float, extendedMarketHours: bool) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def AddSecurity(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], fillDataForward: bool, leverage: float, extendedMarketHours: bool) -> QuantConnect.Securities.Security:
        pass

    def AddSecurity(self, *args) -> QuantConnect.Securities.Security:
        pass

    def AddSeries(self, chart: str, series: str, seriesType: QuantConnect.SeriesType, unit: str) -> None:
        pass

    @typing.overload
    def AddUniverse(self, pyObject: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, pyObject: Python.Runtime.PyObject, pyfine: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, resolution: QuantConnect.Resolution, pySelector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, pySelector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, pySelector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, T: Python.Runtime.PyObject, name: str, selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, T: Python.Runtime.PyObject, name: str, resolution: QuantConnect.Resolution, selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, T: Python.Runtime.PyObject, name: str, resolution: QuantConnect.Resolution, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, T: Python.Runtime.PyObject, name: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, T: Python.Runtime.PyObject, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, T: Python.Runtime.PyObject, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, dataType: type, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, pySelector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverse(self, universe: QuantConnect.Data.UniverseSelection.Universe) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[str]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[str]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, resolution: QuantConnect.Resolution, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, resolution: QuantConnect.Resolution, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[str]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, resolution: QuantConnect.Resolution, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, resolution: QuantConnect.Resolution, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[str]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[str]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: typing.Callable[[typing.List[QuantConnect.Algorithm.T]], typing.List[str]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, selector: typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, coarseSelector: typing.Callable[[typing.List[QuantConnect.Data.UniverseSelection.CoarseFundamental]], typing.List[QuantConnect.Symbol]], fineSelector: typing.Callable[[typing.List[QuantConnect.Data.Fundamental.FineFundamental]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, universe: QuantConnect.Data.UniverseSelection.Universe, fineSelector: typing.Callable[[typing.List[QuantConnect.Data.Fundamental.FineFundamental]], typing.List[QuantConnect.Symbol]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, selector: typing.Callable[[datetime.datetime], typing.List[str]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, name: str, resolution: QuantConnect.Resolution, selector: typing.Callable[[datetime.datetime], typing.List[str]]) -> None:
        pass

    @typing.overload
    def AddUniverse(self, securityType: QuantConnect.SecurityType, name: str, resolution: QuantConnect.Resolution, market: str, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings, selector: typing.Callable[[datetime.datetime], typing.List[str]]) -> None:
        pass

    def AddUniverse(self, *args) -> None:
        pass

    @typing.overload
    def AddUniverseSelection(self, universeSelection: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def AddUniverseSelection(self, universeSelection: QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel) -> None:
        pass

    def AddUniverseSelection(self, *args) -> None:
        pass

    def ADOSC(self, symbol: QuantConnect.Symbol, fastPeriod: int, slowPeriod: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.AccumulationDistributionOscillator:
        pass

    def ADR(self, symbols: typing.List[QuantConnect.Symbol], resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Indicators.AdvanceDeclineRatio:
        pass

    def ADVR(self, symbols: typing.List[QuantConnect.Symbol], resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Indicators.AdvanceDeclineVolumeRatio:
        pass

    def ADX(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.AverageDirectionalIndex:
        pass

    def ADXR(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.AverageDirectionalMovementIndexRating:
        pass

    def ALMA(self, symbol: QuantConnect.Symbol, period: int, sigma: int, offset: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.ArnaudLegouxMovingAverage:
        pass

    def APO(self, symbol: QuantConnect.Symbol, fastPeriod: int, slowPeriod: int, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.AbsolutePriceOscillator:
        pass

    @typing.overload
    def AROON(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.AroonOscillator:
        pass

    @typing.overload
    def AROON(self, symbol: QuantConnect.Symbol, upPeriod: int, downPeriod: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.AroonOscillator:
        pass

    def AROON(self, *args) -> QuantConnect.Indicators.AroonOscillator:
        pass

    def ATR(self, symbol: QuantConnect.Symbol, period: int, type: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.AverageTrueRange:
        pass

    def BB(self, symbol: QuantConnect.Symbol, period: int, k: float, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.BollingerBands:
        pass

    def BOP(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.BalanceOfPower:
        pass

    @typing.overload
    def Buy(self, symbol: QuantConnect.Symbol, quantity: int) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Buy(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Buy(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Buy(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Buy(self, strategy: QuantConnect.Securities.Option.OptionStrategy, quantity: int) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def Buy(self, *args) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    @typing.overload
    def CalculateOrderQuantity(self, symbol: QuantConnect.Symbol, target: float) -> float:
        pass

    @typing.overload
    def CalculateOrderQuantity(self, symbol: QuantConnect.Symbol, target: float) -> float:
        pass

    def CalculateOrderQuantity(self, *args) -> float:
        pass

    def CC(self, symbol: QuantConnect.Symbol, shortRocPeriod: int, longRocPeriod: int, lwmaPeriod: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.CoppockCurve:
        pass

    def CCI(self, symbol: QuantConnect.Symbol, period: int, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CommodityChannelIndex:
        pass

    def CMO(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.ChandeMomentumOscillator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: QuantConnect.Resolution, handler: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: QuantConnect.Resolution, tickType: typing.Optional[QuantConnect.TickType], handler: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: datetime.timedelta, handler: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: datetime.timedelta, tickType: typing.Optional[QuantConnect.TickType], handler: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, calendar: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo], handler: Python.Runtime.PyObject) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: QuantConnect.Resolution, handler: typing.Callable[[QuantConnect.Data.Market.TradeBar], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: datetime.timedelta, handler: typing.Callable[[QuantConnect.Data.Market.TradeBar], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: QuantConnect.Resolution, handler: typing.Callable[[QuantConnect.Data.Market.QuoteBar], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: datetime.timedelta, handler: typing.Callable[[QuantConnect.Data.Market.QuoteBar], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: datetime.timedelta, handler: typing.Callable[[QuantConnect.Algorithm.T], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: QuantConnect.Resolution, tickType: typing.Optional[QuantConnect.TickType], handler: typing.Callable[[QuantConnect.Algorithm.T], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, period: datetime.timedelta, tickType: typing.Optional[QuantConnect.TickType], handler: typing.Callable[[QuantConnect.Algorithm.T], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, calendar: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo], handler: typing.Callable[[QuantConnect.Data.Market.QuoteBar], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, calendar: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo], handler: typing.Callable[[QuantConnect.Data.Market.TradeBar], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def Consolidate(self, symbol: QuantConnect.Symbol, calendar: typing.Callable[[datetime.datetime], QuantConnect.Data.Consolidators.CalendarInfo], handler: typing.Callable[[QuantConnect.Algorithm.T], None]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    def Consolidate(self, *args) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @staticmethod
    def CreateConsolidator(period: datetime.timedelta, consolidatorInputType: type, tickType: typing.Optional[QuantConnect.TickType]) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def CreateIndicatorName(self, symbol: QuantConnect.Symbol, type: System.FormattableString, resolution: typing.Optional[QuantConnect.Resolution]) -> str:
        pass

    @typing.overload
    def CreateIndicatorName(self, symbol: QuantConnect.Symbol, type: str, resolution: typing.Optional[QuantConnect.Resolution]) -> str:
        pass

    def CreateIndicatorName(self, *args) -> str:
        pass

    @typing.overload
    def DCH(self, symbol: QuantConnect.Symbol, upperPeriod: int, lowerPeriod: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.DonchianChannel:
        pass

    @typing.overload
    def DCH(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.DonchianChannel:
        pass

    def DCH(self, *args) -> QuantConnect.Indicators.DonchianChannel:
        pass

    @typing.overload
    def Debug(self, message: str) -> None:
        pass

    @typing.overload
    def Debug(self, message: int) -> None:
        pass

    @typing.overload
    def Debug(self, message: float) -> None:
        pass

    @typing.overload
    def Debug(self, message: float) -> None:
        pass

    @typing.overload
    def Debug(self, message: Python.Runtime.PyObject) -> None:
        pass

    def Debug(self, *args) -> None:
        pass

    def DEMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.DoubleExponentialMovingAverage:
        pass

    @typing.overload
    def Download(self, address: str) -> str:
        pass

    @typing.overload
    def Download(self, address: str, headers: typing.List[System.Collections.Generic.KeyValuePair[str, str]]) -> str:
        pass

    @typing.overload
    def Download(self, address: str, headers: typing.List[System.Collections.Generic.KeyValuePair[str, str]], userName: str, password: str) -> str:
        pass

    @typing.overload
    def Download(self, address: str, headers: Python.Runtime.PyObject) -> str:
        pass

    @typing.overload
    def Download(self, address: str, headers: Python.Runtime.PyObject, userName: str, password: str) -> str:
        pass

    def Download(self, *args) -> str:
        pass

    def DPO(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.DetrendedPriceOscillator:
        pass

    @typing.overload
    def EMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    @typing.overload
    def EMA(self, symbol: QuantConnect.Symbol, period: int, smoothingFactor: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    def EMA(self, *args) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    @typing.overload
    def EmitInsights(self, insights: typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]) -> None:
        pass

    @typing.overload
    def EmitInsights(self, insight: QuantConnect.Algorithm.Framework.Alphas.Insight) -> None:
        pass

    def EmitInsights(self, *args) -> None:
        pass

    def EMV(self, symbol: QuantConnect.Symbol, period: int, scale: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.EaseOfMovementValue:
        pass

    @typing.overload
    def Error(self, message: str) -> None:
        pass

    @typing.overload
    def Error(self, message: int) -> None:
        pass

    @typing.overload
    def Error(self, message: float) -> None:
        pass

    @typing.overload
    def Error(self, message: float) -> None:
        pass

    @typing.overload
    def Error(self, error: System.Exception) -> None:
        pass

    @typing.overload
    def Error(self, message: Python.Runtime.PyObject) -> None:
        pass

    def Error(self, *args) -> None:
        pass

    def ExerciseOption(self, optionSymbol: QuantConnect.Symbol, quantity: int, asynchronous: bool, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def FilteredIdentity(self, symbol: QuantConnect.Symbol, selector: Python.Runtime.PyObject, filter: Python.Runtime.PyObject, fieldName: str) -> QuantConnect.Indicators.FilteredIdentity:
        pass

    @typing.overload
    def FilteredIdentity(self, symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, selector: Python.Runtime.PyObject, filter: Python.Runtime.PyObject, fieldName: str) -> QuantConnect.Indicators.FilteredIdentity:
        pass

    @typing.overload
    def FilteredIdentity(self, symbol: QuantConnect.Symbol, resolution: datetime.timedelta, selector: Python.Runtime.PyObject, filter: Python.Runtime.PyObject, fieldName: str) -> QuantConnect.Indicators.FilteredIdentity:
        pass

    @typing.overload
    def FilteredIdentity(self, symbol: QuantConnect.Symbol, selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar], filter: typing.Callable[[QuantConnect.Data.IBaseData], bool], fieldName: str) -> QuantConnect.Indicators.FilteredIdentity:
        pass

    @typing.overload
    def FilteredIdentity(self, symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar], filter: typing.Callable[[QuantConnect.Data.IBaseData], bool], fieldName: str) -> QuantConnect.Indicators.FilteredIdentity:
        pass

    @typing.overload
    def FilteredIdentity(self, symbol: QuantConnect.Symbol, resolution: datetime.timedelta, selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar], filter: typing.Callable[[QuantConnect.Data.IBaseData], bool], fieldName: str) -> QuantConnect.Indicators.FilteredIdentity:
        pass

    def FilteredIdentity(self, *args) -> QuantConnect.Indicators.FilteredIdentity:
        pass

    def FISH(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.FisherTransform:
        pass

    def FRAMA(self, symbol: QuantConnect.Symbol, period: int, longPeriod: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.FractalAdaptiveMovingAverage:
        pass

    def FrameworkPostInitialize(self) -> None:
        pass

    def GetChartUpdates(self, clearChartData: bool) -> typing.List[QuantConnect.Chart]:
        pass

    def GetLastKnownPrice(self, security: QuantConnect.Securities.Security) -> QuantConnect.Data.BaseData:
        pass

    def GetLocked(self) -> bool:
        pass

    def GetParameter(self, name: str) -> str:
        pass

    def GetParameters(self) -> System.Collections.Generic.IReadOnlyDictionary[str, str]:
        pass

    def GetWarmupHistoryRequests(self) -> typing.List[QuantConnect.Data.HistoryRequest]:
        pass

    def HeikinAshi(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.HeikinAshi:
        pass

    @typing.overload
    def History(self, span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, periods: int, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbols: typing.List[QuantConnect.Symbol], span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbols: typing.List[QuantConnect.Symbol], periods: int, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbols: typing.List[QuantConnect.Symbol], start: datetime.datetime, end: datetime.datetime, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbol: QuantConnect.Symbol, span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbol: QuantConnect.Symbol, periods: int, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbol: QuantConnect.Symbol, periods: int, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbol: QuantConnect.Symbol, start: datetime.datetime, end: datetime.datetime, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbol: QuantConnect.Symbol, span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbol: QuantConnect.Symbol, start: datetime.datetime, end: datetime.datetime, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbols: typing.List[QuantConnect.Symbol], span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbols: typing.List[QuantConnect.Symbol], periods: int, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, symbols: typing.List[QuantConnect.Symbol], start: datetime.datetime, end: datetime.datetime, resolution: typing.Optional[QuantConnect.Resolution], fillForward: typing.Optional[bool], extendedMarket: typing.Optional[bool]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, request: QuantConnect.Data.HistoryRequest) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, requests: typing.List[QuantConnect.Data.HistoryRequest]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, tickers: Python.Runtime.PyObject, periods: int, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, tickers: Python.Runtime.PyObject, span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, tickers: Python.Runtime.PyObject, start: datetime.datetime, end: datetime.datetime, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, type: Python.Runtime.PyObject, tickers: Python.Runtime.PyObject, start: datetime.datetime, end: datetime.datetime, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, type: Python.Runtime.PyObject, tickers: Python.Runtime.PyObject, periods: int, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, type: Python.Runtime.PyObject, tickers: Python.Runtime.PyObject, span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, type: Python.Runtime.PyObject, symbol: QuantConnect.Symbol, start: datetime.datetime, end: datetime.datetime, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, type: Python.Runtime.PyObject, symbol: QuantConnect.Symbol, periods: int, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    @typing.overload
    def History(self, type: Python.Runtime.PyObject, symbol: QuantConnect.Symbol, span: datetime.timedelta, resolution: typing.Optional[QuantConnect.Resolution]) -> pandas.DataFrame:
        pass

    def History(self, *args) -> pandas.DataFrame:
        pass

    def HMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.HullMovingAverage:
        pass

    def ICHIMOKU(self, symbol: QuantConnect.Symbol, tenkanPeriod: int, kijunPeriod: int, senkouAPeriod: int, senkouBPeriod: int, senkouADelayPeriod: int, senkouBDelayPeriod: int, resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Indicators.IchimokuKinkoHyo:
        pass

    @typing.overload
    def Identity(self, symbol: QuantConnect.Symbol, selector: typing.Callable[[QuantConnect.Data.IBaseData], float], fieldName: str) -> QuantConnect.Indicators.Identity:
        pass

    @typing.overload
    def Identity(self, symbol: QuantConnect.Symbol, resolution: QuantConnect.Resolution, selector: typing.Callable[[QuantConnect.Data.IBaseData], float], fieldName: str) -> QuantConnect.Indicators.Identity:
        pass

    @typing.overload
    def Identity(self, symbol: QuantConnect.Symbol, resolution: datetime.timedelta, selector: typing.Callable[[QuantConnect.Data.IBaseData], float], fieldName: str) -> QuantConnect.Indicators.Identity:
        pass

    def Identity(self, *args) -> QuantConnect.Indicators.Identity:
        pass

    def Initialize(self) -> None:
        pass

    def IsMarketOpen(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    @typing.overload
    def KAMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.KaufmanAdaptiveMovingAverage:
        pass

    @typing.overload
    def KAMA(self, symbol: QuantConnect.Symbol, period: int, fastEmaPeriod: int, slowEmaPeriod: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.KaufmanAdaptiveMovingAverage:
        pass

    def KAMA(self, *args) -> QuantConnect.Indicators.KaufmanAdaptiveMovingAverage:
        pass

    def KCH(self, symbol: QuantConnect.Symbol, period: int, k: float, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.KeltnerChannels:
        pass

    @typing.overload
    def LimitOrder(self, symbol: QuantConnect.Symbol, quantity: int, limitPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def LimitOrder(self, symbol: QuantConnect.Symbol, quantity: float, limitPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def LimitOrder(self, symbol: QuantConnect.Symbol, quantity: float, limitPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    def LimitOrder(self, *args) -> QuantConnect.Orders.OrderTicket:
        pass

    def Liquidate(self, symbolToLiquidate: QuantConnect.Symbol, tag: str) -> typing.List[int]:
        pass

    @typing.overload
    def Log(self, message: str) -> None:
        pass

    @typing.overload
    def Log(self, message: int) -> None:
        pass

    @typing.overload
    def Log(self, message: float) -> None:
        pass

    @typing.overload
    def Log(self, message: float) -> None:
        pass

    @typing.overload
    def Log(self, message: Python.Runtime.PyObject) -> None:
        pass

    def Log(self, *args) -> None:
        pass

    def LOGR(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.LogReturn:
        pass

    def LSMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.LeastSquaresMovingAverage:
        pass

    def LWMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.LinearWeightedMovingAverage:
        pass

    def MACD(self, symbol: QuantConnect.Symbol, fastPeriod: int, slowPeriod: int, signalPeriod: int, type: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.MovingAverageConvergenceDivergence:
        pass

    def MAD(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.MeanAbsoluteDeviation:
        pass

    @typing.overload
    def MarketOnCloseOrder(self, symbol: QuantConnect.Symbol, quantity: int, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def MarketOnCloseOrder(self, symbol: QuantConnect.Symbol, quantity: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def MarketOnCloseOrder(self, symbol: QuantConnect.Symbol, quantity: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    def MarketOnCloseOrder(self, *args) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def MarketOnOpenOrder(self, symbol: QuantConnect.Symbol, quantity: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def MarketOnOpenOrder(self, symbol: QuantConnect.Symbol, quantity: int, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def MarketOnOpenOrder(self, symbol: QuantConnect.Symbol, quantity: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    def MarketOnOpenOrder(self, *args) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def MarketOrder(self, symbol: QuantConnect.Symbol, quantity: int, asynchronous: bool, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def MarketOrder(self, symbol: QuantConnect.Symbol, quantity: float, asynchronous: bool, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def MarketOrder(self, symbol: QuantConnect.Symbol, quantity: float, asynchronous: bool, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    def MarketOrder(self, *args) -> QuantConnect.Orders.OrderTicket:
        pass

    def MASS(self, symbol: QuantConnect.Symbol, emaPeriod: int, sumPeriod: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.MassIndex:
        pass

    def MAX(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.Maximum:
        pass

    def MFI(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.MoneyFlowIndex:
        pass

    def MIDPOINT(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.MidPoint:
        pass

    def MIDPRICE(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.MidPrice:
        pass

    def MIN(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.Minimum:
        pass

    def MOM(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.Momentum:
        pass

    def MOMERSION(self, symbol: QuantConnect.Symbol, minPeriod: typing.Optional[int], fullPeriod: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.MomersionIndicator:
        pass

    def MOMP(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.MomentumPercent:
        pass

    def NATR(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.NormalizedAverageTrueRange:
        pass

    def OBV(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.OnBalanceVolume:
        pass

    def OnAssignmentOrderEvent(self, assignmentEvent: QuantConnect.Orders.OrderEvent) -> None:
        pass

    def OnBrokerageDisconnect(self) -> None:
        pass

    def OnBrokerageMessage(self, messageEvent: QuantConnect.Brokerages.BrokerageMessageEvent) -> None:
        pass

    def OnBrokerageReconnect(self) -> None:
        pass

    def OnData(self, slice: QuantConnect.Data.Slice) -> None:
        pass

    def OnEndOfAlgorithm(self) -> None:
        pass

    @typing.overload
    def OnEndOfDay(self) -> None:
        pass

    @typing.overload
    def OnEndOfDay(self, symbol: str) -> None:
        pass

    @typing.overload
    def OnEndOfDay(self, symbol: QuantConnect.Symbol) -> None:
        pass

    def OnEndOfDay(self, *args) -> None:
        pass

    def OnEndOfTimeStep(self) -> None:
        pass

    def OnFrameworkData(self, slice: QuantConnect.Data.Slice) -> None:
        pass

    def OnFrameworkSecuritiesChanged(self, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def OnMarginCall(self, requests: typing.List[QuantConnect.Orders.SubmitOrderRequest]) -> None:
        pass

    def OnMarginCallWarning(self) -> None:
        pass

    def OnOrderEvent(self, orderEvent: QuantConnect.Orders.OrderEvent) -> None:
        pass

    def OnSecuritiesChanged(self, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def OnWarmupFinished(self) -> None:
        pass

    @typing.overload
    def Order(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Order(self, symbol: QuantConnect.Symbol, quantity: int) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Order(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Order(self, symbol: QuantConnect.Symbol, quantity: float, asynchronous: bool, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Order(self, strategy: QuantConnect.Securities.Option.OptionStrategy, quantity: int) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    @typing.overload
    def Order(self, symbol: QuantConnect.Symbol, quantity: int, type: QuantConnect.Orders.OrderType, asynchronous: bool, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Order(self, symbol: QuantConnect.Symbol, quantity: float, type: QuantConnect.Orders.OrderType) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Order(self, symbol: QuantConnect.Symbol, quantity: int, type: QuantConnect.Orders.OrderType) -> QuantConnect.Orders.OrderTicket:
        pass

    def Order(self, *args) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Plot(self, series: str, pyObject: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def Plot(self, chart: str, first: QuantConnect.Indicators.Indicator, second: QuantConnect.Indicators.Indicator, third: QuantConnect.Indicators.Indicator, fourth: QuantConnect.Indicators.Indicator) -> None:
        pass

    @typing.overload
    def Plot(self, chart: str, first: QuantConnect.Indicators.BarIndicator, second: QuantConnect.Indicators.BarIndicator, third: QuantConnect.Indicators.BarIndicator, fourth: QuantConnect.Indicators.BarIndicator) -> None:
        pass

    @typing.overload
    def Plot(self, chart: str, first: QuantConnect.Indicators.TradeBarIndicator, second: QuantConnect.Indicators.TradeBarIndicator, third: QuantConnect.Indicators.TradeBarIndicator, fourth: QuantConnect.Indicators.TradeBarIndicator) -> None:
        pass

    @typing.overload
    def Plot(self, series: str, value: float) -> None:
        pass

    @typing.overload
    def Plot(self, series: str, value: float) -> None:
        pass

    @typing.overload
    def Plot(self, series: str, value: int) -> None:
        pass

    @typing.overload
    def Plot(self, series: str, value: float) -> None:
        pass

    @typing.overload
    def Plot(self, chart: str, series: str, value: float) -> None:
        pass

    @typing.overload
    def Plot(self, chart: str, series: str, value: int) -> None:
        pass

    @typing.overload
    def Plot(self, chart: str, series: str, value: float) -> None:
        pass

    @typing.overload
    def Plot(self, chart: str, series: str, value: float) -> None:
        pass

    @typing.overload
    def Plot(self, chart: str, indicators: typing.List[QuantConnect.Indicators.IndicatorBase]) -> None:
        pass

    def Plot(self, *args) -> None:
        pass

    @typing.overload
    def PlotIndicator(self, chart: str, first: Python.Runtime.PyObject, second: Python.Runtime.PyObject, third: Python.Runtime.PyObject, fourth: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def PlotIndicator(self, chart: str, waitForReady: bool, first: Python.Runtime.PyObject, second: Python.Runtime.PyObject, third: Python.Runtime.PyObject, fourth: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def PlotIndicator(self, chart: str, indicators: typing.List[QuantConnect.Indicators.IndicatorBase]) -> None:
        pass

    @typing.overload
    def PlotIndicator(self, chart: str, waitForReady: bool, indicators: typing.List[QuantConnect.Indicators.IndicatorBase]) -> None:
        pass

    def PlotIndicator(self, *args) -> None:
        pass

    def PostInitialize(self) -> None:
        pass

    def PPO(self, symbol: QuantConnect.Symbol, fastPeriod: int, slowPeriod: int, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.PercentagePriceOscillator:
        pass

    def PSAR(self, symbol: QuantConnect.Symbol, afStart: float, afIncrement: float, afMax: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.ParabolicStopAndReverse:
        pass

    @typing.overload
    def Quit(self, message: str) -> None:
        pass

    @typing.overload
    def Quit(self, message: Python.Runtime.PyObject) -> None:
        pass

    def Quit(self, *args) -> None:
        pass

    def RC(self, symbol: QuantConnect.Symbol, period: int, k: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.RegressionChannel:
        pass

    @typing.overload
    def Record(self, series: str, value: int) -> None:
        pass

    @typing.overload
    def Record(self, series: str, value: float) -> None:
        pass

    @typing.overload
    def Record(self, series: str, value: float) -> None:
        pass

    def Record(self, *args) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: Python.Runtime.PyObject, resolution: typing.Optional[QuantConnect.Resolution], selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: Python.Runtime.PyObject, resolution: typing.Optional[datetime.timedelta], selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: Python.Runtime.PyObject, pyObject: Python.Runtime.PyObject, selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: Python.Runtime.PyObject, consolidator: QuantConnect.Data.Consolidators.IDataConsolidator, selector: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint], resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint], resolution: typing.Optional[datetime.timedelta], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint], consolidator: QuantConnect.Data.Consolidators.IDataConsolidator, selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T], resolution: typing.Optional[QuantConnect.Resolution]) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T], resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Algorithm.T]) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T], resolution: typing.Optional[datetime.timedelta], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Algorithm.T]) -> None:
        pass

    @typing.overload
    def RegisterIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T], consolidator: QuantConnect.Data.Consolidators.IDataConsolidator, selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Algorithm.T]) -> None:
        pass

    def RegisterIndicator(self, *args) -> None:
        pass

    def RemoveSecurity(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    @typing.overload
    def ResolveConsolidator(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], dataType: type) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    @typing.overload
    def ResolveConsolidator(self, symbol: QuantConnect.Symbol, timeSpan: typing.Optional[datetime.timedelta], dataType: type) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    def ResolveConsolidator(self, *args) -> QuantConnect.Data.Consolidators.IDataConsolidator:
        pass

    def ROC(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.RateOfChange:
        pass

    def ROCP(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.RateOfChangePercent:
        pass

    def ROCR(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.RateOfChangeRatio:
        pass

    def RSI(self, symbol: QuantConnect.Symbol, period: int, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.RelativeStrengthIndex:
        pass

    @typing.overload
    def Sell(self, symbol: QuantConnect.Symbol, quantity: int) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Sell(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Sell(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Sell(self, symbol: QuantConnect.Symbol, quantity: float) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def Sell(self, strategy: QuantConnect.Securities.Option.OptionStrategy, quantity: int) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def Sell(self, *args) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def SetAccountCurrency(self, accountCurrency: str) -> None:
        pass

    def SetAlgorithmId(self, algorithmId: str) -> None:
        pass

    @typing.overload
    def SetAlpha(self, alpha: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def SetAlpha(self, alpha: QuantConnect.Algorithm.Framework.Alphas.IAlphaModel) -> None:
        pass

    def SetAlpha(self, *args) -> None:
        pass

    def SetApi(self, api: QuantConnect.Interfaces.IApi) -> None:
        pass

    def SetAvailableDataTypes(self, availableDataTypes: System.Collections.Generic.Dictionary[QuantConnect.SecurityType, typing.List[QuantConnect.TickType]]) -> None:
        pass

    @typing.overload
    def SetBenchmark(self, securityType: QuantConnect.SecurityType, symbol: str) -> None:
        pass

    @typing.overload
    def SetBenchmark(self, ticker: str) -> None:
        pass

    @typing.overload
    def SetBenchmark(self, symbol: QuantConnect.Symbol) -> None:
        pass

    @typing.overload
    def SetBenchmark(self, benchmark: typing.Callable[[datetime.datetime], float]) -> None:
        pass

    @typing.overload
    def SetBenchmark(self, benchmark: Python.Runtime.PyObject) -> None:
        pass

    def SetBenchmark(self, *args) -> None:
        pass

    def SetBrokerageMessageHandler(self, handler: QuantConnect.Brokerages.IBrokerageMessageHandler) -> None:
        pass

    @typing.overload
    def SetBrokerageModel(self, brokerage: QuantConnect.Brokerages.BrokerageName, accountType: QuantConnect.AccountType) -> None:
        pass

    @typing.overload
    def SetBrokerageModel(self, model: QuantConnect.Brokerages.IBrokerageModel) -> None:
        pass

    @typing.overload
    def SetBrokerageModel(self, model: Python.Runtime.PyObject) -> None:
        pass

    def SetBrokerageModel(self, *args) -> None:
        pass

    @typing.overload
    def SetCash(self, startingCash: float) -> None:
        pass

    @typing.overload
    def SetCash(self, startingCash: int) -> None:
        pass

    @typing.overload
    def SetCash(self, startingCash: float) -> None:
        pass

    @typing.overload
    def SetCash(self, symbol: str, startingCash: float, conversionRate: float) -> None:
        pass

    def SetCash(self, *args) -> None:
        pass

    def SetCurrentSlice(self, slice: QuantConnect.Data.Slice) -> None:
        pass

    def SetDateTime(self, frontier: datetime.datetime) -> None:
        pass

    @typing.overload
    def SetEndDate(self, year: int, month: int, day: int) -> None:
        pass

    @typing.overload
    def SetEndDate(self, end: datetime.datetime) -> None:
        pass

    def SetEndDate(self, *args) -> None:
        pass

    @typing.overload
    def SetExecution(self, execution: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def SetExecution(self, execution: QuantConnect.Algorithm.Framework.Execution.IExecutionModel) -> None:
        pass

    def SetExecution(self, *args) -> None:
        pass

    def SetFinishedWarmingUp(self) -> None:
        pass

    def SetFutureChainProvider(self, futureChainProvider: QuantConnect.Interfaces.IFutureChainProvider) -> None:
        pass

    def SetHistoryProvider(self, historyProvider: QuantConnect.Interfaces.IHistoryProvider) -> None:
        pass

    @typing.overload
    def SetHoldings(self, targets: typing.List[QuantConnect.Algorithm.Framework.Portfolio.PortfolioTarget], liquidateExistingHoldings: bool) -> None:
        pass

    @typing.overload
    def SetHoldings(self, symbol: QuantConnect.Symbol, percentage: float, liquidateExistingHoldings: bool) -> None:
        pass

    @typing.overload
    def SetHoldings(self, symbol: QuantConnect.Symbol, percentage: float, liquidateExistingHoldings: bool, tag: str) -> None:
        pass

    @typing.overload
    def SetHoldings(self, symbol: QuantConnect.Symbol, percentage: int, liquidateExistingHoldings: bool, tag: str) -> None:
        pass

    @typing.overload
    def SetHoldings(self, symbol: QuantConnect.Symbol, percentage: float, liquidateExistingHoldings: bool, tag: str) -> None:
        pass

    def SetHoldings(self, *args) -> None:
        pass

    def SetLiveMode(self, live: bool) -> None:
        pass

    def SetLocked(self) -> None:
        pass

    def SetMaximumOrders(self, max: int) -> None:
        pass

    def SetObjectStore(self, objectStore: QuantConnect.Interfaces.IObjectStore) -> None:
        pass

    def SetOptionChainProvider(self, optionChainProvider: QuantConnect.Interfaces.IOptionChainProvider) -> None:
        pass

    def SetPandasConverter(self) -> None:
        pass

    def SetParameters(self, parameters: System.Collections.Generic.Dictionary[str, str]) -> None:
        pass

    @typing.overload
    def SetPortfolioConstruction(self, portfolioConstruction: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def SetPortfolioConstruction(self, portfolioConstruction: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel) -> None:
        pass

    def SetPortfolioConstruction(self, *args) -> None:
        pass

    def SetQuit(self, quit: bool) -> None:
        pass

    @typing.overload
    def SetRiskManagement(self, riskManagement: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def SetRiskManagement(self, riskManagement: QuantConnect.Algorithm.Framework.Risk.IRiskManagementModel) -> None:
        pass

    def SetRiskManagement(self, *args) -> None:
        pass

    def SetRunTimeError(self, exception: System.Exception) -> None:
        pass

    @typing.overload
    def SetRuntimeStatistic(self, name: str, value: str) -> None:
        pass

    @typing.overload
    def SetRuntimeStatistic(self, name: str, value: float) -> None:
        pass

    @typing.overload
    def SetRuntimeStatistic(self, name: str, value: int) -> None:
        pass

    @typing.overload
    def SetRuntimeStatistic(self, name: str, value: float) -> None:
        pass

    def SetRuntimeStatistic(self, *args) -> None:
        pass

    @typing.overload
    def SetSecurityInitializer(self, securityInitializer: QuantConnect.Securities.ISecurityInitializer) -> None:
        pass

    @typing.overload
    def SetSecurityInitializer(self, securityInitializer: typing.Callable[[QuantConnect.Securities.Security, bool], None]) -> None:
        pass

    @typing.overload
    def SetSecurityInitializer(self, securityInitializer: typing.Callable[[QuantConnect.Securities.Security], None]) -> None:
        pass

    @typing.overload
    def SetSecurityInitializer(self, securityInitializer: Python.Runtime.PyObject) -> None:
        pass

    def SetSecurityInitializer(self, *args) -> None:
        pass

    @typing.overload
    def SetStartDate(self, year: int, month: int, day: int) -> None:
        pass

    @typing.overload
    def SetStartDate(self, start: datetime.datetime) -> None:
        pass

    def SetStartDate(self, *args) -> None:
        pass

    def SetStatus(self, status: QuantConnect.AlgorithmStatus) -> None:
        pass

    @typing.overload
    def SetTimeZone(self, timeZone: str) -> None:
        pass

    @typing.overload
    def SetTimeZone(self, timeZone: NodaTime.DateTimeZone) -> None:
        pass

    def SetTimeZone(self, *args) -> None:
        pass

    def SetTradeBuilder(self, tradeBuilder: QuantConnect.Interfaces.ITradeBuilder) -> None:
        pass

    @typing.overload
    def SetUniverseSelection(self, universeSelection: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def SetUniverseSelection(self, universeSelection: QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel) -> None:
        pass

    def SetUniverseSelection(self, *args) -> None:
        pass

    @typing.overload
    def SetWarmUp(self, timeSpan: datetime.timedelta) -> None:
        pass

    @typing.overload
    def SetWarmUp(self, timeSpan: datetime.timedelta, resolution: QuantConnect.Resolution) -> None:
        pass

    @typing.overload
    def SetWarmUp(self, barCount: int) -> None:
        pass

    @typing.overload
    def SetWarmUp(self, barCount: int, resolution: QuantConnect.Resolution) -> None:
        pass

    def SetWarmUp(self, *args) -> None:
        pass

    @typing.overload
    def SetWarmup(self, timeSpan: datetime.timedelta) -> None:
        pass

    @typing.overload
    def SetWarmup(self, timeSpan: datetime.timedelta, resolution: QuantConnect.Resolution) -> None:
        pass

    @typing.overload
    def SetWarmup(self, barCount: int) -> None:
        pass

    @typing.overload
    def SetWarmup(self, barCount: int, resolution: QuantConnect.Resolution) -> None:
        pass

    def SetWarmup(self, *args) -> None:
        pass

    def SMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.SimpleMovingAverage:
        pass

    def STC(self, symbol: QuantConnect.Symbol, cyclePeriod: int, fastPeriod: int, slowPeriod: int, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.SchaffTrendCycle:
        pass

    def STD(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.StandardDeviation:
        pass

    @typing.overload
    def STO(self, symbol: QuantConnect.Symbol, period: int, kPeriod: int, dPeriod: int, resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Indicators.Stochastic:
        pass

    @typing.overload
    def STO(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Indicators.Stochastic:
        pass

    def STO(self, *args) -> QuantConnect.Indicators.Stochastic:
        pass

    @typing.overload
    def StopLimitOrder(self, symbol: QuantConnect.Symbol, quantity: int, stopPrice: float, limitPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def StopLimitOrder(self, symbol: QuantConnect.Symbol, quantity: float, stopPrice: float, limitPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def StopLimitOrder(self, symbol: QuantConnect.Symbol, quantity: float, stopPrice: float, limitPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    def StopLimitOrder(self, *args) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def StopMarketOrder(self, symbol: QuantConnect.Symbol, quantity: int, stopPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def StopMarketOrder(self, symbol: QuantConnect.Symbol, quantity: float, stopPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    @typing.overload
    def StopMarketOrder(self, symbol: QuantConnect.Symbol, quantity: float, stopPrice: float, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    def StopMarketOrder(self, *args) -> QuantConnect.Orders.OrderTicket:
        pass

    def SUM(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.Sum:
        pass

    def SWISS(self, symbol: QuantConnect.Symbol, period: int, delta: float, tool: QuantConnect.Indicators.SwissArmyKnifeTool, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.SwissArmyKnife:
        pass

    def Symbol(self, ticker: str) -> QuantConnect.Symbol:
        pass

    def T3(self, symbol: QuantConnect.Symbol, period: int, volumeFactor: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.T3MovingAverage:
        pass

    def TEMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.TripleExponentialMovingAverage:
        pass

    def TR(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.TrueRange:
        pass

    @typing.overload
    def Train(self, trainingCode: System.Action) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def Train(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, trainingCode: System.Action) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def Train(self, trainingCode: Python.Runtime.PyObject) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    @typing.overload
    def Train(self, dateRule: QuantConnect.Scheduling.IDateRule, timeRule: QuantConnect.Scheduling.ITimeRule, trainingCode: Python.Runtime.PyObject) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def Train(self, *args) -> QuantConnect.Scheduling.ScheduledEvent:
        pass

    def TRIMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.TriangularMovingAverage:
        pass

    def TRIN(self, symbols: typing.List[QuantConnect.Symbol], resolution: typing.Optional[QuantConnect.Resolution]) -> QuantConnect.Indicators.ArmsIndex:
        pass

    def TRIX(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.Trix:
        pass

    def ULTOSC(self, symbol: QuantConnect.Symbol, period1: int, period2: int, period3: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.UltimateOscillator:
        pass

    def VAR(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.Variance:
        pass

    @typing.overload
    def VWAP(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.TradeBar]) -> QuantConnect.Indicators.VolumeWeightedAveragePriceIndicator:
        pass

    @typing.overload
    def VWAP(self, symbol: QuantConnect.Symbol) -> QuantConnect.Indicators.IntradayVwap:
        pass

    def VWAP(self, *args) -> QuantConnect.Indicators.IntradayVwap:
        pass

    @typing.overload
    def WarmUpIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint], resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]:
        pass

    @typing.overload
    def WarmUpIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint], period: datetime.timedelta, selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]:
        pass

    @typing.overload
    def WarmUpIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T], resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Algorithm.T]) -> QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T]:
        pass

    @typing.overload
    def WarmUpIndicator(self, symbol: QuantConnect.Symbol, indicator: QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T], period: datetime.timedelta, selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Algorithm.T]) -> QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T]:
        pass

    def WarmUpIndicator(self, *args) -> QuantConnect.Indicators.IndicatorBase[QuantConnect.Algorithm.T]:
        pass

    def WILR(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.WilliamsPercentR:
        pass

    def WWMA(self, symbol: QuantConnect.Symbol, period: int, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], float]) -> QuantConnect.Indicators.WilderMovingAverage:
        pass

    AccountCurrency: str

    ActiveSecurities: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.Symbol, QuantConnect.Securities.Security]

    AlgorithmId: str

    Alpha: QuantConnect.Algorithm.Framework.Alphas.IAlphaModel

    Benchmark: QuantConnect.Benchmarks.IBenchmark

    BrokerageMessageHandler: QuantConnect.Brokerages.IBrokerageMessageHandler

    BrokerageModel: QuantConnect.Brokerages.IBrokerageModel

    CandlestickPatterns: QuantConnect.Algorithm.CandlestickPatterns

    CurrentSlice: QuantConnect.Data.Slice

    DateRules: QuantConnect.Scheduling.DateRules

    DebugMessages: System.Collections.Concurrent.ConcurrentQueue[str]

    DebugMode: bool

    DefaultOrderProperties: QuantConnect.Interfaces.IOrderProperties

    EnableAutomaticIndicatorWarmUp: bool

    EndDate: datetime.datetime

    ErrorMessages: System.Collections.Concurrent.ConcurrentQueue[str]

    Execution: QuantConnect.Algorithm.Framework.Execution.IExecutionModel

    FutureChainProvider: QuantConnect.Interfaces.IFutureChainProvider

    HistoryProvider: QuantConnect.Interfaces.IHistoryProvider

    IsWarmingUp: bool

    LiveMode: bool

    LogMessages: System.Collections.Concurrent.ConcurrentQueue[str]

    Name: str

    Notify: QuantConnect.Notifications.NotificationManager

    ObjectStore: QuantConnect.Storage.ObjectStore

    OptionChainProvider: QuantConnect.Interfaces.IOptionChainProvider

    PandasConverter: QuantConnect.Python.PandasConverter

    Portfolio: QuantConnect.Securities.SecurityPortfolioManager

    PortfolioConstruction: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioConstructionModel

    RiskManagement: QuantConnect.Algorithm.Framework.Risk.IRiskManagementModel

    RunTimeError: System.Exception

    RuntimeStatistics: System.Collections.Concurrent.ConcurrentDictionary[str, str]

    Schedule: QuantConnect.Scheduling.ScheduleManager

    Securities: QuantConnect.Securities.SecurityManager

    SecurityInitializer: QuantConnect.Securities.ISecurityInitializer

    Settings: QuantConnect.Interfaces.IAlgorithmSettings

    StartDate: datetime.datetime

    Status: QuantConnect.AlgorithmStatus

    SubscriptionManager: QuantConnect.Data.SubscriptionManager

    Time: datetime.datetime

    TimeKeeper: QuantConnect.Interfaces.ITimeKeeper

    TimeRules: QuantConnect.Scheduling.TimeRules

    TimeZone: NodaTime.DateTimeZone

    TradeBuilder: QuantConnect.Interfaces.ITradeBuilder

    TradingCalendar: QuantConnect.TradingCalendar

    Transactions: QuantConnect.Securities.SecurityTransactionManager

    Universe: QuantConnect.Algorithm.UniverseDefinitions

    UniverseManager: QuantConnect.Securities.UniverseManager

    UniverseSelection: QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel

    UniverseSettings: QuantConnect.Data.UniverseSelection.UniverseSettings

    UtcTime: datetime.datetime


    InsightsGenerated: BoundEvent
