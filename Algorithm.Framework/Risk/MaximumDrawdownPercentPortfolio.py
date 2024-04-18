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

class MaximumDrawdownPercentPortfolio(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that limits the drawdown of the portfolio to the specified percentage.'''

    def __init__(self, maximum_drawdown_percent = 0.05, is_trailing = False):
        '''Initializes a new instance of the MaximumDrawdownPercentPortfolio class
        Args:
            maximum_drawdown_percent: The maximum percentage drawdown allowed for algorithm portfolio compared with starting value, defaults to 5% drawdown</param>
            is_trailing: If "false", the drawdown will be relative to the starting value of the portfolio.
                        If "true", the drawdown will be relative the last maximum portfolio value'''
        self.maximum_drawdown_percent = -abs(maximum_drawdown_percent)
        self.is_trailing = is_trailing
        self.initialised = False
        self.portfolio_high = 0

    def manage_risk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance
            targets: The current portfolio targets to be assessed for risk'''
        current_value = algorithm.portfolio.total_portfolio_value

        if not self.initialised:
            self.portfolio_high = current_value   # Set initial portfolio value
            self.initialised = True

        # Update trailing high value if in trailing mode
        if self.is_trailing and self.portfolio_high < current_value:
            self.portfolio_high = current_value
            return []   # return if new high reached

        pnl = self.get_total_drawdown_percent(current_value)
        if pnl < self.maximum_drawdown_percent and len(targets) != 0:
            self.initialised = False # reset the trailing high value for restart investing on next rebalcing period

            risk_adjusted_targets = []
            for target in targets:
                symbol = target.symbol

                # Cancel insights
                algorithm.insights.cancel([symbol])

                # liquidate
                risk_adjusted_targets.append(PortfolioTarget(symbol, 0))
            return risk_adjusted_targets

        return []

    def get_total_drawdown_percent(self, current_value):
        return (float(current_value) / float(self.portfolio_high)) - 1.0
