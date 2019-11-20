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


class AccumulativeInsightPortfolioConstructionModel(PortfolioConstructionModel):
    '''Provides an implementation of IPortfolioConstructionModel that allocates percent of account
    to each insight, defaulting to 3%.
    For insights of direction InsightDirection.Up, long targets are returned and
    for insights of direction InsightDirection.Down, short targets are returned.
    No rebalancing shall be done, as a new insight or the age of the insight shall determine whether to exit
    the positions.
    Rules:
        1. On Up insight, increase position size by percent
        2. On Down insight, decrease position size by percent
        3. On Flat insight, move by percent towards 0
        4. On expired insight, perform a Flat insight'''

    def __init__(self, percent = 0.03):
        '''Initialize a new instance of AccumulativeInsightPortfolioConstructionModel
        Args:
            percent: percent of portfolio to allocate to each position'''
        self.insightCollection = InsightCollection()
        self.removedSymbols = []
        self.nextExpiryTime = UTCMIN
        self.percent = abs(percent)
        self.positionSizes = {}
        self.usedInsight = {}
        self.expiredList = {}

    def ShouldCreateTargetForInsight(self, insight):
        '''Method that will determine if the portfolio construction model should create a
        target for this insight
        Args:
            insight: The insight to create a target for'''
        # We can probably use this to do something smarter
        return True

    def DetermineTargetPercent(self, activeInsights):
        '''Will determine the target percent for each insight
        Args:
            activeInsights: The active insights to generate a target for'''
        result = {}
        
        for insight in activeInsights:
            if insight in self.usedInsight:
                continue
            self.usedInsight[insight] = 1
            if insight.Symbol in self.positionSizes:
                self.positionSizes[insight.Symbol] += self.percent * insight.Direction
            else:
                self.positionSizes[insight.Symbol] = self.percent * insight.Direction
            
            if insight.Direction == 0:
                # We received a Flat
                
                # if adding or subtracting will push past 0, then make it 0
                if abs(self.positionSizes[insight.Symbol]) < self.percent:
                    self.positionSizes[insight.Symbol] = 0
                
                # otherwise, we flatten by percent
                if self.positionSizes[insight.Symbol] > 0:
                    self.positionSizes[insight.Symbol] -= self.percent
                if self.positionSizes[insight.Symbol] < 0:
                    self.positionSizes[insight.Symbol] += self.percent
                    
            result[insight] = self.positionSizes[insight.Symbol]
        return result

    def UpdateExpiredInsights(self, activeInsights):
        '''Will determine the target percent for each insight
        Args:
            activeInsights: The active insights to generate a target for'''
        result = {}
        
        for insight in activeInsights:
            if insight in self.expiredList:
                continue
            self.expiredList[insight] = 1
            
            # if an expiring insight pushes it past 0, then flatten to 0
            if abs(self.positionSizes[insight.Symbol]) < self.percent and insight.Direction != 0:
                self.positionSizes[insight.Symbol] = 0
            else:
                self.positionSizes[insight.Symbol] -= self.percent * insight.Direction
            result[insight] = self.positionSizes[insight.Symbol]
        return result

    def CreateTargets(self, algorithm, insights):
        '''Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portfolio targets from
        Returns:
            An enumerable of portfolio targets to be sent to the execution model'''

        targets = []

        if (algorithm.UtcTime <= self.nextExpiryTime and len(insights) == 0 and self.removedSymbols is None):
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
        for insight in percents:
            target = PortfolioTarget.Percent(algorithm, insight.Symbol, percents[insight])
            if not target is None:
                targets.append(target)
            else:
                errorSymbols[insight.Symbol] = insight.Symbol

        # Get expired insights and create flatten targets for each symbol
        expiredInsights = self.insightCollection.RemoveExpiredInsights(algorithm.UtcTime)

        percents = self.UpdateExpiredInsights(expiredInsights)
        for insight in percents:
            target = PortfolioTarget.Percent(algorithm, insight.Symbol, percents[insight])
            if not target is None:
                targets.append(target)
            else:
                errorSymbols[insight.Symbol] = insight.Symbol
                
        self.nextExpiryTime = self.insightCollection.GetNextExpiryTime()
        if self.nextExpiryTime is None:
            self.nextExpiryTime = UTCMIN
        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # Get removed symbol and invalidate them in the insight collection
        self.removedSymbols = [x.Symbol for x in changes.RemovedSecurities]
        self.insightCollection.Clear(self.removedSymbols)
