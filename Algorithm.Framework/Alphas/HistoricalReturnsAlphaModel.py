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

class HistoricalReturnsAlphaModel(AlphaModel):
    '''Uses Historical returns to create insights.'''

    def __init__(self, *args, **kwargs):
        '''Initializes a new default instance of the HistoricalReturnsAlphaModel class.
        Args:
            lookback(int): Historical return lookback period
            resolution: The resolution of historical data'''
        self.lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), self.lookback)
        self.symbolDataBySymbol = {}

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
            if symbolData.CanEmit:

                direction = InsightDirection.Flat
                magnitude = float(symbolData.ROC.Current.Value)
                if magnitude > 0: direction = InsightDirection.Up
                if magnitude < 0: direction = InsightDirection.Down

                insights.append(Insight.Price(symbol, self.predictionInterval, direction, magnitude, None))

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        for removed in changes.RemovedSecurities:
            symbolData = self.symbolDataBySymbol.pop(removed.Symbol, None)
            if symbolData:
                algorithm.SubscriptionManager.RemoveConsolidator(removed.Symbol, symbolData.Consolidator)

        # initialize data for added securities
        added_symbols = []
        for added in changes.AddedSecurities:
            symbol = added.Symbol
            if symbol not in self.symbolDataBySymbol:
                self.symbolDataBySymbol[symbol] = SymbolData(algorithm, symbol, self.lookback, self.resolution)
                added_symbols.append(symbol)
        if added_symbols:
            history = algorithm.History[TradeBar](added_symbols, self.lookback, self.resolution)
            for trade_bars in history:
                for bar in trade_bars.Values:
                    self.symbolDataBySymbol[bar.Symbol].ROC.Update(bar.EndTime, bar.Value)


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, algorithm, symbol, lookback, resolution):
        self.Symbol = symbol
        self.ROC = RateOfChange('{}.ROC({})'.format(symbol, lookback), lookback)
        self.previous = 0

        self.Consolidator = algorithm.ResolveConsolidator(self.Symbol, resolution)
        algorithm.RegisterIndicator(self.Symbol, self.ROC, self.Consolidator)

    @property
    def CanEmit(self):
        if self.previous == self.ROC.Samples:
            return False

        self.previous = self.ROC.Samples
        return self.ROC.IsReady

    def __str__(self, **kwargs):
        return '{}: {:.2%}'.format(self.ROC.Name, (1 + self.Return)**252 - 1)
