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
### Demonstration algorithm of indicators history window usage
### </summary>
class IndicatorHistoryAlgorithm(QCAlgorithm):
    '''Demonstration algorithm of indicators history window usage.'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2013, 1, 1)
        self.SetEndDate(2014, 12, 31)
        self.SetCash(25000)

        self.symbol = self.AddEquity("SPY", Resolution.Daily).Symbol

        self.sma = self.SMA(self.symbol, 14, Resolution.Daily)
        # Let's keep SMA values for a 14 day period
        self.sma.Window = 14

    def OnData(self, slice: Slice):
        if not self.sma.IsReady or self.Portfolio.Invested: return

        # The window is filled up, we have 14 days worth of SMA values to use at our convenience
        if self.sma.WindowCount == self.sma.Window:
            # Let's say that hypothetically, we want to buy shares of the equity when the SMA is less than its 14 days old value
            if self.sma[0] < self.sma[self.sma.WindowCount - 1]:
                self.Buy(self.symbol, 100)

    def OnEndOfAlgorithm(self):
        if not self.Portfolio.Invested:
            raise Exception("Expected the portfolio to be invested at the end of the algorithm")
