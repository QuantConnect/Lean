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
from QuantConnect.Algorithm import *
from QuantConnect.Orders import *
from QuantConnect.Data.UniverseSelection import *


class UniverseSelectionDefinitionsAlgorithm(QCAlgorithm):
    '''This algorithm shows some of the various helper methods available when defining universes'''

    def __init__(self):
        self.__changes = SecurityChanges.None


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013,10,07)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # subscriptions added via universe selection will have this resolution
        self.UniverseSettings.Resolution = Resolution.Hour
        # force securities to remain in the universe for a minimm of 30 minutes
        self.UniverseSettings.MinimumTimeInUniverse = TimeSpan.FromMinutes(30)
        
        # add universe for the top 50 stocks by dollar volume
        self.AddUniverse(self.Universe.DollarVolume.Top(50));
        # add universe for the bottom 50 stocks by dollar volume
        self.AddUniverse(self.Universe.DollarVolume.Bottom(50));
        # add universe for the 90th dollar volume percentile
        self.AddUniverse(self.Universe.DollarVolume.Percentile(90));
        # add universe for stocks between the 70th and 80th dollar volume percentile
        self.AddUniverse(self.Universe.DollarVolume.Percentile(70, 80));
        
        
    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self.__changes == SecurityChanges.None: return

        # liquidate securities that fell out of our universe
        for security in self.__changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)
        
        # invest in securities just added to our universe
        for security in self.__changes.AddedSecurities:
            if not security.Invested:
                self.MarketOrder(security.Symbol, 10)     

        self.__changes = SecurityChanges.None;


    def OnSecuritiesChanged(self, changes):
        self.__changes = changes