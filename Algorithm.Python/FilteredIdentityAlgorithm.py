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
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Market import Tick

### <summary>
### Example algorithm of the Identity indicator with the filtering enhancement. Filtering is used to check
### the output of the indicator before returning it.
### </summary>
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
class FilteredIdentityAlgorithm(QCAlgorithm):
    ''' Example algorithm of the Identity indicator with the filtering enhancement '''
    
    def Initialize(self):
        
        self.SetStartDate(2014,5,2)       # Set Start Date
        self.SetEndDate(self.StartDate)   # Set End Date
        self.SetCash(100000)              # Set Stratgy Cash
 
        # Find more symbols here: http://quantconnect.com/data
        security = self.AddForex("EURUSD", Resolution.Tick)

        self.symbol = security.Symbol
        self.identity = self.FilteredIdentity(self.symbol, None, self.Filter)
    
    def Filter(self, data):
        '''Filter function: True if data is not an instance of Tick. If it is, true if TickType is Trade
        data -- Data for applying the filter'''
        if isinstance(data, Tick):
            return data.TickType == TickType.Trade
        return True
        
    def OnData(self, data):
        # Since we are only accepting TickType.Trade,
        # this indicator will never be ready
        if not self.identity.IsReady: return
        if not self.Portfolio.Invested:
            self.SetHoldings(self.symbol, 1)
            self.Debug("Purchased Stock")