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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data import SubscriptionDataSource
from QuantConnect.Python import PythonData
from QCAlgorithm import QCAlgorithm
from datetime import date, timedelta, datetime
import decimal
import numpy as np
import math
import json


### <summary>
### Strategy example algorithm using CAPE - a bubble indicator dataset saved in dropbox. CAPE is based on a macroeconomic indicator(CAPE Ratio),
### we are looking for entry/exit points for momentum stocks CAPE data: January 1990 - December 2014
### Goals:
### Capitalize in overvalued markets by generating returns with momentum and selling before the crash
### Capitalize in undervalued markets by purchasing stocks at bottom of trough
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="custom data" />
class BubbleAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetCash(100000)
        self.SetStartDate(1998,1,1)
        self.SetEndDate(2014,6,1)
        self._symbols = []
        self._macdDic, self._rsiDic = {},{}
        self._newLow, self._currCape = None, None
        self._counter, self._counter2 = 0, 0
        self._c, self._cCopy = np.empty([4]), np.empty([4])
        self._symbols.append("SPY")
        # add CAPE data
        self.AddData(Cape, "CAPE")
        
        # # Present Social Media Stocks:
        # self._symbols.append("FB"), self._symbols.append("LNKD"),self._symbols.append("GRPN"), self._symbols.append("TWTR")
        # self.SetStartDate(2011, 1, 1)
        # self.SetEndDate(2014, 12, 1)
        
        # # 2008 Financials
        # self._symbols.append("C"), self._symbols.append("AIG"), self._symbols.append("BAC"), self._symbols.append("HBOS")
        # self.SetStartDate(2003, 1, 1)
        # self.SetEndDate(2011, 1, 1)
        
        # # 2000 Dot.com
        # self._symbols.append("IPET"), self._symbols.append("WBVN"), self._symbols.append("GCTY")
        # self.SetStartDate(1998, 1, 1)
        # self.SetEndDate(2000, 1, 1)
        
        
        for stock in self._symbols:
            self.AddSecurity(SecurityType.Equity, stock, Resolution.Minute)
            self._macd = self.MACD(stock, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily)
            self._macdDic[stock] = self._macd
            self._rsi = self.RSI(stock, 14, MovingAverageType.Exponential, Resolution.Daily)
            self._rsiDic[stock] = self._rsi
        
    # Trying to find if current Cape is the lowest Cape in three months to indicate selling period
    def OnData(self, data):
        
        if self._currCape and self._newLow is not None:   
            try:
                # Bubble territory
                if self._currCape > 20 and self._newLow == False:
                    for stock in self._symbols:
                    # Order stock based on MACD
                    # During market hours, stock is trading, and sufficient cash
                        if self.Securities[stock].Holdings.Quantity == 0 and self._rsiDic[stock].Current.Value < 70 \
                        and self.Securities[stock].Price != 0 \
                        and self.Portfolio.Cash > self.Securities[stock].Price * 100 \
                        and self.Time.hour == 9 and self.Time.minute == 31:
                            self.BuyStock(stock)
                    # Utilize RSI for overbought territories and liquidate that stock
                        if self._rsiDic[stock].Current.Value > 70 and self.Securities[stock].Holdings.Quantity > 0 \
                        and self.Time.hour == 9 and self.Time.minute == 31:
                            self.SellStock(stock)
                           
                # Undervalued territory            
                elif self._newLow:
                    for stock in self._symbols:
                        # Sell stock based on MACD
                        if self.Securities[stock].Holdings.Quantity > 0 and self._rsiDic[stock].Current.Value > 30 \
                        and self.Time.hour == 9 and self.Time.minute == 31:
                            self.SellStock(stock)
                        # Utilize RSI and MACD to understand oversold territories
                        elif self.Securities[stock].Holdings.Quantity == 0 and self._rsiDic[stock].Current.Value < 30 \
                        and Securities[stock].Price != 0 and self.Portfolio.Cash > self.Securities[stock].Price * 100 \
                        and self.Time.hour == 9 and self.Time.minute == 31:
                            self.BuyStock(stock)
                
                # Cape Ratio is missing from orignial data
                # Most recent cape data is most likely to be missing 
                elif self._currCape == 0:
                    self.Debug("Exiting due to no CAPE!")
                    self.Quit("CAPE ratio not supplied in data, exiting.")
                
            except:
                # Do nothing
                return None       

        if not data.ContainsKey("CAPE"): return
        self._newLow = False
        # Adds first four Cape Ratios to array c
        self._currCape = data["CAPE"].Cape
        if self._counter < 4:
            self._c[self._counter] = self._currCape
            self._counter +=1
        # Replaces oldest Cape with current Cape
        # Checks to see if current Cape is lowest in the previous quarter
        # Indicating a sell off
        else:
            self._cCopy = self._c  
            self._cCopy = np.sort(self._cCopy)
            if self._cCopy[0] > self._currCape:
                self._newLow = True
            self._c[self._counter2] = self._currCape
            self._counter2 += 1
            if self._counter2 == 4: self._counter2 = 0
        self.Debug("Current Cape: " + str(self._currCape) + " on " + str(self.Time))
        if self._newLow:
            self.Debug("New Low has been hit on " + str(self.Time))

    # Buy this symbol
    def BuyStock(self,symbol):
        s = self.Securities[symbol].Holdings
        if self._macdDic[symbol].Current.Value>0:
            self.SetHoldings(symbol, 1)
            self.Debug("Purchasing: " + str(symbol) + "   MACD: " + str(self._macdDic[symbol]) + "   RSI: " + str(self._rsiDic[symbol])
                    + "   Price: " + str(round(self.Securities[symbol].Price, 2)) + "   Quantity: " + str(s.Quantity))

    # Sell this symbol
    def SellStock(self,symbol):
        s = self.Securities[symbol].Holdings
        if s.Quantity > 0 and self._macdDic[symbol].Current.Value < 0:
            self.Liquidate(symbol)
            self.Debug("Selling: " + str(symbol) + " at sell MACD: " + str(self._macdDic[symbol]) + "   RSI: " + str(self._rsiDic[symbol])
                    + "   Price: " + str(round(self.Securities[symbol].Price, 2)) + "   Profit from sale: " + str(s.LastTradeProfit))


