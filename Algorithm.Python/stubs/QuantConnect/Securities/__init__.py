from .____init___1 import *
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

# no functions
# classes

class AccountCurrencyImmediateSettlementModel(System.object, QuantConnect.Securities.ISettlementModel):
    """
    Represents the model responsible for applying cash settlement rules
    
    AccountCurrencyImmediateSettlementModel()
    """
    def ApplyFunds(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, applicationTimeUtc: datetime.datetime, currency: str, amount: float) -> None:
        pass


class AccountEvent(System.object):
    """
    Messaging class signifying a change in a user's account
    
    AccountEvent(currencySymbol: str, cashBalance: Decimal)
    """
    def ToString(self) -> str:
        pass

    def __init__(self, currencySymbol: str, cashBalance: float) -> QuantConnect.Securities.AccountEvent:
        pass

    CashBalance: float

    CurrencySymbol: str



class AdjustedPriceVariationModel(System.object, QuantConnect.Securities.IPriceVariationModel):
    """
    Provides an implementation of QuantConnect.Securities.IPriceVariationModel
                for use when data is QuantConnect.DataNormalizationMode.Adjusted.
    
    AdjustedPriceVariationModel()
    """
    def GetMinimumPriceVariation(self, parameters: QuantConnect.Securities.GetMinimumPriceVariationParameters) -> float:
        pass


class BrokerageModelSecurityInitializer(System.object, QuantConnect.Securities.ISecurityInitializer):
    """
    Provides an implementation of QuantConnect.Securities.ISecurityInitializer that initializes a security
                by settings the QuantConnect.Securities.Security.FillModel, QuantConnect.Securities.Security.FeeModel,
                QuantConnect.Securities.Security.SlippageModel, and the QuantConnect.Securities.Security.SettlementModel properties
    
    BrokerageModelSecurityInitializer()
    BrokerageModelSecurityInitializer(brokerageModel: IBrokerageModel, securitySeeder: ISecuritySeeder)
    """
    def Initialize(self, security: QuantConnect.Securities.Security) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Securities.BrokerageModelSecurityInitializer:
        pass

    @typing.overload
    def __init__(self, brokerageModel: QuantConnect.Brokerages.IBrokerageModel, securitySeeder: QuantConnect.Securities.ISecuritySeeder) -> QuantConnect.Securities.BrokerageModelSecurityInitializer:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.BrokerageModelSecurityInitializer:
        pass


class BuyingPower(System.object):
    """
    Defines the result for QuantConnect.Securities.IBuyingPowerModel.GetBuyingPower(QuantConnect.Securities.BuyingPowerParameters)
    
    BuyingPower(buyingPower: Decimal)
    """
    def __init__(self, buyingPower: float) -> QuantConnect.Securities.BuyingPower:
        pass

    Value: float



