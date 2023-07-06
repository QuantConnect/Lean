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

        self.AddEquity("SPY");

        renkoConsolidator = RenkoConsolidator(0.1)
        renkoConsolidator.DataConsolidated += self.OnDataConsolidated;

        self.SubscriptionManager.AddConsolidator("SPY", renkoConsolidator)
        self.mi = MassIndex("MI", 9, 25)
        self.wasi = WilderAccumulativeSwingIndex(8)
        self.wsi = WilderSwingIndex(8)

    def OnDataConsolidated(self, sender, renkoBar):
        self.mi.Update(renkoBar)
        self.wasi.Update(renkoBar)
        self.wsi.Update(renkoBar)

    def OnEndOfAlgorithm(self):
        if not self.mi.IsReady:
            raise Exception("Mass Index indicator should be ready")
        elif self.mi.Current.Value == 0:
            raise Exception("The current value of the Mass Index indicator should be different than zero")

        if not self.wasi.IsReady:
            raise Exception("WilderAccumulativeSwingIndex indicator should be ready")
        elif self.wasi.Current.Value == 0:
            raise Exception("The current value of the WilderAccumulativeSwingIndex indicator should be different than zero")

        if not self.wsi.IsReady:
            raise Exception("WilderSwingIndex indicator should be ready")
        if self.wsi.Current.Value == 0:
            raise Exception("The current value of the WilderSwingIndex indicator should be different than zeros")
