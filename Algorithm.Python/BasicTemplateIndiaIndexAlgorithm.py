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

from AlgorithmImports import *

### <summary>
### Basic Template India Index Algorithm uses framework components to define the algorithm.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class BasicTemplateIndiaIndexAlgorithm(QCAlgorithm):
    '''Basic template framework algorithm uses framework components to define the algorithm.'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetAccountCurrency("INR") #Set Account Currency
        self.SetStartDate(2019, 1, 1)  #Set Start Date
        self.SetEndDate(2019, 1, 5)    #Set End Date
        self.SetCash(1000000)          #Set Strategy Cash

        # Use indicator for signal; but it cannot be traded
        self.Nifty = self.AddIndex("NIFTY50", Resolution.Minute, Market.India).Symbol
        # Trade Index based ETF
        self.NiftyETF = self.AddEquity("JUNIORBEES", Resolution.Minute, Market.India).Symbol
   
        # Set Order Properties as per the requirements for order placement
        self.DefaultOrderProperties = IndiaOrderProperties(Exchange.NSE)

        # Define indicator
        self._emaSlow = self.EMA(self.Nifty, 80)
        self._emaFast = self.EMA(self.Nifty, 200)

        self.Debug("numpy test >>> print numpy.pi: " + str(np.pi))


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''

        if not data.Bars.ContainsKey(self.Nifty) or not data.Bars.ContainsKey(self.NiftyETF):
            return

        if not self._emaSlow.IsReady:
            return

        if self._emaFast > self._emaSlow:
            if not self.Portfolio.Invested:
                self.marketTicket = self.MarketOrder(self.NiftyETF, 1)
        else:
            self.Liquidate()


    def OnEndOfAlgorithm(self):
        if self.Portfolio[self.Nifty].TotalSaleVolume > 0:
            raise Exception("Index is not tradable.")

