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
    def Initialize(self):
        self.SetStartDate(2012, 1, 1)
        self.SetEndDate(2013, 1, 1)

        self.AddEquity("SPY", Resolution.Daily)
        rangeConsolidator = self.CreateRangeConsolidator()
        rangeConsolidator.DataConsolidated += self.OnDataConsolidated

        self.SubscriptionManager.AddConsolidator("SPY", rangeConsolidator)

    def CreateRangeConsolidator(self):
        return RangeConsolidator(100, lambda x: x.Value, lambda x: x.Volume)

    def OnDataConsolidated(self, sender, rangeBar):
        if abs(rangeBar.Low - rangeBar.High) != 1:
            raise Exception(f"The difference between the High and Low for all RangeBar's should be 1, but for this RangeBar was {abs(rangeBar.Low - rangeBar.High)}")
