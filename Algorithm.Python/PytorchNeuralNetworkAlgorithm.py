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
import torch
import torch.nn.functional as F

class PytorchNeuralNetworkAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)  # Set Start Date
        self.SetEndDate(2013, 10, 8) # Set End Date
        
        self.SetCash(100000)  # Set Strategy Cash
        
        # add symbol
        spy = self.AddEquity("SPY", Resolution.Minute)
        self.symbols = [spy.Symbol] # using a list can extend to condition for multiple symbols
        
        self.lookback = 30 # days of historical data (look back)
        
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.AfterMarketOpen("SPY", 28), self.NetTrain) # train the NN
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.AfterMarketOpen("SPY", 30), self.Trade)
    
    def NetTrain(self):
        # Daily historical data is used to train the machine learning model
        history = self.History(self.symbols, self.lookback + 1, Resolution.Daily)
        
        # dicts that store prices for training
        self.prices_x = {} 
        self.prices_y = {}
        
        # dicts that store prices for sell and buy
        self.sell_prices = {}
        self.buy_prices = {}
        
        for symbol in self.symbols:
            if not history.empty:
                # x: preditors; y: response
                self.prices_x[symbol] = list(history.loc[symbol.Value]['open'])[:-1]
                self.prices_y[symbol] = list(history.loc[symbol.Value]['open'])[1:]
                
        for symbol in self.symbols:
            # if this symbol has historical data
            if symbol in self.prices_x:
                
                net = Net(n_feature=1, n_hidden=10, n_output=1)     # define the network
                optimizer = torch.optim.SGD(net.parameters(), lr=0.2)
                loss_func = torch.nn.MSELoss()  # this is for regression mean squared loss
                
                for t in range(200):
                    # Get data and do preprocessing
                    x = torch.from_numpy(np.array(self.prices_x[symbol])).float()
                    y = torch.from_numpy(np.array(self.prices_y[symbol])).float()
                    
                    # unsqueeze data (see pytorch doc for details)
                    x = x.unsqueeze(1) 
                    y = y.unsqueeze(1)
                
                    prediction = net(x)     # input x and predict based on x

                    loss = loss_func(prediction, y)     # must be (1. nn output, 2. target)

                    optimizer.zero_grad()   # clear gradients for next train
                    loss.backward()         # backpropagation, compute gradients
                    optimizer.step()        # apply gradients
            
            # Follow the trend    
            self.buy_prices[symbol] = net(y)[-1] + np.std(y.data.numpy())
            self.sell_prices[symbol] = net(y)[-1] - np.std(y.data.numpy())
        
    def Trade(self):
        ''' 
        Enter or exit positions based on relationship of the open price of the current bar and the prices defined by the machine learning model.
        Liquidate if the open price is below the sell price and buy if the open price is above the buy price 
        ''' 
        for holding in self.Portfolio.Values:
            if self.CurrentSlice[holding.Symbol].Open < self.sell_prices[holding.Symbol] and holding.Invested:
                self.Liquidate(holding.Symbol)
            
            if self.CurrentSlice[holding.Symbol].Open > self.buy_prices[holding.Symbol] and not holding.Invested:
                self.SetHoldings(holding.Symbol, 1 / len(self.symbols))

            
        
# class for Pytorch NN model
class Net(torch.nn.Module):
    def __init__(self, n_feature, n_hidden, n_output):
        super(Net, self).__init__()
        self.hidden = torch.nn.Linear(n_feature, n_hidden)   # hidden layer
        self.predict = torch.nn.Linear(n_hidden, n_output)   # output layer
    
    def forward(self, x):
        x = F.relu(self.hidden(x))      # activation function for hidden layer
        x = self.predict(x)             # linear output
        return x