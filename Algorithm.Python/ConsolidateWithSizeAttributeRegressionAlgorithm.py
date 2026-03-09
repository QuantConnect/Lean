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
### This regression algorithm tests various overloads of the Consolidate method
### using RenkoBar, VolumeRenkoBar, and RangeBar types,
### as well as common bars like TradeBar and QuoteBar with a maxCount parameter.
### It ensures each overload behaves as expected and that the appropriate consolidator instances are correctly created and triggered.
### </summary>
class ConsolidateWithSizeAttributeRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)
        self.set_cash(100000)
        self.add_equity("SPY", Resolution.MINUTE)
        self.sma_indicators = [
            SimpleMovingAverage("RenkoBarSMA", 10),
            SimpleMovingAverage("VolumeRenkoBarSMA", 10),
            SimpleMovingAverage("RangeBarSMA", 10),
            SimpleMovingAverage("TradeBarSMA", 10),
            SimpleMovingAverage("QuoteBarSMA", 10),
            SimpleMovingAverage("BaseDataSMA", 10)
        ]
        self.consolidators = [
            self.consolidate(RenkoBar, "SPY", 0.1, None, lambda bar: self.update_with_renko_bar(bar, 0)),
            self.consolidate(VolumeRenkoBar, "SPY", 10000, None, lambda bar: self.update_with_volume_renko_bar(bar, 1)),
            self.consolidate(RangeBar, "SPY", 12, None, lambda bar: self.update_with_range_bar(bar, 2)),
            
            # Trade and Quote consolidators with max count
            self.consolidate(TradeBar, "SPY", 10, None, lambda bar: self.update_with_trade_bar(bar, 3)),
            self.consolidate(QuoteBar, "SPY", 10, None, lambda bar: self.update_with_quote_bar(bar, 4)),
            self.consolidate(BaseData, "SPY", 10, None, lambda bar: self.update_with_base_data(bar, 5))
        ]
    
    def update_with_base_data(self, base_data, position):
        self.sma_indicators[position].update(base_data.end_time, base_data.value)
        if type(base_data) != TradeBar:
            raise AssertionError(f"The type of the bar should be Trade, but was {type(base_data)}")
    
    def update_with_trade_bar(self, trade_bar, position):
        self.sma_indicators[position].update(trade_bar.end_time, trade_bar.high)
        if type(trade_bar) != TradeBar:
            raise AssertionError(f"The type of the bar should be Trade, but was {type(trade_bar)}")

    def update_with_quote_bar(self, quote_bar, position):
        self.sma_indicators[position].update(quote_bar.end_time, quote_bar.high)
        if type(quote_bar) != QuoteBar:
            raise AssertionError(f"The type of the bar should be Quote, but was {type(quote_bar)}")

    def update_with_renko_bar(self, renko_bar, position):
        self.sma_indicators[position].update(renko_bar.end_time, renko_bar.high)
        if type(renko_bar) != RenkoBar:
            raise AssertionError(f"The type of the bar should be Renko, but was {type(renko_bar)}")

    def update_with_volume_renko_bar(self, volume_renko_bar, position):
        self.sma_indicators[position].update(volume_renko_bar.end_time, volume_renko_bar.high)
        if type(volume_renko_bar) != VolumeRenkoBar:
            raise AssertionError(f"The type of the bar should be VolumeRenko, but was {type(volume_renko_bar)}")

    def update_with_range_bar(self, range_bar, position):
        self.sma_indicators[position].update(range_bar.end_time, range_bar.high)
        if type(range_bar) != RangeBar:
            raise AssertionError(f"The type of the bar should be Range, but was {type(range_bar)}")

    def on_end_of_algorithm(self):
        # Verifies that each SMA was updated and is ready, confirming the Consolidate overloads functioned correctly.
        for sma in self.sma_indicators:
            if (sma.samples == 0):
                raise AssertionError(f'The indicator {sma.name} did not receive any updates. This indicates the associated consolidator was not triggered.')
            if (not sma.is_ready):
                raise AssertionError(f'The indicator {sma.name} is not ready. It received only {sma.samples} samples, but requires at least {sma.period} to be ready.')
        
        expected_consolidator_types = [
            RenkoConsolidator,
            VolumeRenkoConsolidator,
            RangeConsolidator,
            TradeBarConsolidator,
            QuoteBarConsolidator,
            BaseDataConsolidator
        ]

        for i in range(len(self.consolidators)):
            consolidator = self.consolidators[i]
            if type(consolidator) != expected_consolidator_types[i]:
                raise AssertionError(f"Expected consolidator type {expected_consolidator_types[i]} but received {type(consolidator)}")
