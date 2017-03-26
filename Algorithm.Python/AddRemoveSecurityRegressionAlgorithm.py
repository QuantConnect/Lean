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

from System import *
from QuantConnect import *
from QuantConnect.Orders import OrderStatus
from QuantConnect.Algorithm import QCAlgorithm

class AddRemoveSecurityRegressionAlgorithm(QCAlgorithm):
    '''Basic template algorithm simply initializes the date range and cash'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013,10,07)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.spy = self.AddSecurity(SecurityType.Equity, "SPY")
        
        self._lastAction = None


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self._lastAction is not None and self._lastAction.Date == self.Time.Date:
            return

        if not self.Portfolio.Invested:
            self.SetHoldings(self.spy.Symbol, .5)
            self._lastAction = self.Time

        if self.Time.DayOfWeek == DayOfWeek.Tuesday:
            self.aig = self.AddSecurity(SecurityType.Equity, "AIG")
            self.bac = self.AddSecurity(SecurityType.Equity, "BAC")
            self._lastAction = self.Time

        if self.Time.DayOfWeek == DayOfWeek.Wednesday:
            self.SetHoldings(self.aig.Symbol, .25)
            self.SetHoldings(self.bac.Symbol, .25)
            self._lastAction = self.Time

        if self.Time.DayOfWeek == DayOfWeek.Thursday:
            self.RemoveSecurity(self.aig.Symbol)
            self.RemoveSecurity(self.bac.Symbol)
            self._lastAction = self.Time

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Submitted:
            self.Debug("{0}: Submitted: {1}".format(self.Time, self.Transactions.GetOrderById(orderEvent.OrderId)))
        if orderEvent.Status == OrderStatus.Filled:
            self.Debug("{0}: Filled: {1}".format(self.Time, self.Transactions.GetOrderById(orderEvent.OrderId)))