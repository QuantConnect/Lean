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
### Demostrates the use of <see cref="RangeBarConsolidator"/> for creating range bars
### </summary>
### <meta name="tag" content="range bar" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="consolidating data" />
class RangeBarConsolidatorAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)

        self.security = self.AddEquity("IBM", Resolution.Tick)
        self.price_variation = self.security.SymbolProperties.MinimumPriceVariation
        self.ibm = self.security.Symbol

        length = 10 * self.price_variation
        self.range_bar_tick_consolidator = RangeBarConsolidator(length)
        self.range_bar_tick_consolidator.DataConsolidated += self.RangeBarConsolidatorOnDataConsolidated

    def RangeBarConsolidatorOnDataConsolidated(self, sender, bar):
        bar_length = bar.High - bar.Low
        if bar_length > 10:
            raise Exception("The length of consolidated range bar exceeds set value!")

    def OnData(self, slice):
        if slice.Ticks.ContainsKey(self.ibm):
            for tick in slice.Ticks[self.ibm]:
                self.range_bar_tick_consolidator.Update(tick)
       
        
   
