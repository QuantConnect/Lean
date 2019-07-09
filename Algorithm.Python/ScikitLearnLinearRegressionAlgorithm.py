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
        
        self.lookback = 30 # number of previous days for training
        
        self.SetCash(100000)  # Set Strategy Cash
        spy = self.AddEquity("SPY", Resolution.Minute)
        
        self.symbols = [ spy.Symbol ] # In the future, we can include more symbols to the list in this way
        
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.AfterMarketOpen("SPY", 28), self.Regression)
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.AfterMarketOpen("SPY", 30), self.Trade)
        
    
    def Regression(self):
        # Daily historical data is used to train the machine learning model
        history = self.History(self.symbols, self.lookback, Resolution.Daily)

        # price dictionary:    key: symbol; value: historical price
        self.prices = {}
        # slope dictionary:    key: symbol; value: slope
        self.slopes = {}
        
        for symbol in self.symbols:
            if not history.empty:
                # get historical open price
                self.prices[symbol] = list(history.loc[symbol.Value]['open'])

        # A is the design matrix
        A = range(self.lookback + 1)
        
        for symbol in self.symbols:
            if symbol in self.prices:
                # response
                Y = self.prices[symbol]
                # features
                X = np.column_stack([np.ones(len(A)), A])
                
                # data preparation
                length = min(len(X), len(Y))
                X = X[-length:]
                Y = Y[-length:]
                A = A[-length:]
                
                # fit the linear regression
                reg = LinearRegression().fit(X, Y)
                
                # run linear regression y = ax + b
                b = reg.intercept_
                a = reg.coef_[1]
                
                # store slopes for symbols
                self.slopes[symbol] = a/b
                
    
    def Trade(self):
        # if there is no open price
        if not self.prices:
            return 
        
        thod_buy = 0.001 # threshold of slope to buy
        thod_liquidate = -0.001 # threshold of slope to liquidate
        
        for holding in self.Portfolio.Values:
            slope = self.slopes[holding.Symbol] 
            # liquidate when slope smaller than thod_liquidate
            if holding.Invested and slope < thod_liquidate:
                self.Liquidate(holding.Symbol)
        
        for symbol in self.symbols:
            # buy when slope larger than thod_buy
            if self.slopes[symbol] > thod_buy:
                self.SetHoldings(symbol, 1 / len(self.symbols))