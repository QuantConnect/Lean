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
from QuantConnect.Data.Market import *
from QuantConnect.Data.Consolidators import *
from datetime import timedelta

### <summary>
### Demonstration of how to initialize and use the RenkoConsolidator
### </summary>
### <meta name="tag" content="renko" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="consolidating data" />
class RenkoConsolidatorAlgorithm(QCAlgorithm):
    '''Demonstration of how to initialize and use the RenkoConsolidator'''

    def Initialize(self):

        self.SetStartDate(2012, 1, 1)
        self.SetEndDate(2013, 1, 1)

        self.AddEquity("SPY", Resolution.Daily)

        # this is the simple constructor that will perform the
        # renko logic to the Value property of the data it receives.

        # break SPY into $2.5 renko bricks and send that data to our 'OnRenkoBar' method
        renkoClose = RenkoConsolidator(2.5)
        renkoClose.DataConsolidated += self.HandleRenkoClose
        self.SubscriptionManager.AddConsolidator("SPY", renkoClose)

        # this is the full constructor that can accept a value selector and a volume selector
        # this allows us to perform the renko logic on values other than Close, even computed values!

        # break SPY into (2*o + h + l + 3*c)/7
        renko7bar = RenkoConsolidator(2.5, lambda x: (2 * x.Open + x.High + x.Low + 3 * x.Close) / 7, lambda x: x.Volume)
        renko7bar.DataConsolidated += self.HandleRenko7Bar
        self.SubscriptionManager.AddConsolidator("SPY", renko7bar)


    # We're doing our analysis in the OnRenkoBar method, but the framework verifies that this method exists, so we define it.
    def OnData(self, data):
        pass


    def HandleRenkoClose(self, sender, data):
        '''This function is called by our renkoClose consolidator defined in Initialize()
        Args:
            data: The new renko bar produced by the consolidator'''
        if not self.Portfolio.Invested:
            self.SetHoldings(data.Symbol, 1)

        self.Log(f"CLOSE - {data.Time} - {data.Open} {data.Close}")


    def HandleRenko7Bar(self, sender, data):
        '''This function is called by our renko7bar consolidator defined in Initialize()
        Args:
            data: The new renko bar produced by the consolidator'''
        if self.Portfolio.Invested:
            self.Liquidate(data.Symbol)
        self.Log(f"7BAR - {data.Time} - {data.Open} {data.Close}")