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
from QuantConnect.Brokerages import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *


class DividendAlgorithm(QCAlgorithm):
    '''Showcases the dividend and split event of QCAlgorithm
    The data for this algorithm isn't in the github repo, so this will need to be run on the QC site'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(1998,01,01)  #Set Start Date
        self.SetEndDate(2006,01,21)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, "MSFT", Resolution.Daily)
        self.Securities["MSFT"].SetDataNormalizationMode(DataNormalizationMode.Raw)

        # this will use the Tradier Brokerage open order split behavior
        # forward split will modify open order to maintain order value
        # reverse split open orders will be cancelled
        self.SetBrokerageModel(BrokerageName.TradierBrokerage)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self.Transactions.OrdersCount == 0:
            self.SetHoldings("MSFT", .5)
            # place some orders that won't fill, when the split comes in they'll get modified to reflect the split
            self.Debug("Purchased Stock: {0}".format(self.Securities["MSFT"].Price))
            self.StopMarketOrder("MSFT", -self.CalculateOrderQuantity("MSFT", .25), data["MSFT"].Low/2)
            self.LimitOrder("MSFT", -self.CalculateOrderQuantity("MSFT", .25), data["MSFT"].High*2)

        for kvp in data.Dividends:   # update this to Dividends dictionary
            symbol = kvp.Key
            value = kvp.Value.Distribution
            self.Log("{0} >> DIVIDEND >> {1} - {2} - {3} - {4}".format(self.Time, symbol, value, self.Portfolio.Cash, self.Portfolio["MSFT"].Price))

        for kvp in data.Splits:      # update this to Splits dictionary
            symbol = kvp.Key
            value = kvp.Value.SplitFactor
            self.Log("{0} >> SPLIT >> {1} - {2} - {3} - {4}".format(self.Time, symbol, value, self.Portfolio.Cash, self.Portfolio["MSFT"].Quantity))


    def OnOrderEvent(self, orderEvent):
        # orders get adjusted based on split events to maintain order value
        order = self.Transactions.GetOrderById(orderEvent.OrderId)
        self.Log("{0} >> ORDER >> {1}".format(self.Time, order))