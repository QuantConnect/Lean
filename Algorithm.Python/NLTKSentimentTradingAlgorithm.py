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
        self.text = self.get_text() # Get custom text data for creating trading signals
        self.symbols = [spy.Symbol] # This can be extended to multiple symbols
        
        # for what extra models needed to download, please use code nltk.download()
        nltk.download('punkt')
        self.Schedule.On(self.DateRules.EveryDay("SPY"), self.TimeRules.AfterMarketOpen("SPY", 30), self.Trade)
    
    def Trade(self):
        current_time = f'{self.Time.year}-{self.Time.month}-{self.Time.day}'
        current_text = self.text.loc[current_time][0]
        words = nltk.word_tokenize(current_text)
        
        # users should decide their own positive and negative words
        positive_word = 'Up'
        negative_word = 'Down'
        
        for holding in self.Portfolio.Values:
            # liquidate if it contains negative words
            if negative_word in words and holding.Invested:
                self.Liquidate(holding.Symbol)
            
            # buy if it contains positive words
            if positive_word in words and not holding.Invested:
                self.SetHoldings(holding.Symbol, 1 / len(self.symbols))
        
    def get_text(self):
        # import custom data
        # Note: dl must be 1, or it will not download automatically
        url = 'https://www.dropbox.com/s/7xgvkypg6uxp6xl/EconomicNews.csv?dl=1'
        data = self.Download(url).split('\n')

        headline = [x.split(',')[1] for x in data][1:]
        date = [x.split(',')[0] for x in data][1:]
        
        # create a pd dataframe with 1st col being date and 2nd col being headline (content of the text)
        df = pd.DataFrame(headline, index = date, columns = ['headline'])
        return df