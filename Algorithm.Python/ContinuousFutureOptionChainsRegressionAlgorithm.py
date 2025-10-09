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
class ContinuousFutureOptionChainsRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2020, 1, 4)
        self.set_end_date(2020, 1, 8)
        
        self._es = self.add_future(Futures.Indices.SP_500_E_MINI, Resolution.MINUTE, Market.CME)
        self.add_future_option(self._es.symbol, lambda universe: universe.strikes(-1, 1))
        
        self._found_non_empty_chain = False

    def on_data(self, slice: Slice):
        if not slice.has_data or self.portfolio.invested:
            return
        
        # Retrieve the OptionChain for the mapped symbol of the continuous future
        chain = slice.option_chains.get(self._es.mapped)
        if chain is None or not any(chain):
            return
        
        # Mark that we successfully received a non-empty OptionChain
        self._found_non_empty_chain = True
        
        # Buy the first call option we find
        call = next((contract for contract in chain.contracts.values() if contract.right == OptionRight.CALL), None)
        if call is not None and not self.portfolio.invested:
            self.market_order(call.symbol, 1)

    def on_end_of_algorithm(self):
        # Ensure that at least one non-empty OptionChain was found
        if not self._found_non_empty_chain:
            raise RegressionTestException("No option chain found")