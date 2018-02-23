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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *

### <summary>
### Uses daily data and a simple moving average cross to place trades and an ema for stop placement
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="trading and orders" />
class DailyAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,1,1)    #Set Start Date
        self.SetEndDate(2014,1,1)      #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY", Resolution.Daily)
        self.AddEquity("IBM", Resolution.Hour).SetLeverage(1.0)
        self.macd = self.MACD("SPY", 12, 26, 9, MovingAverageType.Wilders, Resolution.Daily, Field.Close)
        self.ema = self.EMA("IBM", 15 * 6, Resolution.Hour, Field.SevenBar)
        self.lastAction = None


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.macd.IsReady: return
        if not data.ContainsKey("IBM"): return
        if data["IBM"] is None:
            self.Log("Price Missing Time: %s"%str(self.Time))
            return
        if self.lastAction is not None and self.lastAction.date() == self.Time.date(): return

        self.lastAction = self.Time
        quantity = self.Portfolio["SPY"].Quantity

        if quantity <= 0 and self.macd.Current.Value > self.macd.Signal.Current.Value and data["IBM"].Price > self.ema.Current.Value:
            self.SetHoldings("IBM", 0.25)
        elif quantity >= 0 and self.macd.Current.Value < self.macd.Signal.Current.Value and data["IBM"].Price < self.ema.Current.Value:
            self.SetHoldings("IBM", -0.25)