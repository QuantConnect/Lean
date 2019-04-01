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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioTarget
from QuantConnect.Algorithm.Framework.Risk import RiskManagementModel

class TrailingStopRiskManagementModel(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that limits the maximum possible loss
    measured from the highest unrealized profit'''
    def __init__(self, maximumDrawdownPercent = 0.05):
        '''Initializes a new instance of the TrailingStopRiskManagementModel class
        Args:
            maximumDrawdownPercent: The maximum percentage drawdown allowed for algorithm portfolio compared with the highest unrealized profit, defaults to 5% drawdown'''
        self.maximumDrawdownPercent = -abs(maximumDrawdownPercent)
        self.trailingHighs = dict()

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
                self.trailingHighs.pop(symbol, None)
                continue

            # Add newly invested securities
            if symbol not in self.trailingHighs:
                self.trailingHighs[symbol] = security.Holdings.AveragePrice   # Set to average holding cost
                continue

            # Check for new highs and update - set to tradebar high
            if self.trailingHighs[symbol] < security.High:
                self.trailingHighs[symbol] = security.High
                continue

            # Check for securities past the drawdown limit
            securityHigh = self.trailingHighs[symbol]
            drawdown = (security.Low / securityHigh) - 1

            if drawdown < self.maximumDrawdownPercent:
                # liquidate
                riskAdjustedTargets.append(PortfolioTarget(symbol, 0))

        return riskAdjustedTargets