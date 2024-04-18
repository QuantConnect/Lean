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

class ImmediateExecutionModel(ExecutionModel):
    '''Provides an implementation of IExecutionModel that immediately submits market orders to achieve the desired portfolio targets'''

    def __init__(self):
        '''Initializes a new instance of the ImmediateExecutionModel class'''
        self.targets_collection = PortfolioTargetCollection()

    def execute(self, algorithm, targets):
        '''Immediately submits orders for the specified portfolio targets.
        Args:
            algorithm: The algorithm instance
            targets: The portfolio targets to be ordered'''

        # for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
        self.targets_collection.add_range(targets)
        if not self.targets_collection.is_empty:
            for target in self.targets_collection.order_by_margin_impact(algorithm):
                security = algorithm.securities[target.symbol]
                # calculate remaining quantity to be ordered
                quantity = OrderSizing.get_unordered_quantity(algorithm, target, security)
                if quantity != 0:
                    above_minimum_portfolio = BuyingPowerModelExtensions.above_minimum_order_margin_portfolio_percentage(
                        security.buying_power_model,
                        security,
                        quantity,
                        algorithm.portfolio,
                        algorithm.settings.minimum_order_margin_portfolio_percentage)
                    if above_minimum_portfolio:
                        algorithm.market_order(security, quantity)
                    elif not PortfolioTarget.minimum_order_margin_percentage_warning_sent:
                        # will trigger the warning if it has not already been sent
                        PortfolioTarget.minimum_order_margin_percentage_warning_sent = False

            self.targets_collection.clear_fulfilled(algorithm)
