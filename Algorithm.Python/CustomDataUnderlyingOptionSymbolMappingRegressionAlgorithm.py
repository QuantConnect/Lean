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
class CustomDataUnderlyingOptionSymbolMappingRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 6, 28)
        self.SetEndDate(2013, 7, 02)
        self.SetCash(100000)

        self.initialSymbolChangedEvent = False

        self.optionSymbol = self.AddOption("FOXA", Resolution.Daily).Symbol
        self.customDataOptionSymbol = self.AddData(SECReport10K, self.optionSymbol).Symbol

    def OnData(self, data):
        if len(data.SymbolChangedEvents) != 0 and not self.initialSymbolChangedEvent:
            self.initialSymbolChangedEvent = True
            return

        if len(data.SymbolChangedEvents) != 0:
            if data.SymbolChangedEvents.ContainsKey(self.customDataOptionSymbol) and data.SymbolChangedEvents.ContainsKey(self.optionSymbol):
                expectedUnderlying = "?FOXA"
                underlying = [i for i in data.SymbolChangedEvents.Keys if i.SecurityType == SecurityType.Base and i == self.customDataOptionSymbol][0].Underlying
                symbol = [i for i in data.SymbolChangedEvents.Keys if i.SecurityType == SecurityType.Equity and i == self.optionSymbol][0]

                if len([i for i in self.SubscriptionManager.Subscriptions if (i.SecurityType == SecurityType.Base or i.SecurityType == SecurityType.Option or i.SecurityType == SecurityType.Equity) and i.MappedSymbol == expectedUnderlying]) != 3:
                    raise Exception(f"Subscription mapped symbols were not updated to {expectedUnderlying}")
                if underlying is None:
                    raise Exception("Custom data Symbol has no underlying")
                if underlying.Underlying is None:
                    raise Exception("Custom data underlying has no underlying equity symbol")
                if underlying.Underlying != symbol.Underlying:
                    raise Exception(f"Custom data underlying->(2) does not match option underlying (equity symbol). Expected {symbol.Underlying.Value} got {underlying.Underlying.Value}")
                if underlying.Underlying.Value != expectedUnderlying:
                    raise Exception(f"Custom data symbol value does not match expected value. Expected {expectedUnderlying}, found {underlying.Underlying.Value}")

                self.SetHoldings(underlying.Underlying, 0.5)

            else:
                raise Exception("Received unknown symbol changed event")
