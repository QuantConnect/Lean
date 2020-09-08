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
import pandas
AddReference("System")
AddReference("QuantConnect.Research")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Research import *
from datetime import datetime, timedelta
from custom_data import QuandlFuture, Nifty
import pandas as pd

class FundamentalHistoryTest():
    def __init__(self, var, start, end):
        self.qb = QuantBook()
        self.var = var
        self.start = start
        self.end = end

        self.qb.AddEquity("AAPL")
        self.qb.AddEquity("GOOG")
        self.qb.AddEquity("SPY")

    def getData(self):
        self.data = self.qb.GetFundamental(self.var, "ValuationRatios.PERatio", self.start, self.end)
        return self.data