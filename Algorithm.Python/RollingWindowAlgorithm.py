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
from QuantConnect.Indicators import *
from QuantConnect.Data.Market import TradeBar

### <summary>
### Using rolling windows for efficient storage of historical data; which automatically clears after a period of time.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="history and warm up" />
### <meta name="tag" content="history" />
### <meta name="tag" content="warm up" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="rolling windows" />
class RollingWindowAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10,1)  #Set Start Date
        self.SetEndDate(2013,11,1)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY", Resolution.Daily)

        # Creates a Rolling Window indicator to keep the 2 TradeBar
        self.window = RollingWindow[TradeBar](2)    # For other security types, use QuoteBar

        # Creates an indicator and adds to a rolling window when it is updated
        self.sma = self.SMA("SPY", 5)
        self.sma.Updated += self.SmaUpdated
        self.smaWin = RollingWindow[IndicatorDataPoint](5)


    def SmaUpdated(self, sender, updated):
        '''Adds updated values to rolling window'''
        self.smaWin.Add(updated)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''

        # Add SPY TradeBar in rollling window
        self.window.Add(data["SPY"])

        # Wait for windows to be ready.
        if not (self.window.IsReady and self.smaWin.IsReady): return

        currBar = self.window[0]                     # Current bar had index zero.
        pastBar = self.window[1]                     # Past bar has index one.
        self.Log("Price: {0} -> {1} ... {2} -> {3}".format(pastBar.Time, pastBar.Close, currBar.Time, currBar.Close))

        currSma = self.smaWin[0]                     # Current SMA had index zero.
        pastSma = self.smaWin[self.smaWin.Count-1]   # Oldest SMA has index of window count minus 1.
        self.Log("SMA:   {0} -> {1} ... {2} -> {3}".format(pastSma.Time, pastSma.Value, currSma.Time, currSma.Value))

        if not self.Portfolio.Invested and currSma.Value > pastSma.Value:
            self.SetHoldings("SPY", 1)