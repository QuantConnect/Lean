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
### Regression algorithm which tests a fine fundamental filtered universe,
### related to GH issue 4127
### </summary>
class FineFundamentalFilteredUniverseRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2014, 10, 8)
        self.SetEndDate(2014, 10, 13)

        self.UniverseSettings.Resolution = Resolution.Daily

        symbol = Symbol(SecurityIdentifier.GenerateConstituentIdentifier("constituents-universe-qctest", SecurityType.Equity, Market.USA), "constituents-universe-qctest")
        self.AddUniverse(ConstituentsUniverse(symbol, self.UniverseSettings), self.FineSelectionFunction)

    def FineSelectionFunction(self, fine):
        return [ x.Symbol for x in fine if x.CompanyProfile != None and x.CompanyProfile.HeadquarterCity != None and x.CompanyProfile.HeadquarterCity.lower() == "cupertino" ]

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            if data.Keys[0].Value != "AAPL":
                raise ValueError(f"Unexpected symbol was added to the universe: {data.Keys[0]}")
            self.SetHoldings(data.Keys[0], 1)
