from QuantConnect.Securities.Option import *
from datetime import datetime, timedelta
import numpy as np

class BasicTemplateOptionStrategyAlgorithm(QCAlgorithm):
    ''' This example demonstrates how to add option strategies for a given underlying equity security.
        It also shows how you can prefilter contracts easily based on strikes and expirations.
        It also shows how you can inspect the option chain to pick a specific option contract to trade. '''

    def Initialize(self):
        # Set the cash we'd like to use for our backtest
        self.SetCash(1000000)
        
        # Start and end dates for the backtest.
        self.SetStartDate(2015,12,24)
        self.SetEndDate(2015,12,24)
        self.UnderlyingTicker = "GOOG"
        
        # Add assets you'd like to see
        equity = self.AddEquity(self.UnderlyingTicker)
        option = self.AddOption(self.UnderlyingTicker)
        self.OptionSymbol = option.Symbol
        equity.SetDataNormalizationMode(DataNormalizationMode.Raw)
        
        # set our strike/expiry filter for this option chain
        option.SetFilter(-2, +2, timedelta(0), timedelta(180))
        
        # use the underlying equity as the benchmark
        self.SetBenchmark(equity.Symbol)
        
    def OnData(self,slice):
        if not self.Portfolio.Invested: 
            for kvp in slice.OptionChains:
                chain = kvp.Value
                contracts = sorted(sorted(chain, key = lambda x: abs(chain.Underlying.Price - x.Strike)), 
                                        key = lambda x: x.Expiry, reverse=False)
                
                if len(contracts) == 0: continue
                atmStraddle = contracts[0]  
                if atmStraddle != None:
                    self.Sell(OptionStrategies.Straddle(self.OptionSymbol, atmStraddle.Strike, atmStraddle.Expiry), 2)
        else:
            self.Liquidate()

    def OnOrderEvent(self, orderEvent):
        ''' Order fill event handler. On an order fill update the resulting information is passed to this method.
            param "orderEvent"Order event details containing details of the evemts '''
        self.Log(str(orderEvent))