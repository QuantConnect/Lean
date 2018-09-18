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

from System import *
from QuantConnect import *
from QuantConnect.Data.Consolidators import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import OrderStatus
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Indicators import *
import numpy as np
from datetime import timedelta, datetime

### <summary>
### Example structure for structuring an algorithm with indicator and consolidator data for many tickers.
### </summary>
### <meta name="tag" content="consolidating data" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="strategy example" />
class MultipleSymbolConsolidationAlgorithm(QCAlgorithm):
    
    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    def Initialize(self):
        
        # This is the period of bars we'll be creating
        BarPeriod = TimeSpan.FromMinutes(10)
        # This is the period of our sma indicators
        SimpleMovingAveragePeriod = 10
        # This is the number of consolidated bars we'll hold in symbol data for reference
        RollingWindowSize = 10
        # Holds all of our data keyed by each symbol
        self.Data = {}
        # Contains all of our equity symbols
        EquitySymbols = ["AAPL","SPY","IBM"]
        # Contains all of our forex symbols
        ForexSymbols =["EURUSD", "USDJPY", "EURGBP", "EURCHF", "USDCAD", "USDCHF", "AUDUSD","NZDUSD"]
        
        self.SetStartDate(2014, 12, 1)
        self.SetEndDate(2015, 2, 1)
        
        # initialize our equity data
        for symbol in EquitySymbols:
            equity = self.AddEquity(symbol)
            self.Data[symbol] = SymbolData(equity.Symbol, BarPeriod, RollingWindowSize)
        
        # initialize our forex data 
        for symbol in ForexSymbols:
            forex = self.AddForex(symbol)
            self.Data[symbol] = SymbolData(forex.Symbol, BarPeriod, RollingWindowSize)

        # loop through all our symbols and request data subscriptions and initialize indicator
        for symbol, symbolData in self.Data.items():
            # define the indicator
            symbolData.SMA = SimpleMovingAverage(self.CreateIndicatorName(symbol, "SMA" + str(SimpleMovingAveragePeriod), Resolution.Minute), SimpleMovingAveragePeriod)
            # define a consolidator to consolidate data for this symbol on the requested period
            consolidator = TradeBarConsolidator(BarPeriod) if symbolData.Symbol.SecurityType == SecurityType.Equity else QuoteBarConsolidator(BarPeriod)
            # write up our consolidator to update the indicator
            consolidator.DataConsolidated += self.OnDataConsolidated
            # we need to add this consolidator so it gets auto updates
            self.SubscriptionManager.AddConsolidator(symbolData.Symbol, consolidator)

    def OnDataConsolidated(self, sender, bar):
        
        self.Data[bar.Symbol.Value].SMA.Update(bar.Time, bar.Close)
        self.Data[bar.Symbol.Value].Bars.Add(bar)

    # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    # Argument "data": Slice object, dictionary object with your stock data 
    def OnData(self,data):
        
        # loop through each symbol in our structure
        for symbol in self.Data.keys():
            symbolData = self.Data[symbol]
            # this check proves that this symbol was JUST updated prior to this OnData function being called
            if symbolData.IsReady() and symbolData.WasJustUpdated(self.Time):
                if not self.Portfolio[symbol].Invested:
                    self.MarketOrder(symbol, 1)

    # End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
    # Method is called 10 minutes before closing to allow user to close out position.
    def OnEndOfDay(self):
        
        i = 0
        for symbol in sorted(self.Data.keys()):
            symbolData = self.Data[symbol]
            # we have too many symbols to plot them all, so plot every other
            i += 1
            if symbolData.IsReady() and i%2 == 0:
                self.Plot(symbol, symbol, symbolData.SMA.Current.Value)
    
       
class SymbolData(object):
    
    def __init__(self, symbol, barPeriod, windowSize):
        self.Symbol = symbol
        # The period used when population the Bars rolling window
        self.BarPeriod = barPeriod
        # A rolling window of data, data needs to be pumped into Bars by using Bars.Update( tradeBar ) and can be accessed like:
        # mySymbolData.Bars[0] - most first recent piece of data
        # mySymbolData.Bars[5] - the sixth most recent piece of data (zero based indexing)
        self.Bars = RollingWindow[IBaseDataBar](windowSize)
        # The simple moving average indicator for our symbol
        self.SMA = None
  
    # Returns true if all the data in this instance is ready (indicators, rolling windows, ect...)
    def IsReady(self):
        return self.Bars.IsReady and self.SMA.IsReady

    # Returns true if the most recent trade bar time matches the current time minus the bar's period, this
    # indicates that update was just called on this instance
    def WasJustUpdated(self, current):
        return self.Bars.Count > 0 and self.Bars[0].Time == current - self.BarPeriod                                               