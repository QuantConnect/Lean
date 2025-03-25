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
### Tests the mapping of the ETF symbol that has a constituent universe attached to it and ensures
### that data is loaded after the mapping event takes place.
### </summary>
class ETFConstituentUniverseFilterFunctionRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2011, 2, 1)
        self.set_end_date(2011, 4, 4)
        self.set_cash(100000)

        self.filter_date_constituent_symbol_count = {}
        self.constituent_data_encountered = {}
        self.constituent_symbols = []
        self.mapping_event_occurred = False

        self.universe_settings.resolution = Resolution.HOUR

        self.aapl = Symbol.create("AAPL", SecurityType.EQUITY, Market.USA)
        self.qqq = self.add_equity("QQQ", Resolution.DAILY).symbol

        self.add_universe(self.universe.etf(self.qqq, self.universe_settings, self.filter_etfs))

    def filter_etfs(self, constituents):
        constituent_symbols = [i.symbol for i in constituents]

        if self.aapl not in constituent_symbols:
            raise AssertionError("AAPL not found in QQQ constituents")

        self.filter_date_constituent_symbol_count[self.utc_time.date()] = len(constituent_symbols)
        for symbol in constituent_symbols:
            self.constituent_symbols.append(symbol)

        self.constituent_symbols = list(set(self.constituent_symbols))
        return constituent_symbols

    def on_data(self, data):
        if len(data.symbol_changed_events) != 0:
            for symbol_changed in data.symbol_changed_events.values():
                if symbol_changed.symbol != self.qqq:
                    raise AssertionError(f"Mapped symbol is not QQQ. Instead, found: {symbol_changed.symbol}")
                if symbol_changed.old_symbol != "QQQQ":
                    raise AssertionError(f"Old QQQ Symbol is not QQQQ. Instead, found: {symbol_changed.old_symbol}")
                if symbol_changed.new_symbol != "QQQ":
                    raise AssertionError(f"New QQQ Symbol is not QQQ. Instead, found: {symbol_changed.new_symbol}")

                self.mapping_event_occurred = True

        if self.qqq in data and len([i for i in data.keys()]) == 1:
            return

        if self.utc_time.date() not in self.constituent_data_encountered:
            self.constituent_data_encountered[self.utc_time.date()] = False
        
        if len([i for i in data.keys() if i in self.constituent_symbols]) != 0:
            self.constituent_data_encountered[self.utc_time.date()] = True

        if not self.portfolio.invested:
            self.set_holdings(self.aapl, 0.5)

    def on_end_of_algorithm(self):
        if len(self.filter_date_constituent_symbol_count) != 2:
            raise AssertionError(f"ETF constituent filtering function was not called 2 times (actual: {len(self.filter_date_constituent_symbol_count)}")

        if not self.mapping_event_occurred:
            raise AssertionError("No mapping/SymbolChangedEvent occurred. Expected for QQQ to be mapped from QQQQ -> QQQ")

        for constituent_date, constituents_count in self.filter_date_constituent_symbol_count.items():
            if constituents_count < 25:
                raise AssertionError(f"Expected 25 or more constituents in filter function on {constituent_date}, found {constituents_count}")

        for constituent_date, constituent_encountered in self.constituent_data_encountered.items():
            if not constituent_encountered:
                raise AssertionError(f"Received data in OnData(...) but it did not contain any constituent data on {constituent_date.strftime('%Y-%m-%d %H:%M:%S.%f')}")
