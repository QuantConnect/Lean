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

class EqualWeightingPortfolioConstructionModel(PortfolioConstructionModel):
    '''Provides an implementation of IPortfolioConstructionModel that gives equal weighting to all securities.
    The target percent holdings of each security is 1/N where N is the number of securities.
    For insights of direction InsightDirection.Up, long targets are returned and
    for insights of direction InsightDirection.Down, short targets are returned.'''

    def __init__(self, rebalancingParam = Resolution.Daily):
        '''Initialize a new instance of EqualWeightingPortfolioConstructionModel
        Args:
            rebalancingParam: Rebalancing parameter. If it is a timedelta or Resolution, it will be converted into a function.
                              The function returns the next expected rebalance time for a given algorithm UTC DateTime'''
        self.insightCollection = InsightCollection()
        self.removedSymbols = []
        self.nextExpiryTime = UTCMIN
        self.rebalancingTime = UTCMIN
        self.rebalancingFunc = rebalancingParam

        # If the argument is an instance if Resolution or Timedelta
        # Redefine self.rebalancingFunc
        if isinstance(rebalancingParam, int):
            rebalancingParam = Extensions.ToTimeSpan(rebalancingParam)
        if isinstance(rebalancingParam, timedelta):
            self.rebalancingFunc = lambda dt: dt + rebalancingParam

    def ShouldCreateTargetForInsight(self, insight):
        '''Method that will determine if the portfolio construction model should create a
        target for this insight
        Args:
            insight: The insight to create a target for'''
        return True

    def DetermineTargetPercent(self, activeInsights):
        '''Will determine the target percent for each insight
        Args:
            activeInsights: The active insights to generate a target for'''
        result = {}

        # give equal weighting to each security
        count = sum(x.Direction != InsightDirection.Flat for x in activeInsights)
        percent = 0 if count == 0 else 1.0 / count
        for insight in activeInsights:
            result[insight] = insight.Direction * percent
        return result

    def CreateTargets(self, algorithm, insights):
        '''Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portfolio targets from
        Returns:
            An enumerable of portfolio targets to be sent to the execution model'''

        targets = []

        if (algorithm.UtcTime <= self.nextExpiryTime and
            algorithm.UtcTime <= self.rebalancingTime and
            len(insights) == 0 and
            self.removedSymbols is None):
            return targets

        for insight in insights:
            if self.ShouldCreateTargetForInsight(insight):
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

        # Determine target percent for the given insights
        percents = self.DetermineTargetPercent(lastActiveInsights)

        errorSymbols = {}
        for insight in lastActiveInsights:
            target = PortfolioTarget.Percent(algorithm, insight.Symbol, percents[insight])
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

        self.rebalancingTime = self.rebalancingFunc(algorithm.UtcTime)

        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # Get removed symbol and invalidate them in the insight collection
        self.removedSymbols = [x.Symbol for x in changes.RemovedSecurities]
        self.insightCollection.Clear(self.removedSymbols)