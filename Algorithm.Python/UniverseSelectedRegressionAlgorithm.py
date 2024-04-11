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
### Regression algorithm asserting the behavior of Universe.Selected collection
### </summary>
class UniverseSelectedRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014, 3, 25)
        self.SetEndDate(2014, 3, 27)

        self.UniverseSettings.Resolution = Resolution.Daily

        self._universe = self.AddUniverse(self.SelectionFunction)
        self.selectionCount = 0

    def SelectionFunction(self, fundamentals):
        sortedByDollarVolume = sorted(fundamentals, key=lambda x: x.DollarVolume, reverse=True)

        sortedByDollarVolume = sortedByDollarVolume[self.selectionCount:]
        self.selectionCount = self.selectionCount + 1

        # return the symbol objects of the top entries from our sorted collection
        return [ x.Symbol for x in sortedByDollarVolume[:self.selectionCount] ]

    def OnData(self, data):
        if Symbol.Create("TSLA", SecurityType.Equity, Market.USA) in self._universe.Selected:
            raise ValueError(f"TSLA shouldn't of been selected")

        self.Buy(next(iter(self._universe.Selected)), 1)

    def OnEndOfAlgorithm(self):
        if self.selectionCount != 3:
            raise ValueError(f"Unexpected selection count {self.selectionCount}")
        if self._universe.Selected.Count != 3 or self._universe.Selected.Count == self._universe.Members.Count:
            raise ValueError(f"Unexpected universe selected count {self._universe.Selected.Count}")
