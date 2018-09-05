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
from QCAlgorithm import QCAlgorithm

import numpy as np
import decimal as d
from datetime import timedelta, datetime

### <summary>
### Algorithm demonstrating custom charting support in QuantConnect.
### The entire charting system of quantconnect is adaptable. You can adjust it to draw whatever you'd like.
### Charts can be stacked, or overlayed on each other. Series can be candles, lines or scatter plots.
### Even the default behaviours of QuantConnect can be overridden.
### </summary>
### <meta name="tag" content="charting" />
### <meta name="tag" content="adding charts" />
### <meta name="tag" content="series types" />
### <meta name="tag" content="plotting indicators" />
class CustomChartingAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2016,1,1)
        self.SetEndDate(2017,1,1)
        self.SetCash(100000)
        self.AddEquity("SPY", Resolution.Daily)

        # In your initialize method:
        # Chart - Master Container for the Chart:
        stockPlot = Chart("Trade Plot")
        # On the Trade Plotter Chart we want 3 series: trades and price:
        stockPlot.AddSeries(Series("Buy", SeriesType.Scatter, 0))
        stockPlot.AddSeries(Series("Sell", SeriesType.Scatter, 0))
        stockPlot.AddSeries(Series("Price", SeriesType.Line, 0))
        self.AddChart(stockPlot)

        avgCross = Chart("Average Cross")
        avgCross.AddSeries(Series("FastMA", SeriesType.Line, 1))
        avgCross.AddSeries(Series("SlowMA", SeriesType.Line, 1))
        self.AddChart(avgCross)

        self.fastMA = 0
        self.slowMA = 0
        self.lastPrice = 0
        self.resample = datetime.min
        self.resamplePeriod = (self.EndDate - self.StartDate) / 2000

    def OnData(self, slice):
        if slice["SPY"] is None: return

        self.lastPrice = slice["SPY"].Close
        if self.fastMA == 0: self.fastMA = self.lastPrice
        if self.slowMA == 0: self.slowMA = self.lastPrice
        self.fastMA = (d.Decimal(0.01) * self.lastPrice) + (d.Decimal(0.99) * self.fastMA)
        self.slowMA = (d.Decimal(0.001) * self.lastPrice) + (d.Decimal(0.999) * self.slowMA)

        if self.Time > self.resample:
            self.resample = self.Time  + self.resamplePeriod
            self.Plot("Average Cross", "FastMA", self.fastMA)
            self.Plot("Average Cross", "SlowMA", self.slowMA)

        # On the 5th days when not invested buy:
        if not self.Portfolio.Invested and self.Time.day % 13 == 0:
        	self.Order("SPY", (int)(self.Portfolio.MarginRemaining / self.lastPrice))
        	self.Plot("Trade Plot", "Buy", self.lastPrice)
        elif self.Time.day % 21 == 0 and self.Portfolio.Invested:
            self.Plot("Trade Plot", "Sell", self.lastPrice)
            self.Liquidate()

    def OnEndOfDay(self):
       #Log the end of day prices:
       self.Plot("Trade Plot", "Price", self.lastPrice)