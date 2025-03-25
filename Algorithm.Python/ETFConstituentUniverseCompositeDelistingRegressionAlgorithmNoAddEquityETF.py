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
### Tests the delisting of the composite Symbol (ETF symbol) and the removal of
### the universe and the symbol from the algorithm, without adding a subscription via AddEquity
### </summary>
class ETFConstituentUniverseCompositeDelistingRegressionAlgorithmNoAddEquityETF(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2020, 12, 1)
        self.set_end_date(2021, 1, 31)
        self.set_cash(100000)

        self.universe_symbol_count = 0
        self.universe_selection_done = False
        self.universe_added = False
        self.universe_removed = False

        self.universe_settings.resolution = Resolution.HOUR
        self.delisting_date = date(2021, 1, 21)

        self.aapl = self.add_equity("AAPL", Resolution.HOUR).symbol
        self.gdvd = Symbol.create("GDVD", SecurityType.EQUITY, Market.USA)

        self.add_universe(self.universe.etf(self.gdvd, self.universe_settings, self.filter_etfs))

    def filter_etfs(self, constituents):
        self.universe_selection_done = True

        if self.utc_time.date() > self.delisting_date:
            raise AssertionError(f"Performing constituent universe selection on {self.utc_time.strftime('%Y-%m-%d %H:%M:%S.%f')} after composite ETF has been delisted")

        constituent_symbols = [i.symbol for i in constituents]
        self.universe_symbol_count = len(set(constituent_symbols))

        return constituent_symbols

    def on_data(self, data):
        if self.utc_time.date() > self.delisting_date and any([i != self.aapl for i in data.keys()]):
            raise AssertionError("Received unexpected slice in OnData(...) after universe was deselected")

        if not self.portfolio.invested:
            self.set_holdings(self.aapl, 0.5)

    def on_securities_changed(self, changes):
        if len(changes.added_securities) != 0 and self.utc_time.date() > self.delisting_date:
            raise AssertionError("New securities added after ETF constituents were delisted")

        if self.universe_selection_done:
            self.universe_added = self.universe_added or len(changes.added_securities) == self.universe_symbol_count

        # TODO: shouldn't be sending AAPL as a removed security since it was added by another universe
        self.universe_removed = self.universe_removed or (
            len(changes.removed_securities) == self.universe_symbol_count and
            self.utc_time.date() >= self.delisting_date and
            self.utc_time.date() < self.end_date.date())

    def on_end_of_algorithm(self):
        if not self.universe_added:
            raise AssertionError("ETF constituent universe was never added to the algorithm")
        if not self.universe_removed:
            raise AssertionError("ETF constituent universe was not removed from the algorithm after delisting")
        if len(self.active_securities) > 2:
            raise AssertionError(f"Expected less than 2 securities after algorithm ended, found {len(self.securities)}")
