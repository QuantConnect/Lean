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

class TrailingStopRiskManagementModel(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that limits the maximum possible loss
    measured from the highest unrealized profit'''
    def __init__(self, maximum_drawdown_percent = 0.05):
        '''Initializes a new instance of the TrailingStopRiskManagementModel class
        Args:
            maximum_drawdown_percent: The maximum percentage drawdown allowed for algorithm portfolio compared with the highest unrealized profit, defaults to 5% drawdown'''
        self.maximum_drawdown_percent = abs(maximum_drawdown_percent)
        self.trailing_absolute_holdings_state = dict()

    def manage_risk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance
            targets: The current portfolio targets to be assessed for risk'''
        risk_adjusted_targets = list()

        for kvp in algorithm.securities:
            symbol = kvp.key
            security = kvp.value

            # Remove if not invested
            if not security.invested:
                self.trailing_absolute_holdings_state.pop(symbol, None)
                continue

            position = PositionSide.LONG if security.holdings.is_long else PositionSide.SHORT
            absolute_holdings_value = security.holdings.absolute_holdings_value
            trailing_absolute_holdings_state = self.trailing_absolute_holdings_state.get(symbol)

            # Add newly invested security (if doesn't exist) or reset holdings state (if position changed)
            if trailing_absolute_holdings_state == None or position != trailing_absolute_holdings_state.position:
                self.trailing_absolute_holdings_state[symbol] = trailing_absolute_holdings_state = self.HoldingsState(position, security.holdings.absolute_holdings_cost)

            trailing_absolute_holdings_value = trailing_absolute_holdings_state.absolute_holdings_value

            # Check for new max (for long position) or min (for short position) absolute holdings value
            if ((position == PositionSide.LONG and trailing_absolute_holdings_value < absolute_holdings_value) or
                (position == PositionSide.SHORT and trailing_absolute_holdings_value > absolute_holdings_value)):
                self.trailing_absolute_holdings_state[symbol].absolute_holdings_value = absolute_holdings_value
                continue

            drawdown = abs((trailing_absolute_holdings_value - absolute_holdings_value) / trailing_absolute_holdings_value)

            if self.maximum_drawdown_percent < drawdown:
                # Cancel insights
                algorithm.insights.cancel([ symbol ]);

                self.trailing_absolute_holdings_state.pop(symbol, None)
                # liquidate
                risk_adjusted_targets.append(PortfolioTarget(symbol, 0))

        return risk_adjusted_targets

    class HoldingsState:
        def __init__(self, position, absolute_holdings_value):
            self.position = position
            self.absolute_holdings_value = absolute_holdings_value
