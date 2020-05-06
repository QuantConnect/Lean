import pandas as pd
import numpy as np
import talib

class CalibratedResistanceAtmosphericScrubbers(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2019, 12, 31)  # Set Start Date
        self.SetEndDate(2020, 1, 5) 
        self.SetCash(100000)  # Set Strategy Cash
        self.AddEquity("SPY", Resolution.Hour)
        
        self.closes = np.array([])
        self.dema_lookback = 3
        self.closes_lookback = self.dema_lookback * 2
        self.SetWarmUp(self.closes_lookback)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
            Arguments:
                data: Slice object keyed by symbol containing the stock data
        '''
        if "SPY" not in data.Bars:
            return
        
        close_ = data["SPY"].Close
        self.closes = np.append(self.closes, close_)[-self.closes_lookback:]
        if self.IsWarmingUp:
            return
        
        dema = talib.DEMA(self.closes, self.dema_lookback)[-1]
        self.Log(f'\nDEMA:\n{dema}')