class BuyingPowerModel(System.object, QuantConnect.Securities.IBuyingPowerModel):
    """
    Provides a base class for all buying power models
    
    BuyingPowerModel()
    BuyingPowerModel(initialMarginRequirement: Decimal, maintenanceMarginRequirement: Decimal, requiredFreeBuyingPowerPercent: Decimal)
    BuyingPowerModel(leverage: Decimal, requiredFreeBuyingPowerPercent: Decimal)
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

    @typing.overload
    def __init__(self) -> QuantConnect.Securities.BuyingPowerModel:
        pass

    @typing.overload
    def __init__(self, initialMarginRequirement: float, maintenanceMarginRequirement: float, requiredFreeBuyingPowerPercent: float) -> QuantConnect.Securities.BuyingPowerModel:
        pass

    @typing.overload
    def __init__(self, leverage: float, requiredFreeBuyingPowerPercent: float) -> QuantConnect.Securities.BuyingPowerModel:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.BuyingPowerModel:
        pass

    RequiredFreeBuyingPowerPercent: float

class BuyingPowerModelExtensions(System.object):
    """ Provides extension methods as backwards compatibility shims """
    @staticmethod
    def GetBuyingPower(model: QuantConnect.Securities.IBuyingPowerModel, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, direction: QuantConnect.Orders.OrderDirection) -> float:
        pass

    @staticmethod
    def GetMaximumOrderQuantityForTargetBuyingPower(model: QuantConnect.Securities.IBuyingPowerModel, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, target: float) -> QuantConnect.Securities.GetMaximumOrderQuantityResult:
        pass

    @staticmethod
    def GetReservedBuyingPowerForPosition(model: QuantConnect.Securities.IBuyingPowerModel, security: QuantConnect.Securities.Security) -> float:
        pass

    @staticmethod
    def HasSufficientBuyingPowerForOrder(model: QuantConnect.Securities.IBuyingPowerModel, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> QuantConnect.Securities.HasSufficientBuyingPowerForOrderResult:
        pass

    __all__: list


class BuyingPowerParameters(System.object):
    """
    Defines the parameters for QuantConnect.Securities.IBuyingPowerModel.GetBuyingPower(QuantConnect.Securities.BuyingPowerParameters)
    
    BuyingPowerParameters(portfolio: SecurityPortfolioManager, security: Security, direction: OrderDirection)
    """
    def Result(self, buyingPower: float, currency: str) -> QuantConnect.Securities.BuyingPower:
        pass

    def ResultInAccountCurrency(self, buyingPower: float) -> QuantConnect.Securities.BuyingPower:
        pass

    def __init__(self, portfolio: QuantConnect.Securities.SecurityPortfolioManager, security: QuantConnect.Securities.Security, direction: QuantConnect.Orders.OrderDirection) -> QuantConnect.Securities.BuyingPowerParameters:
        pass

    Direction: QuantConnect.Orders.OrderDirection

    Portfolio: QuantConnect.Securities.SecurityPortfolioManager

    Security: QuantConnect.Securities.Security



class Cash(System.object):
    """
    Represents a holding of a currency in cash.
    
    Cash(symbol: str, amount: Decimal, conversionRate: Decimal)
    """
    def AddAmount(self, amount: float) -> float:
        pass

    def EnsureCurrencyDataFeed(self, securities: QuantConnect.Securities.SecurityManager, subscriptions: QuantConnect.Data.SubscriptionManager, marketMap: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str], changes: QuantConnect.Data.UniverseSelection.SecurityChanges, securityService: QuantConnect.Interfaces.ISecurityService, accountCurrency: str, defaultResolution: QuantConnect.Resolution) -> QuantConnect.Data.SubscriptionDataConfig:
        pass

    def SetAmount(self, amount: float) -> None:
        pass

    def ToString(self) -> str:
        pass

    def Update(self, data: QuantConnect.Data.BaseData) -> None:
        pass

    def __init__(self, symbol: str, amount: float, conversionRate: float) -> QuantConnect.Securities.Cash:
        pass

    Amount: float

    ConversionRate: float

    ConversionRateSecurity: QuantConnect.Securities.Security

    CurrencySymbol: str

    SecuritySymbol: QuantConnect.Symbol

    Symbol: str

    ValueInAccountCurrency: float


    Updated: BoundEvent


class CashAmount(System.object):
    """
    Represents a cash amount which can be converted to account currency using a currency converter
    
    CashAmount(amount: Decimal, currency: str)
    """
    def Equals(self, obj: object) -> bool:
        pass

    def __init__(self, amount: float, currency: str) -> QuantConnect.Securities.CashAmount:
        pass

    Amount: float

    Currency: str



class ICurrencyConverter:
    """ Provides the ability to convert cash amounts to the account currency """
    def ConvertToAccountCurrency(self, cashAmount: QuantConnect.Securities.CashAmount) -> QuantConnect.Securities.CashAmount:
        pass

    AccountCurrency: str



class CashBook(System.object, System.Collections.IEnumerable, QuantConnect.Securities.ICurrencyConverter, System.Collections.Generic.ICollection[KeyValuePair[str, Cash]], System.Collections.Generic.IDictionary[str, Cash], System.Collections.Generic.IEnumerable[KeyValuePair[str, Cash]]):
    """
    Provides a means of keeping track of the different cash holdings of an algorithm
    
    CashBook()
    """
    @typing.overload
    def Add(self, symbol: str, quantity: float, conversionRate: float) -> None:
        pass

    @typing.overload
    def Add(self, item: System.Collections.Generic.KeyValuePair[str, QuantConnect.Securities.Cash]) -> None:
        pass

    @typing.overload
    def Add(self, symbol: str, value: QuantConnect.Securities.Cash) -> None:
        pass

    def Add(self, *args) -> None:
        pass

    def Clear(self) -> None:
        pass

    def Contains(self, item: System.Collections.Generic.KeyValuePair[str, QuantConnect.Securities.Cash]) -> bool:
        pass

    def ContainsKey(self, symbol: str) -> bool:
        pass

    def Convert(self, sourceQuantity: float, sourceCurrency: str, destinationCurrency: str) -> float:
        pass

    @typing.overload
    def ConvertToAccountCurrency(self, sourceQuantity: float, sourceCurrency: str) -> float:
        pass

    @typing.overload
    def ConvertToAccountCurrency(self, cashAmount: QuantConnect.Securities.CashAmount) -> QuantConnect.Securities.CashAmount:
        pass

    def ConvertToAccountCurrency(self, *args) -> QuantConnect.Securities.CashAmount:
        pass

    def CopyTo(self, array: typing.List[System.Collections.Generic.KeyValuePair], arrayIndex: int) -> None:
        pass

    def EnsureCurrencyDataFeeds(self, securities: QuantConnect.Securities.SecurityManager, subscriptions: QuantConnect.Data.SubscriptionManager, marketMap: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str], changes: QuantConnect.Data.UniverseSelection.SecurityChanges, securityService: QuantConnect.Interfaces.ISecurityService, defaultResolution: QuantConnect.Resolution) -> typing.List[QuantConnect.Data.SubscriptionDataConfig]:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[System.Collections.Generic.KeyValuePair[str, QuantConnect.Securities.Cash]]:
        pass

    @typing.overload
    def Remove(self, symbol: str) -> bool:
        pass

    @typing.overload
    def Remove(self, item: System.Collections.Generic.KeyValuePair[str, QuantConnect.Securities.Cash]) -> bool:
        pass

    def Remove(self, *args) -> bool:
        pass

    def ToString(self) -> str:
        pass

    def TryGetValue(self, symbol: str, value: QuantConnect.Securities.Cash) -> bool:
        pass

    AccountCurrency: str

    Count: int

    IsReadOnly: bool

    Keys: typing.List[str]

    TotalValueInAccountCurrency: float

    Values: typing.List[QuantConnect.Securities.Cash]


    Item: indexer#
    Updated: BoundEvent
    UpdateType: type
