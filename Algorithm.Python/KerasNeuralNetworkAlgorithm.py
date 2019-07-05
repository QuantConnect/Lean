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
from keras.models import Sequential
from keras.layers import Dense, Activation
from keras.optimizers import SGD

class KerasNeuralNetworkAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)  # Set Start Date
        self.SetEndDate(2013, 10, 8) # Set End Date
        
        self.SetCash(100000)  # Set Strategy Cash
        spy = self.AddEquity("SPY", Resolution.Minute)
        self.symbols = [spy.Symbol]
        
        self.lookback = 30
        
        self.Schedule.On(self.DateRules.Every(DayOfWeek.Monday), self.TimeRules.AfterMarketOpen("SPY", 28), Action(self.NetTrain))
        self.Schedule.On(self.DateRules.Every(DayOfWeek.Monday), self.TimeRules.AfterMarketOpen("SPY", 30), Action(self.Trade))


    def OnData(self, data):
        self.data = data
        
    def NetTrain(self):
        history = self.History(self.symbols, self.lookback + 1, Resolution.Daily)
        
        self.prices_x = {} 
        self.prices_y = {}
        
        self.sell_prices = {}
        self.buy_prices = {}
        
        for symbol in self.symbols:
            if not history.empty:
                self.prices_x[symbol.Value] = list(history.loc[symbol.Value]['open'])[:-1]
                self.prices_y[symbol.Value] = list(history.loc[symbol.Value]['open'])[1:]
                
        for symbol in self.symbols:
            if symbol.Value in self.prices_x:
                x_data = np.array(self.prices_x[symbol.Value])
                y_data = np.array(self.prices_y[symbol.Value])
                
                # build a neural network from the 1st layer to the last layer
                model = Sequential()

                # model.add(Dense(units=1, input_dim=1))
                model.add(Dense(10, input_dim = 1))
                model.add(Activation('relu'))
                model.add(Dense(1))

                sgd = SGD(lr = 0.01) # learning rate = 0.01
                
                # choose loss function and optimizing method
                model.compile(loss='mse', optimizer=sgd)

                for step in range(701):
                    # training the model
                    cost = model.train_on_batch(x_data, y_data)

            y_pred_final = model.predict(y_data)[0][-1]
            
            # Follow the trend
            self.buy_prices[symbol.Value] = y_pred_final + np.std(y_data)
            self.sell_prices[symbol.Value] = y_pred_final - np.std(y_data)
        
    def Trade(self):
        for i in self.Portfolio.Values:
            # liquidate
            if self.data[i.Symbol.Value].Open < self.sell_prices[i.Symbol.Value] and i.Invested:
                self.Liquidate(i.Symbol)
            
            # buy
            if self.data[i.Symbol.Value].Open > self.buy_prices[i.Symbol.Value] and not i.Invested:
                self.SetHoldings(i.Symbol, 1 / len(self.symbols))