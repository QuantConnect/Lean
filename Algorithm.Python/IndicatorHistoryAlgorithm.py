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

        self.bollingerBands = self.BB(self.symbol, 20, 2.0, resolution=Resolution.Daily)
        # Let's keep BB values for a 20 day period
        self.bollingerBands.Window = 20
        # Also keep the same period of data for the middle band
        self.bollingerBands.MiddleBand.Window = 20;

    def OnData(self, slice: Slice):
        if not self.bollingerBands.IsReady: return

        # The window is filled up, we have 20 days worth of BB values to use at our convenience
        if self.bollingerBands.WindowCount == self.bollingerBands.Window:
            # We can access the current and oldest (in our period) values of the indicator
            self.Log(f"Current BB value: {self.bollingerBands[0].EndTime} - {self.bollingerBands[0].Value}")
            self.Log(f"Oldest BB value: {self.bollingerBands[self.bollingerBands.WindowCount - 1].EndTime} - "
                     f"{self.bollingerBands[self.bollingerBands.WindowCount - 1].Value}")

            # Let's log the BB values for the last 20 days, for demonstration purposes on how it can be enumerated
            for dataPoint in self.bollingerBands:
                self.Log(f"BB @{dataPoint.EndTime}: {dataPoint.Value}")

            # We can also do the same for internal indicators:
            middleBand = self.bollingerBands.MiddleBand
            self.Log(f"Current BB Middle Band value: {middleBand[0].EndTime} - {middleBand[0].Value}")
            self.Log(f"Oldest BB Middle Band value: {middleBand[middleBand.WindowCount - 1].EndTime} - "
                     f"{middleBand[middleBand.WindowCount - 1].Value}")
            for dataPoint in middleBand:
                self.Log(f"BB Middle Band @{dataPoint.EndTime}: {dataPoint.Value}")
