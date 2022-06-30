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
### The demonstration algorithm shows some of the most common order methods when working with CFD assets.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />

class BasicTemplateCfdAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetAccountCurrency('EUR')

        self.SetStartDate(2019, 2, 20)
        self.SetEndDate(2019, 2, 21)
        self.SetCash('EUR', 100000)

        self.symbol = self.AddCfd('DE30EUR').Symbol

        # Historical Data
        history = self.History(self.symbol, 60, Resolution.Daily)
        self.Log(f"Received {len(history)} bars from CFD historical data call.")

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        Arguments:
            slice: Slice object keyed by symbol containing the stock data
        '''
        # Access Data
        if data.QuoteBars.ContainsKey(self.symbol):
            quoteBar = data.QuoteBars[self.symbol]
            self.Log(f"{quoteBar.EndTime} :: {quoteBar.Close}")

        if not self.Portfolio.Invested:
            self.SetHoldings(self.symbol, 1)

    def OnOrderEvent(self, orderEvent):
        self.Debug("{} {}".format(self.Time, orderEvent.ToString()))