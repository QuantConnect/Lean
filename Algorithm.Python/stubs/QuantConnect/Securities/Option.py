from .__Option_1 import *
import typing
import System.Collections.Concurrent
import System
import QuantConnect.Securities.Option
import QuantConnect.Securities
import QuantConnect.Orders.OptionExercise
import QuantConnect.Orders
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import datetime

# no functions
# classes

class ConstantQLRiskFreeRateEstimator(System.object, QuantConnect.Securities.Option.IQLRiskFreeRateEstimator):
    """
    Class implements default flat risk free curve, implementing QuantConnect.Securities.Option.IQLRiskFreeRateEstimator.
    
    ConstantQLRiskFreeRateEstimator(riskFreeRate: float)
    """
    def Estimate(self, security: QuantConnect.Securities.Security, slice: QuantConnect.Data.Slice, contract: QuantConnect.Data.Market.OptionContract) -> float:
        pass

    def __init__(self, riskFreeRate: float) -> QuantConnect.Securities.Option.ConstantQLRiskFreeRateEstimator:
        pass


class CurrentPriceOptionPriceModel(System.object, QuantConnect.Securities.Option.IOptionPriceModel):
    """
    Provides a default implementation of QuantConnect.Securities.Option.IOptionPriceModel that does not compute any
                greeks and uses the current price for the theoretical price. 
                This is a stub implementation until the real models are implemented
    
    CurrentPriceOptionPriceModel()
    """
    def Evaluate(self, security: QuantConnect.Securities.Security, slice: QuantConnect.Data.Slice, contract: QuantConnect.Data.Market.OptionContract) -> QuantConnect.Securities.Option.OptionPriceModelResult:
        pass


class EmptyOptionChainProvider(System.object, QuantConnect.Interfaces.IOptionChainProvider):
    """
    An implementation of QuantConnect.Interfaces.IOptionChainProvider that always returns an empty list of contracts
    
    EmptyOptionChainProvider()
    """
    def GetOptionContractList(self, symbol: QuantConnect.Symbol, date: datetime.datetime) -> typing.List[QuantConnect.Symbol]:
        pass


class IOptionPriceModel:
    """ Defines a model used to calculate the theoretical price of an option contract. """
    def Evaluate(self, security: QuantConnect.Securities.Security, slice: QuantConnect.Data.Slice, contract: QuantConnect.Data.Market.OptionContract) -> QuantConnect.Securities.Option.OptionPriceModelResult:
        pass


class Option(QuantConnect.Securities.Security, QuantConnect.Interfaces.IOptionPrice, QuantConnect.Securities.IDerivativeSecurity, QuantConnect.Interfaces.ISecurityPrice):
    """
    Option Security Object Implementation for Option Assets
    
    Option(exchangeHours: SecurityExchangeHours, config: SubscriptionDataConfig, quoteCurrency: Cash, symbolProperties: OptionSymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider)
    Option(symbol: Symbol, exchangeHours: SecurityExchangeHours, quoteCurrency: Cash, symbolProperties: OptionSymbolProperties, currencyConverter: ICurrencyConverter, registeredTypes: IRegisteredSecurityDataTypesProvider, securityCache: SecurityCache)
    """
    def EvaluatePriceModel(self, slice: QuantConnect.Data.Slice, contract: QuantConnect.Data.Market.OptionContract) -> QuantConnect.Securities.Option.OptionPriceModelResult:
        pass

    def GetAggregateExerciseAmount(self) -> float:
        pass

    def GetExerciseQuantity(self, quantity: float) -> float:
        pass

    def GetIntrinsicValue(self, underlyingPrice: float) -> float:
        pass

    def GetPayOff(self, underlyingPrice: float) -> float:
        pass

    def IsAutoExercised(self, underlyingPrice: float) -> bool:
        pass

    def SetDataNormalizationMode(self, mode: QuantConnect.DataNormalizationMode) -> None:
        pass

    @typing.overload
    def SetFilter(self, minStrike: int, maxStrike: int) -> None:
        pass

    @typing.overload
    def SetFilter(self, minExpiry: datetime.timedelta, maxExpiry: datetime.timedelta) -> None:
        pass

    @typing.overload
    def SetFilter(self, minStrike: int, maxStrike: int, minExpiry: datetime.timedelta, maxExpiry: datetime.timedelta) -> None:
        pass

    @typing.overload
    def SetFilter(self, minStrike: int, maxStrike: int, minExpiryDays: int, maxExpiryDays: int) -> None:
        pass

    @typing.overload
    def SetFilter(self, universeFunc: typing.Callable[[QuantConnect.Securities.OptionFilterUniverse], QuantConnect.Securities.OptionFilterUniverse]) -> None:
        pass

    @typing.overload
    def SetFilter(self, universeFunc: Python.Runtime.PyObject) -> None:
        pass

    def SetFilter(self, *args) -> None:
        pass

    @typing.overload
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, config: QuantConnect.Data.SubscriptionDataConfig, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.Option.OptionSymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider) -> QuantConnect.Securities.Option.Option:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, exchangeHours: QuantConnect.Securities.SecurityExchangeHours, quoteCurrency: QuantConnect.Securities.Cash, symbolProperties: QuantConnect.Securities.Option.OptionSymbolProperties, currencyConverter: QuantConnect.Securities.ICurrencyConverter, registeredTypes: QuantConnect.Securities.IRegisteredSecurityDataTypesProvider, securityCache: QuantConnect.Securities.SecurityCache) -> QuantConnect.Securities.Option.Option:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Option.Option:
        pass

    AskPrice: float

    BidPrice: float

    ContractFilter: QuantConnect.Securities.IDerivativeSecurityFilter

    ContractMultiplier: int

    ContractUnitOfTrade: int

    EnableGreekApproximation: bool

    ExerciseSettlement: QuantConnect.SettlementType

    Expiry: datetime.datetime

    IsOptionChain: bool

    IsOptionContract: bool

    OptionExerciseModel: QuantConnect.Orders.OptionExercise.IOptionExerciseModel

    PriceModel: QuantConnect.Securities.Option.IOptionPriceModel

    Right: QuantConnect.OptionRight

    StrikePrice: float

    Style: QuantConnect.OptionStyle

    Underlying: QuantConnect.Securities.Security

    SubscriptionsBag: System.Collections.Concurrent.ConcurrentBag[QuantConnect.Data.SubscriptionDataConfig]

    DefaultSettlementDays: int
    DefaultSettlementTime: TimeSpan


