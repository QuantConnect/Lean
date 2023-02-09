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
### Demostrates the use of <see cref="VolumeRenkoConsolidator"/> for creating constant volume bar
### </summary>
### <meta name="tag" content="renko" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="consolidating data" />
class VolumeRenkoConsolidatorAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)

        self.sma = SimpleMovingAverage(10)
        self.tick_consolidated = False

        self.spy = self.AddEquity("SPY", Resolution.Minute).Symbol
        self.tradebar_volume_consolidator = VolumeRenkoConsolidator(1000000)
        self.tradebar_volume_consolidator.DataConsolidated += self.OnSPYDataConsolidated

        self.ibm = self.AddEquity("IBM", Resolution.Tick).Symbol
        self.tick_volume_consolidator = VolumeRenkoConsolidator(1000000)
        self.tick_volume_consolidator.DataConsolidated += self.OnIBMDataConsolidated

        history = self.History[TradeBar](self.spy, 1000, Resolution.Minute);
        for bar in history:
            self.tradebar_volume_consolidator.Update(bar)

    def OnSPYDataConsolidated(self, sender, bar):
        self.sma.Update(bar.EndTime, bar.Value)
        self.Debug(f"SPY {bar.Time} to {bar.EndTime} :: O:{bar.Open} H:{bar.High} L:{bar.Low} C:{bar.Close} V:{bar.Volume}")
        if bar.Volume != 1000000:
            raise Exception("Volume of consolidated bar does not match set value!")

    def OnIBMDataConsolidated(self, sender, bar):
        self.Debug(f"IBM {bar.Time} to {bar.EndTime} :: O:{bar.Open} H:{bar.High} L:{bar.Low} C:{bar.Close} V:{bar.Volume}")
        if bar.Volume != 1000000:
            raise Exception("Volume of consolidated bar does not match set value!")
        self.tick_consolidated = True

    def OnData(self, slice):
        # Update by TradeBar
        if slice.Bars.ContainsKey(self.spy):
            self.tradebar_volume_consolidator.Update(slice.Bars[self.spy])

        # Update by Tick
        if slice.Ticks.ContainsKey(self.ibm):
            for tick in slice.Ticks[self.ibm]:
                self.tick_volume_consolidator.Update(tick)

        if self.sma.IsReady and self.sma.Current.Value < self.Securities[self.spy].Price:
            self.SetHoldings(self.spy, 1)
        else:
            self.SetHoldings(self.spy, 0)
            
    def OnEndOfAlgorithm(self):
        if not self.tick_consolidated:
            raise Exception("Tick consolidator was never been called")