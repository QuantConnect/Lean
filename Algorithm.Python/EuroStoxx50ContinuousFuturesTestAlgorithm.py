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

class EuroStoxx50ContinuousFuturesTestAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2023, 1, 1)
        self.set_end_date(2024, 8, 5)
        self.set_cash(100000)

        self._mappings = []
        self._last_date_log = -1

        self._continuous_contract = self.add_future(Futures.Indices.EURO_STOXX_50,
                                                    Resolution.MINUTE,
                                                    data_normalization_mode=DataNormalizationMode.BACKWARDS_RATIO,
                                                    data_mapping_mode=DataMappingMode.FIRST_DAY_MONTH,
                                                    contract_depth_offset=0)

        self._current_mapped_symbol = self._continuous_contract.symbol

    def on_data(self, data):
        currently_mapped_security = self.securities[self._continuous_contract.mapped]
        if len(data.keys()) != 1:
            raise ValueError(f"We are getting data for more than one symbols! {','.join(data.keys())}")

        for changed_event in data.symbol_changed_events.values():
            if changed_event.symbol == self._continuous_contract.symbol:
                self._mappings.append(changed_event)
                self.log(f"[{self.time}] :: SymbolChanged event: {changed_event}")

                if self._current_mapped_symbol == self._continuous_contract.mapped:
                    raise ValueError(f"Continuous contract current symbol did not change! {self._continuous_contract.mapped}")

        if self._last_date_log != self.time.month and currently_mapped_security.has_data:
            self._last_date_log = self.time.month
            self.log(f"[{self.time}] :: {currently_mapped_security.get_last_data()}")

        self._current_mapped_symbol = self._continuous_contract.mapped

    def on_securities_changed(self, changes):
        self.debug(f"[{self.time}] :: {changes}")

    def on_end_of_algorithm(self):
        mappings_str = '\n'.join([str(mapping) for mapping in self._mappings])
        self.log(f"Mappings:\n{mappings_str}")

        expected_mapping_counts = 6
        if len(self._mappings) != expected_mapping_counts:
            raise ValueError(f"Unexpected symbol changed events: {len(self._mappings)}, was expecting {expected_mapping_counts}")
