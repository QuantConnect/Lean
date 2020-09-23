import typing
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Storage
import QuantConnect.Securities.Option
import QuantConnect.Securities.Future
import QuantConnect.Securities.Forex
import QuantConnect.Securities.Equity
import QuantConnect.Securities.Crypto
import QuantConnect.Securities.Cfd
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Python
import QuantConnect.Orders
import QuantConnect.Notifications
import QuantConnect.Interfaces
import QuantConnect.Indicators.CandlestickPatterns
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Fundamental
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Benchmarks
import QuantConnect.Algorithm.Framework.Selection
import QuantConnect.Algorithm.Framework.Risk
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Execution
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import pandas
import NodaTime
import datetime


class UniverseDefinitions(System.object):
    """
    Provides helpers for defining universes in algorithms
    
    UniverseDefinitions(algorithm: QCAlgorithm)
    """
    def __init__(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> QuantConnect.Algorithm.UniverseDefinitions:
        pass

    Constituent: QuantConnect.Algorithm.ConstituentUniverseDefinitions

    DollarVolume: QuantConnect.Algorithm.DollarVolumeUniverseDefinitions

    Index: QuantConnect.Algorithm.IndexUniverseDefinitions

    Unchanged: QuantConnect.Data.UniverseSelection.UnchangedUniverse
