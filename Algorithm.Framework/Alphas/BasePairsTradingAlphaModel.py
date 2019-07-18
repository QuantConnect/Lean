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
from datetime import timedelta
from enum import Enum

class BasePairsTradingAlphaModel(AlphaModel):
    '''This alpha model is designed to accept every possible pair combination
    from securities selected by the universe selection model
    This model generates alternating long ratio/short ratio insights emitted as a group'''

    def __init__(self, lookback = 1,
            resolution = Resolution.Daily,
            threshold = 1):
        ''' Initializes a new instance of the PairsTradingAlphaModel class
        Args:
            lookback: Lookback period of the analysis
            resolution: Analysis resolution
            threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight'''
        self.lookback = lookback
        self.resolution = resolution
        self.threshold = threshold
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), self.lookback)

        self.pairs = dict()
        self.Securities = list()

        resolutionString = Extensions.GetEnumString(resolution, Resolution)
        self.Name = f'{self.__class__.__name__}({self.lookback},{resolutionString},{Extensions.NormalizeToStr(threshold)})'


    def Update(self, algorithm, data):
        ''' Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []

        for key, pair in self.pairs.items():
            insights.extend(pair.GetInsightGroup())

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        for security in changes.AddedSecurities:
            self.Securities.append(security)

        for security in changes.RemovedSecurities:
            if security in self.Securities:
                self.Securities.remove(security)

        self.UpdatePairs(algorithm)

        for security in changes.RemovedSecurities:
            keys = [k for k in self.pairs.keys() if security.Symbol in k]

            for key in keys:
                self.pairs.pop(key)

    def UpdatePairs(self, algorithm):

        symbols = sorted([x.Symbol for x in self.Securities], key=lambda x: str(x.ID))

        for i in range(0, len(symbols)):
            asset_i = symbols[i]

            for j in range(1 + i, len(symbols)):
                asset_j = symbols[j]

                pair_symbol = (asset_i, asset_j)
                invert = (asset_j, asset_i)

                if pair_symbol in self.pairs or invert in self.pairs:
                    continue

                if not self.HasPassedTest(algorithm, asset_i, asset_j):
                    continue

                pair = self.Pair(algorithm, asset_i, asset_j, self.predictionInterval, self.threshold)
                self.pairs[pair_symbol] = pair

    def HasPassedTest(self, algorithm, asset1, asset2):
        '''Check whether the assets pass a pairs trading test
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            asset1: The first asset's symbol in the pair
            asset2: The second asset's symbol in the pair
        Returns:
            True if the statistical test for the pair is successful'''
        return True

    class Pair:

        class State(Enum):
            ShortRatio = -1
            FlatRatio = 0
            LongRatio = 1

        def __init__(self, algorithm, asset1, asset2, predictionInterval, threshold):
            '''Create a new pair
            Args:
                algorithm: The algorithm instance that experienced the change in securities
                asset1: The first asset's symbol in the pair
                asset2: The second asset's symbol in the pair
                predictionInterval: Period over which this insight is expected to come to fruition
                threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight'''
            self.state = self.State.FlatRatio

            self.asset1 = asset1
            self.asset2 = asset2

            self.asset1Price = algorithm.Identity(asset1)
            self.asset2Price = algorithm.Identity(asset2)

            self.ratio = IndicatorExtensions.Over(self.asset1Price, self.asset2Price)
            self.mean = IndicatorExtensions.Of(ExponentialMovingAverage(500), self.ratio)

            upper = ConstantIndicator[IndicatorDataPoint]("ct", 1 + threshold / 100)
            self.upperThreshold = IndicatorExtensions.Times(self.mean, upper)

            lower = ConstantIndicator[IndicatorDataPoint]("ct", 1 - threshold / 100)
            self.lowerThreshold = IndicatorExtensions.Times(self.mean, lower)

            self.predictionInterval = predictionInterval

        def GetInsightGroup(self):
            '''Gets the insights group for the pair
            Returns:
                Insights grouped by an unique group id'''

            if not self.mean.IsReady:
                return []

            # don't re-emit the same direction
            if self.state is not self.State.LongRatio and self.ratio > self.upperThreshold:
                self.state = self.State.LongRatio

                # asset1/asset2 is more than 2 std away from mean, short asset1, long asset2
                shortAsset1 = Insight.Price(self.asset1, self.predictionInterval, InsightDirection.Down)
                longAsset2 = Insight.Price(self.asset2, self.predictionInterval, InsightDirection.Up)

                # creates a group id and set the GroupId property on each insight object
                return Insight.Group(shortAsset1, longAsset2)

            # don't re-emit the same direction
            if self.state is not self.State.ShortRatio and self.ratio < self.lowerThreshold:
                self.state = self.State.ShortRatio

                # asset1/asset2 is less than 2 std away from mean, long asset1, short asset2
                longAsset1 = Insight.Price(self.asset1, self.predictionInterval, InsightDirection.Up)
                shortAsset2 = Insight.Price(self.asset2, self.predictionInterval, InsightDirection.Down)

                # creates a group id and set the GroupId property on each insight object
                return Insight.Group(longAsset1, shortAsset2)

            return []