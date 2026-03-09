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
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.DAILY
        self.prediction_interval = Time.multiply(Extensions.to_time_span(self.resolution), self.lookback)
        self._symbol_data_by_symbol = {}
        self.insight_collection = InsightCollection()

    def update(self, algorithm, data):
        '''Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []

        for symbol, symbol_data in self._symbol_data_by_symbol.items():
            if symbol_data.can_emit:

                direction = InsightDirection.FLAT
                magnitude = symbol_data.return_
                if magnitude > 0: direction = InsightDirection.UP
                if magnitude < 0: direction = InsightDirection.DOWN

                if direction == InsightDirection.FLAT:
                    self.cancel_insights(algorithm, symbol)
                    continue

                insights.append(Insight.price(symbol, self.prediction_interval, direction, magnitude, None))

        self.insight_collection.add_range(insights)
        return insights

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        for removed in changes.removed_securities:
            symbol_data = self._symbol_data_by_symbol.pop(removed.symbol, None)
            if symbol_data is not None:
                symbol_data.remove_consolidators(algorithm)
            self.cancel_insights(algorithm, removed.symbol)

        # initialize data for added securities
        symbols = [ x.symbol for x in changes.added_securities ]
        history = algorithm.history(symbols, self.lookback, self.resolution)
        if history.empty: return

        tickers = history.index.levels[0]
        for ticker in tickers:
            symbol = SymbolCache.get_symbol(ticker)

            if symbol not in self._symbol_data_by_symbol:
                symbol_data = SymbolData(symbol, self.lookback)
                self._symbol_data_by_symbol[symbol] = symbol_data
                symbol_data.register_indicators(algorithm, self.resolution)
                symbol_data.warm_up_indicators(history.loc[ticker])

    def cancel_insights(self, algorithm, symbol):
        if not self.insight_collection.contains_key(symbol):
            return
        insights = self.insight_collection[symbol]
        algorithm.insights.cancel(insights)
        self.insight_collection.clear([ symbol ]);


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, symbol, lookback):
        self.symbol = symbol
        self.roc = RateOfChange('{}.roc({})'.format(symbol, lookback), lookback)
        self.consolidator = None
        self.previous = 0

    def register_indicators(self, algorithm, resolution):
        self.consolidator = algorithm.resolve_consolidator(self.symbol, resolution)
        algorithm.register_indicator(self.symbol, self.roc, self.consolidator)

    def remove_consolidators(self, algorithm):
        if self.consolidator is not None:
            algorithm.subscription_manager.remove_consolidator(self.symbol, self.consolidator)

    def warm_up_indicators(self, history):
        for tuple in history.itertuples():
            self.roc.update(tuple.Index, tuple.close)

    @property
    def return_(self):
        return float(self.roc.current.value)

    @property
    def can_emit(self):
        if self.previous == self.roc.samples:
            return False

        self.previous = self.roc.samples
        return self.roc.is_ready

    def __str__(self, **kwargs):
        return '{}: {:.2%}'.format(self.roc.name, (1 + self.return_)**252 - 1)
