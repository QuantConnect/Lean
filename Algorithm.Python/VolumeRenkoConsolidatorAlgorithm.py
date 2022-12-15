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
        self.SetStartDate(2020, 6, 1)
        self.SetEndDate(2020, 8, 1)
        self.SetCash(100000)
        
        # Requesting data
        self.spy = self.AddEquity("SPY", Resolution.Minute).Symbol
        self.consolidator = VolumeRenkoConsolidator(1000000)
        self.consolidator.DataConsolidated += \
            lambda sender, bar: self.Log(f"{bar.Time} to {bar.EndTime} :: O:{bar.Open} H:{bar.High} L:{bar.Low} C:{bar.Close} V:{bar.Volume}")
        self.SubscriptionManager.AddConsolidator("SPY", self.consolidator)