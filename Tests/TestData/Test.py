# region imports
from AlgorithmImports import *
# endregion

class Test(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2024, 1, 1)
        self.SetCash(100000)
        self.AddCrypto("BTCUSD", Resolution.Minute)

    def OnData(self, data: Slice):
        if not self.Portfolio.Invested:
            self.SetHoldings("BTCUSD", 0.1)
        else:
            self.SetHoldings("BTCUSD", 0)
