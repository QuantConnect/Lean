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
from QuantConnect.Tests import *
from QuantConnect.Tests.Python import *

# TODO: Rename to PandasResearchTests and keep this class for QB related tests; rename py module to PandasTests
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

# Test class that sets up two dataframes to test on
class PandasDataFrameTests():
    def __init__(self):
        self.spy = Symbols.SPY
        self.aapl = Symbols.AAPL

        # Set our symbol cache
        SymbolCache.Set("SPY", self.spy)
        SymbolCache.Set("AAPL", self.aapl)

        pdConverter = PandasConverter()

        # Create our dataframes
        self.spydf = pdConverter.GetDataFrame(PythonTestingUtils.GetSlices(self.spy))

    def test_contains_user_mapped_ticker(self):
        # Create a new DF that has a plain ticker, test that our mapper doesn't break
        # searching for it.
        df = pd.DataFrame({'spy': [2, 5, 8, 10]})
        return 'spy' in df

    def test_expected_exception(self):
        # Try indexing a ticker that doesn't exist in this frame, but is still in our cache
        try:
            self.spydf['aapl']
        except KeyError as e:
            return str(e)

    def test_contains_user_defined_columns_with_spaces(self, column_name):
        # Adds a column, then try accessing it.
        # If the colums has white spaces, it should not fail
        df = self.spydf.copy()
        df[column_name] = 1
        try:
            x = df[column_name]
            return True
        except:
            return False
