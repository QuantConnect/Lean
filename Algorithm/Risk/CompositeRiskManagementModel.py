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

class CompositeRiskManagementModel(RiskManagementModel):
    '''Provides an implementation of IRiskManagementModel that combines multiple risk models
    into a single risk management model and properly sets each insights 'SourceModel' property.'''

    def __init__(self, *risk_management_models):
        '''Initializes a new instance of the CompositeRiskManagementModel class
        Args:
            risk_management_models: The individual risk management models defining this composite model.'''
        for model in risk_management_models:
            for attribute_names in [('ManageRisk', 'manage_risk'), ('OnSecuritiesChanged', 'on_securities_changed')]:
                if not hasattr(model, attribute_names[0]) and not hasattr(model, attribute_names[1]):
                    raise Exception(f'IRiskManagementModel.{attribute_names[1]} must be implemented. Please implement this missing method on {model.__class__.__name__}')

        self.risk_management_models = risk_management_models

    def manage_risk(self, algorithm, targets):
        '''Manages the algorithm's risk at each time step
        Args:
            algorithm: The algorithm instance
            targets: The current portfolio targets to be assessed for risk'''
        for model in self.risk_management_models:
            # take into account the possibility of ManageRisk returning nothing
            risk_adjusted = model.manage_risk(algorithm, targets)

            # produce a distinct set of new targets giving preference to newer targets
            symbols = [x.symbol for x in risk_adjusted]
            for target in targets:
                if target.symbol not in symbols:
                    risk_adjusted.append(target)

            targets = risk_adjusted

        return targets

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        This method patches this call through the each of the wrapped models.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for model in self.risk_management_models:
            model.on_securities_changed(algorithm, changes)

    def add_risk_management(self, risk_management_model):
        '''Adds a new 'IRiskManagementModel' instance
        Args:
            risk_management_model: The risk management model to add'''
        self.risk_management_models.add(risk_management_model)
