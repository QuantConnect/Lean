Mock QuantConnect framework for local testing
"""
from datetime import datetime, timedelta
from enum import Enum
import random

class Resolution:
    Minute = "minute"
    Hour = "hour" 
    Daily = "daily"

class OptionRight(Enum):
    Call = "Call"
    Put = "Put"

class Symbol:
    def __init__(self, ticker):
        self.Value = ticker
        self.ID = ticker
        
    def __str__(self):
        return self.Value

class Security:
    def __init__(self, symbol):
        self.Symbol = symbol
        self.Price = random.uniform(100, 500)  # Mock price
        self.Volume = random.randint(1000000, 10000000)  # Mock volume
        
class OptionContract:
    def __init__(self, symbol, strike, expiry, right):
        self.Symbol = symbol
        self.Strike = strike
        self.Expiry = expiry
        self.Right = right
        self.Volume = random.randint(1000, 100000)

class Portfolio:
    def __init__(self):
        self._holdings = {}
        
    def __getitem__(self, symbol):
        if symbol not in self._holdings:
            self._holdings[symbol] = MockHolding()
        return self._holdings[symbol]

class MockHolding:
    def __init__(self):
        self.AveragePrice = random.uniform(1, 50)
        self.UnrealizedProfitPercent = random.uniform(-0.1, 0.1)

class OptionChainProvider:
    def GetOptionContractList(self, symbol, time):
        # Mock option chain with some contracts
        contracts = []
        base_price = random.uniform(100, 500)
        
        for days_to_exp in [30, 45, 60]:
            expiry = time + timedelta(days=days_to_exp)
            for strike_offset in [-20, -10, 0, 10, 20]:
                strike = base_price + strike_offset
                
                call_contract = OptionContract(
                    Symbol(f"{symbol.Value}_{strike}C_{expiry.strftime('%y%m%d')}"),
                    strike, expiry, OptionRight.Call
                )
                put_contract = OptionContract(
                    Symbol(f"{symbol.Value}_{strike}P_{expiry.strftime('%y%m%d')}"),
                    strike, expiry, OptionRight.Put
                )
                
                contracts.extend([call_contract, put_contract])
                
        return contracts

class MockSchedule:
    def On(self, date_rule, time_rule, callback):
        # Mock scheduling - just store the callback
        print(f"Scheduled: {callback.__name__}")

class MockDateRules:
    def EveryDay(self):
        return "every_day"

class MockTimeRules:
    def AfterMarketOpen(self, symbol, minutes):
        return f"after_market_open_{symbol}_{minutes}"
        
    def Every(self, timespan):
        return f"every_{timespan}"

class QCAlgorithm:
    def __init__(self):
        self.Time = datetime.now()
        self.Securities = {}
        self.Portfolio = Portfolio()
        self.OptionChainProvider = OptionChainProvider()
        self.Schedule = MockSchedule()
        self.DateRules = MockDateRules()
        self.TimeRules = MockTimeRules()
        
    def SetStartDate(self, year, month, day):
        self.start_date = datetime(year, month, day)
        print(f"Start date set to: {self.start_date}")
        
    def SetEndDate(self, year, month, day):
        self.end_date = datetime(year, month, day)
        print(f"End date set to: {self.end_date}")
        
    def SetCash(self, amount):
        self.cash = amount
        print(f"Cash set to: ${amount:,}")
        
    def AddEquity(self, ticker, resolution):
        symbol = Symbol(ticker)
        security = Security(symbol)
        self.Securities[symbol] = security
        print(f"Added equity: {ticker}")
        return security
        
    def AddOption(self, ticker, resolution):
        print(f"Added options for: {ticker}")
        
    def Buy(self, symbol, quantity):
        print(f"BUY: {quantity} shares of {symbol}")
        
    def Liquidate(self, symbol):
        print(f"LIQUIDATE: {symbol}")
        
    def Log(self, message):
        print(f"[LOG] {message}")

# Make imports available
__all__ = ['QCAlgorithm', 'Resolution', 'OptionRight', 'timedelta']