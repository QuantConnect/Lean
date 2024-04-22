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
from QuantConnect.Logging import *
from enum import Enum

class RsiAlphaModel(AlphaModel):
    '''Uses Wilder's RSI to create insights.
    Using default settings, a cross over below 30 or above 70 will trigger a new insight.'''

    def __init__(self,
                 period = 14,
                 resolution = Resolution.DAILY):
        '''Initializes a new instance of the RsiAlphaModel class
        Args:
            period: The RSI indicator period'''
        self.period = period
        self.resolution = resolution
        self.insight_period = Time.multiply(Extensions.to_time_span(resolution), period)
        self.symbol_data_by_symbol ={}

        resolution_string = Extensions.get_enum_string(resolution, Resolution)
        self.name = '{}({},{})'.format(self.__class__.__name__, period, resolution_string)

    def update(self, algorithm, data):
        '''Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []
        for symbol, symbol_data in self.symbol_data_by_symbol.items():
            rsi = symbol_data.rsi
            previous_state = symbol_data.state
            state = self.get_state(rsi, previous_state)

            if state != previous_state and rsi.is_ready:
                if state == State.TRIPPED_LOW:
                    insights.append(Insight.price(symbol, self.insight_period, InsightDirection.UP))
                if state == State.TRIPPED_HIGH:
                    insights.append(Insight.price(symbol, self.insight_period, InsightDirection.DOWN))

            symbol_data.state = state

        return insights


    def on_securities_changed(self, algorithm, changes):
        '''Cleans out old security data and initializes the RSI for any newly added securities.
        Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        # clean up data for removed securities
        for security in changes.removed_securities:
            symbol_data = self.symbol_data_by_symbol.pop(security.symbol, None)
            if symbol_data:
                symbol_data.dispose()

        # initialize data for added securities
        added_symbols = []
        for security in changes.added_securities:
            symbol = security.symbol
            if symbol not in self.symbol_data_by_symbol:
                symbol_data = SymbolData(algorithm, symbol, self.period, self.resolution)
                self.symbol_data_by_symbol[symbol] = symbol_data
                added_symbols.append(symbol)

        if added_symbols:
            history = algorithm.history[TradeBar](added_symbols, self.period, self.resolution)
            for trade_bars in history:
                for bar in trade_bars.values():
                    self.symbol_data_by_symbol[bar.symbol].update(bar)


    def get_state(self, rsi, previous):
        ''' Determines the new state. This is basically cross-over detection logic that
        includes considerations for bouncing using the configured bounce tolerance.'''
        if rsi.current.value > 70:
            return State.TRIPPED_HIGH
        if rsi.current.value < 30:
            return State.TRIPPED_LOW
        if previous == State.TRIPPED_LOW:
            if rsi.current.value > 35:
                return State.MIDDLE
        if previous == State.TRIPPED_HIGH:
            if rsi.current.value < 65:
                return State.MIDDLE

        return previous


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, algorithm, symbol, period, resolution):
        self.algorithm = algorithm
        self.symbol = symbol
        self.state = State.MIDDLE

        self.rsi = RelativeStrengthIndex(period, MovingAverageType.WILDERS)
        self.consolidator = algorithm.resolve_consolidator(symbol, resolution)
        algorithm.register_indicator(symbol, self.rsi, self.consolidator)

    def update(self, bar):
        self.consolidator.update(bar)

    def dispose(self):
        self.algorithm.subscription_manager.remove_consolidator(self.symbol, self.consolidator)


class State(Enum):
    '''Defines the state. This is used to prevent signal spamming and aid in bounce detection.'''
    TRIPPED_LOW = 0
    MIDDLE = 1
    TRIPPED_HIGH = 2
