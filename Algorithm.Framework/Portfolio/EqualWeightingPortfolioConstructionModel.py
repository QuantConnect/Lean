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
AddReference("QuantConnect.Algorithm.Framework")
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioConstructionModel, PortfolioTarget


class EqualWeightingPortfolioConstructionModel(PortfolioConstructionModel):
    '''Provides an implementation of IPortfolioConstructionModel that gives equal weighting to all securities.
    The target percent holdings of each security is 1/N where N is the number of securities. 
    For insights of direction InsightDirection.Up, long targets are returned and
    for insights of direction InsightDirection.Down, short targets are returned.'''
    def __init__(self):
        self.securities = []
        self.removedSymbols = []

    def CreateTargets(self, algorithm, insights):
        '''Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portoflio targets from
        Returns:
            An enumerable of portoflio targets to be sent to the execution model'''
        targets = []

        if self.removedSymbols is not None:
            # zero out securities removes from the universe
            for symbol in self.removedSymbols:
                targets.append(PortfolioTarget(symbol, 0))
                self.removedSymbols = None

        if len(self.securities) == 0:
            return []

        # give equal weighting to each security
        percent = 1.0 / len(self.securities)
        for insight in insights:
            targets.append(PortfolioTarget.Percent(algorithm, insight.Symbol, insight.Direction * percent))

        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # save securities removed so we can zero out our holdings
        self.removedSymbols = [x.Symbol for x in changes.RemovedSecurities]

        for added in changes.AddedSecurities:
            self.securities.append(added)
        for removed in changes.RemovedSecurities:
            if removed in self.securities:
                self.securities.remove(removed)