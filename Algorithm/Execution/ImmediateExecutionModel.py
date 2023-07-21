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
        self.targetsCollection = PortfolioTargetCollection()

    def Execute(self, algorithm, targets):
        '''Immediately submits orders for the specified portfolio targets.
        Args:
            algorithm: The algorithm instance
            targets: The portfolio targets to be ordered'''

        # for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
        self.targetsCollection.AddRange(targets)
        if not self.targetsCollection.IsEmpty:
            for target in self.targetsCollection.OrderByMarginImpact(algorithm):
                security = algorithm.Securities[target.Symbol]
                # calculate remaining quantity to be ordered
                quantity = OrderSizing.GetUnorderedQuantity(algorithm, target, security)
                if quantity != 0:
                    aboveMinimumPortfolio = BuyingPowerModelExtensions.AboveMinimumOrderMarginPortfolioPercentage(security.BuyingPowerModel, security, quantity, algorithm.Portfolio, algorithm.Settings.MinimumOrderMarginPortfolioPercentage)
                    if aboveMinimumPortfolio:
                        algorithm.MarketOrder(security, quantity)
                    elif not PortfolioTarget.MinimumOrderMarginPercentageWarningSent:
                        # will trigger the warning if it has not already been sent
                        PortfolioTarget.MinimumOrderMarginPercentageWarningSent = False

            self.targetsCollection.ClearFulfilled(algorithm)
