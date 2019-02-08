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
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import QCAlgorithmFrameworkBridge
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Indicators import *
from QuantConnect.Orders.Fees import ConstantFeeModel

import numpy as np
import pandas as pd
from datetime import timedelta, datetime
from decimal import Decimal

class ShareClassMeanReversionAlphaModel(QCAlgorithmFrameworkBridge):

    def Initialize(self):
        
        ## Set testing timeframe and starting cash
        self.SetStartDate(2019,1,1)
        self.SetCash(100000)
        
        ## We choose a pair of stock tickers that represent different
        ## share classes of the same company -- the idea being that their 
        ## prices will move almost identically but likely with slight deviations
        symbols = ['GOOG','GOOGL'] 

        self.symbols = symbols
        for symbol in symbols:
            self.AddEquity(symbol, Resolution.Minute)
            self.Securities[symbol].FeeModel = ConstantFeeModel(0) ## Set fees to $0 for High Freq. Trading
        
        ## Register a 20-bar SMA indicator for tracking the moving average of the
        ## long/short position and a RollingWindow to keep track of our
        ## most recent position values
        self.sma = SimpleMovingAverage(20)
        self.position = RollingWindow[Decimal](2)
        
        ## Warm up our 20-bar indicator
        self.SetWarmup(20)
        
        ## Initialize a list to keep track of our position value, a period counter
        ## to assist in tracking our position relative to the SMA,
        ## and alpha + beta to represent position sizes in our assets
        self.alpha = None
        self.beta = None
        self.Invested = False

    def OnData(self, data):
        
        ## If one or more of the symbols doesn't have a TradeBar for a given slice, then
        ## skip this slice and do nothing until both symbols have data
        
        for symbol in self.symbols:
            if not data.Bars.ContainsKey(symbol): return
        
        ## We want to make and initial calculation of alpha and beta such that our position
        ## in each asset is 50% of our total available cash.
        if (self.alpha is None) and (self.beta is None):
            self.alpha = self.CalculateOrderQuantity(self.symbols[0], 0.5)
            self.beta = self.CalculateOrderQuantity(self.symbols[1], 0.5)

        ## We want to keep updating the SMA indicator and our own position
        ## value list while the algorithm is warming-up
        if not self.sma.IsReady:
            position_value = (self.alpha * data[self.symbols[0]].Close) - (self.beta * data[self.symbols[1]].Close)
            self.sma.Update(data[self.symbols[0]].EndTime, position_value)
            self.position.Add(position_value)
            return

        ## Calculate our position value here, which we then use to update the SMA
        position_value = (self.alpha * data[self.symbols[0]].Close) - (self.beta * data[self.symbols[1]].Close)
        self.sma.Update(data[self.symbols[0]].EndTime, position_value)
        self.position.Add(position_value)
        
        ## Check to see if the position has crossed over the SMA before we liquidate
        ## our positions. This prevents immediate liquidation of a position after entering it
        
        if not self.Invested:
            ## Position value greater than SMA indicates that we should 'sell our portfolio' since it will revert back to the mean value
            ## This means go long 'GOOGL' and go short 'GOOG'
            if position_value >= self.sma.Current.Value:
                insight1 = Insight.Price(self.symbols[1], timedelta(minutes=5), InsightDirection.Up)
                insight2 = Insight.Price(self.symbols[0], timedelta(minutes=5), InsightDirection.Down)
                self.EmitInsights( Insight.Group ( [insight1, insight2] ) )
                self.Log('Insight Emitted')
                
                self.SetHoldings(self.symbols[1], 0.5)
                self.SetHoldings(self.symbols[0], -0.5)
                self.Invested = True
                
            ## Position value greater than SMA indicates that we should 'buy our portfolio' since it will revert back to the mean value
            ## This means go short 'GOOGL' and go long 'GOOG'
            if position_value < self.sma.Current.Value:
                insight1 = Insight.Price(self.symbols[1], timedelta(minutes=5), InsightDirection.Down)
                insight2 = Insight.Price(self.symbols[0], timedelta(minutes=5), InsightDirection.Up)
                self.EmitInsights( Insight.Group ( [insight1, insight2] ) )
                self.Log('Insight Emitted')
                
                self.SetHoldings(self.symbols[1], -0.5)
                self.SetHoldings(self.symbols[0], 0.5)
                self.Invested = True
        
        ## If we are invested and the long/short position has crossed the SMA line, then we close our positions
        if self.Invested and self.crossed_sma():
            self.Liquidate()
            self.Invested = False
            
    ## Helper function to check if the long/short position has crossed the SMA        
    def crossed_sma(self):
        if (self.position[0] >= self.sma.Current.Value) and (self.position[1] < self.sma.Current.Value):
            return True
        elif (self.position[0] < self.sma.Current.Value) and (self.position[1] >= self.sma.Current.Value):
            return True
        else:
            return False