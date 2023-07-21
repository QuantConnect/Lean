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
### This algorithm sends a list of portfolio targets from algorithm's Portfolio
### to Collective2 API every time the ema indicators crosses between themselves.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class Collective2PortfolioSignalExportDemonstrationAlgorithm(QCAlgorithm):

    def Initialize(self):
        ''' Initialize the date and add all equity symbols present in list _symbols '''

        self.SetStartDate(2013, 10, 7)   #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash

        # Symbols accepted by Collective2. Collective2 accepts stock, future, forex and option symbols 
        self.symbols = [["SPY", SecurityType.Equity], ["EURUSD", SecurityType.Forex]]
        for item in self.symbols:
            self.AddSecurity(item[1], item[0])

        self.fast = self.EMA("SPY", 10)
        self.slow = self.EMA("SPY", 100)

        # Initialize these flags, to check when the ema indicators crosses between themselves
        self.emaFastIsNotSet = True;
        self.emaFastWasAbove = False;

        # Set Collective2 export provider
        # Collective2 API: This value is provided by Collective2 in their webpage in your account section (See https://collective2.com/account-info)
        self.collective2Apikey = ""

        # Collective2 System ID: This value is found beside the system's name (strategy's name) on the main system page
        self.collective2SystemId = 0

        # Field to set your platform ID given by Collective2 (See https://collective2.com/api-docs/latest) (Optional)
        self.collective2PlatformId = ""
        self.SignalExport.AddSignalExportProviders(Collective2SignalExport(self.collective2Apikey, self.collective2SystemId, self.collective2PlatformId))
        
        self.first_call = True
        
        self.SetWarmUp(100)

    def OnData(self, data):
        ''' Reduce the quantity of holdings for one security and increase the holdings to the another
        one when the EMA's indicators crosses between themselves, then send a signal to Collective2 API '''
        if self.IsWarmingUp: return
        
        # Place an order as soon as possible to send a signal.
        if self.first_call:
            self.SetHoldings("SPY", 0.1)
            self.SignalExport.SetTargetPortfolioFromPortfolio()
            self.first_call = False

        fast = self.fast.Current.Value
        slow = self.slow.Current.Value

        # Set the value of flag _emaFastWasAbove, to know when the ema indicators crosses between themselves
        if self.emaFastIsNotSet == True:
            if fast > slow *1.001:
                self.emaFastWasAbove = True
            else:
                self.emaFastWasAbove = False
            self.emaFastIsNotSet = False;

        # Check whether ema fast and ema slow crosses. If they do, set holdings to SPY
        # or reduce its holdings, and send signals to Collective2 API from your Portfolio
        if fast > slow * 1.001 and (not self.emaFastWasAbove):
            self.SetHoldings("SPY", 0.1)
            self.SignalExport.SetTargetPortfolioFromPortfolio()
        elif fast < slow * 0.999 and (self.emaFastWasAbove):
            self.SetHoldings("SPY", 0.01)
            self.SignalExport.SetTargetPortfolioFromPortfolio()
