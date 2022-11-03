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
    def __init__(self, maximumDrawdownPercent = 0.05):
        '''Initializes a new instance of the TrailingStopRiskManagementModel class
        Args:
            maximumDrawdownPercent: The maximum percentage drawdown allowed for algorithm portfolio compared with the highest unrealized profit, defaults to 5% drawdown'''
        self.maximumDrawdownPercent = abs(maximumDrawdownPercent)
        self.trailingAbsoluteHoldingsState = dict()

    def ManageRisk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance
            targets: The current portfolio targets to be assessed for risk'''
        riskAdjustedTargets = list()

        for kvp in algorithm.Securities:
            symbol = kvp.Key
            security = kvp.Value

            # Remove if not invested
            if not security.Invested:
                self.trailingAbsoluteHoldingsState.pop(symbol, None)
                continue

            position = PositionSide.Long if security.Holdings.IsLong else PositionSide.Short
            absoluteHoldingsValue = security.Holdings.AbsoluteHoldingsValue
            trailingAbsoluteHoldingsState = self.trailingAbsoluteHoldingsState.get(symbol)

            # Add newly invested security (if doesn't exist) or reset holdings state (if position changed)
            if trailingAbsoluteHoldingsState == None or position != trailingAbsoluteHoldingsState.position:
                self.trailingAbsoluteHoldingsState[symbol] = trailingAbsoluteHoldingsState = self.HoldingsState(position, security.Holdings.AbsoluteHoldingsCost)

            trailingAbsoluteHoldingsValue = trailingAbsoluteHoldingsState.absoluteHoldingsValue

            # Check for new max (for long position) or min (for short position) absolute holdings value
            if ((position == PositionSide.Long and trailingAbsoluteHoldingsValue < absoluteHoldingsValue) or
                (position == PositionSide.Short and trailingAbsoluteHoldingsValue > absoluteHoldingsValue)):
                self.trailingAbsoluteHoldingsState[symbol].absoluteHoldingsValue = absoluteHoldingsValue
                continue

            drawdown = abs((trailingAbsoluteHoldingsValue - absoluteHoldingsValue) / trailingAbsoluteHoldingsValue)

            if self.maximumDrawdownPercent < drawdown:
                self.trailingAbsoluteHoldingsState.pop(symbol, None)
                # liquidate
                riskAdjustedTargets.append(PortfolioTarget(symbol, 0))

        return riskAdjustedTargets

    class HoldingsState:
        def __init__(self, position, absoluteHoldingsValue):
            self.position = position
            self.absoluteHoldingsValue = absoluteHoldingsValue
