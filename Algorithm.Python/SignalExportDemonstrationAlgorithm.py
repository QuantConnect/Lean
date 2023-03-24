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
### This algorithm sends an array of current portfolio targets to different 3rd party API's every time
### the ema indicators crosses between themselves.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class SignalExportDemonstrationAlgorithm(QCAlgorithm):

    def Initialize(self):
        ''' Initialize the date and add all equity symbols present in list _symbols '''

        self.SetStartDate(2013, 10, 7)   #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash

        self.symbols = ["SPY", "AIG", "GOOGL", "AAPL", "AMZN", "TSLA", "NFLX", "INTC", "MSFT", "KO", "WMT", "IBM", "AMGN", "CAT"]
        self.targets = []

        for symbol in self.symbols:
            self.AddEquity(symbol)

        fastPeriod = 100
        slowPeriod = 200

        self.fast = self.EMA("SPY", fastPeriod)
        self.slow = self.EMA("SPY", slowPeriod)

        # Initialize these flags, to check when the ema indicators crosses between themselves
        self.emaFastIsNotSet = True;
        self.emaFastWasAbove = False;

        # Set the signal export providers
        self.collective2Apikey = "" # Replace this value with your Collective2 API key
        self.collective2SystemId = 0 # Replace this value with your Collective2 system ID
        self.SignalExport.AddSignalExportProviders(Collective2SignalExport(self.collective2Apikey, self.collective2SystemId))

        self.crunchDAOApiKey = "" # Replace this value with your CrunchDAO API key
        self.crunchDAOModel = "" # Replace this value with your model's name
        self.SignalExport.AddSignalExportProviders(CrunchDAOSignalExport(self.crunchDAOApiKey, self.crunchDAOModel))

        self.numeraiPublicId = "" # Replace this value with your Numerai Signals Public ID
        self.numeraiSecretId = "" # Replace this value with your Numerai Signals Secret ID
        self.numeraiModelId = "" # Replace this value with your Numerai Signals Model ID
        self.SignalExport.AddSignalExportProviders(NumeraiSignalExport(self.numeraiPublicId, self.numeraiSecretId, self.numeraiModelId))

    def OnData(self, data):
        ''' Reduce the quantity of holdings for one security and increase the holdings to the another
        one when the EMA's indicators crosses between themselves, then send a signal to the 3rd party
        API's defined and quit the algorithm '''

        # Wait for our indicators to be ready
        if not self.fast.IsReady or not self.slow.IsReady:
            return

        fast = self.fast.Current.Value
        slow = self.slow.Current.Value

        # Set the value of flag _emaFastWasAbove, to know when the ema indicators crosses between themselves
        if self.emaFastIsNotSet == True:
            if fast > slow *1.001:
                self.emaFastWasAbove = True
            else:
                self.emaFastWasAbove = False
            self.emaFastIsNotSet = False;
            self.SetInitialSignalValueForTargets()

        # Check whether ema fast and ema slow crosses. If they do, set holdings to SPY
        # or reduce its holdings, and send signals to the 3rd party API's defined above
        if fast > slow * 1.001 and (not self.emaFastWasAbove):
            self.SetHoldingsToSpyAndSendSignals(0.1)
        elif fast < slow * 0.999 and (self.emaFastWasAbove):
            self.SetHoldingsToSpyAndSendSignals(0.01)

    def SetInitialSignalValueForTargets(self):
        """ Set initial signal value for each portfolio target in _targets array """
        for symbol in self.symbols:
            self.targets.append(PortfolioTarget(self.Portfolio[symbol].Symbol, 0.05))

    def SetHoldingsToSpyAndSendSignals(self, quantity):
        """ Set Holdings to SPY and sends signals to the different 3rd party API's already defined """
        self.SetHoldings("SPY", quantity)
        self.targets[0] = PortfolioTarget(self.Portfolio["SPY"].Symbol, quantity)
        self.SignalExport.SetTargetPortfolio(self, self.targets)
            


