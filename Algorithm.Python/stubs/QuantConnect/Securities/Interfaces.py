# encoding: utf-8
# module QuantConnect.Securities.Interfaces calls itself Interfaces
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Data
import QuantConnect.Securities
import QuantConnect.Securities.Interfaces
import System
import System.Collections.Generic
import typing

# no functions
# classes

class AdjustmentType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Enum defines types of possible price adjustments in continuous contract modeling.
    
    enum AdjustmentType, values: BackAdjusted (1), ForwardAdjusted (0)
    """
    value__: int
    BackAdjusted: 'AdjustmentType'
    ForwardAdjusted: 'AdjustmentType'


class IContinuousContractModel:
    """
    Continuous contract model interface. Interfaces is implemented by different classes
                realizing various methods for modeling continuous security series. Primarily, modeling of continuous futures.
                Continuous contracts are used in backtesting of otherwise expiring derivative contracts.
                Continuous contracts are not traded, and are not products traded on exchanges.
    """
    def GetContinuousData(self, dateTime: datetime.datetime) -> System.Collections.Generic.IEnumerator[QuantConnect.Data.BaseData]:
        pass

    def GetCurrentSymbol(self, dateTime: datetime.datetime) -> QuantConnect.Symbol:
        pass

    def GetRollDates(self) -> System.Collections.Generic.IEnumerator[datetime.datetime]:
        pass

    AdjustmentType: QuantConnect.Securities.Interfaces.AdjustmentType

    InputSeries: System.Collections.Generic.IEnumerator[QuantConnect.Data.BaseData]



class ISecurityDataFilter:
    """ Security data filter interface. Defines pattern for the user defined data filter techniques. """
    def Filter(self, vehicle: QuantConnect.Securities.Security, data: QuantConnect.Data.BaseData) -> bool:
        pass


