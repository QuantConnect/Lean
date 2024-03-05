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
        self.filterFineData = filterFineData
        if self.filterFineData == None:
            self._fundamentalData = True
        else:
            self._fundamentalData = False
        self.universeSettings = universeSettings


    def CreateUniverses(self, algorithm):
        '''Creates a new fundamental universe using this class's selection functions
        Args:
            algorithm: The algorithm instance to create universes for
        Returns:
            The universe defined by this model'''
        if self._fundamentalData:
            universeSettings = algorithm.UniverseSettings if self.universeSettings is None else self.universeSettings
            universe = FundamentalUniverseConfig(universeSettings, lambda fundamental: self.Select(algorithm, fundamental))
            return [universe]
        else:
            universe = self.CreateCoarseFundamentalUniverse(algorithm)
            if self.filterFineData:
                if universe.UniverseSettings.Asynchronous:
                    raise ValueError("Asynchronous universe setting is not supported for coarse & fine selections, please use the new Fundamental single pass selection")
                universe = FineFundamentalFilteredUniverse(universe, lambda fine: self.SelectFine(algorithm, fine))
            return [universe]


    def CreateCoarseFundamentalUniverse(self, algorithm):
        '''Creates the coarse fundamental universe object.
        This is provided to allow more flexibility when creating coarse universe.
        Args:
            algorithm: The algorithm instance
        Returns:
            The coarse fundamental universe'''
        universeSettings = algorithm.UniverseSettings if self.universeSettings is None else self.universeSettings
        return CoarseFundamentalUniverse(universeSettings, lambda coarse: self.FilteredSelectCoarse(algorithm, coarse))


    def FilteredSelectCoarse(self, algorithm, coarse):
        '''Defines the coarse fundamental selection function.
        If we're using fine fundamental selection than exclude symbols without fine data
        Args:
            algorithm: The algorithm instance
            coarse: The coarse fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        if self.filterFineData:
            coarse = filter(lambda c: c.HasFundamentalData, coarse)
        return self.SelectCoarse(algorithm, coarse)


    def Select(self, algorithm, fundamental):
        '''Defines the fundamental selection function.
        Args:
            algorithm: The algorithm instance
            fundamental: The fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        raise NotImplementedError("Please overrride the 'Select' fundamental function")


    def SelectCoarse(self, algorithm, coarse):
        '''Defines the coarse fundamental selection function.
        Args:
            algorithm: The algorithm instance
            coarse: The coarse fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        raise NotImplementedError("Please overrride the 'Select' fundamental function")


    def SelectFine(self, algorithm, fine):
        '''Defines the fine fundamental selection function.
        Args:
            algorithm: The algorithm instance
            fine: The fine fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        return [f.Symbol for f in fine]
