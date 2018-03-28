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

### <summary>
### Basic template algorithm simply initializes the date range and cash
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="limit orders" />
### <meta name="tag" content="placing orders" />
### <meta name="tag" content="updating orders" />
### <meta name="tag" content="regression test" />
class LimitFillRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10,7)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY", Resolution.Second)

        self.mid_datetime = self.StartDate + (self.EndDate - self.StartDate)/2


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if data.ContainsKey("SPY"):
            if self.IsRoundHour(self.Time):
                negative = 1 if self.Time < self.mid_datetime else -1
                self.LimitOrder("SPY", negative*10, data["SPY"].Price)


    def IsRoundHour(self, dateTime):
        '''Verify whether datetime is round hour'''
        return dateTime.minute == 0 and dateTime.second == 0