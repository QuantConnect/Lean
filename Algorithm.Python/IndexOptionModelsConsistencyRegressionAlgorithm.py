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

from OptionModelsConsistencyRegressionAlgorithm import OptionModelsConsistencyRegressionAlgorithm

### <summary>
### Regression algorithm asserting that when setting custom models for canonical index options, a one-time warning is sent
### informing the user that the contracts models are different (not the custom ones).
### </summary>
class IndexOptionModelsConsistencyRegressionAlgorithm(OptionModelsConsistencyRegressionAlgorithm):

    def InitializeAlgorithm(self) -> Security:
        self.SetStartDate(2021, 1, 4)
        self.SetEndDate(2021, 1, 5)

        index = self.AddIndex("SPX", Resolution.Minute)
        option = self.AddIndexOption(index.Symbol, "SPX", Resolution.Minute)
        option.SetFilter(lambda u: u.Strikes(-5, +5).Expiration(0, 360))

        return option
