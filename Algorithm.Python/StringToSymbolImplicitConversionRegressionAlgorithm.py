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

### <summary>
### Related to GH issue 4275, reproduces a failed string to symbol implicit conversion asserting the exception
### thrown contains the used ticker
### </summary>
class StringToSymbolImplicitConversionRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10, 8)

        self.AddEquity("SPY", Resolution.Minute)

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        try:
            self.MarketOrder("PEPE", 1)
        except Exception as exception:
            if "This asset symbol (PEPE 0) was not found in your security list" in str(exception) and not self.Portfolio.Invested:
                self.SetHoldings("SPY", 1)