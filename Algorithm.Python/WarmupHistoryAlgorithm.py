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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *

class WarmupHistoryAlgorithm(QCAlgorithm):
    '''This algorithm demonstrates using the history provider to
retrieve data to warm up indicators before data is received'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2014,5,2)   #Set Start Date
        self.SetEndDate(2014,5,2)     #Set End Date
        self.SetCash(100000)            #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        forex = self.AddForex("EURUSD", Resolution.Second)
        
        self.__symbol = forex.Symbol
        self.__fastPeriod = 60
        self.__slowPeriod = 3600
        self.__fast = self.EMA(self.__symbol, self.__fastPeriod)
        self.__slow = self.EMA(self.__symbol, self.__slowPeriod)
        
        # "self.__slowPeriod + 1" because rolling window waits for one to fall off the back to be considered ready
        history = map(lambda x: x[self.__symbol], self.History(self.__slowPeriod + 1))
        for bar in history:
        	datapoint = IndicatorDataPoint(bar.EndTime, bar.Close)
        	self.__fast.Update(datapoint)
        	self.__slow.Update(datapoint)

        self.Log("FAST IS {0} READY. Samples: {1}".format("" if self.__fast.IsReady else "NOT", self.__fast.Samples))
        self.Log("SLOW IS {0} READY. Samples: {1}".format("" if self.__slow.IsReady else "NOT", self.__slow.Samples))


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        
        if self.__fast.Current.Value > self.__slow.Current.Value:
            self.SetHoldings(self.__symbol, 1)
        else:
            self.SetHoldings(self.__symbol, -1)