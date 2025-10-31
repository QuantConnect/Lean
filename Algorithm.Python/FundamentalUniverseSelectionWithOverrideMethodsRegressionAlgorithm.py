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

### <summary>
### Regression algorithm testing that all virtual methods in FundamentalUniverseSelectionModel 
### can be properly overridden and called from both C# and Python implementations.
### </summary>
class FundamentalUniverseSelectionWithOverrideMethodsRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2019, 1, 1)
        self.set_end_date(2019, 1, 10)
        self.model = AllMethodsUniverseSelectionModel()
        self.add_universe_selection(self.model)
    
    def on_end_of_algorithm(self):
        # select method will be call multiple times automatically
        model = self.model
        # The other methods must be invoked manually to ensure their overridden implementations are executed
        model.select_coarse(self, [CoarseFundamental()])
        model.select_fine(self, [FineFundamental()])
        model.create_coarse_fundamental_universe(self)
        if (model.select_call_count == 0):
            raise RegressionTestException("Expected select to be called at least once")
        if (model.select_coarse_call_count == 0):
            raise RegressionTestException("Expected select_coarse to be called at least once")
        if (model.select_fine_call_count == 0):
            raise RegressionTestException("Expected select_fine to be called at least once")
        if (model.create_coarse_call_count == 0):
            raise RegressionTestException("Expected create_coarse_fundamental_universe to be called at least once")

class AllMethodsUniverseSelectionModel(FundamentalUniverseSelectionModel):

    def __init__(self):
        super().__init__()
        self.select_call_count = 0
        self.select_coarse_call_count = 0
        self.select_fine_call_count = 0
        self.create_coarse_call_count = 0

    def select(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]) -> list[Symbol]:
        self.select_call_count += 1
        return []
    
    def select_coarse(self, algorithm, coarse):
        self.select_coarse_call_count += 1
        self.select_coarse_called = True
        
        filtered = [c for c in coarse if c.price > 10]
        return [c.symbol for c in filtered[:2]]
    
    def select_fine(self, algorithm, fine):
        self.select_fine_call_count += 1
        self.select_fine_called = True
        
        return [f.symbol for f in fine[:2]]
    
    def create_coarse_fundamental_universe(self, algorithm):
        self.create_coarse_call_count += 1
        self.create_coarse_called = True
        
        return CoarseFundamentalUniverse(
            algorithm.universe_settings, 
            self.custom_coarse_selector
        )
    
    def custom_coarse_selector(self, coarse):
        filtered = [c for c in coarse if c.has_fundamental_data]
        return [c.symbol for c in filtered[:5]]