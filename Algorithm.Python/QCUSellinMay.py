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

from datetime import datetime 

import clr
clr.AddReference("System")
clr.AddReference("QuantConnect.Algorithm")
clr.AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *


class QCUSellinMay(QCAlgorithm):
    '''Basic template algorithm simply initializes the date range and cash'''

    def __init__(self):
        self.symbol = "SPY"
        self.quantity = 400

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(1998,1,7)   #Set Start Date
        self.SetEndDate(2012,12,30)   #Set End Date
        self.SetCash(100000)          #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, self.symbol)

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        
        # If you do not know the C# DateTime object, convert self.Time to python datetime and have fun 
        time = datetime(self.Time)
        
        if self.Portfolio.HoldStock and time.month == 5:          # May is the 5th month
            self.Order(self.symbol, -self.quantity)
            self.Debug(time.strftime("QCU Sell In May: Flat %Y %B"))
        elif not self.Portfolio.HoldStock and time.month == 11:   # November is the 11th month
            self.Order(self.symbol, self.quantity)
            self.Debug(time.strftime("QCU Sell In May: Long %Y %B"))