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
AddReference("QuantConnect.Indicators")

from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework.Alphas import *
from datetime import timedelta
from enum import Enum

class PairsTradingAlphaModel(AlphaModel):
    '''This alpha model is designed to work against a single, predefined pair.
    This model generates alternating long ratio/short ratio insights emitted as a group'''

    class State(Enum):
        ShortRatio = -1
        FlatRatio = 0
        LongRatio = 1

    def __init__(self, asset1, asset2, threshold = 1):
        ''' Initializes a new instance of the PairsTradingAlphaModel class
        Args:
            asset1: The first asset's symbol in the pair
            asset2: The second asset's symbol in the pair
            threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight'''
        self.asset1 = asset1
        self.asset2 = asset2
        self.threshold = threshold
        self.state = self.State.FlatRatio
        self.asset1Price = None
        self.asset2Price = None
        self.ratio = None
        self.mean = None
        self.upperThreshold = None
        self.lowerThreshold = None

        self.Name = '{}({},{},{})'.format(self.__class__.__name__, asset1, asset2, Extensions.Normalize(threshold))


    def Update(self, algorithm, data):
        ''' Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        if self.mean is None or not self.mean.IsReady:
            return []

        # don't re-emit the same direction
        if self.state is not self.State.LongRatio and self.ratio > self.upperThreshold:
            self.state = self.State.LongRatio

            # asset1/asset2 is more than 2 std away from mean, short asset1, long asset2
            shortAsset1 = Insight.Price(self.asset1, timedelta(minutes = 15), InsightDirection.Down)
            longAsset2 = Insight.Price(self.asset2, timedelta(minutes = 15), InsightDirection.Up)

            # creates a group id and set the GroupId property on each insight object
            return Insight.Group(shortAsset1, longAsset2)

        # don't re-emit the same direction
        if self.state is not self.State.ShortRatio and self.ratio < self.lowerThreshold:
            self.state = self.State.ShortRatio

            # asset1/asset2 is less than 2 std away from mean, long asset1, short asset2
            longAsset1 = Insight.Price(self.asset1, timedelta(minutes = 15), InsightDirection.Up)
            shortAsset2 = Insight.Price(self.asset2, timedelta(minutes = 15), InsightDirection.Down)

            # creates a group id and set the GroupId property on each insight object
            return Insight.Group(longAsset1, shortAsset2)

        return []

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''
        for added in changes.AddedSecurities:
            # this model is limitted to looking at a single pair of assets
            if added.Symbol != self.asset1 and added.Symbol != self.asset2:
                continue

            if added.Symbol == self.asset1:
                self.asset1Price = algorithm.Identity(added.Symbol)
            else:
                self.asset2Price = algorithm.Identity(added.Symbol)

        if self.ratio is None:
            # initialize indicators dependent on both assets
            if self.asset1Price is not None and self.asset2Price is not None:
                self.ratio = IndicatorExtensions.Over(self.asset1Price, self.asset2Price)
                self.mean = IndicatorExtensions.Of(ExponentialMovingAverage(500), self.ratio)
                
                upper = ConstantIndicator[IndicatorDataPoint]("ct", 1 + self.threshold / 100)
                self.upperThreshold = IndicatorExtensions.Times(self.mean, upper)

                lower = ConstantIndicator[IndicatorDataPoint]("ct", 1 - self.threshold / 100)
                self.lowerThreshold = IndicatorExtensions.Times(self.mean, lower)