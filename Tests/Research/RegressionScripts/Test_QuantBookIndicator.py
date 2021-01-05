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
AddReference("QuantConnect.Research")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Research import *
from QuantConnect.Indicators import *

class IndicatorTest():
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)
        self.symbol = self.qb.AddSecurity(security_type, symbol).Symbol

    def __str__(self):
        return "{} on {}".format(self.symbol.ID, self.qb.StartDate)

    def test_bollinger_bands(self, symbol, start, end, resolution):
        ind = BollingerBands(10, 2)
        return self.qb.Indicator(ind, symbol, start, end, resolution)

    def test_average_true_range(self, symbol, start, end, resolution):
        ind = AverageTrueRange(14)
        return self.qb.Indicator(ind, symbol, start, end, resolution)

    def test_on_balance_volume(self, symbol, start, end, resolution):
        ind = OnBalanceVolume(symbol)
        return self.qb.Indicator(ind, symbol, start, end, resolution)