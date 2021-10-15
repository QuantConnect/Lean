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
### Regression algorithm reproducing GH issue #5921. Asserting a security can be warmup correctly on initialize
### </summary>
class SecuritySeederRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10, 8)
        self.SetEndDate(2013,10,10)

        self.SetSecurityInitializer(BrokerageModelSecurityInitializer(self.BrokerageModel,
                                                                      FuncSecuritySeeder(self.GetLastKnownPrices)))
        self.AddEquity("SPY", Resolution.Minute)

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

    def OnSecuritiesChanged(self, changes):
        for addedSecurity in changes.AddedSecurities:
            if not addedSecurity.HasData \
                or addedSecurity.AskPrice == 0 \
                or addedSecurity.BidPrice == 0 \
                or addedSecurity.BidSize == 0 \
                or addedSecurity.AskSize == 0 \
                or addedSecurity.Price == 0 \
                or addedSecurity.Volume == 0 \
                or addedSecurity.High == 0 \
                or addedSecurity.Low == 0 \
                or addedSecurity.Open == 0 \
                or addedSecurity.Close == 0:
                raise ValueError(f"Security {addedSecurity.Symbol} was not warmed up!")
