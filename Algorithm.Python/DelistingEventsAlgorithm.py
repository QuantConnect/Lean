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
from QuantConnect.Orders import *


class DelistingEventsAlgorithm(QCAlgorithm):
    '''Showcases the delisting event of QCAlgorithm
    The data for this algorithm isn't in the github repo, so this will need to be run on the QC site'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2007, 05, 16)  #Set Start Date
        self.SetEndDate(2007, 05, 25)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, "AAA", Resolution.Daily)
        self.AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self.Transactions.OrdersCount == 0:
            self.SetHoldings("AAA", 1)
            self.Debug("Purchased stock")

        for kvp in data.Bars:
            symbol = kvp.Key
            value = kvp.Value

            self.Log("OnData(Slice): {0}: {1}: {2}".format(self.Time, symbol, value.Close))

        # the slice can also contain delisting data: data.Delistings in a dictionary string->Delisting
        for kvp in data.Delistings:
            symbol = kvp.Key
            value = kvp.Value
            
            if value.Type == DelistingType.Warning:
                self.Log("OnData(Delistings): {0}: {1} will be delisted at end of day today.".format(self.Time, symbol))
            if value.Type == DelistingType.Delisted:
                self.Log("OnData(Delistings): {0}: {1} has been delisted.".format(self.Time, symbol))


    def OnOrderEvent(self, orderEvent):
        self.Log("OnOrderEvent(OrderEvent): {0}: {1}".format(self.Time, orderEvent))