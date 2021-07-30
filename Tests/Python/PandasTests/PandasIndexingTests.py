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
AddReference("QuantConnect.Research")
AddReference("QuantConnect.Common")

from AlgorithmImports import *

class PandasIndexingTests():
    def __init__(self):
        self.qb = QuantBook()
        self.qb.SetStartDate(2020, 1, 1)
        self.qb.SetEndDate(2020, 1, 4)
        self.symbol = self.qb.AddEquity("SPY", Resolution.Daily).Symbol

    def test_indexing_dataframe_with_list(self):
        symbols = [self.symbol]
        self.history = self.qb.History(symbols, 30)
        self.history = self.history['close'].unstack(level=0).dropna()
        test = self.history[[self.symbol]]
        return True

