# encoding: utf-8
# module QuantConnect.Orders.Fills calls itself Fills
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect.Interfaces
import QuantConnect.Orders
import QuantConnect.Orders.Fills
import QuantConnect.Python
import QuantConnect.Securities
import System
import typing

# no functions
# classes

class Fill(System.object):
    """
    Defines the result for QuantConnect.Orders.Fills.IFillModel.Fill(QuantConnect.Orders.Fills.FillModelParameters)
    
    Fill(orderEvent: OrderEvent)
    """
    def __init__(self, orderEvent: QuantConnect.Orders.OrderEvent) -> QuantConnect.Orders.Fills.Fill:
        pass

    OrderEvent: QuantConnect.Orders.OrderEvent



class FillModel(System.object, QuantConnect.Orders.Fills.IFillModel):
    """
    Provides a base class for all fill models
    
    FillModel()
    """
    def Fill(self, parameters: QuantConnect.Orders.Fills.FillModelParameters) -> QuantConnect.Orders.Fills.Fill:
        pass

    def LimitFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.LimitOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def MarketFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.MarketOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def MarketOnCloseFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.MarketOnCloseOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def MarketOnOpenFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.MarketOnOpenOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def SetPythonWrapper(self, pythonWrapper: QuantConnect.Python.FillModelPythonWrapper) -> None:
        pass

    def StopLimitFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.StopLimitOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    def StopMarketFill(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.StopMarketOrder) -> QuantConnect.Orders.OrderEvent:
        pass

    PythonWrapper: QuantConnect.Python.FillModelPythonWrapper

    Prices: type


class FillModelParameters(System.object):
    """
    Defines the parameters for the QuantConnect.Orders.Fills.IFillModel method
    
    FillModelParameters(security: Security, order: Order, configProvider: ISubscriptionDataConfigProvider, stalePriceTimeSpan: TimeSpan)
    """
    def __init__(self, security: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order, configProvider: QuantConnect.Interfaces.ISubscriptionDataConfigProvider, stalePriceTimeSpan: datetime.timedelta) -> QuantConnect.Orders.Fills.FillModelParameters:
        pass

    ConfigProvider: QuantConnect.Interfaces.ISubscriptionDataConfigProvider

    Order: QuantConnect.Orders.Order

    Security: QuantConnect.Securities.Security

    StalePriceTimeSpan: datetime.timedelta



class IFillModel:
    """ Represents a model that simulates order fill events """
    def Fill(self, parameters: QuantConnect.Orders.Fills.FillModelParameters) -> QuantConnect.Orders.Fills.Fill:
        pass


class ImmediateFillModel(QuantConnect.Orders.Fills.FillModel, QuantConnect.Orders.Fills.IFillModel):
    """
    Represents the default fill model used to simulate order fills
    
    ImmediateFillModel()
    """
    PythonWrapper: QuantConnect.Python.FillModelPythonWrapper


class LatestPriceFillModel(QuantConnect.Orders.Fills.ImmediateFillModel, QuantConnect.Orders.Fills.IFillModel):
    """
    This fill model is provided because currently the data sourced for Crypto
                is limited to one minute snapshots for Quote data. This fill model will
                ignore the trade/quote distinction and return the latest pricing information
                in order to determine the correct fill price
    
    LatestPriceFillModel()
    """
    PythonWrapper: QuantConnect.Python.FillModelPythonWrapper


