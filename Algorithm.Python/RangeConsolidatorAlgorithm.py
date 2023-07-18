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
### Example algorithm of how to use RangeConsolidator
### </summary>
class RangeConsolidatorAlgorithm(QCAlgorithm):
    UniversalResolution = Resolution.Daily

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.UniverseSettings.Resolution = self.UniversalResolution

        self.AddEquity("SPY")
        rangeConsolidator = self.CreateRangeConsolidator()
        rangeConsolidator.DataConsolidated += self.OnDataConsolidated
        self.firstDataConsolidated = None;

        self.SubscriptionManager.AddConsolidator("SPY", rangeConsolidator)

    def OnEndOfAlgorithm(self):
        if self.firstDataConsolidated == None:
            raise Exception("The consolidator should have consolidated at least one RangeBar, but it did not consolidated any one")
    def CreateRangeConsolidator(self):
        return RangeConsolidator(100)

    def OnDataConsolidated(self, sender, rangeBar):
        if (self.firstDataConsolidated == None):
            self.firstDataConsolidated = rangeBar

        if abs(rangeBar.Low - rangeBar.High) != 1:
            raise Exception(f"The difference between the High and Low for all RangeBar's should be 1, but for this RangeBar was {abs(rangeBar.Low - rangeBar.High)}")
