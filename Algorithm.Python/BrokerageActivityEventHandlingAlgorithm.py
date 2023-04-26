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

from AlgorithmImports import *

### <summary>
### Example algorithm to demostrate the event handlers of Brokerage activities
### </summary>
### <meta name="tag" content="using quantconnect" />
class BrokerageActivityEventHandlingAlgorithm(QCAlgorithm):
    
    ### <summary>
    ### Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    ### </summary>
    def Initialize(self):
        self.SetStartDate(2013, 10, 07)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)

        self.AddEquity("SPY", Resolution.Minute)

    ### <summary>
    ### OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    ### </summary>
    ### <param name="data">Slice object keyed by symbol containing the stock data</param>
    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

    ### <summary>
    ### Brokerage message event handler. This method is called for all types of brokerage messages.
    ### </summary>
    def OnBrokerageMessage(self, messageEvent):
        self.Debug(f"Brokerage meesage received - {messageEvent.ToString()}")

    ### <summary>
    ### Brokerage disconnected event handler. This method is called when the brokerage connection is lost.
    ### </summary>
    def OnBrokerageDisconnect(self):
        self.Debug(f"Brokerage disconnected!")
    
    ### <summary>
    ### Brokerage reconnected event handler. This method is called when the brokerage connection is restored after a disconnection.
    ### </summary>
    def OnBrokerageReconnect(self):
        self.Debug(f"Brokerage reconnected!")