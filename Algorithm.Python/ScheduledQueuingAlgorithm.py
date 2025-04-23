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
from queue import Queue

class ScheduledQueuingAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2020, 9, 1)
        self.set_end_date(2020, 9, 2)
        self.set_cash(100000)
        
        self.__number_of_symbols = 2000
        self.__number_of_symbols_fine = 1000
        self.set_universe_selection(FineFundamentalUniverseSelectionModel(self.coarse_selection_function, self.fine_selection_function, None))
        
        self.set_portfolio_construction(EqualWeightingPortfolioConstructionModel())
        
        self.set_execution(ImmediateExecutionModel())
        
        self._queue = Queue()
        self._dequeue_size = 100
        
        self.add_equity("SPY", Resolution.MINUTE)
        self.schedule.on(self.date_rules.every_day("SPY"), self.time_rules.at(0, 0), self.fill_queue)
        self.schedule.on(self.date_rules.every_day("SPY"), self.time_rules.every(timedelta(minutes=60)), self.take_from_queue)

    def coarse_selection_function(self, coarse: list[CoarseFundamental]) -> list[Symbol]:
        has_fundamentals = [security for security in coarse if security.has_fundamental_data]
        sorted_by_dollar_volume = sorted(has_fundamentals, key=lambda x: x.dollar_volume, reverse=True)
        return [ x.symbol for x in sorted_by_dollar_volume[:self.__number_of_symbols] ]
    
    def fine_selection_function(self, fine: list[FineFundamental]) -> list[Symbol]:
        sorted_by_pe_ratio = sorted(fine, key=lambda x: x.valuation_ratios.pe_ratio, reverse=True)
        return [ x.symbol for x in sorted_by_pe_ratio[:self.__number_of_symbols_fine] ]
        
    def fill_queue(self) -> None:
        securities = [security for security in self.active_securities.values if security.fundamentals]
        
        # Fill queue with symbols sorted by PE ratio (decreasing order)
        self._queue.queue.clear()
        sorted_by_pe_ratio = sorted(securities, key=lambda x: x.fundamentals.valuation_ratios.pe_ratio, reverse=True)
        for security in sorted_by_pe_ratio:
            self._queue.put(security.symbol)
        
    def take_from_queue(self) -> None:
        symbols = [self._queue.get() for _ in range(min(self._dequeue_size, self._queue.qsize()))]
        self.history(symbols, 10, Resolution.DAILY)
        
        self.log(f"Symbols at {self.time}: {[str(symbol) for symbol in symbols]}")
