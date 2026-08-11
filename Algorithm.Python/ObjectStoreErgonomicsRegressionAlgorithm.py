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
from decimal import Decimal

### <summary>
### Regression algorithm asserting the ObjectStore ergonomics: unsupported keys are rejected with the
### key rules stated in the error, sanitize_key converts arbitrary names into supported keys, save_text
### is an alias of save, reading a missing key lists the available keys in the error, and the tolerant
### save_json/read_json and save_dataframe helpers handle datetime/date/Decimal/Symbol and data frames
### out of the box.
### </summary>
class ObjectStoreErgonomicsRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        self.set_benchmark(lambda x: 0)

        self.spy = self.add_equity("SPY", Resolution.DAILY).symbol

    def on_end_of_algorithm(self):
        # an unsupported key, e.g. built from a user-facing name, is rejected stating the rules and the fix.
        # note: BaseException because AlgorithmImports shadows the builtin Exception with System.Exception
        invalid_key = "trade_log_ai_hardware_&_cloud.csv"
        error = None
        try:
            self.object_store.save(invalid_key, "a,b\n1,2")
        except BaseException as e:
            error = str(e)
        if error is None or "keys may only contain" not in error or "SanitizeKey" not in error:
            raise AssertionError(f"Expected the key rules in the unsupported key error, got: '{error}'")

        # sanitize_key converts the arbitrary name into a supported key
        sanitized = self.object_store.sanitize_key(invalid_key)
        if sanitized != "trade_log_ai_hardware___cloud.csv":
            raise AssertionError(f"Unexpected sanitized key: '{sanitized}'")
        if not self.object_store.save(sanitized, "a,b\n1,2"):
            raise AssertionError("Expected the sanitized key to be storable")

        # save_json tolerates datetime/date/Decimal/Symbol values and non-string dictionary keys
        trade_log = {
            "time": datetime(2013, 10, 11, 16, 0, 0),
            "date": date(2013, 10, 11),
            "qty": Decimal("1.5"),
            "symbol": self.spy,
            "fills": [{"price": Decimal("167.42"), self.spy: 10}]
        }
        if not self.object_store.save_json("ergonomics/trade_log.json", trade_log):
            raise AssertionError("Expected save_json to succeed")
        data = self.object_store.read_json("ergonomics/trade_log.json")
        expected = {
            "time": "2013-10-11T16:00:00",
            "date": "2013-10-11",
            "qty": 1.5,
            "symbol": "SPY",
            "fills": [{"price": 167.42, "SPY": 10}]
        }
        if data != expected:
            raise AssertionError(f"Unexpected read_json round trip result: {data}")

        # read_json returns the given default when the key is missing
        if self.object_store.read_json("ergonomics/missing.json") is not None:
            raise AssertionError("Expected read_json of a missing key to return None")
        if self.object_store.read_json("ergonomics/missing.json", {"warmed_up": False}) != {"warmed_up": False}:
            raise AssertionError("Expected read_json of a missing key to return the given default")

        # save_dataframe stores a pandas DataFrame as CSV
        frame = pd.DataFrame({"close": [167.42, 168.0]}, index=pd.to_datetime(["2013-10-10", "2013-10-11"]))
        if not self.object_store.save_dataframe("ergonomics/history.csv", frame):
            raise AssertionError("Expected save_dataframe to succeed")
        csv = self.object_store.read("ergonomics/history.csv")
        if "close" not in csv or "2013-10-10" not in csv:
            raise AssertionError(f"Unexpected save_dataframe content: {csv}")

        # save_text is an alias of save
        if not self.object_store.save_text("ergonomics/report.txt", "The strategy went up"):
            raise AssertionError("Expected save_text to succeed")
        if self.object_store.read("ergonomics/report.txt") != "The strategy went up":
            raise AssertionError("Expected the save_text/read round trip to succeed")

        # reading a missing key lists the available keys in the error
        error = None
        try:
            self.object_store.read_bytes("ergonomics/missing.json")
        except BaseException as e:
            error = str(e)
        if error is None or "Keys: [" not in error or f"'{sanitized}'" not in error:
            raise AssertionError(f"Expected the available keys in the missing key error, got: '{error}'")

        for key in [sanitized, "ergonomics/trade_log.json", "ergonomics/history.csv", "ergonomics/report.txt"]:
            self.object_store.delete(key)
