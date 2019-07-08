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
from QuantConnect.Data.Custom.SEC import *

from datetime import datetime
import json

### <summary>
### Demonstration algorithm showing how to use and access SEC data
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="regression test" />
### <meta name="tag" content="SEC" />
### <meta name="tag" content="rename event" />
### <meta name="tag" content="map" />
### <meta name="tag" content="mapping" />
### <meta name="tag" content="map files" />
class CustomDataUsingMapFileRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        self.SetStartDate(2001, 1, 1)
        self.SetEndDate(2003, 12, 31)
        self.SetCash(100000)

        self.tickers = {}

        self.ticker = "TWX"
        self.symbol = self.AddData(SECReport8K, self.ticker).Symbol
        self.AddEquity(self.ticker, Resolution.Daily)

    def OnData(self, slice):
        if slice.SymbolChangedEvents.ContainsKey(self.symbol):
            self.changed_symbol = True
            self.Log("{0} - Ticker changed from: {1} to {2}".format(str(self.Time), slice.SymbolChangedEvents[self.symbol].OldSymbol, slice.SymbolChangedEvents[self.symbol].NewSymbol))

        if not slice.ContainsKey(self.symbol):
            return

        data = slice[self.symbol]

        if not isinstance(data, SECReport8K):
            return

        report = data.Report
        ticker = data.Symbol.Value
        date = self.Time.date()

        if date == datetime(2001, 1, 26).date() or date == datetime(2003, 10, 22).date():
            self.tickers[str(date)] = ticker

        self.Log(f"{str(self.Time)} - Received 8-K report for {data.Symbol.Value}")

    def OnEndOfAlgorithm(self):
        if not self.changed_symbol:
            raise Exception("The ticker did not rename throughout the course of its life even though it should have")

        expected_tickers = {}
        expected_tickers[str(datetime(2001, 1, 26).date())] = "AOL"
        expected_tickers[str(datetime(2003, 10, 22).date())] = "TWX"

        # Check for dict equality: https://stackoverflow.com/a/4527978
        if not all([k in self.tickers and expected_tickers[k] == self.tickers[k] for k in expected_tickers]):
            self.Log(f"Found: {json.dumps(self.tickers)}")
            self.Log(f"Expected: {json.dumps(expected_tickers)}")
            raise Exception("SEC data event tickers do not match test case")



