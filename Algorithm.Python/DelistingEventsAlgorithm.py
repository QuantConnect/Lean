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

### <summary>
### Demonstration of using the Delisting event in your algorithm. Assets are delisted on their last day of trading, or when their contract expires.
### This data is not included in the open source project.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="data event handlers" />
### <meta name="tag" content="delisting event" />
class DelistingEventsAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2007, 5, 16)  #Set Start Date
        self.SetEndDate(2007, 5, 25)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("AAA", Resolution.Daily)
        self.AddEquity("SPY", Resolution.Daily)


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

        aaa = self.Securities["AAA"]
        if aaa.IsDelisted and aaa.IsTradable:
            raise Exception("Delisted security must NOT be tradable")

        if not aaa.IsDelisted and not aaa.IsTradable:
            raise Exception("Securities must be marked as tradable until they're delisted or removed from the universe")

        for kvp in data.Delistings:
            symbol = kvp.Key
            value = kvp.Value

            if value.Type == DelistingType.Warning:
                self.Log("OnData(Delistings): {0}: {1} will be delisted at end of day today.".format(self.Time, symbol))

                # liquidate on delisting warning
                self.SetHoldings(symbol, 0)

            if value.Type == DelistingType.Delisted:
                self.Log("OnData(Delistings): {0}: {1} has been delisted.".format(self.Time, symbol))

                # fails because the security has already been delisted and is no longer tradable
                self.SetHoldings(symbol, 1)


    def OnOrderEvent(self, orderEvent):
        self.Log("OnOrderEvent(OrderEvent): {0}: {1}".format(self.Time, orderEvent))