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

### <summary>
### Provides an example algorithm showcasing the Security.Data features
### </summary>
class DynamicSecurityDataAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.Ticker = "GOOGL";

        self.SetStartDate(2015, 10, 22)
        self.SetEndDate(2015, 10, 30)

        self.GOOGL = self.AddEquity(self.Ticker, Resolution.Daily)

        self.AddData(SECReport8K, self.Ticker, Resolution.Daily)
        self.AddData(SECReport10K, self.Ticker, Resolution.Daily)
        self.AddData(SECReport10Q, self.Ticker, Resolution.Daily)

    def OnData(self, data):

        # The Security object's Data property provides convenient access
        # to the various types of data related to that security. You can
        # access not only the security's price data, but also any custom
        # data that is mapped to the security, such as our SEC reports.

        # 1. Get the most recent data point of a particular type:
        # 1.a Using the generic method, Get(T): => T
        googlSec8kReport = self.GOOGL.Data.Get(SECReport8K)
        googlSec10kReport = self.GOOGL.Data.Get(SECReport10K)
        self.Log("{}:  8K: {}".format(self.Time, googlSec8kReport))
        self.Log("{}: 10K: {}".format(self.Time, googlSec10kReport))

        # 2. Get the list of data points of a particular type for the most recent time step:
        # 2.a Using the generic method, GetAll(T): => IReadOnlyList<T>
        googlSec8kReports = self.GOOGL.Data.GetAll(SECReport8K)
        googlSec10kReports = self.GOOGL.Data.GetAll(SECReport10K)
        self.Log("{}:  8K: {}".format(self.Time, len(googlSec8kReports)))
        self.Log("{}: 10K: {}".format(self.Time, len(googlSec10kReports)))

        if not self.Portfolio.Invested:
            self.Buy(self.GOOGL.Symbol, 10)
