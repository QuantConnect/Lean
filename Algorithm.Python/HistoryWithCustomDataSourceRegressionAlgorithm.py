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
### Regression test illustrating how history from custom data sources can be requested. The <see cref="QCAlgorithm.history"/> method used in this
### example also allows to specify other parameters than just the resolution, such as the data normalization mode, the data mapping mode, etc.
### </summary>
class HistoryWithCustomDataSourceRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2014, 6, 5)
        self.set_end_date(2014, 6, 6)

        self.aapl = self.add_data(CustomData, "AAPL", Resolution.MINUTE).symbol
        self.spy = self.add_data(CustomData, "SPY", Resolution.MINUTE).symbol

    def on_end_of_algorithm(self):
        aapl_history = self.history(CustomData, self.aapl, self.start_date, self.end_date, Resolution.MINUTE,
            fill_forward=False, extended_market_hours=False, data_normalization_mode=DataNormalizationMode.RAW).droplevel(0, axis=0)
        spy_history = self.history(CustomData, self.spy, self.start_date, self.end_date, Resolution.MINUTE,
            fill_forward=False, extended_market_hours=False, data_normalization_mode=DataNormalizationMode.RAW).droplevel(0, axis=0)

        if aapl_history.size == 0 or spy_history.size == 0:
            raise AssertionError("At least one of the history results is empty")

        # Check that both resutls contain the same data, since CustomData fetches APPL data regardless of the symbol
        if not aapl_history.equals(spy_history):
            raise AssertionError("Histories are not equal")

class CustomData(PythonData):
    '''Custom data source for the regression test algorithm, which returns AAPL equity data regardless of the symbol requested.'''

    def get_source(self, config, date, is_live_mode):
        return TradeBar().get_source(
            SubscriptionDataConfig(
                config,
                CustomData,
                # Create a new symbol as equity so we find the existing data files
                # Symbol.create(config.mapped_symbol, SecurityType.EQUITY, config.market)),
                Symbol.create("AAPL", SecurityType.EQUITY, config.market)),
            date,
            is_live_mode)

    def reader(self, config, line, date, is_live_mode):
        trade_bar = TradeBar.parse_equity(config, line, date)
        data = CustomData()
        data.time = trade_bar.time
        data.value = trade_bar.value
        data.close = trade_bar.close
        data.open = trade_bar.open
        data.high = trade_bar.high
        data.low = trade_bar.low
        data.volume = trade_bar.volume

        return data
