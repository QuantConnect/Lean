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


class WarmupAlgorithm(QCAlgorithm):
    '''Warmup Algorithm'''
    def __init__(self):
        self.__first = True
        self.__symbol = "SPY"
        self.__fastPeriod = 60
        self.__slowPeriod = 3600
        self.__fast = None
        self.__slow = None


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013,10,8)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, self.__symbol, Resolution.Second)

        self.__fast = self.EMA(self.__symbol, self.__fastPeriod)
        self.__slow = self.EMA(self.__symbol, self.__slowPeriod)

        self.SetWarmup(self.__slowPeriod)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self.__first and not self.IsWarmingUp:
            self.__first = False
            self.Log("Fast: {0}".format(self.__fast.Samples))
            self.Log("Slow: {0}".format(self.__slow.Samples))

        if self.__fast.Current.Value > self.__slow.Current.Value:
            self.SetHoldings(self.__symbol, 1)
        else:
            self.SetHoldings(self.__symbol, -1)