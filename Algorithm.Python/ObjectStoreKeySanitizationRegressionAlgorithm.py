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
### Regression algorithm asserting the ObjectStore key rules: an unsupported key is sanitized and used on every
### save and read, and once the sanitized name is written directly a save through the unsupported key throws
### </summary>
class ObjectStoreKeySanitizationRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        self.set_benchmark(lambda x: 0)

        self.add_equity("SPY", Resolution.DAILY)

    def on_end_of_algorithm(self):
        # an unsupported key, e.g. built from a user-facing name, is sanitized and used on every save and read
        invalid_key = "trade_log_ai_hardware_&_cloud.csv"
        sanitized_key = "trade_log_ai_hardware___cloud.csv"
        if not self.object_store.save(invalid_key, "a,b\n1,2"):
            raise AssertionError("Expected the unsupported key to be sanitized and saved")
        keys = list(self.object_store.keys)
        if sanitized_key not in keys or invalid_key in keys:
            raise AssertionError(f"Expected the object to be stored under '{sanitized_key}', keys: {keys}")
        if not self.object_store.contains_key(invalid_key) or self.object_store.read(invalid_key) != "a,b\n1,2" \
                or self.object_store.read(sanitized_key) != "a,b\n1,2":
            raise AssertionError("Expected the object to be read through both the original and the sanitized key")

        # a later save through the same key updates the object
        if not self.object_store.save(invalid_key, "a,b\n3,4") or self.object_store.read(sanitized_key) != "a,b\n3,4":
            raise AssertionError("Expected the second save through the unsupported key to update the object")

        # once the sanitized name is written directly, as an upload or another run would, it is never replaced through the unsupported key
        if not self.object_store.save(sanitized_key, "c,d\n5,6"):
            raise AssertionError("Expected the save through the sanitized key to succeed")
        error = ""
        try:
            self.object_store.save(invalid_key, "e,f\n7,8")
        except BaseException as e:
            error = str(e)
        if f"its sanitized name '{sanitized_key}' already holds another object" not in error:
            raise AssertionError(f"Expected the save through the unsupported key to throw, got: '{error}'")
        if self.object_store.read(sanitized_key) != "c,d\n5,6":
            raise AssertionError("Expected the object written through the sanitized key to be kept")

        self.object_store.delete(sanitized_key)
        if self.object_store.contains_key(invalid_key):
            raise AssertionError("Expected the delete of the sanitized key to remove the object the unsupported key reads")
