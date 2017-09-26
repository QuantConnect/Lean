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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from datetime import datetime

### <summary>
### This is an option split regression algorithm
### </summary>
### <meta name="tag" content="options" />
### <meta name="tag" content="regression test" />
class OptionRenameRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        
        self.SetCash(1000000)
        self.SetStartDate(2013,06,28)
        self.SetEndDate(2013,07,02)
        equity = self.AddEquity("FOXA")
        option = self.AddOption("FOXA")
        Underlying = equity.Symbol
        self.OptionSymbol = option.Symbol
        # set our strike/expiry filter for this option chain
        option.SetFilter(-1, 1, TimeSpan.Zero, TimeSpan.MaxValue)
        # use the underlying equity as the benchmark
        self.SetBenchmark(Underlying)
        equity.SetDataNormalizationMode(DataNormalizationMode.Raw)


    ''' Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        <param name="slice">The current slice of data keyed by symbol string</param> '''

    def OnData(self, slice):
        if not self.Portfolio.Invested: 
            for kvp in slice.OptionChains:
                chain = kvp.Value
                if self.Time.day == 28 and self.Time.hour > 9 and self.Time.minute > 0:
    
                    contracts = [i for i in sorted(chain, key=lambda x:x.Expiry) 
                                         if i.Right ==  OptionRight.Call and 
                                            i.Strike == 33 and
                                            i.Expiry.date() == datetime(2013,8,17).date()]
                    if contracts:
                        # Buying option
                        contract = contracts[0]
                        self.Buy(contract.Symbol, 1)
                        # Buy the undelying stock
                        underlyingSymbol = contract.Symbol.Underlying
                        self.Buy (underlyingSymbol, 100)
                        # check
                        if float(contract.AskPrice) != 1.1:
                            raise ValueError("Regression test failed: current ask price was not loaded from NWSA backtest file and is not $1.1")
        elif self.Time.day == 2 and self.Time.hour > 14 and self.Time.minute > 0:
            for kvp in slice.OptionChains:
                chain = kvp.Value
                self.Liquidate()
                contracts = [i for i in sorted(chain, key=lambda x:x.Expiry) 
                                        if i.Right ==  OptionRight.Call and 
                                           i.Strike == 33 and
                                           i.Expiry.date() == datetime(2013,8,17).date()]
            if contracts:
                contract = contracts[0]
                self.Log("Bid Price" + str(contract.BidPrice))
                if float(contract.BidPrice) != 0.05:
                    raise ValueError("Regression test failed: current bid price was not loaded from FOXA file and is not $0.05")
                    

    ''' Order fill event handler. On an order fill update the resulting information is passed to this method.
        <param name="orderEvent">Order event details containing details of the events</param> '''
        
    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))