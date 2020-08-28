import typing
import System.Threading
import System.IO
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Storage
import QuantConnect.Statistics
import QuantConnect.Securities.Option
import QuantConnect.Securities.Future
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Packets
import QuantConnect.Orders
import QuantConnect.Notifications
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Auxiliary
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Benchmarks
import QuantConnect.Api
import QuantConnect.API
import QuantConnect
import Python.Runtime
import NodaTime
import datetime



class ObjectStoreErrorRaisedEventArgs(System.EventArgs):
    """
    Event arguments for the QuantConnect.Interfaces.IObjectStore.ErrorRaised event
    
    ObjectStoreErrorRaisedEventArgs(error: Exception)
    """
    def __init__(self, error: System.Exception) -> QuantConnect.Interfaces.ObjectStoreErrorRaisedEventArgs:
        pass

    Error: System.Exception
