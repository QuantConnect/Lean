# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0.  Copyright 2014 QuantConnect
# Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
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
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Selection import *

class OnEndOfDayRegressionAlgorithm(QCAlgorithm):
    '''Test algorithm verifying OnEndOfDay callbacks are called as expected. See GH issue 2865.'''
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        self._spySymbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        self._bacSymbol = Symbol.Create("BAC", SecurityType.Equity, Market.USA)
        self._ibmSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA)
        self._onEndOfDaySpyCallCount = 0
        self._onEndOfDayBacCallCount = 0
        self._onEndOfDayIbmCallCount = 0

        self.AddUniverse('my_universe_name', self.selection)

    def selection(self, time):
        if time.day == 8:
            return [self._spySymbol.Value, self._ibmSymbol.Value]
        return [self._spySymbol.Value]

    def OnEndOfDay(self, symbol):
        '''We expect it to be called on each day after the first selection process
        happens and the algorithm has a security in it
        '''
        if symbol == self._spySymbol:
            if self._onEndOfDaySpyCallCount == 0:
                # just the first time
                self.SetHoldings(self._spySymbol, 0.5)
                self.AddEquity("BAC")
            self._onEndOfDaySpyCallCount += 1
        if symbol == self._bacSymbol:
            if self._onEndOfDayBacCallCount == 0:
                # just the first time
                self.SetHoldings(self._bacSymbol, 0.5)
            self._onEndOfDayBacCallCount += 1
        if symbol == self._ibmSymbol:
            self._onEndOfDayIbmCallCount += 1

        self.Log("OnEndOfDay() called: " + str(self.UtcTime)
                + ". SPY count " + str(self._onEndOfDaySpyCallCount)
                + ". BAC count " + str(self._onEndOfDayBacCallCount)
                + ". IBM count " + str(self._onEndOfDayIbmCallCount))

    def OnEndOfAlgorithm(self):
        '''Assert expected behavior'''
        if self._onEndOfDaySpyCallCount != 5:
            raise ValueError("OnEndOfDay(SPY) unexpected count call " + str(self._onEndOfDaySpyCallCount))
        if self._onEndOfDayBacCallCount != 4:
            raise ValueError("OnEndOfDay(BAC) unexpected count call " + str(self._onEndOfDayBacCallCount))
        if self._onEndOfDayIbmCallCount != 1:
            raise ValueError("OnEndOfDay(IBM) unexpected count call " + str(self._onEndOfDayIbmCallCount))