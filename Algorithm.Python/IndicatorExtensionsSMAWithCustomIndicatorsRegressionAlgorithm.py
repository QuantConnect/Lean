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

class IndicatorExtensionsSMAWithCustomIndicatorsRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2020, 2, 20)
        self.set_end_date(2020, 4, 20)
        self.qqq = self.add_equity("QQQ", Resolution.DAILY).symbol
        self.range_indicator = RangeIndicator("range1")
        self.range_sma = IndicatorExtensions.sma(self.range_indicator, 5)

        self.range_indicator_2 = RangeIndicator2("range2")
        self.range_sma_2 = IndicatorExtensions.sma(self.range_indicator_2, 5)

    def on_data(self, data):
        self.range_indicator.update(data.bars.get(self.qqq))
        self.range_indicator_2.update(data.bars.get(self.qqq))

        self.debug(f"{self.range_indicator.name} {self.range_indicator.value}")
        self.debug(f"{self.range_sma.name} {self.range_sma.current.value}")

        self.debug(f"{self.range_indicator_2.name} {self.range_indicator_2.value}")
        self.debug(f"{self.range_sma_2.name} {self.range_sma_2.current.value}")

    def on_end_of_algorithm(self):
        if not self.range_sma.is_ready:
            raise Exception(f"{self.range_sma.name} should have been ready at the end of the algorithm, but it wasn't. The indicator received {self.range_sma.samples} samples.")

        if not self.range_sma_2.is_ready:
            raise Exception(f"{self.range_sma_2.name} should have been ready at the end of the algorithm, but it wasn't. The indicator received {self.range_sma_2.samples} samples.")

class RangeIndicator(PythonIndicator):
    def __init__(self, name):
        self.name = name
        self.time = datetime.min
        self.value = 0
        self.is_ready = False;

    @property
    def is_ready(self):
        return self._is_ready

    @is_ready.setter
    def is_ready(self, value):
        self._is_ready = value

    def update(self, bar: TradeBar):
        if bar is None:
            return False

        self.value = bar.high - bar.low
        self.time = bar.time
        self.is_ready = True
        self.on_updated(IndicatorDataPoint(bar.end_time, self.value))
        return True

class RangeIndicator2(PythonIndicator):
    def __init__(self, name):
        self.name = name
        self.time = datetime.min
        self.value = 0
        self._is_ready = False;

    @property
    def is_ready(self):
        return self._is_ready

    def update(self, bar: TradeBar):
        if bar is None:
            return False

        self.value = bar.high - bar.low
        self.time = bar.time
        self._is_ready = True
        self.on_updated(IndicatorDataPoint(bar.end_time, self.value))
        return True
