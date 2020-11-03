# encoding: utf-8
# module QuantConnect.Algorithm.Selection calls itself Selection
# from QuantConnect.Algorithm, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Algorithm
import QuantConnect.Algorithm.Selection
import QuantConnect.Data
import QuantConnect.Data.UniverseSelection
import QuantConnect.Securities
import System
import typing

# no functions
# classes

class OptionChainedUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    This universe selection model will chain to the security changes of a given QuantConnect.Data.UniverseSelection.Universe selection
                output and create a new QuantConnect.Data.UniverseSelection.OptionChainUniverse for each of them
    
    OptionChainedUniverseSelectionModel(universe: Universe, optionFilter: Func[OptionFilterUniverse, OptionFilterUniverse], universeSettings: UniverseSettings)
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def GetNextRefreshTimeUtc(self) -> datetime.datetime:
        pass

    def __init__(self, universe: QuantConnect.Data.UniverseSelection.Universe, optionFilter: typing.Callable[[QuantConnect.Securities.OptionFilterUniverse], QuantConnect.Securities.OptionFilterUniverse], universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Algorithm.Selection.OptionChainedUniverseSelectionModel:
        pass


class OptionContractUniverse(QuantConnect.Data.UniverseSelection.UserDefinedUniverse, System.IDisposable, QuantConnect.Data.UniverseSelection.ITimeTriggeredUniverse, System.Collections.Specialized.INotifyCollectionChanged):
    """
    This universe will hold single option contracts and their underlying, managing removals and additions
    
    OptionContractUniverse(configuration: SubscriptionDataConfig, universeSettings: UniverseSettings)
    """
    @staticmethod
    def CreateSymbol(market: str, securityType: QuantConnect.SecurityType) -> QuantConnect.Symbol:
        pass

    def SelectSymbols(self, utcTime: datetime.datetime, data: QuantConnect.Data.UniverseSelection.BaseDataCollection) -> typing.List[QuantConnect.Symbol]:
        pass

    def __init__(self, configuration: QuantConnect.Data.SubscriptionDataConfig, universeSettings: QuantConnect.Data.UniverseSelection.UniverseSettings) -> QuantConnect.Algorithm.Selection.OptionContractUniverse:
        pass


