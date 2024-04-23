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
from enum import Enum

class BasePairsTradingAlphaModel(AlphaModel):
    '''This alpha model is designed to accept every possible pair combination
    from securities selected by the universe selection model
    This model generates alternating long ratio/short ratio insights emitted as a group'''

    def __init__(self, lookback = 1,
            resolution = Resolution.DAILY,
            threshold = 1):
        ''' Initializes a new instance of the PairsTradingAlphaModel class
        Args:
            lookback: Lookback period of the analysis
            resolution: Analysis resolution
            threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight'''
        self.lookback = lookback
        self.resolution = resolution
        self.threshold = threshold
        self.prediction_interval = Time.multiply(Extensions.to_time_span(self.resolution), self.lookback)

        self.pairs = dict()
        self.securities = set()

        resolution_string = Extensions.get_enum_string(resolution, Resolution)
        self.name = f'{self.__class__.__name__}({self.lookback},{resolution_string},{Extensions.normalize_to_str(threshold)})'


    def update(self, algorithm, data):
        ''' Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []

        for key, pair in self.pairs.items():
            insights.extend(pair.get_insight_group())

        return insights

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        for security in changes.added_securities:
            self.securities.add(security)

        for security in changes.removed_securities:
            if security in self.securities:
                self.securities.remove(security)

        self.update_pairs(algorithm)

        for security in changes.removed_securities:
            keys = [k for k in self.pairs.keys() if security.symbol in k]
            for key in keys:
                self.pairs.pop(key).dispose()

    def update_pairs(self, algorithm):

        symbols = sorted([x.symbol for x in self.securities])

        for i in range(0, len(symbols)):
            asset_i = symbols[i]

            for j in range(1 + i, len(symbols)):
                asset_j = symbols[j]

                pair_symbol = (asset_i, asset_j)
                invert = (asset_j, asset_i)

                if pair_symbol in self.pairs or invert in self.pairs:
                    continue

                if not self.has_passed_test(algorithm, asset_i, asset_j):
                    continue

                pair = self.Pair(algorithm, asset_i, asset_j, self.prediction_interval, self.threshold)
                self.pairs[pair_symbol] = pair

    def has_passed_test(self, algorithm, asset1, asset2):
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
            SHORT_RATIO = -1
            FLAT_RATIO = 0
            LONG_RATIO = 1

        def __init__(self, algorithm, asset1, asset2, prediction_interval, threshold):
            '''Create a new pair
            Args:
                algorithm: The algorithm instance that experienced the change in securities
                asset1: The first asset's symbol in the pair
                asset2: The second asset's symbol in the pair
                prediction_interval: Period over which this insight is expected to come to fruition
                threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight'''
            self.state = self.State.FLAT_RATIO

            self.algorithm = algorithm
            self.asset1 = asset1
            self.asset2 = asset2

            # Created the Identity indicator for a given Symbol and
            # the consolidator it is registered to. The consolidator reference
            # will be used to remove it from SubscriptionManager
            def create_identity_indicator(symbol: Symbol):
                resolution = min([x.resolution for x in algorithm.subscription_manager.subscription_data_config_service.get_subscription_data_configs(symbol)])

                name = algorithm.create_indicator_name(symbol, "close", resolution)
                identity = Identity(name)

                consolidator = algorithm.resolve_consolidator(symbol, resolution)
                algorithm.register_indicator(symbol, identity, consolidator)

                return identity, consolidator

            self.asset1_price, self.identity_consolidator1 = create_identity_indicator(asset1);
            self.asset2_price, self.identity_consolidator2 = create_identity_indicator(asset2);

            self.ratio = IndicatorExtensions.over(self.asset1_price, self.asset2_price)
            self.mean = IndicatorExtensions.of(ExponentialMovingAverage(500), self.ratio)

            upper = ConstantIndicator[IndicatorDataPoint]("ct", 1 + threshold / 100)
            self.upper_threshold = IndicatorExtensions.times(self.mean, upper)

            lower = ConstantIndicator[IndicatorDataPoint]("ct", 1 - threshold / 100)
            self.lower_threshold = IndicatorExtensions.times(self.mean, lower)

            self.prediction_interval = prediction_interval

        def dispose(self):
            '''
            On disposal, remove the consolidators from the subscription manager
            '''
            self.algorithm.subscription_manager.remove_consolidator(self.asset1, self.identity_consolidator1)
            self.algorithm.subscription_manager.remove_consolidator(self.asset2, self.identity_consolidator2)

        def get_insight_group(self):
            '''Gets the insights group for the pair
            Returns:
                Insights grouped by an unique group id'''

            if not self.mean.is_ready:
                return []

            # don't re-emit the same direction
            if self.state is not self.State.LONG_RATIO and self.ratio > self.upper_threshold:
                self.state = self.State.LONG_RATIO

                # asset1/asset2 is more than 2 std away from mean, short asset1, long asset2
                short_asset_1 = Insight.price(self.asset1, self.prediction_interval, InsightDirection.DOWN)
                long_asset_2 = Insight.price(self.asset2, self.prediction_interval, InsightDirection.UP)

                # creates a group id and set the GroupId property on each insight object
                return Insight.group(short_asset_1, long_asset_2)

            # don't re-emit the same direction
            if self.state is not self.State.SHORT_RATIO and self.ratio < self.lower_threshold:
                self.state = self.State.SHORT_RATIO

                # asset1/asset2 is less than 2 std away from mean, long asset1, short asset2
                long_asset_1 = Insight.price(self.asset1, self.prediction_interval, InsightDirection.UP)
                short_asset_2 = Insight.price(self.asset2, self.prediction_interval, InsightDirection.DOWN)

                # creates a group id and set the GroupId property on each insight object
                return Insight.group(long_asset_1, short_asset_2)

            return []
