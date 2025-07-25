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

### <summary>
### This regression algorithm tests the different overloads of the Consolidate method
### using RenkoBar, VolumeRenkoBar, and RangeBar types.
### It verifies that each overload functions correctly when applied to these bar types,
### </summary>
class ConsolidateWithNonStandardBarTypesRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)
        self.set_cash(100000)
        self.add_equity("SPY", Resolution.TICK)
        self.sma_indicators = [SimpleMovingAverage("RenkoBarSMA", 10), SimpleMovingAverage("VolumeRenkoBarSMA", 10), SimpleMovingAverage("RangeBarSMA", 10)]
        self.consolidate(RenkoBar, "SPY", 0.1, TickType.TRADE, lambda bar: self.update_with_renko_bar(bar, 0))
        self.consolidate(VolumeRenkoBar, "SPY", 10000, TickType.TRADE, lambda bar: self.update_with_volume_renko_bar(bar, 1))
        self.consolidate(RangeBar, "SPY", 12, TickType.TRADE, lambda bar: self.update_with_range_bar(bar, 2))
    
    def update_with_renko_bar(self, bar, position):
        self.sma_indicators[position].update(bar.end_time, bar.high)

    def update_with_volume_renko_bar(self, bar, position):
        self.sma_indicators[position].update(bar.end_time, bar.high)

    def update_with_range_bar(self, bar, position):
        self.sma_indicators[position].update(bar.end_time, bar.high)

    def on_end_of_algorithm(self):
        # Verifies that each SMA was updated and is ready, confirming the Consolidate overloads functioned correctly.
        for sma in self.sma_indicators:
            if (sma.samples == 0):
                raise AssertionError(f'The indicator {sma.name} did not receive any updates. This indicates the associated consolidator was not triggered.')
            if (not sma.is_ready):
                raise AssertionError(f'The indicator {sma.name} is not ready. It received only {sma.samples} samples, but requires at least {sma.period} to be ready.')
