from .____init___4 import *
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



class InitialMarginRequiredForOrderParameters(System.object):
    """
    Defines the parameters for QuantConnect.Securities.BuyingPowerModel.GetInitialMarginRequiredForOrder(QuantConnect.Securities.InitialMarginRequiredForOrderParameters)
    
    InitialMarginRequiredForOrderParameters(currencyConverter: ICurrencyConverter, security: Security, order: Order)
    """
    def __init__(self, currencyConverter: QuantConnect.Securities.ICurrencyConverter, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> QuantConnect.Securities.InitialMarginRequiredForOrderParameters:
        pass

    CurrencyConverter: QuantConnect.Securities.ICurrencyConverter

    Order: QuantConnect.Orders.Order

    Security: QuantConnect.Securities.Security



class IOrderEventProvider:
    """ Represents a type with a new QuantConnect.Orders.OrderEvent event System.EventHandler. """
    NewOrderEvent: BoundEvent


class IOrderProvider:
    """ Represents a type capable of fetching Order instances by its QC order id or by a brokerage id """
    def GetOpenOrders(self, filter: typing.Callable[[QuantConnect.Orders.Order], bool]) -> typing.List[QuantConnect.Orders.Order]:
        pass

    def GetOpenOrderTickets(self, filter: typing.Callable[[QuantConnect.Orders.OrderTicket], bool]) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def GetOrderByBrokerageId(self, brokerageId: str) -> QuantConnect.Orders.Order:
        pass

    def GetOrderById(self, orderId: int) -> QuantConnect.Orders.Order:
        pass

    def GetOrders(self, filter: typing.Callable[[QuantConnect.Orders.Order], bool]) -> typing.List[QuantConnect.Orders.Order]:
        pass

    def GetOrderTicket(self, orderId: int) -> QuantConnect.Orders.OrderTicket:
        pass

    def GetOrderTickets(self, filter: typing.Callable[[QuantConnect.Orders.OrderTicket], bool]) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    OrdersCount: int



class IOrderProcessor(QuantConnect.Securities.IOrderProvider):
    """ Represents a type capable of processing orders """
    def Process(self, request: QuantConnect.Orders.OrderRequest) -> QuantConnect.Orders.OrderTicket:
        pass


class IPriceVariationModel:
    """ Gets the minimum price variation of a given security """
    def GetMinimumPriceVariation(self, parameters: QuantConnect.Securities.GetMinimumPriceVariationParameters) -> float:
        pass


class IRegisteredSecurityDataTypesProvider:
    """ Provides the set of base data types registered in the algorithm """
    def RegisterType(self, type: type) -> bool:
        pass

    def TryGetType(self, name: str, type: type) -> bool:
        pass

    def UnregisterType(self, type: type) -> bool:
        pass


class ISecurityInitializer:
    """ Represents a type capable of initializing a new security """
    def Initialize(self, security: QuantConnect.Securities.Security) -> None:
        pass


class ISecurityPortfolioModel:
    """ Performs order fill application to portfolio """
    def ProcessFill(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, fill: QuantConnect.Orders.OrderEvent) -> None:
        pass


class ISecurityProvider:
    """ Represents a type capable of fetching the holdings for the specified symbol """
    def GetSecurity(self, symbol: QuantConnect.Symbol) -> QuantConnect.Securities.Security:
        pass


class ISecuritySeeder:
    """ Used to seed the security with the correct price """
    def SeedSecurity(self, security: QuantConnect.Securities.Security) -> bool:
        pass


class ISettlementModel:
    """ Represents the model responsible for applying cash settlement rules """
    def ApplyFunds(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, applicationTimeUtc: datetime.datetime, currency: str, amount: float) -> None:
        pass


class IVolatilityModel:
    """ Represents a model that computes the volatility of a security """
    def GetHistoryRequirements(self, security: QuantConnect.Securities.Security, utcTime: datetime.datetime) -> typing.List[QuantConnect.Data.HistoryRequest]:
        pass

    def Update(self, security: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> None:
        pass

    Volatility: float



class LocalMarketHours(System.object):
    """
    Represents the market hours under normal conditions for an exchange and a specific day of the week in terms of local time
    
    LocalMarketHours(day: DayOfWeek, *segments: Array[MarketHoursSegment])
    LocalMarketHours(day: DayOfWeek, segments: IEnumerable[MarketHoursSegment])
    LocalMarketHours(day: DayOfWeek, extendedMarketOpen: TimeSpan, marketOpen: TimeSpan, marketClose: TimeSpan, extendedMarketClose: TimeSpan)
    LocalMarketHours(day: DayOfWeek, marketOpen: TimeSpan, marketClose: TimeSpan)
    """
    @staticmethod
    def ClosedAllDay(dayOfWeek: System.DayOfWeek) -> QuantConnect.Securities.LocalMarketHours:
        pass

    def GetMarketClose(self, time: datetime.timedelta, extendedMarket: bool) -> typing.Optional[datetime.timedelta]:
        pass

    def GetMarketOpen(self, time: datetime.timedelta, extendedMarket: bool) -> typing.Optional[datetime.timedelta]:
        pass

    @typing.overload
    def IsOpen(self, time: datetime.timedelta, extendedMarket: bool) -> bool:
        pass

    @typing.overload
    def IsOpen(self, start: datetime.timedelta, end: datetime.timedelta, extendedMarket: bool) -> bool:
        pass

    def IsOpen(self, *args) -> bool:
        pass

    @staticmethod
    def OpenAllDay(dayOfWeek: System.DayOfWeek) -> QuantConnect.Securities.LocalMarketHours:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self, day: System.DayOfWeek, segments: typing.List[QuantConnect.Securities.MarketHoursSegment]) -> QuantConnect.Securities.LocalMarketHours:
        pass

    @typing.overload
    def __init__(self, day: System.DayOfWeek, segments: typing.List[QuantConnect.Securities.MarketHoursSegment]) -> QuantConnect.Securities.LocalMarketHours:
        pass

    @typing.overload
    def __init__(self, day: System.DayOfWeek, extendedMarketOpen: datetime.timedelta, marketOpen: datetime.timedelta, marketClose: datetime.timedelta, extendedMarketClose: datetime.timedelta) -> QuantConnect.Securities.LocalMarketHours:
        pass

    @typing.overload
    def __init__(self, day: System.DayOfWeek, marketOpen: datetime.timedelta, marketClose: datetime.timedelta) -> QuantConnect.Securities.LocalMarketHours:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.LocalMarketHours:
        pass

    DayOfWeek: System.DayOfWeek

    IsClosedAllDay: bool

    IsOpenAllDay: bool

    MarketDuration: datetime.timedelta

    Segments: typing.List[QuantConnect.Securities.MarketHoursSegment]



class MarginCallModel(System.object):
    """ Provides access to a null implementation for QuantConnect.Securities.IMarginCallModel """
    Null: NullMarginCallModel
    __all__: list


class MarketHoursDatabase(System.object):
    """
    Provides access to exchange hours and raw data times zones in various markets
    
    MarketHoursDatabase(exchangeHours: IReadOnlyDictionary[SecurityDatabaseKey, Entry])
    """
    @staticmethod
    @typing.overload
    def FromDataFolder() -> QuantConnect.Securities.MarketHoursDatabase:
        pass

    @staticmethod
    @typing.overload
    def FromDataFolder(dataFolder: str) -> QuantConnect.Securities.MarketHoursDatabase:
        pass

    def FromDataFolder(self, *args) -> QuantConnect.Securities.MarketHoursDatabase:
        pass

    @staticmethod
    def FromFile(path: str) -> QuantConnect.Securities.MarketHoursDatabase:
        pass

    @staticmethod
    def GetDatabaseSymbolKey(symbol: QuantConnect.Symbol) -> str:
        pass

    def GetDataTimeZone(self, market: str, symbol: QuantConnect.Symbol, securityType: QuantConnect.SecurityType) -> NodaTime.DateTimeZone:
        pass

    @typing.overload
    def GetEntry(self, market: str, symbol: str, securityType: QuantConnect.SecurityType) -> QuantConnect.Securities.Entry:
        pass

    @typing.overload
    def GetEntry(self, market: str, symbol: QuantConnect.Symbol, securityType: QuantConnect.SecurityType) -> QuantConnect.Securities.Entry:
        pass

    def GetEntry(self, *args) -> QuantConnect.Securities.Entry:
        pass

    @typing.overload
    def GetExchangeHours(self, configuration: QuantConnect.Data.SubscriptionDataConfig) -> QuantConnect.Securities.SecurityExchangeHours:
        pass

    @typing.overload
    def GetExchangeHours(self, market: str, symbol: QuantConnect.Symbol, securityType: QuantConnect.SecurityType) -> QuantConnect.Securities.SecurityExchangeHours:
        pass

    def GetExchangeHours(self, *args) -> QuantConnect.Securities.SecurityExchangeHours:
        pass

    @staticmethod
    def Reset() -> None:
        pass

    def SetEntry(self, market: str, symbol: str, securityType: QuantConnect.SecurityType, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, dataTimeZone: NodaTime.DateTimeZone) -> QuantConnect.Securities.Entry:
        pass

    def SetEntryAlwaysOpen(self, market: str, symbol: str, securityType: QuantConnect.SecurityType, timeZone: NodaTime.DateTimeZone) -> QuantConnect.Securities.Entry:
        pass

    @typing.overload
    def TryGetEntry(self, market: str, symbol: QuantConnect.Symbol, securityType: QuantConnect.SecurityType, entry: QuantConnect.Securities.Entry) -> bool:
        pass

    @typing.overload
    def TryGetEntry(self, market: str, symbol: str, securityType: QuantConnect.SecurityType, entry: QuantConnect.Securities.Entry) -> bool:
        pass

    def TryGetEntry(self, *args) -> bool:
        pass

    def __init__(self, exchangeHours: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.Securities.SecurityDatabaseKey, QuantConnect.Securities.Entry]) -> QuantConnect.Securities.MarketHoursDatabase:
        pass

    ExchangeHoursListing: typing.List[System.Collections.Generic.KeyValuePair[QuantConnect.Securities.SecurityDatabaseKey, QuantConnect.Securities.Entry]]


    AlwaysOpen: AlwaysOpenMarketHoursDatabaseImpl
    Entry: type
