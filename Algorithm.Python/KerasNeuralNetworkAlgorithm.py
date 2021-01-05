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

import json
import numpy as np
import pandas as pd
from io import StringIO
from keras.models import Sequential
from keras.layers import Dense, Activation
from keras.optimizers import SGD
from keras.utils.generic_utils import serialize_keras_object

class KerasNeuralNetworkAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2019, 1, 1)   # Set Start Date
        self.SetEndDate(2020, 4, 1)     # Set End Date
        self.SetCash(100000)            # Set Strategy Cash

        self.modelBySymbol = {}

        for ticker in ["SPY", "QQQ", "TLT"]:
            symbol = self.AddEquity(ticker).Symbol

            # Read the model saved in the ObjectStore
            if self.ObjectStore.ContainsKey(f'{symbol}_model'):
                modelStr = self.ObjectStore.Read(f'{symbol}_model')
                config = json.loads(modelStr)['config']
                self.modelBySymbol[symbol] = Sequential.from_config(config)
                self.Debug(f'Model for {symbol} sucessfully retrieved from the ObjectStore')

        # Look-back period for training set
        self.lookback = 30

        # Train Neural Network every monday
        self.Train(
            self.DateRules.Every(DayOfWeek.Monday),
            self.TimeRules.AfterMarketOpen("SPY"),
            self.NeuralNetworkTraining)

        # Place trades on Monday, 30 minutes after the market is open
        self.Schedule.On(
            self.DateRules.EveryDay("SPY"),
            self.TimeRules.AfterMarketOpen("SPY", 30),
            self.Trade) 


    def OnEndOfAlgorithm(self):
        ''' Save the data and the mode using the ObjectStore '''
        for symbol, model in self.modelBySymbol.items():
            modelStr = json.dumps(serialize_keras_object(model))
            self.ObjectStore.Save(f'{symbol}_model', modelStr)
            self.Debug(f'Model for {symbol} sucessfully saved in the ObjectStore')


    def NeuralNetworkTraining(self):
        '''Train the Neural Network and save the model in the ObjectStore'''        
        symbols = self.Securities.keys()

        # Daily historical data is used to train the machine learning model
        history = self.History(symbols, self.lookback + 1, Resolution.Daily)
        history = history.open.unstack(0)

        for symbol in symbols:
            if symbol not in history:
                continue

            predictor = history[symbol][:-1]
            predictand = history[symbol][1:]

            # build a neural network from the 1st layer to the last layer
            model = Sequential()

            model.add(Dense(10, input_dim = 1))
            model.add(Activation('relu'))
            model.add(Dense(1))

            sgd = SGD(lr = 0.01)   # learning rate = 0.01

            # choose loss function and optimizing method
            model.compile(loss='mse', optimizer=sgd)

            # pick an iteration number large enough for convergence 
            for step in range(200):
                # training the model
                cost = model.train_on_batch(predictor, predictand)

            self.modelBySymbol[symbol] = model


    def Trade(self):
        '''
        Predict the price using the trained model and out-of-sample data
        Enter or exit positions based on relationship of the open price of the current bar and the prices defined by the machine learning model.
        Liquidate if the open price is below the sell price and buy if the open price is above the buy price 
        '''
        target = 1 / len(self.Securities)

        for symbol, model in self.modelBySymbol.items():

            # Get the out-of-sample history
            history = self.History(symbol, self.lookback, Resolution.Daily)
            history = history.open.unstack(0)[symbol]

            # Get the final predicted price
            prediction = model.predict(history)[0][-1]
            historyStd = np.std(history)

            holding = self.Portfolio[symbol]
            openPrice = self.CurrentSlice[symbol].Open

            # Follow the trend
            if holding.Invested:
                if openPrice < prediction - historyStd:
                    self.Liquidate(symbol)
            else:
                if openPrice > prediction + historyStd:
                    self.SetHoldings(symbol, target)