# encoding: utf-8
# module QuantConnect.Orders.Slippage calls itself Slippage
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect.Orders
import QuantConnect.Orders.Slippage
import QuantConnect.Securities
import typing

# no functions
# classes

class AlphaStreamsSlippageModel(System.object, QuantConnect.Orders.Slippage.ISlippageModel):
    """
    Represents a slippage model that uses a constant percentage of slip
    
    AlphaStreamsSlippageModel()
    """
    def GetSlippageApproximation(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> float:
        pass


class ConstantSlippageModel(System.object, QuantConnect.Orders.Slippage.ISlippageModel):
    """
    Represents a slippage model that uses a constant percentage of slip
    
    ConstantSlippageModel(slippagePercent: Decimal)
    """
    def GetSlippageApproximation(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> float:
        pass

    def __init__(self, slippagePercent: float) -> QuantConnect.Orders.Slippage.ConstantSlippageModel:
        pass


class ISlippageModel:
    """ Represents a model that simulates market order slippage """
    def GetSlippageApproximation(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> float:
        pass


class VolumeShareSlippageModel(System.object, QuantConnect.Orders.Slippage.ISlippageModel):
    """
    Represents a slippage model that is calculated by multiplying the price impact constant
                by the square of the ratio of the order to the total volume.
    
    VolumeShareSlippageModel(volumeLimit: Decimal, priceImpact: Decimal)
    """
    def GetSlippageApproximation(self, asset: QuantConnect.Securities.Security, order: QuantConnect.Orders.Order) -> float:
        pass

    def __init__(self, volumeLimit: float, priceImpact: float) -> QuantConnect.Orders.Slippage.VolumeShareSlippageModel:
        pass


