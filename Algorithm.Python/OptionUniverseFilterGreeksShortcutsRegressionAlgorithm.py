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

from OptionUniverseFilterGreeksRegressionAlgorithm import OptionUniverseFilterGreeksRegressionAlgorithm

### <summary>
### Regression algorithm demonstrating the option universe filter by greeks and other options data feature
### </summary>
class OptionUniverseFilterGreeksShortcutsRegressionAlgorithm(OptionUniverseFilterGreeksRegressionAlgorithm):

    def set_option_filter(self, security: Option) -> None:
        # Contracts can be filtered by greeks, implied volatility, open interest:
        security.set_filter(lambda u: u
            .strikes(-3, +3)
            .expiration(0, 180)
            .d(0.64, 0.65)
            .g(0.0008, 0.0010)
            .v(7.5, 10.5)
            .t(-1.10, -0.50)
            .r(4, 10)
            .iv(0.10, 0.20)
            .oi(100, 1000))
