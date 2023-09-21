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
### Asserts that algorithms can be universe-only, that is, universe selection is performed even if the ETF security is not explicitly added.
### Reproduces https://github.com/QuantConnect/Lean/issues/7473
### </summary>
class UniverseOnlyRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2020, 12, 1)
        self.SetEndDate(2020, 12, 12)
        self.SetCash(100000)

        self.UniverseSettings.Resolution = Resolution.Daily

        # Add universe without a security added
        self.AddUniverse(self.Universe.ETF("GDVD", self.UniverseSettings, self.FilterUniverse))

        self.selection_done = False

    def FilterUniverse(self, constituents: List[ETFConstituentData]) -> List[Symbol]:
        self.selection_done = True
        return [x.Symbol for x in constituents]

    def OnEndOfAlgorithm(self):
        if not self.selection_done:
            raise Exception("Universe selection was not performed")
