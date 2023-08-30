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
using QuantConnect.Interfaces;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of desired positions to Collective2 API using JSON and HTTPS.
    /// Accepts signals in quantity(number of shares) i.e symbol:"SPY", quant:40
    /// </summary>
    public class Collective2SignalExport : BaseSignalExport
    {
        /// <summary>
        /// API key provided by Collective2
        /// </summary>
        private readonly string _apiKey;

        /// <summary>
        /// Trading system's ID number
        /// </summary>
        private readonly int _systemId;

        /// <summary>
        /// Collective2 API endpoint
        /// </summary>
        private readonly Uri _destination;

        /// <summary>
        /// Algorithm being ran
        /// </summary>
        private IAlgorithm _algorithm;

        /// <summary>
        /// The name of this signal export
        /// </summary>
        protected override string Name { get; } = "Collective2";

        /// <summary>
        /// Lazy initialization of ten seconds rate limiter
        /// </summary>
        private static Lazy<RateGate> _tenSecondsRateLimiter = new Lazy<RateGate>(() => new RateGate(100, TimeSpan.FromMilliseconds(1000)));

        /// <summary>
        /// Lazy initialization of one hour rate limiter
        /// </summary>
        private static Lazy<RateGate> _hourlyRateLimiter = new Lazy<RateGate>(() => new RateGate(1000, TimeSpan.FromHours(1)));

        /// <summary>
        /// Lazy initialization of one day rate limiter
        /// </summary>
        private static Lazy<RateGate> _dailyRateLimiter = new Lazy<RateGate>(() => new RateGate(20000, TimeSpan.FromDays(1)));


        /// <summary>
        /// Collective2SignalExport constructor. It obtains the entry information for Collective2 API requests.
        /// See API documentation at https://trade.collective2.com/c2-api
        /// </summary>
        /// <param name="apiKey">API key provided by Collective2</param>
        /// <param name="systemId">Trading system's ID number</param>
        public Collective2SignalExport(string apiKey, int systemId)
        {
            _apiKey = apiKey;
            _systemId = systemId;
            _destination = new Uri("https://api4-general.collective2.com/Strategies/SetDesiredPositions");
        }

        /// <summary>
        /// Creates a JSON message with the desired positions using the expected
        /// Collective2 API format and then sends it
        /// </summary>
        /// <param name="parameters">A list of holdings from the portfolio 
        /// expected to be sent to Collective2 API and the algorithm being ran</param>
        /// <returns>True if the positions were sent correctly and Collective2 sent no errors, false otherwise</returns>
        public override bool Send(SignalExportTargetParameters parameters)
        {
            if (!base.Send(parameters))
            {
                return false;
            }

            if (!ConvertHoldingsToCollective2(parameters, out List<Collective2Position> positions))
            {
                return false;
            }
            var message = CreateMessage(positions);
            _tenSecondsRateLimiter.Value.WaitToProceed();
            _hourlyRateLimiter.Value.WaitToProceed();
            _dailyRateLimiter.Value.WaitToProceed();
            var result = SendPositions(message);

            return result;
        }

        /// <summary>
        /// Converts a list of targets to a list of Collective2 positions
        /// </summary>
        /// <param name="parameters">A list of targets from the portfolio 
        /// expected to be sent to Collective2 API and the algorithm being ran</param>
        /// <param name="positions">A list of Collective2 positions</param>
        /// <returns>True if the given targets could be converted to a Collective2Position list, false otherwise</returns>
        protected bool ConvertHoldingsToCollective2(SignalExportTargetParameters parameters, out List<Collective2Position> positions)
        {
            _algorithm = parameters.Algorithm;
            var targets = parameters.Targets;
            positions = new List<Collective2Position>();
            foreach (var target in targets)
            {
                if (target == null)
                {
                    _algorithm.Error("One portfolio target was null");
                    return false;
                }

                if (!ConvertTypeOfSymbol(target.Symbol, out string typeOfSymbol))
                {
                    return false;
                }

                var symbol = _algorithm.Ticker(target.Symbol);
                if (target.Symbol.SecurityType == SecurityType.Future)
                {
                    symbol = $"@{SymbolRepresentation.GenerateFutureTicker(target.Symbol.ID.Symbol, target.Symbol.ID.Date, doubleDigitsYear: false, includeExpirationDate: false)}";
                }
                else if (target.Symbol.SecurityType.IsOption())
                {
                    symbol = SymbolRepresentation.GenerateOptionTicker(target.Symbol);
                }

                positions.Add(new Collective2Position
                {
                    C2Symbol = new C2Symbol
                    {
                        FullSymbol = symbol,
                        SymbolType = typeOfSymbol,
                    },
                    Quantity = ConvertPercentageToQuantity(_algorithm, target),
                });
            }

            return true;
        }

        /// <summary>
        /// Classifies a symbol type into the possible symbol types values defined
        /// by Collective2 API.
        /// </summary>
        /// <param name="targetSymbol">Symbol of the desired position</param>
        /// <param name="typeOfSymbol">The type of the symbol according to Collective2 API</param>
        /// <returns>True if the symbol's type is supported by Collective2, false otherwise</returns>
        private bool ConvertTypeOfSymbol(Symbol targetSymbol, out string typeOfSymbol)
        {
            switch (targetSymbol.SecurityType)
            {
                case SecurityType.Equity:
                    typeOfSymbol = "stock";
                    break;
                case SecurityType.Option:
                    typeOfSymbol = "option";
                    break;
                case SecurityType.Future:
                    typeOfSymbol = "future";
                    break;
                case SecurityType.Forex:
                    typeOfSymbol = "forex";
                    break;
                default:
                    typeOfSymbol = "NotImplemented";
                    break;
            }

            if (typeOfSymbol == "NotImplemented")
            {
                _algorithm.Error($"{targetSymbol.SecurityType} security type is not supported by Collective2.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Converts a given percentage of a position into the number of shares of it
        /// </summary>
        /// <param name="algorithm">Algorithm being ran</param>
        /// <param name="target">Desired position to be sent to the Collective2 API</param>
        /// <returns>Number of shares hold of the given position/returns>
        protected int ConvertPercentageToQuantity(IAlgorithm algorithm, PortfolioTarget target)
        {
            var numberShares = PortfolioTarget.Percent(algorithm, target.Symbol, target.Quantity);
            if (numberShares == null)
            {
                throw new InvalidOperationException($"Collective2 failed to calculate target quantity for {target}");
            }

            return (int)numberShares.Quantity;
        }

        /// <summary>
        /// Serializes the list of desired positions with the needed credentials in JSON format
        /// </summary>
        /// <param name="positions">List of Collective2 positions to be sent to Collective2 API</param>
        /// <returns>A JSON request string of the desired positions to be sent by a POST request to Collective2 API</returns>
        protected string CreateMessage(List<Collective2Position> positions)
        {
            var payload = new
            {
                StrategyId = _systemId,
                Positions = positions,
            };

            var jsonMessage = JsonConvert.SerializeObject(payload);
            return jsonMessage;
        }

        /// <summary>
        /// Sends the desired positions list in JSON format to Collective2 API using a POST request. It logs
        /// the message retrieved by the Collective2 API if there was a HttpRequestException
        /// </summary>
        /// <param name="message">A JSON request string of the desired positions list with the credentials</param>
        /// <returns>True if the positions were sent correctly and Collective2 API sent no errors, false otherwise</returns>
        private bool SendPositions(string message)
        {
            using var httpMessage = new StringContent(message, Encoding.UTF8, "application/json");

            //Add the QuantConnect app header
            httpMessage.Headers.Add("X-AppId", "OPA1N90E71");

            //Add the Authorization header
            HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            //Send the message
            using HttpResponseMessage response = HttpClient.PostAsync(_destination, httpMessage).Result;

            //Parse it
            var responseObject = response.Content.ReadFromJsonAsync<C2Response>().Result;

            if (!response.IsSuccessStatusCode)
            {
                _algorithm.Error($"Collective2 API returned the following errors: {string.Join(",", PrintErrors(responseObject.ResponseStatus.Errors))}");
                return false;
            }
            else if (responseObject.Results.Count > 0)
            {
                _algorithm.Debug($"Collective2: NewSignals={string.Join(',', responseObject.Results[0].NewSignals)} | CanceledSignals={string.Join(',', responseObject.Results[0].CanceledSignals)}");
            }
            
            return true;
        }

        private static string PrintErrors(List<ResponseError> errors)
        {
            if (errors?.Count == 0)
            {
                return "NULL";
            }

            StringBuilder sb = new StringBuilder();
            foreach (var error in errors)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"({error.ErrorCode}) {error.FieldName}: {error.Message}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// The main C2 response class for this endpoint
        /// </summary>
        private class C2Response
        {
            [JsonProperty(PropertyName = "Results")]
            public virtual List<DesiredPositionResponse> Results { get; set; }


            [JsonProperty(PropertyName = "ResponseStatus")]
            public ResponseStatus ResponseStatus { get; set; }
        }

        /// <summary>
        /// The Results object
        /// </summary>
        private class DesiredPositionResponse
        {
            [JsonProperty(PropertyName = "NewSignals")]
            public List<long> NewSignals { get; set; } = new List<long>();


            [JsonProperty(PropertyName = "CanceledSignals")]
            public List<long> CanceledSignals { get; set; } = new List<long>();
        }

        /// <summary>
        /// The C2 ResponseStatus object
        /// </summary>
        private class ResponseStatus
        {
            /* Example:

                    "ResponseStatus": 
                    {
                      "ErrorCode": ""401",
                      "Message": ""Unauthorized",
                      "Errors": [
                        {
                          "ErrorCode": "2015",
                          "FieldName": "APIKey",
                          "Message": ""Unknown API Key"
                        }
                      ]
                    }
            */


            [JsonProperty(PropertyName = "ErrorCode")]
            public string ErrorCode { get; set; }


            [JsonProperty(PropertyName = "Message")]
            public string Message { get; set; }


            [JsonProperty(PropertyName = "Errors")]
            public List<ResponseError> Errors { get; set; }

        }

        /// <summary>
        /// The ResponseError object
        /// </summary>
        private class ResponseError
        {
            [JsonProperty(PropertyName = "ErrorCode")]
            public string ErrorCode { get; set; }


            [JsonProperty(PropertyName = "FieldName")]
            public string FieldName { get; set; }


            [JsonProperty(PropertyName = "Message")]
            public string Message { get; set; }
        }

        /// <summary>
        /// Stores position's needed information to be serialized in JSON format
        /// and then sent to Collective2 API
        /// </summary>
        protected class Collective2Position
        {
            /// <summary>
            /// Position symbol
            /// </summary>
            [JsonProperty(PropertyName = "C2Symbol")]
            public C2Symbol C2Symbol { get; set; }

            /// <summary>
            /// Number of shares/contracts of the given symbol. Positive quantites are long positions
            /// and negative short positions.
            /// </summary>
            [JsonProperty(PropertyName = "Quantity")]
            public decimal Quantity { get; set; } // number of shares, not % of the portfolio
        }

        /// <summary>
        /// The Collective2 symbol
        /// </summary>
        protected class C2Symbol
        {
            /// <summary>
            /// The The full native C2 symbol e.g. BSRR2121Q22.5
            /// </summary>
            [JsonProperty(PropertyName = "FullSymbol")]
            public string FullSymbol { get; set; }


            /// <summary>
            /// The type of instrument. e.g. 'stock', 'option', 'future', 'forex'
            /// </summary>
            [JsonProperty(PropertyName = "SymbolType")]
            public string SymbolType { get; set; }
        }
    }
}
