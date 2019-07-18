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
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Algorithm.Framework")

from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Indicators import ExponentialMovingAverage
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

class EmaCrossUniverseSelectionModel(FundamentalUniverseSelectionModel):
    '''Provides an implementation of FundamentalUniverseSelectionModel that subscribes to
    symbols with the larger delta by percentage between the two exponential moving average'''

    def __init__(self,
                 fastPeriod = 100,
                 slowPeriod = 300,
                 universeCount = 500,
                 universeSettings = None,
                 securityInitializer = None):
        '''Initializes a new instance of the EmaCrossUniverseSelectionModel class
        Args:
            fastPeriod: Fast EMA period
            slowPeriod: Slow EMA period
            universeCount: Maximum number of members of this universe selection
            universeSettings: The settings used when adding symbols to the algorithm, specify null to use algorthm.UniverseSettings
            securityInitializer: Optional security initializer invoked when creating new securities, specify null to use algorithm.SecurityInitializer'''
        super().__init__(False, universeSettings, securityInitializer)
        self.fastPeriod = fastPeriod
        self.slowPeriod = slowPeriod
        self.universeCount = universeCount
        self.tolerance = 0.01
        # holds our coarse fundamental indicators by symbol
        self.averages = {}

    def SelectCoarse(self, algorithm, coarse):
        '''Defines the coarse fundamental selection function.
        Args:
            algorithm: The algorithm instance
            coarse: The coarse fundamental data used to perform filtering</param>
        Returns:
            An enumerable of symbols passing the filter'''
        filtered = []

        for cf in coarse:
            if cf.Symbol not in self.averages:
                self.averages[cf.Symbol] = self.SelectionData(cf.Symbol, self.fastPeriod, self.slowPeriod)

            # grab th SelectionData instance for this symbol
            avg = self.averages.get(cf.Symbol)

            # Update returns true when the indicators are ready, so don't accept until they are
            # and only pick symbols who have their fastPeriod-day ema over their slowPeriod-day ema
            if avg.Update(cf.EndTime, cf.AdjustedPrice) and avg.Fast > avg.Slow * (1 + self.tolerance):
                filtered.append(avg)

        # prefer symbols with a larger delta by percentage between the two averages
        filtered = sorted(filtered, key=lambda avg: avg.ScaledDelta, reverse = True)

        # we only need to return the symbol and return 'universeCount' symbols
        return [x.Symbol for x in filtered[:self.universeCount]]

    # class used to improve readability of the coarse selection function
    class SelectionData:
        def __init__(self, symbol, fastPeriod, slowPeriod):
            self.Symbol = symbol
            self.FastEma = ExponentialMovingAverage(fastPeriod)
            self.SlowEma = ExponentialMovingAverage(slowPeriod)

        @property
        def Fast(self):
            return float(self.FastEma.Current.Value)

        @property
        def Slow(self):
            return float(self.SlowEma.Current.Value)

        # computes an object score of how much large the fast is than the slow
        @property
        def ScaledDelta(self):
            return (self.Fast - self.Slow) / ((self.Fast + self.Slow) / 2)

        # updates the EMAFast and EMASlow indicators, returning true when they're both ready
        def Update(self, time, value):
            return self.SlowEma.Update(time, value) & self.FastEma.Update(time, value)