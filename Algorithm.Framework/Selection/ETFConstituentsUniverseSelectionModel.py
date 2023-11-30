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

class ETFConstituentsUniverseSelectionModel(UniverseSelectionModel):
    '''Universe selection model that selects the constituents of an ETF.'''

    def __init__(self,
                 etfSymbol, 
                 universeSettings = None, 
                 universeFilterFunc = None):
        '''Initializes a new instance of the ETFConstituentsUniverseSelectionModel class
        Args:
            etfSymbol: Symbol of the ETF to get constituents for
            universeSettings: Universe settings
            universeFilterFunc: Function to filter universe results'''
        if type(etfSymbol) is str:
            symbol = SymbolCache.TryGetSymbol(etfSymbol, None)
            if symbol[0] and symbol[1].SecurityType == SecurityType.Equity:
                self.etf_symbol = symbol[1]
            else:
                self.etf_symbol = Symbol.Create(etfSymbol, SecurityType.Equity, Market.USA)
        else:
            self.etf_symbol = etfSymbol
        self.universe_settings = universeSettings
        self.universe_filter_function = universeFilterFunc

        self.universe = None

    def CreateUniverses(self, algorithm: QCAlgorithm) -> List[Universe]:
        '''Creates a new ETF constituents universe using this class's selection function
        Args:
            algorithm: The algorithm instance to create universes for
        Returns:
            The universe defined by this model'''
        if self.universe is None:
            self.universe = algorithm.Universe.ETF(self.etf_symbol, self.universe_settings, self.universe_filter_function)           
        return [self.universe]
