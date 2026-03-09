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
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

class EmaCrossUniverseSelectionModel(FundamentalUniverseSelectionModel):
    '''Provides an implementation of FundamentalUniverseSelectionModel that subscribes to
    symbols with the larger delta by percentage between the two exponential moving average'''

    def __init__(self,
                 fastPeriod = 100,
                 slowPeriod = 300,
                 universeCount = 500,
                 universeSettings = None):
        '''Initializes a new instance of the EmaCrossUniverseSelectionModel class
        Args:
            fastPeriod: Fast EMA period
            slowPeriod: Slow EMA period
            universeCount: Maximum number of members of this universe selection
            universeSettings: The settings used when adding symbols to the algorithm, specify null to use algorithm.UniverseSettings'''
        super().__init__(False, universeSettings)
        self.fast_period = fastPeriod
        self.slow_period = slowPeriod
        self.universe_count = universeCount
        self.tolerance = 0.01
        # holds our coarse fundamental indicators by symbol
        self.averages = {}

    def select_coarse(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]) -> list[Symbol]:
        '''Defines the coarse fundamental selection function.
        Args:
            algorithm: The algorithm instance
            fundamental: The coarse fundamental data used to perform filtering</param>
        Returns:
            An enumerable of symbols passing the filter'''
        filtered = []

        for cf in fundamental:
            if cf.symbol not in self.averages:
                self.averages[cf.symbol] = self.SelectionData(cf.symbol, self.fast_period, self.slow_period)

            # grab th SelectionData instance for this symbol
            avg = self.averages.get(cf.symbol)

            # Update returns true when the indicators are ready, so don't accept until they are
            # and only pick symbols who have their fastPeriod-day ema over their slowPeriod-day ema
            if avg.update(cf.end_time, cf.adjusted_price) and avg.fast > avg.slow * (1 + self.tolerance):
                filtered.append(avg)

        # prefer symbols with a larger delta by percentage between the two averages
        filtered = sorted(filtered, key=lambda avg: avg.scaled_delta, reverse = True)

        # we only need to return the symbol and return 'universeCount' symbols
        return [x.symbol for x in filtered[:self.universe_count]]

    # class used to improve readability of the coarse selection function
    class SelectionData:
        def __init__(self, symbol, fast_period, slow_period):
            self.symbol = symbol
            self.fast_ema = ExponentialMovingAverage(fast_period)
            self.slow_ema = ExponentialMovingAverage(slow_period)

        @property
        def fast(self):
            return float(self.fast_ema.current.value)

        @property
        def slow(self):
            return float(self.slow_ema.current.value)

        # computes an object score of how much large the fast is than the slow
        @property
        def scaled_delta(self):
            return (self.fast - self.slow) / ((self.fast + self.slow) / 2)

        # updates the EMAFast and EMASlow indicators, returning true when they're both ready
        def update(self, time, value):
            return self.slow_ema.update(time, value) & self.fast_ema.update(time, value)
