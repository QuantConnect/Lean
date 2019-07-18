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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Indicators")

from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *


class MacdAlphaModel(AlphaModel):
    '''Defines a custom alpha model that uses MACD crossovers. The MACD signal line
    is used to generate up/down insights if it's stronger than the bounce threshold.
    If the MACD signal is within the bounce threshold then a flat price insight is returned.'''

    def __init__(self,
                 fastPeriod = 12,
                 slowPeriod = 26,
                 signalPeriod = 9,
                 movingAverageType = MovingAverageType.Exponential,
                 resolution = Resolution.Daily):
        ''' Initializes a new instance of the MacdAlphaModel class
        Args:
            fastPeriod: The MACD fast period
            slowPeriod: The MACD slow period</param>
            signalPeriod: The smoothing period for the MACD signal
            movingAverageType: The type of moving average to use in the MACD'''
        self.fastPeriod = fastPeriod
        self.slowPeriod = slowPeriod
        self.signalPeriod = signalPeriod
        self.movingAverageType = movingAverageType
        self.resolution = resolution
        self.insightPeriod = Time.Multiply(Extensions.ToTimeSpan(resolution), fastPeriod)
        self.bounceThresholdPercent = 0.01
        self.symbolData = {}

        resolutionString = Extensions.GetEnumString(resolution, Resolution)
        movingAverageTypeString = Extensions.GetEnumString(movingAverageType, MovingAverageType)
        self.Name = '{}({},{},{},{},{})'.format(self.__class__.__name__, fastPeriod, slowPeriod, signalPeriod, movingAverageTypeString, resolutionString)


    def Update(self, algorithm, data):
        ''' Determines an insight for each security based on it's current MACD signal
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []

        for key, sd in self.symbolData.items():
            if sd.Security.Price == 0:
                continue

            direction = InsightDirection.Flat
            normalized_signal = sd.MACD.Signal.Current.Value / sd.Security.Price

            if normalized_signal > self.bounceThresholdPercent:
                direction = InsightDirection.Up
            elif normalized_signal < -self.bounceThresholdPercent:
                direction = InsightDirection.Down

            # ignore signal for same direction as previous signal
            if direction == sd.PreviousDirection:
                continue

            insight = Insight.Price(sd.Security.Symbol, self.insightPeriod, direction)
            sd.PreviousDirection = insight.Direction
            insights.append(insight)

        return insights


    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        This initializes the MACD for each added security and cleans up the indicator for each removed security.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for added in changes.AddedSecurities:
            self.symbolData[added.Symbol] = SymbolData(algorithm, added, self.fastPeriod, self.slowPeriod, self.signalPeriod, self.movingAverageType, self.resolution)

        for removed in changes.RemovedSecurities:
            data = self.symbolData.pop(removed.Symbol, None)
            if data is not None:
                # clean up our consolidator
                algorithm.SubscriptionManager.RemoveConsolidator(removed.Symbol, data.Consolidator)

class SymbolData:
    def __init__(self, algorithm, security, fastPeriod, slowPeriod, signalPeriod, movingAverageType, resolution):
        self.Security = security
        self.MACD = MovingAverageConvergenceDivergence(fastPeriod, slowPeriod, signalPeriod, movingAverageType)

        self.Consolidator = algorithm.ResolveConsolidator(security.Symbol, resolution)
        algorithm.RegisterIndicator(security.Symbol, self.MACD, self.Consolidator)

        self.PreviousDirection = None
