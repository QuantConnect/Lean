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
        /// <returns>True if the positions were sent correctly and Collective2 sent no errors, false otherwise</returns>
        public override bool Send(SignalExportTargetParameters parameters)
        {
            if (parameters.Targets.Count == 0)
            {
                Log.Trace("Collective2SignalExport.Send(): Portfolio target list is empty");
                return false;
            }

            if (ConvertHoldingsToCollective2(parameters, out List<Collective2Position> positions)) return false;
            var message = CreateMessage(positions);
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
            var algorithm = parameters.Algorithm;
            var targets = parameters.Targets;
            positions = new List<Collective2Position>();
            foreach (var target in targets)
            {
                if (target == null)
                {
                    Log.Trace("Collective2SignalExport.ConvertHoldingsToCollective2(): One portfolio target was null");
                    return false;
                }

                if (!ConvertTypeOfSymbol(target.Symbol, out string typeOfSymbol)) return false;
                positions.Add(new Collective2Position { Symbol = target.Symbol, TypeOfSymbol = typeOfSymbol, Quant = ConvertPercentageToQuantity(algorithm, target) });
            }

            return true;
        }

        /// <summary>
        /// Classifies a symbol type into the possible symbol types values defined
        /// by Collective2 API.
        /// </summary>
        /// <param name="targetSymbol">Symbol of the desired position</param>
        /// <param name="typeOfSymbol">The type of the symbol according to Collective2 API</param>
        /// <returns>True if the symbol's type was allowed by Collective2, false otherwise</returns>
        private static bool ConvertTypeOfSymbol(Symbol targetSymbol, out string typeOfSymbol)
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
                case SecurityType.Index:
                    typeOfSymbol = "index";
                    break;
                case SecurityType.IndexOption:
                    typeOfSymbol = "option";
                    break;
                default:
                    typeOfSymbol = "NotImplemented";
                    break;
            }

            if (typeOfSymbol == "NotImplemented")
            {
                Log.Trace($"{targetSymbol.SecurityType} security type has not been implemented by Collective2 yet.");
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
            var numberShares = PortfolioTarget.Percent(algorithm, target.Symbol, target.Quantity).Quantity;

            if (numberShares == null)
            {
                throw new NullReferenceException("Collective2SignalExport.ConvertPercentageToQuantity(): PortfolioTarget.Percent() returned null");
            }
            return (int)numberShares;
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
                positions,
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
        /// <returns>True if the positions were sent correctly and Collective2 API sent no errors, false otherwise</returns>
        private bool SendPositions(string message)
        {
            using var httpMessage = new StringContent(message, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = HttpClient.PostAsync(_destination, httpMessage).Result;

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Collective2SignalExport.SendPositions(): Collective2 API returned HttpRequestException {response.StatusCode}");
                return false;
            }

            var responseContent = response.Content.ReadAsStringAsync().Result;
            var parsedResponseContent = JObject.Parse(responseContent);
            if (parsedResponseContent["error"].HasValues)
            {
                Log.Error($"Collective2SignalExport.SendPositions(): Collective2 API returned the following errors: {string.Join(",", parsedResponseContent["error"])}");
                return false;
            }

            return true;
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
            [JsonProperty(PropertyName = "symbol")]
            public string Symbol { get; set; }

            /// <summary>
            /// Type of symbol. It can be: stock, future, option or forex
            /// </summary>
            [JsonProperty(PropertyName = "typeofsymbol")]
            public string TypeOfSymbol { get; set; }

            /// <summary>
            /// Number of shares of the given symbol. Positive quantites are long positions
            /// and negative short positions
            /// </summary>
            [JsonProperty(PropertyName = "quant")]
            public int Quant { get; set; } // number of shares not % of the portfolio
        }
    }
}
