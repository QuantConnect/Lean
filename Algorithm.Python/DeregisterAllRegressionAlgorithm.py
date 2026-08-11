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
### Example and regression algorithm asserting the behavior of deregister_all: on security removal, a single call
### disposes all the indicators and consolidators created for it through the algorithm helper methods, so add/remove
### churn doesn't leak consolidators, and re-adding the security with fresh indicators keeps working
### </summary>
class DeregisterAllRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        self._spy = self.add_equity("SPY").symbol
        self._ibm = self.add_equity("IBM").symbol

        # per symbol state created through the helper methods, tracked by the engine
        self._ibm_rsi = self.rsi(self._ibm, 14, resolution=Resolution.MINUTE)
        self._ibm_sma = self.sma(self._ibm, 10, Resolution.MINUTE)
        self._ibm_consolidated_count = 0
        self.consolidate(self._ibm, Resolution.HOUR, self._on_hour_bar)

        self._new_ibm_sma = None
        self._ibm_rsi_samples_at_removal = 0
        self._ibm_consolidated_count_at_removal = 0
        self._removed = False
        self._readded = False

    def _on_hour_bar(self, bar):
        self._ibm_consolidated_count += 1

    def on_data(self, data):
        if not self._removed and self.time.day == 8:
            self._removed = True
            self.remove_security(self._ibm)
        elif self._removed and not self._readded and self.time.day == 10:
            self._readded = True
            # re-adding the security after cleanup works: the helpers create fresh consolidators
            self.add_equity("IBM")
            self._new_ibm_sma = self.sma(self._ibm, 10, Resolution.MINUTE)

        if not self.portfolio.invested:
            self.set_holdings(self._spy, 0.5)

    def on_securities_changed(self, changes):
        for security in changes.removed_securities:
            # single call cleanup of every helper-created indicator and consolidator of the removed security
            self.deregister_all(security.symbol)

            if security.symbol == self._ibm:
                self._ibm_rsi_samples_at_removal = self._ibm_rsi.samples
                self._ibm_consolidated_count_at_removal = self._ibm_consolidated_count

                if self._ibm_rsi.consolidators.count != 0 or self._ibm_sma.consolidators.count != 0:
                    raise Exception("The removed security indicators should have no consolidators after deregister_all")

    def on_end_of_algorithm(self):
        if not self._removed or self._ibm_rsi_samples_at_removal == 0:
            raise Exception("The security should have been removed and its indicators deregistered")
        if self._ibm_rsi.samples != self._ibm_rsi_samples_at_removal or self._ibm_consolidated_count != self._ibm_consolidated_count_at_removal:
            raise Exception("Deregistered indicators and consolidators should have stopped getting updates")
        if self._new_ibm_sma is None or not self._new_ibm_sma.is_ready:
            raise Exception("Indicators created after re-adding the security should be getting updates")
