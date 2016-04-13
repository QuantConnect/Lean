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

from datetime import datetime

from clr import AddReference, ImportExtensions
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Indicators import *
from QuantConnect.Orders import *
from QuantConnect.Securities import *
ImportExtensions(OrderExtensions)


class HistoryAndWarmupRegressionAlgorithm(QCAlgorithm):
    '''This algorithm demonstrates the various ways you can call the History function,
    what it returns, and what you can do with the returned values.'''
    def __init__(self):
        self.__SPY   = Symbol.Create("SPY", SecurityType.Equity, "USA")
        self.__GOOG  = Symbol.Create("GOOG", SecurityType.Equity, "USA")
        self.__IBM   = Symbol.Create("IBM", SecurityType.Equity, "USA")
        self.__BAC   = Symbol.Create("BAC", SecurityType.Equity, "USA")
        self.__GOOGL = Symbol.Create("GOOGL", SecurityType.Equity, "USA")
        self.__sd = { }

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013, 10, 8)   #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(1000000)            #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity(self.__SPY.Value, Resolution.Minute)
        self.AddEquity(self.__IBM.Value, Resolution.Minute)
        self.AddEquity(self.__BAC.Value, Resolution.Minute)
        self.AddEquity(self.__GOOG.Value, Resolution.Daily)
        self.AddEquity(self.__GOOGL.Value, Resolution.Daily)

        for security in self.Securities:
            self.__sd[security.Key] = self.SymbolData(security.Key, self)

        # we want to warm up our algorithm
        self.SetWarmup(self.SymbolData.RequiredBarsWarmup)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        # we are only using warmup for indicator spooling, so wait for us to be warm then continue
        if self.IsWarmingUp: return

        for sd in self.__sd.values():
            lastPriceTime = datetime(sd.Close.Current.Time)
            if self.RoundDown(lastPriceTime, sd.Security.SubscriptionDataConfig.Increment):
                sd.Update()


    def OnOrderEvent(self, fill):
        sd = self.__sd.get(fill.Symbol, None)
        if sd is not None:
            sd.OnOrderEvent(fill)


    def RoundDown(self, time, increment):
        if increment.Days != 0:
            return time.hour == 0 and time.minute == 0 and time.second == 0
        else:
            return time.second == 0


    class SymbolData:
        RequiredBarsWarmup = 40
        PercentTolerance = 0.001
        PercentGlobalStopLoss = 0.01
        LotSize = 10
        
        def __init__(self, symbol, algorithm):
            self.Symbol = symbol
            self.__algorithm = algorithm   # if we're receiving daily 

            self.__currentStopLoss = None

            self.Security = algorithm.Securities[symbol]
            self.Close = algorithm.Identity(symbol)
            self.ADX = algorithm.ADX(symbol, 14)
            self.EMA = algorithm.EMA(symbol, 14)
            self.MACD = algorithm.MACD(symbol, 12, 26, 9)

            self.IsReady = self.Close.IsReady and self.ADX.IsReady and self.EMA.IsReady and self.MACD.IsReady
            self.IsUptrend = False
            self.IsDowntrend = False


        def Update(self):
            self.IsReady = self.Close.IsReady and self.ADX.IsReady and self.EMA.IsReady and self.MACD.IsReady

            tolerance = 1 + self.PercentTolerance
            self.IsUptrend = self.MACD.Signal.Current.Value > self.MACD.Current.Value * tolerance and\
                self.EMA.Current.Value > self.Close.Current.Value * tolerance

            tolerance = 1 - self.PercentTolerance
            self.IsDowntrend = self.MACD.Signal.Current.Value < self.MACD.Current.Value * tolerance and\
                self.EMA.Current.Value < self.Close.Current.Value * tolerance

            self.TryEnter()
            self.TryExit()


        def TryEnter(self):
            # can't enter if we're already in
            if self.Security.Invested: return False

            qty = 0
            limit = 0.0

            if self.IsUptrend:
                # 100 order lots
                qty = self.LotSize
                limit = self.Security.Low
            elif self.IsDowntrend:
                qty = -self.LotSize
                limit = self.Security.High
            
            if qty != 0:
                ticket = self.__algorithm.LimitOrder(self.Symbol, qty, limit, "TryEnter at: {0}".format(limit))


        def TryExit(self):
            # can't exit if we haven't entered
            if not self.Security.Invested: return
            
            limit = 0
            qty = self.Security.Holdings.Quantity
            exitTolerance = 1 + 2 * self.PercentTolerance
            if self.Security.Holdings.IsLong and self.Close.Current.Value * exitTolerance < self.EMA.Current.Value:
                limit = self.Security.High
            elif self.Security.Holdings.IsShort and self.Close.Current.Value > self.EMA.Current.Value * exitTolerance:
                limit = self.Security.Low

            if limit != 0:
                ticket = self.__algorithm.LimitOrder(self.Symbol, -qty, limit, "TryExit at: {0}".format(limit))


        def OnOrderEvent(self, fill):
            if fill.Status != OrderStatus.Filled: return

            qty = self.Security.Holdings.Quantity

            # if we just finished entering, place a stop loss as well
            if self.Security.Invested:
                stop = fill.FillPrice*(1 - self.PercentGlobalStopLoss) if self.Security.Holdings.IsLong \
                    else fill.FillPrice*(1 + self.PercentGlobalStopLoss)

                self.__currentStopLoss = self.__algorithm.StopMarketOrder(self.Symbol, -qty, stop, "StopLoss at: {0}".format(stop))
                
            # check for an exit, cancel the stop loss
            elif (self.__currentStopLoss != None and self.__currentStopLoss.Status.IsOpen()):
                # cancel our current stop loss
                self.__currentStopLoss.Cancel("Exited position")
                self.__currentStopLoss = None