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
### Regression algorithm testing history requests for <see cref="OptionUniverse"/> type work as expected
### and return the same data as the option chain provider.
### </summary>
class OptionUniverseHistoryRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 25)
        self.set_end_date(2015, 12, 25)

        option = self.add_option("GOOG").symbol

        historical_options_data_df = self.history(option, 3, Resolution.DAILY)

        if historical_options_data_df.shape[0] != 3:
            raise RegressionTestException(f"Expected 3 option chains from history request, but got {historical_options_data_df.shape[0]}")

        for index, row in historical_options_data_df.iterrows():
            data = row.data
            date = index[4]
            chain = list(self.option_chain_provider.get_option_contract_list(option, date))

            if len(chain) == 0:
                raise RegressionTestException(f"No options in chain on {date}")

            if len(chain) != len(data):
                raise RegressionTestException(f"Expected {len(chain)} options in chain on {date}, but got {len(data)}")

            for i in range(len(chain)):
                if data[i].symbol != chain[i]:
                    raise RegressionTestException(f"Missing option contract {chain[i]} on {date}")
