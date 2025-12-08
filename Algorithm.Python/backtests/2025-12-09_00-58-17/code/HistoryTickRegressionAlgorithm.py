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
### Regression algorithm asserting that tick history request includes both trade and quote data
### </summary>
class HistoryTickRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 12)
        self.set_end_date(2013, 10, 13)

        self._symbol = self.add_equity("SPY", Resolution.TICK).symbol

        trades_count = 0
        quotes_count = 0
        for point in self.history[Tick](self._symbol, timedelta(days=1), Resolution.TICK):
            if point.tick_type == TickType.TRADE:
                trades_count += 1
            elif point.tick_type == TickType.QUOTE:
                quotes_count += 1

            if trades_count > 0 and quotes_count > 0:
                # We already found at least one tick of each type, we can exit the loop
                break

        if trades_count == 0 or quotes_count == 0:
            raise AssertionError("Expected to find at least one tick of each type (quote and trade)")

        self.quit()
