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
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)

        self._option = self.AddOption("GOOG", Resolution.Minute)
        # BlackSholes model does not support American style options
        self._option.PriceModel = OptionPriceModels.BlackScholes()

        self.SetWarmup(1, Resolution.Daily)

        self._optionStyle = OptionStyle.American
        self._optionStyleIsSupported = False
        self._triedGreeksCalculation = False
