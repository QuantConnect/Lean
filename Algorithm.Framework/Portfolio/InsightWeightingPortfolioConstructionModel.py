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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect import Resolution, Extensions
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from itertools import groupby
from datetime import datetime, timedelta
from pytz import utc
UTCMIN = datetime.min.replace(tzinfo=utc)

class InsightWeightingPortfolioConstructionModel(PortfolioConstructionModel):
    '''Provides an implementation of IPortfolioConstructionModel that generates percent targets based on the
    Insight.Weight. The target percent holdings of each Symbol is given by the Insight.Weight from the last
    active Insight for that symbol.
    For insights of direction InsightDirection.Up, long targets are returned and for insights of direction
    InsightDirection.Down, short targets are returned.
    If the sum of all the last active Insight per symbol is bigger than 1, it will factor down each target
    percent holdings proportionally so the sum is 1.
    It will ignore Insight that have no Insight.Weight value.'''

    def __init__(self, resolution = Resolution.Daily):
        '''Initialize a new instance of InsightWeightingPortfolioConstructionModel
        Args:
            resolution: Rebalancing frequency'''
        self.insightCollection = InsightCollection()
        self.removedSymbols = []
        self.nextExpiryTime = UTCMIN
        self.rebalancingTime = UTCMIN
        self.rebalancingPeriod = Extensions.ToTimeSpan(resolution)

    def CreateTargets(self, algorithm, insights):
        '''Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portoflio targets from
        Returns:
            An enumerable of portoflio targets to be sent to the execution model'''

        targets = []

        if (algorithm.UtcTime <= self.nextExpiryTime and
            algorithm.UtcTime <= self.rebalancingTime and
            len(insights) == 0 and
            self.removedSymbols is None):
            return targets

        # Ignore insights that don't have Weight value
        for insight in insights:
            if insight.Weight:
                self.insightCollection.Add(insight)

        # Create flatten target for each security that was removed from the universe
        if self.removedSymbols is not None:
            universeDeselectionTargets = [ PortfolioTarget(symbol, 0) for symbol in self.removedSymbols ]
            targets.extend(universeDeselectionTargets)
            self.removedSymbols = None

        # Get insight that haven't expired of each symbol that is still in the universe
        activeInsights = self.insightCollection.GetActiveInsights(algorithm.UtcTime)

        # Get the last generated active insight for each symbol
        lastActiveInsights = []
        for symbol, g in groupby(activeInsights, lambda x: x.Symbol):
            lastActiveInsights.append(sorted(g, key = lambda x: x.GeneratedTimeUtc)[-1])

        # We will adjust weights proportionally in case the sum is > 1 so it sums to 1.
        weightSums = sum(insight.Weight for insight in lastActiveInsights)
        weightFactor = 1.0
        if weightSums > 1:
            weightFactor = 1 / weightSums

        errorSymbols = {}
        for insight in lastActiveInsights:
            target = PortfolioTarget.Percent(algorithm, insight.Symbol, insight.Direction * insight.Weight * weightFactor)
            if not target is None:
                targets.append(target)
            else:
                errorSymbols[insight.Symbol] = insight.Symbol

        # Get expired insights and create flatten targets for each symbol
        expiredInsights = self.insightCollection.RemoveExpiredInsights(algorithm.UtcTime)

        expiredTargets = []
        for symbol, f in groupby(expiredInsights, lambda x: x.Symbol):
            if not self.insightCollection.HasActiveInsights(symbol, algorithm.UtcTime) and not symbol in errorSymbols:
                expiredTargets.append(PortfolioTarget(symbol, 0))
                continue

        targets.extend(expiredTargets)

        self.nextExpiryTime = self.insightCollection.GetNextExpiryTime()
        if self.nextExpiryTime is None:
            self.nextExpiryTime = UTCMIN

        self.rebalancingTime = algorithm.UtcTime + self.rebalancingPeriod

        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # Get removed symbol and invalidate them in the insight collection
        self.removedSymbols = [x.Symbol for x in changes.RemovedSecurities]
        self.insightCollection.Clear(self.removedSymbols)