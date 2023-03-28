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

from AlgorithmImports import *

class EmaCrossAlphaModel(AlphaModel):
    '''Alpha model that uses an EMA cross to create insights'''

    def __init__(self,
                 fastPeriod = 12,
                 slowPeriod = 26,
                 resolution = Resolution.Daily):
        '''Initializes a new instance of the EmaCrossAlphaModel class
        Args:
            fastPeriod: The fast EMA period
            slowPeriod: The slow EMA period'''
        self.fastPeriod = fastPeriod
        self.slowPeriod = slowPeriod
        self.resolution = resolution
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(resolution), fastPeriod)
        self.symbolDataBySymbol = {}

        resolutionString = Extensions.GetEnumString(resolution, Resolution)
        self.Name = '{}({},{},{})'.format(self.__class__.__name__, fastPeriod, slowPeriod, resolutionString)


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
            if symbolData.Fast.IsReady and symbolData.Slow.IsReady:

                if symbolData.FastIsOverSlow:
                    if symbolData.Slow > symbolData.Fast:
                        insights.append(Insight.Price(symbol, self.predictionInterval, InsightDirection.Down))

                elif symbolData.SlowIsOverFast:
                    if symbolData.Fast > symbolData.Slow:
                        insights.append(Insight.Price(symbol, self.predictionInterval, InsightDirection.Up))

            symbolData.FastIsOverSlow = symbolData.Fast > symbolData.Slow

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        added_symbols = []
        for security in changes.AddedSecurities:
            symbol = security.Symbol 
            if symbol not in self.symbolDataBySymbol:
                self.symbolDataBySymbol[symbol] = SymbolData(symbol, self.fastPeriod, self.slowPeriod, algorithm, self.resolution)
                added_symbols.append(symbol)
            
        if added_symbols:
            history = algorithm.History[TradeBar](added_symbols, self.slowPeriod, self.resolution)
            for trade_bars in history:
                for bar in trade_bars.Values:
                    self.symbolDataBySymbol[bar.Symbol].update(bar)

        for security in changes.RemovedSecurities:
            symbol_data = self.symbolDataBySymbol.pop(security.Symbol, None)
            if symbol_data:
                symbol_data.dispose()


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, symbol, fastPeriod, slowPeriod, algorithm, resolution):
        self.symbol = symbol
        self.algorithm = algorithm

        # create fast/slow EMAs
        self.Fast = ExponentialMovingAverage(symbol, fastPeriod, ExponentialMovingAverage.SmoothingFactorDefault(fastPeriod))
        self.Slow = ExponentialMovingAverage(symbol, slowPeriod, ExponentialMovingAverage.SmoothingFactorDefault(slowPeriod))

        # Create a consolidator to update the EMAs over time
        self.consolidator = algorithm.ResolveConsolidator(symbol, resolution)
        algorithm.RegisterIndicator(self.symbol, self.Fast, self.consolidator)
        algorithm.RegisterIndicator(self.symbol, self.Slow, self.consolidator)

        # True if the fast is above the slow, otherwise false.
        # This is used to prevent emitting the same signal repeatedly
        self.FastIsOverSlow = False

    def update(self, bar):
        self.consolidator.Update(bar)
        
    def dispose(self):
        self.algorithm.SubscriptionManager.RemoveConsolidator(self.symbol, self.consolidator)

    @property
    def SlowIsOverFast(self):
        return not self.FastIsOverSlow
