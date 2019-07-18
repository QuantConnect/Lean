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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Logging")
AddReference("QuantConnect.Common")

from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Logging import Log
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from datetime import timedelta
from enum import Enum

class RsiAlphaModel(AlphaModel):
    '''Uses Wilder's RSI to create insights.
    Using default settings, a cross over below 30 or above 70 will trigger a new insight.'''

    def __init__(self,
                 period = 14,
                 resolution = Resolution.Daily):
        '''Initializes a new instance of the RsiAlphaModel class
        Args:
            period: The RSI indicator period'''
        self.period = period
        self.resolution = resolution
        self.insightPeriod = Time.Multiply(Extensions.ToTimeSpan(resolution), period)
        self.symbolDataBySymbol ={}

        resolutionString = Extensions.GetEnumString(resolution, Resolution)
        self.Name = '{}({},{})'.format(self.__class__.__name__, period, resolutionString)

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
                    insights.append(Insight.Price(symbol, self.insightPeriod, InsightDirection.Up))
                if state == State.TrippedHigh:
                    insights.append(Insight.Price(symbol, self.insightPeriod, InsightDirection.Down))

            symbolData.State = state

        return insights


    def OnSecuritiesChanged(self, algorithm, changes):
        '''Cleans out old security data and initializes the RSI for any newly added securities.
        Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        symbols = [ x.Symbol for x in changes.RemovedSecurities ]
        if len(symbols) > 0:
            for subscription in algorithm.SubscriptionManager.Subscriptions:
                if subscription.Symbol in symbols:
                    self.symbolDataBySymbol.pop(subscription.Symbol, None)
                    subscription.Consolidators.Clear()

        # initialize data for added securities

        addedSymbols = [ x.Symbol for x in changes.AddedSecurities if x.Symbol not in self.symbolDataBySymbol]
        if len(addedSymbols) == 0: return

        history = algorithm.History(addedSymbols, self.period, self.resolution)

        for symbol in addedSymbols:
            rsi = algorithm.RSI(symbol, self.period, MovingAverageType.Wilders, self.resolution)

            if not history.empty:
                ticker = SymbolCache.GetTicker(symbol)

                if ticker not in history.index.levels[0]:
                    Log.Trace(f'RsiAlphaModel.OnSecuritiesChanged: {ticker} not found in history data frame.')
                    continue

                for tuple in history.loc[ticker].itertuples():
                    rsi.Update(tuple.Index, tuple.close)

            self.symbolDataBySymbol[symbol] = SymbolData(symbol, rsi)


    def GetState(self, rsi, previous):
        ''' Determines the new state. This is basically cross-over detection logic that
        includes considerations for bouncing using the configured bounce tolerance.'''
        if rsi.Current.Value > 70:
            return State.TrippedHigh
        if rsi.Current.Value < 30:
            return State.TrippedLow
        if previous == State.TrippedLow:
            if rsi.Current.Value > 35:
                return State.Middle
        if previous == State.TrippedHigh:
            if rsi.Current.Value < 65:
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