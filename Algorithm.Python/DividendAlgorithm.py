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

### <summary>
### Demonstration of payments for cash dividends in backtesting. When data normalization mode is set
### to "Raw" the dividends are paid as cash directly into your portfolio.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="data event handlers" />
### <meta name="tag" content="dividend event" />
class DividendAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(1998,1,1)  #Set Start Date
        self.SetEndDate(2006,1,21)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        equity = self.AddEquity("MSFT", Resolution.Daily)
        equity.SetDataNormalizationMode(DataNormalizationMode.Raw)

        # this will use the Tradier Brokerage open order split behavior
        # forward split will modify open order to maintain order value
        # reverse split open orders will be cancelled
        self.SetBrokerageModel(BrokerageName.TradierBrokerage)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        bar = data["MSFT"]
        if self.Transactions.OrdersCount == 0:
            self.SetHoldings("MSFT", .5)
            # place some orders that won't fill, when the split comes in they'll get modified to reflect the split
            quantity = self.CalculateOrderQuantity("MSFT", .25)
            self.Debug(f"Purchased Stock: {bar.Price}")
            self.StopMarketOrder("MSFT", -quantity, bar.Low/2)
            self.LimitOrder("MSFT", -quantity, bar.High*2)

        if data.Dividends.ContainsKey("MSFT"):
            dividend = data.Dividends["MSFT"]
            self.Log(f"{self.Time} >> DIVIDEND >> {dividend.Symbol} - {dividend.Distribution} - {self.Portfolio.Cash} - {self.Portfolio['MSFT'].Price}")

        if data.Splits.ContainsKey("MSFT"):
            split = data.Splits["MSFT"]
            self.Log(f"{self.Time} >> SPLIT >> {split.Symbol} - {split.SplitFactor} - {self.Portfolio.Cash} - {self.Portfolio['MSFT'].Price}")

    def OnOrderEvent(self, orderEvent):
        # orders get adjusted based on split events to maintain order value
        order = self.Transactions.GetOrderById(orderEvent.OrderId)
        self.Log(f"{self.Time} >> ORDER >> {order}")