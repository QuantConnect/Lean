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
from CustomDataRegressionAlgorithm import Bitcoin

### <summary>
### Regression algorithm used to verify that get_data(type) correctly retrieves
### the latest custom data stored in the security cache.
### </summary>
class CustomDataSecurityCacheGetDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2020,1,5)
        self.set_end_date(2020,1,10)

        self.add_data(Bitcoin, "BTC", Resolution.DAILY)

        seeder = FuncSecuritySeeder(self.get_last_known_prices)
        self.set_security_initializer(lambda x: seeder.seed_security(x))

    def on_data(self, data: Slice) -> None:
        bitcoin = self.securities['BTC'].cache.get_data(Bitcoin)
        if bitcoin is None:
            raise RegressionTestException("Expected Bitcoin data in cache, but none was found")
        if bitcoin.value == 0:
            raise RegressionTestException("Expected Bitcoin value to be non-zero")
        
        bitcoin_from_slice = list(data.get(Bitcoin).values())[0]
        if bitcoin_from_slice != bitcoin:
            raise RegressionTestException("Expected cached Bitcoin to match the one from Slice")
