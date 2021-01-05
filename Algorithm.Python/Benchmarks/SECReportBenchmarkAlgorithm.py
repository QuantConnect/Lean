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
from QuantConnect.Data.Custom.SEC import *

class SECReportBenchmarksAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        self.SetStartDate(2018, 1, 1)
        self.SetEndDate(2019, 1, 1)

        tickers = {"AAPL", "AMZN", "MSFT", "IBM", "FB", "QQQ", "IWM", "BAC", "BNO", "AIG", "UW", "WM" }
        self.securities = []
        for ticker in tickers:
            security = self.AddEquity(ticker)
            self.securities.append(security)
            self.AddData(SECReport10K, security.Symbol, Resolution.Daily)
            self.AddData(SECReport8K, security.Symbol, Resolution.Daily)

    def OnData(self, slice):
        for security in self.securities:
            report8K = security.Data.Get(SECReport8K)
            report10K = security.Data.Get(SECReport10K)

            if not security.HoldStock and report8K != None and report10K != None:
                self.SetHoldings(security.Symbol, 1 / len(self.securities))
