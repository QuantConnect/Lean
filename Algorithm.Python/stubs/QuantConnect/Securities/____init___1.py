from .____init___2 import *
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


class CashBuyingPowerModel(QuantConnect.Securities.BuyingPowerModel, QuantConnect.Securities.IBuyingPowerModel):
    """
    Represents a buying power model for cash accounts
    
    CashBuyingPowerModel()
    """
    def GetBuyingPower(self, parameters: QuantConnect.Securities.BuyingPowerParameters) -> QuantConnect.Securities.BuyingPower:
        pass

    def GetLeverage(self, security: QuantConnect.Securities.Security) -> float:
        pass

    def GetMaximumOrderQuantityForDeltaBuyingPower(self, parameters: QuantConnect.Securities.GetMaximumOrderQuantityForDeltaBuyingPowerParameters) -> QuantConnect.Securities.GetMaximumOrderQuantityResult:
        pass

    def GetMaximumOrderQuantityForTargetBuyingPower(self, parameters: QuantConnect.Securities.GetMaximumOrderQuantityForTargetBuyingPowerParameters) -> QuantConnect.Securities.GetMaximumOrderQuantityResult:
        pass

    def GetReservedBuyingPowerForPosition(self, parameters: QuantConnect.Securities.ReservedBuyingPowerForPositionParameters) -> QuantConnect.Securities.ReservedBuyingPowerForPosition:
        pass

    def HasSufficientBuyingPowerForOrder(self, parameters: QuantConnect.Securities.HasSufficientBuyingPowerForOrderParameters) -> QuantConnect.Securities.HasSufficientBuyingPowerForOrderResult:
        pass

    def SetLeverage(self, security: QuantConnect.Securities.Security, leverage: float) -> None:
        pass

    RequiredFreeBuyingPowerPercent: float

class CompositeSecurityInitializer(System.object, QuantConnect.Securities.ISecurityInitializer):
    """
    Provides an implementation of QuantConnect.Securities.ISecurityInitializer that executes
                each initializer in order
    
    CompositeSecurityInitializer(*initializers: Array[ISecurityInitializer])
    """
    def Initialize(self, security: QuantConnect.Securities.Security) -> None:
        pass

    def __init__(self, initializers: typing.List[QuantConnect.Securities.ISecurityInitializer]) -> QuantConnect.Securities.CompositeSecurityInitializer:
        pass


class DefaultMarginCallModel(System.object, QuantConnect.Securities.IMarginCallModel):
    """
    Represents the model responsible for picking which orders should be executed during a margin call
    
    DefaultMarginCallModel(portfolio: SecurityPortfolioManager, defaultOrderProperties: IOrderProperties)
    """
    def ExecuteMarginCall(self, generatedMarginCallOrders: typing.List[QuantConnect.Orders.SubmitOrderRequest]) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def GetMarginCallOrders(self, issueMarginCallWarning: bool) -> typing.List[QuantConnect.Orders.SubmitOrderRequest]:
        pass

    def __init__(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, defaultOrderProperties: QuantConnect.Interfaces.IOrderProperties) -> QuantConnect.Securities.DefaultMarginCallModel:
        pass



class DelayedSettlementModel(System.object, QuantConnect.Securities.ISettlementModel):
    """
    Represents the model responsible for applying cash settlement rules
    
    DelayedSettlementModel(numberOfDays: int, timeOfDay: TimeSpan)
    """
    def ApplyFunds(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, applicationTimeUtc: datetime.datetime, currency: str, amount: float) -> None:
        pass

    def __init__(self, numberOfDays: int, timeOfDay: datetime.timedelta) -> QuantConnect.Securities.DelayedSettlementModel:
        pass


class DynamicSecurityData(System.object, System.Dynamic.IDynamicMetaObjectProvider):
    """
    Provides access to a security's data via it's type. This implementation supports dynamic access
                by type name.
    
    DynamicSecurityData(registeredTypes: IRegisteredSecurityDataTypesProvider, cache: SecurityCache)
    """
    @typing.overload
    def Get(self) -> QuantConnect.Securities.T:
        pass

    @typing.overload
    def Get(self, type: type) -> Python.Runtime.PyObject:
        pass

    def Get(self, *args) -> Python.Runtime.PyObject:
        pass

    @typing.overload
    def GetAll(self) -> typing.List[QuantConnect.Securities.T]:
        pass

    @typing.overload
    def GetAll(self, type: type) -> System.Collections.IList:
        pass

    def GetAll(self, *args) -> System.Collections.IList:
        pass

    def GetMetaObject(self, parameter: System.Linq.Expressions.Expression) -> System.Dynamic.DynamicMetaObject:
        pass

    def GetProperty(self, name: str) -> object:
        pass

    def HasData(self) -> bool:
        pass

    def HasProperty(self, name: str) -> bool:
        pass

    def SetProperty(self, name: str, value: object) -> object:
        pass

    def __init__(self, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, cache: QuantConnect.Securities.SecurityCache) -> QuantConnect.Securities.DynamicSecurityData:
        pass


