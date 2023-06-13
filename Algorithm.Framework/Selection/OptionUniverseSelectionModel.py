# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from AlgorithmImports import *
from Selection.UniverseSelectionModel import UniverseSelectionModel

class OptionUniverseSelectionModel(UniverseSelectionModel):
    '''Provides an implementation of IUniverseSelectionMode that subscribes to option chains'''
    def __init__(self,
                 refreshInterval,
                 optionChainSymbolSelector,
                 universeSettings = None):
        '''Creates a new instance of OptionUniverseSelectionModel
        Args:
            refreshInterval: Time interval between universe refreshes</param>
            optionChainSymbolSelector: Selects symbols from the provided option chain
            universeSettings: Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed'''
        self.nextRefreshTimeUtc = datetime.min

        self.refreshInterval = refreshInterval
        self.optionChainSymbolSelector = optionChainSymbolSelector
        self.universeSettings = universeSettings

    def GetNextRefreshTimeUtc(self):
        '''Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.'''
        return self.nextRefreshTimeUtc

    def CreateUniverses(self, algorithm):
        '''Creates a new fundamental universe using this class's selection functions
        Args:
            algorithm: The algorithm instance to create universes for
        Returns:
            The universe defined by this model'''
        self.nextRefreshTimeUtc = (algorithm.UtcTime + self.refreshInterval).date()

        uniqueUnderlyingSymbols = set()
        for optionSymbol in self.optionChainSymbolSelector(algorithm.UtcTime):
            if not Extensions.IsOption(optionSymbol.SecurityType):
                raise ValueError("optionChainSymbolSelector must return option, index options, or futures options symbols.")

            # prevent creating duplicate option chains -- one per underlying
            if optionSymbol.Underlying not in uniqueUnderlyingSymbols:
                uniqueUnderlyingSymbols.add(optionSymbol.Underlying)
                yield Extensions.CreateOptionChain(algorithm, optionSymbol, self.Filter, self.universeSettings)

    def Filter(self, filter):
        '''Defines the option chain universe filter'''
        # NOP
        return filter
