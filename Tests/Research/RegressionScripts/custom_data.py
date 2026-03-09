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

from AlgorithmImports import *

import decimal

class CustomPythonData(PythonData):
    def get_source(self, config, date, is_live):
        source = Globals.DataFolder + "/equity/usa/daily/ibm.zip"
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv)

    def reader(self, config, line, date, is_live):
        if line == None:
            return None

        customPythonData = CustomPythonData()
        customPythonData.Symbol = config.Symbol

        scaleFactor = 1 / 10000
        csv = line.split(",")
        customPythonData.Time = datetime.strptime(csv[0], '%Y%m%d %H:%M')
        customPythonData["Open"] = float(csv[1]) * scaleFactor
        customPythonData["High"] = float(csv[2]) * scaleFactor
        customPythonData["Low"] = float(csv[3]) * scaleFactor
        customPythonData["Close"] = float(csv[4]) * scaleFactor
        customPythonData["Volume"] = float(csv[5])
        return customPythonData

class Nifty(PythonData):
    '''NIFTY Custom Data Class'''
    def get_source(self, config, date, is_live_mode):
        return SubscriptionDataSource("https://www.dropbox.com/s/rsmg44jr6wexn2h/CNXNIFTY.csv?dl=1", SubscriptionTransportMedium.REMOTE_FILE)


    def reader(self, config, line, date, is_live_mode):
        if not (line.strip() and line[0].isdigit()): return None

        # New Nifty object
        index = Nifty()
        index.symbol = config.symbol

        try:
            # Example File Format:
            # Date,       Open       High        Low       Close     Volume      Turnover
            # 2011-09-13  7792.9    7799.9     7722.65    7748.7    116534670    6107.78
            data = line.split(',')
            index.time = datetime.strptime(data[0], "%Y-%m-%d")
            index.value = decimal.Decimal(data[4])
            index["Open"] = float(data[1])
            index["High"] = float(data[2])
            index["Low"] = float(data[3])
            index["Close"] = float(data[4])


        except ValueError:
                # Do nothing
                return None

        return index
