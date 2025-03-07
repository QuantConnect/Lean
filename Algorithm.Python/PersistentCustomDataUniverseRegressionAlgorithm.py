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

from datetime import timedelta
from AlgorithmImports import *

### <summary>
### Example algorithm showing that Slice, Securities and Portfolio behave as a Python Dictionary
### </summary>
class PersistentCustomDataUniverseRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.set_start_date(2018, 6, 1)
        self.set_end_date(2018, 6, 19)

        universe = self.add_universe(StockDataSource, "my-stock-data-source", Resolution.DAILY, self.universe_selector)
        self._universe_symbol = universe.symbol
        history = list(self.history[StockDataSource](universe.symbol, datetime(2018, 1, 1), datetime(2018, 6, 1), Resolution.DAILY))
        if (len(history) == 0):
            raise Exception(f"No historical data received for symbol {universe.symbol} and the given date range.")
        self._data_received = False

    def universe_selector(self, data):
        return [x.symbol for x in data]
    
    def OnData(self, slice: Slice):
        self._data_received = True
        if self._universe_symbol not in slice:
            raise RegressionTestException("OnData did not receive data for the universe symbol.")
        
    def OnEndOfAlgorithm(self) -> None:
        if not self._data_received:
            raise RegressionTestException("No data was received after the universe selection.")

class StockDataSource(PythonData):

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live: bool) -> SubscriptionDataSource:
        source = "../../../Tests/TestData/daily-stock-picker-backtest.csv"
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live: bool) -> BaseData:
        if not (line.strip() and line[0].isdigit()): return None

        stocks = StockDataSource()
        stocks.symbol = config.symbol

        try:
            csv = line.split(',')
            stocks.time = datetime.strptime(csv[0], "%Y%m%d")
            stocks.end_time = stocks.time + self.period
            stocks["Symbols"] = csv[1:]

        except ValueError:
            return None

        return stocks
    @property
    def period(self) -> timedelta:
        return timedelta(days=1)
