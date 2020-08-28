from .____init___8 import *
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


class SecurityPortfolioModel(System.object, QuantConnect.Securities.ISecurityPortfolioModel):
    """
    Provides a default implementation of QuantConnect.Securities.ISecurityPortfolioModel that simply
                applies the fills to the algorithm's portfolio. This implementation is intended to
                handle all security types.
    
    SecurityPortfolioModel()
    """
    def ProcessFill(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, fill: QuantConnect.Orders.OrderEvent) -> None:
        pass


class SecurityProviderExtensions(System.object):
    """ Provides extension methods for the QuantConnect.Securities.ISecurityProvider interface. """
    @staticmethod
    def GetHoldingsQuantity(provider: QuantConnect.Securities.ISecurityProvider, symbol: QuantConnect.Symbol) -> float:
        pass

    __all__: list


class SecuritySeeder(System.object):
    """ Provides access to a null implementation for QuantConnect.Securities.ISecuritySeeder """
    Null: NullSecuritySeeder
    __all__: list


class SecurityService(System.object, QuantConnect.Interfaces.ISecurityService):
    """
    This class implements interface QuantConnect.Interfaces.ISecurityService providing methods for creating new QuantConnect.Securities.Security
    
    SecurityService(cashBook: CashBook, marketHoursDatabase: MarketHoursDatabase, symbolPropertiesDatabase: SymbolPropertiesDatabase, securityInitializerProvider: ISecurityInitializerProvider, registeredTypes: IRegisteredSecurityDataTypesProvider, cacheProvider: SecurityCacheProvider)
    """
    @typing.overload
    def CreateSecurity(self, symbol: QuantConnect.Symbol, subscriptionDataConfigList: typing.List[QuantConnect.Data.SubscriptionDataConfig], leverage: float, addToSymbolCache: bool) -> QuantConnect.Securities.Security:
        pass

    @typing.overload
    def CreateSecurity(self, symbol: QuantConnect.Symbol, subscriptionDataConfig: QuantConnect.Data.SubscriptionDataConfig, leverage: float, addToSymbolCache: bool) -> QuantConnect.Securities.Security:
        pass

    def CreateSecurity(self, *args) -> QuantConnect.Securities.Security:
        pass

    def SetLiveMode(self, isLiveMode: bool) -> None:
        pass

    def __init__(self, cashBook: QuantConnect.Securities.CashBook, marketHoursDatabase: QuantConnect.Securities.MarketHoursDatabase, symbolPropertiesDatabase: QuantConnect.Securities.SymbolPropertiesDatabase, securityInitializerProvider: QuantConnect.Interfaces.ISecurityInitializerProvider, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, cacheProvider: QuantConnect.Securities.SecurityCacheProvider) -> QuantConnect.Securities.SecurityService:
        pass


