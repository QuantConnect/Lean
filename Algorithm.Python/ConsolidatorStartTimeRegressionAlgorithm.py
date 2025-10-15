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
from collections import deque

### <summary>
### Regression algorithm show casing and asserting the behavior of creating a consolidator specifying the start time
### </summary>
class ConsolidatorStartTimeRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 4)
        self.set_end_date(2013, 10, 4)

        self.add_equity("SPY", Resolution.MINUTE)

        consolidator = TradeBarConsolidator(timedelta(hours=1), start_time=timedelta(hours=9, minutes=30))
        consolidator.data_consolidated += self.bar_handler
        self.subscription_manager.add_consolidator("SPY", consolidator)

        self._expectedConsolidationTime = deque()
        self._expectedConsolidationTime.append(time(14, 30))
        self._expectedConsolidationTime.append(time(13, 30))
        self._expectedConsolidationTime.append(time(12, 30))
        self._expectedConsolidationTime.append(time(11, 30))
        self._expectedConsolidationTime.append(time(10, 30))
        self._expectedConsolidationTime.append(time(9, 30))

    def bar_handler(self, _, bar):
        if self.time != bar.end_time:
            raise RegressionTestException(f"Unexpected consolidation time {bar.Time} != {Time}!")

        self.debug(f"{self.time} - Consolidation")
        expected = self._expectedConsolidationTime.pop()
        if bar.time.time() != expected:
            raise RegressionTestException(f"Unexpected consolidation time {bar.time.time()} != {expected}!")

        if bar.period != timedelta(hours=1):
            raise RegressionTestException(f"Unexpected consolidation period {bar.period}!")

    def on_end_of_algorithm(self):
        if len(self._expectedConsolidationTime) > 0:
            raise RegressionTestException("Unexpected consolidation times!")
