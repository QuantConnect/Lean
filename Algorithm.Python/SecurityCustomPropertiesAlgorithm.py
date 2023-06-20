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
### Demonstration of how to use custom security properties.
### In this algorithm we trade a security based on the values of a slow and fast EMAs which are stored in the security itself.
### </summary>
class SecurityCustomPropertiesAlgorithm(QCAlgorithm):
    '''Demonstration of how to use custom security properties.
    In this algorithm we trade a security based on the values of a slow and fast EMAs which are stored in the security itself.'''

    def Initialize(self):
        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        self.spy = self.AddEquity("SPY", Resolution.Minute)

        # Using the dynamic interface to store our indicator as a custom property.
        self.spy.SlowEma = self.EMA(self.spy.Symbol, 30, Resolution.Minute)

        # Using the generic interface to store our indicator as a custom property.
        self.spy.Set("FastEma", self.EMA(self.spy.Symbol, 60, Resolution.Minute))

    def OnData(self, data):
        if not self.Portfolio.Invested:
            if self.spy.SlowEma > self.spy.Get[IndicatorBase]("FastEma"):
                self.SetHoldings(self.spy.Symbol, 1)
        else:
            if self.spy.SlowEma < self.spy.Get[IndicatorBase]("FastEma"):
                self.Liquidate(self.spy.Symbol)
