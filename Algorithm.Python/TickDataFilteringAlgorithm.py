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
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Interfaces import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Interfaces import *


class TickDataFilteringAlgorithm(QCAlgorithm):
    '''Tick Filter Example'''

    def Initialize(self):
        '''Initialize the tick filtering example algorithm'''
        self.SetStartDate(2013,10,07)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(25000)            #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, "SPY", Resolution.Tick)

        # Add our custom data filter.
        self.Securities["SPY"].DataFilter = ExchangeDataFilter(self)


    def OnData(self, data):
        '''Data arriving here will now be filtered.

        Arguments:
            data: Ticks data array
        '''
        if not data.ContainsKey("SPY"): return
        spyTickList = data["SPY"]

        #Ticks return a list of ticks this second
        for tick in spyTickList:
            self.Log(tick.Exchange)

        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

class ExchangeDataFilter(ISecurityDataFilter):
    '''Exchange filter class'''

    def __init__(self, algo):
        '''Save instance of the algorithm namespace'''
        self.__algo = algo
        
    def Filter(self, asset, data):
        '''Filter out a tick from this vehicle, with this new data:

        Arguments:
            data: New data packet
            asset: Vehicle of this filter.
        Returns:
            TRUE: Accept Tick
            FALSE: Reject Tick
        '''
        return type(data) == Tick and data.Exchange == "P"