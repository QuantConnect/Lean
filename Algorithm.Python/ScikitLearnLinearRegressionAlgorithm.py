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

import clr
clr.AddReference("System")
clr.AddReference("QuantConnect.Algorithm")
clr.AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *

import numpy as np
from sklearn.linear_model import LinearRegression

class ScikitLearnLinearRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)  # Set Start Date
        self.SetEndDate(2013, 10, 8) # Set End Date
        
        self.lookback = 30
        
        self.SetCash(100000)  # Set Strategy Cash
        spy = self.AddEquity("SPY", Resolution.Minute)
        
        self.symbols = [ spy.Symbol ]
        
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.AfterMarketOpen("SPY", 28), Action(self.Regression))
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.AfterMarketOpen("SPY", 30), Action(self.Trade))
        


    def OnData(self, data):
        pass
    
    def Regression(self):
        history = self.History(self.symbols, self.lookback, Resolution.Daily)

        self.prices = {}
        self.slopes = {}
        
        for symbol in self.symbols:
            if not history.empty:
                self.prices[symbol.Value] = list(history.loc[symbol.Value]['open'])

        A = range(self.lookback + 1)
        
        for symbol in self.symbols:
            if symbol.Value in self.prices:
                Y = self.prices[symbol.Value]
                X = np.column_stack([np.ones(len(A)), A])
                
                length = min(len(X), len(Y))
                X = X[-length:]
                Y = Y[-length:]
                A = A[-length:]
                
                reg = LinearRegression().fit(X, Y)
                
                # run linear regression y = ax + b
                b = reg.intercept_
                a = reg.coef_[1]
                
                self.slopes[symbol] = a/b
                
    
    def Trade(self):
        if not self.prices:
            return 
        
        thod_buy = 0.001
        thod_liquidate = -0.001
        
        # liquidate
        for i in self.Portfolio.Values:
            slope = self.slopes[i.Symbol] 
            if i.Invested and slope < thod_liquidate:
                self.Liquidate(i.Symbol)
        
        # buy
        for symbol in self.symbols:
            if self.slopes[symbol] > thod_buy:
                self.SetHoldings(symbol, 1 / len(self.symbols))