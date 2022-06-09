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
from datetime import timedelta

### <summary>
### Regression algorithm excersizing an equity covered American style option, using an option price model
### that supports American style options and asserting that the option price model is used.
### </summary>
class OptionPriceModelForUnsupportedAmericanOptionRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2015, 12, 23)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(100000)

        index = self.AddEquity("GOOG", Resolution.Minute)
        index.SetDataNormalizationMode(DataNormalizationMode.Raw)
        option = self.AddOption("GOOG", Resolution.Minute)
        # BlackSholes model does not support American style options
        option.PriceModel = OptionPriceModels.BlackScholes()
        self._optionSymbol = option.Symbol

        self.SetWarmUp(timedelta(10))

        self._showGreeks = True
        self._triedGreeksCalculation = False

    def OnData(self, slice):
        if self.IsWarmingUp: return

        for kvp in slice.OptionChains:
            if kvp.Key != self._optionSymbol: continue

            chain = kvp.Value
            contracts = [contract for contract in chain if contract.Right == OptionRight.Call]

            if len(contracts) == 0: return

            if self._showGreeks:
                self._showGreeks = False
                self._triedGreeksCalculation = True

                for contract in contracts:
                    try:
                        greeks = contract.Greeks
                        raise Exception(f'Expected greeks not to be calculated for {contract.Symbol.Value}, an American style option, using an option price model that does not support them, but they were');
                    except ArgumentException:
                        # Expected
                        pass

    def OnEndOfDay(self, symbol):
        self._showGreeks = True

    def OnEndOfAlgorithm(self):
        if not self._triedGreeksCalculation:
            raise Exception("Expected greeks to be calculated")
