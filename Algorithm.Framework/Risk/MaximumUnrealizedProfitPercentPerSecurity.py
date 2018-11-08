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

class MaximumUnrealizedProfitPercentPerSecurity(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that limits the unrealized profit per holding to the specified percentage'''

    def __init__(self, maximumUnrealizedProfitPercent = 0.05):
        '''Initializes a new instance of the MaximumUnrealizedProfitPercentPerSecurity class
        Args:
            maximumUnrealizedProfitPercent: The maximum percentage unrealized profit allowed for any single security holding, defaults to 5% drawdown per security'''
        self.maximumUnrealizedProfitPercent = abs(maximumUnrealizedProfitPercent)

    def ManageRisk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance
            targets: The current portfolio targets to be assessed for risk'''
        targets = []
        for kvp in algorithm.Securities:
            security = kvp.Value

            if not security.Invested:
                continue

            pnl = security.Holdings.UnrealizedProfitPercent
            if pnl > self.maximumUnrealizedProfitPercent:
                # liquidate
                targets.append(PortfolioTarget(security.Symbol, 0))

        return targets