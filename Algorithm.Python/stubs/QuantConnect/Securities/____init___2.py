from .____init___3 import *
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



class FutureFilterUniverseEx(System.object):
    """ Extensions for Linq support """
    @staticmethod
    def Select(universe: QuantConnect.Securities.FutureFilterUniverse, mapFunc: typing.Callable[[QuantConnect.Symbol], QuantConnect.Symbol]) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    @staticmethod
    def SelectMany(universe: QuantConnect.Securities.FutureFilterUniverse, mapFunc: typing.Callable[[QuantConnect.Symbol], typing.List[QuantConnect.Symbol]]) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    @staticmethod
    def Where(universe: QuantConnect.Securities.FutureFilterUniverse, predicate: typing.Callable[[QuantConnect.Symbol], bool]) -> QuantConnect.Securities.FutureFilterUniverse:
        pass

    __all__: list


class Futures(System.object):
    """ Futures static class contains shortcut definitions of major futures contracts available for trading """
    Currencies: type
    Dairy: type
    Energies: type
    Financials: type
    Forestry: type
    Grains: type
    Indices: type
    Meats: type
    Metals: type
    Softs: type
    __all__: list


class GetMaximumOrderQuantityForDeltaBuyingPowerParameters(System.object):
    """
    Defines the parameters for QuantConnect.Securities.IBuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(QuantConnect.Securities.GetMaximumOrderQuantityForDeltaBuyingPowerParameters)
    
    GetMaximumOrderQuantityForDeltaBuyingPowerParameters(portfolio: SecurityPortfolioManager, security: Security, deltaBuyingPower: Decimal, silenceNonErrorReasons: bool)
    """
    def __init__(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, deltaBuyingPower: float, silenceNonErrorReasons: bool) -> QuantConnect.Securities.GetMaximumOrderQuantityForDeltaBuyingPowerParameters:
        pass

    DeltaBuyingPower: float

    Portfolio: QuantConnect.Securities.SecurityPortfolioManager

    Security: QuantConnect.Securities.Security

    SilenceNonErrorReasons: bool



class GetMaximumOrderQuantityForTargetBuyingPowerParameters(System.object):
    """
    Defines the parameters for QuantConnect.Securities.IBuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(QuantConnect.Securities.GetMaximumOrderQuantityForTargetBuyingPowerParameters)
    
    GetMaximumOrderQuantityForTargetBuyingPowerParameters(portfolio: SecurityPortfolioManager, security: Security, targetBuyingPower: Decimal, silenceNonErrorReasons: bool)
    """
    def __init__(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, targetBuyingPower: float, silenceNonErrorReasons: bool) -> QuantConnect.Securities.GetMaximumOrderQuantityForTargetBuyingPowerParameters:
        pass

    Portfolio: QuantConnect.Securities.SecurityPortfolioManager

    Security: QuantConnect.Securities.Security

    SilenceNonErrorReasons: bool

    TargetBuyingPower: float



class GetMaximumOrderQuantityResult(System.object):
    """
    Contains the information returned by QuantConnect.Securities.IBuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(QuantConnect.Securities.GetMaximumOrderQuantityForTargetBuyingPowerParameters)
                and  QuantConnect.Securities.IBuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(QuantConnect.Securities.GetMaximumOrderQuantityForDeltaBuyingPowerParameters)
    
    GetMaximumOrderQuantityResult(quantity: Decimal, reason: str)
    GetMaximumOrderQuantityResult(quantity: Decimal, reason: str, isError: bool)
    """
    @typing.overload
    def __init__(self, quantity: float, reason: str) -> QuantConnect.Securities.GetMaximumOrderQuantityResult:
        pass

    @typing.overload
    def __init__(self, quantity: float, reason: str, isError: bool) -> QuantConnect.Securities.GetMaximumOrderQuantityResult:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.GetMaximumOrderQuantityResult:
        pass

    IsError: bool

    Quantity: float

    Reason: str



class GetMinimumPriceVariationParameters(System.object):
    """
    Defines the parameters for QuantConnect.Securities.IPriceVariationModel.GetMinimumPriceVariation(QuantConnect.Securities.GetMinimumPriceVariationParameters)
    
    GetMinimumPriceVariationParameters(security: Security, referencePrice: Decimal)
    """
    def __init__(self, security: QuantConnect.Securities.Security, referencePrice: float) -> QuantConnect.Securities.GetMinimumPriceVariationParameters:
        pass

    ReferencePrice: float

    Security: QuantConnect.Securities.Security