class OptionCache(QuantConnect.Securities.SecurityCache):
    """
    Option specific caching support
    
    OptionCache()
    """

class OptionDataFilter(QuantConnect.Securities.SecurityDataFilter, QuantConnect.Securities.Interfaces.ISecurityDataFilter):
    """
    Option packet by packet data filtering mechanism for dynamically detecting bad ticks.
    
    OptionDataFilter()
    """

class OptionExchange(QuantConnect.Securities.SecurityExchange):
    """
    Option exchange class - information and helper tools for option exchange properties
    
    OptionExchange(exchangeHours: SecurityExchangeHours)
    """
    def __init__(self, exchangeHours: QuantConnect.Securities.SecurityExchangeHours) -> QuantConnect.Securities.Option.OptionExchange:
        pass

    TradingDaysPerYear: int



class OptionHolding(QuantConnect.Securities.SecurityHolding):
    """
    Option holdings implementation of the base securities class
    
    OptionHolding(security: Option, currencyConverter: ICurrencyConverter)
    """
    def __init__(self, security: QuantConnect.Securities.Option.Option, currencyConverter: QuantConnect.Securities.ICurrencyConverter) -> QuantConnect.Securities.Option.OptionHolding:
        pass



class OptionMarginModel(QuantConnect.Securities.SecurityMarginModel, QuantConnect.Securities.IBuyingPowerModel):
    """
    Represents a simple option margin model.
    
    OptionMarginModel(requiredFreeBuyingPowerPercent: Decimal)
    """
    def GetLeverage(self, security: QuantConnect.Securities.Security) -> float:
        pass

    def SetLeverage(self, security: QuantConnect.Securities.Security, leverage: float) -> None:
        pass

    def __init__(self, requiredFreeBuyingPowerPercent: float) -> QuantConnect.Securities.Option.OptionMarginModel:
        pass

    RequiredFreeBuyingPowerPercent: float

class OptionPortfolioModel(QuantConnect.Securities.SecurityPortfolioModel, QuantConnect.Securities.ISecurityPortfolioModel):
    """
    Provides an implementation of QuantConnect.Securities.ISecurityPortfolioModel for options that supports
                default fills as well as option exercising.
    
    OptionPortfolioModel()
    """
    def ProcessExerciseFill(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, fill: QuantConnect.Orders.OrderEvent) -> None:
        pass

    def ProcessFill(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, fill: QuantConnect.Orders.OrderEvent) -> None:
        pass


class OptionPriceModelResult(System.object):
    """
    Result type for QuantConnect.Securities.Option.IOptionPriceModel.Evaluate(QuantConnect.Securities.Security,QuantConnect.Data.Slice,QuantConnect.Data.Market.OptionContract)
    
    OptionPriceModelResult(theoreticalPrice: Decimal, greeks: Greeks)
    OptionPriceModelResult(theoreticalPrice: Decimal, impliedVolatility: Func[Decimal], greeks: Func[Greeks])
    """
    @typing.overload
    def __init__(self, theoreticalPrice: float, greeks: QuantConnect.Data.Market.Greeks) -> QuantConnect.Securities.Option.OptionPriceModelResult:
        pass

    @typing.overload
    def __init__(self, theoreticalPrice: float, impliedVolatility: typing.Callable[[], float], greeks: typing.Callable[[], QuantConnect.Data.Market.Greeks]) -> QuantConnect.Securities.Option.OptionPriceModelResult:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Option.OptionPriceModelResult:
        pass

    Greeks: QuantConnect.Data.Market.Greeks

    ImpliedVolatility: float

    TheoreticalPrice: float



class OptionPriceModels(System.object):
    """ Static class contains definitions of major option pricing models that can be used in LEAN """
    @staticmethod
    def AdditiveEquiprobabilities() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BaroneAdesiWhaley() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialCoxRossRubinstein() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialJarrowRudd() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialJoshi() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialLeisenReimer() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialTian() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialTrigeorgis() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BjerksundStensland() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BlackScholes() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def CrankNicolsonFD() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def Integral() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    __all__: list
