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
### Base regression algorithm exercising different style options with option price models that might
### or might not support them. Also, if the option style is supported, greeks are asserted to be accesible and have valid values.
### </summary>
class OptionPriceModelForOptionStylesBaseRegressionAlgorithm(QCAlgorithm):
    def __init__(self):
        super().__init__()
        self._optionStyleIsSupported = False
        self._checkGreeks = True
        self._triedGreeksCalculation = False
        self._option = None

    def OnData(self, slice):
        if self.IsWarmingUp: return

        for kvp in slice.OptionChains:
            if self._option is None or kvp.Key != self._option.Symbol: continue

            self.CheckGreeks([contract for contract in kvp.Value])

    def OnEndOfDay(self, symbol):
        self._checkGreeks = True

    def OnEndOfAlgorithm(self):
        if not self._triedGreeksCalculation:
            raise Exception("Expected greeks to be accessed")

    def Init(self, option, optionStyleIsSupported):
        self._option = option
        self._optionStyleIsSupported = optionStyleIsSupported
        self._checkGreeks = True
        self._triedGreeksCalculation = False

    def CheckGreeks(self, contracts):
        if not self._checkGreeks or len(contracts) == 0: return

        self._checkGreeks = False
        self._triedGreeksCalculation = True

        for contract in contracts:
            greeks = Greeks()
            try:
                greeks = contract.Greeks

                # Greeks should have not been successfully accessed if the option style is not supported
                optionStyleStr = 'American' if self._option.Style == OptionStyle.American else 'European'
                if not self._optionStyleIsSupported:
                    raise Exception(f'Expected greeks not to be calculated for {contract.Symbol.Value}, an {optionStyleStr} style option, using {type(self._option.PriceModel).__name__}, which does not support them, but they were')
            except ArgumentException:
                # ArgumentException is only expected if the option style is not supported
                if self._optionStyleIsSupported:
                    raise Exception(f'Expected greeks to be calculated for {contract.Symbol.Value}, an {optionStyleStr} style option, using {type(self._option.PriceModel).__name__}, which supports them, but they were not')

            # Greeks should be valid if they were successfuly accessed for supported option style
            # Delta can be {-1, 0, 1} if the price is too wild, rho can be 0 if risk free rate is 0
            # Vega can be 0 if the price is very off from theoretical price, Gamma = 0 if Delta belongs to {-1, 1}
            if (self._optionStyleIsSupported
                and ((contract.Right == OptionRight.Call and (greeks.Delta < 0.0 or greeks.Delta > 1.0 or greeks.Rho < 0.0))
                    or (contract.Right == OptionRight.Put and (greeks.Delta < -1.0 or greeks.Delta > 0.0 or greeks.Rho > 0.0))
                    or greeks.Theta == 0.0 or greeks.Vega < 0.0 or greeks.Gamma < 0.0)):
                raise Exception(f'Expected greeks to have valid values. Greeks were: Delta: {greeks.Delta}, Rho: {greeks.Rho}, Theta: {greeks.Theta}, Vega: {greeks.Vega}, Gamma: {greeks.Gamma}')



