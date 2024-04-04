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
### Regression test algorithm for scheduled universe selection GH 3890
### </summary>
class FundamentalCustomSelectionTimeRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self._monthStartSelection = 0
        self._monthEndSelection = 0
        self._specificDateSelection = 0
        self._symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA)

        self.SetStartDate(2014, 3, 25)
        self.SetEndDate(2014, 5, 10)
        self.UniverseSettings.Resolution = Resolution.Daily

        # Test use case A
        self.AddUniverse(self.DateRules.MonthStart(), self.SelectionFunction_MonthStart)

        # Test use case B
        otherSettings = UniverseSettings(self.UniverseSettings)
        otherSettings.Schedule.On(self.DateRules.MonthEnd())

        self.AddUniverse(FundamentalUniverse.USA(self.SelectionFunction_MonthEnd, otherSettings))

        # Test use case C
        self.UniverseSettings.Schedule.On(self.DateRules.On(datetime(2014, 5, 9)))
        self.AddUniverse(FundamentalUniverse.USA(self.SelectionFunction_SpecificDate))

    def SelectionFunction_SpecificDate(self, coarse):
        self._specificDateSelection += 1
        if self.Time != datetime(2014, 5, 9):
            raise ValueError("SelectionFunction_SpecificDate unexpected selection: " + str(self.Time))
        return [ self._symbol ]

    def SelectionFunction_MonthStart(self, coarse):
        self._monthStartSelection += 1
        if self._monthStartSelection == 1:
            if self.Time != self.StartDate:
                raise ValueError("Month Start Unexpected initial selection: " + str(self.Time))
        elif self.Time != datetime(2014, 4, 1) and self.Time != datetime(2014, 5, 1):
            raise ValueError("Month Start unexpected selection: " + str(self.Time))
        return [ self._symbol ]

    def SelectionFunction_MonthEnd(self, coarse):
        self._monthEndSelection += 1
        if self._monthEndSelection == 1:
            if self.Time != self.StartDate:
                raise ValueError("Month End unexpected initial selection: " + str(self.Time))
        elif self.Time != datetime(2014, 3, 31) and self.Time != datetime(2014, 4, 30):
            raise ValueError("Month End unexpected selection: " + str(self.Time))
        return [ self._symbol ]

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings(self._symbol, 1)

    def OnEndOfAlgorithm(self):
        if self._monthEndSelection != 3:
            raise ValueError("Month End unexpected selection count: " + str(self._monthEndSelection))
        if self._monthStartSelection != 3:
            raise ValueError("Month Start unexpected selection count: " + str(self._monthStartSelection))
        if self._specificDateSelection != 1:
            raise ValueError("Specific date unexpected selection count: " + str(self._monthStartSelection))