class SecurityPriceVariationModel(System.object, QuantConnect.Securities.IPriceVariationModel):
    """
    Provides default implementation of QuantConnect.Securities.IPriceVariationModel
                for use in defining the minimum price variation.
    
    SecurityPriceVariationModel()
    """
    def GetMinimumPriceVariation(self, parameters: QuantConnect.Securities.GetMinimumPriceVariationParameters) -> float:
        pass


class EquityPriceVariationModel(QuantConnect.Securities.SecurityPriceVariationModel, QuantConnect.Securities.IPriceVariationModel):
    """
    Provides an implementation of QuantConnect.Securities.IPriceVariationModel
                for use in defining the minimum price variation for a given equity
                under Regulation NMS – Rule 612 (a.k.a – the “sub-penny rule”)
    
    EquityPriceVariationModel()
    """
    def GetMinimumPriceVariation(self, parameters: QuantConnect.Securities.GetMinimumPriceVariationParameters) -> float:
        pass


class ErrorCurrencyConverter(System.object, QuantConnect.Securities.ICurrencyConverter):
    """
    Provides an implementation of QuantConnect.Securities.ICurrencyConverter for use in
                tests that don't depend on this behavior.
    """
    def ConvertToAccountCurrency(self, cashAmount: QuantConnect.Securities.CashAmount) -> QuantConnect.Securities.CashAmount:
        pass

    AccountCurrency: str


    Instance: 'ErrorCurrencyConverter'


class FuncSecurityDerivativeFilter(System.object, QuantConnect.Securities.IDerivativeSecurityFilter):
    """
    Provides a functional implementation of QuantConnect.Securities.IDerivativeSecurityFilter
    
    FuncSecurityDerivativeFilter(filter: Func[IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse])
    """
    def Filter(self, universe: QuantConnect.Securities.IDerivativeSecurityFilterUniverse) -> QuantConnect.Securities.IDerivativeSecurityFilterUniverse:
        pass

    def __init__(self, filter: typing.Callable[[QuantConnect.Securities.IDerivativeSecurityFilterUniverse], QuantConnect.Securities.IDerivativeSecurityFilterUniverse]) -> QuantConnect.Securities.FuncSecurityDerivativeFilter:
        pass


class FuncSecurityInitializer(System.object, QuantConnect.Securities.ISecurityInitializer):
    """
    Provides a functional implementation of QuantConnect.Securities.ISecurityInitializer
    
    FuncSecurityInitializer(initializer: Action[Security])
    """
    def Initialize(self, security: QuantConnect.Securities.Security) -> None:
        pass

    def __init__(self, initializer: typing.Callable[[QuantConnect.Securities.Security], None]) -> QuantConnect.Securities.FuncSecurityInitializer:
        pass


class FuncSecuritySeeder(System.object, QuantConnect.Securities.ISecuritySeeder):
    """
    Seed a security price from a history function
    
    FuncSecuritySeeder(seedFunction: Func[Security, BaseData])
    """
    def SeedSecurity(self, security: QuantConnect.Securities.Security) -> bool:
        pass

    def __init__(self, seedFunction: typing.Callable[[QuantConnect.Securities.Security], QuantConnect.Data.BaseData]) -> QuantConnect.Securities.FuncSecuritySeeder:
        pass


class FutureExpirationCycles(System.object):
    """ Static class contains definitions of popular futures expiration cycles """
    AllYear: Array[int]
    February: Array[int]
    FGHJKMNQUVXZ: Array[int]
    FHKNQUVZ: Array[int]
    FHKNQUX: Array[int]
    HKNUVZ: Array[int]
    HKNUZ: Array[int]
    HMUZ: Array[int]
    January: Array[int]
    March: Array[int]
    __all__: list


class IDerivativeSecurityFilterUniverse(System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[Symbol]):
    """ Represents derivative symbols universe used in filtering. """
    IsDynamic: bool

    Underlying: QuantConnect.Data.BaseData



class FutureFilterUniverse(System.object, System.Collections.IEnumerable, QuantConnect.Securities.IDerivativeSecurityFilterUniverse, System.Collections.Generic.IEnumerable[Symbol]):
    """
    Represents futures symbols universe used in filtering.
    
    FutureFilterUniverse(allSymbols: IEnumerable[Symbol], underlying: BaseData)
    """
    def BackMonth(self) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    def BackMonths(self) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    @typing.overload
    def Contracts(self, contracts: typing.List[QuantConnect.Symbol]) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    @typing.overload
    def Contracts(self, contractSelector: typing.Callable[[typing.List[QuantConnect.Symbol]], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    def Contracts(self, *args) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    @typing.overload
    def Expiration(self, minExpiry: datetime.timedelta, maxExpiry: datetime.timedelta) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    @typing.overload
    def Expiration(self, minExpiryDays: int, maxExpiryDays: int) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    def Expiration(self, *args) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    def ExpirationCycle(self, months: typing.List[int]) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    def FrontMonth(self) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Symbol]:
        pass

    def OnlyApplyFilterAtMarketOpen(self) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    def __init__(self, allSymbols: typing.List[QuantConnect.Symbol], underlying: QuantConnect.Data.BaseData) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    IsDynamic: bool

    Underlying: QuantConnect.Data.BaseData
