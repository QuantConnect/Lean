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

class StandardDeviationExecutionModel(ExecutionModel):
    '''Execution model that submits orders while the current market prices is at least the configured number of standard
     deviations away from the mean in the favorable direction (below/above for buy/sell respectively)'''

    def __init__(self,
                 period = 60,
                 deviations = 2,
                 resolution = Resolution.MINUTE):
        '''Initializes a new instance of the StandardDeviationExecutionModel class
        Args:
            period: Period of the standard deviation indicator
            deviations: The number of deviations away from the mean before submitting an order
            resolution: The resolution of the STD and SMA indicators'''
        self.period = period
        self.deviations = deviations
        self.resolution = resolution
        self.targets_collection = PortfolioTargetCollection()
        self._symbol_data = {}

        # Gets or sets the maximum order value in units of the account currency.
        # This defaults to $20,000. For example, if purchasing a stock with a price
        # of $100, then the maximum order size would be 200 shares.
        self.maximum_order_value = 20000


    def execute(self, algorithm, targets):
        '''Executes market orders if the standard deviation of price is more
       than the configured number of deviations in the favorable direction.
       Args:
           algorithm: The algorithm instance
           targets: The portfolio targets'''
        self.targets_collection.add_range(targets)

        # for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
        if not self.targets_collection.is_empty:
            for target in self.targets_collection.order_by_margin_impact(algorithm):
                symbol = target.symbol

                # calculate remaining quantity to be ordered
                unordered_quantity = OrderSizing.get_unordered_quantity(algorithm, target)

                # fetch our symbol data containing our STD/SMA indicators
                data = self._symbol_data.get(symbol, None)
                if data is None: return

                # check order entry conditions
                if data.std.is_ready and self.price_is_favorable(data, unordered_quantity):
                    # Adjust order size to respect the maximum total order value
                    order_size = OrderSizing.get_order_size_for_maximum_value(data.security, self.maximum_order_value, unordered_quantity)

                    if order_size != 0:
                        algorithm.market_order(symbol, order_size)

            self.targets_collection.clear_fulfilled(algorithm)


    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for added in changes.added_securities:
            if added.symbol not in self._symbol_data:
                self._symbol_data[added.symbol] = SymbolData(algorithm, added, self.period, self.resolution)

        for removed in changes.removed_securities:
            # clean up data from removed securities
            symbol = removed.symbol
            if symbol in self._symbol_data:
                if self.is_safe_to_remove(algorithm, symbol):
                    data = self._symbol_data.pop(symbol)
                    algorithm.subscription_manager.remove_consolidator(symbol, data.consolidator)


    def price_is_favorable(self, data, unordered_quantity):
        '''Determines if the current price is more than the configured
       number of standard deviations away from the mean in the favorable direction.'''
        sma = data.sma.current.value
        deviations = self.deviations * data.std.current.value
        if unordered_quantity > 0:
            return data.security.bid_price < sma - deviations
        else:
            return data.security.ask_price > sma + deviations


    def is_safe_to_remove(self, algorithm, symbol):
        '''Determines if it's safe to remove the associated symbol data'''
        # confirm the security isn't currently a member of any universe
        return not any([kvp.value.contains_member(symbol) for kvp in algorithm.universe_manager])

class SymbolData:
    def __init__(self, algorithm, security, period, resolution):
        symbol = security.symbol
        self.security = security
        self.consolidator = algorithm.resolve_consolidator(symbol, resolution)

        sma_name = algorithm.create_indicator_name(symbol, f"SMA{period}", resolution)
        self.sma = SimpleMovingAverage(sma_name, period)
        algorithm.register_indicator(symbol, self.sma, self.consolidator)

        std_name = algorithm.create_indicator_name(symbol, f"STD{period}", resolution)
        self.std = StandardDeviation(std_name, period)
        algorithm.register_indicator(symbol, self.std, self.consolidator)

        # warmup our indicators by pushing history through the indicators
        bars = algorithm.history[self.consolidator.input_type](symbol, period, resolution)
        for bar in bars:
            self.sma.update(bar.end_time, bar.close)
            self.std.update(bar.end_time, bar.close)
