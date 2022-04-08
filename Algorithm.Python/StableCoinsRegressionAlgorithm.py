from AlgorithmImports import *

class StableCoinsRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2018, 5, 1)
        self.SetEndDate(2018, 5, 2)
        self.SetCash("USDT", 200000000, 1)
        self.SetBrokerageModel(BrokerageName.Binance, AccountType.Cash)
        self.AddCrypto("BTCUSDT", Resolution.Hour, Market.Binance)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("BTCUSDT", 1)
