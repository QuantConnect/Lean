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

        self.SetStartDate(2004, 5, 20)
        self.SetEndDate(2016, 7, 26)  
        _symbol = AddEquity("3MINDIA", Resolution.Daily, Market.India).Symbol

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''

        # dividend
        if data.Dividends.ContainsKey("3MINDIA"):
            dividend = data["3MINDIA"];
            if dividend.Price != 645.5700 or (dividend.ReferencePrice != 645.5700) or (dividend.Distribution != 645.5700):
                raise Exception("Did not receive expected price values")

        # split
        if data.Splits.ContainsKey("3MINDIA"):
            split = data["3MINDIA"];
            if split.Type == SplitType.Warning:
                _receivedWarningEvent = True
            elif split.Type == SplitType.SplitOccurred:
                _receivedOccurredEvent = True
                if dividend.Price != 645.5700 or (dividend.ReferencePrice != 645.5700) or (dividend.Distribution != 645.5700):
                    raise Exception("Did not receive expected price values")
        
        # mapping
        if data.SymbolChangedEvents.ContainsKey(_symbol):
                mappingEvent = [x.value for x in data.SymbolChangedEvents if x.Key.SecurityType == SecurityType.Equity][0]
                if Time.Date == DateTime(1999, 1, 1):
                    _initialMapping = True
                    raise Exception(f"Unexpected mapping event {mappingEvent}")
                elif Time.Date == DateTime(2004, 6, 15):
                    if mappingEvent.NewSymbol != "3MINDIA" or mappingEvent.OldSymbol != "BIRLA3M":
                        raise Exception(f"Unexpected mapping event {mappingEvent}")
                    _executionMapping = True

    
    def OnEndOfAlgorithm(self, orderEvent):
        if _initialMapping:
            raise Exception("The ticker generated the initial rename event")

        if not _executionMapping:
            raise Exception("The ticker did not rename throughout the course of its life even though it should have")
