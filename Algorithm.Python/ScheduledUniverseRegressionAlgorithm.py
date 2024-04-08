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
### Regression algorithm asserting the behavior of a ScheduledUniverse
### </summary>
class BasicTemplateAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10, 8)
        
        self._spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        self._selectionTime =[ datetime(2013, 10, 7, 1, 0, 0), datetime(2013, 10, 8, 1, 0, 0)]

        self.AddUniverse(ScheduledUniverse(self.DateRules.EveryDay(), self.TimeRules.At(1, 0), self.SelectAssets))


    def SelectAssets(self, time):
        self.Debug(f"Universe selection called: {Time}")
        expectedTime = self._selectionTime.pop(0)
        if expectedTime != self.Time:
            raise ValueError(f"Unexpected selection time {self.Time} expected {expectedTime}")

        return [ self._spy ]
    
    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings(self._spy, 1)

    def OnEndOfAlgorithm(self):
        if len(self._selectionTime) > 0:
            raise ValueError("Unexpected selection times")
