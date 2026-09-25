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
### Regression algorithm asserting that a future added with a data mapping mode its market has no data for (open interest on EUREX)
### falls back to the market default, and that the related warnings are sent.
### </summary>
class FutureDataMappingModeFallbackRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2024, 6, 3)
        self.set_end_date(2024, 6, 4)
        self.set_account_currency(Currencies.EUR)

        self._future = self.add_future(Futures.Indices.EURO_STOXX_50, Resolution.MINUTE, data_mapping_mode=DataMappingMode.OPEN_INTEREST)
        self._assert_warning("Warning: OpenInterest data mapping mode is not available for EUREX futures, using LastTradingDay instead.")

        # Open interest resolves to the mode already in use, so this is not a conflicting re-add
        self.add_future(Futures.Indices.EURO_STOXX_50, Resolution.MINUTE, data_mapping_mode=DataMappingMode.LAST_TRADING_DAY)
        if any("already added" in message for message in self.debug_messages):
            raise AssertionError("Unexpected re-add warning for a future added again with the same data mapping mode")

        self._checked_after_initialize = False

    def on_data(self, slice):
        if self._checked_after_initialize or self._future.mapped is None:
            return
        self._checked_after_initialize = True

        # Last trading day maps to the June contract, first day of the month would map to September
        if self._future.mapped.id.date.month != 6:
            raise AssertionError(f"Unexpected mapped contract {self._future.mapped}, expected the June contract")

        if self.history(self._future.symbol, 10, Resolution.MINUTE).empty:
            raise AssertionError("Expected history for the continuous future using the fallback data mapping mode")

        open_interest_history = self.history(self._future.symbol, 10, Resolution.MINUTE, data_mapping_mode=DataMappingMode.OPEN_INTEREST)
        if not open_interest_history.empty:
            raise AssertionError("Expected no history for the explicitly requested open interest data mapping mode")
        self._assert_warning("Warning: OpenInterest data mapping mode is not available for EUREX futures, no contract will be mapped. Use LastTradingDay instead.")

        self.add_future(Futures.Indices.EURO_STOXX_50, Resolution.MINUTE, data_mapping_mode=DataMappingMode.FIRST_DAY_MONTH)
        self._assert_warning("Warning: /FESX already added, ignoring data mapping mode FirstDayMonth. Remove it first to change its settings.")

    def on_end_of_algorithm(self):
        if not self._checked_after_initialize:
            raise AssertionError("The continuous future was never mapped")
        if self._future.mapped.id.date.month != 6:
            raise AssertionError(f"Unexpected mapped contract {self._future.mapped} after the ignored re-add, expected the June contract")

    def _assert_warning(self, warning):
        if sum(1 for message in self.debug_messages if message.endswith(warning)) != 1:
            raise AssertionError(f"Expected the warning '{warning}' to be sent once")
