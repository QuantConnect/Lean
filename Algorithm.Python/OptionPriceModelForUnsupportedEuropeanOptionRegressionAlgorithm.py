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
from OptionPriceModelForOptionStylesBaseRegressionAlgorithm import OptionPriceModelForOptionStylesBaseRegressionAlgorithm

### <summary>
### Regression algorithm exercising an equity covered European style option, using an option price model
### that does not support European style options and asserting that the option price model is not used.
### </summary>
class OptionPriceModelForUnsupportedEuropeanOptionRegressionAlgorithm(OptionPriceModelForOptionStylesBaseRegressionAlgorithm):
    def initialize(self):
        self.set_start_date(2021, 1, 14)
        self.set_end_date(2021, 1, 14)

        option = self.add_index_option("SPX", Resolution.HOUR)
        # BaroneAdesiWhaley model does not support European style options
        option.price_model = OptionPriceModels.barone_adesi_whaley()

        self.set_warmup(7, Resolution.DAILY)

        self.init(option, option_style_is_supported=False)
