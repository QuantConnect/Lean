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
### Regrssion algorithm to assert we can update indicators that inherit from IndicatorBase<TradeBar> with RenkoBar's
### </summary>
### <meta name="tag" content="renko" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="consolidating data" />
class IndicatorWithRenkoBarsRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 9)

        self.AddEquity("SPY")
        self.AddEquity("AIG")

        spyRenkoConsolidator = RenkoConsolidator(0.1)
        spyRenkoConsolidator.DataConsolidated += self.OnSPYDataConsolidated

        aigRenkoConsolidator = RenkoConsolidator(0.05)
        aigRenkoConsolidator.DataConsolidated += self.OnAIGDataConsolidated

        self.SubscriptionManager.AddConsolidator("SPY", spyRenkoConsolidator)
        self.SubscriptionManager.AddConsolidator("AIG", aigRenkoConsolidator)

        self.mi = MassIndex("MassIndex", 9, 25)
        self.wasi = WilderAccumulativeSwingIndex("WilderAccumulativeSwingIndex", 8)
        self.wsi = WilderSwingIndex("WilderSwingIndex", 8)
        self.b = Beta("Beta", 3, "AIG", "SPY")
        self.indicators = [self.mi, self.wasi, self.wsi, self.b]

    def OnSPYDataConsolidated(self, sender, renkoBar):
        self.mi.Update(renkoBar)
        self.wasi.Update(renkoBar)
        self.wsi.Update(renkoBar)
        self.b.Update(renkoBar)

    def OnAIGDataConsolidated(self, sender, renkoBar):
        self.b.Update(renkoBar)

    def OnEndOfAlgorithm(self):
        for indicator in self.indicators:
            if not indicator.IsReady:
                raise Exception(f"{indicator.Name} indicator should be ready")
            elif indicator.Current.Value == 0:
                raise Exception(f"The current value of the {indicator.Name} indicator should be different than zero")
