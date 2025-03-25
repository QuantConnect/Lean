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
from QuantConnect.Data.Custom.IconicTypes import *

class CustomDataLinkedIconicTypeAddDataCoarseSelectionRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2014, 3, 24)
        self.set_end_date(2014, 4, 7)
        self.set_cash(100000)

        self.universe_settings.resolution = Resolution.DAILY

        self.add_universe_selection(CoarseFundamentalUniverseSelectionModel(self.coarse_selector))

    def coarse_selector(self, coarse):
        symbols = [
            Symbol.create("AAPL", SecurityType.EQUITY, Market.USA),
            Symbol.create("BAC", SecurityType.EQUITY, Market.USA),
            Symbol.create("FB", SecurityType.EQUITY, Market.USA),
            Symbol.create("GOOGL", SecurityType.EQUITY, Market.USA),
            Symbol.create("GOOG", SecurityType.EQUITY, Market.USA),
            Symbol.create("IBM", SecurityType.EQUITY, Market.USA),
        ]

        self.custom_symbols = []

        for symbol in symbols:
            self.custom_symbols.append(self.add_data(LinkedData, symbol, Resolution.DAILY).symbol)

        return symbols

    def on_data(self, data):
        if not self.portfolio.invested and len(self.transactions.get_open_orders()) == 0:
            aapl = Symbol.create("AAPL", SecurityType.EQUITY, Market.USA)
            self.set_holdings(aapl, 0.5)

        for custom_symbol in self.custom_symbols:
            if not self.active_securities.contains_key(custom_symbol.underlying):
                raise AssertionError(f"Custom data undelrying ({custom_symbol.underlying}) Symbol was not found in active securities")
