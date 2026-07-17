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
### Regression algorithm asserting that removing an option universe and re-adding one for the
### same underlying within the same time step does not throw, and that the re-added universe
### keeps providing option chain data.
### </summary>
class OptionUniverseRemovedAndReAddedRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2014, 6, 6)
        self.set_end_date(2014, 6, 9)
        self.set_cash(100000)

        self._canonical = self._add_aapl_option()
        self._readded = False
        self._chains_before = 0
        self._chains_after = 0

    def _add_aapl_option(self) -> Symbol:
        option = self.add_option("AAPL", Resolution.MINUTE)
        option.set_filter(-2, 2, 0, 180)
        return option.symbol

    def on_data(self, data):
        chain = data.option_chains.get(self._canonical)
        has_chain = chain is not None and len(chain) > 0

        if not self._readded:
            if has_chain:
                self._chains_before += 1

            if self.time.hour >= 10:
                # Remove the option universe and re-add one for the same underlying in the same time step
                self.remove_security(self._canonical)
                self._canonical = self._add_aapl_option()
                self._readded = True
        elif has_chain:
            self._chains_after += 1

    def on_end_of_algorithm(self):
        if not self._readded:
            raise RegressionTestException("The option universe was never removed and re-added")
        if self._chains_before == 0:
            raise RegressionTestException("Expected option chain data before the universe was removed")
        if self._chains_after == 0:
            raise RegressionTestException("Expected option chain data after the universe was re-added")
