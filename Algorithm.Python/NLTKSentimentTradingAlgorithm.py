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

import clr
clr.AddReference("System")
clr.AddReference("QuantConnect.Algorithm")
clr.AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *

import pandas as pd
import nltk
# for details of NLTK, please visit https://www.nltk.org/index.html

class NLTKSentimentTradingAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2018, 1, 1)  # Set Start Date
        self.SetEndDate(2019, 1, 1) # Set End Date
        self.SetCash(100000)  # Set Strategy Cash
        
        spy = self.AddEquity("SPY", Resolution.Minute)
        self.text = self.get_text()
        self.symbols = [spy.Symbol]
        
        # for what extra models needed to download, please use code nltk.download()
        nltk.download('punkt')
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.AfterMarketOpen("SPY", 30), Action(self.Trade))

    def OnData(self, data):
        pass
    
    def Trade(self):
        current_time = str(self.Time.year) + '-' + str(self.Time.month) + '-' + str(self.Time.day)
        current_text = self.text.loc[current_time][0]
        words = nltk.word_tokenize(current_text)
        
        # users should decide their own positive and negative words
        positive_word = 'Up'
        negative_word = 'Down'
        
        for i in self.Portfolio.Values:
            # liquidate
            if negative_word in words and i.Invested:
                self.Liquidate(i.Symbol)
            
            # buy
            if positive_word in words and not i.Invested:
                self.SetHoldings(i.Symbol, 1 / len(self.symbols))
        
    def get_text(self):
        # import custom data
        # Note: dl must be 1, or it will not download automatically
        url = 'https://www.dropbox.com/s/8nhbizxq3lgpced/EconomicNews.csv?dl=1'
        data = self.Download(url).split('\n')

        headline = [x.split(',')[1] for x in data][1:]
        date = [x.split(',')[0] for x in data][1:]
        
        df = pd.DataFrame(headline, index = date, columns = ['headline'])
        return df