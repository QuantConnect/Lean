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
        self.SetStartDate(2010, 1, 1)
        self.SetEndDate(2010, 1, 2)
        self.SetCash(100000)

        self.symbol = self.AddData(MyCustomDataType, "MyCustomDataType", Resolution.Daily).Symbol

        #df_history = self.History(self.symbol, 30, Resolution.Daily)
        #self.Debug(f"DataFrame shape: {df_history.shape}")
        #self.Debug(f"DataFrame:\n{df_history}")



    def OnData(self, slice: Slice) -> None:
        #history = self.History[MyCustomDataType](self.symbol, 30, Resolution.Daily)
        history = list(self.History[MyCustomDataType](self.symbol, 30, Resolution.Daily))
        self.Quit(f"History count: {len(history)}")

class MyCustomDataType(PythonData):

    def GetSource(self, config: SubscriptionDataConfig, date: datetime, isLive: bool) -> SubscriptionDataSource:
        return SubscriptionDataSource("https://www.dropbox.com/s/rsmg44jr6wexn2h/CNXNIFTY.csv?dl=1", SubscriptionTransportMedium.RemoteFile)

    def Reader(self, config: SubscriptionDataConfig, line: str, date: datetime, isLive: bool) -> BaseData:

        if not (line.strip()):
            return None

        index = MyCustomDataType()
        index.Symbol = config.Symbol

        try:
            data = line.split(',')
            index.Time = datetime.strptime(data[0], "%Y-%m-%d")
            index.EndTime = index.Time + timedelta(days=1)
            index["open"] = float(data[1])
            index["high"] = float(data[2])
            index["low"] = float(data[3])
            index["close"] = float(data[4])
            index.Value = index["close"]

        except ValueError:
            # Do nothing
            return None

        return index
