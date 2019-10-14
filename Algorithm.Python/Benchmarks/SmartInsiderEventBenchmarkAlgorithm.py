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
from QuantConnect.Data.Custom.SmartInsider import *

class SmartInsiderEventBenchmarkAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        self.SetStartDate(2005, 1, 1)
        self.SetEndDate(2019, 1, 1)

        tickers = {"AAPL", "AMZN", "MSFT", "IBM", "FB", "QQQ", "IWM", "BAC", "BNO", "AIG", "UW", "WM" }
        self.securities = []
        for ticker in tickers:
            security = self.AddEquity(ticker, Resolution.Daily)
            self.securities.append(security)
            self.AddData(SmartInsiderIntention, security.Symbol, Resolution.Daily)
            self.AddData(SmartInsiderTransaction, security.Symbol, Resolution.Daily)

    def OnData(self, slice):
        intentions = slice.Get(SmartInsiderIntention)
        transactions = slice.Get(SmartInsiderTransaction)
        for security in self.securities:
            intention = security.Data.Get(SmartInsiderIntention)
            transaction = security.Data.Get(SmartInsiderTransaction)

            if not security.HoldStock and intention != None and transaction != None and intentions.Count == transactions.Count:
                self.SetHoldings(security.Symbol, 1 / len(self.securities))
