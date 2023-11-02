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
### Algorithm demonstrating and ensuring that Bybit crypto brokerage model works as expected with custom data types
### </summary>
class BybitCustomDataCryptoRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2022, 12, 13)
        self.SetEndDate(2022, 12, 13)

        self.SetAccountCurrency("USDT")
        self.SetCash(100000)

        self.SetBrokerageModel(BrokerageName.Bybit, AccountType.Cash)

        symbol = self.AddCrypto("BTCUSDT").Symbol
        self.btcUsdt = self.AddData(CustomCryptoData, symbol, Resolution.Minute).Symbol;

        # create two moving averages
        self.fast = self.EMA(self.btcUsdt, 30, Resolution.Minute)
        self.slow = self.EMA(self.btcUsdt, 60, Resolution.Minute)

    def OnData(self, data):
        if not self.slow.IsReady:
            return

        if self.fast.Current.Value > self.slow.Current.Value:
            if self.Transactions.OrdersCount == 0:
                self.Buy(self.btcUsdt, 1)
        else:
            if self.Transactions.OrdersCount == 1:
                self.Liquidate(self.btcUsdt)

    def OnOrderEvent(self, orderEvent):
        self.Debug(f"{self.Time} {orderEvent}");

class CustomCryptoData(PythonData):
    def GetSource(self, config, date, isLiveMode):
        tickTypeString = Extensions.TickTypeToLower(config.TickType)
        formattedDate = date.strftime("%Y%m%d")
        source = os.path.join(Globals.DataFolder, "crypto", "bybit", "minute",
                              config.Symbol.Value.lower(), f"{formattedDate}_{tickTypeString}.zip")

        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def Reader(self, config, line, date, isLiveMode):
        csv = line.split(',')

        data = CustomCryptoData()
        data.Symbol = config.Symbol

        data_datetime = datetime.combine(date.date(), time()) + timedelta(milliseconds=int(csv[0]))
        data.Time = Extensions.ConvertTo(data_datetime, config.DataTimeZone, config.ExchangeTimeZone)
        data.EndTime = data.Time + timedelta(minutes=1)

        data["Open"] = float(csv[1])
        data["High"] = float(csv[2])
        data["Low"] = float(csv[3])
        data["Close"] = float(csv[4])
        data["Volume"] = float(csv[5])
        data.Value = float(csv[4])

        return data
