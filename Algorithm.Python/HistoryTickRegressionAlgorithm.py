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

    def Initialize(self):
        self.SetStartDate(2013, 10, 11)
        self.SetEndDate(2013, 10, 11)

        self._symbol = self.AddEquity("SPY", Resolution.Tick).Symbol

    def OnEndOfAlgorithm(self):
        history = list(self.History[Tick](self._symbol, timedelta(days=1), Resolution.Tick))
        quotes = [x for x in history if x.TickType == TickType.Quote]
        trades = [x for x in history if x.TickType == TickType.Trade]

        if not quotes or not trades:
            raise Exception("Expected to find at least one tick of each type (quote and trade)")
