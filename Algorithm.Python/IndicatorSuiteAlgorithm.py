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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")


from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Data.Custom import *
from QuantConnect.Algorithm import *

### <summary>
### Basic template algorithm simply initializes the date range and cash. This is a skeleton
### framework you can use for designing an algorithm.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class IndicatorSuiteAlgorithm(QCAlgorithm):
    '''Demonstration algorithm of popular indicators and plotting them.'''

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.symbol = "SPY"
        self.customSymbol = "WIKI/FB"
        self.price = None
        
        self.SetStartDate(2013, 1, 1)  #Set Start Date
        self.SetEndDate(2014, 12, 31)    #Set End Date
        self.SetCash(25000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data

        self.AddEquity(self.symbol, Resolution.Daily)
        self.AddData(Quandl, self.customSymbol, Resolution.Daily)

        # Set up default Indicators, these indicators are defined on the Value property of incoming data (except ATR and AROON which use the full TradeBar object)
        self.indicators = {
                            'BB' : self.BB(self.symbol, 20, 1, MovingAverageType.Simple, Resolution.Daily),
                            'RSI' : self.RSI(self.symbol, 14, MovingAverageType.Simple, Resolution.Daily),
                            'EMA' : self.EMA(self.symbol, 14, Resolution.Daily),
                            'SMA' : self.SMA(self.symbol, 14, Resolution.Daily),
                            'MACD' : self.MACD(self.symbol, 12, 26, 9, MovingAverageType.Simple, Resolution.Daily),
                            'MOM' : self.MOM(self.symbol, 20, Resolution.Daily),
                            'MOMP' : self.MOMP(self.symbol, 20, Resolution.Daily),
                            'STD' : self.STD(self.symbol, 20, Resolution.Daily),
                            # by default if the symbol is a tradebar type then it will be the min of the low property
                            'MIN' : self.MIN(self.symbol, 14, Resolution.Daily),
                            # by default if the symbol is a tradebar type then it will be the max of the high property
                            'MAX' : self.MAX(self.symbol, 14, Resolution.Daily),
                            'ATR' : self.ATR(self.symbol, 14, MovingAverageType.Simple, Resolution.Daily),
                            'AROON' : self.AROON(self.symbol, 20, Resolution.Daily)
                          }

        #  Here we're going to define indicators using 'selector' functions. These 'selector' functions will define what data gets sent into the indicator
        #  These functions have a signature like the following: decimal Selector(BaseData baseData), and can be defined like: baseData => baseData.Value
        #  We'll define these 'selector' functions to select the Low value
        #
        #  For more information on 'anonymous functions' see: http:#en.wikipedia.org/wiki/Anonymous_function
        #                                                     https:#msdn.microsoft.com/en-us/library/bb397687.aspx
        # 
        self.selectorIndicators = {
                                    'BB' : self.BB(self.symbol, 20, 1, MovingAverageType.Simple, Resolution.Daily, Field.Low),
                                    'RSI' :self.RSI(self.symbol, 14, MovingAverageType.Simple, Resolution.Daily, Field.Low),
                                    'EMA' :self.EMA(self.symbol, 14, Resolution.Daily, Field.Low),
                                    'SMA' :self.SMA(self.symbol, 14, Resolution.Daily, Field.Low),
                                    'MACD' : self.MACD(self.symbol, 12, 26, 9, MovingAverageType.Simple, Resolution.Daily, Field.Low),
                                    'MOM' : self.MOM(self.symbol, 20, Resolution.Daily, Field.Low),
                                    'MOMP' : self.MOMP(self.symbol, 20, Resolution.Daily, Field.Low),
                                    'STD' : self.STD(self.symbol, 20, Resolution.Daily, Field.Low),
                                    'MIN' : self.MIN(self.symbol, 14, Resolution.Daily, Field.High),
                                    'MAX' : self.MAX(self.symbol, 14, Resolution.Daily, Field.Low),
                                    # ATR and AROON are special in that they accept a TradeBar instance instead of a decimal, we could easily project and/or transform the input TradeBar
                                    # before it gets sent to the ATR/AROON indicator, here we use a function that will multiply the input trade bar by a factor of two
                                    'ATR' : self.ATR(self.symbol, 14, MovingAverageType.Simple, Resolution.Daily, Func[IBaseData, IBaseDataBar](self.selector_double_TradeBar)),
                                    'AROON' : self.AROON(self.symbol, 20, Resolution.Daily, Func[IBaseData, IBaseDataBar](self.selector_double_TradeBar))
                                  }
        # Custom Data Indicator:
        self.rsiCustom = self.RSI(self.customSymbol, 14, MovingAverageType.Simple, Resolution.Daily)
        self.minCustom = self.MIN(self.customSymbol, 14, Resolution.Daily)
        self.maxCustom = self.MAX(self.customSymbol, 14, Resolution.Daily)

        # in addition to defining indicators on a single security, you can all define 'composite' indicators.
        # these are indicators that require multiple inputs. the most common of which is a ratio.
        # suppose we seek the ratio of BTC to SPY, we could write the following:
        spyClose = Identity(self.symbol)
        fbClose = Identity(self.customSymbol)

        # this will create a new indicator whose value is FB/SPY
        self.ratio = IndicatorExtensions.Over(fbClose, spyClose)

        # we can also easily plot our indicators each time they update using th PlotIndicator function
        self.PlotIndicator("Ratio", self.ratio)
        
        # The following methods will add multiple charts to the algorithm output.
        # Those chatrs names will be used later to plot different series in a particular chart.
        # For more information on Lean Charting see: https://www.quantconnect.com/docs#Charting
        Chart('BB')
        Chart('STD')
        Chart('ATR')
        Chart('AROON')
        Chart('MACD')
        Chart('Averages')
        # Here we make use of the Schelude method to update the plots once per day at market close.
        self.Schedule.On(self.DateRules.EveryDay(), self.TimeRules.BeforeMarketClose(self.symbol), self.update_plots)

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        
        if (#not data.Bars.ContainsKey(self.symbol) or
            not self.indicators['BB'].IsReady or
            not self.indicators['RSI'].IsReady):
            return
        
        self.price = data[self.symbol].Close

        if not self.Portfolio.HoldStock:
            quantity = int(self.Portfolio.Cash / self.price)
            self.Order(self.symbol, quantity)
            self.Debug('Purchased SPY on ' + self.Time.strftime('%Y-%m-%d'))

    def update_plots(self):
        if not self.indicators['BB'].IsReady or not self.indicators['STD'].IsReady: 
            return
        
        # Plots can also be created just with this one line command. 
        self.Plot('RSI', self.indicators['RSI'])
        # Custom data indicator
        self.Plot('RSI-FB', self.rsiCustom)
        
        # Here we make use of the chats decalred in the Initialize method, plotting multiple series
        # in each chart.
        self.Plot('STD', 'STD', self.indicators['STD'].Current.Value)
        
        self.Plot('BB', 'Price', self.price)
        self.Plot('BB', 'BollingerUpperBand', self.indicators['BB'].UpperBand.Current.Value)
        self.Plot('BB', 'BollingerMiddleBand', self.indicators['BB'].MiddleBand.Current.Value)
        self.Plot('BB', 'BollingerLowerBand', self.indicators['BB'].LowerBand.Current.Value)
         
        
        self.Plot('AROON', 'Aroon', self.indicators['AROON'].Current.Value)
        self.Plot('AROON', 'AroonUp', self.indicators['AROON'].AroonUp.Current.Value)
        self.Plot('AROON', 'AroonDown', self.indicators['AROON'].AroonDown.Current.Value)
        
        # The following Plot method calls are commented out because of the 10 series limit for backtests
        #self.Plot('ATR', 'ATR', self.indicators['ATR'].Current.Value)
        #self.Plot('ATR', 'ATRDoubleBar', self.selectorIndicators['ATR'].Current.Value)
        #self.Plot('Averages', 'SMA', self.indicators['SMA'].Current.Value)
        #self.Plot('Averages', 'EMA', self.indicators['EMA'].Current.Value)
        #self.Plot('MOM', self.indicators['MOM'].Current.Value)
        #self.Plot('MOMP', self.indicators['MOMP'].Current.Value)
        #self.Plot('MACD', 'MACD', self.indicators['MACD'].Current.Value)
        #self.Plot('MACD', 'MACDSignal', self.indicators['MACD'].Signal.Current.Value)
        
    def selector_double_TradeBar(self, bar):
        trade_bar = TradeBar()
        trade_bar.Close = 2 * bar.Close
        trade_bar.DataType = bar.DataType
        trade_bar.High = 2 * bar.High
        trade_bar.Low = 2 * bar.Low
        trade_bar.Open = 2 * bar.Open
        trade_bar.Symbol = bar.Symbol
        trade_bar.Time = bar.Time
        trade_bar.Value = 2 * bar.Value
        trade_bar.Period = bar.Period
        return trade_bar