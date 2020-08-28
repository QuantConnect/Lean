# encoding: utf-8
# module QuantConnect.Orders.Fees calls itself Fees
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect.Orders
import QuantConnect.Orders.Fees
import QuantConnect.Securities
import System
import typing

# no functions
# classes

class FeeModel(System.object, QuantConnect.Orders.Fees.IFeeModel):
    """
    Base class for any order fee model
    
    FeeModel()
    """
    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass


class AlphaStreamsFeeModel(QuantConnect.Orders.Fees.FeeModel, QuantConnect.Orders.Fees.IFeeModel):
    """
    Provides an implementation of QuantConnect.Orders.Fees.FeeModel that models order fees that alpha stream clients pay/receive
    
    AlphaStreamsFeeModel()
    """
    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass


class BitfinexFeeModel(QuantConnect.Orders.Fees.FeeModel, QuantConnect.Orders.Fees.IFeeModel):
    """
    Provides an implementation of QuantConnect.Orders.Fees.FeeModel that models Bitfinex order fees
    
    BitfinexFeeModel()
    """
    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass

    MakerFee: Decimal
    TakerFee: Decimal


class ConstantFeeModel(QuantConnect.Orders.Fees.FeeModel, QuantConnect.Orders.Fees.IFeeModel):
    """
    Provides an order fee model that always returns the same order fee.
    
    ConstantFeeModel(fee: Decimal, currency: str)
    """
    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass

    def __init__(self, fee: float, currency: str) -> QuantConnect.Orders.Fees.ConstantFeeModel:
        pass


class FeeModelExtensions(System.object):
    """
    Provide extension method for QuantConnect.Orders.Fees.IFeeModel to enable
                backwards compatibility of invocations.
    """
    @staticmethod
    def GetOrderFee(model: QuantConnect.Orders.Fees.IFeeModel, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> float:
        pass

    __all__: list


class FxcmFeeModel(QuantConnect.Orders.Fees.FeeModel, QuantConnect.Orders.Fees.IFeeModel):
    """
    Provides an implementation of QuantConnect.Orders.Fees.FeeModel that models FXCM order fees
    
    FxcmFeeModel(currency: str)
    """
    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass

    def __init__(self, currency: str) -> QuantConnect.Orders.Fees.FxcmFeeModel:
        pass


class GDAXFeeModel(QuantConnect.Orders.Fees.FeeModel, QuantConnect.Orders.Fees.IFeeModel):
    """
    Provides an implementation of QuantConnect.Orders.Fees.FeeModel that models GDAX order fees
    
    GDAXFeeModel()
    """
    @staticmethod
    def GetFeePercentage(utcTime: datetime.datetime, isMaker: bool) -> float:
        pass

    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass


class IFeeModel:
    """ Represents a model the simulates order fees """
    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass


class InteractiveBrokersFeeModel(QuantConnect.Orders.Fees.FeeModel, QuantConnect.Orders.Fees.IFeeModel):
    """
    Provides the default implementation of QuantConnect.Orders.Fees.IFeeModel
    
    InteractiveBrokersFeeModel(monthlyForexTradeAmountInUSDollars: Decimal, monthlyOptionsTradeAmountInContracts: Decimal)
    """
    def GetOrderFee(self, parameters: QuantConnect.Orders.Fees.OrderFeeParameters) -> QuantConnect.Orders.Fees.OrderFee:
        pass

    def __init__(self, monthlyForexTradeAmountInUSDollars: float, monthlyOptionsTradeAmountInContracts: float) -> QuantConnect.Orders.Fees.InteractiveBrokersFeeModel:
        pass


class OrderFee(System.object):
    """
    Defines the result for QuantConnect.Orders.Fees.IFeeModel.GetOrderFee(QuantConnect.Orders.Fees.OrderFeeParameters)
    
    OrderFee(orderFee: CashAmount)
    """
    def ToString(self) -> str:
        pass

    def __init__(self, orderFee: QuantConnect.Securities.CashAmount) -> QuantConnect.Orders.Fees.OrderFee:
        pass

    Value: QuantConnect.Securities.CashAmount


    Zero: 'OrderFee'


class OrderFeeParameters(System.object):
    """
    Defines the parameters for QuantConnect.Orders.Fees.IFeeModel.GetOrderFee(QuantConnect.Orders.Fees.OrderFeeParameters)
    
    OrderFeeParameters(security: Security, order: Order)
    """
    def __init__(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> QuantConnect.Orders.Fees.OrderFeeParameters:
        pass

    Order: QuantConnect.Orders.Order

    Security: QuantConnect.Securities.Security



