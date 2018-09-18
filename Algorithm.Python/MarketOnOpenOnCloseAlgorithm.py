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

from System import *
from QuantConnect import *
from QuantConnect.Securities import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from QCAlgorithm import QCAlgorithm
from datetime import datetime

### <summary>
### Demonstration of the Market On Close order for US Equities.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />
class MarketOnOpenOnCloseAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.equity = self.AddEquity("SPY", Resolution.Second, fillDataForward = True, extendedMarketHours = True)
        self.__submittedMarketOnCloseToday = False
        self.__last = datetime.min


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if self.Time.date() != self.__last.date():   # each morning submit a market on open order
            self.__submittedMarketOnCloseToday = False
            self.MarketOnOpenOrder("SPY", 100)
            self.__last = self.Time

        if not self.__submittedMarketOnCloseToday and self.equity.Exchange.ExchangeOpen:   # once the exchange opens submit a market on close order
            self.__submittedMarketOnCloseToday = True
            self.MarketOnCloseOrder("SPY", -100)


    def OnOrderEvent(self, fill):
        order = self.Transactions.GetOrderById(fill.OrderId)
        self.Log("{0} - {1}:: {2}".format(self.Time, order.Type, fill))