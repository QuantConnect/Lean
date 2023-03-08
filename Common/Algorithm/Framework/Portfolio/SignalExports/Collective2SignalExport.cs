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
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of desired positions to Collective2 API using JSON and HTTPS
    /// </summary>
    public class Collective2SignalExport : ISignalExportTarget
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
        /// User's portfolio
        /// </summary>
        private readonly SecurityPortfolioManager _portfolio;

        /// <summary>
        /// Http client used to make POST requests to Collective2 API
        /// </summary>
        private static HttpClient _client;

        /// <summary>
        /// Collective2SignalExport constructor. It obtains the entry information for Collective2 API requests.
        /// See (https://collective2.com/api-docs/latest)
        /// </summary>
        /// <param name="apiKey">API key provided by Collective2</param>
        /// <param name="systemId">Trading system's ID number</param>
        /// <param name="portfolio">User portfolio</param>
        /// <param name="platformId">Platform ID is only used in the Private Platform context</param>
        public Collective2SignalExport(string apiKey, int systemId, SecurityPortfolioManager portfolio, string platformId = null)
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

            _portfolio = portfolio;
            _client = new HttpClient();
        }

        /// <summary>
        /// Creates a JSON message with the desired positions using the expected
        /// Collective2 API format and then sends it
        /// </summary>
        /// <param name="holdings">A list of holdings from the portfolio 
        /// expected to be sent to Collective2 API</param>
        /// <returns>Returns the message sent. This is used mainly by test means</returns>
        public string Send(List<PortfolioTarget> holdings)
        {
            if (holdings.Count == 0)
            {
                throw new ArgumentException("Portfolio target list is empty");
            }

            var positions = ConvertHoldingsToCollective2(holdings);
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
            // TODO:  We use PortfolioTarget.Percent to calculate it really
            var assetValue = target.Quantity * _portfolio.TotalPortfolioValue;
            var numberShares = (int)(assetValue * _portfolio[target.Symbol].Price);

            if (_portfolio[target.Symbol].IsShort)
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
        /// Sends the desired positions list in JSON format to Collective2 API using a POST request. Then logs
        /// the message retrieved by the Collective2 API
        /// </summary>
        /// <param name="message">A JSON request of the desired positions list with the credentials</param>
        private async void SendPositions(string message)
        {
            var httpMessage = new StringContent(message, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _client.PostAsync(_destination, httpMessage).ConfigureAwait(true);

            if (!response.IsSuccessStatusCode)
            {
                Log.Trace($"HttpRequestException: {response.StatusCode}");
            }

            httpMessage.Dispose();
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
