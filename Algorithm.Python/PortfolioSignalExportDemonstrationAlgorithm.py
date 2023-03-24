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
from SignalExportDemonstrationAlgorithm import SignalExportDemonstrationAlgorithm

### <summary>
### This algorithm sends current portfolio targets from its Psortfolio to different 3rd party API's every time
### the ema indicators crosses between themselves.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class SignalExportDemonstrationAlgorithm(SignalExportDemonstrationAlgorithm):
    def SetInitialSignalValueForTargets(self):
        """ Set initial holding quantity for each symbol in self.symbols list """
        for symbol in self.symbols:
            self.SetHoldings(symbol, 0.05)

    def SetHoldingsToSpyAndSendSignals(self, quantity):
        """ Set Holdings to SPY and sends signals to the different 3rd party API's already defined """
        self.SetHoldings("SPY", quantity)
        self.SignalExport.SetTargetPortfolio(self)
            



