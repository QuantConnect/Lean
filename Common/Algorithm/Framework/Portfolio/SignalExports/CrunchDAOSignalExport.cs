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

using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of the desired positions to CrunchDAO API
    /// </summary>
    public class CrunchDAOSignalExport : ISignalExportTarget
    {
        /// <summary>
        /// CrunchDAO API endpoint
        /// </summary>
        private readonly Uri _destination;

        /// <summary>
        /// Model ID or Name
        /// </summary>
        private readonly string _model;

        /// <summary>
        /// Submission Name (Optional)
        /// </summary>
        private readonly string _submissionName;

        /// <summary>
        /// Comment (Optional)
        /// </summary>
        private readonly string _comment;

        /// <summary>
        /// User's securities
        /// </summary>
        private readonly SecurityManager _securities;

        /// <summary>
        /// Http client to make POST requests to CrunchDAO API
        /// </summary>
        private static HttpClient _client;

        /// <summary>
        /// CrunchDAOSignalExport constructor. It obtains the required information for CrunchDAO API requests.
        /// See (https://colab.research.google.com/drive/1YW1xtHrIZ8ZHW69JvNANWowmxPcnkNu0?authuser=1#scrollTo=aPyWNxtuDc-X)
        /// </summary>
        /// <param name="apiKey">API key provided by CrunchDAO</param>
        /// <param name="model">Model ID or Name</param>
        /// <param name="securities">User's securities</param>
        /// <param name="submissionName">Submission Name (Optional)</param>
        /// <param name="comment">Comment (Optional)</param>
        public CrunchDAOSignalExport(string apiKey, string model, SecurityManager securities, string submissionName = "", string comment = "")
        {
            _model = model;
            _securities = securities;
            _submissionName = submissionName;
            _comment = comment;
            _destination = new Uri($"https://api.tournament.crunchdao.com/v3/alpha-submissions?apiKey={apiKey}");
            _client = new HttpClient();
        }

        /// <summary>
        /// Verifies every holding is a stock, creates a message with the desired positions
        /// using the expected CrunchDAO API format and then sends it with the other required
        /// body features
        /// </summary>
        /// <param name="holdings">A list of holdings from the portfolio,
        /// expected to be sent to CrunchDAO API</param>
        /// <returns>The message with the positions sent to CrunchDAO API. 
        /// This is only used by test means</returns>
        /// <exception cref="ArgumentException">If holding list is empty it throws this exception</exception>
        public string Send(List<PortfolioTarget> holdings)
        {
            if (holdings.Count == 0)
            {
                throw new ArgumentException("Portfolio target is empty");
            }

            VerifyTargetsAreStocks(holdings);
            var positions = ConvertToCSVFormat(holdings);
            SendPositions(positions);

            return positions;
        }

        /// <summary>
        /// Verifies every holding in the given list is a stock or an index
        /// </summary>
        /// <param name="holdings">A list of holdings from the portfolio,
        /// expected to be sent to CrunchDAO API</param>
        /// <exception cref="ArgumentException">Throws this exception when it finds a holding type different than stock</exception>
        private static void VerifyTargetsAreStocks(List<PortfolioTarget> holdings)
        {
            foreach (var signal in holdings)
            {
                if (signal.Symbol.SecurityType != SecurityType.Equity && signal.Symbol.SecurityType != SecurityType.Index)
                {
                    throw new ArgumentException($"{signal.Symbol.SecurityType} security type is not implemented: CrunchDao only accepts signals for US Equities");
                }
            }
        }

        /// <summary>
        /// Converts the list of holdings into a CSV format string
        /// </summary>
        /// <param name="holdings">A list of holdings from the portfolio,
        /// expected to be sent to CrunchDAO API</param>
        /// <returns>A CSV format string of the given holdings with the required features(ticker, date, signal)</returns>
        protected string ConvertToCSVFormat(List<PortfolioTarget> holdings)
        {
            var positions = "ticker,date,signal\n";

            foreach (var holding in holdings)
            {
                positions += $"{holding.Symbol},{_securities[holding.Symbol].LocalTime.ToString("yyyy-MM-dd")},{holding.Quantity}\n";
            }

            return positions;
        }

        /// <summary>
        /// Sends the desired positions, with the other required features, to CrunchDAO API using a POST request. It logs
        /// the message retrieved by the API if there was a HttpRequestException
        /// </summary>
        /// <param name="positions">A CSV format string of the given holdings with the required features</param>
        private async void SendPositions(string positions)
        {
            // Create positions stream
            var positionsStream = new MemoryStream();
            var writer = new StreamWriter(positionsStream);
            writer.Write(positions);
            writer.Flush();
            positionsStream.Position = 0;

            // Create the required body features for the POST request
            var file = new StreamContent(positionsStream);
            var model = new StringContent(_model);
            var submissionName = new StringContent(_submissionName);
            var comment = new StringContent(_comment);

            // Crete the httpMessage to be sent and add the different POST request body features
            using var httpMessage = new MultipartFormDataContent
            {
                { model, "model" },
                { submissionName, "label" },
                { comment, "comment" },
                { file, "file", "submission.csv" }
            };

            // Send the httpMessage
            using HttpResponseMessage response = await _client.PostAsync(_destination, httpMessage).ConfigureAwait(true);
            if(!response.IsSuccessStatusCode)
            {
                Log.Trace($"HttpRequestException: {response.StatusCode}");
            }

            // Dispose the resources used to create and send the Http message
            writer.Dispose();
            file.Dispose();
            model.Dispose();
            submissionName.Dispose();
            comment.Dispose();
            httpMessage.Dispose();
        }
    }
}
