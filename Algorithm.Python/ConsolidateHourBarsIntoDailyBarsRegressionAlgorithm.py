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

class ConsolidateHourBarsIntoDailyBarsRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        # change the start date between runs to check that warm up shows the correct value
        self.set_start_date(2020, 5, 1)
        self.set_end_date(2020, 6, 5)
        self.set_cash(100000)

        self.spy = self.add_equity("SPY", Resolution.HOUR).symbol

        # Resolution.DAILY indicators
        self._rsi = RelativeStrengthIndex("First", 15, MovingAverageType.WILDERS)
        self.register_indicator(self.spy, self._rsi, Resolution.DAILY)

        # Resolution.DAILY indicators
        self._rsi_timedelta = RelativeStrengthIndex("Second", 15, MovingAverageType.WILDERS)
        self._values = {}
        self.count = 0;

    def on_data(self, data: Slice):
        if self.is_warming_up:
            return

        if data.contains_key(self.spy) and data[self.spy] != None:
            if self.time.month == self.end_date.month:
                history = self.history[TradeBar](self.spy, self.count, Resolution.DAILY)
                for bar in history:
                    time = bar.end_time.strftime('%Y-%m-%d')
                    self._rsi_timedelta.update(bar.end_time, bar.close)
                    if self._rsi_timedelta.current.value != self._values[time]:
                        raise Exception(f"Both {self._rsi.name} and {self._rsi_timedelta.name} should have the same values, but they differ. {self._rsi.name}: {self._values[time]} | {self._rsi_timedelta.name}: {self._rsi_timedelta.current.value}")
                self.quit()
            else:
                time = self.time.strftime('%Y-%m-%d')
                self._values[time] = self._rsi.current.value
                if self.time.hour == 16:
                    self.count += 1
