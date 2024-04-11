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
### </summary>
class CustomDataTypeHistoryAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2017, 8, 20)
        self.SetEndDate(2017, 8, 20)

        self.symbol = self.AddData(CustomDataType, "CustomDataType", Resolution.Hour).Symbol

        history = list(self.History[CustomDataType](self.symbol, 48, Resolution.Hour))

        if len(history) == 0:
            raise Exception("History request returned no data")

        self._assertHistoryData(history)

        history2 = list(self.History[CustomDataType]([self.symbol], 48, Resolution.Hour))

        if len(history2) != len(history):
            raise Exception("History requests returned different data")

        self._assertHistoryData([list(y.values)[0] for y in history2])

    def _assertHistoryData(self, history:  List[PythonData]) -> None:
        expectedKeys = ['open', 'close', 'high', 'low', 'some_property']
        if any(any(not x[key] for key in expectedKeys)
               or x["some_property"] != "some property value"
               for x in history):
            raise Exception("History request returned data without the expected properties")

class CustomDataType(PythonData):

    def GetSource(self, config: SubscriptionDataConfig, date: datetime, isLive: bool) -> SubscriptionDataSource:
        source = "https://www.dl.dropboxusercontent.com/s/d83xvd7mm9fzpk0/path_to_my_csv_data.csv?dl=0"
        return SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile)

    def Reader(self, config: SubscriptionDataConfig, line: str, date: datetime, isLive: bool) -> BaseData:
        if not (line.strip()):
            return None

        data = line.split(',')
        obj_data = CustomDataType()
        obj_data.Symbol = config.Symbol

        try:
            obj_data.Time = datetime.strptime(data[0], '%Y-%m-%d %H:%M:%S') + timedelta(hours=20)
            obj_data["open"] = float(data[1])
            obj_data["high"] = float(data[2])
            obj_data["low"] = float(data[3])
            obj_data["close"] = float(data[4])
            obj_data.Value = obj_data["close"]

            # property for asserting the correct data is fetched
            obj_data["some_property"] = "some property value"
        except ValueError:
            return None

        return obj_data
