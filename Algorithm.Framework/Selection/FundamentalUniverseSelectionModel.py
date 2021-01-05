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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect.Data.UniverseSelection import *

class FundamentalUniverseSelectionModel:
    '''Provides a base class for defining equity coarse/fine fundamental selection models'''
    
    def __init__(self,
                 filterFineData,
                 universeSettings = None, 
                 securityInitializer = None):
        '''Initializes a new instance of the FundamentalUniverseSelectionModel class
        Args:
            filterFineData: True to also filter using fine fundamental data, false to only filter on coarse data
            universeSettings: The settings used when adding symbols to the algorithm, specify null to use algorthm.UniverseSettings
            securityInitializer: Optional security initializer invoked when creating new securities, specify null to use algorithm.SecurityInitializer'''
        self.filterFineData = filterFineData
        self.universeSettings = universeSettings
        self.securityInitializer = securityInitializer


    def CreateUniverses(self, algorithm):
        '''Creates a new fundamental universe using this class's selection functions
        Args:
            algorithm: The algorithm instance to create universes for
        Returns:
            The universe defined by this model'''
        universe = self.CreateCoarseFundamentalUniverse(algorithm)
        if self.filterFineData:
            universe = FineFundamentalFilteredUniverse(universe, lambda fine: self.SelectFine(algorithm, fine))
        return [universe]


    def CreateCoarseFundamentalUniverse(self, algorithm):
        '''Creates the coarse fundamental universe object.
        This is provided to allow more flexibility when creating coarse universe, such as using algorithm.Universe.DollarVolume.Top(5)
        Args:
            algorithm: The algorithm instance
        Returns:
            The coarse fundamental universe'''
        universeSettings = algorithm.UniverseSettings if self.universeSettings is None else self.universeSettings
        securityInitializer = algorithm.SecurityInitializer if self.securityInitializer is None else self.securityInitializer
        return CoarseFundamentalUniverse(universeSettings, securityInitializer, lambda coarse: self.FilteredSelectCoarse(algorithm, coarse))


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


    def SelectCoarse(self, algorithm, coarse):
        '''Defines the coarse fundamental selection function.
        Args:
            algorithm: The algorithm instance
            coarse: The coarse fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        raise NotImplementedError("SelectCoarse must be implemented")


    def SelectFine(self, algorithm, fine):
        '''Defines the fine fundamental selection function.
        Args:
            algorithm: The algorithm instance
            fine: The fine fundamental data used to perform filtering
        Returns:
            An enumerable of symbols passing the filter'''
        return [f.Symbol for f in fine]