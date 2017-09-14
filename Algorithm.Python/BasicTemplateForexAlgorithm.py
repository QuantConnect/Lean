from datetime import timedelta
import numpy as np

class BasicTemplateAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Set the cash we'd like to use for our backtest
        self.SetCash(100000)
        
        # Start and end dates for the backtest.
        self.SetStartDate(2013, 10, 07)
        self.SetEndDate(2013, 10, 11)
        
        # Add FOREX contract you want to trade
        # find available contracts here https://www.quantconnect.com/data#forex/oanda/cfd
        self.AddForex("EURUSD", Resolution.Minute)
        self.AddForex("GBPUSD", Resolution.Minute)
        self.AddForex("EURGBP", Resolution.Minute)
        
        self.History(5, Resolution.Daily)
        self.History(5, Resolution.Hour)
        self.History(5, Resolution.Minute)

        history = self.History(TimeSpan.FromSeconds(5), Resolution.Second)
         
        for data in sorted(history, key=lambda x: x.Time):
            for key in data.Keys:
                self.Log(str(key.Value) + ": " + str(data.Time) + " > " + str(data[key].Value))
        
    def OnData(self, data):
        # Print to console to verify that data is coming in
        for key in data.Keys:
            self.Log(str(key.Value) + ": " + str(data.Time) + " > " + str(i[key].Value))