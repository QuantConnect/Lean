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

    def __init__(self, asynchronous=True):
        '''Initializes a new instance of the ImmediateExecutionModel class.
        Args:
            asynchronous: If True, orders will be submitted asynchronously.'''
        super().__init__(asynchronous)
        self.targets_collection = PortfolioTargetCollection()

    def execute(self, algorithm, targets):
        '''Immediately submits orders for the specified portfolio targets.
        Args:
            algorithm: The algorithm instance
            targets: The portfolio targets to be ordered'''
        # for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
        self.targets_collection.add_range(targets)
        if not self.targets_collection.is_empty:
            is_cash_account = algorithm.brokerage_model.account_type == AccountType.Cash
            has_open_position_reducing_order = is_cash_account and self._has_open_position_reducing_order(algorithm)

            for target in self.targets_collection.order_by_margin_impact(algorithm):
                security = algorithm.securities[target.symbol]
                # calculate remaining quantity to be ordered
                quantity = OrderSizing.get_unordered_quantity(algorithm, target, security, True)
                is_position_reducing_order = self._is_position_reducing_order(security.holdings.quantity, quantity)

                if quantity != 0:
                    # In cash accounts, submit position-reducing orders first and wait for them to fill before
                    # submitting cash-consuming orders. This avoids transient insufficient buying power rejects.
                    if has_open_position_reducing_order and not is_position_reducing_order:
                        continue

                    above_minimum_portfolio = BuyingPowerModelExtensions.above_minimum_order_margin_portfolio_percentage(
                        security.buying_power_model,
                        security,
                        quantity,
                        algorithm.portfolio,
                        algorithm.settings.minimum_order_margin_portfolio_percentage)
                    if above_minimum_portfolio:
                        algorithm.market_order(security, quantity, self.asynchronous, target.tag)
                        has_open_position_reducing_order = has_open_position_reducing_order or is_position_reducing_order
                    elif not PortfolioTarget.minimum_order_margin_percentage_warning_sent:
                        # will trigger the warning if it has not already been sent
                        PortfolioTarget.minimum_order_margin_percentage_warning_sent = False

            self.targets_collection.clear_fulfilled(algorithm)

    @staticmethod
    def _has_open_position_reducing_order(algorithm):
        for order in algorithm.transactions.get_open_orders():
            if order.symbol not in algorithm.securities:
                continue
            security = algorithm.securities[order.symbol]
            if ImmediateExecutionModel._is_position_reducing_order(security.holdings.quantity, order.quantity):
                return True
        return False

    @staticmethod
    def _is_position_reducing_order(holdings_quantity, order_quantity):
        return holdings_quantity != 0 and order_quantity != 0 and holdings_quantity * order_quantity < 0
