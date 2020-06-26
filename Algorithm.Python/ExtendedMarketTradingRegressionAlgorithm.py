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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

import datetime
from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import QCAlgorithm

### <summary>
### This algorithm demonstrates extended market hours trading.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="assets" />
### <meta name="tag" content="regression test" />
class ExtendedMarketTradingRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.spy = self.AddEquity("SPY", Resolution.Minute, Market.USA, True, 1, True)

        self._lastAction = None

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if self._lastAction is not None and self._lastAction.date() == self.Time.date():
            return

        spyBar = data.Bars['SPY']

        if not self.InMarketHours():
            self.LimitOrder("SPY", 10, spyBar.Low);
            self._lastAction = self.Time

    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))
        if self.InMarketHours():
            raise Exception("Order processed during market hours.")

    def InMarketHours(self):
        now = self.Time.time()
        open = datetime.time(9,30,0)
        close = datetime.time(16,0,0)
        return (open < now) and (close > now)