class HasSufficientBuyingPowerForOrderParameters(System.object):
    """
    Defines the parameters for QuantConnect.Securities.IBuyingPowerModel.HasSufficientBuyingPowerForOrder(QuantConnect.Securities.HasSufficientBuyingPowerForOrderParameters)
    
    HasSufficientBuyingPowerForOrderParameters(portfolio: SecurityPortfolioManager, security: Security, order: Order)
    """
    def __init__(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> QuantConnect.Securities.HasSufficientBuyingPowerForOrderParameters:
        pass

    Order: QuantConnect.Orders.Order

    Portfolio: QuantConnect.Securities.SecurityPortfolioManager

    Security: QuantConnect.Securities.Security



class HasSufficientBuyingPowerForOrderResult(System.object):
    """
    Contains the information returned by QuantConnect.Securities.IBuyingPowerModel.HasSufficientBuyingPowerForOrder(QuantConnect.Securities.HasSufficientBuyingPowerForOrderParameters)
    
    HasSufficientBuyingPowerForOrderResult(isSufficient: bool, reason: str)
    """
    def __init__(self, isSufficient: bool, reason: str) -> QuantConnect.Securities.HasSufficientBuyingPowerForOrderResult:
        pass

    IsSufficient: bool

    Reason: str



class IBaseCurrencySymbol:
    # no doc
    BaseCurrencySymbol: str



class IBuyingPowerModel:
    """ Represents a security's model of buying power """
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


class IdentityCurrencyConverter(System.object, QuantConnect.Securities.ICurrencyConverter):
    """
    Provides an implementation of QuantConnect.Securities.ICurrencyConverter that does NOT perform conversions.
                This implementation will throw if the specified cashAmount is not in units of account currency.
    
    IdentityCurrencyConverter(accountCurrency: str)
    """
    def ConvertToAccountCurrency(self, cashAmount: QuantConnect.Securities.CashAmount) -> QuantConnect.Securities.CashAmount:
        pass

    def __init__(self, accountCurrency: str) -> QuantConnect.Securities.IdentityCurrencyConverter:
        pass

    AccountCurrency: str



class IDerivativeSecurity:
    """ Defines a security as a derivative of another security """
    Underlying: QuantConnect.Securities.Security



class IDerivativeSecurityFilter:
    """ Filters a set of derivative symbols using the underlying price data. """
    def Filter(self, universe: QuantConnect.Securities.IDerivativeSecurityFilterUniverse) -> QuantConnect.Securities.IDerivativeSecurityFilterUniverse:
        pass


class IMarginCallModel:
    """ Represents the model responsible for picking which orders should be executed during a margin call """
    def ExecuteMarginCall(self, generatedMarginCallOrders: typing.List[QuantConnect.Orders.SubmitOrderRequest]) -> typing.List[QuantConnect.Orders.OrderTicket]:
        pass

    def GetMarginCallOrders(self, issueMarginCallWarning: bool) -> typing.List[QuantConnect.Orders.SubmitOrderRequest]:
        pass


class ImmediateSettlementModel(System.object, QuantConnect.Securities.ISettlementModel):
    """
    Represents the model responsible for applying cash settlement rules
    
    ImmediateSettlementModel()
    """
    def ApplyFunds(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, applicationTimeUtc: datetime.datetime, currency: str, amount: float) -> None:
        pass


class IndicatorVolatilityModel(System.object, QuantConnect.Securities.IVolatilityModel):
    """
    IndicatorVolatilityModel[T](indicator: IIndicator[T])
    IndicatorVolatilityModel[T](indicator: IIndicator[T], indicatorUpdate: Action[Security, BaseData, IIndicator[T]])
    """
    def GetHistoryRequirements(self, security: QuantConnect.Securities.Security, utcTime: datetime.datetime) -> typing.List[QuantConnect.Data.HistoryRequest]:
        pass

    def Update(self, security: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> None:
        pass

    @typing.overload
    def __init__(self, indicator: QuantConnect.Indicators.IIndicator[QuantConnect.Securities.T]) -> QuantConnect.Securities.IndicatorVolatilityModel:
        pass

    @typing.overload
    def __init__(self, indicator: QuantConnect.Indicators.IIndicator[QuantConnect.Securities.T], indicatorUpdate: typing.Callable[[QuantConnect.Securities.Security, QuantConnect.Data.BaseData, QuantConnect.Indicators.IIndicator[QuantConnect.Securities.T]], None]) -> QuantConnect.Securities.IndicatorVolatilityModel:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.IndicatorVolatilityModel:
        pass

    Volatility: float
