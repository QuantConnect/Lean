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
### Options Open Interest data regression test.
### </summary>
### <meta name="tag" content="options" />
### <meta name="tag" content="regression test" />
class OptionOpenInterestRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        
        self.SetCash(1000000)
        self.SetStartDate(2014,06,05)
        self.SetEndDate(2014,06,06)
        equity = self.AddEquity("twx")
        option = self.AddOption("twx")
        Underlying = equity.Symbol
        self.OptionSymbol = option.Symbol
        # set our strike/expiry filter for this option chain
        option.SetFilter(-10, 10, TimeSpan.Zero, TimeSpan.FromDays(365*2))
        # use the underlying equity as the benchmark
        self.SetBenchmark(Underlying)
        equity.SetDataNormalizationMode(DataNormalizationMode.Raw)

    ''' Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override 
        for receiving all subscription data in a single event
        <param name="slice">The current slice of data keyed by symbol string</param> '''

    def OnData(self, slice):
        if not self.Portfolio.Invested: 
            for chain in slice.OptionChains:
                for contract in chain.Value:
                    if float(contract.Symbol.ID.StrikePrice) == 72.5 and \
                       contract.Symbol.ID.OptionRight == OptionRight.Call and \
                       contract.Symbol.ID.Date == datetime(2016, 01, 15):
                        if slice.Time.date() == datetime(2014, 06, 5).date() and contract.OpenInterest != 50:
                            raise ValueError("Regression test failed: current open interest was not correctly loaded and is not equal to 50")  
                        if slice.Time.date() == datetime(2014, 06, 6).date() and contract.OpenInterest != 70:
                            raise ValueError("Regression test failed: current open interest was not correctly loaded and is not equal to 70")  
                        if slice.Time.date() == datetime(2014, 06, 6).date():
                            self.MarketOrder(contract.Symbol, 1)
                            self.MarketOnCloseOrder(contract.Symbol, -1)
                            
 
    # Order fill event handler. On an order fill update the resulting information is passed to this method.
    # </summary>
    # <param name="orderEvent">Order event details containing details of the events</param> 
        
    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))