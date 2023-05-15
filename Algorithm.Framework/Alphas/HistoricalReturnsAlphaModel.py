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
        self.insightCollection = InsightCollection()

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
            
            symbolData.HandleCorporateActions(data)

            if symbolData.CanEmit():

                direction = InsightDirection.Flat
                magnitude = symbolData.ROC.Current.Value
                if magnitude > 0: direction = InsightDirection.Up
                if magnitude < 0: direction = InsightDirection.Down
                
                if direction == InsightDirection.Flat:
                    self.CancelInsights(algorithm, symbol)
                    continue

                insights.append(Insight.Price(symbol, self.predictionInterval, direction, magnitude))

        self.insightCollection.AddRange(insights)
        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        for removed in changes.RemovedSecurities:
            symbolData = self.symbolDataBySymbol.pop(removed.Symbol, None)
            if symbolData is not None:
                symbolData.dispose()
            self.CancelInsights(algorithm, removed.Symbol)

        # Indicators must be updated with scaled raw data to avoid price jumps
        dataNormalizationMode = algorithm.UniverseSettings.DataNormalizationMode
        if dataNormalizationMode == DataNormalizationMode.Raw:
            dataNormalizationMode = DataNormalizationMode.ScaledRaw

        # initialize data for added securities
        addedSymbols = {}
        for added in changes.AddedSecurities:
            symbol = added.Symbol
            if symbol not in self.symbolDataBySymbol:
                symbolData = SymbolData(algorithm, symbol, self.lookback, self.resolution, dataNormalizationMode)
                self.symbolDataBySymbol[symbol] = symbolData
                addedSymbols[str(symbol.ID)] = symbol

        history = algorithm.History(list(addedSymbols.values()), self.lookback, self.resolution, dataNormalizationMode=dataNormalizationMode)
        if history.empty: return

        for index, row in history.iterrows():
            symbol = addedSymbols[index[-2]]
            self.symbolDataBySymbol[symbol].ROC.Update(index[-1], row.close)


    def CancelInsights(self, algorithm, symbol):
        if not self.insightCollection.ContainsKey(symbol):
            return
        insights = self.insightCollection[symbol]
        algorithm.Insights.Cancel(insights)
        self.insightCollection.Clear([ symbol ]);


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, algorithm, symbol, lookback, resolution, dataNormalizationMode):
        self.algorithm = algorithm
        self.symbol = symbol
        self.consolidator = algorithm.ResolveConsolidator(self.symbol, resolution)
        self.ROC = RateOfChange(f'{symbol}.ROC({lookback})', lookback)
        algorithm.RegisterIndicator(self.symbol, self.ROC, self.consolidator)
        self.previous = 0

        def handleCorporateActions(slice):
            if slice.Splits.ContainsKey(symbol) or slice.Dividends.ContainsKey(symbol):
                # We need to keep the relative difference between samples and period
                delta = self.ROC.Samples - self.previous

                self.ROC.Reset()

                # warmup our indicators by pushing history through the consolidators
                history = algorithm.History([symbol], self.ROC.WarmUpPeriod, resolution, dataNormalizationMode=dataNormalizationMode)
                for index, row in history.iterrows():
                    self.ROC.Update(index[-1], row.close)

                self.previous = self.ROC.Samples + delta;

        self.HandleCorporateActions = lambda slice: handleCorporateActions(slice)

    def CanEmit(self):
        if self.previous == self.ROC.Samples:
            return False

        self.previous = self.ROC.Samples
        return self.ROC.IsReady

    def dispose(self):
        self.algorithm.SubscriptionManager.RemoveConsolidator(self.symbol, self.consolidator)

    def __str__(self, **kwargs):
        return '{}: {:.2%}'.format(self.ROC.Name, (1 + self.Return)**252 - 1)
