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
AddReference("System.Core")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.UniverseSelection import *


class CoarseFundamentalTop5Algorithm(QCAlgorithm):
    '''In this algorithm we demonstrate how to use the coarse fundamental data to define a universe as the top dollar volume'''
    def __init__(self):
        self.__numberOfSymbols = 5
        self.__changes = SecurityChanges.None

    # sort the data by daily dollar volume and take the top 'NumberOfSymbols'
    def CoarseSelectionFunction(self, coarse):
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True) 

        # return the symbol objects of the top entries from our sorted collection
        top5 = sortedByDollarVolume[:self.__numberOfSymbols]

        # we need to return only the symbol objects
        return [x.Symbol for x in top5]


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2014,01,01)  #Set Start Date
        self.SetEndDate(2015,01,01)    #Set End Date
        self.SetCash(50000)            #Set Strategy Cash
        
        self.UniverseSettings.Resolution = Resolution.Daily        
        
        # this add universe method accepts a single parameter that is a function that
        # accepts an IEnumerable<CoarseFundamental> and returns IEnumerable<Symbol>
        self.AddUniverse(self.CoarseSelectionFunction)

        
    # Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.
    def OnData(self, data):
        # if we have no changes, do nothing
        if self.__changes == SecurityChanges.None: return

        # liquidate removed securities
        for security in self.__changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)
         
        # we want 20% allocation in each security in our universe
        for security in self.__changes.AddedSecurities:
            self.SetHoldings(security.Symbol, Decimal(0.2))    
 
        self.__changes = SecurityChanges.None;


    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        self.__changes = changes