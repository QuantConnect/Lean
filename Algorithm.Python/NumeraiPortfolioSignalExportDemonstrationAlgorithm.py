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
### to Numerai API every time the ema indicators crosses between themselves.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class NumeraiPortfolioSignalExportDemonstrationAlgorithm(QCAlgorithm):

    def Initialize(self):
        ''' Initialize the date and add all equity symbols present in list _symbols '''

        self.SetStartDate(2013, 10, 7)   #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash

        self.symbols = ["SPY", "AIG", "GOOGL", "AAPL", "AMZN", "TSLA", "NFLX", "INTC", "MSFT", "KO", "WMT", "IBM", "AMGN", "CAT"] # Numerai accepts minimum 10 signals

        for ticker in self.symbols:
            self.AddEquity(ticker)

        self.fast = self.EMA("SPY", 10)
        self.slow = self.EMA("SPY", 100)

        # Initialize these flags, to check when the ema indicators crosses between themselves
        self.emaFastIsNotSet = True;
        self.emaFastWasAbove = False;

        # Set Numerai signal export provider
        # Numerai Public ID: This value is provided by Numerai Signals in their main webpage once you've logged in
        # and created a API key. See (https://signals.numer.ai/account)
        self.numeraiPublicId = ""

        # Numerai Public ID: This value is provided by Numerai Signals in their main webpage once you've logged in
        # and created a API key. See (https://signals.numer.ai/account)
        self.numeraiSecretId = ""

        # Numerai Model ID: This value is provided by Numerai Signals in their main webpage once you've logged in
        # and created a model. See (https://signals.numer.ai/models)
        self.numeraiModelId = ""

        self.numeraiFilename = "" # Replace this values with your submission filename (Optional)
        self.SignalExport.AddSignalExportProviders(NumeraiSignalExport(self.numeraiPublicId, self.numeraiSecretId, self.numeraiModelId, self.numeraiFilename))

    def OnData(self, data):
        ''' Reduce the quantity of holdings for one security and increase the holdings to the another
        one when the EMA's indicators crosses between themselves, then send a signal to Numerai API '''

        # Wait for our indicators to be ready
        if not self.fast.IsReady or not self.slow.IsReady:
            return

        fast = self.fast.Current.Value
        slow = self.slow.Current.Value

        # Set the value of flag _emaFastWasAbove, to know when the ema indicators crosses between themselves
        # Additionally, set an initial holding quantity for each symbol. This is done because Numerai only
        # accept signals between 0 and 1 (exclusive)
        if self.emaFastIsNotSet == True:
            if fast > slow *1.001:
                self.emaFastWasAbove = True
            else:
                self.emaFastWasAbove = False
            self.emaFastIsNotSet = False;
            for symbol in self.symbols:
                self.SetHoldings(symbol, 0.05)

        # Check whether ema fast and ema slow crosses. If they do, set holdings to SPY
        # or reduce its holdings, and send signals to Numerai API from your Portfolio
        if fast > slow * 1.001 and (not self.emaFastWasAbove):
            self.SetHoldings("SPY", 0.1)
            self.SignalExport.SetTargetPortfolioFromPortfolio()
        elif fast < slow * 0.999 and (self.emaFastWasAbove):
            self.SetHoldings("SPY", 0.01)
            self.SignalExport.SetTargetPortfolioFromPortfolio()
