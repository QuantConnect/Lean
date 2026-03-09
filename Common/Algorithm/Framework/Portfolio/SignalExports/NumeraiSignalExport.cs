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
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Exports signals of the desired positions to Numerai API.
    /// Accepts signals in percentage i.e numerai_ticker:"IBM US", signal:0.234
    /// </summary>
    /// <remarks>It does not take into account flags as 
    /// NUMERAI_COMPUTE_ID (https://github.com/numerai/numerapi/blob/master/numerapi/signalsapi.py#L164) and 
    /// TRIGGER_ID(https://github.com/numerai/numerapi/blob/master/numerapi/signalsapi.py#L164)</remarks>
    public class NumeraiSignalExport : BaseSignalExport
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
        /// Algorithm being ran
        /// </summary>
        private IAlgorithm _algorithm;

        /// <summary>
        /// Dictionary to obtain corresponding Numerai Market name for the given LEAN market name
        /// </summary>
        private readonly Dictionary<string, string> _numeraiMarketFormat = new() // There can be stocks from other markets
        {
            {Market.USA, "US" },
            {Market.SGX, "SP" }
        };

        /// <summary>
        /// Hashset of Numerai allowed SecurityTypes
        /// </summary>
        private readonly HashSet<SecurityType> _allowedSecurityTypes = new()
        {
            SecurityType.Equity
        };

        /// <summary>
        /// The name of this signal export
        /// </summary>
        protected override string Name { get; } = "Numerai";

        /// <summary>
        /// Hashset property of Numerai allowed SecurityTypes
        /// </summary>
        protected override HashSet<SecurityType> AllowedSecurityTypes => _allowedSecurityTypes;

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
        }

        /// <summary>
        /// Verifies all the given holdings are accepted by Numerai, creates a message with those holdings in the expected
        /// Numerai API format and sends them to Numerai API
        /// </summary>
        /// <param name="parameters">A list of portfolio holdings expected to be sent to Numerai API and the algorithm being ran</param>
        /// <returns>True if the positions were sent to Numerai API correctly and no errors were returned, false otherwise</returns>
        public override bool Send(SignalExportTargetParameters parameters)
        {
            if (!base.Send(parameters))
            {
                return false;
            }

            _algorithm = parameters.Algorithm;

            if (parameters.Targets.Count < 10)
            {
                _algorithm.Error($"Numerai Signals API accepts minimum 10 different signals, just found {parameters.Targets.Count}");
                return false;
            }

            if (!ConvertTargetsToNumerai(parameters, out string positions))
            {
                return false;
            }
            var result = SendPositions(positions);

            return result;
        }

        /// <summary>
        /// Verifies each holding's signal is between 0 and 1 (exclusive)
        /// </summary>
        /// <param name="parameters">A list of portfolio holdings expected to be sent to Numerai API</param>
        /// <param name="positions">A message with the desired positions in the expected Numerai API format</param>
        /// <returns>True if a string message with the positions could be obtained, false otherwise</returns>
        protected bool ConvertTargetsToNumerai(SignalExportTargetParameters parameters, out string positions)
        {
            positions = "numerai_ticker,signal\n";
            foreach ( var holding in parameters.Targets)
            {
                if (holding.Quantity <= 0 || holding.Quantity >= 1)
                {
                    _algorithm.Error($"All signals must be between 0 and 1 (exclusive), but {holding.Symbol.Value} signal was {holding.Quantity}");
                    return false;
                }

                positions += $"{parameters.Algorithm.Ticker(holding.Symbol)} {_numeraiMarketFormat[holding.Symbol.ID.Market]},{holding.Quantity.ToStringInvariant()}\n";
            }

            return true;
        }

        /// <summary>
        /// Sends the given positions message to Numerai API. It first sends an authentication POST request then a
        /// PUT request to put the positions in certain endpoint and finally sends a submission POST request 
        /// </summary>
        /// <param name="positions">A message with the desired positions in the expected Numerai API format</param>
        /// <returns>True if the positions were sent to Numerai API correctly and no errors were returned, false otherwise</returns>
        private bool SendPositions(string positions)
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

            using var variables = new StringContent(argumentsMessage, Encoding.UTF8, "application/json");
            using var query = new StringContent(authQuery, Encoding.UTF8, "application/json");

            var httpMessage = new MultipartFormDataContent
            {
                { query, "query"},
                { variables, "variables" }
            };

            using var authRequest = new HttpRequestMessage(HttpMethod.Post, _destination);
            authRequest.Headers.Add("Accept", "application/json");
            authRequest.Headers.Add("Authorization", $"Token {_publicId}${_secretId}");
            authRequest.Content = httpMessage;
            var response = HttpClient.SendAsync(authRequest).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            if (!response.IsSuccessStatusCode)
            {
                _algorithm.Error($"Numerai API returned HttpRequestException {response.StatusCode}");
                return false;
            }

            var parsedResponseContent = JObject.Parse(responseContent);
            if (!parsedResponseContent["data"]["submissionUploadSignalsAuth"].HasValues)
            {
                _algorithm.Error($"Numerai API returned the following errors: {string.Join(",", parsedResponseContent["errors"])}");
                return false;
            }

            var putUrl = new Uri((string)parsedResponseContent["data"]["submissionUploadSignalsAuth"]["url"]);
            var submissionFileName = (string)parsedResponseContent["data"]["submissionUploadSignalsAuth"]["filename"];

            // PUT REQUEST
            // Create positions stream
            var positionsStream = new MemoryStream();
            using var writer = new StreamWriter(positionsStream);
            writer.Write(positions);
            writer.Flush();
            positionsStream.Position = 0;            
            using var putRequest = new HttpRequestMessage(HttpMethod.Put, putUrl)
            {
                Content = new StreamContent(positionsStream)
            };
            var putResponse = HttpClient.SendAsync(putRequest).Result;

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

            using var submissionQuery = new StringContent(createQuery, Encoding.UTF8, "application/json");
            using var submissionVariables = new StringContent(createArgumentsMessage, Encoding.UTF8, "application/json");

            var submissionMessage = new MultipartFormDataContent
            {
                {submissionQuery, "query"},
                {submissionVariables, "variables"}
            };

            using var submissionRequest = new HttpRequestMessage(HttpMethod.Post, _destination);
            submissionRequest.Headers.Add("Authorization", $"Token {_publicId}${_secretId}");
            submissionRequest.Content = submissionMessage;
            var submissionResponse = HttpClient.SendAsync(submissionRequest).Result;
            var submissionResponseContent = submissionResponse.Content.ReadAsStringAsync().Result;
            if (!submissionResponse.IsSuccessStatusCode)
            {
                _algorithm.Error($"Numerai API returned HttpRequestException {submissionResponseContent}");
                return false;
            }

            var parsedSubmissionResponseContent = JObject.Parse(submissionResponseContent);
            if (!parsedSubmissionResponseContent["data"]["createSignalsSubmission"].HasValues)
            {
                _algorithm.Error($"Numerai API returned the following errors: {string.Join(",", parsedSubmissionResponseContent["errors"])}");
                return false;
            }

            return true;
        }
    }
}
