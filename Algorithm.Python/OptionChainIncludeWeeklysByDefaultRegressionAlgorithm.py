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
### Verifies that weekly option contracts are included when no standard contracts are available.
### </summary>
class OptionChainIncludeWeeklysByDefaultRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)

        self.option = self.add_option("GOOG")
        self.option_symbol = self.option.Symbol

        self.option.set_filter(lambda u: u.strikes(-8, 8).expiration(0, 0))

        self.weekly_count = 0
        self.total_count = 0

    def on_data(self, data):
        chain = data.option_chains.get(self.option_symbol)
        if chain:
            self.total_count += len(chain.contracts)
            for contract in chain.contracts.values():
                if not OptionSymbol.is_standard(contract.symbol):
                    self.weekly_count += 1
    
    def on_end_of_algorithm(self):
        if self.weekly_count == 0:
            raise RegressionTestException("No weekly contracts found")
        
        if self.total_count != self.weekly_count:
            raise RegressionTestException("When no standard option expirations are available, the option chain must fall back to weekly contracts only")