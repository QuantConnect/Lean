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
        self._any_contract_found = False
        self._unique_future_chains = set()
        self._unique_option_chains = set()
        self._unique_option_contracts = set()

    def set_filter(self):
        """Set future filter - override in derived classes for filtered version"""
        pass

    def on_data(self, slice: Slice):
        if not slice.has_data:
            return

        # FutureChains should be unique
        for future_chain_key in slice.future_chains.keys():
            if future_chain_key in self._unique_future_chains:
                raise RegressionTestException(f"Duplicate FutureChain found: {future_chain_key}")
            self._unique_future_chains.add(future_chain_key)

        # OptionChains should be unique
        for option_chain_key in slice.option_chains.keys():
            if option_chain_key in self._unique_option_chains:
                raise RegressionTestException(f"Duplicate OptionChain found: {option_chain_key}")
            self._unique_option_chains.add(option_chain_key)

        # Option contracts within chains should be unique
        for option_chain in slice.option_chains.values():
            for contract in option_chain.contracts.keys():
                if contract in self._unique_option_contracts:
                    raise RegressionTestException(f"Duplicate OptionContract found: {contract}")
                self._unique_option_contracts.add(contract)

        self._any_contract_found = len(self._unique_option_contracts) > 0

        # OptionChain for mapped symbol
        canonical_symbol = Symbol.create_canonical_option(self.future.mapped)
        if not canonical_symbol in slice.option_chains:
            return
        chain = slice.option_chains[canonical_symbol]
        if chain is None or not any(chain):
            return

        # Mark that we successfully received a non-empty OptionChain for mapped symbol
        self._has_any_option_chain_for_mapped_symbol = True

        # Reset hashsets for next iteration
        self._unique_future_chains.clear()
        self._unique_option_chains.clear()
        self._unique_option_contracts.clear()

    def on_end_of_algorithm(self):
        if not self._has_any_option_chain_for_mapped_symbol:
            raise RegressionTestException("No non-empty option chain found for mapped symbol during algorithm execution")

        if not self._any_contract_found:
            raise RegressionTestException("No option contract found during algorithm execution")