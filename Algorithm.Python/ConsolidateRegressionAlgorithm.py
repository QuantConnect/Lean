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
from CustomDataRegressionAlgorithm import Bitcoin

### <summary>
### Regression algorithm reproducing data type bugs in the Consolidate API. Related to GH 4205.
### </summary>
class ConsolidateRegressionAlgorithm(QCAlgorithm):

    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    def initialize(self):
        self.set_start_date(2013, 10, 8)
        self.set_end_date(2013, 10, 20)

        SP500 = Symbol.create(Futures.Indices.SP_500_E_MINI, SecurityType.FUTURE, Market.CME)
        self._symbol = _symbol = self.future_chain_provider.get_future_contract_list(SP500, self.start_date)[0]
        self.add_future_contract(_symbol)

        self._consolidation_counts = [0] * 6
        self._smas = [SimpleMovingAverage(10) for x in self._consolidation_counts]
        self._last_sma_updates = [datetime.min for x in self._consolidation_counts]
        self._monthly_consolidator_sma = SimpleMovingAverage(10)
        self._monthly_consolidation_count = 0
        self._weekly_consolidator_sma = SimpleMovingAverage(10)
        self._weekly_consolidation_count = 0
        self._last_weekly_sma_update = datetime.min

        self.consolidate(_symbol, Calendar.MONTHLY, lambda bar: self.update_monthly_consolidator(bar, -1)) # shouldn't consolidate

        self.consolidate(_symbol, Calendar.WEEKLY, TickType.TRADE, lambda bar: self.update_weekly_consolidator(bar))

        self.consolidate(_symbol, Resolution.DAILY, lambda bar: self.update_trade_bar(bar, 0))

        self.consolidate(_symbol, Resolution.DAILY, TickType.QUOTE, lambda bar: self.update_quote_bar(bar, 1))

        self.consolidate(_symbol, timedelta(1), lambda bar: self.update_trade_bar(bar, 2))

        self.consolidate(_symbol, timedelta(1), TickType.QUOTE, lambda bar: self.update_quote_bar(bar, 3))

        # sending None tick type
        self.consolidate(_symbol, timedelta(1), None, lambda bar: self.update_trade_bar(bar, 4))

        self.consolidate(_symbol, Resolution.DAILY, None, lambda bar: self.update_trade_bar(bar, 5))

        # custom data
        self._custom_data_consolidator = 0
        custom_symbol = self.add_data(Bitcoin, "BTC", Resolution.MINUTE).symbol
        self.consolidate(custom_symbol, timedelta(1), lambda bar: self.increment_counter(1))

        self._custom_data_consolidator2 = 0
        self.consolidate(custom_symbol, Resolution.DAILY, lambda bar: self.increment_counter(2))

    def increment_counter(self, id):
        if id == 1:
            self._custom_data_consolidator += 1
        if id == 2:
            self._custom_data_consolidator2 += 1

    def update_trade_bar(self, bar, position):
        self._smas[position].update(bar.end_time, bar.volume)
        self._last_sma_updates[position] = bar.end_time
        self._consolidation_counts[position] += 1

    def update_quote_bar(self, bar, position):
        self._smas[position].update(bar.end_time, bar.ask.high)
        self._last_sma_updates[position] = bar.end_time
        self._consolidation_counts[position] += 1

    def update_monthly_consolidator(self, bar):
        self._monthly_consolidator_sma.update(bar.end_time, bar.volume)
        self._monthly_consolidation_count += 1

    def update_weekly_consolidator(self, bar):
        self._weekly_consolidator_sma.update(bar.end_time, bar.volume)
        self._last_weekly_sma_update = bar.end_time
        self._weekly_consolidation_count += 1

    def  OnEndOfAlgorithm(self):
        expected_consolidations = 9
        expected_weekly_consolidations = 1
        if (any(i != expected_consolidations for i in self._consolidation_counts) or
            self._weekly_consolidation_count != expected_weekly_consolidations or
            self._custom_data_consolidator == 0 or
            self._custom_data_consolidator2 == 0):
            raise ValueError("Unexpected consolidation count")

        for i, sma in enumerate(self._smas):
            if sma.samples != expected_consolidations:
                raise Exception(f"Expected {expected_consolidations} samples in each SMA but found {sma.samples} in SMA in index {i}")

            last_update = self._last_sma_updates[i]
            if sma.current.time != last_update:
                raise Exception(f"Expected SMA in index {i} to have been last updated at {last_update} but was {sma.current.time}")

        if self._monthly_consolidation_count != 0 or self._monthly_consolidator_sma.samples != 0:
            raise Exception("Expected monthly consolidator to not have consolidated any data")

        if self._weekly_consolidator_sma.samples != expected_weekly_consolidations:
            raise Exception(f"Expected {expected_weekly_consolidations} samples in the weekly consolidator SMA but found {self._weekly_consolidator_sma.samples}")

        if self._weekly_consolidator_sma.current.time != self._last_weekly_sma_update:
            raise Exception(f"Expected weekly consolidator SMA to have been last updated at {self._last_weekly_sma_update} but was {self._weekly_consolidator_sma.current.time}")

    # on_data event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    def on_data(self, data):
        if not self.portfolio.invested:
           self.set_holdings(self._symbol, 0.5)
