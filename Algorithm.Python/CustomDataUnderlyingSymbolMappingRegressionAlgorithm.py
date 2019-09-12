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
from datetime import datetime

### <summary>
### Regression algorithm ensures that mapping is also applied to the underlying symbol(s) for custom data subscriptions
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="regression test" />
### <meta name="tag" content="rename event" />
### <meta name="tag" content="map" />
### <meta name="tag" content="mapping" />
### <meta name="tag" content="map files" />
class CustomDataUnderlyingSymbolMappingRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(1998, 1, 1)
        self.SetEndDate(2004, 1, 1)
        self.SetCash(100000)

        self.initialSymbolChangedEvent = False

        self.equitySymbol = self.AddEquity("GOOGL", Resolution.Daily).Symbol
        self.customDataSymbol = self.AddData(SECReport10K, self.equitySymbol).Symbol

        self.optionSymbol = self.AddOption("TWX", Resolution.Daily).Symbol
        self.customDataOptionSymbol = self.AddData(SECReport10K, self.optionSymbol).Symbol

    def OnData(self, data):
        if len(data.SymbolChangedEvents) != 0 and not self.initialSymbolChangedEvent:
            self.initialSymbolChangedEvent = True
            return

        if len(data.SymbolChangedEvents) != 0:
            if data.SymbolChangedEvents.ContainsKey(self.customDataSymbol) and data.SymbolChangedEvents.ContainsKey(self.equitySymbol):
                expectedUnderlying = "GOOGL"
                underlying = data.SymbolChangedEvents[self.customDataSymbol].Symbol.Underlying
                symbol = data.SymbolChangedEvents[self.equitySymbol].Symbol

            elif data.SymbolChangedEvents.ContainsKey(self.customDataOptionSymbol) and data.SymbolChangedEvents.ContainsKey(self.optionSymbol):
                expectedUnderlying = "?TWX"
                underlying = data.SymbolChangedEvents[self.customDataOptionSymbol].Symbol.Underlying
                symbol = data.SymbolChangedEvents[self.optionSymbol].Symbol

                if underlying is None:
                    raise PythonError("Custom data Symbol has no underlying")
                if underlying.Underlying is None:
                    raise PythonError("Custom data underlying has no underlying equity symbol")
                if underlying.Underlying != symbol.Underlying:
                    raise PythonError(f"Custom data underlying->(2) does not match option underlying (equity symbol). Expected {symbol.Underlying.Value} got {underlying.Underlying.Value}")
                if underlying.Underlying.Value != expectedUnderlying:
                    raise PythonError(f"Custom data symbol value does not match expected value. Expected {expectedUnderlying}, found {underlying.Underlying.Value}")

                return

            else:
                raise PythonError("Received unknown symbol changed event")

            if underlying != symbol:
                if underlying is None:
                    raise PythonError("Custom data Symbol has no underlying")

                raise PythonError(f"Underlying custom data Symbol does not match equity Symbol after rename event. Expected {symbol.Value} - got {underlying.Value}")

            if underlying.Value != expectedUnderlying:
                raise PythonError(f"Underlying equity symbol value from chained custom data does not match expected value. Expected {symbol.Underlying.Value}, found {underlying.Underlying.Value}")
