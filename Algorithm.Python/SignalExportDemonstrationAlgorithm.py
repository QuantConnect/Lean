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
### This algorithm sends current portfolio targets to different 3rd party API's every time
### the portfolio changes.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class SignalExportDemonstrationAlgorithm(QCAlgorithm):

    def Initialize(self):
        ''' Initialize the date and add two securities '''

        self.SetStartDate(2013, 10, 7)   #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash
        self.spy = self.AddEquity("SPY").Symbol
        self.aig = self.AddEquity("AIG").Symbol

        # Receive parameters from the Job
        fast_period = 100
        slow_period = 200

        self.fast = self.EMA("SPY", fast_period)
        self.slow = self.EMA("SPY", slow_period)

        # Set the signal export providers
        self.collective2Apikey = "" # Replace this value with your Collective2 API key
        self.collective2SystemId = 1 # Replace this value with your Collective2 system ID
        self.SignalExport.AddSignalExportProviders(Collective2SignalExport(self.collective2Apikey, self.collective2SystemId, self.Portfolio))

    def OnData(self, data):
        '''Remove one security and set holdings to the another one when the EMA's cross, then send a signal to the 3rd party API's defined'''

        # wait for our indicators to ready
        if not self.fast.IsReady or not self.slow.IsReady:
            return

        fast = self.fast.Current.Value
        slow = self.slow.Current.Value

        '''This is not actually checking whether the EMA's are crossing between themselves'''
        if fast > slow * 1.001:
            self.SetHoldings("SPY", 1)
            self.Liquidate("AIG")
            self.SignalExport.SetTargetPortfolio(self.Portfolio)
        elif fast < slow * 0.999:
            self.Liquidate("SPY")
            self.SetHoldings("AIG", 1)
            self.SignalExport.SetTargetPortfolio(self.Portfolio)
            


