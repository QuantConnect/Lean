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
### Regression algorithm asserting that when the mirror contract of an option indicator leaves the option universe,
### only the mirror's consolidator is removed: the indicators of the contract that stays keep their consolidators
### and keep updating. See settings.automatic_indicator_deregistration
### </summary>
class AutomaticIndicatorDeregistrationOnOptionUniverseSelectionRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 28)
        self.set_cash(100000)

        self._put_removal_date = datetime(2015, 12, 25)
        self._call = None
        self._put = None
        self._mirrored_delta = None
        self._delta = None
        self._put_removed = False
        self._delta_samples_at_put_removal = 0

        self.add_equity("GOOG")
        option = self.add_option("GOOG")
        option.set_filter(self._filter)

    def _filter(self, universe):
        universe = universe.strikes([750]).expiration([datetime(2016, 6, 17)])
        # the put leaves the universe on the removal date, the call stays and keeps getting data on the 28th
        return universe.calls_only() if universe.local_time.date() >= self._put_removal_date.date() else universe

    def on_securities_changed(self, changes):
        for security in changes.added_securities:
            if security.symbol.security_type != SecurityType.OPTION:
                continue
            if security.symbol.id.option_right == OptionRight.CALL:
                self._call = security.symbol
            else:
                self._put = security.symbol

        if self._delta is None and self._call is not None and self._put is not None:
            self._mirrored_delta = self.d(self._call, self._put)
            self._delta = self.d(self._call)

        for security in changes.removed_securities:
            if security.symbol == self._call:
                raise Exception("The call should stay in the universe")
            if security.symbol == self._put:
                self._on_put_removed()

    def _on_put_removed(self):
        self._put_removed = True
        self._delta_samples_at_put_removal = self._delta.samples

        if not self._mirrored_delta.is_ready:
            raise Exception("The mirrored delta should have been updated while the put was in the universe")
        if self._consolidator_count(self._put) != 0:
            raise Exception("The put consolidators should have been removed with the put")
        if self._consolidator_count(self._call) != 2 or self._consolidator_count(self._call.underlying) != 2:
            raise Exception("The call and underlying consolidators of both deltas should be kept")
        if self._mirrored_delta.consolidators.count != 2 or self._delta.consolidators.count != 2:
            raise Exception("The deltas should only lose the put consolidator")

    def _consolidator_count(self, symbol):
        return sum(config.consolidators.count for config in self.subscription_manager.subscriptions if config.symbol == symbol)

    def on_end_of_algorithm(self):
        if not self._put_removed:
            raise Exception("The put should have been removed from the universe")
        if self._delta.samples <= self._delta_samples_at_put_removal or self._delta.current.end_time.date() <= self._put_removal_date.date():
            raise Exception("The delta of the call should keep updating after the put is removed")
