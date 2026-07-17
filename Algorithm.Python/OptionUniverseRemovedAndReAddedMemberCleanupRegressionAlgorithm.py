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
### Regression algorithm asserting that when an option universe is removed and one for the same
### underlying is re-added in the same time step, the previously selected contracts that are not
### re-selected by the new universe are properly cleaned up: their subscriptions are removed and
### removed security changes are emitted for them.
### </summary>
class OptionUniverseRemovedAndReAddedMemberCleanupRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2014, 6, 6)
        self.set_end_date(2014, 6, 9)
        self.set_cash(100000)

        # Wide filter: several strikes and expirations will be selected
        self._canonical = self._add_aapl_option(lambda u: u.strikes(-2, 2).expiration(0, 180))
        self._readded = False
        self._checked = False
        self._old_members = []
        self._removed_symbols = set()

    def _add_aapl_option(self, filter_func) -> Symbol:
        option = self.add_option("AAPL", Resolution.MINUTE)
        option.set_filter(filter_func)
        return option.symbol

    def on_securities_changed(self, changes):
        for security in changes.removed_securities:
            self._removed_symbols.add(security.symbol)

    def on_data(self, data):
        if not self._readded:
            if self.time.hour < 10:
                return
            universe = self.universe_manager[self._canonical]
            if universe.members.count == 0:
                return

            self._old_members = [kvp.key for kvp in universe.members if not kvp.key.is_canonical()]

            # Remove the universe and re-add it with a narrower filter in the same time step:
            # most of the previously selected contracts will not be re-selected by the new universe
            self.remove_security(self._canonical)
            self._canonical = self._add_aapl_option(lambda u: u.strikes(0, 0).expiration(0, 30))
            self._readded = True
        elif not self._checked and (self.time.hour > 10 or self.time.minute >= 30):
            self._checked = True
            self._assert_old_members_cleaned_up()

    def _assert_old_members_cleaned_up(self):
        universe = self.universe_manager[self._canonical]
        current_members = set(kvp.key for kvp in universe.members)
        subscribed = set(config.symbol for config in self.subscription_manager.subscriptions)

        not_reselected = [s for s in self._old_members if s not in current_members]
        if len(not_reselected) == 0:
            raise RegressionTestException("Expected some previously selected contracts to not be re-selected")

        still_subscribed = [s for s in not_reselected if s in subscribed]
        if len(still_subscribed) > 0:
            raise RegressionTestException(
                f"Expected the subscriptions of the {len(not_reselected)} deselected contracts to be removed, "
                f"but {len(still_subscribed)} are still subscribed, e.g. {[str(s) for s in still_subscribed[:3]]}")

        missing_removed_events = [s for s in not_reselected if s not in self._removed_symbols]
        if len(missing_removed_events) > 0:
            raise RegressionTestException(
                f"Expected removed security changes for the {len(not_reselected)} deselected contracts, "
                f"but {len(missing_removed_events)} were not notified, e.g. {[str(s) for s in missing_removed_events[:3]]}")

    def on_end_of_algorithm(self):
        if not self._readded:
            raise RegressionTestException("The option universe was never removed and re-added")
        if not self._checked:
            raise RegressionTestException("The clean up assertions were never performed")
