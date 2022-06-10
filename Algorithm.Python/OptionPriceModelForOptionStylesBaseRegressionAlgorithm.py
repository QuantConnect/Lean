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
### Base regression algorithm excersizing for exercising different style options with option price models that migth
### or might not support them. Also, if the option style is supported, greeks are asserted to be accesible and have valid values.
### </summary>
class OptionPriceModelForOptionStylesBaseRegressionAlgorithm(QCAlgorithm):
    def __init__(self):
        super().__init__()
        self._optionStyleIsSupported = False
        self._triedGreeksCalculation = False
        self._option = None
        self._contracts = None

    def OnData(self, slice):
        if self.IsWarmingUp: return

        for kvp in slice.OptionChains:
            if self._option is None or kvp.Key != self._option.Symbol: continue

            self._contracts = [contract for contract in kvp.Value]

    def OnEndOfDay(self, symbol):
        if not self.IsWarmingUp:
            self.CheckGreeks()

    def OnEndOfAlgorithm(self):
        if not self._triedGreeksCalculation:
            raise Exception("Expected greeks to be accessed")

    def Init(self, option, optionStyleIsSupported):
        self._option = option
        self._optionStyleIsSupported = optionStyleIsSupported
        self._triedGreeksCalculation = False

    def CheckGreeks(self):
        if self._contracts is None or len(self._contracts) == 0: return

        self._triedGreeksCalculation = True

        for contract in self._contracts:
            greeks = Greeks()
            try:
                greeks = contract.Greeks

                # Greeks should have not been successfully accessed if the option style is not supported
                if not self._optionStyleIsSupported:
                    raise Exception(f'Expected greeks not to be calculated for {contract.Symbol.Value}, an {self._option.Style} style option, using {type(self._option.PriceModel).__name__}, which does not support them, but they were')
            except ArgumentException:
                # ArgumentException is only expected if the option style is not supported
                raise Exception(f'Expected greeks to be calculated for {contract.Symbol.Value}, an {self._option.Style} style option, using {type(self._option.PriceModel).__name__}, which supports them, but they were not')

            # Greeks shpould be valid if they were successfuly accessed for supported option style
            if (self._optionStyleIsSupported
                and ((contract.Right == OptionRight.Call and (greeks.Delta < 0.0 or greeks.Delta > 1.0 or greeks.Rho <= 0.0))
                    or (contract.Right == OptionRight.Put and (greeks.Delta < -1.0 or greeks.Delta > 0.0 or greeks.Rho >= 0.0))
                    or greeks.Theta >= 0.0 or greeks.Vega <= 0.0)):
                raise Exception(f'Expected greeks to have valid values. Greeks were: Gamma: {greeks.Gamma}, Rho: {greeks.Rho}, Delta: {greeks.Delta}, Vega: {greeks.Vega}')



