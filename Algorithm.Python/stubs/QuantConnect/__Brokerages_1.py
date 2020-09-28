from .__Brokerages_2 import *
import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Packets
import QuantConnect.Orders.Slippage
import QuantConnect.Orders.Fills
import QuantConnect.Orders.Fees
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Data.Market
import QuantConnect.Brokerages
import QuantConnect
import datetime


class DowngradeErrorCodeToWarningBrokerageMessageHandler(System.object, QuantConnect.Brokerages.IBrokerageMessageHandler):
    """
    Provides an implementation of QuantConnect.Brokerages.IBrokerageMessageHandler that converts specified error codes into warnings
    
    DowngradeErrorCodeToWarningBrokerageMessageHandler(brokerageMessageHandler: IBrokerageMessageHandler, errorCodesToIgnore: Array[str])
    """
    def Handle(self, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> None:
        pass

    def __init__(self, brokerageMessageHandler: QuantConnect.Brokerages.IBrokerageMessageHandler, errorCodesToIgnore: typing.List[str]) -> QuantConnect.Brokerages.DowngradeErrorCodeToWarningBrokerageMessageHandler:
        pass


class FxcmBrokerageModel(QuantConnect.Brokerages.DefaultBrokerageModel, QuantConnect.Brokerages.IBrokerageModel):
    """
    Provides FXCM specific properties
    
    FxcmBrokerageModel(accountType: AccountType)
    """
    def CanSubmitOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def CanUpdateOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, request: QuantConnect.Orders.UpdateOrderRequest, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetFillModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fills.IFillModel:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.ISettlementModel:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSettlementModel(self, *args) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSlippageModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Slippage.ISlippageModel:
        pass

    def __init__(self, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.FxcmBrokerageModel:
        pass

    DefaultMarkets: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str]


    DefaultMarketMap: ReadOnlyDictionary[SecurityType, str]


class GDAXBrokerageModel(QuantConnect.Brokerages.DefaultBrokerageModel, QuantConnect.Brokerages.IBrokerageModel):
    """
    Provides GDAX specific properties
    
    GDAXBrokerageModel(accountType: AccountType)
    """
    def CanSubmitOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def CanUpdateOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, request: QuantConnect.Orders.UpdateOrderRequest, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    @typing.overload
    def GetBuyingPowerModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    @typing.overload
    def GetBuyingPowerModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    def GetBuyingPowerModel(self, *args) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetFillModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fills.IFillModel:
        pass

    def GetLeverage(self, security: QuantConnect.Securities.Security) -> float:
        pass

    def __init__(self, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.GDAXBrokerageModel:
        pass


class IBrokerageMessageHandler:
    """
    Provides an plugin point to allow algorithms to directly handle the messages
                that come from their brokerage
    """
    def Handle(self, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> None:
        pass


class IBrokerageModel:
    """ Models brokerage transactions, fees, and order """
    def ApplySplit(self, tickets: typing.List[QuantConnect.Orders.OrderTicket], split: QuantConnect.Data.Market.Split) -> None:
        pass

    def CanExecuteOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> bool:
        pass

    def CanSubmitOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def CanUpdateOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, request: QuantConnect.Orders.UpdateOrderRequest, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    @typing.overload
    def GetBuyingPowerModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    @typing.overload
    def GetBuyingPowerModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    def GetBuyingPowerModel(self, *args) -> QuantConnect.Securities.IBuyingPowerModel:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetFillModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fills.IFillModel:
        pass

    def GetLeverage(self, security: QuantConnect.Securities.Security) -> float:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.ISettlementModel:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSettlementModel(self, *args) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSlippageModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Slippage.ISlippageModel:
        pass

    AccountType: QuantConnect.AccountType

    DefaultMarkets: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str]

    RequiredFreeBuyingPowerPercent: float



class InteractiveBrokersBrokerageModel(QuantConnect.Brokerages.DefaultBrokerageModel, QuantConnect.Brokerages.IBrokerageModel):
    """
    Provides properties specific to interactive brokers
    
    InteractiveBrokersBrokerageModel(accountType: AccountType)
    """
    def CanExecuteOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> bool:
        pass

    def CanSubmitOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def CanUpdateOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, request: QuantConnect.Orders.UpdateOrderRequest, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def __init__(self, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.InteractiveBrokersBrokerageModel:
        pass

    DefaultMarkets: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str]


    DefaultMarketMap: ReadOnlyDictionary[SecurityType, str]


class OandaBrokerageModel(QuantConnect.Brokerages.DefaultBrokerageModel, QuantConnect.Brokerages.IBrokerageModel):
    """
    Oanda Brokerage Model Implementation for Back Testing.
    
    OandaBrokerageModel(accountType: AccountType)
    """
    def CanSubmitOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetFillModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fills.IFillModel:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Securities.ISettlementModel:
        pass

    @typing.overload
    def GetSettlementModel(self, security: QuantConnect.Securities.Security, accountType: QuantConnect.AccountType) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSettlementModel(self, *args) -> QuantConnect.Securities.ISettlementModel:
        pass

    def GetSlippageModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Slippage.ISlippageModel:
        pass

    def __init__(self, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.OandaBrokerageModel:
        pass

    DefaultMarkets: System.Collections.Generic.IReadOnlyDictionary[QuantConnect.SecurityType, str]


    DefaultMarketMap: ReadOnlyDictionary[SecurityType, str]


class TradierBrokerageModel(QuantConnect.Brokerages.DefaultBrokerageModel, QuantConnect.Brokerages.IBrokerageModel):
    """
    Provides tradier specific properties
    
    TradierBrokerageModel(accountType: AccountType)
    """
    def ApplySplit(self, tickets: typing.List[QuantConnect.Orders.OrderTicket], split: QuantConnect.Data.Market.Split) -> None:
        pass

    def CanExecuteOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> bool:
        pass

    def CanSubmitOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def CanUpdateOrder(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, request: QuantConnect.Orders.UpdateOrderRequest, message: QuantConnect.Brokerages.BrokerageMessageEvent) -> bool:
        pass

    def GetFeeModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fees.IFeeModel:
        pass

    def GetFillModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Fills.IFillModel:
        pass

    def GetSlippageModel(self, security: QuantConnect.Securities.Security) -> QuantConnect.Orders.Slippage.ISlippageModel:
        pass

    def __init__(self, accountType: QuantConnect.AccountType) -> QuantConnect.Brokerages.TradierBrokerageModel:
        pass
