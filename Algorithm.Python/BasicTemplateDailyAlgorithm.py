import numpy as np

class BasicTemplateAlgorithm(QCAlgorithm):
    '''Basic template algorithm simply initializes the date range and cash'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013,10,07)  #Set Start Date
        self.SetEndDate(2013,10,18)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY", Resolution.Daily)
   

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)
            self.Debug("Purchased Stock")