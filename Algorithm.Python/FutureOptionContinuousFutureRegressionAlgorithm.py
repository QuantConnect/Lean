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
### Regression algorithm that validates that when using a continuous future (without a filter)
### the option chains are correctly populated using the mapped symbol.
### </summary>
class FutureOptionContinuousFutureRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2020, 1, 4)
        self.set_end_date(2020, 1, 8)
        
        self.future = self.add_future(Futures.Indices.SP_500_E_MINI, Resolution.MINUTE, Market.CME)
        self.set_filter()
        
        self.add_future_option(self.future.symbol, lambda universe: universe.strikes(-1, 1))
        
        self._has_any_option_chain_for_mapped_symbol = False

    def set_filter(self):
        """Set future filter - override in derived classes for filtered version"""
        pass

    def on_data(self, slice: Slice):
        if len(slice.option_chains) == 0:
            return

        self.validate_option_chains(slice)
        
        # OptionChain for mapped symbol
        chain = slice.option_chains[self.future.mapped]
        if chain is None or not any(chain):
            raise RegressionTestException("No option chain found for mapped symbol during algorithm execution")

        # Mark that we successfully received a non-empty OptionChain for mapped symbol
        self._has_any_option_chain_for_mapped_symbol = True
    
    def validate_option_chains(self, slice: Slice):
        if len(slice.option_chains) != 1:
            raise RegressionTestException("Expected only one option chain for the mapped symbol")

    def on_end_of_algorithm(self):
        if not self._has_any_option_chain_for_mapped_symbol:
            raise RegressionTestException("No non-empty option chain found for mapped symbol during algorithm execution")