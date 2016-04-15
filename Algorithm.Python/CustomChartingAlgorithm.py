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

from datetime import datetime, timedelta

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Market import *


class CustomChartingAlgorithm(QCAlgorithm):
    '''4.0 DEMONSTRATION OF CUSTOM CHARTING FLEXIBILITY:
    
    The entire charting system of quantconnect is adaptable. You can adjust it to draw whatever you'd like.
    
    Charts can be stacked, or overlayed on each other.
    Series can be candles, lines or scatter plots.
    
    Even the default behaviours of QuantConnect can be overridden'''
    def __init__(self):
        self.__fastMA = None
        self.__slowMA = None
        self.__lastPrice = None
        self.__resample = None
        self.__resamplePeriod = None


    def Initialize(self):
        '''Called at the start of your algorithm to setup your requirements'''

        self.SetStartDate(2010, 3, 3)  #Set Start Date
        self.SetEndDate(2014, 3, 3)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, "SPY", Resolution.Minute)

        #Chart - Master Container for the Chart:
        stockPlot = Chart("Trade Plot")
        #On the Trade Plotter Chart we want 3 series: trades and price:
        buyOrders = Series("Buy", SeriesType.Scatter, 0)
        sellOrders = Series("Sell", SeriesType.Scatter, 0)
        assetPrice = Series("Price", SeriesType.Line, 0)
        stockPlot.AddSeries(buyOrders)
        stockPlot.AddSeries(sellOrders)
        stockPlot.AddSeries(assetPrice)
        self.AddChart(stockPlot)

        avgCross = Chart("Strategy Equity")
        fastMA = Series("FastMA", SeriesType.Line, 1)
        slowMA = Series("SlowMA", SeriesType.Line, 1)
        avgCross.AddSeries(fastMA)
        avgCross.AddSeries(slowMA)
        self.AddChart(avgCross)

        self.__resample = datetime(self.StartDate)
        self.__resamplePeriod = timedelta(minutes = (self.EndDate - self.StartDate).TotalMinutes / 2000)


    def OnEndOfDay(self):
        '''OnEndOfDay Event Handler - At the end of each trading day we fire this code.
        To avoid flooding, we recommend running your plotting at the end of each day.'''
        #Log the end of day prices:
        self.Plot("Trade Plot", "Price", self.__lastPrice)


    def OnData(self, data):
        '''On receiving new tradebar data it will be passed into this function. The general pattern is:
        "public void OnData( CustomType name ) {...}"

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not data.ContainsKey("SPY") or data["SPY"] is None: return
        
        pyTime = datetime(self.Time)
        self.__lastPrice = data["SPY"].Close

        if self.__fastMA == None: self.__fastMA = self.__lastPrice
        if self.__slowMA == None: self.__slowMA = self.__lastPrice

        self.__fastMA = (0.01 * self.__lastPrice) + (0.99 * self.__fastMA)
        self.__slowMA = (0.001 * self.__lastPrice) + (0.999 * self.__slowMA)

        if pyTime > self.__resample:
            self.__resample = pyTime + self.__resamplePeriod
            self.Plot("Strategy Equity", "FastMA", self.__fastMA)
            self.Plot("Strategy Equity", "SlowMA", self.__slowMA)
            
        #On the 5th days when not invested buy:
        if pyTime.day % 13 == 0 and not self.Portfolio.Invested:
            self.Order("SPY", int(self.Portfolio.Cash / data["SPY"].Close))
            self.Plot("Trade Plot", "Buy", self.__lastPrice)
            
        elif pyTime.day % 21 == 0 and self.Portfolio.Invested:
            self.Plot("Trade Plot", "Sell", self.__lastPrice)
            self.Liquidate()