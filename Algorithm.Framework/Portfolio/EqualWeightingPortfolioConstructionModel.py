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
from QuantConnect.Algorithm.Framework.Alphas import InsightCollection, InsightDirection
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioConstructionModel, PortfolioTarget
from itertools import groupby

class EqualWeightingPortfolioConstructionModel(PortfolioConstructionModel):
    '''Provides an implementation of IPortfolioConstructionModel that gives equal weighting to all securities.
    The target percent holdings of each security is 1/N where N is the number of securities.
    For insights of direction InsightDirection.Up, long targets are returned and
    for insights of direction InsightDirection.Down, short targets are returned.'''
    def __init__(self):
        self.insightCollection = InsightCollection()
        self.removedSymbols = []

    def CreateTargets(self, algorithm, insights):
        '''Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portoflio targets from
        Returns:
            An enumerable of portoflio targets to be sent to the execution model'''
        self.insightCollection.AddRange(insights)

        targets = []

        if self.removedSymbols is not None:
            # zero out securities removes from the universe
            for symbol in self.removedSymbols:
                targets.append(PortfolioTarget(symbol, 0))
                self.removedSymbols = None

        if len(insights) == 0:
            return targets

        # Get last insight that haven't expired of each symbol that is still in the universe
        activeInsights = list()
        # Remove expired insights
        validInsights = [ i for i in self.insightCollection if i.CloseTimeUtc > algorithm.UtcTime ]
        # Force one group per symbol
        for symbol, g in groupby(validInsights, lambda x: x.Symbol):
            # For direction, we'll trust the most recent insight
            activeInsights.append(sorted(g, key = lambda x: x.GeneratedTimeUtc)[-1])

        if len(activeInsights) == 0:
            return targets

        # give equal weighting to each security
        count = sum(x.Direction != InsightDirection.Flat for x in activeInsights)
        percent = 0 if count == 0 else 1.0 / count

        for insight in activeInsights:
            targets.append(PortfolioTarget.Percent(algorithm, insight.Symbol, insight.Direction * percent))

        # add targets to remove invested securities
        for kvp in algorithm.Portfolio:
            if kvp.Value.Invested and all([kvp.Key != x.Symbol for x in targets]):
                targets.append(PortfolioTarget(kvp.Key, 0))

        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # save securities removed so we can zero out our holdings
        self.removedSymbols = [x.Symbol for x in changes.RemovedSecurities]

        # remove the insights of the removed symbol from the collection
        for removedSymbol in self.removedSymbols:
            if self.insightCollection.ContainsKey(removedSymbol):
                for insight in self.insightCollection[removedSymbol]:
                    self.insightCollection.Remove(insight)