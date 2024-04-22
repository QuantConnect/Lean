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
                 fast_period = 12,
                 slow_period = 26,
                 resolution = Resolution.DAILY):
        '''Initializes a new instance of the EmaCrossAlphaModel class
        Args:
            fast_period: The fast EMA period
            slow_period: The slow EMA period'''
        self.fast_period = fast_period
        self.slow_period = slow_period
        self.resolution = resolution
        self.prediction_interval = Time.multiply(Extensions.to_time_span(resolution), fast_period)
        self.symbol_data_by_symbol = {}

        resolution_string = Extensions.get_enum_string(resolution, Resolution)
        self.name = '{}({},{},{})'.format(self.__class__.__name__, fast_period, slow_period, resolution_string)


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
            if symbol_data.fast.is_ready and symbol_data.slow.is_ready:

                if symbol_data.fast_is_over_slow:
                    if symbol_data.slow > symbol_data.fast:
                        insights.append(Insight.price(symbol_data.symbol, self.prediction_interval, InsightDirection.DOWN))

                elif symbol_data.slow_is_over_fast:
                    if symbol_data.fast > symbol_data.slow:
                        insights.append(Insight.price(symbol_data.symbol, self.prediction_interval, InsightDirection.UP))

            symbol_data.fast_is_over_slow = symbol_data.fast > symbol_data.slow

        return insights

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for added in changes.added_securities:
            symbol_data = self.symbol_data_by_symbol.get(added.symbol)
            if symbol_data is None:
                symbol_data = SymbolData(added, self.fast_period, self.slow_period, algorithm, self.resolution)
                self.symbol_data_by_symbol[added.symbol] = symbol_data
            else:
                # a security that was already initialized was re-added, reset the indicators
                symbol_data.fast.reset()
                symbol_data.slow.reset()

        for removed in changes.removed_securities:
            data = self.symbol_data_by_symbol.pop(removed.symbol, None)
            if data is not None:
                # clean up our consolidators
                data.remove_consolidators()


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, security, fast_period, slow_period, algorithm, resolution):
        self.security = security
        self.symbol = security.symbol
        self.algorithm = algorithm

        self.fast_consolidator = algorithm.resolve_consolidator(security.symbol, resolution)
        self.slow_consolidator = algorithm.resolve_consolidator(security.symbol, resolution)

        algorithm.subscription_manager.add_consolidator(security.symbol, self.fast_consolidator)
        algorithm.subscription_manager.add_consolidator(security.symbol, self.slow_consolidator)

        # create fast/slow EMAs
        self.fast = ExponentialMovingAverage(security.symbol, fast_period, ExponentialMovingAverage.smoothing_factor_default(fast_period))
        self.slow = ExponentialMovingAverage(security.symbol, slow_period, ExponentialMovingAverage.smoothing_factor_default(slow_period))

        algorithm.register_indicator(security.symbol, self.fast, self.fast_consolidator);
        algorithm.register_indicator(security.symbol, self.slow, self.slow_consolidator);

        algorithm.warm_up_indicator(security.symbol, self.fast, resolution);
        algorithm.warm_up_indicator(security.symbol, self.slow, resolution);

        # True if the fast is above the slow, otherwise false.
        # This is used to prevent emitting the same signal repeatedly
        self.fast_is_over_slow = False

    def remove_consolidators(self):
        self.algorithm.subscription_manager.remove_consolidator(self.security.symbol, self.fast_consolidator)
        self.algorithm.subscription_manager.remove_consolidator(self.security.symbol, self.slow_consolidator)

    @property
    def slow_is_over_fast(self):
        return not self.fast_is_over_slow
