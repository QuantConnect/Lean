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
import tensorflow as tf

class TensorFlowNeuralNetworkAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)  # Set Start Date
        self.SetEndDate(2013, 10, 8) # Set End Date
        
        self.SetCash(100000)  # Set Strategy Cash
        spy = self.AddEquity("SPY", Resolution.Minute) # Add Equity
        
        self.symbols = [spy.Symbol] # potential trading symbols pool (in this algorithm there is only 1). 
        self.lookback = 30 # number of previous days for training
        
        self.Schedule.On(self.DateRules.Every(DayOfWeek.Monday), self.TimeRules.AfterMarketOpen("SPY", 28), self.NetTrain) # train the neural network 28 mins after market open
        self.Schedule.On(self.DateRules.Every(DayOfWeek.Monday), self.TimeRules.AfterMarketOpen("SPY", 30), self.Trade) # trade 30 mins after market open
        
    def add_layer(self, inputs, in_size, out_size, activation_function=None):
        # add one more layer and return the output of this layer
        # this is one NN with only one hidden layer
        Weights = tf.Variable(tf.random_normal([in_size, out_size]))
        biases = tf.Variable(tf.zeros([1, out_size]) + 0.1)
        Wx_plus_b = tf.matmul(inputs, Weights) + biases
        if activation_function is None:
            outputs = Wx_plus_b
        else:
            outputs = activation_function(Wx_plus_b)
        return outputs
    
    def NetTrain(self):
        # Daily historical data is used to train the machine learning model
        history = self.History(self.symbols, self.lookback + 1, Resolution.Daily)
        
        # model: use prices_x to fit prices_y; key: symbol; value: according price
        self.prices_x, self.prices_y = {}, {}
        
        # key: symbol; values: prices for sell or buy 
        self.sell_prices, self.buy_prices = {}, {}
        
        for symbol in self.symbols:
            if not history.empty:
                # Daily historical data is used to train the machine learning model 
                # use open prices to predict the next days'
                self.prices_x[symbol] = list(history.loc[symbol.Value]['open'][:-1])
                self.prices_y[symbol] = list(history.loc[symbol.Value]['open'][1:])
        
        for symbol in self.symbols:
            if symbol in self.prices_x:
                # create numpy array
                x_data = np.array(self.prices_x[symbol]).astype(np.float32).reshape((-1,1))
                y_data = np.array(self.prices_y[symbol]).astype(np.float32).reshape((-1,1))
                
                # define placeholder for inputs to network
                xs = tf.placeholder(tf.float32, [None, 1])
                ys = tf.placeholder(tf.float32, [None, 1])
                
                # add hidden layer
                l1 = self.add_layer(xs, 1, 10, activation_function=tf.nn.relu)
                # add output layer
                prediction = self.add_layer(l1, 10, 1, activation_function=None)
                
                # the error between prediciton and real data
                loss = tf.reduce_mean(tf.reduce_sum(tf.square(ys - prediction),
                                     reduction_indices=[1]))
                # use gradient descent and square error
                train_step = tf.train.GradientDescentOptimizer(0.1).minimize(loss)
                
                # the following is precedure for tensorflow
                sess = tf.Session()
                
                init = tf.global_variables_initializer()
                sess.run(init)
                
                for i in range(200):
                    # training
                    sess.run(train_step, feed_dict={xs: x_data, ys: y_data})
            
            # predict today's price
            y_pred_final = sess.run(prediction, feed_dict = {xs: y_data})[0][-1]
            
            # get sell prices and buy prices as trading signals
            self.sell_prices[symbol] = y_pred_final - np.std(y_data)
            self.buy_prices[symbol] = y_pred_final + np.std(y_data)
        
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