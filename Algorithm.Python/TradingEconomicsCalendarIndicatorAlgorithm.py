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
from QuantConnect.Data.Custom.TradingEconomics import *

### <summary>
### This example algorithm shows how to import and use Trading Economics data.
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="tradingeconomics" />
class TradingEconomicsCalendarIndicatorAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2018, 1, 1)
        self.SetEndDate(2019, 1, 1)

        self.calendar = self.AddData(TradingEconomicsCalendar, TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol
        self.indicator = self.AddData(TradingEconomicsIndicator, TradingEconomics.Indicator.UnitedStates.InterestRate).Symbol


    def OnData(self, slice):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if slice.ContainsKey(self.calendar):
            self.Log(f"{self.Time} - {slice[self.calendar]}")
        if slice.ContainsKey(self.indicator):
            self.Log(f"{self.Time} - {slice[self.indicator]}")