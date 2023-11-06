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
### Regression algorithm asserting that using separate coarse & fine selection with async universe settings is not allowed
### </summary>
class CoarseFineAsyncUniverseRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)

        self.UniverseSettings.Asynchronous = True

        threw_exception = False
        try:
            self.AddUniverse(self.CoarseSelectionFunction, self.FineSelectionFunction)
        except:
            # expected
            threw_exception = True
            pass

        if not threw_exception:
            raise ValueError("Expected exception to be thrown for AddUniverse")

        self.SetUniverseSelection(FineFundamentalUniverseSelectionModel(self.CoarseSelectionFunction, self.FineSelectionFunction))

    def CoarseSelectionFunction(self, coarse):
        return []

    def FineSelectionFunction(self, fine):
        return []
