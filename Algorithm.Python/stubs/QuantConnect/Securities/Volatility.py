# encoding: utf-8
# module QuantConnect.Securities.Volatility calls itself Volatility
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect.Data
import QuantConnect.Interfaces
import QuantConnect.Securities
import System
import typing

# no functions
# classes

class BaseVolatilityModel(System.object, QuantConnect.Securities.IVolatilityModel):
    """
    Represents a base model that computes the volatility of a security
    
    BaseVolatilityModel()
    """
    def GetHistoryRequirements(self, security: QuantConnect.Securities.Security, utcTime: datetime.datetime) -> typing.List[QuantConnect.Data.HistoryRequest]:
        pass

    def SetSubscriptionDataConfigProvider(self, subscriptionDataConfigProvider: QuantConnect.Interfaces.ISubscriptionDataConfigProvider) -> None:
        pass

    def Update(self, security: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> None:
        pass

    Volatility: float

    SubscriptionDataConfigProvider: QuantConnect.Interfaces.ISubscriptionDataConfigProvider


