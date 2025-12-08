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
### Regression algorithm testing history requests for <see cref="FutureUniverse"/> type work as expected
### and return the same data as the futures chain provider.
### </summary>
class OptionUniverseHistoryRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 11)
        self.set_end_date(2013, 10, 11)

        future = self.add_future(Futures.Indices.SP_500_E_MINI).symbol

        historical_futures_data_df = self.history(FutureUniverse, future, 3, flatten=True)

        # Level 0 of the multi-index is the date, we expect 3 dates, 3 future chains
        if historical_futures_data_df.index.levshape[0] != 3:
            raise RegressionTestException(f"Expected 3 futures chains from history request, "
                                          f"but got {historical_futures_data_df.index.levshape[1]}")

        for date in historical_futures_data_df.index.levels[0]:
            expected_chain = list(self.future_chain_provider.get_future_contract_list(future, date))
            expected_chain_count = len(expected_chain)

            actual_chain = historical_futures_data_df.loc[date]
            actual_chain_count = len(actual_chain)

            if expected_chain_count != actual_chain_count:
                raise RegressionTestException(f"Expected {expected_chain_count} futures in chain on {date}, "
                                              f"but got {actual_chain_count}")

            for i, symbol in enumerate(actual_chain.index):
                expected_symbol = expected_chain[i]
                if symbol != expected_symbol:
                    raise RegressionTestException(f"Expected symbol {expected_symbol} at index "
                                                  f" {i} on {date}, but got {symbol}")