# CAPE Ratio for SP500 PE Ratio for avg inflation adjusted earnings for previous ten years Custom Data from DropBox
# Original Data from: http://www.econ.yale.edu/~shiller/data.htm
class Cape(PythonData):
    
    # Return the URL string source of the file. This will be converted to a stream
    # <param name="config">Configuration object</param>
    # <param name="date">Date of this source file</param>
    # <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
    # <returns>String URL of source file.</returns>

    def GetSource(self, config, date, isLiveMode):
        # Remember to add the "?dl=1" for dropbox links
        return SubscriptionDataSource("https://www.dropbox.com/s/ggt6blmib54q36e/CAPE.csv?dl=1", SubscriptionTransportMedium.RemoteFile)
    
    
    ''' Reader Method : using set of arguements we specify read out type. Enumerate until 
        the end of the data stream or file. E.g. Read CSV file line by line and convert into data types. '''
        
    # <returns>BaseData type set by Subscription Method.</returns>
    # <param name="config">Config.</param>
    # <param name="line">Line.</param>
    # <param name="date">Date.</param>
    # <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
    
    def Reader(self, config, line, date, isLiveMode):
        if not (line.strip() and line[0].isdigit()): return None
    
        # New Nifty object
        index = Cape()
        index.Symbol = config.Symbol
    
        try:
            # Example File Format:
            # Date   |  Price |  Div  | Earning | CPI  | FractionalDate | Interest Rate | RealPrice | RealDiv | RealEarnings | CAPE
            # 2014.06  1947.09  37.38   103.12   238.343    2014.37          2.6           1923.95     36.94        101.89     25.55
            data = line.split(',')
            # Dates must be in the format YYYY-MM-DD. If your data source does not have this format, you must use
            # DateTime.ParseExact() and explicit declare the format your data source has.
            index.Time = datetime.strptime(data[0], "%Y-%m")
            index["Cape"] = float(data[10]) 
            index.Value = decimal.Decimal(data[10])
            
    
        except ValueError:
                # Do nothing
                return None
    
        return index