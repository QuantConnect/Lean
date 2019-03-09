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
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Securities import *
from QuantConnect.Data.Consolidators import *
from datetime import timedelta

### <summary>
### A demonstration of consolidating options data into larger bars for your algorithm.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="benchmarks" />
### <meta name="tag" content="consolidating data" />
### <meta name="tag" content="options" />
class BasicTemplateOptionsConsolidationAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(1000000)

        # Subscribe and set our filter for the options chain
        option = self.AddOption('SPY')
        option.SetFilter(-2, 2, timedelta(0), timedelta(180))
        self.consolidators = dict()
    
    def OnData(self,slice):
        pass

    def OnQuoteBarConsolidated(self, sender, quoteBar):
        self.Log("OnQuoteBarConsolidated called on " + str(self.Time))
        self.Log(str(quoteBar))

    def OnTradeBarConsolidated(self, sender, tradeBar):
        self.Log("OnTradeBarConsolidated called on " + str(self.Time))
        self.Log(str(tradeBar))
        
    def OnSecuritiesChanged(self, changes):
        for security in changes.AddedSecurities:
            if security.Type == SecurityType.Equity:
                consolidator = TradeBarConsolidator(timedelta(minutes=5))
                consolidator.DataConsolidated += self.OnTradeBarConsolidated
            else:
                consolidator = QuoteBarConsolidator(timedelta(minutes=5))
                consolidator.DataConsolidated += self.OnQuoteBarConsolidated
                
            self.SubscriptionManager.AddConsolidator(security.Symbol, consolidator)
            self.consolidators[security.Symbol] = consolidator
            
        for security in changes.RemovedSecurities:
            consolidator = self.consolidators.pop(security.Symbol)
            self.SubscriptionManager.RemoveConsolidator(security.Symbol, consolidator)
            
            if security.Type == SecurityType.Equity:
                consolidator.DataConsolidated -= self.OnTradeBarConsolidated
            else:
                consolidator.DataConsolidated -= self.OnQuoteBarConsolidated