import typing
import System.Xml.Linq
import System.Threading
import System.Text
import System.Linq.Expressions
import System.Linq
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Util
import QuantConnect.Securities
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import NodaTime
import Newtonsoft.Json
import datetime


class WorkerThread(System.object, System.IDisposable):
    """
    This worker tread is required to guarantee all python operations are
                executed by the same thread, to enable complete debugging functionality.
                We don't use the main thread, to avoid any chance of blocking the process
    """
    def Add(self, action: System.Action) -> None:
        pass

    def Dispose(self) -> None:
        pass

    FinishedWorkItem: System.Threading.AutoResetEvent


    Instance: 'WorkerThread'


class XElementExtensions(System.object):
    """ Provides extension methods for the XML to LINQ types """
    @staticmethod
    def Get(element: System.Xml.Linq.XElement, name: str) -> QuantConnect.Util.T:
        pass

    __all__: list
