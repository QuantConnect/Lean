from AlgorithmImports import *

class HistoryOverloadsRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)
        self.spy = self.AddEquity("SPY", Resolution.Minute).Symbol

    def OnData(self, data):
        slices = self.History([self.spy], self.Time-timedelta(1), self.Time, Resolution.Minute, True, True)
        self.Log(slices)
