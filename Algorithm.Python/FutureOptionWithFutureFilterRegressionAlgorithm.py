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

from FutureOptionContinuousFutureRegressionAlgorithm import FutureOptionContinuousFutureRegressionAlgorithm

### <summary>
### Regression algorithm that validates that when using a Future with filter
### the option chains are correctly populated and are unique
### </summary>
class FutureOptionWithFutureFilterRegressionAlgorithm(FutureOptionContinuousFutureRegressionAlgorithm):
    def set_filter(self):
        """Set future filter for specific contracts"""
        self.future.set_filter(0, 368)
    
    def validate_option_chains(self, slice: Slice):
        future_contracts_with_option_chains = 0
        for future_chain in slice.future_chains.values():
            for future_contract in future_chain:
                # Not all future contracts have option chains, so we need to check if the contract is in the option chain
                if future_contract.symbol in slice.option_chains:
                    chain = slice.option_chains[future_contract.symbol]
                    if len(chain) == 0:
                        raise RegressionTestException("Expected at least one option contract for {}".format(chain.symbol))
                    future_contracts_with_option_chains += 1
        
        if future_contracts_with_option_chains < 1:
            raise RegressionTestException("Expected at least two future contracts with option chains, but found {}".format(future_contracts_with_option_chains))