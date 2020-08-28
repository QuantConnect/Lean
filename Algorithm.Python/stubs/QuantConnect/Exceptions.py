# encoding: utf-8
# module QuantConnect.Exceptions calls itself Exceptions
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect.Exceptions
import System
import System.Reflection
import typing

# no functions
# classes

class DllNotFoundPythonExceptionInterpreter(System.object, QuantConnect.Exceptions.IExceptionInterpreter):
    """
    Interprets QuantConnect.Exceptions.DllNotFoundPythonExceptionInterpreter instances
    
    DllNotFoundPythonExceptionInterpreter()
    """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    Order: int



class IExceptionInterpreter:
    """ Defines an exception interpreter. Interpretations are invoked on QuantConnect.Interfaces.IAlgorithm.RunTimeError """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    Order: int



class InvalidTokenPythonExceptionInterpreter(System.object, QuantConnect.Exceptions.IExceptionInterpreter):
    """
    Interprets QuantConnect.Exceptions.InvalidTokenPythonExceptionInterpreter instances
    
    InvalidTokenPythonExceptionInterpreter()
    """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    Order: int



class KeyErrorPythonExceptionInterpreter(System.object, QuantConnect.Exceptions.IExceptionInterpreter):
    """
    Interprets QuantConnect.Exceptions.KeyErrorPythonExceptionInterpreter instances
    
    KeyErrorPythonExceptionInterpreter()
    """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    Order: int



class NoMethodMatchPythonExceptionInterpreter(System.object, QuantConnect.Exceptions.IExceptionInterpreter):
    """
    Interprets QuantConnect.Exceptions.NoMethodMatchPythonExceptionInterpreter instances
    
    NoMethodMatchPythonExceptionInterpreter()
    """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    Order: int



class PythonExceptionInterpreter(System.object, QuantConnect.Exceptions.IExceptionInterpreter):
    """
    Interprets QuantConnect.Exceptions.PythonExceptionInterpreter instances
    
    PythonExceptionInterpreter()
    """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    Order: int



class ScheduledEventExceptionInterpreter(System.object, QuantConnect.Exceptions.IExceptionInterpreter):
    """
    Interprets QuantConnect.Scheduling.ScheduledEventException instances
    
    ScheduledEventExceptionInterpreter()
    """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    Order: int



class StackExceptionInterpreter(System.object, QuantConnect.Exceptions.IExceptionInterpreter):
    """
    Interprets exceptions using the configured interpretations
    
    StackExceptionInterpreter(interpreters: IEnumerable[IExceptionInterpreter])
    """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    @staticmethod
    def CreateFromAssemblies(assemblies: typing.List[System.Reflection.Assembly]) -> QuantConnect.Exceptions.StackExceptionInterpreter:
        pass

    def GetExceptionMessageHeader(self, exception: System.Exception) -> str:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    def __init__(self, interpreters: typing.List[QuantConnect.Exceptions.IExceptionInterpreter]) -> QuantConnect.Exceptions.StackExceptionInterpreter:
        pass

    Interpreters: typing.List[QuantConnect.Exceptions.IExceptionInterpreter]

    Order: int



class UnsupportedOperandPythonExceptionInterpreter(System.object, QuantConnect.Exceptions.IExceptionInterpreter):
    """
    Interprets QuantConnect.Exceptions.UnsupportedOperandPythonExceptionInterpreter instances
    
    UnsupportedOperandPythonExceptionInterpreter()
    """
    def CanInterpret(self, exception: System.Exception) -> bool:
        pass

    def Interpret(self, exception: System.Exception, innerInterpreter: QuantConnect.Exceptions.IExceptionInterpreter) -> System.Exception:
        pass

    Order: int



