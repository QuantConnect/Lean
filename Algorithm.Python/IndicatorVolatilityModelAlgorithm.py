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
### Algorithm illustrating the usage of the IndicatorVolatilityModel and
### how to handle splits and dividends to avoid price discontinuities
### </summary>
class IndicatorVolatilityModelAlgorithm(QCAlgorithm):

    _indicator_periods = 7

    _data_normalization_mode = DataNormalizationMode.RAW

    def initialize(self):
        self.set_start_date(2014, 1, 1)
        self.set_end_date(2014, 12, 31)
        self.set_cash(100000)

        equity = self.add_equity("AAPL", Resolution.DAILY, data_normalization_mode=self._data_normalization_mode)
        self._aapl = equity.symbol

        std = StandardDeviation(self._indicator_periods)
        mean = SimpleMovingAverage(self._indicator_periods)
        self._indicator = IndicatorExtensions.over(std, mean)

        def update_indicator(security, data, indicator):
            if data.price > 0:
                std.update(data.time, data.price)
                mean.update(data.time, data.price)

        self._volatility_model = IndicatorVolatilityModel(self._indicator, update_indicator)
        equity.set_volatility_model(self._volatility_model)

        self._splits_and_dividends_count = 0
        self._volatility_checked = False

    def on_data(self, slice):
        if slice.splits.contains_key(self._aapl) or slice.dividends.contains_key(self._aapl):
            self._splits_and_dividends_count += 1

            # On a split or dividend event, we need to reset and warm the indicator up as Lean does to BaseVolatilityModel's
            # to avoid big jumps in volatility due to price discontinuities
            self._indicator.reset()
            equity = self.securities[self._aapl]
            VolatilityModelExtensions.warm_up(
                self._volatility_model,
                self,
                equity,
                equity.resolution,
                self._indicator_periods,
                self._data_normalization_mode
            )

    def on_end_of_day(self, symbol):
        if symbol != self._aapl or not self._indicator.is_ready:
            return

        self._volatility_checked = True

        # This is expected only in this case, 0.05 is not a magical number of any kind.
        # Just making sure we don't get big jumps on volatility
        volatility = self.securities[self._aapl].volatility_model.volatility
        if volatility <= 0 or volatility > 0.05:
            raise RegressionTestException(
                "Expected volatility to stay less than 0.05 (not big jumps due to price discontinuities on splits and dividends), "
                f"but got {volatility}")

    def on_end_of_algorithm(self):
        if self._splits_and_dividends_count == 0:
            raise RegressionTestException("Expected to get at least one split or dividend event")

        if not self._volatility_checked:
            raise RegressionTestException("Expected to check volatility at least once")
