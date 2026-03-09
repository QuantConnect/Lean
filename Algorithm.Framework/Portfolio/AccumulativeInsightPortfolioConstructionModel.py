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
from EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel

class AccumulativeInsightPortfolioConstructionModel(EqualWeightingPortfolioConstructionModel):
    '''Provides an implementation of IPortfolioConstructionModel that allocates percent of account
    to each insight, defaulting to 3%.
    For insights of direction InsightDirection.UP, long targets are returned and
    for insights of direction InsightDirection.DOWN, short targets are returned.
    By default, no rebalancing shall be done.
    Rules:
        1. On active Up insight, increase position size by percent
        2. On active Down insight, decrease position size by percent
        3. On active Flat insight, move by percent towards 0
        4. On expired insight, and no other active insight, emits a 0 target'''

    def __init__(self,  rebalance = None, portfolio_bias = PortfolioBias.LONG_SHORT, percent = 0.03):
        '''Initialize a new instance of AccumulativeInsightPortfolioConstructionModel
        Args:
            rebalance: Rebalancing parameter. If it is a timedelta, date rules or Resolution, it will be converted into a function.
                              If None will be ignored.
                              The function returns the next expected rebalance time for a given algorithm UTC DateTime.
                              The function returns null if unknown, in which case the function will be called again in the
                              next loop. Returning current time will trigger rebalance.
            portfolio_bias: Specifies the bias of the portfolio (Short, Long/Short, Long)
            percent: percent of portfolio to allocate to each position'''
        super().__init__(rebalance)
        self.portfolio_bias = portfolio_bias
        self.percent = abs(percent)
        self.sign = lambda x: -1 if x < 0 else (1 if x > 0 else 0)

    def determine_target_percent(self, active_insights):
        '''Will determine the target percent for each insight
        Args:
            active_insights: The active insights to generate a target for'''
        percent_per_symbol = {}

        insights = sorted(self.algorithm.insights.get_active_insights(self.current_utc_time), key=lambda insight: insight.generated_time_utc)

        for insight in insights:
            target_percent = 0
            if insight.symbol in percent_per_symbol:
                target_percent = percent_per_symbol[insight.symbol]
                if insight.direction == InsightDirection.FLAT:
                    # We received a Flat
                    # if adding or subtracting will push past 0, then make it 0
                    if abs(target_percent) < self.percent:
                        target_percent = 0
                    else:
                        # otherwise, we flatten by percent
                        target_percent += (-self.percent if target_percent > 0 else self.percent)
            target_percent += self.percent * insight.direction

            # adjust to respect portfolio bias
            if self.portfolio_bias != PortfolioBias.LONG_SHORT and self.sign(target_percent) != self.portfolio_bias:
                target_percent = 0

            percent_per_symbol[insight.symbol] = target_percent

        return dict((insight, percent_per_symbol[insight.symbol]) for insight in active_insights)

    def create_targets(self, algorithm, insights):
        '''Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portfolio targets from
        Returns:
            An enumerable of portfolio targets to be sent to the execution model'''
        self.current_utc_time = algorithm.utc_time
        return super().create_targets(algorithm, insights)
