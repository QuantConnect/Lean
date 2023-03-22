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
using Newtonsoft.Json.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of desired positions to Collective2 API using JSON and HTTPS
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
        /// Collective2SignalExport constructor. It obtains the entry information for Collective2 API requests.
        /// See (https://collective2.com/api-docs/latest)
        /// </summary>
        /// <param name="apiKey">API key provided by Collective2</param>
        /// <param name="systemId">Trading system's ID number</param>
        /// <param name="platformId">Platform ID, it's only used in the Private Platform context</param>
        public Collective2SignalExport(string apiKey, int systemId, string platformId = null)
        {
            _apiKey = apiKey;
            _systemId = systemId;
            if (platformId == null)
            {
                _destination = new Uri("https://api.collective2.com/world/apiv3/setDesiredPositions");
            } 
            else
            {
                _destination = new Uri($"https://api.collective2.com/world/{platformId}/setDesiredPositions");
            }
        }

        /// <summary>
        /// Creates a JSON message with the desired positions using the expected
        /// Collective2 API format and then sends it
        /// </summary>
        /// <param name="parameters">A list of holdings from the portfolio 
        /// expected to be sent to Collective2 API and the algorithm being ran</param>
        /// <returns>Returns the message sent. This is used mainly by test means</returns>
        public override string Send(SignalExportTargetParameters parameters)
        {
            if (parameters.Targets.Count == 0)
            {
                throw new ArgumentException("Portfolio target list is empty");
            }

            _algorithm = parameters.Algorithm;

            var positions = ConvertHoldingsToCollective2(parameters.Targets);
            var message = CreateMessage(positions);
            SendPositions(message);

            return message;
        }

        /// <summary>
        /// Converts a list of targets to a list of Collective2 positions
        /// </summary>
        /// <param name="holdings">A list of holdings from the portfolio 
        /// expected to be sent to Collective2 API</param>
        /// <returns>A list of Collective2 positions</returns>
        protected List<Collective2Position> ConvertHoldingsToCollective2(List<PortfolioTarget> holdings)
        {
            var positions = new List<Collective2Position>();
            foreach (var target in holdings)
            {
                if (target == null)
                {
                    throw new ArgumentException("A target from PortfolioTarget was null");
                }

                positions.Add(new Collective2Position { symbol = target.Symbol, typeofsymbol = ConvertTypeOfSymbol(target.Symbol), quant = ConvertPercentageToQuantity(target) });
            }

            return positions;
        }

        /// <summary>
        /// Classifies a symbol type into the possible symbol types values defined
        /// by Collective2 API. If the symbol type is not allowed by Collective2 API
        /// it throws an Argument Exception
        /// </summary>
        /// <param name="targetSymbol">Symbol of the desired position</param>
        /// <returns>The type of the symbol according to Collective2 API</returns>
        private static string ConvertTypeOfSymbol(Symbol targetSymbol)
        {
            return targetSymbol.SecurityType switch
            {
                SecurityType.Equity => "stock",
                SecurityType.Option => "option",
                SecurityType.Future => "future",
                SecurityType.Forex => "forex",
                SecurityType.Index => "stock",
                SecurityType.IndexOption => "option",
                _ => throw new ArgumentException($"{targetSymbol.SecurityType} security type has not been implemented by Collective2 yet.")
            };
        }

        /// <summary>
        /// Converts a given percentage of a position into the number of shares of it
        /// </summary>
        /// <param name="target">Desired position to be sent to the Collective2 API</param>
        /// <returns>Number of shares hold of the given position/returns>
        private int ConvertPercentageToQuantity(PortfolioTarget target)
        {
            var numberShares = (int)(PortfolioTarget.Percent(_algorithm, target.Symbol, target.Quantity).Quantity);

            if (_algorithm.Portfolio[target.Symbol].IsShort)
            {
                numberShares *= -1;
            }

            return numberShares;
        }

        /// <summary>
        /// Serializes the list of desired positions with the needed credentials in JSON format
        /// </summary>
        /// <param name="_positions">List of Collective2 positions to be sent to Collective2 API</param>
        /// <returns>A JSON request string of the desired positions to be sent by a POST request to Collective2 API</returns>
        private string CreateMessage(List<Collective2Position> _positions)
        {
            var payload = new
            {
                positions = _positions,
                systemid = _systemId,
                apikey = _apiKey
            };

            var jsonMessage = JsonConvert.SerializeObject(payload);
            return jsonMessage;
        }

        /// <summary>
        /// Sends the desired positions list in JSON format to Collective2 API using a POST request. It logs
        /// the message retrieved by the Collective2 API if there was a HttpRequestException
        /// </summary>
        /// <param name="message">A JSON request string of the desired positions list with the credentials</param>
        private void SendPositions(string message)
        {
            using var httpMessage = new StringContent(message, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = HttpClient.PostAsync(_destination, httpMessage).Result;

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Collective2SignalExport.SendPositions(): Collective2 API returned HttpRequestException {response.StatusCode} at line 182");
                return;
            }

            var responseContent = response.Content.ReadAsStringAsync().Result;
            var parsedResponseContent = JObject.Parse(responseContent);
            if (parsedResponseContent["error"].HasValues)
            {
                Log.Error($"Collective2SignalExport.SendPositions(): Collective2 API returned the following errors: {String.Join(",", parsedResponseContent["error"])} at line 182");
            }
        }

        /// <summary>
        /// Stores position's needed information to be serialized in JSON format
        /// and then sent to Collective2 API
        /// </summary>
        public class Collective2Position
        {
            /// <summary>
            /// Position symbol
            /// </summary>
            public string symbol;

            /// <summary>
            /// Type of symbol. It can be: stock, future, option or forex
            /// </summary>
            public string typeofsymbol;

            /// <summary>
            /// Number of shares of the given symbol. Positive quantites are long positions
            /// and negative short positions
            /// </summary>
            public int quant; // number of shares not % of the portfolio
        }
    }
}
