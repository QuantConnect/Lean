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
Exception = Exception

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
        self.SetStartDate(2014, 3, 1)
        self.SetEndDate(2014, 4, 9)
        self.SetCash(100000)

        self.initialSymbolChangedEvent = False

        QuantConnect.SymbolCache.Clear()
        self.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelector))

    def CoarseSelector(self, coarse):
        return [
            QuantConnect.Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
            QuantConnect.Symbol.Create("GOOGL", SecurityType.Equity, Market.USA)
        ]

    def OnData(self, data):
        if len(data.SymbolChangedEvents) != 0 and not self.initialSymbolChangedEvent:
            self.initialSymbolChangedEvent = True
            return

        if len(data.SymbolChangedEvents) != 0:
            if data.SymbolChangedEvents.ContainsKey(self.customDataSymbol) and data.SymbolChangedEvents.ContainsKey(self.equitySymbol):
                expectedUnderlying = "GOOGL"
                underlying = [i for i in data.SymbolChangedEvents.Keys if i.SecurityType == SecurityType.Base and i == self.customDataSymbol][0].Underlying
                symbol = [i for i in data.SymbolChangedEvents.Keys if i.SecurityType == SecurityType.Equity and i == self.equitySymbol][0]

                if len([i for i in self.SubscriptionManager.Subscriptions if (i.SecurityType == SecurityType.Base or i.SecurityType == SecurityType.Equity) and i.MappedSymbol == expectedUnderlying]) != 2:
                    raise Exception(f"Subscription mapped symbols were not updated to {expectedUnderlying}")

                if underlying is None:
                    raise Exception("Custom data Symbol for GOOGL has no underlying")

                if underlying != symbol:
                    raise Exception(f"Underlying custom data Symbol does not match equity Symbol after rename event. Expected {symbol.Value} - got {underlying.Value}")

                if underlying.Value != expectedUnderlying:
                    raise Exception(f"Underlying equity symbol value from chained custom data does not match expected value. Expected {symbol.Underlying.Value}, found {underlying.Underlying.Value}")

                self.SetHoldings(symbol, 0.5)

            elif data.SymbolChangedEvents.ContainsKey(self.badCustomDataSymbol) and data.SymbolChangedEvents.ContainsKey(self.badEquitySymbol):
                underlying = [i for i in data.SymbolChangedEvents.Keys if i.SecurityType == SecurityType.Base and i == self.badCustomDataSymbol][0].Underlying
                symbol = [i for i in data.SymbolChangedEvents.Keys if i.SecurityType == SecurityType.Equity and i == self.badEquitySymbol][0]

                if underlying is None:
                    raise Exception("Bad custom data symbol does not have underlying")

                if underlying == symbol:
                    raise Exception("Underlying custom data Symbol is equal to bad Symbol")

            else:
                raise Exception("Received unknown symbol changed event")

    def OnSecuritiesChanged(self, changes):
        for added in [i for i in changes.AddedSecurities if i.Symbol.SecurityType == SecurityType.Equity]:
            # It is in fact "GOOGL" we're catching here, and we're adding it as "GOOG" with the ticker,
            # which will resolve to GOOCV in the past if we use the ticker and not the symbol
            if added.Symbol.ID.Symbol == "GOOG":
                self.badEquitySymbol = added.Symbol
                self.badCustomDataSymbol = self.AddData(SECReport10K, "GOOG").Symbol

                self.equitySymbol = added.Symbol
                self.customDataSymbol = self.AddData(SECReport10K, added.Symbol).Symbol
