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
AddReference("QuantConnect.Jupyter")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Jupyter import *
from datetime import datetime, timedelta
from custom_data import QuandlFuture, Nifty
import pandas as pd

class SecurityHistoryTest():
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)
        self.symbol = self.qb.AddSecurity(security_type, symbol).Symbol
        self.column = 'close'

    def __str__(self):
        return "{} on {}".format(self.symbol.ID, self.qb.StartDate)

    def test_period_overload(self, period):
        history = self.qb.History([self.symbol], period)
        return history[self.column].unstack(level=0)

    def test_daterange_overload(self, end):
        start = end - timedelta(1)
        history = self.qb.History([self.symbol], start, end)
        return history[self.column].unstack(level=0)

class OptionHistoryTest(SecurityHistoryTest):
    def test_daterange_overload(self, end, start = None):
        if start is None:
            start = end - timedelta(1)
        history = self.qb.GetOptionHistory(self.symbol, start, end)
        return history.GetAllData()

class FutureHistoryTest(SecurityHistoryTest):
    def test_daterange_overload(self, end, start = None):
        if start is None:
            start = end - timedelta(1)
        history = self.qb.GetFutureHistory(self.symbol, start, end)
        return history.GetAllData()

class FutureContractHistoryTest():
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)
        self.symbol = symbol
        self.column = 'close'

    def test_daterange_overload(self, end):
        start = end - timedelta(1)
        history = self.qb.GetFutureHistory(self.symbol, start, end)
        return history.GetAllData()

class OptionContractHistoryTest(FutureContractHistoryTest):
    def test_daterange_overload(self, end):
        start = end - timedelta(1)
        history = self.qb.GetOptionHistory(self.symbol, start, end)
        return history.GetAllData()

class CustomDataHistoryTest(SecurityHistoryTest):
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)

        if security_type == 'Nifty':
            type = Nifty
            self.column = 'close'
        elif security_type == 'QuandlFuture':
            type = QuandlFuture
            self.column = 'settle'
        else:
            raise

        self.symbol = self.qb.AddData(type, symbol, Resolution.Daily).Symbol

class MultipleSecuritiesHistoryTest(SecurityHistoryTest):
    def __init__(self, start_date, security_type, symbol):
        self.qb = QuantBook()
        self.qb.SetStartDate(start_date)
        self.qb.AddEquity('SPY', Resolution.Daily)
        self.qb.AddForex('EURUSD', Resolution.Daily)
        self.qb.AddCrypto('BTCUSD', Resolution.Daily)

    def test_period_overload(self, period):
        history = self.qb.History(self.qb.Securities.Keys, period)
        return history['close'].unstack(level=0)