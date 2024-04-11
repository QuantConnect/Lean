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
### Basic template framework algorithm uses framework components to define the algorithm.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class IndiaDataRegressionAlgorithm(QCAlgorithm):
    '''Basic template framework algorithm uses framework components to define the algorithm.'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetAccountCurrency("INR")
        self.SetStartDate(2004, 5, 20)
        self.SetEndDate(2016, 7, 26) 
        self._mappingSymbol = self.AddEquity("3MINDIA", Resolution.Daily, Market.India).Symbol
        self._splitAndDividendSymbol = self.AddEquity("CCCL", Resolution.Daily, Market.India).Symbol
        self._receivedWarningEvent = False
        self._receivedOccurredEvent = False
        self._initialMapping = False
        self._executionMapping = False
        self.Debug("numpy test >>> print numpy.pi: " + str(np.pi))

    def OnDividends(self, dividends: Dividends):
        if dividends.ContainsKey(self._splitAndDividendSymbol):
            dividend = dividends[self._splitAndDividendSymbol]
            if ((self.Time.year == 2010 and self.Time.month == 6 and self.Time.day == 15) and
                    (dividend.Price != 0.5 or dividend.ReferencePrice != 88.8 or dividend.Distribution != 0.5)):
                raise Exception("Did not receive expected dividend values")

    def OnSplits(self, splits: Splits):
        if splits.ContainsKey(self._splitAndDividendSymbol):
            split = splits[self._splitAndDividendSymbol]
            if split.Type == SplitType.Warning:
                self._receivedWarningEvent = True
            elif split.Type == SplitType.SplitOccurred:
                self._receivedOccurredEvent = True
                if split.Price != 421.0 or split.ReferencePrice != 421.0 or split.SplitFactor != 0.2:
                    raise Exception("Did not receive expected price values")

    def OnSymbolChangedEvents(self, symbolsChanged: SymbolChangedEvents):
        if symbolsChanged.ContainsKey(self._mappingSymbol):
                mappingEvent = [x.Value for x in symbolsChanged if x.Key.SecurityType == 1][0]
                if self.Time.year == 1999 and self.Time.month == 1 and self.Time.day == 1:
                    self._initialMapping = True
                elif self.Time.year == 2004 and self.Time.month == 6 and self.Time.day == 15:
                    if mappingEvent.NewSymbol == "3MINDIA" and mappingEvent.OldSymbol == "BIRLA3M":
                        self._executionMapping = True
    
    def OnEndOfAlgorithm(self):
        if self._initialMapping:
            raise Exception("The ticker generated the initial rename event")

        if not self._executionMapping:
            raise Exception("The ticker did not rename throughout the course of its life even though it should have")
        
        if not self._receivedOccurredEvent:
            raise Exception("Did not receive expected split event")

        if not self._receivedWarningEvent:
            raise Exception("Did not receive expected split warning event")
