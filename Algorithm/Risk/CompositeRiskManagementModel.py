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
from QuantConnect.Algorithm.Framework.Risk import RiskManagementModel

class CompositeRiskManagementModel(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that combines multiple risk models
    into a single risk management model and properly sets each insights 'SourceModel' property.'''

    def __init__(self, *riskManagementModels):
        '''Initializes a new instance of the CompositeRiskManagementModel class
        Args:
            riskManagementModels: The individual risk management models defining this composite model.'''
        for model in riskManagementModels:
            for attributeName in ['ManageRisk', 'OnSecuritiesChanged']:
                if not hasattr(model, attributeName):
                    raise Exception(f'IRiskManagementModel.{attributeName} must be implemented. Please implement this missing method on {model.__class__.__name__}')

        self.riskManagementModels = riskManagementModels

    def ManageRisk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance
            targets: The current portfolio targets to be assessed for risk'''
        for model in self.riskManagementModels:
            # take into account the possibility of ManageRisk returning nothing
            riskAdjusted = model.ManageRisk(algorithm, targets)

            # produce a distinct set of new targets giving preference to newer targets
            symbols = [x.Symbol for x in riskAdjusted]
            for target in targets:
                if target.Symbol not in symbols:
                    riskAdjusted.append(target)

            targets = riskAdjusted

        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        This method patches this call through the each of the wrapped models.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for model in self.riskManagementModels:
            model.OnSecuritiesChanged(algorithm, changes)

    def AddRiskManagement(riskManagementModel):
        '''Adds a new 'IRiskManagementModel' instance
        Args:
            riskManagementModel: The risk management model to add'''
        self.riskManagementModels.Add(riskManagementModel)