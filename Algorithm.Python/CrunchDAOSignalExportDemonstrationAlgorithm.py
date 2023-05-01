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
### This algorithm sends a current portfolio target to CrunchDAO API every time
### the ema indicators crosses between themselves.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class CrunchDAOSignalExportDemonstrationAlgorithm(QCAlgorithm):

    def Initialize(self):
        ''' Initialize the date and add one equity symbol, as CrunchDAO only accepts stock and index symbols '''

        self.SetStartDate(2013, 10, 7)   #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash

        self.spy = self.AddEquity("SPY").Symbol;

        self.fast = self.EMA("SPY", 10)
        self.slow = self.EMA("SPY", 100)

        # Initialize these flags, to check when the ema indicators crosses between themselves
        self.emaFastIsNotSet = True;
        self.emaFastWasAbove = False;

        # Set the CrunchDAO signal export provider
        # CrunchDAO API key: This value is provided by CrunchDAO when you sign up
        self.crunchDAOApiKey = ""

        # CrunchDAO Model ID: When your email is verified, you can find this value in your CrunchDAO profile main page: https://tournament.crunchdao.com/profile/alpha
        self.crunchDAOModel = ""

        # Replace this value with the name for your submission (Optional)
        self.crunchDAOSubmissionName = ""

        # Replace this value with a comment for your submission (Optional)
        self.crunchDAOComment = ""
        self.SignalExport.AddSignalExportProviders(CrunchDAOSignalExport(self.crunchDAOApiKey, self.crunchDAOModel, self.crunchDAOSubmissionName, self.crunchDAOComment))
        
        self.first_call = True
        
        self.SetWarmUp(100)

    def OnData(self, data):
        ''' Reduce the quantity of holdings for spy or increase it when the EMA's indicators crosses
        between themselves, then send a signal to CrunchDAO API '''
        if self.IsWarmingUp: return
        
        # Place an order as soon as possible to send a signal.
        if self.first_call:
            self.SetHoldings("SPY", 0.1)
            target = PortfolioTarget(self.spy, 0.1)
            self.SignalExport.SetTargetPortfolio(target)
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
        # or reduce its holdings,update its value in self.target and send signals
        # to the CrunchDAO API from self.target
        if fast > slow * 1.001 and (not self.emaFastWasAbove):
            self.SetHoldings("SPY", 0.1)
            target = PortfolioTarget(self.spy, 0.1)
            self.SignalExport.SetTargetPortfolio(target)
        elif fast < slow * 0.999 and (self.emaFastWasAbove):
            self.SetHoldings("SPY", 0.01)
            target = PortfolioTarget(self.spy, 0.01)
            self.SignalExport.SetTargetPortfolio(target)
