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

class SpreadExecutionModel(ExecutionModel):
    '''Execution model that submits orders while the current spread is tight.
       Note this execution model will not work using Resolution.DAILY since Exchange.exchange_open will be false, suggested resolution is Minute
    '''

    def __init__(self, accepting_spread_percent=0.005):
        '''Initializes a new instance of the SpreadExecutionModel class'''
        self.targets_collection = PortfolioTargetCollection()

        # Gets or sets the maximum spread compare to current price in percentage.
        self.accepting_spread_percent = Math.abs(accepting_spread_percent)

    def execute(self, algorithm, targets):
        '''Executes market orders if the spread percentage to price is in desirable range.
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

                # check order entry conditions
                if unordered_quantity != 0:
                    # get security information
                    security = algorithm.securities[symbol]
                    if self.spread_is_favorable(security):
                        algorithm.market_order(symbol, unordered_quantity)

            self.targets_collection.clear_fulfilled(algorithm)

    def spread_is_favorable(self, security):
        '''Determines if the spread is in desirable range.'''
        # Price has to be larger than zero to avoid zero division error, or negative price causing the spread percentage < 0 by error
        # Has to be in opening hours of exchange to avoid extreme spread in OTC period
        return security.exchange.exchange_open \
            and security.price > 0 and security.ask_price > 0 and security.bid_price > 0 \
            and (security.ask_price - security.bid_price) / security.price <= self.accepting_spread_percent
