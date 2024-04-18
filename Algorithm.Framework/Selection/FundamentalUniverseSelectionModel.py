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

class FundamentalUniverseSelectionModel:
    '''Provides a base class for defining equity coarse/fine fundamental selection models'''
    
    def __init__(self,
                 filterFineData = None,
                 universeSettings = None):
        '''Initializes a new instance of the FundamentalUniverseSelectionModel class
        Args:
            filterFineData: [Obsolete] Fine and Coarse selection are merged
            universeSettings: The settings used when adding symbols to the algorithm, specify null to use algorithm.UniverseSettings'''
        self.filter_fine_data = filterFineData
        if self.filter_fine_data == None:
            self.fundamental_data = True
        else:
            self.fundamental_data = False
        self.market = Market.USA
        self.universe_settings = universeSettings


    def create_universes(self, algorithm: QCAlgorithm) -> list[Universe]:
        '''Creates a new fundamental universe using this class's selection functions
        Args:
            algorithm: The algorithm instance to create universes for
        Returns:
            The universe defined by this model'''
        if self.fundamental_data:
            universe_settings = algorithm.universe_settings if self.universe_settings is None else self.universe_settings
            # handle both 'Select' and 'select' for backwards compatibility
            selection = lambda fundamental: self.select(algorithm, fundamental)
            if hasattr(self, "Select") and callable(self.Select):
                selection = lambda fundamental: self.Select(algorithm, fundamental)
            universe = FundamentalUniverseFactory(self.market, universe_settings, selection)
            return [universe]
        else:
            universe = self.create_coarse_fundamental_universe(algorithm)
            if self.filter_fine_data:
                if universe.universe_settings.asynchronous:
                    raise ValueError("Asynchronous universe setting is not supported for coarse & fine selections, please use the new Fundamental single pass selection")
                selection = lambda fine: self.select_fine(algorithm, fine)
                if hasattr(self, "SelectFine") and callable(self.SelectFine):
                    selection = lambda fine: self.SelectFine(algorithm, fine)
                universe = FineFundamentalFilteredUniverse(universe, selection)
            return [universe]


    def create_coarse_fundamental_universe(self, algorithm: QCAlgorithm) -> Universe:
        '''Creates the coarse fundamental universe object.
        This is provided to allow more flexibility when creating coarse universe.
        Args:
            algorithm: The algorithm instance
        Returns:
            The coarse fundamental universe'''
        universe_settings = algorithm.universe_settings if self.universe_settings is None else self.universe_settings
        return CoarseFundamentalUniverse(universe_settings, lambda coarse: self.filtered_select_coarse(algorithm, coarse))


    def filtered_select_coarse(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]) -> list[Symbol]:
        '''Defines the coarse fundamental selection function.
        If we're using fine fundamental selection than exclude symbols without fine data
        Args:
            algorithm: The algorithm instance
            coarse: The coarse fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        if self.filter_fine_data:
            fundamental = filter(lambda c: c.has_fundamental_data, fundamental)
        if hasattr(self, "SelectCoarse") and callable(self.SelectCoarse):
            # handle both 'select_coarse' and 'SelectCoarse' for backwards compatibility
            return self.SelectCoarse(algorithm, fundamental)
        return self.select_coarse(algorithm, fundamental)


    def select(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]) -> list[Symbol]:
        '''Defines the fundamental selection function.
        Args:
            algorithm: The algorithm instance
            fundamental: The fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        raise NotImplementedError("Please overrride the 'select' fundamental function")


    def select_coarse(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]) -> list[Symbol]:
        '''Defines the coarse fundamental selection function.
        Args:
            algorithm: The algorithm instance
            coarse: The coarse fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        raise NotImplementedError("Please overrride the 'select' fundamental function")


    def select_fine(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]) -> list[Symbol]:
        '''Defines the fine fundamental selection function.
        Args:
            algorithm: The algorithm instance
            fine: The fine fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        return [f.symbol for f in fundamental]
