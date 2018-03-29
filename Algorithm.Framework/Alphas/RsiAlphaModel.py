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
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework.Alphas import *
from datetime import timedelta
from enum import Enum

class RsiAlphaModel:
    '''Uses Wilder's RSI to create insights. 
    Using default settings, a cross over below 30 or above 70 will trigger a new insight.'''

    def __init__(self, parameters = None):
        '''Initializes a new default instance of the RsiAlphaModel class.
        This uses the traditional 30/70 bounds coupled with 5% bounce protection.
        The traditional period of 14 days is used and the prediction interval is set to 14 days as well.
        Args:
            fastPeriod: The fast EMA period
            slowPeriod: The slow EMA period
            predictionInterval: The interval over which we're predicting'''
        self.parameters = parameters if parameters is not None else Parameters()
        self.symbolDataBySymbol ={}

    def Update(self, algorithm, data):
        '''Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []
        for symbol, symbolData in self.symbolDataBySymbol.items():
            rsi = symbolData.RSI
            previous_state = symbolData.State
            state = self.GetState(rsi, previous_state)

            if state != previous_state and rsi.IsReady:
                if state == State.TrippedLow:
                    insights.append(Insight(symbol, InsightType.Price, InsightDirection.Up, self.parameters.PredictionInterval))
                if state == State.TrippedHigh:
                    insights.append(Insight(symbol, InsightType.Price, InsightDirection.Down, self.parameters.PredictionInterval))

            symbolData.State = state

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Cleans out old security data and initializes the RSI for any newly added securities.
        Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        removed = [ x.Symbol for x in changes.RemovedSecurities ]
        if len(removed) > 0:
            for subscription in algorithm.SubscriptionManager.Subscriptions:
                symbol = subscription.Symbol
                if symbol in removed and symbol in self.symbolDataBySymbol:
                    self.symbolDataBySymbol.pop(symbol)

        # initialize data for added securities
        for added in changes.AddedSecurities:
            if added.Symbol not in self.symbolDataBySymbol:
                rsi = algorithm.RSI(added.Symbol, self.parameters.RsiPeriod, MovingAverageType.Wilders, self. parameters.Resolution)

                # seed new indicators using history request
                history = algorithm.History(added.Symbol, self.parameters.RsiPeriod)
                for row in history.itertuples():
                    rsi.Update(row.Index[1], row.close)

                self.symbolDataBySymbol[added.Symbol] = SymbolData(added, rsi)
                if self.parameters.Plot:
                    algorithm.PlotIndicator("RSI Alpha Model", True, rsi)


    def GetState(self, rsi, previous):
        ''' Determines the new state. This is basically cross-over detection logic that
        includes considerations for bouncing using the configured bounce tolerance.'''
        if rsi.Current.Value > self.parameters.UpperRsiBound:
            return State.TrippedHigh
        if rsi.Current.Value < self.parameters.LowerRsiBound:
            return State.TrippedLow
        if previous == State.TrippedLow:
            if rsi.Current.Value > self.parameters.LowerRsiBound + self.parameters.BounceTolerance:
                return State.Middle
        if previous == State.TrippedHigh:
            if rsi.Current.Value < self.parameters.UpperRsiBound - self.parameters.BounceTolerance:
                return State.Middle

        return previous


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, symbol, rsi):
        self.Symbol = symbol
        self.RSI = rsi
        self.State = State.Middle


class State(Enum):
    '''Defines the state. This is used to prevent signal spamming and aid in bounce detection.'''
    TrippedLow = 0
    Middle = 1
    TrippedHigh = 2


class Parameters:
    def __init__(self, *args, **kwargs):
        ''' Intializes a new instance of the Parameters class
        Args:
            0 - resolution: The RSI indicator resolution
            1 - rsiPeriod: The RSI indicator period
            2 - lowerRsiBound: The RSI lower bound, used to signal UP insights
            3 - upperRsiBound: The RSI upper bound, used to signal DOWN insights
            4 - predictionInterval: The period applied to each generated insight'''

        self.Plot = False                           # Plots the indicator values

        if (args.count == 0):
            self.Resolution = Resolution.Daily      # RSI indicator resolution
            self.RsiPeriod = 14                     # RSI period
            self.LowerRsiBound = 30                 # RSI lower bound. Values below this will trigger an UP prediction.
            self.UpperRsiBound = 70                 # RSI upper bound. Values above this will trigger a DOWN prediction.
            self.PredictionInterval = timedelta(14) # Generated insight prediction interval
        else:
            self.Resolution = args[0]
            self.RsiPeriod = args[1]
            self.LowerRsiBound = args[2]
            self.UpperRsiBound = args[3]
            self.PredictionInterval = args[4]

        # Before allowing another signal to be generated, we must cross-over this tolernce towards 50.
        # For example, if we just crossed below the lower bound (nominally 30), we won't interpret
        # another crossing until it moves above 35 (lower bound + tolerance). 
        # Likewise for the upper bound, just that we subtract, nominally 70 - 5 = 65.
        self.BounceTolerance = 5
