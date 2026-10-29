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
### Regression algorithm asserting that the indicators and consolidators created through the helper methods are
### deregistered when their security is deselected by the universe or removed manually, that the ones created
### after the security is selected or added again receive data while the deregistered ones don't, and that the
### ones of a security that stays in the universe keep updating. See settings.automatic_indicator_deregistration
### </summary>
class AutomaticIndicatorDeregistrationOnUniverseSelectionRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2014, 3, 24)
        self.set_end_date(2014, 4, 7)
        self.set_cash(100000)

        self._removal_date = datetime(2014, 3, 27)
        self._re_addition_date = datetime(2014, 4, 1)
        self._aig_data = None
        self._ibm_data = None
        self._spy_removed = False
        self._spy_re_added = False
        self._ibm_bars = 0

        self.universe_settings.resolution = Resolution.DAILY
        self._aig = Symbol.create("AIG", SecurityType.EQUITY, Market.USA)
        self._ibm = Symbol.create("IBM", SecurityType.EQUITY, Market.USA)
        self.add_universe("MyUniverse", Resolution.DAILY, self._select)

        self._spy = self.add_equity("SPY", Resolution.DAILY).symbol
        self._spy_data = SymbolData(self, self._spy)

    def _select(self, date_time):
        # AIG leaves the universe and comes back on the same dates SPY is removed and re-added manually, IBM stays
        select_aig = date_time < self._removal_date or date_time >= self._re_addition_date
        return ["IBM", "AIG"] if select_aig else ["IBM"]

    def on_data(self, slice):
        if slice.bars.contains_key(self._ibm):
            self._ibm_bars += 1

        if not self._spy_removed and self.time.date() >= self._removal_date.date():
            self._spy_removed = True
            self.remove_security(self._spy)
        elif self._spy_removed and not self._spy_re_added and self.time.date() >= self._re_addition_date.date():
            self._spy_re_added = True
            self.add_equity("SPY", Resolution.DAILY)

    def on_securities_changed(self, changes):
        for security in changes.added_securities:
            if security.symbol == self._aig:
                if self._aig_data is None:
                    self._aig_data = SymbolData(self, self._aig)
                else:
                    self._aig_data.on_re_added()
            elif security.symbol == self._spy and self._spy_re_added:
                self._spy_data.on_re_added()
            elif security.symbol == self._ibm:
                self._ibm_data = SymbolData(self, self._ibm)

        for security in changes.removed_securities:
            if security.symbol == self._aig:
                self._aig_data.on_removed()
            elif security.symbol == self._spy:
                self._spy_data.on_removed()
            elif security.symbol == self._ibm:
                raise Exception("IBM should stay in the universe")

    def on_end_of_algorithm(self):
        if not self._spy_removed or not self._spy_re_added:
            raise Exception("SPY should have been removed and re-added")

        self._spy_data.assert_deregistered_on_removal()
        self._aig_data.assert_deregistered_on_removal()
        self._ibm_data.assert_kept_updating(self._ibm_bars)

class SymbolData:

    def __init__(self, algorithm, symbol):
        self._algorithm = algorithm
        self._symbol = symbol
        self._new_sma = None
        self._consolidated_count = 0
        self._new_consolidated_count = 0
        self._sma_samples_at_removal = 0
        self._consolidated_count_at_removal = 0
        self._removals = 0
        self._re_additions = 0

        self._sma = algorithm.sma(symbol, 2, Resolution.DAILY)
        algorithm.consolidate(symbol, Resolution.DAILY, self._on_consolidated)

    def _on_consolidated(self, bar):
        self._consolidated_count += 1

    def _on_new_consolidated(self, bar):
        self._new_consolidated_count += 1

    def on_removed(self):
        self._removals += 1
        self._sma_samples_at_removal = self._sma.samples
        self._consolidated_count_at_removal = self._consolidated_count

        if self._sma.consolidators.count != 0:
            raise Exception(f"{self._symbol}: the indicator should have been deregistered when the security was removed")

    def on_re_added(self):
        self._re_additions += 1
        self._new_sma = self._algorithm.sma(self._symbol, 2, Resolution.DAILY)
        self._algorithm.consolidate(self._symbol, Resolution.DAILY, self._on_new_consolidated)

    def assert_deregistered_on_removal(self):
        if self._removals == 0 or self._re_additions == 0:
            raise Exception(f"{self._symbol}: expected the security to be removed and re-added. Removals: {self._removals}, re-additions: {self._re_additions}")
        if self._sma_samples_at_removal == 0 or self._consolidated_count_at_removal == 0:
            raise Exception(f"{self._symbol}: the indicator and consolidator should have been updated before the security was removed")
        if self._sma.samples != self._sma_samples_at_removal or self._consolidated_count != self._consolidated_count_at_removal:
            raise Exception(f"{self._symbol}: the deregistered indicator and consolidator should not get updates after the security is removed")
        if self._new_sma.samples == 0 or self._new_consolidated_count == 0:
            raise Exception(f"{self._symbol}: the indicator and consolidator created after the security is re-added should get updates")

    def assert_kept_updating(self, expected_updates):
        if self._removals != 0 or self._sma.consolidators.count != 1:
            raise Exception(f"{self._symbol}: the indicator of a security that stays should keep its consolidator")
        if expected_updates == 0 or self._sma.samples != expected_updates or self._consolidated_count != expected_updates:
            raise Exception(f"{self._symbol}: the indicator and consolidator should get every bar. Bars: {expected_updates}, indicator: {self._sma.samples}, consolidator: {self._consolidated_count}")
