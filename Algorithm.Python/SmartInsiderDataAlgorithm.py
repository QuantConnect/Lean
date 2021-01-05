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
from QuantConnect.Data import *
from QuantConnect.Data.Custom.SmartInsider import *
from QuantConnect.Algorithm import *

### <summary>
### Example algorithm demonstrating usage of SmartInsider data
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="smart insider" />
### <meta name="tag" content="form 4" />
### <meta name="tag" content="insider trading" />
class SmartInsiderDataAlgoritm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2019, 7, 25)
        self.SetEndDate(2019, 8, 2)
        self.SetCash(100000)

        self.symbol = self.AddEquity("KO", Resolution.Daily).Symbol
        self.AddData(SmartInsiderTransaction, "KO")
        self.AddData(SmartInsiderIntention, "KO")

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if not data.ContainsKey(self.symbol.Value):
            return

        has_open_orders = len(self.Transactions.GetOpenOrders()) != 0
        ko_data = data[self.symbol.Value]

        if isinstance(ko_data, SmartInsiderTransaction):
            if not self.Portfolio.Invested and not has_open_orders:
                if ko_data.BuybackPercentage > 0.0001 and ko_data.VolumePercentage > 0.001:
                    self.Log(f"Buying {self.symbol.Value} due to stock transaction")
                    self.SetHoldings(self.symbol, 0.50)

        elif isinstance(ko_data, SmartInsiderIntention):
            if not self.Portfolio.Invested and not has_open_orders:
                if ko_data.Percentage > 0.0001:
                    self.Log(f"Buying {self.symbol.Value} due to intention to purchase stock")
                    self.SetHoldings(self.symbol, 0.50)

            elif self.Portfolio.Invested and not has_open_orders:
                if ko_data.Percentage < 0.0:
                    self.Log(f"Liquidating {self.symbol.Value}")
                    self.Liquidate(self.symbol)





