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

from clr import AddReference
AddReference("System.Core")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.UniverseSelection import *


class UserDefinedUniverseAlgorithm(QCAlgorithm):
    '''This algorithm shows how you can handle universe selection in anyway you like, at any time you like.
    This algorithm has a list of 10 stocks that it rotates through every hour.'''

    def __init__(self):
        self.__Symbols = [ "SPY", "GOOG", "IBM", "AAPL", "MSFT", "CSCO", "ADBE", "WMT" ]
        

    def CoarseSelectionFunction(self, time):
        pyTime = datetime(time)
        hour = pyTime.hour
        index = hour%len(self.__Symbols)
        return [self.__Symbols[index]]


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2015,01,01)  #Set Start Date
        self.SetEndDate(2015,12,01)    #Set End Date
        # this sets the resolution for data subscriptions added by our universe
        self.UniverseSettings.Resolution = Resolution.Hour        
        
        self.AddUniverse("my-universe-name", Resolution.Hour, self.CoarseSelectionFunction)

        
    def OnData(self, data):
        pass

    def OnSecuritiesChanged(self, changes):
        '''Event fired each time the we add/remove securities from the data feed'''
        for security in changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)
        
        for security in changes.AddedSecurities:
            self.SetHoldings(security.Symbol, 1./len(changes.AddedSecurities))