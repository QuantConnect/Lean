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
from QuantConnect.Data.Custom.BrainData import *

### <summary>
### This example algorithm shows how to import and use braindata aggregated sentiment data
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="braindata" />
### <meta name="tag" content="sentiment" />
class BasicTemplateAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the algorithm with our custom data'''

        self.SetStartDate(2018, 1, 1)
        self.SetEndDate(2019, 1, 1)
        self.SetCash(100000)

        self.ticker = "AAPL"
        self.symbol = self.AddEquity(self.ticker, Resolution.Daily).Symbol
        self.AddData(BrainDataSentimentWeekly, self.ticker, Resolution.Daily)
        self.AddData(BrainDataSentimentMonthly, self.ticker, Resolution.Daily)

    def OnData(self, slice):
        '''
        Loads each new data point into the algorithm. On sentiment data, we place orders depending on the sentiment
        '''
        if not slice.ContainsKey(self.ticker):
            return

        data = slice[self.ticker]

        if isinstance(data, BrainDataSentimentWeekly):
            if not self.Portfolio.Invested and len(self.Transactions.GetOpenOrders()) == 0 and data.SentimentScore > 0.07:
                self.Log(f"BrainDataSentimentWeekly: Order placed for {self.ticker}")
                self.SetHoldings(self.symbol, 0.5)

            elif self.Portfolio.Invested and data.SentimentScore < -0.05 and self.Portfolio.ContainsKey(self.symbol):
                self.Log(f"BrainDataSentimentWeekly: Liquidating {self.ticker}")
                self.Liquidate(self.symbol)

        elif isinstance(data, BrainDataSentimentMonthly):
            if not self.Portfolio.Invested and len(self.Transactions.GetOpenOrders()) == 0 and data.SentimentScore > 0.07:
                self.Log(f"BrainDataSentimentMonthly: Order placed for {self.ticker}")
                self.SetHoldings(self.symbol, 0.5)

            elif self.Portfolio.Invested and data.SentimentScore < -0.05 and self.Portfolio.ContainsKey(self.symbol):
                self.Log(f"BrainDataSentimentMonthly: Liquidating {self.ticker}")
                self.Liquidate(self.symbol)


