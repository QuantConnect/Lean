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

from AlgorithmImports import *

### <summary>
### Regression algorithm to demonstrate the use of SetBenchmark() with custom data
### </summary>
class CustomDataBenchmarkRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2017, 8, 18)  # Set Start Date
        self.SetEndDate(2017, 8, 21)  # Set End Date
        self.SetCash(100000)  # Set Strategy Cash

        self.AddEquity("SPY", Resolution.Hour)
        # Load benchmark data
        self.customSymbol = self.AddData(ExampleCustomData, "ExampleCustomData", Resolution.Hour).Symbol
        self.SetBenchmark(self.customSymbol)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

    def OnEndOfAlgorithm(self):
        securityBenchmark = self.Benchmark;
        if securityBenchmark.Security.Price == 0:
            raise Exception("Security benchmark price was not expected to be zero")

class ExampleCustomData(PythonData):

    def GetSource(self, config, date, isLive):
        source = "https://www.dl.dropboxusercontent.com/s/d83xvd7mm9fzpk0/path_to_my_csv_data.csv?dl=0";
        return SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile)

    def Reader(self, config, line, date, isLive):
        data = line.split(',')
        obj_data = ExampleCustomData()
        obj_data.Symbol = config.Symbol
        obj_data.Time = datetime.strptime(data[0], '%Y-%m-%d %H:%M:%S') + timedelta(hours=20)
        obj_data.Value = float(data[4])
        obj_data["Open"] = float(data[1])
        obj_data["High"] = float(data[2])
        obj_data["Low"] = float(data[3])
        obj_data["Close"] = float(data[4])
        return obj_data
