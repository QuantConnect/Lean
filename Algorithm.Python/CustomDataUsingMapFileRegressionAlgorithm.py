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
from QuantConnect.Data import *
from QuantConnect.Python import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.Market import *
from datetime import datetime

### <summary>
### Regression algorithm demonstrating use of map files with custom data
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="regression test" />
### <meta name="tag" content="rename event" />
### <meta name="tag" content="map" />
### <meta name="tag" content="mapping" />
### <meta name="tag" content="map files" />
class CustomDataUsingMapFileRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        self.SetStartDate(2013, 6, 27)
        self.SetEndDate(2013, 7, 2)

        self.initialMapping = False
        self.executionMapping = False
        self.foxa = Symbol.Create("FOXA", SecurityType.Equity, Market.USA)
        self.symbol = self.AddData(CustomDataUsingMapping, self.foxa).Symbol

    def OnData(self, slice):
        date = self.Time.date()
        if slice.SymbolChangedEvents.ContainsKey(self.symbol):
            mappingEvent = slice.SymbolChangedEvents[self.symbol]
            self.Log("{0} - Ticker changed from: {1} to {2}".format(str(self.Time), mappingEvent.OldSymbol, mappingEvent.NewSymbol))

            if date == datetime(2013, 6, 27).date():
                # initial mapping event since we added FOXA and it's currently NWSA - GH issue 3327
                if mappingEvent.NewSymbol != "NWSA" or mappingEvent.OldSymbol != "FOXA":
                    raise Exception("Unexpected mapping event mappingEvent")
                self.initialMapping = True

            if date == datetime(2013, 6, 29).date():
                if mappingEvent.NewSymbol != "FOXA" or mappingEvent.OldSymbol != "NWSA":
                    raise Exception("Unexpected mapping event mappingEvent")
                self.SetHoldings(self.symbol, 1)
                self.executionMapping = True

    def OnEndOfAlgorithm(self):
        if not self.initialMapping:
            raise Exception("The ticker did not generate the initial rename event")
        if not self.executionMapping:
            raise Exception("The ticker did not rename throughout the course of its life even though it should have")

class CustomDataUsingMapping(PythonData):
    '''Test example custom data showing how to enable the use of mapping.
    Implemented as a wrapper of existing NWSA->FOXA equity'''

    def GetSource(self, config, date, isLiveMode):
        return TradeBar().GetSource(SubscriptionDataConfig(config, CustomDataUsingMapping,
            # create a new symbol as equity so we find the existing data files
            Symbol.Create(config.MappedSymbol, SecurityType.Equity, config.Market)),
            date,
            isLiveMode);

    def Reader(self, config, line, date, isLiveMode):
        return TradeBar.ParseEquity(config, line, date)

    def RequiresMapping(self):
        '''True indicates mapping should be done'''
        return True