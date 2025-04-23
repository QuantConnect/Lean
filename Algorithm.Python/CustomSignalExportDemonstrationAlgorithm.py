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

### <summary>
### his algorithm sends a list of portfolio targets to custom endpoint
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class CustomSignalExportDemonstrationAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        ''' Initialize the date and add all equity symbols present in list _symbols '''

        self.set_start_date(2013, 10, 7)   #Set Start Date
        self.set_end_date(2013, 10, 11)    #Set End Date
        self.set_cash(100000)             #Set Strategy Cash

        # Our custom signal export accepts all asset types
        self.add_equity("SPY", Resolution.SECOND)
        self.add_crypto("BTCUSD", Resolution.SECOND)
        self.add_forex("EURUSD", Resolution.SECOND)
        self.add_future_contract(Symbol.create_future("ES", Market.CME, datetime(2023, 12, 15)))
        self.add_option_contract(Symbol.create_option("SPY", Market.USA, OptionStyle.AMERICAN, OptionRight.CALL, 130, datetime(2023, 9, 1)))

        # Set CustomSignalExport signal export provider.
        self.signal_export.add_signal_export_provider(CustomSignalExport())

    def on_data(self, data: Slice) -> None:
        '''Buy and hold EURUSD and SPY'''
        for ticker in [ "SPY", "EURUSD", "BTCUSD" ]:
            if not self.portfolio[ticker].invested and self.securities[ticker].has_data:
                self.set_holdings(ticker, 0.5)

from requests import post
class CustomSignalExport:
    def send(self, parameters: SignalExportTargetParameters) -> bool:
        targets = [PortfolioTarget.percent(parameters.algorithm, x.symbol, x.quantity)
                   for x in parameters.targets]
        data = [ {'symbol' : x.symbol.value, 'quantity': x.quantity} for x in targets ]
        response = post("http://localhost:5000/", json = data)
        result = response.json()
        success = result.get('success', False)
        parameters.algorithm.log(f"Send #{len(parameters.targets)} targets. Success: {success}")
        return success

    def dispose(self):
        pass

'''
# To test the algorithm, you can create a simple Python Flask application (app.py) and run flask
# $ flask --app app run

# app.py:
from flask import Flask, request, jsonify
from json import loads
app = Flask(__name__)
@app.post('/')
def handle_positions():
    result = loads(request.data)
    print(result)
    return jsonify({'success': True,'message': f'{len(result)} positions received'})
if __name__ == '__main__':
    app.run(debug=True)
'''
