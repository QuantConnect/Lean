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

class MaximumDrawdownPercentPerSecurity(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that limits the drawdown per holding to the specified percentage'''

    def __init__(self, maximum_drawdown_percent = 0.05):
        '''Initializes a new instance of the MaximumDrawdownPercentPerSecurity class
        Args:
            maximum_drawdown_percent: The maximum percentage drawdown allowed for any single security holding'''
        self.maximum_drawdown_percent = -abs(maximum_drawdown_percent)

    def manage_risk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance
            targets: The current portfolio targets to be assessed for risk'''
        targets = []
        for kvp in algorithm.securities:
            security = kvp.value

            if not security.invested:
                continue

            pnl = security.holdings.unrealized_profit_percent
            if pnl < self.maximum_drawdown_percent:
                symbol = security.symbol

                # Cancel insights
                algorithm.insights.cancel([symbol])

                # liquidate
                targets.append(PortfolioTarget(symbol, 0))

        return targets
