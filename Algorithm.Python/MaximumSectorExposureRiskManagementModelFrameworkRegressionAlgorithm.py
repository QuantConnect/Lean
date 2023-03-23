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
from BaseFrameworkRegressionAlgorithm import BaseFrameworkRegressionAlgorithm
from Risk.MaximumSectorExposureRiskManagementModel import MaximumSectorExposureRiskManagementModel

### <summary>
### Regression algorithm to assert the behavior of <see cref="MaximumSectorExposureRiskManagementModel"/>.
### </summary>
class MaximumSectorExposureRiskManagementModelFrameworkRegressionAlgorithm(BaseFrameworkRegressionAlgorithm):

    def Initialize(self):
        super().Initialize()
        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2014, 2, 1)  #Set Start Date
        self.SetEndDate(2014, 5, 1)    #Set End Date

        # set algorithm framework models
        tickers = [ "AAPL", "MSFT", "GOOG", "AIG", "BAC" ]
        self.SetUniverseSelection(FineFundamentalUniverseSelectionModel(
            lambda coarse: [ x.Symbol for x in coarse if x.Symbol.Value in tickers ],
            lambda fine: [ x.Symbol for x in fine ]
        ))

        # define risk management model such that maximum weight of a single sector be 10%
        # Number of of trades changed from 34 to 30 when using the MaximumSectorExposureRiskManagementModel
        self.SetRiskManagement(MaximumSectorExposureRiskManagementModel(0.1))

    def OnEndOfAlgorithm(self):
        pass
