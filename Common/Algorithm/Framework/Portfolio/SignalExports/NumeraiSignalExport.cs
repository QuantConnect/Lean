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
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of the desired positions to Numerai API.
    /// </summary>
    /// <remarks>It does not take into account flags as 
    /// NUMERAI_COMPUTE_ID (https://github.com/numerai/numerapi/blob/master/numerapi/signalsapi.py#L164) and 
    /// TRIGGER_ID(https://github.com/numerai/numerapi/blob/master/numerapi/signalsapi.py#L164)</remarks>
    public class NumeraiSignalExport : ISignalExportTarget
    {
        /// <summary>
        /// Numerai API submission endpoint
        /// </summary>
        private readonly Uri _destination;

        /// <summary>
        /// PUBLIC_ID provided by Numerai
        /// </summary>
        private readonly string _publicId;

        /// <summary>
        /// SECRET_ID provided by Numerai
        /// </summary>
        private readonly string _secretId;

        /// <summary>
        /// ID of the Numerai Model being used
        /// </summary>
        private readonly string _modelId;

        /// <summary>
        /// Signal file's name
        /// </summary>
        private readonly string _fileName;

        /// <summary>
        /// HttpClient used to send the signals to Numerai API
        /// </summary>
        private static HttpClient _httpClient;

        /// <summary>
        /// Dictionary to obtain corresponding Numerai Market name for the given LEAN market name
        /// </summary>
        private readonly Dictionary<string, string> _numeraiMarketFormat = new() // There can be stocks from other markets
        {
            {Market.USA, "US" },
            {Market.SGX, "SP" }
        };

        /// <summary>
        /// NumeraiSignalExport Constructor. It obtains the required information for Numerai API requests
        /// </summary>
        /// <param name="publicId">PUBLIC_ID provided by Numerai</param>
        /// <param name="secretId">SECRET_ID provided by Numerai</param>
        /// <param name="modelId">ID of the Numerai Model being used</param>
        /// <param name="fileName">Signal file's name</param>
        public NumeraiSignalExport(string publicId, string secretId, string modelId, string fileName = "predictions.csv")
        {
            _destination = new Uri("https://api-tournament.numer.ai");
            _publicId = publicId;
            _secretId = secretId;
            _modelId = modelId;
            _fileName = fileName;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Verifies all the given holdings are accepted by Numerai, creates a message with those holdings in the expected
        /// Numerai API format and sends them to Numerai API
        /// </summary>
        /// <param name="holdings">A list of portfolio holdings expected to be sent to Numerai API</param>
        /// <returns>The created holdings message in the expected Numerai API format</returns>
        /// <exception cref="ArgumentException">It throws an exception if there is less than 10 different signals</exception>
        public string Send(List<PortfolioTarget> holdings)
        {
            if (holdings.Count < 10)
            {
                throw new ArgumentException($"Numerai Signals API accepts minimum 10 different signals, just found {holdings.Count}");
            }

            CrunchDAOSignalExport.VerifyTargetsAreStocks(holdings);
            var positions = ConvertTargetsToNumerai(holdings);
            SendPositions(positions);

            return positions;
        }

        /// <summary>
        /// Verifies each holding's signal is between 0 and 1 (exclusive) and returns a message with the holdings in the expected Numerai
        /// API format.
        /// </summary>
        /// <param name="holdings">A list of portfolio holdings expected to be sent to Numerai API</param>
        /// <returns>A message with the desired positions in the expected Numerai API format</returns>
        /// <exception cref="ArgumentException">It throws an exception whenever it finds a holding's signal that's not between 0 and 1
        /// (exclusive). It also throws an exception if the given market is not supported yet by LEAN</exception>
        private string ConvertTargetsToNumerai(List<PortfolioTarget> holdings)
        {
            var positions = "numerai_ticker,signal\n";
            foreach ( var holding in holdings )
            {
                if (holding.Quantity <= 0 || holding.Quantity >= 1)
                {
                    throw new ArgumentException($"All signals must be between 0 and 1 (exclusive, but {holding.Symbol.Value} signal was {holding.Quantity}");
                }

                if (!_numeraiMarketFormat.ContainsKey(holding.Symbol.ID.Market))
                {
                    throw new ArgumentException($"LEAN does not support Market {holding.Symbol.ID.Market} yet");
                }
                positions += $"{holding.Symbol.Value} {_numeraiMarketFormat[holding.Symbol.ID.Market]},{holding.Quantity}\n";
            }

            return positions;
        }

        /// <summary>
        /// Sends the given positions message to Numerai API. It first sends an authentication POST request then a
        /// PUT request to put the positions in certain endpoint and finally sends a submission POST request 
        /// </summary>
        /// <param name="positions">A message with the desired positions in the expected Numerai API format</param>
        private async void SendPositions(string positions)
        {
            // AUTHENTICATION REQUEST
            var authQuery = @"query($filename: String!
                  $modelId: String) {
              submissionUploadSignalsAuth(filename: $filename
                                        modelId: $modelId) {
                    filename
                    url
                }
            }";

            var arguments = new
            {
                filename = _fileName,
                modelId = _modelId
            };
            var argumentsMessage = JsonConvert.SerializeObject(arguments);

            var variables = new StringContent(argumentsMessage, Encoding.UTF8, "application/json");
            var query = new StringContent(authQuery, Encoding.UTF8, "application/json");

            using var httpMessage = new MultipartFormDataContent
            {
                { query, "query"},
                { variables, "variables" }
            };

            var authRequest = new HttpRequestMessage(HttpMethod.Post, _destination);
            authRequest.Headers.Add("Accept", "application/json");
            authRequest.Headers.Add("Authorization", $"Token {_publicId}${_secretId}");
            authRequest.Content = httpMessage;
            using var response = await _httpClient.SendAsync(authRequest).ConfigureAwait(true);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            if (!response.IsSuccessStatusCode)
            {
                Log.Trace($"HttpRequestException: {responseContent}");
            }

            var body = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Submission>>>(responseContent);
            var putUrl = new Uri(body["data"]["submissionUploadSignalsAuth"].url);
            var submissionFileName = body["data"]["submissionUploadSignalsAuth"].filename;

            // PUT REQUEST
            // Create positions stream
            var positionsStream = new MemoryStream();
            var writer = new StreamWriter(positionsStream);
            writer.Write(positions);
            writer.Flush();
            positionsStream.Position = 0;

            var putRequest = new HttpRequestMessage(HttpMethod.Put, putUrl)
            {
                Content = new StreamContent(positionsStream)
            };
            using var putResponse = await _httpClient.SendAsync(putRequest).ConfigureAwait(true);

            // SUBMISSION REQUEST
            var createQuery = @"mutation($filename: String!
                     $modelId: String
                     $triggerId: String) {
                createSignalsSubmission(filename: $filename
                                        modelId: $modelId
                                        triggerId: $triggerId
                                        source: ""numerapi"") {
                    id
                    firstEffectiveDate
                }
            }";

            var createArguments = new
            {
                filename = submissionFileName,
                modelId = _modelId
            };
            var createArgumentsMessage = JsonConvert.SerializeObject(createArguments);

            var submissionQuery = new StringContent(createQuery, Encoding.UTF8, "application/json");
            var submissionVariables = new StringContent(createArgumentsMessage, Encoding.UTF8, "application/json");

            using var submissionMessage = new MultipartFormDataContent
            {
                {submissionQuery, "query"},
                {submissionVariables, "variables"}
            };

            var submissionRequest = new HttpRequestMessage(HttpMethod.Post, _destination);
            submissionRequest.Headers.Add("Accept", "application/json");
            submissionRequest.Headers.Add("Authorization", $"Token {_publicId}${_secretId}");
            submissionRequest.Content = submissionMessage;
            using var submissionResponse = await _httpClient.SendAsync(submissionRequest).ConfigureAwait(true);
            var submissionResponseContent = await submissionResponse.Content.ReadAsStringAsync().ConfigureAwait(true);
            if (!submissionResponse.IsSuccessStatusCode)
            {
                Log.Trace($"HttpRequestException: {submissionResponseContent}");
            }

            // Dispose the unmanaged resources used
            variables.Dispose();
            query.Dispose();
            authRequest.Dispose();
            writer.Dispose();
            putRequest.Dispose();
            putResponse.Dispose();
            submissionQuery.Dispose();
            submissionVariables.Dispose();
            submissionRequest.Dispose();
            submissionResponse.Dispose();
        }

        /// <summary>
        /// Helper class to deserialize Numerai API authentication request
        /// </summary>
        private class Submission
        {
            /// <summary>
            /// New filename provided by Numerai API
            /// </summary>
            public string filename { get; set; }

            /// <summary>
            /// Numerai API endpoint to upload the desired positions
            /// </summary>
            public string url { get; set; }
        }
    }
}
