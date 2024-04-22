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

class VolumeWeightedAveragePriceExecutionModel(ExecutionModel):
    '''Execution model that submits orders while the current market price is more favorable that the current volume weighted average price.'''

    def __init__(self):
        '''Initializes a new instance of the VolumeWeightedAveragePriceExecutionModel class'''
        self.targets_collection = PortfolioTargetCollection()
        self.symbol_data = {}

        # Gets or sets the maximum order quantity as a percentage of the current bar's volume.
        # This defaults to 0.01m = 1%. For example, if the current bar's volume is 100,
        # then the maximum order size would equal 1 share.
        self.maximum_order_quantity_percent_volume = 0.01


    def execute(self, algorithm, targets):
        '''Executes market orders if the standard deviation of price is more
       than the configured number of deviations in the favorable direction.
       Args:
           algorithm: The algorithm instance
           targets: The portfolio targets'''

        # update the complete set of portfolio targets with the new targets
        self.targets_collection.add_range(targets)

        # for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
        if not self.targets_collection.is_empty:
            for target in self.targets_collection.order_by_margin_impact(algorithm):
                symbol = target.symbol

                # calculate remaining quantity to be ordered
                unordered_quantity = OrderSizing.get_unordered_quantity(algorithm, target)

                # fetch our symbol data containing our VWAP indicator
                data = self.symbol_data.get(symbol, None)
                if data is None: return

                # check order entry conditions
                if self.price_is_favorable(data, unordered_quantity):
                    # adjust order size to respect maximum order size based on a percentage of current volume
                    order_size = OrderSizing.get_order_size_for_percent_volume(data.security, self.maximum_order_quantity_percent_volume, unordered_quantity)

                    if order_size != 0:
                        algorithm.market_order(symbol, order_size)

            self.targets_collection.clear_fulfilled(algorithm)


    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for removed in changes.removed_securities:
            # clean up removed security data
            if removed.symbol in self.symbol_data:
                if self.is_safe_to_remove(algorithm, removed.symbol):
                    data = self.symbol_data.pop(removed.symbol)
                    algorithm.subscription_manager.remove_consolidator(removed.symbol, data.consolidator)

        for added in changes.added_securities:
            if added.symbol not in self.symbol_data:
                self.symbol_data[added.symbol] = SymbolData(algorithm, added)


    def price_is_favorable(self, data, unordered_quantity):
        '''Determines if the current price is more than the configured
       number of standard deviations away from the mean in the favorable direction.'''
        if unordered_quantity > 0:
            if data.security.bid_price < data.vwap:
                return True
        else:
            if data.security.ask_price > data.vwap:
                return True

        return False

    def is_safe_to_remove(self, algorithm, symbol):
        '''Determines if it's safe to remove the associated symbol data'''
        # confirm the security isn't currently a member of any universe
        return not any([kvp.value.contains_member(symbol) for kvp in algorithm.universe_manager])

class SymbolData:
    def __init__(self, algorithm, security):
        self.security = security
        self.consolidator = algorithm.resolve_consolidator(security.symbol, security.resolution)
        name = algorithm.create_indicator_name(security.symbol, "VWAP", security.resolution)
        self._vwap = IntradayVwap(name)
        algorithm.register_indicator(security.symbol, self._vwap, self.consolidator)

    @property
    def vwap(self):
       return self._vwap.value

class IntradayVwap:
    '''Defines the canonical intraday VWAP indicator'''
    def __init__(self, name):
        self.name = name
        self.value = 0.0
        self.last_date = datetime.min
        self.sum_of_volume = 0.0
        self.sum_of_price_times_volume = 0.0

    @property
    def is_ready(self):
        return self.sum_of_volume > 0.0

    def update(self, input):
        '''Computes the new VWAP'''
        success, volume, average_price = self.get_volume_and_average_price(input)
        if not success:
            return self.is_ready

        # reset vwap on daily boundaries
        if self.last_date != input.end_time.date():
            self.sum_of_volume = 0.0
            self.sum_of_price_times_volume = 0.0
            self.last_date = input.end_time.date()

        # running totals for Σ PiVi / Σ Vi
        self.sum_of_volume += volume
        self.sum_of_price_times_volume += average_price * volume

        if self.sum_of_volume == 0.0:
           # if we have no trade volume then use the current price as VWAP
           self.value = input.value
           return self.is_ready

        self.value = self.sum_of_price_times_volume / self.sum_of_volume
        return self.is_ready

    def get_volume_and_average_price(self, input):
        '''Determines the volume and price to be used for the current input in the VWAP computation'''

        if type(input) is Tick:
            if input.tick_type == TickType.TRADE:
                return True, float(input.quantity), float(input.last_price)

        if type(input) is TradeBar:
            if not input.is_fill_forward:
                average_price = float(input.high + input.low + input.close) / 3
                return True, float(input.volume), average_price

        return False, 0.0, 0.0
