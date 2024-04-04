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
### Example algorithm using and asserting the behavior of auxiliary data handlers
### </summary>
class AuxiliaryDataHandlersRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2007, 5, 16)
        self.SetEndDate(2008, 10, 1)

        # will get delisted
        self.AddEquity("AAA.1", Resolution.Daily)

        # get's remapped
        self.AddEquity("SPWR", Resolution.Daily)

        # emits dividends
        self.AddEquity("SPY", Resolution.Daily)

    def OnDelistings(self, delistings: Delistings):
        self._onDelistingsCalled = True

    def OnSymbolChangedEvents(self, symbolsChanged: SymbolChangedEvents):
        self._onSymbolChangedEvents = True

    def OnPriceDiscontinuity(self, splits: Splits, dividends: Dividends):
        self._onPriceDiscontinuity = True

    def OnEndOfAlgorithm(self):
        if not self._onDelistingsCalled:
            raise ValueError("OnDelistings was not called!")
        if not self._onSymbolChangedEvents:
            raise ValueError("OnSymbolChangedEvents was not called!")
        if not self._onPriceDiscontinuity:
            raise ValueError("OnPriceDiscontinuity was not called!")
