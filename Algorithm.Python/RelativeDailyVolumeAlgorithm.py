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

from AlgorithmImports import *

### <summary>
### Relative Daily Volume Algorithm that uses EnableAutomaticIndicatorWarmUp
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class RelativeDailyVOlumeAlgorithm(QCAlgorithm):
    '''RelativeDailyVolumeAlgorithm uses the RDV indicator to trade an asset '''

    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Hour

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,20)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        self.EnableAutomaticIndicatorWarmUp = True;
        self.spy = self.AddEquity("SPY", Resolution.Hour).Symbol;
        self.rdv = self.RDV(self.spy, 2);

    def OnData(self):
        if self.rdv.Current.Value > 1 and not self.Portfolio[self.spy].Invested:
            self.SetHoldings(self.spy, 1)
        elif self.rdv.Current.Value <= 1 and self.Portfolio[self.spy].Invested:
            self.Liquidate(symbol)

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            self.Debug("Purchased Stock: {0}".format(orderEvent.Symbol))
