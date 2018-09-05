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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QCAlgorithm import QCAlgorithm

import numpy as np
import decimal as d
from datetime import timedelta, datetime

### <summary>
### Regression Channel algorithm simply initializes the date range and cash
### </summary>
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
### <meta name="tag" content="placing orders" />
### <meta name="tag" content="plotting indicators" />
class RegressionChannelAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetCash(100000)
        self.SetStartDate(2009,1,1)
        self.SetEndDate(2015,1,1)

        equity = self.AddEquity("SPY", Resolution.Minute)
        self._spy = equity.Symbol
        self._holdings = equity.Holdings
        self._rc = self.RC(self._spy, 30, 2, Resolution.Daily)

        stockPlot = Chart("Trade Plot")
        stockPlot.AddSeries(Series("Buy", SeriesType.Scatter, 0))
        stockPlot.AddSeries(Series("Sell", SeriesType.Scatter, 0))
        stockPlot.AddSeries(Series("UpperChannel", SeriesType.Line, 0))
        stockPlot.AddSeries(Series("LowerChannel", SeriesType.Line, 0))
        stockPlot.AddSeries(Series("Regression", SeriesType.Line, 0))
        self.AddChart(stockPlot)

    def OnData(self, data):
        if (not self._rc.IsReady) or (not data.ContainsKey(self._spy)): return
        if data[self._spy] is None: return
        value = data[self._spy].Value
        if self._holdings.Quantity <= 0 and value < self._rc.LowerChannel.Current.Value:
            self.SetHoldings(self._spy, 1)
            self.Plot("Trade Plot", "Buy", value)
        if self._holdings.Quantity >= 0 and value > self._rc.UpperChannel.Current.Value:
            self.SetHoldings(self._spy, -1)
            self.Plot("Trade Plot", "Sell", value)

    def OnEndOfDay(self):
        self.Plot("Trade Plot", "UpperChannel", self._rc.UpperChannel.Current.Value)
        self.Plot("Trade Plot", "LowerChannel", self._rc.LowerChannel.Current.Value)
        self.Plot("Trade Plot", "Regression", self._rc.LinearRegression.Current.Value)