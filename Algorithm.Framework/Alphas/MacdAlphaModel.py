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
AddReference("QuantConnect.Indicators")

from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework.Alphas import *


class MacdAlphaModel:
    '''Defines a custom alpha model that uses MACD crossovers. The MACD signal line
    is used to generate up/down insights if it's stronger than the bounce threshold.
    If the MACD signal is within the bounce threshold then a flat price insight is returned.'''

    def __init__(self, consolidatorPeriod, insightPeriod, bounceThresholdPercent):
        ''' Initializes a new instance of the MacdAlphaModel class
        Args:
            consolidatorPeriod: The period of the MACD's input
            insightPeriod: The period assigned to generate insight
            bounceThresholdPercent: The percent change required in the MACD signal line to warrant an up/down insight'''
        self.insightPeriod = insightPeriod
        self.consolidatorPeriod = consolidatorPeriod
        self.bounceThresholdPercent = abs(bounceThresholdPercent)
        self.symbolData = {};

    def Update(self, algorithm, data):
        ''' Determines an insight for each security based on it's current MACD signal
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        for key, sd in self.symbolData.items():
            if sd.Security.Price == 0:
                continue
            
            direction = InsightDirection.Flat
            normalized_signal = sd.MACD.Signal.Current.Value / sd.Security.Price
            if normalized_signal > self.bounceThresholdPercent:
                direction = InsightDirection.Up
            elif normalized_signal < -self.bounceThresholdPercent:
                direction = InsightDirection.Down

            insight = Insight(sd.Security.Symbol, InsightType.Price, direction, self.insightPeriod)
            if insight.Equals(sd.previous_insight):
                continue

            sd.previous_insight = insight.Clone()

            yield insight


    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        This initializes the MACD for each added security and cleans up the indicator for each removed security.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for added in changes.AddedSecurities:
            self.symbolData[added.Symbol] = SymbolData(algorithm, added, self.consolidatorPeriod)

        for removed in changes.RemovedSecurities:
            data = self.symbolData.get(removed.Symbol)
            if data is not None:
                data.CleanUp(algorithm)
                self.symbolData.pop(removed.Symbol)

class SymbolData:
    def __init__(self, algorithm, security, period):
        self.Security = security
        self.Consolidator = algorithm.ResolveConsolidator(security.Symbol, period)
        algorithm.SubscriptionManager.AddConsolidator(security.Symbol, self.Consolidator)

        self.MACD = MovingAverageConvergenceDivergence(12, 26, 9, MovingAverageType.Exponential)
        self.Consolidator.DataConsolidated += self.OnDataConsolidated

        self.previous_insight = None

    def OnDataConsolidated(self, sender, consolidated):
        self.MACD.Update(consolidated.EndTime, consolidated.Value)

    def CleanUp(self, algorithm):
        '''Cleans up the indicator and consolidator
        Args:
            algorithm: The algorithm instance'''
        self.Consolidator.DataConsolidated -= self.OnDataConsolidated
        algorithm.SubscriptionManager.RemoveConsolidator(self.Security.Symbol, self.Consolidator)