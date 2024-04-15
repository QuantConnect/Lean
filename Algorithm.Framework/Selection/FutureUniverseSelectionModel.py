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

class FutureUniverseSelectionModel(UniverseSelectionModel):
    '''Provides an implementation of IUniverseSelectionMode that subscribes to future chains'''
    def __init__(self,
                 refreshInterval,
                 futureChainSymbolSelector,
                 universeSettings = None):
        '''Creates a new instance of FutureUniverseSelectionModel
        Args:
            refreshInterval: Time interval between universe refreshes</param>
            futureChainSymbolSelector: Selects symbols from the provided future chain
            universeSettings: Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed'''
        self.next_refresh_time_utc = datetime.min

        self.refresh_interval = refreshInterval
        self.future_chain_symbol_selector = futureChainSymbolSelector
        self.universe_settings = universeSettings

    def get_next_refresh_time_utc(self):
        '''Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.'''
        return self.next_refresh_time_utc

    def create_universes(self, algorithm: QCAlgorithm) -> list[Universe]:
        '''Creates a new fundamental universe using this class's selection functions
        Args:
            algorithm: The algorithm instance to create universes for
        Returns:
            The universe defined by this model'''
        self.next_refresh_time_utc = algorithm.utc_time + self.refresh_interval

        unique_symbols = set()
        for future_symbol in self.future_chain_symbol_selector(algorithm.utc_time):
            if future_symbol.SecurityType != SecurityType.FUTURE:
                raise ValueError("futureChainSymbolSelector must return future symbols.")

            # prevent creating duplicate future chains -- one per symbol
            if future_symbol not in unique_symbols:
                unique_symbols.add(future_symbol)
                selection = self.filter
                if hasattr(self, "Filter") and callable(self.Filter):
                    selection = self.Filter
                for universe in Extensions.create_future_chain(algorithm, future_symbol, selection, self.universe_settings):
                    yield universe

    def filter(self, filter):
        '''Defines the future chain universe filter'''
        # NOP
        return filter
