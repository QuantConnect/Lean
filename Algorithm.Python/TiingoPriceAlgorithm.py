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
from QuantConnect.Data.Custom.Tiingo import *

### <summary>
### This example algorithm shows how to import and use Tiingo daily prices data.
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="tiingo" />
class TiingoPriceAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        self.SetStartDate(2017, 1, 1)
        self.SetEndDate(2017, 12, 31)
        self.SetCash(100000)

        # Set your Tiingo API Token here
        Tiingo.SetAuthCode("my-tiingo-api-token")

        self.ticker = "AAPL"
        self.symbol = self.AddData(TiingoPrice, self.ticker, Resolution.Daily).Symbol

        self.emaFast = self.EMA(self.symbol, 5)
        self.emaSlow = self.EMA(self.symbol, 10)


    def OnData(self, slice):
        # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        if not slice.ContainsKey(self.ticker): return

        # Extract Tiingo data from the slice
        row = slice[self.ticker]

        self.Log(f"{self.Time} - {row.Symbol.Value} - {row.Close} {row.Value} {row.Price} - EmaFast:{self.emaFast} - EmaSlow:{self.emaSlow}")

        # Simple EMA cross
        if not self.Portfolio.Invested and self.emaFast > self.emaSlow:
            self.SetHoldings(self.symbol, 1)

        elif self.Portfolio.Invested and self.emaFast < self.emaSlow:
            self.Liquidate(self.symbol)