class SecurityTransactionManager(System.object, QuantConnect.Securities.IOrderProvider):
    """
    Algorithm Transactions Manager - Recording Transactions
    
    SecurityTransactionManager(algorithm: IAlgorithm, security: SecurityManager)
    """
    def AddOrder(self, request: QuantConnect.Orders.SubmitOrderRequest) -> QuantConnect.Orders.OrderTicket:
        pass

    def AddTransactionRecord(self, time: datetime.datetime, transactionProfitLoss: float) -> None:
        pass

    @typing.overload
    def CancelOpenOrders(self) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    @typing.overload
    def CancelOpenOrders(self, symbol: QuantConnect.Symbol, tag: str) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def CancelOpenOrders(self, *args) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def CancelOrder(self, orderId: int, orderTag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    def GetIncrementOrderId(self) -> int:
        pass

    @typing.overload
    def GetOpenOrders(self, symbol: QuantConnect.Symbol) -> typing.List[QuantConnect.Orders.Order]:
        pass

    @typing.overload
    def GetOpenOrders(self, filter: typing.Callable[[QuantConnect.Orders.Order], bool]) -> typing.List[QuantConnect.Orders.Order]:
        pass

    def GetOpenOrders(self, *args) -> typing.List[QuantConnect.Orders.Order]:
        pass

    @typing.overload
    def GetOpenOrderTickets(self, symbol: QuantConnect.Symbol) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    @typing.overload
    def GetOpenOrderTickets(self, filter: typing.Callable[[QuantConnect.Orders.OrderTicket], bool]) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def GetOpenOrderTickets(self, *args) -> typing.List[QuantConnect.Orders.OrderTicket]:
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

    def ProcessRequest(self, request: QuantConnect.Orders.OrderRequest) -> QuantConnect.Orders.OrderTicket:
        pass

    def RemoveOrder(self, orderId: int, tag: str) -> QuantConnect.Orders.OrderTicket:
        pass

    def SetOrderProcessor(self, orderProvider: QuantConnect.Securities.IOrderProcessor) -> None:
        pass

    def UpdateOrder(self, request: QuantConnect.Orders.UpdateOrderRequest) -> QuantConnect.Orders.OrderTicket:
        pass

    def WaitForOrder(self, orderId: int) -> bool:
        pass

    def __init__(self, algorithm: QuantConnect.Interfaces.IAlgorithm, security: QuantConnect.Securities.SecurityManager) -> QuantConnect.Securities.SecurityTransactionManager:
        pass

    LastOrderId: int

    MarketOrderFillTimeout: datetime.timedelta

    MinimumOrderQuantity: int

    MinimumOrderSize: float

    OrdersCount: int

    TransactionRecord: System.Collections.Generic.Dictionary[datetime.datetime, float]

    UtcTime: datetime.datetime



class StandardDeviationOfReturnsVolatilityModel(QuantConnect.Securities.Volatility.BaseVolatilityModel, QuantConnect.Securities.IVolatilityModel):
    """
    Provides an implementation of QuantConnect.Securities.IVolatilityModel that computes the
                annualized sample standard deviation of daily returns as the volatility of the security
    
    StandardDeviationOfReturnsVolatilityModel(periods: int)
    """
    def GetHistoryRequirements(self, security: QuantConnect.Securities.Security, utcTime: datetime.datetime) -> typing.List[QuantConnect.Data.HistoryRequest]:
        pass

    def Update(self, security: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> None:
        pass

    def __init__(self, periods: int) -> QuantConnect.Securities.StandardDeviationOfReturnsVolatilityModel:
        pass

    Volatility: float

    SubscriptionDataConfigProvider: QuantConnect.Interfaces.ISubscriptionDataConfigProvider


class SymbolProperties(System.object):
    """
    Represents common properties for a specific security, uniquely identified by market, symbol and security type
    
    SymbolProperties(description: str, quoteCurrency: str, contractMultiplier: Decimal, minimumPriceVariation: Decimal, lotSize: Decimal)
    """
    @staticmethod
    def GetDefault(quoteCurrency: str) -> QuantConnect.Securities.SymbolProperties:
        pass

    def __init__(self, description: str, quoteCurrency: str, contractMultiplier: float, minimumPriceVariation: float, lotSize: float) -> QuantConnect.Securities.SymbolProperties:
        pass

    ContractMultiplier: float

    Description: str

    LotSize: float

    MinimumPriceVariation: float

    QuoteCurrency: str



class SymbolPropertiesDatabase(System.object):
    """ Provides access to specific properties for various symbols """
    @typing.overload
    def ContainsKey(self, market: str, symbol: str, securityType: QuantConnect.SecurityType) -> bool:
        pass

    @typing.overload
    def ContainsKey(self, market: str, symbol: QuantConnect.Symbol, securityType: QuantConnect.SecurityType) -> bool:
        pass

    def ContainsKey(self, *args) -> bool:
        pass

    @staticmethod
    def FromDataFolder() -> QuantConnect.Securities.SymbolPropertiesDatabase:
        pass

    @typing.overload
    def GetSymbolProperties(self, market: str, symbol: str, securityType: QuantConnect.SecurityType, defaultQuoteCurrency: str) -> QuantConnect.Securities.SymbolProperties:
        pass

    @typing.overload
    def GetSymbolProperties(self, market: str, symbol: QuantConnect.Symbol, securityType: QuantConnect.SecurityType, defaultQuoteCurrency: str) -> QuantConnect.Securities.SymbolProperties:
        pass

    def GetSymbolProperties(self, *args) -> QuantConnect.Securities.SymbolProperties:
        pass

    def TryGetMarket(self, symbol: str, securityType: QuantConnect.SecurityType, market: str) -> bool:
        pass


class UniverseManager(System.object, System.Collections.IEnumerable, System.Collections.Generic.ICollection[KeyValuePair[Symbol, Universe]], System.Collections.Generic.IDictionary[Symbol, Universe], System.Collections.Generic.IEnumerable[KeyValuePair[Symbol, Universe]], System.Collections.Specialized.INotifyCollectionChanged):
    """
    Manages the algorithm's collection of universes
    
    UniverseManager()
    """
    @typing.overload
    def Add(self, item: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.UniverseSelection.Universe]) -> None:
        pass

    @typing.overload
    def Add(self, key: QuantConnect.Symbol, universe: QuantConnect.Data.UniverseSelection.Universe) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def Clear(self) -> None:
        pass

    def Contains(self, item: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.UniverseSelection.Universe]) -> bool:
        pass

    def ContainsKey(self, key: QuantConnect.Symbol) -> bool:
        pass

    def CopyTo(self, array: typing.List[System.Collections.Generic.KeyValuePair], arrayIndex: int) -> None:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.UniverseSelection.Universe]]:
        pass

    @typing.overload
    def Remove(self, item: System.Collections.Generic.KeyValuePair[QuantConnect.Symbol, QuantConnect.Data.UniverseSelection.Universe]) -> bool:
        pass

    @typing.overload
    def Remove(self, key: QuantConnect.Symbol) -> bool:
        pass

    def Remove(self, *args) -> bool:
        pass

    def TryGetValue(self, key: QuantConnect.Symbol, value: QuantConnect.Data.UniverseSelection.Universe) -> bool:
        pass

    ActiveSecurities: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.Symbol, QuantConnect.Securities.Security]

    Count: int

    IsReadOnly: bool

    Keys: typing.List[QuantConnect.Symbol]

    Values: typing.List[QuantConnect.Data.UniverseSelection.Universe]


    CollectionChanged: BoundEvent
    Item: indexer#
