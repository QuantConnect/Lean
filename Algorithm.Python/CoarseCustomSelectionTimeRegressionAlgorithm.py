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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.UniverseSelection import CoarseFundamentalUniverse
from datetime import *

### <summary>
### Regression test algorithm for scheduled universe selection GH 3890
### </summary>
class CoarseCustomSelectionTimeRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self._monthStartSelection = 0
        self._monthStartFineSelection = 0
        self._monthEndSelection = 0
        self._symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA)

        self.SetStartDate(2014, 3, 25)
        self.SetEndDate(2014, 5, 10)
        self.UniverseSettings.Resolution = Resolution.Daily

        # Test use case A
        self.AddUniverse(self.CoarseSelectionFunction_MonthStart, self.DateRules.MonthStart());

        # Test use case B
        otherSettings = self.UniverseSettings.Clone();
        otherSettings.Schedule.On(self.DateRules.MonthEnd());
        self.AddUniverse(CoarseFundamentalUniverse(otherSettings, self.SecurityInitializer, self.CoarseSelectionFunction_MonthEnd));

        # Test use case C
        self.AddUniverse(self.CoarseSelectionFunction_MonthStart, self.FineSelectionFunction_MonthStart, self.DateRules.MonthStart());

    def CoarseSelectionFunction_MonthStart(self, coarse):
        self._monthStartSelection += 1
        if self.Time != datetime(2014, 4, 1) and self.Time != datetime(2014, 5, 1):
            raise ValueError("Month Start unexpected selection: " + str(self.Time));
        return [ self._symbol ]

    def CoarseSelectionFunction_MonthEnd(self, coarse):
        self._monthEndSelection += 1
        if self.Time != datetime(2014, 3, 31) and self.Time != datetime(2014, 4, 30):
            raise ValueError("Month End unexpected selection: " + str(self.Time));
        return [ self._symbol ]

    def FineSelectionFunction_MonthStart(self, fine):
        self._monthStartFineSelection += 1
        if self.Time != datetime(2014, 4, 1) and self.Time != datetime(2014, 5, 1):
            raise ValueError("Month Start Fine unexpected selection: " + str(self.Time));
        return [ self._symbol ]

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings(self._symbol, 1)

    def OnEndOfAlgorithm(self):
        if self._monthEndSelection != 2:
            raise ValueError("Month End unexpected selection count: " + str(self._monthEndSelection))
        if self._monthStartFineSelection != 2:
            raise ValueError("Month Start Fine unexpected selection count: " + str(self._monthEndSelection))
        if self._monthStartSelection != 4:
            raise ValueError("Month Start unexpected selection count: " + str(self._monthStartSelection))
