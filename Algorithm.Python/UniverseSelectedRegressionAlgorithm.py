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
        self.SetStartDate(2014, 3, 24)
        self.SetEndDate(2014, 3, 28)

        self.UniverseSettings.Resolution = Resolution.Daily

        self.universe = self.AddUniverse(self.SelectionFunction)
        self.selectionCount = 0

    def SelectionFunction(self, fundamentals):
        sortedByDollarVolume = sorted(fundamentals, key=lambda x: x.DollarVolume, reverse=True)

        sortedByDollarVolume = sortedByDollarVolume[self.selectionCount:]
        self.selectionCount = self.selectionCount + 1

        # return the symbol objects of the top entries from our sorted collection
        return [ x.Symbol for x in sortedByDollarVolume[:1] ]

    def OnData(self, data):
        if Symbol.Create("TSLA", SecurityType.Equity, Market.USA) in self.universe.Selected:
            raise ValueError(f"Unexpected selected symbol")

        self.Buy(next(iter(self.universe.Selected)), 1)

    def OnEndOfAlgorithm(self):
        if self.selectionCount != 5:
            raise ValueError(f"Unexpected selection count {self.selectionCount}")
        if self.universe.Selected.Count != 1 or self.universe.Selected.Count == self.universe.Members.Count:
            raise ValueError(f"Unexpected universe selected count {self.universe.Selected.Count}")
