# encoding: utf-8
# module QuantConnect.Orders.OptionExercise calls itself OptionExercise
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect.Orders
import QuantConnect.Securities.Option
import typing

# no functions
# classes

class DefaultExerciseModel(System.object, QuantConnect.Orders.OptionExercise.IOptionExerciseModel):
    """
    Represents the default option exercise model (physical, cash settlement)
    
    DefaultExerciseModel()
    """
    def OptionExercise(self, option: QuantConnect.Securities.Option.Option, order: QuantConnect.Orders.OptionExerciseOrder) -> typing.List[QuantConnect.Orders.OrderEvent]:
        pass


class IOptionExerciseModel:
    """ Represents a model that simulates option exercise and lapse events """
    def OptionExercise(self, option: QuantConnect.Securities.Option.Option, order: QuantConnect.Orders.OptionExerciseOrder) -> typing.List[QuantConnect.Orders.OrderEvent]:
        pass


