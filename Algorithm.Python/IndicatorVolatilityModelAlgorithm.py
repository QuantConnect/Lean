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
### Algorithm illustrating the usage of the IndicatorVolatilityModel
### with an externally updated indicator.
### </summary>
class IndicatorVolatilityModelAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2014, 1, 1)
        self.set_end_date(2014, 12, 31)
        self.set_cash(100000)

        self._indicator_periods = 7

        equity = self.add_equity(
            "AAPL",
            Resolution.DAILY,
            data_normalization_mode=DataNormalizationMode.RAW
        )
        self._symbol = equity.symbol

        std = StandardDeviation(self._indicator_periods)
        mean = SimpleMovingAverage(self._indicator_periods)
        self._volatility_indicator = IndicatorExtensions.over(std, mean)

        # IndicatorVolatilityModel can consume any indicator that yields a volatility value.
        # Here we provide a ratio of std/mean and update the underlying indicators in on_data.
        equity.set_volatility_model(IndicatorVolatilityModel(self._volatility_indicator))

        self._std = std
        self._mean = mean

    def on_data(self, slice: Slice):
        if not slice.bars.contains_key(self._symbol):
            return

        bar = slice.bars[self._symbol]
        if bar.close <= 0:
            return

        self._std.update(bar.end_time, bar.close)
        self._mean.update(bar.end_time, bar.close)

        if self._volatility_indicator.is_ready and not self.portfolio.invested:
            self.set_holdings(self._symbol, 1)
