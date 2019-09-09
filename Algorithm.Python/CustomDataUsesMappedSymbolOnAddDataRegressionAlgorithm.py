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
AddReference("System.Core")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

# Store the Exception type so that we can handle all python-related failures with the Exception base class
PythonError = Exception

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Custom.SEC import *
from QuantConnect.Data.Fundamental import *
from QuantConnect.Data.UniverseSelection import *
from datetime import datetime

### <summary>
### Adds stock TWX from Universe and add to custom data subscription to link custom data -> equity via underlying
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="regression test" />
### <meta name="tag" content="rename event" />
### <meta name="tag" content="map" />
### <meta name="tag" content="mapping" />
### <meta name="tag" content="map files" />
class CustomDataUsesMappedSymbolOnAddDataRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        # We set the end date to 2015 so that we have the chance to load the "AOL" ticker that began trading on
        # 2009-12-10. With this way, we can know for certain that we're applying a mapped symbol to custom data
        # instead of assuming that we're supplying the last known ticker to AddData. So, instead of mapping AOL
        # from 2009-12-10 to 2015-06-24, we're actually subscribing to TWX's previous ticker, not the new AOL
        self.SetStartDate(2003, 10, 14)
        self.SetEndDate(2009, 12, 31)
        self.SetCash(100000)

        self.addedAol = False
        self.twx = QuantConnect.Symbol.Create("TWX", SecurityType.Equity, Market.USA)

        self.UniverseSettings.Resolution = Resolution.Daily
        self.AddUniverse(self.CoarseSelection, self.FineSelection)

    def CoarseSelection(coarse):
        aol = [i.Symbol for i in coarse if coarse.Symbol == self.twx][0]

        if not self.addedAol:
            # Should map the underlying Symbol to AOL, which will in turn become TWX in the future
            self.twxCustom = AddData<SECReport10K>(aol)
            self.addedAol = True

        return [aol]

    def FineSelection(fine):
        return [i.Symbol for i in fine]

    ### <summary>
    ### Checks that custom data matches our expectation that it will be subscribed to the "*TWX -> AOL" data
    ### and not "*AOL" where "*TICKER" is the most recent-day ticker
    ### </summary>
    ### <param name="data"></param>
    def OnData(data):
        symbol = data[_twxCustom.Symbol].Symbol

        if self.Time < datetime(2003, 10, 16) and data.ContainsKey(self.twxCustom.Symbol):
            expectedTicker = "AOL"
        elif self.Time >= datetime(2003, 10, 16) and data.ContainsKey(self.twxCustom.Symbol):
            expectedTicker = "TWX"
        else:
            return

        if not symbol.HasUnderlying:
            raise PythonError("Custom data symbol has no underlying")
        if symbol.Value != expectedTicker:
            raise PythonError(f"Custom data symbol value does not match expected value. Expected {expectedTicker}, found {symbol.Value}")
        # TODO: maybe we don't need this since we have CustomDataUnderlyingSymbolMappingRegressionAlgorithm?
        if symbol.Underlying.Value != expectedTicker:
            raise PythonError(f"Custom data underlying {symbol.Underlying.Value} was not mapped to {expectedTicker}")
