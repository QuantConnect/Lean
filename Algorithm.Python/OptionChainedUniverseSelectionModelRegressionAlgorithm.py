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
### Regression algorithm to test the OptionChainedUniverseSelectionModel class
### </summary>
class OptionChainedUniverseSelectionModelRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.UniverseSettings.Resolution = Resolution.Daily
        self.SetStartDate(2014, 3, 22)
        self.SetEndDate(2014, 4, 7)
        self.SetCash(100000)
        
        self.AddSecurity(SecurityType.Equity, "GOOG", Resolution.Daily)
        universe = self.AddUniverse(lambda coarse: self.Selector(coarse))
        self.AddUniverseSelection(
            OptionChainedUniverseSelectionModel(
                universe,
                lambda option_filter_universe: option_filter_universe,
                self.UniverseSettings
            )
        )
    
    def OnEndOfAlgorithm(self):
        if not self.UniverseManager.ContainsKey("?GOOCV"):
            raise Exception("Option chain {?GOOCV} should have been in the universe but it was not")
        
        if not self.UniverseManager.ContainsKey("?GOOG"):
            raise Exception("Option chain {?GOOG} should have been in the universe but it was not")
        
        if not self.UniverseManager.ContainsKey("?GOOAV"):
            raise Exception("Option chain {?GOOAV} should have been in the universe but it was not")
    
    def Selector(self, coarse):
        result = []
        for c in coarse:
            sym = c.Symbol.Value
            if sym == "GOOG" or sym == "GOOCV" or sym == "GOOAV":
                result.append(c.Symbol)
        return result
            
