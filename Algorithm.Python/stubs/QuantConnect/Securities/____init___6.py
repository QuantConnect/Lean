from .____init___7 import *
import typing
import System.Linq.Expressions
import System.Dynamic
import System.Collections.Generic
import System.Collections.Concurrent
import System.Collections
import System
import QuantConnect.Securities.Interfaces
import QuantConnect.Securities
import QuantConnect.Orders.Slippage
import QuantConnect.Orders.Fills
import QuantConnect.Orders.Fees
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect
import Python.Runtime
import NodaTime
import datetime


class SecurityDataFilter(System.object, QuantConnect.Securities.Interfaces.ISecurityDataFilter):
    """
    Base class implementation for packet by packet data filtering mechanism to dynamically detect bad ticks.
    
    SecurityDataFilter()
    """
    def Filter(self, vehicle: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> bool:
        pass


class SecurityExchange(System.object):
    """
    Base exchange class providing information and helper tools for reading the current exchange situation
    
    SecurityExchange(exchangeHours: SecurityExchangeHours)
    """
    def DateIsOpen(self, dateToCheck: datetime.datetime) -> bool:
        pass

    def DateTimeIsOpen(self, dateTime: datetime.datetime) -> bool:
        pass

    def IsClosingSoon(self, minutesToClose: int) -> bool:
        pass

    def IsOpenDuringBar(self, barStartTime: datetime.datetime, barEndTime: datetime.datetime, isExtendedMarketHours: bool) -> bool:
        pass

    def SetLocalDateTimeFrontier(self, newLocalTime: datetime.datetime) -> None:
        pass

    def SetMarketHours(self, marketHoursSegments: typing.List[QuantConnect.Securities.MarketHoursSegment], days: typing.List[System.DayOfWeek]) -> None:
        pass

    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Securities.SecurityExchange:
        pass

    ClosingSoon: bool

    ExchangeOpen: bool

    Hours: QuantConnect.Securities.SecurityExchangeHours

    LocalTime: datetime.datetime

    TimeZone: NodaTime.DateTimeZone

    TradingDaysPerYear: int



class SecurityExchangeHours(System.object):
    """
    Represents the schedule of a security exchange. This includes daily regular and extended market hours
                as well as holidays, early closes and late opens.
    
    SecurityExchangeHours(timeZone: DateTimeZone, holidayDates: IEnumerable[DateTime], marketHoursForEachDayOfWeek: IReadOnlyDictionary[DayOfWeek, LocalMarketHours], earlyCloses: IReadOnlyDictionary[DateTime, TimeSpan], lateOpens: IReadOnlyDictionary[DateTime, TimeSpan])
    """
    @staticmethod
    def AlwaysOpen(timeZone: NodaTime.DateTimeZone) -> QuantConnect.Securities.SecurityExchangeHours:
        pass

    def GetMarketHours(self, localDateTime: datetime.datetime) -> QuantConnect.Securities.LocalMarketHours:
        pass

    def GetNextMarketClose(self, localDateTime: datetime.datetime, extendedMarket: bool) -> datetime.datetime:
        pass

    def GetNextMarketOpen(self, localDateTime: datetime.datetime, extendedMarket: bool) -> datetime.datetime:
        pass

    def GetNextTradingDay(self, date: datetime.datetime) -> datetime.datetime:
        pass

    def GetPreviousTradingDay(self, localDate: datetime.datetime) -> datetime.datetime:
        pass

    def IsDateOpen(self, localDateTime: datetime.datetime) -> bool:
        pass

    @typing.overload
    def IsOpen(self, localDateTime: datetime.datetime, extendedMarket: bool) -> bool:
        pass

    @typing.overload
    def IsOpen(self, startLocalDateTime: datetime.datetime, endLocalDateTime: datetime.datetime, extendedMarket: bool) -> bool:
        pass

    def IsOpen(self, *args) -> bool:
        pass

    def __init__(self, timeZone: NodaTime.DateTimeZone, holidayDates: typing.List[datetime.datetime], marketHoursForEachDayOfWeek: System.Collections.Generic.IReadOnlyDictionary[System.DayOfWeek, QuantConnect.Securities.LocalMarketHours], earlyCloses: System.Collections.Generic.IReadOnlyDictionary[datetime.datetime, datetime.timedelta], lateOpens: System.Collections.Generic.IReadOnlyDictionary[datetime.datetime, datetime.timedelta]) -> QuantConnect.Securities.SecurityExchangeHours:
        pass

    EarlyCloses: System.Collections.Generic.IReadOnlyDictionary[datetime.datetime, datetime.timedelta]

    Holidays: System.Collections.Generic.HashSet[datetime.datetime]

    LateOpens: System.Collections.Generic.IReadOnlyDictionary[datetime.datetime, datetime.timedelta]

    MarketHours: System.Collections.Generic.IReadOnlyDictionary[System.DayOfWeek, QuantConnect.Securities.LocalMarketHours]

    RegularMarketDuration: datetime.timedelta

    TimeZone: NodaTime.DateTimeZone



class SecurityHolding(System.object):
    """
    SecurityHolding is a base class for purchasing and holding a market item which manages the asset portfolio
    
    SecurityHolding(security: Security, currencyConverter: ICurrencyConverter)
    """
    def AddNewFee(self, newFee: float) -> None:
        pass

    def AddNewProfit(self, profitLoss: float) -> None:
        pass

    def AddNewSale(self, saleValue: float) -> None:
        pass

    @typing.overload
    def SetHoldings(self, averagePrice: float, quantity: int) -> None:
        pass

    @typing.overload
    def SetHoldings(self, averagePrice: float, quantity: float) -> None:
        pass

    def SetHoldings(self, *args) -> None:
        pass

    def SetLastTradeProfit(self, lastTradeProfit: float) -> None:
        pass

    def TotalCloseProfit(self) -> float:
        pass

    def UpdateMarketPrice(self, closingPrice: float) -> None:
        pass

    def __init__(self, security: QuantConnect.Securities.Security, currencyConverter: QuantConnect.Securities.ICurrencyConverter) -> QuantConnect.Securities.SecurityHolding:
        pass

    AbsoluteHoldingsCost: float

    AbsoluteHoldingsValue: float

    AbsoluteQuantity: float

    AveragePrice: float

    HoldingsCost: float

    HoldingsValue: float

    HoldStock: bool

    Invested: bool

    IsLong: bool

    IsShort: bool

    LastTradeProfit: float

    Leverage: float

    NetProfit: float

    Price: float

    Profit: float

    Quantity: float

    Symbol: QuantConnect.Symbol

    Target: QuantConnect.Algorithm.Framework.Portfolio.IPortfolioTarget

    TotalFees: float

    TotalSaleVolume: float

    Type: QuantConnect.SecurityType

    UnleveredAbsoluteHoldingsCost: float

    UnleveredHoldingsCost: float

    UnrealizedProfit: float

    UnrealizedProfitPercent: float



class SecurityInitializer(System.object):
    """ Provides static access to the QuantConnect.Securities.SecurityInitializer.Null security initializer """
    Null: NullSecurityInitializer
    __all__: list


class SecurityManager(QuantConnect.ExtendedDictionary[Security], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, Security], System.Collections.Generic.ICollection[KeyValuePair[Symbol, Security]], System.Collections.Generic.IDictionary[Symbol, Security], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, Security]], System.Collections.Specialized.INotifyCollectionChanged):
    """
    Enumerable security management class for grouping security objects into an array and providing any common properties.
    
    SecurityManager(timeKeeper: ITimeKeeper)
    """
    @typing.overload
    def Add(self, symbol: QuantConnect.Symbol, security: QuantConnect.Securities.Security) -> None:
        pass

    @typing.overload
    def Add(self, security: QuantConnect.Securities.Security) -> None:
        pass

    @typing.overload
    def Add(self, pair: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Securities.Security]) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def Clear(self) -> None:
        pass

    def Contains(self, pair: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Securities.Security]) -> bool:
        pass

    def ContainsKey(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def CopyTo(self, array: typing.List[System.Collections.Generic.KeyValuePair], number: int) -> None:
        pass

    @typing.overload
    def CreateSecurity(self, symbol: QuantConnect.Symbol, subscriptionDataConfigList: typing.List[QuantConnect.Data.SubscriptionDataConfig], leverage: float, addToSymbolCache: bool) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def CreateSecurity(self, symbol: QuantConnect.Symbol, subscriptionDataConfig: QuantConnect.Data.SubscriptionDataConfig, leverage: float, addToSymbolCache: bool) -> QuantConnect.Securities.Security:
        pass

    def CreateSecurity(self, *args) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def Remove(self, pair: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Securities.Security]) -> bool:
        pass

    @typing.overload
    def Remove(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def Remove(self, *args) -> bool:
        pass

    def SetLiveMode(self, isLiveMode: bool) -> None:
        pass

    def SetSecurityService(self, securityService: QuantConnect.Securities.SecurityService) -> None:
        pass

    def TryGetValue(self, symbol: QuantConnect.Symbol, security: QuantConnect.Securities.Security) -> bool:
        pass

    def __init__(self, timeKeeper: QuantConnect.Interfaces.ITimeKeeper) -> QuantConnect.Securities.SecurityManager:
        pass

    Count: int

    IsReadOnly: bool

    Keys: typing.List[QuantConnect.Symbol]

    UtcTime: datetime.datetime

    Values: typing.List[QuantConnect.Securities.Security]


    CollectionChanged: BoundEvent
    Item: indexer#


class SecurityPortfolioManager(QuantConnect.ExtendedDictionary[SecurityHolding], System.Collections.IEnumerable, QuantConnect.Interfaces.IExtendedDictionary[Symbol, SecurityHolding], System.Collections.Generic.ICollection[KeyValuePair[Symbol, SecurityHolding]], System.Collections.Generic.IDictionary[Symbol, SecurityHolding], QuantConnect.Securities.ISecurityProvider, System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, SecurityHolding]]):
    """
    Portfolio manager class groups popular properties and makes them accessible through one interface.
                It also provide indexing by the vehicle symbol to get the Security.Holding objects.
    
    SecurityPortfolioManager(securityManager: SecurityManager, transactions: SecurityTransactionManager, defaultOrderProperties: IOrderProperties)
    """
    @typing.overload
    def Add(self, symbol: QuantConnect.Symbol, holding: QuantConnect.Securities.SecurityHolding) -> None:
        pass

    @typing.overload
    def Add(self, pair: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Securities.SecurityHolding]) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def AddTransactionRecord(self, time: datetime.datetime, transactionProfitLoss: float) -> None:
        pass

    def AddUnsettledCashAmount(self, item: QuantConnect.Securities.UnsettledCashAmount) -> None:
        pass

    def ApplyDividend(self, dividend: QuantConnect.Data.Market.Dividend, liveMode: bool, mode: QuantConnect.DataNormalizationMode) -> None:
        pass

    def ApplySplit(self, split: QuantConnect.Data.Market.Split, liveMode: bool, mode: QuantConnect.DataNormalizationMode) -> None:
        pass

    def Clear(self) -> None:
        pass

    def Contains(self, pair: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Securities.SecurityHolding]) -> bool:
        pass

    def ContainsKey(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def CopyTo(self, array: typing.List[System.Collections.Generic.KeyValuePair], index: int) -> None:
        pass

    def GetBuyingPower(self, symbol: QuantConnect.Symbol, direction: QuantConnect.Orders.OrderDirection) -> float:
        pass

    @typing.overload
    def GetMarginRemaining(self, totalPortfolioValue: float) -> float:
        pass

    @typing.overload
    def GetMarginRemaining(self, symbol: QuantConnect.Symbol, direction: QuantConnect.Orders.OrderDirection) -> float:
        pass

    def GetMarginRemaining(self, *args) -> float:
        pass

    def InvalidateTotalPortfolioValue(self) -> None:
        pass

    def LogMarginInformation(self, orderRequest: QuantConnect.Orders.OrderRequest) -> None:
        pass

    def ProcessFill(self, fill: QuantConnect.Orders.OrderEvent) -> None:
        pass

    @typing.overload
    def Remove(self, pair: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Securities.SecurityHolding]) -> bool:
        pass

    @typing.overload
    def Remove(self, symbol: QuantConnect.Symbol) -> bool:
        pass

    def Remove(self, *args) -> bool:
        pass

    def ScanForCashSettlement(self, timeUtc: datetime.datetime) -> None:
        pass

    def SetAccountCurrency(self, accountCurrency: str) -> None:
        pass

    @typing.overload
    def SetCash(self, cash: float) -> None:
        pass

    @typing.overload
    def SetCash(self, symbol: str, cash: float, conversionRate: float) -> None:
        pass

    def SetCash(self, *args) -> None:
        pass

    @typing.overload
    def SetMarginCallModel(self, marginCallModel: QuantConnect.Securities.IMarginCallModel) -> None:
        pass

    @typing.overload
    def SetMarginCallModel(self, pyObject: Python.Runtime.PyObject) -> None:
        pass

    def SetMarginCallModel(self, *args) -> None:
        pass

    def TryGetValue(self, symbol: QuantConnect.Symbol, holding: QuantConnect.Securities.SecurityHolding) -> bool:
        pass

    def __init__(self, securityManager: QuantConnect.Securities.SecurityManager, transactions: QuantConnect.Securities.SecurityTransactionManager, defaultOrderProperties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Securities.SecurityPortfolioManager:
        pass

    Cash: float

    CashBook: QuantConnect.Securities.CashBook

    Count: int

    HoldStock: bool

    Invested: bool

    IsReadOnly: bool

    Keys: typing.List[QuantConnect.Symbol]

    MarginCallModel: QuantConnect.Securities.IMarginCallModel

    MarginRemaining: float

    TotalAbsoluteHoldingsCost: float

    TotalFees: float

    TotalHoldingsValue: float

    TotalMarginUsed: float

    TotalPortfolioValue: float

    TotalProfit: float

    TotalSaleVolume: float

    TotalUnleveredAbsoluteHoldingsCost: float

    TotalUnrealisedProfit: float

    TotalUnrealizedProfit: float

    UnsettledCash: float

    UnsettledCashBook: QuantConnect.Securities.CashBook

    Values: typing.List[QuantConnect.Securities.SecurityHolding]

    Securities: QuantConnect.Securities.SecurityManager
    Transactions: QuantConnect.Securities.SecurityTransactionManager

    Item: indexer#
