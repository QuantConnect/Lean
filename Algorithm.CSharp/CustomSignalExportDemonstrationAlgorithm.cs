/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using Newtonsoft.Json;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Api;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends a list of portfolio targets to custom endpoint
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class CustomSignalExportDemonstrationAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialize the date and add all equity symbols present
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            /// Our custom signal export accepts all asset types
            AddEquity("SPY", Resolution.Second);
            AddForex("EURUSD", Resolution.Second);
            AddFutureContract(QuantConnect.Symbol.CreateFuture("ES", Market.CME, new DateTime(2023, 12, 15), null));
            AddOptionContract(QuantConnect.Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 130, new DateTime(2023, 9, 1)));
            
            // Set CustomSignalExport signal export provider.
            SignalExport.AddSignalExportProvider(new CustomSignalExport());
        }

        /// <summary>
        /// Buy and hold EURUSD and SPY
        /// </summary>
        /// <param name="slice"></param>
        public override void OnData(Slice slice)
        {
            foreach (var ticker in new[] { "SPY", "EURUSD" })
            {
                if (!Portfolio[ticker].Invested && Securities[ticker].HasData)
                {
                    SetHoldings(ticker, 0.5m);
                }
            }
        }
    }

    internal class CustomSignalExport : ISignalExportTarget
    {
        private readonly Uri _requestUri = new ("http://localhost:5000/");
        private readonly HttpClient _httpClient = new();

        public bool Send(SignalExportTargetParameters parameters)
        {
            var message = JsonConvert.SerializeObject(parameters.Targets);
            using var httpMessage = new StringContent(message, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = _httpClient.PostAsync(_requestUri, httpMessage).Result;
            var result = response.Content.ReadFromJsonAsync<RestResponse>().Result;
            return result.Success;
        }

        public void Dispose() => _httpClient.Dispose();
    }
}

/*
# $ flask --app app run 

# app.py:
from flask import Flask, request, jsonify
from json import loads
app = Flask(__name__)
@app.post('/')
def handle_positions():
    result = loads(request.data)
    return jsonify({'success': True,'message': f'{len(result)} positions received'})
if __name__ == '__main__':
    app.run(debug=True)
*/
