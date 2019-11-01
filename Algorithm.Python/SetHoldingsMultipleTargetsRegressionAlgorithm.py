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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework.Portfolio import *

### <summary>
### Regression algorithm testing GH feature 3790, using SetHoldings with a collection of targets
### which will be ordered by margin impact before being executed, with the objective of avoiding any
### margin errors
### </summary>
class SetHoldingsMultipleTargetsRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)

        # use leverage 1 so we test the margin impact ordering
        self._spy = self.AddEquity("SPY", Resolution.Minute, Market.USA, False, 1).Symbol
        self._ibm = self.AddEquity("IBM", Resolution.Minute, Market.USA, False, 1).Symbol

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings([PortfolioTarget(self._spy, 0.8), PortfolioTarget(self._ibm, 0.2)])
        else:
            self.SetHoldings([PortfolioTarget(self._ibm, 0.8), PortfolioTarget(self._spy, 0.2)])